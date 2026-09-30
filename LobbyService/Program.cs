using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

// In-memory rooms. /lobby is signaling plus accounts. /relay copies game frames and does not read them.
// Closing /lobby does not abort /relay. A relay seat drops only when that relay socket ends.
// Restart clears every room. Listens on 0.0.0.0:7788.

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:7788");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var app = builder.Build();
var relay = new RelayHub();
var auth = new AuthStore(Path.Combine(app.Environment.ContentRootPath, "data", "lobby.db"));
var book = new RoomBook(auth);

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.MapGet("/", () => Results.Text("StarTrekCCG lobby. WebSocket /lobby. Relay WebSocket /relay copies frames only."));
app.Map("/lobby", async (HttpContext ctx) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsync("WebSocket required.");
        return;
    }

    using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
    var session = new LobbySession(socket, book, auth);
    await session.RunAsync();
});

app.Map("/relay", async (HttpContext ctx) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsync("WebSocket required.");
        return;
    }

    using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
    await relay.RunAsync(socket, book);
});

app.Logger.LogWarning("Lobby service listening on http://0.0.0.0:7788 (/lobby signaling, /relay copies frames).");
app.Run();

static class Limits
{
    public const int MaxRooms = 50;
    public const int MaxMessageBytes = 262144;
    public const int MaxChatLines = 30;
}

sealed class LobbySession
{
    private readonly WebSocket _socket;
    private readonly RoomBook _book;
    private readonly SemaphoreSlim _send = new(1, 1);
    private readonly byte[] _buffer = new byte[Limits.MaxMessageBytes];

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string Name { get; private set; } = "";
    public string Engine { get; set; } = "";
    public string CardHash { get; set; } = "";
    public bool SaidHello { get; private set; }

    private readonly AuthStore _auth;

    public string Mode { get; private set; } = "sandbox";
    public string? Token { get; private set; }
    public int UserId { get; private set; }

    public LobbySession(WebSocket socket, RoomBook book, AuthStore auth)
    {
        _socket = socket;
        _book = book;
        _auth = auth;
    }

