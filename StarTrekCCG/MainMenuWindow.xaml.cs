using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using StarTrekCCG.Network;

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

    public MainMenuWindow()
        : this(LobbySignIn.Sandbox())
    {
    }

    public MainMenuWindow(LobbySignIn signIn)
    {
        _signIn = signIn ?? LobbySignIn.Sandbox();
        InitializeComponent();

        _matchmaking.PlayerListChanged += players => Dispatcher.BeginInvoke(() => OnLobbyPlayerListChanged(players));
        _matchmaking.ChatChanged += chat => Dispatcher.BeginInvoke(() => OnLobbyChatReceived(chat));
        _matchmaking.StateChanged += state => Dispatcher.BeginInvoke(() => OnLobbyStateChanged(state));
    }

    private void MainMenuWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateProfileUI();
        RefreshCrewDock();
        RefreshChatView();
        _ = AutoConnectLoungeAsync();
    }

    private async Task AutoConnectLoungeAsync()
    {
        var host = string.IsNullOrWhiteSpace(_signIn.Host) ? "127.0.0.1" : _signIn.Host;
        var port = _signIn.Port is > 0 and <= 65535 ? _signIn.Port : 7788;
        var name = string.IsNullOrWhiteSpace(_signIn.Name) ? (_signIn.IsAccount ? "AccountPlayer" : "Cadet") : _signIn.Name;

        try
        {
            await _matchmaking.ConnectAsync(host, port, name, _signIn.Mode, _signIn.Token).ConfigureAwait(true);
            await _matchmaking.JoinRoomAsync("Lounge").ConfigureAwait(true);
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
            // Do not list ourselves in the crew dock if preferred, or display with SELF badge
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

        // Avatar icon placeholder
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

        // Details + Buttons
        var detailStack = new StackPanel
        {
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };

        // Name & Online Dot
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

        // Buttons: INVITE TO CHAT, TRADE, PLAY
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
        // When clicking PLAY on a crew member, prompt for game mode or configure challenge
        var choice = MessageBox.Show(
            this,
            $"Challenge {targetUser} to a match?\n\nClick YES for 'Official Match (Ranked)' or NO for 'Holodeck (Open Rules)'.",
            $"Challenge {targetUser}",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (choice == MessageBoxResult.Yes)
        {
            LaunchNetworkLobby(ranked: true);
        }
        else if (choice == MessageBoxResult.No)
        {
            LaunchNetworkLobby(ranked: false);
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

    private void BtnNavPlay_Click(object sender, RoutedEventArgs e)
    {
        LaunchGameTable();
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
        LaunchNetworkLobby();
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
            await Task.Delay(500);
            var remaining = (_signIn.Latinum ?? 100) - 50;
            return PackBuyResult.Bought(remaining, new[] { "Premiere Rare #1", "Premiere Uncommon #2" });
        });
        booster.Owner = this;
        booster.ShowDialog();
    }

    private void BtnNavBack_Click(object sender, RoutedEventArgs e)
    {
        // Resets viewscreen to base bridge overview
        MainScreenContentHost.Content = null;
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

    private void BtnLaunchRankedMatch_Click(object sender, RoutedEventArgs e)
    {
        LaunchNetworkLobby(ranked: true);
    }

    private void BtnLaunchHolodeckMatch_Click(object sender, RoutedEventArgs e)
    {
        LaunchNetworkLobby(ranked: false);
    }

    private void LaunchNetworkLobby(bool ranked = true)
    {
        var lobby = new NetworkLobbyWindow(_signIn, ranked);
        lobby.Owner = this;
        lobby.ShowDialog();
    }

    private void LaunchTradeWindow()
    {
        var trade = new TradeWindow(
            (to, give, ask) => Task.FromResult(default(System.Text.Json.JsonElement)),
            () => Task.FromResult(default(System.Text.Json.JsonElement)),
            id => Task.FromResult(default(System.Text.Json.JsonElement)),
            id => Task.FromResult(default(System.Text.Json.JsonElement)),
            () => Task.FromResult(default(System.Text.Json.JsonElement))
        );
        trade.Owner = this;
        trade.ShowDialog();
    }
}
