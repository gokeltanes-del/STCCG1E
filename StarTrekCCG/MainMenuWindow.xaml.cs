using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using StarTrekCCG.Models;
using StarTrekCCG.Network;
using Ellipse = System.Windows.Shapes.Ellipse;
using Path = System.IO.Path;

namespace StarTrekCCG;

public partial class MainMenuWindow : Window
{
    private LobbySignIn _signIn;
    private readonly MatchmakingClient _matchmaking = new();
    private bool _isFriendsMode;
    private string _activeChatTab = "Lounge";
    private readonly HashSet<string> _invitedPrivateChatUsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<LobbyPlayerInfo> _onlinePlayers = new();
    private readonly Dictionary<string, List<string>> _chatHistories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Lounge"] = new() { "System: Welcome to the Lounge communications channel." },
        ["Private"] = new() { "System: Private conference channel. Use 'INVITE TO CHAT' on crew members to add participants." }
    };

    // Match Transport & Networking
    private NetServer? _server;
    private NetClient? _client;
    private INetLink? _link;
    private bool _relayHost;
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
    private bool _detachedWasHost;
    private bool _isRankedMatch = true;

    // Deck & Readiness
    private string? _localDeckPath;
    private string? _localDeckJson;
    private string? _localDeckName;
    private bool _localReady;
    private string? _peerDeckName;
    private string? _peerDeckJson;
    private bool _peerReady;

    // Skip seed phase consensus
    private bool _skipSeedConsensusActive;
    private bool? _skipSeedLocalAccept;
    private bool? _skipSeedPeerAccept;
    private int _skipSeedProposedBy;
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
    public bool IsHost => _server is not null || _relayHost || _detachedWasHost;
    public bool TransportDetached { get; private set; }
    private int LocalPlayerNumber => IsHost ? 1 : 2;

    public MainMenuWindow()
        : this(LobbySignIn.Sandbox())
    {
    }

    public MainMenuWindow(LobbySignIn signIn)
    {
        _signIn = signIn ?? LobbySignIn.Sandbox();
        InitializeComponent();

        _serviceHost = string.IsNullOrWhiteSpace(_signIn.Host) ? "127.0.0.1" : _signIn.Host;
        _servicePort = _signIn.Port is > 0 and <= 65535 ? _signIn.Port : 7788;

        _matchmaking.PlayerListChanged += players => Dispatcher.BeginInvoke(() => OnLobbyPlayerListChanged(players));
        _matchmaking.ChatChanged += chat => Dispatcher.BeginInvoke(() => OnLobbyChatReceived(chat));
        _matchmaking.StateChanged += state => Dispatcher.BeginInvoke(() => OnLobbyStateChanged(state));
        _matchmaking.PlayersChanged += text => Dispatcher.BeginInvoke(() =>
        {
            if (MmPlayersText != null)
                MmPlayersText.Text = text ?? "";
        });
        _matchmaking.SavesChanged += () => Dispatcher.BeginInvoke(UpdateLoadGameButton);
        _matchmaking.ResumeOffered += (matchId, secret, blob) => Dispatcher.BeginInvoke(() => OnGuestResume(matchId, secret, blob));
        _matchmaking.AddressAnnounced += (announcedHost, announcedPort) => Dispatcher.BeginInvoke(() =>
        {
            SetMmState($"Host published {announcedHost}:{announcedPort}. Open direct game when ready.");
            if (HostBox != null) HostBox.Text = announcedHost;
            if (PortBox != null) PortBox.Text = announcedPort.ToString(CultureInfo.InvariantCulture);
        });
    }

    private void MainMenuWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateProfileUI();
        RefreshCrewDock();
        RefreshChatView();
        _ = AutoConnectLoungeAsync();
    }

    private void MainMenuWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        try { _matchmaking.Dispose(); } catch { /* ignore */ }
        if (TransportDetached)
            return;
        DisconnectInternal(null);
    }

    private async Task AutoConnectLoungeAsync()
    {
        var host = string.IsNullOrWhiteSpace(_signIn.Host) ? "127.0.0.1" : _signIn.Host;
        var port = _signIn.Port is > 0 and <= 65535 ? _signIn.Port : 7788;
        var name = string.IsNullOrWhiteSpace(_signIn.Name) ? (_signIn.IsAccount ? "AccountPlayer" : "Cadet") : _signIn.Name;
        _serviceHost = host;
        _servicePort = port;

        try
        {
            await _matchmaking.ConnectAsync(host, port, name, _signIn.Mode, _signIn.Token).ConfigureAwait(true);
            await _matchmaking.JoinRoomAsync("Lounge").ConfigureAwait(true);
            if (_signIn.IsAccount)
            {
                await LoadServerDecksAsync().ConfigureAwait(true);
                await RefreshAccountPoolAsync().ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            if (!_chatHistories.ContainsKey("Lounge"))
                _chatHistories["Lounge"] = new List<string>();
            _chatHistories["Lounge"].Add($"[Server offline or connecting to {host}:{port}: {ex.Message}]");
            RefreshChatView();
        }
    }

    private void OnLobbyPlayerListChanged(IReadOnlyList<LobbyPlayerInfo> players)
    {
        _onlinePlayers.Clear();
        if (players != null)
        {
            foreach (var p in players)
            {
                if (!string.IsNullOrWhiteSpace(p.Name))
                    _onlinePlayers.Add(p);
            }
        }
        RefreshCrewDock();
    }

    private void OnLobbyChatReceived(string fullChat)
    {
        if (string.IsNullOrWhiteSpace(fullChat)) return;
        var lines = fullChat.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        _chatHistories["Lounge"] = new List<string>(lines);
        if (_activeChatTab == "Lounge")
            RefreshChatView();
    }

    private void OnLobbyStateChanged(string state)
    {
        if (string.IsNullOrWhiteSpace(state)) return;
        if (!_chatHistories.ContainsKey("Lounge"))
            _chatHistories["Lounge"] = new List<string>();
        _chatHistories["Lounge"].Add($"[Server: {state}]");
        if (_activeChatTab == "Lounge")
            RefreshChatView();
        SetMmState(state);
    }

    private void UpdateProfileUI()
    {
        if (_signIn.IsAccount)
        {
            PlayerNameText.Text = string.IsNullOrWhiteSpace(_signIn.Name) ? "ACCOUNT PLAYER" : _signIn.Name.ToUpperInvariant();
            PlayerRankText.Text = "COMMANDER";
            PlayerFactionText.Text = "FEDERATION";
            BtnAuthSwitch.Content = "LOGOUT";
            OnlineIndicatorLight.Fill = (Brush)FindResource("SkinBrush_GreenOnline");
            OnlineStatusText.Text = "online";
            LatinumAmountText.Text = _signIn.Latinum?.ToString(CultureInfo.InvariantCulture) ?? "0";
        }
        else
        {
            PlayerNameText.Text = string.IsNullOrWhiteSpace(_signIn.Name) ? "SANDBOX CADET" : _signIn.Name.ToUpperInvariant();
            PlayerRankText.Text = "CADET";
            PlayerFactionText.Text = "UNALIGNED";
            BtnAuthSwitch.Content = "LOGIN";
            OnlineIndicatorLight.Fill = (Brush)FindResource("SkinBrush_RedOffline");
            OnlineStatusText.Text = "offline (sandbox)";
            LatinumAmountText.Text = "0";
        }
    }

    private void RefreshCrewDock()
    {
        AvatarsContainerPanel.Children.Clear();
        AvatarDockTitle.Text = _isFriendsMode ? "FRIENDS LIST" : "LOUNGE CREW (ONLINE)";

        if (_onlinePlayers.Count == 0)
        {
            var emptyNotice = new TextBlock
            {
                Text = "No other crew members online in the Lounge.",
                Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            };
            AvatarsContainerPanel.Children.Add(emptyNotice);
            return;
        }

        foreach (var player in _onlinePlayers)
        {
            var isSelf = string.Equals(player.Name, _signIn.Name, StringComparison.OrdinalIgnoreCase);
            var role = string.IsNullOrWhiteSpace(player.Role) ? "MEMBER" : player.Role.ToUpperInvariant();
            var status = string.IsNullOrWhiteSpace(player.DeckName) ? "IN LOUNGE" : $"DECK: {player.DeckName}";
            var card = CreateAvatarCard(player.Name, role, status, true, isSelf);
            AvatarsContainerPanel.Children.Add(card);
        }
    }

    private Border CreateAvatarCard(string name, string rank, string status, bool online, bool isSelf = false)
    {
        var border = new Border
        {
            Width = 240,
            Height = 96,
            Margin = new Thickness(4, 2, 4, 2),
            Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x2A, 0x3A)),
            BorderBrush = isSelf ? (Brush)FindResource("SkinBrush_CyanAccent") : new SolidColorBrush(Color.FromRgb(0x3B, 0x57, 0x75)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var avatarBox = new Border
        {
            Width = 44,
            Height = 44,
            Background = new SolidColorBrush(Color.FromRgb(0x10, 0x1A, 0x24)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            VerticalAlignment = VerticalAlignment.Top
        };
        var iconText = new TextBlock
        {
            Text = name.Length > 0 ? name[..1].ToUpperInvariant() : "?",
            Foreground = new SolidColorBrush(Color.FromRgb(0xBA, 0xE6, 0xFD)),
            FontWeight = FontWeights.Bold,
            FontSize = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        avatarBox.Child = iconText;
        Grid.SetColumn(avatarBox, 0);
        grid.Children.Add(avatarBox);

        var detailStack = new StackPanel
        {
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        var nameBlock = new TextBlock
        {
            Text = isSelf ? $"{name} (YOU)" : name,
            Foreground = isSelf ? (Brush)FindResource("SkinBrush_CyanAccent") : Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 12
        };
        var dot = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = online ? (Brush)FindResource("SkinBrush_GreenOnline") : (Brush)FindResource("SkinBrush_RedOffline"),
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        nameRow.Children.Add(nameBlock);
        nameRow.Children.Add(dot);
        detailStack.Children.Add(nameRow);

        var statusBlock = new TextBlock
        {
            Text = $"{rank} · {status}",
            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            FontSize = 10,
            Margin = new Thickness(0, 2, 0, 6)
        };
        detailStack.Children.Add(statusBlock);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
        var isInvited = _invitedPrivateChatUsers.Contains(name);
        var btnInviteChat = new Button
        {
            Content = isInvited ? "INVITED" : "INVITE TO CHAT",
            Style = (Style)FindResource("SkinStyle_ActionMiniButton"),
            Tag = name,
            IsEnabled = !isSelf,
            MinWidth = 88
        };
        btnInviteChat.Click += (s, e) => InviteUserToPrivateChat(name, btnInviteChat);

        var btnTrade = new Button
        {
            Content = "TRADE",
            Style = (Style)FindResource("SkinStyle_ActionMiniButton"),
            Tag = name,
            IsEnabled = !isSelf
        };
        btnTrade.Click += (s, e) => LaunchTradeWindow();

        var btnPlay = new Button
        {
            Content = "PLAY",
            Style = (Style)FindResource("SkinStyle_ActionMiniButton"),
            Tag = name,
            IsEnabled = !isSelf
        };
        btnPlay.Click += (s, e) => OpenMatchSelectionPrompt(name);

        btnRow.Children.Add(btnInviteChat);
        btnRow.Children.Add(btnTrade);
        btnRow.Children.Add(btnPlay);
        detailStack.Children.Add(btnRow);

        Grid.SetColumn(detailStack, 1);
        grid.Children.Add(detailStack);

        border.Child = grid;
        return border;
    }

    private void InviteUserToPrivateChat(string targetUser, Button btn)
    {
        _invitedPrivateChatUsers.Add(targetUser);
        btn.Content = "INVITED";
        _activeChatTab = "Private";

        if (!_chatHistories.ContainsKey("Private"))
            _chatHistories["Private"] = new List<string>();

        var list = string.Join(", ", _invitedPrivateChatUsers);
        _chatHistories["Private"].Add($"[Invited {targetUser} to private conference. Participants: {list}]");
        RefreshChatView();
    }

    private void OpenMatchSelectionPrompt(string targetUser)
    {
        var choice = MessageBox.Show(
            this,
            $"Challenge {targetUser} to a match?\n\nClick YES for 'Official Match (Ranked)' or NO for 'Holodeck (Open Rules)'.",
            $"Challenge {targetUser}",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (choice == MessageBoxResult.Yes)
        {
            OpenMatchLobby(ranked: true, targetRoomOrUser: $"Match_{targetUser}");
        }
        else if (choice == MessageBoxResult.No)
        {
            OpenMatchLobby(ranked: false, targetRoomOrUser: $"Match_{targetUser}");
        }
    }

    private void RefreshChatView()
    {
        BtnChatTabLounge.Opacity = _activeChatTab == "Lounge" ? 1.0 : 0.6;
        BtnChatTabPrivate.Opacity = _activeChatTab == "Private" ? 1.0 : 0.6;

        if (_chatHistories.TryGetValue(_activeChatTab, out var lines))
        {
            ChatHistoryBox.Text = string.Join(Environment.NewLine, lines);
            ChatScrollViewer.ScrollToEnd();
        }
    }

    private void BtnChatTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string tab)
        {
            _activeChatTab = tab;
            RefreshChatView();
        }
    }

    private void BtnChatSend_Click(object sender, RoutedEventArgs e)
    {
        SubmitChatMessage();
    }

    private void ChatInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SubmitChatMessage();
        }
    }

    private void SubmitChatMessage()
    {
        var text = (ChatInputBox.Text ?? "").Trim();
        if (text.Length == 0) return;

        if (_activeChatTab == "Lounge" && _matchmaking.InRoom)
        {
            _ = _matchmaking.SendChatAsync(text);
        }
        else
        {
            var senderName = string.IsNullOrWhiteSpace(_signIn.Name) ? "You" : _signIn.Name;
            if (!_chatHistories.ContainsKey(_activeChatTab))
                _chatHistories[_activeChatTab] = new List<string>();

            _chatHistories[_activeChatTab].Add($"{senderName}: {text}");
            RefreshChatView();
        }
        ChatInputBox.Text = "";
    }

    private void BtnAuthSwitch_Click(object sender, RoutedEventArgs e)
    {
        if (_signIn.IsAccount)
        {
            _signIn = LobbySignIn.Sandbox();
            UpdateProfileUI();
            _ = AutoConnectLoungeAsync();
            return;
        }

        var gate = new AccountGateWindow { Owner = this };
        if (gate.ShowDialog() == true)
        {
            _signIn = gate.Result;
            UpdateProfileUI();
            _ = AutoConnectLoungeAsync();
        }
    }

    // =========================================================================
    // VIEW NAVIGATION & MATCH CONSOLE
    // =========================================================================

    public void OpenMatchLobby(bool ranked = true, string? targetRoomOrUser = null)
    {
        _isRankedMatch = ranked;
        LobbyModeBadgeText.Text = ranked ? "OFFICIAL MATCH (RANKED)" : "HOLODECK (OPEN RULES)";
        LobbyModeBadgeBorder.Background = ranked
            ? new SolidColorBrush(Color.FromRgb(0x4A, 0x3B, 0x1B))
            : new SolidColorBrush(Color.FromRgb(0x1B, 0x3B, 0x4A));
        LobbyModeBadgeText.Foreground = ranked
            ? new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47))
            : (Brush)FindResource("SkinBrush_CyanAccent");

        if (!string.IsNullOrWhiteSpace(targetRoomOrUser))
            MmRoomBox.Text = targetRoomOrUser;
        else if (string.IsNullOrWhiteSpace(MmRoomBox.Text) || MmRoomBox.Text == "Lounge")
            MmRoomBox.Text = "Table";

        BridgeOverviewGrid.Visibility = Visibility.Collapsed;
        MatchLobbyGrid.Visibility = Visibility.Visible;

        if (_signIn.IsAccount)
        {
            BtnBrowseDeck.Content = "Upload";
            _ = LoadServerDecksAsync();
        }
        else
        {
            BtnBrowseDeck.Content = "Browse…";
            RefreshDeckList();
        }

        UpdateLobbyUi();
        UpdateLoadGameButton();
    }

    public void CloseMatchLobby()
    {
        if (IsConnected)
            DisconnectInternal("Returned to bridge overview.");

        if (_matchmaking is { InRoom: true } && !string.Equals(_matchmaking.RoomName, "Lounge", StringComparison.OrdinalIgnoreCase))
        {
            _ = _matchmaking.JoinRoomAsync("Lounge");
        }

        MatchLobbyGrid.Visibility = Visibility.Collapsed;
        BridgeOverviewGrid.Visibility = Visibility.Visible;
    }

    private void BtnLobbyBackToBridge_Click(object sender, RoutedEventArgs e)
    {
        CloseMatchLobby();
    }

    private void BtnNavBack_Click(object sender, RoutedEventArgs e)
    {
        CloseMatchLobby();
    }

    private void BtnLaunchRankedMatch_Click(object sender, RoutedEventArgs e)
    {
        OpenMatchLobby(ranked: true);
    }

    private void BtnLaunchHolodeckMatch_Click(object sender, RoutedEventArgs e)
    {
        OpenMatchLobby(ranked: false);
    }

    private void BtnNavPlay_Click(object sender, RoutedEventArgs e)
    {
        LaunchGameTable();
    }

    private void BtnQuickLaunchGame_Click(object sender, RoutedEventArgs e)
    {
        LaunchGameTable();
    }

    private void LaunchGameTable()
    {
        var table = new TableWindow();
        table.Show();
        Close();
    }

    private void BtnNavCollection_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this, "Collection screen will open here.", "Collection", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnNavDecks_Click(object sender, RoutedEventArgs e)
    {
        var deckBuilder = new DeckBuilderWindow();
        deckBuilder.Owner = this;
        deckBuilder.ShowDialog();
    }

    private void BtnNavCareer_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this, "Career & Stats screen will open here.", "Career", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnNavOptions_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this, "Options & Configuration console.", "Options", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnNavLounge_Click(object sender, RoutedEventArgs e)
    {
        _isFriendsMode = false;
        RefreshCrewDock();
        OpenMatchLobby(ranked: true);
    }

    private void BtnNavFriends_Click(object sender, RoutedEventArgs e)
    {
        _isFriendsMode = !_isFriendsMode;
        RefreshCrewDock();
    }

    private void BtnShopOpen_Click(object sender, RoutedEventArgs e)
    {
        var booster = new BoosterWindow(_signIn.Latinum, async () =>
        {
            if (_signIn.IsAccount && _matchmaking != null && !string.IsNullOrWhiteSpace(_signIn.Token))
            {
                try
                {
                    var res = await _matchmaking.BuyPackAsync(_signIn.Token, "premiere").ConfigureAwait(true);
                    if (res.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "booster")
                    {
                        var lat = res.TryGetProperty("latinum", out var latEl) && latEl.TryGetInt32(out var n) ? n : (_signIn.Latinum ?? 100) - 50;
                        _signIn.Latinum = lat;
                        UpdateProfileUI();
                        var ids = new List<string>();
                        if (res.TryGetProperty("cards", out var cardsEl) && cardsEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var c in cardsEl.EnumerateArray())
                            {
                                if (c.ValueKind == JsonValueKind.String && c.GetString() is { } s)
                                    ids.Add(s);
                            }
                        }
                        return PackBuyResult.Bought(lat, ids);
                    }
                    var msg = res.TryGetProperty("message", out var m) ? m.GetString() : "Purchase failed.";
                    return PackBuyResult.Fail(msg ?? "Purchase failed.");
                }
                catch (Exception ex)
                {
                    return PackBuyResult.Fail(ex.Message);
                }
            }
            await Task.Delay(500);
            var remaining = (_signIn.Latinum ?? 100) - 50;
            return PackBuyResult.Bought(remaining, new[] { "Premiere Rare #1", "Premiere Uncommon #2" });
        });
        booster.Owner = this;
        booster.ShowDialog();
    }

    private void LaunchTradeWindow()
    {
        var trade = new TradeWindow(
            (to, give, ask) =>
            {
                var giveArr = JsonSerializer.Deserialize<JsonElement>(give);
                var askArr = JsonSerializer.Deserialize<JsonElement>(ask);
                return _matchmaking.TradeOfferAsync(_signIn.Token ?? "", to, giveArr, askArr);
            },
            () => _matchmaking.TradeListAsync(_signIn.Token ?? ""),
            id => _matchmaking.TradeAcceptAsync(_signIn.Token ?? "", id),
            id => _matchmaking.TradeDeclineAsync(_signIn.Token ?? "", id),
            () => _matchmaking.GetPoolAsync(_signIn.Token ?? "")
        );
        trade.Owner = this;
        trade.ShowDialog();
    }

    // =========================================================================
    // MATCHMAKING & TRANSPORT LOGIC
    // =========================================================================

    private void EnterLobbyRoom()
    {
        void Apply()
        {
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
            // The session already owns the read.
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
                if (IsHost && ready.IsReady && !string.IsNullOrWhiteSpace(ready.DeckJson))
                    _peerDeckJson = ready.DeckJson;
                UpdateLobbyUi();

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
            if (IsHost) return;
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

        if (_signIn.IsAccount && _matchmaking is not { InRoom: true })
        {
            SetStatus("Account matches start from a room so the server can freeze the deck.");
            return;
        }
        if (_matchmaking is { InRoom: true })
        {
            try
            {
                await SendDeckHashIfAnyAsync().ConfigureAwait(true);
                await _matchmaking.BeginAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                SetStatus("Match freeze failed: " + ex.Message);
                return;
            }
        }

        StopSkipSeedTimeout();
        _skipSeedConsensusActive = false;

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

        StopSkipSeedTimeout();
        _skipSeedConsensusActive = false;

        var startArgs = new LobbyGameStartArgs
        {
            IsHost = IsHost,
            DeckP1Name = start.DeckP1Name,
            DeckP2Name = start.DeckP2Name,
            DeckP1Json = start.DeckP1Json,
            DeckP2Json = start.DeckP2Json,
            SkipSeedPhase = start.SkipSeedPhase,
            MatchId = _matchmaking?.MatchId ?? "",
            ReportSecret = _matchmaking?.ReportSecret ?? "",
            LobbyHost = _serviceHost,
            LobbyPort = _servicePort,
            AccountMatch = _signIn.IsAccount
        };

        var (server, client, link) = DetachTransport();
        var table = new TableWindow();
        var hostAddr = HostBox?.Text;
        var portVal = int.TryParse(PortBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) ? p : 7777;
        table.StartNetSessionFromLobby(server, client, link, startArgs.IsHost, hostAddr, portVal, startArgs);
        table.Show();
        Close();
    }

    private void RaiseResumeStarting(string matchId, string secret, string? hostBlob)
    {
        if (_gameStarting) return;
        _gameStarting = true;
        SetStatus("Loading the saved game…");

        StopSkipSeedTimeout();
        _skipSeedConsensusActive = false;

        var startArgs = new LobbyGameStartArgs
        {
            IsHost = IsHost,
            Resume = true,
            ResumeSaveJson = hostBlob ?? "",
            AccountMatch = true,
            MatchId = matchId ?? "",
            ReportSecret = secret ?? "",
            LobbyHost = _serviceHost,
            LobbyPort = _servicePort
        };

        var (server, client, link) = DetachTransport();
        var table = new TableWindow();
        var hostAddr = HostBox?.Text;
        var portVal = int.TryParse(PortBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) ? p : 7777;
        table.StartNetSessionFromLobby(server, client, link, startArgs.IsHost, hostAddr, portVal, startArgs);
        table.Show();
        Close();
    }

    private async Task SendLobbyAsync(NetMessage message)
    {
        var ct = _cts?.Token ?? CancellationToken.None;
        if (_link != null)
            await _link.SendAsync(message, ct).ConfigureAwait(false);
        else if (_server != null)
            await _server.SendAsync(message, ct).ConfigureAwait(false);
        else if (_client != null)
            await _client.SendAsync(message, ct).ConfigureAwait(false);
    }

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
        var saves = _matchmaking?.Saves;
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
        if (_matchmaking == null || !_signIn.IsAccount)
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
        var saves = _matchmaking.Saves;
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
            var reply = await _matchmaking.LoadAsync(pick.MatchId, pick.Kind, pick.Name).ConfigureAwait(true);
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
                if (_matchmaking == null || string.IsNullOrWhiteSpace(_matchmaking.PlayerId) || string.IsNullOrWhiteSpace(_matchmaking.RoomName))
                {
                    SetStatus("Load is waiting, but this seat is not in the room.");
                    return;
                }
                var asHost = string.Equals(_matchmaking.Role, "host", StringComparison.Ordinal);
                await RunRelayAsync(_serviceHost, _servicePort, _matchmaking.RoomName, asHost, _matchmaking.PlayerId).ConfigureAwait(true);
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
            DeckCombo.SelectedIndex = -1;
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
        if (!int.TryParse((PortBox.Text ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out port) || port < 1 || port > 65535)
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
        if (!IsHost) return;

        if (_skipSeedLocalAccept == false || _skipSeedPeerAccept == false)
        {
            SetStatus("Skip declined — starting with manual seed…");
            _ = TryHostSendStartGameAsync(skipSeedPhase: false);
            return;
        }

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

    public (NetServer? server, NetClient? client, INetLink? link) DetachTransport()
    {
        TransportDetached = true;
        _detachedWasHost = _server is not null || _relayHost;
        _relayHost = false;
        if (_link is RelayNetLink relay)
        {
            Volatile.Write(ref _relayReceiveHandedOff, 1);
            relay.HandOffInFlightReceive();
            _cts = null;
            _lobbyReceiveTask = null;
        }
        else
        {
            try { _cts?.Cancel(); } catch { /* ignore */ }
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
    }

    private void HideLobbyRoom()
    {
        void Apply()
        {
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
            NatStatusText.Margin = string.IsNullOrEmpty(text) ? new Thickness(0) : new Thickness(0, 4, 0, 0);
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

    private async void BtnMmCreate_Click(object sender, RoutedEventArgs e)
        => await RoomCommandAsync(join: false).ConfigureAwait(true);

    private async void BtnMmJoin_Click(object sender, RoutedEventArgs e)
        => await RoomCommandAsync(join: true).ConfigureAwait(true);

    private async Task RoomCommandAsync(bool join)
    {
        if (!join && !TryParsePort(out _))
            return;
        try
        {
            if (join)
                await _matchmaking.JoinRoomAsync((MmRoomBox.Text ?? "").Trim()).ConfigureAwait(true);
            else
                await _matchmaking.CreateRoomAsync((MmRoomBox.Text ?? "").Trim(), int.Parse((PortBox.Text ?? "").Trim(), CultureInfo.InvariantCulture)).ConfigureAwait(true);
            await SendDeckHashIfAnyAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetMmState(ex.Message);
        }
    }

    private async void BtnMmRefresh_Click(object sender, RoutedEventArgs e)
    {
        try { await _matchmaking.RefreshAsync().ConfigureAwait(true); }
        catch (Exception ex) { SetMmState(ex.Message); }
    }

    private async void BtnMmPlay_Click(object sender, RoutedEventArgs e)
    {
        if (_matchmaking is not { InRoom: true } || string.Equals(_matchmaking.RoomName, "Lounge", StringComparison.OrdinalIgnoreCase))
        {
            SetMmState("Create or join a match room first. Host, Join, and Localhost below still work.");
            return;
        }
        if (VersionBlocks(out var blocked))
        {
            SetMmState(blocked);
            return;
        }

        if (string.Equals(_matchmaking.Role, "host", StringComparison.Ordinal))
        {
            await RunHostAsync(loopbackOnly: false).ConfigureAwait(true);
            return;
        }

        if (string.IsNullOrWhiteSpace(_matchmaking.DirectHost) || _matchmaking.DirectPort is < 1 or > 65535)
        {
            SetMmState("The host has not published an address yet.");
            return;
        }

        HostBox.Text = _matchmaking.DirectHost;
        PortBox.Text = _matchmaking.DirectPort.ToString(CultureInfo.InvariantCulture);
        await RunJoinAsync().ConfigureAwait(true);
    }

    private async void BtnMmRelay_Click(object sender, RoutedEventArgs e)
    {
        if (_matchmaking is not { InRoom: true } || string.Equals(_matchmaking.RoomName, "Lounge", StringComparison.OrdinalIgnoreCase))
        {
            SetMmState("Create or join a match room first. Direct IP and Localhost still work.");
            return;
        }
        if (VersionBlocks(out var blocked))
        {
            SetMmState(blocked);
            return;
        }
        if (string.IsNullOrWhiteSpace(_matchmaking.PlayerId) || string.IsNullOrWhiteSpace(_matchmaking.RoomName))
        {
            SetMmState("The room has no player id yet.");
            return;
        }
        var asHost = string.Equals(_matchmaking.Role, "host", StringComparison.Ordinal);
        await RunRelayAsync(_serviceHost, _servicePort, _matchmaking.RoomName, asHost, _matchmaking.PlayerId).ConfigureAwait(true);
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
        if (port == 7777)
        {
            SetMmState("Play via relay does not dial port 7777. Use the server address with port 7788. Direct IP stays on Host and Join.");
            SetStatus("Relay refused port 7777. No local listener. No UPnP.");
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
            if (_server != null)
                throw new InvalidOperationException("Relay must not keep a local listener.");
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
            SetNatStatus("Relay. No listener on 7777. No UPnP or PCP.");
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

    private async Task PublishMatchmakingAddressAsync(int listenPort)
    {
        if (_matchmaking is not { InRoom: true } || !string.Equals(_matchmaking.Role, "host", StringComparison.Ordinal))
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

            await _matchmaking.SendAddressAsync(host, port).ConfigureAwait(true);
            SetMmState($"Published {host}:{port} to the room. Direct IP still works. Relay is Play via relay.");
        }
        catch (Exception ex)
        {
            SetMmState("Could not publish the address (" + ex.Message + "). The host is still listening.");
        }
    }

    private async Task SendDeckHashIfAnyAsync()
    {
        if (_matchmaking is not { InRoom: true } || string.IsNullOrWhiteSpace(_localDeckJson) || string.IsNullOrWhiteSpace(_localDeckName))
            return;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_localDeckJson))).ToLowerInvariant();
        string? cards = _signIn.IsAccount ? null : DeckCardList.FromDeckJson(_localDeckJson);
        await _matchmaking.SendDeckAsync(_localDeckName, hash, cards).ConfigureAwait(true);
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

    private async Task ApplyServerDeckAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(_signIn.Token))
        {
            SetStatus("Connect to the service to use an account deck.");
            return;
        }
        try
        {
            var body = await _matchmaking.GetDeckAsync(_signIn.Token, name).ConfigureAwait(true);
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
        if (string.IsNullOrWhiteSpace(_signIn.Token))
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
            var db = new Services.CardDatabase(GamePaths.DataRoot);
            db.LoadAll();
            db.LinkDeck(loaded);
            var name = string.IsNullOrWhiteSpace(loaded.Name)
                ? Path.GetFileNameWithoutExtension(dlg.FileName)
                : loaded.Name.Trim();
            if (name.Length > 80)
                name = name[..80];
            var cards = DeckCardList.FromDeck(loaded);
            var saved = await _matchmaking.SaveDeckAsync(_signIn.Token, name, cards).ConfigureAwait(true);
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
        if (!_signIn.IsAccount || string.IsNullOrWhiteSpace(_signIn.Token))
            return;
        var body = await _matchmaking.ListDecksAsync(_signIn.Token).ConfigureAwait(true);
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

    private async Task RefreshAccountPoolAsync()
    {
        if (!_signIn.IsAccount || string.IsNullOrWhiteSpace(_signIn.Token))
            return;
        try
        {
            var body = await _matchmaking.GetPoolAsync(_signIn.Token).ConfigureAwait(true);
            if (body.TryGetProperty("latinum", out var latinum) && latinum.TryGetInt32(out var n))
            {
                _signIn.Latinum = n;
                UpdateProfileUI();
            }
        }
        catch
        {
            // keep the number from sign-in
        }
        if (_signIn.Latinum is int shown)
            SetMmState("Account. Server deck list only. Latinum " + shown.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private void SetMmState(string text)
    {
        void Apply()
        {
            if (MmStateText != null)
                MmStateText.Text = WithLatinum(text ?? "");
        }
        if (Dispatcher.CheckAccess()) Apply();
        else Dispatcher.Invoke(Apply);
    }

    private string WithLatinum(string text)
    {
        if (!_signIn.IsAccount || _signIn.Latinum is not int n)
            return text;
        var number = n.ToString(CultureInfo.InvariantCulture);
        var mark = " Latinum ";
        var cut = text.LastIndexOf(mark, StringComparison.Ordinal);
        if (cut >= 0)
            text = text[..cut].TrimEnd();
        if (text.Contains("No Latinum", StringComparison.Ordinal))
            return text.Replace("No Latinum", "Latinum " + number);
        if (text.Contains("Latinum " + number, StringComparison.Ordinal))
            return text;
        return text.TrimEnd() + " Latinum " + number + ".";
    }

    private bool VersionBlocks(out string why)
    {
        why = "";
        if (_matchmaking == null)
            return false;
        if (_matchmaking.VersionBlocksStart)
        {
            why = string.IsNullOrWhiteSpace(_matchmaking.VersionNote) ? "Version mismatch. No start." : _matchmaking.VersionNote;
            return true;
        }
        if (!_matchmaking.VersionKnown)
        {
            why = string.IsNullOrWhiteSpace(_matchmaking.VersionNote) ? "Waiting for both version stamps." : _matchmaking.VersionNote;
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
            if (_signIn.IsAccount && _matchmaking is not { InRoom: true })
            {
                SetStatus("Account matches start from a room so the server can freeze the deck.");
                return;
            }
            if (_matchmaking is { InRoom: true })
                await _matchmaking.WaitForMatchAsync().ConfigureAwait(true);
            var start = NetLobbyDto.FromJson<NetLobbyDto.StartGame>(msg.PayloadJson);
            RaiseGameStarting(start);
        }
        catch (Exception ex)
        {
            SetStatus("Bad StartGame: " + ex.Message);
        }
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
}
