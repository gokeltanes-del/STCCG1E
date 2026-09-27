using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using StarTrekCCG.Network;

namespace StarTrekCCG;

// Verb: network lobby host join
/// <summary>
/// Phase-2 Lobby: Host / Join / Localhost against NetServer/NetClient.
/// Connection + handshake; DetachTransport for Phase-3 session — no game-state sync.
/// </summary>
public partial class NetworkLobbyWindow : Window
{
    private NetServer? _server;
    private NetClient? _client;
    private CancellationTokenSource? _cts;
    private bool _busy;

    public bool IsConnected { get; private set; }

    /// <summary>True when this window hosted the listen/accept side.</summary>
    public bool IsHost => _server is not null || _detachedWasHost;

    public NetServer? Server => _server;
    public NetClient? Client => _client;

    /// <summary>True after DetachTransport — Closing must not dispose the live socket.</summary>
    public bool TransportDetached { get; private set; }

    private bool _detachedWasHost;

    public event EventHandler<bool>? ConnectionChanged;

    public NetworkLobbyWindow()
    {
        InitializeComponent();
    }

    private async void BtnHost_Click(object sender, RoutedEventArgs e)
        => await RunHostAsync(loopbackOnly: false).ConfigureAwait(true);

    private async void BtnLocalhost_Click(object sender, RoutedEventArgs e)
        => await RunHostAsync(loopbackOnly: true).ConfigureAwait(true);

    private async void BtnJoin_Click(object sender, RoutedEventArgs e)
        => await RunJoinAsync().ConfigureAwait(true);

    private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        => DisconnectInternal("Disconnected");

    private async Task RunHostAsync(bool loopbackOnly)
    {
        if (_busy) return;
        if (!TryParsePort(out var port)) return;

        DisconnectInternal(null);
        _busy = true;
        SetBusyUi(true);
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            _server = new NetServer();
            await _server.StartAsync(port, loopbackOnly, ct).ConfigureAwait(true);
            var mode = loopbackOnly ? "localhost (loopback)" : "any interface";
            SetStatus($"Listening on port {port} ({mode}) — waiting for guest…");

            await _server.AcceptClientAsync(ct).ConfigureAwait(true);
            SetStatus("Guest connected — handshake…");

            var hello = NetMessage.Create(
                NetMessage.Types.Handshake,
                payloadJson: JsonSerializer.Serialize(new { role = "host", player = 1 }));
            await _server.SendAsync(hello, ct).ConfigureAwait(true);

            var reply = await _server.ReceiveAsync(ct).ConfigureAwait(true);
            if (!string.Equals(reply.Type, NetMessage.Types.Handshake, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Expected handshake, got '{reply.Type}'.");

            IsConnected = true;
            SetStatus("Connected as Host (P1)");
            ConnectionChanged?.Invoke(this, true);
            SetBusyUi(connected: true);
        }
        catch (OperationCanceledException)
        {
            DisconnectInternal("Cancelled");
        }
        catch (Exception ex)
        {
            DisconnectInternal($"Host error: {ex.Message}");
        }
        finally
        {
            _busy = false;
            if (!IsConnected)
                SetBusyUi(false);
        }
    }

    private async Task RunJoinAsync()
    {
        if (_busy) return;
        if (!TryParsePort(out var port)) return;
        var host = (HostBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            SetStatus("Host address required.");
            return;
        }

        DisconnectInternal(null);
        _busy = true;
        SetBusyUi(true);
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            _client = new NetClient();
            SetStatus($"Connecting to {host}:{port}…");
            await _client.ConnectAsync(host, port, ct).ConfigureAwait(true);
            SetStatus("Connected — handshake…");

            var hello = NetMessage.Create(
                NetMessage.Types.Handshake,
                payloadJson: JsonSerializer.Serialize(new { role = "guest", player = 2 }));
            await _client.SendAsync(hello, ct).ConfigureAwait(true);

            var reply = await _client.ReceiveAsync(ct).ConfigureAwait(true);
            if (!string.Equals(reply.Type, NetMessage.Types.Handshake, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Expected handshake, got '{reply.Type}'.");

            IsConnected = true;
            SetStatus("Connected as Guest (P2)");
            ConnectionChanged?.Invoke(this, true);
            SetBusyUi(connected: true);
        }
        catch (OperationCanceledException)
        {
            DisconnectInternal("Cancelled");
        }
        catch (Exception ex)
        {
            DisconnectInternal($"Join error: {ex.Message}");
        }
        finally
        {
            _busy = false;
            if (!IsConnected)
                SetBusyUi(false);
        }
    }

    private bool TryParsePort(out int port)
    {
        port = 0;
        if (!int.TryParse((PortBox.Text ?? string.Empty).Trim(), out port) || port < 1 || port > 65535)
        {
            SetStatus("Invalid port (1–65535).");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Hand ownership of the live NetServer/NetClient to NetPlaySession.
    /// Nulls local refs without Dispose so Closing will not kill the connection.
    /// </summary>
    public (NetServer? server, NetClient? client) DetachTransport()
    {
        TransportDetached = true;
        _detachedWasHost = _server is not null;
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _cts?.Dispose(); } catch { /* ignore */ }
        _cts = null;

        var server = _server;
        var client = _client;
        _server = null;
        _client = null;
        return (server, client);
    }

    private void DisconnectInternal(string? statusMessage)
    {
        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _cts?.Dispose(); } catch { /* ignore */ }
        _cts = null;

        try { _server?.Dispose(); } catch { /* ignore */ }
        _server = null;
        try { _client?.Dispose(); } catch { /* ignore */ }
        _client = null;

        var wasConnected = IsConnected;
        IsConnected = false;
        _busy = false;

        if (statusMessage != null)
            SetStatus(statusMessage);
        SetBusyUi(false);

        if (wasConnected)
            ConnectionChanged?.Invoke(this, false);
    }

    private void SetBusyUi(bool busyOrConnected = false, bool connected = false)
    {
        void Apply()
        {
            var active = busyOrConnected || connected;
            BtnHost.IsEnabled = !active;
            BtnJoin.IsEnabled = !active;
            BtnLocalhost.IsEnabled = !active;
            BtnDisconnect.IsEnabled = active;
            PortBox.IsEnabled = !active;
            HostBox.IsEnabled = !active;
        }

        if (Dispatcher.CheckAccess())
            Apply();
        else
            Dispatcher.Invoke(Apply);
    }

    private void SetStatus(string text)
    {
        void Apply() => StatusText.Text = text;
        if (Dispatcher.CheckAccess())
            Apply();
        else
            Dispatcher.Invoke(Apply);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Phase 3: if session took the transport, do not dispose the live socket.
        if (TransportDetached)
            return;
        DisconnectInternal(null);
    }
}
