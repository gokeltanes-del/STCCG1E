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
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private Task? _read;

    public string? PlayerId { get; private set; }
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

    public async Task ConnectAsync(string host, int port, string name, CancellationToken cancellationToken = default)
    {
        Dispose();
        var socket = new ClientWebSocket();
        var cts = new CancellationTokenSource();
        var uri = new Uri($"ws://{host}:{port}/lobby");
        await socket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        _socket = socket;
        _cts = cts;
        await SendAsync(new { type = "hello", name }).ConfigureAwait(false);
        _read = Task.Run(() => ReadLoopAsync(socket, cts.Token));
    }

    public async Task CreateRoomAsync(string room, int gamePort)
    {
        await SendVersionAsync().ConfigureAwait(false);
        await SendAsync(new { type = "create", room, gamePort }).ConfigureAwait(false);
    }

    public async Task JoinRoomAsync(string room)
    {
        await SendVersionAsync().ConfigureAwait(false);
        await SendAsync(new { type = "join", room }).ConfigureAwait(false);
    }

    private Task SendVersionAsync()
        => SendAsync(new { type = "version", engine = EngineStamp.Id, cardHash = EngineStamp.CardHash });

    public Task RefreshAsync()
        => SendAsync(new { type = "list" });

    public Task SendChatAsync(string text)
        => SendAsync(new { type = "chat", text });

    public Task SendDeckAsync(string deckName, string deckHash)
        => SendAsync(new { type = "deck", deckName, deckHash });

    public Task SendAddressAsync(string host, int port)
        => SendAsync(new { type = "address", host, port });

    public void Dispose()
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _socket?.Abort(); } catch { /* ignore */ }
        try { _socket?.Dispose(); } catch { /* ignore */ }
        _socket = null;
        try { _cts?.Dispose(); } catch { /* ignore */ }
        _cts = null;
        PlayerId = null;
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
        var buffer = new byte[4096];
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
                    if (ms.Length > 8192)
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

    private void Handle(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            switch (type)
            {
                case "welcome":
                    PlayerId = ReadString(root, "playerId");
                    StateChanged?.Invoke("Connected to matchmaking. Create or join a room.");
                    break;
                case "rooms":
                    StateChanged?.Invoke(FormatRooms(root));
                    break;
                case "room":
                    ApplyRoom(root);
                    break;
                case "error":
                    StateChanged?.Invoke(ReadString(root, "message"));
                    break;
                case "gone":
                    InRoom = false;
                    Role = null;
                    RoomName = null;
                    DirectHost = null;
                    DirectPort = 0;
                    PlayersChanged?.Invoke("");
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

        VersionNote = ReadString(root, "versionNote");
        VersionKnown = root.TryGetProperty("versionKnown", out var known) && known.ValueKind == JsonValueKind.True;
        var versionOk = root.TryGetProperty("versionOk", out var okEl) && okEl.ValueKind == JsonValueKind.True;
        VersionBlocksStart = VersionKnown && !versionOk;

        if (VersionBlocksStart)
            StateChanged?.Invoke(VersionNote);
        else if (!VersionKnown)
            StateChanged?.Invoke(VersionNote.Length == 0 ? "Waiting for both version stamps." : VersionNote);
        else if (string.IsNullOrWhiteSpace(address) || port <= 0)
            StateChanged?.Invoke($"In room {RoomName} as {Role}. Versions match. Direct address not published yet.");
        else
            StateChanged?.Invoke($"In room {RoomName} as {Role}. Versions match. Direct {address}:{port}.");
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
