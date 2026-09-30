using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

// Copies one binary frame from one seat to the other. Does not read the frame.
// A seat is dropped only when its own relay socket ends. That aborts the peer
// so the pipe does not stay half open. Lobby leave does not call in here.
sealed class RelayHub
{
    private readonly object _gate = new();
    private readonly Dictionary<string, RelayPair> _pairs = new(StringComparer.Ordinal);

    public async Task RunAsync(WebSocket socket, RoomBook book)
    {
        RelayEnd? me = null;
        try
        {
            var hello = await ReadTextAsync(socket, 4096).ConfigureAwait(false);
            if (hello == null)
                return;
            if (!TryParseJoin(hello, out var room, out var role, out var playerId, out var error))
            {
                await SendTextAsync(socket, new { type = "relay", status = "error", message = error }).ConfigureAwait(false);
                return;
            }

            var denied = book.AuthorizeRelay(playerId, room, role);
            if (denied != null)
            {
                await SendTextAsync(socket, new { type = "relay", status = "error", message = denied }).ConfigureAwait(false);
                return;
            }

            me = new RelayEnd(socket, room, role, playerId);
            bool opened;
            try
            {
                opened = Attach(me);
            }
            catch (InvalidOperationException ex)
            {
                await SendTextAsync(socket, new { type = "relay", status = "error", message = ex.Message }).ConfigureAwait(false);
                return;
            }

            if (!opened)
                await SendTextAsync(socket, new { type = "relay", status = "waiting" }).ConfigureAwait(false);
            else
                await OpenPipeAsync(me).ConfigureAwait(false);

            await PumpAsync(me).ConfigureAwait(false);
        }
        catch (WebSocketException)
        {
            // peer dropped
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("relay ended: " + ex.GetType().Name);
        }
        finally
        {
            if (me != null)
                Drop(me);
        }
    }

    private void Drop(RelayEnd me)
    {
        RelayEnd? other = null;
        lock (_gate)
        {
            if (!_pairs.TryGetValue(me.Room, out var pair))
                return;
            var mine = me.Role == "host" ? pair.Host : pair.Guest;
            if (!ReferenceEquals(mine, me))
                return;
            other = ReferenceEquals(pair.Host, me) ? pair.Guest : pair.Host;
            _pairs.Remove(me.Room);
        }

        CloseQuiet(other, me.PlayerId);
    }

    private bool Attach(RelayEnd me)
    {
        lock (_gate)
        {
            if (!_pairs.TryGetValue(me.Room, out var pair))
            {
                pair = new RelayPair();
                _pairs[me.Room] = pair;
            }

            if (me.Role == "host")
            {
                if (pair.Host != null)
                    throw new InvalidOperationException("host relay already attached");
                pair.Host = me;
            }
            else
            {
                if (pair.Guest != null)
                    throw new InvalidOperationException("guest relay already attached");
                pair.Guest = me;
            }

            return pair.Host != null && pair.Guest != null;
        }
    }

    private async Task OpenPipeAsync(RelayEnd me)
    {
        RelayEnd host;
        RelayEnd guest;
        lock (_gate)
        {
            if (!_pairs.TryGetValue(me.Room, out var pair) || pair.Host == null || pair.Guest == null)
                return;
            host = pair.Host;
            guest = pair.Guest;
        }

        await SendTextAsync(host.Socket, new { type = "relay", status = "open" }).ConfigureAwait(false);
        await SendTextAsync(guest.Socket, new { type = "relay", status = "open" }).ConfigureAwait(false);

        while (true)
        {
            byte[][] hostFrames;
            byte[][] guestFrames;
            lock (_gate)
            {
                if (!_pairs.TryGetValue(me.Room, out var pair)
                    || !ReferenceEquals(pair.Host, host)
                    || !ReferenceEquals(pair.Guest, guest))
                    return;
                lock (host.Pending)
                {
                    hostFrames = host.Pending.ToArray();
                    host.Pending.Clear();
                    host.PendingBytes = 0;
                }
                lock (guest.Pending)
                {
                    guestFrames = guest.Pending.ToArray();
                    guest.Pending.Clear();
                    guest.PendingBytes = 0;
                }
                if (hostFrames.Length == 0 && guestFrames.Length == 0)
                {
                    pair.Forwarding = true;
                    break;
                }
            }

            foreach (var frame in hostFrames)
                await SendBinaryAsync(guest, frame).ConfigureAwait(false);
            foreach (var frame in guestFrames)
                await SendBinaryAsync(host, frame).ConfigureAwait(false);
        }

        Console.Error.WriteLine("relay open room " + me.Room);
    }

