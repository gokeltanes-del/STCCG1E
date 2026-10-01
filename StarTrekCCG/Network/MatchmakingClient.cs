using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StarTrekCCG.Network;

/// <summary>
/// Talks to the in-memory lobby service. Deck JSON never leaves this process through here.
/// The socket URL is always ws://host:port/lobby. No other path is fetched.
/// </summary>
public sealed class MatchmakingClient : IDisposable
{
    private readonly SemaphoreSlim _send = new(1, 1);
    private readonly object _waitGate = new();
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private Task? _read;
    private TaskCompletionSource<JsonElement>? _pending;
    private TaskCompletionSource<bool>? _matchWait;

    public string? PlayerId { get; private set; }
    public string? DisplayName { get; private set; }
    public string Mode { get; private set; } = "sandbox";
    public string? Token { get; private set; }
    public string? MatchId { get; private set; }
    public string? ReportSecret { get; private set; }
    public string? Role { get; private set; }
    public string VersionNote { get; private set; } = "";
    public bool VersionKnown { get; private set; }
    public bool VersionBlocksStart { get; private set; }
    public bool InRoom { get; private set; }
    public string? RoomName { get; private set; }
    public string? DirectHost { get; private set; }
    public int DirectPort { get; private set; }

    public event Action<string>? StateChanged;
    public event Action<string>? PlayersChanged;
    public event Action<string>? ChatChanged;
    public event Action<string, int>? AddressAnnounced;
    public event Action? SavesChanged;
    /// <summary>Guest only. The host load reply is not raised here. Blob is empty.</summary>
    public event Action<string, string, string>? ResumeOffered;

    public IReadOnlyList<LobbySaveInfo> Saves { get; private set; } = Array.Empty<LobbySaveInfo>();

    public async Task ConnectAsync(string host, int port, string name, string mode, string? token, CancellationToken cancellationToken = default)
    {
        await ConnectSocketAsync(host, port, cancellationToken).ConfigureAwait(false);
        Mode = string.Equals(mode, "account", StringComparison.Ordinal) ? "account" : "sandbox";
        Token = Mode == "account" ? token : null;
        var hello = Envelope("hello");
        hello["name"] = name ?? "";
        hello["mode"] = Mode;
        await SendAsync(hello).ConfigureAwait(false);
    }

