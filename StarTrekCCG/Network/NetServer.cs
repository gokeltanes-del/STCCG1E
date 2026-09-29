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
/// TCP-Host: Listen, Accept (ein Client), Send/Receive von NetMessage.
/// Framing: Int32 BE Länge + UTF-8 JSON. Keine Spiel-Logik.
/// </summary>
public sealed class NetServer : IDisposable
{
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private IDisposable? _natLease;
    private bool _disposed;

    public bool IsListening => _listener is not null;
    public bool HasClient => _client is { Connected: true };

    public async Task StartAsync(int port, bool loopbackOnly = false, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_listener is not null)
            throw new InvalidOperationException("Server already listening.");

        var address = loopbackOnly ? IPAddress.Loopback : IPAddress.Any;
        _listener = new TcpListener(address, port);
        _listener.Start();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task AcceptClientAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_listener is null)
            throw new InvalidOperationException("Server is not listening. Call StartAsync first.");
        if (_client is not null)
            throw new InvalidOperationException("A client is already connected.");

        _client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        _stream = _client.GetStream();
    }

    public Task SendAsync(NetMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return WriteMessageAsync(GetStreamOrThrow(), message, cancellationToken);
    }

    public Task<NetMessage> ReceiveAsync(CancellationToken cancellationToken = default)
        => ReadMessageAsync(GetStreamOrThrow(), cancellationToken);

    /// <summary>
    /// Close the accepted socket only. The listener stays up so the same game can accept again.
    /// </summary>
    public void DropClient()
    {
        try { _stream?.Close(); } catch { /* ignore */ }
        try { _client?.Close(); } catch { /* ignore */ }
        _stream = null;
        _client = null;
    }

    /// <summary>Released when the listener stops. Idempotent.</summary>
    public void HoldNatLease(IDisposable lease)
    {
        var previous = _natLease;
        _natLease = lease;
        if (!ReferenceEquals(previous, lease))
        {
            try { previous?.Dispose(); } catch { /* ignore */ }
        }
    }

    public void Stop()
    {
        ReleaseNatLease();
        try { _stream?.Close(); } catch { /* ignore */ }
        try { _client?.Close(); } catch { /* ignore */ }
        try { _listener?.Stop(); } catch { /* ignore */ }
        _stream = null;
        _client = null;
        _listener = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _sendLock.Dispose();
    }

    private void ReleaseNatLease()
    {
        var lease = _natLease;
        _natLease = null;
        try { lease?.Dispose(); } catch { /* ignore */ }
    }

    private NetworkStream GetStreamOrThrow()
    {
        ThrowIfDisposed();
        if (_stream is null)
            throw new InvalidOperationException("No client connected. Call AcceptClientAsync first.");
        return _stream;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(NetServer));
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
