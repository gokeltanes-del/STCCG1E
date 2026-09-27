using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using StarTrekCCG.Network;

namespace StarTrekCCG;

// Verb: network lobby host join deck ready
/// <summary>
/// Lobby room: Host/Join then deck pick + Ready handshake.
/// netztauglich: StartGame over NetMessage JSON; Host authoritative when both ready.
/// DetachTransport for NetPlaySession after GameStarting — no UI-only start rule.
/// </summary>
public partial class NetworkLobbyWindow : Window
{
    private NetServer? _server;
    private NetClient? _client;
    private CancellationTokenSource? _cts;
    private Task? _lobbyReceiveTask;
    private bool _busy;
    private bool _gameStarting;

    private string? _localDeckPath;
    private string? _localDeckJson;
    private string? _localDeckName;
    private bool _localReady;

    private string? _peerDeckName;
    private string? _peerDeckJson;
    private bool _peerReady;

    private sealed class DeckListItem
    {
        public string DisplayName { get; init; } = "";
        public string Path { get; init; } = "";
        public override string ToString() => DisplayName;
    }

    public bool IsConnected { get; private set; }

    /// <summary>True when this window hosted the listen/accept side.</summary>
    public bool IsHost => _server is not null || _detachedWasHost;

    public NetServer? Server => _server;
    public NetClient? Client => _client;

    /// <summary>True after DetachTransport - Closing must not dispose the live socket.</summary>
    public bool TransportDetached { get; private set; }

    private bool _detachedWasHost;

    public event EventHandler<bool>? ConnectionChanged;

    /// <summary>Fired when StartGame is agreed (Host sent / Guest received). TableWindow starts session.</summary>
    public event EventHandler<LobbyGameStartArgs>? GameStarting;

    public NetworkLobbyWindow()
    {
        InitializeComponent();
        RefreshDeckList();
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
            SetStatus($"Listening on port {port} ({mode}) - waiting for guest.");

            await _server.AcceptClientAsync(ct).ConfigureAwait(true);
            SetStatus("Guest connected - handshake.");

            var hello = NetMessage.Create(
                NetMessage.Types.Handshake,
                payloadJson: JsonSerializer.Serialize(new { role = "host", player = 1 }));
            await _server.SendAsync(hello, ct).ConfigureAwait(true);

            var reply = await _server.ReceiveAsync(ct).ConfigureAwait(true);
            if (!string.Equals(reply.Type, NetMessage.Types.Handshake, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Expected handshake, got '{reply.Type}'.");

            IsConnected = true;
            SetStatus("Connected as Host (P1) — pick deck, then Start game when both ready.");
            ConnectionChanged?.Invoke(this, true);
            SetBusyUi(connected: true);
            EnterLobbyRoom();
            StartLobbyReceiveLoop();
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
            SetStatus($"Connecting to {host}:{port}.");
            await _client.ConnectAsync(host, port, ct).ConfigureAwait(true);
            SetStatus("Connected - handshake.");

            var hello = NetMessage.Create(
                NetMessage.Types.Handshake,
                payloadJson: JsonSerializer.Serialize(new { role = "guest", player = 2 }));
            await _client.SendAsync(hello, ct).ConfigureAwait(true);

            var reply = await _client.ReceiveAsync(ct).ConfigureAwait(true);
            if (!string.Equals(reply.Type, NetMessage.Types.Handshake, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Expected handshake, got '{reply.Type}'.");

            IsConnected = true;
            SetStatus("Connected as Guest (P2) — pick deck, then Start game when both ready.");
            ConnectionChanged?.Invoke(this, true);
            SetBusyUi(connected: true);
            EnterLobbyRoom();
            StartLobbyReceiveLoop();
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

    private void EnterLobbyRoom()
    {
        void Apply()
        {
            LobbyPanel.Visibility = Visibility.Visible;
            RefreshDeckList();
            UpdateLobbyUi();
            BtnStartGame.IsEnabled = !string.IsNullOrWhiteSpace(_localDeckJson);
        }

        if (Dispatcher.CheckAccess()) Apply();
        else Dispatcher.Invoke(Apply);
    }

    private void StartLobbyReceiveLoop()
    {
        if (_cts == null) return;
        var ct = _cts.Token;
        _lobbyReceiveTask = Task.Run(() => LobbyReceiveLoopAsync(ct), ct);
    }

    private async Task LobbyReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && !_gameStarting)
            {
                NetMessage msg;
                if (_server != null)
                    msg = await _server.ReceiveAsync(ct).ConfigureAwait(false);
                else if (_client != null)
                    msg = await _client.ReceiveAsync(ct).ConfigureAwait(false);
                else
                    break;

                await Dispatcher.InvokeAsync(() => HandleLobbyMessage(msg));
            }
        }
        catch (OperationCanceledException)
        {
            // normal: DetachTransport or Disconnect
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (!_gameStarting && IsConnected)
                    DisconnectInternal("Lobby receive error: " + ex.Message);
            });
        }
    }

