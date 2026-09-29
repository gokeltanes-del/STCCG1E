using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StarTrekCCG.Network;

/// <summary>
/// Owns NetServer XOR NetClient after lobby handshake.
/// Host evaluates Actions; Guest sends Actions and applies masked State.
/// Receive loop posts events onto a sync context / dispatcher callback.
/// </summary>
public sealed class NetPlaySession : IDisposable
{
    public enum SessionRole { Host, Guest }

    private readonly NetServer? _server;
    private readonly NetClient? _client;
    private readonly CancellationTokenSource _cts = new();
    private Task? _receiveTask;
    private SynchronizationContext? _sync;
    private bool _disposed;
    private long _seq;
    private const int PingIntervalMs = 5000;
    private const int DeadAfterMs = 15000;
    private long _lastInboundTicks;
    private int _deadReported;
    private int _loopGeneration;
    private string? _sessionToken;
    public const int ReconnectGraceMs = 120_000;

    public SessionRole Role { get; }
    public int LocalPlayer { get; }
    public bool IsHost => Role == SessionRole.Host;
    public bool IsGuest => Role == SessionRole.Guest;

    /// <summary>Host: remote GameAction arrived (already marshalled to sync context).</summary>
    public event Action<NetActionDto>? ActionReceived;

    /// <summary>Guest: masked GameSave from host.</summary>
    public event Action<GameSave>? StateReceived;

    /// <summary>Either side: Error payload text.</summary>
    public event Action<string>? ErrorReceived;

    /// <summary>Incoming ChoiceRequest (Guest receives Host asks).</summary>
    public event Action<NetChoiceDto>? ChoiceRequestReceived;

    /// <summary>Incoming ChoiceResponse (Host awaits Guest answer).</summary>
    public event Action<NetChoiceDto>? ChoiceResponseReceived;

    /// <summary>Guest: Host play fly-in reveal after successful Play.</summary>
    public event Action<NetPlayRevealDto>? PlayRevealReceived;

    /// <summary>Transport fault / disconnect.</summary>
    public event Action<string>? Disconnected;

    /// <summary>Socket died. The game stays up; the listener is still open on the host.</summary>
    public event Action<string>? TransportLost;

    /// <summary>Guest: resume token for this game. The value is not written to the game log.</summary>
    public event Action<string>? SessionTokenReceived;

    private NetPlaySession(SessionRole role, int localPlayer, NetServer? server, NetClient? client)
    {
        Role = role;
        LocalPlayer = localPlayer;
        _server = server;
        _client = client;
    }

    public static NetPlaySession CreateHost(NetServer server, int localPlayer = 1)
    {
        ArgumentNullException.ThrowIfNull(server);
        return new NetPlaySession(SessionRole.Host, localPlayer, server, null);
    }

    public static NetPlaySession CreateGuest(NetClient client, int localPlayer = 2)
    {
        ArgumentNullException.ThrowIfNull(client);
        return new NetPlaySession(SessionRole.Guest, localPlayer, null, client);
    }

    /// <summary>
    /// Start background receive loop. Events are posted to <paramref name="sync"/>
    /// (pass DispatcherSynchronizationContext / SynchronizationContext.Current from UI thread).
    /// </summary>
    public void StartReceiveLoop(SynchronizationContext? sync = null)
    {
        ThrowIfDisposed();
        if (_receiveTask != null)
            throw new InvalidOperationException("Receive loop already started.");
        _sync = sync ?? SynchronizationContext.Current;
        var ct = _cts.Token;
        var generation = Volatile.Read(ref _loopGeneration);
        _receiveTask = Task.Run(() => ReceiveLoopAsync(ct, generation), ct);
    }

    /// <summary>
    /// After the same guest is back on a new socket. The previous loop must not
    /// mark that socket dead.
    /// </summary>
    public void RestartReceiveLoop()
    {
        ThrowIfDisposed();
        var generation = Interlocked.Increment(ref _loopGeneration);
        Interlocked.Exchange(ref _deadReported, 0);
        Interlocked.Exchange(ref _lastInboundTicks, Environment.TickCount64);
        var ct = _cts.Token;
        _receiveTask = Task.Run(() => ReceiveLoopAsync(ct, generation), ct);
    }

