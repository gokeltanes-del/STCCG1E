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
using System.Windows.Media;
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
    private INetLink? _link;
    private bool _relayHost;
    private readonly LobbySignIn _signIn;
    private MatchmakingClient? _mm;
    private string? _matchPublicAddress;
    private int _matchPublicPort;
    private string _serviceHost = "127.0.0.1";
    private int _servicePort = 7788;
    private bool _suppressDeck;
    private List<DeckListItem> _serverDeckItems = new();
    private CancellationTokenSource? _cts;
    private Task? _lobbyReceiveTask;
    private bool _busy;
    private bool _gameStarting;
    private int _relayReceiveHandedOff;
    private string? _guestResumeMatchId;
    private string? _guestResumeSecret;
    private bool _guestLoadSignaled;

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
        public string ServerName { get; init; } = "";
        public bool FromServer { get; init; }
        public override string ToString() => DisplayName;
    }

    public bool IsConnected { get; private set; }

    /// <summary>True when this window hosted the listen/accept side.</summary>
    public bool IsHost => _server is not null || _relayHost || _detachedWasHost;

    public NetServer? Server => _server;
    public NetClient? Client => _client;

    /// <summary>True after DetachTransport - Closing must not dispose the live socket.</summary>
    public bool TransportDetached { get; private set; }

    private bool _detachedWasHost;

    public event EventHandler<bool>? ConnectionChanged;

    /// <summary>Fired when StartGame is agreed (Host sent / Guest received). TableWindow starts session.</summary>
    public event EventHandler<LobbyGameStartArgs>? GameStarting;

    public NetworkLobbyWindow()
        : this(LobbySignIn.Sandbox())
    {
    }

    public NetworkLobbyWindow(LobbySignIn signIn)
    {
        _signIn = signIn ?? LobbySignIn.Sandbox();
        InitializeComponent();
        _serviceHost = string.IsNullOrWhiteSpace(_signIn.Host) ? "127.0.0.1" : _signIn.Host;
        _servicePort = _signIn.Port is > 0 and <= 65535 ? _signIn.Port : 7788;
        MmServiceBox.Text = _serviceHost + ":" + _servicePort.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (_signIn.IsAccount)
        {
            MmNameBox.Text = _signIn.Name;
            MmNameBox.IsReadOnly = true;
            BtnBrowseDeck.Content = "Upload";
            SetMmState("Account. Server deck list only. No Latinum.");
        }
        else
        {
            MmNameBox.Text = string.IsNullOrWhiteSpace(_signIn.Name) ? Environment.UserName : _signIn.Name;
            SetMmState("Sandbox. Local decks. Online versus Sandbox. No Latinum.");
        }
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
                payloadJson: StampPayload("host", 1));
            await _server.SendAsync(hello, ct).ConfigureAwait(true);

            var reply = await _server.ReceiveAsync(ct).ConfigureAwait(true);
            if (!string.Equals(reply.Type, NetMessage.Types.Handshake, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Expected handshake, got '{reply.Type}'.");
            EnsureSameStamp(reply);

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
                payloadJson: StampPayload("guest", 2));
            await _client.SendAsync(hello, ct).ConfigureAwait(true);

            var reply = await _client.ReceiveAsync(ct).ConfigureAwait(true);
            if (!string.Equals(reply.Type, NetMessage.Types.Handshake, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Expected handshake, got '{reply.Type}'.");
            EnsureSameStamp(reply);

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
            UpdateLoadGameButton();
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
            while (!ct.IsCancellationRequested && !_gameStarting && Volatile.Read(ref _relayReceiveHandedOff) == 0)
            {
                NetMessage msg;
                if (_link != null)
                {
                    // None: this token must not enter ClientWebSocket.ReceiveAsync.
                    // Detach hands the in-flight read to the session. Cancelling it aborts the socket on net8.
                    // After that handoff this continuation must not call ReceiveAsync again.
                    if (Volatile.Read(ref _relayReceiveHandedOff) != 0)
                        break;
                    msg = await _link.ReceiveAsync(CancellationToken.None).ConfigureAwait(false);
                    if (_gameStarting || Volatile.Read(ref _relayReceiveHandedOff) != 0)
                        break;
                }
                else if (_server != null)
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
        catch (InvalidOperationException) when (Volatile.Read(ref _relayReceiveHandedOff) != 0)
        {
            // The session already owns the read. Do not drop the socket from here.
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (!_gameStarting && Volatile.Read(ref _relayReceiveHandedOff) == 0 && IsConnected)
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
                // Host keeps the guest list. The guest never stores the host deck.
                if (IsHost && ready.IsReady && !string.IsNullOrWhiteSpace(ready.DeckJson))
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
            _ = AcceptStartAsync(msg);
            return;
        }

        if (string.Equals(msg.Type, NetMessage.Types.LoadGame, StringComparison.OrdinalIgnoreCase))
        {
            if (IsHost)
                return;
            _guestLoadSignaled = true;
            SetStatus("Host is loading the saved game.");
            TryBeginGuestResume();
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

                if (_signIn.IsAccount && _mm is not { InRoom: true })
        {
            SetStatus("Account matches start from a room so the server can freeze the deck.");
            return;
        }
        if (_mm is { InRoom: true })
        {
            try
            {
                await SendDeckHashIfAnyAsync().ConfigureAwait(true);
                await _mm.BeginAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                SetStatus("Match freeze failed: " + ex.Message);
                return;
            }
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
            // Wire copy has no host deck. RaiseGameStarting keeps both lists for the host engine.
            var wire = new NetLobbyDto.StartGame
            {
                DeckP1Name = "",
                DeckP2Name = start.DeckP2Name,
                DeckP1Json = "",
                DeckP2Json = start.DeckP2Json,
                SkipSeedPhase = start.SkipSeedPhase
            };
            var msg = NetMessage.Create(NetMessage.Types.StartGame, payloadJson: NetLobbyDto.ToJson(wire));
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
            SkipSeedPhase = start.SkipSeedPhase,
            MatchId = _mm?.MatchId ?? "",
            ReportSecret = _mm?.ReportSecret ?? "",
            LobbyHost = _serviceHost,
            LobbyPort = _servicePort,
            AccountMatch = _signIn.IsAccount
        });
    }

    private async Task SendLobbyAsync(NetMessage message)
    {
        var ct = _cts?.Token ?? CancellationToken.None;
        if (_link != null)
            await _link.SendAsync(message, ct).ConfigureAwait(true);
        else if (_server != null)
            await _server.SendAsync(message, ct).ConfigureAwait(true);
        else if (_client != null)
            await _client.SendAsync(message, ct).ConfigureAwait(true);
    }

    private int LocalPlayerNumber => IsHost ? 1 : 2;

    private async void DeckCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressDeck) return;
        if (DeckCombo.SelectedItem is not DeckListItem item) return;
        if (item.FromServer)
            await ApplyServerDeckAsync(item.ServerName).ConfigureAwait(true);
        else
            await ApplyLocalDeckAsync(item.Path, Path.GetFileNameWithoutExtension(item.Path)).ConfigureAwait(true);
    }

    private async void BtnBrowseDeck_Click(object sender, RoutedEventArgs e)
    {
        if (_signIn.IsAccount)
        {
            await UploadAccountDeckAsync().ConfigureAwait(true);
            return;
        }
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

    private void UpdateLoadGameButton()
    {
        var saves = _mm?.Saves;
        bool any = _signIn.IsAccount && saves != null && saves.Count > 0;
        if (BtnLoadGame != null)
        {
            BtnLoadGame.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
            BtnLoadGame.IsEnabled = any && IsHost && !_gameStarting;
        }
        if (LoadGameHint != null)
        {
            LoadGameHint.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
            LoadGameHint.Text = !any
                ? ""
                : IsHost
                    ? "Load game continues the server save for these two accounts. You stay host."
                    : "A saved game is waiting. Only the host can load it. You get the masked table.";
        }
    }

    private async void BtnLoadGame_Click(object sender, RoutedEventArgs e)
    {
        if (_mm == null || !_signIn.IsAccount)
        {
            SetStatus("Load game is only for an account match.");
            return;
        }
        if (!IsHost)
        {
            SetStatus("Only the host loads the saved game.");
            return;
        }
        if (!IsConnected)
        {
            SetStatus("Play via relay first, then Load game.");
            return;
        }
        var saves = _mm.Saves;
        if (saves.Count == 0)
        {
            SetStatus("No save for these two accounts.");
            return;
        }
        var pick = saves.Count == 1 ? saves[0] : PickSave(saves);
        if (pick == null)
            return;
        try
        {
            BtnLoadGame.IsEnabled = false;
            var reply = await _mm.LoadAsync(pick.MatchId, pick.Kind, pick.Name).ConfigureAwait(true);
            if (reply.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "error")
            {
                var message = reply.TryGetProperty("message", out var msg) ? msg.GetString() : "Load failed.";
                SetStatus(message ?? "Load failed.");
                UpdateLoadGameButton();
                return;
            }
            var blob = reply.TryGetProperty("blob", out var blobEl) && blobEl.ValueKind == JsonValueKind.String
                ? blobEl.GetString() ?? ""
                : "";
            if (blob.Length == 0)
            {
                SetStatus("The server did not return the save.");
                UpdateLoadGameButton();
                return;
            }
            var matchId = reply.TryGetProperty("matchId", out var idEl) && idEl.ValueKind == JsonValueKind.String
                ? idEl.GetString() ?? pick.MatchId
                : pick.MatchId;
            var secret = reply.TryGetProperty("secret", out var secEl) && secEl.ValueKind == JsonValueKind.String
                ? secEl.GetString() ?? ""
                : "";
            var signal = NetMessage.Create(
                NetMessage.Types.LoadGame,
                payloadJson: JsonSerializer.Serialize(new { matchId }));
            await SendLobbyAsync(signal).ConfigureAwait(true);
            RaiseResumeStarting(matchId, secret, blob);
        }
        catch (Exception ex)
        {
            SetStatus("Load failed: " + ex.Message);
            UpdateLoadGameButton();
        }
    }

    private void OnGuestResume(string matchId, string secret, string blob)
    {
        if (!string.IsNullOrEmpty(blob) || _gameStarting || !_signIn.IsAccount)
            return;
        _guestResumeMatchId = matchId;
        _guestResumeSecret = secret;
        _ = GuestResumeAsync();
    }

    private async Task GuestResumeAsync()
    {
        try
        {
            if (!IsConnected)
            {
                if (_mm == null || string.IsNullOrWhiteSpace(_mm.PlayerId) || string.IsNullOrWhiteSpace(_mm.RoomName))
                {
                    SetStatus("Load is waiting, but this seat is not in the room.");
                    return;
                }
                if (!TryParseLobbyService(MmServiceBox.Text, out var host, out var port))
                    return;
                var asHost = string.Equals(_mm.Role, "host", StringComparison.Ordinal);
                await RunRelayAsync(host, port, _mm.RoomName, asHost, _mm.PlayerId).ConfigureAwait(true);
            }
            if (!IsConnected)
            {
                SetStatus("Could not open the relay for the loaded game.");
                return;
            }
            TryBeginGuestResume();
        }
        catch (Exception ex)
        {
            SetStatus("Load failed: " + ex.Message);
        }
    }

    private void TryBeginGuestResume()
    {
        if (_gameStarting || !_guestLoadSignaled)
            return;
        if (string.IsNullOrWhiteSpace(_guestResumeSecret))
            return;
        RaiseResumeStarting(_guestResumeMatchId ?? "", _guestResumeSecret, null);
    }

    private void RaiseResumeStarting(string matchId, string secret, string? hostBlob)
    {
        if (_gameStarting)
            return;
        _gameStarting = true;
        SetStatus("Loading the saved game...");
        GameStarting?.Invoke(this, new LobbyGameStartArgs
        {
            IsHost = IsHost,
            Resume = true,
            ResumeSaveJson = hostBlob ?? "",
            AccountMatch = true,
            MatchId = matchId ?? "",
            ReportSecret = secret ?? "",
            LobbyHost = _serviceHost,
            LobbyPort = _servicePort
        });
    }

    private LobbySaveInfo? PickSave(IReadOnlyList<LobbySaveInfo> saves)
    {
        var win = new Window
        {
            Title = "Load game",
            Width = 440,
            Height = 360,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x22)),
            ResizeMode = ResizeMode.NoResize
        };
        var root = new DockPanel { Margin = new Thickness(12) };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var ok = new Button
        {
            Content = "Load game",
            Width = 110,
            Height = 32,
            Margin = new Thickness(0, 0, 8, 0),
            Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x63, 0x9C)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        var cancel = new Button
        {
            Content = "Cancel",
            Width = 90,
            Height = 32,
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x48)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        DockPanel.SetDock(buttons, Dock.Bottom);
        var title = new TextBlock
        {
            Text = "Choose a save for these two accounts.",
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 8)
        };
        DockPanel.SetDock(title, Dock.Top);
        var list = new ListBox
        {
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x22)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x48))
        };
        list.ItemContainerStyle = new Style(typeof(ListBoxItem))
        {
            Setters =
            {
                new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x22))),
                new Setter(Control.ForegroundProperty, Brushes.White)
            }
        };
        foreach (var save in saves)
        {
            var when = string.IsNullOrWhiteSpace(save.SavedUtc) ? "" : "  " + save.SavedUtc;
            var label = save.Kind == "auto"
                ? "Autosave" + when
                : "Manual: " + save.Name + when;
            list.Items.Add(new ListBoxItem { Content = label, Tag = save, Foreground = Brushes.White });
        }
        if (list.Items.Count > 0)
            list.SelectedIndex = 0;
        root.Children.Add(buttons);
        root.Children.Add(title);
        root.Children.Add(list);
        win.Content = root;
        LobbySaveInfo? picked = null;
        ok.Click += (_, _) =>
        {
            if (list.SelectedItem is ListBoxItem item && item.Tag is LobbySaveInfo save)
                picked = save;
            win.DialogResult = picked != null;
        };
        cancel.Click += (_, _) => { win.DialogResult = false; };
        win.ShowDialog();
        return picked;
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

        try
        {
            await SendDeckHashIfAnyAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus("Deck was not stored for the match: " + ex.Message);
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
                DeckJson = IsHost ? null : _localDeckJson
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
        if (_signIn.IsAccount)
        {
            _suppressDeck = true;
            DeckCombo.ItemsSource = _serverDeckItems;
            _suppressDeck = false;
            return;
        }
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
            var peerDeck = string.IsNullOrWhiteSpace(_peerDeckName) ? "no deck yet" : "deck selected";
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
    /// Hand ownership of the live transport to NetPlaySession.
    /// Direct IP cancels its lobby read so only one reader remains on the stream.
    /// Relay does not: cancelling the token inside RelayNetLink.ReceiveAsync aborts the socket on net8.
    /// That in-flight read is handed to the session and is not started again.
    /// </summary>
    public (NetServer? server, NetClient? client, INetLink? link) DetachTransport()
    {
        TransportDetached = true;
        _detachedWasHost = _server is not null || _relayHost;
        _relayHost = false;
        if (_link is RelayNetLink relay)
        {
            // Visible to the lobby continuation before it can start another read.
            Volatile.Write(ref _relayReceiveHandedOff, 1);
            relay.HandOffInFlightReceive();
            // Leave _cts uncancelled. The lobby loop is inside ReceiveAsync(CancellationToken.None).
            // When that read completes, the continuation exits on the handoff and does not read again.
            _cts = null;
            _lobbyReceiveTask = null;
        }
        else
        {
            try { _cts?.Cancel(); } catch { /* ignore */ }
            // Ensure lobby reader stops before NetPlaySession owns the stream.
            try { _lobbyReceiveTask?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
            try { _cts?.Dispose(); } catch { /* ignore */ }
            _cts = null;
            _lobbyReceiveTask = null;
        }

        var server = _server;
        var client = _client;
        var link = _link;
        _server = null;
        _client = null;
        _link = null;
        return (server, client, link);
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
        try { _link?.Dispose(); } catch { /* ignore */ }
        _link = null;
        _relayHost = false;

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

        _serviceHost = host;
        _servicePort = port;
        var name = _signIn.IsAccount
            ? _signIn.Name
            : (string.IsNullOrWhiteSpace(MmNameBox.Text) ? "Player" : MmNameBox.Text.Trim());
        if (name.Length > 24)
            name = name[..24];
        var mm = new MatchmakingClient();
        mm.StateChanged += text => Dispatcher.BeginInvoke(() => SetMmState(text));
        mm.PlayersChanged += text => Dispatcher.BeginInvoke(() => MmPlayersText.Text = text ?? "");
        mm.ChatChanged += text => Dispatcher.BeginInvoke(() => MmChatLog.Text = text ?? "");
        mm.SavesChanged += () => Dispatcher.BeginInvoke(UpdateLoadGameButton);
        mm.ResumeOffered += (matchId, secret, blob) => Dispatcher.BeginInvoke(() => OnGuestResume(matchId, secret, blob));
        mm.AddressAnnounced += (announcedHost, announcedPort) =>
            Dispatcher.BeginInvoke(() => SetMmState($"Host published {announcedHost}:{announcedPort}. Open direct game when ready."));
        try
        {
            await mm.ConnectAsync(host, port, name, _signIn.Mode, _signIn.Token).ConfigureAwait(true);
            _mm?.Dispose();
            _mm = mm;
            if (_signIn.IsAccount)
                await LoadServerDecksAsync().ConfigureAwait(true);
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

    private async void BtnMmPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_mm is not { InRoom: true })
        {
            SetMmState("Create or join a room first. Host, Join, and Localhost above still work.");
            return;
        }
        if (VersionBlocks(out var blocked))
        {
            SetMmState(blocked);
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
            SetMmState($"Published {host}:{port} to the room. Direct IP still works. Relay is Play via relay.");
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
        string? cards = _signIn.IsAccount ? null : DeckCardList.FromDeckJson(_localDeckJson);
        await _mm.SendDeckAsync(_localDeckName, hash, cards).ConfigureAwait(true);
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

    private async void BtnMmRelay_Click(object sender, RoutedEventArgs e)
    {
        if (_mm is not { InRoom: true })
        {
            SetMmState("Create or join a room first. Direct IP and Localhost still work.");
            return;
        }
        if (VersionBlocks(out var blocked))
        {
            SetMmState(blocked);
            return;
        }
        if (string.IsNullOrWhiteSpace(_mm.PlayerId) || string.IsNullOrWhiteSpace(_mm.RoomName))
        {
            SetMmState("The room has no player id yet.");
            return;
        }
        if (!TryParseLobbyService(MmServiceBox.Text, out var host, out var port))
        {
            SetMmState("Service address must be host:port, for example 127.0.0.1:7788.");
            return;
        }
        var asHost = string.Equals(_mm.Role, "host", StringComparison.Ordinal);
        await RunRelayAsync(host, port, _mm.RoomName, asHost, _mm.PlayerId).ConfigureAwait(true);
    }

    private async Task RunRelayAsync(string host, int port, string room, bool asHost, string playerId)
    {
        if (_link is RelayNetLink { IsConnected: true })
        {
            SetStatus(asHost
                ? "Relay: Host (P1). No host port. Pick a deck, then Start game."
                : "Relay: Guest (P2). No host port. Pick a deck, then Start game.");
            return;
        }
        if (_busy) return;
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
            var link = new RelayNetLink();
            SetStatus(asHost
                ? $"Relay: connecting as host to {host}:{port}."
                : $"Relay: connecting as guest to {host}:{port}.");
            await link.ConnectAsync(host, port, room, asHost ? "host" : "guest", playerId, ct).ConfigureAwait(true);
            _link = link;
            _relayHost = asHost;
            var hello = NetMessage.Create(
                NetMessage.Types.Handshake,
                payloadJson: StampPayload(asHost ? "host" : "guest", asHost ? 1 : 2));
            await link.SendAsync(hello, ct).ConfigureAwait(true);
            var reply = await link.ReceiveAsync(ct).ConfigureAwait(true);
            if (!string.Equals(reply.Type, NetMessage.Types.Handshake, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Expected handshake, got '{reply.Type}'.");
            EnsureSameStamp(reply);
            IsConnected = true;
            SetStatus(asHost
                ? "Relay: Host (P1). No host port. Pick a deck, then Start game."
                : "Relay: Guest (P2). No host port. Pick a deck, then Start game.");
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
            DisconnectInternal("Relay error: " + ex.Message);
        }
        finally
        {
            _busy = false;
            if (!IsConnected)
                SetBusyUi(false);
        }
    }


    private bool VersionBlocks(out string why)
    {
        why = "";
        if (_mm == null)
            return false;
        if (_mm.VersionBlocksStart)
        {
            why = string.IsNullOrWhiteSpace(_mm.VersionNote) ? "Version mismatch. No start." : _mm.VersionNote;
            return true;
        }
        if (!_mm.VersionKnown)
        {
            why = string.IsNullOrWhiteSpace(_mm.VersionNote) ? "Waiting for both version stamps." : _mm.VersionNote;
            return true;
        }
        return false;
    }

    private string StampPayload(string role, int player)
        => JsonSerializer.Serialize(new { role, player, engine = EngineStamp.Id, cardHash = EngineStamp.CardHash, mode = _signIn.Mode });

    private void EnsureSameStamp(NetMessage reply)
    {
        string engine = "";
        string hash = "";
        if (!string.IsNullOrWhiteSpace(reply.PayloadJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(reply.PayloadJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("engine", out var en) && en.ValueKind == JsonValueKind.String)
                    engine = en.GetString() ?? "";
                if (root.TryGetProperty("cardHash", out var ch) && ch.ValueKind == JsonValueKind.String)
                    hash = ch.GetString() ?? "";
            }
            catch
            {
                engine = "";
            }
        }
        if (!string.Equals(engine, EngineStamp.Id, StringComparison.Ordinal)
            || !string.Equals(hash, EngineStamp.CardHash, StringComparison.OrdinalIgnoreCase))
        {
            var mine = EngineStamp.CardHash.Length >= 8 ? EngineStamp.CardHash[..8] : EngineStamp.CardHash;
            var theirs = hash.Length >= 8 ? hash[..8] : (hash.Length == 0 ? "none" : hash);
            throw new InvalidOperationException(
                $"Version mismatch: engine '{engine}' vs '{EngineStamp.Id}', cards {theirs} vs {mine}. No start.");
        }
        var mode = "sandbox";
        if (!string.IsNullOrWhiteSpace(reply.PayloadJson))
        {
            try
            {
                using var modeDoc = JsonDocument.Parse(reply.PayloadJson);
                if (modeDoc.RootElement.TryGetProperty("mode", out var modeEl) && modeEl.ValueKind == JsonValueKind.String)
                {
                    var raw = modeEl.GetString() ?? "";
                    if (raw.Length > 0)
                        mode = raw;
                }
            }
            catch
            {
                mode = "sandbox";
            }
        }
        if (!string.Equals(mode, _signIn.Mode, StringComparison.Ordinal))
            throw new InvalidOperationException("sandbox and account cannot play the same match.");
    }


    private async Task AcceptStartAsync(NetMessage msg)
    {
        if (_gameStarting) return;
        if (string.IsNullOrWhiteSpace(msg.PayloadJson)) return;
        try
        {
            if (_signIn.IsAccount && _mm is not { InRoom: true })
            {
                SetStatus("Account matches start from a room so the server can freeze the deck.");
                return;
            }
            if (_mm is { InRoom: true })
                await _mm.WaitForMatchAsync().ConfigureAwait(true);
            var start = NetLobbyDto.FromJson<NetLobbyDto.StartGame>(msg.PayloadJson);
            RaiseGameStarting(start);
        }
        catch (Exception ex)
        {
            SetStatus("Bad StartGame: " + ex.Message);
        }
    }

    private async Task ApplyServerDeckAsync(string name)
    {
        if (_mm == null || string.IsNullOrWhiteSpace(_signIn.Token))
        {
            SetStatus("Connect to the service to use an account deck.");
            return;
        }
        try
        {
            var body = await _mm.GetDeckAsync(_signIn.Token, name).ConfigureAwait(true);
            if (!body.TryGetProperty("cardIds", out var cardIds) || cardIds.ValueKind != JsonValueKind.Object)
            {
                var message = body.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String
                    ? msg.GetString()
                    : "Deck was not read.";
                SetStatus(message ?? "Deck was not read.");
                return;
            }
            var deck = DeckCardList.ToDeck(name, cardIds);
            _localDeckPath = null;
            _localDeckJson = DeckCardList.SerializeDeck(deck);
            _localDeckName = string.IsNullOrWhiteSpace(deck.Name) ? name : deck.Name;
            BtnStartGame.IsEnabled = true;
            SetStatus("Account deck: " + _localDeckName);
            await SendDeckHashIfAnyAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus("Account deck failed: " + ex.Message);
        }
    }

    private async Task UploadAccountDeckAsync()
    {
        if (_mm == null || string.IsNullOrWhiteSpace(_signIn.Token))
        {
            SetStatus("Connect to the service before uploading a deck.");
            return;
        }
        var dlg = new OpenFileDialog
        {
            Title = "Upload deck as card ids",
            Filter = "STCCG Deck (*.stdeck)|*.stdeck",
            InitialDirectory = Directory.Exists(GamePaths.DecksRoot) ? GamePaths.DecksRoot : Environment.CurrentDirectory
        };
        if (dlg.ShowDialog() != true)
            return;
        try
        {
            var json = File.ReadAllText(dlg.FileName);
            var loaded = new Services.DeckService().LoadFromJson(json);
            var name = string.IsNullOrWhiteSpace(loaded.Name)
                ? Path.GetFileNameWithoutExtension(dlg.FileName)
                : loaded.Name.Trim();
            if (name.Length > 80)
                name = name[..80];
            var cards = DeckCardList.FromDeck(loaded);
            var saved = await _mm.SaveDeckAsync(_signIn.Token, name, cards).ConfigureAwait(true);
            if (saved.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "error")
            {
                var message = saved.TryGetProperty("message", out var msg) ? msg.GetString() : "Upload failed.";
                SetStatus(message ?? "Upload failed.");
                return;
            }
            await LoadServerDecksAsync().ConfigureAwait(true);
            SetStatus("Uploaded " + name + " as card ids.");
        }
        catch (Exception ex)
        {
            SetStatus("Upload failed: " + ex.Message);
        }
    }

    private async Task LoadServerDecksAsync()
    {
        if (!_signIn.IsAccount || _mm == null || string.IsNullOrWhiteSpace(_signIn.Token))
            return;
        var body = await _mm.ListDecksAsync(_signIn.Token).ConfigureAwait(true);
        var items = new List<DeckListItem>();
        if (body.TryGetProperty("decks", out var decks) && decks.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in decks.EnumerateArray())
            {
                var name = row.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : "";
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var count = row.TryGetProperty("count", out var c) && c.TryGetInt32(out var k) ? k : 0;
                items.Add(new DeckListItem
                {
                    DisplayName = name + " (" + count + ")",
                    ServerName = name,
                    FromServer = true
                });
            }
        }
        void Apply()
        {
            _suppressDeck = true;
            _serverDeckItems = items;
            DeckCombo.ItemsSource = null;
            DeckCombo.ItemsSource = items;
            _suppressDeck = false;
        }
        if (Dispatcher.CheckAccess())
            Apply();
        else
            Dispatcher.Invoke(Apply);
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