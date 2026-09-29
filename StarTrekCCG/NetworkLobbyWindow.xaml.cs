using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
    private MatchmakingClient? _mm;
    private string? _matchPublicAddress;
    private int _matchPublicPort;
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

    // Skip seed phase consensus (after both Ready, before StartGame)
    private bool _skipSeedConsensusActive;
    private bool? _skipSeedLocalAccept; // null = not voted, true = propose/accept, false = decline
    private bool? _skipSeedPeerAccept;
    private int _skipSeedProposedBy; // 0 = none, 1/2 = player
    private DispatcherTimer? _skipSeedTimeout;
    private const int SkipSeedTimeoutSeconds = 45;

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
        if (string.IsNullOrWhiteSpace(MmNameBox.Text))
            MmNameBox.Text = Environment.UserName;
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

        var keepPath = _localDeckPath;
        var keepJson = _localDeckJson;
        var keepName = _localDeckName;
        DisconnectInternal(null);
        _localDeckPath = keepPath;
        _localDeckJson = keepJson;
        _localDeckName = keepName;
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
            if (loopbackOnly)
                SetNatStatus("Localhost only. No router port forward.");
            else
                await AnnouncePortForwardAsync(port, ct).ConfigureAwait(true);
            await PublishMatchmakingAddressAsync(port).ConfigureAwait(true);

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

        var keepPath = _localDeckPath;
        var keepJson = _localDeckJson;
        var keepName = _localDeckName;
        DisconnectInternal(null);
        _localDeckPath = keepPath;
        _localDeckJson = keepJson;
        _localDeckName = keepName;
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
        if (!string.IsNullOrWhiteSpace(_localDeckJson))
            _ = SendExistingDeckPickAsync();
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

                // Both ready → Skip seed phase consensus (Host starts only after accept/decline/timeout)
                if (_localReady && _peerReady)
                    BeginSkipSeedConsensus();
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

        if (string.Equals(msg.Type, NetMessage.Types.LobbySkipSeed, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
            try
            {
                var vote = NetLobbyDto.FromJson<NetLobbyDto.SkipSeedVote>(msg.PayloadJson);
                ApplyPeerSkipSeedVote(vote);
            }
            catch (Exception ex)
            {
                SetStatus("Bad LobbySkipSeed: " + ex.Message);
            }
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

    private async Task TryHostSendStartGameAsync(bool skipSeedPhase)
    {
        if (!IsHost || !_localReady || !_peerReady) return;
        if (string.IsNullOrWhiteSpace(_localDeckJson) || string.IsNullOrWhiteSpace(_peerDeckJson))
        {
            SetStatus("Both ready but deck JSON missing — re-select decks.");
            return;
        }

        StopSkipSeedTimeout();
        _skipSeedConsensusActive = false;

        // Host is always P1: local deck is P1, peer is P2
        var start = new NetLobbyDto.StartGame
        {
            DeckP1Name = _localDeckName ?? "P1",
            DeckP2Name = _peerDeckName ?? "P2",
            DeckP1Json = _localDeckJson!,
            DeckP2Json = _peerDeckJson!,
            SkipSeedPhase = skipSeedPhase
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
            DeckP2Json = start.DeckP2Json,
            SkipSeedPhase = start.SkipSeedPhase
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
            await SendDeckHashIfAnyAsync().ConfigureAwait(true);

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

            if (_peerReady)
                BeginSkipSeedConsensus();
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
        if (MmDeckCombo != null)
            MmDeckCombo.ItemsSource = items;
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


    private void BeginSkipSeedConsensus()
    {
        if (_gameStarting) return;
        if (!_localReady || !_peerReady) return;
        if (_skipSeedConsensusActive) return;

        _skipSeedConsensusActive = true;
        _skipSeedLocalAccept = null;
        _skipSeedPeerAccept = null;
        _skipSeedProposedBy = 0;
        UpdateSkipSeedUi();
        StartSkipSeedTimeout();
        SetStatus("Skip seed phase? Propose skip or choose Manual seed.");
    }

    private void UpdateSkipSeedUi()
    {
        void Apply()
        {
            if (SkipSeedPanel == null) return;
            SkipSeedPanel.Visibility = _skipSeedConsensusActive && !_gameStarting
                ? Visibility.Visible
                : Visibility.Collapsed;

            bool proposed = _skipSeedProposedBy != 0;
            bool iProposed = _skipSeedProposedBy == LocalPlayerNumber;
            bool localVoted = _skipSeedLocalAccept.HasValue;

            if (BtnSkipSeedPropose != null)
            {
                BtnSkipSeedPropose.Visibility = proposed ? Visibility.Collapsed : Visibility.Visible;
                BtnSkipSeedPropose.IsEnabled = !localVoted;
            }
            if (BtnSkipSeedAccept != null)
            {
                BtnSkipSeedAccept.Visibility = proposed && !iProposed ? Visibility.Visible : Visibility.Collapsed;
                BtnSkipSeedAccept.IsEnabled = proposed && !localVoted && _skipSeedLocalAccept != true;
            }
            if (BtnSkipSeedDecline != null)
                BtnSkipSeedDecline.IsEnabled = !localVoted || _skipSeedLocalAccept != false;

            string status;
            if (!_skipSeedConsensusActive)
                status = "";
            else if (_skipSeedLocalAccept == false || _skipSeedPeerAccept == false)
                status = "Manual seed — starting…";
            else if (_skipSeedLocalAccept == true && _skipSeedPeerAccept == true)
                status = "Both accepted — skipping seed phase…";
            else if (proposed)
            {
                string who = _skipSeedProposedBy == LocalPlayerNumber ? "You" : $"P{_skipSeedProposedBy}";
                string peer = _skipSeedPeerAccept == true ? "accepted" : "waiting";
                string self = _skipSeedLocalAccept == true ? "accepted" : "decide";
                status = $"{who} proposed Skip seed phase. You: {self}. Peer: {peer}.";
            }
            else
                status = "Both ready — propose Skip seed phase or Manual seed.";

            if (SkipSeedStatusText != null)
                SkipSeedStatusText.Text = status;
        }

        if (Dispatcher.CheckAccess()) Apply();
        else Dispatcher.Invoke(Apply);
    }

    private async void BtnSkipSeedPropose_Click(object sender, RoutedEventArgs e)
    {
        if (!_skipSeedConsensusActive || _gameStarting) return;
        if (_skipSeedProposedBy != 0) return;
        _skipSeedProposedBy = LocalPlayerNumber;
        _skipSeedLocalAccept = true;
        UpdateSkipSeedUi();
        SetStatus("You proposed Skip seed phase — waiting for opponent…");
        try
        {
            var vote = new NetLobbyDto.SkipSeedVote
            {
                Player = LocalPlayerNumber,
                Action = NetLobbyDto.SkipSeedVote.Actions.Propose
            };
            await SendLobbyAsync(NetMessage.Create(NetMessage.Types.LobbySkipSeed, NetLobbyDto.ToJson(vote)))
                .ConfigureAwait(true);
            TryResolveSkipSeedConsensus();
        }
        catch (Exception ex)
        {
            SetStatus("Skip seed propose failed: " + ex.Message);
        }
    }

    private async void BtnSkipSeedAccept_Click(object sender, RoutedEventArgs e)
    {
        if (!_skipSeedConsensusActive || _gameStarting) return;
        if (_skipSeedProposedBy == 0) return;
        _skipSeedLocalAccept = true;
        UpdateSkipSeedUi();
        try
        {
            var vote = new NetLobbyDto.SkipSeedVote
            {
                Player = LocalPlayerNumber,
                Action = NetLobbyDto.SkipSeedVote.Actions.Accept
            };
            await SendLobbyAsync(NetMessage.Create(NetMessage.Types.LobbySkipSeed, NetLobbyDto.ToJson(vote)))
                .ConfigureAwait(true);
            TryResolveSkipSeedConsensus();
        }
        catch (Exception ex)
        {
            SetStatus("Skip seed accept failed: " + ex.Message);
        }
    }

    private async void BtnSkipSeedDecline_Click(object sender, RoutedEventArgs e)
    {
        if (!_skipSeedConsensusActive || _gameStarting) return;
        _skipSeedLocalAccept = false;
        UpdateSkipSeedUi();
        SetStatus("Manual seed selected.");
        try
        {
            var vote = new NetLobbyDto.SkipSeedVote
            {
                Player = LocalPlayerNumber,
                Action = NetLobbyDto.SkipSeedVote.Actions.Decline
            };
            await SendLobbyAsync(NetMessage.Create(NetMessage.Types.LobbySkipSeed, NetLobbyDto.ToJson(vote)))
                .ConfigureAwait(true);
            TryResolveSkipSeedConsensus();
        }
        catch (Exception ex)
        {
            SetStatus("Skip seed decline failed: " + ex.Message);
        }
    }

    private void ApplyPeerSkipSeedVote(NetLobbyDto.SkipSeedVote vote)
    {
        if (_gameStarting) return;
        if (!_skipSeedConsensusActive)
            BeginSkipSeedConsensus();

        string action = vote.Action ?? "";
        if (string.Equals(action, NetLobbyDto.SkipSeedVote.Actions.Propose, StringComparison.OrdinalIgnoreCase))
        {
            if (_skipSeedProposedBy == 0)
                _skipSeedProposedBy = vote.Player;
            _skipSeedPeerAccept = true;
        }
        else if (string.Equals(action, NetLobbyDto.SkipSeedVote.Actions.Accept, StringComparison.OrdinalIgnoreCase))
        {
            _skipSeedPeerAccept = true;
            if (_skipSeedProposedBy == 0)
                _skipSeedProposedBy = vote.Player;
        }
        else if (string.Equals(action, NetLobbyDto.SkipSeedVote.Actions.Decline, StringComparison.OrdinalIgnoreCase))
        {
            _skipSeedPeerAccept = false;
        }

        UpdateSkipSeedUi();
        TryResolveSkipSeedConsensus();
    }

    private void TryResolveSkipSeedConsensus()
    {
        if (!_skipSeedConsensusActive || _gameStarting) return;
        if (!IsHost) return; // Host alone decides and broadcasts StartGame

        // Decline / any false → manual seed
        if (_skipSeedLocalAccept == false || _skipSeedPeerAccept == false)
        {
            SetStatus("Skip declined — starting with manual seed…");
            _ = TryHostSendStartGameAsync(skipSeedPhase: false);
            return;
        }

        // Both accepted (propose counts as accept for proposer)
        if (_skipSeedLocalAccept == true && _skipSeedPeerAccept == true && _skipSeedProposedBy != 0)
        {
            SetStatus("Both accepted — Skip seed phase…");
            _ = TryHostSendStartGameAsync(skipSeedPhase: true);
        }
    }

    private void StartSkipSeedTimeout()
    {
        StopSkipSeedTimeout();
        _skipSeedTimeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(SkipSeedTimeoutSeconds) };
        _skipSeedTimeout.Tick += (_, _) =>
        {
            StopSkipSeedTimeout();
            if (_gameStarting || !_skipSeedConsensusActive) return;
            SetStatus("Skip seed timeout — starting with manual seed.");
            // Prefer decline (manual) over unclear skip
            if (_skipSeedLocalAccept == null)
                _skipSeedLocalAccept = false;
            if (_skipSeedPeerAccept == null)
                _skipSeedPeerAccept = false;
            UpdateSkipSeedUi();
            if (IsHost)
                _ = TryHostSendStartGameAsync(skipSeedPhase: false);
        };
        _skipSeedTimeout.Start();
    }

    private void StopSkipSeedTimeout()
    {
        if (_skipSeedTimeout == null) return;
        try { _skipSeedTimeout.Stop(); } catch { /* ignore */ }
        _skipSeedTimeout = null;
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
        _matchPublicAddress = null;
        _matchPublicPort = 0;
        SetNatStatus("");
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
        StopSkipSeedTimeout();
        _skipSeedConsensusActive = false;
        _skipSeedLocalAccept = null;
        _skipSeedPeerAccept = null;
        _skipSeedProposedBy = 0;

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
            if (SkipSeedPanel != null)
                SkipSeedPanel.Visibility = Visibility.Collapsed;
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

    private async Task AnnouncePortForwardAsync(int port, CancellationToken ct)
    {
        try
        {
            var lease = await NatPortForward.TryOpenAsync(port, ct).ConfigureAwait(true);
            if (ct.IsCancellationRequested)
            {
                lease.Dispose();
                return;
            }
            if (lease.Mapped && _server != null)
            {
                _server.HoldNatLease(lease);
                _matchPublicAddress = string.IsNullOrWhiteSpace(lease.ExternalAddress) ? null : lease.ExternalAddress.Trim();
                _matchPublicPort = lease.ExternalPort;
                var address = string.IsNullOrWhiteSpace(lease.ExternalAddress)
                    ? "address unknown"
                    : lease.ExternalAddress;
                SetNatStatus($"Public {address}:{lease.ExternalPort} ({lease.Protocol}).");
            }
            else
            {
                _matchPublicAddress = null;
                _matchPublicPort = 0;
                lease.Dispose();
                SetNatStatus(
                    $"Port forward failed ({lease.Failure}). Open TCP {port} on the router manually. Host is still listening.");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            SetNatStatus(
                $"Port forward failed ({ex.Message}). Open TCP {port} on the router manually. Host is still listening.");
        }
    }

    private void SetNatStatus(string text)
    {
        void Apply()
        {
            if (NatStatusText == null) return;
            NatStatusText.Text = text ?? "";
            NatStatusText.Margin = string.IsNullOrEmpty(text) ? new Thickness(0) : new Thickness(0, 6, 0, 0);
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
        try { _mm?.Dispose(); } catch { /* ignore */ }
        _mm = null;
        // If session took the transport, do not dispose the live socket.
        if (TransportDetached)
            return;
        DisconnectInternal(null);
    }

    private async void BtnMmConnect_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseLobbyService(MmServiceBox.Text, out var host, out var port))
        {
            SetMmState("Service must be host:port or ws://host:port/lobby.");
            return;
        }

        var name = string.IsNullOrWhiteSpace(MmNameBox.Text) ? "Player" : MmNameBox.Text.Trim();
        var mm = new MatchmakingClient();
        mm.StateChanged += text => Dispatcher.BeginInvoke(() => SetMmState(text));
        mm.PlayersChanged += text => Dispatcher.BeginInvoke(() => MmPlayersText.Text = text ?? "");
        mm.ChatChanged += text => Dispatcher.BeginInvoke(() => MmChatLog.Text = text ?? "");
        mm.AddressAnnounced += (announcedHost, announcedPort) =>
            Dispatcher.BeginInvoke(() => SetMmState($"Host published {announcedHost}:{announcedPort}. Open direct game when ready."));
        try
        {
            await mm.ConnectAsync(host, port, name).ConfigureAwait(true);
            _mm?.Dispose();
            _mm = mm;
        }
        catch (Exception ex)
        {
            mm.Dispose();
            SetMmState("Matchmaking connect failed: " + ex.Message);
        }
    }

    private async void BtnMmCreate_Click(object sender, RoutedEventArgs e)
        => await RoomCommandAsync(join: false).ConfigureAwait(true);

    private async void BtnMmJoin_Click(object sender, RoutedEventArgs e)
        => await RoomCommandAsync(join: true).ConfigureAwait(true);

    private async Task RoomCommandAsync(bool join)
    {
        if (_mm == null)
        {
            SetMmState("Connect to the service first.");
            return;
        }
        if (!join && !TryParsePort(out _))
            return;
        try
        {
            if (join)
                await _mm.JoinRoomAsync((MmRoomBox.Text ?? "").Trim()).ConfigureAwait(true);
            else
                await _mm.CreateRoomAsync((MmRoomBox.Text ?? "").Trim(), int.Parse((PortBox.Text ?? "").Trim(), System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(true);
            await SendDeckHashIfAnyAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetMmState(ex.Message);
        }
    }

    private async void BtnMmRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (_mm == null)
        {
            SetMmState("Connect to the service first.");
            return;
        }
        try { await _mm.RefreshAsync().ConfigureAwait(true); }
        catch (Exception ex) { SetMmState(ex.Message); }
    }

    private async void BtnMmChat_Click(object sender, RoutedEventArgs e)
    {
        if (_mm == null)
        {
            SetMmState("Connect to the service first.");
            return;
        }
        var text = (MmChatBox.Text ?? "").Trim();
        if (text.Length == 0)
            return;
        try
        {
            await _mm.SendChatAsync(text).ConfigureAwait(true);
            MmChatBox.Text = "";
        }
        catch (Exception ex)
        {
            SetMmState(ex.Message);
        }
    }

    private async void MmDeckCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MmDeckCombo.SelectedItem is not DeckListItem item) return;
        await ApplyLocalDeckAsync(item.Path, Path.GetFileNameWithoutExtension(item.Path)).ConfigureAwait(true);
    }

    private async void BtnMmPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_mm is not { InRoom: true })
        {
            SetMmState("Create or join a room first. Host, Join, and Localhost above still work.");
            return;
        }

        if (string.Equals(_mm.Role, "host", StringComparison.Ordinal))
        {
            await RunHostAsync(loopbackOnly: false).ConfigureAwait(true);
            return;
        }

        if (string.IsNullOrWhiteSpace(_mm.DirectHost) || _mm.DirectPort is < 1 or > 65535)
        {
            SetMmState("The host has not published an address yet.");
            return;
        }

        HostBox.Text = _mm.DirectHost;
        PortBox.Text = _mm.DirectPort.ToString();
        await RunJoinAsync().ConfigureAwait(true);
    }

    private async Task PublishMatchmakingAddressAsync(int listenPort)
    {
        if (_mm is not { InRoom: true } || !string.Equals(_mm.Role, "host", StringComparison.Ordinal))
            return;
        try
        {
            string host;
            int port;
            if (IPAddress.TryParse(_matchPublicAddress, out var published)
                && published.AddressFamily == AddressFamily.InterNetwork
                && !published.Equals(IPAddress.Any)
                && _matchPublicPort is > 0 and <= 65535)
            {
                host = published.ToString();
                port = _matchPublicPort;
            }
            else
            {
                host = FirstLanIPv4() ?? "127.0.0.1";
                port = listenPort;
            }

            await _mm.SendAddressAsync(host, port).ConfigureAwait(true);
            SetMmState($"Published {host}:{port} to the room. Relay is not built.");
        }
        catch (Exception ex)
        {
            SetMmState("Could not publish the address (" + ex.Message + "). The host is still listening.");
        }
    }

    private async Task SendDeckHashIfAnyAsync()
    {
        if (_mm is not { InRoom: true } || string.IsNullOrWhiteSpace(_localDeckJson) || string.IsNullOrWhiteSpace(_localDeckName))
            return;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_localDeckJson))).ToLowerInvariant();
        await _mm.SendDeckAsync(_localDeckName, hash).ConfigureAwait(true);
    }

    private async Task SendExistingDeckPickAsync()
    {
        if (!IsConnected || string.IsNullOrWhiteSpace(_localDeckName))
            return;
        try
        {
            var pick = new NetLobbyDto.DeckPick
            {
                Player = LocalPlayerNumber,
                DeckName = _localDeckName
            };
            await SendLobbyAsync(NetMessage.Create(NetMessage.Types.LobbyDeck, NetLobbyDto.ToJson(pick)))
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus("Deck announce failed: " + ex.Message);
        }
    }

    private void SetMmState(string text)
    {
        void Apply()
        {
            if (MmStateText != null)
                MmStateText.Text = text ?? "";
        }
        if (Dispatcher.CheckAccess()) Apply();
        else Dispatcher.Invoke(Apply);
    }

    private static string? FirstLanIPv4()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;
            foreach (var address in ni.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                if (IPAddress.IsLoopback(address.Address) || address.Address.Equals(IPAddress.Any))
                    continue;
                return address.Address.ToString();
            }
        }
        return null;
    }

    private static bool TryParseLobbyService(string? text, out string host, out int port)
    {
        host = "";
        port = 0;
        text = (text ?? "").Trim();
        if (text.Length == 0)
            return false;
        if (text.Contains("://", StringComparison.Ordinal))
        {
            if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
                return false;
            if (!string.Equals(uri.Scheme, "ws", StringComparison.OrdinalIgnoreCase))
                return false;
            if (uri.AbsolutePath is not "/lobby" and not "/lobby/")
                return false;
            if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
                return false;
            host = uri.Host;
            port = uri.IsDefaultPort ? 7788 : uri.Port;
        }
        else
        {
            var colon = text.LastIndexOf(':');
            if (colon <= 0 || colon == text.Length - 1)
                return false;
            host = text[..colon].Trim();
            if (!int.TryParse(text[(colon + 1)..].Trim(), out port))
                return false;
        }

        if (port is < 1 or > 65535 || host.Length == 0 || host.IndexOfAny(new[] { '/', ' ', '\\' }) >= 0)
            return false;
        return true;
    }
}