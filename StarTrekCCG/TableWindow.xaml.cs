using Microsoft.Win32;
using StarTrekCCG.Models;
using StarTrekCCG.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static StarTrekCCG.BattleRules;

namespace StarTrekCCG;

public partial class TableWindow : Window
{
    private CardDatabase? _db;
    private readonly DeckService _deckService = new();
    private Deck? _loadedDeck;
    private Deck? _loadedDeckOpp;

    // Laufende Spieler-Stapel (Sandbox) – Spieler 1 (unten beim Start)
    private readonly List<Card> _handCards = new();
    // Seed-Unterstapel nach Regelbuch-Phasen
    private readonly List<Card> _doorwayCards = new();
    private readonly List<Card> _missionSeedCards = new();
    private readonly List<Card> _dilemmaSeedCards = new();
    private readonly List<Card> _artifactSeedCards = new();
    private readonly List<Card> _facilitySeedCards = new(); // Facility, Ship, Personnel, Event, …
    private readonly List<Card> _seedCards = new();   // Legacy/Rest nach Seed-Ende
    private readonly List<Card> _drawCards = new();
    private readonly List<Card> _sideCards = new(); // Legacy
    private readonly List<Card> _qsTentCards = new();
    private readonly List<Card> _battleBridgeCards = new();
    private readonly List<Card> _qContinuumCards = new();
    private readonly List<Card> _sitePileCards = new();
    private readonly List<Card> _tribbleCards = new();
    private readonly List<Card> _discardCards = new();

    // Hotseat: Spieler 2 (oben beim Start; bei Zug unten bedienbar)
    private readonly List<Card> _oppHandCards = new();
    private readonly List<Card> _oppDrawCards = new();
    private readonly List<Card> _oppDiscardCards = new();
    private readonly List<Card> _oppSeedCards = new(); // ungelegter Seed / Rest
    private readonly List<Card> _oppQsTentCards = new();
    private readonly List<Card> _oppBattleBridgeCards = new();
    private readonly List<Card> _oppQContinuumCards = new();
    private readonly List<Card> _oppSitePileCards = new();
    private readonly List<Card> _oppTribbleCards = new();
    private readonly List<Card> _oppSideCards = new();
    private readonly List<Card> _oppDoorwayCards = new();
    private readonly List<Card> _oppMissionSeedCards = new();
    private readonly List<Card> _oppDilemmaSeedCards = new();
    private readonly List<Card> _oppFacilitySeedCards = new();

    /// <summary>Table-Karte → Besitzer (1 oder 2). Missionen = 0 (neutral).</summary>
    private readonly Dictionary<Border, int> _borderOwner = new();

    private const int OpeningHandSize = 7;
    private bool _seedPhaseActive;
    private Card? _detailCard;
    /// <summary>Host opened via double-click stack popup (ship/mission/facility).</summary>
    private Border? _detailHost;

    private enum GameMode { Hotseat, Network, SingleAi }
    private GameMode _gameMode = GameMode.Hotseat;
    private readonly GameSession _session = new();
    private BitmapImage? _cardBackImage;
    private int _activePlayer = 1; // 1 = unten (Startspieler), 2 = oben
    private int _turnNumber = 1;

    private SeedSubPhase _seedSubPhase = SeedSubPhase.Doorway;

    /// <summary>Geordnete Missionen pro Quadrant (Spaceline-Reihenfolge links→rechts).</summary>
    private readonly Dictionary<string, List<Border>> _missionsByQuadrant = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Eine Spaceline, links→rechts (Quadranten durch 1 Kartenbreite getrennt).</summary>
    private readonly List<Border> _spacelineOrder = new();
    /// <summary>Second copy of a unique mission stacked on the first (shared location).</summary>
    private readonly Dictionary<Border, List<Border>> _sharedMissionCopies = new();
    private readonly List<Rectangle> _missionSlotPreviews = new();

    private enum SeedSubPhase
    {
        Doorway = 0,
        Mission = 1,
        Dilemma = 2,   // Dilemma- + Artifact-Karten (getrennte Stapel, gleiche Phase)
        Facility = 3,
        Done = 4
    }

    private bool _isPanning;
    private Point _panStart;
    private double _scrollStartH;
    private double _scrollStartV;

    private Border? _dragCard;
    private Point _dragOffset;
    private Point _dragOrigin;           // Position vor dem Ziehen
    private Border? _dragSourceMission; // Mission, unter der die Karte lag (falls)
    private bool _isDragging;           // true erst nach Bewegungsschwelle
    private Point _mouseDownScreen;     // für Schwelle
    private const double DragThreshold = 6; // Pixel, bevor echtes Ziehen beginnt
    private const double BadgeAreaHeight = 22; // Platz unter der Karte für Crew/Eq-Badge

    private Rectangle? _selectionFrame; // Rahmen um ausgewählte Karte inkl. Badge
    private Border? _selectedCard;
    private StackPanel? _actionPanel;
    private enum CardActionMode { None, BeamPickTarget, FlyPickMission, AttackPickTarget, PersonnelAttackPick, EventPickTarget }
    private CardActionMode _cardActionMode = CardActionMode.None;
    private Border? _actionSourceHost;
    /// <summary>Host chosen by drag before event resolve (or click in EventPickTarget mode).</summary>
    private Border? _eventPreferredHost;
    private Border? _eventPreferredHost2;
    private readonly HashSet<Border> _beamSelected = new();
    private readonly HashSet<Border> _solvedMissions = new();
    /// <summary>Mission-Border → Spieler, der gelöst hat.</summary>
    private readonly Dictionary<Border, int> _missionSolver = new();
    private readonly Dictionary<Border, TextBlock> _missionSolvedLabels = new();
    private int _scoreP1;
    private int _scoreP2;

    /// <summary>HULL-Schaden in % (0 / 50 / 100) – Rotation Damage.</summary>
    private readonly Dictionary<Border, int> _hullDamagePercent = new();
    /// <summary>Gestoppte Schiffe/Personal (bis Start des nächsten Zugs).</summary>
    private readonly HashSet<Border> _stoppedBorders = new();
    /// <summary>Visuelle Damage-Overlays (Rotate / Badge).</summary>
    private readonly Dictionary<Border, TextBlock> _damageBadges = new();
    /// <summary>
    /// Volle eigene Züge, die ein beschädigtes Schiff ununterbrochen an einem
    /// eigenen Repair-Facility (Outpost) verbracht hat. Ab 2 → Repair.
    /// </summary>
    private readonly Dictionary<Border, int> _repairTurnsAtOutpost = new();


    private readonly List<Rectangle> _targetHighlights = new();
    private Rectangle? _turnTintP1;
    private Rectangle? _turnTintP2;
    private TextBlock? _turnWatermark;


    // Drag aus dem rechten Stapel-Panel (Host oder Zone)
    private HostCardRef? _panelDragRef;
    private ZoneCardRef? _zoneDragRef;
    private bool _panelDragging;
    private Rectangle? _snapPreview;    // Ziel-Position
    private Rectangle? _originPreview;  // alte Position der Karte

    // Digitale Stapel: Host-Border → enthaltene Karten-Borders (sichtbar=false)
    private readonly Dictionary<Border, List<Border>> _stackOnHost = new();
    /// <summary>Verbleibende RANGE pro Schiff-Border in diesem Zug.</summary>
    private readonly Dictionary<Border, int> _shipRangeLeft = new();
    // Verdeckt unter Mission (Dilemmas/Artifacts – Typ oft unbekannt für Gegner)
    private readonly Dictionary<Border, List<Border>> _seedUnderMission = new();
    /// <summary>Artifacts revealed mid-attempt (face-up for both players); acquired only on solve.</summary>
    private readonly Dictionary<Border, List<Card>> _revealedArtifactsUnderMission = new();
    /// <summary>Last dilemma faced at this mission (for View contents).</summary>
    private readonly Dictionary<Border, Card> _lastEncounteredDilemma = new();
    /// <summary>Visible Borg Ship dilemma token on the spaceline (self-controlling).</summary>
    private Border? _borgShipToken;

    /// <summary>Rogue Borg interrupt tokens aboard ships (self-controlling until Lore Returns).</summary>
    private sealed class RogueBorgUnit
    {
        public required Card Card { get; init; }
        public required Border Host { get; set; }
        public int PlayedBy { get; init; }
        /// <summary>0 = self-controlling; 1/2 = under that player's control (Lore Returns).</summary>
        public int Controller { get; set; }
        public Border? Visual { get; set; }
    }
    private readonly List<RogueBorgUnit> _rogueBorg = new();
    /// <summary>Ship borders with active Crosis (doubles Rogue Borg STRENGTH until start of next turn).</summary>
    private readonly HashSet<Border> _crosisShips = new();
    // Badge unter dem Host (Crew/Eq)
    private readonly Dictionary<Border, TextBlock> _hostBadges = new();
    private readonly Dictionary<Border, TextBlock> _hostBadgesP2 = new();
    // Badge für verdeckte Seed-Karten unter Mission
    private readonly Dictionary<Border, TextBlock> _seedBadges = new();
    /// <summary>Developer: show names/images of cards under missions.</summary>
    private bool _devRevealSeed;
    /// <summary>Developer: show the seed-count badge on missions.</summary>
    private bool _devShowSeedCounts;
    /// <summary>Developer: inspect opponent Hand/Draw/Seed/Side (normally private).</summary>
    private bool _devPeekOpponentPiles;

    // Player aids (Options menu) — on by default
    private bool _aidLegalResponses = true;
    private bool _aidOwnSeedCounts = true;
    private bool _devShowDebugLog = true;
    /// <summary>Dev: play Dilemma/Artifact from hand onto a mission (artifacts acquire immediately).</summary>
    private bool _devPlaySeedFromHand;
    private bool _aidZoneCounts = true;

    // ---------- Nur noch der zoombare Mittelbereich ----------
    // Alle Karten auf dem Spielfeld gleiche Größe
    private const double TableCardWidth = 100;
    private const double TableCardHeight = 140;
    private const double MissionGap = 20;
    private const double SpacelineStartX = 200;
    private const double SpacelineY = 520;       // Alpha-Spaceline (Default)
    private const double ShipYPlayer = 700;
    private const double ShipYOpponent = 300;
    // Platz für Facility unter Mission + mehrere Schiffe untereinander
    private const double UnderMissionGap = 175;
    private const double ShipSnapRange = 280; // großzügiger für Host-/Mission-Snap

    private Border? _hostHighlight; // Umrandung am Ziel-Host während Drag
    private Border? _zoneHighlightTarget; // Side-Deck-Zone während Doorway-Drag
    private Thickness _zoneHighlightPrevThickness;
    private Brush? _zoneHighlightPrevBrush;
    private const double MissionSlotGap = 16;

    // Stapel-Größe – volle Beschriftung (Doorways, Missions, …)
    // Kartenformat ~ 5:7 (wie TableCard 100×140)
    private const double ZoneW = 72;
    private const double ZoneH = 100;

    /// <summary>Side-Deck-Name → Doorway-Karte als Deckblatt (entsperrt).</summary>
    private readonly Dictionary<string, Card> _sideDeckCovers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Card> _oppSideDeckCovers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Card> _oppTablePermanentCards = new();

    /// <summary>Karten die fest "am Tisch" liegen (Doorways ohne Cover, Events, Objectives, …).</summary>
    private readonly List<Card> _tablePermanentCards = new();

    private readonly TimingRules.ActionStack _stack = new();
    private readonly List<Card> _outOfPlayP1 = new();
    private readonly List<Card> _outOfPlayP2 = new();
    private sealed class AttachedDilemma
    {
        public required Card Card { get; init; }
        public required DilemmaRules.PersistKind Kind { get; init; }
        public int Countdown { get; set; }
        public required Border Host { get; init; }
        public Card? Extra { get; set; }
    }

    private readonly List<AttachedDilemma> _attachedDilemmas = new();

    /// <summary>During a mission attempt: cards discarded from this location (for Temporal Causality Loop).</summary>
    private Border? _attemptMission;
    private readonly List<(Card card, int owner, Border? returnHost, bool wasSeed)> _attemptDiscards = new();
    /// <summary>Borg Ship dilemma token direction along spaceline (+1 / -1).</summary>
    private int _borgShipDir = 1;

    private enum RevealButtons { Ok, YesNo, PlayerPick }
    private enum RevealAnswer { None, Ok, Yes, No }
    private RevealAnswer _revealAnswer = RevealAnswer.None;
    private System.Windows.Threading.DispatcherFrame? _revealFrame;
    /// <summary>True if the last ShowCardReveal closed because the timer fired.</summary>
    private bool _revealTimedOut;

    /// <summary>Non-modal action announce: Commit pause → overlay → Respond/Pass.</summary>
    private enum AnnounceKind { Hidden, OkOnly, RespondOrPass, PickCard }
    private AnnounceKind _announceKind = AnnounceKind.Hidden;
    private System.Windows.Threading.DispatcherTimer? _commitDelayTimer;
    private System.Windows.Threading.DispatcherTimer? _announceDeadlineTimer;
    private DateTime _announceDeadlineUtc;

    /// <summary>Horga'hn permanent auf dem Tisch pro Spieler.</summary>
    private bool _horgahnP1, _horgahnP2;
    /// <summary>Zusätzliche Normal-Play durch Horga'hn in diesem Zug bereits verbraucht.</summary>
    private bool _horgahnExtraPlayUsed;

    private sealed class AttachedEvent
    {
        public required Card Card { get; init; }
        public required EventRules.Persist Kind { get; init; }
        public required int Owner { get; init; }
        public Border? Host { get; set; }
        public Border? Host2 { get; set; }
        public int Countdown { get; set; }
        public bool FaceUp { get; set; } = true;
        public string? EspionageAs { get; init; }
        public string? EspionageOn { get; init; }
        public int? TravelerPlayer { get; set; }

        /// <summary>Compendium turn wording: next / every / each / owner's next.</summary>
        public TimingRules.TurnScope TurnScope { get; set; } = TimingRules.TurnScope.EveryTurn;
        /// <summary>Start of turn vs end of turn trigger.</summary>
        public TimingRules.TurnPhasePoint PhasePoint { get; set; } = TimingRules.TurnPhasePoint.EndOfTurn;
        /// <summary>Player for SpecificPlayerNextTurn / EachSubjectTurn (owner/controller).</summary>
        public int? ScopePlayer { get; set; }
    }

    private readonly List<AttachedEvent> _attachedEvents = new();
    private int _ionizationBeamsThisTurn;
    private int _redAlertPlaysLeft;
    private readonly HashSet<Border> _movedThisTurnAfterArrival = new();

    /// <summary>true = Panel-Drag schwebt auf DragLayer (Fensterkoordinaten).</summary>
    private bool _dragOnOverlay;

    private static readonly Color ColDiscard = Color.FromRgb(140, 60, 60);
    private static readonly Color ColSeed = Color.FromRgb(160, 110, 40);   // Legacy nach Seed
    private static readonly Color ColDoorway = Color.FromRgb(120, 70, 160);
    private static readonly Color ColMission = Color.FromRgb(40, 100, 160);
    private static readonly Color ColDilemma = Color.FromRgb(180, 140, 40);
    private static readonly Color ColArtifact = Color.FromRgb(160, 100, 50);
    private static readonly Color ColFacility = Color.FromRgb(40, 140, 120);
    private static readonly Color ColDraw = Color.FromRgb(50, 120, 50);
    private static readonly Color ColSide = Color.FromRgb(70, 70, 140);
    private static readonly Color ColHand = Color.FromRgb(120, 100, 40);

    /// <summary>Set folders (cards.json + images). See GamePaths.</summary>
    private static string DataPath => GamePaths.DataRoot;

    /// <summary>Hotkey for Next phase / End turn. Custom binding later.</summary>
    private Key _hotkeyNextPhase = Key.Space;

    public TableWindow()
    {
        InitializeComponent();
        Loaded += TableWindow_Loaded;
        _session.Log.Changed = () => Dispatcher.BeginInvoke(RefreshActionHistory);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is TextBox) return;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape && CardDetailOverlay?.Visibility == Visibility.Visible)
        {
            CloseCardDetailPopup();
            e.Handled = true;
            return;
        }
        if (key != _hotkeyNextPhase) return;

        if (CardRevealOverlay?.Visibility == Visibility.Visible)
        {
            if (_announceKind == AnnounceKind.RespondOrPass || _announceKind == AnnounceKind.PickCard)
            {
                BtnRevealPass_Click(BtnRevealPass, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
            if (_announceKind == AnnounceKind.OkOnly)
            {
                BtnRevealOk_Click(BtnRevealOk, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
            // Modal Yes/No: don't steal Space
            if (BtnRevealYes?.Visibility == Visibility.Visible)
                return;
            CloseReveal(RevealAnswer.Ok);
            e.Handled = true;
            Dispatcher.BeginInvoke(new Action(() => TryHotkeyNextPhase()),
                System.Windows.Threading.DispatcherPriority.Input);
            return;
        }

        if (HistoryOverlay?.Visibility == Visibility.Visible) return;
        if (TeamOverlay?.Visibility == Visibility.Visible) return;
        if (KidnapOverlay?.Visibility == Visibility.Visible) return;

        bool handled = TryHotkeyNextPhase();
        if (handled) e.Handled = true;
    }

    /// <summary>Space (default): Next phase / End turn / Finish seed.</summary>
    private bool TryHotkeyNextPhase()
    {
        if (_stack.IsOpen && _announceKind != AnnounceKind.Hidden)
        {
            BtnRevealPass_Click(BtnRevealPass, new RoutedEventArgs());
            return true;
        }

        if (BtnPhaseNext != null && BtnPhaseNext.IsVisible && BtnPhaseNext.IsEnabled)
        {
            BtnPhaseNext_Click(BtnPhaseNext, new RoutedEventArgs());
            return true;
        }
        if (BtnEndTurn != null && BtnEndTurn.IsVisible && BtnEndTurn.IsEnabled)
        {
            BtnEndTurn_Click(BtnEndTurn, new RoutedEventArgs());
            return true;
        }
        if (BtnSeedFinish != null && BtnSeedFinish.IsVisible && BtnSeedFinish.IsEnabled)
        {
            BtnSeedFinish_Click(BtnSeedFinish, new RoutedEventArgs());
            return true;
        }
        return false;
    }

    private void BtnHistoryOpen_Click(object sender, RoutedEventArgs e)
    {
        RefreshActionHistory();
        if (HistoryOverlay != null)
            HistoryOverlay.Visibility = Visibility.Visible;
    }

    private void BtnHistoryClose_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryOverlay != null)
            HistoryOverlay.Visibility = Visibility.Collapsed;
    }

    private void BtnTeamClose_Click(object sender, RoutedEventArgs e)
    {
        if (TeamOverlay != null)
            TeamOverlay.Visibility = Visibility.Collapsed;
    }

    private string? _kidnapNamedType;
    private int _kidnapOwner;
    private bool _kidnapResolved;
    private bool _handPickMandatory;
    private Card? _handPickResult;
    private bool _handPickTimedOut;
    private List<Card> _kidnapHand = new();
    private System.Windows.Threading.DispatcherFrame? _kidnapFrame;
    private Border? _hoverPreview;

    private void RunKidnappers(int owner, Card source)
    {
        _kidnapOwner = owner;
        _kidnapNamedType = null;
        _kidnapResolved = false;
        _kidnapHand = (owner == 1 ? _oppHandCards : _handCards).ToList();
        if (KidnapOverlay == null || _kidnapHand.Count == 0)
        {
            StatusText.Text = "Kidnappers: opponent hand is empty.";
            return;
        }

        KidnapTitle.Text = "Telepathic Alien Kidnappers";
        KidnapHint.Text = "Name a card type. Then a random card is taken from the opponent's hand and revealed.";
        if (BtnKidnapCancel != null)
        {
            BtnKidnapCancel.Content = "Cancel";
            BtnKidnapCancel.Visibility = Visibility.Visible;
        }
        KidnapTypePanel.Children.Clear();
        foreach (var t in new[]
                 {
                     // Draw-deck / hand types only (Compendium: missions & dilemmas seed, not drawn)
                     "Personnel", "Ship", "Event", "Interrupt", "Equipment",
                     "Doorway", "Artifact", "Facility"
                 })
        {
            var btn = new Button
            {
                Content = t,
                Margin = new Thickness(0, 0, 3, 3),
                Padding = new Thickness(8, 4, 8, 4),
                Tag = t
            };
            btn.Click += (_, _) =>
            {
                _kidnapNamedType = t;
                foreach (var child in KidnapTypePanel.Children.OfType<Button>())
                    child.Opacity = (string)child.Tag == t ? 1 : 0.45;
                DrawRandomKidnapCard();
            };
            KidnapTypePanel.Children.Add(btn);
        }

        // Shuffle so even the back row is not in hand order
        var rng = new Random();
        _kidnapHand = _kidnapHand.OrderBy(_ => rng.Next()).ToList();
        KidnapCardsPanel.Children.Clear();
        foreach (var _ in _kidnapHand)
        {
            var face = new Border
            {
                Width = 70,
                Height = 98,
                Margin = new Thickness(4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 90)),
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false
            };
            var img = new Image { Stretch = Stretch.Uniform };
            if (_cardBackImage != null)
                img.Source = _cardBackImage;
            face.Child = img;
            KidnapCardsPanel.Children.Add(face);
        }

        KidnapOverlay.Visibility = Visibility.Visible;
        _kidnapFrame = new System.Windows.Threading.DispatcherFrame();
        try { System.Windows.Threading.Dispatcher.PushFrame(_kidnapFrame); }
        finally
        {
            _kidnapFrame = null;
            KidnapOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void DrawRandomKidnapCard()
    {
        if (_kidnapResolved) return;
        if (string.IsNullOrEmpty(_kidnapNamedType) || _kidnapHand.Count == 0)
            return;
        _kidnapResolved = true;

        var rng = new Random();
        int index = rng.Next(_kidnapHand.Count);
        FinishKidnappers(index);
    }

    private void FinishKidnappers(int index)
    {
        if (string.IsNullOrEmpty(_kidnapNamedType))
        {
            KidnapHint.Text = "Name a card type first.";
            return;
        }
        if (index < 0 || index >= _kidnapHand.Count) return;
        var card = _kidnapHand[index];
        bool match = (card.Type ?? "").Contains(_kidnapNamedType, StringComparison.OrdinalIgnoreCase);
        var oppHand = _kidnapOwner == 1 ? _oppHandCards : _handCards;

        KidnapCardsPanel.Children.Clear();
        var revealed = CreateMiniCard(card, faceDown: false);
        revealed.Width = 100;
        revealed.Height = 140;
        revealed.IsHitTestVisible = false;
        KidnapCardsPanel.Children.Add(revealed);

        if (match)
        {
            oppHand.Remove(card);
            var disc = _kidnapOwner == 1 ? _oppDiscardCards : _discardCards;
            if (!disc.Contains(card)) disc.Add(card);
            KidnapHint.Text =
                $"Named {_kidnapNamedType}. Revealed: {card.Name} ({card.Type}). MATCH — discarded.";
            StatusText.Text = $"Kidnappers: named {_kidnapNamedType} — discarded {card.Name}.";
            _session.Log.Add(_session.TurnNumber, $"P{_kidnapOwner}",
                $"Kidnappers named {_kidnapNamedType}, revealed {card.Name} — discarded");
        }
        else
        {
            KidnapHint.Text =
                $"Named {_kidnapNamedType}. Revealed: {card.Name} ({card.Type}). No match — stays in hand.";
            StatusText.Text =
                $"Kidnappers: named {_kidnapNamedType}, revealed {card.Name} ({card.Type}) — no discard.";
            _session.Log.Add(_session.TurnNumber, $"P{_kidnapOwner}",
                $"Kidnappers named {_kidnapNamedType}, revealed {card.Name} — no match");
        }
        RefreshZoneCounts();
        RefreshHandStrips();

        var ok = new Button
        {
            Content = "OK",
            Padding = new Thickness(16, 6, 16, 6),
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        ok.Click += (_, _) =>
        {
            if (_kidnapFrame != null) _kidnapFrame.Continue = false;
        };
        KidnapCardsPanel.Children.Add(ok);
    }

    private void BtnKidnapCancel_Click(object sender, RoutedEventArgs e)
    {
        if (_handPickMandatory)
        {
            AutoPickHandCard();
            return;
        }
        StatusText.Text = "Kidnappers cancelled.";
        if (_kidnapFrame != null) _kidnapFrame.Continue = false;
    }

    /// <summary>
    /// Show the owner's hand as clickable images. Mandatory discard (SWB):
    /// 10s timeout auto-picks and reveals the chosen card.
    /// </summary>
    private Card? PickHandCardToDiscard(int owner, string title, string hint)
    {
        var hand = owner == 1 ? _handCards : _oppHandCards;
        if (hand.Count == 0) return null;
        if (hand.Count == 1)
        {
            ShowCardReveal(hand[0], title, $"Only card in hand — discarding {hand[0].Name}.", RevealButtons.Ok);
            return hand[0];
        }

        _handPickMandatory = true;
        _handPickResult = null;
        _handPickTimedOut = false;
        _kidnapResolved = false;
        _kidnapOwner = owner;
        _kidnapHand = hand.ToList();

        KidnapTitle.Text = title;
        KidnapHint.Text = hint + "  Click a card.  10s with no choice → one is chosen for you.";
        KidnapTypePanel.Children.Clear();
        KidnapCardsPanel.Children.Clear();
        if (BtnKidnapCancel != null)
        {
            BtnKidnapCancel.Content = "Let the game choose";
            BtnKidnapCancel.Visibility = Visibility.Visible;
        }

        foreach (var c in _kidnapHand)
        {
            var mini = CreateMiniCard(c, faceDown: false);
            mini.Width = 96;
            mini.Height = 134;
            Card cardRef = c;
            mini.MouseLeftButtonDown += (_, ev) =>
            {
                if (_kidnapResolved) return;
                _kidnapResolved = true;
                _handPickResult = cardRef;
                if (_kidnapFrame != null) _kidnapFrame.Continue = false;
                ev.Handled = true;
            };
            KidnapCardsPanel.Children.Add(mini);
        }

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (!_kidnapResolved)
                AutoPickHandCard();
        };
        timer.Start();

        KidnapOverlay.Visibility = Visibility.Visible;
        _kidnapFrame = new System.Windows.Threading.DispatcherFrame();
        try { System.Windows.Threading.Dispatcher.PushFrame(_kidnapFrame); }
        finally
        {
            timer.Stop();
            _kidnapFrame = null;
            KidnapOverlay.Visibility = Visibility.Collapsed;
            _handPickMandatory = false;
            if (BtnKidnapCancel != null) BtnKidnapCancel.Content = "Cancel";
        }

        var chosen = _handPickResult;
        if (chosen != null)
        {
            string how = _handPickTimedOut ? "No choice in time — discarded" : "Discarded";
            ShowCardReveal(chosen, title, $"{how}: {chosen.Name}.", RevealButtons.Ok);
            _session.Log.Add(_session.TurnNumber, $"P{owner}",
                $"Static Warp Bubble discarded {chosen.Name}"
                + (_handPickTimedOut ? " (auto)" : ""));
        }
        return chosen;
    }

    private void AutoPickHandCard()
    {
        if (_kidnapResolved) return;
        _kidnapResolved = true;
        _handPickTimedOut = true;
        if (_kidnapHand.Count > 0)
            _handPickResult = _kidnapHand[new Random().Next(_kidnapHand.Count)];
        if (_kidnapFrame != null) _kidnapFrame.Continue = false;
    }

    private void ShowTeamOverlay(Border host, Card hostCard)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(hostCard.Name);
        if (IsShipCard(hostCard))
            sb.AppendLine(FormatShipEffectiveLine(hostCard));
        sb.AppendLine();
        for (int p = 1; p <= 2; p++)
        {
            var present = GetAllCardsOnHost(host, p);
            if (!present.Any()) continue;
            sb.AppendLine($"— Player {p} —");
            if (present.Any(ModifierRules.IsPersonnelCard))
                sb.AppendLine(ModifierRules.FormatTeamSummary(ModifierRules.SummarizeTeam(present, p), p));
            foreach (var c in present)
                sb.AppendLine($"  • {c.Name}  [{c.Type}]");
            sb.AppendLine();
        }
        if (TeamOverlayTitle != null)
            TeamOverlayTitle.Text = IsFacilityCard(hostCard)
                ? $"Present at facility — {hostCard.Name}"
                : $"Crew / Away Team — {hostCard.Name}";
        if (TeamOverlayBody != null) TeamOverlayBody.Text = sb.ToString();
        if (TeamOverlay != null) TeamOverlay.Visibility = Visibility.Visible;
    }

    private string FormatShipEffectiveLine(Card ship)
    {
        int printedR = BattleRules.EffectiveRange(ship, 0);
        int printedW = BattleRules.GetWeapons(ship);
        int printedS = BattleRules.GetShields(ship);

        Border? border = TableCanvas.Children.OfType<Border>()
            .FirstOrDefault(b => b.Tag is Card c && ReferenceEquals(c, ship));
        int owner = border == null ? _activePlayer : (GetBorderOwner(border) == 0 ? 1 : GetBorderOwner(border));
        var aboard = border == null ? new List<Card>() : GetAllCardsOnHost(border, owner);
        var evs = border == null
            ? Enumerable.Empty<(EventRules.Persist, Card)>()
            : EventsOn(border).Select(e => (e.Kind, e.Card));

        int w = printedW + EventRules.WeaponsBonusFromEvents(evs);
        int s = printedS + EventRules.ShieldsBonusFromEvents(evs, aboard);
        int k = BattleRules.KurlanMultiplier(aboard);
        w *= k;
        s *= k;

        bool transwarp = _attachedEvents.Any(e =>
            (e.Card.Name ?? "").Equals("Transwarp Conduit", StringComparison.OrdinalIgnoreCase)
            && (border == null || ReferenceEquals(e.Host, border)));
        int rangeFull = printedR * (transwarp ? 2 : 1);
        if (border != null && GetHullDamage(border) >= 50 && rangeFull > 5)
            rangeFull = 5;
        int remain = border != null ? GetRemainingRange(border, ship) : rangeFull;

        var mods = new List<string>();
        if (EventRules.WeaponsBonusFromEvents(evs) != 0) mods.Add("Bynars/events");
        if (k > 1) mods.Add($"Kurlan ×{k}");
        if (transwarp) mods.Add("Transwarp ×2");

        // Compendium: Rotation Damage → RANGE limited to maximum 5 (not halved).
        string line = $"RANGE {remain}/{rangeFull} (printed {printedR})  •  WEAPONS {w} (printed {printedW})  •  SHIELDS {s} (printed {printedS})";
        if (border != null)
        {
            int hull = GetHullDamage(border);
            if (hull > 0)
                line += $"\nDAMAGE: HULL {hull}%"
                        + (hull >= 50 ? "  ·  Rotation Damage (RANGE max 5)" : "")
                        + (hull >= 100 ? "  ·  DESTROYED" : "");
        }
        if (mods.Count > 0)
            line += "\nModifiers: " + string.Join(", ", mods);
        return line;
    }

    private void RefreshActionHistory()
    {
        if (ActionHistoryList == null) return;
        ActionHistoryList.Items.Clear();
        foreach (var line in _session.Log.FormatLines(100, includeDebug: _devShowDebugLog))
            ActionHistoryList.Items.Add(line);
        if (ActionHistoryList.Items.Count > 0)
            ActionHistoryList.ScrollIntoView(ActionHistoryList.Items[^1]);
    }

    /// <summary>
    /// Modal: große Karte + Text über dem Tisch. Blockiert bis Button.
    /// </summary>
    private RevealAnswer ShowCardReveal(
        Card? card,
        string title,
        string body,
        RevealButtons buttons = RevealButtons.Ok,
        string? subtitle = null,
        int? autoCloseMs = null,
        RevealAnswer autoDefault = RevealAnswer.No)
    {
        if (CardRevealOverlay == null)
        {
            MessageBox.Show(body, title);
            return RevealAnswer.Ok;
        }

        RevealTitle.Text = title;
        RevealSubtitle.Text = subtitle ?? (card != null
            ? $"{card.Type}" + (string.IsNullOrEmpty(card.Affiliation) ? "" : $"  ·  {card.Affiliation}")
            : "");
        RevealBody.Text = body;
        if (RevealAudience != null) RevealAudience.Text = "";
        if (RevealTimerText != null) RevealTimerText.Text = "";
        if (BtnRevealRespond != null) BtnRevealRespond.Visibility = Visibility.Collapsed;
        if (BtnRevealPass != null) BtnRevealPass.Visibility = Visibility.Collapsed;
        _announceKind = AnnounceKind.Hidden;

        RevealImage.Source = null;
        if (card != null && !string.IsNullOrEmpty(card.FullImagePath) && System.IO.File.Exists(card.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(card.FullImagePath, UriKind.Absolute);
                bmp.DecodePixelWidth = 440;
                bmp.EndInit();
                RevealImage.Source = bmp;
            }
            catch { }
        }

        bool yesNo = buttons == RevealButtons.YesNo;
        bool playerPick = buttons == RevealButtons.PlayerPick;
        bool choice = yesNo || playerPick;
        BtnRevealOk.Visibility = choice ? Visibility.Collapsed : Visibility.Visible;
        BtnRevealYes.Visibility = choice ? Visibility.Visible : Visibility.Collapsed;
        BtnRevealNo.Visibility = choice ? Visibility.Visible : Visibility.Collapsed;
        if (playerPick)
        {
            BtnRevealYes.Content = "Player 1";
            BtnRevealNo.Content = "Player 2";
        }
        else if (yesNo)
        {
            BtnRevealYes.Content = "Yes";
            BtnRevealNo.Content = "No";
        }

        _revealAnswer = RevealAnswer.None;
        _revealTimedOut = false;
        CardRevealOverlay.Visibility = Visibility.Visible;
        if (card != null)
            ShowCardDetail(card);

        // Choice dialogs get 10s unless caller overrides.
        if (choice && autoCloseMs == null)
            autoCloseMs = 10000;

        System.Windows.Threading.DispatcherTimer? autoTimer = null;
        if (autoCloseMs is int ms && ms > 0)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(ms);
            if (RevealTimerText != null)
                RevealTimerText.Text = $"{ms / 1000.0:0}s remaining";
            autoTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            autoTimer.Tick += (_, _) =>
            {
                double left = (deadline - DateTime.UtcNow).TotalSeconds;
                if (RevealTimerText != null)
                    RevealTimerText.Text = left > 0 ? $"{left:0}s remaining" : "";
                if (left > 0) return;
                autoTimer.Stop();
                if (_revealFrame != null && _revealAnswer == RevealAnswer.None)
                {
                    _revealTimedOut = true;
                    CloseReveal(choice ? autoDefault : RevealAnswer.Ok);
                }
            };
            autoTimer.Start();
        }
        else if (RevealTimerText != null)
        {
            RevealTimerText.Text = "";
        }

        _revealFrame = new System.Windows.Threading.DispatcherFrame();
        try
        {
            System.Windows.Threading.Dispatcher.PushFrame(_revealFrame);
        }
        finally
        {
            autoTimer?.Stop();
            _revealFrame = null;
            if (RevealTimerText != null) RevealTimerText.Text = "";
            CardRevealOverlay.Visibility = Visibility.Collapsed;
            RevealImage.Source = null;
        }

        return _revealAnswer == RevealAnswer.None ? RevealAnswer.Ok : _revealAnswer;
    }

    private void CloseReveal(RevealAnswer answer)
    {
        _revealAnswer = answer;
        if (_revealFrame != null)
            _revealFrame.Continue = false;
    }

    private void BtnRevealOk_Click(object sender, RoutedEventArgs e)
    {
        if (_announceKind != AnnounceKind.Hidden)
        {
            BtnRevealPass_Click(BtnRevealPass, e);
            return;
        }
        CloseReveal(RevealAnswer.Ok);
    }

    /// <summary>Visible outcome after a timed/default or manual player choice.</summary>
    private void AnnounceChoiceResult(Card? card, string title, string body)
    {
        StatusText.Text = body.Replace("\n", " ");
        // No auto-timer — player confirms with OK
        ShowCardReveal(card, title + " — Result", body, RevealButtons.Ok, null);
    }
    private void BtnRevealYes_Click(object sender, RoutedEventArgs e) => CloseReveal(RevealAnswer.Yes);
    private void BtnRevealNo_Click(object sender, RoutedEventArgs e) => CloseReveal(RevealAnswer.No);

    private void BtnHistoryRefresh_Click(object sender, RoutedEventArgs e) => RefreshActionHistory();

    private void BtnHistoryCopy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string text = string.Join(Environment.NewLine, _session.Log.FormatLines(500, includeDebug: _devShowDebugLog));
            Clipboard.SetText(string.IsNullOrEmpty(text) ? "(empty)" : text);
            StatusText.Text = "Action History copied to clipboard.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Copy failed: " + ex.Message;
        }
    }

    private void TableWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CheckTrace.Emit = msg =>
        {
            if (_devShowDebugLog)
                _session.Log.AddDebug(_session.TurnNumber, "Check", msg);
        };
        BuildFixedZones();
        UpdatePhaseControls();

        try
        {
            _db = new CardDatabase(DataPath);
            _cardBackImage = CardBack.Load(DataPath);
            int count = _db.LoadAll();
            StatusText.Text = $"{count} cards loaded – start New Game";
            RefreshActionHistory();
            DrawSpacelineBackground();
            ApplyRandomBoardBackground();
            // Keine Demo-Karten – Start über Welcome-Overlay / Neues Spiel
            Dispatcher.BeginInvoke(new Action(CenterOnSpaceline),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            StatusText.Text = "Failed to load card data";
            MessageBox.Show(
                $"Could not load card data:\n\n{ex.Message}\n\n" +
                "Expected a Data/ folder with set subfolders and cards.json\n"
                + "(or set STCCG_DATA / keep C:\\STCCG_DATA as fallback).\n"
                + $"Tried: {DataPath}",
                "Data error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ===================== FIXE STAPEL (oben/unten) =====================

    private void BuildFixedZones()
    {
        OpponentZonesPanel.Children.Clear();
        PlayerZonesPanel.Children.Clear();

        if (_seedPhaseActive)
        {
            // P1 unten
            AddSeedZonesFor(PlayerZonesPanel, opponent: false);
            AddSideDeckZonesIfPresent(PlayerZonesPanel, opponent: false);
            // P2 oben (gleiche Struktur)
            if (_gameMode == GameMode.Hotseat && _loadedDeckOpp != null)
            {
                AddSeedZonesFor(OpponentZonesPanel, opponent: true);
                AddSideDeckZonesIfPresent(OpponentZonesPanel, opponent: true);
            }
        }
        else
        {
            // Spielphase: P2 oben, P1 unten – gleiche Zonen
            AddZone(OpponentZonesPanel, "Discard", ColDiscard, opponent: true);
            AddZone(OpponentZonesPanel, "Draw Deck", ColDraw, opponent: true);
            if (_oppSeedCards.Count > 0)
                AddZone(OpponentZonesPanel, "Seed Deck", ColSeed, opponent: true);
            AddSideDeckZonesIfPresent(OpponentZonesPanel, opponent: true);
            // Hand is shown in the horizontal strip (not a left zone box)

            AddZone(PlayerZonesPanel, "Discard", ColDiscard, opponent: false);
            if (_seedCards.Count > 0)
                AddZone(PlayerZonesPanel, "Seed Deck", ColSeed, opponent: false);
            AddZone(PlayerZonesPanel, "Draw Deck", ColDraw, opponent: false);
            AddSideDeckZonesIfPresent(PlayerZonesPanel, opponent: false);
        }

        RefreshHandStrips();
    }

    private string _stripZoneP1 = "Hand";
    private string _stripZoneP2 = "Hand";
    /// <summary>Last host inspected (beam / refresh). Per-strip hosts stay independent.</summary>
    private Border? _hostStripHost;
    private Border? _hostStripHostP1;
    private Border? _hostStripHostP2;
    private bool _hostStripBeam;
    /// <summary>Host chosen by dropping a crew/ship-targeted interrupt.</summary>
    private Border? _interruptTargetHost;

    // ----- Top/bottom inspector: Spaceline stack vs opponent-pile interaction -----
    private enum InspectorMode { SpacelineInspect, OpponentPileInteract }
    private enum TargetPileType
    {
        Hand, Draw, Discard, BattleBridge, QsTent, QContinuum, SitePile, Tribble, OutOfPlay
    }
    [Flags]
    private enum PileInteraction
    {
        ViewOnly = 1,
        SelectCard = 2,
        FullView = 4,       // show faces even for private piles (draw)
        TakeToHand = 8,
        StealToTable = 16
    }

    private InspectorMode _inspectorMode = InspectorMode.SpacelineInspect;
    private string _savedStripZoneP1 = "Hand";
    private string _savedStripZoneP2 = "Hand";
    private Border? _savedHostStripHost;
    private Border? _savedHostStripHostP1;
    private Border? _savedHostStripHostP2;
    private bool _savedHostStripBeam;
    private int _pileTargetPlayer;
    private TargetPileType _pileTargetType;
    private PileInteraction _pileInteractions;
    private string _pilePrompt = "";
    private Action<Card?>? _pileOnComplete;
    private System.Windows.Threading.DispatcherFrame? _pileFrame;
    private bool _pileResolved;

    /// <summary>Horizontal strip: Hand by default; other piles when selected; Host = crew.</summary>
    private void RefreshHandStrips()
    {
        if (_inspectorMode == InspectorMode.OpponentPileInteract)
        {
            // Keep the interaction gallery on the target player's strip; refresh the other strip normally.
            bool targetIsP2 = _pileTargetPlayer == 2;
            if (targetIsP2)
            {
                FillOpponentPileGallery();
                RefreshOneStrip(opponent: false);
            }
            else
            {
                FillOpponentPileGallery();
                RefreshOneStrip(opponent: true);
            }
            return;
        }

        RefreshOneStrip(opponent: false);
        RefreshOneStrip(opponent: true);
    }

    private void RefreshOneStrip(bool opponent)
    {
        string zone = opponent ? _stripZoneP2 : _stripZoneP1;
        // Hand strips stay Hand (stack contents live in the detail popup).
        // Pile-click (Draw/Discard/…) still uses the strip as a gallery.
        if (zone == "Host" || string.IsNullOrEmpty(zone) || zone == "PileInteract")
            FillStrip(opponent, "Hand");
        else
            FillStrip(opponent, zone);
    }

    private void FillStrip(bool opponent, string zoneName)
    {
        var panel = opponent ? OppStripPanel : PlayerStripPanel;
        var title = opponent ? OppStripTitle : PlayerStripTitle;
        if (panel == null) return;

        panel.Children.Clear();
        if (title != null)
        {
            string who = opponent ? "P2" : "P1";
            title.Text = zoneName == "Hand" ? $"{who} HAND" : $"{who} · {zoneName}";
            title.Foreground = zoneName == "Hand"
                ? new SolidColorBrush(Color.FromRgb(0x6A, 0x9A, 0x70))
                : new SolidColorBrush(Color.FromRgb(0x9C, 0xDC, 0xFE));
        }

        var cards = GetCardsForZone(zoneName, opponent);
        int stripOwner = opponent ? 2 : 1;
        // Hotseat: both hands stay face-up so either player can play interrupts at any time.
        // Draw / seed / side decks remain hidden for the inactive player.
        bool isActiveSide = stripOwner == _activePlayer || _devPeekOpponentPiles
                            || (_gameMode == GameMode.Hotseat && zoneName == "Hand");
        bool privateZone = zoneName is "Hand" or "Draw Deck" or "Seed Deck"
            or "Q's Tent" or "Battle Bridge" or "Q-Continuum" or "Site Pile" or "Tribble" or "Side Deck";
        bool faceDownAlways = zoneName is "Draw Deck" or "Battle Bridge" or "Q-Continuum";

        if (!isActiveSide && privateZone && zoneName != "Hand")
        {
            int n = cards.Count;
            for (int i = 0; i < Math.Min(n, 12); i++)
            {
                var mini = CreateMiniCard(cards.Count > i ? cards[i] : cards[0], faceDown: true);
                mini.IsHitTestVisible = false;
                mini.RenderTransform = Transform.Identity;
                panel.Children.Add(mini);
            }
            if (n > 12)
                panel.Children.Add(new TextBlock
                {
                    Text = $"+{n - 12}",
                    Foreground = Brushes.Gray,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4)
                });
            return;
        }

        foreach (var card in cards)
        {
            var mini = CreateMiniCard(card, faceDown: faceDownAlways && zoneName != "Hand");
            mini.RenderTransform = Transform.Identity;
            mini.Tag = new ZoneCardRef(zoneName, card, opponent);
            mini.MouseLeftButtonDown += ZoneMini_MouseDown;
            mini.MouseRightButtonDown += ZoneMini_MouseRightDown;
            mini.MouseRightButtonUp += ZoneMini_MouseRightUp;
            panel.Children.Add(mini);
        }
    }

    /// <summary>
    /// Show crew / away team / seed of a host in a horizontal strip
    /// (the old right-hand StackGrid is collapsed after the 2026-08-20 layout).
    /// </summary>
    private void FillHostStrip(bool opponent)
    {
        var panel = opponent ? OppStripPanel : PlayerStripPanel;
        var title = opponent ? OppStripTitle : PlayerStripTitle;
        if (panel == null) return;
        panel.Children.Clear();

        var host = opponent ? _hostStripHostP2 : _hostStripHostP1;
        if (host?.Tag is not Card hostCard)
        {
            if (title != null) title.Text = opponent ? "P2 HAND" : "P1 HAND";
            return;
        }

        string who = opponent ? "P2" : "P1";
        bool beam = _hostStripBeam && !opponent && _activePlayer != 2
                    || _hostStripBeam && opponent && _activePlayer == 2;
        if (title != null)
        {
            title.Text = beam
                ? $"{who} · BEAM · {hostCard.Name}"
                : $"{who} · {hostCard.Name}";
            title.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x82));
        }

        // Ship RANGE/WEAPONS/SHIELDS live in "Crew / stats…" next to the card — not repeated in the strip.

        bool isMission = string.Equals(hostCard.Type, "Mission", StringComparison.OrdinalIgnoreCase);
        int seedCount = _seedUnderMission.TryGetValue(host, out var seedList) ? seedList.Count : 0;
        bool hasStack = _stackOnHost.TryGetValue(host, out var list) && list.Count > 0;

        if (_lastEncounteredDilemma.TryGetValue(host, out var lastDil) && lastDil != null)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Last dilemma",
                Foreground = new SolidColorBrush(Color.FromRgb(220, 120, 100)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 6, 0)
            });
            var lastMini = CreateMiniCard(lastDil, faceDown: false);
            lastMini.ToolTip = $"Last encountered: {lastDil.Name}\nDouble-click / right-click = large view";
            WireHostStripMini(lastMini, lastDil);
            panel.Children.Add(lastMini);
        }

        if (!hasStack && seedCount == 0 && !_lastEncounteredDilemma.ContainsKey(host))
        {
            panel.Children.Add(new TextBlock
            {
                Text = isMission
                    ? "Empty. Drag personnel/equipment here = Away Team."
                    : "Empty. Drag personnel/equipment onto this host.",
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Width = 220,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6)
            });
            return;
        }

        if (seedCount > 0 && seedList != null)
        {
            bool reveal = _devRevealSeed || _seedPhaseActive;
            bool showCount = _devShowSeedCounts || _seedPhaseActive || _devRevealSeed;
            panel.Children.Add(new TextBlock
            {
                Text = reveal
                    ? $"Seed{(showCount ? $": {seedCount}" : "")}"
                    : (showCount ? $"Seed {seedCount}" : "Seed"),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 180, 100)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 6, 0)
            });
            if (reveal)
            {
                foreach (var seedBorder in seedList)
                {
                    if (seedBorder.Tag is not Card sc) continue;
                    panel.Children.Add(CreateMiniCard(sc));
                }
            }
            else
            {
                int backs = showCount ? Math.Min(seedCount, 8) : 1;
                var backCard = seedList.Select(b => b.Tag as Card).FirstOrDefault(c => c != null);
                if (backCard != null)
                {
                    for (int i = 0; i < backs; i++)
                        panel.Children.Add(CreateMiniCard(backCard, faceDown: true));
                }
            }
        }

        if (!hasStack || list == null) return;

        foreach (var cardBorder in list.ToList())
        {
            if (cardBorder.Tag is not Card c) continue;
            int o = GetBorderOwner(cardBorder);
            if (o == 0) o = 1;
            bool beamThis = _hostStripBeam && o == _activePlayer;
            if (_hostStripBeam && o != _activePlayer)
            {
                // Still show opponent cards (public), but no checkbox
            }

            var mini = CreateMiniCard(c);
            mini.Tag = new HostCardRef(host, cardBorder, c);
            mini.Opacity = IsBorderStopped(cardBorder) ? 0.55 : 1;
            mini.BorderBrush = new SolidColorBrush(o == 2
                ? Color.FromRgb(80, 140, 180)
                : Color.FromRgb(120, 160, 90));
            mini.ToolTip = $"{c.Name}  (P{o})"
                           + (IsBorderStopped(cardBorder) ? "  · stopped" : "")
                           + "\nClick = detail  ·  Drag = move";

            if (beamThis)
            {
                bool sel = _beamSelected.Contains(cardBorder);
                var cell = new Grid { Margin = new Thickness(2) };
                var cb = new CheckBox
                {
                    IsChecked = sel,
                    Tag = cardBorder,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(2, 2, 0, 0),
                    Background = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                    ToolTip = "Select for beam"
                };
                Panel.SetZIndex(cb, 5);
                cb.Checked += BeamSelect_Changed;
                cb.Unchecked += BeamSelect_Changed;
                mini.MouseLeftButtonDown += (s2, e2) =>
                {
                    cb.IsChecked = !(cb.IsChecked == true);
                    e2.Handled = true;
                };
                cell.Children.Add(mini);
                cell.Children.Add(cb);
                panel.Children.Add(cell);
            }
            else
            {
                bool movable = CanDragOffHost(c);
                mini.ToolTip = $"{c.Name}  (P{o})"
                               + (IsBorderStopped(cardBorder) ? "  · stopped" : "")
                               + "\nClick = detail · Double-click / right-click = large view"
                               + (movable ? " · Drag = move" : " · stays on host");
                WireHostStripMini(mini, c);
                panel.Children.Add(mini);
            }
        }

        // Attached events only in _attachedEvents (no stack border)
        foreach (var ae in EventsOn(host).ToList())
        {
            if (hasStack && list != null
                && list.Any(b => b.Tag is Card sc && ReferenceEquals(sc, ae.Card)))
                continue;
            var emini = CreateMiniCard(ae.Card);
            emini.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 160, 60));
            emini.ToolTip = $"{ae.Card.Name} (attached)\nClick = detail · Double-click / right-click = large view";
            WireHostStripMini(emini, ae.Card);
            panel.Children.Add(emini);
        }

        // Keep that player's hand visible on the same strip so interrupts stay playable.
        var hand = opponent ? _oppHandCards : _handCards;
        if (hand.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = opponent ? "P2 HAND" : "P1 HAND",
                Foreground = new SolidColorBrush(Color.FromRgb(0x6A, 0x9A, 0x70)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 6, 0)
            });
            foreach (var card in hand)
            {
                var mini = CreateMiniCard(card, faceDown: false);
                mini.Tag = new ZoneCardRef("Hand", card, opponent);
                mini.MouseLeftButtonDown += ZoneMini_MouseDown;
                mini.MouseRightButtonDown += ZoneMini_MouseRightDown;
                mini.MouseRightButtonUp += ZoneMini_MouseRightUp;
                panel.Children.Add(mini);
            }
        }
    }

    /// <summary>Detail / large view for strip minis (incl. attached events).</summary>
    private void WireHostStripMini(Border mini, Card card)
    {
        mini.MouseLeftButtonDown += (s, e) =>
        {
            ShowCardDetail(card);
            if (e.ClickCount >= 2)
            {
                _detailHost = null;
                OpenCardDetailPopup();
                e.Handled = true;
                return;
            }
            if (s is Border b && b.Tag is HostCardRef)
                MiniCard_Click(s, e);
            else
                e.Handled = true;
        };
        mini.MouseRightButtonDown += (s, e) =>
        {
            e.Handled = true;
            ShowCardDetail(card);
            _detailHost = null;
            OpenCardDetailPopup();
            BeginHoldZoom(card, s as IInputElement);
        };
        mini.MouseRightButtonUp += (_, e) =>
        {
            EndHoldZoom();
            e.Handled = true;
        };
    }

    private void ResetStripsToHand()
    {
        if (_inspectorMode == InspectorMode.OpponentPileInteract)
            return; // locked until pile interaction finishes
        _stripZoneP1 = "Hand";
        _stripZoneP2 = "Hand";
        if (_cardActionMode != CardActionMode.BeamPickTarget)
        {
            _hostStripHost = null;
            _hostStripHostP1 = null;
            _hostStripHostP2 = null;
            _hostStripBeam = false;
        }
        RefreshHandStrips();
    }

    private static string PileTypeLabel(TargetPileType t) => t switch
    {
        TargetPileType.Hand => "Hand",
        TargetPileType.Draw => "Draw Deck",
        TargetPileType.Discard => "Discard",
        TargetPileType.BattleBridge => "Battle Bridge",
        TargetPileType.QsTent => "Q's Tent",
        TargetPileType.QContinuum => "Q-Continuum",
        TargetPileType.SitePile => "Site Pile",
        TargetPileType.Tribble => "Tribble",
        TargetPileType.OutOfPlay => "Out of Play",
        _ => t.ToString()
    };

    private List<Card> GetPileCards(int player, TargetPileType pile)
    {
        bool opp = player == 2;
        return pile switch
        {
            TargetPileType.Hand => (opp ? _oppHandCards : _handCards).ToList(),
            TargetPileType.Draw => (opp ? _oppDrawCards : _drawCards).ToList(),
            TargetPileType.Discard => (opp ? _oppDiscardCards : _discardCards).ToList(),
            TargetPileType.BattleBridge => (opp ? _oppBattleBridgeCards : _battleBridgeCards).ToList(),
            TargetPileType.QsTent => (opp ? _oppQsTentCards : _qsTentCards).ToList(),
            TargetPileType.QContinuum => (opp ? _oppQContinuumCards : _qContinuumCards).ToList(),
            TargetPileType.SitePile => (opp ? _oppSitePileCards : _sitePileCards).ToList(),
            TargetPileType.Tribble => (opp ? _oppTribbleCards : _tribbleCards).ToList(),
            _ => new List<Card>()
        };
    }

    /// <summary>
    /// Switch the target player's strip into an interaction gallery for a pile effect.
    /// Modal until a card is chosen (SelectCard) or the user cancels (ViewOnly / optional cancel).
    /// Restores the previous Spaceline/host inspector context afterward.
    /// </summary>
    private Card? BeginOpponentPileInteract(
        int targetPlayer,
        TargetPileType pile,
        PileInteraction flags,
        string prompt,
        bool allowCancel = true)
    {
        if (_inspectorMode == InspectorMode.OpponentPileInteract)
            EndOpponentPileInteract(null);

        _savedStripZoneP1 = _stripZoneP1;
        _savedStripZoneP2 = _stripZoneP2;
        _savedHostStripHost = _hostStripHost;
        _savedHostStripHostP1 = _hostStripHostP1;
        _savedHostStripHostP2 = _hostStripHostP2;
        _savedHostStripBeam = _hostStripBeam;

        _inspectorMode = InspectorMode.OpponentPileInteract;
        _pileTargetPlayer = targetPlayer;
        _pileTargetType = pile;
        _pileInteractions = flags;
        _pilePrompt = prompt;
        _pileResolved = false;
        Card? chosen = null;
        _pileOnComplete = c => chosen = c;

        if (targetPlayer == 2)
            _stripZoneP2 = "PileInteract";
        else
            _stripZoneP1 = "PileInteract";

        FillOpponentPileGallery();
        UpdatePhaseControls();
        StatusText.Text = prompt;

        bool needsModal = flags.HasFlag(PileInteraction.SelectCard);
        if (needsModal)
        {
            _pileFrame = new System.Windows.Threading.DispatcherFrame();
            try { System.Windows.Threading.Dispatcher.PushFrame(_pileFrame); }
            finally { _pileFrame = null; }
            EndOpponentPileInteract(chosen);
            return chosen;
        }

        // View-only: leave gallery open until user clicks Done
        return null;
    }

    private void FillOpponentPileGallery()
    {
        bool targetIsP2 = _pileTargetPlayer == 2;
        var panel = targetIsP2 ? OppStripPanel : PlayerStripPanel;
        var title = targetIsP2 ? OppStripTitle : PlayerStripTitle;
        var border = targetIsP2 ? OppHandStripBorder : PlayerHandStripBorder;
        if (panel == null) return;

        panel.Children.Clear();
        string pileName = PileTypeLabel(_pileTargetType);
        if (title != null)
        {
            title.Text = $"P{_pileTargetPlayer} · {pileName} — {_pilePrompt}";
            title.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x82));
        }
        if (border != null)
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));

        var cards = GetPileCards(_pileTargetPlayer, _pileTargetType);
        bool faceDown = !_pileInteractions.HasFlag(PileInteraction.FullView)
                        && _pileTargetType is TargetPileType.Draw or TargetPileType.BattleBridge
                            or TargetPileType.QContinuum;
        bool canSelect = _pileInteractions.HasFlag(PileInteraction.SelectCard);

        if (cards.Count == 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"({pileName} empty)",
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8)
            });
        }
        else
        {
            foreach (var card in cards)
            {
                var mini = CreateMiniCard(card, faceDown: faceDown);
                mini.Width = 78;
                mini.Height = 108;
                if (canSelect && !faceDown)
                {
                    Card cardRef = card;
                    mini.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 200, 120));
                    mini.BorderThickness = new Thickness(2);
                    mini.MouseLeftButtonDown += (_, e) =>
                    {
                        if (_pileResolved) return;
                        CompletePileSelection(cardRef);
                        e.Handled = true;
                    };
                }
                else if (canSelect && faceDown)
                {
                    // Still selectable (e.g. random-looking backs) — pick by click
                    Card cardRef = card;
                    mini.MouseLeftButtonDown += (_, e) =>
                    {
                        if (_pileResolved) return;
                        CompletePileSelection(cardRef);
                        e.Handled = true;
                    };
                }
                panel.Children.Add(mini);
            }
        }

        var done = new Button
        {
            Content = canSelect ? "Cancel" : "Done",
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(12, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 70)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 110))
        };
        done.Click += (_, _) => CompletePileSelection(null);
        panel.Children.Add(done);
    }

    private void CompletePileSelection(Card? card)
    {
        if (_pileResolved) return;
        _pileResolved = true;
        _pileOnComplete?.Invoke(card);
        if (_pileFrame != null)
            _pileFrame.Continue = false; // EndOpponentPileInteract runs after PushFrame returns
        else
            EndOpponentPileInteract(card);
    }

    private void EndOpponentPileInteract(Card? selected)
    {
        _inspectorMode = InspectorMode.SpacelineInspect;
        _pileOnComplete = null;
        _pilePrompt = "";

        // Restore strip chrome
        if (OppHandStripBorder != null)
            OppHandStripBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x4A, 0x55));
        if (PlayerHandStripBorder != null)
            PlayerHandStripBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x5A, 0x48));

        _stripZoneP1 = _savedStripZoneP1;
        _stripZoneP2 = _savedStripZoneP2;
        _hostStripHost = _savedHostStripHost;
        _hostStripHostP1 = _savedHostStripHostP1;
        _hostStripHostP2 = _savedHostStripHostP2;
        _hostStripBeam = _savedHostStripBeam;

        RefreshHandStrips();
        UpdatePhaseControls();

        if (selected != null)
            StatusText.Text = $"Selected from P{_pileTargetPlayer} {PileTypeLabel(_pileTargetType)}: {selected.Name}";
        else
            StatusText.Text = "Pile inspection closed.";
    }

    private void AddSeedZonesFor(Panel parent, bool opponent)
    {
        int door = opponent ? _oppDoorwayCards.Count : _doorwayCards.Count;
        int miss = opponent ? _oppMissionSeedCards.Count : _missionSeedCards.Count;
        int dil = opponent ? _oppDilemmaSeedCards.Count : _dilemmaSeedCards.Count;
        int fac = opponent ? _oppFacilitySeedCards.Count : _facilitySeedCards.Count;

        if (door > 0 || (_seedSubPhase == SeedSubPhase.Doorway && !opponent) ||
            (_seedSubPhase == SeedSubPhase.Doorway && opponent && _activePlayer == 2))
            AddZone(parent, "Doorways", ColDoorway, opponent);
        if (miss > 0 || (_seedSubPhase == SeedSubPhase.Mission && ((!opponent && _activePlayer == 1) || (opponent && _activePlayer == 2))))
            AddZone(parent, "Missions", ColMission, opponent);
        if (dil > 0 || (_seedSubPhase == SeedSubPhase.Dilemma && ((!opponent && _activePlayer == 1) || (opponent && _activePlayer == 2))))
            AddZone(parent, "Dilemmas", ColDilemma, opponent);
        if (fac > 0 || (_seedSubPhase == SeedSubPhase.Facility && ((!opponent && _activePlayer == 1) || (opponent && _activePlayer == 2))))
            AddZone(parent, "Facilities", ColFacility, opponent);
    }

    /// <summary>Side-Deck-Zonen nur anzeigen, wenn Karten vorhanden.</summary>
    private void AddSideDeckZonesIfPresent(Panel parent, bool opponent)
    {
        bool p2 = opponent;
        if ((p2 ? _oppQsTentCards : _qsTentCards).Count > 0)
            AddZone(parent, "Q's Tent", Color.FromRgb(106, 27, 154), opponent);
        if ((p2 ? _oppBattleBridgeCards : _battleBridgeCards).Count > 0)
            AddZone(parent, "Battle Bridge", Color.FromRgb(183, 28, 28), opponent);
        if ((p2 ? _oppQContinuumCards : _qContinuumCards).Count > 0)
            AddZone(parent, "Q-Continuum", Color.FromRgb(94, 53, 177), opponent);
        if ((p2 ? _oppSitePileCards : _sitePileCards).Count > 0)
            AddZone(parent, "Site Pile", Color.FromRgb(0, 131, 143), opponent);
        if ((p2 ? _oppTribbleCards : _tribbleCards).Count > 0)
            AddZone(parent, "Tribble", Color.FromRgb(239, 108, 0), opponent);
        if ((p2 ? _oppSideCards : _sideCards).Count > 0)
            AddZone(parent, "Side Deck", ColSide, opponent);
    }

    /// <summary>
    /// Zonen neu aufbauen (z. B. beim Wechsel Seed ↔ Spielphase).
    /// </summary>
    private void RebuildPlayerZones()
    {
        BuildFixedZones();
        RefreshZoneCounts();
    }

    private void AddZone(Panel parent, string title, Color color, bool opponent)
    {
        bool isSide = IsSideDeckZone(title);
        bool locked = isSide && !IsSideDeckUnlocked(title, opponent);
        byte fillAlpha = opponent ? (byte)50 : locked ? (byte)40 : (byte)100;
        byte borderAlpha = opponent ? (byte)140 : locked ? (byte)100 : (byte)230;

        var box = new Border
        {
            Width = ZoneW,
            Height = ZoneH,
            Margin = new Thickness(0, 0, 6, 6),
            Background = new SolidColorBrush(Color.FromArgb(fillAlpha, color.R, color.G, color.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(borderAlpha, color.R, color.G, color.B)),
            BorderThickness = new Thickness(opponent ? 1 : 2),
            CornerRadius = new CornerRadius(4),
            Cursor = locked ? Cursors.No : Cursors.Hand,
            Tag = $"zone:{(opponent ? "opp" : "you")}:{title}",
            ClipToBounds = true,
            ToolTip = locked
                ? $"{title}: LOCKED – place matching doorway on it"
                : $"{title} ({(opponent ? "Gegner" : "Du")})"
        };

        var root = new Grid();
        // Deckblatt / Cover-Bild
        var face = new Image
        {
            Name = "ZoneFace",
            Stretch = Stretch.UniformToFill,
            IsHitTestVisible = false,
            Opacity = locked ? 0.35 : 1.0
        };
        root.Children.Add(face);

        if (CoversFor(opponent).TryGetValue(title, out var cover) &&
            !string.IsNullOrEmpty(cover.FullImagePath) && System.IO.File.Exists(cover.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(cover.FullImagePath, UriKind.Absolute);
                bmp.EndInit();
                face.Source = bmp;
            }
            catch { /* ignore */ }
        }

        // Name + Anzahl unten
        var footer = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 10, 10, 12)),
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Thickness(2, 2, 2, 2),
            IsHitTestVisible = false
        };
        var label = new TextBlock
        {
            Text = ShortName(title),
            Foreground = Brushes.White,
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            IsHitTestVisible = false
        };
        footer.Child = label;
        root.Children.Add(footer);

        // Count-Badge
        var countTb = new TextBlock
        {
            Text = "0",
            Foreground = Brushes.White,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Tag = "zoneCount",
            IsHitTestVisible = false
        };
        var countBadge = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(220, 20, 20, 24)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(5, 1, 5, 1),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 3, 3, 0),
            IsHitTestVisible = false,
            Child = countTb
        };
        root.Children.Add(countBadge);

        if (locked)
        {
            var lockOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = "🔒",
                    FontSize = 22,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                }
            };
            root.Children.Add(lockOverlay);
        }

        box.Child = root;
        box.MouseLeftButtonDown += Zone_MouseLeftButtonDown;
        parent.Children.Add(box);
    }

    private static bool IsSideDeckZone(string zoneName) =>
        zoneName is "Q's Tent" or "Battle Bridge" or "Q-Continuum" or "Site Pile" or "Tribble" or "Side Deck";

    private bool IsSideDeckUnlocked(string zoneName, bool opponent = false) =>
        !IsSideDeckZone(zoneName) ||
        (opponent ? _oppSideDeckCovers : _sideDeckCovers).ContainsKey(zoneName);

    private Dictionary<string, Card> CoversFor(bool opponent) =>
        opponent ? _oppSideDeckCovers : _sideDeckCovers;

    /// <summary>Welches Side-Deck öffnet dieser Doorway? (Name-Heuristik)</summary>
    private static string? GetSideDeckForDoorway(Card doorway)
    {
        string n = (doorway.Name ?? "").ToLowerInvariant();
        string t = (doorway.Text ?? "").ToLowerInvariant();
        if (n.Contains("q's tent") || n.Contains("qs tent") || n.Contains("q’s tent")
            || t.Contains("q's tent side deck") || t.Contains("q’s tent side deck"))
            return "Q's Tent";
        if (n.Contains("battle bridge") || t.Contains("battle bridge side deck"))
            return "Battle Bridge";
        if (n.Contains("continuum") || t.Contains("q-continuum side deck") || t.Contains("q continuum side deck"))
            return "Q-Continuum";
        if (n.Contains("tribble") || t.Contains("tribble side deck"))
            return "Tribble";
        if ((n.Contains("site") && (n.Contains("door") || n.Contains("hq") || n.Contains("headquarters")))
            || t.Contains("site pile"))
            return "Site Pile";
        return null;
    }

    private static bool IsDoorwayCard(Card c) =>
        (c.Type ?? "").Contains("doorway", StringComparison.OrdinalIgnoreCase);

    private static string ShortName(string title) => title switch
    {
        "Discard" => "Discard",
        "Seed Deck" => "Seed",
        "Doorways" => "Doorways",
        "Missions" => "Missions",
        "Dilemmas" => "Dil/Art",
        "Facilities" => "Facilities",
        "Draw Deck" => "Draw",
        "Side Deck" => "Side",
        "Q's Tent" => "Q's Tent",
        "Battle Bridge" => "B.Bridge",
        "Q-Continuum" => "Q-Cont.",
        "Site Pile" => "Sites",
        "Tribble" => "Tribble",
        "Hand" => "Hand",
        _ => title
    };

    private Border? _lastZoneClick;
    private DateTime _lastZoneClickTime;

    private void Zone_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border box || box.Tag is not string tag || !tag.StartsWith("zone:"))
            return;

        var parts = tag.Split(':');
        if (parts.Length < 3) return;

        bool opponent = parts[1] == "opp";
        string zoneName = parts[2];

        if (IsSideDeckZone(zoneName) && !IsSideDeckUnlocked(zoneName, opponent))
        {
            StatusText.Text = $"{zoneName} ist gesperrt – passenden Doorway als Deckblatt darauf legen.";
            StackTitle.Text = $"{zoneName} (LOCKED)";
            e.Handled = true;
            return;
        }

        // Doppelklick Draw-Deck der aktiven Seite → 1 Karte ziehen
        var now = DateTime.Now;
        int zonePlayer = opponent ? 2 : 1;
        if (zoneName == "Draw Deck" && zonePlayer == _activePlayer
            && _lastZoneClick == box && (now - _lastZoneClickTime).TotalMilliseconds < 350)
        {
            DrawOneToHand();
            _lastZoneClick = null;
            e.Handled = true;
            return;
        }
        _lastZoneClick = box;
        _lastZoneClickTime = now;

        // Deckblatt (Doorway) in Detailansicht zeigen
        if (CoversFor(opponent).TryGetValue(zoneName, out var coverCard))
            ShowCardDetail(coverCard);

        if (opponent && !CanInspectOpponentZone(zoneName))
        {
            ShowPrivateOpponentZone(zoneName);
            e.Handled = true;
            return;
        }

        ShowStackContents(zoneName, opponent);
        e.Handled = true;
    }

    /// <summary>
    /// Opponent piles are private (Hand, Draw, Seed, Side). Discard is public.
    /// In-play cards on the table (ships, facilities, attached events/dilemmas) stay visible.
    /// </summary>
    private bool CanInspectOpponentZone(string zoneName)
    {
        if (_devPeekOpponentPiles) return true;
        if (zoneName is "Discard") return true;
        // Everything else in the zone strip is private to the owner
        return false;
    }

    private void ShowPrivateOpponentZone(string zoneName)
    {
        _stripZoneP2 = zoneName;
        FillStrip(true, zoneName);
        int n = GetCardsForZone(zoneName, opponent: true).Count;
        StatusText.Text = zoneName == "Hand"
            ? $"P2 hand: {n} card(s) hidden (Developer > Peek opponent piles)."
            : $"P2 {zoneName}: {n} card(s) hidden.";
    }

    private void ShowStackContents(string zoneName, bool opponent)
    {
        // Zone contents appear in the horizontal hand strip; TABLE stays on the right.
        if (opponent) _stripZoneP2 = zoneName;
        else _stripZoneP1 = zoneName;
        FillStrip(opponent, zoneName);

        if (StackTitle != null)
            StackTitle.Text = $"{zoneName} ({(opponent ? "P2" : "P1")})";

        string owner = opponent ? "P2" : "P1";
        int n = GetCardsForZone(zoneName, opponent).Count;
        if (!opponent && zoneName == "Draw Deck" && _drawCards.Count > 0 && !_seedPhaseActive)
            StatusText.Text = $"Draw ({owner}): {_drawCards.Count} – double-click zone = draw 1 to hand";
        else if (zoneName == "Hand")
            StatusText.Text = _session.Match == GameSession.MatchPhase.Play
                ? $"Hand {owner}: {n} · {_session.StatusLine()}"
                : $"Hand {owner}: {n} – Personnel/Eq → Host · Events → TABLE · Discard";
        else if (!opponent && IsSeedZone(zoneName))
            StatusText.Text = SeedPhaseStatusLine() + $"  ·  {zoneName}: {n}";
        else
            StatusText.Text = $"{zoneName} ({owner}): {n} cards";
    }

    private static bool IsSeedZone(string zoneName) =>
        zoneName is "Seed Deck" or "Doorways" or "Missions" or "Dilemmas" or "Facilities";

    private string SeedPhaseStatusLine()
    {
        if (!_seedPhaseActive) return "Spielphase";
        return _seedSubPhase switch
        {
            SeedSubPhase.Doorway => "SEED 1/4 DOORWAY – unlock side decks; P1 all, then P2",
            SeedSubPhase.Mission => "SEED 2/4 MISSION – abwechselnd legen (Hotseat) · Regionen beachten",
            SeedSubPhase.Dilemma => "SEED 3/4 DILEMMA – alternate dil/art under missions",
            SeedSubPhase.Facility => "SEED 4/4 FACILITY – abwechselnd Facilities/Schiffe/Events …",
            _ => "SEED beendet"
        };
    }

    private string SeedPhaseShortName() => _seedSubPhase switch
    {
        SeedSubPhase.Doorway => "1/4 Doorway",
        SeedSubPhase.Mission => "2/4 Mission",
        SeedSubPhase.Dilemma => "3/4 Dilemma",
        SeedSubPhase.Facility => "4/4 Facility",
        _ => "–"
    };

    private void ZoneMini_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border mini || mini.Tag is not ZoneCardRef zref)
            return;

        if (_stack.IsOpen && TryPlayAsResponse(zref))
        {
            e.Handled = true;
            return;
        }

        ShowCardDetail(zref.Card);
        if (e.ClickCount >= 2)
        {
            _detailHost = null; // hand / zone card — no host stack section
            OpenCardDetailPopup();
            e.Handled = true;
            return;
        }
        _zoneDragRef = zref;
        _panelDragRef = null;
        _panelDragging = false;
        _mouseDownScreen = e.GetPosition(this);
        mini.CaptureMouse();
        mini.MouseMove += ZoneMini_MouseMove;
        mini.MouseLeftButtonUp += ZoneMini_MouseUp;
        e.Handled = true;
    }

    private void ZoneMini_MouseRightDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border mini || mini.Tag is not ZoneCardRef zref)
            return;
        ShowCardDetail(zref.Card);
        BeginHoldZoom(zref.Card, mini);
        e.Handled = true;
    }

    private void ZoneMini_MouseRightUp(object sender, MouseButtonEventArgs e)
    {
        EndHoldZoom();
        e.Handled = true;
    }

    private void ZoneMini_MouseMove(object sender, MouseEventArgs e)
    {
        if (_zoneDragRef == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var pos = e.GetPosition(this);
        double dx = pos.X - _mouseDownScreen.X;
        double dy = pos.Y - _mouseDownScreen.Y;

        if (!_panelDragging)
        {
            if (dx * dx + dy * dy < DragThreshold * DragThreshold)
                return;
            // Nur Karten der eigenen Seite (P1 unten / P2 oben)
            int sidePlayer = _zoneDragRef.Opponent ? 2 : 1;
            if (_gameMode == GameMode.Hotseat && sidePlayer != _activePlayer)
            {
                bool anytime = _zoneDragRef.ZoneName == "Hand"
                               && TimingRules.IsAnytimeType(_zoneDragRef.Card);
                if (!anytime)
                {
                    StatusText.Text =
                        $"Player {_activePlayer}'s turn – the other player may only play interrupts / doorways.";
                    return;
                }
            }
            bool fromSeed = _seedPhaseActive && IsSeedZone(_zoneDragRef.ZoneName);
            bool fromUnlockedSide = IsSideDeckZone(_zoneDragRef.ZoneName)
                && IsSideDeckUnlocked(_zoneDragRef.ZoneName, _zoneDragRef.Opponent);
            if (_zoneDragRef.ZoneName is not "Hand" && !fromSeed && !fromUnlockedSide)
                return;

            HideMiniHover();
            _panelDragging = true;
            _dragOnOverlay = true;
            // Schwebekarte auf DragLayer (ganzes Fenster) – damit Drop auf untere Stapel möglich ist
            var border = CreateFloatingCard(_zoneDragRef.Card);
            SetBorderOwner(border, _zoneDragRef.Opponent ? 2 : 1);
            Canvas.SetLeft(border, pos.X - TableCardWidth / 2);
            Canvas.SetTop(border, pos.Y - TableCardHeight / 2);
            DragLayer.Children.Add(border);
            _dragCard = border;
            _dragOffset = new Point(TableCardWidth / 2, TableCardHeight / 2);
            _isDragging = true;

            RemoveCardFromZone(_zoneDragRef.ZoneName, _zoneDragRef.Card, _zoneDragRef.Opponent);
            if (IsMissionCard(_zoneDragRef.Card))
                ShowMissionSlotPreviews(_zoneDragRef.Card);
            if (IsDoorwayCard(_zoneDragRef.Card))
                HighlightSideDeckForDoorway(_zoneDragRef.Card);
            if (IsTablePermanentType(_zoneDragRef.Card) && EventBelongsOnTableColumn(_zoneDragRef.Card))
                SetTischZoneHighlight(true);
            ShowLegalPlayHighlights(_zoneDragRef.Card, fromHand: _zoneDragRef.ZoneName == "Hand");
            // Discard-Highlight kommt in MouseMove bei Nähe (UpdateDiscardSnap)
        }
        else if (_dragCard != null)
        {
            if (_dragOnOverlay)
            {
                Canvas.SetLeft(_dragCard, pos.X - _dragOffset.X);
                Canvas.SetTop(_dragCard, pos.Y - _dragOffset.Y);
            }
            else
            {
                var canvasPos = e.GetPosition(TableCanvas);
                Canvas.SetLeft(_dragCard, canvasPos.X - _dragOffset.X);
                Canvas.SetTop(_dragCard, canvasPos.Y - _dragOffset.Y);
            }
            var winPos = e.GetPosition(this);
            // Discard-Snap (rot + Karte verkleinern) hat Vorrang
            if (UpdateDiscardSnap(winPos))
            {
                // Karte snapt auf Discard-Zone
            }
            else if (UpdateTableSnap(winPos))
            {
                // Event/Doorway/Objective snapt auf P1/P2 TABLE (rechts)
            }
            else
            {
                if (_dragCard != null && Math.Abs(_dragCard.Width - TableCardWidth) > 0.5)
                {
                    RestoreFloatingCardSize();
                    Canvas.SetLeft(_dragCard, pos.X - _dragOffset.X);
                    Canvas.SetTop(_dragCard, pos.Y - _dragOffset.Y);
                }
                var floating = _dragCard;
                if (floating?.Tag is Card dc && IsMissionCard(dc))
                    ShowMissionSlotPreviews(dc);
                else if (floating?.Tag is Card dw && IsDoorwayCard(dw))
                {
                    UpdateZoneSnapForDoorway(dw, winPos);
                    if (_zoneHighlightTarget == null)
                    {
                        RestoreFloatingCardSize();
                        Canvas.SetLeft(floating, pos.X - _dragOffset.X);
                        Canvas.SetTop(floating, pos.Y - _dragOffset.Y);
                    }
                }
                else
                    UpdateSnapPreviewFromWindow(winPos);
            }
        }
    }

    /// <summary>
    /// Über Discard-Zone: rot umranden, Karte auf Stapelgröße snappen.
    /// </summary>
    private bool UpdateDiscardSnap(Point windowPos)
    {
        if (_seedPhaseActive)
        {
            HighlightDiscardZone(false);
            return false;
        }
        var box = FindZoneBorder("Discard", opponent: _activePlayer == 2);
        if (box == null || !IsPointNearElement(box, windowPos, 28))
        {
            bool was = _discardHighlighted;
            HighlightDiscardZone(false);
            if (was && _dragCard != null && _dragOnOverlay)
            {
                RestoreFloatingCardSize();
                Canvas.SetLeft(_dragCard, windowPos.X - _dragOffset.X);
                Canvas.SetTop(_dragCard, windowPos.Y - _dragOffset.Y);
            }
            return false;
        }

        HighlightDiscardZone(true);
        if (_dragCard == null) return true;
        try
        {
            var tl = box.TransformToAncestor(this).Transform(new Point(0, 0));
            double tw = box.ActualWidth > 0 ? box.ActualWidth : ZoneW;
            double th = box.ActualHeight > 0 ? box.ActualHeight : ZoneH;
            _dragCard.Width = tw;
            _dragCard.Height = th;
            Canvas.SetLeft(_dragCard, tl.X);
            Canvas.SetTop(_dragCard, tl.Y);
            _dragOffset = new Point(tw / 2, th / 2);
        }
        catch { }
        return true;
    }

    /// <summary>
    /// Doorway über Side-Deck: Zone umranden, Karte auf Stapelgröße verkleinern und an Zone ausrichten.
    /// </summary>
    private void UpdateZoneSnapForDoorway(Card doorway, Point windowPos)
    {
        HideSnapPreview();
        ClearZoneHighlight();

        string? preferred = GetSideDeckForDoorway(doorway);
        Border? target = null;

        // Bevorzugt passenden Stapel, sonst jede Side-Deck-Zone unter dem Cursor
        if (preferred != null)
        {
            var box = FindZoneBorder(preferred, opponent: _activePlayer == 2);
            if (box != null && IsPointNearElement(box, windowPos, 28))
                target = box;
        }
        if (target == null)
        {
            bool opp = _activePlayer == 2;
            var panel = opp ? OpponentZonesPanel : PlayerZonesPanel;
            foreach (var child in panel.Children.OfType<Border>())
            {
                if (child.Tag is not string tag) continue;
                string name = tag.Contains(':') ? tag[(tag.LastIndexOf(':') + 1)..] : tag;
                if (!IsSideDeckZone(name)) continue;
                if (IsPointNearElement(child, windowPos, 20))
                {
                    if (preferred == null || string.Equals(preferred, name, StringComparison.OrdinalIgnoreCase))
                    {
                        target = child;
                        break;
                    }
                }
            }
        }

        if (target == null || _dragCard == null)
            return;

        // Umrandung am Stapel
        _zoneHighlightTarget = target;
        _zoneHighlightPrevThickness = target.BorderThickness;
        _zoneHighlightPrevBrush = target.BorderBrush;
        target.BorderThickness = new Thickness(3);
        target.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 220, 255));

        // Karte auf Stapelgröße schrumpfen und über die Zone legen
        try
        {
            var tl = target.TransformToAncestor(this).Transform(new Point(0, 0));
            double tw = target.ActualWidth > 0 ? target.ActualWidth : ZoneW;
            double th = target.ActualHeight > 0 ? target.ActualHeight : ZoneH;
            _dragCard.Width = tw;
            _dragCard.Height = th;
            Canvas.SetLeft(_dragCard, tl.X);
            Canvas.SetTop(_dragCard, tl.Y);
            _dragOffset = new Point(tw / 2, th / 2);
        }
        catch { /* ignore */ }
    }

    private void ClearZoneHighlight()
    {
        if (_zoneHighlightTarget != null)
        {
            // Nicht Discard-Zone (die hat eigene Highlight-Logik)
            if (_zoneHighlightTarget == FindPlayerZoneBorder("Discard"))
            {
                _zoneHighlightTarget = null;
            }
            else
            {
                _zoneHighlightTarget.BorderThickness = _zoneHighlightPrevThickness;
                if (_zoneHighlightPrevBrush != null)
                    _zoneHighlightTarget.BorderBrush = _zoneHighlightPrevBrush;
                _zoneHighlightTarget = null;
            }
        }
    }

    private void RestoreFloatingCardSize()
    {
        if (_dragCard == null) return;
        _dragCard.Width = TableCardWidth;
        _dragCard.Height = TableCardHeight;
        _dragOffset = new Point(TableCardWidth / 2, TableCardHeight / 2);
    }

    /// <summary>Doorway aufgenommen → passendes Side-Deck sofort umrahmen (auch ohne Nähe).</summary>
    private void HighlightSideDeckForDoorway(Card doorway)
    {
        ClearZoneHighlight();
        string? zone = GetSideDeckForDoorway(doorway);
        if (zone == null) return;
        var box = FindZoneBorder(zone, opponent: _activePlayer == 2);
        if (box == null) return;
        _zoneHighlightTarget = box;
        _zoneHighlightPrevThickness = box.BorderThickness;
        _zoneHighlightPrevBrush = box.BorderBrush;
        box.BorderThickness = new Thickness(3);
        box.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 220, 255));
    }


    private void RestoreTableZoneChrome(Border? box, bool opponent)
    {
        if (box == null) return;
        if (opponent)
        {
            box.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x28));
            box.BorderBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1C));
            box.BorderThickness = new Thickness(0, 0, 0, 1);
        }
        else
        {
            box.Background = new SolidColorBrush(Color.FromRgb(0x3E, 0x3E, 0x46));
            box.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x34));
            box.BorderThickness = new Thickness(0, 1, 0, 0);
        }
    }

    /// <summary>P1 TABLE = bottom-right, P2 TABLE = top-right (not the hand strips).</summary>
    private Border? ActiveTableZoneBorder() =>
        _activePlayer == 2 ? OppTischZoneBorder : TischZoneBorder;

    private bool IsPointOverOwnTable(Point windowPos)
    {
        var box = ActiveTableZoneBorder();
        return box != null && IsPointNearElement(box, windowPos, 8);
    }

    private void SetTischZoneHighlight(bool on)
    {
        RestoreTableZoneChrome(TischZoneBorder, opponent: false);
        RestoreTableZoneChrome(OppTischZoneBorder, opponent: true);
        if (!on) return;
        var box = ActiveTableZoneBorder();
        if (box == null) return;
        box.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 200, 255));
        box.BorderThickness = new Thickness(2);
        box.Background = new SolidColorBrush(Color.FromArgb(70, 0, 160, 220));
    }

    /// <summary>While dragging a table-permanent, snap the card onto the owner's TABLE column.</summary>
    private bool UpdateTableSnap(Point windowPos)
    {
        if (_dragCard?.Tag is not Card c || !IsTablePermanentType(c) || !EventBelongsOnTableColumn(c))
            return false;
        var box = ActiveTableZoneBorder();
        if (box == null || !IsPointNearElement(box, windowPos, 10))
            return false;

        SetTischZoneHighlight(true);
        try
        {
            var tl = box.TransformToAncestor(this).Transform(new Point(0, 0));
            _dragCard.Width = ZoneW;
            _dragCard.Height = ZoneH;
            Canvas.SetLeft(_dragCard, tl.X + 8);
            Canvas.SetTop(_dragCard, tl.Y + 22);
            _dragOffset = new Point(ZoneW / 2, ZoneH / 2);
        }
        catch { }
        return true;
    }

    private Thickness _discardPrevThickness;
    private Brush? _discardPrevBrush;
    private bool _discardHighlighted;

    private void HighlightDiscardZone(bool on)
    {
        var box = FindZoneBorder("Discard", opponent: _activePlayer == 2);
        if (box == null) return;
        if (on)
        {
            if (!_discardHighlighted)
            {
                _discardPrevThickness = box.BorderThickness;
                _discardPrevBrush = box.BorderBrush;
                _discardHighlighted = true;
            }
            box.BorderThickness = new Thickness(3);
            box.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 80, 80));
        }
        else if (_discardHighlighted)
        {
            box.BorderThickness = _discardPrevThickness;
            if (_discardPrevBrush != null)
                box.BorderBrush = _discardPrevBrush;
            _discardHighlighted = false;
        }
    }

    /// <summary>
    /// Snap-Vorschau während Overlay-Drag: Fensterkoordinaten → Canvas, dann UpdateSnapPreview.
    /// </summary>
    private void UpdateSnapPreviewFromWindow(Point windowPos)
    {
        if (_dragCard?.Tag is not Card card)
        {
            HideSnapPreview();
            return;
        }
        bool eventTarget = EventRules.IsEvent(card) && !_seedPhaseActive
                           && EventRules.NeedsTableHost(EventRules.GetTargetKind(EventRules.ResolvePlay(card)));
        if (!IsStackableCard(card) && !IsDockableUnderMission(card)
            && !(IsSeedableUnderMission(card) && _seedPhaseActive)
            && !eventTarget)
        {
            HideSnapPreview();
            return;
        }

        try
        {
            var origin = TableCanvas.TransformToAncestor(this).Transform(new Point(0, 0));
            double scale = ZoomTransform.ScaleX;
            if (scale < 0.01) scale = 1;
            double canvasX = (windowPos.X - origin.X) / scale - TableCardWidth / 2;
            double canvasY = (windowPos.Y - origin.Y) / scale - TableCardHeight / 2;

            // Temporär Canvas-Position setzen (auch wenn Karte noch auf DragLayer liegt)
            Canvas.SetLeft(_dragCard, canvasX);
            Canvas.SetTop(_dragCard, canvasY);
            UpdateSnapPreview(_dragCard);

            // Overlay-Position beibehalten
            if (_dragOnOverlay)
            {
                Canvas.SetLeft(_dragCard, windowPos.X - _dragOffset.X);
                Canvas.SetTop(_dragCard, windowPos.Y - _dragOffset.Y);
            }
        }
        catch
        {
            HideSnapPreview();
        }
    }

    private Border CreateFloatingCard(Card card)
    {
        var border = new Border
        {
            Width = TableCardWidth,
            Height = TableCardHeight,
            BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            CornerRadius = new CornerRadius(3),
            Tag = card,
            IsHitTestVisible = false
        };
        var img = new Image { Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
        if (!string.IsNullOrEmpty(card.FullImagePath) && System.IO.File.Exists(card.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(card.FullImagePath, UriKind.Absolute);
                bmp.DecodePixelWidth = 200;
                bmp.EndInit();
                img.Source = bmp;
            }
            catch { }
        }
        border.Child = img;
        return border;
    }


    /// <summary>
    /// Compendium 6.1 / 6.1.1:
    /// - Interrupt/Doorway: jederzeit, kein normal card play
    /// - "for free": unbegrenzt im Play-Segment, zählt nicht als normal card play
    /// - sonst: max. 1 normal card play, nur im Play-Segment vor Execute
    /// </summary>
    /// <summary>Statuszeile + MessageBox, damit Ablehnungen nicht "verschwinden".</summary>
    private void ShowPlayError(string message)
    {
        StatusText.Text = message;
        try
        {
            MessageBox.Show(message, "Illegal action", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch { /* Designer / Headless */ }
    }

    // ===================== ACTION STACK / RESPONSES =====================

    private void BeginPlayCardStack(Card card, bool isResponse, int? controllerOverride = null, Card? target = null)
    {
        int controller = controllerOverride
                         ?? (isResponse ? _stack.ResponsePlayer : _activePlayer);
        string targetBit = target != null ? $" → {target.Name}" : "";
        var action = new TimingRules.PendingAction
        {
            Kind = TimingRules.ActionKind.PlayCard,
            Controller = controller,
            Card = card,
            IsResponse = isResponse,
            TargetCard = target,
            Summary = isResponse
                ? $"P{controller} Response: {card.Name}{targetBit}"
                : $"P{controller} plays {card.Name}{targetBit}"
        };
        _stack.Push(action);
        _session.Log.Add(_session.TurnNumber, $"P{controller}",
            (isResponse ? "Response: " : "Play: ") + card.Name);

        if (isResponse)
            ApplyResponseEffect(action);

        int next = opponentOf(controller);
        OpenResponseWindow(next);
        // Card is already on the table / discarded-from-hand: pause, then announce
        ScheduleActionAnnounce(isResponse ? 400 : 600);
    }

    private void BeginShipBattleStack(
        Border attacker, Card atkCard, Border defender, Card defCard, int atkOwner, int defOwner)
    {
        var action = new TimingRules.PendingAction
        {
            Kind = TimingRules.ActionKind.InitiateShipBattle,
            Controller = atkOwner,
            AttackerHost = attacker,
            AttackerCard = atkCard,
            DefenderHost = defender,
            DefenderCard = defCard,
            DefenderOwner = defOwner,
            Summary = $"P{atkOwner} Ship Battle: {atkCard.Name} → {defCard.Name}"
        };
        _stack.Push(action);
        _session.Log.Add(_session.TurnNumber, $"P{atkOwner}",
            $"Battle initiated: {atkCard.Name} vs {defCard.Name}");
        OpenResponseWindow(defOwner);
        ScheduleActionAnnounce(600);
    }

    private void BeginPersonnelBattleStack(
        Border sourceHost, Border targetHost,
        List<Border> atkBorders, List<Border> defBorders,
        List<Card> atkPresent, List<Card> defPresent,
        int atkOwner, int defOwner)
    {
        var action = new TimingRules.PendingAction
        {
            Kind = TimingRules.ActionKind.InitiatePersonnelBattle,
            Controller = atkOwner,
            AttackerHost = sourceHost,
            DefenderHost = targetHost,
            AttackerTeam = atkBorders.Cast<object>().ToList(),
            DefenderTeam = defBorders.Cast<object>().ToList(),
            AttackerPresent = atkPresent,
            DefenderPresent = defPresent,
            DefenderOwner = defOwner,
            Summary = $"P{atkOwner} Personnel Battle"
        };
        _stack.Push(action);
        _session.Log.Add(_session.TurnNumber, $"P{atkOwner}", "Personnel battle initiated");
        OpenResponseWindow(defOwner);
        ScheduleActionAnnounce(600);
    }

    private static int opponentOf(int p) => p == 1 ? 2 : 1;

    private void ApplyResponseEffect(TimingRules.PendingAction response)
    {
        if (_stack.Items.Count < 2 || response.Card == null) return;
        var target = _stack.Items[^2];
        var check = TimingRules.CanRespond(response.Card, target, response.Controller);
        if (!check.ok) return;

        target.Cancelled = true;
        target.CancelledBy = response.Card.Name;
        StatusText.Text = $"Response {response.Card.Name}: {check.reason}";
    }

    private void OpenResponseWindow(int firstResponder)
    {
        _stack.ResponsePlayer = firstResponder;
        _stack.ConsecutivePasses = 0;
        if (ResponsePanel != null) ResponsePanel.Visibility = Visibility.Collapsed;
        BtnPhaseNext.IsEnabled = false;
        BtnEndTurn.IsEnabled = false;
        UpdatePhaseControls();
    }

    private void RefreshResponseUi()
    {
        if (!_stack.IsOpen)
        {
            HideActionAnnounce();
            if (ResponsePanel != null) ResponsePanel.Visibility = Visibility.Collapsed;
            UpdatePhaseControls();
            return;
        }
        if (ResponsePanel != null) ResponsePanel.Visibility = Visibility.Collapsed;
        BtnPhaseNext.IsEnabled = false;
        BtnEndTurn.IsEnabled = false;
        ShowActionAnnounce();
    }

    private void ScheduleActionAnnounce(int delayMs)
    {
        StopAnnounceTimers();
        HideActionAnnounce();
        StatusText.Text = "Card played — announcing shortly…";
        _commitDelayTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(0, delayMs))
        };
        _commitDelayTimer.Tick += (_, _) =>
        {
            _commitDelayTimer.Stop();
            ShowActionAnnounce();
        };
        _commitDelayTimer.Start();
    }

    private void StopAnnounceTimers()
    {
        _commitDelayTimer?.Stop();
        _announceDeadlineTimer?.Stop();
        _commitDelayTimer = null;
        _announceDeadlineTimer = null;
    }

    private void HideActionAnnounce()
    {
        StopAnnounceTimers();
        _announceKind = AnnounceKind.Hidden;
        if (_revealFrame != null) return; // modal reveal in progress
        if (CardRevealOverlay != null && BtnRevealRespond != null)
        {
            BtnRevealRespond.Visibility = Visibility.Collapsed;
            BtnRevealPass.Visibility = Visibility.Collapsed;
            if (RevealAudience != null) RevealAudience.Text = "";
            if (RevealTimerText != null) RevealTimerText.Text = "";
            if (_revealFrame == null)
                CardRevealOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void FillRevealImage(Card? card)
    {
        RevealImage.Source = null;
        if (card == null || string.IsNullOrEmpty(card.FullImagePath) || !System.IO.File.Exists(card.FullImagePath))
            return;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(card.FullImagePath, UriKind.Absolute);
            bmp.DecodePixelWidth = 440;
            bmp.EndInit();
            RevealImage.Source = bmp;
        }
        catch { }
    }

    private void ShowActionAnnounce()
    {
        if (!_stack.IsOpen || _stack.Top == null)
        {
            HideActionAnnounce();
            UpdatePhaseControls();
            return;
        }

        var top = _stack.Top;
        int responder = _stack.ResponsePlayer;
        var hand = responder == 1 ? _handCards : _oppHandCards;
        var legal = TimingRules.LegalResponsesInHand(hand, top, responder);

        Card? shown = top.Card ?? top.AttackerCard;
        RevealTitle.Text = top.IsResponse
            ? $"Response — Player {top.Controller}"
            : $"Player {top.Controller} plays";
        RevealSubtitle.Text = shown != null
            ? $"{shown.Name}  ·  {shown.Type}"
            : top.Summary;
        RevealBody.Text = (shown?.Text ?? top.Summary) + "\n\n" + TimingRules.FormatStack(_stack);

        FillRevealImage(shown);
        if (shown != null) ShowCardDetail(shown);

        BtnRevealYes.Visibility = Visibility.Collapsed;
        BtnRevealNo.Visibility = Visibility.Collapsed;

        // No legal cards for this responder
        if (legal.Count == 0)
        {
            int other = opponentOf(responder);
            var otherHand = other == 1 ? _handCards : _oppHandCards;
            var otherLegal = TimingRules.LegalResponsesInHand(otherHand, top, other);

            // Never open a second "respond to your own card?" window when you have nothing legal.
            // 1E: responses are almost always to the opponent's just-played action / initiation.
            if (responder == top.Controller)
            {
                _stack.ConsecutivePasses++;
                _session.Log.Add(_session.TurnNumber, $"P{responder}", "Pass (no legal response to own action)");
                if (_stack.ConsecutivePasses >= 2 || otherLegal.Count == 0)
                {
                    ResolveEntireStack();
                    return;
                }
                _stack.ResponsePlayer = other;
                ShowActionAnnounce();
                return;
            }

            // Opponent has nothing legal either → single center announce, then resolve
            if (otherLegal.Count == 0)
            {
                _announceKind = AnnounceKind.OkOnly;
                RevealAudience.Text = "No legal responses — resolving.";
                BtnRevealOk.Visibility = Visibility.Visible;
                BtnRevealRespond.Visibility = Visibility.Collapsed;
                BtnRevealPass.Visibility = Visibility.Collapsed;
                CardRevealOverlay.Visibility = Visibility.Visible;
                StatusText.Text = $"{top.Summary} — no responses.";
                StartAnnounceDeadline(5000);
                return;
            }

            // Opponent has nothing, but controller might — brief announce then hand window to controller
            _announceKind = AnnounceKind.OkOnly;
            RevealAudience.Text = $"Player {responder}: no legal response.";
            BtnRevealOk.Visibility = Visibility.Visible;
            BtnRevealRespond.Visibility = Visibility.Collapsed;
            BtnRevealPass.Visibility = Visibility.Collapsed;
            CardRevealOverlay.Visibility = Visibility.Visible;
            StatusText.Text = $"{top.Summary} — P{responder} cannot respond.";
            StartAnnounceDeadline(5000);
            return;
        }

        // Only open the center UI when this player can actually respond
        _announceKind = AnnounceKind.RespondOrPass;
        RevealAudience.Text = $"Player {responder}: respond to this?";
        BtnRevealOk.Visibility = Visibility.Collapsed;
        BtnRevealRespond.Visibility = Visibility.Visible;
        BtnRevealPass.Visibility = Visibility.Visible;
        ShowResponsePlayerHand();

        CardRevealOverlay.Visibility = Visibility.Visible;
        StatusText.Text = $"Player {responder} — response window";
        StartAnnounceDeadline(10000);
    }

    private void StartAnnounceDeadline(int ms)
    {
        _announceDeadlineTimer?.Stop();
        _announceDeadlineUtc = DateTime.UtcNow.AddMilliseconds(ms);
        _announceDeadlineTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _announceDeadlineTimer.Tick += (_, _) =>
        {
            double left = (_announceDeadlineUtc - DateTime.UtcNow).TotalSeconds;
            if (RevealTimerText != null)
                RevealTimerText.Text = left > 0 ? $"{left:0}s remaining" : "";
            if (left > 0) return;
            _announceDeadlineTimer.Stop();
            if (_announceKind == AnnounceKind.OkOnly)
                BtnRevealOk_Click(BtnRevealOk, new RoutedEventArgs());
            else if (_announceKind is AnnounceKind.RespondOrPass or AnnounceKind.PickCard)
                BtnRevealPass_Click(BtnRevealPass, new RoutedEventArgs());
        };
        _announceDeadlineTimer.Start();
        if (RevealTimerText != null) RevealTimerText.Text = $"{ms / 1000}s remaining";
    }

    private void BtnRevealRespond_Click(object sender, RoutedEventArgs e)
    {
        if (!_stack.IsOpen) return;
        _announceKind = AnnounceKind.PickCard;
        RevealAudience.Text = $"Player {_stack.ResponsePlayer}: click a legal card in hand.";
        BtnRevealRespond.Visibility = Visibility.Collapsed;
        BtnRevealPass.Visibility = Visibility.Visible;
        BtnRevealOk.Visibility = Visibility.Collapsed;
        ShowResponsePlayerHand();
        StartAnnounceDeadline(10000);
    }

    private void BtnRevealPass_Click(object sender, RoutedEventArgs e)
    {
        if (!_stack.IsOpen)
        {
            HideActionAnnounce();
            return;
        }
        HideActionAnnounce();
        BtnResponsePass_Click(BtnResponsePass, new RoutedEventArgs());
    }

    private void ShowResponsePlayerHand()
    {
        if (_seedPhaseActive) return;
        ShowStackContents("Hand", opponent: _stack.ResponsePlayer == 2);
        HighlightLegalResponsesInStack();
    }

    private void HighlightLegalResponsesInStack()
    {
        if (!_stack.IsOpen || _stack.Top == null) return;
        var panel = _stack.ResponsePlayer == 2 ? OppStripPanel : PlayerStripPanel;
        if (panel == null) return;
        foreach (var child in panel.Children)
        {
            if (child is not Border mini || mini.Tag is not ZoneCardRef zref) continue;
            if (!_aidLegalResponses)
            {
                mini.BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));
                mini.BorderThickness = new Thickness(1);
                mini.Opacity = 1.0;
                continue;
            }
            var cr = TimingRules.CanRespond(zref.Card, _stack.Top, _stack.ResponsePlayer);
            mini.BorderBrush = cr.ok
                ? new SolidColorBrush(Color.FromRgb(180, 80, 220))
                : new SolidColorBrush(Color.FromRgb(70, 70, 70));
            mini.BorderThickness = new Thickness(cr.ok ? 3 : 1);
            mini.Opacity = cr.ok ? 1.0 : 0.55;
        }
    }

    private bool TryPlayAsResponse(ZoneCardRef zref)
    {
        if (!_stack.IsOpen || _stack.Top == null) return false;
        if (zref.ZoneName != "Hand") return false;
        int owner = zref.Opponent ? 2 : 1;
        if (owner != _stack.ResponsePlayer) return false;

        var cr = TimingRules.CanRespond(zref.Card, _stack.Top, owner);
        if (!cr.ok)
        {
            ShowCardDetail(zref.Card);
            return false;
        }

        var hand = owner == 1 ? _handCards : _oppHandCards;
        hand.Remove(zref.Card);
        RefreshZoneCounts();
        BeginPlayCardStack(zref.Card, isResponse: true);
        return true;
    }

    private void BtnResponsePass_Click(object sender, RoutedEventArgs e)
    {
        if (!_stack.IsOpen) return;
        _stack.ConsecutivePasses++;
        _session.Log.Add(_session.TurnNumber, $"P{_stack.ResponsePlayer}", "Pass (response)");
        if (_stack.ConsecutivePasses >= 2)
        {
            ResolveEntireStack();
            return;
        }
        _stack.ResponsePlayer = opponentOf(_stack.ResponsePlayer);
        RefreshResponseUi();
        ShowResponsePlayerHand();
    }

    private void ResolveEntireStack()
    {
        HideActionAnnounce();
        while (_stack.IsOpen)
            ResolveTopOfStack();
        RefreshResponseUi();
        ShowActivePlayerHand();
        UpdatePhaseControls();
        RefreshZoneCounts();
    }

    private void ResolveTopOfStack()
    {
        if (!_stack.IsOpen) return;
        var a = _stack.Pop();

        if (a.Kind == TimingRules.ActionKind.PlayCard && a.Card != null)
        {
            if (a.Cancelled)
            {
                var dest = a.CancelledBy != null
                           && a.CancelledBy.Equals("Energy Vortex", StringComparison.OrdinalIgnoreCase)
                    ? TimingRules.Destination.ReturnToHand
                    : TimingRules.Destination.Discard;
                SendCardTo(a.Card, a.Controller, dest);
                StatusText.Text = $"{a.Card.Name} cancelled ({a.CancelledBy}).";
                _session.Log.Add(_session.TurnNumber, $"P{a.Controller}",
                    $"{a.Card.Name} cancelled by {a.CancelledBy}");
                return;
            }

            if (a.IsResponse)
            {
                // Kevin (etc.) may still need TargetCard nullify when used as a response
                if ((TimingRules.IsInterrupt(a.Card) || InterruptRules.IsInterrupt(a.Card))
                    && a.TargetCard != null)
                {
                    TryResolveInterruptPlay(a.Card, a.Controller, isResponse: true, a.TargetCard);
                }
                else
                {
                    SendCardTo(a.Card, a.Controller, TimingRules.SelfDestination(a.Card));
                }
                return;
            }

            // Ungestörte Card Play
            if (ArtifactRules.IsArtifact(a.Card) && TryResolveArtifactHandPlay(a.Card, a.Controller))
            {
                // Spezial-Effekt erledigt
            }
            else if (EventRules.IsEvent(a.Card) && TryResolveEventPlay(a.Card, a.Controller))
            {
                // Event-Effekt / Attach
            }
            else if (TimingRules.IsInterrupt(a.Card) || InterruptRules.IsInterrupt(a.Card))
            {
                if (!TryResolveInterruptPlay(a.Card, a.Controller, a.IsResponse, a.TargetCard))
                {
                    SendCardTo(a.Card, a.Controller, TimingRules.Destination.Discard);
                    StatusText.Text = $"P{a.Controller}: Interrupt {a.Card.Name} resolved (Discard).";
                }
            }
            else if (EventBelongsOnTableColumn(a.Card))
            {
                CommitCardToTable(a.Card, a.Controller);
            }

            if (GameSession.UsesNormalCardPlay(a.Card) && !PlayRules.PlaysForFree(a.Card))
                OnSuccessfulHandPlay(a.Card, "Hand");
            return;
        }

        if (a.Kind == TimingRules.ActionKind.InitiateShipBattle)
        {
            var atkB = a.AttackerHost as Border;
            var defB = a.DefenderHost as Border;
            if (atkB == null || defB == null || a.AttackerCard == null || a.DefenderCard == null)
                return;

            if (a.Cancelled)
            {
                // Cancelled battle: Teilnehmer trotzdem gestoppt
                MarkStopped(atkB);
                MarkStopped(defB);
                StatusText.Text =
                    $"Ship Battle cancelled ({a.CancelledBy}) – both ships stopped.";
                _session.Log.Add(_session.TurnNumber, $"P{a.Controller}",
                    $"Battle cancelled by {a.CancelledBy}");
                return;
            }

            AskReturnFireAndResolve(atkB, a.AttackerCard, defB, a.DefenderCard);
            return;
        }

        if (a.Kind == TimingRules.ActionKind.InitiatePersonnelBattle)
        {
            if (a.Cancelled)
            {
                StopPersonnelTeam(a.AttackerTeam);
                StopPersonnelTeam(a.DefenderTeam);
                StatusText.Text =
                    $"Personnel Battle cancelled ({a.CancelledBy}) – forces stopped.";
                return;
            }

            ResolvePendingPersonnelBattle(a);
        }
    }

    private void SendCardTo(Card card, int owner, TimingRules.Destination dest)
    {
        switch (dest)
        {
            case TimingRules.Destination.ReturnToHand:
                ReturnCardToHand(card, owner);
                break;
            case TimingRules.Destination.OutOfPlay:
                (owner == 2 ? _outOfPlayP2 : _outOfPlayP1).Add(card);
                StatusText.Text = $"{card.Name} out-of-play.";
                break;
            case TimingRules.Destination.Table:
                CommitCardToTable(card, owner);
                break;
            default:
                _tablePermanentCards.Remove(card);
                _oppTablePermanentCards.Remove(card);
                var list = owner == 2 ? _oppDiscardCards : _discardCards;
                if (!list.Contains(card)) list.Add(card);
                RebuildTablePermanentsPanel();
                RefreshZoneCounts();
                break;
        }
    }

    private void ReturnCardToHand(Card card, int owner)
    {
        _tablePermanentCards.Remove(card);
        _oppTablePermanentCards.Remove(card);
        var hand = owner == 2 ? _oppHandCards : _handCards;
        if (!hand.Contains(card)) hand.Add(card);
        RebuildTablePermanentsPanel();
        RefreshZoneCounts();
    }

    private void StopPersonnelTeam(List<object>? team)
    {
        if (team == null) return;
        foreach (var o in team)
        {
            if (o is Border b)
                MarkStopped(b);
        }
    }

    private void AskReturnFireAndResolve(
        Border attackerBorder, Card attackerShip,
        Border defenderBorder, Card defenderCard)
    {
        int defOwner = GetBorderOwner(defenderBorder);
        if (defOwner == 0) defOwner = 2;
        bool returnFire = false;
        int defWeapons = BattleRules.GetWeapons(defenderCard);
        if (defWeapons > 0 && !IsBorderStopped(defenderBorder))
        {
            var rf = MessageBox.Show(
                $"Ship Battle: {attackerShip.Name} greift {defenderCard.Name} an.\n\n" +
                $"Angreifer WEAPONS {BattleRules.GetWeapons(attackerShip)} · " +
                $"Ziel SHIELDS {BattleRules.GetShields(defenderCard)}.\n\n" +
                $"Verteidiger (S{defOwner}): Return Fire?\n" +
                $"(target would have WEAPONS {defWeapons})",
                "Ship Battle – Return Fire",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            returnFire = rf == MessageBoxResult.Yes;
        }
        ResolveShipBattle(attackerBorder, attackerShip, defenderBorder, defenderCard, returnFire);
    }

    private void ResolvePendingPersonnelBattle(TimingRules.PendingAction a)
    {
        if (a.AttackerTeam == null || a.DefenderTeam == null) return;
        var atkBorders = a.AttackerTeam.OfType<Border>().ToList();
        var defBorders = a.DefenderTeam.OfType<Border>().ToList();
        var atkCards = atkBorders.Where(b => b.Tag is Card).Select(b => (Card)b.Tag!).ToList();
        var defCards = defBorders.Where(b => b.Tag is Card).Select(b => (Card)b.Tag!).ToList();
        int atkOwner = a.Controller;
        int defOwner = a.DefenderOwner;
        var result = BattleRules.ResolvePersonnelBattle(
            atkCards, defCards, null,
            a.AttackerPresent, a.DefenderPresent, atkOwner, defOwner);
        if (!result.Ok)
        {
            ShowPlayError(result.Reason);
            return;
        }

        var killedSet = new HashSet<string>(result.KilledNames, StringComparer.OrdinalIgnoreCase);
        void KillFrom(List<Border> borders, int owner)
        {
            foreach (var b in borders.ToList())
            {
                if (b.Tag is not Card c) continue;
                if (!killedSet.Contains(c.Name ?? "")) continue;
                DiscardPersonnelBorder(b, c, owner);
            }
        }
        KillFrom(atkBorders, atkOwner);
        KillFrom(defBorders, defOwner);
        foreach (var b in atkBorders) MarkStopped(b);
        foreach (var b in defBorders)
        {
            if (b.Tag is Card c && killedSet.Contains(c.Name ?? "")) continue;
            MarkStopped(b);
        }

        _session.Log.Add(_session.TurnNumber, $"P{atkOwner}", result.LogSummary);
        MessageBox.Show(result.LogSummary, "Personnel Battle", MessageBoxButton.OK, MessageBoxImage.Information);
        StatusText.Text = result.LogSummary.Replace('\n', ' ');
        if (a.AttackerHost is Border src && src.Tag is Card sc)
            ShowHostContents(src, sc);
        else if (a.DefenderHost is Border dst && dst.Tag is Card dc)
            ShowHostContents(dst, dc);
    }

    private bool TryAllowHandPlay(Card card, string sourceZone, out string denyReason)
    {
        denyReason = "";
        if (sourceZone != "Hand") return true;
        if (_seedPhaseActive) return true;
        if (_session.Match != GameSession.MatchPhase.Play) return true;

        // Stack offen: nur gültige Responses des Spielers, der am Zug der Response ist
        if (_stack.IsOpen)
        {
            if (_stack.Top == null)
            {
                denyReason = "Action-Stack ist inkonsistent.";
                return false;
            }
            var cr = TimingRules.CanRespond(card, _stack.Top, _stack.ResponsePlayer);
            if (!cr.ok)
            {
                denyReason = cr.reason + " Other at-any-time cards only after the stack is empty.";
                return false;
            }
            return true;
        }

        // Interrupt / Doorway: at any time
        if (!GameSession.UsesNormalCardPlay(card))
        {
            if (TimingRules.IsInterrupt(card) && GoddessBlocksInterrupt(card))
            {
                denyReason = "Goddess of Empathy: interrupts may not be played (except Kevin/Q2/Q/Ref).";
                return false;
            }
            return true;
        }

        bool forFree = PlayRules.PlaysForFree(card);

        // Alle Card Plays (auch for free) vor Execute
        if (_session.Segment != GameSession.TurnSegment.Play)
        {
            denyReason =
                "Hand cards only in the Play segment (before Execute). "
                + "Interrupts/Doorways weiterhin jederzeit.";
            return false;
        }

        if (forFree)
            return true; // unbegrenzt, zählt nicht

        if (_session.NormalCardPlayUsed)
        {
            if (_redAlertPlaysLeft > 0
                && (ModifierRules.IsPersonnelCard(card) || ModifierRules.IsEquipmentCard(card)))
                return true;
            // Horga'hn: eine zusätzliche Normal-Play pro Zug
            if (HasHorgahn(_activePlayer) && !_horgahnExtraPlayUsed)
                return true;
            denyReason =
                "Normal card play bereits genutzt (1 Personal/Schiff/Event pro Zug). "
                + "Picard & Co. next turn or if the card plays for free. "
                + "Interrupts/Doorways jederzeit.";
            return false;
        }

        return true;
    }

    private void OnSuccessfulHandPlay(Card card, string sourceZone)
    {
        if (sourceZone != "Hand") return;
        if (_seedPhaseActive) return;
        if (_session.Match != GameSession.MatchPhase.Play) return;
        if (!GameSession.UsesNormalCardPlay(card)) return;

        if (_redAlertPlaysLeft > 0
            && (ModifierRules.IsPersonnelCard(card) || ModifierRules.IsEquipmentCard(card)))
        {
            if (!_session.NormalCardPlayUsed)
                _session.MarkNormalCardPlay("Red Alert!");
            _redAlertPlaysLeft--;
            _session.Log.Add(_session.TurnNumber, $"P{_session.ActivePlayer}",
                $"Red Alert play: {card.Name} ({_redAlertPlaysLeft} left)");
            StatusText.Text = _redAlertPlaysLeft > 0
                ? $"Red Alert: {card.Name} reported. {_redAlertPlaysLeft} personnel/equipment still allowed this turn."
                : $"Red Alert: {card.Name} reported. That was the last of 5 this turn.";
            UpdatePhaseControls();
            return;
        }

        if (PlayRules.PlaysForFree(card))
        {
            _session.Log.Add(_session.TurnNumber, $"P{_session.ActivePlayer}",
                $"For free: {card.Name}");
            StatusText.Text = $"{card.Name} plays for free (does not count as normal card play).";
            UpdatePhaseControls();
            return;
        }

        if (_session.NormalCardPlayUsed && HasHorgahn(_activePlayer) && !_horgahnExtraPlayUsed)
        {
            _horgahnExtraPlayUsed = true;
            _session.Log.Add(_session.TurnNumber, $"P{_session.ActivePlayer}",
                $"Horga'hn extra play: {card.Name}");
            StatusText.Text = $"Horga'hn: extra card play {card.Name}.";
            // Bleibt im Play-Segment bis manuell Execute – oder auto Execute
            if (_session.Segment == GameSession.TurnSegment.Play)
            {
                _session.AdvanceSegment();
                SyncSessionToUi();
                OnTurnContextChanged($"Horga'hn-Play {card.Name} · → Execute.");
            }
            UpdatePhaseControls();
            return;
        }

        _session.MarkNormalCardPlay(card.Name ?? "?");
        // Normale Card Play verbraucht → Play-Segment abgeschlossen → Execute
        // Ausnahme: Horga'hn noch verfügbar → im Play bleiben
        if (_session.Segment == GameSession.TurnSegment.Play
            && _session.NormalCardPlayUsed
            && !_session.NormalCardPlayForfeited
            && !(HasHorgahn(_activePlayer) && !_horgahnExtraPlayUsed))
        {
            _session.AdvanceSegment();
            SyncSessionToUi();
            OnTurnContextChanged($"{card.Name} played · auto → Execute (orders).");
            UpdatePhaseControls();
            return;
        }
        if (HasHorgahn(_activePlayer) && !_horgahnExtraPlayUsed)
        {
            StatusText.Text =
                $"{card.Name} played. Horga'hn: 1 extra card play still available.";
            UpdatePhaseControls();
            return;
        }
        UpdatePhaseControls();
    }

    /// <summary>Alle Karten "im Spiel" für Unique-Checks (Hosts, Dockables, Tisch, …).</summary>
    private List<Card> CollectCardsInPlay(bool opponent)
    {
        var list = new List<Card>();
        void Add(Card? c)
        {
            if (c != null && !list.Contains(c)) list.Add(c);
        }

        // Tisch-Permanents
        foreach (var c in opponent ? _oppTablePermanentCards : _tablePermanentCards)
            Add(c);

        // Canvas: sichtbare Karten des Besitzers + Host-Stapel
        int owner = opponent ? 2 : 1;
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Tag is not Card c) continue;
            int o = GetBorderOwner(b);
            if (o == 0) o = 1;
            if (o != owner) continue;
            // Missionen zählen für not-duplicatable / shared später
            Add(c);
            if (_stackOnHost.TryGetValue(b, out var stacked))
            {
                foreach (var sb in stacked)
                    if (sb.Tag is Card sc) Add(sc);
            }
        }

        return list;
    }

    private List<Card> CollectAllCardsInPlay()
    {
        var a = CollectCardsInPlay(false);
        var b = CollectCardsInPlay(true);
        foreach (var c in b)
            if (!a.Contains(c)) a.Add(c);
        return a;
    }

    private bool TryEnterPlayCheck(Card card, out string denyReason)
    {
        denyReason = "";
        if (_seedPhaseActive) return true;
        if (_session.Match != GameSession.MatchPhase.Play) return true;

        var owned = CollectCardsInPlay(_activePlayer == 2);
        var all = CollectAllCardsInPlay();
        var result = PlayRules.CanEnterPlay(card, owned, all);
        if (!result.Ok)
        {
            denyReason = result.Reason;
            return false;
        }
        return true;
    }

    private void ZoneMini_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border mini)
        {
            mini.ReleaseMouseCapture();
            mini.MouseMove -= ZoneMini_MouseMove;
            mini.MouseLeftButtonUp -= ZoneMini_MouseUp;
        }

        if (_zoneDragRef == null)
            return;

        var zref = _zoneDragRef;
        _zoneDragRef = null;

        if (!_panelDragging)
        {
            // Nur Detail
            return;
        }

        HideSnapPreview();
        ClearEventTargetHighlights();
        var cardBorder = _dragCard;
        _dragCard = null;
        _isDragging = false;
        _panelDragging = false;

        if (cardBorder == null)
            return;

        var card = zref.Card;
        bool placedOk = false;
        var windowPos = e.GetPosition(this);

        // Normal card play / for free / Timing (nur aus der Hand, Spielphase)
        if (!TryAllowHandPlay(card, zref.ZoneName, out string handDeny))
        {
            ShowPlayError(handDeny);
            ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
            RefreshZoneCounts();
            return;
        }

        // 6.2 Entering Play – Unique / Universal / not duplicatable
        if (zref.ZoneName == "Hand" && !_seedPhaseActive
            && !TryEnterPlayCheck(card, out string enterDeny))
        {
            ShowPlayError(enterDeny);
            ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
            RefreshZoneCounts();
            return;
        }

        // Overlay-Karte vom DragLayer nehmen
        if (_dragOnOverlay && DragLayer.Children.Contains(cardBorder))
            DragLayer.Children.Remove(cardBorder);
        _dragOnOverlay = false;
        ClearZoneHighlight();
        SetTischZoneHighlight(false);
        HighlightDiscardZone(false);
        cardBorder.Width = TableCardWidth;
        cardBorder.Height = TableCardHeight;

        // TABLE drop first — do not treat the right column as "return to hand"
        if (IsTablePermanentType(card) && IsPointOverOwnTable(windowPos)
            && EventBelongsOnTableColumn(card))
        {
            PlaceOnTablePermanents(card, cardBorder);
            SetTischZoneHighlight(false);
            placedOk = true;
        }
        // Targeted interrupt onto crew/ship in the same strip must run BEFORE "return to hand"
        else if (!_seedPhaseActive && InterruptRules.IsInterrupt(card)
                 && InterruptRules.NeedsDropTarget(card) && zref.ZoneName == "Hand")
        {
            int owner = zref.Opponent ? 2 : 1;
            if (TryPlayInterruptFromHand(card, cardBorder, windowPos, owner))
                placedOk = true;
            else if (TryReturnCardToZone(card, cardBorder, zref.ZoneName, windowPos, zref.Opponent))
                placedOk = true;
            else
            {
                ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
                placedOk = false;
            }
        }
        // Zurück in den Quell-Stapel (Maus über Zone oder Hand-Leiste, nicht TABLE)
        else if (TryReturnCardToZone(card, cardBorder, zref.ZoneName, windowPos, zref.Opponent))
        {
            placedOk = true;
        }
        // Auf Discard legen (Spielphase oder bewusst abwerfen)
        else if (TryPlaceOnDiscard(card, cardBorder, windowPos))
        {
            placedOk = true;
        }
        // Doorway auf Side-Deck legen → entsperren (Deckblatt)
        else if (IsDoorwayCard(card) && TryPlaceDoorwayOnSideDeck(card, cardBorder, windowPos))
        {
            placedOk = true;
        }
        else if (!_seedPhaseActive && InterruptRules.IsInterrupt(card) && zref.ZoneName == "Hand")
        {
            int owner = zref.Opponent ? 2 : 1;
            if (TryPlayInterruptFromHand(card, cardBorder, windowPos, owner))
                placedOk = true;
            else
            {
                ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
                placedOk = false;
            }
        }
        else if (IsMissionCard(card) && _seedPhaseActive)
        {
            CommitFloatingToCanvas(cardBorder, windowPos);
            PlaceMissionOnSpaceline(cardBorder, card);
            SetBorderOwner(cardBorder, _activePlayer);

            SetSelection(cardBorder);
            placedOk = true;
        }
        else if (IsSeedableUnderMission(card)
                 && (_seedPhaseActive || (_devPlaySeedFromHand && zref.ZoneName == "Hand" && !_seedPhaseActive)))
        {
            // Dilemma/Artifact unter legaler Mission (Seed-Phase oder Dev-Ausnahme)
            CommitFloatingToCanvas(cardBorder, windowPos);
            var targetMission = TrySnapToMission(cardBorder);
            if (targetMission != null && targetMission.Tag is Card mc
                && CanSeedCardUnderMission(card, mc).ok)
            {
                if (_devPlaySeedFromHand && !_seedPhaseActive && ArtifactRules.IsArtifact(card))
                {
                    // Test path: acquire immediately at this mission (crew present)
                    if (DragLayer.Children.Contains(cardBorder))
                        DragLayer.Children.Remove(cardBorder);
                    if (TableCanvas.Children.Contains(cardBorder))
                        TableCanvas.Children.Remove(cardBorder);
                    ApplyArtifactAcquire(card, targetMission, mc);
                    SetSelection(targetMission);
                    StatusText.Text = $"Dev: acquired {card.Name} at {mc.Name}.";
                    _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                        $"Dev acquire {card.Name} @ {mc.Name}");
                }
                else
                {
                    AddSeedUnderMission(targetMission, cardBorder);
                    SetSelection(targetMission);
                    if (_devPlaySeedFromHand && !_seedPhaseActive)
                        StatusText.Text = $"Dev: seeded {card.Name} under {mc.Name} (attempt to encounter).";
                }
                placedOk = true;
            }
            else
            {
                string reason = targetMission?.Tag is Card m2
                    ? CanSeedCardUnderMission(card, m2).reason
                    : $"{card.Name} braucht eine passende Mission (Planet/Space).";
                StatusText.Text = reason;
                ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
                placedOk = false;
            }
        }
        else if (IsFacilityCard(card))
        {
            // Outpost/Facility nur unter legaler Mission (Planet für Outposts)
            CommitFloatingToCanvas(cardBorder, windowPos);
            var targetMission = FindNearestLegalFacilityMission(card, cardBorder, out _);
            if (targetMission != null)
            {
                int owner = GetBorderOwner(cardBorder);
                if (owner == 0) owner = _activePlayer;
                SetBorderOwner(cardBorder, owner);
                Canvas.SetLeft(cardBorder, Canvas.GetLeft(targetMission));
                Canvas.SetTop(cardBorder, Canvas.GetTop(targetMission) + DockSlotOffsetY(0, owner));
                RelayoutDockablesUnderMission(targetMission);
                UpdateHostBadge(cardBorder);
                SetSelection(cardBorder);
                if (targetMission.Tag is Card tm)
                    StatusText.Text = $"{card.Name} an {tm.Name}.";
                placedOk = true;
            }
            else
            {
                StatusText.Text = $"{card.Name} needs a matching planet mission (outposts not at space).";
                ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
                placedOk = false;
            }
        }
        else if (IsShipCard(card))
        {
            CommitFloatingToCanvas(cardBorder, windowPos);
            int owner = zref.Opponent ? 2 : 1;
            SetBorderOwner(cardBorder, owner);

            bool fromHandPlay = zref.ZoneName == "Hand" && !_seedPhaseActive
                && _session.Match == GameSession.MatchPhase.Play;

            if (fromHandPlay)
            {
                // 6.3: Schiff reportet an usable/compatible Facility — snap only onto that facility
                double fw = cardBorder.Width > 0 ? cardBorder.Width : TableCardWidth;
                double fh = cardBorder.Height > 0 ? cardBorder.Height : TableCardHeight;
                double fcx = Canvas.GetLeft(cardBorder) + fw / 2.0;
                double fcy = Canvas.GetTop(cardBorder) + fh / 2.0;
                var facility = FindNearestLegalReportHost(fcx, fcy, owner, card, out double fdist);
                if (facility == null || fdist > ShipSnapRange * 1.8)
                {
                    StatusText.Text =
                        $"{card.Name}: snap onto your matching Outpost/HQ to report "
                        + "(not onto a mission or ship).";
                    ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
                    placedOk = false;
                }
                else
                {
                    // Visuell an derselben Mission-Spalte wie die Facility
                    var mission = FindMissionForDockable(facility) ?? TrySnapToMission(facility);
                    if (mission != null)
                    {
                        Canvas.SetLeft(cardBorder, Canvas.GetLeft(mission));
                        Canvas.SetTop(cardBorder, Canvas.GetTop(mission) + DockSlotOffsetY(0, owner));
                        RelayoutDockablesUnderMission(mission);
                    }
                    else
                    {
                        Canvas.SetLeft(cardBorder, Canvas.GetLeft(facility));
                        Canvas.SetTop(cardBorder, Canvas.GetTop(facility) + DockSlotOffsetY(0, owner));
                    }
                    UpdateHostBadge(cardBorder);
                    SetSelection(cardBorder);
                    if (facility.Tag is Card fc)
                        StatusText.Text = $"P{owner}: Ship {card.Name} reports for duty at {fc.Name}.";
                    placedOk = true;
                }
            }
            else
            {
                // Seed / Bewegung: an Mission docken
                var targetMission = TrySnapToMission(cardBorder);
                if (targetMission != null && targetMission.Tag is Card tmc
                    && string.Equals(tmc.Type, "Mission", StringComparison.OrdinalIgnoreCase))
                {
                    Canvas.SetLeft(cardBorder, Canvas.GetLeft(targetMission));
                    Canvas.SetTop(cardBorder, Canvas.GetTop(targetMission) + DockSlotOffsetY(0, owner));
                    RelayoutDockablesUnderMission(targetMission);
                    UpdateHostBadge(cardBorder);
                    SetSelection(cardBorder);
                    StatusText.Text = $"P{owner}: Ship {card.Name} at {tmc.Name}.";
                    placedOk = true;
                }
                else
                {
                    StatusText.Text = $"{card.Name} place at a mission.";
                    ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
                    placedOk = false;
                }
            }
        }
        else if (IsStackableCard(card))
        {
            CommitFloatingToCanvas(cardBorder, windowPos);
            int owner = zref.Opponent ? 2 : 1;
            SetBorderOwner(cardBorder, owner);
            bool fromHand = zref.ZoneName == "Hand";
            bool isReport = fromHand && !_seedPhaseActive
                && _session.Match == GameSession.MatchPhase.Play
                && ReportingRules.MustReportForDuty(card);
            var host = TrySnapToHost(cardBorder, owner, reportTargetsOnly: isReport);
            if (host != null && host.Tag is Card hostCard)
            {
                if (isReport)
                {
                    var (ok, reason) = CanReportToHost(card, hostCard, host);
                    if (!ok)
                    {
                        ShowPlayError(reason);
                        ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
                        placedOk = false;
                    }
                    else
                    {
                        AddCardToHostStack(host, cardBorder);
                        SetSelection(host);
                        StatusText.Text = $"P{owner}: {card.Name} reports for duty at {hostCard.Name}.";
                        placedOk = true;
                    }
                }
                else
                {
                    // Seed / Execute / bereits im Spiel: Host-Stapel ohne Report-Check
                    AddCardToHostStack(host, cardBorder);
                    SetSelection(host);
                    placedOk = true;
                }
            }
            else
            {
                int facCount = TableCanvas.Children.OfType<Border>()
                    .Count(b => b.Tag is Card fc && ReportingRules.IsFacilityHost(fc)
                                && GetBorderOwner(b) == owner);
                ShowPlayError(
                    $"{card.Name}: no valid report target (snap). " +
                    $"Your facilities for P{owner}: {facCount}. " +
                    "Drag onto the Outpost/HQ (not the mission).");
                ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
                placedOk = false;
            }
        }
        else if (IsTablePermanentType(card))
        {
            // Events with a table host: prefer drag-snap onto ship/mission/gap
            if (EventRules.IsEvent(card) && !_seedPhaseActive)
            {
                var er = EventRules.ResolvePlay(card);
                var tk = EventRules.GetTargetKind(er);
                if (EventRules.NeedsTableHost(tk))
                {
                    // Floating card is on DragLayer (window coords) — convert drop point to table space
                    var tablePt = WindowToTablePoint(windowPos);
                    var snapped = TrySnapEventTargetAt(tablePt.X, tablePt.Y, tk, zref.Opponent ? 2 : 1, card);
                    if (snapped.host != null)
                    {
                        _eventPreferredHost = snapped.host;
                        _eventPreferredHost2 = snapped.host2;
                        string h1 = (snapped.host.Tag as Card)?.Name ?? "?";
                        string h2 = snapped.host2 != null ? ((snapped.host2.Tag as Card)?.Name ?? "?") : "";
                        StatusText.Text = string.IsNullOrEmpty(h2)
                            ? $"{card.Name} → target {h1}"
                            : $"{card.Name} → gap {h1} ↔ {h2}";
                    }
                    else
                    {
                        StatusText.Text = $"{card.Name}: no legal snap — choose target from the menu when the card resolves.";
                    }
                }
            }
            PlaceOnTablePermanents(card, cardBorder);
            SetTischZoneHighlight(false);
            placedOk = true;
        }
        else
        {
            // Kein freies Ablegen auf dem Canvas – zurück in Quell-Stapel
            StatusText.Text = $"{card.Name} has no legal placement – returned to {zref.ZoneName}.";
            ReturnFloatingToZone(card, cardBorder, zref.ZoneName, zref.Opponent);
            placedOk = false;
        }

        RefreshZoneCounts();

        // Normal card play nur wenn die Karte wirklich ins Spiel ging (nicht Discard/Zurück)
        if (placedOk && !_seedPhaseActive && zref.ZoneName == "Hand")
        {
            bool stillInHand = (zref.Opponent ? _oppHandCards : _handCards).Contains(card);
            bool inDiscard = (zref.Opponent ? _oppDiscardCards : _discardCards).Contains(card);
            if (!stillInHand && !inDiscard && !_stack.IsOpen)
                OnSuccessfulHandPlay(card, zref.ZoneName);
        }

        if (placedOk && _seedPhaseActive)
        {
            if (_seedSubPhase == SeedSubPhase.Doorway)
                TrySequentialSeedPlayer(); // P1 alle Doorways, dann P2
            else if (_seedSubPhase is SeedSubPhase.Mission or SeedSubPhase.Dilemma or SeedSubPhase.Facility)
                TryAlternateSeedPlayer();
            TryAutoAdvanceSeedPhase();
            // Immer den aktuellen Seed-Stapel des aktiven Spielers zeigen
            ShowCurrentSeedStack();
        }
        else if (IsSideDeckUnlocked(zref.ZoneName, zref.Opponent) || !IsSideDeckZone(zref.ZoneName))
        {
            ShowStackContents(zref.ZoneName, opponent: zref.Opponent);
        }
    }

    /// <summary>Karte auf den Discard-Stapel legen (Maus über Discard-Zone).</summary>
    private bool TryPlaceOnDiscard(Card card, Border cardBorder, Point windowPos)
    {
        bool opp = _activePlayer == 2;
        var discardBox = FindZoneBorder("Discard", opponent: opp);
        if (discardBox == null || !IsPointNearElement(discardBox, windowPos, 28))
            return false;

        if (DragLayer.Children.Contains(cardBorder))
            DragLayer.Children.Remove(cardBorder);
        if (TableCanvas.Children.Contains(cardBorder))
            TableCanvas.Children.Remove(cardBorder);
        _tablePermanentCards.Remove(card);
        _oppTablePermanentCards.Remove(card);

        foreach (var kv in _stackOnHost.ToList())
            kv.Value.Remove(cardBorder);
        foreach (var kv in _seedUnderMission.ToList())
            kv.Value.Remove(cardBorder);

        var discardList = opp ? _oppDiscardCards : _discardCards;
        if (!discardList.Contains(card))
            discardList.Add(card);

        ClearZoneHighlight();
        SetTischZoneHighlight(false);
        RefreshZoneCounts();
        ShowStackContents("Discard", opponent: opp);
        StatusText.Text = $"P{_activePlayer} Discard: {card.Name} ({discardList.Count})";
        return true;
    }

    /// <summary>Schwebekarte verwerfen und Karte wieder in den genannten Stapel legen.</summary>
    private void ReturnFloatingToZone(Card card, Border cardBorder, string zoneName, bool opponent = false)
    {
        if (DragLayer.Children.Contains(cardBorder))
            DragLayer.Children.Remove(cardBorder);
        if (TableCanvas.Children.Contains(cardBorder))
            TableCanvas.Children.Remove(cardBorder);
        _tablePermanentCards.Remove(card);

        var list = GetZoneList(zoneName, opponent);
        if (list != null && !list.Contains(card))
            list.Add(card);

        ClearZoneHighlight();
        SetTischZoneHighlight(false);
        RefreshZoneCounts();
        ShowStackContents(zoneName, opponent: opponent);
    }

    private List<Card>? GetZoneList(string zoneName, bool opponent = false)
    {
        bool p1 = !opponent;
        return zoneName switch
        {
            "Hand" => p1 ? _handCards : _oppHandCards,
            "Seed Deck" => p1 ? _seedCards : _oppSeedCards,
            "Doorways" => p1 ? _doorwayCards : _oppDoorwayCards,
            "Missions" => p1 ? _missionSeedCards : _oppMissionSeedCards,
            "Dilemmas" => p1 ? _dilemmaSeedCards : _oppDilemmaSeedCards,
            "Facilities" => p1 ? _facilitySeedCards : _oppFacilitySeedCards,
            "Draw Deck" => p1 ? _drawCards : _oppDrawCards,
            "Side Deck" => p1 ? _sideCards : _oppSideCards,
            "Q's Tent" => p1 ? _qsTentCards : _oppQsTentCards,
            "Battle Bridge" => p1 ? _battleBridgeCards : _oppBattleBridgeCards,
            "Q-Continuum" => p1 ? _qContinuumCards : _oppQContinuumCards,
            "Site Pile" => p1 ? _sitePileCards : _oppSitePileCards,
            "Tribble" => p1 ? _tribbleCards : _oppTribbleCards,
            "Discard" => p1 ? _discardCards : _oppDiscardCards,
            _ => null
        };
    }



    private Border? FindNearestLegalFacilityMission(Card facility, Border cardBorder, out double distance)
    {
        double w = cardBorder.Width > 0 ? cardBorder.Width : TableCardWidth;
        double h = cardBorder.Height > 0 ? cardBorder.Height : TableCardHeight;
        double cx = Canvas.GetLeft(cardBorder) + w / 2.0;
        double cy = Canvas.GetTop(cardBorder) + h / 2.0;

        Border? nearest = null;
        distance = double.MaxValue;
        foreach (var m in TableCanvas.Children.OfType<Border>()
                     .Where(b => b.Visibility == Visibility.Visible
                                 && b != _hostHighlight
                                 && b.Tag is Card c
                                 && string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase)))
        {
            if (m.Tag is not Card mc) continue;
            if (!CanSeedFacilityAtMission(facility, mc).ok) continue;

            double mw = m.Width > 0 ? m.Width : TableCardWidth;
            double mh = m.Height > 0 ? m.Height : TableCardHeight;
            double mx = Canvas.GetLeft(m) + mw / 2.0;
            double my = Canvas.GetTop(m) + mh / 2.0;
            double dx = cx - mx, dy = cy - my;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < distance)
            {
                distance = dist;
                nearest = m;
            }
        }
        if (nearest == null || distance > ShipSnapRange)
            return null;
        return nearest;
    }

    private bool IsTablePermanentType(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        // Side-Deck-Doorways (z. B. Battle Bridge Door, Q's Tent) nur als Deckblatt,
        // nicht auf den Tisch legen (Kartentext / Seed).
        if (t.Contains("doorway") && GetSideDeckForDoorway(c) != null)
            return false;
        if (t.Contains("doorway") || t.Contains("event") || t.Contains("objective")
            || t.Contains("incident") || t.Contains("interrupt"))
            return true;
        // Artifacts in Dilemma-Seed-Phase unter Missionen, sonst Tisch
        if (t.Contains("artifact") && !(_seedPhaseActive && _seedSubPhase == SeedSubPhase.Dilemma))
            return true;
        return false;
    }

    private void PlaceOnTablePermanents(Card card, Border? floating)
    {
        if (floating != null)
        {
            if (DragLayer.Children.Contains(floating))
                DragLayer.Children.Remove(floating);
            if (TableCanvas.Children.Contains(floating))
                TableCanvas.Children.Remove(floating);
        }

        if (!_seedPhaseActive && _session.Match == GameSession.MatchPhase.Play)
        {
            if (_stack.IsOpen)
            {
                // Nur als Response, wenn legal – sonst zurück
                var top = _stack.Top!;
                var cr = TimingRules.CanRespond(card, top, _stack.ResponsePlayer);
                if (!cr.ok)
                {
                    ReturnCardToHand(card, _stack.ResponsePlayer);
                    ShowPlayError(cr.reason);
                    return;
                }
                BeginPlayCardStack(card, isResponse: true);
                return;
            }

            if (TimingRules.ShouldOpenStackForPlay(card, _seedPhaseActive))
            {
                // Only TABLE-column cards appear there during the commit delay.
                // Ship / mission / gap events stay off TABLE and attach to the target.
                if (EventBelongsOnTableColumn(card) && !TimingRules.IsInterrupt(card)
                    && !InterruptRules.IsInterrupt(card))
                    CommitCardToTable(card);
                BeginPlayCardStack(card, isResponse: false);
                return;
            }
        }

        if (EventBelongsOnTableColumn(card))
            CommitCardToTable(card);
    }

    /// <summary>
    /// True if this card belongs in the P1/P2 TABLE column.
    /// Host events (on ship / planet / mission / gap) and instants do not.
    /// </summary>
    private bool EventBelongsOnTableColumn(Card card)
    {
        if (TimingRules.IsInterrupt(card) || InterruptRules.IsInterrupt(card))
            return false;
        if (EventRules.IsEvent(card))
            return EventRules.StaysOnTable(card);
        return IsTablePermanentType(card);
    }

    private void CommitCardToTable(Card card, int? ownerOverride = null)
    {
        int owner = ownerOverride ?? _activePlayer;
        var list = owner == 2 ? _oppTablePermanentCards : _tablePermanentCards;
        if (!list.Contains(card))
            list.Add(card);
        RebuildTablePermanentsPanel();

        if (TreatyRules.IsTreatyCard(card) || TreatyRules.ParseTreaty(card) != null)
        {
            var links = TreatyRules.ParseAllLinks(card);
            string pair = links.Count > 0
                ? string.Join(", ", links.Select(l => $"{l.AffilA}/{l.AffilB}"))
                : "?";
            StatusText.Text =
                $"P{owner}: Treaty {card.Name} active ({pair}) – Mix & Cooperate.";
            _session.Log.Add(_session.TurnNumber, $"P{owner}",
                $"Treaty played: {card.Name} ({pair})");
        }
        else
        {
            StatusText.Text = $"P{owner}: {card.Name} placed on table.";
        }
    }

    private void RebuildTablePermanentsPanel()
    {
        void Fill(Panel panel, List<Card> cards)
        {
            panel.Children.Clear();
            foreach (var card in cards)
            {
                var mini = new Border
                {
                    Width = ZoneW,
                    Height = ZoneH,
                    Margin = new Thickness(0, 0, 3, 3),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 32)),
                    Cursor = Cursors.Hand,
                    Tag = card,
                    ClipToBounds = false,
                    ToolTip = card.Name
                };
                var img = new Image { Stretch = Stretch.Uniform, IsHitTestVisible = false };
                if (!string.IsNullOrEmpty(card.FullImagePath) && System.IO.File.Exists(card.FullImagePath))
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(card.FullImagePath, UriKind.Absolute);
                        bmp.DecodePixelWidth = 120;
                        bmp.EndInit();
                        img.Source = bmp;
                    }
                    catch { }
                }
                mini.Child = img;
                Card cRef = card;
                mini.MouseLeftButtonDown += (s2, e) =>
                {
                    ShowCardDetail(cRef);
                    if (e.ClickCount >= 2) OpenCardDetailPopup();
                    StatusText.Text = $"Table: {cRef.Name}";
                    e.Handled = true;
                };
                mini.MouseRightButtonDown += (s2, e) =>
                {
                    ShowCardDetail(cRef);
                    BeginHoldZoom(cRef, s2 as IInputElement);
                    e.Handled = true;
                };
                mini.MouseRightButtonUp += (s2, e) =>
                {
                    EndHoldZoom();
                    e.Handled = true;
                };
                AttachMiniHover(mini, cRef);
                panel.Children.Add(mini);
            }
        }

        if (TablePermanentsPanel != null)
            Fill(TablePermanentsPanel, _tablePermanentCards);
        if (OppTablePermanentsPanel != null)
            Fill(OppTablePermanentsPanel, _oppTablePermanentCards);
    }


    private void CommitFloatingToCanvas(Border floating, Point windowPos)
    {
        if (DragLayer.Children.Contains(floating))
            DragLayer.Children.Remove(floating);
        if (!TableCanvas.Children.Contains(floating))
            TableCanvas.Children.Add(floating);
        // Ungefähre Canvas-Position aus Fensterkoordinaten (Fallback Mitte)
        try
        {
            var p = TableCanvas.TransformToAncestor(this).Transform(new Point(0, 0));
            double scale = ZoomTransform.ScaleX;
            double cx = (windowPos.X - p.X) / scale;
            double cy = (windowPos.Y - p.Y) / scale;
            Canvas.SetLeft(floating, cx - TableCardWidth / 2);
            Canvas.SetTop(floating, cy - TableCardHeight / 2);
        }
        catch
        {
            Canvas.SetLeft(floating, 400);
            Canvas.SetTop(floating, 400);
        }
        floating.IsHitTestVisible = true;
        floating.MouseLeftButtonDown -= Card_MouseLeftButtonDown;
        floating.MouseLeftButtonUp -= Card_MouseLeftButtonUp;
        floating.MouseMove -= Card_MouseMove;
        floating.MouseLeftButtonDown += Card_MouseLeftButtonDown;
        floating.MouseLeftButtonUp += Card_MouseLeftButtonUp;
        floating.MouseMove += Card_MouseMove;
    }

    /// <summary>Karte wieder in den Stapel legen, wenn über Zone / Stapel-Ansicht losgelassen.</summary>
    private bool TryReturnCardToZone(Card card, Border cardBorder, string sourceZone, Point windowPos, bool opponent = false)
    {
        var zoneBox = FindZoneBorder(sourceZone, opponent);
        bool overZone = zoneBox != null && IsPointNearElement(zoneBox, windowPos, 24);
        // Hand strip of the same player — not the TABLE column
        bool overHandStrip = false;
        var handStrip = opponent ? OppHandStripBorder : PlayerHandStripBorder;
        if (handStrip != null)
            overHandStrip = IsPointOverElement(handStrip, windowPos);

        if (!overZone && !overHandStrip)
            return false;

        var list = GetZoneList(sourceZone, opponent);
        if (list == null) return false;
        if (!list.Contains(card))
            list.Add(card);

        if (DragLayer.Children.Contains(cardBorder))
            DragLayer.Children.Remove(cardBorder);
        TableCanvas.Children.Remove(cardBorder);
        _tablePermanentCards.Remove(card);

        ClearZoneHighlight();
        SetTischZoneHighlight(false);
        RefreshZoneCounts();
        ShowStackContents(sourceZone, opponent: opponent);
        StatusText.Text = $"{card.Name} returned to {sourceZone}.";
        return true;
    }

    /// <summary>
    /// Doorway auf passenden Side-Deck-Stapel legen → wird Deckblatt, Stapel entsperrt.
    /// </summary>
    private bool TryPlaceDoorwayOnSideDeck(Card doorway, Border cardBorder, Point windowPos)
    {
        string? targetZone = GetSideDeckForDoorway(doorway);
        bool opponent = _activePlayer == 2;
        var covers = CoversFor(opponent);

        Border? zoneBox = null;
        if (targetZone != null)
            zoneBox = FindZoneBorder(targetZone, opponent);

        if (zoneBox == null || !IsPointNearElement(zoneBox, windowPos, 48))
        {
            zoneBox = null;
            var panel = opponent ? OpponentZonesPanel : PlayerZonesPanel;
            foreach (var child in panel.Children.OfType<Border>())
            {
                if (child.Tag is not string tag) continue;
                string name = tag.Contains(':') ? tag[(tag.LastIndexOf(':') + 1)..] : tag;
                if (!IsSideDeckZone(name)) continue;
                if (!IsPointNearElement(child, windowPos, 28)) continue;
                if (targetZone != null && !string.Equals(targetZone, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (targetZone == null) continue;
                zoneBox = child;
                targetZone = name;
                break;
            }
        }

        if (targetZone == null || zoneBox == null)
            return false;

        if (covers.ContainsKey(targetZone))
        {
            StatusText.Text = $"{targetZone} (P{_activePlayer}) is already unlocked.";
            return false;
        }

        if (DragLayer.Children.Contains(cardBorder))
            DragLayer.Children.Remove(cardBorder);
        TableCanvas.Children.Remove(cardBorder);
        if (_selectionFrame != null && _selectedCard == cardBorder)
        {
            _selectionFrame.Visibility = Visibility.Collapsed;
            _selectedCard = null;
        }

        covers[targetZone] = doorway;
        RebuildPlayerZones();
        StatusText.Text = $"P{_activePlayer}: doorway {doorway.Name} unlocks {targetZone}.";
        ShowStackContents(targetZone, opponent: opponent);
        return true;
    }


    private Border? FindPlayerZoneBorder(string zoneName) =>
        FindZoneBorder(zoneName, opponent: false);

    private Border? FindZoneBorder(string zoneName, bool opponent)
    {
        var panel = opponent ? OpponentZonesPanel : PlayerZonesPanel;
        string want = opponent ? $"zone:opp:{zoneName}" : $"zone:you:{zoneName}";
        foreach (var child in panel.Children.OfType<Border>())
        {
            if (child.Tag is string tag &&
                (tag == want || tag.EndsWith(":" + zoneName, StringComparison.Ordinal)))
                return child;
        }
        return null;
    }


    private bool IsPointOverElement(FrameworkElement el, Point windowPos)
    {
        try
        {
            var tl = el.TransformToAncestor(this).Transform(new Point(0, 0));
            return windowPos.X >= tl.X && windowPos.X <= tl.X + el.ActualWidth
                && windowPos.Y >= tl.Y && windowPos.Y <= tl.Y + el.ActualHeight;
        }
        catch { return false; }
    }

    private bool IsPointNearElement(FrameworkElement el, Point windowPos, double margin)
    {
        try
        {
            var tl = el.TransformToAncestor(this).Transform(new Point(0, 0));
            return windowPos.X >= tl.X - margin && windowPos.X <= tl.X + el.ActualWidth + margin
                && windowPos.Y >= tl.Y - margin && windowPos.Y <= tl.Y + el.ActualHeight + margin;
        }
        catch { return false; }
    }

    private static bool IsMissionCard(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("mission");
    }

    private static bool IsUniversalMission(Card c)
    {
        string u = (c.Uniqueness ?? "").ToLowerInvariant();
        return u.Contains("universal") || u.Contains("❖") || (c.Name?.StartsWith("❖") ?? false);
    }

    /// <summary>Native Quadrant aus JSON; leer/fehlend = Alpha.</summary>
    private static string GetNativeQuadrant(Card c)
    {
        string q = (c.Quadrant ?? "").Trim();
        if (string.IsNullOrEmpty(q)) return "Alpha";
        q = q.ToLowerInvariant();
        if (q.Contains("gamma") || q.Contains("γ") || q == "g") return "Gamma";
        if (q.Contains("delta") || q.Contains("δ") || q.Contains("∆") || q == "d") return "Delta";
        if (q.Contains("mirror") || q == "m") return "Mirror";
        return "Alpha";
    }

    private static string? GetRegion(Card c)
    {
        string? r = c.Region?.Trim();
        return string.IsNullOrEmpty(r) ? null : r;
    }

    private void PlaceMissionOnSpaceline(Border missionBorder, Card card)
    {
        string quadrant = GetNativeQuadrant(card);
        double dropX = Canvas.GetLeft(missionBorder) + TableCardWidth / 2.0;

        if (!_missionsByQuadrant.TryGetValue(quadrant, out var qList))
        {
            qList = new List<Border>();
            _missionsByQuadrant[quadrant] = qList;
        }

        // Shared mission: gleiche Location / Name, nicht Universal → auf bestehende stapeln
        if (!IsUniversalMission(card))
        {
            var existing = _spacelineOrder.FirstOrDefault(b =>
                b.Tag is Card ec &&
                !IsUniversalMission(ec) &&
                string.Equals(NormalizeMissionLocation(ec.Name), NormalizeMissionLocation(card.Name),
                    StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                SetBorderOwner(missionBorder, _activePlayer);
                AttachSharedMissionCopy(existing, missionBorder);
                StatusText.Text = $"Shared mission: {card.Name} stacked on existing location ({quadrant})";
                ClearMissionSlotPreviews();
                return;
            }
        }

        // Aus alter Position entfernen falls Verschieben
        _spacelineOrder.Remove(missionBorder);
        qList.Remove(missionBorder);

        var validIndices = GetValidSpacelineInsertIndices(quadrant, GetRegion(card));
        int insertIndex = PickInsertIndexByDropX(validIndices, dropX);
        insertIndex = Math.Clamp(insertIndex, 0, _spacelineOrder.Count);
        _spacelineOrder.Insert(insertIndex, missionBorder);
        if (!qList.Contains(missionBorder))
            qList.Add(missionBorder);

        RelayoutMissionsOnSpaceline();
        // Besitzer = wer gelegt hat (für Orientierung); Wechsel nur 1× im Drop-Handler
        SetBorderOwner(missionBorder, _activePlayer);
        ClearMissionSlotPreviews();
        string? region = GetRegion(card);
        StatusText.Text = $"Mission placed: {card.Name} ({quadrant}" +
                          (region != null ? $", Region {region}" : "") + ")";
    }

    /// <summary>
    /// Erlaubte Einfüge-Indizes: Quadrant existiert → nur in diesem Block (auch zwischen Missionen).
    /// Neuer Quadrant → Enden und Quadrant-Grenzen.
    /// </summary>
    private List<int> GetValidSpacelineInsertIndices(string quadrant, string? region)
    {
        var indices = new List<int>();
        int n = _spacelineOrder.Count;

        if (n == 0)
        {
            indices.Add(0);
            return indices;
        }

        var qIdx = new List<int>();
        for (int i = 0; i < n; i++)
        {
            if (_spacelineOrder[i].Tag is Card c && IsMissionCard(c) && GetNativeQuadrant(c) == quadrant)
                qIdx.Add(i);
        }

        if (qIdx.Count > 0)
        {
            int left = qIdx[0];
            int right = qIdx[^1];
            for (int i = left; i <= right + 1; i++)
                indices.Add(i);

            if (region != null)
            {
                var rIdx = new List<int>();
                foreach (int i in qIdx)
                {
                    if (_spacelineOrder[i].Tag is Card mc &&
                        string.Equals(GetRegion(mc), region, StringComparison.OrdinalIgnoreCase))
                        rIdx.Add(i);
                }
                if (rIdx.Count > 0)
                {
                    indices.Clear();
                    for (int i = rIdx[0]; i <= rIdx[^1] + 1; i++)
                        indices.Add(i);
                }
            }
        }
        else
        {
            indices.Add(0);
            indices.Add(n);
            for (int i = 1; i < n; i++)
            {
                if (_spacelineOrder[i - 1].Tag is Card a && _spacelineOrder[i].Tag is Card b &&
                    GetNativeQuadrant(a) != GetNativeQuadrant(b))
                    indices.Add(i);
            }
        }

        // Regionen müssen zusammenhängend bleiben:
        // - Zwischen zwei verschiedenen Regionen: nie einfügen
        // - Zwischen zwei gleichen Regionen R: nur einfügen, wenn neue Mission auch Region R hat
        var filtered = new List<int>();
        foreach (int idx in indices.Distinct().OrderBy(x => x))
        {
            if (idx > 0 && idx < _spacelineOrder.Count)
            {
                var leftR = _spacelineOrder[idx - 1].Tag is Card lc ? GetRegion(lc) : null;
                var rightR = _spacelineOrder[idx].Tag is Card rc ? GetRegion(rc) : null;

                if (leftR != null && rightR != null)
                {
                    if (!string.Equals(leftR, rightR, StringComparison.OrdinalIgnoreCase))
                    {
                        // Grenze zwischen zwei verschiedenen Regionen → nie
                        continue;
                    }
                    // Beide Nachbarn haben dieselbe Region R → neue Karte muss auch R sein
                    if (region == null ||
                        !string.Equals(region, leftR, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
            }
            filtered.Add(idx);
        }
        return filtered;
    }

    private int PickInsertIndexByDropX(List<int> validIndices, double dropX)
    {
        if (validIndices.Count == 0) return _spacelineOrder.Count;
        if (_spacelineOrder.Count == 0) return 0;

        int best = validIndices[0];
        double bestDist = double.MaxValue;
        foreach (int idx in validIndices)
        {
            double slotX;
            if (idx <= 0)
                slotX = Canvas.GetLeft(_spacelineOrder[0]) - TableCardWidth / 2.0;
            else if (idx >= _spacelineOrder.Count)
                slotX = Canvas.GetLeft(_spacelineOrder[^1]) + TableCardWidth + TableCardWidth / 2.0;
            else
                slotX = (Canvas.GetLeft(_spacelineOrder[idx - 1]) + Canvas.GetLeft(_spacelineOrder[idx]) + TableCardWidth) / 2.0;

            double d = Math.Abs(dropX - slotX);
            if (d < bestDist)
            {
                bestDist = d;
                best = idx;
            }
        }
        return best;
    }

    private static string NormalizeMissionLocation(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        string n = name.Trim();
        if (n.EndsWith(" II", StringComparison.OrdinalIgnoreCase))
            n = n[..^3].Trim();
        if (n.EndsWith(" 2", StringComparison.OrdinalIgnoreCase))
            n = n[..^2].Trim();
        return n;
    }

    /// <summary>
    /// Eine Spaceline: Missionen links→rechts; zwischen unterschiedlichen Quadranten
    /// eine zusätzliche Kartenbreite Abstand.
    /// </summary>
    private void RelayoutMissionsOnSpaceline()
    {
        if (_spacelineOrder.Count == 0) return;

        // Snapshot dockables by OLD mission X before missions move (otherwise they
        // stay at the previous column when a location is inserted).
        var dockByMission = new Dictionary<Border, List<Border>>();
        foreach (var m in _spacelineOrder)
        {
            if (m.Tag is not Card mc || !IsMissionCard(mc)) continue;
            dockByMission[m] = GetDockablesUnderMission(m).ToList();
        }

        var order = _spacelineOrder;

        double width = 0;
        for (int i = 0; i < order.Count; i++)
        {
            if (i > 0)
            {
                string pq = GetSpacelineQuadrant(order[i - 1]);
                string cq = GetSpacelineQuadrant(order[i]);
                width += (pq != cq) ? (TableCardWidth + MissionGap) : MissionGap;
            }
            width += TableCardWidth;
        }

        double x = Math.Max(40, TableCanvas.Width / 2.0 - width / 2.0);
        for (int i = 0; i < order.Count; i++)
        {
            if (i > 0)
            {
                string pq = GetSpacelineQuadrant(order[i - 1]);
                string cq = GetSpacelineQuadrant(order[i]);
                x += (pq != cq) ? (TableCardWidth + MissionGap) : MissionGap;
            }
            Canvas.SetLeft(order[i], x);
            Canvas.SetTop(order[i], SpacelineY);
            // Face mission toward its owner: P1 upright, P2 rotated 180° (readable from top)
            order[i].RenderTransformOrigin = new Point(0.5, 0.5);
            int missionOwner = GetBorderOwner(order[i]);
            order[i].RenderTransform = missionOwner == 2
                ? new RotateTransform(180)
                : Transform.Identity;
            if (order[i].Tag is Card oc && IsMissionCard(oc))
            {
                UpdateSeedBadge(order[i]);
                // Move known dockables to the new column, then tidy slots
                if (dockByMission.TryGetValue(order[i], out var docks))
                {
                    foreach (var d in docks)
                        Canvas.SetLeft(d, x);
                }
                RelayoutDockablesUnderMission(order[i]);
            }
            if (_missionSolvedLabels.TryGetValue(order[i], out var solvedLbl))
            {
                Canvas.SetLeft(solvedLbl, x);
                Canvas.SetTop(solvedLbl, SpacelineY - 16);
            }
            LayoutSharedMissionCopies(order[i], x);
            x += TableCardWidth;
        }
    }

    private void AttachSharedMissionCopy(Border primary, Border copy)
    {
        if (!_sharedMissionCopies.TryGetValue(primary, out var list))
        {
            list = new List<Border>();
            _sharedMissionCopies[primary] = list;
        }
        if (!list.Contains(copy))
            list.Add(copy);
        if (!TableCanvas.Children.Contains(copy))
            TableCanvas.Children.Add(copy);
        LayoutSharedMissionCopies(primary, Canvas.GetLeft(primary));
    }

    private void LayoutSharedMissionCopies(Border primary, double primaryX)
    {
        if (!_sharedMissionCopies.TryGetValue(primary, out var list) || list.Count == 0)
            return;
        int i = 1;
        foreach (var copy in list)
        {
            Canvas.SetLeft(copy, primaryX + i * 10);
            Canvas.SetTop(copy, SpacelineY + i * 12);
            copy.RenderTransformOrigin = new Point(0.5, 0.5);
            int owner = GetBorderOwner(copy);
            copy.RenderTransform = owner == 2 ? new RotateTransform(180) : Transform.Identity;
            Panel.SetZIndex(copy, 20 + i);
            i++;
        }
    }

    private void ClearMissionSlotPreviews()
    {
        foreach (var r in _missionSlotPreviews)
            TableCanvas.Children.Remove(r);
        _missionSlotPreviews.Clear();
    }

    private void ShowMissionSlotPreviews(Card missionCard)
    {
        ClearMissionSlotPreviews();
        if (!_seedPhaseActive || _seedSubPhase != SeedSubPhase.Mission)
            return;

        string quadrant = GetNativeQuadrant(missionCard);
        var valid = GetValidSpacelineInsertIndices(quadrant, GetRegion(missionCard));

        foreach (int idx in valid)
        {
            double slotX;
            if (_spacelineOrder.Count == 0)
                slotX = TableCanvas.Width / 2.0 - TableCardWidth / 2.0;
            else if (idx <= 0)
                slotX = Canvas.GetLeft(_spacelineOrder[0]) - TableCardWidth - MissionSlotGap;
            else if (idx >= _spacelineOrder.Count)
                slotX = Canvas.GetLeft(_spacelineOrder[^1]) + TableCardWidth + MissionSlotGap;
            else
            {
                double left = Canvas.GetLeft(_spacelineOrder[idx - 1]);
                double right = Canvas.GetLeft(_spacelineOrder[idx]);
                slotX = (left + TableCardWidth + right) / 2.0 - TableCardWidth / 2.0;
            }

            var rect = new Rectangle
            {
                Width = TableCardWidth,
                Height = TableCardHeight,
                Stroke = new SolidColorBrush(Color.FromRgb(80, 160, 220)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Fill = new SolidColorBrush(Color.FromArgb(40, 40, 100, 160)),
                IsHitTestVisible = false,
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(rect, slotX);
            Canvas.SetTop(rect, SpacelineY);
            Panel.SetZIndex(rect, 5);
            TableCanvas.Children.Add(rect);
            _missionSlotPreviews.Add(rect);
        }
    }

    private static bool IsSeedableUnderMission(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("dilemma") || t.Contains("artifact");
    }

    /// <summary>
    /// Lackey/CCG: mission_dilemma_type = "[P]", "[S]", "[S/P]", "[P/S]" …
    /// Regelbuch: Planet-Dilemmas only under planet missions, Space nur unter Space,
    /// Dual unter beiden; Artifacts nur Planet (Ausnahmen: Cryosatellite, Orb Negotiations, The Nexus).
    /// </summary>
    private static (bool planet, bool space, bool known) ParseLocationIcons(Card card)
    {
        string mdt = (card.MissionDilemmaType ?? "").Trim();
        string icons = (card.Icons ?? "").Trim();
        string blob = $"{mdt} {icons}".ToUpperInvariant();

        bool planet = false, space = false, known = false;

        // Primär: [P] / [S] / [S/P] / [P/S] / [P][S]
        if (blob.Contains("[S/P]") || blob.Contains("[P/S]") || blob.Contains("[P][S]") || blob.Contains("[S][P]"))
        {
            planet = space = known = true;
        }
        else
        {
            if (blob.Contains("[P]") || blob.Contains("[PLANET]"))
            { planet = true; known = true; }
            if (blob.Contains("[S]") || blob.Contains("[SPACE]"))
            { space = true; known = true; }
        }

        // Ohne Klammern: reines P / S / P/S
        if (!known)
        {
            string bare = mdt.Trim().ToUpperInvariant();
            if (bare is "P" or "PLANET")
            { planet = true; known = true; }
            else if (bare is "S" or "SPACE")
            { space = true; known = true; }
            else if (bare is "S/P" or "P/S" or "BOTH" or "DUAL")
            { planet = space = known = true; }
        }

        // Text-Hinweise (z.B. Crystalline Entity: "[P]: … [S]: …")
        if (!known)
        {
            string text = (card.Text ?? "").ToUpperInvariant();
            bool tp = text.Contains("[P]") || text.Contains("PLANET MISSION") || text.Contains("AWAY TEAM");
            bool ts = text.Contains("[S]") || text.Contains("SPACE MISSION") || text.Contains("ABOARD");
            // Nur wenn klar einseitig
            if (tp && !ts) { planet = true; known = true; }
            else if (ts && !tp) { space = true; known = true; }
            else if (tp && ts) { planet = space = known = true; }
        }

        return (planet, space, known);
    }

    private static (bool planet, bool space) GetMissionLocationIcons(Card mission)
    {
        var (p, s, known) = ParseLocationIcons(mission);
        // Unbekannt → vorsichtig beides (selten; Premiere hat immer [P]/[S])
        if (!known) return (true, true);
        return (p, s);
    }

    private static (bool planet, bool space) GetDilemmaLocationIcons(Card dilemma)
    {
        var (p, s, known) = ParseLocationIcons(dilemma);
        if (!known) return (true, true);
        return (p, s);
    }

    /// <summary>Regelbuch: Cryosatellite, Orb Negotiations, The Nexus – Ausnahmen für Artifact-Seed.</summary>
    private static bool IsArtifactSpaceException(Card c)
    {
        string n = (c.Name ?? "").ToLowerInvariant();
        return n.Contains("cryosatellite")
               || n.Contains("cryo satellite")
               || n.Contains("orb negotiations")
               || n.Contains("the nexus");
    }

    /// <summary>
    /// Darf seedCard unter diese Mission? (Dilemma-Icons, Artifact→Planet, Regelbuch-Ausnahmen)
    /// </summary>
    private static (bool ok, string reason) CanSeedCardUnderMission(Card seedCard, Card mission)
    {
        var (mPlanet, mSpace) = GetMissionLocationIcons(mission);
        string t = (seedCard.Type ?? "").ToLowerInvariant();

        if (t.Contains("artifact"))
        {
            if (IsArtifactSpaceException(seedCard))
                return (true, "");
            if (!mPlanet)
                return (false, $"Artifact {seedCard.Name} only under planet missions (Regelbuch 2.3 / Seed).");
            return (true, "");
        }

        if (t.Contains("dilemma"))
        {
            var (dPlanet, dSpace) = GetDilemmaLocationIcons(seedCard);
            bool ok = (dPlanet && mPlanet) || (dSpace && mSpace);
            if (!ok)
            {
                string need = dPlanet && dSpace ? "Planet/Space"
                    : dPlanet ? "Planet [P]" : dSpace ? "Space [S]" : "unbekannt";
                string have = mPlanet && mSpace ? "Planet/Space"
                    : mPlanet ? "Planet [P]" : mSpace ? "Space [S]" : "unbekannt";
                return (false, $"Dilemma {seedCard.Name} ({need}) passt nicht auf {mission.Name} ({have}).");
            }
            return (true, "");
        }

        return (true, "");
    }

    /// <summary>
    /// Outposts: seed one OR build where you have matching ENGINEER – typisch Planet-Mission.
    /// Station/HQ: Kartentext bestimmt den Ort.
    /// </summary>
    private static (bool ok, string reason) CanSeedFacilityAtMission(Card facility, Card mission)
    {
        var (mPlanet, mSpace) = GetMissionLocationIcons(mission);
        string name = (facility.Name ?? "").ToLowerInvariant();
        string text = (facility.Text ?? "").ToLowerInvariant();
        string type = (facility.Type ?? "").ToLowerInvariant();

        // Headquarters spielen oft "on table" / homeworld – hier Mission-Snap nur wenn Planet
        bool isOutpost = name.Contains("outpost") || type.Contains("outpost");
        bool isHq = name.Contains("headquarters") || text.Contains("headquarters");
        bool isStation = name.Contains("station") || type.Contains("station");

        if (isOutpost || isHq)
        {
            if (!mPlanet)
                return (false, $"{facility.Name} (Outpost/HQ) belongs at a planet mission.");
            // Affiliation: Outpost nur an Mission mit passendem Icon (oder Missions ohne Affiliation)
            if (!AffiliationsCompatible(facility, mission))
            {
                return (false,
                    $"{facility.Name} ({facility.Affiliation ?? "?"}) does not match mission {mission.Name} ({mission.Affiliation ?? "neutral"}).");
            }
            return (true, "");
        }

        if (isStation)
        {
            // z.B. Nor: "Seeds at a [S] mission" – Text prüfen
            if (text.Contains("[s]") || text.Contains("space mission"))
            {
                if (!mSpace)
                    return (false, $"{facility.Name} belongs at a space mission.");
                return (true, "");
            }
            if (text.Contains("[p]") || text.Contains("planet"))
            {
                if (!mPlanet)
                    return (false, $"{facility.Name} belongs at a planet mission.");
                return (true, "");
            }
        }

        // Default Facility → Planet
        if (!mPlanet)
            return (false, $"{facility.Name} belongs at a planet mission.");
        return (true, "");
    }



    /// <summary>Aktive Treaty-Links des Spielers (Events auf TISCH).</summary>
    private List<TreatyRules.TreatyLink> GetActiveTreaties(int player)
    {
        var list = player == 2 ? _oppTablePermanentCards : _tablePermanentCards;
        return TreatyRules.CollectActiveLinks(list);
    }

    /// <summary>Compendium 6.3 – delegiert an ReportingRules (+ Treaties).</summary>
    private (bool ok, string reason) CanReportToHost(Card card, Card host, Border hostBorder)
    {
        int hostOwner = GetBorderOwner(hostBorder);
        if (hostOwner == 0) hostOwner = 1;
        int player = _activePlayer;

        // Persona-Limit zusätzlich
        var owned = CollectCardsInPlay(player == 2);
        var persona = ReportingRules.CheckPersonaLimit(card, owned);
        if (!persona.Ok)
            return (false, persona.Reason);

        var treaties = GetActiveTreaties(player);
        var r = ReportingRules.CanReportTo(
            card, host, hostOwner, player,
            specialReporting: false,
            treatyAllowsMix: false,
            allowReportToShip: false,
            treaties: treaties);

        return (r.Ok, r.Reason);
    }

    private static bool AffiliationsCompatible(Card facility, Card mission)
    {
        var fac = ParseAffiliationTokens(facility.Affiliation);
        var mis = ParseAffiliationTokens(mission.Affiliation);
        if (mis.Count == 0) return true;
        if (fac.Count == 0) return true;
        return fac.Overlaps(mis);
    }

    private static HashSet<string> ParseAffiliationTokens(string? raw)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw)) return set;
        var matches = System.Text.RegularExpressions.Regex.Matches(raw, @"\[([^\]]+)\]");
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
            string t = m.Groups[1].Value.Trim();
            if (t.Length > 0) set.Add(NormalizeAffiliationToken(t));
        }
        if (set.Count == 0)
        {
            string u = raw.Trim();
            if (u.Length > 0) set.Add(NormalizeAffiliationToken(u));
        }
        return set;
    }

    private static string NormalizeAffiliationToken(string t)
    {
        t = t.ToUpperInvariant();
        if (t is "FEDERATION" or "FED") return "FED";
        if (t is "KLINGON" or "KLI") return "KLI";
        if (t is "ROMULAN" or "ROM") return "ROM";
        if (t is "BAJORAN" or "BAJ") return "BAJ";
        if (t is "CARDASSIAN" or "CARD" or "CAR") return "CARD";
        if (t is "DOMINION" or "DOM") return "DOM";
        if (t is "FERENGI" or "FER") return "FER";
        if (t is "BORG") return "BORG";
        if (t is "NON-ALIGNED" or "NONALIGNED" or "NA" or "NON") return "NA";
        return t;
    }

    private void RemoveCardFromZone(string zoneName, Card card, bool opponent = false)
    {
        GetZoneList(zoneName, opponent)?.Remove(card);
        RefreshZoneCounts();
    }

    /// <summary>
    /// Eine Karte vom Draw auf die Hand legen (Sandbox).
    /// </summary>
    private void DrawOneToHand(bool endOfTurn = false)
    {
        if (_seedPhaseActive)
        {
            StatusText.Text = "SEED PHASE – finish seed before drawing";
            return;
        }
        var draw = _activePlayer == 1 ? _drawCards : _oppDrawCards;
        var hand = _activePlayer == 1 ? _handCards : _oppHandCards;
        if (draw.Count == 0)
        {
            StatusText.Text = "Draw-Deck ist leer";
            return;
        }
        var card = draw[0];
        draw.RemoveAt(0);
        hand.Add(card);
        _session.MarkDrawn();
        RefreshZoneCounts();
        string why = endOfTurn ? "end of turn" : "manual (debug)";
        StatusText.Text =
            $"P{_activePlayer} draws ({why}): {card.Name}  (Hand {hand.Count}, Draw {draw.Count})";
        if (!endOfTurn)
        {
            ShowCardDetail(card);
            ShowActivePlayerHand();
            SyncSessionToUi();
        }
    }

    private List<Card> GetCardsForZone(string zoneName, bool opponent)
    {
        // Fest: unten = Spieler 1, oben = Spieler 2 (keine Perspektiv-Spiegelung)
        bool p1 = !opponent;
        return zoneName switch
        {
            "Seed Deck" => (p1 ? _seedCards : _oppSeedCards).ToList(),
            "Doorways" => (p1 ? _doorwayCards : _oppDoorwayCards).ToList(),
            "Missions" => (p1 ? _missionSeedCards : _oppMissionSeedCards).ToList(),
            "Dilemmas" => (p1 ? _dilemmaSeedCards : _oppDilemmaSeedCards).ToList(),
            "Facilities" => (p1 ? _facilitySeedCards : _oppFacilitySeedCards).ToList(),
            "Draw Deck" => (p1 ? _drawCards : _oppDrawCards).ToList(),
            "Side Deck" => (p1 ? _sideCards : _oppSideCards).ToList(),
            "Q's Tent" => (p1 ? _qsTentCards : _oppQsTentCards).ToList(),
            "Battle Bridge" => (p1 ? _battleBridgeCards : _oppBattleBridgeCards).ToList(),
            "Q-Continuum" => (p1 ? _qContinuumCards : _oppQContinuumCards).ToList(),
            "Site Pile" => (p1 ? _sitePileCards : _oppSitePileCards).ToList(),
            "Tribble" => (p1 ? _tribbleCards : _oppTribbleCards).ToList(),
            "Discard" => (p1 ? _discardCards : _oppDiscardCards).ToList(),
            "Hand" => (p1 ? _handCards : _oppHandCards).ToList(),
            _ => new List<Card>()
        };
    }

    private Border CreateMiniCard(Card card, bool faceDown = false)
    {
        var border = new Border
        {
            Width = 68,
            Height = 94,
            Margin = new Thickness(2),
            BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(25, 25, 25)),
            CornerRadius = new CornerRadius(2),
            Cursor = Cursors.Hand,
            Tag = card,
            ToolTip = faceDown ? "Facedown" : card.Name + "\nClick = detail  ·  Drag = to table / other host"
        };

        var img = new Image { Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.LowQuality);

        if (faceDown && _cardBackImage != null)
        {
            img.Source = _cardBackImage;
        }
        else if (!string.IsNullOrEmpty(card.FullImagePath) && System.IO.File.Exists(card.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(card.FullImagePath, UriKind.Absolute);
                bmp.DecodePixelWidth = 80;
                bmp.EndInit();
                img.Source = bmp;
            }
            catch { }
        }

        border.Child = img;
        if (!faceDown)
            AttachMiniHover(border, card);
        return border;
    }

    /// <summary>
    /// Hover preview ~1.7× a table card. In-place scale would clip in the hand strip;
    /// other CCGs (Arena, Hearthstone) use a side preview.
    /// </summary>
    private void AttachMiniHover(FrameworkElement el, Card card)
    {
        el.MouseEnter += (_, _) => ShowMiniHover(el, card);
        el.MouseLeave += (_, _) => HideMiniHover();
    }

    private void ShowMiniHover(FrameworkElement source, Card card)
    {
        if (_isDragging || _panelDragging || _holdZoomActive) return;
        if (ZoomOverlay?.Visibility == Visibility.Visible) return;
        HideMiniHover();

        const double w = 170, h = 238;
        var preview = new Border
        {
            Width = w,
            Height = h,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x82)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(18, 18, 20)),
            CornerRadius = new CornerRadius(4),
            IsHitTestVisible = false,
            Opacity = 0
        };
        var img = new Image { Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
        if (!string.IsNullOrEmpty(card.FullImagePath) && System.IO.File.Exists(card.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(card.FullImagePath, UriKind.Absolute);
                bmp.DecodePixelWidth = 280;
                bmp.EndInit();
                img.Source = bmp;
            }
            catch { }
        }
        preview.Child = img;

        // Hand strip is a sibling of DragLayer, not a descendant — use screen space.
        // Bottom of large card aligns with bottom of mini (grows upward).
        double left = 20, top = 20;
        double ox = 0.5, oy = 1.0;
        try
        {
            var layer = (FrameworkElement)DragLayer;
            double sw = Math.Max(1, source.RenderSize.Width);
            double sh = Math.Max(1, source.RenderSize.Height);
            var miniTopLeft = layer.PointFromScreen(source.PointToScreen(new Point(0, 0)));
            var miniBottomRight = layer.PointFromScreen(source.PointToScreen(new Point(sw, sh)));
            double cx = (miniTopLeft.X + miniBottomRight.X) / 2.0;
            double miniBottom = miniBottomRight.Y;
            const double margin = 4;
            double layerW = layer.ActualWidth > 0 ? layer.ActualWidth : 1280;
            double layerH = layer.ActualHeight > 0 ? layer.ActualHeight : 800;

            left = cx - w / 2.0;
            top = miniBottom - h;
            bool pinL = false, pinR = false;
            if (left < margin) { left = margin; pinL = true; }
            if (left + w > layerW - margin) { left = layerW - margin - w; pinR = true; }
            if (top < margin)
            {
                top = margin;
                oy = 0.5;
            }
            if (top + h > layerH - margin)
            {
                top = layerH - margin - h;
                oy = 1.0;
            }

            if (pinL && !pinR) ox = 0;
            else if (pinR && !pinL) ox = 1;
        }
        catch { }

        Canvas.SetLeft(preview, left);
        Canvas.SetTop(preview, top);
        DragLayer.Children.Add(preview);
        _hoverPreview = preview;

        var scale = new ScaleTransform(0.82, 0.82);
        preview.RenderTransform = scale;
        preview.RenderTransformOrigin = new Point(ox, oy);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.82, 1.0, TimeSpan.FromMilliseconds(120)) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.82, 1.0, TimeSpan.FromMilliseconds(120)) { EasingFunction = ease });
        preview.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(100)));
    }

    private void HideMiniHover()
    {
        if (_hoverPreview == null) return;
        try { DragLayer.Children.Remove(_hoverPreview); } catch { }
        _hoverPreview = null;
    }

    // ===================== SPACELINE (nur Mitte) =====================

    private void DrawSpacelineBackground()
    {
        var bg = new Rectangle
        {
            Width = 2600,
            Height = TableCardHeight + 24,
            Fill = new SolidColorBrush(Color.FromArgb(45, 14, 99, 156)),
            RadiusX = 4,
            RadiusY = 4,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(bg, 40);
        Canvas.SetTop(bg, SpacelineY - 12);
        TableCanvas.Children.Add(bg);

        var label = new TextBlock
        {
            Text = "SPACELINE  ·  quadrants separated by 1 card width",
            Foreground = new SolidColorBrush(Color.FromRgb(120, 140, 160)),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(label, 48);
        Canvas.SetTop(label, SpacelineY - 30);
        TableCanvas.Children.Add(label);
    }

    // ===================== MENÜ =====================

    private void MenuLoadDeck_Click(object sender, RoutedEventArgs e)
    {
        if (_db == null)
        {
            MessageBox.Show("Card data is not loaded yet.");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Deck laden",
            Filter = "STCCG Deck (*.stdeck)|*.stdeck"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var deck = _deckService.Load(dialog.FileName);

            void Link(List<DeckEntry> list)
            {
                foreach (var entry in list)
                {
                    entry.Card = _db.AllCards.FirstOrDefault(c =>
                        string.Equals(c.Name, entry.Name, StringComparison.OrdinalIgnoreCase) &&
                        (entry.Set == null || string.Equals(c.SetFolder, entry.Set, StringComparison.OrdinalIgnoreCase)));
                }
            }
            Link(deck.SeedCards);
            Link(deck.DrawCards);
            Link(deck.QsTentCards);
            Link(deck.BattleBridgeCards);
            Link(deck.QContinuumCards);
            Link(deck.SitePileCards);
            Link(deck.TribbleCards);
            Link(deck.SideCards);

            _loadedDeck = deck;
            ApplySelectedGameMode();
            PlaceDeckOnTable(deck);

            if (_gameMode == GameMode.Hotseat)
                TryLoadOpponentDeck();

            CenterOnSpaceline();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Load error:\n\n{ex.Message}", "Invalid file",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void TryLoadOpponentDeck()
    {
        if (_db == null) return;
        var result = MessageBox.Show(
            "Hotseat: Load deck for Player 2 now?\n\n(No = Player 1 only, opponent zones stay empty)",
            "Player 2 deck", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        var dialog = new OpenFileDialog
        {
            Title = "Load Player 2 deck",
            Filter = "STCCG Deck (*.stdeck)|*.stdeck"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var deck = _deckService.Load(dialog.FileName);
            void Link(List<DeckEntry> list)
            {
                foreach (var entry in list)
                {
                    entry.Card = _db!.AllCards.FirstOrDefault(c =>
                        string.Equals(c.Name, entry.Name, StringComparison.OrdinalIgnoreCase) &&
                        (entry.Set == null || string.Equals(c.SetFolder, entry.Set, StringComparison.OrdinalIgnoreCase)));
                }
            }
            Link(deck.SeedCards);
            Link(deck.DrawCards);
            Link(deck.QsTentCards);
            Link(deck.BattleBridgeCards);
            Link(deck.QContinuumCards);
            Link(deck.SitePileCards);
            Link(deck.TribbleCards);
            Link(deck.SideCards);

            _loadedDeckOpp = deck;
            PlaceOpponentDeck(deck);
            StatusText.Text = $"Hotseat: P1 {_loadedDeck?.Name} + P2 {deck.Name} – Player 1 seeds.";
            RefreshZoneCounts();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Player 2 deck:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void PlaceOpponentDeck(Deck deck)
    {
        _oppHandCards.Clear();
        _oppDrawCards.Clear();
        _oppDiscardCards.Clear();
        _oppSeedCards.Clear();
        _oppDoorwayCards.Clear();
        _oppMissionSeedCards.Clear();
        _oppDilemmaSeedCards.Clear();
        _oppFacilitySeedCards.Clear();
        _oppQsTentCards.Clear();
        _oppBattleBridgeCards.Clear();
        _oppQContinuumCards.Clear();
        _oppSitePileCards.Clear();
        _oppTribbleCards.Clear();
        _oppSideCards.Clear();

        foreach (var e in deck.SeedCards.Where(x => x.Card != null))
        {
            for (int i = 0; i < e.Quantity; i++)
            {
                var card = e.Card!;
                _oppSeedCards.Add(card);
                string t = (card.Type ?? "").ToLowerInvariant();
                if (t.Contains("doorway"))
                    _oppDoorwayCards.Add(card);
                else if (t.Contains("mission"))
                    _oppMissionSeedCards.Add(card);
                else if (t.Contains("dilemma") || t.Contains("artifact"))
                    _oppDilemmaSeedCards.Add(card);
                else
                    _oppFacilitySeedCards.Add(card);
            }
        }

        foreach (var e in deck.DrawCards.Where(x => x.Card != null))
            for (int i = 0; i < e.Quantity; i++)
                _oppDrawCards.Add(e.Card!);

        void Expand(List<DeckEntry> entries, List<Card> target)
        {
            foreach (var e in entries.Where(x => x.Card != null))
                for (int i = 0; i < e.Quantity; i++)
                    target.Add(e.Card!);
        }
        Expand(deck.QsTentCards, _oppQsTentCards);
        Expand(deck.BattleBridgeCards, _oppBattleBridgeCards);
        Expand(deck.QContinuumCards, _oppQContinuumCards);
        Expand(deck.SitePileCards, _oppSitePileCards);
        Expand(deck.TribbleCards, _oppTribbleCards);
        Expand(deck.SideCards, _oppSideCards);
    }

    private void MenuDeckBuilder_Click(object sender, RoutedEventArgs e)
    {
        new DeckBuilderWindow().Show();
    }

    private void MenuZoomReset_Click(object sender, RoutedEventArgs e)
    {
        ZoomTransform.ScaleX = 1;
        ZoomTransform.ScaleY = 1;
        CenterOnSpaceline();
    }

    private void MenuCenterSpaceline_Click(object sender, RoutedEventArgs e)
    {
        CenterOnSpaceline();
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    private void DevRevealSeed_Click(object sender, RoutedEventArgs e)
    {
        _devRevealSeed = DevRevealSeedItem?.IsChecked == true;
        RefreshDevSeedDisplay();
        StatusText.Text = _devRevealSeed ? "Dev: seed under missions revealed." : "Dev: seed under missions hidden.";
    }

    private void DevShowSeedCount_Click(object sender, RoutedEventArgs e)
    {
        _devShowSeedCounts = DevShowSeedCountItem?.IsChecked == true;
        RefreshDevSeedDisplay();
        StatusText.Text = _devShowSeedCounts ? "Dev: seed counts visible." : "Dev: seed counts hidden.";
    }

    private void DevInspectOppHand_Click(object sender, RoutedEventArgs e)
    {
        int target = _activePlayer == 1 ? 2 : 1;
        BeginOpponentPileInteract(target, TargetPileType.Hand,
            PileInteraction.ViewOnly | PileInteraction.FullView,
            "Dev: view opponent hand — click Done when finished.");
    }

    private void DevInspectOppDiscard_Click(object sender, RoutedEventArgs e)
    {
        int target = _activePlayer == 1 ? 2 : 1;
        BeginOpponentPileInteract(target, TargetPileType.Discard,
            PileInteraction.ViewOnly | PileInteraction.FullView,
            "Dev: view opponent discard — click Done when finished.");
    }

    private void DevInspectOppDraw_Click(object sender, RoutedEventArgs e)
    {
        int target = _activePlayer == 1 ? 2 : 1;
        // Draw stays face-down unless FullView is granted by a card effect
        BeginOpponentPileInteract(target, TargetPileType.Draw,
            PileInteraction.ViewOnly,
            "Dev: opponent draw deck (backs only) — click Done.");
    }

    private void DevPeekOpponent_Click(object sender, RoutedEventArgs e)
    {
        _devPeekOpponentPiles = DevPeekOpponentItem?.IsChecked == true;
        StatusText.Text = _devPeekOpponentPiles
            ? "Dev: opponent piles visible."
            : "Dev: opponent piles private again.";
    }

    private void DevShowDebugLog_Click(object sender, RoutedEventArgs e)
    {
        _devShowDebugLog = DevShowDebugLogItem?.IsChecked == true;
        RefreshActionHistory();
        StatusText.Text = _devShowDebugLog
            ? "Dev: Debug: lines visible in Action History."
            : "Dev: Debug: lines hidden from Action History.";
    }

    private void DevPlaySeedFromHand_Click(object sender, RoutedEventArgs e)
    {
        _devPlaySeedFromHand = DevPlaySeedFromHandItem?.IsChecked == true;
        StatusText.Text = _devPlaySeedFromHand
            ? "Dev: drop Dilemmas/Artifacts from hand onto missions (artifacts acquire immediately)."
            : "Dev: seed-from-hand off.";
    }

    private void AidLegalResponses_Click(object sender, RoutedEventArgs e)
    {
        _aidLegalResponses = AidLegalResponsesItem?.IsChecked == true;
        if (_stack.IsOpen) HighlightLegalResponsesInStack();
        StatusText.Text = _aidLegalResponses
            ? "Player aid: legal responses highlighted."
            : "Player aid: legal response highlight off.";
    }

    private void AidOwnSeedCount_Click(object sender, RoutedEventArgs e)
    {
        _aidOwnSeedCounts = AidOwnSeedCountItem?.IsChecked == true;
        RefreshDevSeedDisplay();
        StatusText.Text = _aidOwnSeedCounts
            ? "Player aid: own seed counts under missions."
            : "Player aid: seed counts hidden.";
    }

    private void AidZoneCounts_Click(object sender, RoutedEventArgs e)
    {
        _aidZoneCounts = AidZoneCountsItem?.IsChecked == true;
        RefreshZoneCounts();
        StatusText.Text = _aidZoneCounts
            ? "Player aid: zone pile counts on."
            : "Player aid: zone pile counts off.";
    }

    private void RefreshDevSeedDisplay()
    {
        foreach (var m in AllMissionBorders())
            UpdateSeedBadge(m);
    }

    private void DevAddCard_Click(object sender, RoutedEventArgs e)
    {
        if (_db == null)
        {
            MessageBox.Show("Card database is not loaded.", "Developer",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var win = new Window
        {
            Title = "Developer – add card",
            Width = 520,
            Height = 560,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 32))
        };

        var search = new TextBox { Margin = new Thickness(8, 8, 8, 4), FontSize = 14 };
        var dest = new ComboBox
        {
            Margin = new Thickness(8, 0, 8, 4),
            ItemsSource = new[]
            {
                "Active hand",
                "Active draw deck (top)",
                "Active discard",
                "Table (permanent)"
            },
            SelectedIndex = 0
        };
        var list = new ListBox
        {
            Margin = new Thickness(8, 4, 8, 4),
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 22)),
            Foreground = Brushes.White
        };

        void Refill()
        {
            string q = search.Text?.Trim() ?? "";
            IEnumerable<Card> src = _db.AllCards;
            if (q.Length > 0)
                src = src.Where(c =>
                    (c.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (c.Type ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));
            list.ItemsSource = src.OrderBy(c => c.Name).Take(200).ToList();
            list.DisplayMemberPath = "Name";
        }

        search.TextChanged += (_, _) => Refill();
        Refill();

        var addBtn = new Button
        {
            Content = "Add to game",
            Height = 32,
            Margin = new Thickness(8),
            Background = new SolidColorBrush(Color.FromRgb(46, 125, 50)),
            Foreground = Brushes.White
        };
        addBtn.Click += (_, _) =>
        {
            if (list.SelectedItem is not Card card)
            {
                MessageBox.Show("Select a card first.", "Developer");
                return;
            }
            InjectDevCard(card, dest.SelectedIndex);
            StatusText.Text = $"Dev: added {card.Name}.";
        };

        var root = new DockPanel();
        DockPanel.SetDock(search, Dock.Top);
        DockPanel.SetDock(dest, Dock.Top);
        DockPanel.SetDock(addBtn, Dock.Bottom);
        root.Children.Add(search);
        root.Children.Add(dest);
        root.Children.Add(addBtn);
        root.Children.Add(list);
        win.Content = root;
        win.Show();
    }

    private void InjectDevCard(Card card, int destIndex)
    {
        bool p2 = _activePlayer == 2;
        switch (destIndex)
        {
            case 1:
                (p2 ? _oppDrawCards : _drawCards).Insert(0, card);
                break;
            case 2:
                (p2 ? _oppDiscardCards : _discardCards).Add(card);
                break;
            case 3:
                CommitCardToTable(card, _activePlayer);
                break;
            default:
                (p2 ? _oppHandCards : _handCards).Add(card);
                ShowActivePlayerHand();
                break;
        }
        RefreshZoneCounts();
        _session.Log.Add(_session.TurnNumber, "Dev", $"Injected {card.Name} → dest {destIndex} (P{_activePlayer})");
    }

    private void MenuSeedStart_Click(object sender, RoutedEventArgs e)
    {
        EnterSeedPhase(SeedSubPhase.Doorway);
        StatusText.Text = SeedPhaseStatusLine() + "  ·  Drag cards from the current stack";
        ShowCurrentSeedStack();
    }

    private void MenuSeedNextPhase_Click(object sender, RoutedEventArgs e)
    {
        if (!_seedPhaseActive)
        {
            StatusText.Text = "Keine aktive Seed-Phase – zuerst Deck laden oder Seed starten.";
            return;
        }
        AdvanceSeedPhase(manual: true);
    }

    private void MenuSeedFinish_Click(object sender, RoutedEventArgs e)
    {
        if (!_seedPhaseActive && _handCards.Count > 0)
        {
            StatusText.Text = "Seed-Phase ist bereits beendet.";
            return;
        }

        FinishSeedPhaseAndDrawOpeningHand();
    }

    private void EnterSeedPhase(SeedSubPhase start)
    {
        ApplySelectedGameMode();
        _seedPhaseActive = true;
        _seedSubPhase = start;
        SkipEmptySeedPhases();
        RebuildPlayerZones();
        UpdatePhaseControls();
        StatusText.Text = SeedPhaseStatusLine();
        ShowCurrentSeedStack();
    }

    /// <summary>Leere Phasen (z. B. keine Doorways) automatisch überspringen.</summary>
    private void SkipEmptySeedPhases()
    {
        while (_seedPhaseActive && _seedSubPhase < SeedSubPhase.Facility && CurrentPhaseRemainingCount() == 0)
            _seedSubPhase = (SeedSubPhase)((int)_seedSubPhase + 1);
    }

    private int CurrentPhaseRemainingCount() => _seedSubPhase switch
    {
        SeedSubPhase.Doorway => _doorwayCards.Count + _oppDoorwayCards.Count,
        SeedSubPhase.Mission => _missionSeedCards.Count + _oppMissionSeedCards.Count,
        SeedSubPhase.Dilemma => _dilemmaSeedCards.Count + _oppDilemmaSeedCards.Count,
        SeedSubPhase.Facility => _facilitySeedCards.Count + _oppFacilitySeedCards.Count,
        _ => 0
    };

    private void AdvanceSeedPhase(bool manual)
    {
        ClearMissionSlotPreviews();
        if (_seedSubPhase >= SeedSubPhase.Facility)
        {
            if (manual)
            {
                // Noch Facility-Karten beim anderen Spieler? → nicht beenden
                if (_gameMode == GameMode.Hotseat)
                {
                    int other = _activePlayer == 1 ? 2 : 1;
                    if (CountSeedFor(SeedSubPhase.Facility, other) > 0)
                    {
                        _activePlayer = other;
                        ApplyPerspective();
                        UpdatePhaseControls();
                        OnTurnContextChanged(
                            $"Facility: Player {_activePlayer} is still seeding " +
                            $"(noch {CountSeedFor(SeedSubPhase.Facility, other)}).");
                        return;
                    }
                    if (CountSeedFor(SeedSubPhase.Facility, _activePlayer) > 0)
                    {
                        StatusText.Text =
                            $"{CountSeedFor(SeedSubPhase.Facility, _activePlayer)} facility cards left – keep placing or Finish seed later.";
                        return;
                    }
                }
                FinishSeedPhaseAndDrawOpeningHand();
            }
            return;
        }

        _seedSubPhase = (SeedSubPhase)((int)_seedSubPhase + 1);
        // Nächste Teilphase beginnt mit Spieler 1
        _activePlayer = 1;
        SkipEmptySeedPhases();
        // Falls in neuer Phase S1 leer, S2 aber nicht → sofort zu S2
        if (_seedSubPhase == SeedSubPhase.Doorway)
            TrySequentialSeedPlayer();
        else if (_seedSubPhase is SeedSubPhase.Mission or SeedSubPhase.Dilemma or SeedSubPhase.Facility)
        {
            if (CountSeedFor(_seedSubPhase, 1) == 0 && CountSeedFor(_seedSubPhase, 2) > 0)
            {
                _activePlayer = 2;
                StatusText.Text = $"P1 has no cards in this phase → Player 2's turn.";
            }
        }
        ApplyPerspective();
        UpdatePhaseControls();
        OnTurnContextChanged((manual ? "Phase abgeschlossen → " : "Automatisch weiter → ") + SeedPhaseStatusLine());
        ShowCurrentSeedStack();
    }

    private void TryAutoAdvanceSeedPhase()
    {
        if (!_seedPhaseActive) return;

        if (CurrentPhaseRemainingCount() == 0 && _seedSubPhase < SeedSubPhase.Facility)
            AdvanceSeedPhase(manual: false);
        else if (CurrentPhaseRemainingCount() == 0 && _seedSubPhase == SeedSubPhase.Facility)
        {
            // Beide Spieler: keine Seed-Karten mehr → automatisch Opening Hand
            StatusText.Text = "SEED abgeschlossen – Opening Hand …";
            FinishSeedPhaseAndDrawOpeningHand();
        }
    }

    private void ShowCurrentSeedStack()
    {
        string zone = _seedSubPhase switch
        {
            SeedSubPhase.Doorway => "Doorways",
            SeedSubPhase.Mission => "Missions",
            SeedSubPhase.Dilemma => "Dilemmas",
            SeedSubPhase.Facility => "Facilities",
            _ => "Hand"
        };
        if (_seedPhaseActive)
            ShowStackContents(zone, opponent: _activePlayer == 2);
    }

    private void UpdateScoreDisplay()
    {
        if (ScoreText == null) return;
        ScoreText.Text = $"P1: {_scoreP1}   ·   P2: {_scoreP2}";
    }

    private void MarkMissionSolved(Border missionBorder, Card mission, int player, int points)
    {
        _solvedMissions.Add(missionBorder);
        _missionSolver[missionBorder] = player;
        if (player == 1) _scoreP1 += points;
        else _scoreP2 += points;
        UpdateScoreDisplay();
        foreach (var e in _attachedEvents.Where(x =>
                     x.Kind == EventRules.Persist.Espionage && ReferenceEquals(x.Host, missionBorder)).ToList())
        {
            _attachedEvents.Remove(e);
            SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
        }

        missionBorder.Opacity = 0.7;
        // Rahmen farblich nach Spieler
        missionBorder.BorderBrush = new SolidColorBrush(
            player == 1 ? Color.FromRgb(40, 180, 90) : Color.FromRgb(60, 120, 220));
        missionBorder.BorderThickness = new Thickness(3);

        if (_missionSolvedLabels.TryGetValue(missionBorder, out var oldLbl))
            TableCanvas.Children.Remove(oldLbl);

        var lbl = new TextBlock
        {
            Text = $"✓ S{player}  +{points}",
            Foreground = player == 1
                ? new SolidColorBrush(Color.FromRgb(120, 255, 160))
                : new SolidColorBrush(Color.FromRgb(140, 190, 255)),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromArgb(200, 20, 20, 28)),
            Padding = new Thickness(4, 1, 4, 1),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(lbl, Canvas.GetLeft(missionBorder));
        Canvas.SetTop(lbl, Canvas.GetTop(missionBorder) - 16);
        Panel.SetZIndex(lbl, 20);
        TableCanvas.Children.Add(lbl);
        _missionSolvedLabels[missionBorder] = lbl;
    }

    private void UpdatePhaseControls()
    {
        UpdateTurnTints();
        if (PhaseLabel == null) return;
        if (_stack.IsOpen)
        {
            BtnPhaseNext.IsEnabled = false;
            if (BtnEndTurn != null) BtnEndTurn.IsEnabled = false;
            if (ActivePlayerText != null)
                ActivePlayerText.Text =
                    $"TURN {_turnNumber} · Player {_activePlayer} · RESPONSE WINDOW — resolve stack first";
            return;
        }

        void SetActiveBanner(string text, string colorHex)
        {
            if (ActivePlayerText != null)
                ActivePlayerText.Text = text;
            if (ActivePlayerBanner != null)
            {
                try
                {
                    ActivePlayerBanner.Background = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(colorHex)!);
                }
                catch { }
            }
        }

        if (ScoreText != null)
            ScoreText.Text = $"P1: {_scoreP1}  ·  P2: {_scoreP2}";

        if (_seedPhaseActive)
        {
            string side = _activePlayer == 1 ? "Player 1 (BOTTOM)" : "Player 2 (TOP)";
            int left = CountSeedFor(_seedSubPhase, _activePlayer);
            SetActiveBanner(
                $"SEED · {SeedPhaseShortName()} · {side} · {left} card(s) left",
                _activePlayer == 1 ? "#1B5E20" : "#0D47A1");
            ApplyBoardPlayerTint();
            if (PhaseLabel != null) PhaseLabel.Text = SeedPhaseShortName();
            if (PhaseHint != null) PhaseHint.Text = "";
            bool facilityExit = _seedSubPhase == SeedSubPhase.Facility;
            BtnPhaseNext.Visibility = facilityExit ? Visibility.Collapsed : Visibility.Visible;
            BtnPhaseNext.IsEnabled = !facilityExit;
            BtnPhaseNext.Content = _seedSubPhase switch
            {
                SeedSubPhase.Doorway => "End Doorway phase (Space)",
                SeedSubPhase.Mission => "End Mission phase (Space)",
                SeedSubPhase.Dilemma => "End Dilemma phase (Space)",
                SeedSubPhase.Facility => "End Facility phase (Space)",
                _ => "Next phase (Space)"
            };
            BtnSeedFinish.Content = "Seed → Start play";
            BtnSeedFinish.Visibility = facilityExit ? Visibility.Visible : Visibility.Collapsed;
            BtnSeedFinish.IsEnabled = facilityExit;
            if (BtnEndTurn != null) BtnEndTurn.Visibility = Visibility.Collapsed;
        }
        else if (_loadedDeck != null)
        {
            string side = _activePlayer == 1 ? "Player 1 (BOTTOM)" : "Player 2 (TOP)";
            string phase = _session.Segment switch
            {
                GameSession.TurnSegment.Play => "PLAY",
                GameSession.TurnSegment.Execute => "EXECUTE",
                GameSession.TurnSegment.Draw => "DRAW",
                _ => _session.SegmentLabel()
            };
            string actions;
            if (_stack.IsOpen)
                actions = "Response window open";
            else if (_session.Segment == GameSession.TurnSegment.Play)
            {
                if (_redAlertPlaysLeft > 0)
                    actions = $"Red Alert: up to {_redAlertPlaysLeft} personnel/equipment";
                else if (_session.NormalCardPlayUsed)
                    actions = "Normal card play used";
                else
                    actions = "1× card play possible";
            }
            else if (_session.Segment == GameSession.TurnSegment.Execute)
                actions = "Orders (move / battle / attempt)";
            else
                actions = "End of turn";

            SetActiveBanner(
                $"TURN {_turnNumber} · {side} · {phase} · {actions}",
                _activePlayer == 1 ? "#1B5E20" : "#0D47A1");
            ApplyBoardPlayerTint();
            if (PhaseLabel != null) PhaseLabel.Text = phase;
            if (PhaseHint != null) PhaseHint.Text = "";
            BtnPhaseNext.IsEnabled = false;
            BtnPhaseNext.Visibility = Visibility.Collapsed;
            BtnSeedFinish.IsEnabled = false;
            BtnSeedFinish.Visibility = Visibility.Collapsed;
            if (BtnEndTurn != null)
            {
                BtnEndTurn.Visibility = Visibility.Visible;
                bool pileLock = _inspectorMode == InspectorMode.OpponentPileInteract;
                BtnEndTurn.IsEnabled = !pileLock;
                BtnEndTurn.Content = pileLock
                    ? "Finish pile action first…"
                    : _session.Segment == GameSession.TurnSegment.Play
                        ? "End PLAY phase (Space)"
                        : "End EXECUTE phase (Space)";
            }
        }
        else
        {
            SetActiveBanner("Start a new game and load decks", "#2A3A4A");
            if (PhaseLabel != null) PhaseLabel.Text = "–";
            if (PhaseHint != null) PhaseHint.Text = "";
            BtnPhaseNext.IsEnabled = false;
            BtnPhaseNext.Visibility = Visibility.Visible;
            BtnSeedFinish.IsEnabled = false;
            BtnSeedFinish.Visibility = Visibility.Collapsed;
            if (BtnEndTurn != null) BtnEndTurn.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplySelectedGameMode()
    {
        if (ModeHotseat?.IsChecked == true) _gameMode = GameMode.Hotseat;
        else if (ModeNetwork?.IsChecked == true) _gameMode = GameMode.Network;
        else if (ModeSingle?.IsChecked == true) _gameMode = GameMode.SingleAi;
        else _gameMode = GameMode.Hotseat;

        _activePlayer = 1;
        _turnNumber = 1;
        StatusText.Text = _gameMode switch
        {
            GameMode.Hotseat => "Mode: Hotseat (two players on one PC)",
            GameMode.Network => "Mode: Network (not available yet)",
            GameMode.SingleAi => "Mode: Singleplayer AI (not available yet)",
            _ => "Mode selected"
        };
    }

    private void BtnEndTurn_Click(object sender, RoutedEventArgs e)
    {
        if (_seedPhaseActive || _gameMode != GameMode.Hotseat) return;
        if (_inspectorMode == InspectorMode.OpponentPileInteract)
        {
            StatusText.Text = "Resolve or cancel the pile interaction first.";
            return;
        }

        // Play → Execute
        if (_session.Segment == GameSession.TurnSegment.Play)
        {
            _session.AdvanceSegment();
            SyncSessionToUi();
            OnTurnContextChanged(_session.StatusLine() + " – execute orders.");
            return;
        }

        // Execute beendet → Draw (end of turn) + Gegner
        if (_session.Segment == GameSession.TurnSegment.Execute)
        {
            FinishExecuteAndEndTurn();
            return;
        }

        // Falls irgendwie in Draw hängen geblieben
        FinishExecuteAndEndTurn();
    }

    /// <summary>
    /// Execute fertig: optional 1 Karte ziehen, dann Gegner (Play-Segment).
    /// SuppressEndOfTurnDraw für spätere Karteneffekte.
    /// </summary>
    private void FinishExecuteAndEndTurn()
    {
        // Compendium 8: "at end of turn" effects first, THEN the end-of-turn draw.
        // Static Warp Bubble must discard from the current hand, not the card just drawn.
        int finishingPlayer = _session.ActivePlayer;
        ProcessEndOfTurnRepairs(finishingPlayer);
        ProcessEndOfTurnDilemmas(finishingPlayer);
        ProcessEndOfTurnEvents(finishingPlayer);
        ProcessRogueBorgEndOfTurn(finishingPlayer);
        _ionizationBeamsThisTurn = 0;
        _movedThisTurnAfterArrival.Clear();

        if (!_session.SuppressEndOfTurnDraw && !_session.HasDrawnThisTurn)
            DrawOneToHand(endOfTurn: true);
        else if (_session.SuppressEndOfTurnDraw)
            StatusText.Text = "No draw at end of turn (card effect).";

        if (HasHorgahn(finishingPlayer) && !_horgahnExtraPlayUsed)
        {
            DrawOneToHand(endOfTurn: true);
            _session.Log.Add(_session.TurnNumber, $"P{finishingPlayer}", "Horga'hn extra draw");
            StatusText.Text = "Horga'hn: extra card at end of turn.";
        }
        _horgahnExtraPlayUsed = false;

        _session.EndTurn();

        UnstopAllCards(); // Compendium: Stopped endet zu Beginn des nächsten Zugs (hier Zugwechsel)
        ResetShipRangesForTurn();
        RefreshRedAlertForTurn();
        ProcessStartOfTurnTimedEffects();
        ProcessStartOfTurnDilemmas(_session.ActivePlayer);
        SyncSessionToUi();
        ApplyPerspective();
        OnTurnContextChanged(); // Deselektieren + Hand des neuen Spielers
        StatusText.Text =
            $"{_session.StatusLine()} " +
            $"(Hand {(_activePlayer == 1 ? _handCards.Count : _oppHandCards.Count)}, " +
            $"Draw {(_activePlayer == 1 ? _drawCards.Count : _oppDrawCards.Count)}).";
    }

    /// <summary>Gestoppte Karten werden zu Beginn jedes Spielerzuges wieder freigegeben.</summary>
    private void UnstopAllCards()
    {
        if (_stoppedBorders.Count == 0) return;
        foreach (var b in _stoppedBorders.ToList())
            ApplyStoppedVisual(b, stopped: false);
        _stoppedBorders.Clear();
        _session.Log.Add(_session.TurnNumber, "Pystem", "All stopped cards are active again.");
    }

    private void SyncSessionToUi()
    {
        _activePlayer = _session.ActivePlayer;
        _turnNumber = _session.TurnNumber;
        UpdatePhaseControls();
        UpdateTurnTints();
    }

    private void BtnPhaseNext_Click(object sender, RoutedEventArgs e) => AdvanceSeedPhase(manual: true);
    private void BtnSeedFinish_Click(object sender, RoutedEventArgs e)
    {
        // Facility-Phase: anderen Spieler nicht überspringen
        if (_seedPhaseActive && _seedSubPhase == SeedSubPhase.Facility && _gameMode == GameMode.Hotseat)
        {
            int other = _activePlayer == 1 ? 2 : 1;
            int otherLeft = CountSeedFor(SeedSubPhase.Facility, other);
            int selfLeft = CountSeedFor(SeedSubPhase.Facility, _activePlayer);
            if (otherLeft > 0)
            {
                _activePlayer = other;
                ApplyPerspective();
                ShowCurrentSeedStack();
                UpdatePhaseControls();
                StatusText.Text =
                    $"Facility phase: Player {_activePlayer} must still seed ({otherLeft} left). " +
                    "Finish seed only after that.";
                return;
            }
            if (selfLeft > 0)
            {
                StatusText.Text =
                    $"{selfLeft} facility cards left for Player {_activePlayer}. " +
                    "Place cards or finish the phase when done – then Finish seed again.";
                return;
            }
        }
        // Andere Seed-Teilphasen: Warnung wenn Gegner noch Karten hat
        if (_seedPhaseActive && _gameMode == GameMode.Hotseat && _seedSubPhase < SeedSubPhase.Facility)
        {
            StatusText.Text = "Please complete all seed sub-phases first (or Next phase).";
            return;
        }
        FinishSeedPhaseAndDrawOpeningHand();
    }

    private void WelcomeNewGame_Click(object sender, RoutedEventArgs e)
    {
        NewGameOptions.Visibility = Visibility.Visible;
        StatusText.Text = "Neues Spiel – bitte Deck laden oder im Builder erstellen";
    }

    private void MenuNewGame_Click(object sender, RoutedEventArgs e)
    {
        WelcomeOverlay.Visibility = Visibility.Visible;
        NewGameOptions.Visibility = Visibility.Visible;
        StatusText.Text = "Neues Spiel – Deck laden oder erstellen";
    }

    private void MenuQuickGame_Click(object sender, RoutedEventArgs e)
    {
        if (_db == null)
        {
            MessageBox.Show("Card data is not loaded yet.", "Quick game",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var p1 = PickDeckFile("Quick game – Player 1 deck");
        if (p1 == null) return;
        var p2 = PickDeckFile("Quick game – Player 2 deck");
        if (p2 == null) return;

        try
        {
            var deck1 = LoadAndLinkDeck(p1);
            var deck2 = LoadAndLinkDeck(p2);
            _loadedDeck = deck1;
            ApplySelectedGameMode();
            PlaceDeckOnTable(deck1);
            _loadedDeckOpp = deck2;
            PlaceOpponentDeck(deck2);
            AutoCompleteSeed();
            CenterOnSpaceline();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Quick game failed:\n\n{ex.Message}", "Quick game",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private string? PickDeckFile(string title)
    {
        var dlg = new OpenFileDialog
        {
            Title = title,
            Filter = "STCCG Deck (*.stdeck)|*.stdeck"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private Deck LoadAndLinkDeck(string path)
    {
        var deck = _deckService.Load(path);
        void Link(List<DeckEntry> list)
        {
            foreach (var entry in list)
            {
                entry.Card = _db!.AllCards.FirstOrDefault(c =>
                    string.Equals(c.Name, entry.Name, StringComparison.OrdinalIgnoreCase) &&
                    (entry.Set == null || string.Equals(c.SetFolder, entry.Set, StringComparison.OrdinalIgnoreCase)));
            }
        }
        Link(deck.SeedCards);
        Link(deck.DrawCards);
        Link(deck.QsTentCards);
        Link(deck.BattleBridgeCards);
        Link(deck.QContinuumCards);
        Link(deck.SitePileCards);
        Link(deck.TribbleCards);
        Link(deck.SideCards);
        return deck;
    }

    /// <summary>
    /// Computer seed: Doorway (P1 then P2) → Missions alternate → Dil/Art alternate → Facility alternate.
    /// Leftover illegal cards stay in the leftover seed pile.
    /// </summary>
    private Random _autoSeedRng = new();

    private void AutoCompleteSeed()
    {
        _autoSeedRng = new Random();
        var rng = _autoSeedRng;

        void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        List<Card> Pile(int player, SeedSubPhase phase) => phase switch
        {
            SeedSubPhase.Doorway => player == 1 ? _doorwayCards : _oppDoorwayCards,
            SeedSubPhase.Mission => player == 1 ? _missionSeedCards : _oppMissionSeedCards,
            SeedSubPhase.Dilemma => player == 1 ? _dilemmaSeedCards : _oppDilemmaSeedCards,
            _ => player == 1 ? _facilitySeedCards : _oppFacilitySeedCards
        };

        // 1) Doorways: P1 all, then P2
        foreach (int player in new[] { 1, 2 })
        {
            _activePlayer = player;
            var pile = Pile(player, SeedSubPhase.Doorway);
            foreach (var card in pile.ToList())
            {
                pile.Remove(card);
                AutoSeedDoorway(card, player);
            }
        }

        // 2) Missions alternate
        Shuffle(_missionSeedCards);
        Shuffle(_oppMissionSeedCards);
        AutoSeedAlternate(SeedSubPhase.Mission, AutoSeedMission);

        // 3) Dilemmas / artifacts alternate
        Shuffle(_dilemmaSeedCards);
        Shuffle(_oppDilemmaSeedCards);
        AutoSeedAlternate(SeedSubPhase.Dilemma, AutoSeedDilemma);

        // 4) Facilities / leftover seed cards
        Shuffle(_facilitySeedCards);
        Shuffle(_oppFacilitySeedCards);
        AutoSeedAlternate(SeedSubPhase.Facility, AutoSeedFacility);

        RebuildPlayerZones();
        RelayoutMissionsOnSpaceline();
        FinishSeedPhaseAndDrawOpeningHand();
        _session.Log.Add(_session.TurnNumber, "Pystem",
            $"Quick game seeded: {_spacelineOrder.Count} missions, " +
            $"P1 {_loadedDeck?.Name}, P2 {_loadedDeckOpp?.Name}");
        StatusText.Text = "Quick game: seed complete, opening hands dealt.";
    }

    private void AutoSeedAlternate(SeedSubPhase phase, Action<Card, int> place)
    {
        int player = 1;
        int guard = 0;
        while (guard++ < 400)
        {
            var pile = phase switch
            {
                SeedSubPhase.Mission => player == 1 ? _missionSeedCards : _oppMissionSeedCards,
                SeedSubPhase.Dilemma => player == 1 ? _dilemmaSeedCards : _oppDilemmaSeedCards,
                _ => player == 1 ? _facilitySeedCards : _oppFacilitySeedCards
            };
            if (pile.Count == 0)
            {
                int other = player == 1 ? 2 : 1;
                var otherPile = phase switch
                {
                    SeedSubPhase.Mission => other == 1 ? _missionSeedCards : _oppMissionSeedCards,
                    SeedSubPhase.Dilemma => other == 1 ? _dilemmaSeedCards : _oppDilemmaSeedCards,
                    _ => other == 1 ? _facilitySeedCards : _oppFacilitySeedCards
                };
                if (otherPile.Count == 0) break;
                player = other;
                continue;
            }
            var card = pile[0];
            pile.RemoveAt(0);
            _activePlayer = player;
            place(card, player);
            player = player == 1 ? 2 : 1;
        }
    }

    private void AutoSeedDoorway(Card card, int player)
    {
        string? zone = GetSideDeckForDoorway(card);
        var covers = CoversFor(player == 2);
        if (zone != null && !covers.ContainsKey(zone))
        {
            covers[zone] = card;
            return;
        }
        CommitCardToTable(card, player);
    }

    private void AutoSeedMission(Card card, int player)
    {
        // Random drop-X within existing spaceline so region/quadrant insert varies
        double baseX = SpacelineStartX;
        if (_spacelineOrder.Count > 0)
        {
            double left = Canvas.GetLeft(_spacelineOrder[0]);
            double right = Canvas.GetLeft(_spacelineOrder[^1]) + TableCardWidth;
            baseX = left + _autoSeedRng.NextDouble() * Math.Max(TableCardWidth, right - left);
        }
        else
            baseX = SpacelineStartX + _autoSeedRng.Next(0, 400);

        var border = AddCardToTable(card, baseX, SpacelineY, TableCardWidth);
        PlaceMissionOnSpaceline(border, card);
        SetBorderOwner(border, player);
    }

    private void AutoSeedDilemma(Card card, int player)
    {
        var legal = AllMissionBorders()
            .Where(m => m.Tag is Card mc && CanSeedCardUnderMission(card, mc).ok)
            .ToList();
        if (legal.Count == 0)
        {
            (player == 1 ? _seedCards : _oppSeedCards).Add(card);
            return;
        }

        int totalDil = _dilemmaSeedCards.Count + _oppDilemmaSeedCards.Count
                       + legal.Sum(m => _seedUnderMission.TryGetValue(m, out var l) ? l.Count : 0);
        // Target depth per mission ≈ total remaining+placed / mission count, ±20% soft band
        double avg = legal.Count > 0 ? Math.Max(1.0, (double)Math.Max(totalDil, legal.Count) / legal.Count) : 1.0;
        double softMin = avg * 0.80;
        double softMax = avg * 1.20;

        double Weight(Border m)
        {
            int depth = _seedUnderMission.TryGetValue(m, out var list) ? list.Count : 0;
            int points = ParseMissionPoints(m.Tag as Card);
            // Prefer thinner stacks, but allow ±20% band without hard reject
            double depthScore;
            if (depth < softMin) depthScore = 3.0 + (softMin - depth);      // under-filled
            else if (depth > softMax) depthScore = Math.Max(0.15, 1.0 - (depth - softMax)); // over soft max
            else depthScore = 2.0 + (softMax - depth) * 0.3;                // inside band

            // Higher-point missions attract more dilemmas (human tactic), mild curve
            double pointScore = 1.0 + Math.Sqrt(Math.Max(0, points)) * 0.35;

            // Slight preference for seeding under any mission; tiny own-mission nudge
            double ownerNudge = GetBorderOwner(m) == player ? 1.08 : 1.0;

            // Requirements: dilemmas should add NEW skills, not repeat the mission (or the stack)
            var missionSkills = PersonnelSkillIndex.ExtractRequirementSkills((m.Tag as Card)?.Text);
            var dilSkills = PersonnelSkillIndex.ExtractRequirementSkills(card.Text);
            int overlapMission = missionSkills.Intersect(dilSkills, StringComparer.OrdinalIgnoreCase).Count();
            var stackSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_seedUnderMission.TryGetValue(m, out var under))
            {
                foreach (var sb in under)
                {
                    if (sb.Tag is Card sc)
                        foreach (var s in PersonnelSkillIndex.ExtractRequirementSkills(sc.Text))
                            stackSkills.Add(s);
                }
            }
            int overlapStack = stackSkills.Intersect(dilSkills, StringComparer.OrdinalIgnoreCase).Count();
            double skillScore = 1.0;
            if (dilSkills.Count > 0)
            {
                skillScore = Math.Pow(0.10, overlapMission) * Math.Pow(0.40, overlapStack);
                if (overlapMission == 0) skillScore *= 1.7;
            }

            // Noise so two runs never match
            double noise = 0.65 + _autoSeedRng.NextDouble() * 0.7;
            return depthScore * pointScore * ownerNudge * skillScore * noise;
        }

        var mission = WeightedPick(legal, Weight);
        var border = AddCardToTable(card, Canvas.GetLeft(mission), Canvas.GetTop(mission), TableCardWidth);
        AddSeedUnderMission(mission, border);
        SetBorderOwner(border, player);
    }

    private static int ParseMissionPoints(Card? mission)
    {
        if (mission?.Points == null) return 0;
        string s = mission.Points.Trim();
        if (int.TryParse(s, out int p)) return p;
        // "35*" / "40 / 5" → first number
        var digits = new string(s.TakeWhile(ch => char.IsDigit(ch) || ch == '-').ToArray());
        return int.TryParse(digits, out p) ? p : 0;
    }

    private Border WeightedPick(List<Border> items, Func<Border, double> weight)
    {
        double sum = 0;
        var w = new double[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            w[i] = Math.Max(0.01, weight(items[i]));
            sum += w[i];
        }
        double r = _autoSeedRng.NextDouble() * sum;
        double acc = 0;
        for (int i = 0; i < items.Count; i++)
        {
            acc += w[i];
            if (r <= acc) return items[i];
        }
        return items[^1];
    }

    private void AutoSeedFacility(Card card, int player)
    {
        if (IsFacilityCard(card))
        {
            var missions = AllMissionBorders()
                .Where(m => m.Tag is Card mc && CanSeedFacilityAtMission(card, mc).ok)
                .ToList();
            if (missions.Count == 0)
            {
                (player == 1 ? _seedCards : _oppSeedCards).Add(card);
                return;
            }

            // Prefer own missions, strongly avoid stacking this player's outposts
            // on the same mission (second outpost ~15% weight, third ~2%, …).
            var mission = WeightedPick(missions, m =>
            {
                int ownFacilities = GetDockablesUnderMission(m).Count(b =>
                    GetBorderOwner(b) == player
                    && b.Tag is Card fc
                    && IsFacilityCard(fc));
                double ownMission = GetBorderOwner(m) == player ? 3.0 : 1.0;
                double stackPenalty = Math.Pow(0.15, ownFacilities);
                double jitter = 0.7 + _autoSeedRng.NextDouble();
                return ownMission * stackPenalty * jitter;
            });

            var border = AddCardToTable(card, Canvas.GetLeft(mission),
                Canvas.GetTop(mission) + DockSlotOffsetY(0, player), TableCardWidth);
            SetBorderOwner(border, player);
            RelayoutDockablesUnderMission(mission);
            UpdateHostBadge(border);
            return;
        }

        if (IsShipCard(card))
        {
            // Prefer a mission that already has this player's facility
            var candidates = AllMissionBorders().ToList();
            if (candidates.Count == 0)
            {
                (player == 1 ? _seedCards : _oppSeedCards).Add(card);
                return;
            }
            var mission = WeightedPick(candidates, m =>
            {
                bool hasOwnFacility = GetDockablesUnderMission(m).Any(b =>
                    GetBorderOwner(b) == player
                    && b.Tag is Card fc
                    && IsFacilityCard(fc));
                double w = hasOwnFacility ? 6.0 : 1.0;
                if (GetBorderOwner(m) == player) w *= 2.0;
                return w * (0.7 + _autoSeedRng.NextDouble());
            });
            var border = AddCardToTable(card, Canvas.GetLeft(mission),
                Canvas.GetTop(mission) + DockSlotOffsetY(0, player), TableCardWidth);
            SetBorderOwner(border, player);
            RelayoutDockablesUnderMission(mission);
            UpdateHostBadge(border);
            return;
        }

        if (IsTablePermanentType(card) || IsDoorwayCard(card))
        {
            CommitCardToTable(card, player);
            return;
        }

        // Personnel/equipment seeded: report to own facility if any
        var host = TableCanvas.Children.OfType<Border>()
            .FirstOrDefault(b => b.Tag is Card hc && ReportingRules.IsFacilityHost(hc)
                                 && GetBorderOwner(b) == player);
        if (host != null)
        {
            var border = AddCardToTable(card, Canvas.GetLeft(host), Canvas.GetTop(host), TableCardWidth);
            SetBorderOwner(border, player);
            AddCardToHostStack(host, border);
            UpdateHostBadge(host);
            return;
        }

        (player == 1 ? _seedCards : _oppSeedCards).Add(card);
    }

    private List<Border> AllMissionBorders() =>
        TableCanvas.Children.OfType<Border>()
            .Where(b => b.Tag is Card c &&
                        string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase))
            .ToList();

    private void MenuSaveGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save game",
                Filter = "STCCG save (*.stsave)|*.stsave",
                FileName = $"game_{DateTime.Now:yyyyMMdd_HHmm}.stsave"
            };
            if (dlg.ShowDialog() != true) return;
            // Sync UI counters into session before capture
            _session.ActivePlayer = _activePlayer is 1 or 2 ? _activePlayer : _session.ActivePlayer;
            if (_turnNumber > 0) _session.TurnNumber = _turnNumber;
            var snap = CaptureGameSave();
            var json = JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(dlg.FileName, json);
            _session.Log.Add(_session.TurnNumber, "Pystem", $"Game saved: {System.IO.Path.GetFileName(dlg.FileName)}");
            StatusText.Text = $"Saved {System.IO.Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save:\n\n{ex.Message}", "Save game",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void MenuLoadGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load game",
                Filter = "STCCG save (*.stsave)|*.stsave"
            };
            if (dlg.ShowDialog() != true) return;
            var snap = JsonSerializer.Deserialize<GameSave>(
                System.IO.File.ReadAllText(dlg.FileName),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (snap == null)
            {
                MessageBox.Show("Empty or invalid save file.", "Load game",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ApplyGameSave(snap);
            StatusText.Text =
                $"Loaded · T{_turnNumber} · P{_activePlayer} · P1 {_scoreP1} / P2 {_scoreP2}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not load:\n\n{ex.Message}", "Load game",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static CardRef ToRef(Card c) => new()
    {
        Name = c.Name ?? "",
        Set = c.SetFolder,
        Type = c.Type
    };

    private Card? ResolveCard(CardRef? r)
    {
        if (r == null || string.IsNullOrWhiteSpace(r.Name) || _db == null) return null;
        var all = _db.AllCards;
        Card? hit = null;
        if (!string.IsNullOrWhiteSpace(r.Set))
            hit = all.FirstOrDefault(c =>
                string.Equals(c.Name, r.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.SetFolder, r.Set, StringComparison.OrdinalIgnoreCase));
        hit ??= all.FirstOrDefault(c => string.Equals(c.Name, r.Name, StringComparison.OrdinalIgnoreCase));
        return hit;
    }

    private GameSave CaptureGameSave()
    {
        var save = new GameSave
        {
            DeckNameP1 = _loadedDeck?.Name,
            DeckNameP2 = _loadedDeckOpp?.Name,
            Session = new SessionSnap
            {
                Match = _session.Match.ToString(),
                Segment = _session.Segment.ToString(),
                ActivePlayer = _session.ActivePlayer,
                TurnNumber = _session.TurnNumber,
                HasDrawn = _session.HasDrawnThisTurn,
                SuppressDraw = _session.SuppressEndOfTurnDraw,
                NormalPlayUsed = _session.NormalCardPlayUsed,
                NormalPlayForfeit = _session.NormalCardPlayForfeited,
                SeedPhaseActive = _seedPhaseActive,
                SeedSubPhase = _seedSubPhase.ToString(),
                ScoreP1 = _scoreP1,
                ScoreP2 = _scoreP2,
                HorgahnP1 = _horgahnP1,
                HorgahnP2 = _horgahnP2,
                HorgahnExtraUsed = _horgahnExtraPlayUsed,
                IonizationBeamsThisTurn = _ionizationBeamsThisTurn,
                RedAlertPlaysLeft = _redAlertPlaysLeft
            }
        };

        void PutZone(string key, IEnumerable<Card> cards)
        {
            save.Zones[key] = cards.Select(ToRef).ToList();
        }
        PutZone("p1.hand", _handCards);
        PutZone("p1.draw", _drawCards);
        PutZone("p1.discard", _discardCards);
        PutZone("p1.seed", _seedCards);
        PutZone("p1.door", _doorwayCards);
        PutZone("p1.mission", _missionSeedCards);
        PutZone("p1.dilemma", _dilemmaSeedCards);
        PutZone("p1.facility", _facilitySeedCards);
        PutZone("p1.qs", _qsTentCards);
        PutZone("p1.bb", _battleBridgeCards);
        PutZone("p1.qc", _qContinuumCards);
        PutZone("p1.site", _sitePileCards);
        PutZone("p1.tribble", _tribbleCards);
        PutZone("p1.side", _sideCards);
        PutZone("p1.oop", _outOfPlayP1);
        PutZone("p2.hand", _oppHandCards);
        PutZone("p2.draw", _oppDrawCards);
        PutZone("p2.discard", _oppDiscardCards);
        PutZone("p2.seed", _oppSeedCards);
        PutZone("p2.door", _oppDoorwayCards);
        PutZone("p2.mission", _oppMissionSeedCards);
        PutZone("p2.dilemma", _oppDilemmaSeedCards);
        PutZone("p2.facility", _oppFacilitySeedCards);
        PutZone("p2.qs", _oppQsTentCards);
        PutZone("p2.bb", _oppBattleBridgeCards);
        PutZone("p2.qc", _oppQContinuumCards);
        PutZone("p2.site", _oppSitePileCards);
        PutZone("p2.tribble", _oppTribbleCards);
        PutZone("p2.side", _oppSideCards);
        PutZone("p2.oop", _outOfPlayP2);

        var tableBorders = TableCanvas.Children.OfType<Border>().Where(b => b.Tag is Card).ToList();
        var idOf = new Dictionary<Border, int>();
        int next = 1;
        foreach (var b in tableBorders)
        {
            idOf[b] = next;
            var card = (Card)b.Tag!;
            save.Table.Add(new TableCardSnap
            {
                Id = next,
                Name = card.Name ?? "",
                Set = card.SetFolder,
                Type = card.Type,
                X = Canvas.GetLeft(b),
                Y = Canvas.GetTop(b),
                Z = Panel.GetZIndex(b),
                Owner = GetBorderOwner(b),
                Visible = b.Visibility == Visibility.Visible,
                Hull = _hullDamagePercent.GetValueOrDefault(b),
                Stopped = _stoppedBorders.Contains(b),
                RangeLeft = _shipRangeLeft.TryGetValue(b, out int rng) ? rng : null,
                RepairTurns = _repairTurnsAtOutpost.GetValueOrDefault(b),
                SolvedBy = _missionSolver.TryGetValue(b, out int sol) ? sol : null
            });
            next++;
        }

        foreach (var kv in _stackOnHost)
        {
            if (!idOf.TryGetValue(kv.Key, out int hid)) continue;
            save.Stacks.Add(new StackSnap
            {
                HostId = hid,
                ChildIds = kv.Value.Where(idOf.ContainsKey).Select(c => idOf[c]).ToList()
            });
        }
        foreach (var kv in _seedUnderMission)
        {
            if (!idOf.TryGetValue(kv.Key, out int hid)) continue;
            save.SeedUnder.Add(new StackSnap
            {
                HostId = hid,
                ChildIds = kv.Value.Where(idOf.ContainsKey).Select(c => idOf[c]).ToList()
            });
        }
        foreach (var m in _spacelineOrder)
        {
            if (idOf.TryGetValue(m, out int mid))
                save.Spaceline.Add(mid);
        }

        foreach (var ev in _attachedEvents)
        {
            save.AttachedEvents.Add(new AttachedEventSnap
            {
                Card = ToRef(ev.Card),
                Kind = ev.Kind.ToString(),
                Owner = ev.Owner,
                HostId = ev.Host != null && idOf.TryGetValue(ev.Host, out int h1) ? h1 : null,
                Host2Id = ev.Host2 != null && idOf.TryGetValue(ev.Host2, out int h2) ? h2 : null,
                Countdown = ev.Countdown,
                FaceUp = ev.FaceUp,
                EspionageAs = ev.EspionageAs,
                EspionageOn = ev.EspionageOn
            });
        }
        foreach (var d in _attachedDilemmas)
        {
            save.AttachedDilemmas.Add(new AttachedDilemmaSnap
            {
                Card = ToRef(d.Card),
                Kind = d.Kind.ToString(),
                HostId = d.Host != null && idOf.TryGetValue(d.Host, out int hid) ? hid : 0,
                Countdown = d.Countdown
            });
        }

        foreach (var e in _session.Log.Entries)
            save.Log.Add(new LogSnap { Utc = e.Utc, Turn = e.Turn, Actor = e.Actor, Text = e.Text });

        return save;
    }

    private void ApplyGameSave(GameSave save)
    {
        if (_db == null)
        {
            MessageBox.Show("Card database is not loaded.", "Load game",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ClearTableCards();
        _handCards.Clear(); _seedCards.Clear(); _doorwayCards.Clear();
        _missionSeedCards.Clear(); _dilemmaSeedCards.Clear(); _artifactSeedCards.Clear();
        _facilitySeedCards.Clear(); _drawCards.Clear(); _sideCards.Clear();
        _qsTentCards.Clear(); _battleBridgeCards.Clear(); _qContinuumCards.Clear();
        _sitePileCards.Clear(); _tribbleCards.Clear(); _discardCards.Clear();
        _oppHandCards.Clear(); _oppDrawCards.Clear(); _oppDiscardCards.Clear();
        _oppSeedCards.Clear(); _oppQsTentCards.Clear(); _oppBattleBridgeCards.Clear();
        _oppQContinuumCards.Clear(); _oppSitePileCards.Clear(); _oppTribbleCards.Clear();
        _oppSideCards.Clear(); _oppDoorwayCards.Clear(); _oppMissionSeedCards.Clear();
        _oppDilemmaSeedCards.Clear(); _oppFacilitySeedCards.Clear();
        _outOfPlayP1.Clear(); _outOfPlayP2.Clear();
        _stackOnHost.Clear(); _seedUnderMission.Clear();
        _revealedArtifactsUnderMission.Clear(); _lastEncounteredDilemma.Clear();
        RemoveBorgShipToken();
        _solvedMissions.Clear(); _missionSolver.Clear();
        _hullDamagePercent.Clear(); _stoppedBorders.Clear();
        _repairTurnsAtOutpost.Clear(); _shipRangeLeft.Clear();
        _borderOwner.Clear(); _attachedDilemmas.Clear(); _attachedEvents.Clear();
        _missionsByQuadrant.Clear(); _spacelineOrder.Clear();
        ClearMissionSlotPreviews();
        foreach (var kv in _damageBadges.ToList())
        {
            if (TableCanvas.Children.Contains(kv.Value)) TableCanvas.Children.Remove(kv.Value);
        }
        _damageBadges.Clear();
        foreach (var kv in _missionSolvedLabels.ToList())
            TableCanvas.Children.Remove(kv.Value);
        _missionSolvedLabels.Clear();

        List<Card> Fill(string key)
        {
            var list = new List<Card>();
            if (!save.Zones.TryGetValue(key, out var refs) || refs == null) return list;
            foreach (var r in refs)
            {
                var c = ResolveCard(r);
                if (c != null) list.Add(c);
            }
            return list;
        }
        _handCards.AddRange(Fill("p1.hand"));
        _drawCards.AddRange(Fill("p1.draw"));
        _discardCards.AddRange(Fill("p1.discard"));
        _seedCards.AddRange(Fill("p1.seed"));
        _doorwayCards.AddRange(Fill("p1.door"));
        _missionSeedCards.AddRange(Fill("p1.mission"));
        _dilemmaSeedCards.AddRange(Fill("p1.dilemma"));
        _facilitySeedCards.AddRange(Fill("p1.facility"));
        _qsTentCards.AddRange(Fill("p1.qs"));
        _battleBridgeCards.AddRange(Fill("p1.bb"));
        _qContinuumCards.AddRange(Fill("p1.qc"));
        _sitePileCards.AddRange(Fill("p1.site"));
        _tribbleCards.AddRange(Fill("p1.tribble"));
        _sideCards.AddRange(Fill("p1.side"));
        _outOfPlayP1.AddRange(Fill("p1.oop"));
        _oppHandCards.AddRange(Fill("p2.hand"));
        _oppDrawCards.AddRange(Fill("p2.draw"));
        _oppDiscardCards.AddRange(Fill("p2.discard"));
        _oppSeedCards.AddRange(Fill("p2.seed"));
        _oppDoorwayCards.AddRange(Fill("p2.door"));
        _oppMissionSeedCards.AddRange(Fill("p2.mission"));
        _oppDilemmaSeedCards.AddRange(Fill("p2.dilemma"));
        _oppFacilitySeedCards.AddRange(Fill("p2.facility"));
        _oppQsTentCards.AddRange(Fill("p2.qs"));
        _oppBattleBridgeCards.AddRange(Fill("p2.bb"));
        _oppQContinuumCards.AddRange(Fill("p2.qc"));
        _oppSitePileCards.AddRange(Fill("p2.site"));
        _oppTribbleCards.AddRange(Fill("p2.tribble"));
        _oppSideCards.AddRange(Fill("p2.side"));
        _outOfPlayP2.AddRange(Fill("p2.oop"));

        var byId = new Dictionary<int, Border>();
        foreach (var snap in save.Table)
        {
            var card = ResolveCard(new CardRef { Name = snap.Name, Set = snap.Set, Type = snap.Type });
            if (card == null) continue;
            var border = AddCardToTable(card, snap.X, snap.Y, TableCardWidth);
            Panel.SetZIndex(border, snap.Z);
            SetBorderOwner(border, snap.Owner is 1 or 2 ? snap.Owner : 1);
            if (!snap.Visible) border.Visibility = Visibility.Collapsed;
            if (snap.Hull > 0)
            {
                _hullDamagePercent[border] = snap.Hull;
                if (snap.Hull >= 50 && snap.Hull < 100)
                {
                    border.RenderTransformOrigin = new Point(0.5, 0.5);
                    border.RenderTransform = new RotateTransform(180);
                }
                UpdateDamageBadge(border, snap.Hull);
            }
            if (snap.Stopped) MarkStopped(border);
            if (snap.RangeLeft.HasValue) _shipRangeLeft[border] = snap.RangeLeft.Value;
            if (snap.RepairTurns > 0) _repairTurnsAtOutpost[border] = snap.RepairTurns;
            if (snap.SolvedBy is 1 or 2)
            {
                _solvedMissions.Add(border);
                _missionSolver[border] = snap.SolvedBy.Value;
            }
            byId[snap.Id] = border;
        }

        foreach (var st in save.Stacks)
        {
            if (!byId.TryGetValue(st.HostId, out var host)) continue;
            foreach (var cid in st.ChildIds)
            {
                if (byId.TryGetValue(cid, out var child))
                    AddCardToHostStack(host, child);
            }
            UpdateHostBadge(host);
        }
        foreach (var st in save.SeedUnder)
        {
            if (!byId.TryGetValue(st.HostId, out var mission)) continue;
            foreach (var cid in st.ChildIds)
            {
                if (byId.TryGetValue(cid, out var child))
                    AddSeedUnderMission(mission, child);
            }
        }

        _spacelineOrder.Clear();
        _missionsByQuadrant.Clear();
        foreach (var id in save.Spaceline)
        {
            if (!byId.TryGetValue(id, out var m) || m.Tag is not Card mc) continue;
            _spacelineOrder.Add(m);
            string q = string.IsNullOrWhiteSpace(mc.Quadrant) ? "Alpha" : mc.Quadrant!;
            if (!_missionsByQuadrant.TryGetValue(q, out var list))
            {
                list = new List<Border>();
                _missionsByQuadrant[q] = list;
            }
            list.Add(m);
        }
        if (_spacelineOrder.Count == 0)
        {
            foreach (var b in byId.Values)
            {
                if (b.Tag is Card c && (c.Type ?? "").Contains("Mission", StringComparison.OrdinalIgnoreCase))
                    _spacelineOrder.Add(b);
            }
        }

        foreach (var ev in save.AttachedEvents)
        {
            var card = ResolveCard(ev.Card);
            if (card == null) continue;
            if (!Enum.TryParse(ev.Kind, out EventRules.Persist kind))
                kind = EventRules.Persist.Table;
            Border? host = ev.HostId is int hid && byId.TryGetValue(hid, out var h) ? h : null;
            Border? host2 = ev.Host2Id is int hid2 && byId.TryGetValue(hid2, out var h2) ? h2 : null;
            _attachedEvents.Add(new AttachedEvent
            {
                Card = card,
                Kind = kind,
                Owner = ev.Owner,
                Host = host,
                Host2 = host2,
                Countdown = ev.Countdown,
                FaceUp = ev.FaceUp,
                EspionageAs = ev.EspionageAs,
                EspionageOn = ev.EspionageOn
            });
        }
        foreach (var d in save.AttachedDilemmas)
        {
            var card = ResolveCard(d.Card);
            if (card == null) continue;
            if (!byId.TryGetValue(d.HostId, out var host)) continue;
            if (!Enum.TryParse(d.Kind, out DilemmaRules.PersistKind kind))
                kind = DilemmaRules.PersistKind.None;
            _attachedDilemmas.Add(new AttachedDilemma
            {
                Card = card,
                Kind = kind,
                Host = host,
                Countdown = d.Countdown
            });
        }

        var s = save.Session ?? new SessionSnap();
        if (!Enum.TryParse(s.Match, true, out GameSession.MatchPhase match))
            match = s.SeedPhaseActive ? GameSession.MatchPhase.Seed : GameSession.MatchPhase.Play;
        if (!Enum.TryParse(s.Segment, true, out GameSession.TurnSegment seg))
            seg = GameSession.TurnSegment.Play;
        int active = s.ActivePlayer is 1 or 2 ? s.ActivePlayer : 1;
        int turn = Math.Max(0, s.TurnNumber);
        if (match == GameSession.MatchPhase.Play && turn < 1) turn = 1;
        _session.Restore(match, seg, active, turn,
            s.HasDrawn, s.SuppressDraw, s.NormalPlayUsed, s.NormalPlayForfeit);
        _activePlayer = active;
        _turnNumber = turn;
        _scoreP1 = s.ScoreP1;
        _scoreP2 = s.ScoreP2;
        _horgahnP1 = s.HorgahnP1;
        _horgahnP2 = s.HorgahnP2;
        _horgahnExtraPlayUsed = s.HorgahnExtraUsed;
        _ionizationBeamsThisTurn = s.IonizationBeamsThisTurn;
        _redAlertPlaysLeft = s.RedAlertPlaysLeft;
        _seedPhaseActive = s.SeedPhaseActive || match == GameSession.MatchPhase.Seed;
        if (Enum.TryParse(s.SeedSubPhase, true, out SeedSubPhase ssp))
            _seedSubPhase = ssp;
        else if (!_seedPhaseActive)
            _seedSubPhase = SeedSubPhase.Done;

        // UI treats null decks as "no game" — keep placeholder names from save
        _loadedDeck ??= new Deck { Name = save.DeckNameP1 ?? "Player 1" };
        if (!string.IsNullOrWhiteSpace(save.DeckNameP1))
            _loadedDeck.Name = save.DeckNameP1!;
        _loadedDeckOpp ??= new Deck { Name = save.DeckNameP2 ?? "Player 2" };
        if (!string.IsNullOrWhiteSpace(save.DeckNameP2))
            _loadedDeckOpp.Name = save.DeckNameP2!;

        if (save.Log != null && save.Log.Count > 0)
        {
            _session.Log.Restore(save.Log.Select(e =>
                new ActionLog.Entry(e.Utc == default ? DateTime.UtcNow : e.Utc, e.Turn, e.Actor ?? "", e.Text ?? "")));
        }
        else
            _session.Log.Clear();
        _session.Log.Add(_session.TurnNumber, "Pystem",
            $"Game loaded (T{_session.TurnNumber}, P{_session.ActivePlayer}, score {_scoreP1}/{_scoreP2})");

        if (WelcomeOverlay != null) WelcomeOverlay.Visibility = Visibility.Collapsed;
        RelayoutMissionsOnSpaceline();
        UpdateScoreDisplay();
        RefreshZoneCounts();
        ApplyPerspective();
        ShowActivePlayerHand();
        SyncSessionToUi();
        RefreshActionHistory();
        UpdateTurnTints();
        if (_seedPhaseActive)
            ShowCurrentSeedStack();
        StatusText.Text =
            $"Restored: Turn {_turnNumber}, Player {_activePlayer}, " +
            $"segment {_session.Segment}, score P1 {_scoreP1} · P2 {_scoreP2}";
    }

    private void WelcomeDeckBuilder_Click(object sender, RoutedEventArgs e)
    {
        WelcomeOverlay.Visibility = Visibility.Collapsed;
        MenuDeckBuilder_Click(sender, e);
    }

    private void FinishSeedPhaseAndDrawOpeningHand()
    {
        // Ungelegte Seed-Karten → Rest-Seed-Stapel (pro Spieler)
        _seedCards.Clear();
        _seedCards.AddRange(_doorwayCards);
        _seedCards.AddRange(_missionSeedCards);
        _seedCards.AddRange(_dilemmaSeedCards);
        _seedCards.AddRange(_facilitySeedCards);
        _doorwayCards.Clear();
        _missionSeedCards.Clear();
        _dilemmaSeedCards.Clear();
        _facilitySeedCards.Clear();

        _oppSeedCards.Clear();
        _oppSeedCards.AddRange(_oppDoorwayCards);
        _oppSeedCards.AddRange(_oppMissionSeedCards);
        _oppSeedCards.AddRange(_oppDilemmaSeedCards);
        _oppSeedCards.AddRange(_oppFacilitySeedCards);
        _oppDoorwayCards.Clear();
        _oppMissionSeedCards.Clear();
        _oppDilemmaSeedCards.Clear();
        _oppFacilitySeedCards.Clear();

        ClearMissionSlotPreviews();

        // Draw mischen + Opening Hand Spieler 1
        ShuffleList(_drawCards);
        int hand = Math.Min(OpeningHandSize, _drawCards.Count);
        for (int i = 0; i < hand; i++)
        {
            _handCards.Add(_drawCards[0]);
            _drawCards.RemoveAt(0);
        }

        // Hotseat: Opening Hand Spieler 2
        if (_gameMode == GameMode.Hotseat && _oppDrawCards.Count > 0)
        {
            ShuffleList(_oppDrawCards);
            int hand2 = Math.Min(OpeningHandSize, _oppDrawCards.Count);
            for (int i = 0; i < hand2; i++)
            {
                _oppHandCards.Add(_oppDrawCards[0]);
                _oppDrawCards.RemoveAt(0);
            }
        }

        _seedPhaseActive = false;
        _seedSubPhase = SeedSubPhase.Done;
        _activePlayer = 1;
        _turnNumber = 1;
        _session.StartPlayFromSeed();
        ApplyRandomBoardBackground();
        ResetShipRangesForTurn();
        RefreshRedAlertForTurn();
        _activePlayer = _session.ActivePlayer;
        _turnNumber = _session.TurnNumber;
        ApplyPerspective();
        UpdatePhaseControls();
        string oppInfo = _loadedDeckOpp != null
            ? $", S2-Hand {_oppHandCards.Count}/Draw {_oppDrawCards.Count}"
            : "";
        StatusText.Text =
            $"Seed beendet → S1-Hand {_handCards.Count}/Draw {_drawCards.Count}{oppInfo}. " +
            "Hotseat: bottom = active player. End turn switches.";
        ShowStackContents("Hand", opponent: false);
        SetTischZoneHighlight(false);
        ClearZoneHighlight();
    }

    private static void ShuffleList<T>(IList<T> list)
    {
        var rng = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Scroll so the middle of the spaceline sits in the middle of the viewport
    /// (not the leftmost mission).
    /// </summary>
    private void CenterOnSpaceline()
    {
        double scale = ZoomTransform.ScaleX <= 0 ? 1 : ZoomTransform.ScaleX;

        double midX;
        if (_spacelineOrder.Count >= 2)
        {
            double left = Canvas.GetLeft(_spacelineOrder[0]);
            double right = Canvas.GetLeft(_spacelineOrder[^1]) + TableCardWidth;
            midX = (left + right) / 2.0;
        }
        else if (_spacelineOrder.Count == 1)
        {
            midX = Canvas.GetLeft(_spacelineOrder[0]) + TableCardWidth / 2.0;
        }
        else
        {
            midX = SpacelineStartX + 250;
        }

        double targetY = (SpacelineY + TableCardHeight / 2.0) * scale;
        double targetX = midX * scale;

        double viewH = TableScroll.ViewportHeight;
        double viewW = TableScroll.ViewportWidth;
        if (viewW <= 0 || viewH <= 0) return;

        TableScroll.ScrollToVerticalOffset(Math.Max(0, targetY - viewH / 2));
        TableScroll.ScrollToHorizontalOffset(Math.Max(0, targetX - viewW / 2));
    }

    /// <summary>
    /// Pick a random PNG/JPG from BoardBackgrounds (DataPath first, then app Assets).
    /// One full image covers the 2800×1400 canvas — not split P1/P2.
    /// </summary>
    private void ApplyRandomBoardBackground()
    {
        if (TableCanvas == null) return;
        try
        {
            var files = new List<string>();
            void AddDir(string dir)
            {
                if (!Directory.Exists(dir)) return;
                files.AddRange(Directory.GetFiles(dir, "*.png"));
                files.AddRange(Directory.GetFiles(dir, "*.jpg"));
                files.AddRange(Directory.GetFiles(dir, "*.jpeg"));
            }
            AddDir(GamePaths.BoardBackgrounds);
            AddDir(System.IO.Path.Combine(DataPath, "BoardBackgrounds"));
            AddDir(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "BoardBackgrounds"));

            files = files
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(f => !System.IO.Path.GetFileName(f).StartsWith(".", StringComparison.Ordinal))
                .ToList();
            if (files.Count == 0)
            {
                TableCanvas.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
                return;
            }

            string pick = files[Random.Shared.Next(files.Count)];
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(pick, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            TableCanvas.Background = new ImageBrush(bmp)
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
            StatusText.Text = (StatusText.Text ?? "") + $"  ·  board: {System.IO.Path.GetFileName(pick)}";
        }
        catch
        {
            TableCanvas.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
        }
    }

    // ===================== KARTEN =====================

    private void RefreshZoneCounts()
    {
        void SetCount(Panel panel, string zoneTitle, int count)
        {
            string display = _aidZoneCounts ? count.ToString() : "";
            foreach (var child in panel.Children)
            {
                if (child is Border b && b.Tag is string tag && tag.EndsWith(":" + zoneTitle))
                {
                    if (b.Child is Grid g)
                    {
                        foreach (var el in g.Children)
                        {
                            if (el is Border badge && badge.Child is TextBlock ctb &&
                                ctb.Tag as string == "zoneCount")
                            {
                                ctb.Text = display;
                                badge.Visibility = _aidZoneCounts && count > 0
                                    ? Visibility.Visible : Visibility.Collapsed;
                            }
                            if (el is Border footer && footer.Child is TextBlock ftb &&
                                footer.VerticalAlignment == VerticalAlignment.Bottom)
                                ftb.Text = ShortName(zoneTitle);
                        }
                    }
                    else if (b.Child is TextBlock tb)
                    {
                        tb.Text = _aidZoneCounts && count > 0
                            ? $"{ShortName(zoneTitle)}\n{count}"
                            : ShortName(zoneTitle);
                    }
                }
            }
        }

        SetCount(PlayerZonesPanel, "Seed Deck", _seedCards.Count);
        SetCount(PlayerZonesPanel, "Doorways", _doorwayCards.Count);
        SetCount(PlayerZonesPanel, "Missions", _missionSeedCards.Count);
        SetCount(PlayerZonesPanel, "Dilemmas", _dilemmaSeedCards.Count);
        SetCount(PlayerZonesPanel, "Facilities", _facilitySeedCards.Count);
        SetCount(PlayerZonesPanel, "Draw Deck", _drawCards.Count);
        SetCount(PlayerZonesPanel, "Side Deck", _sideCards.Count);
        SetCount(PlayerZonesPanel, "Q's Tent", _qsTentCards.Count);
        SetCount(PlayerZonesPanel, "Battle Bridge", _battleBridgeCards.Count);
        SetCount(PlayerZonesPanel, "Q-Continuum", _qContinuumCards.Count);
        SetCount(PlayerZonesPanel, "Site Pile", _sitePileCards.Count);
        SetCount(PlayerZonesPanel, "Tribble", _tribbleCards.Count);
        SetCount(PlayerZonesPanel, "Discard", _discardCards.Count);
        SetCount(PlayerZonesPanel, "Hand", _handCards.Count);
        SetCount(PlayerZonesPanel, "Draw Deck", _drawCards.Count);
        SetCount(PlayerZonesPanel, "Doorways", _doorwayCards.Count);
        SetCount(PlayerZonesPanel, "Missions", _missionSeedCards.Count);
        SetCount(PlayerZonesPanel, "Dilemmas", _dilemmaSeedCards.Count);
        SetCount(PlayerZonesPanel, "Facilities", _facilitySeedCards.Count);

        SetCount(OpponentZonesPanel, "Discard", _oppDiscardCards.Count);
        SetCount(OpponentZonesPanel, "Hand", _oppHandCards.Count);
        SetCount(OpponentZonesPanel, "Draw Deck", _oppDrawCards.Count);
        SetCount(OpponentZonesPanel, "Doorways", _oppDoorwayCards.Count);
        SetCount(OpponentZonesPanel, "Missions", _oppMissionSeedCards.Count);
        SetCount(OpponentZonesPanel, "Dilemmas", _oppDilemmaSeedCards.Count);
        SetCount(OpponentZonesPanel, "Facilities", _oppFacilitySeedCards.Count);
        SetCount(OpponentZonesPanel, "Seed Deck", _oppSeedCards.Count);
        SetCount(OpponentZonesPanel, "Side Deck", _oppSideCards.Count);
        SetCount(OpponentZonesPanel, "Q's Tent", _oppQsTentCards.Count);
        SetCount(OpponentZonesPanel, "Battle Bridge", _oppBattleBridgeCards.Count);
        SetCount(OpponentZonesPanel, "Q-Continuum", _oppQContinuumCards.Count);
        SetCount(OpponentZonesPanel, "Site Pile", _oppSitePileCards.Count);
        SetCount(OpponentZonesPanel, "Tribble", _oppTribbleCards.Count);
    }

    private void PlaceDeckOnTable(Deck deck)
    {
        ClearTableCards();

        _handCards.Clear();
        _seedCards.Clear();
        _doorwayCards.Clear();
        _missionSeedCards.Clear();
        _dilemmaSeedCards.Clear();
        _artifactSeedCards.Clear(); // nicht mehr separat genutzt
        _sideDeckCovers.Clear();
        _oppSideDeckCovers.Clear();
        _oppTablePermanentCards.Clear();
        _tablePermanentCards.Clear();
        _attachedDilemmas.Clear();
        _horgahnP1 = _horgahnP2 = false;
        _horgahnExtraPlayUsed = false;
        _attachedEvents.Clear();
        _ionizationBeamsThisTurn = 0;
        _redAlertPlaysLeft = 0;
        _movedThisTurnAfterArrival.Clear();
        _stack.Clear();
        _outOfPlayP1.Clear();
        _outOfPlayP2.Clear();
        if (ResponsePanel != null) ResponsePanel.Visibility = Visibility.Collapsed;
        _borderOwner.Clear();
        if (TablePermanentsPanel != null) TablePermanentsPanel.Children.Clear();
        _facilitySeedCards.Clear();
        _drawCards.Clear();
        _sideCards.Clear();
        _qsTentCards.Clear();
        _battleBridgeCards.Clear();
        _qContinuumCards.Clear();
        _sitePileCards.Clear();
        _tribbleCards.Clear();
        _discardCards.Clear();
        _stackOnHost.Clear();
        _solvedMissions.Clear();
        _missionSolver.Clear();
        _hullDamagePercent.Clear();
        _stoppedBorders.Clear();
        _repairTurnsAtOutpost.Clear();
        foreach (var kv in _damageBadges.ToList())
        {
            if (TableCanvas.Children.Contains(kv.Value))
                TableCanvas.Children.Remove(kv.Value);
        }
        _damageBadges.Clear();
        _shipRangeLeft.Clear();
        foreach (var kv in _missionSolvedLabels.ToList())
        {
            TableCanvas.Children.Remove(kv.Value);
        }
        _missionSolvedLabels.Clear();
        _scoreP1 = 0;
        _scoreP2 = 0;
        UpdateScoreDisplay();

        _seedUnderMission.Clear();
        _revealedArtifactsUnderMission.Clear();
        _lastEncounteredDilemma.Clear();
        RemoveBorgShipToken();
        _missionsByQuadrant.Clear();
        _spacelineOrder.Clear();
        ClearMissionSlotPreviews();

        // Seed-Karten nach Regelbuch-Phasen aufteilen (manuell legen)
        foreach (var e in deck.SeedCards.Where(x => x.Card != null))
        {
            for (int i = 0; i < e.Quantity; i++)
                ClassifySeedCard(e.Card!);
        }

        // Draw-Deck
        foreach (var e in deck.DrawCards.Where(x => x.Card != null))
            for (int i = 0; i < e.Quantity; i++)
                _drawCards.Add(e.Card!);

        // Side Decks (benannt) + Legacy
        void Expand(List<DeckEntry> entries, List<Card> target)
        {
            foreach (var e in entries.Where(x => x.Card != null))
                for (int i = 0; i < e.Quantity; i++)
                    target.Add(e.Card!);
        }
        Expand(deck.QsTentCards, _qsTentCards);
        Expand(deck.BattleBridgeCards, _battleBridgeCards);
        Expand(deck.QContinuumCards, _qContinuumCards);
        Expand(deck.SitePileCards, _sitePileCards);
        Expand(deck.TribbleCards, _tribbleCards);
        Expand(deck.SideCards, _sideCards);

        WelcomeOverlay.Visibility = Visibility.Collapsed;
        EnterSeedPhase(SeedSubPhase.Doorway);
        StatusText.Text =
            $"SEED gestartet – Deck {deck.Name}: " +
            $"Door {_doorwayCards.Count}, Miss {_missionSeedCards.Count}, " +
            $"Dil/Art {_dilemmaSeedCards.Count}, Fac {_facilitySeedCards.Count}, " +
            $"Draw {_drawCards.Count}, Side {_sideCards.Count}";
        ShowCurrentSeedStack();
    }

    /// <summary>
    /// Teilt eine Seed-Karte in den passenden Phasen-Stapel ein.
    /// Doorway | Mission | Dilemma+Artifact | Rest → Facilities
    /// </summary>
    private void ClassifySeedCard(Card card)
    {
        string t = (card.Type ?? "").ToLowerInvariant();
        if (t.Contains("doorway"))
            _doorwayCards.Add(card);
        else if (t.Contains("mission"))
            _missionSeedCards.Add(card);
        else if (t.Contains("dilemma") || t.Contains("artifact"))
            _dilemmaSeedCards.Add(card);
        else
            _facilitySeedCards.Add(card);
    }

    private void ClearTableCards()
    {
        var toRemove = TableCanvas.Children.OfType<Border>()
            .Where(b => b.Tag is Card)
            .ToList();
        foreach (var b in toRemove)
            TableCanvas.Children.Remove(b);

        foreach (var badge in _hostBadges.Values.ToList())
            TableCanvas.Children.Remove(badge);
        _hostBadges.Clear();
        foreach (var badge in _hostBadgesP2.Values.ToList())
            TableCanvas.Children.Remove(badge);
        _hostBadgesP2.Clear();

        foreach (var badge in _seedBadges.Values.ToList())
            TableCanvas.Children.Remove(badge);
        _seedBadges.Clear();

        if (_selectionFrame != null)
            _selectionFrame.Visibility = Visibility.Collapsed;
        _selectedCard = null;
    }

    private void PlaceDemoCards()
    {
        if (_db == null) return;

        var missions = _db.AllCards
            .Where(c => string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        var missionBorders = new List<Border>();
        for (int i = 0; i < missions.Count; i++)
        {
            double x = SpacelineStartX + i * (TableCardWidth + MissionGap);
            missionBorders.Add(AddCardToTable(missions[i], x, SpacelineY, TableCardWidth));
        }

        // Demo: verdeckte Dilemmas unter erster Mission
        if (missionBorders.Count > 0)
        {
            var dilemmas = _db.AllCards
                .Where(c => string.Equals(c.Type, "Dilemma", StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .ToList();
            foreach (var d in dilemmas)
            {
                var b = AddCardToTable(d, SpacelineStartX, SpacelineY, TableCardWidth);
                AddSeedUnderMission(missionBorders[0], b);
            }
        }

        var ship = _db.AllCards
            .FirstOrDefault(c => string.Equals(c.Type, "Ship", StringComparison.OrdinalIgnoreCase));
        if (ship != null && missions.Count > 0)
        {
            // Direkt unter der ersten Mission platzieren
            double mx = SpacelineStartX;
            AddCardToTable(ship, mx, SpacelineY + UnderMissionGap, TableCardWidth);
        }
        else if (ship != null)
        {
            AddCardToTable(ship, SpacelineStartX + 40, ShipYPlayer, TableCardWidth);
        }

        // Zweites Schiff unter dieselbe Mission (Stapel-Demo)
        var ship2 = _db.AllCards
            .Where(c => string.Equals(c.Type, "Ship", StringComparison.OrdinalIgnoreCase))
            .Skip(1)
            .FirstOrDefault();
        if (ship2 != null && missions.Count > 0)
        {
            AddCardToTable(ship2, SpacelineStartX, SpacelineY + UnderMissionGap * 2, TableCardWidth);
        }

        // Outpost unter erste Mission (zwischen Mission und Schiffen)
        var outpost = _db.AllCards.FirstOrDefault(c =>
        {
            string t = (c.Type ?? "").ToLowerInvariant();
            string n = (c.Name ?? "").ToLowerInvariant();
            return t.Contains("facility") || t.Contains("outpost") || n.Contains("outpost");
        });
        if (outpost != null && missions.Count > 0)
        {
            AddCardToTable(outpost, SpacelineStartX, SpacelineY + UnderMissionGap, TableCardWidth);
            // Schiffe eine Position tiefer – Relayout nach allen Platzierungen
        }

        var personnel = _db.AllCards
            .Where(c => string.Equals(c.Type, "Personnel", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        // Stapel unter erster Mission sauber anordnen
        var firstMission = TableCanvas.Children.OfType<Border>()
            .FirstOrDefault(b => b.Tag is Card c &&
                                 string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase));
        if (firstMission != null)
            RelayoutDockablesUnderMission(firstMission);

        // Erstes Schiff finden und Personnel digital an Bord (verschwinden vom Feld)
        var firstShip = TableCanvas.Children.OfType<Border>()
            .FirstOrDefault(b => b.Tag is Card c && IsShipCard(c));

        if (firstShip != null)
        {
            foreach (var p in personnel)
            {
                var border = AddCardToTable(p, Canvas.GetLeft(firstShip), Canvas.GetTop(firstShip), TableCardWidth);
                AddCardToHostStack(firstShip, border);
            }

            // Equipment zum Testen
            var equipment = _db.AllCards
                .Where(c => string.Equals(c.Type, "Equipment", StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            foreach (var eq in equipment)
            {
                var border = AddCardToTable(eq, Canvas.GetLeft(firstShip), Canvas.GetTop(firstShip), TableCardWidth);
                AddCardToHostStack(firstShip, border);
            }
        }
        else
        {
            double px = SpacelineStartX + 220;
            foreach (var p in personnel)
            {
                AddCardToTable(p, px, ShipYPlayer + 40, TableCardWidth);
                px += 110;
            }
        }
    }

    private Border AddCardToTable(Card card, double x, double y, double width)
    {
        // Einheitliche Größe auf dem Spielfeld
        width = TableCardWidth;
        var border = new Border
        {
            Width = TableCardWidth,
            Height = TableCardHeight,
            BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            CornerRadius = new CornerRadius(3),
            Cursor = Cursors.Hand,
            Tag = card
        };

        var img = new Image { Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

        if (!string.IsNullOrEmpty(card.FullImagePath) && System.IO.File.Exists(card.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(card.FullImagePath, UriKind.Absolute);
                bmp.DecodePixelWidth = 200;
                bmp.EndInit();
                img.Source = bmp;
            }
            catch { }
        }

        border.Child = img;
        border.MouseLeftButtonDown += Card_MouseLeftButtonDown;
        border.MouseLeftButtonUp += Card_MouseLeftButtonUp;
        border.MouseMove += Card_MouseMove;
        border.PreviewMouseRightButtonDown += Card_MouseRightButtonDown;
        border.MouseRightButtonDown += Card_MouseRightButtonDown;
        border.MouseRightButtonUp += Card_MouseRightButtonUp;

        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        TableCanvas.Children.Add(border);
        return border;
    }

    // ===================== DRAG =====================

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not Card card)
            return;

        // Versteckte Stapel-Karten nicht vom Canvas ziehen
        if (border.Visibility != Visibility.Visible)
            return;

        // Beam/Fly-Modus: Klick = Ziel
        if (_cardActionMode != CardActionMode.None)
        {
            if (TryHandleActionModeClick(border))
            {
                e.Handled = true;
                return;
            }
        }

        ShowCardDetail(card);
        SetSelection(border);
        if (e.ClickCount >= 2)
        {
            // Double-click host stack → detail + scrollable contents (not the hand strip)
            bool isHost = IsShipCard(card) || IsFacilityCard(card)
                          || string.Equals(card.Type, "Mission", StringComparison.OrdinalIgnoreCase);
            _detailHost = isHost ? border : null;
            OpenCardDetailPopup();
            e.Handled = true;
            return;
        }

        // Single click: selection + action buttons only (no strip fill)

        // Missionen: nur auswählen, nicht verschieben
        if (string.Equals(card.Type, "Mission", StringComparison.OrdinalIgnoreCase))
        {
            e.Handled = true;
            return;
        }

        // Events / dilemmas / artifacts on the board are not free-drag furniture.
        // (Q-Net, Gaps, Plasma Fire, seeded dilemmas, … stay where rules placed them.)
        // Ships, facilities, personnel, equipment remain draggable for legal moves.
        if (!_seedPhaseActive
            && (EventRules.IsEvent(card)
                || string.Equals(card.Type, "Dilemma", StringComparison.OrdinalIgnoreCase)
                || string.Equals(card.Type, "Artifact", StringComparison.OrdinalIgnoreCase)
                || ArtifactRules.IsArtifact(card)))
        {
            e.Handled = true;
            return;
        }

        // Drag vorbereiten, aber NOCH nicht als Ziehen werten / nichts umsortieren
        _dragCard = border;
        _dragOffset = e.GetPosition(border);
        _dragOrigin = new Point(Canvas.GetLeft(border), Canvas.GetTop(border));
        _dragSourceMission = FindMissionContainingCard(border);
        _isDragging = false;
        _mouseDownScreen = e.GetPosition(TableCanvas);
        border.CaptureMouse();
        e.Handled = true;
    }

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (_dragCard == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var pos = e.GetPosition(TableCanvas);

        if (!_isDragging)
        {
            double dx = pos.X - _mouseDownScreen.X;
            double dy = pos.Y - _mouseDownScreen.Y;
            if (dx * dx + dy * dy < DragThreshold * DragThreshold)
                return; // nur Klick, noch kein Drag

            // Ab hier: echtes Ziehen
            _isDragging = true;
            Panel.SetZIndex(_dragCard, 1000);
            ShowOriginPreview(_dragOrigin);

            // Quell-Stapel erst jetzt nachrücken lassen
            if (_dragSourceMission != null && _dragCard.Tag is Card c && IsDockableUnderMission(c))
                RelayoutDockablesUnderMission(_dragSourceMission, exclude: _dragCard);
        }

        Canvas.SetLeft(_dragCard, pos.X - _dragOffset.X);
        Canvas.SetTop(_dragCard, pos.Y - _dragOffset.Y);
        UpdateSnapPreview(_dragCard);
    }

    private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragCard == null) return;

        var cardBorder = _dragCard;
        var card = cardBorder.Tag as Card;
        bool wasDragging = _isDragging;

        HideSnapPreview();
        cardBorder.ReleaseMouseCapture();
        Panel.SetZIndex(cardBorder, 0);

        // Nur Klick ohne Bewegung → Positionen unverändert lassen
        if (!wasDragging)
        {
            _dragCard = null;
            _dragSourceMission = null;
            _isDragging = false;
            return;
        }

        if (card != null && IsDoorwayCard(card) &&
            TryPlaceDoorwayOnSideDeck(card, cardBorder, e.GetPosition(this)))
        {
            // auf Side-Deck gelegt
        }
        else if (card != null && IsMissionCard(card))
        {
            foreach (var kv in _missionsByQuadrant.ToList())
                kv.Value.Remove(cardBorder);
            PlaceMissionOnSpaceline(cardBorder, card);
        }
        else if (card != null && IsSeedableUnderMission(card)
                 && (_seedPhaseActive || _devPlaySeedFromHand))
        {
            var targetMission = TrySnapToMission(cardBorder);
            if (targetMission != null)
                AddSeedUnderMission(targetMission, cardBorder);
        }
        else if (card != null && IsStackableCard(card))
        {
            var host = TrySnapToHost(cardBorder);
            if (host != null)
                AddCardToHostStack(host, cardBorder);
        }
        else if (card != null && IsFacilityCard(card))
        {
            var targetMission = FindNearestLegalFacilityMission(card, cardBorder, out _);
            if (targetMission != null)
            {
                Canvas.SetLeft(cardBorder, Canvas.GetLeft(targetMission));
                {
                    int ow = GetBorderOwner(cardBorder);
                    if (ow == 0) ow = _activePlayer;
                    SetBorderOwner(cardBorder, ow);
                    Canvas.SetTop(cardBorder, Canvas.GetTop(targetMission) + DockSlotOffsetY(0, ow));
                }
                if (_dragSourceMission != null && _dragSourceMission != targetMission)
                    RelayoutDockablesUnderMission(_dragSourceMission);
                RelayoutDockablesUnderMission(targetMission);
                UpdateHostBadge(cardBorder);
            }
            else
            {
                // Ungültig: an Ursprung zurück oder Status
                if (_dragOrigin.X > 0 || _dragOrigin.Y > 0)
                {
                    Canvas.SetLeft(cardBorder, _dragOrigin.X);
                    Canvas.SetTop(cardBorder, _dragOrigin.Y);
                }
                StatusText.Text = $"{card.Name} belongs at a planet mission.";
            }
        }
        else if (card != null && IsDockableUnderMission(card))
        {
            int owner = GetBorderOwner(cardBorder);
            if (owner == 0) owner = _activePlayer;
            SetBorderOwner(cardBorder, owner);

            var targetMission = TrySnapToMission(cardBorder);
            bool okMission = targetMission != null
                && targetMission.Tag is Card tmc
                && string.Equals(tmc.Type, "Mission", StringComparison.OrdinalIgnoreCase);

            if (_dragSourceMission != null && _dragSourceMission != targetMission)
                RelayoutDockablesUnderMission(_dragSourceMission);

            if (okMission && IsShipCard(card) && !_seedPhaseActive
                && _session.Match == GameSession.MatchPhase.Play)
            {
                // 7.1.3 + 7.1.5
                if (!TryMoveShipWithRules(cardBorder, card, _dragSourceMission, targetMission!))
                {
                    if (_dragOrigin.X > 0 || _dragOrigin.Y > 0)
                    {
                        Canvas.SetLeft(cardBorder, _dragOrigin.X);
                        Canvas.SetTop(cardBorder, _dragOrigin.Y);
                    }
                    if (_dragSourceMission != null)
                        RelayoutDockablesUnderMission(_dragSourceMission);
                }
                else
                {
                    Canvas.SetLeft(cardBorder, Canvas.GetLeft(targetMission));
                    Canvas.SetTop(cardBorder, Canvas.GetTop(targetMission) + DockSlotOffsetY(0, owner));
                    RelayoutDockablesUnderMission(targetMission!);
                    UpdateHostBadge(cardBorder);
                    foreach (var dock in GetDockablesUnderMission(targetMission!))
                        UpdateHostBadge(dock);
                }
            }
            else if (okMission)
            {
                Canvas.SetLeft(cardBorder, Canvas.GetLeft(targetMission));
                Canvas.SetTop(cardBorder, Canvas.GetTop(targetMission) + DockSlotOffsetY(0, owner));
                RelayoutDockablesUnderMission(targetMission!);
                UpdateHostBadge(cardBorder);
                foreach (var dock in GetDockablesUnderMission(targetMission!))
                    UpdateHostBadge(dock);
            }
            else if (IsShipCard(card) || IsFacilityCard(card))
            {
                if (_dragOrigin.X > 0 || _dragOrigin.Y > 0)
                {
                    Canvas.SetLeft(cardBorder, _dragOrigin.X);
                    Canvas.SetTop(cardBorder, _dragOrigin.Y);
                }
                StatusText.Text = $"{card.Name} nur place at a mission.";
            }
            else if (targetMission != null)
            {
                RelayoutDockablesUnderMission(targetMission);
                UpdateHostBadge(cardBorder);
            }
        }

        _dragCard = null;
        _dragSourceMission = null;
        _isDragging = false;
    }


    private void ResetShipRangesForTurn()
    {
        _shipRangeLeft.Clear();
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Tag is Card c && IsShipCard(c))
            {
                int hull = GetHullDamage(b);
                _shipRangeLeft[b] = BattleRules.EffectiveRange(c, hull);
            }
        }
    }

    private int GetRemainingRange(Border shipBorder, Card ship)
    {
        if (_shipRangeLeft.TryGetValue(shipBorder, out int left))
            return left;
        int hull = GetHullDamage(shipBorder);
        int full = BattleRules.EffectiveRange(ship, hull);
        int junior = _attachedDilemmas.Count(a =>
            a.Kind == DilemmaRules.PersistKind.Junior && ReferenceEquals(a.Host, shipBorder));
        full = Math.Max(0, full - junior); // bereits abgezogene Züge: Countdown als Penalty-Zähler
        foreach (var j in _attachedDilemmas.Where(a =>
                     a.Kind == DilemmaRules.PersistKind.Junior && ReferenceEquals(a.Host, shipBorder)))
            full = Math.Max(0, full - Math.Max(0, j.Countdown));
        _shipRangeLeft[shipBorder] = full;
        return full;
    }

    private int GetHullDamage(Border border)
        => _hullDamagePercent.TryGetValue(border, out int h) ? h : 0;

    private bool IsBorderStopped(Border border)
        => _stoppedBorders.Contains(border);

    private List<Card> GetCrewOnShip(Border shipBorder)
    {
        // Nur Personal im Host-Stapel DIESES Schiffs (nicht Outpost!)
        var list = new List<Card>();
        if (!_stackOnHost.TryGetValue(shipBorder, out var stacked))
            return list;
        foreach (var sb in stacked)
        {
            if (sb.Tag is not Card c) continue;
            if (IsCrewType(c) || (c.Type ?? "").Contains("personnel", StringComparison.OrdinalIgnoreCase))
                list.Add(c);
        }
        return list;
    }

    private List<Card> GetOrderedMissionCards()
    {
        return _spacelineOrder
            .Where(b => b.Tag is Card c && IsMissionCard(c))
            .Select(b => (Card)b.Tag!)
            .ToList();
    }

    private static bool IsSpacelineSpanCard(Card c)
    {
        if (!EventRules.IsEvent(c)) return false;
        return EventRules.GetTargetKind(EventRules.ResolvePlay(c))
               == EventRules.TargetKind.GapBetweenMissions;
    }

    private string GetSpacelineQuadrant(Border b)
    {
        if (b.Tag is Card c && IsMissionCard(c))
            return GetNativeQuadrant(c);
        int i = _spacelineOrder.IndexOf(b);
        for (int j = i - 1; j >= 0; j--)
        {
            if (_spacelineOrder[j].Tag is Card lc && IsMissionCard(lc))
                return GetNativeQuadrant(lc);
        }
        for (int j = i + 1; j < _spacelineOrder.Count; j++)
        {
            if (_spacelineOrder[j].Tag is Card rc && IsMissionCard(rc))
                return GetNativeQuadrant(rc);
        }
        return "Alpha";
    }

    private int IndexOfMission(Border? mission)
    {
        if (mission == null) return -1;
        return _spacelineOrder.IndexOf(mission);
    }

    /// <summary>
    /// Execute: Schiff-Bewegung mit Staffing + RANGE. Bei Fehler Ursprung beibehalten.
    /// </summary>
    private bool TryMoveShipWithRules(Border shipBorder, Card ship, Border? fromMission, Border toMission)
    {
        if (_seedPhaseActive) return true;
        if (_session.Match != GameSession.MatchPhase.Play)
            return true;

        // Nur im Execute-Segment bewegen (Play = report; Draw = end of turn)
        if (_session.Segment != GameSession.TurnSegment.Execute)
        {
            if (_session.Segment == GameSession.TurnSegment.Play)
            {
                StatusText.Text = "Schiff bewegen erst im Execute-Segment (Play fertig → Execute).";
                return false;
            }
        }

        int owner = GetBorderOwner(shipBorder);
        if (owner == 0) owner = _activePlayer;
        if (owner != _activePlayer)
        {
            StatusText.Text = "Nur eigene Schiffe bewegen.";
            return false;
        }

        var crew = GetCrewOnShip(shipBorder);
        var staff = MovementRules.IsShipStaffed(ship, crew, GetActiveTreaties(_activePlayer));
        if (!staff.Ok && !ShipStaffedByRogueBorg(shipBorder))
        {
            string names = crew.Count == 0
                ? "(0 cards on ship stack – crew is not on this ship. "
                  + "In Execute, drag personnel from the outpost stack onto the ship.)"
                : string.Join(", ", crew.Select(c =>
                    $"{c.Name}[{c.Icons ?? "?"}]"));
            ShowPlayError(staff.Reason + " · Crew: " + names);
            return false;
        }

        int fromIdx = IndexOfMission(fromMission);
        int toIdx = IndexOfMission(toMission);
        if (fromIdx < 0 || toIdx < 0)
        {
            StatusText.Text = "Mission nicht auf der Spaceline.";
            return false;
        }

        int remain = GetRemainingRange(shipBorder, ship);
        var missions = GetOrderedMissionCards();

        var block = CheckEventMovement(shipBorder, ship, fromIdx, toIdx, crew);
        if (block != null)
        {
            StatusText.Text = block;
            return false;
        }

        // Where No One Has Gone Before: Enden als benachbart
        bool endsHop = HasTableEvent("Where No One Has Gone Before")
                       && missions.Count >= 2
                       && ((fromIdx == 0 && toIdx == missions.Count - 1)
                           || (toIdx == 0 && fromIdx == missions.Count - 1));

        int mover = _activePlayer;
        bool SpanForOwner(int missionIndex)
        {
            if (missionIndex < 0 || missionIndex >= _spacelineOrder.Count) return true;
            int mo = GetBorderOwner(_spacelineOrder[missionIndex]);
            return mo == 0 || mo == mover;
        }

        MovementRules.MoveResult move;
        if (endsHop)
        {
            int cost = MovementRules.GetMissionSpan(missions[toIdx], SpanForOwner(toIdx));
            if (cost > remain)
            {
                StatusText.Text = "Where No One Has Gone Before: RANGE reicht nicht.";
                return false;
            }
            move = new MovementRules.MoveResult(true, "Ends adjacent (WNOHGB).", cost, remain - cost);
        }
        else
        {
            move = MovementRules.CanMoveShip(ship, crew, remain, missions, fromIdx, toIdx,
                GetActiveTreaties(_activePlayer), SpanForOwner);
        }
        if (!move.Ok)
        {
            StatusText.Text = move.Reason;
            return false;
        }

        ApplyEventAfterMove(shipBorder, ship, fromIdx, toIdx, crew);

        _shipRangeLeft[shipBorder] = move.RangeLeft;
        StatusText.Text =
            $"{ship.Name} → {((Card)toMission.Tag!).Name} · −{move.RangeCost} RANGE " +
            $"(noch {move.RangeLeft}/{MovementRules.GetShipRange(ship)}) · {staff.Reason}";
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"{ship.Name} moved, cost {move.RangeCost}, left {move.RangeLeft}");
        return true;
    }

    private static bool IsDockableUnderMission(Card c)
    {
        string st = (c.Type ?? "").ToLowerInvariant();
        return st.Contains("ship")
               || st.Contains("facility")
               || st.Contains("outpost")
               || st.Contains("headquarters")
               || st.Contains("station");
    }

    private static bool IsHostCard(Card c)
    {
        // Alles, worauf Crew/Equipment liegen kann
        string st = (c.Type ?? "").ToLowerInvariant();
        return st.Contains("ship")
               || st.Contains("facility")
               || st.Contains("outpost")
               || st.Contains("headquarters")
               || st.Contains("station")
               || st.Contains("mission");
    }

    private bool IsStackableCard(Card c)
    {
        // Karten, die "auf" einem Host liegen und digital im Stapel verschwinden.
        // Artifacts in der Seed-Phase → unter Mission (IsSeedableUnderMission), nicht Host-Crew-Stapel.
        string st = (c.Type ?? "").ToLowerInvariant();
        if (st.Contains("artifact") && _seedPhaseActive)
            return false;
        return st.Contains("personnel")
               || st.Contains("equipment")
               || st.Contains("animal")
               || st.Contains("android")
               || st.Contains("artifact");
    }

    private static bool IsShipCard(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("ship");
    }

    private static bool IsFacilityCard(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("facility") || t.Contains("outpost")
               || t.Contains("headquarters") || t.Contains("station");
    }

    private static bool IsCrewType(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("personnel") || t.Contains("animal") || t.Contains("android");
    }

    private static bool IsEquipmentType(Card c)
    {
        string t = (c.Type ?? "").ToLowerInvariant();
        return t.Contains("equipment");
    }

    /// <summary>Personnel, Equipment, acquired Artifacts; Rogue Borg only with Lore Returns (handled via host).</summary>
    private static bool IsBeamableCard(Card c)
    {
        if (c == null) return false;
        string t = (c.Type ?? "").ToLowerInvariant();
        if (t.Contains("personnel") || t.Contains("animal") || t.Contains("android")) return true;
        if (t.Contains("equipment")) return true;
        if (t.Contains("artifact")) return true;
        // Interrupt "Rogue Borg" is beamable after Lore Returns — caller must also check ownership/Lore.
        if ((c.Name ?? "").Equals("Rogue Borg", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private bool IsBeamableFromHost(Card c, Border host, Border cardBorder)
    {
        if (IsRogueBorgCard(c))
        {
            if (!ShipHasLoreReturns(host)) return false;
            int o = GetBorderOwner(cardBorder);
            if (o == 0) o = CardOwner(cardBorder);
            var unit = _rogueBorg.FirstOrDefault(r => ReferenceEquals(r.Visual, cardBorder)
                                                      || ReferenceEquals(r.Card, c) && ReferenceEquals(r.Host, host));
            if (unit != null && unit.Controller != 0)
                o = unit.Controller;
            return o == _activePlayer;
        }
        if (!IsBeamableCard(c)) return false;
        return CardOwner(cardBorder) == _activePlayer || GetBorderOwner(cardBorder) == _activePlayer;
    }

    // ---------- Snap-Vorschau + Host-Umrandung ----------

    private void UpdateSnapPreview(Border dragging)
    {
        if (dragging.Tag is not Card card)
        {
            HideSnapPreview();
            return;
        }

        // Position: Canvas-Koordinaten (auch wenn gerade Overlay-Drag – Aufrufer rechnet um)
        double left = Canvas.GetLeft(dragging);
        double top = Canvas.GetTop(dragging);
        if (double.IsNaN(left) || double.IsNaN(top))
        {
            HideSnapPreview();
            return;
        }

        double cx = left + (dragging.Width > 0 ? dragging.Width : TableCardWidth) / 2.0;
        double cy = top + (dragging.Height > 0 ? dragging.Height : TableCardHeight) / 2.0;

        Border? target = null;
        double previewLeft = 0, previewTop = 0;
        bool showSlotPreview = false;

        if (EventRules.IsEvent(card) && !_seedPhaseActive)
        {
            var tk = EventRules.GetTargetKind(EventRules.ResolvePlay(card));
            if (EventRules.NeedsTableHost(tk))
            {
                int owner = GetBorderOwner(dragging);
                if (owner == 0) owner = _activePlayer;
                var snapped = TrySnapEventTargetAt(cx, cy, tk, owner, card);
                if (snapped.host == null)
                {
                    HideSnapPreview();
                    return;
                }

                // Gap events: only the BETWEEN slot — never a mission host highlight
                if (tk == EventRules.TargetKind.GapBetweenMissions)
                {
                    if (snapped.host2 == null)
                    {
                        HideSnapPreview();
                        return;
                    }
                    if (_hostHighlight != null)
                        _hostHighlight.Visibility = Visibility.Collapsed;
                    double lx = Canvas.GetLeft(snapped.host) + TableCardWidth;
                    double rx = Canvas.GetLeft(snapped.host2);
                    previewLeft = (lx + rx) / 2.0 - TableCardWidth / 2.0;
                    previewTop = SpacelineY;
                    EnsureSnapPreview();
                    Canvas.SetLeft(_snapPreview!, previewLeft);
                    Canvas.SetTop(_snapPreview!, previewTop);
                    _snapPreview!.Visibility = Visibility.Visible;
                    Panel.SetZIndex(_snapPreview!, 500);
                    return;
                }

                target = snapped.host;
                previewLeft = Canvas.GetLeft(target);
                previewTop = Canvas.GetTop(target);
                // jump to common preview draw by wrapping: we fall through after setting
                goto DrawEventSnap;
            }
        }

        if (IsHandReportDrag(card) && (IsStackableCard(card) || IsShipCard(card)))
        {
            int owner = GetBorderOwner(dragging);
            if (owner == 0) owner = _activePlayer;
            target = FindNearestLegalReportHost(cx, cy, owner, card, out double rdist);
            if (target == null || rdist > ShipSnapRange * 1.6)
            {
                HideSnapPreview();
                return;
            }
            previewLeft = Canvas.GetLeft(target);
            previewTop = Canvas.GetTop(target);
            goto DrawEventSnap;
        }

        if (IsStackableCard(card) || (IsSeedableUnderMission(card) && (_seedPhaseActive || _devPlaySeedFromHand)))
        {
            // Personnel/Equipment → Host; Dilemma/Artifact in Seed (or Dev) → Mission
            if (IsSeedableUnderMission(card) && (_seedPhaseActive || _devPlaySeedFromHand))
            {
                target = FindNearestLegalSeedMission(card, cx, cy, out double dist);
                if (target == null || dist > ShipSnapRange)
                {
                    HideSnapPreview();
                    return;
                }
                previewLeft = Canvas.GetLeft(target);
                previewTop = Canvas.GetTop(target);
            }
            else
            {
                int owner = GetBorderOwner(dragging);
                if (owner == 0) owner = _activePlayer;
                target = FindNearestHost(cx, cy, owner, out double dist);
                if (target == null || dist > ShipSnapRange)
                {
                    HideSnapPreview();
                    return;
                }
                previewLeft = Canvas.GetLeft(target);
                previewTop = Canvas.GetTop(target);
            }
        }
        else if (IsShipCard(card) || IsFacilityCard(card))
        {
            // Schiffe & Facilities nur an Missionen – nie auf fremde Outposts/Schiffe
            double dist;
            if (IsFacilityCard(card))
                target = FindNearestLegalFacilityMission(card, dragging, out dist);
            else
                target = FindNearestMission(cx, cy, out dist);

            if (target == null || dist > ShipSnapRange)
            {
                HideSnapPreview();
                return;
            }
            // Ziel muss Mission sein
            if (target.Tag is not Card tc || !string.Equals(tc.Type, "Mission", StringComparison.OrdinalIgnoreCase))
            {
                HideSnapPreview();
                return;
            }
            int owner = GetBorderOwner(dragging);
            if (owner == 0) owner = _activePlayer;
            int index = CountDockablesForOwner(target, owner, exclude: dragging);
            previewLeft = Canvas.GetLeft(target);
            previewTop = Canvas.GetTop(target) + DockSlotOffsetY(index, owner);
            showSlotPreview = true;
        }
        else if (IsDockableUnderMission(card))
        {
            // Fallback (sollte kaum greifen)
            target = FindNearestMission(cx, cy, out double dist);
            if (target == null || dist > ShipSnapRange)
            {
                HideSnapPreview();
                return;
            }
            int owner = GetBorderOwner(dragging);
            if (owner == 0) owner = _activePlayer;
            int index = CountDockablesForOwner(target, owner, exclude: dragging);
            previewLeft = Canvas.GetLeft(target);
            previewTop = Canvas.GetTop(target) + DockSlotOffsetY(index, owner);
            showSlotPreview = true;
        }
        else
        {
            HideSnapPreview();
            return;
        }

    DrawEventSnap:
        // 1) Kräftige Umrandung um das Ziel (Host / Mission)
        ShowHostHighlight(target);

        // 2) Zusätzlich gestrichelter Slot (bei Schiff unter Mission = Landeplatz)
        EnsureSnapPreview();
        if (showSlotPreview)
        {
            Canvas.SetLeft(_snapPreview!, previewLeft);
            Canvas.SetTop(_snapPreview!, previewTop);
            _snapPreview!.Visibility = Visibility.Visible;
            Panel.SetZIndex(_snapPreview!, 500);
        }
        else
        {
            // Bei Host-Stapel reicht die Host-Umrandung
            _snapPreview!.Visibility = Visibility.Collapsed;
        }
    }

    private void ShowHostHighlight(Border host)
    {
        EnsureHostHighlight();
        double pad = 6;
        Canvas.SetLeft(_hostHighlight!, Canvas.GetLeft(host) - pad);
        Canvas.SetTop(_hostHighlight!, Canvas.GetTop(host) - pad);
        _hostHighlight!.Width = (host.Width > 0 ? host.Width : TableCardWidth) + pad * 2;
        _hostHighlight!.Height = (host.Height > 0 ? host.Height : TableCardHeight) + pad * 2
                                 + BadgeAreaHeight; // inkl. Badge-Bereich
        Panel.SetZIndex(_hostHighlight!, 450);
        _hostHighlight.Visibility = Visibility.Visible;
    }

    private void EnsureHostHighlight()
    {
        if (_hostHighlight != null)
        {
            if (!TableCanvas.Children.Contains(_hostHighlight))
                TableCanvas.Children.Add(_hostHighlight);
            return;
        }
        _hostHighlight = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 200, 255)),
            BorderThickness = new Thickness(3),
            Background = new SolidColorBrush(Color.FromArgb(35, 0, 180, 255)),
            CornerRadius = new CornerRadius(6),
            IsHitTestVisible = false
        };
        TableCanvas.Children.Add(_hostHighlight);
    }

    private void EnsureSnapPreview()
    {
        if (_snapPreview != null)
        {
            if (!TableCanvas.Children.Contains(_snapPreview))
                TableCanvas.Children.Add(_snapPreview);
            return;
        }
        _snapPreview = new Rectangle
        {
            Width = TableCardWidth,
            Height = TableCardHeight,
            Stroke = new SolidColorBrush(Color.FromRgb(80, 180, 255)),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(40, 80, 180, 255)),
            IsHitTestVisible = false
        };
        TableCanvas.Children.Add(_snapPreview);
    }

    private void HideSnapPreview()
    {
        if (_snapPreview != null)
            _snapPreview.Visibility = Visibility.Collapsed;
        if (_originPreview != null)
            _originPreview.Visibility = Visibility.Collapsed;
        if (_hostHighlight != null)
            _hostHighlight.Visibility = Visibility.Collapsed;
        ClearZoneHighlight();
    }

    private void ShowOriginPreview(Point origin)
    {
        if (_originPreview == null)
        {
            _originPreview = new Rectangle
            {
                Width = TableCardWidth,
                Height = TableCardHeight,
                Stroke = new SolidColorBrush(Color.FromRgb(180, 180, 80)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(35, 180, 180, 80)),
                IsHitTestVisible = false
            };
            TableCanvas.Children.Add(_originPreview);
        }

        Canvas.SetLeft(_originPreview, origin.X);
        Canvas.SetTop(_originPreview, origin.Y);
        Panel.SetZIndex(_originPreview, 400);
        _originPreview.Visibility = Visibility.Visible;
    }

    private int CountDockablesUnderMission(Border mission, Border? exclude)
    {
        return GetDockablesUnderMission(mission, exclude).Count;
    }

    private List<Border> GetDockablesUnderMission(Border mission, Border? exclude = null)
    {
        double missionLeft = Canvas.GetLeft(mission);
        double missionTop = Canvas.GetTop(mission);
        // Spalte der Mission: P1 unterhalb, P2 oberhalb – beides erfassen
        double yMin = missionTop - UnderMissionGap * 8;
        double yMax = missionTop + UnderMissionGap * 8;

        return TableCanvas.Children.OfType<Border>()
            .Where(b => b != exclude && b != mission && b.Visibility == Visibility.Visible
                        && b.Tag is Card sc && IsDockableUnderMission(sc))
            .Where(b =>
            {
                double bx = Canvas.GetLeft(b);
                double by = Canvas.GetTop(b);
                // Gleiche Spalte, nicht die Mission selbst
                if (Math.Abs(bx - missionLeft) >= 45) return false;
                if (by >= yMin && by <= yMax && Math.Abs(by - missionTop) > 20)
                    return true;
                return false;
            })
            .OrderBy(b => Canvas.GetTop(b))
            .ToList();
    }

    // ---------- Host-Stapel (digital) ----------

    private Border? FindNearestHost(double centerX, double centerY, out double distance) =>
        FindNearestHost(centerX, centerY, _activePlayer, out distance);



    private Border? FindNearestOwnedFacility(Border shipBorder, int owner, Card reportingShip)
    {
        double w = shipBorder.Width > 0 ? shipBorder.Width : TableCardWidth;
        double h = shipBorder.Height > 0 ? shipBorder.Height : TableCardHeight;
        double cx = Canvas.GetLeft(shipBorder) + w / 2.0;
        double cy = Canvas.GetTop(shipBorder) + h / 2.0;

        Border? best = null;
        double bestDist = double.MaxValue;
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Visibility != Visibility.Visible || b.Tag is not Card hc) continue;
            if (!ReportingRules.IsFacilityHost(hc)) continue;
            int ho = GetBorderOwner(b);
            if (ho == 0) ho = 1;
            var check = ReportingRules.CanReportTo(
                reportingShip, hc, ho, owner,
                specialReporting: false, treatyAllowsMix: false, allowReportToShip: false,
                treaties: GetActiveTreaties(owner));
            if (!check.Ok) continue;

            double bw = b.Width > 0 ? b.Width : TableCardWidth;
            double bh = b.Height > 0 ? b.Height : TableCardHeight;
            double bx = Canvas.GetLeft(b) + bw / 2.0;
            double by = Canvas.GetTop(b) + bh / 2.0;
            double dist = Math.Sqrt((cx - bx) * (cx - bx) + (cy - by) * (cy - by));
            if (dist < bestDist)
            {
                bestDist = dist;
                best = b;
            }
        }
        return best; // nächste legale Facility (auch wenn weiter weg)
    }

    private Border? FindMissionForDockable(Border dockable)
    {
        double left = Canvas.GetLeft(dockable);
        foreach (var m in TableCanvas.Children.OfType<Border>())
        {
            if (m.Tag is not Card c || !string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase))
                continue;
            if (Math.Abs(Canvas.GetLeft(m) - left) < 45)
                return m;
        }
        return null;
    }

    private Border? TrySnapToHost(Border cardBorder, int? requiredOwner = null, bool reportTargetsOnly = false)
    {
        double w = cardBorder.Width > 0 ? cardBorder.Width : TableCardWidth;
        double h = cardBorder.Height > 0 ? cardBorder.Height : TableCardHeight;
        double cx = Canvas.GetLeft(cardBorder) + w / 2.0;
        double cy = Canvas.GetTop(cardBorder) + h / 2.0;
        int owner = requiredOwner ?? GetBorderOwner(cardBorder);
        if (owner == 0) owner = _activePlayer;
        // Großzügigerer Radius beim Report an Facility
        double maxDist = reportTargetsOnly ? ShipSnapRange * 2.5 : ShipSnapRange;
        var host = FindNearestHost(cx, cy, owner, out double dist, reportTargetsOnly);
        if (host == null || dist > maxDist)
            return null;
        return host;
    }

    /// <summary>
    /// Host für Crew/Eq: eigene Schiffe/Facilities oder Missionen (Away Team).
    /// Fremde Outposts/Schiffe sind tabu.
    /// </summary>
    private Border? FindNearestHost(double centerX, double centerY, int cardOwner, out double distance)
        => FindNearestHost(centerX, centerY, cardOwner, out distance, reportTargetsOnly: false);

    /// <param name="reportTargetsOnly">
    /// true = nur Facilities (Built-in Report); Missionen/Schiffe werden übersprungen.
    /// </param>
    private Border? FindNearestHost(double centerX, double centerY, int cardOwner, out double distance, bool reportTargetsOnly)
    {
        Border? nearest = null;
        distance = double.MaxValue;

        foreach (var h in TableCanvas.Children.OfType<Border>()
                     .Where(b => b.Visibility == Visibility.Visible
                                 && b != _hostHighlight
                                 && b.Tag is Card c && IsHostCard(c)))
        {
            if (h.Tag is not Card hc) continue;
            bool isMission = string.Equals(hc.Type, "Mission", StringComparison.OrdinalIgnoreCase);
            int hostOwner = GetBorderOwner(h);
            if (hostOwner == 0 && !isMission) hostOwner = 1;

            if (reportTargetsOnly)
            {
                // Built-in Report: only own compatible Outpost/HQ (not ships/missions)
                if (!ReportingRules.IsFacilityHost(hc)) continue;
                if (hostOwner != cardOwner) continue;
                if (_dragCard?.Tag is Card reporting
                    && !CanReportToHost(reporting, hc, h).ok)
                    continue;
            }
            else
            {
                // Mission: beide Spieler Away Teams; Schiff/Facility: nur gleicher Besitzer
                if (!isMission && hostOwner != cardOwner)
                    continue;
            }

            double w = h.Width > 0 ? h.Width : TableCardWidth;
            double ht = h.Height > 0 ? h.Height : TableCardHeight;
            double hx = Canvas.GetLeft(h) + w / 2.0;
            double hy = Canvas.GetTop(h) + ht / 2.0;
            double dx = centerX - hx;
            double dy = centerY - hy;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            bool isFac = ReportingRules.IsFacilityHost(hc);
            bool isShip = ReportingRules.IsShipHost(hc);
            // Report: Facility bevorzugen. Boarding/Execute: Schiff bevorzugen, Facility nicht bevorzugen.
            double score = dist;
            if (isMission) score += 80.0;
            if (reportTargetsOnly)
                score += isFac ? -25.0 : 0.0;
            else
                score += isShip ? -20.0 : (isFac ? 10.0 : 0.0);
            if (score < distance)
            {
                distance = score;
                nearest = h;
            }
        }
        return nearest;
    }

    private void AddCardToHostStack(Border host, Border cardBorder)
    {
        if (!_stackOnHost.TryGetValue(host, out var list))
        {
            list = new List<Border>();
            _stackOnHost[host] = list;
        }

        if (!list.Contains(cardBorder))
            list.Add(cardBorder);

        // Owner festhalten (wichtig für versteckte Stapel-Karten)
        if (!_borderOwner.TryGetValue(cardBorder, out int co) || co == 0)
        {
            int hostOwner = GetBorderOwner(host);
            if (hostOwner == 0
                && host.Tag is Card hc
                && string.Equals(hc.Type, "Mission", StringComparison.OrdinalIgnoreCase))
                hostOwner = _activePlayer;
            if (hostOwner == 0) hostOwner = _activePlayer;
            _borderOwner[cardBorder] = hostOwner;
        }

        // Vom Spielfeld "verschwinden"
        cardBorder.Visibility = Visibility.Collapsed;

        if (host.Tag is Card hostCard && cardBorder.Tag is Card card)
        {
            StatusText.Text = $"{card.Name} → {hostCard.Name}";
            int o = GetBorderOwner(cardBorder);
            if (o == 0) o = _activePlayer;
            _session.Log.Add(_session.TurnNumber, $"P{o}",
                $"{card.Name} → on {hostCard.Name} [{hostCard.Type}]");
        }

        UpdateHostBadge(host);
    }

    private void RemoveCardFromHostStack(Border host, Border cardBorder)
    {
        if (_stackOnHost.TryGetValue(host, out var list))
        {
            list.Remove(cardBorder);
            if (list.Count == 0)
                _stackOnHost.Remove(host);
        }
        cardBorder.Visibility = Visibility.Visible;
        if (host.Tag is Card hostCard && cardBorder.Tag is Card card)
        {
            int o = GetBorderOwner(cardBorder);
            if (o == 0) o = _activePlayer;
            _session.Log.Add(_session.TurnNumber, $"P{o}",
                $"{card.Name} ← left {hostCard.Name}");
        }
        UpdateHostBadge(host);
    }

    private void UpdateHostBadge(Border host)
    {
        if (host.Tag is not Card hostCard)
            return;

        bool isMission = string.Equals(hostCard.Type, "Mission", StringComparison.OrdinalIgnoreCase);

        int crew1 = 0, equip1 = 0, art1 = 0, other1 = 0;
        int crew2 = 0, equip2 = 0, art2 = 0, other2 = 0;
        if (_stackOnHost.TryGetValue(host, out var list))
        {
            foreach (var b in list)
            {
                if (b.Tag is not Card c) continue;
                int o = GetBorderOwner(b);
                if (o == 0) o = 1;
                if (o == 1)
                {
                    if (IsCrewType(c)) crew1++;
                    else if (IsEquipmentType(c)) equip1++;
                    else if ((c.Type ?? "").Contains("artifact", StringComparison.OrdinalIgnoreCase)) art1++;
                    else other1++;
                }
                else
                {
                    if (IsCrewType(c)) crew2++;
                    else if (IsEquipmentType(c)) equip2++;
                    else if ((c.Type ?? "").Contains("artifact", StringComparison.OrdinalIgnoreCase)) art2++;
                    else other2++;
                }
            }
        }

        string LabelFor(int crew, int equip, int art, int other, int player)
        {
            var parts = new List<string>();
            if (isMission)
            {
                if (crew > 0) parts.Add($"Away P{player} {crew}");
                if (equip > 0) parts.Add($"Eq P{player} {equip}");
                if (art > 0) parts.Add($"Art P{player} {art}");
                if (other > 0) parts.Add($"+{other}");
            }
            else
            {
                if (crew > 0) parts.Add($"Crew {crew}");
                if (equip > 0) parts.Add($"Eq {equip}");
                if (art > 0) parts.Add($"Art {art}");
                if (other > 0) parts.Add($"+{other}");
            }
            return parts.Count > 0 ? string.Join(" · ", parts) : "";
        }

        string text1 = LabelFor(crew1, equip1, art1, other1, 1);
        string text2 = LabelFor(crew2, equip2, art2, other2, 2);

        int rbCount = CountRogueBorgOn(host);
        if (rbCount > 0)
        {
            string rbBadge = $"RB×{rbCount} STR{RogueBorgStrengthOn(host)}";
            if (HostHasCrosis(host)) rbBadge += " C×2";
            if (ShipHasLoreReturns(host)) rbBadge += " Lore";
            // Show on active controller side if Lore, else P1 badge as neutral notice
            var units = RogueBorgUnitsOn(host).ToList();
            int ctrl = units.Select(u => u.Controller).FirstOrDefault(c => c != 0);
            if (ctrl == 2)
                text2 = string.IsNullOrEmpty(text2) ? rbBadge : text2 + " · " + rbBadge;
            else
                text1 = string.IsNullOrEmpty(text1) ? rbBadge : text1 + " · " + rbBadge;
        }

        // Badge P1: unter der Karte
        EnsureSideBadge(host, playerSide: 1, text1, isMission);
        // Badge P2: bei Mission über der Karte, sonst unter Crew-Badge
        EnsureSideBadge(host, playerSide: 2, text2, isMission);

        if (_selectedCard == host)
            UpdateSelectionFrame(host);
    }

    private void EnsureSideBadge(Border host, int playerSide, string text, bool isMission)
    {
        // Key-Trick: zweites Dictionary für P2
        var dict = playerSide == 1 ? _hostBadges : _hostBadgesP2;
        if (!dict.TryGetValue(host, out var badge))
        {
            badge = new TextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(playerSide == 1
                    ? Color.FromRgb(120, 200, 255)
                    : Color.FromRgb(255, 180, 120)),
                Background = new SolidColorBrush(Color.FromArgb(180, 20, 20, 20)),
                Padding = new Thickness(4, 1, 4, 1),
                IsHitTestVisible = false
            };
            dict[host] = badge;
            TableCanvas.Children.Add(badge);
        }

        badge.Text = text;
        badge.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;

        double left = Canvas.GetLeft(host);
        double hostTop = Canvas.GetTop(host);
        double top;
        int hostOwner = GetBorderOwner(host);
        if (hostOwner == 0) hostOwner = 1;

        if (isMission)
        {
            // P1 unter Mission, P2 über Mission – gleicher Abstand zur Kante
            if (playerSide == 2)
                top = hostTop - BadgeAreaHeight - 2;
            else
            {
                top = hostTop + TableCardHeight + 3;
                if (_seedUnderMission.TryGetValue(host, out var seeds) && seeds.Count > 0)
                    top += BadgeAreaHeight;
            }
        }
        else
        {
            // Schiff/Facility: Badge direkt an der Karte (gleicher Gap wie P1)
            // P2-Hosts stehen über der Spaceline → Badge knapp unter der Karte (Richtung Mission)
            // P1-Hosts unter der Spaceline → Badge knapp unter der Karte
            top = hostTop + TableCardHeight + 3;

            // Zwei Badges nur stapeln, wenn beide Seiten Text haben
            if (playerSide == 2)
            {
                string otherText = "";
                if (_hostBadges.TryGetValue(host, out var other) && other.Visibility == Visibility.Visible)
                    otherText = other.Text ?? "";
                if (!string.IsNullOrEmpty(otherText))
                    top += BadgeAreaHeight; // unter P1-Badge
            }
        }

        Canvas.SetLeft(badge, left);
        Canvas.SetTop(badge, top);
        Panel.SetZIndex(badge, Panel.GetZIndex(host) + 5 + playerSide);
    }

    private void AddSeedUnderMission(Border mission, Border cardBorder)
    {
        if (mission.Tag is Card mc && cardBorder.Tag is Card sc)
        {
            var (ok, reason) = CanSeedCardUnderMission(sc, mc);
            if (!ok)
            {
                StatusText.Text = reason;
                cardBorder.Visibility = Visibility.Visible;
                return;
            }
        }

        if (!_seedUnderMission.TryGetValue(mission, out var list))
        {
            list = new List<Border>();
            _seedUnderMission[mission] = list;
        }
        if (!list.Contains(cardBorder))
            list.Add(cardBorder);
        cardBorder.Visibility = Visibility.Collapsed;
        UpdateSeedBadge(mission);
        if (mission.Tag is Card m2 && cardBorder.Tag is Card s2)
            StatusText.Text = $"{s2.Name} unter {m2.Name} geseedet.";
    }

    private void UpdateSeedBadge(Border mission)
    {
        int count = _seedUnderMission.TryGetValue(mission, out var list) ? list.Count : 0;

        if (!_seedBadges.TryGetValue(mission, out var badge))
        {
            badge = new TextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 180, 100)),
                Background = new SolidColorBrush(Color.FromArgb(200, 30, 25, 10)),
                Padding = new Thickness(4, 1, 4, 1),
                IsHitTestVisible = false
            };
            _seedBadges[mission] = badge;
            TableCanvas.Children.Add(badge);
        }

        // Dev: full count. Player aid: only own seed cards. Seed phase: full for placement feedback.
        int ownCount = 0;
        if (list != null)
        {
            foreach (var b in list)
            {
                int o = GetBorderOwner(b);
                if (o == 0 || o == _activePlayer) ownCount++;
            }
        }
        bool showAll = _devShowSeedCounts || _seedPhaseActive;
        bool showOwn = _aidOwnSeedCounts && ownCount > 0;
        bool show = count > 0 && (showAll || showOwn);
        if (showAll)
            badge.Text = $"{count}";
        else if (showOwn)
            badge.Text = $"{ownCount}";
        else
            badge.Text = "";
        badge.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        Canvas.SetLeft(badge, Canvas.GetLeft(mission) + 8);
        Canvas.SetTop(badge, Canvas.GetTop(mission) + TableCardHeight + 3);
        Panel.SetZIndex(badge, 18);
    }

    /// <summary>
    /// Host click → crew / away team / seed in the owner's hand strip (TABLE stays on the right).
    /// </summary>
    private void ShowHostContents(Border hostBorder, Card hostCard, bool beamSelectMode = false)
    {
        if (_inspectorMode == InspectorMode.OpponentPileInteract)
        {
            StatusText.Text = "Finish the pile interaction first.";
            return;
        }

        UpdateHostBadge(hostBorder);
        if (!beamSelectMode)
            return;

        // Beam selection lives in the detail popup so both hand strips stay Hand.
        _hostStripHost = hostBorder;
        _hostStripBeam = true;
        _detailHost = hostBorder;
        ShowCardDetail(hostCard);
        OpenCardDetailPopup();
        StatusText.Text =
            $"BEAM {hostCard.Name}: check cards in the detail window, then click a highlighted destination.";
    }

    /// <summary>
    /// Personnel / equipment can leave a host (beam / report). Events, dilemmas, artifacts
    /// stay where they were played unless another card explicitly relocates them.
    /// </summary>
    private bool CanDragOffHost(Card card)
    {
        if (_seedPhaseActive) return true;
        if (ModifierRules.IsPersonnelCard(card) || ModifierRules.IsEquipmentCard(card))
            return true;
        if (IsShipCard(card) || IsFacilityCard(card))
            return true;
        return false;
    }

    /// <summary>
    /// Mini-Karte im Stapel: Klick = Detail, Ziehen = aufs Spielfeld / anderen Host.
    /// </summary>
    private void MiniCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border mini || mini.Tag is not HostCardRef href)
            return;

        // Events / dilemmas / artifacts: detail only (drag handled by WireHostStripMini)
        if (!CanDragOffHost(href.Card))
        {
            e.Handled = true;
            return;
        }

        _panelDragRef = href;
        _panelDragging = false;
        _mouseDownScreen = e.GetPosition(this);
        mini.CaptureMouse();
        mini.MouseMove += MiniCard_MouseMove;
        mini.MouseLeftButtonUp += MiniCard_MouseUp;
        e.Handled = true;
    }

    private void MiniCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (_panelDragRef == null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var pos = e.GetPosition(this);
        double dx = pos.X - _mouseDownScreen.X;
        double dy = pos.Y - _mouseDownScreen.Y;

        if (!_panelDragging)
        {
            if (dx * dx + dy * dy < DragThreshold * DragThreshold)
                return;

            _panelDragging = true;
            // Karte aus Host-Stapel sichtbar machen und über dem Canvas bewegen
            var cb = _panelDragRef.CardBorder;
            RemoveCardFromHostStack(_panelDragRef.Host, cb);
            cb.Visibility = Visibility.Visible;
            Panel.SetZIndex(cb, 2000);

            // Mausposition auf Canvas
            var canvasPos = e.GetPosition(TableCanvas);
            Canvas.SetLeft(cb, canvasPos.X - TableCardWidth / 2);
            Canvas.SetTop(cb, canvasPos.Y - TableCardHeight / 2);
            _dragCard = cb;
            _dragOffset = new Point(TableCardWidth / 2, TableCardHeight / 2);
            _isDragging = true;
            ShowOriginPreview(new Point(Canvas.GetLeft(_panelDragRef.Host), Canvas.GetTop(_panelDragRef.Host)));
        }
        else if (_dragCard != null)
        {
            var winPos = e.GetPosition(this);
            if (UpdateDiscardSnap(winPos))
            {
                // snapt auf Discard
            }
            else
            {
                RestoreFloatingCardSize();
                var canvasPos = e.GetPosition(TableCanvas);
                Canvas.SetLeft(_dragCard, canvasPos.X - _dragOffset.X);
                Canvas.SetTop(_dragCard, canvasPos.Y - _dragOffset.Y);
                UpdateSnapPreview(_dragCard);
            }
        }
    }

    private void MiniCard_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border mini)
        {
            mini.ReleaseMouseCapture();
            mini.MouseMove -= MiniCard_MouseMove;
            mini.MouseLeftButtonUp -= MiniCard_MouseUp;
        }

        if (_panelDragRef == null)
            return;

        var href = _panelDragRef;
        _panelDragRef = null;

        if (!_panelDragging)
        {
            // Nur Klick → Detail bereits gezeigt, Stapel unverändert
            return;
        }

        HideSnapPreview();
        var cardBorder = href.CardBorder;
        var card = href.Card;

        // Auf neuen Host snappen oder Discard?
        var windowPos = e.GetPosition(this);
        if (TryPlaceOnDiscard(card, cardBorder, windowPos))
        {
            // Karte war aus Host → aus Canvas entfernen bereits in TryPlaceOnDiscard
            StatusText.Text = $"Discard: {card.Name}";
        }
        else if (IsStackableCard(card))
        {
            var host = TrySnapToHost(cardBorder);
            if (host != null)
            {
                AddCardToHostStack(host, cardBorder);
                SetSelection(host);
                if (host.Tag is Card hc)
                    ShowHostContents(host, hc);
            }
            else
            {
                // Zurück auf ursprünglichen Host
                AddCardToHostStack(href.Host, cardBorder);
                SetSelection(href.Host);
                if (href.Host.Tag is Card oh)
                    ShowHostContents(href.Host, oh);
                StatusText.Text = $"{card.Name} stays on host (no valid target).";
            }
        }

        Panel.SetZIndex(cardBorder, 30);
        _dragCard = null;
        _isDragging = false;
        _panelDragging = false;

        // Alten Host-Inhalt aktualisieren falls noch offen
        if (href.Host.Tag is Card oldHostCard)
            ShowHostContents(href.Host, oldHostCard);
    }

    private void SetSelection(Border cardBorder)
    {
        _selectedCard = cardBorder;
        UpdateSelectionFrame(cardBorder);
        // Aktionsleiste nur bei frischer Auswahl, nicht während Beam-Zielwahl neu bauen
        if (_cardActionMode == CardActionMode.None)
            RefreshCardActionPanel(cardBorder);
    }


    private void ClearSelectionAndDetail()
    {
        ClearCardActionUi();
        _selectedCard = null;
        if (_selectionFrame != null)
            _selectionFrame.Visibility = Visibility.Collapsed;
        ClearDetailPanel();
        ClearHostStackPanel();
    }

    /// <summary>
    /// Bei Phasen- oder Spielerwechsel: Auswahl weg, Aktionsleiste weg, Hand anzeigen.
    /// </summary>
    private void OnTurnContextChanged(string? statusNote = null)
    {
        ClearSelectionAndDetail();
        if (!_seedPhaseActive && _session.Match == GameSession.MatchPhase.Play)
            ShowActivePlayerHand();
        else if (_seedPhaseActive)
            ShowCurrentSeedStack();
        if (!string.IsNullOrEmpty(statusNote))
            StatusText.Text = statusNote;
    }


    private void ClearDetailPanel()
    {
        _detailCard = null;
        DetailName.Text = "–";
        DetailType.Text = "";
        DetailText.Text = "";
        DetailAttributes.Text = "";
        DetailClass.Text = "";
        DetailStaff.Text = "";
        DetailIcons.Text = "";
        try { DetailImage.Source = null; } catch { }
    }

    private void ClearHostStackPanel()
    {
        if (StackTitle != null) StackTitle.Text = "Stack";
        if (StackGrid != null) StackGrid.Children.Clear();
        if (_cardActionMode == CardActionMode.BeamPickTarget)
            return;
        bool wasHost = _stripZoneP1 == "Host" || _stripZoneP2 == "Host";
        _hostStripHost = null;
        _hostStripHostP1 = null;
        _hostStripHostP2 = null;
        _hostStripBeam = false;
        if (_stripZoneP1 == "Host") _stripZoneP1 = "Hand";
        if (_stripZoneP2 == "Host") _stripZoneP2 = "Hand";
        if (wasHost) RefreshHandStrips();
    }

    private void TableCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Klick auf leeren Tisch (nicht auf Karte) → Auswahl weg
        if (e.OriginalSource == TableCanvas || e.Source == TableCanvas)
        {
            if (_cardActionMode != CardActionMode.None)
            {
                ClearCardActionUi();
                StatusText.Text = "Aktion abgebrochen.";
            }
            else
            {
                ClearSelectionAndDetail();
                ResetStripsToHand();
            }
            e.Handled = true;
        }
    }


    private void EnsureTurnTints()
    {
        if (_turnTintP1 != null) return;
        // P1 = green (bottom), P2 = blue (top) — matches side banner
        _turnTintP1 = new Rectangle
        {
            IsHitTestVisible = false,
            Fill = new SolidColorBrush(Color.FromArgb(55, 40, 140, 70)),
            RadiusX = 4,
            RadiusY = 4
        };
        _turnTintP2 = new Rectangle
        {
            IsHitTestVisible = false,
            Fill = new SolidColorBrush(Color.FromArgb(55, 40, 90, 170)),
            RadiusX = 4,
            RadiusY = 4
        };
        _turnWatermark = new TextBlock
        {
            IsHitTestVisible = false,
            FontSize = 96,
            FontWeight = FontWeights.Bold,
            Opacity = 0.12,
            Foreground = Brushes.White,
            Text = "PLAYER 1"
        };
        Panel.SetZIndex(_turnTintP1, 0);
        Panel.SetZIndex(_turnTintP2!, 0);
        Panel.SetZIndex(_turnWatermark, 0);
        TableCanvas.Children.Insert(0, _turnTintP1);
        TableCanvas.Children.Insert(0, _turnTintP2!);
        TableCanvas.Children.Insert(0, _turnWatermark);
    }

    private void UpdateTurnTints()
    {
        // Hotseat: no turn-side field tint and no large PLAYER watermark
        EnsureTurnTints();
        if (_turnTintP1 != null) _turnTintP1.Visibility = Visibility.Collapsed;
        if (_turnTintP2 != null) _turnTintP2.Visibility = Visibility.Collapsed;
        if (_turnWatermark != null) _turnWatermark.Visibility = Visibility.Collapsed;
        // Active player's hand face-up; inactive side card backs
        RefreshHandStrips();
    }

    private void ClearCardActionUi()
    {
        bool wasBeam = _cardActionMode == CardActionMode.BeamPickTarget;
        _cardActionMode = CardActionMode.None;
        _actionSourceHost = null;
        _beamSelected.Clear();
        ClearTargetHighlights();
        if (_actionPanel != null)
        {
            TableCanvas.Children.Remove(_actionPanel);
            _actionPanel = null;
        }
        if (wasBeam && _hostStripBeam)
        {
            _hostStripBeam = false;
            if (_hostStripHost?.Tag is Card hc)
                ShowHostContents(_hostStripHost, hc);
        }
    }

    private void ClearTargetHighlights()
    {
        foreach (var r in _targetHighlights)
            TableCanvas.Children.Remove(r);
        _targetHighlights.Clear();
    }

    private void RefreshCardActionPanel(Border? cardBorder)
    {
        ClearCardActionUi();
        if (cardBorder == null || cardBorder.Tag is not Card card) return;
        if (_seedPhaseActive) return;
        if (_session.Match != GameSession.MatchPhase.Play) return;

        bool isShip = IsShipCard(card);
        bool isFac = ReportingRules.IsFacilityHost(card);
        bool isMission = string.Equals(card.Type, "Mission", StringComparison.OrdinalIgnoreCase);

        // Missionen gehören niemandem – Owner-Check nur für Schiffe/Facilities
        if (!isMission)
        {
            int owner = GetBorderOwner(cardBorder);
            if (owner == 0) owner = 1;
            if (owner != _activePlayer) return;
        }

        // Away Team: Owner aus Dictionary (nicht Canvas-Position versteckter Karten!)
        bool missionHasMyAway = false;
        if (isMission && _stackOnHost.TryGetValue(cardBorder, out var awayList))
            missionHasMyAway = awayList.Any(b => CardOwner(b) == _activePlayer);
        if (!isShip && !isFac && !(isMission && missionHasMyAway)) return;

        _actionPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Background = new SolidColorBrush(Color.FromArgb(230, 20, 28, 40))
        };
        Panel.SetZIndex(_actionPanel, 40);

        void AddBtn(string label, RoutedEventHandler onClick)
        {
            var b = new Button
            {
                Content = label,
                Margin = new Thickness(2),
                Padding = new Thickness(6, 3, 6, 3),
                FontSize = 11,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromRgb(45, 70, 100)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 140, 200))
            };
            b.Click += onClick;
            _actionPanel.Children.Add(b);
        }

        if (_session.Segment == GameSession.TurnSegment.Execute)
        {
            if (isShip)
            {
                if (IsBorderStopped(cardBorder))
                {
                    AddBtn("(stopped – no orders)", (_, _) =>
                        StatusText.Text = "This ship is stopped until the start of your next turn.");
                }
                else
                {
                    // Contents + crew stats: double-click host (detail popup)
                    AddBtn("Beam crew…", (_, _) => BeginBeamMode(cardBorder));
                    AddBtn("Fly (highlight destinations)", (_, _) => BeginFlyHighlight(cardBorder, card));
                    AddBtn("Attack ship…", (_, _) => BeginAttackMode(cardBorder, card));
                    if (CanOfferPersonnelBattleFromShip(cardBorder))
                        AddBtn("Attack crew…", (_, _) => BeginPersonnelAttackFromHost(cardBorder));
                    if (GetHullDamage(cardBorder) > 0 && GetHullDamage(cardBorder) < 100)
                        AddBtn("Repair status", (_, _) => ShowRepairStatus(cardBorder, card));
                    AddBtn("Solvable missions?", (_, _) => HighlightSolvableMissions(GetCrewOnShip(cardBorder)));
                    if (EventsOn(cardBorder).Any(ae => ae.Kind == EventRules.Persist.PlasmaFire)
                        && EventRules.HasSkill(GetCrewOnShip(cardBorder), "SECURITY"))
                    {
                        AddBtn("Nullify Plasma Fire (SECURITY)", (_, _) =>
                            TryNullifyPlasmaFire(cardBorder));
                    }
                    if (_attachedDilemmas.Any(d => d.Kind == DilemmaRules.PersistKind.BorgShip))
                    {
                        AddBtn("Attack Borg Ship…", (_, _) =>
                            TryDestroyBorgShipInBattle(cardBorder, card));
                    }
                    var mAt = FindMissionForDockable(cardBorder);
                    if (mAt != null && mAt.Tag is Card mc
                        && MissionRules.IsSpaceMission(mc)
                        && !_solvedMissions.Contains(mAt))
                    {
                        AddBtn("Attempt mission (Space)", (_, _) => TryAttemptMission(mAt, mc));
                    }
                }
            }
            else if (isFac)
            {
                AddBtn("Beam personnel…", (_, _) => BeginBeamMode(cardBorder));
            }
            else if (isMission && missionHasMyAway)
            {
                AddBtn("Beam away team…", (_, _) => BeginBeamMode(cardBorder));
                var awayTeam = new List<Card>();
                if (_stackOnHost.TryGetValue(cardBorder, out var al))
                    foreach (var b in al)
                    {
                        if (b.Tag is not Card c) continue;
                        int o = GetBorderOwner(b); if (o == 0) o = 1;
                        if (o == _activePlayer && (IsCrewType(c) || (c.Type ?? "").Contains("personnel", StringComparison.OrdinalIgnoreCase)))
                            awayTeam.Add(c);
                    }
                if (GetPersonnelBordersAtHost(cardBorder, opponentOf: _activePlayer).Count > 0)
                    AddBtn("Attack away team…", (_, _) => BeginPersonnelAttackFromHost(cardBorder));
                AddBtn("Solvable missions?", (_, _) => HighlightSolvableMissions(awayTeam));
                if (!_solvedMissions.Contains(cardBorder))
                    AddBtn("Attempt mission", (_, _) => TryAttemptMission(cardBorder, card));
            }
        }
        else if (_session.Segment == GameSession.TurnSegment.Play
                 && isShip
                 && EventsOn(cardBorder).Any(ae => ae.Kind == EventRules.Persist.PlasmaFire)
                 && EventRules.HasSkill(GetCrewOnShip(cardBorder), "SECURITY"))
        {
            AddBtn("Nullify Plasma Fire (SECURITY)", (_, _) =>
                TryNullifyPlasmaFire(cardBorder));
        }
        // Draw phase: no action panel on hosts (orders are Execute-only)

        if (_actionPanel.Children.Count == 0)
        {
            _actionPanel = null;
            return;
        }

        Canvas.SetLeft(_actionPanel, Canvas.GetLeft(cardBorder) + TableCardWidth + 6);
        Canvas.SetTop(_actionPanel, Canvas.GetTop(cardBorder));
        TableCanvas.Children.Add(_actionPanel);
    }


    private void BeamSelect_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox cb || cb.Tag is not Border cardBorder) return;
        if (cb.IsChecked == true) _beamSelected.Add(cardBorder);
        else _beamSelected.Remove(cardBorder);
        StatusText.Text = $"BEAM: {_beamSelected.Count} card(s) selected – click destination.";
    }

    private void TryAttemptMission(Border missionBorder, Card mission)
    {
        if (_session.Segment != GameSession.TurnSegment.Execute)
        {
            ShowPlayError("Missions may only be attempted during the Execute segment.");
            return;
        }
        if (_solvedMissions.Contains(missionBorder))
        {
            ShowPlayError("This mission is already solved.");
            return;
        }
        // Track discards for Temporal Causality Loop
        _attemptMission = missionBorder;
        _attemptDiscards.Clear();
        if (_attachedDilemmas.Any(a => a.Kind == DilemmaRules.PersistKind.Scow && ReferenceEquals(a.Host, missionBorder)))
        {
            ShowPlayError("Radioactive Garbage Scow: this mission cannot be attempted (tow with Tractor Beam + 2 ENGINEER).");
            return;
        }
        if (_attachedEvents.Any(e => e.Kind == EventRules.Persist.Supernova && ReferenceEquals(e.Host, missionBorder)))
        {
            ShowPlayError("Supernova: this mission can no longer be attempted.");
            return;
        }

        var teamBorders = CollectTeamBordersAtMission(missionBorder, mission);
        var team = teamBorders
            .Where(b => b.Tag is Card)
            .Select(b => (Card)b.Tag!)
            .ToList();
        if (team.Count == 0)
        {
            ShowPlayError("No unstopped away team / crew at this mission.");
            return;
        }

        if (!MissionRules.TeamMatchesMissionAffiliation(mission, team))
        {
            string need = string.Join("/", MissionRules.ParseAffiliationTokens(mission.Affiliation));
            ShowPlayError($"Cannot attempt {mission.Name}: requires affiliation {need}.");
            return;
        }

        // Seed stack: last seeded = list end = encountered first (bottom → top).
        // Artifacts in that order are revealed face-up for both players, but only
        // acquired after the mission is solved.
        if (!_seedUnderMission.TryGetValue(missionBorder, out var seedStack))
            seedStack = new List<Border>();

        while (seedStack.Count > 0)
        {
            int idx = seedStack.Count - 1;
            var seedBorder = seedStack[idx];
            if (seedBorder.Tag is not Card seedCard)
            {
                seedStack.RemoveAt(idx);
                continue;
            }

            // Artifact in encounter order: reveal, leave under mission, continue attempt
            if (MissionRules.IsArtifact(seedCard) || ArtifactRules.IsArtifact(seedCard))
            {
                seedStack.RemoveAt(idx);
                _seedUnderMission[missionBorder] = seedStack;
                if (!_revealedArtifactsUnderMission.TryGetValue(missionBorder, out var revealed))
                {
                    revealed = new List<Card>();
                    _revealedArtifactsUnderMission[missionBorder] = revealed;
                }
                if (!revealed.Contains(seedCard))
                    revealed.Add(seedCard);
                UpdateSeedBadge(missionBorder);
                ShowCardReveal(seedCard, "Artifact revealed",
                    $"{seedCard.Name} is found under {mission.Name}.\n"
                    + "Both players can see it. It is acquired only when this mission is solved.",
                    RevealButtons.Ok, seedCard.Name);
                _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                    $"Revealed artifact {seedCard.Name} under {mission.Name}");
                continue;
            }

            if (!MissionRules.IsDilemma(seedCard))
            {
                seedStack.RemoveAt(idx);
                continue;
            }

            if (!MissionRules.DilemmaAllowedAtMission(seedCard, mission))
            {
                seedStack.RemoveAt(idx);
                _seedUnderMission[missionBorder] = seedStack;
                UpdateSeedBadge(missionBorder);
                ShowCardReveal(seedCard, "Mis-seeded dilemma",
                    $"{seedCard.Name} does not match this mission (planet/space) and is removed.",
                    RevealButtons.Ok, seedCard.Name);
                continue;
            }

            _lastEncounteredDilemma[missionBorder] = seedCard;
            ShowCardDetail(seedCard);

            var present = CollectPresentAtMission(missionBorder, mission);
            Card? ship = null;
            Border? shipBorder = null;
            if (!MissionRules.IsPlanetMission(mission))
            {
                foreach (var dock in GetDockablesUnderMission(missionBorder))
                {
                    if (dock.Tag is not Card dc || !IsShipCard(dc)) continue;
                    if (GetBorderOwner(dock) != _activePlayer) continue;
                    ship = dc;
                    shipBorder = dock;
                    break;
                }
            }

            var hand = _activePlayer == 1 ? _handCards : _oppHandCards;
            var dilResult = DilemmaRules.Resolve(new DilemmaRules.Ctx
            {
                Dilemma = seedCard,
                Mission = mission,
                Team = team,
                Present = present,
                Ship = ship,
                ShipShields = ship != null ? BattleRules.GetShields(ship) : 0,
                AttemptingPlayer = _activePlayer,
                Hand = hand.ToList(),
                PickYou = (prompt, pool) => PickCardFromList(prompt, pool, "Choose a card"),
                PickOpp = (prompt, pool) => PickCardFromList(prompt, pool, "Opponent chooses"),
                Confirm = prompt =>
                    ShowCardReveal(seedCard, seedCard.Name ?? "Dilemma", prompt,
                        RevealButtons.YesNo, "Your choice") == RevealAnswer.Yes
            });

            bool failed = dilResult.Fate is DilemmaRules.Fate.WallFailed
                or DilemmaRules.Fate.EffectAndEnd
                or DilemmaRules.Fate.AttachAndEnd
                or DilemmaRules.Fate.EndAttempt;

            string outcomeHeader = failed
                ? "FAILED — attempt ends"
                : "OVERCOME";
            string victimLine = FormatDilemmaVictims(dilResult);
            string consequences = failed
                ? (dilResult.StopTeam
                    ? "\nConsequences: team is stopped; mission attempt ends."
                    : "\nConsequences: mission attempt ends.")
                : (dilResult.StopTeam
                    ? "\nNote: team is stopped after overcoming this dilemma."
                    : "\nContinue to the next dilemma (or mission requirements).");
            if (dilResult.Score > 0)
                consequences += $"\nBonus points: +{dilResult.Score}";
            if (victimLine.Length > 0)
                consequences = "\n" + victimLine + consequences;

            string outcomeBody = (seedCard.Text ?? "") + "\n\n→ " + dilResult.Message + consequences;

            // No auto-timer — player must acknowledge with OK
            ShowCardReveal(
                seedCard,
                outcomeHeader,
                outcomeBody,
                RevealButtons.Ok,
                seedCard.Name);

            // ApplyDilemmaResult already removes overcome / attached / effect dilemmas from seedStack
            ApplyDilemmaResult(dilResult, seedCard, missionBorder, mission, teamBorders, shipBorder, seedStack);

            string logMsg = dilResult.Message + (victimLine.Length > 0 ? " " + victimLine : "");
            if (failed)
            {
                if (dilResult.StopTeam)
                    StopMissionAttemptTeam(missionBorder, mission, teamBorders);
                StatusText.Text = logMsg;
                _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                    $"Dilemma {seedCard.Name} FAILED: {logMsg}");
                if (dilResult.EndTurn)
                    FinishExecuteAndEndTurn();
                ClearCardActionUi();
                return;
            }

            // Overcome: seed already removed in ApplyDilemmaResult
            if (dilResult.Score > 0)
                AwardDilemmaPoints(dilResult.Score);
            StatusText.Text = logMsg;
            _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                $"OVERCOME {seedCard.Name}"
                + (dilResult.Score > 0 ? $" +{dilResult.Score}" : "")
                + (victimLine.Length > 0 ? " — " + victimLine : ""));

            if (dilResult.StopTeam)
            {
                StopMissionAttemptTeam(missionBorder, mission, teamBorders);
                ClearCardActionUi();
                return;
            }

            teamBorders = CollectTeamBordersAtMission(missionBorder, mission);
            team = teamBorders.Where(b => b.Tag is Card).Select(b => (Card)b.Tag!).ToList();
            if (team.Count == 0)
            {
                ShowPlayError("No personnel left in the attempt — mission ends.");
                ClearCardActionUi();
                return;
            }
        }

        // All dilemmas clear → check mission requirements, then solve, then artifacts
        int missionOwner = GetBorderOwner(missionBorder);
        var result = MissionRules.CanSolve(mission, team, dilemmasRemaining: 0,
            attemptingPlayer: _activePlayer, missionOwner: missionOwner);
        if (!result.Ok)
        {
            ShowCardReveal(mission, "Mission not solved",
                "All dilemmas are clear, but mission requirements are not met:\n" + result.Reason
                + "\n\nArtifacts remain under the mission until it is solved.",
                RevealButtons.Ok, mission.Name);
            return;
        }

        MarkMissionSolved(missionBorder, mission, _activePlayer, result.Points);
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"Solved {mission.Name} for {result.Points} points");

        // Acquire revealed artifacts + any still under the mission (only after solve)
        var toAcquire = new List<Card>();
        if (_revealedArtifactsUnderMission.TryGetValue(missionBorder, out var foundArts))
        {
            toAcquire.AddRange(foundArts);
            _revealedArtifactsUnderMission.Remove(missionBorder);
        }
        if (_seedUnderMission.TryGetValue(missionBorder, out var remaining) && remaining.Count > 0)
        {
            foreach (var sb in remaining.ToList())
            {
                if (sb.Tag is not Card ac) continue;
                if (!(MissionRules.IsArtifact(ac) || ArtifactRules.IsArtifact(ac))) continue;
                remaining.Remove(sb);
                if (!toAcquire.Contains(ac)) toAcquire.Add(ac);
            }
            _seedUnderMission[missionBorder] = remaining;
            UpdateSeedBadge(missionBorder);
        }
        foreach (var ac in toAcquire)
        {
            ApplyArtifactAcquire(ac, missionBorder, mission);
            ShowCardReveal(ac, "Artifact acquired",
                $"After solving {mission.Name}, you acquire {ac.Name}.",
                RevealButtons.Ok, ac.Name);
        }

        ShowCardReveal(mission, "Mission solved",
            $"Player {_activePlayer} solved {mission.Name}!\n+{result.Points} points\n\n"
            + $"Score: P1 {_scoreP1}  ·  P2 {_scoreP2}\n\n"
            + "More mission attempts are allowed in the same Execute segment.",
            RevealButtons.Ok, mission.Name);
        StatusText.Text =
            $"Mission solved! +{result.Points} (P{_activePlayer}). Score P1 {_scoreP1} · P2 {_scoreP2}.";
        _attemptMission = null;
        _attemptDiscards.Clear();
        ClearCardActionUi();
    }

    /// <summary>Premiere-Interrupt-Effekte. true = erledigt (kein generisches Discard nötig).</summary>
    /// <summary>
    /// Kevin (and similar) can be dropped on an Event in play: TABLE column or host strip.
    /// Other interrupts just open the action stack from either player's hand.
    /// </summary>
    private bool TryPlayInterruptFromHand(Card card, Border? floating, Point windowPos, int owner)
    {
        if (floating != null)
        {
            if (DragLayer.Children.Contains(floating))
                DragLayer.Children.Remove(floating);
            if (TableCanvas.Children.Contains(floating))
                TableCanvas.Children.Remove(floating);
        }
        RemoveOrphanTableCopies(card);
        ClearEventTargetHighlights();

        bool isKevin = (card.Name ?? "").Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase);
        if (isKevin)
        {
            var target = FindEventTargetAt(windowPos);
            if (target == null)
            {
                ShowPlayError("Kevin Uxbridge: drop onto an Event in play (TABLE or a host stack).");
                return false;
            }
            var chk = TimingRules.CanKevinTargetEvent(target);
            if (!chk.ok)
            {
                ShowPlayError(chk.reason);
                return false;
            }
            BeginPlayCardStack(card, isResponse: _stack.IsOpen, controllerOverride: owner, target: target);
            return true;
        }

        var need = InterruptRules.GetPlayTarget(card);
        if (need is InterruptRules.PlayTarget.OwnCrew or InterruptRules.PlayTarget.AnyCrew
            or InterruptRules.PlayTarget.OwnShip or InterruptRules.PlayTarget.AnyShip)
        {
            var host = FindTeamOrShipHostAt(windowPos, owner, need, card);
            if (host == null)
            {
                ShowPlayError($"{card.Name}: no legal target under the cursor. "
                    + "Highlighted ships/crew only — card returns to hand.");
                return false;
            }
            string cn = (card.Name ?? "").Trim();
            if (cn.Equals("Rogue Borg", StringComparison.OrdinalIgnoreCase)
                && !ShipIsOccupied(host))
            {
                ShowPlayError("Rogue Borg: plays on an occupied ship (personnel must be aboard).");
                return false;
            }
            _interruptTargetHost = host;
            Card? targetCard = host.Tag as Card;
            BeginPlayCardStack(card, isResponse: _stack.IsOpen, controllerOverride: owner, target: targetCard);
            return true;
        }

        if (_stack.IsOpen)
            BeginPlayCardStack(card, isResponse: true, controllerOverride: owner);
        else
            BeginPlayCardStack(card, isResponse: false, controllerOverride: owner);
        return true;
    }

    /// <summary>
    /// Hit-test a crew mini in either strip, or a ship/mission/facility on the board.
    /// </summary>
    private Border? FindTeamOrShipHostAt(Point windowPos, int owner, InterruptRules.PlayTarget need,
        Card? interrupt = null)
    {
        bool Matches(Border host) =>
            interrupt != null
                ? HostMatchesInterruptTargetForCard(host, owner, interrupt)
                : HostMatchesInterruptTarget(host, owner, need);

        var hit = InputHitTest(windowPos) as DependencyObject;
        while (hit != null)
        {
            if (hit is FrameworkElement fe)
            {
                if (fe.Tag is HostCardRef href && Matches(href.Host))
                    return href.Host;
            }
            if (hit is Border b && b.Tag is Card && Matches(b))
                return b;
            hit = VisualTreeHelper.GetParent(hit);
        }

        // Geometry fallback (zoom-safe: screen corners → window coords, padded hit box)
        Border? best = null;
        double bestDist = double.MaxValue;
        foreach (var b in TableCanvas.Children.OfType<Border>().ToList())
        {
            if (b.Visibility != Visibility.Visible || b.Tag is not Card) continue;
            if (!Matches(b)) continue;
            if (!TryGetBorderWindowRect(b, out var rect)) continue;
            rect.Inflate(28, 28);
            if (!rect.Contains(windowPos)) continue;
            double cx = rect.Left + rect.Width / 2;
            double cy = rect.Top + rect.Height / 2;
            double d = (windowPos.X - cx) * (windowPos.X - cx) + (windowPos.Y - cy) * (windowPos.Y - cy);
            if (d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }
        return best;
    }

    private bool TryGetBorderWindowRect(Border b, out Rect rect)
    {
        rect = default;
        try
        {
            double w = b.ActualWidth > 1 ? b.ActualWidth : (b.Width > 1 ? b.Width : TableCardWidth);
            double h = b.ActualHeight > 1 ? b.ActualHeight : (b.Height > 1 ? b.Height : TableCardHeight);
            var tl = PointFromScreen(b.PointToScreen(new Point(0, 0)));
            var br = PointFromScreen(b.PointToScreen(new Point(w, h)));
            rect = new Rect(tl, br);
            return rect.Width > 0 && rect.Height > 0;
        }
        catch
        {
            return false;
        }
    }

    private bool HostMatchesInterruptTargetForCard(Border host, int owner, Card interrupt)
    {
        string n = (interrupt.Name ?? "").Trim();
        if (n.Equals("Rogue Borg", StringComparison.OrdinalIgnoreCase))
            return host.Tag is Card hc && IsShipCard(hc) && ShipIsOccupied(host);
        if (n.Equals("Crosis", StringComparison.OrdinalIgnoreCase))
            return host.Tag is Card hc && IsShipCard(hc);
        return HostMatchesInterruptTarget(host, owner, InterruptRules.GetPlayTarget(interrupt));
    }

    private bool HostMatchesInterruptTarget(Border host, int owner, InterruptRules.PlayTarget need)
    {
        if (host.Tag is not Card hc) return false;
        int ho = GetBorderOwner(host);
        if (ho == 0) ho = owner;

        bool isShip = IsShipCard(hc);
        bool isFac = IsFacilityCard(hc);
        bool isMission = IsMissionCard(hc);
        if (!isShip && !isFac && !isMission) return false;

        bool ownHost = ho == owner;
        bool hasOwnPersonnel = HostHasPersonnelOf(host, owner);
        bool hasAnyPersonnel = HostHasPersonnelOf(host, 0);

        return need switch
        {
            InterruptRules.PlayTarget.OwnShip => (isShip || isFac) && ownHost,
            InterruptRules.PlayTarget.AnyShip => isShip || isFac,
            InterruptRules.PlayTarget.OwnCrew => hasOwnPersonnel,
            InterruptRules.PlayTarget.AnyCrew => hasAnyPersonnel,
            _ => false
        };
    }

    private bool HostMatchesRogueBorgTarget(Border host, Card interrupt)
        => HostMatchesInterruptTargetForCard(host, _activePlayer, interrupt);

    /// <summary>Personnel (any owner) aboard this host ship/mission/facility.</summary>
    private bool ShipIsOccupied(Border host) => HostHasPersonnelOf(host, 0);

    private bool HostHasPersonnelOf(Border host, int ownerOrZero)
    {
        if (!_stackOnHost.TryGetValue(host, out var list) || list.Count == 0)
            return false;
        foreach (var b in list)
        {
            if (b.Tag is not Card c) continue;
            if (!IsCrewType(c) && !ModifierRules.IsPersonnelCard(c)
                && !(c.Type ?? "").Contains("personnel", StringComparison.OrdinalIgnoreCase))
                continue;
            // Rogue Borg tokens are interrupts, not personnel
            if ((c.Name ?? "").Equals("Rogue Borg", StringComparison.OrdinalIgnoreCase))
                continue;
            if (ownerOrZero != 0)
            {
                int o = GetBorderOwner(b);
                if (o == 0) o = CardOwner(b);
                if (o != ownerOrZero) continue;
            }
            return true;
        }
        return false;
    }

    private Card? FindEventTargetAt(Point windowPos)
    {
        var hit = InputHitTest(windowPos) as DependencyObject;
        while (hit != null)
        {
            if (hit is FrameworkElement fe)
            {
                if (fe.Tag is HostCardRef href && EventRules.IsEvent(href.Card))
                    return href.Card;
                if (fe.Tag is Card c && EventRules.IsEvent(c))
                    return c;
            }
            hit = VisualTreeHelper.GetParent(hit);
        }

        // Dropped on a ship / mission / facility that has an attached event
        foreach (var b in TableCanvas.Children.OfType<Border>().ToList())
        {
            if (b.Tag is not Card hc) continue;
            if (b.Visibility != Visibility.Visible) continue;
            try
            {
                var tl = b.TransformToAncestor(this).Transform(new Point(0, 0));
                var rect = new Rect(tl, b.RenderSize);
                if (!rect.Contains(windowPos)) continue;
            }
            catch { continue; }

            var events = new List<Card>();
            if (_stackOnHost.TryGetValue(b, out var list))
            {
                foreach (var mb in list)
                    if (mb.Tag is Card ec && EventRules.IsEvent(ec))
                        events.Add(ec);
            }
            foreach (var ae in _attachedEvents)
            {
                if ((ReferenceEquals(ae.Host, b) || ReferenceEquals(ae.Host2, b))
                    && EventRules.IsEvent(ae.Card) && !events.Contains(ae.Card))
                    events.Add(ae.Card);
            }
            if (events.Count == 1) return events[0];
            if (events.Count > 1)
                return events[0];
        }
        return null;
    }

    private void NullifyEventInPlay(Card ev, int byPlayer)
    {
        int owner = _tablePermanentCards.Contains(ev) ? 1
            : _oppTablePermanentCards.Contains(ev) ? 2
            : byPlayer;

        RemoveCardFromTableColumn(ev);
        foreach (var ae in _attachedEvents.Where(x => ReferenceEquals(x.Card, ev)).ToList())
            _attachedEvents.Remove(ae);

        foreach (var kv in _stackOnHost.ToList())
        {
            foreach (var b in kv.Value.ToList())
            {
                if (b.Tag is not Card c || !ReferenceEquals(c, ev)) continue;
                kv.Value.Remove(b);
                if (TableCanvas.Children.Contains(b))
                    TableCanvas.Children.Remove(b);
            }
        }

        SendCardTo(ev, owner, TimingRules.Destination.Discard);
        RebuildTablePermanentsPanel();
        if (_hostStripHost != null && _hostStripHost.Tag is Card hc)
            ShowHostContents(_hostStripHost, hc);
        _session.Log.Add(_session.TurnNumber, $"P{byPlayer}", $"Nullified {ev.Name}");
        StatusText.Text = $"{ev.Name} nullified.";
    }

    private bool TryResolveInterruptPlay(Card card, int controller, bool isResponse, Card? target = null)
    {
        var r = InterruptRules.Resolve(card);
        if (r.Kind == InterruptRules.Kind.TimingOnly)
        {
            bool isKevin = (card.Name ?? "").Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase);
            if (isKevin && target != null && !isResponse)
                NullifyEventInPlay(target, controller);
            // Response nullify of a just-played event is already applied in ApplyResponseEffect
            else if (isKevin && target != null)
                NullifyEventInPlay(target, controller);

            if (r.OutOfPlay)
                SendCardTo(card, controller, TimingRules.Destination.OutOfPlay);
            else if (r.DiscardAfter)
                SendCardTo(card, controller, TimingRules.Destination.Discard);
            StatusText.Text = target != null
                ? $"{card.Name} nullifies {target.Name}."
                : r.Message;
            return true;
        }

        ShowCardReveal(card, "Interrupt",
            (card.Text ?? "") + "\n\n→ " + r.Message, RevealButtons.Ok, card.Name);

        switch (r.Effect)
        {
            case InterruptRules.Effect.RogueBorg:
                {
                    var host = _interruptTargetHost;
                    if (host == null || host.Tag is not Card ship || !IsShipCard(ship)
                        || !ShipIsOccupied(host))
                    {
                        ShowPlayError("Rogue Borg: need an occupied ship. Card returns to hand.");
                        var hand = controller == 1 ? _handCards : _oppHandCards;
                        if (!hand.Contains(card)) hand.Add(card);
                        RefreshHandStrips();
                        RefreshZoneCounts();
                        break;
                    }
                    PlaceRogueBorg(card, host, controller);
                    break;
                }
            case InterruptRules.Effect.Crosis:
                {
                    var host = _interruptTargetHost;
                    if (host == null || host.Tag is not Card ship || !IsShipCard(ship))
                    {
                        ShowPlayError("Crosis: drop onto a ship. Card returns to hand.");
                        var hand = controller == 1 ? _handCards : _oppHandCards;
                        if (!hand.Contains(card)) hand.Add(card);
                        RefreshHandStrips();
                        RefreshZoneCounts();
                        break;
                    }
                    _crosisShips.Add(host);
                    _attachedEvents.Add(new AttachedEvent
                    {
                        Card = card,
                        Kind = EventRules.Persist.None,
                        Countdown = 1,
                        Host = host,
                        Owner = controller,
                        // "At start of next turn, discard" = chronological next turn
                        TurnScope = TimingRules.TurnScope.NextTurn,
                        PhasePoint = TimingRules.TurnPhasePoint.StartOfTurn,
                        ScopePlayer = null
                    });
                    // Visual on ship stack
                    var crosisBorder = CreateFloatingCard(card);
                    crosisBorder.Visibility = Visibility.Collapsed;
                    if (!TableCanvas.Children.Contains(crosisBorder))
                        TableCanvas.Children.Add(crosisBorder);
                    AddCardToHostStack(host, crosisBorder);

                    int n = CountRogueBorgOn(host);
                    int str = RogueBorgStrengthOn(host);
                    ShowCardReveal(card, "Crosis",
                        $"On {ship.Name}: doubles STRENGTH of all Rogue Borg present.\n"
                        + $"Rogue Borg ×{n}  →  STRENGTH {str} (includes Crosis ×2).\n"
                        + "Discarded at start of next turn.",
                        RevealButtons.Ok, card.Name);
                    StatusText.Text = $"Crosis on {ship.Name} — Rogue Borg STR {str}.";
                    _session.Log.Add(_session.TurnNumber, $"P{controller}",
                        $"Crosis on {ship.Name}: RB STR {str}");
                    UpdateHostBadge(host);
                    break;
                }
            case InterruptRules.Effect.EmergencyBeam:
                {
                    var host = _interruptTargetHost;
                    if (host != null && host.Tag is Card)
                    {
                        ShowHostContents(host, (Card)host.Tag!, beamSelectMode: true);
                        BeginBeamMode(host);
                        StatusText.Text = "Emergency Transporter Armbands: select personnel, then click a destination.";
                    }
                    else
                        StatusText.Text = "Emergency Transporter Armbands: no team host.";
                    break;
                }
            case InterruptRules.Effect.DisruptorOverload:
                {
                    Border? host = _interruptTargetHost ?? PickAnyHostWithEquipment();
                    if (host != null && _stackOnHost.TryGetValue(host, out var list))
                    {
                        var eqs = list.Where(b => b.Tag is Card c && ModifierRules.IsEquipmentCard(c)
                                                  && !(c.Icons ?? "").Contains("Shield", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        if (eqs.Count > 0)
                        {
                            var victim = eqs[new Random().Next(eqs.Count)];
                            if (victim.Tag is Card eq)
                                RemoveEquipmentFromHost(host, eq);
                        }
                    }
                    break;
                }
            case InterruptRules.Effect.PalorToff:
                {
                    var disc = controller == 1 ? _discardCards : _oppDiscardCards;
                    var hand = controller == 1 ? _handCards : _oppHandCards;
                    Card? pick = null;
                    foreach (var c in disc.Where(c => !ModifierRules.IsPersonnelCard(c)).ToList())
                    {
                        if (ShowCardReveal(c, "Palor Toff", $"{c.Name} nehmen?", RevealButtons.YesNo) == RevealAnswer.Yes)
                        {
                            pick = c;
                            break;
                        }
                    }
                    if (pick != null)
                    {
                        disc.Remove(pick);
                        hand.Add(pick);
                    }
                    break;
                }
            case InterruptRules.Effect.TheJuggler:
                {
                    int who = ShowCardReveal(card, "The Juggler", "Choose whose draw deck is shuffled.",
                        RevealButtons.PlayerPick) == RevealAnswer.Yes ? 1 : 2;
                    bool timedJ = _revealTimedOut;
                    var draw = who == 1 ? _drawCards : _oppDrawCards;
                    var shuffled = draw.OrderBy(_ => Guid.NewGuid()).ToList();
                    draw.Clear();
                    draw.AddRange(shuffled);
                    _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}", $"Juggler → P{who}");
                    AnnounceChoiceResult(card, "The Juggler",
                        $"Player {who}'s draw deck was shuffled."
                        + (timedJ ? "\n(No choice in time — default Player 1.)" : ""));
                    break;
                }
            case InterruptRules.Effect.JaglomLook:
                {
                    // Premiere text: "Examine opponent's draw deck, then replace unshuffled."
                    int oppPlayer = controller == 1 ? 2 : 1;
                    BeginOpponentPileInteract(
                        oppPlayer,
                        TargetPileType.Draw,
                        PileInteraction.ViewOnly | PileInteraction.FullView,
                        "Jaglom Shrek — examine opponent's draw deck (faces up), then Done. Deck is not shuffled.");
                    break;
                }
            case InterruptRules.Effect.PlanetScan:
            case InterruptRules.Effect.SpaceScan:
                {
                    bool planet = r.Effect == InterruptRules.Effect.PlanetScan;
                    foreach (var m in _spacelineOrder)
                    {
                        if (m.Tag is not Card mc) continue;
                        if (planet && !MissionRules.IsPlanetMission(mc)) continue;
                        if (!planet && !MissionRules.IsSpaceMission(mc)) continue;
                        if (!_seedUnderMission.TryGetValue(m, out var stack) || stack.Count == 0) continue;
                        if (stack[^1].Tag is Card bottom)
                            ShowCardReveal(bottom, "Scan",
                                $"Unterste Seed unter {mc.Name}:\n{bottom.Name}\n{bottom.Text}",
                                RevealButtons.Ok);
                        break;
                    }
                    break;
                }
            case InterruptRules.Effect.LongRangeScan:
                {
                    foreach (var b in TableCanvas.Children.OfType<Border>())
                    {
                        if (b.Tag is not Card sc || !IsShipCard(sc)) continue;
                        var aboard = GetAllCardsOnHost(b, GetBorderOwner(b) == 0 ? 1 : GetBorderOwner(b));
                        ShowCardReveal(sc, "Long-Range Scan",
                            string.Join("\n", aboard.Select(c => c.Name)), RevealButtons.Ok);
                        break;
                    }
                    break;
                }
            case InterruptRules.Effect.ParticleFountain:
                AwardDilemmaPoints(r.Points > 0 ? r.Points : 5);
                break;
            case InterruptRules.Effect.DeathYell:
                AwardDilemmaPoints(5);
                break;
            case InterruptRules.Effect.ShipSeizure:
                {
                    foreach (var b in TableCanvas.Children.OfType<Border>())
                    {
                        if (b.Tag is not Card sc || !IsShipCard(sc)) continue;
                        if (GetBorderOwner(b) == controller) continue;
                        if (GetCrewOnShip(b).Count > 0) continue;
                        DestroyShipOrFacility(b, sc, GetBorderOwner(b) == 0 ? 1 : GetBorderOwner(b));
                        break;
                    }
                    break;
                }
            case InterruptRules.Effect.Wormhole:
                {
                    Border? shipB = null;
                    foreach (var b in TableCanvas.Children.OfType<Border>())
                    {
                        if (b.Tag is Card sc && IsShipCard(sc) && GetBorderOwner(b) == controller)
                        {
                            if (ShowCardReveal(sc, "Wormhole", $"Schiff {sc.Name}?", RevealButtons.YesNo) == RevealAnswer.Yes)
                            {
                                shipB = b;
                                break;
                            }
                        }
                    }
                    Border? dest = null;
                    foreach (var m in _spacelineOrder)
                    {
                        if (ShowCardReveal(card, "Wormhole",
                                $"Destination {(m.Tag as Card)?.Name}?", RevealButtons.YesNo) == RevealAnswer.Yes)
                        {
                            dest = m;
                            break;
                        }
                    }
                    if (shipB != null && dest != null)
                    {
                        // Relocate ship under mission visually via existing dock logic if any
                        MarkStopped(shipB);
                        StatusText.Text = $"Wormhole: {(shipB.Tag as Card)?.Name} → {(dest.Tag as Card)?.Name} (stopped).";
                    }
                    break;
                }
            case InterruptRules.Effect.Transwarp:
                {
                    Border? shipB = PickOwnShip(controller);
                    if (shipB != null && shipB.Tag is Card sc)
                    {
                        int printed = BattleRules.EffectiveRange(sc, GetHullDamage(shipB));
                        int left = GetRemainingRange(shipB, sc);
                        int used = Math.Max(0, printed - left);
                        // Full RANGE is doubled; RANGE already spent this turn still counts.
                        _shipRangeLeft[shipB] = Math.Max(0, printed * 2 - used);
                        // discard end of turn via attached dilemma-like flag on attached events list reuse
                        _attachedEvents.Add(new AttachedEvent
                        {
                            Card = card,
                            Kind = EventRules.Persist.None,
                            Owner = controller,
                            Host = shipB,
                            Countdown = 0
                        });
                    }
                    break;
                }
            case InterruptRules.Effect.AutoDestruct:
                {
                    Border? shipB = PickOwnShip(controller);
                    if (shipB != null)
                    {
                        _attachedDilemmas.Add(new AttachedDilemma
                        {
                            Card = card,
                            Kind = DilemmaRules.PersistKind.Nitrium, // countdown destroy ship
                            Countdown = r.Countdown > 0 ? r.Countdown : 2,
                            Host = shipB
                        });
                    }
                    break;
                }
            case InterruptRules.Effect.SubspaceSchism:
                _session.SuppressEndOfTurnDraw = false;
                // next draw discard: simple flag on session via log note
                StatusText.Text = "Subspace Schism active (next draw: card discarded).";
                break;
        }

        if (r.OutOfPlay)
            SendCardTo(card, controller, TimingRules.Destination.OutOfPlay);
        else if (r.DiscardAfter)
            SendCardTo(card, controller, TimingRules.Destination.Discard);

        // Instant interrupts must never remain as free-floating board cards
        // (skip Rogue Borg / Crosis that legitimately sit on a host stack)
        if (r.Effect is not InterruptRules.Effect.RogueBorg and not InterruptRules.Effect.Crosis)
            RemoveOrphanTableCopies(card);

        _session.Log.Add(_session.TurnNumber, $"P{controller}", $"Interrupt {card.Name}: {r.Effect}");
        RefreshZoneCounts();
        return true;
    }

    /// <summary>Remove any TableCanvas borders still showing this card (failed drops / zoom bugs).</summary>
    private void RemoveOrphanTableCopies(Card card)
    {
        foreach (var b in TableCanvas.Children.OfType<Border>().ToList())
        {
            if (b.Tag is not Card c || !ReferenceEquals(c, card)) continue;
            // Keep cards that are aboard a host stack (Rogue Borg tokens, etc.)
            if (_stackOnHost.Values.Any(list => list.Contains(b))) continue;
            TableCanvas.Children.Remove(b);
        }
        if (DragLayer != null)
        {
            foreach (var b in DragLayer.Children.OfType<Border>().ToList())
            {
                if (b.Tag is Card c && ReferenceEquals(c, card))
                    DragLayer.Children.Remove(b);
            }
        }
    }

    private Border? PickOwnShip(int owner)
    {
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Tag is Card sc && IsShipCard(sc) && GetBorderOwner(b) == owner)
            {
                if (ShowCardReveal(sc, "Choose ship", $"{sc.Name}?", RevealButtons.YesNo) == RevealAnswer.Yes)
                    return b;
            }
        }
        return TableCanvas.Children.OfType<Border>()
            .FirstOrDefault(b => b.Tag is Card sc && IsShipCard(sc) && GetBorderOwner(b) == owner);
    }

    private Border? PickAnyHostWithEquipment()
    {
        foreach (var kv in _stackOnHost)
        {
            if (kv.Value.Any(b => b.Tag is Card c && ModifierRules.IsEquipmentCard(c)))
                return kv.Key;
        }
        return null;
    }

    private bool GoddessBlocksInterrupt(Card interrupt)
    {
        if (EventRules.IsGoddessException(interrupt)) return false;
        return _attachedEvents.Any(e => e.Kind == EventRules.Persist.Goddess)
               || _tablePermanentCards.Any(c => (c.Name ?? "").Equals("Goddess of Empathy", StringComparison.OrdinalIgnoreCase))
               || _oppTablePermanentCards.Any(c => (c.Name ?? "").Equals("Goddess of Empathy", StringComparison.OrdinalIgnoreCase));
    }

    private bool HasTableEvent(string name) =>
        _tablePermanentCards.Concat(_oppTablePermanentCards)
            .Any(c => (c.Name ?? "").Equals(name, StringComparison.OrdinalIgnoreCase))
        || _attachedEvents.Any(e => (e.Card.Name ?? "").Equals(name, StringComparison.OrdinalIgnoreCase));

    private bool HasPatternEnhancers() =>
        _attachedEvents.Any(e => e.Kind == EventRules.Persist.PatternEnhancers) || HasTableEvent("Pattern Enhancers");

    private IEnumerable<AttachedEvent> EventsOn(Border? host) =>
        host == null
            ? Enumerable.Empty<AttachedEvent>()
            : _attachedEvents.Where(e => ReferenceEquals(e.Host, host) || ReferenceEquals(e.Host2, host));

    private bool TryResolveEventPlay(Card ev, int controller)
    {
        var r = EventRules.ResolvePlay(ev);
        if (r.NeedsToxUthat && !HasTableEvent("Tox Uthat")
            && !(_activePlayer == 1 ? _tablePermanentCards : _oppTablePermanentCards)
                .Any(c => (c.Name ?? "").Equals("Tox Uthat", StringComparison.OrdinalIgnoreCase)))
        {
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!hand.Any(c => (c.Name ?? "").Equals("Tox Uthat", StringComparison.OrdinalIgnoreCase))
                && !HasTableEvent("Tox Uthat"))
            {
                ShowPlayError("Supernova erfordert Tox Uthat.");
                var h = controller == 1 ? _handCards : _oppHandCards;
                if (!h.Contains(ev)) h.Add(ev);
                return true;
            }
        }

        if (r.Place == EventRules.Place.Instant)
        {
            ApplyInstantEvent(ev, controller, r);
            SendCardTo(ev, controller, TimingRules.Destination.Discard);
            return true;
        }

        var tk = EventRules.GetTargetKind(r);
        Border? host = null;
        Border? host2 = null;
        Border? span = null;

        if (tk == EventRules.TargetKind.GapBetweenMissions)
        {
            host = PickGapEndpoint(ev, controller);
            host2 = PickAdjacentMission(host);
            if (host == null || host2 == null)
            {
                ShowPlayError($"No same-quadrant gap for {ev.Name} – card stays in hand.");
                RemoveCardFromTableColumn(ev);
                var h = controller == 1 ? _handCards : _oppHandCards;
                if (!h.Contains(ev)) h.Add(ev);
                return true;
            }
            span = PlaceSpanOnSpaceline(ev, host, host2, controller);
            RemoveCardFromTableColumn(ev);
        }
        else if (EventRules.NeedsTableHost(tk))
        {
            host = PickEventHost(ev, controller, r.Place);
            if (host == null)
            {
                ShowPlayError($"No valid target for {ev.Name} – card stays in hand.");
                RemoveCardFromTableColumn(ev);
                var h = controller == 1 ? _handCards : _oppHandCards;
                if (!h.Contains(ev)) h.Add(ev);
                return true;
            }
            AttachCardToHost(ev, host, controller);
        }
        else
        {
            // Generic table events (Treaties, Goddess, …)
            CommitCardToTable(ev, controller);
        }

        if (r.Persist != EventRules.Persist.None)
        {
            var ae = new AttachedEvent
            {
                Card = ev,
                Kind = r.Persist,
                Owner = controller,
                Host = host,
                Host2 = host2,
                Countdown = r.Countdown,
                EspionageAs = r.EspionageAs,
                EspionageOn = r.EspionageOn
            };
            // Compendium turn wording defaults for known persist kinds
            AssignTurnScopeForEvent(ae, r.Persist, controller, host);
            if (r.Persist == EventRules.Persist.Traveler)
            {
                bool timed;
                ae.TravelerPlayer = ShowCardReveal(ev, "The Traveler",
                    "Choose which player gets the Traveler draw benefit.",
                    RevealButtons.PlayerPick) == RevealAnswer.Yes
                    ? 1 : 2;
                timed = _revealTimedOut;
                AnnounceChoiceResult(ev, "The Traveler",
                    $"Draw benefit applies to Player {ae.TravelerPlayer}."
                    + (timed ? "\n(No choice in time — default applied.)" : ""));
            }
            if ((r.Persist == EventRules.Persist.QNet || r.Persist == EventRules.Persist.Gaps)
                && ae.Host2 == null)
            {
                ae.Host2 = PickAdjacentMission(host);
            }
            if (r.Persist == EventRules.Persist.RedAlert)
            {
                // Playing Red Alert is this turn's normal card play.
                // The 5 personnel/equipment replace the normal play on later turns.
                _redAlertPlaysLeft = 0;
                StatusText.Text =
                    "Red Alert! is on table. Starting next turn, instead of your normal " +
                    "card play you may play up to 5 personnel and/or equipment.";
            }
            if (r.Persist == EventRules.Persist.Traveler)
            {
                // Continuous nullify: SWBs stay on table but have no effect while Traveler is in play.
                // (If Kevin removes Traveler, existing Static Warp Bubbles resume.)
                int swbCount = _attachedEvents.Count(x => x.Kind == EventRules.Persist.StaticWarp)
                    + _tablePermanentCards.Count(c => (c.Name ?? "").Equals("Static Warp Bubble", StringComparison.OrdinalIgnoreCase))
                    + _oppTablePermanentCards.Count(c => (c.Name ?? "").Equals("Static Warp Bubble", StringComparison.OrdinalIgnoreCase));
                if (swbCount > 0)
                {
                    ShowCardReveal(ev, "The Traveler: Transcendence",
                        "Nullifies each Static Warp Bubble while this event remains in play.\n"
                        + $"({swbCount} Static Warp Bubble(s) stay on table but have no effect until Traveler leaves.)",
                        RevealButtons.Ok, ev.Name);
                }
            }
            if (r.Persist == EventRules.Persist.PlasmaFire && host != null && host.Tag is Card fireShip)
            {
                UpdateHostBadge(host);
                ShowHostContents(host, fireShip);
                StatusText.Text =
                    $"Plasma Fire on {fireShip.Name}. Damages at the end of that ship's controller's turn. "
                    + "SECURITY may nullify (button on the ship).";
            }
            if (r.Persist == EventRules.Persist.LoreReturns && host != null)
            {
                if (!TryApplyLoreReturns(ev, host, controller))
                {
                    // detach and return to hand
                    RemoveCardFromHostStack(host, FindBorderForCard(ev) ?? host);
                    RemoveCardFromTableColumn(ev);
                    var h = controller == 1 ? _handCards : _oppHandCards;
                    if (!h.Contains(ev)) h.Add(ev);
                    _attachedEvents.RemoveAll(x => ReferenceEquals(x.Card, ev));
                    return true;
                }
            }
            if (r.Persist == EventRules.Persist.Supernova && host != null)
                ApplySupernova(host);
            if (r.Persist == EventRules.Persist.RaiseStakes)
            {
                if (ShowCardReveal(ev, "Raise the Stakes",
                        $"P{3 - controller}: Opponent wins immediately? (No = event stays)",
                        RevealButtons.YesNo) == RevealAnswer.Yes)
                {
                    StatusText.Text = $"Raise the Stakes: Player {controller} wins (sandbox).";
                    _session.Log.Add(_session.TurnNumber, $"P{controller}", "Raise the Stakes: claimed win");
                }
            }
            _attachedEvents.Add(ae);
        }

        StatusText.Text = r.Message;
        _session.Log.Add(_session.TurnNumber, $"P{controller}", $"Event {ev.Name}");
        return true;
    }

    private Border? PickEventHost(Card ev, int controller, EventRules.Place place)
    {
        var tk = EventRules.GetTargetKind(EventRules.ResolvePlay(ev));
        if (tk == EventRules.TargetKind.GapBetweenMissions)
            return PickGapEndpoint(ev, controller);

        var candidates = CollectEventTargets(ev, controller, tk, place);

        // Drag preference
        if (_eventPreferredHost != null && candidates.Contains(_eventPreferredHost))
        {
            var pref = _eventPreferredHost;
            _eventPreferredHost = null;
            _eventPreferredHost2 = null;
            return pref;
        }
        _eventPreferredHost = null;
        _eventPreferredHost2 = null;

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];
        return ShowTargetPickDialog(ev, candidates, "Choose target");
    }

    private List<Border> CollectEventTargets(Card ev, int controller, EventRules.TargetKind tk, EventRules.Place place)
    {
        var candidates = new List<Border>();
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Tag is not Card c) continue;
            if (tk == EventRules.TargetKind.Ship || place == EventRules.Place.OnShip)
            {
                if (!IsShipCard(c)) continue;
                string n = ev.Name ?? "";
                if (n.Equals("Lore Returns", StringComparison.OrdinalIgnoreCase))
                {
                    // Opponent's empty ship with Rogue Borg aboard
                    int ho = GetBorderOwner(b);
                    if (ho == 0) ho = 1;
                    if (ho == controller) continue;
                    if (CountRogueBorgOn(b) == 0) continue;
                    if (HostHasPersonnelOf(b, 0)) continue; // must be empty of personnel
                    candidates.Add(b);
                    continue;
                }
                bool anyShip = n.Contains("Plasma", StringComparison.OrdinalIgnoreCase)
                               || n.Contains("Warp Core", StringComparison.OrdinalIgnoreCase)
                               || n.Contains("Neural", StringComparison.OrdinalIgnoreCase);
                if (n.Equals("Plasma Fire", StringComparison.OrdinalIgnoreCase)
                    && IsBorgAffiliation(c))
                    continue; // non-[Bor] ships only
                if (anyShip || GetBorderOwner(b) == controller)
                    candidates.Add(b);
            }
            else if (tk == EventRules.TargetKind.PlanetMission || place == EventRules.Place.OnPlanet)
            {
                if (string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase)
                    && MissionRules.IsPlanetMission(c))
                    candidates.Add(b);
            }
            else if (tk == EventRules.TargetKind.Mission || place == EventRules.Place.OnMission)
            {
                if (string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase))
                    candidates.Add(b);
            }
            else if (tk == EventRules.TargetKind.Outpost || place == EventRules.Place.OnOutpost)
            {
                if ((c.Type ?? "").Contains("facility", StringComparison.OrdinalIgnoreCase)
                    && GetBorderOwner(b) == controller)
                    candidates.Add(b);
            }
        }
        return candidates.Distinct().ToList();
    }

    /// <summary>Gaps/Q-Net: pick left endpoint of a same-quadrant adjacent pair (Host2 = neighbor).</summary>
    private Border? PickGapEndpoint(Card ev, int controller)
    {
        var pairs = ListSameQuadrantGaps();
        if (_eventPreferredHost != null && _eventPreferredHost2 != null)
        {
            var match = pairs.FirstOrDefault(p =>
                (ReferenceEquals(p.left, _eventPreferredHost) && ReferenceEquals(p.right, _eventPreferredHost2))
                || (ReferenceEquals(p.left, _eventPreferredHost2) && ReferenceEquals(p.right, _eventPreferredHost)));
            if (match.left != null)
            {
                // Keep host2 for PickAdjacentMission
                _eventPreferredHost = null;
                _eventPreferredHost2 = match.right;
                return match.left;
            }
            _eventPreferredHost = null;
            _eventPreferredHost2 = null;
        }
        _eventPreferredHost = null;
        _eventPreferredHost2 = null;
        if (pairs.Count == 0)
        {
            StatusText.Text = "No same-quadrant gaps (need two adjacent missions in one quadrant).";
            return null;
        }
        if (pairs.Count == 1)
            return pairs[0].left;

        var labels = pairs.Select(p =>
        {
            string a = (p.left.Tag as Card)?.Name ?? "?";
            string b = (p.right.Tag as Card)?.Name ?? "?";
            return $"{a}  ↔  {b}";
        }).ToList();
        int idx = ShowIndexPickDialog(ev.Name ?? "Gap", labels);
        if (idx < 0 || idx >= pairs.Count) return null;
        // Ensure PickAdjacentMission returns the other side
        _eventPreferredHost2 = pairs[idx].right;
        return pairs[idx].left;
    }

    private List<(Border left, Border right)> ListSameQuadrantGaps()
    {
        var list = new List<(Border, Border)>();
        for (int i = 0; i + 1 < _spacelineOrder.Count; i++)
        {
            var a = _spacelineOrder[i];
            var b = _spacelineOrder[i + 1];
            if (a.Tag is not Card ca || b.Tag is not Card cb) continue;
            if (GetNativeQuadrant(ca) != GetNativeQuadrant(cb)) continue;
            list.Add((a, b));
        }
        return list;
    }

    private Border? ShowTargetPickDialog(Card ev, List<Border> candidates, string title)
    {
        var labels = candidates.Select(b => (b.Tag as Card)?.Name ?? "?").ToList();
        int idx = ShowIndexPickDialog($"{ev.Name}: {title}", labels);
        if (idx < 0 || idx >= candidates.Count) return null;
        return candidates[idx];
    }

    private int ShowIndexPickDialog(string title, List<string> labels)
    {
        int chosen = -1;
        var win = new Window
        {
            Title = title,
            Width = 420,
            Height = 360,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(32, 32, 34))
        };
        var lb = new ListBox
        {
            Margin = new Thickness(8),
            Background = new SolidColorBrush(Color.FromRgb(20, 20, 22)),
            Foreground = Brushes.White,
            ItemsSource = labels
        };
        if (labels.Count > 0) lb.SelectedIndex = 0;
        var ok = new Button
        {
            Content = "Select",
            Height = 32,
            Margin = new Thickness(8),
            Background = new SolidColorBrush(Color.FromRgb(14, 99, 156)),
            Foreground = Brushes.White
        };
        ok.Click += (_, _) =>
        {
            chosen = lb.SelectedIndex;
            win.Close();
        };
        var root = new DockPanel();
        DockPanel.SetDock(ok, Dock.Bottom);
        root.Children.Add(ok);
        root.Children.Add(lb);
        win.Content = root;
        win.ShowDialog();
        return chosen;
    }

    private Point WindowToTablePoint(Point windowPos)
    {
        try
        {
            var origin = TableCanvas.TransformToAncestor(this).Transform(new Point(0, 0));
            double scale = ZoomTransform.ScaleX;
            if (scale < 0.01) scale = 1;
            return new Point((windowPos.X - origin.X) / scale, (windowPos.Y - origin.Y) / scale);
        }
        catch
        {
            return windowPos;
        }
    }

    /// <summary>Snap event onto ship/mission/gap at table coordinates (not DragLayer window coords).</summary>
    private (Border? host, Border? host2) TrySnapEventTargetAt(double cx, double cy, EventRules.TargetKind tk, int controller, Card ev)
    {
        if (tk == EventRules.TargetKind.GapBetweenMissions)
        {
            var gaps = ListSameQuadrantGaps();
            if (gaps.Count == 0) return (null, null);
            (Border left, Border right)? best = null;
            double bestD = double.MaxValue;
            foreach (var (left, right) in gaps)
            {
                double lx = Canvas.GetLeft(left) + TableCardWidth;
                double rx = Canvas.GetLeft(right);
                double midX = (lx + rx) / 2.0;
                double midY = Canvas.GetTop(left) + TableCardHeight / 2.0;
                double d = Math.Abs(cx - midX) + Math.Abs(cy - midY) * 0.35;
                if (d < bestD)
                {
                    bestD = d;
                    best = (left, right);
                }
            }
            // Must be near the mid-gap on the spaceline — not the center of a mission card
            if (best != null && bestD < TableCardWidth * 0.95)
                return (best.Value.left, best.Value.right);
            return (null, null);
        }

        var place = tk switch
        {
            EventRules.TargetKind.Ship => EventRules.Place.OnShip,
            EventRules.TargetKind.PlanetMission => EventRules.Place.OnPlanet,
            EventRules.TargetKind.Mission => EventRules.Place.OnMission,
            EventRules.TargetKind.Outpost => EventRules.Place.OnOutpost,
            _ => EventRules.Place.Table
        };
        var candidates = CollectEventTargets(ev, controller, tk, place);
        Border? nearest = null;
        double dist = double.MaxValue;
        foreach (var b in candidates)
        {
            double bx = Canvas.GetLeft(b) + TableCardWidth / 2.0;
            double by = Canvas.GetTop(b) + TableCardHeight / 2.0;
            double d = Math.Sqrt((cx - bx) * (cx - bx) + (cy - by) * (cy - by));
            if (d < dist)
            {
                dist = d;
                nearest = b;
            }
        }
        if (nearest != null && dist < TableCardWidth * 1.8)
            return (nearest, null);
        return (null, null);
    }

    private Border PlaceSpanOnSpaceline(Card ev, Border left, Border right, int owner)
    {
        int i1 = _spacelineOrder.IndexOf(left);
        int i2 = _spacelineOrder.IndexOf(right);
        if (i1 < 0) i1 = 0;
        if (i2 < 0) i2 = i1 + 1;
        int insert = Math.Min(i1, i2) + 1;

        var border = AddCardToTable(ev, 0, SpacelineY, TableCardWidth);
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(180, 120, 220));
        border.BorderThickness = new Thickness(2);
        border.ToolTip = (ev.Name ?? "Span") + "\nSpan event between missions";
        SetBorderOwner(border, owner);
        if (insert < 0) insert = 0;
        if (insert > _spacelineOrder.Count) insert = _spacelineOrder.Count;
        _spacelineOrder.Insert(insert, border);
        RelayoutMissionsOnSpaceline();
        StatusText.Text = $"{ev.Name} inserted on spaceline between {(left.Tag as Card)?.Name} and {(right.Tag as Card)?.Name}.";
        return border;
    }

    private bool IsHandReportDrag(Card card)
    {
        if (_seedPhaseActive) return false;
        if (_session.Match != GameSession.MatchPhase.Play) return false;
        if (!ReportingRules.MustReportForDuty(card)) return false;
        return _zoneDragRef != null && _zoneDragRef.ZoneName == "Hand";
    }

    private List<Border> CollectLegalReportHosts(Card card, int owner)
    {
        var list = new List<Border>();
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Visibility != Visibility.Visible || b.Tag is not Card hc) continue;
            if (!ReportingRules.IsFacilityHost(hc)) continue;
            if (!CanReportToHost(card, hc, b).ok) continue;
            list.Add(b);
        }
        return list;
    }

    private Border? FindNearestLegalReportHost(double cx, double cy, int owner, Card card, out double distance)
    {
        Border? best = null;
        distance = double.MaxValue;
        foreach (var b in CollectLegalReportHosts(card, owner))
        {
            double bw = b.Width > 0 ? b.Width : TableCardWidth;
            double bh = b.Height > 0 ? b.Height : TableCardHeight;
            double bx = Canvas.GetLeft(b) + bw / 2.0;
            double by = Canvas.GetTop(b) + bh / 2.0;
            double d = Math.Sqrt((cx - bx) * (cx - bx) + (cy - by) * (cy - by));
            if (d < distance)
            {
                distance = d;
                best = b;
            }
        }
        return best;
    }

    /// <summary>Highlight only legal drop targets for the card currently being played.</summary>
    private void ShowLegalPlayHighlights(Card card, bool fromHand)
    {
        ClearEventTargetHighlights();
        if (_seedPhaseActive || (_devPlaySeedFromHand && fromHand && IsSeedableUnderMission(card)))
        {
            if (IsSeedableUnderMission(card))
            {
                foreach (var m in AllMissionBorders())
                {
                    if (m.Tag is not Card mc) continue;
                    if (!CanSeedCardUnderMission(card, mc).ok) continue;
                    AddTargetHalo(m, Color.FromRgb(220, 160, 60));
                }
            }
            else if (IsFacilityCard(card))
            {
                foreach (var m in AllMissionBorders())
                {
                    if (m.Tag is Card mc && CanSeedFacilityAtMission(card, mc).ok)
                        AddTargetHalo(m, Color.FromRgb(40, 180, 140));
                }
            }
            return;
        }

        if (EventRules.IsEvent(card))
        {
            ShowEventTargetHighlights(card);
            return;
        }

        if ((card.Name ?? "").Equals("Kevin Uxbridge", StringComparison.OrdinalIgnoreCase))
        {
            // Snapshot — AddTargetHalo mutates TableCanvas.Children
            foreach (var b in TableCanvas.Children.OfType<Border>().ToList())
            {
                if (b.Tag is not Card) continue;
                bool has = false;
                if (_stackOnHost.TryGetValue(b, out var list))
                    has = list.Any(x => x.Tag is Card c && EventRules.IsEvent(c)
                                        && TimingRules.CanKevinTargetEvent(c).ok);
                if (!has)
                    has = _attachedEvents.Any(e =>
                        (ReferenceEquals(e.Host, b) || ReferenceEquals(e.Host2, b))
                        && TimingRules.CanKevinTargetEvent(e.Card).ok);
                if (has) AddTargetHalo(b, Color.FromRgb(220, 180, 60));
            }
            void MarkEventMini(Border el, Card ev)
            {
                if (!TimingRules.CanKevinTargetEvent(ev).ok) return;
                el.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 210, 80));
                el.BorderThickness = new Thickness(2);
            }
            foreach (var mini in (OppStripPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
                if (mini.Tag is HostCardRef href) MarkEventMini(mini, href.Card);
            foreach (var mini in (PlayerStripPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
                if (mini.Tag is HostCardRef href) MarkEventMini(mini, href.Card);
            foreach (var mini in (TablePermanentsPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
                if (mini.Tag is Card ev) MarkEventMini(mini, ev);
            foreach (var mini in (OppTablePermanentsPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
                if (mini.Tag is Card ev) MarkEventMini(mini, ev);
            return;
        }

        if (fromHand && InterruptRules.IsInterrupt(card)
            && InterruptRules.GetPlayTarget(card) is InterruptRules.PlayTarget.OwnCrew
                or InterruptRules.PlayTarget.AnyCrew
                or InterruptRules.PlayTarget.OwnShip
                or InterruptRules.PlayTarget.AnyShip)
        {
            int owner = _zoneDragRef?.Opponent == true ? 2 : _activePlayer;
            foreach (var b in TableCanvas.Children.OfType<Border>().ToList())
            {
                if (HostMatchesInterruptTargetForCard(b, owner, card))
                    AddTargetHalo(b, Color.FromRgb(80, 200, 220));
            }
            void MarkCrewMini(Border el)
            {
                if (el.Tag is not HostCardRef href) return;
                if (!HostMatchesInterruptTargetForCard(href.Host, owner, card)) return;
                el.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 220, 200));
                el.BorderThickness = new Thickness(2);
            }
            foreach (var mini in (OppStripPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
                MarkCrewMini(mini);
            foreach (var mini in (PlayerStripPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
                MarkCrewMini(mini);
            return;
        }

        if (fromHand && ReportingRules.MustReportForDuty(card))
        {
            int owner = _zoneDragRef?.Opponent == true ? 2 : _activePlayer;
            foreach (var h in CollectLegalReportHosts(card, owner))
                AddTargetHalo(h, Color.FromRgb(80, 200, 120));
            return;
        }

        // Instant event / interrupt with no drop target → highlight the board
        if (fromHand && IsBoardPlayHighlightCard(card))
            SetBoardPlayHalo(true);
    }

    private static bool IsBoardPlayHighlightCard(Card card)
    {
        if (EventRules.IsEvent(card))
        {
            var place = EventRules.ResolvePlay(card).Place;
            return place == EventRules.Place.Instant;
        }
        if (InterruptRules.IsInterrupt(card))
            return InterruptRules.GetPlayTarget(card) == InterruptRules.PlayTarget.None;
        return false;
    }

    private void ApplyBoardPlayerTint()
    {
        if (BoardTintOverlay == null || BoardFrameBorder == null) return;
        if (_activePlayer == 2)
        {
            BoardTintOverlay.Background = new SolidColorBrush(Color.FromRgb(0x14, 0x2A, 0x48));
            BoardTintOverlay.Opacity = 0.28;
            BoardFrameBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x50, 0x90, 0xE0));
            BoardFrameBorder.BorderThickness = new Thickness(3);
            if (BoardInnerGlow != null)
            {
                BoardInnerGlow.BorderBrush = new SolidColorBrush(Color.FromRgb(0x70, 0xB0, 0xFF));
                BoardInnerGlow.Opacity = 0.65;
            }
            if (OppHandStripBorder != null)
                OppHandStripBorder.BorderThickness = new Thickness(0, 0, 0, 3);
            if (PlayerHandStripBorder != null)
                PlayerHandStripBorder.BorderThickness = new Thickness(0, 1, 0, 0);
        }
        else
        {
            BoardTintOverlay.Background = new SolidColorBrush(Color.FromRgb(0x14, 0x32, 0x1C));
            BoardTintOverlay.Opacity = 0.26;
            BoardFrameBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x50, 0xC0, 0x70));
            BoardFrameBorder.BorderThickness = new Thickness(3);
            if (BoardInnerGlow != null)
            {
                BoardInnerGlow.BorderBrush = new SolidColorBrush(Color.FromRgb(0x70, 0xE0, 0x90));
                BoardInnerGlow.Opacity = 0.65;
            }
            if (PlayerHandStripBorder != null)
                PlayerHandStripBorder.BorderThickness = new Thickness(0, 3, 0, 0);
            if (OppHandStripBorder != null)
                OppHandStripBorder.BorderThickness = new Thickness(0, 0, 0, 1);
        }
    }

    private Rectangle? _boardPlayCanvasGlow;

    private void SetBoardPlayHalo(bool on)
    {
        if (BoardPlayHalo != null)
            BoardPlayHalo.Visibility = on ? Visibility.Visible : Visibility.Collapsed;

        // Canvas-level glow so the legal-play area is visible while zoomed in
        // (frame halo alone sits outside the zoomed content and is easy to miss).
        if (on)
        {
            if (_boardPlayCanvasGlow == null)
            {
                _boardPlayCanvasGlow = new Rectangle
                {
                    IsHitTestVisible = false,
                    Stroke = new SolidColorBrush(Color.FromRgb(0x70, 0xE8, 0xFF)),
                    StrokeThickness = 6,
                    Fill = new SolidColorBrush(Color.FromArgb(40, 0, 180, 220)),
                    RadiusX = 8,
                    RadiusY = 8
                };
            }
            if (!TableCanvas.Children.Contains(_boardPlayCanvasGlow))
                TableCanvas.Children.Add(_boardPlayCanvasGlow);
            double w = TableCanvas.Width > 0 ? TableCanvas.Width : 2800;
            double h = TableCanvas.Height > 0 ? TableCanvas.Height : 1400;
            _boardPlayCanvasGlow.Width = w - 40;
            _boardPlayCanvasGlow.Height = h - 40;
            Canvas.SetLeft(_boardPlayCanvasGlow, 20);
            Canvas.SetTop(_boardPlayCanvasGlow, 20);
            Panel.SetZIndex(_boardPlayCanvasGlow, 5);
            _boardPlayCanvasGlow.Visibility = Visibility.Visible;
        }
        else if (_boardPlayCanvasGlow != null)
        {
            _boardPlayCanvasGlow.Visibility = Visibility.Collapsed;
        }

    }

    private void AddTargetHalo(Border host, Color color)
    {
        var rect = new Rectangle
        {
            Width = (host.Width > 0 ? host.Width : TableCardWidth) + 10,
            Height = (host.Height > 0 ? host.Height : TableCardHeight) + 10,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 3,
            Fill = new SolidColorBrush(Color.FromArgb(35, color.R, color.G, color.B)),
            IsHitTestVisible = false,
            RadiusX = 6,
            RadiusY = 6
        };
        Canvas.SetLeft(rect, Canvas.GetLeft(host) - 5);
        Canvas.SetTop(rect, Canvas.GetTop(host) - 5);
        Panel.SetZIndex(rect, 20);
        TableCanvas.Children.Add(rect);
        _targetHighlights.Add(rect);
    }

    private void ShowEventTargetHighlights(Card ev)
    {
        ClearEventTargetHighlights();
        var tk = EventRules.GetTargetKind(EventRules.ResolvePlay(ev));
        if (!EventRules.NeedsTableHost(tk)) return;

        var color = tk == EventRules.TargetKind.GapBetweenMissions
            ? Color.FromRgb(180, 120, 220)
            : tk == EventRules.TargetKind.Ship
                ? Color.FromRgb(80, 180, 220)
                : Color.FromRgb(220, 160, 60);

        if (tk == EventRules.TargetKind.GapBetweenMissions)
        {
            foreach (var (left, right) in ListSameQuadrantGaps())
            {
                double lx = Canvas.GetLeft(left) + TableCardWidth;
                double rx = Canvas.GetLeft(right);
                double x = (lx + rx) / 2.0 - TableCardWidth / 2.0;
                var rect = new Rectangle
                {
                    Width = TableCardWidth,
                    Height = TableCardHeight,
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 3 },
                    Fill = new SolidColorBrush(Color.FromArgb(50, color.R, color.G, color.B)),
                    IsHitTestVisible = false,
                    RadiusX = 4,
                    RadiusY = 4
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, SpacelineY);
                Panel.SetZIndex(rect, 8);
                TableCanvas.Children.Add(rect);
                _targetHighlights.Add(rect);
            }
            return;
        }

        var hosts = CollectEventTargets(ev, _activePlayer, tk, EventRules.ResolvePlay(ev).Place);
        foreach (var h in hosts)
        {
            var rect = new Rectangle
            {
                Width = (h.Width > 0 ? h.Width : TableCardWidth) + 10,
                Height = (h.Height > 0 ? h.Height : TableCardHeight) + 10,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false,
                RadiusX = 6,
                RadiusY = 6
            };
            Canvas.SetLeft(rect, Canvas.GetLeft(h) - 5);
            Canvas.SetTop(rect, Canvas.GetTop(h) - 5);
            Panel.SetZIndex(rect, 20);
            TableCanvas.Children.Add(rect);
            _targetHighlights.Add(rect);
        }
    }

    private void ClearEventTargetHighlights()
    {
        foreach (var r in _targetHighlights)
            TableCanvas.Children.Remove(r);
        _targetHighlights.Clear();
        SetBoardPlayHalo(false);
    }

    private Border? PickAdjacentMission(Border? from)
    {
        if (_eventPreferredHost2 != null)
        {
            var h2 = _eventPreferredHost2;
            _eventPreferredHost2 = null;
            return h2;
        }
        if (from == null || from.Tag is not Card fromCard) return null;
        int idx = IndexOfMission(from);
        if (idx < 0) return null;
        string q = GetNativeQuadrant(fromCard);
        var options = new List<Border>();
        if (idx + 1 < _spacelineOrder.Count
            && _spacelineOrder[idx + 1].Tag is Card r
            && GetNativeQuadrant(r) == q)
            options.Add(_spacelineOrder[idx + 1]);
        if (idx - 1 >= 0
            && _spacelineOrder[idx - 1].Tag is Card l
            && GetNativeQuadrant(l) == q)
            options.Add(_spacelineOrder[idx - 1]);
        if (options.Count == 0)
        {
            StatusText.Text = "No same-quadrant adjacent mission (cannot place Gaps/Q-Net at quadrant edge).";
            return null;
        }
        if (options.Count == 1) return options[0];
        return options[_autoSeedRng.Next(options.Count)];
    }

    private void ApplyInstantEvent(Card ev, int controller, EventRules.PlayResult r)
    {
        if (r.DrawCards > 0)
        {
            int who = ShowCardReveal(ev, ev.Name ?? "Event",
                "Choose which player draws 3 cards.",
                RevealButtons.PlayerPick) == RevealAnswer.Yes
                ? 1 : 2;
            bool timed = _revealTimedOut;
            int saved = _activePlayer;
            _activePlayer = who;
            for (int i = 0; i < r.DrawCards; i++)
                DrawOneToHand();
            _activePlayer = saved;
            ShowActivePlayerHand();
            AnnounceChoiceResult(ev, ev.Name ?? "Event",
                $"Player {who} draws {r.DrawCards} card(s)."
                + (timed ? "\n(No choice in time — default Player 1.)" : ""));
            _session.Log.Add(_session.TurnNumber, $"P{controller}",
                $"{ev.Name}: P{who} draws {r.DrawCards}");
        }
        if (r.Masaka)
        {
            // Default on timeout: the controller (who played the event)
            var pick = ShowCardReveal(
                ev, "Masaka Transformations",
                "Choose which player's hand is placed under their draw deck and redrawn (same number).",
                RevealButtons.PlayerPick,
                ev.Name,
                autoDefault: controller == 1 ? RevealAnswer.Yes : RevealAnswer.No);
            bool timed = _revealTimedOut;
            int who = pick == RevealAnswer.Yes ? 1 : 2;
            var hand = who == 1 ? _handCards : _oppHandCards;
            var draw = who == 1 ? _drawCards : _oppDrawCards;
            int n = hand.Count;
            foreach (var c in hand.ToList())
                draw.Add(c);
            hand.Clear();
            for (int i = 0; i < n && draw.Count > 0; i++)
            {
                hand.Add(draw[0]);
                draw.RemoveAt(0);
            }
            ShowActivePlayerHand();
            RefreshZoneCounts();
            StatusText.Text = $"Masaka Transformations: Player {who} ({n} cards).";
            _session.Log.Add(_session.TurnNumber, $"P{controller}", $"Masaka on P{who} ({n})");
            AnnounceChoiceResult(ev, "Masaka Transformations",
                $"Effect applied to Player {who} ({n} cards redrawn)."
                + (timed ? "\n(No choice in time — default: the player who played this event.)" : ""));
        }
        if (r.ResQ)
        {
            var disc = controller == 1 ? _discardCards : _oppDiscardCards;
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (disc.Count == 0)
            {
                StatusText.Text = "Res-Q: Discard leer.";
                return;
            }
            Card? pick = null;
            foreach (var c in disc.ToList())
            {
                if (ShowCardReveal(c, "Res-Q", $"{c.Name} auf die Hand?", RevealButtons.YesNo) == RevealAnswer.Yes)
                {
                    pick = c;
                    break;
                }
            }
            pick ??= disc[^1];
            disc.Remove(pick);
            hand.Add(pick);
            ShowActivePlayerHand();
            RefreshZoneCounts();
        }
        RefreshZoneCounts();
    }

    private void ApplySupernova(Border mission)
    {
        foreach (var dock in GetDockablesUnderMission(mission).ToList())
        {
            if (dock.Tag is Card c)
                DestroyShipOrFacility(dock, c, GetBorderOwner(dock) == 0 ? 1 : GetBorderOwner(dock));
        }
        _solvedMissions.Add(mission); // unattemptable
        StatusText.Text = $"Supernova at {(mission.Tag as Card)?.Name}.";
    }

    /// <summary>Hand-Play von Artifacts die als Event/Interrupt wirken. true = erledigt.</summary>
    private bool TryResolveArtifactHandPlay(Card art, int controller)
    {
        string n = (art.Name ?? "").Trim();
        switch (n)
        {
            case "Vulcan Stone of Gol":
                {
                    // Away Team an gewählter Planet-Mission: ohne Youth und CUNNING≤7 sterben
                    var planets = TableCanvas.Children.OfType<Border>()
                        .Where(b => b.Tag is Card c
                                    && string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase)
                                    && MissionRules.IsPlanetMission(c))
                        .ToList();
                    if (planets.Count == 0)
                    {
                        StatusText.Text = "Stone of Gol: keine Planet-Mission.";
                        SendCardTo(art, controller, TimingRules.Destination.Discard);
                        return true;
                    }
                    Border target = planets[0];
                    foreach (var p in planets)
                    {
                        if (ShowCardReveal(art, "Stone of Gol – Ziel?",
                                $"Attack away team at {(p.Tag as Card)?.Name}?",
                                RevealButtons.YesNo) == RevealAnswer.Yes)
                        {
                            target = p;
                            break;
                        }
                    }
                    int kills = 0;
                    if (_stackOnHost.TryGetValue(target, out var team))
                    {
                        foreach (var b in team.ToList())
                        {
                            if (b.Tag is not Card p || !ModifierRules.IsPersonnelCard(p)) continue;
                            int own = GetBorderOwner(b); if (own == 0) own = 1;
                            // Gegner-Away-Team typisch – hier alle present ohne Youth / CUNNING≤7
                            var ep = ModifierRules.ResolvePersonnel(p, team.Select(x => x.Tag).OfType<Card>(), own);
                            bool youth = ep.Skills.Keys.Any(k =>
                                k.Equals("Youth", StringComparison.OrdinalIgnoreCase));
                            if (!youth && ep.Cunning <= 7)
                            {
                                DiscardPersonnelBorder(b, p, own);
                                kills++;
                            }
                        }
                    }
                    SendCardTo(art, controller, TimingRules.Destination.Discard);
                    StatusText.Text = $"Stone of Gol: {kills} personnel killed.";
                    _session.Log.Add(_session.TurnNumber, $"P{controller}", $"Stone of Gol kills {kills}");
                    return true;
                }
            case "Thought Maker":
                {
                    var types = new[] { "Personnel", "Ship", "Event", "Interrupt", "Equipment", "Dilemma", "Doorway" };
                    string chosen = types[0];
                    foreach (var t in types)
                    {
                        if (ShowCardReveal(art, "Thought Maker", $"Choose card type {t}?",
                                RevealButtons.YesNo) == RevealAnswer.Yes)
                        {
                            chosen = t;
                            break;
                        }
                    }
                    var oppDraw = controller == 1 ? _oppDrawCards : _drawCards;
                    var moved = oppDraw
                        .Where(c => (c.Type ?? "").Contains(chosen, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (var c in moved)
                        oppDraw.Remove(c);
                    // shuffle to bottom
                    foreach (var c in moved.OrderBy(_ => Guid.NewGuid()))
                        oppDraw.Add(c);
                    SendCardTo(art, controller, TimingRules.Destination.Discard);
                    StatusText.Text = $"Thought Maker: {moved.Count}× {chosen} im Gegner-Draw nach unten.";
                    RefreshZoneCounts();
                    return true;
                }
            case "Kurlan Naiskos":
                {
                    // Auf eigenes Schiff legen – Attribute ×3 wenn voll staffed
                    Border? shipB = null;
                    foreach (var b in TableCanvas.Children.OfType<Border>())
                    {
                        if (b.Tag is not Card sc || !IsShipCard(sc)) continue;
                        if (GetBorderOwner(b) != controller) continue;
                        if (ShowCardReveal(art, "Kurlan Naiskos",
                                $"Auf {sc.Name} spielen?", RevealButtons.YesNo) == RevealAnswer.Yes)
                        {
                            shipB = b;
                            break;
                        }
                    }
                    if (shipB == null)
                    {
                        StatusText.Text = "Kurlan: no ship chosen → kept in hand.";
                        var hand = controller == 1 ? _handCards : _oppHandCards;
                        if (!hand.Contains(art)) hand.Add(art);
                        return true;
                    }
                    AttachCardToHost(art, shipB, controller);
                    StatusText.Text = "Kurlan Naiskos auf Schiff (Attribute ×3 wenn alle Classifications).";
                    return true;
                }
            case "Tox Uthat":
                CommitCardToTable(art, controller);
                StatusText.Text = "Tox Uthat on table (Supernova protection / nullify).";
                return true;
            default:
                return false;
        }
    }

    private void ApplyArtifactAcquire(Card art, Border missionBorder, Card mission)
    {
        var acq = ArtifactRules.ResolveAcquire(art);
        ShowCardReveal(art, "Artifact verdient",
            (art.Text ?? "") + "\n\n→ " + acq.Message,
            RevealButtons.Ok, art.Name);

        switch (acq.Kind)
        {
            case ArtifactRules.AcquireKind.ImmediateDiscard:
                if (acq.DownloadFromDraw > 0)
                {
                    var draw = _activePlayer == 1 ? _drawCards : _oppDrawCards;
                    var hand = _activePlayer == 1 ? _handCards : _oppHandCards;
                    int n = Math.Min(acq.DownloadFromDraw, draw.Count);
                    for (int i = 0; i < n; i++)
                    {
                        hand.Add(draw[0]);
                        draw.RemoveAt(0);
                    }
                    ShowActivePlayerHand();
                    RefreshZoneCounts();
                    StatusText.Text = $"Betazoid Gift Box: {n} card(s) vom Draw → Hand.";
                }
                // Artifact discarded (nicht ins Spiel)
                {
                    var disc = _activePlayer == 2 ? _oppDiscardCards : _discardCards;
                    if (!disc.Contains(art)) disc.Add(art);
                }
                break;

            case ArtifactRules.AcquireKind.PlaceOnTable:
                if (acq.GrantsHorgahn)
                {
                    if (_activePlayer == 1) _horgahnP1 = true;
                    else _horgahnP2 = true;
                }
                CommitCardToTable(art, _activePlayer);
                StatusText.Text = acq.Message;
                break;

            case ArtifactRules.AcquireKind.UseAsEquipment:
                // Am Away Team (Planet) bzw. auf erstes Schiff (Space) ablegen
                Border host = missionBorder;
                if (!MissionRules.IsPlanetMission(mission))
                {
                    foreach (var dock in GetDockablesUnderMission(missionBorder))
                    {
                        if (dock.Tag is Card dc && IsShipCard(dc) && GetBorderOwner(dock) == _activePlayer)
                        {
                            host = dock;
                            break;
                        }
                    }
                }
                AttachCardToHost(art, host, _activePlayer);
                StatusText.Text = acq.Message;
                break;

            default: // ToHand
                if (_activePlayer == 1) _handCards.Add(art);
                else _oppHandCards.Add(art);
                ShowActivePlayerHand();
                StatusText.Text = acq.Message;
                break;
        }

        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"Artifact {art.Name}: {acq.Kind}");
        RefreshZoneCounts();
    }

    /// <summary>Artifact/Equipment als Border auf Host-Stapel legen.</summary>
    private void RemoveCardFromTableColumn(Card card)
    {
        _tablePermanentCards.Remove(card);
        _oppTablePermanentCards.Remove(card);
        RebuildTablePermanentsPanel();
    }

    private void AttachCardToHost(Card card, Border host, int owner)
    {
        RemoveCardFromTableColumn(card);
        if (!_stackOnHost.TryGetValue(host, out var list))
        {
            list = new List<Border>();
            _stackOnHost[host] = list;
        }
        var mini = CreateMiniCard(card, faceDown: false);
        SetBorderOwner(mini, owner);
        list.Add(mini);
        if (!TableCanvas.Children.Contains(mini))
            TableCanvas.Children.Add(mini);
        // Stack cards are hidden on the canvas; only the host + badge are visible.
        mini.Visibility = Visibility.Collapsed;
        try
        {
            Canvas.SetLeft(mini, Canvas.GetLeft(host) + 12);
            Canvas.SetTop(mini, Canvas.GetTop(host) + 12);
        }
        catch { }
    }

    private bool HasHorgahn(int player) => player == 1 ? _horgahnP1 : _horgahnP2;

    /// <summary>
    /// The Traveler: Transcendence nullifies Static Warp Bubble while in play (both stay on table).
    /// </summary>
    private bool IsTravelerInPlay()
    {
        if (_attachedEvents.Any(e => e.Kind == EventRules.Persist.Traveler))
            return true;
        bool IsTraveler(Card c) =>
            (c.Name ?? "").Equals("The Traveler: Transcendence", StringComparison.OrdinalIgnoreCase);
        return _tablePermanentCards.Any(IsTraveler) || _oppTablePermanentCards.Any(IsTraveler);
    }

    private bool HasRedAlert(int player)
    {
        if (_attachedEvents.Any(e => e.Kind == EventRules.Persist.RedAlert && e.Owner == player))
            return true;
        var table = player == 2 ? _oppTablePermanentCards : _tablePermanentCards;
        return table.Any(c => (c.Name ?? "").Equals("Red Alert!", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Premiere Red Alert! has no countdown and is not "activated".
    /// Each of the owner's turns: replace the single normal card play with up to 5
    /// personnel/equipment reports from hand. Playing the event itself is this turn's play.
    /// </summary>
    private string FormatRedAlertStatusLine(Card card)
    {
        int owner = 0;
        var ae = _attachedEvents.FirstOrDefault(e =>
            e.Kind == EventRules.Persist.RedAlert && ReferenceEquals(e.Card, card));
        if (ae != null) owner = ae.Owner;
        if (owner is not (1 or 2))
        {
            if (_tablePermanentCards.Contains(card)) owner = 1;
            else if (_oppTablePermanentCards.Contains(card)) owner = 2;
        }
        bool onTable = HasRedAlert(owner == 0 ? _activePlayer : owner);
        var lines = new List<string>
        {
            "Red Alert! stays on table (no Use button).",
            "Each of your turns: instead of 1 normal card play, drag up to 5 personnel/equipment from hand onto a legal Outpost/HQ."
        };
        if (!onTable)
        {
            lines.Add("Not in play.");
            return string.Join("\n", lines);
        }
        if (owner != 0 && owner != _session.ActivePlayer)
        {
            lines.Add($"Owner P{owner} — waiting for that player's turn.");
            return string.Join("\n", lines);
        }
        if (_redAlertPlaysLeft > 0)
            lines.Add($"THIS TURN: {_redAlertPlaysLeft} / 5 personnel or equipment still allowed.");
        else if (_session.NormalCardPlayUsed)
            lines.Add("THIS TURN: normal card play already used (or the 5 reports are finished). Next turn: 5 again.");
        else
            lines.Add("THIS TURN: 5 reports available after your turn starts with Red Alert already in play.");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Premiere Red Alert!: in place of your normal card play, up to 5 personnel and/or equipment.
    /// Refreshed at the start of that player's turn if the event is still on table.
    /// </summary>
    private void RefreshRedAlertForTurn()
    {
        _redAlertPlaysLeft = HasRedAlert(_session.ActivePlayer) ? 5 : 0;
        if (_redAlertPlaysLeft > 0)
        {
            _session.Log.Add(_session.TurnNumber, $"P{_session.ActivePlayer}",
                "Red Alert: up to 5 personnel/equipment instead of normal card play");
        }
    }

    /// <summary>
    /// Visual card picker (same overlay as Static Warp Bubble). No timer — click a card.
    /// </summary>
    private Card? PickCardFromList(string prompt, IReadOnlyList<Card> pool, string title)
    {
        if (pool.Count == 0) return null;
        if (pool.Count == 1)
        {
            ShowCardReveal(pool[0], title, $"{prompt}\n\nOnly option: {pool[0].Name}.", RevealButtons.Ok);
            return pool[0];
        }

        if (KidnapOverlay == null)
        {
            // Fallback without MessageBox: first card
            ShowCardReveal(pool[0], title, prompt + $"\n\nSelected: {pool[0].Name}", RevealButtons.Ok);
            return pool[0];
        }

        _handPickMandatory = true;
        _handPickResult = null;
        _handPickTimedOut = false;
        _kidnapResolved = false;
        _kidnapHand = pool.ToList();

        KidnapTitle.Text = title;
        KidnapHint.Text = prompt + "\nClick a card to choose.";
        KidnapTypePanel.Children.Clear();
        KidnapCardsPanel.Children.Clear();
        if (BtnKidnapCancel != null)
            BtnKidnapCancel.Visibility = Visibility.Collapsed;

        foreach (var c in _kidnapHand)
        {
            var mini = CreateMiniCard(c, faceDown: false);
            mini.Width = 96;
            mini.Height = 134;
            Card cardRef = c;
            mini.MouseLeftButtonDown += (_, ev) =>
            {
                if (_kidnapResolved) return;
                _kidnapResolved = true;
                _handPickResult = cardRef;
                if (_kidnapFrame != null) _kidnapFrame.Continue = false;
                ev.Handled = true;
            };
            KidnapCardsPanel.Children.Add(mini);
        }

        KidnapOverlay.Visibility = Visibility.Visible;
        _kidnapFrame = new System.Windows.Threading.DispatcherFrame();
        try { System.Windows.Threading.Dispatcher.PushFrame(_kidnapFrame); }
        finally
        {
            _kidnapFrame = null;
            KidnapOverlay.Visibility = Visibility.Collapsed;
            _handPickMandatory = false;
            if (BtnKidnapCancel != null) BtnKidnapCancel.Visibility = Visibility.Visible;
        }

        var chosen = _handPickResult ?? pool[0];
        ShowCardReveal(chosen, title, $"{prompt}\n\nChosen: {chosen.Name}", RevealButtons.Ok);
        return chosen;
    }

    private void AwardDilemmaPoints(int pts)
    {
        if (pts <= 0) return;
        if (_activePlayer == 1) _scoreP1 += pts;
        else _scoreP2 += pts;
        if (ScoreText != null)
            ScoreText.Text = $"P1: {_scoreP1}   ·   P2: {_scoreP2}";
    }

    private static string FormatDilemmaVictims(DilemmaRules.Result r)
    {
        if (r.Kill == null || r.Kill.Count == 0) return "";
        bool stasis = r.Persist == DilemmaRules.PersistKind.Phased
                      || r.Persist == DilemmaRules.PersistKind.Abduction;
        string names = string.Join(", ",
            r.Kill.Where(c => c != null).Select(c => c.Name ?? "?").Distinct());
        if (names.Length == 0) return "";
        if (stasis)
            return r.Kill.Count == 1
                ? $"Held / relocated: {names}."
                : $"Held / relocated: {names}.";
        bool eq = r.Kill.All(c => c != null && ModifierRules.IsEquipmentCard(c));
        if (eq)
            return r.Kill.Count == 1
                ? $"Destroyed: {names}."
                : $"Destroyed: {names}.";
        return r.Kill.Count == 1
            ? $"Killed: {names}."
            : $"Killed: {names}.";
    }

    private void ApplyDilemmaResult(
        DilemmaRules.Result r,
        Card seedCard,
        Border missionBorder,
        Card mission,
        List<Border> teamBorders,
        Border? shipBorder,
        List<Border> seedStack)
    {
        // Kills / Equipment destroy
        bool stasis = r.Persist == DilemmaRules.PersistKind.Phased
                      || r.Persist == DilemmaRules.PersistKind.Abduction;
        foreach (var victim in r.Kill.ToList())
        {
            var b = teamBorders.FirstOrDefault(x => x.Tag is Card c && ReferenceEquals(c, victim));
            if (stasis)
            {
                if (b != null) MarkStopped(b); // Stasis-Sandbox: gestoppt
                continue;
            }
            if (victim == null) continue;
            if (b != null)
                DiscardPersonnelBorder(b, victim, _activePlayer);
            else if (ModifierRules.IsEquipmentCard(victim) && shipBorder != null)
                RemoveEquipmentFromHost(shipBorder, victim);
            else if (ModifierRules.IsEquipmentCard(victim))
                RemoveEquipmentFromHost(missionBorder, victim);
        }

        if (r.Relocate != null && r.Persist != DilemmaRules.PersistKind.Abduction)
            RelocatePersonnelToFurthestPlanet(r.Relocate, missionBorder);

        if (r.DamageShip && shipBorder != null && shipBorder.Tag is Card)
            ApplyHullDamage(shipBorder, (Card)shipBorder.Tag!, Math.Min(100, GetHullDamage(shipBorder) + 50));

        if (r.DestroyShip && shipBorder != null && shipBorder.Tag is Card sc)
            DestroyShipOrFacility(shipBorder, sc, _activePlayer);

        if (r.DiscardNonPersonnelFromHand.Count > 0)
        {
            var hand = _activePlayer == 1 ? _handCards : _oppHandCards;
            int n = 0;
            foreach (var c in r.DiscardNonPersonnelFromHand.ToList())
            {
                if (!hand.Remove(c)) continue;
                var disc = _activePlayer == 2 ? _oppDiscardCards : _discardCards;
                if (!disc.Contains(c)) disc.Add(c);
                n++;
            }
            if (r.DrawForDiscarded)
                for (int i = 0; i < n; i++)
                    DrawOneToHand();
            RefreshZoneCounts();
        }

        bool removeFromSeed = r.Fate is DilemmaRules.Fate.EffectAndEnd
                              or DilemmaRules.Fate.AttachAndEnd
                              or DilemmaRules.Fate.EndAttempt
                              or DilemmaRules.Fate.Overcome;
        if (r.Fate == DilemmaRules.Fate.WallFailed)
            removeFromSeed = false;

        if (removeFromSeed && seedStack.Count > 0)
        {
            // Track overcome/removed seeds for Temporal Causality Loop re-seed
            if (_attemptMission != null && ReferenceEquals(_attemptMission, missionBorder)
                && (r.Fate == DilemmaRules.Fate.Overcome
                    || (r.Fate == DilemmaRules.Fate.EffectAndEnd
                        && !(seedCard.Name ?? "").Equals("Temporal Causality Loop", StringComparison.OrdinalIgnoreCase))))
            {
                _attemptDiscards.Add((seedCard, _activePlayer, missionBorder, wasSeed: true));
            }
            // Prefer remove by card identity (not a stale index)
            int ri = seedStack.FindLastIndex(b => b.Tag is Card c && ReferenceEquals(c, seedCard));
            if (ri < 0) ri = seedStack.Count - 1;
            if (ri >= 0 && ri < seedStack.Count)
                seedStack.RemoveAt(ri);
            _seedUnderMission[missionBorder] = seedStack;
            UpdateSeedBadge(missionBorder);
        }

        // Temporal Causality Loop fail: restore cards discarded this attempt, re-seed seeds
        if ((seedCard.Name ?? "").Equals("Temporal Causality Loop", StringComparison.OrdinalIgnoreCase)
            && r.Fate == DilemmaRules.Fate.EffectAndEnd)
        {
            ApplyTemporalCausalityLoopRestore(missionBorder, seedCard);
        }

        if (r.Fate == DilemmaRules.Fate.AttachAndEnd)
        {
            Border host = r.Persist is DilemmaRules.PersistKind.Scow or DilemmaRules.PersistKind.BorgShip
                          or DilemmaRules.PersistKind.Abduction or DilemmaRules.PersistKind.Phased
                          or DilemmaRules.PersistKind.HyperAging
                ? missionBorder
                : (shipBorder ?? missionBorder);
            if (r.Persist == DilemmaRules.PersistKind.BorgShip)
            {
                host = FindFurthestMission(missionBorder) ?? missionBorder;
                // One-way trip: from furthest end back toward the encounter end, then off the spaceline
                int farIdx = _spacelineOrder.IndexOf(host);
                int nearIdx = _spacelineOrder.IndexOf(missionBorder);
                if (farIdx < 0) farIdx = _spacelineOrder.Count - 1;
                if (nearIdx < 0) nearIdx = 0;
                _borgShipDir = farIdx >= nearIdx ? -1 : 1;
            }
            _attachedDilemmas.Add(new AttachedDilemma
            {
                Card = seedCard,
                Kind = r.Persist,
                Countdown = r.Countdown,
                Host = host,
                Extra = r.Relocate
            });
            if (r.Persist == DilemmaRules.PersistKind.BorgShip)
            {
                PlaceBorgShipToken(seedCard, host);
                ShowCardReveal(seedCard, "Borg Ship",
                    $"Placed at furthest end: {(host.Tag as Card)?.Name}.\n"
                    + "End of every turn: attacks all ships here (WEAPONS 24), then moves one mission "
                    + "toward the opposite end and off the spaceline (leaves play).\n"
                    + "Destroy it in ship battle (same location) for 15 points.",
                    RevealButtons.Ok, seedCard.Name);
            }
            _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                $"Attached {seedCard.Name} ({r.Persist}) @ {(host.Tag as Card)?.Name}");
        }

        if (r.Score > 0 && r.Fate != DilemmaRules.Fate.Overcome)
            AwardDilemmaPoints(r.Score);
    }

    /// <summary>
    /// Temporal Causality Loop fail: return personnel/equipment discarded from this attempt
    /// and re-seed overcome seed cards under the mission; TCL itself is discarded.
    /// </summary>
    private void ApplyTemporalCausalityLoopRestore(Border missionBorder, Card loopCard)
    {
        int restored = 0;
        foreach (var (card, owner, returnHost, wasSeed) in _attemptDiscards.ToList())
        {
            var disc = owner == 2 ? _oppDiscardCards : _discardCards;
            disc.Remove(card);

            if (wasSeed)
            {
                var mini = CreateMiniCard(card, faceDown: true);
                SetBorderOwner(mini, owner);
                mini.Visibility = Visibility.Collapsed;
                if (!_seedUnderMission.TryGetValue(missionBorder, out var seeds))
                {
                    seeds = new List<Border>();
                    _seedUnderMission[missionBorder] = seeds;
                }
                // Re-seed under (bottom of encounter order = top of list for our Add-at-end scheme: insert at 0)
                seeds.Insert(0, mini);
                if (!TableCanvas.Children.Contains(mini))
                    TableCanvas.Children.Add(mini);
                restored++;
                continue;
            }

            Border host = returnHost ?? missionBorder;
            if (!TableCanvas.Children.Contains(host))
                host = missionBorder;
            if (!_stackOnHost.TryGetValue(host, out var list))
            {
                list = new List<Border>();
                _stackOnHost[host] = list;
            }
            var b = CreateMiniCard(card, faceDown: false);
            SetBorderOwner(b, owner);
            b.Visibility = Visibility.Collapsed;
            list.Add(b);
            if (!TableCanvas.Children.Contains(b))
                TableCanvas.Children.Add(b);
            restored++;
        }
        _attemptDiscards.Clear();
        UpdateSeedBadge(missionBorder);
        // Loop dilemma is discarded (already removed from seed as EffectAndEnd)
        var loopDisc = _activePlayer == 2 ? _oppDiscardCards : _discardCards;
        if (!loopDisc.Contains(loopCard))
            loopDisc.Add(loopCard);
        RefreshZoneCounts();
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"Temporal Causality Loop: restored {restored} card(s); turn ends");
        StatusText.Text = $"Temporal Causality Loop: {restored} card(s) returned; turn ends.";
        _attemptMission = null;
    }

    private void RemoveEquipmentFromHost(Border host, Card eq)
    {
        if (!_stackOnHost.TryGetValue(host, out var list)) return;
        var b = list.FirstOrDefault(x => x.Tag is Card c && ReferenceEquals(c, eq));
        if (b == null) return;
        list.Remove(b);
        var disc = _activePlayer == 2 ? _oppDiscardCards : _discardCards;
        if (!disc.Contains(eq)) disc.Add(eq);
        if (TableCanvas.Children.Contains(b)) TableCanvas.Children.Remove(b);
        RefreshZoneCounts();
    }

    private void RelocatePersonnelToFurthestPlanet(Card person, Border fromMission)
    {
        Border? dest = FindFurthestPlanet(fromMission);
        if (dest == null) return;
        Border? srcHost = null;
        Border? cardB = null;
        foreach (var kv in _stackOnHost)
        {
            var found = kv.Value.FirstOrDefault(b => b.Tag is Card c && ReferenceEquals(c, person));
            if (found == null) continue;
            srcHost = kv.Key;
            cardB = found;
            break;
        }
        if (srcHost == null || cardB == null) return;
        _stackOnHost[srcHost].Remove(cardB);
        if (!_stackOnHost.TryGetValue(dest, out var destList))
        {
            destList = new List<Border>();
            _stackOnHost[dest] = destList;
        }
        destList.Add(cardB);
        StatusText.Text = $"{person.Name} relocatiert nach {(dest.Tag as Card)?.Name}.";
    }

    private Border? FindFurthestPlanet(Border from)
    {
        var missions = TableCanvas.Children.OfType<Border>()
            .Where(b => b.Tag is Card c && string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase)
                        && MissionRules.IsPlanetMission(c) && !ReferenceEquals(b, from))
            .ToList();
        if (missions.Count == 0) return null;
        double fx = Canvas.GetLeft(from);
        return missions.OrderByDescending(b => Math.Abs(Canvas.GetLeft(b) - fx)).First();
    }

    private Border? FindFurthestMission(Border from)
    {
        var missions = TableCanvas.Children.OfType<Border>()
            .Where(b => b.Tag is Card c && string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase)
                        && !ReferenceEquals(b, from))
            .ToList();
        if (missions.Count == 0) return from;
        double fx = Canvas.GetLeft(from);
        return missions.OrderByDescending(b => Math.Abs(Canvas.GetLeft(b) - fx)).First();
    }

    /// <summary>
    /// Nach fehlgeschlagenem Dilemma: alle beteiligten Personal-Borders stoppen;
    /// bei Space-Mission zusätzlich die eigenen Schiffe, die Crew gestellt haben.
    /// </summary>
    private void StopMissionAttemptTeam(Border missionBorder, Card mission, List<Border> teamBorders)
    {
        foreach (var b in teamBorders)
            MarkStopped(b);

        if (!MissionRules.IsPlanetMission(mission))
        {
            // Space: Schiffe mit beteiligter Crew stoppen (Crew boarding stopped ship → stopped)
            foreach (var dock in GetDockablesUnderMission(missionBorder))
            {
                if (dock.Tag is not Card dc || !IsShipCard(dc)) continue;
                if (GetBorderOwner(dock) != _activePlayer) continue;
                if (IsBorderStopped(dock)) continue;
                // Nur stoppen, wenn mindestens ein Crew-Mitglied im Team war
                if (!_stackOnHost.TryGetValue(dock, out var stacked)) continue;
                bool involved = stacked.Any(sb => teamBorders.Contains(sb));
                if (involved)
                {
                    MarkStopped(dock);
                    StopCrewOnHost(dock);
                }
            }
        }

        ShowCardReveal(mission, "Dilemma — team stopped",
            "Mission attempt failed.\n\n"
            + "The involved away team / crew is stopped"
            + (MissionRules.IsPlanetMission(mission) ? "." : " (and the ship, on space missions).")
            + "\nStopped ends automatically at the start of that player's next turn.",
            RevealButtons.Ok, mission.Name);
    }

    /// <summary>Ungestopptes eigenes Personal an der Mission (Borders).</summary>
    private List<Border> CollectTeamBordersAtMission(Border missionBorder, Card mission)
    {
        var borders = new List<Border>();
        bool planet = MissionRules.IsPlanetMission(mission);
        if (planet)
        {
            if (_stackOnHost.TryGetValue(missionBorder, out var away))
            {
                foreach (var b in away)
                {
                    if (b.Tag is not Card c) continue;
                    if (CardOwner(b) != _activePlayer) continue;
                    if (IsBorderStopped(b)) continue;
                    if (IsCrewType(c) || (c.Type ?? "").Contains("personnel", StringComparison.OrdinalIgnoreCase))
                        borders.Add(b);
                }
            }
        }
        else
        {
            foreach (var dock in GetDockablesUnderMission(missionBorder))
            {
                if (dock.Tag is not Card dc || !IsShipCard(dc)) continue;
                if (GetBorderOwner(dock) != _activePlayer) continue;
                if (IsBorderStopped(dock)) continue;
                if (!_stackOnHost.TryGetValue(dock, out var stacked)) continue;
                foreach (var sb in stacked)
                {
                    if (sb.Tag is not Card c) continue;
                    if (IsBorderStopped(sb)) continue;
                    if (IsCrewType(c) || (c.Type ?? "").Contains("personnel", StringComparison.OrdinalIgnoreCase))
                        borders.Add(sb);
                }
            }
        }
        return borders;
    }

    /// <summary>
    /// Personal + Equipment des aktiven Spielers an der Mission (für Modifier/Mission).
    /// </summary>
    private List<Card> CollectPresentAtMission(Border missionBorder, Card mission)
    {
        var cards = new List<Card>();
        bool planet = MissionRules.IsPlanetMission(mission);
        void AddFromHost(Border host)
        {
            if (!_stackOnHost.TryGetValue(host, out var stacked)) return;
            foreach (var sb in stacked)
            {
                if (sb.Tag is not Card c) continue;
                if (CardOwner(sb) != _activePlayer) continue;
                if (IsBorderStopped(sb)) continue;
                if (IsCrewType(c) || IsEquipmentType(c)
                    || (c.Type ?? "").Contains("personnel", StringComparison.OrdinalIgnoreCase))
                    cards.Add(c);
            }
        }

        if (planet)
            AddFromHost(missionBorder);
        else
        {
            foreach (var dock in GetDockablesUnderMission(missionBorder))
            {
                if (dock.Tag is not Card dc || !IsShipCard(dc)) continue;
                if (GetBorderOwner(dock) != _activePlayer) continue;
                if (IsBorderStopped(dock)) continue;
                AddFromHost(dock);
            }
        }
        return cards;
    }

    private List<Card> CollectTeamAtMission(Border missionBorder, Card mission)
    {
        // Für Solve/Dilemma: present inkl. Equipment
        return CollectPresentAtMission(missionBorder, mission);
    }

    /// <summary>
    /// QoL: Missionen auf der Spaceline markieren, deren Requirements das Team erfüllen würde.
    /// Grün = Skills ok + keine Dilemmas; Gelb = Skills ok, noch Dilemmas; sonst nicht markiert.
    /// </summary>
    private void HighlightSolvableMissions(IEnumerable<Card> team)
    {
        ClearTargetHighlights();
        var teamList = team.ToList();
        if (teamList.Count == 0)
        {
            ShowPlayError("No team to check.");
            return;
        }

        int green = 0, yellow = 0;
        foreach (var mb in _spacelineOrder)
        {
            if (mb.Tag is not Card mission) continue;
            if (_solvedMissions.Contains(mb)) continue;
            int dil = _seedUnderMission.TryGetValue(mb, out var seeds) ? seeds.Count : 0;
            int missionOwner = GetBorderOwner(mb);
            var check = MissionRules.CanSolve(mission, teamList, dilemmasRemaining: 0,
                attemptingPlayer: _activePlayer, missionOwner: missionOwner);
            if (!check.Ok) continue;
            if (dil > 0)
            {
                AddTargetHighlight(mb, Color.FromArgb(100, 220, 180, 40)); // gelb
                yellow++;
            }
            else
            {
                AddTargetHighlight(mb, Color.FromArgb(100, 60, 200, 100)); // grün
                green++;
            }
        }

        StatusText.Text =
            $"Team skills vs spaceline: {green} solvable now (green), {yellow} with dilemmas (yellow). " +
            "Includes affiliation + owner/opponent-side requirements. Right-click clears highlights.";
    }


    private void BeginBeamMode(Border hostBorder)
    {
        if (hostBorder.Tag is not Card hostCard) return;
        _actionSourceHost = hostBorder;
        ClearTargetHighlights();

        bool sourceIsMission = string.Equals(hostCard.Type, "Mission", StringComparison.OrdinalIgnoreCase);
        if (!_stackOnHost.TryGetValue(hostBorder, out var crew) || crew.Count == 0)
        {
            ShowPlayError("No personnel on this host to beam.");
            return;
        }

        // Bei Mission: nur eigenes Away Team zählen
        int myCrew = crew.Count(b => CardOwner(b) == _activePlayer);
        if (sourceIsMission && myCrew == 0)
        {
            ShowPlayError("No unstopped away team of yours at this mission.");
            return;
        }

        _cardActionMode = CardActionMode.BeamPickTarget;
        var mission = sourceIsMission ? hostBorder : FindMissionForDockable(hostBorder);
        int owner = _activePlayer;
        var targets = new List<Border>();
        if (mission != null)
        {
            foreach (var dock in GetDockablesUnderMission(mission))
            {
                if (ReferenceEquals(dock, hostBorder)) continue;
                int o = GetBorderOwner(dock);
                if (o != owner) continue; // eigene Schiffe/Outposts
                targets.Add(dock);
            }
            // Planet als Ziel, wenn Quelle Schiff/Outpost ist
            if (!sourceIsMission)
                targets.Add(mission);
        }

        foreach (var t in targets.Distinct())
            AddTargetHighlight(t, Color.FromArgb(100, 80, 220, 120));

        if (targets.Count == 0)
        {
            ShowPlayError("No beam targets at this location (your ship/outpost?).");
            _cardActionMode = CardActionMode.None;
            return;
        }

        _beamSelected.Clear();
        // Standard: alle eigenen Crew-Karten vorauswählen
        foreach (var b in crew)
        {
            if (CardOwner(b) == _activePlayer) _beamSelected.Add(b);
        }
        StatusText.Text =
            $"BEAM: Check cards in the hand strip, then click a highlighted destination. " +
            $"Selected: {_beamSelected.Count}. Right-click = cancel.";
        ShowHostContents(hostBorder, hostCard, beamSelectMode: true);
    }

    private void BeginFlyHighlight(Border shipBorder, Card ship)
    {
        if (IsBorderStopped(shipBorder))
        {
            ShowPlayError("Gestopptes Schiff kann nicht fliegen.");
            return;
        }
        if (_attachedDilemmas.Any(a =>
                ReferenceEquals(a.Host, shipBorder)
                && a.Kind is DilemmaRules.PersistKind.Menthar
                    or DilemmaRules.PersistKind.TwoDim))
        {
            ShowPlayError("Ship cannot move due to a dilemma (Menthar / 2D-Creatures).");
            return;
        }

        _actionSourceHost = shipBorder;
        ClearTargetHighlights();

        var fromMission = FindMissionForDockable(shipBorder);
        int fromIdx = IndexOfMission(fromMission);
        if (fromIdx < 0)
        {
            ShowPlayError("Schiff ist keiner Mission zugeordnet.");
            return;
        }

        var crew = GetCrewOnShip(shipBorder);
        var staff = MovementRules.IsShipStaffed(ship, crew, GetActiveTreaties(_activePlayer));
        if (!staff.Ok && !ShipStaffedByRogueBorg(shipBorder))
        {
            ShowPlayError(staff.Reason);
            return;
        }

        _cardActionMode = CardActionMode.FlyPickMission;
        int remain = GetRemainingRange(shipBorder, ship);
        var missions = GetOrderedMissionCards();
        bool wnohgb = HasTableEvent("Where No One Has Gone Before") && missions.Count >= 2;
        int marked = 0;
        for (int i = 0; i < _spacelineOrder.Count; i++)
        {
            if (i == fromIdx) continue;
            bool endsHop = wnohgb
                && ((fromIdx == 0 && i == missions.Count - 1)
                    || (i == 0 && fromIdx == missions.Count - 1));
            bool ok;
            if (endsHop)
            {
                int cost = MovementRules.GetMissionSpan(missions[i], forOwner: true);
                ok = cost <= remain;
            }
            else
            {
                var move = MovementRules.CanMoveShip(ship, crew, remain, missions, fromIdx, i,
                    GetActiveTreaties(_activePlayer));
                ok = move.Ok;
            }
            if (!ok) continue;
            AddTargetHighlight(_spacelineOrder[i], Color.FromArgb(90, 80, 160, 255));
            marked++;
        }

        StatusText.Text = marked == 0
            ? $"No mission in RANGE (left {remain})."
            : $"FLY: {marked} mission(s) marked (RANGE {remain})"
              + (wnohgb ? " · WNOHGB: ends are adjacent." : "")
              + ". Click a mission or drag the ship.";
    }

    private void AddTargetHighlight(Border target, Color fill)
    {
        double w = target.Width > 0 ? target.Width : TableCardWidth;
        double h = target.Height > 0 ? target.Height : TableCardHeight;
        var r = new Rectangle
        {
            Width = w + 10,
            Height = h + 10,
            Stroke = new SolidColorBrush(Color.FromRgb(fill.R, fill.G, fill.B)),
            StrokeThickness = 3,
            Fill = new SolidColorBrush(fill),
            IsHitTestVisible = false,
            RadiusX = 4,
            RadiusY = 4
        };
        Canvas.SetLeft(r, Canvas.GetLeft(target) - 5);
        Canvas.SetTop(r, Canvas.GetTop(target) - 5);
        Panel.SetZIndex(r, 12);
        TableCanvas.Children.Add(r);
        _targetHighlights.Add(r);
    }

    private bool TryHandleActionModeClick(Border clicked)
    {
        if (_cardActionMode == CardActionMode.None || _actionSourceHost == null)
            return false;
        if (clicked.Tag is not Card) return false;

        if (_cardActionMode == CardActionMode.BeamPickTarget)
            return CompleteBeamTo(clicked);

        if (_cardActionMode == CardActionMode.FlyPickMission)
        {
            if (!string.Equals((clicked.Tag as Card)?.Type, "Mission", StringComparison.OrdinalIgnoreCase))
                return false;
            if (_actionSourceHost.Tag is not Card ship) return false;
            var from = FindMissionForDockable(_actionSourceHost);
            if (!TryMoveShipWithRules(_actionSourceHost, ship, from, clicked))
                return true;
            int owner = GetBorderOwner(_actionSourceHost);
            Canvas.SetLeft(_actionSourceHost, Canvas.GetLeft(clicked));
            Canvas.SetTop(_actionSourceHost, Canvas.GetTop(clicked) + DockSlotOffsetY(0, owner));
            if (from != null) RelayoutDockablesUnderMission(from);
            RelayoutDockablesUnderMission(clicked);
            UpdateHostBadge(_actionSourceHost);
            var shipRef = _actionSourceHost;
            ClearCardActionUi();
            SetSelection(shipRef);
            return true;
        }

        if (_cardActionMode == CardActionMode.AttackPickTarget)
        {
            return CompleteShipAttack(clicked);
        }

        if (_cardActionMode == CardActionMode.PersonnelAttackPick)
        {
            // Ziel-Host mit gegnerischem Personal (Mission oder Schiff)
            return CompletePersonnelAttack(clicked);
        }

        return false;
    }

    // ---------- Ship Battle (7.4.3 + Rotation Damage 7.5.1.2) ----------

    private void BeginAttackMode(Border shipBorder, Card ship)
    {
        if (_session.Segment != GameSession.TurnSegment.Execute)
        {
            ShowPlayError("Ship Battle nur im Execute-Segment.");
            return;
        }
        if (IsBorderStopped(shipBorder))
        {
            ShowPlayError("Gestopptes Schiff kann nicht angreifen.");
            return;
        }

        var crew = GetCrewOnShip(shipBorder);
        int owner = GetBorderOwner(shipBorder);
        if (owner == 0) owner = _activePlayer;

        // Vorab-Check ohne konkretes Ziel (WEAPONS + Leader)
        if (BattleRules.GetWeapons(ship) <= 0)
        {
            ShowPlayError($"{ship.Name} hat keine WEAPONS.");
            return;
        }
        if (!BattleRules.HasLeader(crew))
        {
            ShowPlayError("No leader aboard (OFFICER or Leadership required).");
            return;
        }

        var mission = FindMissionForDockable(shipBorder);
        if (mission == null)
        {
            ShowPlayError("Schiff ist keiner Mission zugeordnet.");
            return;
        }

        ClearTargetHighlights();
        _actionSourceHost = shipBorder;
        _cardActionMode = CardActionMode.AttackPickTarget;

        var enemies = new List<Border>();
        foreach (var dock in GetDockablesUnderMission(mission))
        {
            if (ReferenceEquals(dock, shipBorder)) continue;
            if (dock.Tag is not Card tc) continue;
            if (!BattleRules.IsShipOrFacility(tc)) continue;
            int o = GetBorderOwner(dock);
            if (o == 0) o = 1;
            if (o == owner) continue;
            if (GetHullDamage(dock) >= 100) continue;
            enemies.Add(dock);
        }

        foreach (var e in enemies)
            AddTargetHighlight(e, Color.FromArgb(120, 220, 60, 40));

        if (enemies.Count == 0)
        {
            ShowPlayError("Keine gegnerischen Schiffe/Facilities an dieser Location.");
            _cardActionMode = CardActionMode.None;
            ClearTargetHighlights();
            return;
        }

        StatusText.Text =
            $"ANGRIFF: {ship.Name} (W {BattleRules.GetWeapons(ship)}) – " +
            $"click enemy target ({enemies.Count} available). Right-click = cancel.";
    }

    private bool CompleteShipAttack(Border targetBorder)
    {
        var attackerBorder = _actionSourceHost;
        if (attackerBorder == null || attackerBorder.Tag is not Card attackerShip)
        {
            ClearCardActionUi();
            return true;
        }
        if (targetBorder.Tag is not Card targetCard)
            return false;

        if (!BattleRules.IsShipOrFacility(targetCard))
        {
            ShowPlayError("Ziel muss Schiff oder Facility sein.");
            return true;
        }

        int atkOwner = GetBorderOwner(attackerBorder);
        if (atkOwner == 0) atkOwner = _activePlayer;
        int defOwner = GetBorderOwner(targetBorder);
        if (defOwner == 0) defOwner = atkOwner == 1 ? 2 : 1;

        var crew = GetCrewOnShip(attackerBorder);
        var check = BattleRules.CanInitiateShipAttack(
            attackerShip, crew, atkOwner, targetCard, defOwner,
            GetHullDamage(attackerBorder), IsBorderStopped(attackerBorder));

        if (!check.Ok)
        {
            ShowPlayError(check.Reason);
            ClearCardActionUi();
            return true;
        }

        BeginShipBattleStack(attackerBorder, attackerShip, targetBorder, targetCard, atkOwner, defOwner);
        ClearCardActionUi();
        return true;
    }

    private void ResolveShipBattle(
        Border attackerBorder, Card attackerShip,
        Border defenderBorder, Card defenderCard,
        bool returnFire)
    {
        int atkOwner = GetBorderOwner(attackerBorder);
        if (atkOwner == 0) atkOwner = 1;
        int defOwner = GetBorderOwner(defenderBorder);
        if (defOwner == 0) defOwner = atkOwner == 1 ? 2 : 1;

        var logLines = new List<string>();
        logLines.Add($"SHIP BATTLE: {attackerShip.Name} (S{atkOwner}) → {defenderCard.Name} (S{defOwner})");

        int atkMult = BattleRules.KurlanMultiplier(GetAllCardsOnHost(attackerBorder, atkOwner));
        int defMult = BattleRules.KurlanMultiplier(GetAllCardsOnHost(defenderBorder, defOwner));
        var atkEv = EventsOn(attackerBorder).Select(e => (e.Kind, e.Card));
        var defEv = EventsOn(defenderBorder).Select(e => (e.Kind, e.Card));
        int atkBonus = BattleRules.GetWeapons(attackerShip) * (atkMult - 1)
                       + EventRules.WeaponsBonusFromEvents(atkEv);
        int defShieldBonus = BattleRules.GetShields(defenderCard) * (defMult - 1)
                             + EventRules.ShieldsBonusFromEvents(defEv, GetAllCardsOnHost(defenderBorder, defOwner));
        int defWeaponsBonus = BattleRules.GetWeapons(defenderCard) * (defMult - 1)
                              + EventRules.WeaponsBonusFromEvents(defEv);

        // --- Open Fire ---
        var openFire = BattleRules.ResolveFire(
            new[] { (attackerShip, atkBonus) },
            defenderCard,
            targetShieldsBonus: defShieldBonus);
        logLines.Add($"Open Fire: {openFire.Summary}" + (atkMult > 1 ? $" (Kurlan ×{atkMult})" : ""));

        int defHullBefore = GetHullDamage(defenderBorder);
        var defDmg = BattleRules.ApplyRotationDamage(defHullBefore, openFire.Result);
        int atkHullTaken = 0;
        int defHullTaken = Math.Max(0, defDmg.HullAfter - defDmg.HullBefore);

        if (defDmg.NewlyDamaged)
        {
            ApplyHullDamage(defenderBorder, defenderCard, defDmg.HullAfter);
            logLines.Add($"  Verteidiger: {defDmg.Description}");
        }

        // --- Return Fire ---
        FireCalc? returnCalc = null;
        DamageOutcome? atkDmg = null;
        if (returnFire && !defDmg.Destroyed)
        {
            // Verteidiger schießt zurück auf den Angreifer (1 Ziel)
            returnCalc = BattleRules.ResolveFire(
                new[] { (defenderCard, defWeaponsBonus) },
                attackerShip,
                targetShieldsBonus: BattleRules.GetShields(attackerShip) * (atkMult - 1));
            logLines.Add($"Return Fire: {returnCalc.Value.Summary}" + (defMult > 1 ? $" (Kurlan ×{defMult})" : ""));

            int atkHullBefore = GetHullDamage(attackerBorder);
            atkDmg = BattleRules.ApplyRotationDamage(atkHullBefore, returnCalc.Value.Result);
            atkHullTaken = Math.Max(0, atkDmg.Value.HullAfter - atkDmg.Value.HullBefore);
            if (atkDmg.Value.NewlyDamaged)
            {
                ApplyHullDamage(attackerBorder, attackerShip, atkDmg.Value.HullAfter);
                logLines.Add($"  Angreifer: {atkDmg.Value.Description}");
            }
        }
        else if (returnFire && defDmg.Destroyed)
        {
            logLines.Add("Return Fire skipped (defender already destroyed).");
        }

        string winner = BattleRules.DetermineWinner(atkHullTaken, defHullTaken);
        logLines.Add($"Winner (HULL-Schaden): {winner}");

        // --- Resolution: Destroyed → Discard ---
        bool defDestroyed = defDmg.Destroyed || GetHullDamage(defenderBorder) >= 100;
        bool atkDestroyed = (atkDmg?.Destroyed ?? false) || GetHullDamage(attackerBorder) >= 100;

        if (defDestroyed)
        {
            logLines.Add($"DESTROYED: {defenderCard.Name} → Discard (P{defOwner})");
            DestroyShipOrFacility(defenderBorder, defenderCard, defOwner);
        }
        if (atkDestroyed)
        {
            logLines.Add($"DESTROYED: {attackerShip.Name} → Discard (P{atkOwner})");
            DestroyShipOrFacility(attackerBorder, attackerShip, atkOwner);
        }

        // Überlebende Forces stoppen
        if (!atkDestroyed && attackerBorder.Parent != null)
            MarkStopped(attackerBorder);
        if (!defDestroyed && defenderBorder.Parent != null)
            MarkStopped(defenderBorder);

        // Crew auf gestoppten Schiffen mitstoppen (visuell)
        StopCrewOnHost(attackerBorder);
        StopCrewOnHost(defenderBorder);

        string summary = string.Join("\n", logLines);
        _session.Log.Add(_session.TurnNumber, $"P{atkOwner}",
            $"Battle {attackerShip.Name} vs {defenderCard.Name}: OF={openFire.Result}" +
            (returnCalc.HasValue ? $" RF={returnCalc.Value.Result}" : "") +
            $", Winner={winner}");

        StatusText.Text =
            $"Battle: {attackerShip.Name} → {defenderCard.Name} · " +
            $"OF {openFire.Result}" +
            (returnCalc.HasValue ? $" · RF {returnCalc.Value.Result}" : "") +
            (defDestroyed ? " · defender DESTROYED" : "") +
            (atkDestroyed ? " · attacker DESTROYED" : "") +
            " · survivors stopped.";

        MessageBox.Show(summary, "Ship Battle – Ergebnis", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ApplyHullDamage(Border border, Card card, int hullPercent)
    {
        hullPercent = Math.Clamp(hullPercent, 0, 100);
        _hullDamagePercent[border] = hullPercent;

        // Neuer Schaden → Repair-Fortschritt zurücksetzen
        if (hullPercent > 0)
            _repairTurnsAtOutpost[border] = 0;
        else
            _repairTurnsAtOutpost.Remove(border);

        // Visuell: 180° Rotation bei Schaden ≥ 50 %
        if (hullPercent >= 50 && hullPercent < 100)
        {
            border.RenderTransformOrigin = new Point(0.5, 0.5);
            border.RenderTransform = new RotateTransform(180);
            // RANGE sofort auf max 5 begrenzen
            if (IsShipCard(card))
            {
                int eff = BattleRules.EffectiveRange(card, hullPercent);
                if (_shipRangeLeft.TryGetValue(border, out int left) && left > eff)
                    _shipRangeLeft[border] = eff;
                else if (!_shipRangeLeft.ContainsKey(border))
                    _shipRangeLeft[border] = eff;
            }
        }
        else if (hullPercent <= 0)
        {
            border.RenderTransform = null;
        }

        UpdateDamageBadge(border, hullPercent);
    }

    /// <summary>
    /// Compendium 7.5.2: Rotation Damage wird nach 2 vollen eigenen Zügen
    /// an einem Outpost (Repair-Facility) derselben Location repariert.
    /// Aufgerufen am end of turn des Besitzers.
    /// </summary>
    private void ProcessEndOfTurnRepairs(int owner)
    {
        var repaired = new List<string>();
        var progress = new List<string>();

        foreach (var b in TableCanvas.Children.OfType<Border>().ToList())
        {
            if (b.Tag is not Card c || !IsShipCard(c)) continue;
            int o = GetBorderOwner(b);
            if (o == 0) o = 1;
            if (o != owner) continue;

            int hull = GetHullDamage(b);
            if (hull <= 0 || hull >= 100) continue;

            if (IsShipAtOwnRepairFacility(b, owner))
            {
                int turns = _repairTurnsAtOutpost.GetValueOrDefault(b, 0) + 1;
                _repairTurnsAtOutpost[b] = turns;
                UpdateDamageBadge(b, hull);

                if (turns >= 2)
                {
                    RepairShipFully(b, c);
                    repaired.Add(c.Name);
                }
                else
                {
                    progress.Add($"{c.Name} ({turns}/2)");
                }
            }
            else
            {
                // Nicht (mehr) am Outpost → Fortschritt verfällt
                if (_repairTurnsAtOutpost.GetValueOrDefault(b, 0) > 0)
                {
                    _repairTurnsAtOutpost[b] = 0;
                    UpdateDamageBadge(b, hull);
                }
            }
        }

        if (repaired.Count > 0)
        {
            string msg = "Repaired (2 turns at outpost): " + string.Join(", ", repaired);
            _session.Log.Add(_session.TurnNumber, $"P{owner}", msg);
            StatusText.Text = msg;
        }
        else if (progress.Count > 0)
        {
            string msg = "Repair-Fortschritt: " + string.Join(", ", progress);
            _session.Log.Add(_session.TurnNumber, $"P{owner}", msg);
        }
    }

    private void ProcessEndOfTurnDilemmas(int owner)
    {
        foreach (var a in _attachedDilemmas.ToList())
        {
            if (a.Host.Tag is not Card hostCard) continue;
            int ho = GetBorderOwner(a.Host);
            if (ho == 0) ho = 1;

            var present = GetAllCardsOnHost(a.Host, ho);
            if (a.Kind == DilemmaRules.PersistKind.Scow)
                present = GetCrewOnShip(a.Host); // scow on mission – check ships later

            if (DilemmaRules.CanCure(a.Kind, present, ho))
            {
                _attachedDilemmas.Remove(a);
                if (a.Kind == DilemmaRules.PersistKind.HyperAging || a.Kind == DilemmaRules.PersistKind.RemFatigue)
                    AwardDilemmaPoints(5);
                _session.Log.Add(_session.TurnNumber, $"P{ho}", $"Cured {a.Card.Name}");
                continue;
            }

            if (a.Kind == DilemmaRules.PersistKind.Junior && ho == owner)
            {
                a.Countdown++;
                if (a.Host.Tag is Card ship)
                {
                    int range = Math.Max(0, BattleRules.EffectiveRange(ship, GetHullDamage(a.Host)) - a.Countdown);
                    if (range < 1)
                    {
                        DestroyShipOrFacility(a.Host, ship, ho);
                        _attachedDilemmas.Remove(a);
                    }
                }
            }

            if (a.Countdown > 0 && a.Kind is DilemmaRules.PersistKind.Nitrium
                    or DilemmaRules.PersistKind.HyperAging or DilemmaRules.PersistKind.RemFatigue
                && ho == owner)
            {
                a.Countdown--;
                if (a.Countdown <= 0)
                {
                    if (a.Kind == DilemmaRules.PersistKind.Nitrium && a.Host.Tag is Card ns)
                        DestroyShipOrFacility(a.Host, ns, ho);
                    else
                    {
                        if (_stackOnHost.TryGetValue(a.Host, out var stacked))
                        {
                            foreach (var b in stacked.ToList())
                            {
                                if (b.Tag is not Card p || !ModifierRules.IsPersonnelCard(p)) continue;
                                if (a.Kind == DilemmaRules.PersistKind.HyperAging && DilemmaRules.IsInorganic(p))
                                    continue;
                                DiscardPersonnelBorder(b, p, ho);
                            }
                        }
                    }
                    _attachedDilemmas.Remove(a);
                    _session.Log.Add(_session.TurnNumber, $"P{ho}", $"{a.Card.Name} countdown expired");
                }
            }

            if (a.Kind == DilemmaRules.PersistKind.BorgShip)
            {
                ProcessBorgShipEndOfTurn(a);
            }
        }
    }

    /// <summary>
    /// Borg Ship (self-controlling dilemma): WEAPONS 24 / SHIELDS 24.
    /// End of every turn: attack all ships here, then move one mission toward the opposite
    /// spaceline end. When it would move past the end, it leaves play (classic "off the long end").
    /// Does not bounce back and forth.
    /// </summary>
    private void ProcessBorgShipEndOfTurn(AttachedDilemma a)
    {
        const int borgWeapons = 24;
        var host = a.Host;
        if (host.Tag is not Card hostMission) return;

        var hit = new List<string>();
        foreach (var dock in GetDockablesUnderMission(host).ToList())
        {
            if (dock.Tag is not Card sc || !IsShipCard(sc)) continue;
            int shields = BattleRules.GetShields(sc);
            var aboard = GetAllCardsOnHost(dock, GetBorderOwner(dock) == 0 ? 1 : GetBorderOwner(dock));
            shields += EventRules.ShieldsBonusFromEvents(EventsOn(dock).Select(e => (e.Kind, e.Card)), aboard);
            if (borgWeapons > shields)
            {
                int next = Math.Min(100, GetHullDamage(dock) + 50);
                ApplyHullDamage(dock, sc, next);
                hit.Add($"{sc.Name} (HULL {next}%)");
                if (next >= 100)
                    DestroyShipOrFacility(dock, sc, GetBorderOwner(dock) == 0 ? 1 : GetBorderOwner(dock));
            }
        }

        int idx = _spacelineOrder.IndexOf(host);
        if (idx < 0 && _spacelineOrder.Count > 0)
            idx = _borgShipDir > 0 ? 0 : _spacelineOrder.Count - 1;
        int nextIdx = idx + _borgShipDir;

        string attackPart = hit.Count > 0
            ? $"Attacks at {hostMission.Name}: {string.Join(", ", hit)}."
            : $"At {hostMission.Name}: no ships damaged (SHIELDS ≥ 24).";

        // Off the end → leave play (do not reverse direction)
        if (nextIdx < 0 || nextIdx >= _spacelineOrder.Count)
        {
            _attachedDilemmas.Remove(a);
            RemoveBorgShipToken();
            string leaveMsg = attackPart + " Moves off the spaceline and leaves play.";
            _session.Log.Add(_session.TurnNumber, "sys", leaveMsg);
            StatusText.Text = leaveMsg;
            ShowCardReveal(a.Card, "Borg Ship leaves", leaveMsg, RevealButtons.Ok, a.Card.Name);
            return;
        }

        var newHost = _spacelineOrder[nextIdx];
        _attachedDilemmas.Remove(a);
        _attachedDilemmas.Add(new AttachedDilemma
        {
            Card = a.Card,
            Kind = a.Kind,
            Countdown = a.Countdown,
            Host = newHost,
            Extra = a.Extra
        });
        PositionBorgShipToken(newHost);

        string where = (newHost.Tag as Card)?.Name ?? "?";
        string msg = attackPart + $" Moves to {where}.";
        _session.Log.Add(_session.TurnNumber, "sys", msg);
        StatusText.Text = msg;
        ShowCardReveal(a.Card, "Borg Ship", msg, RevealButtons.Ok, a.Card.Name);
    }

    private void PlaceBorgShipToken(Card borgCard, Border hostMission)
    {
        RemoveBorgShipToken();
        var token = new Border
        {
            Width = TableCardWidth,
            Height = TableCardHeight,
            BorderBrush = new SolidColorBrush(Color.FromRgb(180, 40, 40)),
            BorderThickness = new Thickness(3),
            Background = new SolidColorBrush(Color.FromRgb(20, 10, 10)),
            CornerRadius = new CornerRadius(4),
            Tag = borgCard,
            ToolTip = "Borg Ship (self-controlling dilemma)\nAttacks ships each end of turn, then moves toward opposite end."
        };
        var img = new Image { Stretch = Stretch.Uniform };
        if (!string.IsNullOrEmpty(borgCard.FullImagePath) && System.IO.File.Exists(borgCard.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(borgCard.FullImagePath, UriKind.Absolute);
                bmp.DecodePixelWidth = 200;
                bmp.EndInit();
                img.Source = bmp;
            }
            catch { }
        }
        token.Child = img;
        token.MouseLeftButtonDown += (_, e) =>
        {
            ShowCardDetail(borgCard);
            if (e.ClickCount >= 2) { _detailHost = null; OpenCardDetailPopup(); }
            e.Handled = true;
        };
        token.MouseRightButtonDown += (s, e) =>
        {
            ShowCardDetail(borgCard);
            BeginHoldZoom(borgCard, s as IInputElement);
            e.Handled = true;
        };
        token.MouseRightButtonUp += (_, e) =>
        {
            if (_holdZoomActive) { EndHoldZoom(); e.Handled = true; }
        };
        TableCanvas.Children.Add(token);
        Panel.SetZIndex(token, 40);
        _borgShipToken = token;
        PositionBorgShipToken(hostMission);
    }

    private void PositionBorgShipToken(Border hostMission)
    {
        if (_borgShipToken == null) return;
        double left = Canvas.GetLeft(hostMission);
        double top = Canvas.GetTop(hostMission);
        // Sit slightly above the mission on the spaceline (self-controlling ship)
        Canvas.SetLeft(_borgShipToken, left);
        Canvas.SetTop(_borgShipToken, top - UnderMissionGap * 0.55);
        Panel.SetZIndex(_borgShipToken, 40);
    }

    private void RemoveBorgShipToken()
    {
        if (_borgShipToken != null)
        {
            if (TableCanvas.Children.Contains(_borgShipToken))
                TableCanvas.Children.Remove(_borgShipToken);
            _borgShipToken = null;
        }
    }

    private bool SameHostShip(Border? a, Border? b)
    {
        if (a == null || b == null) return false;
        if (ReferenceEquals(a, b)) return true;
        // Same Card instance after layout/commandeer edge cases
        return a.Tag is Card ca && b.Tag is Card cb && ReferenceEquals(ca, cb);
    }

    private IEnumerable<RogueBorgUnit> RogueBorgUnitsOn(Border host) =>
        _rogueBorg.Where(r => SameHostShip(r.Host, host));

    private int CountRogueBorgOn(Border host) =>
        RogueBorgUnitsOn(host).Count();

    private bool ShipHasLoreReturns(Border ship) =>
        _attachedEvents.Any(e => e.Kind == EventRules.Persist.LoreReturns
                                 && ReferenceEquals(e.Host, ship));

    private bool ShipStaffedByRogueBorg(Border ship) =>
        CountRogueBorgOn(ship) > 0 && ShipHasLoreReturns(ship);

    private bool HostHasCrosis(Border host) =>
        _crosisShips.Contains(host)
        || _attachedEvents.Any(e => ReferenceEquals(e.Host, host)
                                    && (e.Card.Name ?? "").Equals("Crosis", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// X = number of Rogue Borg present.
    /// Each Rogue Borg has STRENGTH = X; total Away Team STRENGTH = X×X.
    /// Crosis doubles STRENGTH of all present → each 2X, total 2·X².
    /// </summary>
    private int RogueBorgStrengthOn(Border host)
    {
        int n = CountRogueBorgOn(host);
        if (n == 0) return 0;
        int total = n * n; // each has X → X² total
        if (HostHasCrosis(host)) total *= 2;
        return total;
    }

    /// <summary>Per-Borg STRENGTH in pairings: X each, or 2X with Crosis.</summary>
    private int RogueBorgStrengthEach(Border host)
    {
        int n = CountRogueBorgOn(host);
        if (n == 0) return 0;
        return HostHasCrosis(host) ? n * 2 : n;
    }

    private string FormatRogueBorgDetailLine(Border host)
    {
        int n = CountRogueBorgOn(host);
        if (n == 0) return "";
        int each = RogueBorgStrengthEach(host);
        int str = RogueBorgStrengthOn(host);
        var units = RogueBorgUnitsOn(host).ToList();
        string ctrl;
        if (units.Any(u => u.Controller == 1) && units.Any(u => u.Controller == 2))
            ctrl = "mixed control";
        else if (units.Any(u => u.Controller == 1))
            ctrl = "controlled by P1 (Lore Returns)";
        else if (units.Any(u => u.Controller == 2))
            ctrl = "controlled by P2 (Lore Returns)";
        else
            ctrl = "self-controlling";
        string crosis = HostHasCrosis(host) ? " · Crosis ×2" : "";
        return $"Rogue Borg ×{n}  ·  each STR {each}  ·  total STRENGTH {str} (X={n}{(HostHasCrosis(host) ? "×2" : "")})  ·  {ctrl}{crosis}";
    }

    private void PlaceRogueBorg(Card card, Border host, int playedBy)
    {
        var unit = new RogueBorgUnit
        {
            Card = card,
            Host = host,
            PlayedBy = playedBy,
            Controller = 0
        };
        var border = CreateFloatingCard(card);
        border.IsHitTestVisible = true;
        border.Visibility = Visibility.Collapsed;
        if (!TableCanvas.Children.Contains(border))
            TableCanvas.Children.Add(border);
        unit.Visual = border;
        SetBorderOwner(border, 0); // self-controlling until Lore Returns
        AddCardToHostStack(host, border);
        _rogueBorg.Add(unit);

        int n = CountRogueBorgOn(host);
        int str = RogueBorgStrengthOn(host);
        string shipName = (host.Tag as Card)?.Name ?? "ship";
        ShowCardReveal(card, "Rogue Borg",
            $"Aboard {shipName}.\n"
            + $"Rogue Borg present: {n}  ·  each STRENGTH {RogueBorgStrengthEach(host)}  ·  total {str}"
            + (HostHasCrosis(host) ? " (Crosis ×2)" : "") + ".\n"
            + "End of every player's turn: they battle personnel present.",
            RevealButtons.Ok, card.Name);
        StatusText.Text = $"Rogue Borg on {shipName} ({n} present, STR {str}).";
        _session.Log.Add(_session.TurnNumber, $"P{playedBy}",
            $"Rogue Borg → {shipName} ({n} total, STR {str})");
        UpdateHostBadge(host);
    }

    private bool TryApplyLoreReturns(Card ev, Border host, int controller)
    {
        if (host.Tag is not Card ship || !IsShipCard(ship))
        {
            ShowPlayError("Lore Returns: target must be a ship.");
            return false;
        }
        int ho = GetBorderOwner(host);
        if (ho == 0) ho = 1;
        if (ho == controller)
        {
            ShowPlayError("Lore Returns: must be an opponent's ship.");
            return false;
        }
        if (CountRogueBorgOn(host) == 0)
        {
            ShowPlayError("Lore Returns: no Rogue Borg aboard.");
            return false;
        }
        if (HostHasPersonnelOf(host, 0))
        {
            ShowPlayError("Lore Returns: ship must be empty of personnel.");
            return false;
        }

        foreach (var rb in _rogueBorg.Where(r => ReferenceEquals(r.Host, host)))
        {
            rb.Controller = controller;
            if (rb.Visual != null)
                SetBorderOwner(rb.Visual, controller);
        }
        SetBorderOwner(host, controller);

        ShowCardReveal(ev, "Lore Returns",
            $"You control the Rogue Borg aboard {ship.Name}.\n"
            + "Ship commandeered (Non-Aligned). While Rogue Borg aboard, it is staffed "
            + "and may initiate battle / beam your Rogue Borg.",
            RevealButtons.Ok, ship.Name);
        StatusText.Text = $"Lore Returns: P{controller} commandeers {ship.Name} with Rogue Borg.";
        _session.Log.Add(_session.TurnNumber, $"P{controller}",
            $"Lore Returns commandeers {ship.Name}");
        UpdateHostBadge(host);
        return true;
    }

    private void DiscardRogueBorgUnit(RogueBorgUnit unit, string reason)
    {
        if (unit.Visual != null)
        {
            if (_stackOnHost.TryGetValue(unit.Host, out var list))
                list.Remove(unit.Visual);
            if (TableCanvas.Children.Contains(unit.Visual))
                TableCanvas.Children.Remove(unit.Visual);
        }
        _rogueBorg.Remove(unit);
        int owner = unit.Controller != 0 ? unit.Controller : unit.PlayedBy;
        if (owner == 0) owner = 1;
        SendCardTo(unit.Card, owner, TimingRules.Destination.Discard);
        _session.Log.Add(_session.TurnNumber, "sys",
            $"Rogue Borg discarded ({reason})");
        UpdateHostBadge(unit.Host);
    }

    /// <summary>
    /// End of every player's turn (P1 and P2): Rogue Borg battle personnel aboard the ship.
    /// Ship owner does not matter; any personnel present (either player) can be fought.
    /// </summary>
    private void ProcessRogueBorgEndOfTurn(int finishingPlayer)
    {
        // Prune dead references (host removed from canvas)
        _rogueBorg.RemoveAll(r => r.Host == null || !TableCanvas.Children.Contains(r.Host));

        foreach (var group in _rogueBorg.GroupBy(r => r.Host).ToList())
        {
            var host = group.Key;
            if (host?.Tag is not Card ship) continue;
            var units = group.ToList();
            int n = units.Count;
            int str = RogueBorgStrengthOn(host);
            if (n == 0 || str <= 0) continue;

            // All non-RB personnel on the ship (any owner) — battle every turn if anyone is present
            var defenders = new List<Border>();
            if (_stackOnHost.TryGetValue(host, out var list))
            {
                foreach (var b in list)
                {
                    if (b.Tag is not Card c) continue;
                    if (!ModifierRules.IsPersonnelCard(c)) continue;
                    if (IsRogueBorgCard(c)) continue;
                    defenders.Add(b);
                }
            }
            if (defenders.Count == 0)
            {
                _session.Log.Add(_session.TurnNumber, "sys",
                    $"Rogue Borg on {ship.Name}: end of P{finishingPlayer} turn — no personnel present (no battle).");
                continue;
            }

            // One combatant per Rogue Borg; STRENGTH 1 each (2 with Crosis)
            int each = RogueBorgStrengthEach(host);
            var atkCards = new List<Card>();
            var unitBySynthetic = new Dictionary<Card, RogueBorgUnit>();
            for (int i = 0; i < units.Count; i++)
            {
                var syn = new Card
                {
                    Name = $"Rogue Borg #{i + 1}",
                    Type = "Personnel",
                    StrengthOrShields = each.ToString(),
                    Class = "CIVILIAN"
                };
                atkCards.Add(syn);
                unitBySynthetic[syn] = units[i];
            }

            var defCards = defenders.Select(b => (Card)b.Tag!).ToList();
            var result = BattleRules.ResolvePersonnelBattle(
                atkCards, defCards, null,
                atkCards, defCards, 0, finishingPlayer);
            if (!result.Ok) continue;

            var killed = new HashSet<string>(result.KilledNames, StringComparer.OrdinalIgnoreCase);

            foreach (var b in defenders.ToList())
            {
                if (b.Tag is not Card c) continue;
                int victimOwner = GetBorderOwner(b);
                if (victimOwner == 0) victimOwner = CardOwner(b);
                if (victimOwner == 0) victimOwner = finishingPlayer;
                if (killed.Contains(c.Name ?? ""))
                    DiscardPersonnelBorder(b, c, victimOwner);
                else
                    MarkStopped(b);
            }

            // Rogue Borg that were mortally wounded → discard (no longer active)
            int rbKilled = 0;
            foreach (var kv in unitBySynthetic)
            {
                if (!killed.Contains(kv.Key.Name ?? "")) continue;
                DiscardRogueBorgUnit(kv.Value, "killed in personnel battle");
                rbKilled++;
            }

            int strAfter = RogueBorgStrengthOn(host);
            ShowCardReveal(units[0].Card, "Rogue Borg battle",
                $"End of P{finishingPlayer}'s turn aboard {ship.Name}.\n"
                + $"Rogue Borg: {n} × STR {each} each = total {str}"
                + (HostHasCrosis(host) ? " (Crosis ×2)" : "") + ".\n"
                + result.LogSummary
                + (rbKilled > 0 ? $"\nRogue Borg killed → discard: {rbKilled}." : "")
                + $"\nRemaining: {CountRogueBorgOn(host)} Rogue Borg, STRENGTH {strAfter}.",
                RevealButtons.Ok, ship.Name);
            _session.Log.Add(_session.TurnNumber, "sys",
                $"Rogue Borg vs P{finishingPlayer} on {ship.Name}: {result.LogSummary}; RB killed={rbKilled}");
        }
    }

    private static bool IsRogueBorgCard(Card c) =>
        (c.Name ?? "").Equals("Rogue Borg", StringComparison.OrdinalIgnoreCase)
        || (c.Name ?? "").StartsWith("Rogue Borg", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Map known event persist kinds to Compendium turn wording.
    /// Future cards can set TurnScope/PhasePoint/ScopePlayer explicitly.
    /// </summary>
    private void AssignTurnScopeForEvent(AttachedEvent ae, EventRules.Persist persist, int controller, Border? host)
    {
        switch (persist)
        {
            case EventRules.Persist.WarpCore:
                // "destroyed at end of controller's next turn"
                ae.TurnScope = TimingRules.TurnScope.SpecificPlayerNextTurn;
                ae.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                ae.ScopePlayer = host != null && GetBorderOwner(host) is int o and > 0 ? o : controller;
                break;
            case EventRules.Persist.PlasmaFire:
                // Damages at end of each of that ship's controller's turns
                ae.TurnScope = TimingRules.TurnScope.EachSubjectTurn;
                ae.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                ae.ScopePlayer = host != null && GetBorderOwner(host) is int po and > 0 ? po : controller;
                break;
            case EventRules.Persist.AntiTime:
                ae.TurnScope = TimingRules.TurnScope.EveryTurn;
                ae.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                ae.ScopePlayer = controller;
                break;
            case EventRules.Persist.StaticWarp:
                // End of each opponent's turn (effect body still filters Owner != turn player)
                ae.TurnScope = TimingRules.TurnScope.EveryTurn;
                ae.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                ae.ScopePlayer = controller;
                break;
            case EventRules.Persist.Kidnappers:
            case EventRules.Persist.Traveler:
                // "each of your turns" style end-of-turn for the subject
                ae.TurnScope = TimingRules.TurnScope.EachSubjectTurn;
                ae.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                ae.ScopePlayer = controller;
                break;
            default:
                if (ae.Countdown > 0)
                {
                    ae.TurnScope = TimingRules.TurnScope.EveryTurn;
                    ae.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                }
                break;
        }
    }

    /// <summary>
    /// Start-of-turn delayed effects using TimingRules.TurnScope
    /// (e.g. Crosis: "At start of next turn, discard").
    /// Called after ActivePlayer has switched to the player whose turn is beginning.
    /// </summary>
    private void ProcessStartOfTurnTimedEffects()
    {
        int turnPlayer = _session.ActivePlayer;
        foreach (var ae in _attachedEvents
                     .Where(e => e.PhasePoint == TimingRules.TurnPhasePoint.StartOfTurn)
                     .ToList())
        {
            int cd = ae.Countdown;
            bool expired = TimingRules.TickCountdown(
                ref cd,
                ae.TurnScope,
                ae.PhasePoint,
                TimingRules.TurnPhasePoint.StartOfTurn,
                turnPlayer,
                ae.ScopePlayer);
            ae.Countdown = cd;
            if (!expired) continue;

            // Crosis and any other start-of-next-turn discards
            if ((ae.Card.Name ?? "").Equals("Crosis", StringComparison.OrdinalIgnoreCase)
                || ae.TurnScope == TimingRules.TurnScope.NextTurn)
            {
                if (ae.Host != null)
                    _crosisShips.Remove(ae.Host);
                _attachedEvents.Remove(ae);
                if (ae.Host != null && _stackOnHost.TryGetValue(ae.Host, out var stack))
                {
                    foreach (var b in stack.Where(x => x.Tag is Card c && ReferenceEquals(c, ae.Card)).ToList())
                    {
                        stack.Remove(b);
                        if (TableCanvas.Children.Contains(b))
                            TableCanvas.Children.Remove(b);
                    }
                }
                SendCardTo(ae.Card, ae.Owner, TimingRules.Destination.Discard);
                _session.Log.Add(_session.TurnNumber, "sys",
                    $"{ae.Card.Name} discarded (start of {TimingRules.DescribeScope(ae.TurnScope, ae.ScopePlayer)}).");
                StatusText.Text = $"{ae.Card.Name} discarded at start of turn.";
                if (ae.Host != null)
                    UpdateHostBadge(ae.Host);
            }
        }
    }

    [Obsolete("Use ProcessStartOfTurnTimedEffects")]
    private void ProcessCrosisStartOfTurn() => ProcessStartOfTurnTimedEffects();

    private void TryDestroyBorgShipInBattle(Border shipBorder, Card shipCard)
    {
        var borg = _attachedDilemmas.FirstOrDefault(d => d.Kind == DilemmaRules.PersistKind.BorgShip);
        if (borg == null)
        {
            ShowPlayError("No Borg Ship on the spaceline.");
            return;
        }
        var mission = FindMissionForDockable(shipBorder);
        if (mission == null || !ReferenceEquals(mission, borg.Host))
        {
            ShowPlayError("Your ship must be at the same mission as the Borg Ship.");
            return;
        }
        int owner = GetBorderOwner(shipBorder);
        if (owner == 0) owner = _activePlayer;
        var crew = GetCrewOnShip(shipBorder);
        int weapons = BattleRules.GetWeapons(shipCard)
                      + EventRules.WeaponsBonusFromEvents(EventsOn(shipBorder).Select(e => (e.Kind, e.Card)));
        weapons *= BattleRules.KurlanMultiplier(crew);
        // Borg SHIELDS 24 — need WEAPONS > 24 to damage; second hit destroys (100%)
        const int borgShields = 24;
        if (weapons <= borgShields)
        {
            ShowPlayError($"Need WEAPONS > {borgShields} to damage the Borg Ship (you have {weapons}).");
            return;
        }
        // One successful hit destroys the dilemma token (sandbox: strong enough shot wins 15)
        _attachedDilemmas.Remove(borg);
        RemoveBorgShipToken();
        AwardDilemmaPoints(15);
        _session.Log.Add(_session.TurnNumber, $"P{owner}",
            $"Destroyed Borg Ship with {shipCard.Name} (WEAPONS {weapons}) for 15 points");
        ShowCardReveal(borg.Card, "Borg Ship destroyed",
            $"{shipCard.Name} destroys the Borg Ship!\n+15 points (P{owner}).",
            RevealButtons.Ok, shipCard.Name);
        StatusText.Text = $"Borg Ship destroyed by {shipCard.Name}. +15 (P{owner}).";
    }

    private void ProcessStartOfTurnDilemmas(int owner)
    {
        foreach (var a in _attachedDilemmas.ToList())
        {
            if (a.Kind != DilemmaRules.PersistKind.Ktarian) continue;
            int ho = GetBorderOwner(a.Host);
            if (ho == 0) ho = 1;
            if (ho != owner) continue;
            var present = GetAllCardsOnHost(a.Host, ho);
            if (DilemmaRules.CanCure(a.Kind, present, ho))
            {
                _attachedDilemmas.Remove(a);
                continue;
            }
            if (_stackOnHost.TryGetValue(a.Host, out var list))
            {
                var crew = list.Where(b => b.Tag is Card c && ModifierRules.IsPersonnelCard(c)).ToList();
                if (crew.Count > 0)
                    MarkStopped(crew[new Random().Next(crew.Count)]);
            }
        }
    }

    private string? CheckEventMovement(Border ship, Card shipCard, int fromIdx, int toIdx, List<Card> crew)
    {
        int lo = Math.Min(fromIdx, toIdx);
        int hi = Math.Max(fromIdx, toIdx);
        foreach (var e in _attachedEvents)
        {
            int h1 = IndexOfMission(e.Host);
            int h2 = IndexOfMission(e.Host2);
            if (e.Kind == EventRules.Persist.QNet)
            {
                bool crosses = (h1 >= 0 && h2 >= 0 && lo <= Math.Min(h1, h2) && hi >= Math.Max(h1, h2) && fromIdx != toIdx)
                               || (h1 >= 0 && fromIdx < h1 && toIdx > h1);
                if (crosses && !EventRules.HasSkill(crew, "Diplomacy", 2))
                    return "Q-Net: 2 Diplomacy required aboard.";
            }
            if (e.Kind == EventRules.Persist.Tetryon && h1 >= 0)
            {
                if (fromIdx < h1 && toIdx > h1)
                    return "Tetryon Field: ships may not pass this location.";
                if (toIdx == h1 && _movedThisTurnAfterArrival.Contains(ship)
                    && !EventRules.HasSkill(crew, "Navigation"))
                    return "Tetryon Field: Navigation required to continue.";
            }
        }
        return null;
    }

    private void ApplyEventAfterMove(Border ship, Card shipCard, int fromIdx, int toIdx, List<Card> crew)
    {
        int lo = Math.Min(fromIdx, toIdx);
        int hi = Math.Max(fromIdx, toIdx);
        var dest = (toIdx >= 0 && toIdx < _spacelineOrder.Count) ? _spacelineOrder[toIdx] : null;
        foreach (var e in _attachedEvents.ToList())
        {
            int h1 = IndexOfMission(e.Host);
            if (e.Kind == EventRules.Persist.Rift && h1 >= 0 && fromIdx < h1 && toIdx > h1)
            {
                ApplyHullDamage(ship, shipCard, Math.Min(100, GetHullDamage(ship) + 50));
                _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}", "Subspace Warp Rift damage");
            }
            if (e.Kind == EventRules.Persist.Rift && dest != null && ReferenceEquals(e.Host, dest)
                && _movedThisTurnAfterArrival.Contains(ship))
            {
                ApplyHullDamage(ship, shipCard, Math.Min(100, GetHullDamage(ship) + 50));
            }
            if (e.Kind == EventRules.Persist.Gaps && dest != null &&
                (ReferenceEquals(e.Host, dest) || ReferenceEquals(e.Host2, dest)))
            {
                if (_stackOnHost.TryGetValue(ship, out var list))
                {
                    var crewB = list.Where(b => b.Tag is Card c && ModifierRules.IsPersonnelCard(c)).ToList();
                    if (crewB.Count > 0)
                    {
                        var victim = crewB[new Random().Next(crewB.Count)];
                        if (victim.Tag is Card vc)
                            DiscardPersonnelBorder(victim, vc, GetBorderOwner(victim) == 0 ? _activePlayer : GetBorderOwner(victim));
                    }
                }
            }
        }
        _movedThisTurnAfterArrival.Add(ship);

        // Spacedock: dock at own outpost with Spacedock → full repair
        var mission = dest;
        if (mission != null && GetHullDamage(ship) > 0)
        {
            foreach (var dock in GetDockablesUnderMission(mission))
            {
                if (!EventsOn(dock).Any(x => x.Kind == EventRules.Persist.Spacedock)) continue;
                RepairShipFully(ship, shipCard);
                _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}", "Spacedock repair");
                break;
            }
        }
    }

    private bool CanBeamAtMission(Border mission, int plannedCount)
    {
        if (HasPatternEnhancers()) return true;
        foreach (var e in EventsOn(mission))
        {
            if (e.Kind == EventRules.Persist.Distortion && e.FaceUp)
            {
                ShowPlayError("Distortion Field (face-up): no beaming.");
                return false;
            }
            if (e.Kind == EventRules.Persist.Ionization)
            {
                if (plannedCount > 1)
                {
                    ShowPlayError("Atmospheric Ionization: nur 1 Personal pro Beam.");
                    return false;
                }
                if (_ionizationBeamsThisTurn >= 3)
                {
                    ShowPlayError("Atmospheric Ionization: max. 3 Beams diesen Zug.");
                    return false;
                }
                _ionizationBeamsThisTurn++;
            }
        }
        return true;
    }

    private bool TryGenetronicSave(Border border, Card card, int owner)
    {
        bool has = _attachedEvents.Any(e => e.Kind == EventRules.Persist.Table
                                           && (e.Card.Name ?? "").Equals("Genetronic Replicator", StringComparison.OrdinalIgnoreCase))
                   || HasTableEvent("Genetronic Replicator");
        if (!has) return false;
        Border? host = null;
        foreach (var kv in _stackOnHost)
        {
            if (kv.Value.Contains(border)) { host = kv.Key; break; }
        }
        if (host == null) return false;
        var present = GetAllCardsOnHost(host, owner);
        if (!EventRules.HasSkill(present, "MEDICAL", 2)) return false;
        if (ShowCardReveal(card, "Genetronic Replicator",
                $"{card.Name} retten (2 MEDICAL stoppen → Hand)?",
                RevealButtons.YesNo) != RevealAnswer.Yes)
            return false;
        // 2 MEDICAL stoppen
        int stopped = 0;
        if (_stackOnHost.TryGetValue(host, out var list))
        {
            foreach (var b in list)
            {
                if (stopped >= 2) break;
                if (ReferenceEquals(b, border)) continue;
                if (b.Tag is not Card p || !ModifierRules.IsPersonnelCard(p)) continue;
                if (!MissionRules.ParsePersonnelSkills(p).Keys.Any(k =>
                        k.Contains("MEDICAL", StringComparison.OrdinalIgnoreCase)))
                    continue;
                MarkStopped(b);
                stopped++;
            }
        }
        foreach (var kv in _stackOnHost.ToList())
            kv.Value.Remove(border);
        if (TableCanvas.Children.Contains(border))
            TableCanvas.Children.Remove(border);
        var hand = owner == 1 ? _handCards : _oppHandCards;
        if (!hand.Contains(card)) hand.Add(card);
        ShowActivePlayerHand();
        StatusText.Text = $"Genetronic: {card.Name} → Hand.";
        _session.Log.Add(_session.TurnNumber, $"P{owner}", $"Genetronic saved {card.Name}");
        return true;
    }

    private static bool IsBorgAffiliation(Card c)
    {
        var tokens = MissionRules.ParseAffiliationTokens(c.Affiliation);
        return tokens.Contains("BORG");
    }

    private void TryNullifyPlasmaFire(Border shipBorder)
    {
        if (!EventRules.HasSkill(GetCrewOnShip(shipBorder), "SECURITY"))
        {
            ShowPlayError("Need SECURITY aboard to nullify Plasma Fire.");
            return;
        }
        var fires = EventsOn(shipBorder).Where(ae => ae.Kind == EventRules.Persist.PlasmaFire).ToList();
        if (fires.Count == 0)
        {
            ShowPlayError("No Plasma Fire on this ship.");
            return;
        }
        foreach (var e in fires)
        {
            _attachedEvents.Remove(e);
            SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
        }
        UpdateHostBadge(shipBorder);
        if (shipBorder.Tag is Card sc)
            ShowHostContents(shipBorder, sc);
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"Nullified Plasma Fire on {(shipBorder.Tag as Card)?.Name}");
        StatusText.Text = "Plasma Fire nullified (SECURITY).";
    }

    private void ProcessEndOfTurnEvents(int owner)
    {
        foreach (var e in _attachedEvents.ToList())
        {
            if ((e.Card.Name ?? "").Equals("Transwarp Conduit", StringComparison.OrdinalIgnoreCase)
                && e.Owner == owner)
            {
                _attachedEvents.Remove(e);
                SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
                continue;
            }

            if (e.Kind == EventRules.Persist.Distortion)
            {
                e.FaceUp = !e.FaceUp;
                _session.Log.Add(_session.TurnNumber, "sys",
                    $"Distortion Field now {(e.FaceUp ? "face-up" : "face-down")}");
            }

            if (e.Kind == EventRules.Persist.PlasmaFire && e.Host != null
                && e.Host.Tag is Card ship)
            {
                int shipOwner = GetBorderOwner(e.Host);
                if (shipOwner == 0) shipOwner = e.Owner;
                e.ScopePlayer ??= shipOwner;
                e.TurnScope = TimingRules.TurnScope.EachSubjectTurn;
                e.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                if (!TimingRules.ShouldProcessOnTurn(
                        e.TurnScope, e.PhasePoint,
                        TimingRules.TurnPhasePoint.EndOfTurn, owner, e.ScopePlayer))
                    continue;

                // "May be nullified by SECURITY" is optional — not automatic.
                int next = Math.Min(100, GetHullDamage(e.Host) + 50);
                ApplyHullDamage(e.Host, ship, next);
                _session.Log.Add(_session.TurnNumber, "sys",
                    $"Plasma Fire damages {ship.Name} (HULL {next}%).");
                if (next >= 100)
                {
                    ShowCardReveal(e.Card, "Plasma Fire",
                        $"{ship.Name} is destroyed by Plasma Fire (HULL 100%).",
                        RevealButtons.Ok, ship.Name, autoCloseMs: 4000);
                    DestroyShipOrFacility(e.Host, ship, shipOwner);
                    _attachedEvents.Remove(e);
                    SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
                }
                else
                {
                    ShowCardReveal(e.Card, "Plasma Fire",
                        $"{ship.Name} is damaged (HULL {next}%).\n"
                        + "May be nullified later if SECURITY is aboard.",
                        RevealButtons.Ok, ship.Name, autoCloseMs: 4000);
                }
            }

            if (e.Kind == EventRules.Persist.WarpCore && e.Host != null)
            {
                // "End of owner's next turn" → only when finishing player is ship controller
                int shipOwner = GetBorderOwner(e.Host);
                if (shipOwner == 0) shipOwner = e.Owner;
                e.ScopePlayer ??= shipOwner;
                e.TurnScope = TimingRules.TurnScope.SpecificPlayerNextTurn;
                e.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;

                if (!TimingRules.ShouldProcessOnTurn(
                        e.TurnScope, e.PhasePoint,
                        TimingRules.TurnPhasePoint.EndOfTurn, owner, e.ScopePlayer))
                    continue;

                var crew = GetCrewOnShip(e.Host);
                if (EventRules.HasSkill(crew, "ENGINEER"))
                {
                    _attachedEvents.Remove(e);
                    SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
                    continue;
                }
                int cd = e.Countdown;
                bool explode = TimingRules.TickCountdown(
                    ref cd, e.TurnScope, e.PhasePoint,
                    TimingRules.TurnPhasePoint.EndOfTurn, owner, e.ScopePlayer);
                e.Countdown = cd;
                if (explode && e.Host.Tag is Card ws)
                {
                    DestroyShipOrFacility(e.Host, ws, shipOwner);
                    _attachedEvents.Remove(e);
                    SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
                }
            }

            if (e.Kind == EventRules.Persist.StaticWarp && e.Owner != owner)
            {
                // The Traveler: Transcendence continuously nullifies Static Warp Bubble
                if (IsTravelerInPlay())
                {
                    _session.Log.AddDebug(_session.TurnNumber, "Check",
                        "Static Warp Bubble: suppressed by The Traveler: Transcendence in play");
                    continue;
                }
                var hand = owner == 1 ? _handCards : _oppHandCards;
                if (hand.Count > 0)
                {
                    var pick = PickHandCardToDiscard(owner,
                        "Static Warp Bubble",
                        "Discard one card from hand (end of turn, before you draw).");
                    if (pick != null)
                    {
                        hand.Remove(pick);
                        var disc = owner == 1 ? _discardCards : _oppDiscardCards;
                        if (!disc.Contains(pick)) disc.Add(pick);
                        RefreshZoneCounts();
                        RefreshHandStrips();
                    }
                }
            }

            if (e.Kind == EventRules.Persist.Traveler && e.TravelerPlayer == owner)
                DrawOneToHand(endOfTurn: true);

            if (e.Kind == EventRules.Persist.Kidnappers && e.Owner == owner)
                RunKidnappers(owner, e.Card);

            if (e.Kind == EventRules.Persist.AntiTime)
            {
                // Countdown ticks every turn (both players) by default
                e.TurnScope = TimingRules.TurnScope.EveryTurn;
                e.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                int cd = e.Countdown;
                bool done = TimingRules.TickCountdown(
                    ref cd, e.TurnScope, e.PhasePoint,
                    TimingRules.TurnPhasePoint.EndOfTurn, owner, e.ScopePlayer);
                e.Countdown = cd;
                if (done)
                {
                    // alle eigenen Personnel ins Draw
                    foreach (var kv in _stackOnHost.ToList())
                    {
                        foreach (var b in kv.Value.ToList())
                        {
                            if (b.Tag is not Card p || !ModifierRules.IsPersonnelCard(p)) continue;
                            int o = GetBorderOwner(b); if (o == 0) o = 1;
                            kv.Value.Remove(b);
                            if (TableCanvas.Children.Contains(b)) TableCanvas.Children.Remove(b);
                            var draw = o == 1 ? _drawCards : _oppDrawCards;
                            draw.Add(p);
                        }
                    }
                    _attachedEvents.Remove(e);
                    SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
                }
            }
        }
        RefreshZoneCounts();
    }

    /// <summary>Schiff an derselben Location wie eigenes Outpost/HQ (Repair-Facility).</summary>
    private bool IsShipAtOwnRepairFacility(Border shipBorder, int owner)
    {
        var mission = FindMissionForDockable(shipBorder);
        if (mission == null) return false;

        foreach (var dock in GetDockablesUnderMission(mission))
        {
            if (ReferenceEquals(dock, shipBorder)) continue;
            if (dock.Tag is not Card fc) continue;
            if (!IsRepairFacility(fc)) continue;
            int fo = GetBorderOwner(dock);
            if (fo == 0) fo = 1;
            if (fo == owner) return true;
        }
        return false;
    }

    private static bool IsRepairFacility(Card c)
    {
        // Premiere-Kern: Outposts reparieren; HQ/Station mit Repair-Text später
        string t = (c.Type ?? "").ToLowerInvariant();
        string n = (c.Name ?? "").ToLowerInvariant();
        if (t.Contains("outpost") || n.Contains("outpost")) return true;
        if (t.Contains("headquarters") || n.Contains("headquarters")) return true;
        // Spacedock etc. – Text-Heuristik
        string text = (c.Text ?? "").ToLowerInvariant();
        if (text.Contains("repair") || text.Contains("repairs")) return true;
        return false;
    }

    private void RepairShipFully(Border shipBorder, Card ship)
    {
        _hullDamagePercent.Remove(shipBorder);
        _repairTurnsAtOutpost.Remove(shipBorder);
        shipBorder.RenderTransform = null;
        shipBorder.Opacity = 1.0;
        UpdateDamageBadge(shipBorder, 0);

        // RANGE wieder voll (nächster Zug / sofort für Rest des Spiels)
        int full = MovementRules.GetShipRange(ship);
        _shipRangeLeft[shipBorder] = full;

        _session.Log.Add(_session.TurnNumber, $"P{GetBorderOwner(shipBorder)}",
            $"Repaired {ship.Name} (Rotation Damage cleared)");
    }

    private void ShowRepairStatus(Border shipBorder, Card ship)
    {
        int hull = GetHullDamage(shipBorder);
        int turns = _repairTurnsAtOutpost.GetValueOrDefault(shipBorder, 0);
        int owner = GetBorderOwner(shipBorder);
        if (owner == 0) owner = _activePlayer;
        bool atOutpost = IsShipAtOwnRepairFacility(shipBorder, owner);

        string loc = atOutpost
            ? "Ja – an eigenem Outpost/HQ (gleiche Location)"
            : "Nein – Schiff muss an derselben Mission wie eigenes Outpost liegen";

        MessageBox.Show(
            $"Reparatur – {ship.Name}\n\n" +
            $"HULL-Schaden: {hull}%\n" +
            $"Turns at outpost: {turns}/2\n" +
            $"Aktuell am Repair-Facility: {loc}\n\n" +
            "Rotation damage: after 2 full turns of yours at the outpost " +
            "wird der Schaden entfernt (Compendium 7.5.2).\n" +
            "If the ship leaves the location, repair progress resets.",
            "Repair status",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void UpdateDamageBadge(Border border, int hullPercent)
    {
        if (_damageBadges.TryGetValue(border, out var old))
        {
            if (TableCanvas.Children.Contains(old))
                TableCanvas.Children.Remove(old);
            _damageBadges.Remove(border);
        }
        if (hullPercent <= 0 || hullPercent >= 100) return;

        int repairTurns = _repairTurnsAtOutpost.GetValueOrDefault(border, 0);
        string label = repairTurns > 0
            ? $"DMG {hullPercent}% · R{repairTurns}/2"
            : $"DMG {hullPercent}%";

        var badge = new TextBlock
        {
            Text = label,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(200, 180, 30, 30)),
            Padding = new Thickness(3, 1, 3, 1)
        };
        Canvas.SetLeft(badge, Canvas.GetLeft(border));
        Canvas.SetTop(badge, Canvas.GetTop(border) + TableCardHeight - 16);
        Panel.SetZIndex(badge, 25);
        TableCanvas.Children.Add(badge);
        _damageBadges[border] = badge;
    }

    private void MarkStopped(Border border)
    {
        _stoppedBorders.Add(border);
        ApplyStoppedVisual(border, stopped: true);
    }

    private void ApplyStoppedVisual(Border border, bool stopped)
    {
        if (stopped)
            border.Opacity = 0.55;
        else
            border.Opacity = 1.0;
    }

    private void StopCrewOnHost(Border host)
    {
        if (!_stackOnHost.TryGetValue(host, out var list)) return;
        foreach (var b in list)
            MarkStopped(b);
    }

    /// <summary>
    /// Schiff/Facility zerstört: Crew+Equipment → Discard des Besitzers, Karte vom Tisch.
    /// </summary>
    private void DestroyShipOrFacility(Border border, Card card, int owner)
    {
        bool opp = owner == 2;
        var discardList = opp ? _oppDiscardCards : _discardCards;

        // Crew / Equipment an Bord mit in den Discard
        if (_stackOnHost.TryGetValue(border, out var stacked))
        {
            foreach (var sb in stacked.ToList())
            {
                if (sb.Tag is Card sc)
                {
                    if (!discardList.Contains(sc))
                        discardList.Add(sc);
                }
                if (TableCanvas.Children.Contains(sb))
                    TableCanvas.Children.Remove(sb);
                _hullDamagePercent.Remove(sb);
                _stoppedBorders.Remove(sb);
                if (_damageBadges.TryGetValue(sb, out var db))
                {
                    if (TableCanvas.Children.Contains(db))
                        TableCanvas.Children.Remove(db);
                    _damageBadges.Remove(sb);
                }
            }
            _stackOnHost.Remove(border);
        }

        if (!discardList.Contains(card))
            discardList.Add(card);

        // Badges
        if (_damageBadges.TryGetValue(border, out var dmgB))
        {
            if (TableCanvas.Children.Contains(dmgB))
                TableCanvas.Children.Remove(dmgB);
            _damageBadges.Remove(border);
        }
        if (_hostBadges.TryGetValue(border, out var hb))
        {
            if (TableCanvas.Children.Contains(hb))
                TableCanvas.Children.Remove(hb);
            _hostBadges.Remove(border);
        }
        if (_hostBadgesP2.TryGetValue(border, out var hb2))
        {
            if (TableCanvas.Children.Contains(hb2))
                TableCanvas.Children.Remove(hb2);
            _hostBadgesP2.Remove(border);
        }

        var mission = FindMissionForDockable(border);

        if (TableCanvas.Children.Contains(border))
            TableCanvas.Children.Remove(border);
        _tablePermanentCards.Remove(card);
        _oppTablePermanentCards.Remove(card);
        _hullDamagePercent.Remove(border);
        _stoppedBorders.Remove(border);
        _repairTurnsAtOutpost.Remove(border);
        _shipRangeLeft.Remove(border);
        _borderOwner.Remove(border);

        if (mission != null)
            RelayoutDockablesUnderMission(mission);

        RefreshZoneCounts();
    }

    // ---------- Personnel / Away Team Battle (7.4.2) ----------

    /// <summary>Personal-Borders auf einem Host (Mission=Away, Schiff=Crew), optional nur ein Spieler.</summary>
    private List<Border> GetPersonnelBordersAtHost(Border host, int? ownerFilter = null, int? opponentOf = null)
    {
        var result = new List<Border>();
        if (!_stackOnHost.TryGetValue(host, out var list)) return result;
        foreach (var b in list)
        {
            if (b.Tag is not Card c || !BattleRules.IsPersonnelCombatant(c)) continue;
            if (IsBorderStopped(b)) continue;
            int o = CardOwner(b);
            if (o == 0) o = 1;
            if (ownerFilter.HasValue && o != ownerFilter.Value) continue;
            if (opponentOf.HasValue && o == opponentOf.Value) continue;
            result.Add(b);
        }
        return result;
    }

    /// <summary>Alle Karten eines Owners auf dem Host (Personnel + Equipment) für Modifier.</summary>
    private List<Card> GetAllCardsOnHost(Border host, int owner)
    {
        var cards = new List<Card>();
        if (!_stackOnHost.TryGetValue(host, out var list)) return cards;
        foreach (var b in list)
        {
            if (b.Tag is not Card c) continue;
            int o = CardOwner(b);
            if (o == 0) o = 1;
            if (o != owner) continue;
            cards.Add(c);
        }
        return cards;
    }

    /// <summary>
    /// Sucht den Host-Stapel, in dem diese Karte liegt → present-Context für Modifier.
    /// </summary>
    private (List<Card> cards, int owner)? FindPresentContextForCard(Card card)
    {
        foreach (var kv in _stackOnHost)
        {
            foreach (var b in kv.Value)
            {
                if (!ReferenceEquals(b.Tag, card)) continue;
                int o = CardOwner(b);
                if (o == 0) o = 1;
                return (GetAllCardsOnHost(kv.Key, o), o);
            }
        }
        // Fallback: gleiche Instanz nicht gefunden – Name + Owner am Host
        foreach (var kv in _stackOnHost)
        {
            foreach (var b in kv.Value)
            {
                if (b.Tag is not Card found) continue;
                if (!string.Equals(found.Name, card.Name, StringComparison.OrdinalIgnoreCase))
                    continue;
                int o = CardOwner(b);
                if (o == 0) o = 1;
                return (GetAllCardsOnHost(kv.Key, o), o);
            }
        }
        return null;
    }

    private bool CanOfferPersonnelBattleFromShip(Border shipBorder)
    {
        var myCrew = GetPersonnelBordersAtHost(shipBorder, ownerFilter: _activePlayer);
        if (myCrew.Count == 0) return false;
        // Gegner-Crew auf demselben Schiff
        if (GetPersonnelBordersAtHost(shipBorder, opponentOf: _activePlayer).Count > 0)
            return true;
        // Oder Away Team des Gegners auf Planet derselben Mission
        var mission = FindMissionForDockable(shipBorder);
        if (mission?.Tag is Card mc && MissionRules.IsPlanetMission(mc))
            return GetPersonnelBordersAtHost(mission, opponentOf: _activePlayer).Count > 0;
        return false;
    }

    private void BeginPersonnelAttackFromHost(Border hostBorder)
    {
        if (_session.Segment != GameSession.TurnSegment.Execute)
        {
            ShowPlayError("Personnel battle is an Execute order.");
            return;
        }

        var myForceBorders = GetPersonnelBordersAtHost(hostBorder, ownerFilter: _activePlayer);
        if (myForceBorders.Count == 0)
        {
            ShowPlayError("No personnel of yours on this host.");
            return;
        }

        var myCards = myForceBorders.Select(b => (Card)b.Tag!).ToList();
        if (!BattleRules.HasLeader(myCards))
        {
            ShowPlayError("No leader in the force (OFFICER or Leadership).");
            return;
        }

        ClearTargetHighlights();
        _actionSourceHost = hostBorder;
        _cardActionMode = CardActionMode.PersonnelAttackPick;

        var targets = new List<Border>();

        // 1) Gegner-Personal auf demselben Host
        foreach (var b in GetPersonnelBordersAtHost(hostBorder, opponentOf: _activePlayer))
        {
            // Highlight den Host (nicht einzelne gestapelte Karten)
            if (!targets.Contains(hostBorder))
                targets.Add(hostBorder);
            break;
        }

        // 2) Location: Mission + Dockables
        Border? mission = string.Equals((hostBorder.Tag as Card)?.Type, "Mission", StringComparison.OrdinalIgnoreCase)
            ? hostBorder
            : FindMissionForDockable(hostBorder);

        if (mission != null)
        {
            if (!ReferenceEquals(mission, hostBorder)
                && GetPersonnelBordersAtHost(mission, opponentOf: _activePlayer).Count > 0)
                targets.Add(mission);

            foreach (var dock in GetDockablesUnderMission(mission))
            {
                if (ReferenceEquals(dock, hostBorder)) continue;
                if (GetPersonnelBordersAtHost(dock, opponentOf: _activePlayer).Count > 0)
                    targets.Add(dock);
            }
        }

        targets = targets.Distinct().ToList();
        foreach (var t in targets)
            AddTargetHighlight(t, Color.FromArgb(120, 220, 80, 40));

        if (targets.Count == 0)
        {
            ShowPlayError("No opposing personnel at this location.");
            _cardActionMode = CardActionMode.None;
            ClearTargetHighlights();
            return;
        }

        StatusText.Text =
            $"PERSONNEL BATTLE: Force ({myForceBorders.Count}) – " +
            "Host mit Gegner-Personal anklicken. Rechtsklick = Abbruch.";
    }

    private bool CompletePersonnelAttack(Border targetHost)
    {
        var sourceHost = _actionSourceHost;
        if (sourceHost == null)
        {
            ClearCardActionUi();
            return true;
        }

        // Forces müssen an derselben Location sein
        Border? srcMission = string.Equals((sourceHost.Tag as Card)?.Type, "Mission", StringComparison.OrdinalIgnoreCase)
            ? sourceHost
            : FindMissionForDockable(sourceHost);
        Border? dstMission = string.Equals((targetHost.Tag as Card)?.Type, "Mission", StringComparison.OrdinalIgnoreCase)
            ? targetHost
            : FindMissionForDockable(targetHost);

        // Auf demselben Schiff ohne Mission-Bezug: ok wenn gleicher Host
        bool sameHost = ReferenceEquals(sourceHost, targetHost);
        bool sameLocation = sameHost
            || (srcMission != null && dstMission != null && ReferenceEquals(srcMission, dstMission));

        if (!sameLocation)
        {
            ShowPlayError("Target must be at the same location.");
            return true;
        }

        var atkBorders = GetPersonnelBordersAtHost(sourceHost, ownerFilter: _activePlayer);
        var defBorders = GetPersonnelBordersAtHost(targetHost, opponentOf: _activePlayer);

        // Wenn Ziel-Mission und Quelle-Schiff: Away Team des Gegners auf Mission
        if (defBorders.Count == 0 && !ReferenceEquals(sourceHost, targetHost))
            defBorders = GetPersonnelBordersAtHost(targetHost, opponentOf: _activePlayer);

        if (atkBorders.Count == 0 || defBorders.Count == 0)
        {
            ShowPlayError("One of the forces is empty (already stopped?).");
            ClearCardActionUi();
            return true;
        }

        int atkOwner = _activePlayer;
        int defOwner = CardOwner(defBorders[0]);
        if (defOwner == 0) defOwner = atkOwner == 1 ? 2 : 1;

        var atkCards = atkBorders.Select(b => (Card)b.Tag!).ToList();
        var defCards = defBorders.Select(b => (Card)b.Tag!).ToList();

        // Present inkl. Equipment am jeweiligen Host (Modifier)
        var atkPresent = GetAllCardsOnHost(sourceHost, atkOwner);
        var defPresent = GetAllCardsOnHost(targetHost, defOwner);

        var check = BattleRules.CanInitiatePersonnelAttack(atkCards, defCards, atkOwner, defOwner);
        if (!check.Ok)
        {
            ShowPlayError(check.Reason);
            ClearCardActionUi();
            return true;
        }

        BeginPersonnelBattleStack(
            sourceHost, targetHost, atkBorders, defBorders,
            atkPresent, defPresent, atkOwner, defOwner);
        ClearCardActionUi();
        return true;
    }

    private void DiscardPersonnelBorder(Border border, Card card, int owner)
    {
        if (TryGenetronicSave(border, card, owner))
            return;

        Border? returnHost = null;
        foreach (var kv in _stackOnHost.ToList())
        {
            if (kv.Value.Contains(border))
            {
                returnHost = kv.Key;
                break;
            }
        }
        // Temporal Causality Loop: only cards discarded from the attempted location
        if (_attemptMission != null)
        {
            bool fromHere = returnHost != null && (
                ReferenceEquals(returnHost, _attemptMission)
                || FindMissionForDockable(returnHost) == _attemptMission
                || (returnHost.Tag is Card hc && IsShipCard(hc)
                    && GetDockablesUnderMission(_attemptMission).Contains(returnHost)));
            if (fromHere)
                _attemptDiscards.Add((card, owner, returnHost, wasSeed: false));
        }

        bool opp = owner == 2;
        var discardList = opp ? _oppDiscardCards : _discardCards;
        if (!discardList.Contains(card))
            discardList.Add(card);

        // Aus Host-Stapel
        foreach (var kv in _stackOnHost.ToList())
        {
            if (kv.Value.Remove(border))
            {
                UpdateHostBadge(kv.Key);
                if (kv.Value.Count == 0)
                    _stackOnHost.Remove(kv.Key);
            }
        }

        if (TableCanvas.Children.Contains(border))
            TableCanvas.Children.Remove(border);
        _stoppedBorders.Remove(border);
        _borderOwner.Remove(border);
        RefreshZoneCounts();
    }

    private bool CompleteBeamTo(Border targetHost)
    {
        var source = _actionSourceHost!;
        if (!_stackOnHost.TryGetValue(source, out var list) || list.Count == 0)
        {
            ShowPlayError("Keine Crew mehr auf der Quelle.");
            ClearCardActionUi();
            return true;
        }

        var srcMission = FindMissionForDockable(source)
            ?? (string.Equals((source.Tag as Card)?.Type, "Mission", StringComparison.OrdinalIgnoreCase) ? source : null);
        var dstMission = FindMissionForDockable(targetHost)
            ?? (string.Equals((targetHost.Tag as Card)?.Type, "Mission", StringComparison.OrdinalIgnoreCase) ? targetHost : null);

        if (srcMission == null || dstMission == null || !ReferenceEquals(srcMission, dstMission))
        {
            ShowPlayError("Beam only at the same mission (same location).");
            return true;
        }

        if (!CanBeamAtMission(srcMission, plannedCount: Math.Max(1, _beamSelected.Count)))
            return true;

        var toMove = list.Where(b =>
        {
            if (b.Tag is not Card c) return false;
            if (_beamSelected.Count > 0 && !_beamSelected.Contains(b)) return false;
            return IsBeamableFromHost(c, source, b);
        }).ToList();
        if (toMove.Count == 0)
        {
            ShowPlayError("No cards selected to beam (checkboxes in the detail window).");
            return true;
        }

        foreach (var b in toMove.ToList())
        {
            RemoveCardFromHostStack(source, b);
            SetBorderOwner(b, _activePlayer);
            AddCardToHostStack(targetHost, b);
            // Keep Rogue Borg unit host in sync so strength/battles track the new ship
            foreach (var rb in _rogueBorg.Where(r => ReferenceEquals(r.Visual, b)))
            {
                rb.Host = targetHost;
                rb.Controller = _activePlayer;
            }
        }

        UpdateHostBadge(source);
        UpdateHostBadge(targetHost);
        if (targetHost.Tag is Card tc)
        {
            string fromName = (source.Tag as Card)?.Name ?? "?";
            string names = string.Join(", ",
                toMove.Select(b => (b.Tag as Card)?.Name ?? "?").Where(n => n != "?"));
            StatusText.Text = $"Beamed {toMove.Count} card(s) {fromName} → {tc.Name}.";
            _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                $"Beam: {names}  ({fromName} → {tc.Name})");
            ClearCardActionUi();
            SetSelection(targetHost);
            ShowHostContents(targetHost, tc);
        }
        else
        {
            _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}", $"Beam {toMove.Count} cards");
            ClearCardActionUi();
            SetSelection(targetHost);
        }
        return true;
    }


    private void UpdateSelectionFrame(Border cardBorder)
    {
        if (_selectionFrame == null)
        {
            _selectionFrame = new Rectangle
            {
                Stroke = new SolidColorBrush(Color.FromRgb(0, 180, 255)),
                StrokeThickness = 2,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false,
                RadiusX = 4,
                RadiusY = 4
            };
            TableCanvas.Children.Add(_selectionFrame);
        }

        bool hasHostBadge = _hostBadges.TryGetValue(cardBorder, out var badge)
                            && badge.Visibility == Visibility.Visible;
        bool hasSeedBadge = _seedBadges.TryGetValue(cardBorder, out var sb)
                            && sb.Visibility == Visibility.Visible;

        double extra = 4;
        if (hasSeedBadge) extra += BadgeAreaHeight;
        if (hasHostBadge) extra += BadgeAreaHeight;
        double height = TableCardHeight + extra;

        _selectionFrame.Width = TableCardWidth + 6;
        _selectionFrame.Height = height + 4;
        Canvas.SetLeft(_selectionFrame, Canvas.GetLeft(cardBorder) - 3);
        Canvas.SetTop(_selectionFrame, Canvas.GetTop(cardBorder) - 3);
        Panel.SetZIndex(_selectionFrame, 15);
        _selectionFrame.Visibility = Visibility.Visible;
    }

    private sealed class HostCardRef
    {
        public Border Host { get; }
        public Border CardBorder { get; }
        public Card Card { get; }
        public HostCardRef(Border host, Border cardBorder, Card card)
        {
            Host = host;
            CardBorder = cardBorder;
            Card = card;
        }
    }

    private sealed class ZoneCardRef
    {
        public string ZoneName { get; }
        public Card Card { get; }
        public bool Opponent { get; }
        public ZoneCardRef(string zoneName, Card card, bool opponent = false)
        {
            ZoneName = zoneName;
            Card = card;
            Opponent = opponent;
        }
    }

    // ---------- Mission-Snap (Schiffe/Facilities) ----------

    private Border? FindNearestMission(double centerX, double centerY, out double distance)
    {
        Border? nearest = null;
        distance = double.MaxValue;

        foreach (var m in TableCanvas.Children.OfType<Border>()
                     .Where(b => b.Visibility == Visibility.Visible
                                 && b != _hostHighlight
                                 && b.Tag is Card c &&
                                 string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase)))
        {
            double w = m.Width > 0 ? m.Width : TableCardWidth;
            double ht = m.Height > 0 ? m.Height : TableCardHeight;
            double mx = Canvas.GetLeft(m) + w / 2.0;
            double my = Canvas.GetTop(m) + ht / 2.0;
            double dx = centerX - mx;
            double dy = centerY - my;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < distance)
            {
                distance = dist;
                nearest = m;
            }
        }
        return nearest;
    }

    private Border? FindNearestLegalSeedMission(Card seedCard, double centerX, double centerY, out double distance)
    {
        Border? nearest = null;
        distance = double.MaxValue;
        foreach (var m in TableCanvas.Children.OfType<Border>()
                     .Where(b => b.Visibility == Visibility.Visible
                                 && b != _hostHighlight
                                 && b.Tag is Card c &&
                                 string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase)))
        {
            if (m.Tag is not Card mc) continue;
            var (ok, _) = CanSeedCardUnderMission(seedCard, mc);
            if (!ok) continue;
            double w = m.Width > 0 ? m.Width : TableCardWidth;
            double ht = m.Height > 0 ? m.Height : TableCardHeight;
            double mx = Canvas.GetLeft(m) + w / 2.0;
            double my = Canvas.GetTop(m) + ht / 2.0;
            double dx = centerX - mx, dy = centerY - my;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < distance) { distance = dist; nearest = m; }
        }
        return nearest;
    }

    private Border? TrySnapToMission(Border cardBorder)
    {
        if (cardBorder.Tag is not Card card)
            return null;
        // Dockable (Schiff/Facility) oder Seedable (Dilemma/Artifact in Seed)
        if (!IsDockableUnderMission(card)
            && !(IsSeedableUnderMission(card) && (_seedPhaseActive || _devPlaySeedFromHand)))
            return null;

        double w = cardBorder.Width > 0 ? cardBorder.Width : TableCardWidth;
        double h = cardBorder.Height > 0 ? cardBorder.Height : TableCardHeight;
        double cx = Canvas.GetLeft(cardBorder) + w / 2.0;
        double cy = Canvas.GetTop(cardBorder) + h / 2.0;
        var mission = FindNearestMission(cx, cy, out double dist);
        if (mission == null || dist > ShipSnapRange)
            return null;

        if (IsDockableUnderMission(card))
        {
            Canvas.SetLeft(cardBorder, Canvas.GetLeft(mission));
            Canvas.SetTop(cardBorder, Canvas.GetTop(mission) + UnderMissionGap);
        }

        if (mission.Tag is Card missionCard)
            StatusText.Text = $"{card.Name} → Mission {missionCard.Name}";

        return mission;
    }

    private Border? FindMissionContainingCard(Border cardBorder)
    {
        double bx = Canvas.GetLeft(cardBorder);
        double by = Canvas.GetTop(cardBorder);

        foreach (var m in TableCanvas.Children.OfType<Border>()
                     .Where(b => b.Tag is Card c &&
                                 string.Equals(c.Type, "Mission", StringComparison.OrdinalIgnoreCase)))
        {
            double mx = Canvas.GetLeft(m);
            double my = Canvas.GetTop(m);
            if (Math.Abs(bx - mx) < 40 && by >= my + TableCardHeight - 30)
                return m;
        }
        return null;
    }


    /// <summary>P1: positive Y (unter Mission), P2: negative Y (über Mission).</summary>
    private static double DockSlotOffsetY(int slotIndexZeroBased, int ownerPlayer)
    {
        double sign = ownerPlayer == 2 ? -1.0 : 1.0;
        return sign * UnderMissionGap * (slotIndexZeroBased + 1);
    }

    private int CountDockablesForOwner(Border mission, int ownerPlayer, Border? exclude = null)
    {
        return GetDockablesUnderMission(mission, exclude)
            .Count(b =>
            {
                int o = GetBorderOwner(b);
                if (o == 0) o = 1;
                return o == ownerPlayer;
            });
    }

    private void RelayoutDockablesUnderMission(Border mission, Border? exclude = null)
    {
        double missionLeft = Canvas.GetLeft(mission);
        double missionTop = Canvas.GetTop(mission);

        // Facility/Outpost näher an Mission, dann Schiffe; je Besitzer sortieren
        int DockSortKey(Border b)
        {
            string t = (((Card)b.Tag!).Type ?? "").ToLowerInvariant();
            bool facility = t.Contains("facility") || t.Contains("outpost")
                            || t.Contains("headquarters") || t.Contains("station");
            return facility ? 0 : 1;
        }

        var dockables = GetDockablesUnderMission(mission, exclude).ToList();

        // Fest: Spieler 1 unter der Spaceline, Spieler 2 darüber
        var below = dockables.Where(d => GetBorderOwner(d) != 2)
            .OrderBy(DockSortKey).ThenBy(b => Canvas.GetTop(b)).ToList();
        var above = dockables.Where(d => GetBorderOwner(d) == 2)
            .OrderBy(DockSortKey).ThenByDescending(b => Canvas.GetTop(b)).ToList();

        for (int i = 0; i < below.Count; i++)
        {
            Canvas.SetLeft(below[i], missionLeft);
            Canvas.SetTop(below[i], missionTop + UnderMissionGap * (i + 1));
            // Ships above facilities so they stay clickable
            int z = DockSortKey(below[i]) == 0 ? 12 + i : 22 + i;
            Panel.SetZIndex(below[i], z);
            SetBorderOwner(below[i], 1);
            below[i].RenderTransform = Transform.Identity;
            UpdateHostBadge(below[i]);
        }
        for (int i = 0; i < above.Count; i++)
        {
            // i=0 Facility directly above mission, i=1 ship above that, …
            Canvas.SetLeft(above[i], missionLeft);
            Canvas.SetTop(above[i], missionTop - UnderMissionGap * (i + 1));
            int z = DockSortKey(above[i]) == 0 ? 12 + i : 22 + i;
            Panel.SetZIndex(above[i], z);
            SetBorderOwner(above[i], 2);
            above[i].RenderTransform = Transform.Identity;
            UpdateHostBadge(above[i]);
        }

        // Mission-Badge nur für Away-Team auf der Mission, nicht für Crew auf Schiffen
        UpdateHostBadge(mission);

        if (_selectedCard != null)
            UpdateSelectionFrame(_selectedCard);
    }


    // ===================== HOTSEAT (feste Seiten) =====================

    /// <summary>
    /// Besitzer einer Karte. Gestapelte/versteckte Karten: nur Dictionary, nie Canvas-Y
    /// (sonst landet 0,0 fälschlich bei P2 und Away-Team-Beamen verschwindet).
    /// </summary>
    private int CardOwner(Border b)
    {
        if (_borderOwner.TryGetValue(b, out int o) && o != 0)
            return o;
        // Sichtbar auf dem Feld → Position
        if (b.Visibility == Visibility.Visible)
            return GetBorderOwner(b);
        // Versteckt im Stapel ohne Owner → aktiver Spieler (beim Beamen/Report gesetzt)
        return _activePlayer;
    }

    private int GetBorderOwner(Border b)

    {
        if (_borderOwner.TryGetValue(b, out int o) && o is 1 or 2)
            return o;

        // Fallback: Position relativ zur Spaceline (P2 oben, P1 unten)
        if (b.Tag is Card c && (IsShipCard(c) || IsFacilityCard(c) || IsStackableCard(c)))
        {
            double top = Canvas.GetTop(b);
            if (!double.IsNaN(top))
            {
                if (top + TableCardHeight / 2 < SpacelineY)
                    return 2;
                if (top > SpacelineY + TableCardHeight / 2)
                    return 1;
            }
        }
        return 1;
    }

    private void SetBorderOwner(Border b, int playerId)
    {
        _borderOwner[b] = playerId;
        // Hotseat: never flip/rotate cards by owner — only hull damage may rotate
        if (!_hullDamagePercent.TryGetValue(b, out int hull) || hull < 50 || hull >= 100)
            b.RenderTransform = Transform.Identity;
    }

    /// <summary>Nur UI/Stapel aktualisieren – keine Perspektiv-Spiegelung mehr.</summary>
    private void ApplyPerspective()
    {
        RelayoutMissionsOnSpaceline();
        RebuildPlayerZones();
        RefreshZoneCounts();
        UpdatePhaseControls();
        ShowActivePlayerHand();
        UpdateTurnTints();
    }

    /// <summary>Rechte Stapel-Ansicht = Hand des Spielers am Zug (Spielphase).</summary>
    private void ShowActivePlayerHand()
    {
        if (_seedPhaseActive || _loadedDeck == null) return;
        ResetStripsToHand();
    }

    /// <summary>
    /// Doorway-Phase: Spieler legt alle eigenen Doorways, danach wechselt es zum anderen
    /// (nicht Karte-für-Karte abwechselnd).
    /// </summary>
    private void TrySequentialSeedPlayer()
    {
        if (!_seedPhaseActive || _gameMode != GameMode.Hotseat) return;
        if (_seedSubPhase != SeedSubPhase.Doorway) return;

        int selfCount = CountSeedFor(SeedSubPhase.Doorway, _activePlayer);
        if (selfCount > 0) return; // noch Doorways → weitermachen

        int other = _activePlayer == 1 ? 2 : 1;
        int otherCount = CountSeedFor(SeedSubPhase.Doorway, other);
        if (otherCount > 0)
        {
            _activePlayer = other;
            ApplyPerspective();
            OnTurnContextChanged();
            StatusText.Text =
                $"SEED Doorway → Player {_activePlayer} places all doorways ({otherCount} left).";
        }
        else
        {
            StatusText.Text = "Both players: no doorways left → phase may continue.";
        }
    }

    /// <summary>
    /// Hotseat Seed: nach dem Legen einer Mission/Dilemma zum anderen Spieler wechseln,
    /// sofern der noch Karten dieser Phase hat.
    /// </summary>
    private void TryAlternateSeedPlayer()
    {
        if (!_seedPhaseActive || _gameMode != GameMode.Hotseat) return;
        if (_seedSubPhase is not (SeedSubPhase.Mission or SeedSubPhase.Dilemma or SeedSubPhase.Facility))
            return;

        int other = _activePlayer == 1 ? 2 : 1;
        int otherCount = CountSeedFor(_seedSubPhase, other);
        int selfCount = CountSeedFor(_seedSubPhase, _activePlayer);

        // Abwechselnd, solange der andere noch Karten hat
        if (otherCount > 0)
        {
            _activePlayer = other;
            ApplyPerspective();
            ShowCurrentSeedStack();
            string phaseName = _seedSubPhase switch
            {
                SeedSubPhase.Mission => "Mission",
                SeedSubPhase.Dilemma => "Dilemma/Art",
                SeedSubPhase.Facility => "Facility/Schiff/…",
                _ => "Seed"
            };
            StatusText.Text =
                $"SEED alternating → Player {_activePlayer} ({phaseName}) " +
                $"– noch {otherCount} bei ihm, {CountSeedFor(_seedSubPhase, _activePlayer == 1 ? 2 : 1)} beim anderen.";
        }
        else if (selfCount == 0)
        {
            StatusText.Text = "No seed cards left in this sub-phase for either player.";
        }
        // else: anderer leer, aktiver darf weiterlegen
    }

    private int CountSeedFor(SeedSubPhase phase, int player) => phase switch
    {
        SeedSubPhase.Doorway => player == 1 ? _doorwayCards.Count : _oppDoorwayCards.Count,
        SeedSubPhase.Mission => player == 1 ? _missionSeedCards.Count : _oppMissionSeedCards.Count,
        SeedSubPhase.Dilemma => player == 1 ? _dilemmaSeedCards.Count : _oppDilemmaSeedCards.Count,
        SeedSubPhase.Facility => player == 1 ? _facilitySeedCards.Count : _oppFacilitySeedCards.Count,
        _ => 0
    };

    // ===================== ZOOM / PAN (nur Mitte) =====================

    private void TableCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        Point mouse = e.GetPosition(TableScroll);

        double scale = ZoomTransform.ScaleX;
        double contentX = (TableScroll.HorizontalOffset + mouse.X) / scale;
        double contentY = (TableScroll.VerticalOffset + mouse.Y) / scale;

        double zoomFactor = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
        double newScale = scale * zoomFactor;
        if (newScale < 0.25) newScale = 0.25;
        if (newScale > 3.0) newScale = 3.0;

        ZoomTransform.ScaleX = newScale;
        ZoomTransform.ScaleY = newScale;

        TableScroll.ScrollToHorizontalOffset(contentX * newScale - mouse.X);
        TableScroll.ScrollToVerticalOffset(contentY * newScale - mouse.Y);
        e.Handled = true;
    }

    private void TableCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_cardActionMode != CardActionMode.None)
        {
            ClearCardActionUi();
            if (_selectedCard != null) RefreshCardActionPanel(_selectedCard);
            StatusText.Text = "Action cancelled.";
            e.Handled = true;
            return;
        }

        // Prefer card zoom over board pan (P2 ships above outposts were losing RMB to the canvas).
        DependencyObject? src = e.OriginalSource as DependencyObject;
        while (src != null && !ReferenceEquals(src, TableCanvas))
        {
            if (src is Border b && b.Tag is Card c && b.Visibility == Visibility.Visible)
            {
                ShowCardDetail(c);
                BeginHoldZoom(c, b);
                e.Handled = true;
                return;
            }
            src = VisualTreeHelper.GetParent(src);
        }

        _isPanning = true;
        _panStart = e.GetPosition(this);
        _scrollStartH = TableScroll.HorizontalOffset;
        _scrollStartV = TableScroll.VerticalOffset;
        TableCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void TableCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_holdZoomActive)
            EndHoldZoom();
        _isPanning = false;
        TableCanvas.ReleaseMouseCapture();
    }

    private void TableCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning || e.RightButton != MouseButtonState.Pressed)
            return;

        var pos = e.GetPosition(this);
        TableScroll.ScrollToHorizontalOffset(_scrollStartH - (pos.X - _panStart.X));
        TableScroll.ScrollToVerticalOffset(_scrollStartV - (pos.Y - _panStart.Y));
    }

    // ===================== DETAIL =====================

    private Border? FindBorderForCard(Card card)
    {
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Tag is Card c && ReferenceEquals(c, card))
                return b;
        }
        return null;
    }

    private static string TrimDetail(string text, int max)
    {
        text = text.Replace('\n', ' ').Trim();
        if (text.Length <= max) return text;
        return text[..max].TrimEnd() + "…";
    }

    /// <summary>Detail line for an attached event/dilemma, including countdown when set.</summary>
    /// <summary>
    /// Status line only (name, counter, persist). Full card text lives in DetailText — do not duplicate.
    /// </summary>
    private static string FormatAttachedCounterLine(string kind, Card card, int countdown, string? persistKind)
    {
        string line = $"{kind}: {card.Name}";
        if (countdown > 0)
            line += $"  ·  COUNTER {countdown}";
        else if (!string.IsNullOrEmpty(persistKind)
                 && !persistKind.Equals("None", StringComparison.OrdinalIgnoreCase)
                 && !persistKind.Equals("PlasmaFire", StringComparison.OrdinalIgnoreCase))
            line += $"  ·  {persistKind}";
        // Plasma Fire has no numeric counter — damages every EOT of the ship's controller
        if (persistKind != null
            && persistKind.Equals("PlasmaFire", StringComparison.OrdinalIgnoreCase))
            line += "  ·  damages each of controller's EOT";
        return line;
    }

    private void ShowCardDetail(Card card)
    {
        _detailCard = card;
        DetailName.Text = card.Name;
        DetailType.Text = $"{card.Type}" + (string.IsNullOrEmpty(card.Affiliation) ? "" : $"  •  {card.Affiliation}");
        DetailText.Text = string.IsNullOrWhiteSpace(card.Text) ? "" : card.Text;

        string type = (card.Type ?? "").ToLowerInvariant();
        var attrs = new List<string>();

        // Mission: Quadrant + Region in der Detailansicht
        if (type.Contains("mission"))
        {
            attrs.Add($"QUADRANT {GetNativeQuadrant(card)}");
            string? region = GetRegion(card);
            if (region != null)
                attrs.Add($"REGION {region}");
            if (!string.IsNullOrWhiteSpace(card.Points))
                attrs.Add($"POINTS {card.Points}");
            DetailAttributes.Text = string.Join("  •  ", attrs);
            DetailClass.Text = "";
            DetailStaff.Text = "";
            DetailIcons.Text = string.IsNullOrWhiteSpace(card.Icons) ? "" : $"Icons: {card.Icons}";
        }
        else if (type.Contains("ship"))
        {
            DetailAttributes.Text = FormatShipEffectiveLine(card);
            DetailClass.Text = string.IsNullOrWhiteSpace(card.Class) ? "" : $"Class: {card.Class}";
            DetailStaff.Text = string.IsNullOrWhiteSpace(card.Staff) ? "" : $"Staffing: {card.Staff}";

            var eventLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(card.Icons))
                eventLines.Add($"Icons: {card.Icons}");
            // Attached / stacked events on this ship (e.g. Plasma Fire, Bynars)
            Border? shipBorder = FindBorderForCard(card);
            if (shipBorder != null)
            {
                foreach (var ae in EventsOn(shipBorder))
                    eventLines.Add(FormatAttachedCounterLine("Event", ae.Card, ae.Countdown, ae.Kind.ToString()));
                foreach (var ad in _attachedDilemmas.Where(d => ReferenceEquals(d.Host, shipBorder)))
                    eventLines.Add(FormatAttachedCounterLine("Dilemma", ad.Card, ad.Countdown, ad.Kind.ToString()));
                if (_stackOnHost.TryGetValue(shipBorder, out var stacked))
                {
                    foreach (var b in stacked)
                    {
                        if (b.Tag is not Card ec || !EventRules.IsEvent(ec)) continue;
                        if (EventsOn(shipBorder).Any(ae => ReferenceEquals(ae.Card, ec))) continue;
                        eventLines.Add(FormatAttachedCounterLine("Event", ec, 0, null));
                    }
                }
            }
            DetailIcons.Text = string.Join("\n", eventLines);
        }
        else if (EventRules.IsEvent(card) || type.Contains("dilemma"))
        {
            if (!string.IsNullOrWhiteSpace(card.Icons)) attrs.Add($"Icons: {card.Icons}");
            DetailAttributes.Text = string.Join("  •  ", attrs);
            DetailClass.Text = "";
            DetailStaff.Text = "";
            var counterLines = new List<string>();
            foreach (var ae in _attachedEvents.Where(e => ReferenceEquals(e.Card, card)))
            {
                string hostName = (ae.Host?.Tag as Card)?.Name ?? "(no host)";
                counterLines.Add(FormatAttachedCounterLine("Attached", card, ae.Countdown, ae.Kind.ToString())
                                 + $"  ·  on {hostName}");
            }
            foreach (var ad in _attachedDilemmas.Where(d => ReferenceEquals(d.Card, card)))
            {
                string hostName = (ad.Host?.Tag as Card)?.Name ?? "(no host)";
                counterLines.Add(FormatAttachedCounterLine("Attached", card, ad.Countdown, ad.Kind.ToString())
                                 + $"  ·  on {hostName}");
            }
            if ((card.Name ?? "").Equals("Red Alert!", StringComparison.OrdinalIgnoreCase))
                counterLines.Add(FormatRedAlertStatusLine(card));
            if (counterLines.Count == 0 && !string.IsNullOrWhiteSpace(card.Icons))
                counterLines.Add($"Icons: {card.Icons}");
            DetailIcons.Text = string.Join("\n", counterLines);
        }
        else if (type.Contains("personnel") || type.Contains("animal") || type.Contains("android"))
        {
            // Effektive Werte, falls Karte auf einem Host present ist
            var presentCtx = FindPresentContextForCard(card);
            if (presentCtx != null)
            {
                var ep = ModifierRules.ResolvePersonnel(card, presentCtx.Value.cards, presentCtx.Value.owner);
                DetailAttributes.Text = ModifierRules.FormatProfileLines(ep);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(card.IntegrityOrRange)) attrs.Add($"INTEGRITY {card.IntegrityOrRange}");
                if (!string.IsNullOrWhiteSpace(card.CunningOrWeapons)) attrs.Add($"CUNNING {card.CunningOrWeapons}");
                if (!string.IsNullOrWhiteSpace(card.StrengthOrShields)) attrs.Add($"STRENGTH {card.StrengthOrShields}");
                if (!string.IsNullOrWhiteSpace(card.Points)) attrs.Add($"POINTS {card.Points}");
                DetailAttributes.Text = string.Join("  •  ", attrs);
            }
            if (string.IsNullOrWhiteSpace(DetailAttributes.Text) || presentCtx == null)
            {
                // keep stats from profile when present
            }
            DetailClass.Text = string.IsNullOrWhiteSpace(card.Class)
                ? ""
                : $"Classification: {card.Class}";
            DetailStaff.Text = string.IsNullOrWhiteSpace(card.Staff) ? "" : $"Staffing: {card.Staff}";
            if (presentCtx == null)
            {
                var parsed = MissionRules.ParsePersonnelSkills(card);
                var classKeys = new HashSet<string>(MissionRules.Classifications, StringComparer.OrdinalIgnoreCase);
                var onlySkills = parsed
                    .Where(kv => !classKeys.Contains(kv.Key))
                    .OrderBy(kv => kv.Key)
                    .Select(kv => kv.Value > 1 ? $"{kv.Key}×{kv.Value}" : kv.Key);
                DetailIcons.Text = string.IsNullOrWhiteSpace(card.Icons) ? "" : $"Icons: {card.Icons}";
                if (onlySkills.Any())
                    DetailIcons.Text = (string.IsNullOrEmpty(DetailIcons.Text) ? "" : DetailIcons.Text + "\n")
                        + "Skills: " + string.Join(", ", onlySkills);
            }
            else
                DetailIcons.Text = string.IsNullOrWhiteSpace(card.Icons) ? "" : $"Icons: {card.Icons}";
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(card.IntegrityOrRange)) attrs.Add($"INT/RNG {card.IntegrityOrRange}");
            if (!string.IsNullOrWhiteSpace(card.CunningOrWeapons)) attrs.Add($"CUN/WPN {card.CunningOrWeapons}");
            if (!string.IsNullOrWhiteSpace(card.StrengthOrShields)) attrs.Add($"STR/SHD {card.StrengthOrShields}");
            if (!string.IsNullOrWhiteSpace(card.Points)) attrs.Add($"POINTS {card.Points}");
            DetailAttributes.Text = string.Join("  •  ", attrs);
            DetailClass.Text = string.IsNullOrWhiteSpace(card.Class) ? "" : $"Classification: {card.Class}";
            DetailStaff.Text = string.IsNullOrWhiteSpace(card.Staff) ? "" : $"Staffing: {card.Staff}";
            DetailIcons.Text = string.IsNullOrWhiteSpace(card.Icons) ? "" : $"Icons: {card.Icons}";
        }

        if (!string.IsNullOrEmpty(card.FullImagePath) && System.IO.File.Exists(card.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(card.FullImagePath, UriKind.Absolute);
                bmp.EndInit();
                DetailImage.Source = bmp;
            }
            catch { DetailImage.Source = null; }
        }
        else DetailImage.Source = null;
    }

    private void OpenCardDetailPopup()
    {
        if (_detailCard == null) return;
        FillDetailStackSection(_detailHost);
        if (CardDetailOverlay != null)
            CardDetailOverlay.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Host double-click: show scrollable contents + crew/stats under the card detail.
    /// </summary>
    private void FillDetailStackSection(Border? host)
    {
        if (DetailStackSection == null || DetailStackCards == null)
            return;

        DetailStackCards.Children.Clear();
        if (DetailStackStats != null) DetailStackStats.Text = "";

        if (host == null || host.Tag is not Card hostCard)
        {
            DetailStackSection.Visibility = Visibility.Collapsed;
            return;
        }

        bool isMission = string.Equals(hostCard.Type, "Mission", StringComparison.OrdinalIgnoreCase);
        bool isHostKind = isMission || IsShipCard(hostCard) || IsFacilityCard(hostCard);
        if (!isHostKind)
        {
            DetailStackSection.Visibility = Visibility.Collapsed;
            return;
        }

        DetailStackSection.Visibility = Visibility.Visible;
        if (DetailStackTitle != null)
        {
            if (_hostStripBeam)
                DetailStackTitle.Text = $"BEAM — check cards to move from {hostCard.Name}";
            else
                DetailStackTitle.Text = isMission
                    ? $"Contents under {hostCard.Name}"
                    : $"Contents of {hostCard.Name}";
        }

        UpdateDetailBeamSelectButton(host);

        // Crew / team stats (same data as the old TeamOverlay)
        var sb = new System.Text.StringBuilder();
        if (IsShipCard(hostCard))
            sb.AppendLine(FormatShipEffectiveLine(hostCard));
        string rbLine = FormatRogueBorgDetailLine(host);
        if (!string.IsNullOrEmpty(rbLine))
            sb.AppendLine(rbLine);
        for (int p = 1; p <= 2; p++)
        {
            var present = GetAllCardsOnHost(host, p);
            if (!present.Any()) continue;
            sb.AppendLine($"— Player {p} —");
            if (present.Any(ModifierRules.IsPersonnelCard))
                sb.AppendLine(ModifierRules.FormatTeamSummary(ModifierRules.SummarizeTeam(present, p), p));
        }
        if (DetailStackStats != null)
            DetailStackStats.Text = sb.ToString().TrimEnd();

        void AddStackMini(Card c, string? badge = null, Border? cardBorder = null)
        {
            var mini = CreateMiniCard(c, faceDown: false);
            mini.Width = 72;
            mini.Height = 100;
            mini.Margin = new Thickness(3);
            mini.Cursor = Cursors.Hand;
            if (!string.IsNullOrEmpty(badge))
                mini.ToolTip = $"{c.Name}\n{badge}\nClick = show in this window · RMB hold = enlarge";
            else
                mini.ToolTip = $"{c.Name}\nClick = show in this window · RMB hold = enlarge";

            Card cRef = c;
            mini.MouseLeftButtonDown += (_, ev) =>
            {
                ShowCardDetail(cRef);
                ev.Handled = true;
            };
            mini.MouseRightButtonDown += (s, ev) =>
            {
                ShowCardDetail(cRef);
                BeginHoldZoom(cRef, s as IInputElement);
                ev.Handled = true;
            };
            mini.MouseRightButtonUp += (_, ev) =>
            {
                if (_holdZoomActive) { EndHoldZoom(); ev.Handled = true; }
            };

            bool beamThis = _hostStripBeam && cardBorder != null
                            && IsBeamableFromHost(c, host, cardBorder);
            if (beamThis)
            {
                var cell = new Grid { Margin = new Thickness(2) };
                var cb = new CheckBox
                {
                    IsChecked = _beamSelected.Contains(cardBorder!),
                    Tag = cardBorder,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(2, 2, 0, 0),
                    ToolTip = "Select for beam"
                };
                Panel.SetZIndex(cb, 5);
                cb.Checked += BeamSelect_Changed;
                cb.Unchecked += BeamSelect_Changed;
                cell.Children.Add(mini);
                cell.Children.Add(cb);
                DetailStackCards.Children.Add(cell);
            }
            else
                DetailStackCards.Children.Add(mini);
        }

        // Last dilemma faced at this mission
        if (_lastEncounteredDilemma.TryGetValue(host, out var lastDil) && lastDil != null)
        {
            DetailStackCards.Children.Add(new TextBlock
            {
                Text = "Last dilemma",
                Foreground = new SolidColorBrush(Color.FromRgb(220, 120, 100)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 6, 0)
            });
            AddStackMini(lastDil, "Last encountered");
        }

        // Artifacts revealed mid-attempt (face-up for both; not acquired yet)
        if (_revealedArtifactsUnderMission.TryGetValue(host, out var foundArts) && foundArts.Count > 0)
        {
            DetailStackCards.Children.Add(new TextBlock
            {
                Text = $"Found artifacts ({foundArts.Count})",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 160, 80)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 6, 0)
            });
            foreach (var ac in foundArts)
                AddStackMini(ac, "Revealed — acquired when mission is solved");
        }

        // Remaining face-down seed under mission (count only unless Dev reveal)
        if (_seedUnderMission.TryGetValue(host, out var seeds) && seeds.Count > 0)
        {
            DetailStackCards.Children.Add(new TextBlock
            {
                Text = _devRevealSeed ? $"Seed remaining ({seeds.Count})" : $"Seed remaining: {seeds.Count} face-down",
                Foreground = new SolidColorBrush(Color.FromRgb(180, 160, 100)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 6, 0)
            });
            if (_devRevealSeed)
            {
                foreach (var sbord in seeds)
                {
                    if (sbord.Tag is Card sc)
                        AddStackMini(sc, "Seed under mission");
                }
            }
        }

        // Attached events on host
        foreach (var ae in EventsOn(host))
            AddStackMini(ae.Card, ae.Countdown > 0 ? $"Event · countdown {ae.Countdown}" : "Event");
        foreach (var ad in _attachedDilemmas.Where(d => ReferenceEquals(d.Host, host)))
            AddStackMini(ad.Card, ad.Countdown > 0 ? $"Dilemma · countdown {ad.Countdown}" : "Dilemma");

        // Aboard / present
        if (_stackOnHost.TryGetValue(host, out var list) && list.Count > 0)
        {
            foreach (var b in list)
            {
                if (b.Tag is not Card c) continue;
                if (EventRules.IsEvent(c) && EventsOn(host).Any(ae => ReferenceEquals(ae.Card, c)))
                    continue;
                AddStackMini(c, cardBorder: b);
            }
        }

        if (DetailStackCards.Children.Count == 0)
        {
            DetailStackCards.Children.Add(new TextBlock
            {
                Text = "(empty)",
                Foreground = Brushes.Gray,
                FontSize = 12,
                Margin = new Thickness(4)
            });
        }
    }

    private void BtnCardDetailClose_Click(object sender, RoutedEventArgs e)
    {
        CloseCardDetailPopup();
    }

    private void UpdateDetailBeamSelectButton(Border? host)
    {
        if (BtnDetailBeamSelect == null) return;
        if (!_hostStripBeam || host == null)
        {
            BtnDetailBeamSelect.Visibility = Visibility.Collapsed;
            return;
        }
        BtnDetailBeamSelect.Visibility = Visibility.Visible;
        bool allOn = true;
        if (_stackOnHost.TryGetValue(host, out var crewList))
        {
            var beamable = crewList
                .Where(b => b.Tag is Card c && IsBeamableFromHost(c, host, b))
                .ToList();
            allOn = beamable.Count > 0 && beamable.All(b => _beamSelected.Contains(b));
        }
        // All selected → show "Select none" (red). Partial/none → "Select all" (green).
        if (allOn)
        {
            BtnDetailBeamSelect.Content = "Select none";
            BtnDetailBeamSelect.Background = new SolidColorBrush(Color.FromRgb(90, 40, 40));
            BtnDetailBeamSelect.Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 180));
            BtnDetailBeamSelect.BorderBrush = new SolidColorBrush(Color.FromRgb(180, 80, 80));
        }
        else
        {
            BtnDetailBeamSelect.Content = "Select all";
            BtnDetailBeamSelect.Background = new SolidColorBrush(Color.FromRgb(40, 90, 50));
            BtnDetailBeamSelect.Foreground = new SolidColorBrush(Color.FromRgb(180, 255, 190));
            BtnDetailBeamSelect.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 180, 100));
        }
    }

    private void BtnDetailBeamSelect_Click(object sender, RoutedEventArgs e)
    {
        var host = _detailHost ?? _hostStripHost;
        if (host == null || !_stackOnHost.TryGetValue(host, out var crewList)) return;
        var beamable = crewList
            .Where(b => b.Tag is Card c && IsBeamableFromHost(c, host, b))
            .ToList();
        bool allOn = beamable.Count > 0 && beamable.All(b => _beamSelected.Contains(b));
        if (allOn)
        {
            foreach (var b in beamable) _beamSelected.Remove(b);
        }
        else
        {
            foreach (var b in beamable) _beamSelected.Add(b);
        }
        StatusText.Text = $"BEAM: {_beamSelected.Count} card(s) selected – click destination.";
        FillDetailStackSection(host);
    }

    private void CloseCardDetailPopup()
    {
        if (CardDetailOverlay != null)
            CardDetailOverlay.Visibility = Visibility.Collapsed;
        if (DetailStackCards != null)
            DetailStackCards.Children.Clear();
        if (DetailStackSection != null)
            DetailStackSection.Visibility = Visibility.Collapsed;
        if (BtnDetailBeamSelect != null)
            BtnDetailBeamSelect.Visibility = Visibility.Collapsed;
        _detailHost = null;
    }

    private void CardDetailOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CloseCardDetailPopup();
        e.Handled = true;
    }

    private void CardDetailPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // don't close when clicking the panel
    }

    private void DetailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Left click only selects detail — zoom is right-hold
        e.Handled = true;
    }

    private void DetailImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_detailCard != null)
            BeginHoldZoom(_detailCard, DetailImage);
        else if (DetailImage.Source != null)
            BeginHoldZoom(null, DetailImage);
        e.Handled = true;
    }

    private void DetailImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndHoldZoom();
        e.Handled = true;
    }

    private bool _holdZoomActive;

    private void Card_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not Card card)
            return;
        if (border.Visibility != Visibility.Visible)
            return;
        // Don't start pan when inspecting a card
        ShowCardDetail(card);
        BeginHoldZoom(card, border);
        e.Handled = true;
    }

    private void Card_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_holdZoomActive)
        {
            EndHoldZoom();
            e.Handled = true;
        }
    }

    private void BeginHoldZoom(Card? card, IInputElement? captureTarget)
    {
        _holdZoomActive = true;
        if (card != null)
            _detailCard = card;
        OpenZoomView(holdMode: true);
        try { captureTarget?.CaptureMouse(); } catch { }
    }

    private void EndHoldZoom()
    {
        if (!_holdZoomActive) return;
        _holdZoomActive = false;
        try { Mouse.Capture(null); } catch { }
        CloseZoomView(animate: true);
    }

    private void OpenZoomView(bool holdMode = false)
    {
        if (_detailCard == null && DetailImage.Source == null)
            return;

        ZoomCaption.Text = _detailCard != null
            ? (holdMode ? $"{_detailCard.Name}  ·  release RMB to close" : $"{_detailCard.Name}")
            : "release RMB to close";

        if (_detailCard != null && !string.IsNullOrEmpty(_detailCard.FullImagePath)
            && System.IO.File.Exists(_detailCard.FullImagePath))
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(_detailCard.FullImagePath, UriKind.Absolute);
                bmp.EndInit();
                ZoomImage.Source = bmp;
            }
            catch
            {
                ZoomImage.Source = DetailImage.Source;
            }
        }
        else
            ZoomImage.Source = DetailImage.Source;

        // Scale-up animation from ~30% → 100%
        var scale = new ScaleTransform(0.28, 0.28);
        ZoomImage.RenderTransform = scale;
        ZoomImage.RenderTransformOrigin = new Point(0.5, 0.5);
        ZoomOverlay.Opacity = 0;
        ZoomOverlay.Visibility = Visibility.Visible;
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.28, 1.0, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.28, 1.0, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease });
        ZoomOverlay.BeginAnimation(UIElement.OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120)));

        StatusText.Text = holdMode
            ? "Close-up – hold right mouse button"
            : "Close-up";
    }

    private void CloseZoomView(bool animate = false)
    {
        if (animate && ZoomOverlay.Visibility == Visibility.Visible)
        {
            var scale = ZoomImage.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
            ZoomImage.RenderTransform = scale;
            ZoomImage.RenderTransformOrigin = new Point(0.5, 0.5);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseIn };
            var animX = new DoubleAnimation(scale.ScaleX, 0.28, TimeSpan.FromMilliseconds(120)) { EasingFunction = ease };
            var animY = new DoubleAnimation(scale.ScaleY, 0.28, TimeSpan.FromMilliseconds(120)) { EasingFunction = ease };
            var animO = new DoubleAnimation(ZoomOverlay.Opacity, 0, TimeSpan.FromMilliseconds(120));
            animO.Completed += (_, _) =>
            {
                ZoomOverlay.Visibility = Visibility.Collapsed;
                ZoomImage.Source = null;
                ZoomImage.RenderTransform = Transform.Identity;
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
            ZoomOverlay.BeginAnimation(UIElement.OpacityProperty, animO);
        }
        else
        {
            ZoomOverlay.Visibility = Visibility.Collapsed;
            ZoomImage.Source = null;
            ZoomImage.RenderTransform = Transform.Identity;
        }
        if (_detailCard != null)
            StatusText.Text = $"Selected: {_detailCard.Name}";
    }

    private void ZoomOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Hold mode: ignore clicks; release RMB closes
        if (_holdZoomActive) { e.Handled = true; return; }
        EndHoldZoom();
        CloseZoomView();
        e.Handled = true;
    }

    private void ZoomCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_holdZoomActive) { e.Handled = true; return; }
        EndHoldZoom();
        CloseZoomView();
        e.Handled = true;
    }
}
