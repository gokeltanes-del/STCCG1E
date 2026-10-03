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
    private Button? _devLatinumButton;
    private TextBlock? _devLatinumNote;
    private CardShopScreen? _cardShop;
    private CollectionScreen? _collection;
    private AccountTradeScreen? _tradeScreen;
    private string? _tradeSessionId;
    private string? _tradePartner;
    private bool _tradeSealed;
    private int _tradeLoad;
    private string[] _tradeMine = { "", "", "", "" };
    private string[] _tradeTheirs = { "", "", "", "" };
    private bool _tradeMineLocked;
    private bool _tradeTheirsLocked;
    private Services.CardDatabase? _shopCatalog;
    private readonly MatchmakingClient _matchmaking = new();
    private bool _isFriendsMode;
    private bool _crewFriends;
    private readonly List<string> _friendNames = new();
    private readonly List<bool> _friendOnline = new();
    private readonly Queue<(string From, string Id)> _friendAskQueue = new();
    private string _crewStatus = "";
    private TextBlock? _crewNoteBlock;
    private bool _friendPromptOpen;
    private string _activeChatTab = "Lounge";
    private readonly HashSet<string> _invitedPrivateChatUsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _privateChatMembers = new(StringComparer.OrdinalIgnoreCase);
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

    private enum MenuLevel
    {
        Bridge,
        Options,
        AccountDelete,
        ModeChoice,
        MatchConsole,
        DeckBuilder,
        CardShop,
        Collection,
        CollectionBinder,
        Trade,
        PlayChoice,
        Singleplayer,
        Crew
    }

    private readonly List<MenuLevel> _menu = new() { MenuLevel.Bridge };
    private string? _openChallengeId;
    private bool _pickingMode;
    private string? _modeNote;
    private string? _challengeMatchMode;
    private bool _holodeckDirect;
    private Grid? _promptLayer;
    private bool _promptOpen;
    private Action? _promptDecline;

    private MenuLevel CurrentMenu => _menu.Count == 0 ? MenuLevel.Bridge : _menu[^1];

    // Deck & Readiness
    private string? _localDeckPath;
    private string? _localDeckJson;
    private string? _localDeckName;
    private bool _localReady;
    private string? _peerDeckName;
    private string? _peerDeckJson;
    private bool _peerReady;
    private int _localDeckCount;
    private int _peerDeckCount;
    private bool _directGame;
    private DeckBuilderWindow? _deckBuilder;

    // Skip seed phase consensus
    private bool _skipSeedConsensusActive;
    private bool? _skipSeedLocalAccept;
    private bool? _skipSeedPeerAccept;
    private int _skipSeedProposedBy;
    private DispatcherTimer? _skipSeedTimeout;
    private const int SkipSeedTimeoutSeconds = 45;
    private readonly SemaphoreSlim _lobbySend = new(1, 1);

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
        DeveloperMode.Changed += () => Dispatcher.BeginInvoke(new Action(EnsureDevLatinumButton));
        PreviewKeyDown += MainMenu_PreviewKeyDown;

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
        _matchmaking.InviteReceived += (from, id) => Dispatcher.BeginInvoke(() => OnInviteReceived(from, id));
        _matchmaking.FriendAskReceived += (from, id) => Dispatcher.BeginInvoke(() => OnFriendAsk(from, id));
        _matchmaking.FriendNotice += notice => Dispatcher.BeginInvoke(() => OnFriendNotice(notice));
        _matchmaking.FriendsChanged += (names, online) => Dispatcher.BeginInvoke(() => OnFriendsChanged(names, online));
        _matchmaking.InviteAccepted += (by, id) => Dispatcher.BeginInvoke(() => OnInviteAccepted(by, id));
        _matchmaking.PrivateChatInviteReceived += (from, id) => Dispatcher.BeginInvoke(() => OnPrivateChatInvite(from, id));
        _matchmaking.PrivateChatInviteSent += to => Dispatcher.BeginInvoke(() => OnPrivateChatInviteSent(to));
        _matchmaking.PrivateChatInviteFailed += (to, message) => Dispatcher.BeginInvoke(() => OnPrivateChatInviteFailed(to, message));
        _matchmaking.PrivateChatDeclined += by => Dispatcher.BeginInvoke(() => OnPrivateChatDeclined(by));
        _matchmaking.PrivateChatInviteClosed += message => Dispatcher.BeginInvoke(() => OnPrivateChatInviteClosed(message));
        _matchmaking.PrivateChatJoined += names => Dispatcher.BeginInvoke(() => OnPrivateChatJoined(names));
        _matchmaking.PrivateChatLine += (from, line) => Dispatcher.BeginInvoke(() => OnPrivateChatLine(from, line));
        _matchmaking.PrivateChatEnded += message => Dispatcher.BeginInvoke(() => OnPrivateChatEnded(message));
        _matchmaking.TradeAsked += (from, id) => Dispatcher.BeginInvoke(() => OnTradeAsked(from, id));
        _matchmaking.TradeAskSent += to => Dispatcher.BeginInvoke(() => SetStatus("Asked " + to + " to trade. Waiting for an answer."));
        _matchmaking.TradeAskFailed += (to, message) => Dispatcher.BeginInvoke(() => OnTradeAskFailed(to, message));
        _matchmaking.TradeDeclined += by => Dispatcher.BeginInvoke(() => OnTradeDeclined(by));
        _matchmaking.TradeOpened += (sessionId, partner) => Dispatcher.BeginInvoke(() => OnTradeOpened(sessionId, partner));
        _matchmaking.TradeStateReceived += (sessionId, mine, theirs, mineLocked, theirsLocked) =>
            Dispatcher.BeginInvoke(() => OnTradeState(sessionId, mine, theirs, mineLocked, theirsLocked));
        _matchmaking.TradeDone += sessionId => Dispatcher.BeginInvoke(() => OnTradeDone(sessionId));
        _matchmaking.TradeAborted += (sessionId, message) => Dispatcher.BeginInvoke(() => OnTradeAborted(sessionId, message));
        _matchmaking.TradeClosed += (sessionId, message) => Dispatcher.BeginInvoke(() => OnTradeClosed(sessionId, message));
        _matchmaking.ModeOfferReceived += (from, mode, id) => Dispatcher.BeginInvoke(() => OnModeOffer(from, mode, id));
        _matchmaking.ModeDeclined += message => Dispatcher.BeginInvoke(() => OnModeDeclined(message));
        _matchmaking.MatchSeated += () => Dispatcher.BeginInvoke(OnChallengeMatchSeated);
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
        EnsureDevLatinumButton();
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
                await _matchmaking.RequestFriendsAsync().ConfigureAwait(true);
            }
            else
            {
                _friendNames.Clear();
                _friendOnline.Clear();
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
        NotePeerDeckFromRoom();
        NoteFriendsSeenInLounge();
        if (CurrentMenu == MenuLevel.Crew)
        {
            if (_crewFriends && _signIn.IsAccount)
                _ = _matchmaking.RequestFriendsAsync();
            else
                ShowCrew();
        }
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
        var friendNote = FriendErrorGerman(state);
        if (friendNote != null)
            SetCrewNote(friendNote);
        SetMmState(state);
        if (_pickingMode && state.Contains("sandbox and account cannot play the same match.", StringComparison.Ordinal))
        {
            _modeNote = state;
            if (CurrentMenu == MenuLevel.ModeChoice)
                ShowModeChoiceButtons();
            return;
        }
        NoteInviteEnded(state);
        if (_menu.Contains(MenuLevel.ModeChoice) && _matchmaking is not { InRoom: true })
        {
            if (IsConnected)
                DisconnectInternal(null);
            _menu.Clear();
            _menu.Add(MenuLevel.Bridge);
            ApplyMenu(MenuLevel.Bridge);
            _ = RejoinLoungeAsync();
        }
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
            Margin = new Thickness(4, 2, 4, 4),
            Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x2A, 0x3A)),
            BorderBrush = isSelf ? (Brush)FindResource("SkinBrush_CyanAccent") : new SolidColorBrush(Color.FromRgb(0x3B, 0x57, 0x75)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(6)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var avatarBox = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x10, 0x1A, 0x24)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
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
        detailStack.SizeChanged += (_, _) =>
        {
            var h = detailStack.ActualHeight;
            if (h < 1 || (Math.Abs(avatarBox.Width - h) < 0.5 && Math.Abs(avatarBox.Height - h) < 0.5))
                return;
            avatarBox.Width = h;
            avatarBox.Height = h;
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
            Margin = new Thickness(0, 2, 0, 4),
            TextWrapping = TextWrapping.NoWrap
        };
        detailStack.Children.Add(statusBlock);

        var inPrivateChat = _privateChatMembers.Contains(name);
        var isInvited = _invitedPrivateChatUsers.Contains(name);
        var btnInviteChat = new Button
        {
            Content = inPrivateChat ? "IN CHAT" : isInvited ? "INVITED" : "INVITE TO CHAT",
            Style = (Style)FindResource("SkinStyle_ActionMiniButton"),
            Tag = name,
            IsEnabled = !isSelf && !inPrivateChat,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 2)
        };
        btnInviteChat.Click += (s, e) => InviteUserToPrivateChat(name, btnInviteChat);

        var btnTrade = new Button
        {
            Content = "TRADE",
            Style = (Style)FindResource("SkinStyle_ActionMiniButton"),
            Tag = name,
            IsEnabled = !isSelf,
            Margin = new Thickness(0, 0, 4, 0)
        };
        btnTrade.Click += (_, _) => _ = RequestAccountTradeAsync(name);

        var btnPlay = new Button
        {
            Content = "PLAY",
            Style = (Style)FindResource("SkinStyle_ActionMiniButton"),
            Tag = name,
            IsEnabled = !isSelf,
            Margin = new Thickness(4, 0, 0, 0)
        };
        btnPlay.Click += (s, e) => OpenMatchSelectionPrompt(name);

        var actionRow = new StackPanel { Orientation = Orientation.Horizontal };
        actionRow.Children.Add(btnTrade);
        actionRow.Children.Add(btnPlay);

        detailStack.Children.Add(btnInviteChat);
        detailStack.Children.Add(actionRow);

        Grid.SetColumn(detailStack, 1);
        grid.Children.Add(detailStack);

        border.Child = grid;
        return border;
    }

    private void InviteUserToPrivateChat(string targetUser, Button btn)
    {
        _ = SendPrivateChatInviteAsync(targetUser);
    }

    private async Task SendPrivateChatInviteAsync(string targetUser)
    {
        _activeChatTab = "Private";
        NotePrivateChat("[Asking " + targetUser + " to join the private chat.]");
        try
        {
            await _matchmaking.SendChatInviteAsync(targetUser).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _invitedPrivateChatUsers.Remove(targetUser);
            NotePrivateChat("[Could not invite " + targetUser + ": " + ex.Message + "]");
            SetStatus(ex.Message);
            RefreshCrewDock();
        }
    }

    private void OnPrivateChatInvite(string from, string inviteId)
    {
        var answer = BridgeDialog.Show(
            this,
            from + " invites you to a private chat. Accept?",
            "Private chat",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        _ = ReplyPrivateChatInviteAsync(inviteId, answer == MessageBoxResult.Yes);
    }

    private async Task ReplyPrivateChatInviteAsync(string inviteId, bool accept)
    {
        try
        {
            await _matchmaking.ReplyChatInviteAsync(inviteId, accept).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void OnPrivateChatInviteSent(string to)
    {
        if (!string.IsNullOrWhiteSpace(to))
            _invitedPrivateChatUsers.Add(to);
        _activeChatTab = "Private";
        NotePrivateChat("[Invited " + to + " to private chat. Waiting for an answer.]");
        SetStatus("Invited " + to + " to private chat. Waiting for an answer.");
        RefreshCrewDock();
    }

    private void OnPrivateChatInviteFailed(string to, string message)
    {
        if (!string.IsNullOrWhiteSpace(to))
            _invitedPrivateChatUsers.Remove(to);
        var why = string.IsNullOrWhiteSpace(message) ? "unavailable" : message;
        var who = string.IsNullOrWhiteSpace(to) ? "That player" : to;
        _activeChatTab = "Private";
        NotePrivateChat("[" + who + ": " + why + "]");
        SetStatus(who + ": " + why);
        RefreshCrewDock();
    }

    private void OnPrivateChatDeclined(string by)
    {
        var name = string.IsNullOrWhiteSpace(by) ? "The other player" : by;
        _invitedPrivateChatUsers.Remove(name);
        _activeChatTab = "Private";
        NotePrivateChat("[" + name + " declined the private chat.]");
        SetStatus(name + " declined the private chat.");
        RefreshCrewDock();
    }

    private void OnPrivateChatInviteClosed(string message)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "The private chat invite ended." : message;
        NotePrivateChat("[" + text + "]");
        SetStatus(text);
    }

    private void OnPrivateChatJoined(IReadOnlyList<string> names)
    {
        _privateChatMembers.Clear();
        foreach (var name in names)
        {
            if (!string.IsNullOrWhiteSpace(name))
                _privateChatMembers.Add(name);
        }
        foreach (var name in _privateChatMembers)
        {
            if (!IsSelfName(name))
                _invitedPrivateChatUsers.Add(name);
        }
        _activeChatTab = "Private";
        NotePrivateChat("[Private chat: " + string.Join(", ", _privateChatMembers) + "]");
        SetStatus("Private chat: " + string.Join(", ", _privateChatMembers));
        RefreshCrewDock();
    }

    private void OnPrivateChatLine(string from, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        var speaker = string.IsNullOrWhiteSpace(from) ? "Private" : from;
        NotePrivateChat(speaker + ": " + text);
    }

    private void OnPrivateChatEnded(string message)
    {
        _privateChatMembers.Clear();
        _invitedPrivateChatUsers.Clear();
        var text = string.IsNullOrWhiteSpace(message) ? "Private chat ended." : message;
        _activeChatTab = "Private";
        NotePrivateChat("[" + text + "]");
        SetStatus(text);
        RefreshCrewDock();
    }

    private void NotePrivateChat(string line)
    {
        if (!_chatHistories.ContainsKey("Private"))
            _chatHistories["Private"] = new List<string>();
        _chatHistories["Private"].Add(line);
        RefreshChatView();
    }

    private bool IsSelfName(string name)
        => string.Equals(name, _signIn.Name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, _matchmaking.DisplayName, StringComparison.OrdinalIgnoreCase);

    private void OpenMatchSelectionPrompt(string targetUser)
    {
        if (_matchmaking is not { InRoom: true } || !string.Equals(_matchmaking.RoomName, "Lounge", StringComparison.OrdinalIgnoreCase))
        {
            SetStatus("Challenge from the lounge.");
            return;
        }
        _ = SendChallengeAsync(targetUser);
    }

    private async Task SendChallengeAsync(string targetUser)
    {
        try
        {
            await _matchmaking.SendChallengeAsync(targetUser).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void OnInviteReceived(string from, string challengeId)
    {
        ShowChoicePrompt(
            from + " challenges you to a match. Accept?",
            () => _ = ReplyInviteAsync(challengeId, true),
            () => _ = ReplyInviteAsync(challengeId, false));
    }

    private void OnInviteAccepted(string by, string challengeId)
    {
        _openChallengeId = challengeId;
        _pickingMode = true;
        _modeNote = string.IsNullOrWhiteSpace(by) ? null : by + " accepted. Choose a match.";
        _menu.Clear();
        _menu.Add(MenuLevel.Bridge);
        _menu.Add(MenuLevel.ModeChoice);
        ApplyMenu(MenuLevel.ModeChoice);
    }

    private void OnModeOffer(string from, string mode, string challengeId)
    {
        _challengeMatchMode = mode;
        var label = string.Equals(mode, "account", StringComparison.Ordinal)
            ? "Official Match (Ranked)"
            : "Holodeck (Open Rules)";
        ShowChoicePrompt(
            from + " proposes " + label + ". Accept?",
            () => _ = ReplyModeAsync(challengeId, true),
            () => _ = ReplyModeAsync(challengeId, false));
    }

    private void OnModeDeclined(string message)
    {
        ClosePrompt();
        if (!_pickingMode)
            return;
        _modeNote = string.IsNullOrWhiteSpace(message) ? "Declined." : message;
        if (CurrentMenu != MenuLevel.ModeChoice)
        {
            _menu.Clear();
            _menu.Add(MenuLevel.Bridge);
            _menu.Add(MenuLevel.ModeChoice);
        }
        ApplyMenu(MenuLevel.ModeChoice);
    }

    private async Task ReplyInviteAsync(string challengeId, bool accept)
    {
        try
        {
            await _matchmaking.ReplyChallengeAsync(challengeId, accept).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private async Task ReplyModeAsync(string challengeId, bool accept)
    {
        try
        {
            await _matchmaking.ReplyModeAsync(challengeId, accept).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void OnChallengeMatchSeated()
    {
        ClosePrompt();
        _pickingMode = false;
        _openChallengeId = null;
        _modeNote = null;
        var ranked = string.Equals(_challengeMatchMode, "account", StringComparison.Ordinal);
        _challengeMatchMode = null;
        _holodeckDirect = !ranked;
        _menu.Clear();
        _menu.Add(MenuLevel.Bridge);
        _menu.Add(MenuLevel.MatchConsole);
        if (ranked)
            _ = ContinueSeatedMatchAsync(true);
        else
            OpenHolodeckDeckSelect();
    }

    private void OpenHolodeckDeckSelect()
    {
        OpenMatchLobby(ranked: false, targetRoomOrUser: _matchmaking.RoomName);
        _directGame = true;
        if (GameOptionsPanel != null)
            GameOptionsPanel.Visibility = Visibility.Collapsed;
        SetStatus("Holodeck. Pick a deck, then Engage. Direct game on TCP 7777. No points and no latinum.");
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
        else if (_activeChatTab == "Private")
        {
            if (_privateChatMembers.Count < 2)
                NotePrivateChat("[Nobody else is in the private chat.]");
            else
                _ = SendPrivateChatLineAsync(text);
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

    private async Task SendPrivateChatLineAsync(string text)
    {
        try
        {
            await _matchmaking.SendPrivateChatAsync(text).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            NotePrivateChat("[Private chat was not sent: " + ex.Message + "]");
            SetStatus(ex.Message);
        }
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


    private void LeaveMenuLevel(MenuLevel leaving)
    {
        if (leaving == MenuLevel.AccountDelete)
        {
            ClearDynamicView();
            return;
        }
        if (leaving == MenuLevel.ModeChoice)
        {
            ClearDynamicView();
            _modeNote = null;
            _pickingMode = false;
            _ = CancelOpenChallengeAsync();
            return;
        }
        if (leaving == MenuLevel.DeckBuilder)
        {
            HideDeckBuilder();
            return;
        }
        if (leaving == MenuLevel.CardShop)
        {
            _cardShop?.Unmount();
            ClearDynamicView();
            return;
        }
        if (leaving == MenuLevel.Collection)
        {
            _collection?.Unmount();
            ClearDynamicView();
            return;
        }
        if (leaving == MenuLevel.Trade)
        {
            var session = _tradeSessionId;
            var tell = !_tradeSealed && !string.IsNullOrEmpty(session);
            _tradeScreen?.Unmount();
            _tradeScreen = null;
            _tradeSessionId = null;
            _tradePartner = null;
            ClearDynamicView();
            _tradeSealed = false;
            if (tell)
                _ = _matchmaking.CancelTradeAsync(session!);
            return;
        }
        if (leaving is MenuLevel.PlayChoice or MenuLevel.Singleplayer or MenuLevel.Crew)
        {
            ClearDynamicView();
            return;
        }
        if (leaving == MenuLevel.MatchConsole)
        {
            _holodeckDirect = false;
            MatchLobbyGrid.Visibility = Visibility.Collapsed;
            if (CurrentMenu != MenuLevel.ModeChoice)
            {
                if (IsConnected)
                    DisconnectInternal("Returned to bridge overview.");
                _ = LeaveSeatedMatchAsync();
            }
        }
    }

    private void ApplyMenu(MenuLevel level)
    {
        switch (level)
        {
            case MenuLevel.Bridge:
                ShowNormalRail();
                ClearDynamicView();
                MatchLobbyGrid.Visibility = Visibility.Collapsed;
                break;
            case MenuLevel.Options:
                ShowOptionsRail();
                ShowDeveloperModeEntry();
                MatchLobbyGrid.Visibility = Visibility.Collapsed;
                break;
            case MenuLevel.AccountDelete:
                ShowOptionsRail();
                MatchLobbyGrid.Visibility = Visibility.Collapsed;
                ShowDeleteAccountButton();
                break;
            case MenuLevel.ModeChoice:
                ShowNormalRail();
                MatchLobbyGrid.Visibility = Visibility.Collapsed;
                ShowModeChoiceButtons();
                break;
            case MenuLevel.DeckBuilder:
                MatchLobbyGrid.Visibility = Visibility.Collapsed;
                _ = ShowDeckBuilderAsync();
                break;
            case MenuLevel.CardShop:
                ShowCardShop();
                break;
            case MenuLevel.Collection:
                ShowCollection(pages: false);
                break;
            case MenuLevel.CollectionBinder:
                ShowCollection(pages: true);
                break;
            case MenuLevel.Trade:
                ShowTrade();
                break;
            case MenuLevel.PlayChoice:
                ShowPlayChoice();
                break;
            case MenuLevel.Singleplayer:
                ShowSingleplayer();
                break;
            case MenuLevel.Crew:
                ShowCrew();
                break;
            case MenuLevel.MatchConsole:
                ClearDynamicView();
                MatchLobbyGrid.Visibility = Visibility.Visible;
                break;
        }
    }

    private void ShowNormalRail()
    {
        NavButtonGrid.Visibility = Visibility.Visible;
        PlayChoiceGrid.Visibility = Visibility.Collapsed;
        CrewRailGrid.Visibility = Visibility.Collapsed;
        BtnNavPlay.Content = "PLAY";
        BtnNavCollection.Visibility = Visibility.Visible;
        BtnNavDecks.Visibility = Visibility.Visible;
        BtnNavCareer.Visibility = Visibility.Visible;
        BtnNavOptions.Visibility = Visibility.Visible;
    }

    private void ShowOptionsRail()
    {
        NavButtonGrid.Visibility = Visibility.Visible;
        PlayChoiceGrid.Visibility = Visibility.Collapsed;
        CrewRailGrid.Visibility = Visibility.Collapsed;
        BtnNavPlay.Content = "ACCOUNT SETTINGS";
        BtnNavCollection.Visibility = Visibility.Hidden;
        BtnNavDecks.Visibility = Visibility.Hidden;
        BtnNavCareer.Visibility = Visibility.Hidden;
        BtnNavOptions.Visibility = Visibility.Hidden;
    }

    private void ClearDynamicView()
    {
        _cardShop?.Unmount();
        _collection?.Unmount();
        _tradeScreen?.Unmount();
        DynamicViewHost.Children.Clear();
    }

    private void ShowOnViewscreen(params Button[] buttons)
    {
        DynamicViewHost.Children.Clear();
        var row = new StackPanel
        {
            Orientation = buttons.Length > 1 ? Orientation.Horizontal : Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        foreach (var button in buttons)
            row.Children.Add(button);
        DynamicViewHost.Children.Add(row);
    }

    private Button MakeViewButton(string title, string subtitle, Brush titleBrush, RoutedEventHandler onClick)
    {
        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = titleBrush,
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        if (!string.IsNullOrEmpty(subtitle))
        {
            stack.Children.Add(new TextBlock
            {
                Text = subtitle,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0)
            });
        }
        var button = new Button
        {
            Content = stack,
            Width = 280,
            Height = 70,
            Margin = new Thickness(8, 0, 8, 0),
            Style = (Style)FindResource("SkinStyle_SciFiButton")
        };
        button.Click += onClick;
        return button;
    }


    private void NoteInviteEnded(string state)
    {
        var ended = state.Contains("declined", StringComparison.OrdinalIgnoreCase)
            || state.Contains("not in the lounge", StringComparison.OrdinalIgnoreCase)
            || state.Contains("already in a match", StringComparison.OrdinalIgnoreCase)
            || state.Contains("player left", StringComparison.OrdinalIgnoreCase)
            || state.Contains("cancelled", StringComparison.OrdinalIgnoreCase);
        if (!ended)
            return;
        ClosePrompt();
        var wasPicking = _pickingMode || _menu.Contains(MenuLevel.ModeChoice);
        _pickingMode = false;
        _openChallengeId = null;
        _modeNote = null;
        if (wasPicking)
        {
            _menu.Clear();
            _menu.Add(MenuLevel.Bridge);
            ApplyMenu(MenuLevel.Bridge);
        }
        if (MatchLobbyGrid.Visibility != Visibility.Visible
            && !string.Equals(state, "cancelled", StringComparison.Ordinal)
            && !string.Equals(state, "declined", StringComparison.Ordinal))
            ShowFlowText(state);
    }

    private void ShowFlowText(string text)
    {
        DynamicViewHost.Children.Clear();
        DynamicViewHost.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 520,
            Margin = new Thickness(24)
        });
    }

    private void EnsurePromptLayer()
    {
        if (_promptLayer != null)
            return;
        _promptLayer = new Grid { Visibility = Visibility.Collapsed };
        if (DynamicViewHost.Parent is Grid parent)
        {
            Panel.SetZIndex(_promptLayer, 20);
            parent.Children.Add(_promptLayer);
        }
    }

    private void ShowChoicePrompt(string text, Action onAccept, Action onDecline)
    {
        EnsurePromptLayer();
        if (_promptLayer == null)
            return;
        _promptDecline = onDecline;
        _promptOpen = true;
        _promptLayer.Children.Clear();
        _promptLayer.Background = new SolidColorBrush(Color.FromArgb(0x99, 0x03, 0x06, 0x0B));
        var accept = new Button
        {
            Content = "ACCEPT",
            Width = 140,
            Height = 36,
            Margin = new Thickness(8, 0, 8, 0),
            Style = (Style)FindResource("SkinStyle_SciFiButton")
        };
        var decline = new Button
        {
            Content = "DECLINE",
            Width = 140,
            Height = 36,
            Margin = new Thickness(8, 0, 8, 0),
            Style = (Style)FindResource("SkinStyle_SciFiButton")
        };
        accept.Click += (_, _) =>
        {
            ClosePrompt();
            onAccept();
        };
        decline.Click += (_, _) =>
        {
            ClosePrompt();
            onDecline();
        };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        buttons.Children.Add(accept);
        buttons.Children.Add(decline);
        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            MaxWidth = 460,
            Margin = new Thickness(0, 0, 0, 16)
        });
        stack.Children.Add(buttons);
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xE6, 0x0D, 0x18, 0x26)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x38, 0x4E, 0x68)),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(24, 18, 24, 18),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = stack
        };
        _promptLayer.Children.Add(card);
        _promptLayer.Visibility = Visibility.Visible;
    }

    private void ClosePrompt()
    {
        _promptOpen = false;
        _promptDecline = null;
        if (_promptLayer == null)
            return;
        _promptLayer.Children.Clear();
        _promptLayer.Visibility = Visibility.Collapsed;
    }

    private void DeclineOpenPrompt()
    {
        var decline = _promptDecline;
        ClosePrompt();
        decline?.Invoke();
    }

    private async Task CancelOpenChallengeAsync()
    {
        var id = _openChallengeId;
        _openChallengeId = null;
        if (string.IsNullOrEmpty(id))
            return;
        try
        {
            await _matchmaking.CancelChallengeAsync(id).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private void ShowModeChoiceButtons()
    {
        var gold = new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47));
        var cyan = (Brush)FindResource("SkinBrush_CyanAccent");
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        row.Children.Add(MakeViewButton("OFFICIAL MATCH (RANKED)", "Relay \u00b7 Account Card Pool \u00b7 Rated", gold, ModeChoiceRanked_Click));
        row.Children.Add(MakeViewButton("HOLODECK (OPEN RULES)", "Sandbox \u00b7 All Cards Unlocked \u00b7 Freeplay", cyan, ModeChoiceHolodeck_Click));
        var wrap = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (!string.IsNullOrWhiteSpace(_modeNote))
        {
            wrap.Children.Add(new TextBlock
            {
                Text = _modeNote,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 560,
                Margin = new Thickness(0, 0, 0, 16)
            });
        }
        wrap.Children.Add(row);
        DynamicViewHost.Children.Clear();
        DynamicViewHost.Children.Add(wrap);
    }

    private void ShowDeleteAccountButton()
    {
        var gold = new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47));
        ShowOnViewscreen(MakeViewButton("KONTO L\u00d6SCHEN", "Passwort zur Best\u00e4tigung", gold, DeleteAccount_Click));
    }

    private void ShowDeveloperModeEntry()
    {
        ClearDynamicView();
        if (DeveloperMode.IsEnabled)
        {
            DynamicViewHost.Children.Add(DeveloperModeNote("Entwicklermodus ist an. Er gilt in diesem Lauf fuer das Menue und die Partie."));
            return;
        }

        var gold = new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47));
        ShowOnViewscreen(MakeViewButton("ENTWICKLERMODUS", "Passwort erforderlich", gold, DeveloperModeButton_Click));
    }

    private void DeveloperModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (DeveloperMode.IsEnabled)
        {
            ShowDeveloperModeEntry();
            return;
        }

        var note = DeveloperModeNote("Passwort fuer den Entwicklermodus.");
        var box = new PasswordBox
        {
            Width = 280,
            Height = 32,
            Margin = new Thickness(0, 12, 0, 12),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var gold = new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47));
        var confirm = MakeViewButton("AKTIVIEREN", "", gold, ConfirmDeveloperPassword_Click);
        confirm.Tag = box;
        var wrap = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        wrap.Children.Add(note);
        wrap.Children.Add(box);
        wrap.Children.Add(confirm);
        DynamicViewHost.Children.Clear();
        DynamicViewHost.Children.Add(wrap);
    }

    private void ConfirmDeveloperPassword_Click(object sender, RoutedEventArgs e)
    {
        var box = (sender as Button)?.Tag as PasswordBox;
        var entered = box?.Password ?? "";
        if (box != null)
            box.Password = "";
        // Compared here only. Not written to a log, a status line, or the account database.
        if (!string.Equals(entered, "St1000312200614", StringComparison.Ordinal))
        {
            ShowDeveloperModeRejected();
            return;
        }

        DeveloperMode.Enable();
        ShowDeveloperModeEntry();
    }

    private void ShowDeveloperModeRejected()
    {
        ClearDynamicView();
        var gold = new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47));
        var wrap = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        wrap.Children.Add(DeveloperModeNote("Das Passwort ist falsch. Der Entwicklermodus bleibt aus."));
        wrap.Children.Add(MakeViewButton("ENTWICKLERMODUS", "Passwort erforderlich", gold, DeveloperModeButton_Click));
        DynamicViewHost.Children.Add(wrap);
    }

    private static TextBlock DeveloperModeNote(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 560,
            Margin = new Thickness(0, 0, 0, 16)
        };
    }

    // DEV-ONLY HOOK. Remove EnsureDevLatinumButton and DevGrantLatinum_Click for release.
    // The button is created only while DeveloperMode is on, so it is not in the tree when the flag is off.
    private void EnsureDevLatinumButton()
    {
        if (!DeveloperMode.IsEnabled || _devLatinumButton != null || ShopDock == null)
            return;

        _devLatinumNote = new TextBlock
        {
            Text = "",
            Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };
        DockPanel.SetDock(_devLatinumNote, Dock.Bottom);
        _devLatinumButton = new Button
        {
            Content = "500 LATINUM",
            Height = 32,
            Margin = new Thickness(0, 0, 0, 6),
            Style = (Style)FindResource("SkinStyle_SciFiButton")
        };
        _devLatinumButton.Click += DevGrantLatinum_Click;
        DockPanel.SetDock(_devLatinumButton, Dock.Bottom);
        var index = ShopDock.Children.IndexOf(BtnShopOpen);
        if (index < 0)
            index = ShopDock.Children.Count;
        ShopDock.Children.Insert(index, _devLatinumNote);
        ShopDock.Children.Insert(index + 1, _devLatinumButton);
    }

    // DEV-ONLY HOOK. One click asks the server to add 500 to this account's latinum. No other copy of this grant.
    private async void DevGrantLatinum_Click(object sender, RoutedEventArgs e)
    {
        if (!DeveloperMode.IsEnabled)
            return;
        if (!_signIn.IsAccount || string.IsNullOrWhiteSpace(_signIn.Token))
        {
            if (_devLatinumNote != null)
                _devLatinumNote.Text = "Nicht angemeldet. Es wurde kein Latinum gutgeschrieben.";
            return;
        }

        try
        {
            var res = await _matchmaking.DevGrantLatinumAsync(_signIn.Token).ConfigureAwait(true);
            var type = res.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (type == "latinum" && res.TryGetProperty("latinum", out var latEl) && latEl.TryGetInt32(out var n))
            {
                _signIn.Latinum = n;
                UpdateProfileUI();
                _cardShop?.SetLatinum(n);
                if (_devLatinumNote != null)
                    _devLatinumNote.Text = "500 Latinum gutgeschrieben.";
                return;
            }

            var message = res.TryGetProperty("message", out var messageEl) ? messageEl.GetString() : null;
            if (_devLatinumNote != null)
            {
                _devLatinumNote.Text = message is "login required" or "token required" or null or ""
                    ? "Nicht angemeldet. Es wurde kein Latinum gutgeschrieben."
                    : message;
            }
        }
        catch (Exception ex)
        {
            if (_devLatinumNote != null)
                _devLatinumNote.Text = ex.Message;
        }
    }

    private async void ModeChoiceRanked_Click(object sender, RoutedEventArgs e)
    {
        if (!string.Equals(_signIn.Mode, "account", StringComparison.Ordinal))
        {
            _modeNote = "Official Match needs an account login. Holodeck is sandbox.";
            ShowModeChoiceButtons();
            return;
        }
        await SendModeAsync("account").ConfigureAwait(true);
    }

    private async void ModeChoiceHolodeck_Click(object sender, RoutedEventArgs e)
    {
        await SendModeAsync("sandbox").ConfigureAwait(true);
    }

    private async Task SendModeAsync(string mode)
    {
        if (string.IsNullOrEmpty(_openChallengeId))
        {
            _modeNote = "No open invite.";
            ShowModeChoiceButtons();
            return;
        }
        try
        {
            await _matchmaking.SendModeAsync(_openChallengeId, mode).ConfigureAwait(true);
            _challengeMatchMode = mode;
            _modeNote = "Waiting for an answer.";
            ShowModeChoiceButtons();
        }
        catch (Exception ex)
        {
            _modeNote = ex.Message;
            ShowModeChoiceButtons();
        }
    }

    private async Task ContinueSeatedMatchAsync(bool ranked)
    {
        if (CurrentMenu != MenuLevel.MatchConsole)
            _menu.Add(MenuLevel.MatchConsole);
        OpenMatchLobby(ranked: ranked, targetRoomOrUser: _matchmaking.RoomName);
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

    private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
    {
        if (!_signIn.IsAccount || string.IsNullOrWhiteSpace(_signIn.Token))
        {
            Say("Niemand ist angemeldet. Das Konto wurde nicht gel\u00f6scht.");
            return;
        }
        var password = PromptAccountPassword();
        if (password == null)
            return;
        try
        {
            var res = await _matchmaking.DeleteAccountAsync(password).ConfigureAwait(true);
            var type = res.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (!string.Equals(type, "accountDeleted", StringComparison.Ordinal))
            {
                var message = res.TryGetProperty("message", out var msg) ? msg.GetString() : null;
                if (string.Equals(message, "wrong password", StringComparison.Ordinal))
                    Say("Das Passwort ist falsch. Das Konto wurde nicht gel\u00f6scht.");
                else if (string.IsNullOrWhiteSpace(message) || string.Equals(message, "login required", StringComparison.Ordinal))
                    Say("Niemand ist angemeldet. Das Konto wurde nicht gel\u00f6scht.");
                else
                    Say("Das Konto wurde nicht gel\u00f6scht. " + message);
                return;
            }
        }
        catch (Exception ex)
        {
            Say("Das Konto wurde nicht gel\u00f6scht. " + ex.Message);
            return;
        }

        _signIn = LobbySignIn.Sandbox();
        _menu.Clear();
        _menu.Add(MenuLevel.Bridge);
        ApplyMenu(MenuLevel.Bridge);
        UpdateProfileUI();
        Say("Konto gel\u00f6scht. Abgemeldet.");
        _ = AutoConnectLoungeAsync();
    }

    private string? PromptAccountPassword()
    {
        var box = new PasswordBox { Margin = new Thickness(0, 8, 0, 0) };
        var ok = new Button { Content = "L\u00f6schen", Width = 100, Margin = new Thickness(0, 12, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Abbrechen", Width = 100, Margin = new Thickness(0, 12, 0, 0), IsCancel = true };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = "Passwort zur Best\u00e4tigung eingeben.",
            TextWrapping = TextWrapping.Wrap,
            Width = 280
        });
        panel.Children.Add(box);
        panel.Children.Add(buttons);
        var dialog = new Window
        {
            Title = "Konto l\u00f6schen",
            Content = panel,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false
        };
        string? value = null;
        ok.Click += (_, _) =>
        {
            value = box.Password;
            dialog.DialogResult = true;
        };
        return dialog.ShowDialog() == true ? value : null;
    }

    private void Say(string text)
    {
        SetStatus(text);
        if (MatchLobbyGrid.Visibility != Visibility.Visible)
            BridgeDialog.Show(this, text, "Main Bridge", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task RejoinLoungeAsync()
    {
        try
        {
            if (_matchmaking is not { InRoom: true })
                await _matchmaking.JoinRoomAsync("Lounge").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private async Task LeaveSeatedMatchAsync()
    {
        if (_matchmaking is not { InRoom: true })
            return;
        if (string.Equals(_matchmaking.RoomName, "Lounge", StringComparison.OrdinalIgnoreCase))
            return;
        try
        {
            await _matchmaking.LeaveAsync().ConfigureAwait(true);
            for (var i = 0; i < 40 && _matchmaking.InRoom; i++)
                await Task.Delay(50).ConfigureAwait(true);
            if (_matchmaking is not { InRoom: true })
                await _matchmaking.JoinRoomAsync("Lounge").ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    public void OpenMatchLobby(bool ranked = true, string? targetRoomOrUser = null)
    {
        _isRankedMatch = ranked;
        _directGame = false;
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

        ClearDynamicView();
        MatchLobbyGrid.Visibility = Visibility.Visible;

        if (ranked)
        {
            if (_signIn.IsAccount)
            {
                BtnBrowseDeck.Content = "Upload";
                _ = LoadServerDecksAsync();
            }
            else
            {
                _serverDeckItems = new List<DeckListItem>();
                RefreshDeckList();
            }
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
        _menu.Clear();
        _menu.Add(MenuLevel.Bridge);
        ApplyMenu(MenuLevel.Bridge);
        _ = LeaveSeatedMatchAsync();
    }

    private void BtnLobbyBackToBridge_Click(object sender, RoutedEventArgs e)
    {
        BtnNavBack_Click(sender, e);
    }

    private void BtnNavBack_Click(object sender, RoutedEventArgs e)
    {
        if (_promptOpen)
        {
            DeclineOpenPrompt();
            return;
        }
        if (_menu.Count <= 1)
        {
            _menu.Clear();
            _menu.Add(MenuLevel.Bridge);
            ApplyMenu(MenuLevel.Bridge);
            return;
        }
        var leaving = CurrentMenu;
        _menu.RemoveAt(_menu.Count - 1);
        LeaveMenuLevel(leaving);
        ApplyMenu(CurrentMenu);
    }


    private void OpenCrew(bool friends)
    {
        _crewFriends = friends;
        _isFriendsMode = false;
        RefreshCrewDock();
        if (friends && _signIn.IsAccount)
            _ = _matchmaking.RequestFriendsAsync();
        if (CurrentMenu == MenuLevel.Crew)
        {
            ShowCrew();
            return;
        }
        _menu.Add(MenuLevel.Crew);
        ApplyMenu(MenuLevel.Crew);
    }

    private void ShowPlayRail()
    {
        NavButtonGrid.Visibility = Visibility.Collapsed;
        CrewRailGrid.Visibility = Visibility.Collapsed;
        PlayChoiceGrid.Visibility = Visibility.Visible;
    }

    private void ShowCrewRail()
    {
        NavButtonGrid.Visibility = Visibility.Collapsed;
        PlayChoiceGrid.Visibility = Visibility.Collapsed;
        CrewRailGrid.Visibility = Visibility.Visible;
    }

    private void ShowPlayChoice()
    {
        ShowPlayRail();
        MatchLobbyGrid.Visibility = Visibility.Collapsed;
        ClearDynamicView();
    }

    private void ShowSingleplayer()
    {
        ShowPlayRail();
        MatchLobbyGrid.Visibility = Visibility.Collapsed;
        ClearDynamicView();
        DynamicViewHost.Children.Add(new TextBlock
        {
            Text = "Die Kampagne wird hier sein.",
            Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24)
        });
    }

    private void ShowCrew()
    {
        ShowCrewRail();
        MatchLobbyGrid.Visibility = Visibility.Collapsed;
        ClearDynamicView();
        _crewNoteBlock = null;

        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var title = new TextBlock
        {
            Text = _crewFriends ? "FRIENDS" : "LOUNGE",
            Foreground = (Brush)FindResource("SkinBrush_GoldAccent"),
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var input = new TextBox
        {
            Width = 280,
            Height = 28,
            FontSize = 14,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        var add = new Button
        {
            Content = "ADD FRIEND",
            Style = (Style)FindResource("SkinStyle_SciFiButton"),
            Height = 32,
            MinWidth = 140
        };
        add.Click += (_, _) => _ = SendFriendByNameAsync(input.Text);
        var inputRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8)
        };
        inputRow.Children.Add(input);
        inputRow.Children.Add(add);
        Grid.SetRow(inputRow, 1);
        root.Children.Add(inputRow);

        _crewNoteBlock = new TextBlock
        {
            Text = _crewStatus,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(_crewNoteBlock, 2);
        root.Children.Add(_crewNoteBlock);

        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        if (_crewFriends)
        {
            if (_friendNames.Count == 0)
                wrap.Children.Add(CrewEmpty(_signIn.IsAccount ? "Noch keine Freunde." : "Niemand ist angemeldet."));
            else
            {
                for (var i = 0; i < _friendNames.Count; i++)
                {
                    wrap.Children.Add(CreateCrewCard(_friendNames[i], FriendIsOnline(_friendNames[i], i)));
                }
            }
        }
        else if (_onlinePlayers.Count == 0)
        {
            wrap.Children.Add(CrewEmpty("Niemand ist in der Lounge."));
        }
        else
        {
            foreach (var player in _onlinePlayers)
                wrap.Children.Add(CreateCrewCard(player.Name, true));
        }

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = wrap
        };
        Grid.SetRow(scroll, 3);
        root.Children.Add(scroll);
        DynamicViewHost.Children.Add(root);
    }

    private static TextBlock CrewEmpty(string text)
        => new()
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),
            FontSize = 14,
            FontStyle = FontStyles.Italic,
            Margin = new Thickness(4)
        };

    private Border CreateCrewCard(string name, bool online)
    {
        var isSelf = string.Equals(name, _signIn.Name, StringComparison.OrdinalIgnoreCase);
        var border = new Border
        {
            Width = 280,
            Margin = new Thickness(6),
            Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x2A, 0x3A)),
            BorderBrush = isSelf ? (Brush)FindResource("SkinBrush_CyanAccent") : new SolidColorBrush(Color.FromRgb(0x3B, 0x57, 0x75)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10)
        };
        var stack = new StackPanel();
        var avatar = new Border
        {
            Width = 96,
            Height = 96,
            Background = new SolidColorBrush(Color.FromRgb(0x10, 0x1A, 0x24)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 8),
            Child = new TextBlock
            {
                Text = name.Length > 0 ? name[..1].ToUpperInvariant() : "?",
                Foreground = new SolidColorBrush(Color.FromRgb(0xBA, 0xE6, 0xFD)),
                FontWeight = FontWeights.Bold,
                FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        stack.Children.Add(avatar);
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
        nameRow.Children.Add(new TextBlock
        {
            Text = isSelf ? name + " (YOU)" : name,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 16
        });
        nameRow.Children.Add(new Ellipse
        {
            Width = 8,
            Height = 8,
            Fill = online ? (Brush)FindResource("SkinBrush_GreenOnline") : (Brush)FindResource("SkinBrush_RedOffline"),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        stack.Children.Add(nameRow);

        var play = new Button
        {
            Content = "PLAY",
            Style = (Style)FindResource("SkinStyle_ActionMiniButton"),
            IsEnabled = !isSelf,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 8, 0)
        };
        play.Click += (_, _) => OpenMatchSelectionPrompt(name);
        var already = IsFriend(name);
        var friend = new Button
        {
            Content = already ? "REMOVE FRIEND" : "ADD FRIEND",
            Style = (Style)FindResource("SkinStyle_ActionMiniButton"),
            IsEnabled = !isSelf,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        };
        var captured = name;
        friend.Click += (_, _) => _ = already ? RemoveFriendAsync(captured) : SendFriendByNameAsync(captured);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(play);
        actions.Children.Add(friend);
        stack.Children.Add(actions);
        border.Child = stack;
        return border;
    }

    private bool IsFriend(string name)
        => _friendNames.Any(friend => string.Equals(friend, name, StringComparison.OrdinalIgnoreCase));

    private bool FriendIsOnline(string name, int index)
    {
        if (index >= 0 && index < _friendOnline.Count && _friendOnline[index])
            return true;
        foreach (var player in _onlinePlayers)
        {
            if (string.Equals(player.Name, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void NoteFriendsSeenInLounge()
    {
        if (CurrentMenu != MenuLevel.Crew || !_crewFriends)
            return;
        ShowCrew();
    }

    private void SetCrewNote(string text)
    {
        _crewStatus = text ?? "";
        if (_crewNoteBlock != null)
            _crewNoteBlock.Text = _crewStatus;
    }

    private async Task SendFriendByNameAsync(string? name)
    {
        var clean = (name ?? "").Trim();
        if (!_signIn.IsAccount)
        {
            SetCrewNote("Niemand ist angemeldet.");
            return;
        }
        if (clean.Length == 0)
        {
            SetCrewNote("Name fehlt.");
            return;
        }
        if (string.Equals(clean, _signIn.Name, StringComparison.OrdinalIgnoreCase))
        {
            SetCrewNote("Du kannst dich nicht selbst hinzufuegen.");
            return;
        }
        try
        {
            await _matchmaking.SendFriendRequestAsync(clean).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetCrewNote(ex.Message);
        }
    }

    private async Task RemoveFriendAsync(string name)
    {
        if (!_signIn.IsAccount)
        {
            SetCrewNote("Niemand ist angemeldet.");
            return;
        }
        try
        {
            await _matchmaking.RemoveFriendAsync(name).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetCrewNote(ex.Message);
        }
    }

    private void OnFriendsChanged(IReadOnlyList<string> names, IReadOnlyList<bool> online)
    {
        _friendNames.Clear();
        _friendOnline.Clear();
        if (names != null)
            _friendNames.AddRange(names);
        if (online != null)
            _friendOnline.AddRange(online);
        if (CurrentMenu == MenuLevel.Crew)
            ShowCrew();
    }

    private void OnFriendAsk(string from, string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return;
        _friendAskQueue.Enqueue((from ?? "", requestId));
        if (!_friendPromptOpen)
            DrainFriendAsks();
    }

    private void DrainFriendAsks()
    {
        if (_friendPromptOpen || _friendAskQueue.Count == 0)
            return;
        var ask = _friendAskQueue.Dequeue();
        _friendPromptOpen = true;
        var answer = BridgeDialog.Show(
            this,
            ask.From + " moechte dich als Freund hinzufuegen. Annehmen?",
            "Freundschaftsanfrage",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        _friendPromptOpen = false;
        if (answer == MessageBoxResult.Yes)
            _ = ReplyFriendAsync(ask.Id, true);
        else if (answer == MessageBoxResult.No)
            _ = ReplyFriendAsync(ask.Id, false);
        DrainFriendAsks();
    }

    private async Task ReplyFriendAsync(string requestId, bool accept)
    {
        try
        {
            await _matchmaking.ReplyFriendAsync(requestId, accept).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetCrewNote(ex.Message);
        }
    }

    private void OnFriendNotice(string notice)
    {
        notice ??= "";
        if (notice.StartsWith("friendRequestSent:", StringComparison.Ordinal))
            SetCrewNote("Anfrage an " + notice["friendRequestSent:".Length..] + " gesendet.");
        else if (notice.StartsWith("friendAccepted:", StringComparison.Ordinal))
            SetCrewNote(notice["friendAccepted:".Length..] + " ist jetzt ein Freund.");
        else if (notice.StartsWith("friendDeclined:", StringComparison.Ordinal))
            SetCrewNote("Abgelehnt. Nichts gespeichert.");
        else if (notice.StartsWith("friendRemoved:", StringComparison.Ordinal))
            SetCrewNote("Freund entfernt.");
        else if (notice.StartsWith("friendClosed:", StringComparison.Ordinal))
            CloseFriendPrompt();
    }

    private void CloseFriendPrompt()
    {
        _friendAskQueue.Clear();
        SetCrewNote("Die Anfrage ist weg. Nichts gespeichert.");
        if (!_friendPromptOpen)
            return;
        foreach (Window window in Application.Current.Windows)
        {
            if (window is BridgeDialogWindow)
                window.Close();
        }
    }

    private static string? FriendErrorGerman(string state)
    {
        return state switch
        {
            "unknown name" => "Unbekannter Name. Nichts gespeichert.",
            "player is offline" => "Dieser Spieler ist offline. Nichts gespeichert.",
            "login required" => "Niemand ist angemeldet.",
            "cannot add yourself" => "Du kannst dich nicht selbst hinzufuegen.",
            "already friends" => "Ihr seid bereits befreundet.",
            "name is not unique" => "Der Name ist nicht eindeutig. Nichts gespeichert.",
            "request already open" => "Die Anfrage ist schon offen.",
            "that player has no account" => "Dieser Spieler hat kein Konto. Nichts gespeichert.",
            "not friends" => "Das ist kein Freund.",
            "no such friend request" => "Die Anfrage gibt es nicht mehr. Nichts gespeichert.",
            "name required" => "Name fehlt.",
            _ => null
        };
    }

    private void BtnNavPlay_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentMenu is MenuLevel.Options or MenuLevel.AccountDelete)
        {
            if (CurrentMenu != MenuLevel.AccountDelete)
                _menu.Add(MenuLevel.AccountDelete);
            ApplyMenu(MenuLevel.AccountDelete);
            return;
        }
        if (CurrentMenu == MenuLevel.PlayChoice)
            return;
        _menu.Add(MenuLevel.PlayChoice);
        ApplyMenu(MenuLevel.PlayChoice);
    }

    private void BtnQuickLaunchGame_Click(object sender, RoutedEventArgs e)
    {
        LaunchGameTable();
    }

    private void LaunchGameTable()
    {
        var table = new TableWindow();
        table.RememberReturnMenu(_signIn);
        if (!string.IsNullOrWhiteSpace(_signIn.Name))
            table.SetSeatNames(_signIn.Name, null);
        table.Show();
        Close();
    }

    private void BtnNavCollection_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentMenu is MenuLevel.Collection or MenuLevel.CollectionBinder)
            return;
        _menu.Add(MenuLevel.Collection);
        ApplyMenu(MenuLevel.Collection);
    }

    private void ShowCollection(bool pages)
    {
        HideDeckBuilder();
        MatchLobbyGrid.Visibility = Visibility.Collapsed;
        ShowNormalRail();
        if (_collection == null)
        {
            _collection = new CollectionScreen();
            _collection.BinderChosen += Collection_BinderChosen;
        }
        if (!_collection.IsMounted)
        {
            _cardShop?.Unmount();
            DynamicViewHost.Children.Clear();
            _collection.Mount(DynamicViewHost);
            _collection.UseCatalog(ShopCatalog());
            _ = RefreshCollectionPoolAsync();
        }
        if (pages)
            _collection.ShowPages();
        else
            _collection.ShowShelf();
    }

    private void Collection_BinderChosen(string setKey)
    {
        if (CurrentMenu != MenuLevel.Collection)
            return;
        _menu.Add(MenuLevel.CollectionBinder);
        ApplyMenu(MenuLevel.CollectionBinder);
    }

    private async Task RefreshCollectionPoolAsync()
    {
        var screen = _collection;
        if (screen == null)
            return;
        if (!_signIn.IsAccount || string.IsNullOrWhiteSpace(_signIn.Token))
        {
            screen.SetOwnership(false, null, null);
            return;
        }
        try
        {
            var body = await _matchmaking.GetPoolAsync(_signIn.Token).ConfigureAwait(true);
            if (!ReferenceEquals(_collection, screen))
                return;
            var type = body.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (type != "pool")
            {
                var message = body.TryGetProperty("message", out var messageEl) ? messageEl.GetString() : "pool was not read";
                screen.SetOwnership(true, null, "Account pool was not read: " + (message ?? "pool was not read") + ".");
                return;
            }
            var owned = new Dictionary<string, int>(StringComparer.Ordinal);
            if (body.TryGetProperty("cards", out var cards) && cards.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in cards.EnumerateArray())
                {
                    var id = row.TryGetProperty("cardId", out var idEl) && idEl.ValueKind == JsonValueKind.String
                        ? idEl.GetString()
                        : null;
                    var qty = row.TryGetProperty("quantity", out var qtyEl) && qtyEl.TryGetInt32(out var n) ? n : 0;
                    if (!string.IsNullOrWhiteSpace(id) && qty > 0)
                        owned[id] = qty;
                }
            }
            screen.SetOwnership(true, owned, null);
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(_collection, screen))
                screen.SetOwnership(true, null, "Account pool was not read: " + ex.Message + ".");
        }
    }

    private void BtnNavDecks_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentMenu == MenuLevel.DeckBuilder)
            return;
        _menu.Add(MenuLevel.DeckBuilder);
        ApplyMenu(MenuLevel.DeckBuilder);
    }

    private void BtnNavCareer_Click(object sender, RoutedEventArgs e)
    {
        BridgeDialog.Show(this, "Career & Stats screen will open here.", "Career", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnNavOptions_Click(object sender, RoutedEventArgs e)
    {
        if (_promptOpen)
            DeclineOpenPrompt();
        if (_menu.Contains(MenuLevel.ModeChoice))
        {
            _pickingMode = false;
            _modeNote = null;
            _ = CancelOpenChallengeAsync();
        }
        if (CurrentMenu == MenuLevel.MatchConsole)
        {
            if (IsConnected)
                DisconnectInternal(null);
            _ = LeaveSeatedMatchAsync();
        }
        _menu.Clear();
        _menu.Add(MenuLevel.Bridge);
        _menu.Add(MenuLevel.Options);
        ApplyMenu(MenuLevel.Options);
    }

    private void BtnNavLounge_Click(object sender, RoutedEventArgs e)
    {
        OpenCrew(friends: false);
    }

    private void BtnNavFriends_Click(object sender, RoutedEventArgs e)
    {
        OpenCrew(friends: true);
    }

    private void BtnNavSingleplayer_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentMenu == MenuLevel.Singleplayer)
            return;
        _menu.Add(MenuLevel.Singleplayer);
        ApplyMenu(MenuLevel.Singleplayer);
    }

    private void BtnNavMultiplayer_Click(object sender, RoutedEventArgs e)
    {
        OpenCrew(friends: false);
    }

    private void BtnRailLounge_Click(object sender, RoutedEventArgs e)
    {
        OpenCrew(friends: false);
    }

    private void BtnRailFriends_Click(object sender, RoutedEventArgs e)
    {
        OpenCrew(friends: true);
    }

    private void BtnShopOpen_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentMenu == MenuLevel.CardShop)
            return;
        _menu.Add(MenuLevel.CardShop);
        ApplyMenu(MenuLevel.CardShop);
    }

    private void ShowCardShop()
    {
        HideDeckBuilder();
        MatchLobbyGrid.Visibility = Visibility.Collapsed;
        ShowNormalRail();
        if (_cardShop == null)
        {
            _cardShop = new CardShopScreen();
            _cardShop.BuyRequested += CardShop_BuyRequested;
        }
        ClearDynamicView();
        _cardShop.Mount(DynamicViewHost);
        _cardShop.SetLatinum(_signIn.Latinum);
    }

    private async void CardShop_BuyRequested(ShopPackSlot slot)
    {
        if (slot.PackType == null)
            return;
        if (!_signIn.IsAccount || string.IsNullOrWhiteSpace(_signIn.Token))
        {
            _cardShop?.ShowNote("Not logged in. No pack was bought.");
            return;
        }

        _cardShop?.SetBusy(true);
        try
        {
            var res = await _matchmaking.BuyPackAsync(_signIn.Token, slot.PackType).ConfigureAwait(true);
            if (CurrentMenu != MenuLevel.CardShop)
                return;
            var type = res.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
            if (type != "pack")
            {
                var message = res.TryGetProperty("message", out var messageEl) ? messageEl.GetString() : "Purchase failed.";
                _cardShop?.ShowNote(string.IsNullOrWhiteSpace(message) ? "Purchase failed." : message);
                return;
            }

            if (res.TryGetProperty("latinum", out var latEl) && latEl.TryGetInt32(out var latinum))
            {
                _signIn.Latinum = latinum;
                UpdateProfileUI();
                _cardShop?.SetLatinum(latinum);
            }

            var ids = new List<string>();
            if (res.TryGetProperty("cardIds", out var cardsEl) && cardsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var card in cardsEl.EnumerateArray())
                {
                    if (card.ValueKind == JsonValueKind.String && card.GetString() is { } id && id.Length > 0)
                        ids.Add(id);
                }
            }

            var poolNote = await ConfirmPoolHasCardsAsync(ids).ConfigureAwait(true);
            _cardShop?.SetLatinum(_signIn.Latinum);
            _cardShop?.ShowCards(ids, ShopCatalog());
            if (!string.IsNullOrEmpty(poolNote))
                _cardShop?.ShowNote(poolNote);
        }
        catch (Exception ex)
        {
            _cardShop?.ShowNote(ex.Message);
        }
        finally
        {
            _cardShop?.SetBusy(false);
        }
    }

    private Services.CardDatabase? ShopCatalog()
    {
        if (_shopCatalog != null)
            return _shopCatalog;
        try
        {
            var db = new Services.CardDatabase(GamePaths.DataRoot);
            db.LoadAll();
            _shopCatalog = db;
            return db;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> ConfirmPoolHasCardsAsync(IReadOnlyList<string> cardIds)
    {
        if (!_signIn.IsAccount || string.IsNullOrWhiteSpace(_signIn.Token))
            return "Not logged in. No pack was bought.";
        try
        {
            var body = await _matchmaking.GetPoolAsync(_signIn.Token).ConfigureAwait(true);
            var poolType = body.TryGetProperty("type", out var poolTypeEl) ? poolTypeEl.GetString() : null;
            if (poolType != "pool")
            {
                var message = body.TryGetProperty("message", out var messageEl) ? messageEl.GetString() : "pool was not read";
                return "Pack bought. Pool was not read: " + (message ?? "pool was not read");
            }
            if (body.TryGetProperty("latinum", out var latinum) && latinum.TryGetInt32(out var n))
            {
                _signIn.Latinum = n;
                UpdateProfileUI();
            }
            var owned = new HashSet<string>(StringComparer.Ordinal);
            if (body.TryGetProperty("cards", out var cards) && cards.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in cards.EnumerateArray())
                {
                    var id = row.TryGetProperty("cardId", out var idEl) && idEl.ValueKind == JsonValueKind.String
                        ? idEl.GetString()
                        : null;
                    if (!string.IsNullOrWhiteSpace(id))
                        owned.Add(id);
                }
            }
            var missing = cardIds.FirstOrDefault(id => !owned.Contains(id));
            return missing == null ? null : "The account pool does not list " + missing + ".";
        }
        catch (Exception ex)
        {
            return "Pack bought. Pool was not read: " + ex.Message;
        }
    }

    private async Task RequestAccountTradeAsync(string target)
    {
        if (!_signIn.IsAccount)
        {
            BridgeDialog.Show(this, "Log in to trade. Nothing moved.", "Trade", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (CurrentMenu == MenuLevel.Trade || !string.IsNullOrEmpty(_tradeSessionId))
        {
            SetStatus("A trade is already open.");
            return;
        }
        try
        {
            await _matchmaking.SendTradeAskAsync(target).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            BridgeDialog.Show(this, target + " is offline. Nothing moved.", "Trade", MessageBoxButton.OK, MessageBoxImage.Information);
            SetStatus(ex.Message);
        }
    }

    private void OnTradeAsked(string from, string requestId)
    {
        var answer = BridgeDialog.Show(
            this,
            from + " wants to trade cards. Accept?",
            "Trade",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        _ = ReplyTradeAsync(requestId, answer == MessageBoxResult.Yes);
    }

    private async Task ReplyTradeAsync(string requestId, bool accept)
    {
        try
        {
            await _matchmaking.ReplyTradeAskAsync(requestId, accept).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            BridgeDialog.Show(this, "The trade was not opened. Nothing moved.", "Trade", MessageBoxButton.OK, MessageBoxImage.Information);
            SetStatus(ex.Message);
        }
    }

    private void OnTradeAskFailed(string to, string message)
    {
        var text = string.IsNullOrWhiteSpace(message)
            ? (string.IsNullOrWhiteSpace(to) ? "The trade was not opened. Nothing moved." : to + " is offline. Nothing moved.")
            : message;
        BridgeDialog.Show(this, text, "Trade", MessageBoxButton.OK, MessageBoxImage.Information);
        SetStatus(text);
    }

    private void OnTradeDeclined(string by)
    {
        var text = (string.IsNullOrWhiteSpace(by) ? "The other player" : by) + " declined the trade. Nothing moved.";
        BridgeDialog.Show(this, text, "Trade", MessageBoxButton.OK, MessageBoxImage.Information);
        SetStatus(text);
    }

    private void OnTradeOpened(string sessionId, string partner)
    {
        _tradeSessionId = sessionId;
        _tradePartner = partner;
        _tradeSealed = false;
        _tradeMineLocked = false;
        _tradeTheirsLocked = false;
        _tradeMine = new[] { "", "", "", "" };
        _tradeTheirs = new[] { "", "", "", "" };
        if (CurrentMenu != MenuLevel.Trade)
        {
            _menu.Add(MenuLevel.Trade);
            ApplyMenu(MenuLevel.Trade);
        }
        else
        {
            ShowTrade();
        }
        SetStatus("Trading with " + partner + ".");
    }

    private void OnTradeState(string sessionId, string[] mine, string[] theirs, bool mineLocked, bool theirsLocked)
    {
        if (!string.IsNullOrEmpty(_tradeSessionId) && !string.Equals(_tradeSessionId, sessionId, StringComparison.Ordinal))
            return;
        _tradeMine = mine ?? new[] { "", "", "", "" };
        _tradeTheirs = theirs ?? new[] { "", "", "", "" };
        _tradeMineLocked = mineLocked;
        _tradeTheirsLocked = theirsLocked;
        _tradeScreen?.Apply(_tradeMine, _tradeTheirs, mineLocked, theirsLocked);
    }

    private void OnTradeDone(string sessionId)
    {
        if (!string.IsNullOrEmpty(_tradeSessionId) && !string.Equals(_tradeSessionId, sessionId, StringComparison.Ordinal))
            return;
        _tradeSealed = false;
        _tradeMineLocked = false;
        _tradeTheirsLocked = false;
        _tradeMine = new[] { "", "", "", "" };
        _tradeTheirs = new[] { "", "", "", "" };
        _tradeScreen?.ClearOffers();
        SetStatus("The trade moved both pools. The slots are clear.");
        _ = LoadTradeLibraryAsync();
    }

    private void OnTradeAborted(string sessionId, string message)
    {
        if (!string.IsNullOrEmpty(_tradeSessionId) && !string.Equals(_tradeSessionId, sessionId, StringComparison.Ordinal))
            return;
        var text = string.IsNullOrWhiteSpace(message) ? "The trade was aborted. Nothing moved." : message;
        LeaveTradeMenu();
        BridgeDialog.Show(this, text, "Trade", MessageBoxButton.OK, MessageBoxImage.Information);
        SetStatus(text);
    }

    private void OnTradeClosed(string sessionId, string message)
    {
        var mine = _tradeSessionId;
        if (!string.IsNullOrEmpty(mine) && !string.IsNullOrEmpty(sessionId) && !string.Equals(mine, sessionId, StringComparison.Ordinal))
            return;
        var self = string.Equals(message, "cancelled", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(mine))
            LeaveTradeMenu();
        if (self || string.IsNullOrWhiteSpace(message))
            return;
        BridgeDialog.Show(this, message, "Trade", MessageBoxButton.OK, MessageBoxImage.Information);
        SetStatus(message);
    }

    private void LeaveTradeMenu()
    {
        _tradeSealed = true;
        if (CurrentMenu == MenuLevel.Trade)
        {
            _menu.RemoveAt(_menu.Count - 1);
            if (_menu.Count == 0)
                _menu.Add(MenuLevel.Bridge);
            LeaveMenuLevel(MenuLevel.Trade);
            ApplyMenu(CurrentMenu);
            return;
        }
        _tradeScreen?.Unmount();
        _tradeScreen = null;
        _tradeSessionId = null;
        _tradePartner = null;
    }

    private void ShowTrade()
    {
        HideDeckBuilder();
        MatchLobbyGrid.Visibility = Visibility.Collapsed;
        ShowNormalRail();
        _ = LoadTradeLibraryAsync();
    }

    private async Task LoadTradeLibraryAsync()
    {
        var load = ++_tradeLoad;
        var session = _tradeSessionId;
        Dictionary<string, int>? owned = null;
        string? failure = null;
        if (!_signIn.IsAccount || string.IsNullOrWhiteSpace(_signIn.Token))
            failure = "Not logged in.";
        else
        {
            try
            {
                var body = await _matchmaking.GetPoolAsync(_signIn.Token).ConfigureAwait(true);
                if (load != _tradeLoad || CurrentMenu != MenuLevel.Trade || !string.Equals(_tradeSessionId, session, StringComparison.Ordinal))
                    return;
                var type = body.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
                if (type != "pool")
                {
                    failure = body.TryGetProperty("message", out var messageEl) ? messageEl.GetString() : "pool was not read";
                    failure ??= "pool was not read";
                }
                else
                {
                    owned = new Dictionary<string, int>(StringComparer.Ordinal);
                    if (body.TryGetProperty("cards", out var cards) && cards.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var row in cards.EnumerateArray())
                        {
                            var id = row.TryGetProperty("cardId", out var idEl) && idEl.ValueKind == JsonValueKind.String
                                ? idEl.GetString()
                                : null;
                            var qty = row.TryGetProperty("quantity", out var qtyEl) && qtyEl.TryGetInt32(out var q) ? q : 0;
                            if (!string.IsNullOrWhiteSpace(id) && qty > 0)
                                owned[id] = qty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                failure = ex.Message;
            }
        }
        if (load != _tradeLoad || CurrentMenu != MenuLevel.Trade || !string.Equals(_tradeSessionId, session, StringComparison.Ordinal))
            return;
        _cardShop?.Unmount();
        _collection?.Unmount();
        _tradeScreen?.Unmount();
        DynamicViewHost.Children.Clear();
        var screen = new AccountTradeScreen();
        screen.SlotsChanged += cards => _ = SendTradeSlotsAsync(cards);
        screen.AcceptPressed += cards => _ = LockTradeAsync(cards);
        _tradeScreen = screen;
        screen.Mount(DynamicViewHost);
        screen.UseLibrary(ShopCatalog(), owned ?? new Dictionary<string, int>(StringComparer.Ordinal));
        screen.Apply(_tradeMine, _tradeTheirs, _tradeMineLocked, _tradeTheirsLocked);
        if (!string.IsNullOrWhiteSpace(failure))
            SetStatus("Account pool was not read: " + failure + " No cards were invented.");
    }

    private async Task SendTradeSlotsAsync(string[] cards)
    {
        var session = _tradeSessionId;
        if (string.IsNullOrEmpty(session) || _tradeSealed)
            return;
        try
        {
            await _matchmaking.SendTradeSlotsAsync(session, cards).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
        }
    }

    private async Task LockTradeAsync(string[] cards)
    {
        var session = _tradeSessionId;
        if (string.IsNullOrEmpty(session) || _tradeSealed)
            return;
        try
        {
            await _matchmaking.SendTradeSlotsAsync(session, cards).ConfigureAwait(true);
            await _matchmaking.SendTradeLockAsync(session).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message);
            _tradeScreen?.Apply(_tradeMine, _tradeTheirs, _tradeMineLocked, _tradeTheirsLocked);
        }
    }

    // =========================================================================
    // MATCHMAKING & TRANSPORT LOGIC
    // =========================================================================

﻿    private void MainMenu_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (CurrentMenu == MenuLevel.DeckBuilder)
            _deckBuilder?.HandlePreviewKey(e);
    }

    private async Task ShowDeckBuilderAsync()
    {
        HideDeckBuilder();
        NavButtonGrid.Visibility = Visibility.Collapsed;
        PlayChoiceGrid.Visibility = Visibility.Collapsed;
        CrewRailGrid.Visibility = Visibility.Collapsed;
        NavRailSeparator.Visibility = Visibility.Collapsed;
        LoungeButtonStack.Visibility = Visibility.Collapsed;
        LeftRailColumn.Width = new GridLength(300);
        DeckFilterHost.Visibility = Visibility.Visible;
        DeckListColumn.Width = new GridLength(260);
        DeckListHost.Visibility = Visibility.Visible;
        BottomSocialGrid.Visibility = Visibility.Collapsed;
        DeckActionHost.Visibility = Visibility.Visible;
        RightFeatureGrid.Visibility = Visibility.Collapsed;
        DeckDetailHost.Visibility = Visibility.Visible;
        MatchLobbyGrid.Visibility = Visibility.Collapsed;
        AccountDeckBridge? bridge = null;
        if (_signIn.IsAccount && !string.IsNullOrWhiteSpace(_signIn.Token))
        {
            try
            {
                bridge = await BuildAccountDeckBridgeAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                SetStatus("Account decks: " + ex.Message);
            }
        }
        if (CurrentMenu != MenuLevel.DeckBuilder)
        {
            HideDeckBuilder();
            return;
        }
        var builder = new DeckBuilderWindow(bridge);
        _deckBuilder = builder;
        ClearDynamicView();
        builder.HostIn(DeckFilterHost, DeckListHost, DynamicViewHost, DeckDetailHost, DeckActionHost, DeckZoomHost);
    }

    private void HideDeckBuilder()
    {
        if (_deckBuilder != null)
        {
            _deckBuilder.Detach();
            try { _deckBuilder.Close(); } catch { /* not shown */ }
            _deckBuilder = null;
        }
        if (DeckFilterHost != null)
            DeckFilterHost.Visibility = Visibility.Collapsed;
        if (DeckListHost != null)
        {
            DeckListHost.Children.Clear();
            DeckListHost.Visibility = Visibility.Collapsed;
        }
        if (DeckListColumn != null)
            DeckListColumn.Width = new GridLength(0);
        if (DeckActionHost != null)
        {
            DeckActionHost.Children.Clear();
            DeckActionHost.Visibility = Visibility.Collapsed;
        }
        if (DeckDetailHost != null)
        {
            DeckDetailHost.Children.Clear();
            DeckDetailHost.Visibility = Visibility.Collapsed;
        }
        if (DeckZoomHost != null)
            DeckZoomHost.Children.Clear();
        if (NavButtonGrid != null)
            NavButtonGrid.Visibility = Visibility.Visible;
        if (NavRailSeparator != null)
            NavRailSeparator.Visibility = Visibility.Visible;
        if (LoungeButtonStack != null)
            LoungeButtonStack.Visibility = Visibility.Visible;
        if (LeftRailColumn != null)
            LeftRailColumn.Width = new GridLength(156);
        if (BottomSocialGrid != null)
            BottomSocialGrid.Visibility = Visibility.Visible;
        if (RightFeatureGrid != null)
            RightFeatureGrid.Visibility = Visibility.Visible;
    }

    private async Task<AccountDeckBridge> BuildAccountDeckBridgeAsync()
    {
        var token = _signIn.Token ?? "";
        var body = await _matchmaking.GetPoolAsync(token).ConfigureAwait(true);
        var pool = new Dictionary<string, int>(StringComparer.Ordinal);
        if (body.TryGetProperty("cards", out var cards) && cards.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in cards.EnumerateArray())
            {
                var id = row.TryGetProperty("cardId", out var idEl) && idEl.ValueKind == JsonValueKind.String ? idEl.GetString() : "";
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                var qty = row.TryGetProperty("quantity", out var q) && q.TryGetInt32(out var n) ? n : 0;
                pool[id] = qty;
            }
        }
        if (body.TryGetProperty("latinum", out var latinum) && latinum.TryGetInt32(out var lat))
        {
            _signIn.Latinum = lat;
            UpdateProfileUI();
        }
        return new AccountDeckBridge(
            pool,
            async (name, cardIds) =>
            {
                var saved = await _matchmaking.SaveDeckAsync(token, name, cardIds).ConfigureAwait(true);
                if (saved.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "error")
                {
                    var message = saved.TryGetProperty("message", out var msg) ? msg.GetString() : "Deck was not saved.";
                    return string.IsNullOrWhiteSpace(message) ? "Deck was not saved." : message;
                }
                return null;
            },
            async () =>
            {
                var list = await _matchmaking.ListDecksAsync(token).ConfigureAwait(true);
                var decks = new List<(string Name, int Count)>();
                if (list.TryGetProperty("decks", out var rows) && rows.ValueKind == JsonValueKind.Array)
                {
                    foreach (var row in rows.EnumerateArray())
                    {
                        var name = row.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : "";
                        if (string.IsNullOrWhiteSpace(name))
                            continue;
                        var count = row.TryGetProperty("count", out var c) && c.TryGetInt32(out var k) ? k : 0;
                        decks.Add((name, count));
                    }
                }
                return decks;
            },
            async name =>
            {
                var got = await _matchmaking.GetDeckAsync(token, name).ConfigureAwait(true);
                if (!got.TryGetProperty("cardIds", out var cardIds) || cardIds.ValueKind != JsonValueKind.Object)
                    return null;
                return DeckCardList.ToDeck(name, cardIds);
            });
    }

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
            _ = ResendDeckAfterConnectAsync();
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
                if (pick.Count > 0)
                    _peerDeckCount = pick.Count;
                if (IsHost && !string.IsNullOrWhiteSpace(pick.DeckJson))
                    _peerDeckJson = pick.DeckJson;
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
            AccountMatch = _isRankedMatch && !_directGame && _signIn.IsAccount,
            Player1Name = KnownSeatNames().P1,
            Player2Name = KnownSeatNames().P2,
        };

        LaunchTable(startArgs);
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
            LobbyPort = _servicePort,
            Player1Name = KnownSeatNames().P1,
            Player2Name = KnownSeatNames().P2,
        };

        LaunchTable(startArgs);
    }

    private async Task SendLobbyAsync(NetMessage message)
    {
        var ct = _cts?.Token ?? CancellationToken.None;
        // Deck announce and Engage ready share this socket. Overlapped writes
        // corrupt the first frame, so the other side never sees ready and
        // Engage looks like it did nothing until a second click.
        await _lobbySend.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_link != null)
                await _link.SendAsync(message, ct).ConfigureAwait(false);
            else if (_server != null)
                await _server.SendAsync(message, ct).ConfigureAwait(false);
            else if (_client != null)
                await _client.SendAsync(message, ct).ConfigureAwait(false);
        }
        finally
        {
            _lobbySend.Release();
        }
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
        if (_signIn.IsAccount && _isRankedMatch)
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
            _localDeckCount = deck.TotalCards;
            _localReady = false;
            if (deck.TotalCards < 1)
            {
                _localDeckJson = null;
                _localDeckName = null;
                _localDeckCount = 0;
                BtnStartGame.IsEnabled = false;
                UpdateLobbyUi();
                SetStatus("Deck has no cards.");
                return;
            }

            BtnStartGame.IsEnabled = true;
            SetStatus($"Deck selected: {_localDeckName}");
            await PublishSelectedDeckAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _localDeckPath = null;
            _localDeckJson = null;
            _localDeckName = null;
            _localDeckCount = 0;
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
        if (_isRankedMatch)
            _directGame = false;
        else if (_holodeckDirect)
            _directGame = true;
        if (!await EnsureMatchTransportAsync().ConfigureAwait(true))
            return;
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
        if (_isRankedMatch)
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
            var you = string.IsNullOrWhiteSpace(_localDeckName)
                ? "You: not ready (no deck)"
                : _localReady
                    ? $"You: ready — {_localDeckName} ({_localDeckCount})"
                    : $"You: selected {_localDeckName} ({_localDeckCount})";
            LobbyReadyText.Text = you;

            var peerLabel = IsHost ? "Guest (P2)" : "Host (P1)";
            string peerDeck;
            if (string.IsNullOrWhiteSpace(_peerDeckName))
                peerDeck = "no deck yet — not ready";
            else if (_peerDeckCount > 0)
                peerDeck = "selected " + _peerDeckName + " (" + _peerDeckCount.ToString(CultureInfo.InvariantCulture) + ")"
                    + (_peerReady ? " — ready" : "");
            else
                peerDeck = "selected " + _peerDeckName + (_peerReady ? " — ready" : "");
            LobbyPeerText.Text = peerLabel + ": " + peerDeck;
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
        _peerDeckCount = 0;
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
            {
                SetNatStatus("Localhost only. No router port forward.");
                await PublishMatchmakingAddressAsync(port).ConfigureAwait(true);
            }
            else if (_holodeckDirect)
            {
                // LAN address of this PC, port 7777. Not the account server port and not loopback.
                await PublishHolodeckAddressAsync(port).ConfigureAwait(true);
            }
            else
            {
                await AnnouncePortForwardAsync(port, ct).ConfigureAwait(true);
                await PublishMatchmakingAddressAsync(port).ConfigureAwait(true);
            }

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
        => await OpenDirectGameAsync().ConfigureAwait(true);

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

    private async Task PublishHolodeckAddressAsync(int listenPort)
    {
        if (listenPort is < 1 or > 65535)
            listenPort = 7777;
        if (_matchmaking is not { InRoom: true } || !string.Equals(_matchmaking.Role, "host", StringComparison.Ordinal))
            return;
        var host = ReachableLanIPv4(_serviceHost);
        if (string.IsNullOrWhiteSpace(host))
        {
            if (IsLoopbackHost(_serviceHost))
                host = "127.0.0.1";
            else
            {
                SetStatus("No LAN address for direct game. The other PC cannot use 127.0.0.1.");
                SetMmState("No LAN address for direct game.");
                return;
            }
        }
        if (PortBox != null)
            PortBox.Text = listenPort.ToString(CultureInfo.InvariantCulture);
        if (HostBox != null)
            HostBox.Text = host;
        try
        {
            await _matchmaking.SendAddressAsync(host, listenPort).ConfigureAwait(true);
            var note = "Direct game " + host + ":" + listenPort.ToString(CultureInfo.InvariantCulture) + ". Waiting for the guest.";
            SetStatus(note);
            SetMmState(note);
        }
        catch (Exception ex)
        {
            SetStatus("Could not publish the direct address (" + ex.Message + ").");
            SetMmState("Could not publish the address (" + ex.Message + "). The host is still listening.");
        }
    }

    private static bool IsLoopbackHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return true;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;
        return IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip);
    }

    private static string? ReachableLanIPv4(string remoteHost)
    {
        if (IsLoopbackHost(remoteHost))
            return null;
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(remoteHost, 7788);
            if (socket.LocalEndPoint is IPEndPoint ep
                && ep.Address.AddressFamily == AddressFamily.InterNetwork
                && !IPAddress.IsLoopback(ep.Address)
                && !ep.Address.Equals(IPAddress.Any))
                return ep.Address.ToString();
        }
        catch
        {
            // Fall through to an address on the same subnet as the account server.
        }

        if (!IPAddress.TryParse(remoteHost, out var remote) || remote.AddressFamily != AddressFamily.InterNetwork)
            return null;
        var remoteBytes = remote.GetAddressBytes();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up)
                continue;
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;
            foreach (var unicast in ni.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                var local = unicast.Address.GetAddressBytes();
                if (local.Length != 4 || remoteBytes.Length != 4)
                    continue;
                if (local[0] == remoteBytes[0] && local[1] == remoteBytes[1] && local[2] == remoteBytes[2])
                    return unicast.Address.ToString();
            }
        }
        return null;
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
        string? cards = _isRankedMatch && _signIn.IsAccount ? null : DeckCardList.FromDeckJson(_localDeckJson);
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
                DeckName = _localDeckName,
                Count = _localDeckCount,
                DeckJson = IsHost ? null : _localDeckJson
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
            _localDeckCount = deck.TotalCards;
            _localReady = false;
            if (deck.TotalCards < 1)
            {
                _localDeckJson = null;
                _localDeckName = null;
                _localDeckCount = 0;
                BtnStartGame.IsEnabled = false;
                UpdateLobbyUi();
                SetStatus("Account deck has no cards.");
                return;
            }
            BtnStartGame.IsEnabled = true;
            SetStatus("Account deck: " + _localDeckName);
            await PublishSelectedDeckAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _localDeckPath = null;
            _localDeckJson = null;
            _localDeckName = null;
            _localDeckCount = 0;
            _localReady = false;
            BtnStartGame.IsEnabled = false;
            UpdateLobbyUi();
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
        if (_isRankedMatch && !string.Equals(mode, _signIn.Mode, StringComparison.Ordinal))
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

    private void NotePeerDeckFromRoom()
    {
        if (MatchLobbyGrid == null || MatchLobbyGrid.Visibility != Visibility.Visible)
            return;
        var mine = _matchmaking.Role ?? "";
        if (mine.Length == 0)
            return;
        foreach (var player in _onlinePlayers)
        {
            if (string.IsNullOrWhiteSpace(player.DeckName))
                continue;
            if (string.Equals(player.Role, mine, StringComparison.Ordinal))
                continue;
            if (!string.Equals(_peerDeckName, player.DeckName, StringComparison.Ordinal))
                _peerDeckCount = 0;
            _peerDeckName = player.DeckName;
            UpdateLobbyUi();
            return;
        }
    }

    private async Task PublishSelectedDeckAsync()
    {
        UpdateLobbyUi();
        if (BtnStartGame != null)
            BtnStartGame.IsEnabled = !string.IsNullOrWhiteSpace(_localDeckJson);
        try
        {
            await SendDeckHashIfAnyAsync().ConfigureAwait(true);
            await SendExistingDeckPickAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _localDeckPath = null;
            _localDeckJson = null;
            _localDeckName = null;
            _localDeckCount = 0;
            _localReady = false;
            if (BtnStartGame != null)
                BtnStartGame.IsEnabled = false;
            UpdateLobbyUi();
            SetStatus("Deck was not stored for the match: " + ex.Message);
        }
    }

    private async Task ResendDeckAfterConnectAsync()
    {
        try
        {
            await SendDeckHashIfAnyAsync().ConfigureAwait(true);
            await SendExistingDeckPickAsync().ConfigureAwait(true);
            UpdateLobbyUi();
        }
        catch (Exception ex)
        {
            SetStatus("Deck was not stored for the match: " + ex.Message);
        }
    }

    private async Task<bool> EnsureMatchTransportAsync()
    {
        if (_isRankedMatch || !_directGame)
        {
            if (_matchmaking is not { InRoom: true } || string.Equals(_matchmaking.RoomName, "Lounge", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Join the match room before Start game.");
                return false;
            }
            if (VersionBlocks(out var why))
            {
                SetStatus(why);
                return false;
            }
            if (string.IsNullOrWhiteSpace(_matchmaking.PlayerId) || string.IsNullOrWhiteSpace(_matchmaking.RoomName))
            {
                SetStatus("The room has no player id yet.");
                return false;
            }
            var asHost = string.Equals(_matchmaking.Role, "host", StringComparison.Ordinal);
            await RunRelayAsync(_serviceHost, _servicePort, _matchmaking.RoomName, asHost, _matchmaking.PlayerId).ConfigureAwait(true);
            if (_link is not RelayNetLink { IsConnected: true })
            {
                SetStatus("Relay did not connect. Ranked and the default start stay on the server relay.");
                return false;
            }
            return true;
        }

        if (IsConnected && _link == null && (_server != null || _client != null))
            return true;
        await OpenDirectGameAsync().ConfigureAwait(true);
        if (IsConnected && _link == null && (_server != null || _client != null))
            return true;
        var dial = string.IsNullOrWhiteSpace(_matchmaking.DirectHost) ? "no address" : _matchmaking.DirectHost + ":7777";
        SetStatus("Direct game did not connect (" + dial + ").");
        return false;
    }

    private async Task OpenDirectGameAsync()
    {
        if (_isRankedMatch)
        {
            SetStatus("Official Match does not offer a direct game.");
            return;
        }
        if (PortBox != null)
            PortBox.Text = "7777";
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

        if (!await WaitForDirectAddressAsync().ConfigureAwait(true))
        {
            SetMmState("The host has not published a LAN address yet.");
            return;
        }

        var dialHost = (_matchmaking.DirectHost ?? "").Trim();
        if (HostBox != null)
            HostBox.Text = dialHost;
        if (PortBox != null)
            PortBox.Text = "7777";
        await RunJoinAsync().ConfigureAwait(true);
    }

    private async Task<bool> WaitForDirectAddressAsync()
    {
        if (DirectAddressUsable())
            return true;
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnAddress(string host, int port)
        {
            if (DirectAddressUsable())
                done.TrySetResult(true);
        }
        _matchmaking.AddressAnnounced += OnAddress;
        try
        {
            if (DirectAddressUsable())
                return true;
            SetStatus("Waiting for the host to open direct game on TCP 7777.");
            var finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(30))).ConfigureAwait(true);
            return finished == done.Task && DirectAddressUsable();
        }
        finally
        {
            _matchmaking.AddressAnnounced -= OnAddress;
        }
    }

    private bool DirectAddressUsable()
    {
        var host = (_matchmaking.DirectHost ?? "").Trim();
        if (host.Length == 0 || _matchmaking.DirectPort is < 1 or > 65535)
            return false;
        // Two PCs on the account LAN cannot dial the host's loopback.
        if (IsLoopbackHost(host) && !IsLoopbackHost(_serviceHost))
            return false;
        if (_matchmaking.DirectPort == _servicePort && _servicePort != 7777)
            return false;
        return true;
    }

    private void BtnGameOptions_Click(object sender, RoutedEventArgs e)
    {
        if (GameOptionsPanel == null || GameOptionsBody == null)
            return;
        if (GameOptionsPanel.Visibility == Visibility.Visible)
        {
            GameOptionsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        GameOptionsBody.Children.Clear();
        if (_isRankedMatch)
        {
            GameOptionsBody.Children.Add(new TextBlock
            {
                Text = "Offizielles Match laeuft ueber das Server-Relay (TCP 7788). Direct game gibt es hier nicht.",
                Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            GameOptionsBody.Children.Add(new TextBlock
            {
                Text = "Holodeck kann Direct game (TCP 7777) waehlen. Keine Punkte und keine Belohnung. Der normale Start bleibt das Relay.",
                Foreground = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            var direct = new Button
            {
                Content = "Direct game",
                Height = 28,
                MinWidth = 140,
                Margin = new Thickness(0, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            direct.Click += async (_, _) =>
            {
                _directGame = true;
                GameOptionsPanel.Visibility = Visibility.Collapsed;
                SetStatus("Direct game. No points and no rewards.");
                await OpenDirectGameAsync().ConfigureAwait(true);
            };
            var relay = new Button
            {
                Content = "Server relay",
                Height = 28,
                MinWidth = 140,
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            relay.Click += async (_, _) =>
            {
                _directGame = false;
                GameOptionsPanel.Visibility = Visibility.Collapsed;
                SetStatus("Server relay. Start game uses the room on TCP 7788.");
                await EnsureMatchTransportAsync().ConfigureAwait(true);
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(direct);
            row.Children.Add(relay);
            GameOptionsBody.Children.Add(row);
        }
        GameOptionsPanel.Visibility = Visibility.Visible;
    }


    private (string P1, string P2) KnownSeatNames()
    {
        var local = (_signIn.Name ?? "").Trim();
        if (local.Length == 0)
            local = (_matchmaking?.DisplayName ?? "").Trim();
        var peer = "";
        var myRole = _matchmaking?.Role ?? "";
        foreach (var player in _onlinePlayers)
        {
            if (string.IsNullOrWhiteSpace(player.Name))
                continue;
            if (myRole.Length > 0 && string.Equals(player.Role, myRole, StringComparison.OrdinalIgnoreCase))
                continue;
            if (local.Length > 0 && string.Equals(player.Name, local, StringComparison.OrdinalIgnoreCase))
                continue;
            peer = player.Name.Trim();
            break;
        }
        return IsHost ? (local, peer) : (peer, local);
    }

    private void LaunchTable(LobbyGameStartArgs startArgs)
    {
        var (server, client, link) = DetachTransport();
        var table = new TableWindow();
        table.RememberReturnMenu(_signIn);
        var hostAddr = HostBox?.Text;
        var portVal = int.TryParse(PortBox?.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 7777;
        var started = false;
        RoutedEventHandler kick = null!;
        kick = (_, _) =>
        {
            if (started)
                return;
            started = true;
            table.Loaded -= kick;
            table.StartNetSessionFromLobby(server, client, link, startArgs.IsHost, hostAddr, portVal, startArgs);
        };
        table.Loaded += kick;
        table.Show();
        Close();
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
