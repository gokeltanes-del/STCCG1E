using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

// In-memory rooms. /lobby is signaling plus accounts. /relay copies game frames and does not read them.
// The server does not simulate rules. Both clients connect outbound.
// Closing /lobby does not abort /relay. A relay seat drops only when that relay socket ends.
// If a client is gone and does not rejoin within the 120s grace, that seat is freed.
// A live match relay pair is not deleted during the grace. An empty room may go after that.
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
    public const int MaxMessageBytes = 2097152;
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
            if (type is "register" or "login" or "deckSave" or "deckList" or "deckGet" or "get_pool" or "buy_pack" or "devGrantLatinum" or "trade_offer" or "trade_list" or "trade_accept" or "trade_decline" or "report" or "saveAuto" or "saveManual" or "deleteAccount")
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
                case "chatInvite":
                    await FanOut(_book.ChatInvite(this, ReadString(root, "to")));
                    break;
                case "chatInviteReply":
                    await FanOut(_book.ChatInviteReply(this, ReadString(root, "inviteId"), ReadBool(root, "accept")));
                    break;
                case "tradeAsk":
                    await FanOut(_book.TradeAsk(this, ReadString(root, "to")));
                    break;
                case "tradeReply":
                    await FanOut(_book.TradeReply(this, ReadString(root, "requestId"), ReadBool(root, "accept")));
                    break;
                case "tradeSlots":
                    await FanOut(_book.TradeSlots(this, ReadString(root, "sessionId"), ReadTradeCards(root)));
                    break;
                case "tradeLock":
                    await FanOut(_book.TradeLock(this, ReadString(root, "sessionId")));
                    break;
                case "tradeCancel":
                    await FanOut(_book.TradeCancel(this, ReadString(root, "sessionId")));
                    break;
                case "privateChat":
                    await FanOut(_book.PrivateChat(this, ReadString(root, "text")));
                    break;
                case "deck":
                    await FanOut(_book.Deck(this, root));
                    break;
                case "begin":
                    await FanOut(_book.Begin(this));
                    break;
                case "load":
                    await FanOut(_book.Load(this, root));
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
                case "challenge":
                    await FanOut(_book.Challenge(this, ReadString(root, "to"), ReadString(root, "mode")));
                    break;
                case "challengeReply":
                    await FanOut(_book.ChallengeReply(this, ReadString(root, "challengeId"), ReadBool(root, "accept")));
                    break;
                case "challengeMode":
                    await FanOut(_book.ProposeMode(this, ReadString(root, "challengeId"), ReadString(root, "mode")));
                    break;
                case "modeReply":
                    await FanOut(_book.ModeReply(this, ReadString(root, "challengeId"), ReadBool(root, "accept")));
                    break;
                case "challengeCancel":
                    await FanOut(_book.CancelChallenge(this, ReadString(root, "challengeId")));
                    break;
                case "friendRequest":
                    await FanOut(_book.FriendRequest(this, ReadString(root, "to")));
                    break;
                case "friendReply":
                    await FanOut(_book.FriendReply(this, ReadString(root, "requestId"), ReadBool(root, "accept")));
                    break;
                case "friendRemove":
                    await FanOut(_book.FriendRemove(this, ReadString(root, "name")));
                    break;
                case "friendList":
                    await FanOut(_book.FriendList(this));
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
            case "deleteAccount":
                if (!SaidHello || !string.Equals(Mode, "account", StringComparison.Ordinal))
                    return new { type = "error", message = "login required" };
                return _auth.DeleteAccount(ReadString(root, "token"), ReadString(root, "password"));
            case "deckSave":
                return _auth.SaveDeck(ReadString(root, "token"), ReadString(root, "name"),
                    root.TryGetProperty("cardIds", out var cards) ? cards : default);
            case "deckList":
                return _auth.ListDecks(ReadString(root, "token"));
            case "deckGet":
                return _auth.GetDeck(ReadString(root, "token"), ReadString(root, "name"));
            case "get_pool":
                if (!SaidHello || !string.Equals(Mode, "account", StringComparison.Ordinal))
                    return new { type = "error", message = "sandbox does not use an account" };
                return _auth.GetPool(ReadString(root, "token"));
            case "buy_pack":
                if (!SaidHello || !string.Equals(Mode, "account", StringComparison.Ordinal))
                    return new { type = "error", message = "sandbox cannot buy" };
                if (!string.Equals(ReadString(root, "token"), Token, StringComparison.Ordinal) || string.IsNullOrEmpty(Token))
                    return new { type = "error", message = "token required" };
                // packType only. Card names on this message are ignored.
                return _auth.BuyPack(Token, ReadString(root, "packType"));
            case "devGrantLatinum":
                // DEV-ONLY HOOK. Remove for release. Fixed credit on users.latinum. No amount is read from the message.
                if (!SaidHello || !string.Equals(Mode, "account", StringComparison.Ordinal))
                    return new { type = "error", message = "login required" };
                if (!string.Equals(ReadString(root, "token"), Token, StringComparison.Ordinal) || string.IsNullOrEmpty(Token))
                    return new { type = "error", message = "token required" };
                return _auth.DevGrantLatinum(Token);
            case "trade_offer":
            case "trade_list":
            case "trade_accept":
            case "trade_decline":
                if (!SaidHello || !string.Equals(Mode, "account", StringComparison.Ordinal))
                    return new { type = "error", message = "sandbox cannot trade" };
                if (!string.Equals(ReadString(root, "token"), Token, StringComparison.Ordinal) || string.IsNullOrEmpty(Token))
                    return new { type = "error", message = "token required" };
                if (type == "trade_offer")
                    return _auth.OfferTrade(Token, ReadString(root, "to"), root);
                if (type == "trade_list")
                    return _auth.ListTrades(Token);
                if (type == "trade_accept")
                    return _auth.AcceptTrade(Token, ReadId(root, "offerId"));
                return _auth.DeclineTrade(Token, ReadId(root, "offerId"));
            case "report":
                // winner plus an optional boolean. A card id on this message is ignored.
                return _auth.Report(
                    ReadString(root, "matchId"),
                    ReadString(root, "secret"),
                    ReadString(root, "winner"),
                    ReadBool(root, "raiseTheStakes"));
            case "saveAuto":
                return _auth.PutSave(ReadString(root, "matchId"), ReadString(root, "secret"), "auto", "", ReadBlob(root));
            case "saveManual":
                return _auth.PutSave(ReadString(root, "matchId"), ReadString(root, "secret"), "manual", ReadString(root, "name"), ReadBlob(root));
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

    private static string ReadId(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el))
            return "";
        if (el.ValueKind == JsonValueKind.String)
            return el.GetString() ?? "";
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var n))
            return n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return "";
    }

    private static bool ReadBool(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.True;

    private static string[] ReadTradeCards(JsonElement root)
    {
        var cards = new string[] { "", "", "", "" };
        if (!root.TryGetProperty("cards", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return cards;
        var index = 0;
        foreach (var item in arr.EnumerateArray())
        {
            if (index >= 4)
                break;
            cards[index++] = item.ValueKind == JsonValueKind.String ? (item.GetString() ?? "").Trim() : "";
        }
        return cards;
    }

    private static string ReadBlob(JsonElement root)
    {
        if (!root.TryGetProperty("blob", out var el))
            return "";
        if (el.ValueKind == JsonValueKind.String)
            return el.GetString() ?? "";
        if (el.ValueKind == JsonValueKind.Object)
            return el.GetRawText();
        return "";
    }

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
    public const string LoungeRoomName = "Lounge";

    private readonly object _gate = new();
    private readonly Dictionary<string, Room> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PendingChallenge> _challenges = new();
    private readonly List<PendingFriend> _friendAsks = new();
    private readonly List<PendingChatInvite> _chatInvites = new();
    private readonly List<PrivateConference> _privateChats = new();
    private readonly List<PendingTradeAsk> _tradeAsks = new();
    private readonly List<LiveTrade> _trades = new();
    private readonly AuthStore _auth;

    public RoomBook(AuthStore auth)
    {
        _auth = auth;
        _rooms[LoungeRoomName] = new Room { Name = LoungeRoomName, Mode = "all" };
    }

    public object List()
    {
        lock (_gate)
        {
            return _rooms.Values
                .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .Select(r => new
                {
                    name = r.Name,
                    hostName = r.IsLounge ? "Server" : (r.Host?.Name ?? ""),
                    players = r.IsLounge ? r.Members.Count : ((r.Host == null ? 0 : 1) + (r.Guest == null ? 0 : 1)),
                    open = r.IsLounge || (r.Host == null || r.Guest == null),
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
        if (string.Equals(roomName, LoungeRoomName, StringComparison.OrdinalIgnoreCase))
            return Err(who, "Lounge is a permanent room");
        if (gamePort is < 1 or > 65535)
            return Err(who, "game port is not valid");

        lock (_gate)
        {
            var notes = new List<Outbound>();
            var current = FindSeat(who);
            if (current != null)
            {
                if (current.Value.Room.IsLounge)
                {
                    current.Value.Room.Members.RemoveAll(s => ReferenceEquals(s.Session, who));
                    notes.AddRange(RoomNotes(current.Value.Room));
                }
                else
                {
                    return Err(who, "leave the current room first");
                }
            }
            if (_rooms.Count >= Limits.MaxRooms)
                return Err(who, "too many rooms");
            if (_rooms.ContainsKey(roomName))
                return Err(who, "room already exists");

            var room = new Room { Name = roomName, ListenPort = gamePort, Mode = who.Mode };
            room.Host = new Seat { Session = who, Name = who.Name, Role = "host", Engine = who.Engine, CardHash = who.CardHash };
            _rooms[roomName] = room;
            notes.AddRange(RoomNotes(room));
            return notes;
        }
    }

    public List<Outbound> Join(LobbySession who, string roomName)
    {
        roomName = LobbySession.ClipName(roomName, 24);
        lock (_gate)
        {
            var notes = new List<Outbound>();
            var isLoungeTarget = string.Equals(roomName, LoungeRoomName, StringComparison.OrdinalIgnoreCase);
            var current = FindSeat(who);
            if (current != null)
            {
                if (current.Value.Room.IsLounge)
                {
                    if (isLoungeTarget)
                    {
                        // Already in Lounge; refresh view
                        return RoomNotes(current.Value.Room);
                    }
                    current.Value.Room.Members.RemoveAll(s => ReferenceEquals(s.Session, who));
                    notes.AddRange(RoomNotes(current.Value.Room));
                }
                else
                {
                    return Err(who, "leave the current room first");
                }
            }

            if (isLoungeTarget && !_rooms.ContainsKey(LoungeRoomName))
            {
                _rooms[LoungeRoomName] = new Room { Name = LoungeRoomName, Mode = "all" };
            }

            if (!_rooms.TryGetValue(roomName, out var room))
                return Err(who, "no such room");

            if (room.IsLounge)
            {
                var seat = new Seat
                {
                    Session = who,
                    Name = who.Name,
                    Role = "member",
                    Engine = who.Engine,
                    CardHash = who.CardHash
                };
                room.Members.Add(seat);
                notes.AddRange(RoomNotes(room));
                return notes;
            }

            if (string.Equals(room.Mode, "account", StringComparison.Ordinal))
            {
                if (!string.Equals(who.Mode, "account", StringComparison.Ordinal))
                    return Err(who, "a match is one mode");
            }
            else if (!string.Equals(room.Mode, "sandbox", StringComparison.Ordinal)
                && !string.Equals(room.Mode, who.Mode, StringComparison.Ordinal))
            {
                return Err(who, "a match is one mode");
            }
            if (room.Host != null && room.Guest != null)
                return Err(who, "room is full");
            // Whichever seat the grace freed. The other account sits there.
            if (room.Host == null)
            {
                room.Host = new Seat { Session = who, Name = who.Name, Role = "host", Engine = who.Engine, CardHash = who.CardHash };
                room.HostLobbyClosed = false;
            }
            else
            {
                room.Guest = new Seat { Session = who, Name = who.Name, Role = "guest", Engine = who.Engine, CardHash = who.CardHash };
                room.GuestLobbyClosed = false;
            }
            notes.AddRange(RoomNotes(room));
            return notes;
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

        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return Err(who, "join a room first");
            // Holodeck room accepts the local card list. Login type does not make it ranked.
            var holodeckRoom = string.Equals(found.Value.Room.Mode, "sandbox", StringComparison.Ordinal);
            string cards;
            if (!holodeckRoom && who.Mode == "account")
            {
                if (!_auth.TryReadDeck(who.UserId, deckName, out cards, out var error))
                    return Err(who, error);
                if (!_auth.TryMatchDeck(who.UserId, cards, out error))
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
            found.Value.Seat.DeckName = deckName;
            found.Value.Seat.DeckHash = deckHash;
            found.Value.Seat.PrivateCards = cards;
            var notes = RoomNotes(found.Value.Room);
            notes.Insert(0, new Outbound(who, new { type = "deckOk" }));
            return notes;
        }
    }

    public List<Outbound> Begin(LobbySession who)
    {
        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return Err(who, "join a room first");
            if (found.Value.Room.IsLounge)
                return Err(who, "cannot start a match in Lounge");
            if (!string.Equals(found.Value.Seat.Role, "host", StringComparison.Ordinal))
                return Err(who, "only the host freezes the match");
            var room = found.Value.Room;
            if (room.Host == null || room.Guest == null)
                return Err(who, "waiting for both players");
            if (string.IsNullOrEmpty(room.Host.PrivateCards) || string.IsNullOrEmpty(room.Guest.PrivateCards))
                return Err(who, "both players need a deck before the match starts");
            if (room.MatchId == null)
            {
                if (string.Equals(room.Mode, "account", StringComparison.Ordinal))
                {
                    if (!_auth.TryMatchDeck(room.Host.Session.UserId, room.Host.PrivateCards!, out var hostWhy))
                        return Err(who, "host deck: " + hostWhy);
                    if (!_auth.TryMatchDeck(room.Guest.Session.UserId, room.Guest.PrivateCards!, out var guestWhy))
                        return Err(who, "guest deck: " + guestWhy);
                }
                var frozen = _auth.Freeze(
                    room.Mode,
                    room.Host.Name,
                    room.Guest.Name,
                    room.Host.DeckName ?? "",
                    room.Host.PrivateCards,
                    room.Guest.DeckName ?? "",
                    room.Guest.PrivateCards,
                    room.Host.Session.UserId,
                    room.Guest.Session.UserId);
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

    public List<Outbound> Load(LobbySession who, JsonElement root)
    {
        lock (_gate)
        {
            var found = FindSeat(who);
            if (found == null)
                return Err(who, "join a room first");
            if (found.Value.Room.IsLounge)
                return Err(who, "cannot load in Lounge");
            if (!string.Equals(found.Value.Seat.Role, "host", StringComparison.Ordinal))
                return Err(who, "only the host loads a saved game");
            var room = found.Value.Room;
            if (!string.Equals(room.Mode, "account", StringComparison.Ordinal))
                return Err(who, "only an account match can be loaded");
            if (room.Host == null || room.Guest == null)
                return Err(who, "waiting for both players");
            var hostId = room.Host.Session.UserId;
            var guestId = room.Guest.Session.UserId;
            if (hostId <= 0 || guestId <= 0)
                return Err(who, "only an account match can be loaded");
            var matchId = ReadString(root, "matchId");
            var kind = ReadString(root, "kind");
            var name = ReadString(root, "name");
            if (!_auth.TryLoad(hostId, guestId, matchId, kind, name, out var blob, out var hostSecret, out var guestSecret, out var error))
                return Err(who, error);
            room.MatchId = matchId.Trim();
            room.HostSecret = hostSecret;
            room.GuestSecret = guestSecret;
            // Full save goes to the host seat only. The guest gets the match id and its own secret.
            return new List<Outbound>
            {
                new(room.Host.Session, new { type = "resume", matchId = room.MatchId, seat = "host", secret = hostSecret, blob }),
                new(room.Guest.Session, new { type = "resume", matchId = room.MatchId, seat = "guest", secret = guestSecret })
            };
        }
    }

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
            if (found.Value.Room.IsLounge)
                return Err(who, "cannot publish address in Lounge");
            if (!string.Equals(found.Value.Seat.Role, "host", StringComparison.Ordinal))
                return Err(who, "only the host publishes the address");
            found.Value.Room.Address = ip.ToString();
            found.Value.Room.Port = port;
            return RoomNotes(found.Value.Room);
        }
    }

    public List<Outbound> Challenge(LobbySession who, string targetName, string mode)
    {
        // Invite only. Mode is chosen later with challengeMode. The argument is ignored.
        _ = mode;
        targetName = LobbySession.ClipName(targetName, 24);
        if (targetName.Length == 0)
            return Err(who, "name required");

        lock (_gate)
        {
            if (Involved(who))
                return Err(who, "a challenge is already open");
            var seat = FindSeat(who);
            if (seat == null || !seat.Value.Room.IsLounge)
                return Err(who, "challenge from the lounge");

            LobbySession? target = null;
            var inMatch = false;
            foreach (var room in _rooms.Values)
            {
                if (room.IsLounge)
                {
                    foreach (var member in room.Members)
                    {
                        if (!string.Equals(member.Name, targetName, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (target != null)
                            return Err(who, "name is not unique");
                        target = member.Session;
                    }
                }
                else if (NameIs(room.Host, targetName) || NameIs(room.Guest, targetName))
                {
                    inMatch = true;
                }
            }
            if (target == null)
                return Err(who, inMatch ? "player is already in a match" : "player is not in the lounge");
            if (ReferenceEquals(target, who))
                return Err(who, "cannot challenge yourself");
            if (Involved(target))
                return Err(who, "a challenge is already open");

            var id = Guid.NewGuid().ToString("N");
            _challenges.Add(new PendingChallenge { Id = id, From = who, To = target, Mode = "", Phase = "invite" });
            return new List<Outbound>
            {
                new(target, new { type = "challenge", challengeId = id, from = who.Name }),
                new(who, new { type = "challengeSent", challengeId = id, to = target.Name })
            };
        }
    }

    public List<Outbound> ChallengeReply(LobbySession who, string challengeId, bool accept)
    {
        challengeId = (challengeId ?? "").Trim();
        lock (_gate)
        {
            var pending = _challenges.FirstOrDefault(c =>
                string.Equals(c.Id, challengeId, StringComparison.Ordinal)
                && ReferenceEquals(c.To, who)
                && c.Phase == "invite");
            if (pending == null)
                return Err(who, "no such challenge");
            if (!accept)
            {
                _challenges.Remove(pending);
                return new List<Outbound>
                {
                    new(pending.From, new { type = "challengeClosed", message = pending.To.Name + " declined" }),
                    new(who, new { type = "challengeClosed", message = "declined" })
                };
            }

            var fromSeat = FindSeat(pending.From);
            var toSeat = FindSeat(pending.To);
            if (fromSeat == null || toSeat == null || !fromSeat.Value.Room.IsLounge || !toSeat.Value.Room.IsLounge)
            {
                _challenges.Remove(pending);
                return new List<Outbound>
                {
                    new(pending.From, new { type = "challengeClosed", message = "player is not in the lounge" }),
                    new(who, new { type = "challengeClosed", message = "player is not in the lounge" })
                };
            }

            pending.Phase = "pick";
            pending.Mode = "";
            return new List<Outbound>
            {
                new(pending.From, new { type = "inviteAccepted", challengeId = pending.Id, by = pending.To.Name }),
                new(who, new { type = "inviteWaiting", challengeId = pending.Id })
            };
        }
    }

    public List<Outbound> ProposeMode(LobbySession who, string challengeId, string mode)
    {
        challengeId = (challengeId ?? "").Trim();
        lock (_gate)
        {
            var pending = _challenges.FirstOrDefault(c =>
                string.Equals(c.Id, challengeId, StringComparison.Ordinal) && ReferenceEquals(c.From, who));
            if (pending == null || pending.Phase is not ("pick" or "offer"))
                return Err(who, "no such challenge");
            if (mode is not ("account" or "sandbox"))
                return Err(who, "match mode is not valid");
            // Ranked is the login type. Holodeck (sandbox) is the mode they just chose.
            if (string.Equals(mode, "account", StringComparison.Ordinal)
                && (!string.Equals(who.Mode, "account", StringComparison.Ordinal)
                    || !string.Equals(pending.To.Mode, "account", StringComparison.Ordinal)))
            {
                pending.Phase = "pick";
                pending.Mode = "";
                return Err(who, "sandbox and account cannot play the same match.");
            }

            pending.Phase = "offer";
            pending.Mode = mode;
            return new List<Outbound>
            {
                new(pending.To, new { type = "modeOffer", challengeId = pending.Id, from = who.Name, mode }),
                new(who, new { type = "modeOffered", challengeId = pending.Id, mode })
            };
        }
    }

    public List<Outbound> ModeReply(LobbySession who, string challengeId, bool accept)
    {
        challengeId = (challengeId ?? "").Trim();
        lock (_gate)
        {
            var pending = _challenges.FirstOrDefault(c =>
                string.Equals(c.Id, challengeId, StringComparison.Ordinal)
                && ReferenceEquals(c.To, who)
                && c.Phase == "offer");
            if (pending == null)
                return Err(who, "no such challenge");
            if (!accept)
            {
                pending.Phase = "pick";
                pending.Mode = "";
                return new List<Outbound>
                {
                    new(pending.From, new { type = "modeDeclined", challengeId = pending.Id, message = pending.To.Name + " declined" }),
                    new(who, new { type = "modeDeclined", challengeId = pending.Id, message = "declined" })
                };
            }

            return SeatAcceptedMode(pending);
        }
    }

    public List<Outbound> CancelChallenge(LobbySession who, string challengeId)
    {
        challengeId = (challengeId ?? "").Trim();
        lock (_gate)
        {
            var pending = _challenges.FirstOrDefault(c =>
                string.Equals(c.Id, challengeId, StringComparison.Ordinal) && ReferenceEquals(c.From, who));
            if (pending == null)
                return Err(who, "no such challenge");
            _challenges.Remove(pending);
            return new List<Outbound>
            {
                new(pending.To, new { type = "challengeClosed", message = pending.From.Name + " cancelled" }),
                new(who, new { type = "challengeClosed", message = "cancelled" })
            };
        }
    }

    private List<Outbound> SeatAcceptedMode(PendingChallenge pending)
    {
        var fromSeat = FindSeat(pending.From);
        var toSeat = FindSeat(pending.To);
        if (fromSeat == null || toSeat == null || !fromSeat.Value.Room.IsLounge || !toSeat.Value.Room.IsLounge)
        {
            _challenges.Remove(pending);
            return new List<Outbound>
            {
                new(pending.From, new { type = "challengeClosed", message = "player is not in the lounge" }),
                new(pending.To, new { type = "challengeClosed", message = "player is not in the lounge" })
            };
        }
        if (pending.Mode is not ("account" or "sandbox")
            || (string.Equals(pending.Mode, "account", StringComparison.Ordinal)
                && (!string.Equals(pending.From.Mode, "account", StringComparison.Ordinal)
                    || !string.Equals(pending.To.Mode, "account", StringComparison.Ordinal))))
        {
            pending.Phase = "pick";
            pending.Mode = "";
            return new List<Outbound>
            {
                new(pending.From, new { type = "error", message = "sandbox and account cannot play the same match." }),
                new(pending.To, new { type = "challengeClosed", message = "sandbox and account cannot play the same match." })
            };
        }
        if (_rooms.Count >= Limits.MaxRooms)
            return Err(pending.To, "too many rooms");

        var roomName = NewMatchRoomName();
        var room = new Room { Name = roomName, ListenPort = 7788, Mode = pending.Mode };
        room.Host = new Seat
        {
            Session = pending.From,
            Name = pending.From.Name,
            Role = "host",
            Engine = pending.From.Engine,
            CardHash = pending.From.CardHash
        };
        room.Guest = new Seat
        {
            Session = pending.To,
            Name = pending.To.Name,
            Role = "guest",
            Engine = pending.To.Engine,
            CardHash = pending.To.CardHash
        };
        var lounge = fromSeat.Value.Room;
        lounge.Members.RemoveAll(s => ReferenceEquals(s.Session, pending.From) || ReferenceEquals(s.Session, pending.To));
        _rooms[roomName] = room;
        _challenges.Remove(pending);
        var notes = new List<Outbound>
        {
            new(pending.From, new { type = "matchReady" }),
            new(pending.To, new { type = "matchReady" })
        };
        notes.AddRange(RoomNotes(lounge));
        notes.AddRange(RoomNotes(room));
        return notes;
    }

    public List<Outbound> FriendRequest(LobbySession who, string targetName)
    {
        targetName = LobbySession.ClipName(targetName, 24);
        if (targetName.Length == 0)
            return Err(who, "name required");
        if (!IsAccount(who))
            return Err(who, "login required");

        lock (_gate)
        {
            if (string.Equals(who.Name, targetName, StringComparison.OrdinalIgnoreCase) || _auth.TryUser(targetName, out var selfId, out _) && selfId == who.UserId && string.Equals(who.Name, targetName, StringComparison.OrdinalIgnoreCase))
                return Err(who, "cannot add yourself");

            var known = _auth.TryUser(targetName, out var targetId, out var storedName);
            if (known && targetId == who.UserId)
                return Err(who, "cannot add yourself");

            var sessions = SessionsNamed(known ? storedName : targetName);
            if (!known)
                return Err(who, sessions.Count == 0 ? "unknown name" : "that player has no account");
            if (_auth.AreFriends(who.UserId, targetId))
                return Err(who, "already friends");
            if (sessions.Count == 0)
                return Err(who, "player is offline");
            if (sessions.Count != 1)
                return Err(who, "name is not unique");

            var target = sessions[0];
            if (!IsAccount(target) || target.UserId != targetId)
                return Err(who, "that player has no account");
            if (ReferenceEquals(target, who))
                return Err(who, "cannot add yourself");
            if (_friendAsks.Any(ask => PairAsk(ask, who, target)))
                return Err(who, "request already open");

            var id = Guid.NewGuid().ToString("N");
            _friendAsks.Add(new PendingFriend { Id = id, From = who, To = target });
            return new List<Outbound>
            {
                new(target, new { type = "friendAsk", requestId = id, from = who.Name }),
                new(who, new { type = "friendRequestSent", to = target.Name })
            };
        }
    }

    public List<Outbound> FriendReply(LobbySession who, string requestId, bool accept)
    {
        requestId = (requestId ?? "").Trim();
        lock (_gate)
        {
            var pending = _friendAsks.FirstOrDefault(ask =>
                string.Equals(ask.Id, requestId, StringComparison.Ordinal)
                && ReferenceEquals(ask.To, who));
            if (pending == null)
                return Err(who, "no such friend request");
            _friendAsks.Remove(pending);
            if (!accept)
            {
                return new List<Outbound>
                {
                    new(pending.From, new { type = "friendDeclined", name = who.Name })
                };
            }

            if (!IsAccount(pending.From) || !IsAccount(who))
            {
                return new List<Outbound>
                {
                    new(pending.From, new { type = "error", message = "login required" }),
                    new(who, new { type = "error", message = "login required" })
                };
            }

            _auth.AddFriendship(pending.From.UserId, who.UserId);
            var notes = new List<Outbound>
            {
                new(pending.From, new { type = "friendAccepted", name = who.Name }),
                new(who, new { type = "friendAccepted", name = pending.From.Name })
            };
            notes.Add(FriendListNote(pending.From));
            notes.Add(FriendListNote(who));
            return notes;
        }
    }

    public List<Outbound> FriendRemove(LobbySession who, string targetName)
    {
        targetName = LobbySession.ClipName(targetName, 24);
        if (!IsAccount(who))
            return Err(who, "login required");
        if (targetName.Length == 0)
            return Err(who, "name required");

        lock (_gate)
        {
            if (!_auth.TryUser(targetName, out var targetId, out var storedName))
                return Err(who, "unknown name");
            if (!_auth.RemoveFriendship(who.UserId, targetId))
                return Err(who, "not friends");
            var notes = new List<Outbound>
            {
                new(who, new { type = "friendRemoved", name = storedName }),
                FriendListNote(who)
            };
            foreach (var other in SessionsForUser(targetId))
            {
                notes.Add(new Outbound(other, new { type = "friendRemoved", name = who.Name }));
                notes.Add(FriendListNote(other));
            }
            return notes;
        }
    }

    public List<Outbound> FriendList(LobbySession who)
    {
        lock (_gate)
            return new List<Outbound> { FriendListNote(who) };
    }

    private Outbound FriendListNote(LobbySession who)
    {
        if (!IsAccount(who))
            return new Outbound(who, new { type = "friends", names = Array.Empty<string>(), online = Array.Empty<bool>() });
        var rows = _auth.FriendRows(who.UserId);
        return new Outbound(who, new
        {
            type = "friends",
            names = rows.Select(row => row.Name).ToArray(),
            online = rows.Select(row => SessionsForUser(row.Id).Count > 0).ToArray()
        });
    }

    private List<Outbound> DropFriendAsks(LobbySession who)
    {
        var notes = new List<Outbound>();
        for (var i = _friendAsks.Count - 1; i >= 0; i--)
        {
            var pending = _friendAsks[i];
            if (!ReferenceEquals(pending.From, who) && !ReferenceEquals(pending.To, who))
                continue;
            _friendAsks.RemoveAt(i);
            var other = ReferenceEquals(pending.From, who) ? pending.To : pending.From;
            notes.Add(new Outbound(other, new { type = "friendClosed", message = "player left" }));
        }
        return notes;
    }

    private List<LobbySession> SessionsNamed(string name)
    {
        var found = new List<LobbySession>();
        foreach (var room in _rooms.Values)
        {
            if (room.IsLounge)
            {
                foreach (var member in room.Members)
                {
                    if (string.Equals(member.Name, name, StringComparison.OrdinalIgnoreCase))
                        AddSession(found, member.Session);
                }
            }
            else
            {
                if (NameIs(room.Host, name))
                    AddSession(found, room.Host!.Session);
                if (NameIs(room.Guest, name))
                    AddSession(found, room.Guest!.Session);
            }
        }
        return found;
    }

    private List<LobbySession> SessionsForUser(int userId)
    {
        var found = new List<LobbySession>();
        if (userId <= 0)
            return found;
        foreach (var room in _rooms.Values)
        {
            if (room.IsLounge)
            {
                foreach (var member in room.Members)
                {
                    if (member.Session.UserId == userId)
                        AddSession(found, member.Session);
                }
            }
            else
            {
                if (room.Host?.Session.UserId == userId)
                    AddSession(found, room.Host.Session);
                if (room.Guest?.Session.UserId == userId)
                    AddSession(found, room.Guest.Session);
            }
        }
        return found;
    }

    private static void AddSession(List<LobbySession> found, LobbySession session)
    {
        if (!found.Contains(session))
            found.Add(session);
    }

    private static bool PairAsk(PendingFriend ask, LobbySession left, LobbySession right)
        => (ReferenceEquals(ask.From, left) && ReferenceEquals(ask.To, right))
            || (ReferenceEquals(ask.From, right) && ReferenceEquals(ask.To, left));

    private static bool IsAccount(LobbySession session)
        => string.Equals(session.Mode, "account", StringComparison.Ordinal) && session.UserId > 0;

    private List<Outbound> DropChallenges(LobbySession who)
    {
        var notes = new List<Outbound>();
        for (var i = _challenges.Count - 1; i >= 0; i--)
        {
            var pending = _challenges[i];
            if (!ReferenceEquals(pending.From, who) && !ReferenceEquals(pending.To, who))
                continue;
            _challenges.RemoveAt(i);
            var other = ReferenceEquals(pending.From, who) ? pending.To : pending.From;
            notes.Add(new Outbound(other, new { type = "challengeClosed", message = "player left" }));
        }
        return notes;
    }

    private bool Involved(LobbySession who)
        => _challenges.Any(c => ReferenceEquals(c.From, who) || ReferenceEquals(c.To, who));

    private static bool NameIs(Seat? seat, string name)
        => seat != null && string.Equals(seat.Name, name, StringComparison.OrdinalIgnoreCase);

    private string NewMatchRoomName()
    {
        for (var n = 0; n < 8; n++)
        {
            var name = "M" + Guid.NewGuid().ToString("N")[..8];
            if (!_rooms.ContainsKey(name))
                return name;
        }
        return "M" + Guid.NewGuid().ToString("N");
    }

    private static List<Outbound> Merge(List<Outbound> head, List<Outbound> tail)
    {
        head.AddRange(tail);
        return head;
    }

    public List<Outbound> ChatInvite(LobbySession who, string targetName)
    {
        targetName = LobbySession.ClipName(targetName, 24);
        if (targetName.Length == 0)
            return ChatFail(who, "", "name required");

        lock (_gate)
        {
            if (string.Equals(who.Name, targetName, StringComparison.OrdinalIgnoreCase))
                return ChatFail(who, targetName, "cannot invite yourself");
            var sessions = SessionsNamed(targetName);
            if (sessions.Count == 0)
                return ChatFail(who, targetName, "player is not online");
            if (sessions.Count != 1)
                return ChatFail(who, targetName, "name is not unique");
            var target = sessions[0];
            if (ReferenceEquals(target, who))
                return ChatFail(who, target.Name, "cannot invite yourself");

            var mine = FindPrivate(who);
            var theirs = FindPrivate(target);
            if (mine != null && ReferenceEquals(mine, theirs))
            {
                return new List<Outbound>
                {
                    new(who, new { type = "chatJoined", names = mine.Members.Select(m => m.Name).ToArray() })
                };
            }
            if (theirs != null)
                return ChatFail(who, target.Name, target.Name + " is unavailable");
            if (_chatInvites.Any(invite => ReferenceEquals(invite.From, who) && ReferenceEquals(invite.To, target)))
                return ChatFail(who, target.Name, "invite already sent");
            if (_chatInvites.Any(invite => ReferenceEquals(invite.To, target) || ReferenceEquals(invite.From, target)))
                return ChatFail(who, target.Name, target.Name + " is unavailable");

            var id = Guid.NewGuid().ToString("N");
            _chatInvites.Add(new PendingChatInvite { Id = id, From = who, To = target });
            return new List<Outbound>
            {
                new(target, new { type = "chatInvite", inviteId = id, from = who.Name }),
                new(who, new { type = "chatInviteSent", inviteId = id, to = target.Name })
            };
        }
    }

    public List<Outbound> ChatInviteReply(LobbySession who, string inviteId, bool accept)
    {
        inviteId = (inviteId ?? "").Trim();
        lock (_gate)
        {
            var pending = _chatInvites.FirstOrDefault(invite =>
                string.Equals(invite.Id, inviteId, StringComparison.Ordinal)
                && ReferenceEquals(invite.To, who));
            if (pending == null)
                return ChatFail(who, "", "no such chat invite");
            _chatInvites.Remove(pending);
            if (FindSeat(pending.From) == null)
                return ChatFail(who, pending.From.Name, pending.From.Name + " is unavailable");
            if (!accept)
            {
                return new List<Outbound>
                {
                    new(pending.From, new { type = "chatInviteDeclined", by = who.Name }),
                    new(who, new { type = "chatInviteClosed", message = "declined" })
                };
            }

            var mine = FindPrivate(pending.From);
            var theirs = FindPrivate(who);
            if (theirs != null && !ReferenceEquals(mine, theirs))
            {
                return new List<Outbound>
                {
                    new(pending.From, new { type = "chatInviteFailed", to = who.Name, message = who.Name + " is unavailable" }),
                    new(who, new { type = "chatInviteClosed", message = "unavailable" })
                };
            }

            var chat = mine ?? new PrivateConference();
            if (mine == null)
                _privateChats.Add(chat);
            if (!chat.Members.Any(member => ReferenceEquals(member, pending.From)))
                chat.Members.Add(pending.From);
            if (!chat.Members.Any(member => ReferenceEquals(member, who)))
                chat.Members.Add(who);
            var names = chat.Members.Select(member => member.Name).ToArray();
            return chat.Members
                .Select(member => new Outbound(member, new { type = "chatJoined", names }))
                .ToList();
        }
    }

    public List<Outbound> PrivateChat(LobbySession who, string text)
    {
        text = LobbySession.ClipName(text, 200);
        if (text.Length == 0)
            return Err(who, "empty chat");
        lock (_gate)
        {
            var chat = FindPrivate(who);
            if (chat == null || chat.Members.Count < 2)
                return Err(who, "nobody else is in the private chat");
            return chat.Members
                .Select(member => new Outbound(member, new { type = "privateChat", from = who.Name, text }))
                .ToList();
        }
    }

    private List<Outbound> DropPrivateChat(LobbySession who)
    {
        var notes = new List<Outbound>();
        for (var i = _chatInvites.Count - 1; i >= 0; i--)
        {
            var pending = _chatInvites[i];
            if (!ReferenceEquals(pending.From, who) && !ReferenceEquals(pending.To, who))
                continue;
            _chatInvites.RemoveAt(i);
            if (ReferenceEquals(pending.From, who))
                notes.Add(new Outbound(pending.To, new { type = "chatInviteClosed", message = "player is unavailable" }));
            else
                notes.Add(new Outbound(pending.From, new { type = "chatInviteFailed", to = who.Name, message = who.Name + " is unavailable" }));
        }

        for (var i = _privateChats.Count - 1; i >= 0; i--)
        {
            var chat = _privateChats[i];
            if (!chat.Members.Any(member => ReferenceEquals(member, who)))
                continue;
            chat.Members.RemoveAll(member => ReferenceEquals(member, who));
            var message = who.Name + " left the private chat";
            if (chat.Members.Count < 2)
            {
                foreach (var member in chat.Members)
                    notes.Add(new Outbound(member, new { type = "privateChatEnded", message }));
                _privateChats.RemoveAt(i);
            }
            else
            {
                var names = chat.Members.Select(member => member.Name).ToArray();
                foreach (var member in chat.Members)
                    notes.Add(new Outbound(member, new { type = "chatJoined", names, left = who.Name }));
            }
        }
        return notes;
    }

    private PrivateConference? FindPrivate(LobbySession who)
    {
        foreach (var chat in _privateChats)
        {
            if (chat.Members.Any(member => ReferenceEquals(member, who)))
                return chat;
        }
        return null;
    }

    private static List<Outbound> ChatFail(LobbySession who, string to, string message)
        => new() { new(who, new { type = "chatInviteFailed", to, message }) };


    public List<Outbound> TradeAsk(LobbySession who, string targetName)
    {
        targetName = LobbySession.ClipName(targetName, 24);
        if (!IsAccount(who))
            return TradeFail(who, targetName, "Log in to trade. Nothing moved.");
        if (targetName.Length == 0)
            return TradeFail(who, "", "name required");
        lock (_gate)
        {
            if (string.Equals(who.Name, targetName, StringComparison.OrdinalIgnoreCase))
                return TradeFail(who, targetName, "cannot trade with yourself");
            var sessions = SessionsNamed(targetName);
            if (sessions.Count == 0)
                return TradeFail(who, targetName, targetName + " is offline. Nothing moved.");
            if (sessions.Count != 1)
                return TradeFail(who, targetName, "name is not unique. Nothing moved.");
            var target = sessions[0];
            if (ReferenceEquals(target, who) || target.UserId == who.UserId)
                return TradeFail(who, target.Name, "cannot trade with yourself");
            if (!IsAccount(target))
                return TradeFail(who, target.Name, target.Name + " is unavailable. Nothing moved.");
            if (TradeBusy(who) || TradeBusy(target))
                return TradeFail(who, target.Name, target.Name + " is unavailable. Nothing moved.");
            var id = Guid.NewGuid().ToString("N");
            _tradeAsks.Add(new PendingTradeAsk { Id = id, From = who, To = target });
            return new List<Outbound>
            {
                new(target, new { type = "tradeAsked", requestId = id, from = who.Name }),
                new(who, new { type = "tradeAskSent", requestId = id, to = target.Name })
            };
        }
    }

    public List<Outbound> TradeReply(LobbySession who, string requestId, bool accept)
    {
        requestId = (requestId ?? "").Trim();
        lock (_gate)
        {
            var pending = _tradeAsks.FirstOrDefault(ask =>
                string.Equals(ask.Id, requestId, StringComparison.Ordinal)
                && ReferenceEquals(ask.To, who));
            if (pending == null)
                return TradeFail(who, "", "That trade request is gone. Nothing moved.");
            _tradeAsks.Remove(pending);
            if (FindSeat(pending.From) == null)
                return TradeFail(who, pending.From.Name, pending.From.Name + " is offline. Nothing moved.");
            if (!accept)
            {
                return new List<Outbound>
                {
                    new(pending.From, new { type = "tradeDeclined", by = who.Name }),
                    new(who, new { type = "tradeClosed", sessionId = "", message = "cancelled" })
                };
            }
            if (TradeBusy(pending.From) || TradeBusy(who))
            {
                return new List<Outbound>
                {
                    new(pending.From, new { type = "tradeAskFailed", to = who.Name, message = who.Name + " is unavailable. Nothing moved." }),
                    new(who, new { type = "tradeAskFailed", to = pending.From.Name, message = pending.From.Name + " is unavailable. Nothing moved." })
                };
            }
            var trade = new LiveTrade
            {
                Id = Guid.NewGuid().ToString("N"),
                A = pending.From,
                B = who
            };
            _trades.Add(trade);
            return new List<Outbound>
            {
                new(trade.A, new { type = "tradeOpened", sessionId = trade.Id, partner = trade.B.Name }),
                new(trade.B, new { type = "tradeOpened", sessionId = trade.Id, partner = trade.A.Name }),
                TradeState(trade, trade.A),
                TradeState(trade, trade.B)
            };
        }
    }

    public List<Outbound> TradeSlots(LobbySession who, string sessionId, string[] cards)
    {
        lock (_gate)
        {
            var trade = FindTrade(who, sessionId);
            if (trade == null)
                return new List<Outbound>();
            var mine = ReferenceEquals(trade.A, who) ? trade.OfferA : trade.OfferB;
            var locked = ReferenceEquals(trade.A, who) ? trade.LockA : trade.LockB;
            if (!locked && cards != null && CardsOk(cards))
            {
                for (var i = 0; i < 4; i++)
                    mine[i] = cards[i] ?? "";
            }
            return new List<Outbound> { TradeState(trade, trade.A), TradeState(trade, trade.B) };
        }
    }

    public List<Outbound> TradeLock(LobbySession who, string sessionId)
    {
        lock (_gate)
        {
            var trade = FindTrade(who, sessionId);
            if (trade == null)
                return new List<Outbound>();
            var isA = ReferenceEquals(trade.A, who);
            if (isA ? trade.LockA : trade.LockB)
                return new List<Outbound> { TradeState(trade, trade.A), TradeState(trade, trade.B) };
            if (isA)
                trade.LockA = true;
            else
                trade.LockB = true;
            var notes = new List<Outbound> { TradeState(trade, trade.A), TradeState(trade, trade.B) };
            if (!trade.LockA || !trade.LockB)
                return notes;
            if (!_auth.TryCommitAccountTrade(trade.A.UserId, trade.B.UserId, trade.OfferA, trade.OfferB, out var error))
            {
                _trades.Remove(trade);
                var message = string.IsNullOrWhiteSpace(error)
                    ? "The trade was aborted. Nothing moved."
                    : "The trade was aborted. Nothing moved. " + error;
                notes.Add(new Outbound(trade.A, new { type = "tradeAborted", sessionId = trade.Id, message }));
                notes.Add(new Outbound(trade.B, new { type = "tradeAborted", sessionId = trade.Id, message }));
                return notes;
            }
            for (var i = 0; i < 4; i++)
            {
                trade.OfferA[i] = "";
                trade.OfferB[i] = "";
            }
            trade.LockA = false;
            trade.LockB = false;
            return new List<Outbound>
            {
                TradeState(trade, trade.A),
                TradeState(trade, trade.B),
                new(trade.A, new { type = "tradeDone", sessionId = trade.Id }),
                new(trade.B, new { type = "tradeDone", sessionId = trade.Id })
            };
        }
    }

    public List<Outbound> TradeCancel(LobbySession who, string sessionId)
    {
        lock (_gate)
        {
            var trade = FindTrade(who, sessionId);
            if (trade == null)
                return new List<Outbound>();
            _trades.Remove(trade);
            var other = ReferenceEquals(trade.A, who) ? trade.B : trade.A;
            return new List<Outbound>
            {
                new(who, new { type = "tradeClosed", sessionId = trade.Id, message = "cancelled" }),
                new(other, new { type = "tradeClosed", sessionId = trade.Id, message = who.Name + " cancelled the trade. Nothing moved." })
            };
        }
    }

    private List<Outbound> DropTrades(LobbySession who)
    {
        var notes = new List<Outbound>();
        for (var i = _tradeAsks.Count - 1; i >= 0; i--)
        {
            var pending = _tradeAsks[i];
            if (!ReferenceEquals(pending.From, who) && !ReferenceEquals(pending.To, who))
                continue;
            _tradeAsks.RemoveAt(i);
            if (ReferenceEquals(pending.From, who))
                notes.Add(new Outbound(pending.To, new { type = "tradeAskFailed", to = who.Name, message = who.Name + " is offline. Nothing moved." }));
            else
                notes.Add(new Outbound(pending.From, new { type = "tradeAskFailed", to = who.Name, message = who.Name + " is offline. Nothing moved." }));
        }
        for (var i = _trades.Count - 1; i >= 0; i--)
        {
            var trade = _trades[i];
            if (!ReferenceEquals(trade.A, who) && !ReferenceEquals(trade.B, who))
                continue;
            _trades.RemoveAt(i);
            var other = ReferenceEquals(trade.A, who) ? trade.B : trade.A;
            notes.Add(new Outbound(other, new { type = "tradeClosed", sessionId = trade.Id, message = who.Name + " is offline. Nothing moved." }));
        }
        return notes;
    }

    private bool TradeBusy(LobbySession who)
        => _tradeAsks.Any(ask => ReferenceEquals(ask.From, who) || ReferenceEquals(ask.To, who))
            || _trades.Any(trade => ReferenceEquals(trade.A, who) || ReferenceEquals(trade.B, who));

    private LiveTrade? FindTrade(LobbySession who, string? sessionId)
    {
        sessionId = (sessionId ?? "").Trim();
        return _trades.FirstOrDefault(trade =>
            string.Equals(trade.Id, sessionId, StringComparison.Ordinal)
            && (ReferenceEquals(trade.A, who) || ReferenceEquals(trade.B, who)));
    }

    private static bool CardsOk(string[] cards)
    {
        if (cards.Length != 4)
            return false;
        foreach (var raw in cards)
        {
            var id = raw ?? "";
            if (id.Length == 0)
                continue;
            if (id.Length is < 3 or > 180)
                return false;
            foreach (var ch in id)
            {
                if (char.IsControl(ch))
                    return false;
            }
            var first = id.IndexOf('/');
            if (first <= 0)
                return false;
            var second = id.IndexOf('/', first + 1);
            if (second < 0 || second >= id.Length - 1)
                return false;
        }
        return true;
    }

    private static Outbound TradeState(LiveTrade trade, LobbySession who)
    {
        var mineIsA = ReferenceEquals(who, trade.A);
        return new Outbound(who, new
        {
            type = "tradeState",
            sessionId = trade.Id,
            partner = mineIsA ? trade.B.Name : trade.A.Name,
            mine = Copy4(mineIsA ? trade.OfferA : trade.OfferB),
            theirs = Copy4(mineIsA ? trade.OfferB : trade.OfferA),
            mineLocked = mineIsA ? trade.LockA : trade.LockB,
            theirsLocked = mineIsA ? trade.LockB : trade.LockA
        });
    }

    private static string[] Copy4(string[] source)
    {
        var copy = new string[4];
        for (var i = 0; i < 4; i++)
            copy[i] = source[i] ?? "";
        return copy;
    }

    private static List<Outbound> TradeFail(LobbySession who, string to, string message)
        => new() { new(who, new { type = "tradeAskFailed", to, message }) };

    public List<Outbound> Leave(LobbySession who)
    {
        // Lobby close, including the window closing at game start, must not abort /relay.
        lock (_gate)
        {
            var dropped = Merge(DropChallenges(who), DropFriendAsks(who));
            dropped = Merge(dropped, DropPrivateChat(who));
            dropped = Merge(dropped, DropTrades(who));
            var found = FindSeat(who);
            if (found == null)
                return dropped;
            var room = found.Value.Room;

            if (room.IsLounge)
            {
                room.Members.RemoveAll(s => ReferenceEquals(s.Session, who));
                var notes = RoomNotes(room);
                notes.Add(new Outbound(who, new { type = "gone", message = "you left" }));
                return Merge(dropped, notes);
            }

            // The match is already frozen and the relay seat was authorized.
            // Closing the lobby window must keep this playerId so the same role
            // can replace its relay socket. RelayHub releases the record after
            // the 120s grace. A direct-IP room (no relay) still leaves as before.
            if (room.MatchId != null && room.RelayHeld)
            {
                if (ReferenceEquals(room.Host?.Session, who))
                    room.HostLobbyClosed = true;
                else
                    room.GuestLobbyClosed = true;
                // Grace still running: keep this playerId so the same relay
                // socket can be replaced. Do not delete the relay pair here.
                if (!room.RelayGraceEnded)
                    return dropped;
                return Merge(dropped, FreeClosedSeats(room, who));
            }
            if (ReferenceEquals(room.Host?.Session, who))
            {
                _rooms.Remove(room.Name);
                var notes = new List<Outbound>();
                if (room.Guest != null)
                    notes.Add(new Outbound(room.Guest.Session, new { type = "gone", message = "host left" }));
                notes.Add(new Outbound(who, new { type = "gone", message = "you left" }));
                return Merge(dropped, notes);
            }

            room.Guest = null;
            room.Address = null;
            room.Port = 0;
            var stay = RoomNotes(room);
            stay.Add(new Outbound(who, new { type = "gone", message = "you left" }));
            return Merge(dropped, stay);
        }
    }

    private (Room Room, Seat Seat)? FindSeat(LobbySession who)
    {
        foreach (var room in _rooms.Values)
        {
            if (room.IsLounge)
            {
                var member = room.Members.FirstOrDefault(s => ReferenceEquals(s.Session, who));
                if (member != null)
                    return (room, member);
                continue;
            }
            if (ReferenceEquals(room.Host?.Session, who))
                return (room, room.Host);
            if (ReferenceEquals(room.Guest?.Session, who))
                return (room, room.Guest);
        }
        return null;
    }

    private List<Outbound> RoomNotes(Room room)
    {
        var notes = new List<Outbound>();
        if (room.IsLounge)
        {
            var view = LoungeRoomView(room);
            foreach (var member in room.Members)
            {
                notes.Add(new Outbound(member.Session, view));
            }
            return notes;
        }

        var saves = SavesFor(room);
        var note = SaveNote(room, saves.Length);
        if (room.Host != null)
            notes.Add(new Outbound(room.Host.Session, RoomView(room, "host", saves, note)));
        if (room.Guest != null)
            notes.Add(new Outbound(room.Guest.Session, RoomView(room, "guest", saves, note)));
        return notes;
    }

    private static object LoungeRoomView(Room room)
    {
        return new
        {
            type = "room",
            name = room.Name,
            role = "member",
            address = "",
            port = 0,
            listenPort = 0,
            versionKnown = true,
            versionOk = true,
            versionNote = "Lounge",
            players = room.Members.Select(s => new
            {
                name = s.Name,
                role = s.Role,
                deckName = s.DeckName ?? "",
                deckHash = s.DeckHash ?? ""
            }).ToArray(),
            lines = room.Lines.Select(l => new { from = l.From, text = l.Text }).ToArray(),
            saves = Array.Empty<object>(),
            saveNote = ""
        };
    }

    private object[] SavesFor(Room room)
    {
        if (!string.Equals(room.Mode, "account", StringComparison.Ordinal))
            return Array.Empty<object>();
        if (room.Host == null || room.Guest == null)
            return Array.Empty<object>();
        var hostId = room.Host.Session.UserId;
        var guestId = room.Guest.Session.UserId;
        if (hostId <= 0 || guestId <= 0)
            return Array.Empty<object>();
        return _auth.ListSaves(hostId, guestId);
    }

    private string SaveNote(Room room, int orderedCount)
    {
        if (orderedCount > 0)
            return "";
        if (!string.Equals(room.Mode, "account", StringComparison.Ordinal))
            return "";
        if (room.Host == null || room.Guest == null)
            return "";
        var hostId = room.Host.Session.UserId;
        var guestId = room.Guest.Session.UserId;
        if (hostId <= 0 || guestId <= 0 || hostId == guestId)
            return "";
        if (_auth.ListSaves(guestId, hostId).Length == 0)
            return "";
        return "The original host must host the room to load the save.";
    }

    private static object RoomView(Room room, string role, object[] saves, string saveNote)
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
            lines = room.Lines.Select(l => new { from = l.From, text = l.Text }).ToArray(),
            saves,
            saveNote
        };
    }

    public string? AuthorizeRelay(string playerId, string roomName, string role)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomName, out var room))
                return "no such room";
            if (room.IsLounge)
                return "cannot relay in Lounge";
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
            room.RelayHeld = true;
            // A new live pair. Lobby close must keep the seat until the next grace.
            room.RelayGraceEnded = false;
            return null;
        }
    }

    /// <summary>
    /// Relay pair is already gone after the 120s grace. Free each seat whose
    /// lobby socket is already gone. A live lobby keeps its seat. An empty
    /// room is removed. Does not delete the relay pair and does not delete saves.
    /// </summary>
    public List<Outbound> ReleaseRelayRoom(string roomName)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomName, out var room))
                return new List<Outbound>();
            if (room.IsLounge)
                return new List<Outbound>();
            if (room.MatchId == null || !room.RelayHeld)
                return new List<Outbound>();
            room.RelayGraceEnded = true;
            return FreeClosedSeats(room, null);
        }
    }

    /// <summary>
    /// Caller holds _gate. Grace has ended. A closed lobby seat is cleared.
    /// Both seats gone: the room goes. One live seat stays so the other account can join.
    /// </summary>
    private List<Outbound> FreeClosedSeats(Room room, LobbySession? leaver)
    {
        var notes = new List<Outbound>();
        if (room.HostLobbyClosed && room.Host != null)
        {
            var session = room.Host.Session;
            room.Host = null;
            room.HostLobbyClosed = false;
            room.Address = null;
            room.Port = 0;
            if (leaver != null && ReferenceEquals(session, leaver))
                notes.Add(new Outbound(leaver, new { type = "gone", message = "you left" }));
        }
        if (room.GuestLobbyClosed && room.Guest != null)
        {
            var session = room.Guest.Session;
            room.Guest = null;
            room.GuestLobbyClosed = false;
            room.Address = null;
            room.Port = 0;
            if (leaver != null && ReferenceEquals(session, leaver))
                notes.Add(new Outbound(leaver, new { type = "gone", message = "you left" }));
        }
        if (room.Host == null && room.Guest == null)
        {
            _rooms.Remove(room.Name);
            return notes;
        }
        notes.AddRange(RoomNotes(room));
        return notes;
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

    private sealed class PendingTradeAsk
    {
        public string Id { get; set; } = "";
        public LobbySession From { get; set; } = null!;
        public LobbySession To { get; set; } = null!;
    }

    private sealed class LiveTrade
    {
        public string Id { get; set; } = "";
        public LobbySession A { get; set; } = null!;
        public LobbySession B { get; set; } = null!;
        public string[] OfferA { get; } = new string[] { "", "", "", "" };
        public string[] OfferB { get; } = new string[] { "", "", "", "" };
        public bool LockA;
        public bool LockB;
    }

    private sealed class PendingChatInvite
    {
        public string Id { get; set; } = "";
        public LobbySession From { get; set; } = null!;
        public LobbySession To { get; set; } = null!;
    }

    private sealed class PrivateConference
    {
        public List<LobbySession> Members { get; } = new();
    }

    private sealed class PendingFriend
    {
        public string Id { get; set; } = "";
        public LobbySession From { get; set; } = null!;
        public LobbySession To { get; set; } = null!;
    }

    private sealed class PendingChallenge
    {
        public string Id { get; set; } = "";
        public LobbySession From { get; set; } = null!;
        public LobbySession To { get; set; } = null!;
        public string Mode { get; set; } = "";
        public string Phase { get; set; } = "invite";
    }

    private sealed class Room
    {
        public string Name { get; set; } = "";
        public string Mode { get; set; } = "sandbox";
        public bool IsLounge => string.Equals(Name, RoomBook.LoungeRoomName, StringComparison.OrdinalIgnoreCase);
        public int ListenPort { get; set; }
        public string? MatchId { get; set; }
        public string? HostSecret { get; set; }
        public string? GuestSecret { get; set; }
        public Seat? Host { get; set; }
        public Seat? Guest { get; set; }
        public List<Seat> Members { get; } = new();
        public string? Address { get; set; }
        public int Port { get; set; }
        public bool RelayHeld { get; set; }
        public bool RelayGraceEnded { get; set; }
        public bool HostLobbyClosed { get; set; }
        public bool GuestLobbyClosed { get; set; }
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