    public async Task RunAsync()
    {
        try
        {
            while (_socket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(_buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await CloseQuiet();
                        return;
                    }
                    if (result.MessageType != WebSocketMessageType.Text)
                        return;
                    ms.Write(_buffer, 0, result.Count);
                    if (ms.Length > Limits.MaxMessageBytes)
                    {
                        await SendAsync(new { type = "error", message = "message too large" });
                        return;
                    }
                }
                while (!result.EndOfMessage);

                await HandleAsync(Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length));
            }
        }
        catch (WebSocketException)
        {
            // peer dropped
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("lobby session ended: " + ex.GetType().Name);
        }
        finally
        {
            var notes = _book.Leave(this);
            await FanOut(notes);
        }
    }

    private async Task HandleAsync(string json)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch
        {
            await SendAsync(new { type = "error", message = "bad json" });
            return;
        }

        using (doc)
        {
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (type is "register" or "login" or "deckSave" or "deckList" or "deckGet" or "report")
            {
                await SendAsync(AccountCall(type, root));
                return;
            }

            if (!SaidHello)
            {
                if (!string.Equals(type, "hello", StringComparison.Ordinal))
                {
                    await SendAsync(new { type = "error", message = "say hello first" });
                    return;
                }
                if (!TryHello(root, out var helloError))
                {
                    await SendAsync(new { type = "error", message = helloError });
                    return;
                }
                SaidHello = true;
                await SendAsync(new { type = "welcome", playerId = Id, name = Name, mode = Mode });
                return;
            }

            var tokenError = RejectToken(root);
            if (tokenError != null)
            {
                await SendAsync(new { type = "error", message = tokenError });
                return;
            }

            switch (type)
            {
                case "list":
                    await SendAsync(new { type = "rooms", rooms = _book.List() });
                    break;
                case "create":
                    await FanOut(_book.Create(this, ReadString(root, "room"), ReadInt(root, "gamePort")));
                    break;
                case "join":
                    await FanOut(_book.Join(this, ReadString(root, "room")));
                    break;
                case "chat":
                    await FanOut(_book.Chat(this, ReadString(root, "text")));
                    break;
                case "deck":
                    await FanOut(_book.Deck(this, root));
                    break;
                case "begin":
                    await FanOut(_book.Begin(this));
                    break;
                case "address":
                    await FanOut(_book.Address(this, ReadString(root, "host"), ReadInt(root, "port")));
                    break;
                case "version":
                    await FanOut(_book.ApplyVersion(this, ReadString(root, "engine"), ReadString(root, "cardHash")));
                    break;
                case "leave":
                    await FanOut(_book.Leave(this));
                    break;
                default:
                    await SendAsync(new { type = "error", message = "unknown type" });
                    break;
            }
        }
    }

    public async Task SendAsync(object payload)
    {
        if (_socket.State != WebSocketState.Open)
            return;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        await _send.WaitAsync();
        try
        {
            if (_socket.State == WebSocketState.Open)
                await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        finally
        {
            _send.Release();
        }
    }

    private async Task FanOut(IReadOnlyList<Outbound> notes)
    {
        foreach (var note in notes)
        {
            try { await note.To.SendAsync(note.Payload); }
            catch { /* one slow peer does not stop the other */ }
        }
    }

    private async Task CloseQuiet()
    {
        try
        {
            if (_socket.State == WebSocketState.Open)
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch { /* ignore */ }
    }


    private object AccountCall(string type, JsonElement root)
    {
        switch (type)
        {
            case "register":
                return _auth.Register(ReadString(root, "name"), ReadString(root, "password"));
            case "login":
                return _auth.Login(ReadString(root, "name"), ReadString(root, "password"));
            case "deckSave":
                return _auth.SaveDeck(ReadString(root, "token"), ReadString(root, "name"),
                    root.TryGetProperty("cardIds", out var cards) ? cards : default);
            case "deckList":
                return _auth.ListDecks(ReadString(root, "token"));
            case "deckGet":
                return _auth.GetDeck(ReadString(root, "token"), ReadString(root, "name"));
            case "report":
                return _auth.Report(ReadString(root, "matchId"), ReadString(root, "secret"), ReadString(root, "winner"));
            default:
                return new { type = "error", message = "unknown type" };
        }
    }

    private bool TryHello(JsonElement root, out string error)
    {
        error = "";
        var mode = ReadString(root, "mode");
        if (mode.Length == 0)
            mode = "sandbox";
        if (mode is not ("sandbox" or "account"))
        {
            error = "mode must be sandbox or account";
            return false;
        }
        var token = ReadString(root, "token");
        if (mode == "account")
        {
            if (!_auth.TrySession(token, out var user))
            {
                error = "login required";
                return false;
            }
            Name = user.Name;
            Mode = "account";
            Token = token;
            UserId = user.Id;
            return true;
        }
        if (token.Length > 0)
        {
            error = "sandbox does not use an account";
            return false;
        }
        var name = ClipName(ReadString(root, "name"), 24);
        if (name.Length == 0)
        {
            error = "name required";
            return false;
        }
        Name = name;
        Mode = "sandbox";
        Token = null;
        UserId = 0;
        return true;
    }

    private string? RejectToken(JsonElement root)
    {
        var token = ReadString(root, "token");
        if (Mode == "account")
            return string.Equals(token, Token, StringComparison.Ordinal) && token.Length > 0
                ? null
                : "token required";
        return token.Length == 0 ? null : "sandbox does not use an account";
    }

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? (el.GetString() ?? "")
            : "";

    private static int ReadInt(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.TryGetInt32(out var n) ? n : 0;

    public static string ClipName(string? raw, int max)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw.Trim())
        {
            if (ch is '\r' or '\n' or '\t')
                continue;
            if (char.IsControl(ch))
                continue;
            sb.Append(ch);
            if (sb.Length >= max)
                break;
        }
        return sb.ToString().Trim();
    }
}

sealed record Outbound(LobbySession To, object Payload);

