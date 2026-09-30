using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StarTrekCCG.Network;

// Verb: network transport tcp json
/// <summary>
/// TCP-Gast: Connect zu Host:Port, Send/Receive von NetMessage, Disconnect.
/// Framing: Int32 BE Länge + UTF-8 JSON. Keine Spiel-Logik.
/// </summary>
public sealed class NetClient : INetLink
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    public bool IsConnected => _client is { Connected: true };

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        if (_client is not null)
            throw new InvalidOperationException("Client already connected. Call Disconnect first.");

        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        _client = client;
        _stream = client.GetStream();
    }

    public Task SendAsync(NetMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return WriteMessageAsync(GetStreamOrThrow(), message, cancellationToken);
    }

    public Task<NetMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        => ReadMessageAsync(GetStreamOrThrow(), cancellationToken);

    public void Disconnect()
    {
        try { _stream?.Close(); } catch { /* ignore */ }
        try { _client?.Close(); } catch { /* ignore */ }
        _stream = null;
        _client = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
        _sendLock.Dispose();
    }

    private NetworkStream GetStreamOrThrow()
    {
        ThrowIfDisposed();
        if (_stream is null)
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");
        return _stream;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(NetClient));
    }

    private async Task WriteMessageAsync(NetworkStream stream, NetMessage message, CancellationToken cancellationToken)
    {
        var json = NetMessage.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var header = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(body.Length));

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static async Task<NetMessage> ReadMessageAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var header = new byte[4];
        await ReadExactAsync(stream, header, cancellationToken).ConfigureAwait(false);
        var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0));
        if (length < 0 || length > 16 * 1024 * 1024)
            throw new InvalidDataException($"Invalid NetMessage length: {length}");

        var body = new byte[length];
        await ReadExactAsync(stream, body, cancellationToken).ConfigureAwait(false);
        return NetMessage.Deserialize(Encoding.UTF8.GetString(body));
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
                throw new EndOfStreamException("Remote closed the connection.");
            offset += read;
        }
    }
}