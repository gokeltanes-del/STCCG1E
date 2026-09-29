using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

// In-memory matchmaking only. No game state, no relay, no deck bodies.
// Restart clears every room. Listens on 0.0.0.0:7788, WebSocket path /lobby.

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:7788");
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var app = builder.Build();
var book = new RoomBook();

app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.MapGet("/", () => Results.Text("StarTrekCCG lobby. WebSocket /lobby. Rooms are in memory. No game relay."));
app.Map("/lobby", async (HttpContext ctx) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsync("WebSocket required.");
        return;
    }

    using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
    var session = new LobbySession(socket, book);
    await session.RunAsync();
});

app.Logger.LogWarning("Lobby service listening on http://0.0.0.0:7788/lobby (in-memory rooms, no relay).");
app.Run();

static class Limits
{
    public const int MaxRooms = 50;
    public const int MaxMessageBytes = 4096;
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
    public bool SaidHello { get; private set; }

    public LobbySession(WebSocket socket, RoomBook book)
    {
        _socket = socket;
        _book = book;
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
            if (!SaidHello)
            {
                if (!string.Equals(type, "hello", StringComparison.Ordinal))
                {
                    await SendAsync(new { type = "error", message = "say hello first" });
                    return;
                }
                var name = ClipName(ReadString(root, "name"), 24);
                if (name.Length == 0)
                {
                    await SendAsync(new { type = "error", message = "name required" });
                    return;
                }
                Name = name;
                SaidHello = true;
                await SendAsync(new { type = "welcome", playerId = Id, name = Name });
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
                    await FanOut(_book.Deck(this, ReadString(root, "deckName"), ReadString(root, "deckHash")));
                    break;
                case "address":
                    await FanOut(_book.Address(this, ReadString(root, "host"), ReadInt(root, "port")));
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
                    open = r.Guest == null
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

            var room = new Room { Name = roomName, ListenPort = gamePort };
            room.Host = new Seat { Session = who, Name = who.Name, Role = "host" };
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
            if (room.Guest != null)
                return Err(who, "room is full");
            room.Guest = new Seat { Session = who, Name = who.Name, Role = "guest" };
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

    public List<Outbound> Deck(LobbySession who, string deckName, string deckHash)
    {
        deckName = LobbySession.ClipName(deckName, 80);
        deckHash = (deckHash ?? "").Trim().ToLowerInvariant();
        if (deckName.Length == 0)
            return Err(who, "deck name required");
        if (deckHash.Length != 64 || deckHash.Any(c => !((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))))
            return Err(who, "deck hash must be 64 hex characters");

        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return Err(who, "join a room first");
            found.Value.Seat.DeckName = deckName;
            found.Value.Seat.DeckHash = deckHash;
            return RoomNotes(found.Value.Room);
        }
    }

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
        public int ListenPort { get; set; }
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
    }
}