sealed class RoomBook
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Room> _rooms = new(StringComparer.Ordinal);
    private readonly AuthStore _auth;

    public RoomBook(AuthStore auth)
    {
        _auth = auth;
    }

    public object List()
    {
        lock (_gate)
        {
            return _rooms.Values
                .OrderBy(r => r.Name, StringComparer.Ordinal)
                .Select(r => new
                {
                    name = r.Name,
                    hostName = r.Host?.Name ?? "",
                    players = (r.Host == null ? 0 : 1) + (r.Guest == null ? 0 : 1),
                    open = r.Guest == null,
                    mode = r.Mode
                })
                .ToArray();
        }
    }

    public List<Outbound> Create(LobbySession who, string roomName, int gamePort)
    {
        roomName = LobbySession.ClipName(roomName, 24);
        if (roomName.Length == 0 || !RoomNameOk(roomName))
            return Err(who, "room name must be 1-24 letters, digits, space, _ or -");
        if (gamePort is < 1 or > 65535)
            return Err(who, "game port is not valid");

        lock (_gate)
        {
            if (FindSeat(who) != null)
                return Err(who, "leave the current room first");
            if (_rooms.Count >= Limits.MaxRooms)
                return Err(who, "too many rooms");
            if (_rooms.ContainsKey(roomName))
                return Err(who, "room already exists");

            var room = new Room { Name = roomName, ListenPort = gamePort, Mode = who.Mode };
            room.Host = new Seat { Session = who, Name = who.Name, Role = "host", Engine = who.Engine, CardHash = who.CardHash };
            _rooms[roomName] = room;
            return RoomNotes(room);
        }
    }

    public List<Outbound> Join(LobbySession who, string roomName)
    {
        roomName = LobbySession.ClipName(roomName, 24);
        lock (_gate)
        {
            if (FindSeat(who) != null)
                return Err(who, "leave the current room first");
            if (!_rooms.TryGetValue(roomName, out var room))
                return Err(who, "no such room");
            if (!string.Equals(room.Mode, who.Mode, StringComparison.Ordinal))
                return Err(who, "a match is one mode");
            if (room.Guest != null)
                return Err(who, "room is full");
            room.Guest = new Seat { Session = who, Name = who.Name, Role = "guest", Engine = who.Engine, CardHash = who.CardHash };
            return RoomNotes(room);
        }
    }

    public List<Outbound> Chat(LobbySession who, string text)
    {
        text = LobbySession.ClipName(text, 200);
        if (text.Length == 0)
            return Err(who, "empty chat");
        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return Err(who, "join a room first");
            found.Value.Room.Lines.Add((who.Name, text));
            if (found.Value.Room.Lines.Count > Limits.MaxChatLines)
                found.Value.Room.Lines.RemoveAt(0);
            return RoomNotes(found.Value.Room);
        }
    }

    public List<Outbound> Deck(LobbySession who, JsonElement root)
    {
        var deckName = LobbySession.ClipName(ReadString(root, "deckName"), 80);
        var deckHash = ReadString(root, "deckHash").Trim().ToLowerInvariant();
        if (deckName.Length == 0)
            return Err(who, "deck name required");

        string cards;
        if (who.Mode == "account")
        {
            if (!_auth.TryReadDeck(who.UserId, deckName, out cards, out var error))
                return Err(who, error);
            deckHash = AuthStore.Sha256Hex(cards);
        }
        else
        {
            if (deckHash.Length != 64 || deckHash.Any(c => !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))))
                return Err(who, "deck hash must be 64 hex characters");
            if (!root.TryGetProperty("cardIds", out var cardIds))
                return Err(who, "sandbox deck needs card ids");
            if (!AuthStore.TryCanonical(cardIds, out cards, out _, out var error))
                return Err(who, error);
        }

        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return Err(who, "join a room first");
            found.Value.Seat.DeckName = deckName;
            found.Value.Seat.DeckHash = deckHash;
            found.Value.Seat.PrivateCards = cards;
            return RoomNotes(found.Value.Room);
        }
    }

    public List<Outbound> Begin(LobbySession who)
    {
        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return Err(who, "join a room first");
            if (!string.Equals(found.Value.Seat.Role, "host", StringComparison.Ordinal))
                return Err(who, "only the host freezes the match");
            var room = found.Value.Room;
            if (room.Host == null || room.Guest == null)
                return Err(who, "waiting for both players");
            if (string.IsNullOrEmpty(room.Host.PrivateCards) || string.IsNullOrEmpty(room.Guest.PrivateCards))
                return Err(who, "both players need a deck before the match starts");
            if (room.MatchId == null)
            {
                var frozen = _auth.Freeze(
                    room.Mode,
                    room.Host.Name,
                    room.Guest.Name,
                    room.Host.DeckName ?? "",
                    room.Host.PrivateCards,
                    room.Guest.DeckName ?? "",
                    room.Guest.PrivateCards);
                room.MatchId = frozen.MatchId;
                room.HostSecret = frozen.HostSecret;
                room.GuestSecret = frozen.GuestSecret;
            }
            // Each seat gets its own secret. Neither payload includes a deck list.
            return new List<Outbound>
            {
                new(room.Host.Session, MatchNote(room.MatchId, "host", room.HostSecret!, room.Host.PrivateCards!)),
                new(room.Guest.Session, MatchNote(room.MatchId, "guest", room.GuestSecret!, room.Guest.PrivateCards!))
            };
        }
    }

    private static object MatchNote(string matchId, string seat, string secret, string cards)
        => new { type = "match", matchId, seat, secret, count = AuthStore.CountIds(cards) };

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? (el.GetString() ?? "")
            : "";

    public List<Outbound> Address(LobbySession who, string host, int port)
    {
        host = (host ?? "").Trim();
        if (!IPAddress.TryParse(host, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return Err(who, "address must be an IPv4 literal");
        if (ip.Equals(IPAddress.Any))
            return Err(who, "address is not usable");
        if (port is < 1 or > 65535)
            return Err(who, "port is not valid");

        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return Err(who, "join a room first");
            if (!string.Equals(found.Value.Seat.Role, "host", StringComparison.Ordinal))
                return Err(who, "only the host publishes the address");
            found.Value.Room.Address = ip.ToString();
            found.Value.Room.Port = port;
            return RoomNotes(found.Value.Room);
        }
    }

    public List<Outbound> Leave(LobbySession who)
    {
        // Lobby close, including the window closing at game start, must not abort /relay.
        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return new List<Outbound>();
            var room = found.Value.Room;
            if (ReferenceEquals(room.Host?.Session, who))
            {
                _rooms.Remove(room.Name);
                var notes = new List<Outbound>();
                if (room.Guest != null)
                    notes.Add(new Outbound(room.Guest.Session, new { type = "gone", message = "host left" }));
                notes.Add(new Outbound(who, new { type = "gone", message = "you left" }));
                return notes;
            }

            room.Guest = null;
            room.Address = null;
            room.Port = 0;
            var stay = RoomNotes(room);
            stay.Add(new Outbound(who, new { type = "gone", message = "you left" }));
            return stay;
        }
    }

    private (Room Room, Seat Seat)? FindSeat(LobbySession who)
    {
        foreach (var room in _rooms.Values)
        {
            if (ReferenceEquals(room.Host?.Session, who))
                return (room, room.Host);
            if (ReferenceEquals(room.Guest?.Session, who))
                return (room, room.Guest);
        }
        return null;
    }

    private static List<Outbound> RoomNotes(Room room)
    {
        var notes = new List<Outbound>();
        if (room.Host != null)
            notes.Add(new Outbound(room.Host.Session, RoomView(room, "host")));
        if (room.Guest != null)
            notes.Add(new Outbound(room.Guest.Session, RoomView(room, "guest")));
        return notes;
    }

    private static object RoomView(Room room, string role)
    {
        Seat?[] seats = { room.Host, room.Guest };
        return new
        {
            type = "room",
            name = room.Name,
            role,
            address = room.Address ?? "",
            port = room.Port,
            listenPort = room.ListenPort,
            versionKnown = VersionKnown(room),
            versionOk = VersionKnown(room) && VersionsMatch(room),
            versionNote = VersionNote(room),
            players = seats.Where(s => s != null).Select(s => new
            {
                name = s!.Name,
                role = s.Role,
                deckName = s.DeckName ?? "",
                deckHash = s.DeckHash ?? ""
            }).ToArray(),
            lines = room.Lines.Select(l => new { from = l.From, text = l.Text }).ToArray()
        };
    }

    public string? AuthorizeRelay(string playerId, string roomName, string role)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomName, out var room))
                return "no such room";
            var seat = role == "host" ? room.Host : room.Guest;
            if (seat == null)
                return "that seat is empty";
            if (!string.Equals(seat.Session.Id, playerId, StringComparison.Ordinal))
                return "not your seat";
            if (room.Host == null || room.Guest == null)
                return "waiting for both players";
            if (!VersionKnown(room))
                return "waiting for both version stamps";
            if (!VersionsMatch(room))
                return VersionNote(room);
            return null;
        }
    }

    public List<Outbound> ApplyVersion(LobbySession who, string engine, string cardHash)
    {
        engine = LobbySession.ClipName(engine, 40);
        cardHash = (cardHash ?? "").Trim().ToLowerInvariant();
        if (engine.Length == 0)
            return Err(who, "engine required");
        if (cardHash.Length != 64 || cardHash.Any(c => !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))))
            return Err(who, "card hash must be 64 hex characters");

        who.Engine = engine;
        who.CardHash = cardHash;
        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return new List<Outbound>();
            found.Value.Seat.Engine = engine;
            found.Value.Seat.CardHash = cardHash;
            return RoomNotes(found.Value.Room);
        }
    }

    private static bool VersionKnown(Room room)
        => room.Host != null && room.Guest != null
           && room.Host.Engine.Length > 0 && room.Guest.Engine.Length > 0
           && room.Host.CardHash.Length > 0 && room.Guest.CardHash.Length > 0;

    private static bool VersionsMatch(Room room)
        => string.Equals(room.Host!.Engine, room.Guest!.Engine, StringComparison.Ordinal)
           && string.Equals(room.Host.CardHash, room.Guest.CardHash, StringComparison.OrdinalIgnoreCase);

    private static string VersionNote(Room room)
    {
        if (room.Host == null || room.Guest == null)
            return "waiting for both players";
        if (!VersionKnown(room))
            return "waiting for both version stamps";
        if (!string.Equals(room.Host.Engine, room.Guest.Engine, StringComparison.Ordinal))
            return "engine mismatch: " + room.Host.Engine + " vs " + room.Guest.Engine + ". No start.";
        if (!string.Equals(room.Host.CardHash, room.Guest.CardHash, StringComparison.OrdinalIgnoreCase))
            return "card data mismatch (" + room.Host.CardHash[..8] + " vs " + room.Guest.CardHash[..8] + "). No start.";
        return "versions match";
    }

    private static List<Outbound> Err(LobbySession who, string message)
        => new() { new Outbound(who, new { type = "error", message }) };

    private static bool RoomNameOk(string name)
    {
        foreach (var ch in name)
        {
            if (char.IsAsciiLetterOrDigit(ch) || ch is ' ' or '_' or '-')
                continue;
            return false;
        }
        return true;
    }

    private sealed class Room
    {
        public string Name { get; set; } = "";
        public string Mode { get; set; } = "sandbox";
        public int ListenPort { get; set; }
        public string? MatchId { get; set; }
        public string? HostSecret { get; set; }
        public string? GuestSecret { get; set; }
        public Seat? Host { get; set; }
        public Seat? Guest { get; set; }
        public string? Address { get; set; }
        public int Port { get; set; }
        public List<(string From, string Text)> Lines { get; } = new();
    }

    private sealed class Seat
    {
        public LobbySession Session { get; set; } = null!;
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public string? DeckName { get; set; }
        public string? DeckHash { get; set; }
        public string? PrivateCards { get; set; }
        public string Engine { get; set; } = "";
        public string CardHash { get; set; } = "";
    }
}