    private async Task PumpAsync(RelayEnd me)
    {
        while (me.Socket.State == WebSocketState.Open)
        {
            var incoming = await ReadAnyAsync(me.Socket).ConfigureAwait(false);
            if (incoming == null)
                return;
            if (!incoming.Value.Binary)
                continue;

            var frame = incoming.Value.Bytes;
            RelayEnd? peer;
            lock (_gate)
            {
                if (!_pairs.TryGetValue(me.Room, out var pair) || !pair.Forwarding)
                {
                    Queue(me, frame);
                    continue;
                }
                if (ReferenceEquals(pair.Host, me))
                    peer = pair.Guest;
                else if (ReferenceEquals(pair.Guest, me))
                    peer = pair.Host;
                else
                    peer = null;
                if (peer == null)
                {
                    Queue(me, frame);
                    continue;
                }
            }

            await SendBinaryAsync(peer, frame).ConfigureAwait(false);
        }
    }

    private static void Queue(RelayEnd me, byte[] frame)
    {
        lock (me.Pending)
        {
            if (me.PendingBytes + frame.Length > 2 * 1024 * 1024 || me.Pending.Count >= 8)
                throw new InvalidOperationException("relay peer not here");
            me.Pending.Add(frame);
            me.PendingBytes += frame.Length;
        }
    }

    private static async Task<(bool Binary, byte[] Bytes)?> ReadAnyAsync(WebSocket socket)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[64 * 1024];
        bool? binary = null;
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
                return null;
            var isBinary = result.MessageType == WebSocketMessageType.Binary;
            if (binary == null)
                binary = isBinary;
            else if (binary != isBinary)
                throw new InvalidOperationException("relay frame changed type");
            ms.Write(buffer, 0, result.Count);
            if (ms.Length > 16 * 1024 * 1024)
                throw new InvalidOperationException("relay frame too large");
        }
        while (!result.EndOfMessage);
        return (binary == true, ms.ToArray());
    }

    private static async Task SendBinaryAsync(RelayEnd dest, byte[] payload)
    {
        if (dest.Socket.State != WebSocketState.Open)
            return;
        await dest.Send.WaitAsync().ConfigureAwait(false);
        try
        {
            if (dest.Socket.State == WebSocketState.Open)
                await dest.Socket.SendAsync(payload, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            dest.Send.Release();
        }
    }

    private static async Task SendTextAsync(WebSocket socket, object payload)
    {
        if (socket.State != WebSocketState.Open)
            return;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
    }

    private static void CloseQuiet(RelayEnd? end, string exceptPlayerId)
    {
        if (end == null || string.Equals(end.PlayerId, exceptPlayerId, StringComparison.Ordinal))
            return;
        try
        {
            if (end.Socket.State == WebSocketState.Open)
                end.Socket.Abort();
        }
        catch { /* ignore */ }
    }

    private static bool TryParseJoin(string json, out string room, out string role, out string playerId, out string error)
    {
        room = "";
        role = "";
        playerId = "";
        error = "bad relay join";
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (!string.Equals(type, "relay", StringComparison.Ordinal))
            {
                error = "say relay first";
                return false;
            }
            room = LobbySession.ClipName(ReadString(root, "room"), 24);
            role = ReadString(root, "role");
            playerId = ReadString(root, "playerId").Trim();
            if (room.Length == 0 || (role != "host" && role != "guest") || playerId.Length == 0)
            {
                error = "relay join needs room, role, and playerId";
                return false;
            }
            error = "";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? (el.GetString() ?? "")
            : "";

    private static async Task<string?> ReadTextAsync(WebSocket socket, int max)
    {
        var frame = await ReadFrameAsync(socket, max, textOnly: true).ConfigureAwait(false);
        if (frame == null)
            return null;
        return Encoding.UTF8.GetString(frame);
    }

    private static async Task<byte[]?> ReadFrameAsync(WebSocket socket, int max, bool textOnly)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[64 * 1024];
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
                return null;
            if (textOnly && result.MessageType != WebSocketMessageType.Text)
                throw new InvalidOperationException("relay join must be text");
            if (!textOnly && result.MessageType != WebSocketMessageType.Binary)
                throw new InvalidOperationException("relay frame must be binary");
            ms.Write(buffer, 0, result.Count);
            if (ms.Length > max)
                throw new InvalidOperationException("relay frame too large");
        }
        while (!result.EndOfMessage);
        return ms.ToArray();
    }

    private sealed class RelayPair
    {
        public RelayEnd? Host { get; set; }
        public RelayEnd? Guest { get; set; }
        public bool Forwarding { get; set; }
    }

    private sealed class RelayEnd
    {
        public RelayEnd(WebSocket socket, string room, string role, string playerId)
        {
            Socket = socket;
            Room = room;
            Role = role;
            PlayerId = playerId;
        }

        public WebSocket Socket { get; }
        public string Room { get; }
        public string Role { get; }
        public string PlayerId { get; }
        public SemaphoreSlim Send { get; } = new(1, 1);
        public List<byte[]> Pending { get; } = new();
        public int PendingBytes { get; set; }
    }
}
