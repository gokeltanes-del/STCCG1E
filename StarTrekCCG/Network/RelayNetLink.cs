using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StarTrekCCG.Network;

/// <summary>
/// Outbound game pipe to LobbyService /relay. Each binary message is one NetMessage
/// frame: Int32 BE length + UTF-8 JSON, the same bytes NetClient writes on TCP.
/// </summary>
public sealed class RelayNetLink : INetLink
{
    private ClientWebSocket? _socket;
    private readonly SemaphoreSlim _send = new(1, 1);
    private readonly object _receiveGate = new();
    private Task<NetMessage>? _receive;
    private TaskCompletionSource<NetMessage>? _receiveTcs;
    private bool _disposed;
    private string _hostName = "";
    private int _port;
    private string _room = "";
    private string _role = "";
    private string _playerId = "";

    public bool IsConnected => _socket is { State: WebSocketState.Open };

    public async Task ConnectAsync(string host, int port, string room, string role, string playerId, CancellationToken cancellationToken = default)
    {
        if (_socket != null)
            throw new InvalidOperationException("Relay already connected.");
        // Direct-IP dials TCP 7777. This socket is only the relay (default 7788).
        if (port == 7777)
            throw new InvalidOperationException("Relay does not dial port 7777.");
        var socket = new ClientWebSocket();
        var uri = new Uri($"ws://{host}:{port}/relay");
        try
        {
            await socket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            var hello = JsonSerializer.Serialize(new { type = "relay", room, role, playerId });
            await socket.SendAsync(Encoding.UTF8.GetBytes(hello), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            while (true)
            {
                var text = await ReadTextAsync(socket, cancellationToken).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(text);
                var status = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() : null;
                if (string.Equals(status, "open", StringComparison.Ordinal))
                    break;
                if (string.Equals(status, "waiting", StringComparison.Ordinal))
                    continue;
                var message = doc.RootElement.TryGetProperty("message", out var msg) ? msg.GetString() : status;
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "Relay refused." : message);
            }
        }
        catch
        {
            try { socket.Abort(); } catch { /* ignore */ }
            socket.Dispose();
            throw;
        }

        _hostName = host;
        _port = port;
        _room = room;
        _role = role;
        _playerId = playerId;
        _socket = socket;
    }

    /// <summary>Same room and seat. Returns only after this socket is open.</summary>
    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_hostName) || _port is < 1 or > 65535)
            throw new InvalidOperationException("Relay has no endpoint.");
        Disconnect();
        await ConnectAsync(_hostName, _port, _room, _role, _playerId, cancellationToken).ConfigureAwait(false);
        if (!IsConnected)
            throw new InvalidOperationException("Relay is not connected.");
    }

    public async Task SendAsync(NetMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var socket = SocketOrThrow();
        var json = NetMessage.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var frame = new byte[4 + body.Length];
        BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length)).CopyTo(frame, 0);
        body.CopyTo(frame, 4);
        await _send.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await socket.SendAsync(frame, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _send.Release();
        }
    }

    public Task<NetMessage> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        Task<NetMessage>? existing = null;
        TaskCompletionSource<NetMessage>? created = null;
        ClientWebSocket? socket = null;
        lock (_receiveGate)
        {
            // One socket read. Lobby-after-handoff and a restarted session loop
            // join this task. A second ReceiveAsync must not throw: ReportDead
            // would Abort this socket, and the relay then aborts the guest.
            if (_receive != null)
                existing = _receive;
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                socket = SocketOrThrow();
                created = new TaskCompletionSource<NetMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                _receiveTcs = created;
                _receive = created.Task;
            }
        }

        if (existing != null)
            return existing;

        _ = PumpAsync(socket!, cancellationToken, created!);
        return created!.Task;
    }

    /// <summary>
    /// Lobby is blocked in ReceiveAsync. The session awaits that same task.
    /// A later ReceiveAsync joins it and does not start a second socket read.
    /// Do not cancel the token inside that read: on net8 that aborts the socket.
    /// </summary>
    public void HandOffInFlightReceive()
    {
        // Mark only. The in-flight task is already the single socket read.
    }

    private async Task PumpAsync(ClientWebSocket socket, CancellationToken cancellationToken, TaskCompletionSource<NetMessage> done)
    {
        try
        {
            var frame = await ReadBinaryAsync(socket, cancellationToken).ConfigureAwait(false);
            if (frame.Length < 4)
                throw new InvalidDataException("Relay frame is too short.");
            var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(frame, 0));
            if (length < 0 || length > 16 * 1024 * 1024 || frame.Length != 4 + length)
                throw new InvalidDataException($"Invalid relay frame length: {length}.");
            done.TrySetResult(NetMessage.Deserialize(Encoding.UTF8.GetString(frame, 4, length)));
        }
        catch (Exception ex)
        {
            done.TrySetException(ex);
        }
        finally
        {
            lock (_receiveGate)
            {
                if (ReferenceEquals(_receive, done.Task))
                {
                    _receive = null;
                    _receiveTcs = null;
                }
            }
        }
    }

    public void Disconnect()
    {
        TaskCompletionSource<NetMessage>? pending;
        lock (_receiveGate)
        {
            pending = _receiveTcs;
            _receive = null;
            _receiveTcs = null;
        }
        try { _socket?.Abort(); } catch { /* ignore */ }
        try { _socket?.Dispose(); } catch { /* ignore */ }
        _socket = null;
        pending?.TrySetException(new IOException("Relay socket closed."));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
        _send.Dispose();
    }

    private ClientWebSocket SocketOrThrow()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RelayNetLink));
        if (_socket == null || _socket.State != WebSocketState.Open)
            throw new InvalidOperationException("Relay is not connected.");
        return _socket;
    }

    private static async Task<string> ReadTextAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var bytes = await ReadMessageAsync(socket, 8192, WebSocketMessageType.Text, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

    private static async Task<byte[]> ReadBinaryAsync(ClientWebSocket socket, CancellationToken cancellationToken)
        => await ReadMessageAsync(socket, 16 * 1024 * 1024 + 4, WebSocketMessageType.Binary, cancellationToken).ConfigureAwait(false);

    private static async Task<byte[]> ReadMessageAsync(ClientWebSocket socket, int max, WebSocketMessageType expected, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[64 * 1024];
        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new EndOfStreamException("Relay closed the connection.");
            if (result.MessageType != expected)
                throw new InvalidDataException("Unexpected relay message.");
            ms.Write(buffer, 0, result.Count);
            if (ms.Length > max)
                throw new InvalidDataException("Relay message is too large.");
        }
        while (!result.EndOfMessage);
        return ms.ToArray();
    }
}