    public async Task ConnectSocketAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        Dispose();
        var socket = new ClientWebSocket();
        var cts = new CancellationTokenSource();
        var uri = new Uri($"ws://{host}:{port}/lobby");
        await socket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        _socket = socket;
        _cts = cts;
        _read = Task.Run(() => ReadLoopAsync(socket, cts.Token));
    }

    public async Task CreateRoomAsync(string room, int gamePort)
    {
        await SendVersionAsync().ConfigureAwait(false);
        var env = Envelope("create");
        env["room"] = room;
        env["gamePort"] = gamePort;
        await SendAsync(env).ConfigureAwait(false);
    }

    public async Task JoinRoomAsync(string room)
    {
        await SendVersionAsync().ConfigureAwait(false);
        var env = Envelope("join");
        env["room"] = room;
        await SendAsync(env).ConfigureAwait(false);
    }

    private Task SendVersionAsync()
    {
        var env = Envelope("version");
        env["engine"] = EngineStamp.Id;
        env["cardHash"] = EngineStamp.CardHash;
        return SendAsync(env);
    }

    public Task RefreshAsync()
        => SendAsync(Envelope("list"));

    public Task SendChatAsync(string text)
    {
        var env = Envelope("chat");
        env["text"] = text;
        return SendAsync(env);
    }

    public Task SendDeckAsync(string deckName, string deckHash, string? cardIdsJson)
    {
        var env = Envelope("deck");
        env["deckName"] = deckName;
        env["deckHash"] = deckHash;
        if (!string.IsNullOrEmpty(cardIdsJson))
            env["cardIds"] = JsonSerializer.Deserialize<JsonElement>(cardIdsJson);
        return SendAsync(env);
    }

    public Task SendAddressAsync(string host, int port)
    {
        var env = Envelope("address");
        env["host"] = host;
        env["port"] = port;
        return SendAsync(env);
    }

    public async Task BeginAsync()
    {
        TaskCompletionSource<bool> wait;
        lock (_waitGate)
        {
            if (_matchWait != null)
                throw new InvalidOperationException("Already waiting for a match freeze.");
            wait = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _matchWait = wait;
        }
        await SendAsync(Envelope("begin")).ConfigureAwait(false);
        await wait.Task.WaitAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
        if (string.IsNullOrEmpty(MatchId) || string.IsNullOrEmpty(ReportSecret))
            throw new InvalidOperationException("The server did not freeze the match.");
    }

    public async Task WaitForMatchAsync()
    {
        if (!string.IsNullOrEmpty(MatchId) && !string.IsNullOrEmpty(ReportSecret))
            return;
        TaskCompletionSource<bool> wait;
        lock (_waitGate)
        {
            if (!string.IsNullOrEmpty(MatchId) && !string.IsNullOrEmpty(ReportSecret))
                return;
            _matchWait ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            wait = _matchWait;
        }
        await wait.Task.WaitAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
        if (string.IsNullOrEmpty(MatchId) || string.IsNullOrEmpty(ReportSecret))
            throw new InvalidOperationException("The server did not freeze the match.");
    }

    public async Task<JsonElement> RegisterAsync(string name, string password)
        => await RequestAsync(new { type = "register", name, password }).ConfigureAwait(false);

    public async Task<JsonElement> LoginAsync(string name, string password)
        => await RequestAsync(new { type = "login", name, password }).ConfigureAwait(false);

    public async Task<JsonElement> SaveDeckAsync(string token, string name, string cardIdsJson)
        => await RequestAsync(new
        {
            type = "deckSave",
            token,
            name,
            cardIds = JsonSerializer.Deserialize<JsonElement>(cardIdsJson)
        }).ConfigureAwait(false);

    public async Task<JsonElement> ListDecksAsync(string token)
        => await RequestAsync(new { type = "deckList", token }).ConfigureAwait(false);

    public async Task<JsonElement> GetDeckAsync(string token, string name)
        => await RequestAsync(new { type = "deckGet", token, name }).ConfigureAwait(false);

    public async Task<JsonElement> GetPoolAsync(string token)
        => await RequestAsync(new { type = "get_pool", token }).ConfigureAwait(false);

    public async Task<JsonElement> ReportAsync(string matchId, string secret, string winner)
        => await RequestAsync(new { type = "report", matchId, secret, winner }).ConfigureAwait(false);

    public async Task<JsonElement> SaveAutoAsync(string matchId, string secret, string blob)
        => await RequestAsync(new { type = "saveAuto", matchId, secret, blob }).ConfigureAwait(false);

    public async Task<JsonElement> SaveManualAsync(string matchId, string secret, string name, string blob)
        => await RequestAsync(new { type = "saveManual", matchId, secret, name, blob }).ConfigureAwait(false);

    public async Task<JsonElement> LoadAsync(string matchId, string kind, string name)
    {
        var env = Envelope("load");
        env["matchId"] = matchId;
        env["kind"] = kind;
        env["name"] = name ?? "";
        return await RequestAsync(env).ConfigureAwait(false);
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _socket?.Abort(); } catch { /* ignore */ }
        try { _socket?.Dispose(); } catch { /* ignore */ }
        _socket = null;
        try { _cts?.Dispose(); } catch { /* ignore */ }
        _cts = null;
        PlayerId = null;
        DisplayName = null;
        Token = null;
        Mode = "sandbox";
        MatchId = null;
        ReportSecret = null;
        Role = null;
        InRoom = false;
        VersionNote = "";
        VersionKnown = false;
        VersionBlocksStart = false;
        RoomName = null;
        DirectHost = null;
        DirectPort = 0;
    }

    private async Task SendAsync(object payload)
    {
        var socket = _socket;
        if (socket == null || socket.State != WebSocketState.Open)
            throw new InvalidOperationException("Matchmaking is not connected.");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        await _send.WaitAsync().ConfigureAwait(false);
        try
        {
            if (socket.State == WebSocketState.Open)
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _send.Release();
        }
    }

    private async Task ReadLoopAsync(ClientWebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[8192];
        try
        {
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, ct).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                        return;
                    ms.Write(buffer, 0, result.Count);
                    if (ms.Length > 2097152)
                        return;
                }
                while (!result.EndOfMessage);

                Handle(Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length));
            }
        }
        catch (OperationCanceledException)
        {
            // disposed
        }
        catch (WebSocketException)
        {
            StateChanged?.Invoke("Matchmaking connection lost. Direct IP and Localhost still work.");
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke("Matchmaking stopped: " + ex.Message);
        }
    }


    private Dictionary<string, object?> Envelope(string type)
    {
        var env = new Dictionary<string, object?> { ["type"] = type };
        if (string.Equals(Mode, "account", StringComparison.Ordinal) && !string.IsNullOrEmpty(Token))
            env["token"] = Token;
        return env;
    }

    private async Task<JsonElement> RequestAsync(object payload)
    {
        var wait = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_waitGate)
        {
            if (_pending != null)
                throw new InvalidOperationException("A lobby request is already waiting.");
            _pending = wait;
        }
        try
        {
            await SendAsync(payload).ConfigureAwait(false);
            return await wait.Task.WaitAsync(TimeSpan.FromSeconds(8)).ConfigureAwait(false);
        }
        finally
        {
            lock (_waitGate)
            {
                if (ReferenceEquals(_pending, wait))
                    _pending = null;
            }
        }
    }

    private void Handle(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (string.Equals(type, "resume", StringComparison.Ordinal))
            {
                MatchId = ReadString(root, "matchId");
                ReportSecret = ReadString(root, "secret");
                var blob = ReadString(root, "blob");
                TaskCompletionSource<JsonElement>? pending;
                lock (_waitGate)
                {
                    pending = _pending;
                    if (pending != null)
                        _pending = null;
                }
                if (pending != null)
                {
                    pending.TrySetResult(root.Clone());
                    return;
                }
                ResumeOffered?.Invoke(MatchId ?? "", ReportSecret ?? "", blob);
                return;
            }
            if (string.Equals(type, "match", StringComparison.Ordinal))
            {
                MatchId = ReadString(root, "matchId");
                ReportSecret = ReadString(root, "secret");
                TaskCompletionSource<bool>? wait;
                lock (_waitGate)
                {
                    wait = _matchWait;
                    _matchWait = null;
                }
                wait?.TrySetResult(true);
                return;
            }
            if (string.Equals(type, "error", StringComparison.Ordinal))
            {
                var message = ReadString(root, "message");
                TaskCompletionSource<bool>? matchWait;
                TaskCompletionSource<JsonElement>? pending;
                lock (_waitGate)
                {
                    matchWait = _matchWait;
                    _matchWait = null;
                    pending = _pending;
                    _pending = null;
                }
                if (matchWait != null)
                {
                    matchWait.TrySetException(new InvalidOperationException(message));
                    return;
                }
                if (pending != null)
                {
                    pending.TrySetResult(root.Clone());
                    return;
                }
                StateChanged?.Invoke(message);
                return;
            }
            if (_pending != null && type is "auth" or "deckSaved" or "deckList" or "deckBody" or "pool" or "report" or "saveStored")
            {
                var copy = root.Clone();
                TaskCompletionSource<JsonElement>? pending;
                lock (_waitGate)
                {
                    pending = _pending;
                    _pending = null;
                }
                pending?.TrySetResult(copy);
                return;
            }
            switch (type)
            {
                case "welcome":
                    PlayerId = ReadString(root, "playerId");
                    var bound = ReadString(root, "name");
                    if (bound.Length > 0)
                        DisplayName = bound;
                    StateChanged?.Invoke("Connected to matchmaking. Create or join a room.");
                    break;
                case "rooms":
                    StateChanged?.Invoke(FormatRooms(root));
                    break;
                case "room":
                    ApplyRoom(root);
                    break;
                case "gone":
                    InRoom = false;
                    Role = null;
                    RoomName = null;
                    DirectHost = null;
                    DirectPort = 0;
                    Saves = Array.Empty<LobbySaveInfo>();
                    PlayersChanged?.Invoke("");
                    SavesChanged?.Invoke();
                    StateChanged?.Invoke(ReadString(root, "message"));
                    break;
                default:
                    StateChanged?.Invoke("Ignored lobby message.");
                    break;
            }
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke("Bad lobby message: " + ex.Message);
        }
    }

    private void ApplyRoom(JsonElement root)
    {
        InRoom = true;
        Role = ReadString(root, "role");
        RoomName = ReadString(root, "name");
        var address = ReadString(root, "address");
        var port = root.TryGetProperty("port", out var portEl) && portEl.TryGetInt32(out var n) ? n : 0;
        if (!string.IsNullOrWhiteSpace(address) && port > 0)
        {
            var changed = !string.Equals(DirectHost, address, StringComparison.Ordinal) || DirectPort != port;
            DirectHost = address;
            DirectPort = port;
            if (changed)
                AddressAnnounced?.Invoke(address, port);
        }

        var players = new StringBuilder();
        if (root.TryGetProperty("players", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in arr.EnumerateArray())
            {
                var hash = ReadString(p, "deckHash");
                var shortHash = hash.Length >= 8 ? hash[..8] : (hash.Length == 0 ? "no hash" : hash);
                if (players.Length > 0)
                    players.AppendLine();
                players.Append(ReadString(p, "role"));
                players.Append(' ');
                players.Append(ReadString(p, "name"));
                players.Append(" — ");
                var deck = ReadString(p, "deckName");
                players.Append(deck.Length == 0 ? "no deck" : "deck");
                players.Append(" (");
                players.Append(shortHash);
                players.Append(')');
            }
        }
        PlayersChanged?.Invoke(players.ToString());

        var chat = new StringBuilder();
        if (root.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
        {
            foreach (var line in lines.EnumerateArray())
            {
                if (chat.Length > 0)
                    chat.AppendLine();
                chat.Append(ReadString(line, "from"));
                chat.Append(": ");
                chat.Append(ReadString(line, "text"));
            }
        }
        ChatChanged?.Invoke(chat.ToString());

        var saves = new List<LobbySaveInfo>();
        if (root.TryGetProperty("saves", out var saveEl) && saveEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in saveEl.EnumerateArray())
            {
                var kind = ReadString(row, "kind");
                if (kind is not ("auto" or "manual"))
                    continue;
                var id = ReadString(row, "matchId");
                if (id.Length == 0)
                    continue;
                saves.Add(new LobbySaveInfo
                {
                    MatchId = id,
                    Kind = kind,
                    Name = ReadString(row, "name"),
                    SavedUtc = ReadString(row, "savedUtc")
                });
            }
        }
        Saves = saves;
        SavesChanged?.Invoke();

        var saveNote = ReadString(root, "saveNote");
        VersionNote = ReadString(root, "versionNote");
        VersionKnown = root.TryGetProperty("versionKnown", out var known) && known.ValueKind == JsonValueKind.True;
        var versionOk = root.TryGetProperty("versionOk", out var okEl) && okEl.ValueKind == JsonValueKind.True;
        VersionBlocksStart = VersionKnown && !versionOk;

        var saveTail = Saves.Count > 0
            ? " Saved game for these two accounts. Play via relay, then Load game."
            : "";
        if (saveNote.Length > 0)
            StateChanged?.Invoke(saveNote);
        else if (VersionBlocksStart)
            StateChanged?.Invoke(VersionNote);
        else if (!VersionKnown)
            StateChanged?.Invoke((VersionNote.Length == 0 ? "Waiting for both version stamps." : VersionNote) + saveTail);
        else if (string.IsNullOrWhiteSpace(address) || port <= 0)
            StateChanged?.Invoke($"In room {RoomName} as {Role}. Versions match. Direct address not published yet." + saveTail);
        else
            StateChanged?.Invoke($"In room {RoomName} as {Role}. Versions match. Direct {address}:{port}." + saveTail);
    }

    private static string FormatRooms(JsonElement root)
    {
        if (!root.TryGetProperty("rooms", out var rooms) || rooms.ValueKind != JsonValueKind.Array || rooms.GetArrayLength() == 0)
            return "No rooms.";
        var sb = new StringBuilder("Rooms: ");
        var first = true;
        foreach (var room in rooms.EnumerateArray())
        {
            if (!first)
                sb.Append(", ");
            first = false;
            sb.Append(ReadString(room, "name"));
            sb.Append(" (");
            sb.Append(room.TryGetProperty("players", out var n) && n.TryGetInt32(out var count) ? count : 0);
            sb.Append(ReadString(room, "open") == "true" || (room.TryGetProperty("open", out var open) && open.ValueKind == JsonValueKind.True) ? ", open" : ", full");
            sb.Append(')');
        }
        return sb.ToString();
    }

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? (el.GetString() ?? "")
            : "";
}

public sealed class LobbySaveInfo
{
    public string MatchId { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Name { get; init; } = "";
    public string SavedUtc { get; init; } = "";
}