    private void HandleLobbyMessage(NetMessage msg)
    {
        if (_gameStarting) return;

        if (string.Equals(msg.Type, NetMessage.Types.LobbyDeck, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var pick = NetLobbyDto.FromJson<NetLobbyDto.DeckPick>(msg.PayloadJson);
                _peerDeckName = pick.DeckName;
                UpdateLobbyUi();
            }
            catch (Exception ex)
            {
                SetStatus("Bad LobbyDeck: " + ex.Message);
            }
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.LobbyReady, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var ready = NetLobbyDto.FromJson<NetLobbyDto.Ready>(msg.PayloadJson);
                _peerReady = ready.IsReady;
                if (!string.IsNullOrWhiteSpace(ready.DeckName))
                    _peerDeckName = ready.DeckName;
                if (ready.IsReady && !string.IsNullOrWhiteSpace(ready.DeckJson))
                    _peerDeckJson = ready.DeckJson;
                UpdateLobbyUi();

                // Host authoritative: start only when both ready
                if (IsHost && _localReady && _peerReady)
                    _ = TryHostSendStartGameAsync();
            }
            catch (Exception ex)
            {
                SetStatus("Bad LobbyReady: " + ex.Message);
            }
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.LobbyStatus, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var st = NetLobbyDto.FromJson<NetLobbyDto.Status>(msg.PayloadJson);
                if (!string.IsNullOrWhiteSpace(st.DeckName))
                    _peerDeckName = st.DeckName;
                _peerReady = st.Ready;
                UpdateLobbyUi();
            }
            catch { /* ignore bad status */ }
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.StartGame, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var start = NetLobbyDto.FromJson<NetLobbyDto.StartGame>(msg.PayloadJson);
                RaiseGameStarting(start);
            }
            catch (Exception ex)
            {
                SetStatus("Bad StartGame: " + ex.Message);
            }
            return;
        }
    }

    private async Task TryHostSendStartGameAsync()
    {
        if (!IsHost || !_localReady || !_peerReady) return;
        if (string.IsNullOrWhiteSpace(_localDeckJson) || string.IsNullOrWhiteSpace(_peerDeckJson))
        {
            SetStatus("Both ready but deck JSON missing — re-select decks.");
            return;
        }

        var start = new NetLobbyDto.StartGame
        {
            DeckP1Name = _localDeckName ?? "P1",
            DeckP2Name = _peerDeckName ?? "P2",
            DeckP1Json = _localDeckJson!,
            DeckP2Json = _peerDeckJson!
        };

        try
        {
            var msg = NetMessage.Create(NetMessage.Types.StartGame, payloadJson: NetLobbyDto.ToJson(start));
            await SendLobbyAsync(msg).ConfigureAwait(true);
            RaiseGameStarting(start);
        }
        catch (Exception ex)
        {
            SetStatus("StartGame send failed: " + ex.Message);
        }
    }

    private void RaiseGameStarting(NetLobbyDto.StartGame start)
    {
        if (_gameStarting) return;
        _gameStarting = true;
        SetStatus("Starting game…");
        GameStarting?.Invoke(this, new LobbyGameStartArgs
        {
            IsHost = IsHost,
            DeckP1Name = start.DeckP1Name,
            DeckP2Name = start.DeckP2Name,
            DeckP1Json = start.DeckP1Json,
            DeckP2Json = start.DeckP2Json
        });
    }

    private async Task SendLobbyAsync(NetMessage message)
    {
        var ct = _cts?.Token ?? CancellationToken.None;
        if (_server != null)
            await _server.SendAsync(message, ct).ConfigureAwait(true);
        else if (_client != null)
            await _client.SendAsync(message, ct).ConfigureAwait(true);
    }

    private int LocalPlayerNumber => IsHost ? 1 : 2;

    private async void DeckCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeckCombo.SelectedItem is not DeckListItem item) return;
        await ApplyLocalDeckAsync(item.Path, Path.GetFileNameWithoutExtension(item.Path)).ConfigureAwait(true);
    }

    private async void BtnBrowseDeck_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select deck",
            Filter = "STCCG Deck (*.stdeck)|*.stdeck",
            InitialDirectory = Directory.Exists(GamePaths.DecksRoot) ? GamePaths.DecksRoot : Environment.CurrentDirectory
        };
        if (dlg.ShowDialog() != true) return;
        await ApplyLocalDeckAsync(dlg.FileName, Path.GetFileNameWithoutExtension(dlg.FileName)).ConfigureAwait(true);
    }

    private async Task ApplyLocalDeckAsync(string path, string displayName)
    {
        try
        {
            if (!File.Exists(path))
            {
                SetStatus("Deck file not found: " + path);
                return;
            }

            var json = await File.ReadAllTextAsync(path).ConfigureAwait(true);
            // Validate via DeckService
            var svc = new Services.DeckService();
            var deck = svc.LoadFromJson(json);
            _localDeckPath = path;
            _localDeckJson = json;
            _localDeckName = string.IsNullOrWhiteSpace(deck.Name) ? displayName : deck.Name;
            _localReady = false;

            BtnStartGame.IsEnabled = true;
            UpdateLobbyUi();
            SetStatus($"Deck selected: {_localDeckName}");

            if (IsConnected)
            {
                var pick = new NetLobbyDto.DeckPick
                {
                    Player = LocalPlayerNumber,
                    DeckName = _localDeckName ?? displayName
                };
                await SendLobbyAsync(NetMessage.Create(NetMessage.Types.LobbyDeck, NetLobbyDto.ToJson(pick)))
                    .ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            _localDeckPath = null;
            _localDeckJson = null;
            _localDeckName = null;
            _localReady = false;
            BtnStartGame.IsEnabled = false;
            SetStatus("Deck load failed: " + ex.Message);
            UpdateLobbyUi();
        }
    }

    private async void BtnStartGame_Click(object sender, RoutedEventArgs e)
    {
        if (!IsConnected)
        {
            SetStatus("Not connected.");
            return;
        }
        if (string.IsNullOrWhiteSpace(_localDeckJson))
        {
            SetStatus("Select a deck first.");
            return;
        }

        _localReady = true;
        UpdateLobbyUi();
        SetStatus(_peerReady
            ? "You are ready — starting when Host confirms…"
            : "You are ready — waiting for opponent…");

        try
        {
            var ready = new NetLobbyDto.Ready
            {
                Player = LocalPlayerNumber,
                IsReady = true,
                DeckName = _localDeckName ?? "Deck",
                DeckJson = _localDeckJson
            };
            await SendLobbyAsync(NetMessage.Create(NetMessage.Types.LobbyReady, NetLobbyDto.ToJson(ready)))
                .ConfigureAwait(true);

            if (IsHost && _peerReady)
                await TryHostSendStartGameAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _localReady = false;
            UpdateLobbyUi();
            SetStatus("Ready send failed: " + ex.Message);
        }
    }

    private void RefreshDeckList()
    {
        var items = new List<DeckListItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddDir(string? dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;
            foreach (var file in Directory.EnumerateFiles(dir, "*.stdeck", SearchOption.TopDirectoryOnly)
                         .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
            {
                if (!seen.Add(file)) continue;
                items.Add(new DeckListItem
                {
                    Path = file,
                    DisplayName = Path.GetFileNameWithoutExtension(file)
                });
            }
        }

        AddDir(GamePaths.DecksRoot);

        // App / project Decks folder (beside Data or under output)
        try
        {
            var appDecks = Path.Combine(AppContext.BaseDirectory, "Decks");
            AddDir(appDecks);
            var walk = AppContext.BaseDirectory;
            for (int i = 0; i < 6 && !string.IsNullOrEmpty(walk); i++)
            {
                AddDir(Path.Combine(walk, "Decks"));
                AddDir(Path.Combine(walk, "StarTrekCCG", "Decks"));
                walk = Directory.GetParent(walk)?.FullName;
            }
        }
        catch { /* ignore */ }

        DeckCombo.ItemsSource = items;
        if (items.Count > 0 && DeckCombo.SelectedIndex < 0)
            DeckCombo.SelectedIndex = -1; // user must pick explicitly
    }

    private void UpdateLobbyUi()
    {
        void Apply()
        {
            var you = _localReady
                ? $"You: ready ({_localDeckName ?? "no deck"})"
                : $"You: not ready ({_localDeckName ?? "no deck"})";
            LobbyReadyText.Text = you;

            var peerLabel = IsHost ? "Guest (P2)" : "Host (P1)";
            var peerDeck = string.IsNullOrWhiteSpace(_peerDeckName) ? "no deck yet" : _peerDeckName;
            var peerReady = _peerReady ? "ready" : "not ready";
            LobbyPeerText.Text = $"{peerLabel}: {peerDeck} — {peerReady}";

            if (_localReady && !_peerReady)
                SetStatus("Waiting for opponent…");
            else if (!_localReady && _peerReady)
                SetStatus("Opponent is ready — select deck and press Start game.");
        }

        if (Dispatcher.CheckAccess()) Apply();
        else Dispatcher.Invoke(Apply);
    }

    private bool TryParsePort(out int port)
    {
        port = 0;
        if (!int.TryParse((PortBox.Text ?? string.Empty).Trim(), out port) || port < 1 || port > 65535)
        {
            SetStatus("Invalid port (1-65535).");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Hand ownership of the live NetServer/NetClient to NetPlaySession.
    /// Cancels lobby receive loop first so only one reader remains on the stream.
    /// </summary>
    public (NetServer? server, NetClient? client) DetachTransport()
    {
        TransportDetached = true;
        _detachedWasHost = _server is not null;
        try { _cts?.Cancel(); } catch { /* ignore */ }
        // Ensure lobby reader stops before NetPlaySession owns the stream.
        try { _lobbyReceiveTask?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        try { _cts?.Dispose(); } catch { /* ignore */ }
        _cts = null;
        _lobbyReceiveTask = null;

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
        _lobbyReceiveTask = null;

        try { _server?.Dispose(); } catch { /* ignore */ }
        _server = null;
        try { _client?.Dispose(); } catch { /* ignore */ }
        _client = null;

        var wasConnected = IsConnected;
        IsConnected = false;
        _busy = false;
        _gameStarting = false;
        _localReady = false;
        _peerReady = false;
        _peerDeckName = null;
        _peerDeckJson = null;

        if (statusMessage != null)
            SetStatus(statusMessage);
        SetBusyUi(false);
        HideLobbyRoom();

        if (wasConnected)
            ConnectionChanged?.Invoke(this, false);
    }

    private void HideLobbyRoom()
    {
        void Apply()
        {
            LobbyPanel.Visibility = Visibility.Collapsed;
            BtnStartGame.IsEnabled = false;
        }
        if (Dispatcher.CheckAccess()) Apply();
        else Dispatcher.Invoke(Apply);
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
        // If session took the transport, do not dispose the live socket.
        if (TransportDetached)
            return;
        DisconnectInternal(null);
    }
}