    /// <summary>Host, once per game. A reconnect does not mint a new token.</summary>
    public Task IssueSessionTokenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!IsHost)
            throw new InvalidOperationException("Only the host issues the session token.");
        if (string.IsNullOrEmpty(_sessionToken))
            _sessionToken = Guid.NewGuid().ToString("N");
        var payload = JsonSerializer.Serialize(new { token = _sessionToken, player = 2 });
        var msg = NetMessage.Create(NetMessage.Types.Session, payloadJson: payload, seq: NextSeq());
        return SendRawAsync(msg, cancellationToken);
    }

    public enum ResumeResult { Accepted, Rejected, TimedOut, Cancelled }

    /// <summary>
    /// Block until a socket arrives, then read one resume message.
    /// A mismatch drops that socket and leaves the listener up.
    /// </summary>
    public async Task<ResumeResult> AcceptSameGuestAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        if (_server is null)
            throw new InvalidOperationException("Only the host accepts a resume.");
        await _server.AcceptClientAsync(cancellationToken).ConfigureAwait(false);
        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        readCts.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            var msg = await _server.ReceiveAsync(readCts.Token).ConfigureAwait(false);
            if (IsMatchingResume(msg))
                return ResumeResult.Accepted;
            DropAcceptedClient();
            return ResumeResult.Rejected;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            DropAcceptedClient();
            return ResumeResult.TimedOut;
        }
        catch (OperationCanceledException)
        {
            DropAcceptedClient();
            return ResumeResult.Cancelled;
        }
        catch
        {
            DropAcceptedClient();
            return ResumeResult.Rejected;
        }
    }

    public void DropAcceptedClient()
    {
        try { _server?.DropClient(); } catch { /* ignore */ }
    }

    public bool HostListening => _server?.IsListening == true;

    /// <summary>Grace elapsed. Same end as a transport fault: listener closes, Disconnected fires.</summary>
    public void AbandonAfterGrace()
    {
        if (_disposed) return;
        Interlocked.Increment(ref _loopGeneration);
        try { _server?.Stop(); } catch { /* ignore */ }
        try { _client?.Disconnect(); } catch { /* ignore */ }
        Post(() => Disconnected?.Invoke("Reconnect grace ended (120s)."));
    }

    private bool IsMatchingResume(NetMessage msg)
    {
        if (!string.Equals(msg.Type, NetMessage.Types.Resume, StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrWhiteSpace(msg.PayloadJson) || string.IsNullOrEmpty(_sessionToken))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(msg.PayloadJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("token", out var tok) || !root.TryGetProperty("player", out var pl))
                return false;
            if (pl.ValueKind != JsonValueKind.Number || pl.GetInt32() != 2)
                return false;
            var token = tok.GetString();
            return !string.IsNullOrEmpty(token)
                && string.Equals(token, _sessionToken, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    public Task BroadcastStateAsync(GameSave masked, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!IsHost)
            throw new InvalidOperationException("Only Host may broadcast state.");
        ArgumentNullException.ThrowIfNull(masked);
        var json = JsonSerializer.Serialize(masked);
        var msg = NetMessage.Create(NetMessage.Types.State, payloadJson: json, seq: NextSeq());
        return SendRawAsync(msg, cancellationToken);
    }

    public Task SendActionAsync(NetActionDto dto, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!IsGuest)
            throw new InvalidOperationException("Only Guest sends actions to Host (Host applies locally).");
        ArgumentNullException.ThrowIfNull(dto);
        var msg = NetMessage.Create(NetMessage.Types.Action, payloadJson: dto.ToJson(), seq: NextSeq());
        return SendRawAsync(msg, cancellationToken);
    }

    public Task SendErrorAsync(string message, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var payload = JsonSerializer.Serialize(new { message });
        var msg = NetMessage.Create(NetMessage.Types.Error, payloadJson: payload, seq: NextSeq());
        return SendRawAsync(msg, cancellationToken);
    }


    /// <summary>Host (typically) asks the remote player for a choice / response-window pass.</summary>
    public Task SendChoiceRequestAsync(NetChoiceDto dto, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(dto);
        if (string.IsNullOrWhiteSpace(dto.CorrelationId))
            dto.CorrelationId = Guid.NewGuid().ToString("N");
        var msg = NetMessage.Create(
            NetMessage.Types.ChoiceRequest,
            payloadJson: dto.ToJson(),
            seq: NextSeq(),
            correlationId: dto.CorrelationId);
        return SendRawAsync(msg, cancellationToken);
    }

    /// <summary>Guest (typically) answers a ChoiceRequest.</summary>
    public Task SendChoiceResponseAsync(NetChoiceDto dto, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(dto);
        var msg = NetMessage.Create(
            NetMessage.Types.ChoiceResponse,
            payloadJson: dto.ToJson(),
            seq: NextSeq(),
            correlationId: dto.CorrelationId);
        return SendRawAsync(msg, cancellationToken);
    }

    /// <summary>Host: both clients show the same play fly-in (Guest listens; Host shows locally).</summary>
    public Task BroadcastPlayRevealAsync(NetPlayRevealDto dto, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (!IsHost)
            throw new InvalidOperationException("Only Host may broadcast play reveal.");
        ArgumentNullException.ThrowIfNull(dto);
        var msg = NetMessage.Create(
            NetMessage.Types.PlayReveal,
            payloadJson: dto.ToJson(),
            seq: NextSeq());
        return SendRawAsync(msg, cancellationToken);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct, int generation)
    {
        Interlocked.Exchange(ref _lastInboundTicks, Environment.TickCount64);
        _ = HeartbeatLoopAsync(ct, generation);
        try
        {
            while (!ct.IsCancellationRequested
                   && Volatile.Read(ref _deadReported) == 0
                   && generation == Volatile.Read(ref _loopGeneration))
            {
                NetMessage? msg;
                try
                {
                    msg = await ReceiveNextAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested || _disposed)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested || _disposed)
                        break;
                    if (generation != Volatile.Read(ref _loopGeneration))
                        break;
                    ReportDead(ex.Message, generation);
                    break;
                }

                if (msg == null)
                    break;
                if (generation != Volatile.Read(ref _loopGeneration))
                    break;

                Interlocked.Exchange(ref _lastInboundTicks, Environment.TickCount64);
                HandleMessage(msg, generation);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    /// <summary>
    /// One framed read. No bytes for <see cref="DeadAfterMs"/> is a dead socket
    /// (silent drop has no FIN/RST) and uses the same cleanup as a socket fault.
    /// </summary>
    private async Task<NetMessage?> ReceiveNextAsync(CancellationToken ct)
    {
        using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        readCts.CancelAfter(DeadAfterMs);
        try
        {
            if (_server != null)
                return await _server.ReceiveAsync(readCts.Token).ConfigureAwait(false);
            if (_client != null)
                return await _client.ReceiveAsync(readCts.Token).ConfigureAwait(false);
            return null;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Connection timed out: no packet for 15s.");
        }
    }

    /// <summary>
    /// NAT keepalive. Ping is written on the socket every 5s. Any inbound packet
    /// (Ping, Pong, or a game message) resets the 15s dead timer. Not a game rule.
    /// </summary>
    private async Task HeartbeatLoopAsync(CancellationToken ct, int generation)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(PingIntervalMs));
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (ct.IsCancellationRequested || _disposed || Volatile.Read(ref _deadReported) != 0)
                    return;
                if (generation != Volatile.Read(ref _loopGeneration))
                    return;

                var silent = Environment.TickCount64 - Interlocked.Read(ref _lastInboundTicks);
                if (silent >= DeadAfterMs)
                {
                    ReportDead("Connection timed out: no packet for 15s.", generation);
                    return;
                }

                await SendRawAsync(
                    NetMessage.Create(NetMessage.Types.Ping, seq: NextSeq()),
                    ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // session shutdown
        }
        catch (Exception ex)
        {
            if (ct.IsCancellationRequested || _disposed)
                return;
            if (generation != Volatile.Read(ref _loopGeneration))
                return;
            ReportDead(ex.Message, generation);
        }
    }

    /// <summary>
    /// Heartbeat death. Drop the socket once and raise TransportLost.
    /// The host listener stays open. Disconnected is only after the grace period.
    /// </summary>
    private void ReportDead(string reason, int generation)
    {
        if (_disposed)
            return;
        if (generation != Volatile.Read(ref _loopGeneration))
            return;
        if (Interlocked.Exchange(ref _deadReported, 1) != 0)
            return;
        try
        {
            if (_server != null)
                _server.DropClient();
            else
                _client?.Disconnect();
        }
        catch { /* ignore */ }
        var text = string.IsNullOrWhiteSpace(reason) ? "Connection lost." : reason;
        Post(() => TransportLost?.Invoke(text));
    }

    private void HandleMessage(NetMessage msg, int generation)
    {
        if (string.Equals(msg.Type, NetMessage.Types.Session, StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGuest || string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                using var doc = JsonDocument.Parse(msg.PayloadJson);
                if (!doc.RootElement.TryGetProperty("token", out var tok)) return;
                var token = tok.GetString();
                if (string.IsNullOrWhiteSpace(token)) return;
                Post(() => SessionTokenReceived?.Invoke(token));
            }
            catch (Exception ex)
            {
                Post(() => ErrorReceived?.Invoke("Bad session payload: " + ex.Message));
            }
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.Action, StringComparison.OrdinalIgnoreCase))
        {
            if (!IsHost) return;
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var dto = NetActionDto.FromJson(msg.PayloadJson);
                Post(() => ActionReceived?.Invoke(dto));
            }
            catch (Exception ex)
            {
                Post(() => ErrorReceived?.Invoke("Bad action payload: " + ex.Message));
            }
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.State, StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGuest) return;
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var save = JsonSerializer.Deserialize<GameSave>(
                    msg.PayloadJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (save == null)
                {
                    Post(() => ErrorReceived?.Invoke("State deserialize returned null."));
                    return;
                }
                Post(() => StateReceived?.Invoke(save));
            }
            catch (Exception ex)
            {
                Post(() => ErrorReceived?.Invoke("Bad state payload: " + ex.Message));
            }
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.Error, StringComparison.OrdinalIgnoreCase))
        {
            string text = msg.PayloadJson ?? "error";
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("message", out var m))
                    text = m.GetString() ?? text;
            }
            catch { /* raw payload */ }
            Post(() => ErrorReceived?.Invoke(text));
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.ChoiceRequest, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var dto = NetChoiceDto.FromJson(msg.PayloadJson);
                if (string.IsNullOrWhiteSpace(dto.CorrelationId) && !string.IsNullOrWhiteSpace(msg.CorrelationId))
                    dto.CorrelationId = msg.CorrelationId;
                Post(() => ChoiceRequestReceived?.Invoke(dto));
            }
            catch (Exception ex)
            {
                Post(() => ErrorReceived?.Invoke("Bad ChoiceRequest payload: " + ex.Message));
            }
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.ChoiceResponse, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var dto = NetChoiceDto.FromJson(msg.PayloadJson);
                if (string.IsNullOrWhiteSpace(dto.CorrelationId) && !string.IsNullOrWhiteSpace(msg.CorrelationId))
                    dto.CorrelationId = msg.CorrelationId;
                Post(() => ChoiceResponseReceived?.Invoke(dto));
            }
            catch (Exception ex)
            {
                Post(() => ErrorReceived?.Invoke("Bad ChoiceResponse payload: " + ex.Message));
            }
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.PlayReveal, StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGuest) return;
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var dto = NetPlayRevealDto.FromJson(msg.PayloadJson);
                Post(() => PlayRevealReceived?.Invoke(dto));
            }
            catch (Exception ex)
            {
                Post(() => ErrorReceived?.Invoke("Bad PlayReveal payload: " + ex.Message));
            }
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.Ping, StringComparison.OrdinalIgnoreCase))
        {
            _ = ReplyPongAsync(generation);
            return;
        }

        // Pong is inbound traffic only. It does not change game state.
        if (string.Equals(msg.Type, NetMessage.Types.Pong, StringComparison.OrdinalIgnoreCase))
            return;
    }

    private async Task ReplyPongAsync(int generation)
    {
        try
        {
            await SendRawAsync(NetMessage.Create(NetMessage.Types.Pong, seq: NextSeq())).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_disposed)
                return;
            if (generation != Volatile.Read(ref _loopGeneration))
                return;
            ReportDead(ex.Message, generation);
        }
    }

    private Task SendRawAsync(NetMessage message, CancellationToken cancellationToken = default)
    {
        if (_server != null)
            return _server.SendAsync(message, cancellationToken);
        if (_client != null)
            return _client.SendAsync(message, cancellationToken);
        return Task.CompletedTask;
    }

    private long NextSeq() => Interlocked.Increment(ref _seq);

    private void Post(Action action)
    {
        if (_sync != null)
            _sync.Post(_ => action(), null);
        else
            action();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _cts.Cancel(); } catch { /* ignore */ }
        try { _cts.Dispose(); } catch { /* ignore */ }
        try { _server?.Dispose(); } catch { /* ignore */ }
        try { _client?.Dispose(); } catch { /* ignore */ }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(NetPlaySession));
    }
}
