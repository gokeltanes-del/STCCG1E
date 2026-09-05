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
using System.Windows.Media.Media3D;
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
    private bool _detailPickMode;
    private Card? _detailPickResult;
    private System.Windows.Threading.DispatcherFrame? _detailPickFrame;

    private enum GameMode { Hotseat, Network, SingleAi }
    private GameMode _gameMode = GameMode.Hotseat;
    private readonly GameSession _session = new();
    /// <summary>E1: last compact state: line so Capture does not flood the log.</summary>
    private string? _lastEngineStateLine;
    // Log.Changed -> RefreshActionHistory -> LegalMoves fly-eval DebugLog.Move -> HistorySink
    // -> AddDebug -> Changed again. Without these guards the dispatcher queue never drains (Beam hang).
    private bool _historyRefreshing;
    private bool _historyRefreshQueued;
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
    /// <summary>Hugh played on the Borg Ship dilemma: skip its next attack pulse.</summary>
    private bool _hughBlocksBorgShipAttack;

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
    private bool _aidSortHand = true;

    // ---------- Nur noch der zoombare Mittelbereich ----------
    // Alle Karten auf dem Spielfeld gleiche Größe
    private const double TableCardWidth = 100;
    private const double TableCardHeight = 140;
    private const double MissionGap = 20;
    private const double SpacelineStartX = 200;
    private double SpacelineY = 520;             // rises when P2 stacks need room above
    private const double ShipYPlayer = 700;
    private const double ShipYOpponent = 300;
    // Platz für Facility unter Mission + mehrere Schiffe untereinander
    private const double UnderMissionGap = 175;
    private const double ShipSnapRange = 280; // großzügiger für Host-/Mission-Snap

    private Border? _hostHighlight; // Umrandung am Ziel-Host während Drag
    private Border? _currentSnapHost; // legal host currently in snap range
    private Card? _tableColumnSnapCard; // Kevin/Devil snap on P1/P2 TABLE mini
    private List<TargetSite> _targetSites = new();
    private TargetSite? _snapSite;
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
        public Border? Dest { get; set; }
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
    /// <summary>Cards Energy Vortex returned to hand this turn — not legal as the replacement play.</summary>
    private readonly HashSet<Card> _energyVortexBlocked = new();
    /// <summary>Subspace Schism: once every turn per player (resets with TurnNumber).</summary>
    private readonly HashSet<int> _schismUsedBy = new();
    private int _schismRound;
    /// <summary>EOT draw opened a Schism window — finish the turn after the stack clears.</summary>
    private bool _endTurnAfterDrawStack;
    private int _endTurnFinishingPlayer;
    private int _pendingExtraDraws;
    /// <summary>First Wormhole locked an exposed ship; second must drop on a location.</summary>
    private Border? _wormholeShip;
    /// <summary>Drag-Peek: host under cursor while a targeting card is held.</summary>
    private Border? _peekHoverHost;
    private System.Windows.Threading.DispatcherTimer? _peekHoverTimer;
    private readonly HashSet<Card> _peekLegalTargets = new();

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
        /// <summary>Owner/controller to restore after Neural Servo (etc.).</summary>
        public int SavedHostOwner { get; set; }
    }

    private readonly List<AttachedEvent> _attachedEvents = new();
    private int _ionizationBeamsThisTurn;
    private int _redAlertPlaysLeft;
    private readonly HashSet<Border> _movedThisTurnAfterArrival = new();
    /// <summary>Mission index this ship arrived at during the current turn (Rift / Tetryon “move again”).</summary>
    private readonly Dictionary<Border, int> _arrivedMissionThisTurn = new();
    private readonly HashSet<Border> _cloakedShips = new();
    /// <summary>Ship → facility it is docked at (7.1.4).</summary>
    private readonly Dictionary<Border, Border> _dockedAt = new();
    private sealed class EscapePodState
    {
        public Card Pod = null!;
        public Border? Mission;
        public int Owner;
        public List<Card> Crew = new();
    }
    private readonly List<EscapePodState> _escapePods = new();
    private bool _resolvingDestroy;
    private readonly HashSet<Border> _cloakLocked = new();
    private bool _auPlayedThisTurnP1;
    private bool _auPlayedThisTurnP2;
    private readonly Dictionary<Border, Border> _conundrumChase = new();
    private readonly Dictionary<Border, int> _edoContinuePenalty = new();
    private bool _seniorStaffArmed;
    private string? _wartimeVsAffiliation;
    private bool _uniquePersonnelPlayedP1;
    private bool _uniquePersonnelPlayedP2;

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
        _session.Log.Changed = () =>
        {
            if (_historyRefreshing || _historyRefreshQueued) return;
            _historyRefreshQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _historyRefreshQueued = false;
                RefreshActionHistory();
            }));
        };
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

        if (HistoryOverlay?.Visibility == Visibility.Visible)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && key == Key.C)
            {
                CopyActionHistorySelection();
                e.Handled = true;
            }
            return;
        }
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
        RefreshLegalMovesPanel();
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
        _kidnapHand = ShuffledCopy(hand);

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
        bool captainsLog = border != null && HasMatchingCommander(border, ship)
            && _attachedEvents.Any(e => e.Kind == EventRules.Persist.CaptainsLog && e.Owner == owner);
        if (captainsLog)
        {
            w += 3; s += 3;
        }
        int k = BattleRules.KurlanMultiplier(aboard);
        w *= k;
        s *= k;

        bool transwarp = _attachedEvents.Any(e =>
            InterruptRules.IsTranswarpConduit(e.Card)
            && (border == null || ReferenceEquals(e.Host, border)));
        int rangeFull = printedR * (transwarp ? 2 : 1);
        if (border != null)
            rangeFull = Math.Max(0, rangeFull - EventsOn(border).Count(e => e.Kind == EventRules.Persist.Baryon) * 2);
        if (border != null && GetHullDamage(border) >= 50 && rangeFull > 5)
            rangeFull = 5;
        int remain = border != null ? GetRemainingRange(border, ship) : rangeFull;

        var mods = new List<string>();
        if (EventRules.WeaponsBonusFromEvents(evs) != 0) mods.Add("Bynars/events");
        if (captainsLog) mods.Add("Captain's Log +3");
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
        if (_historyRefreshing) return;
        _historyRefreshing = true;
        try
        {
            ActionHistoryList.Items.Clear();
            foreach (var line in _session.Log.FormatLines(100, includeDebug: _devShowDebugLog))
                ActionHistoryList.Items.Add(line);
            if (ActionHistoryList.Items.Count > 0)
                ActionHistoryList.ScrollIntoView(ActionHistoryList.Items[^1]);
            RefreshLegalMovesPanel();
        }
        finally
        {
            _historyRefreshing = false;
        }
    }

    /// <summary>
    /// Snapshot for LegalMoves / EffectRegistry. No pixels — identities + session + board pieces.
    /// </summary>
    private GameState CaptureEngineState()
    {
        var board = new List<BoardPiece>();
        var store = BoardStore.Current;
        bool storeReady = store.Spaceline.Locations.Count > 0;
        var fallbackHosts = new List<string>();

        foreach (var kv in _borderOwner)
        {
            if (kv.Key.Tag is not Card c) continue;
            var kind = MapBoardKind(c);
            int owner = kv.Value;
            var aboard = new List<Card>();
            bool missionSolved = false;
            bool attemptBlocked = false;
            string? attemptBlock = null;
            int rangeLeft = -1;
            bool stopped = IsBorderStopped(kv.Key);
            bool cloaked = IsShipCloaked(kv.Key);
            int dockedAtId = GetDockedAtInstanceId(kv.Key);
            int hullPercent = GetHullDamage(kv.Key);
            bool staffed = false;
            string? staffReason = null;
            string? hostName = null;
            int spacelineIndex = -1;
            IReadOnlyList<Card> crewSnap = aboard;

            // E2: when BoardStore has this ship/facility Occupant, skip UI crew/staff/host walks.
            // E3/E3b: RangeLeft / Stopped / Cloak / Dock / Hull prefer instance (UI dicts mirror).
            bool storeHasHost = (kind is BoardPieceKind.Ship or BoardPieceKind.Facility)
                                && c.InstanceId > 0
                                && store.FindOccupant(c.InstanceId) != null;

            if (c.InstanceId > 0 && store.ById.TryGetValue(c.InstanceId, out var statusInst))
            {
                stopped = statusInst.Stopped || stopped;
                if (statusInst is ShipInstance statusShip)
                {
                    if (statusShip.RangeLeft >= 0)
                        rangeLeft = statusShip.RangeLeft;
                    cloaked = statusShip.Cloaked || cloaked;
                    if (statusShip.DockedAtId > 0)
                        dockedAtId = statusShip.DockedAtId;
                    if (statusShip.HullPercent >= 0)
                        hullPercent = statusShip.HullPercent;
                }
            }

            if (storeHasHost)
            {
                if (kind == BoardPieceKind.Ship && rangeLeft < 0)
                    rangeLeft = GetRemainingRange(kv.Key, c);
            }
            else if (kind is BoardPieceKind.Ship or BoardPieceKind.Facility or BoardPieceKind.Mission)
            {
                if (kind is BoardPieceKind.Ship or BoardPieceKind.Facility)
                    fallbackHosts.Add($"{c.Name}#{c.InstanceId}");

                int who = owner == 0 ? 1 : owner;
                aboard = GetAllCardsOnHost(kv.Key, who);
                if (kind == BoardPieceKind.Mission)
                {
                    // Away Teams of either player sit on the mission host
                    aboard = new List<Card>();
                    if (_stackOnHost.TryGetValue(kv.Key, out var stacked))
                    {
                        foreach (var b in stacked)
                            if (b.Tag is Card sc) aboard.Add(sc);
                    }
                }
                else if (kind == BoardPieceKind.Facility && aboard.Count == 0)
                {
                    if (_stackOnHost.TryGetValue(kv.Key, out var stackedFac))
                    {
                        foreach (var b in stackedFac)
                            if (b.Tag is Card sc) aboard.Add(sc);
                    }
                }

                crewSnap = aboard;
                if (kind == BoardPieceKind.Ship)
                {
                    rangeLeft = GetRemainingRange(kv.Key, c);
                    var crew = GetCrewOnShip(kv.Key);
                    crewSnap = crew.Count > 0 ? crew.ToList() : aboard;
                    int staffOwner = owner == 0 ? _activePlayer : owner;
                    var staff = MovementRules.IsShipStaffed(c, crew, GetActiveTreaties(staffOwner));
                    staffed = staff.Ok || ShipStaffedByRogueBorg(kv.Key);
                    staffReason = staffed
                        ? (staff.Ok ? staff.Reason : "Rogue Borg + Lore Returns")
                        : staff.Reason;
                    var at = FindMissionForDockable(kv.Key);
                    hostName = (at?.Tag as Card)?.Name;
                }
            }

            if (kind == BoardPieceKind.Mission)
            {
                missionSolved = _solvedMissions.Contains(kv.Key);
                if (_attachedDilemmas.Any(a =>
                        a.Kind == DilemmaRules.PersistKind.Scow && ReferenceEquals(a.Host, kv.Key)))
                {
                    attemptBlocked = true;
                    attemptBlock =
                        "Radioactive Garbage Scow: this mission cannot be attempted (tow with Tractor Beam + 2 ENGINEER).";
                }
                if (_attachedEvents.Any(e =>
                        e.Kind == EventRules.Persist.Supernova && ReferenceEquals(e.Host, kv.Key)))
                {
                    attemptBlocked = true;
                    attemptBlock = "Supernova: this mission can no longer be attempted.";
                }
                spacelineIndex = IndexOfMission(kv.Key);
            }

            board.Add(new BoardPiece
            {
                Card = c,
                Kind = kind,
                Owner = owner,
                Controller = c.Controller != 0 ? c.Controller : owner,
                InstanceId = c.InstanceId,
                FaceUp = c.FaceUp,
                HostName = hostName,
                Occupied = (kind == BoardPieceKind.Ship && !storeHasHost && ShipIsOccupied(kv.Key))
                           || ((kind == BoardPieceKind.Facility || kind == BoardPieceKind.Mission)
                               && aboard.Any(ModifierRules.IsPersonnelCard)),
                HasSecurityAboard = QuietHasSkill(aboard, "SECURITY"),
                HasEngineerAboard = QuietHasSkill(aboard, "ENGINEER"),
                MissionSolved = missionSolved,
                AttemptBlocked = attemptBlocked,
                AttemptBlockReason = attemptBlock,
                RangeLeft = rangeLeft,
                Stopped = stopped,
                Cloaked = cloaked,
                DockedAtId = dockedAtId,
                HullPercent = hullPercent,
                Staffed = staffed,
                StaffReason = staffReason,
                SpacelineIndex = spacelineIndex,
                Aboard = crewSnap
            });
        }

        foreach (var ev in _attachedEvents)
        {
            board.Add(new BoardPiece
            {
                Card = ev.Card,
                Kind = MapBoardKind(ev.Card),
                Owner = ev.Owner,
                Controller = ev.Card.Controller != 0 ? ev.Card.Controller : ev.Owner,
                InstanceId = ev.Card.InstanceId,
                FaceUp = ev.FaceUp,
                HostName = (ev.Host?.Tag as Card)?.Name,
                Persist = ev.Kind,
                Countdown = ev.Countdown,
                TurnScope = ev.TurnScope,
                PhasePoint = ev.PhasePoint
            });
        }

        foreach (var c in _tablePermanentCards.Concat(_oppTablePermanentCards))
        {
            if (!CardKinds.IsCorePermanent(c) && !TreatyRules.IsTreatyCard(c)) continue;
            if (board.Any(p => ReferenceEquals(p.Card, c))) continue;
            int own = _tablePermanentCards.Contains(c) ? 1 : 2;
            board.Add(new BoardPiece
            {
                Card = c,
                Kind = MapBoardKind(c),
                Owner = own,
                Controller = c.Controller != 0 ? c.Controller : own,
                InstanceId = c.InstanceId,
                FaceUp = c.FaceUp,
                Zone = CardZone.TableCore
            });
        }

        var seed = new GameStateSeed
        {
            Match = _session.Match,
            Segment = _session.Segment,
            ActivePlayer = _session.ActivePlayer,
            TurnNumber = _session.TurnNumber,
            SeedPhase = _seedPhaseActive,
            SeedSubPhase = (int)_seedSubPhase,
            SeedPileP1 = CurrentSeedPileCards(1),
            SeedPileP2 = CurrentSeedPileCards(2),
            CryoPersonnelSeededP1 = CountCryoPersonnelSeeded(1),
            CryoPersonnelSeededP2 = CountCryoPersonnelSeeded(2),
            NormalCardPlayAvailable = _session.NormalCardPlayAvailable,
            NormalCardPlayUsed = _session.NormalCardPlayUsed,
            StackOpen = _stack.IsOpen,
            ResponsePlayer = _stack.ResponsePlayer,
            StackTop = _stack.Top,
            ScoreP1 = _scoreP1,
            ScoreP2 = _scoreP2,
            HandP1 = _handCards.ToList(),
            HandP2 = _oppHandCards.ToList(),
            UiBoard = board,
            HasGoddess = HasTableCard(EventRules.IsGoddess),
            TentOpenP1 = IsSideDeckUnlocked("Q's Tent", opponent: false),
            TentOpenP2 = IsSideDeckUnlocked("Q's Tent", opponent: true),
            TentCountP1 = _qsTentCards.Count,
            TentCountP2 = _oppQsTentCards.Count,
            Spaceline = _spacelineOrder
                .Select(b => (b.Tag as Card)?.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .Cast<string>()
                .ToList(),
            TreatiesP1 = GetActiveTreaties(1),
            TreatiesP2 = GetActiveTreaties(2),
            HasWhereNoOneHasGoneBeforeP1 = PlayerHasWnohgb(1),
            HasWhereNoOneHasGoneBeforeP2 = PlayerHasWnohgb(2),
            TentDownloadUsedP1 = DownloadRules.TentDownloadUsedThisTurn(_session, 1),
            TentDownloadUsedP2 = DownloadRules.TentDownloadUsedThisTurn(_session, 2),
            OncePerGameKeys = _session.OncePerGame.ToList(),
            StoppedInstanceIds = _stoppedBorders
                .Select(b => b.Tag is Card sc ? sc.InstanceId : 0)
                .Where(id => id != 0)
                .Distinct()
                .ToList(),
            UntilEndOfTurnKeys = _session.UntilEndOfTurn.ToList()
        };

        var state = store.ToGameState(seed);
        string line = BoardStore.FormatStateLine(state);
        if (line != _lastEngineStateLine)
        {
            _lastEngineStateLine = line;
            string src = storeReady ? "store" : "fallback";
            DebugLog.Engine(_session.TurnNumber, _activePlayer, $"capture: source={src}");
            if (fallbackHosts.Count > 0)
                DebugLog.Engine(_session.TurnNumber, _activePlayer,
                    $"state-fallback: hosts={string.Join(",", fallbackHosts)}");
            DebugLog.Engine(_session.TurnNumber, _activePlayer, line);
            if (storeReady)
                DebugLog.Engine(_session.TurnNumber, _activePlayer, store.FormatDumpCrewLine());
        }
        return state;
    }

    private void RefreshLegalMovesPanel()
    {
        if (LegalMovesList == null) return;
        LegalMovesList.Items.Clear();
        var state = CaptureEngineState();
        int side = state.StackOpen ? state.ResponsePlayer : state.ActivePlayer;
        foreach (var line in LegalMoves.FormatLines(state, side))
            LegalMovesList.Items.Add(line);
    }

    private void TraceEngine(GameAction action) => _ = AuthorizePlay(action);

    /// <summary>
    /// Single authority entry for plays. Logs OK/DENY + template; returns payload for UI apply.
    /// </summary>
    private ApplyResult AuthorizePlay(GameAction action)
    {
        var result = EngineAuthority.Evaluate(CaptureEngineState(), action);
        _session.Log.AddDebug(_session.TurnNumber, "Engine", EngineAuthority.FormatResult(result));
        foreach (var line in EngineAuthority.FormatEvents(result))
            _session.Log.AddDebug(_session.TurnNumber, "Engine", line);
        return result;
    }

    private void DevDumpLegalMoves_Click(object sender, RoutedEventArgs e)
    {
        var state = CaptureEngineState();
        int side = state.StackOpen ? state.ResponsePlayer : state.ActivePlayer;
        foreach (var line in LegalMoves.FormatLines(state, side))
            _session.Log.AddDebug(_session.TurnNumber, "LegalMoves", line);
        RefreshActionHistory();
        StatusText.Text = "Legal moves dumped to Action History (debug).";
    }

    private void DevDumpBoard_Click(object sender, RoutedEventArgs e)
    {
        SyncBoardFromTable();
        var lines = BoardStore.Current.DumpLines().ToList();
        var snap = CaptureEngineState();
        lines.Add(BoardStore.FormatStateLine(snap));
        lines.Add(BoardStore.Current.FormatDumpCrewLine());
        DebugLog.Block(DebugLog.Channel.Board, _session.TurnNumber, _activePlayer, "Board", lines);
        foreach (var line in lines)
            _session.Log.AddDebug(_session.TurnNumber, "Board", line);
        RefreshActionHistory();
        StatusText.Text = $"Board dump: {BoardStore.Current.Spaceline.Locations.Count} location(s), {BoardStore.Current.ById.Count} instance(s).";
    }

    /// <summary>Schritt 2: copy UI truth into BoardStore. Does not change the canvas.</summary>
    private void SyncBoardFromTable(bool logDual = true)
    {
        var store = BoardStore.Current;
        store.Clear();

        foreach (var c in _handCards) store.HandP1.Add(store.Wrap(c).InstanceId);
        foreach (var c in _oppHandCards) store.HandP2.Add(store.Wrap(c).InstanceId);
        foreach (var c in _tablePermanentCards) store.TableP1.Add(store.Wrap(c).InstanceId);
        foreach (var c in _oppTablePermanentCards) store.TableP2.Add(store.Wrap(c).InstanceId);

        var missionCells = _spacelineOrder
            .Where(b => b.Tag is Card mc && IsMissionCard(mc))
            .ToList();
        var cellByMission = new Dictionary<int, Border>();
        foreach (var cell in missionCells)
        {
            if (cell.Tag is not Card printed) continue;
            var loc = AddMissionLocation(store, cell, printed);
            if (printed.InstanceId > 0)
                cellByMission[printed.InstanceId] = cell;
            store.Spaceline.Add(loc);
        }

        // Gaps / Q-Net: AttachedEvent Host+Host2 is the rule source (span card
        // may be missing from _spacelineOrder after load or a drag path).
        var placedSpanIds = new HashSet<int>();
        foreach (var ae in _attachedEvents)
        {
            if (ae.Kind is not (EventRules.Persist.Gaps or EventRules.Persist.QNet))
                continue;
            PlaceAttachedSpan(store, ae, placedSpanIds);
        }

        foreach (var cell in _spacelineOrder)
        {
            if (cell.Tag is not Card sc || !IsSpacelineSpanCard(sc)) continue;
            if (sc.InstanceId > 0 && placedSpanIds.Contains(sc.InstanceId)) continue;
            var ends = SpanEndpoints(cell);
            var fake = new AttachedEvent
            {
                Card = sc,
                Kind = EventRules.NameIs(sc, "Q-Net")
                    ? EventRules.Persist.QNet
                    : EventRules.Persist.Gaps,
                Owner = GetBorderOwner(cell),
                Host = ends.left,
                Host2 = ends.right
            };
            PlaceAttachedSpan(store, fake, placedSpanIds);
        }

        // E3/E3b: UI dicts remain mirrors; copy RangeLeft / Stopped / Cloak / Dock / Hull onto fresh wraps.
        ApplyUiStatusToStore(store);

        if (!logDual) return;
        var uiParts = new List<string>();
        foreach (var cell in missionCells)
        {
            if (cell.Tag is not Card mc) continue;
            uiParts.Add(DebugLog.Card(mc));
            foreach (var ae in _attachedEvents)
            {
                if (ae.Kind is not (EventRules.Persist.Gaps or EventRules.Persist.QNet))
                    continue;
                var left = ae.Host != null ? ResolveOnSpaceline(ae.Host) : null;
                if (!ReferenceEquals(left, cell)) continue;
                if (ae.Kind == EventRules.Persist.QNet)
                    uiParts.Add("Q-Net");
                else
                    uiParts.Add(ae.Card != null ? DebugLog.Card(ae.Card) : "Gaps");
            }
        }
        var boardParts = new List<string>();
        foreach (var l in store.Spaceline.Locations)
        {
            boardParts.Add(l.Label);
            if (l.BarrierAfter) boardParts.Add("Q-Net");
        }
        string ui = string.Join(" | ", uiParts);
        string board = string.Join(" | ", boardParts);
        DebugLog.Dual(_session.TurnNumber, _activePlayer, "spaceline", ui, board);
    }

    private Location AddMissionLocation(BoardStore store, Border cell, Card printed)
    {
        var loc = new Location
        {
            Id = store.Spaceline.NextId(),
            Kind = CardKinds.Of(printed) == CardKind.TimeLocation
                ? LocationKind.TimeLocation
                : LocationKind.Mission,
            Printed = printed,
            Quadrant = GetNativeQuadrant(printed),
            Span = MovementRules.GetMissionSpan(printed, forOwner: true)
        };
        var wrapped = store.Wrap(printed);
        if (wrapped is MissionInstance mi) loc.Mission = mi;

        foreach (var dock in GetDockablesUnderMission(cell))
        {
            if (dock.Tag is not Card dc) continue;
            var inst = store.Wrap(dc);
            var occ = new Occupant(inst);
            FillForceFromHost(store, occ.Crew, dock);
            loc.Occupants.Add(occ);
        }

        if (_stackOnHost.TryGetValue(cell, out var stacked))
        {
            foreach (var b in stacked)
            {
                if (b.Tag is not Card sc) continue;
                if (IsDockableUnderMission(sc)) continue;
                int o = CardOwner(b);
                if (o != 2) o = 1;
                var inst = store.Wrap(sc);
                var force = o == 2 ? loc.AwayTeamP2 : loc.AwayTeamP1;
                if (inst is PersonnelInstance p) force.Personnel.Add(p);
                else if (inst is EquipmentInstance eq) force.Equipment.Add(eq);
            }
        }

        return loc;
    }

    private void PlaceAttachedSpan(BoardStore store, AttachedEvent ae, HashSet<int> placedSpanIds)
    {
        if (ae.Card != null && ae.Card.InstanceId > 0 && !placedSpanIds.Add(ae.Card.InstanceId))
            return;

        Border? leftB = ae.Host != null ? ResolveOnSpaceline(ae.Host) : null;
        Card? leftCard = leftB?.Tag as Card;
        int leftIdx = -1;
        if (leftCard != null)
        {
            for (int i = 0; i < store.Spaceline.Locations.Count; i++)
            {
                var p = store.Spaceline.Locations[i].Printed;
                if (p != null && (ReferenceEquals(p, leftCard) ||
                    (p.InstanceId > 0 && p.InstanceId == leftCard.InstanceId)))
                {
                    leftIdx = i;
                    break;
                }
            }
        }

        if (ae.Kind == EventRules.Persist.QNet)
        {
            if (ae.Card != null) store.Wrap(ae.Card);
            if (leftIdx >= 0)
                store.Spaceline.Locations[leftIdx].BarrierAfter = true;
            return;
        }

        var gapsCard = ae.Card;
        var loc = new Location
        {
            Id = store.Spaceline.NextId(),
            Kind = LocationKind.Span,
            Printed = gapsCard,
            Quadrant = leftB != null ? GetSpacelineQuadrant(leftB) : "Alpha",
            Span = 4
        };
        if (gapsCard != null) store.Wrap(gapsCard);
        int insertAt = leftIdx >= 0 ? leftIdx + 1 : store.Spaceline.Locations.Count;
        store.Spaceline.Insert(insertAt, loc);
    }

    private void FillForceFromHost(BoardStore store, Force force, Border host)
    {
        if (!_stackOnHost.TryGetValue(host, out var stacked)) return;
        foreach (var b in stacked)
        {
            if (b.Tag is not Card c) continue;
            var inst = store.Wrap(c);
            if (inst is PersonnelInstance p) force.Personnel.Add(p);
            else if (inst is EquipmentInstance eq) force.Equipment.Add(eq);
        }
    }

    private void DevTentDownload_Click(object sender, RoutedEventArgs e)
    {
        var hand = _activePlayer == 2 ? _oppHandCards : _handCards;
        var door = hand.FirstOrDefault(c =>
            string.Equals(GetSideDeckForDoorway(c), "Q's Tent", StringComparison.OrdinalIgnoreCase));
        if (door == null)
        {
            ShowPlayError("Need a Q's Tent doorway in the active player's hand (the play that takes a card).");
            return;
        }
        TryDownloadFromTent(door, null);
    }

    private void DevFlipHiddenAgendas_Click(object sender, RoutedEventArgs e)
    {
        var list = _activePlayer == 2 ? _oppTablePermanentCards : _tablePermanentCards;
        var downs = list.Where(c => CardIcons.HasHiddenAgenda(c) && !c.FaceUp).ToList();
        if (downs.Count == 0)
        {
            StatusText.Text = "No face-down Hidden Agenda on your TABLE.";
            return;
        }
        var pick = downs.Count == 1 ? downs[0] : PickCardFromList("Flip Hidden Agenda", downs, "Hidden Agenda");
        if (pick != null)
            TryFlipHiddenAgenda(pick, _activePlayer);
    }

    private static BoardPieceKind MapBoardKind(Card c) => CardKinds.Of(c) switch
    {
        CardKind.Ship => BoardPieceKind.Ship,
        CardKind.Mission or CardKind.QMission => BoardPieceKind.Mission,
        CardKind.TimeLocation => BoardPieceKind.TimeLocation,
        CardKind.Facility => BoardPieceKind.Facility,
        CardKind.Event or CardKind.QEvent => BoardPieceKind.Event,
        CardKind.Incident => BoardPieceKind.Incident,
        CardKind.Objective => BoardPieceKind.Objective,
        CardKind.Dilemma or CardKind.QDilemma => BoardPieceKind.Dilemma,
        CardKind.Artifact or CardKind.QArtifact => BoardPieceKind.Artifact,
        CardKind.Tactic or CardKind.DamageMarker => BoardPieceKind.Tactic,
        CardKind.Site => BoardPieceKind.Site,
        CardKind.Interrupt or CardKind.QInterrupt => BoardPieceKind.InterruptToken,
        _ => BoardPieceKind.Other
    };

    private static bool QuietHasSkill(IEnumerable<Card> aboard, string skill)
    {
        foreach (var p in aboard)
        {
            foreach (var kv in MissionRules.ParsePersonnelSkills(p))
            {
                if (kv.Key.Equals(skill, StringComparison.OrdinalIgnoreCase)
                    || kv.Key.Contains(skill, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return false;
    }

    private void ProcessUntilEndOfTurnBag(int finishingPlayer)
    {
        foreach (var fx in TurnExpiry.Due(_session, finishingPlayer).ToList())
        {
            if (fx.Verb.Equals("Discard", StringComparison.OrdinalIgnoreCase) && fx.Card != null)
            {
                SendCardTo(fx.Card, fx.Owner > 0 ? fx.Owner : finishingPlayer,
                    TimingRules.Destination.Discard);
                _attachedEvents.RemoveAll(e => ReferenceEquals(e.Card, fx.Card));
                _session.Log.Add(_session.TurnNumber, $"P{finishingPlayer}",
                    $"Until end of turn: discarded {fx.Card.Name}"
                    + (string.IsNullOrEmpty(fx.Note) ? "" : $" ({fx.Note})"));
            }
            else if (fx.Verb.Equals("Flag", StringComparison.OrdinalIgnoreCase)
                     || fx.Verb.Equals("Unmod", StringComparison.OrdinalIgnoreCase))
            {
                _session.Log.Add(_session.TurnNumber, $"P{finishingPlayer}",
                    $"Until end of turn: cleared {fx.Verb.ToLowerInvariant()} {fx.Key}"
                    + (string.IsNullOrEmpty(fx.Note) ? "" : $" ({fx.Note})"));
            }
            else if (fx.Card != null)
            {
                _session.Log.Add(_session.TurnNumber, $"P{finishingPlayer}",
                    $"Until end of turn: expired {fx.Card.Name} [{fx.Verb}]");
            }
            TurnExpiry.Consume(_session, fx);
        }
    }

    private IEnumerable<Card> CrewWithSpecialDownload(Border host)
    {
        int owner = GetBorderOwner(host);
        if (owner == 0) owner = _activePlayer;
        foreach (var c in GetAllCardsOnHost(host, owner))
        {
            if (!CardIcons.HasSpecialDownload(c)) continue;
            if (!DownloadRules.CanSpecialDownload(_session, owner, c).ok) continue;
            yield return c;
        }
    }

    private bool TryDownloadFromTent(Card doorway, Border? floating)
    {
        int player = _activePlayer;
        var tent = player == 2 ? _oppQsTentCards : _qsTentCards;
        var auth = AuthorizePlay(new GameAction
        {
            Kind = GameActionKind.Download,
            Player = player,
            Card = doorway,
            Note = "Q's Tent"
        });
        if (!auth.Ok)
        {
            ShowPlayError(auth.Message);
            return false;
        }
        // Session-level once-per-turn (DownloadRules) stays the mark after success.

        if (floating != null)
        {
            if (DragLayer.Children.Contains(floating))
                DragLayer.Children.Remove(floating);
            if (TableCanvas.Children.Contains(floating))
                TableCanvas.Children.Remove(floating);
        }

        var hand = player == 2 ? _oppHandCards : _handCards;
        hand.Remove(doorway);

        string tentPick = AskChoice(doorway, "Q's Tent",
            "Either way: draw no cards this turn.",
            "Choose a card", "Random card");
        bool choose = tentPick.StartsWith("Choose", StringComparison.OrdinalIgnoreCase);

        Card? taken;
        if (choose)
            taken = PickCardFromList("Take from Q's Tent", tent, "Q's Tent");
        else
            taken = tent.Count == 0 ? null : tent[new Random().Next(tent.Count)];

        if (taken == null)
        {
            if (!hand.Contains(doorway)) hand.Add(doorway);
            RefreshHandStrips();
            ShowPlayError("No card taken.");
            return true;
        }

        tent.Remove(taken);
        taken.FaceUp = true;
        if (!hand.Contains(taken)) hand.Add(taken);
        ShowCardReveal(taken, "Q's Tent — downloaded",
            $"{taken.Name} goes to P{player}'s hand (show opponent).",
            RevealButtons.Ok, taken.Name);

        if (choose)
            SendCardTo(doorway, player, TimingRules.Destination.Discard);
        else
        {
            var draw = player == 2 ? _oppDrawCards : _drawCards;
            draw.Insert(0, doorway);
        }

        DownloadRules.MarkTentDownload(_session, player);
        _session.SuppressEndOfTurnDraw = true;
        _session.Log.Add(_session.TurnNumber, $"P{player}",
            $"Q's Tent download: {taken.Name}");
        RefreshHandStrips();
        RefreshZoneCounts();
        StatusText.Text = $"Q's Tent: {taken.Name} → hand. No draw this turn.";
        return true;
    }

    private void TrySpecialDownload(Card source, int owner)
    {
        if (owner == 0) owner = _activePlayer;
        var auth = AuthorizePlay(new GameAction
        {
            Kind = GameActionKind.Download,
            Player = owner,
            Card = source,
            Note = "Special Download"
        });
        if (!auth.Ok)
        {
            ShowPlayError(auth.Message);
            return;
        }

        string? want = DownloadRules.ParseSpecialDownloadName(source);
        var draw = owner == 2 ? _oppDrawCards : _drawCards;
        var tent = owner == 2 ? _oppQsTentCards : _qsTentCards;
        var req = new DownloadRules.Request
        {
            Player = owner,
            Source = DownloadRules.Source.DrawDeck,
            Dest = DownloadRules.Dest.Hand,
            NameEquals = want,
            SpecialDownload = true,
            SourceCard = source
        };

        var pool = DownloadRules.FilterSource(draw, req);
        if (pool.Count == 0 && IsSideDeckUnlocked("Q's Tent", owner == 2))
            pool = DownloadRules.FilterSource(tent, req);
        if (pool.Count == 0 && want == null)
            pool = draw.ToList();

        Card? taken = pool.Count == 1
            ? pool[0]
            : PickCardFromList(
                want != null ? $"Special Download {want}" : "Special Download — choose a card",
                pool.Count > 0 ? pool : draw,
                source.Name ?? "Special Download");
        if (taken == null)
        {
            ShowPlayError("Special Download cancelled or no matching card.");
            return;
        }

        draw.Remove(taken);
        tent.Remove(taken);
        var hand = owner == 2 ? _oppHandCards : _handCards;
        taken.FaceUp = true;
        if (!hand.Contains(taken)) hand.Add(taken);
        DownloadRules.MarkSpecialDownload(_session, owner, source);
        ShowCardReveal(taken, "Special Download",
            $"{source.Name} downloads {taken.Name} to hand.",
            RevealButtons.Ok, taken.Name);
        RefreshHandStrips();
        RefreshZoneCounts();
        _session.Log.Add(_session.TurnNumber, $"P{owner}",
            $"Special Download: {source.Name} → {taken.Name}");
        StatusText.Text = $"Special Download: {taken.Name} → hand.";
    }

    private void TryFlipHiddenAgenda(Card card, int owner)
    {
        var auth = AuthorizePlay(GameAction.FlipHiddenAgenda(owner, card));
        if (!auth.Ok)
        {
            ShowPlayError(auth.Message);
            return;
        }
        card.FaceUp = true;
        ShowCardReveal(card, "Hidden Agenda flipped",
            card.Text ?? card.Name ?? "Hidden Agenda",
            RevealButtons.Ok, card.Name);
        RebuildTablePermanentsPanel();
        _session.Log.Add(_session.TurnNumber, $"P{owner}", $"Flipped Hidden Agenda: {card.Name}");
        StatusText.Text = $"Flipped {card.Name} face-up.";
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
        RevealAnswer autoDefault = RevealAnswer.No,
        string? yesLabel = null,
        string? noLabel = null,
        bool randomOnTimeout = false)
    {
        if (CardRevealOverlay == null)
        {
            MessageBox.Show(body, title);
            return RevealAnswer.Ok;
        }

        RevealTitle.Text = title;
        RevealSubtitle.Text = subtitle ?? (card != null
            ? $"{card.Type}" + (string.IsNullOrEmpty(card.Affiliation) ? "" : $"  ·  {card.Affiliation}")
              + (DualAffiliationRules.CurrentMode(card) is string mode ? $"  [{DualAffiliationRules.DisplayName(mode)}]" : "")
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
            BtnRevealYes.Content = yesLabel ?? "Player 1";
            BtnRevealNo.Content = noLabel ?? "Player 2";
        }
        else if (yesNo)
        {
            BtnRevealYes.Content = yesLabel ?? "Yes";
            BtnRevealNo.Content = noLabel ?? "No";
        }
        else
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
        // Timeout with no click: pick at random (not a hidden default).
        if (choice)
        {
            if (autoCloseMs == null) autoCloseMs = 10000;
            randomOnTimeout = true;
        }

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
                    RevealAnswer def;
                    if (choice && randomOnTimeout)
                        def = _autoSeedRng.Next(2) == 0 ? RevealAnswer.Yes : RevealAnswer.No;
                    else if (choice)
                        def = autoDefault;
                    else
                        def = RevealAnswer.Ok;
                    CloseReveal(def);
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

    /// <summary>
    /// Labeled OR-choice. Buttons show the option names (not Yes/No).
    /// Timeout with no click → uniform random option, then a result overlay.
    /// </summary>
    private string AskChoice(Card? card, string title, string prompt, params string[] options)
    {
        if (options == null || options.Length == 0) return "";
        var clean = options.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToArray();
        if (clean.Length == 0) return "";
        if (clean.Length == 1) return clean[0];

        string picked;
        bool timed;
        if (clean.Length == 2)
        {
            string body = string.IsNullOrWhiteSpace(prompt) ? $"Choose one:" : prompt;
            var ans = ShowCardReveal(card, title, body, RevealButtons.YesNo,
                yesLabel: clean[0], noLabel: clean[1], randomOnTimeout: true);
            timed = _revealTimedOut;
            picked = ans == RevealAnswer.Yes ? clean[0] : clean[1];
        }
        else
        {
            var fake = clean.Select(o => new Card { Name = o, Type = "Choice" }).ToList();
            var cardPick = PickCardFromList(
                string.IsNullOrWhiteSpace(prompt) ? "Click one option." : prompt,
                fake, title, card);
            timed = cardPick == null;
            picked = cardPick?.Name ?? clean[_autoSeedRng.Next(clean.Length)];
        }

        string how = timed ? "No answer in time — random choice" : "Chosen";
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"{title}: {picked}" + (timed ? " (timeout, random)" : ""));
        StatusText.Text = $"{title}: {picked}";
        if (timed)
            AnnounceChoiceResult(card, title, $"{how}:\n{picked}");
        return picked;
    }

    private int AskPlayer(Card? card, string title, string prompt) =>
        AskChoice(card, title, prompt, "Player 1", "Player 2")
            .StartsWith("Player 2", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
    private void BtnRevealYes_Click(object sender, RoutedEventArgs e) => CloseReveal(RevealAnswer.Yes);
    private void BtnRevealNo_Click(object sender, RoutedEventArgs e) => CloseReveal(RevealAnswer.No);

    private void BtnHistoryRefresh_Click(object sender, RoutedEventArgs e) => RefreshActionHistory();

    private void BtnHistoryCopy_Click(object sender, RoutedEventArgs e) => CopyActionHistorySelection();

    private void CopyActionHistorySelection()
    {
        try
        {
            var selected = ActionHistoryList?.SelectedItems.Cast<object>()
                .Select(o => o?.ToString() ?? "")
                .Where(s => s.Length > 0)
                .ToList() ?? new List<string>();
            IEnumerable<string> lines = selected.Count > 0
                ? selected
                : _session.Log.FormatLines(500, includeDebug: _devShowDebugLog);
            string text = string.Join(Environment.NewLine, lines);
            Clipboard.SetText(string.IsNullOrEmpty(text) ? "(empty)" : text);
            StatusText.Text = selected.Count > 0
                ? $"Copied {selected.Count} history line(s)."
                : "Action History copied to clipboard.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Copy failed: " + ex.Message;
        }
    }

    private void TableWindow_Loaded(object sender, RoutedEventArgs e)
    {
        DebugLog.StartSession("TableWindow");
        DebugLog.Enabled = DevFileLogItem?.IsChecked != false;
        // File log always gets Move/Beam/Target. Action History skip while refreshing —
        // LegalMoves fly-eval is extremely chatty and used to re-enter via Changed.
        DebugLog.HistorySink = (turn, actor, text) =>
        {
            if (_historyRefreshing) return;
            _session.Log.AddDebug(turn, actor, text);
        };
        CheckTrace.Emit = msg =>
        {
            if (_devShowDebugLog)
                _session.Log.AddDebug(_session.TurnNumber, "Check", msg);
        };
        BuildFixedZones();
        UpdatePhaseControls();
        if (TableScroll != null)
            TableScroll.PreviewMouseWheel += TableCanvas_MouseWheel;

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
    private List<Card> CardsForStripDisplay(IReadOnlyList<Card> source, string zoneName)
    {
        if (source == null || source.Count == 0)
            return new List<Card>();
        bool sort = _aidSortHand && zoneName == "Hand";
        if (!sort)
            return source.ToList();
        return source
            .OrderBy(CardKinds.HandSortRank)
            .ThenBy(c => c.Name ?? "", StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<Card> ShuffledCopy(IReadOnlyList<Card> source)
    {
        var list = source.ToList();
        var rng = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

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

        var cards = CardsForStripDisplay(GetCardsForZone(zoneName, opponent), zoneName);
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
        var hand = CardsForStripDisplay(opponent ? _oppHandCards : _handCards, "Hand");
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

    private static bool IsDoorwayCard(Card c) => CardKinds.IsDoorway(c);

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
        if (zoneName == "Q's Tent" && zonePlayer == _activePlayer && !_seedPhaseActive
            && _lastZoneClick == box && (now - _lastZoneClickTime).TotalMilliseconds < 350)
        {
            var hand = _activePlayer == 2 ? _oppHandCards : _handCards;
            var door = hand.FirstOrDefault(c =>
                string.Equals(GetSideDeckForDoorway(c), "Q's Tent", StringComparison.OrdinalIgnoreCase));
            if (door != null)
            {
                TryDownloadFromTent(door, null);
                _lastZoneClick = null;
                e.Handled = true;
                return;
            }
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
            else if (UpdateHandReturnSnap(winPos))
            {
                // Karte zurück in die Hand — kein Play
            }
            else if (CardDetailOverlay?.Visibility == Visibility.Visible && _peekLegalTargets.Count > 0)
            {
                // Peek lock: only the open detail strip may snap.
            }
            else if (UpdateNullifyTableSnap(winPos))
            {
                // Kevin / Devil snap onto a TABLE-column event
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
                else if (!(CardDetailOverlay?.Visibility == Visibility.Visible && _peekLegalTargets.Count > 0))
                    UpdateSnapPreviewFromWindow(winPos);
            }
            UpdateStackPeek(winPos);
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

    private bool UpdateHandReturnSnap(Point windowPos)
    {
        if (_zoneDragRef == null || _zoneDragRef.ZoneName != "Hand")
        {
            HighlightHandReturn(false);
            return false;
        }
        var strip = _zoneDragRef.Opponent ? OppHandStripBorder : PlayerHandStripBorder;
        var zoneBox = FindZoneBorder("Hand", _zoneDragRef.Opponent);
        bool over = (strip != null && IsPointOverElement(strip, windowPos))
                    || (zoneBox != null && IsPointNearElement(zoneBox, windowPos, 20));
        if (!over)
        {
            HighlightHandReturn(false);
            return false;
        }
        HighlightHandReturn(true);
        return true;
    }

    private Border? _handReturnPrev;
    private Brush? _handReturnPrevBrush;
    private Thickness _handReturnPrevThickness;

    private void HighlightHandReturn(bool on)
    {
        if (!on)
        {
            if (_handReturnPrev != null)
            {
                _handReturnPrev.BorderBrush = _handReturnPrevBrush;
                _handReturnPrev.BorderThickness = _handReturnPrevThickness;
                _handReturnPrev = null;
            }
            return;
        }
        var strip = _zoneDragRef?.Opponent == true ? OppHandStripBorder : PlayerHandStripBorder;
        if (strip == null || ReferenceEquals(_handReturnPrev, strip)) return;
        HighlightHandReturn(false);
        _handReturnPrev = strip;
        _handReturnPrevBrush = strip.BorderBrush;
        _handReturnPrevThickness = strip.BorderThickness;
        strip.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 220, 120));
        strip.BorderThickness = new Thickness(3);
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

    private bool UpdateNullifyTableSnap(Point windowPos)
    {
        if (_dragCard?.Tag is not Card drag) return false;
        if (!InterruptRules.IsKevinNullify(drag) && !InterruptRules.IsDevil(drag))
            return false;

        bool Legal(Card ev) => InterruptRules.IsDevil(drag)
            ? TimingRules.CanDevilTarget(ev).ok
            : TimingRules.CanKevinTargetEvent(ev).ok;

        Border? hitMini = null;
        Card? hitCard = null;
        foreach (var panel in new[] { TablePermanentsPanel, OppTablePermanentsPanel })
        {
            if (panel == null) continue;
            foreach (var mini in panel.Children.OfType<Border>())
            {
                if (mini.Tag is not Card ev || !Legal(ev)) continue;
                try
                {
                    var tl = mini.TransformToAncestor(this).Transform(new Point(0, 0));
                    var rect = new Rect(tl, mini.RenderSize);
                    rect.Inflate(10, 10);
                    if (!rect.Contains(windowPos)) continue;
                    hitMini = mini;
                    hitCard = ev;
                    break;
                }
                catch { }
            }
            if (hitMini != null) break;
        }

        _tableColumnSnapCard = hitCard;
        HighlightKevinTableMinis(Legal);
        if (hitMini != null && hitCard != null)
        {
            hitMini.BorderBrush = new SolidColorBrush(Color.FromRgb(40, 255, 120));
            hitMini.BorderThickness = new Thickness(4);
            hitMini.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(40, 255, 120),
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.95
            };
            _snapSite = new TargetSite(TargetSiteKind.TableCard, hitCard, hitCard, null,
                TargetWhy.Nullify, "Nullify that Event.");
            StatusText.Text = $"Snap: {hitCard.Name} — drop to nullify.";
            return true;
        }
        return false;
    }

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
        bool boardSnap = TargetingRules.UsesBoardSnap(card);
        if (!IsStackableCard(card) && !IsDockableUnderMission(card)
            && !(IsSeedableUnderMission(card) && _seedPhaseActive)
            && !eventTarget && !boardSnap)
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
        // Schism (and other responses) used to always open a second 10s window.
        // Skip it when nobody has a legal response to the response itself (Amanda/Q2).
        if (isResponse && _stack.Top != null
            && LegalResponsesFor(_handCards, _stack.Top, 1).Count == 0
            && LegalResponsesFor(_oppHandCards, _stack.Top, 2).Count == 0)
        {
            ResolveEntireStack();
            return;
        }
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

    private void SyncSchismRound()
    {
        int round = Math.Max(1, _session.TurnNumber);
        if (_schismRound != round)
        {
            _schismUsedBy.Clear();
            _schismRound = round;
        }
    }

    private bool SchismAvailable(int player)
    {
        SyncSchismRound();
        return !_schismUsedBy.Contains(player);
    }

    private void MarkSchismUsed(int player)
    {
        if (player is 1 or 2) _schismUsedBy.Add(player);
    }

    private List<Card> LegalResponsesFor(IEnumerable<Card> hand, TimingRules.PendingAction top, int owner)
    {
        return TimingRules.LegalResponsesInHand(hand, top, owner)
            .Where(c => !InterruptRules.IsSubspaceSchism(c) || SchismAvailable(owner))
            .ToList();
    }

    private void ApplyResponseEffect(TimingRules.PendingAction response)
    {
        if (_stack.Items.Count < 2 || response.Card == null) return;
        var target = _stack.Items[^2];
        var check = TimingRules.CanRespond(response.Card, target, response.Controller);
        if (!check.ok) return;

        target.Cancelled = true;
        target.CancelledBy = response.Card.Name;
        if (InterruptRules.IsSubspaceSchism(response.Card))
            MarkSchismUsed(response.Controller);
        if (InterruptRules.IsEscapePod(response.Card) && target.Kind == TimingRules.ActionKind.ShipDestroyed)
            ApplyEscapePodFromResponse(response.Controller, target);
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

        CloseCardDetailPopup();
        var top = _stack.Top;
        int responder = FirstLegalResponder(top, _stack.ResponsePlayer);
        _stack.ResponsePlayer = responder;
        var hand = responder == 1 ? _handCards : _oppHandCards;
        var legal = LegalResponsesFor(hand, top, responder);

        Card? shown = top.Card ?? top.AttackerCard;
        RevealTitle.Text = top.IsResponse
            ? $"Response — Player {top.Controller}"
            : top.Kind == TimingRules.ActionKind.DrawCard
                ? $"Player {top.Controller} draws"
                : $"Player {top.Controller} plays";
        RevealSubtitle.Text = shown != null
            ? $"{shown.Name}  ·  {shown.Type}"
            : top.Summary;
        RevealBody.Text = (shown?.Text ?? top.Summary) + "\n\n" + TimingRules.FormatStack(_stack);

        FillRevealImage(shown);

        BtnRevealYes.Visibility = Visibility.Collapsed;
        BtnRevealNo.Visibility = Visibility.Collapsed;

        // No legal cards for this responder
        if (legal.Count == 0)
        {
            int other = opponentOf(responder);
            var otherHand = other == 1 ? _handCards : _oppHandCards;
            var otherLegal = LegalResponsesFor(otherHand, top, other);

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

            // Nobody left who can respond — resolve, no extra empty window.
            if (otherLegal.Count == 0 || _stack.ConsecutivePasses > 0)
            {
                ResolveEntireStack();
                return;
            }

            // This player has nothing — skip the empty 5s card and go to whoever can respond.
            _stack.ConsecutivePasses++;
            _session.Log.Add(_session.TurnNumber, $"P{responder}", "Pass (no legal response)");
            _stack.ResponsePlayer = other;
            ShowActionAnnounce();
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
        int other = opponentOf(_stack.ResponsePlayer);
        var otherHand = other == 1 ? _handCards : _oppHandCards;
        bool otherCan = _stack.Top != null
            && LegalResponsesFor(otherHand, _stack.Top, other).Count > 0;
        if (_stack.ConsecutivePasses >= 2 || !otherCan)
        {
            ResolveEntireStack();
            return;
        }
        _stack.ResponsePlayer = other;
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
        if (_endTurnAfterDrawStack && !_stack.IsOpen)
        {
            int who = _endTurnFinishingPlayer;
            FinishEndOfTurnDrawExtras(who);
            if (_stack.IsOpen && _stack.Top?.Kind == TimingRules.ActionKind.DrawCard)
                return;
            CompleteTurnChange();
        }
    }

    private void ResolveTopOfStack()
    {
        if (!_stack.IsOpen) return;
        var a = _stack.Pop();

        if (a.Kind == TimingRules.ActionKind.PlayCard && a.Card != null)
        {
            if (a.Cancelled)
            {
                if (InterruptRules.IsWormhole(a.Card))
                    _wormholeShip = null;
                var dest = a.CancelledBy != null
                           && a.CancelledBy.Equals("Energy Vortex", StringComparison.OrdinalIgnoreCase)
                    ? TimingRules.Destination.ReturnToHand
                    : TimingRules.Destination.Discard;
                if (dest == TimingRules.Destination.ReturnToHand)
                    UndoTablePlacement(a.Card);
                SendCardTo(a.Card, a.Controller, dest);
                if (dest == TimingRules.Destination.ReturnToHand
                    && a.CancelledBy != null
                    && a.CancelledBy.Equals("Energy Vortex", StringComparison.OrdinalIgnoreCase))
                {
                    _energyVortexBlocked.Add(a.Card);
                    if (_session.Segment == GameSession.TurnSegment.Execute
                        && !_session.NormalCardPlayForfeited)
                    {
                        // Replacement play is still the same normal card play (costs stay paid).
                    }
                    StatusText.Text =
                        $"{a.Card.Name} cancelled by Energy Vortex — returns to hand. "
                        + "Play a different card as that normal card play (not this one).";
                    _session.Log.Add(_session.TurnNumber, $"P{a.Controller}",
                        $"{a.Card.Name} Energy Vortex → hand; this copy blocked for the replacement play");
                }
                else
                {
                    StatusText.Text = $"{a.Card.Name} cancelled ({a.CancelledBy}).";
                    _session.Log.Add(_session.TurnNumber, $"P{a.Controller}",
                        $"{a.Card.Name} cancelled by {a.CancelledBy}");
                }
                return;
            }

            if (a.IsResponse)
            {
                bool attachStay = InterruptRules.NameIs(a.Card, "Asteroid Sanctuary")
                                  || InterruptRules.NameIs(a.Card, "Distortion of Space/Time Continuum")
                                  || InterruptRules.NameIs(a.Card, "Tachyon Detection Grid");
                // Kevin (etc.) may still need TargetCard nullify when used as a response
                if ((TimingRules.IsInterrupt(a.Card) || InterruptRules.IsInterrupt(a.Card))
                    && (a.TargetCard != null || attachStay))
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

        if (a.Kind == TimingRules.ActionKind.DrawCard && a.Card != null)
        {
            int drawer = a.Controller;
            var hand = drawer == 1 ? _handCards : _oppHandCards;
            if (a.Cancelled)
            {
                /* marked when the Schism response resolved */
                SendCardTo(a.Card, drawer, TimingRules.Destination.Discard);
                StatusText.Text =
                    $"Subspace Schism discards {a.Card.Name}. P{drawer} draws the next card.";
                _session.Log.Add(_session.TurnNumber, $"P{drawer}",
                    $"Subspace Schism discarded draw {a.Card.Name}");
                DrawOneToHandFor(drawer, endOfTurn: false, skipSchism: true);
                return;
            }
            hand.Add(a.Card);
            _session.MarkDrawn();
            RefreshZoneCounts();
            RefreshHandStrips();
            StatusText.Text = $"P{drawer} draws {a.Card.Name}.";
            return;
        }

        if (a.Kind == TimingRules.ActionKind.ShipDestroyed)
        {
            var shipB = a.AttackerHost as Border;
            if (shipB == null || a.Card == null) return;
            if (a.Cancelled && string.Equals(a.CancelledBy, "Escape Pod", StringComparison.OrdinalIgnoreCase))
            {
                // Crew already moved in ApplyEscapePodFromResponse.
            }
            _resolvingDestroy = true;
            DestroyShipOrFacility(shipB, a.Card, a.Controller);
            _resolvingDestroy = false;
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
        if (dest != TimingRules.Destination.Table)
            OnCardLeftPlay(card);
        switch (dest)
        {
            case TimingRules.Destination.ReturnToHand:
                ReturnCardToHand(card, owner);
                break;
            case TimingRules.Destination.OutOfPlay:
                _tablePermanentCards.Remove(card);
                _oppTablePermanentCards.Remove(card);
                (owner == 2 ? _outOfPlayP2 : _outOfPlayP1).Add(card);
                RebuildTablePermanentsPanel();
                RefreshZoneCounts();
                StatusText.Text = $"{card.Name} out-of-play.";
                _session.Log.Add(_session.TurnNumber, $"P{owner}", $"{card.Name} → out-of-play");
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

    private bool OpponentHoldsEnergyVortex(int controller)
    {
        var hand = controller == 1 ? _oppHandCards : _handCards;
        return hand.Any(c =>
            (c.Name ?? "").Equals("Energy Vortex", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Open the 10s window so Energy Vortex can bounce this normal card play.</summary>
    private bool TryAnnounceNormalPlayForVortex(Card card, int owner, Border? host)
    {
        if (_seedPhaseActive || _stack.IsOpen) return false;
        if (!GameSession.UsesNormalCardPlay(card)) return false;
        if (!OpponentHoldsEnergyVortex(owner)) return false;
        BeginPlayCardStack(card, isResponse: false, controllerOverride: owner);
        if (_stack.Top != null)
            _stack.Top.AttackerHost = host;
        return true;
    }

    private void UndoTablePlacement(Card card)
    {
        var b = FindBorderForCard(card);
        if (b != null)
        {
            foreach (var kv in _stackOnHost.ToList())
            {
                if (kv.Value.Contains(b))
                    RemoveCardFromHostStack(kv.Key, b);
            }
            var mission = FindMissionForDockable(b);
            if (TableCanvas.Children.Contains(b))
                TableCanvas.Children.Remove(b);
            if (mission != null)
                RelayoutDockablesUnderMission(mission);
        }
        foreach (var list in new[] { _tablePermanentCards, _oppTablePermanentCards })
            list.Remove(card);
        RebuildTablePermanentsPanel();
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
            string pick = AskChoice(defenderCard, "Return Fire?",
                $"{attackerShip.Name} attacks {defenderCard.Name}.\n" +
                $"Attacker WEAPONS {BattleRules.GetWeapons(attackerShip)} · " +
                $"target SHIELDS {BattleRules.GetShields(defenderCard)}.\n" +
                $"P{defOwner}: return fire (WEAPONS {defWeapons})?",
                "Return Fire", "No");
            returnFire = pick.StartsWith("Return", StringComparison.OrdinalIgnoreCase);
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
        ShowCardReveal(atkCards.FirstOrDefault() ?? defCards.FirstOrDefault(),
            "Personnel Battle", result.LogSummary, RevealButtons.Ok);
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
            if (InterruptRules.IsSubspaceSchism(card)
                && !(_stack.IsOpen && _stack.Top?.Kind == TimingRules.ActionKind.DrawCard))
            {
                denyReason = "Subspace Schism plays when a player would draw a card.";
                return false;
            }
            if (TimingRules.IsInterrupt(card) && GoddessBlocksInterrupt(card))
            {
                denyReason = "Goddess of Empathy: interrupts may not be played (except Kevin/Q2/Q/Ref).";
                return false;
            }
            return true;
        }

        bool forFree = PlayRules.PlaysForFree(card);

        if (_energyVortexBlocked.Contains(card) && GameSession.UsesNormalCardPlay(card))
        {
            denyReason =
                "Energy Vortex: that card was cancelled and returned to hand. "
                + "Play a different card as the replacement normal card play.";
            return false;
        }

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

        NoteAuPlay(card, _activePlayer);
        if (ModifierRules.IsPersonnelCard(card) && !ModifierRules.IsUniversalNonHolo(card)
            && !(card.Uniqueness ?? "").Contains("univ", StringComparison.OrdinalIgnoreCase))
        {
            if (_activePlayer == 1) _uniquePersonnelPlayedP1 = true;
            else _uniquePersonnelPlayedP2 = true;
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


    /// <summary>Alle Karten "im Spiel" f├╝r Unique-Checks (Hosts, Dockables, Tisch, ÔÇª).</summary>
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
            // E4: Unique/persona by Owner (Glossary); Lore control does not free the persona slot.
            int o = c.OwnerPlayer != 0 ? c.OwnerPlayer : GetBorderOwner(b);
            if (o == 0) o = 1;
            if (o != owner) continue;
            // Missionen z├ñhlen f├╝r not-duplicatable / shared sp├ñter
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

        // E4: BoardStore.InPlay by Owner (persona restrict stays with owner / Lore); canvas fallback.
        PlayRules.EnterPlayResult result;
        if (BoardStore.Current.HasInPlaySurface)
            result = PlayRules.CanEnterPlay(card, _activePlayer, BoardStore.Current);
        else
        {
            var owned = CollectCardsInPlay(_activePlayer == 2);
            var all = CollectAllCardsInPlay();
            result = PlayRules.CanEnterPlay(card, owned, all, _activePlayer);
        }
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
        CancelStackPeek(keepOverlay: true);
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

        // Drop back on own hand = cancel play, never resolve Kevin/Devil.
        if (zref.ZoneName == "Hand"
            && TryReturnCardToZone(card, cardBorder, zref.ZoneName, windowPos, zref.Opponent))
        {
            EndTargetSession();
            ClearEventTargetHighlights();
            placedOk = true;
        }
        // Kevin / The Devil target a card already on TABLE — must NOT commit as a
        // TABLE permanent first (that path opens the stack with no TargetCard).
        else if (!_seedPhaseActive && zref.ZoneName == "Hand"
            && InterruptRules.IsInterrupt(card)
            && (InterruptRules.IsKevinNullify(card) || InterruptRules.IsDevil(card)))
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
        // TABLE drop first — do not treat the right column as "return to hand"
        else if (IsTablePermanentType(card) && IsPointOverOwnTable(windowPos)
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
                if (!_seedPhaseActive && zref.ZoneName == "Hand")
                    TryAnnounceNormalPlayForVortex(card, owner, targetMission);
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
                    TryAnnounceNormalPlayForVortex(card, owner, facility);
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
                        TryAnnounceNormalPlayForVortex(card, owner, host);
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
        // Side-Deck-Doorways only as covers, not TABLE.
        if (CardKinds.IsDoorway(c) && GetSideDeckForDoorway(c) != null)
            return false;
        if (CardKinds.IsDoorway(c) || CardKinds.IsEvent(c) || CardKinds.IsObjective(c)
            || CardKinds.IsIncident(c) || CardKinds.IsInterrupt(c))
            return true;
        // Artifacts seed under missions in Dilemma phase; otherwise TABLE.
        if (CardKinds.IsArtifact(c) && !(_seedPhaseActive && _seedSubPhase == SeedSubPhase.Dilemma))
            return true;
        return CardKinds.IsCorePermanent(c);
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

        if (!_seedPhaseActive && _session.Match == GameSession.MatchPhase.Play
            && InterruptRules.IsInterrupt(card)
            && (InterruptRules.IsKevinNullify(card) || InterruptRules.IsDevil(card)))
        {
            int owner = _activePlayer;
            // Missed target must not eat the card (TABLE-column drop path).
            if (!TryPlayInterruptFromHand(card, floating, new Point(-999, -999), owner))
                ReturnCardToHand(card, owner);
            return;
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
        if (CardIcons.HasHiddenAgenda(card))
            card.FaceUp = false;
        var list = owner == 2 ? _oppTablePermanentCards : _tablePermanentCards;
        if (!list.Contains(card))
            list.Add(card);
        if (ArtifactRules.IsHorgahn(card))
            SetHorgahnFlag(owner, true);
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
                bool hideFace = CardIcons.HasHiddenAgenda(card) && !card.FaceUp;
                if (hideFace && _cardBackImage != null)
                {
                    img.Source = _cardBackImage;
                    mini.ToolTip = "Hidden Agenda (face-down)";
                    mini.BorderBrush = new SolidColorBrush(Color.FromRgb(160, 120, 40));
                }
                else if (!string.IsNullOrEmpty(card.FullImagePath) && System.IO.File.Exists(card.FullImagePath))
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
                    int owner = _tablePermanentCards.Contains(cRef) ? 1 : 2;
                    if (CardIcons.HasHiddenAgenda(cRef) && !cRef.FaceUp)
                    {
                        if (owner == _activePlayer && !_seedPhaseActive)
                            TryFlipHiddenAgenda(cRef, owner);
                        else
                            StatusText.Text = "Hidden Agenda (face-down).";
                        e.Handled = true;
                        return;
                    }
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

    private static bool SameTableCard(Card a, Card b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.InstanceId != 0 && b.InstanceId != 0 && a.InstanceId == b.InstanceId)
            return true;
        return string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
               && string.Equals(a.SetFolder ?? "", b.SetFolder ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TableListHas(List<Card> list, Card card) =>
        list.Any(c => SameTableCard(c, card));

    private static void DedupTablePermanents(List<Card> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < i; j++)
            {
                if (SameTableCard(list[i], list[j]))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private Card? ResolveAttachedEventCard(CardRef? r, int owner)
    {
        if (r == null) return null;
        var pool = owner == 2 ? _oppTablePermanentCards : _tablePermanentCards;
        var existing = pool.FirstOrDefault(c =>
            string.Equals(c.Name, r.Name, StringComparison.OrdinalIgnoreCase)
            && (r.InstanceId == 0 || c.InstanceId == 0 || c.InstanceId == r.InstanceId));
        return existing ?? ResolveCard(r);
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
            if (string.Equals(targetZone, "Q's Tent", StringComparison.OrdinalIgnoreCase)
                && !_seedPhaseActive)
                return TryDownloadFromTent(doorway, cardBorder);
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

    /// <summary>4.2: each player uses only their seeded copy at a shared location.</summary>
    private Card MissionPrintedFor(Border location, int player)
    {
        if (location.Tag is not Card primary)
            return new Card { Name = "?", Type = "Mission" };
        if (GetBorderOwner(location) == player)
            return primary;
        if (_sharedMissionCopies.TryGetValue(location, out var copies))
        {
            foreach (var b in copies)
            {
                if (GetBorderOwner(b) == player && b.Tag is Card copy)
                    return copy;
            }
        }
        return primary;
    }

    /// <summary>4.2.0.3: shared location is both "your mission" and "opponent's mission".</summary>
    private bool LocationIsYourMission(Border location, int player)
    {
        if (GetBorderOwner(location) == player) return true;
        return _sharedMissionCopies.TryGetValue(location, out var copies)
               && copies.Any(b => GetBorderOwner(b) == player);
    }

    private bool LocationIsOpponentMission(Border location, int player)
    {
        if (_sharedMissionCopies.TryGetValue(location, out var copies) && copies.Count > 0)
            return true;
        int o = GetBorderOwner(location);
        return o is 1 or 2 && o != player;
    }

    private bool IsSharedMissionLocation(Border primary) =>
        _sharedMissionCopies.TryGetValue(primary, out var copies) && copies.Count > 0;

    private void ApplyMissionFaceVisual(Border primary, double x)
    {
        bool shared = IsSharedMissionLocation(primary);
        int face = shared
            ? (_activePlayer is 1 or 2 ? _activePlayer : 1)
            : (GetBorderOwner(primary) == 2 ? 2 : 1);
        if (shared)
        {
            var printed = MissionPrintedFor(primary, face);
            if (primary.Child is Image img
                && !string.IsNullOrEmpty(printed.FullImagePath)
                && System.IO.File.Exists(printed.FullImagePath))
            {
                string? current = (img.Source as BitmapImage)?.UriSource?.LocalPath;
                if (!string.Equals(current, printed.FullImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.UriSource = new Uri(printed.FullImagePath, UriKind.Absolute);
                        bmp.DecodePixelWidth = 200;
                        bmp.EndInit();
                        img.Source = bmp;
                    }
                    catch { }
                }
            }
        }
        primary.RenderTransformOrigin = new Point(0.5, 0.5);
        primary.RenderTransform = face == 2
            ? new RotateTransform(180)
            : Transform.Identity;
        Canvas.SetLeft(primary, x);
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

        var display = BuildSpacelineDisplayOrder();
        if (display.Count == 0) return;

        double width = 0;
        for (int i = 0; i < display.Count; i++)
        {
            if (i > 0)
            {
                string pq = GetSpacelineQuadrant(display[i - 1]);
                string cq = GetSpacelineQuadrant(display[i]);
                width += (pq != cq) ? (TableCardWidth + MissionGap) : MissionGap;
            }
            width += TableCardWidth;
        }

        double canvasW = TableCanvas.ActualWidth;
        if (double.IsNaN(canvasW) || canvasW < 200)
            canvasW = TableCanvas.Width;
        if (double.IsNaN(canvasW) || canvasW < 200)
            canvasW = 1600;
        double x = Math.Max(40, canvasW / 2.0 - width / 2.0);
        for (int i = 0; i < display.Count; i++)
        {
            if (i > 0)
            {
                string pq = GetSpacelineQuadrant(display[i - 1]);
                string cq = GetSpacelineQuadrant(display[i]);
                x += (pq != cq) ? (TableCardWidth + MissionGap) : MissionGap;
            }
            var cell = display[i];
            Canvas.SetTop(cell, SpacelineY);
            if (cell.Tag is Card spanCard && IsSpacelineSpanCard(spanCard))
            {
                cell.RenderTransform = Transform.Identity;
                Canvas.SetLeft(cell, x);
                Panel.SetZIndex(cell, 9);
            }
            else
            {
                ApplyMissionFaceVisual(cell, x);
            }
            if (cell.Tag is Card oc && IsMissionCard(oc))
            {
                UpdateSeedBadge(cell);
                if (dockByMission.TryGetValue(cell, out var docks))
                {
                    foreach (var d in docks)
                        Canvas.SetLeft(d, x);
                }
                RelayoutDockablesUnderMission(cell);
            }
            if (_missionSolvedLabels.TryGetValue(cell, out var solvedLbl))
            {
                Canvas.SetLeft(solvedLbl, x);
                Canvas.SetTop(solvedLbl, SpacelineY - 16);
            }
            if (cell.Tag is Card mc && IsMissionCard(mc))
                LayoutSharedMissionCopies(cell, x);
            x += TableCardWidth;
        }
    }

    /// <summary>Missions in seed order, each Gaps/Q-Net spliced after its left-hand mission.</summary>
    private List<Border> BuildSpacelineDisplayOrder()
    {
        var missions = _spacelineOrder
            .Where(b => b.Tag is Card c && IsMissionCard(c))
            .ToList();
        var spans = _spacelineOrder
            .Where(b => b.Tag is Card c && IsSpacelineSpanCard(c))
            .ToList();
        var used = new HashSet<Border>();
        var display = new List<Border>();
        for (int i = 0; i < missions.Count; i++)
        {
            display.Add(missions[i]);
            foreach (var span in spans)
            {
                if (!used.Add(span)) continue;
                var (left, right) = SpanEndpoints(span);
                bool afterThis = left != null && ReferenceEquals(left, missions[i]);
                if (!afterThis && right != null && i + 1 < missions.Count
                    && ReferenceEquals(right, missions[i + 1])
                    && (left == null || missions.IndexOf(left) <= i))
                    afterThis = true;
                if (!afterThis)
                {
                    used.Remove(span);
                    continue;
                }
                display.Add(span);
            }
        }
        foreach (var span in spans)
            if (used.Add(span))
                display.Add(span);
        return display;
    }

    private (Border? left, Border? right) SpanEndpoints(Border span)
    {
        var card = span.Tag as Card;
        var ae = _attachedEvents.FirstOrDefault(e =>
            ReferenceEquals(e.Card, card)
            && e.Kind is EventRules.Persist.QNet or EventRules.Persist.Gaps);
        if (ae?.Host != null && ae.Host2 != null)
            return (ResolveOnSpaceline(ae.Host), ResolveOnSpaceline(ae.Host2));

        int i = _spacelineOrder.IndexOf(span);
        Border? left = null, right = null;
        for (int j = i - 1; j >= 0; j--)
            if (_spacelineOrder[j].Tag is Card c && IsMissionCard(c))
            { left = _spacelineOrder[j]; break; }
        for (int j = i + 1; j < _spacelineOrder.Count; j++)
            if (_spacelineOrder[j].Tag is Card c && IsMissionCard(c))
            { right = _spacelineOrder[j]; break; }
        return (left, right);
    }

    private Border ResolveOnSpaceline(Border b)
    {
        if (_spacelineOrder.Contains(b)) return b;
        if (b.Tag is not Card c) return b;
        return _spacelineOrder.FirstOrDefault(x =>
                   x.Tag is Card xc
                   && (ReferenceEquals(xc, c)
                       || string.Equals(xc.Name, c.Name, StringComparison.OrdinalIgnoreCase)
                          && IsMissionCard(xc)))
               ?? b;
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
        // Same unique location — keep the extra card for ownership, do not draw a second mission.
        copy.Visibility = Visibility.Collapsed;
        copy.IsHitTestVisible = false;
        Canvas.SetLeft(copy, Canvas.GetLeft(primary));
        Canvas.SetTop(copy, Canvas.GetTop(primary));
        LayoutSharedMissionCopies(primary, Canvas.GetLeft(primary));
        UpdateSeedBadge(primary);
        StatusText.Text =
            $"Shared unique mission: {((primary.Tag as Card)?.Name)} — one location, both players.";
    }

    private void LayoutSharedMissionCopies(Border primary, double primaryX)
    {
        if (!_sharedMissionCopies.TryGetValue(primary, out var list) || list.Count == 0)
            return;
        double y = Canvas.GetTop(primary);
        if (double.IsNaN(y)) y = SpacelineY;
        foreach (var copy in list)
        {
            copy.Visibility = Visibility.Collapsed;
            copy.IsHitTestVisible = false;
            Canvas.SetLeft(copy, primaryX);
            Canvas.SetTop(copy, y);
            Panel.SetZIndex(copy, 0);
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

    // Seed legality lives in SeedRules (shared with Quick Game / later AI).
    private static (bool ok, string reason) CanSeedCardUnderMission(Card seedCard, Card mission) =>
        SeedRules.CanSeedUnderMission(seedCard, mission);

    private static (bool ok, string reason) CanSeedFacilityAtMission(Card facility, Card mission) =>
        SeedRules.CanSeedFacilityAt(facility, mission);

    private static (bool planet, bool space) GetMissionLocationIcons(Card mission) =>
        SeedRules.GetMissionLocation(mission);



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

        if (!EnsureAffiliationModeForHost(card, host))
            return (false, $"{card.Name} has no affiliation mode compatible with {host.Name}.");

        // Persona-Limit zusätzlich
        // E4: unique/persona by Owner from BoardStore when ready.
        var owned = BoardStore.Current.HasInPlaySurface
            ? BoardStore.Current.InPlay(player, BoardStore.InPlaySide.Owner).ToList()
            : CollectCardsInPlay(player == 2);
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

    private bool EnsureAffiliationModeForHost(Card card, Card host)
    {
        if (!DualAffiliationRules.IsMulti(card)) return true;
        var modes = DualAffiliationRules.PrintedModes(card);
        var treaties = GetActiveTreaties(_activePlayer);
        var compatible = new List<string>();
        string? saved = card.CurrentAffiliation;
        foreach (var m in modes)
        {
            card.CurrentAffiliation = m;
            if (ReportingRules.AreCompatible(card, host, treatyAllowsMix: false, treaties))
                compatible.Add(m);
        }
        card.CurrentAffiliation = saved;
        if (compatible.Count == 0) return false;
        if (!string.IsNullOrWhiteSpace(card.CurrentAffiliation)
            && compatible.Contains(ReportingRules.NormalizeAffil(card.CurrentAffiliation)))
            return true;
        if (compatible.Count == 1)
        {
            DualAffiliationRules.TrySetMode(card, compatible[0]);
            return true;
        }
        string a = compatible[0], b = compatible[1];
        string picked = AskChoice(card, "Report affiliation",
            $"Report {card.Name} as which affiliation?",
            DualAffiliationRules.DisplayName(a), DualAffiliationRules.DisplayName(b));
        string mode = string.Equals(picked, DualAffiliationRules.DisplayName(a), StringComparison.OrdinalIgnoreCase) ? a : b;
        DualAffiliationRules.TrySetMode(card, mode);
        return true;
    }

    private static string NextAffiliationMode(Card card)
    {
        var modes = DualAffiliationRules.PrintedModes(card);
        if (modes.Count == 0) return "FED";
        string cur = DualAffiliationRules.CurrentMode(card) ?? modes[0];
        int i = modes.FindIndex(m => string.Equals(m, cur, StringComparison.OrdinalIgnoreCase));
        return modes[(i + 1) % modes.Count];
    }

    private void TrySwitchAffiliation(Border border, Card card, string next)
    {
        if (_attemptMission != null)
        {
            ShowPlayError("Cannot change affiliation during a mission attempt.");
            return;
        }
        string? prev = card.CurrentAffiliation;
        if (!DualAffiliationRules.TrySetMode(card, next))
            return;

        Border? host = null;
        foreach (var kv in _stackOnHost)
        {
            if (kv.Value.Contains(border))
            {
                host = kv.Key;
                break;
            }
        }
        if (host != null && host.Tag is Card hc
            && (IsShipCard(hc) || ReportingRules.IsFacilityHost(hc)))
        {
            var treaties = GetActiveTreaties(_activePlayer);
            if (!ReportingRules.AreCompatible(card, hc, false, treaties))
            {
                card.CurrentAffiliation = prev;
                ShowPlayError($"Cannot switch to {DualAffiliationRules.DisplayName(next)} while aboard {hc.Name} (incompatible).");
                return;
            }
        }

        StatusText.Text = $"{card.Name} is now {DualAffiliationRules.DisplayName(next)}.";
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"{card.Name} affiliation → {next}");
        ClearCardActionUi();
        if (host != null) UpdateHostBadge(host);
    }

    private static bool AffiliationsCompatible(Card facility, Card mission) =>
        SeedRules.AffiliationsCompatible(facility, mission);

    private static HashSet<string> ParseAffiliationTokens(string? raw) =>
        SeedRules.ParseAffiliationTokens(raw);

    private void RemoveCardFromZone(string zoneName, Card card, bool opponent = false)
    {
        GetZoneList(zoneName, opponent)?.Remove(card);
        RefreshZoneCounts();
    }

    /// <summary>
    /// Eine Karte vom Draw auf die Hand legen (Sandbox).
    /// </summary>
    private void DrawOneToHand(bool endOfTurn = false) =>
        DrawOneToHandFor(_activePlayer, endOfTurn);

    private void DrawOneToHandFor(int player, bool endOfTurn = false)
        => DrawOneToHandFor(player, endOfTurn, skipSchism: false);

    private void DrawOneToHandFor(int player, bool endOfTurn, bool skipSchism)
    {
        if (_seedPhaseActive)
        {
            StatusText.Text = "SEED PHASE – finish seed before drawing";
            return;
        }
        var draw = player == 1 ? _drawCards : _oppDrawCards;
        var hand = player == 1 ? _handCards : _oppHandCards;
        if (draw.Count == 0)
        {
            StatusText.Text = "Draw-Deck ist leer";
            return;
        }
        var card = draw[0];
        draw.RemoveAt(0);

        SyncSchismRound();
        bool anyoneHasSchism =
            (SchismAvailable(1) && _handCards.Any(InterruptRules.IsSubspaceSchism))
            || (SchismAvailable(2) && _oppHandCards.Any(InterruptRules.IsSubspaceSchism));
        if (!skipSchism && anyoneHasSchism && !_stack.IsOpen)
        {
            _stack.Push(new TimingRules.PendingAction
            {
                Kind = TimingRules.ActionKind.DrawCard,
                Controller = player,
                Card = card,
                Summary = $"P{player} would draw {card.Name}"
            });
            OpenResponseWindow(opponentOf(player));
            ScheduleActionAnnounce(400);
            StatusText.Text = $"P{player} would draw {card.Name} — response window (Subspace Schism).";
            return;
        }

        hand.Add(card);
        _session.MarkDrawn();
        RefreshZoneCounts();
        string why = endOfTurn ? "end of turn" : "draw";
        StatusText.Text =
            $"P{player} draws ({why}): {card.Name}  (Hand {hand.Count}, Draw {draw.Count})";
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
            Filter = "STCCG Deck (*.stdeck)|*.stdeck",
            InitialDirectory = GamePaths.DecksRoot
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
            Filter = "STCCG Deck (*.stdeck)|*.stdeck",
            InitialDirectory = GamePaths.DecksRoot
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
                var card = CardFactory.Instantiate(e.Card!, 2);
                _oppSeedCards.Add(card);
                switch (CardKinds.SeedPile(card))
                {
                    case CardKinds.SeedBucket.Doorway:
                        _oppDoorwayCards.Add(card); break;
                    case CardKinds.SeedBucket.Mission:
                        _oppMissionSeedCards.Add(card); break;
                    case CardKinds.SeedBucket.Dilemma:
                        _oppDilemmaSeedCards.Add(card); break;
                    default:
                        _oppFacilitySeedCards.Add(card); break;
                }
            }
        }

        foreach (var e in deck.DrawCards.Where(x => x.Card != null))
            for (int i = 0; i < e.Quantity; i++)
                _oppDrawCards.Add(CardFactory.Instantiate(e.Card!, 2));

        void Expand(List<DeckEntry> entries, List<Card> target)
        {
            foreach (var e in entries.Where(x => x.Card != null))
                for (int i = 0; i < e.Quantity; i++)
                    target.Add(CardFactory.Instantiate(e.Card!, 2));
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

    private void DevFileLog_Click(object sender, RoutedEventArgs e)
    {
        DebugLog.Enabled = DevFileLogItem?.IsChecked != false;
        if (DebugLog.Enabled && !DebugLog.IsOpen)
            DebugLog.StartSession("menu");
        DebugLog.Write(DebugLog.Channel.System, _session.TurnNumber, _activePlayer,
            DebugLog.Enabled ? "file logging on" : "file logging off");
        StatusText.Text = DebugLog.Enabled
            ? (DebugLog.CurrentPath != null
                ? "File log: " + System.IO.Path.GetFileName(DebugLog.CurrentPath)
                : "File log on.")
            : "File log off.";
    }

    private void DevOpenLogFile_Click(object sender, RoutedEventArgs e)
    {
        if (!DebugLog.IsOpen)
            DebugLog.StartSession("open");
        if (!DebugLog.TryOpenFile())
        {
            StatusText.Text = DebugLog.CurrentPath != null
                ? "Could not open " + DebugLog.CurrentPath
                : "No log file yet.";
        }
    }

    private void DevOpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        if (!DebugLog.TryOpenFolder())
            StatusText.Text = "Could not open " + GamePaths.LogsRoot;
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

    private void AidSortHand_Click(object sender, RoutedEventArgs e)
    {
        _aidSortHand = AidSortHandItem?.IsChecked == true;
        RefreshHandStrips();
        StatusText.Text = _aidSortHand
            ? "Player aid: hand sorted by type."
            : "Player aid: hand in draw order.";
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
        card = CardFactory.Instantiate(card, _activePlayer);
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
        var winner = _session.CheckVictory(_scoreP1, _scoreP2);
        if (winner is > 0)
            StatusText.Text = $"P{winner} wins at {_session.PointsToWin} points.";
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
        // Any Edo Probe "abandon" locks on other missions lift when a different mission is solved.
        foreach (var d in _attachedDilemmas.Where(x => x.Kind == DilemmaRules.PersistKind.EdoProbe).ToList())
        {
            if (ReferenceEquals(d.Host, missionBorder)) continue;
            _attachedDilemmas.Remove(d);
            SendCardTo(d.Card, player, TimingRules.Destination.Discard);
        }
        _edoContinuePenalty.Remove(missionBorder);

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
            ProcessIncomingMessageMoves(_session.ActivePlayer);
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
        ProcessUntilEndOfTurnBag(finishingPlayer);
        ProcessRogueBorgEndOfTurn(finishingPlayer);
        ApplyEdoEndOfTurnPenalties(finishingPlayer);
        if (KlimBlocksDraw(finishingPlayer))
            _session.SuppressEndOfTurnDraw = true;
        _ionizationBeamsThisTurn = 0;
        _movedThisTurnAfterArrival.Clear();
        _arrivedMissionThisTurn.Clear();
        _auPlayedThisTurnP1 = false;
        _auPlayedThisTurnP2 = false;
        _uniquePersonnelPlayedP1 = false;
        _uniquePersonnelPlayedP2 = false;

        if (!_session.SuppressEndOfTurnDraw && !_session.HasDrawnThisTurn)
            DrawOneToHand(endOfTurn: true);
        else if (_session.SuppressEndOfTurnDraw)
            StatusText.Text = "No draw at end of turn (card effect).";

        if (_stack.IsOpen && _stack.Top?.Kind == TimingRules.ActionKind.DrawCard)
        {
            _endTurnAfterDrawStack = true;
            _endTurnFinishingPlayer = finishingPlayer;
            return;
        }

        FinishEndOfTurnDrawExtras(finishingPlayer);
        CompleteTurnChange();
    }

    private void FinishEndOfTurnDrawExtras(int finishingPlayer)
    {
        if (HasHorgahn(finishingPlayer) && !_horgahnExtraPlayUsed)
        {
            _pendingExtraDraws++;
            _horgahnExtraPlayUsed = true;
            _session.Log.Add(_session.TurnNumber, $"P{finishingPlayer}", "Horga'hn extra draw");
        }

        while (_pendingExtraDraws > 0)
        {
            _pendingExtraDraws--;
            DrawOneToHandFor(finishingPlayer, endOfTurn: true,
                skipSchism: !SchismAvailable(1) && !SchismAvailable(2));
            if (_stack.IsOpen && _stack.Top?.Kind == TimingRules.ActionKind.DrawCard)
            {
                _endTurnAfterDrawStack = true;
                _endTurnFinishingPlayer = finishingPlayer;
                return;
            }
        }
    }

    private int FirstLegalResponder(TimingRules.PendingAction top, int preferred)
    {
        var hand = preferred == 1 ? _handCards : _oppHandCards;
        if (LegalResponsesFor(hand, top, preferred).Count > 0)
            return preferred;
        // After a pass, do not bounce back to the player who already passed.
        if (_stack.ConsecutivePasses > 0)
            return preferred;
        int other = opponentOf(preferred);
        var otherHand = other == 1 ? _handCards : _oppHandCards;
        if (LegalResponsesFor(otherHand, top, other).Count > 0)
            return other;
        return preferred;
    }

    private void CompleteTurnChange()
    {
        _endTurnAfterDrawStack = false;
        _horgahnExtraPlayUsed = false;
        _pendingExtraDraws = 0;
        _energyVortexBlocked.Clear();
        SyncSchismRound();
        _wormholeShip = null;

        _session.EndTurn();

        UnstopAllCards(); // Compendium: Stopped endet zu Beginn des nächsten Zugs (hier Zugwechsel)
        ResetShipRangesForTurn();
        RefreshRedAlertForTurn();
        ProcessIncomingMessageMoves(_session.ActivePlayer);
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
        {
            ApplyStoppedVisual(b, stopped: false);
            if (b.Tag is Card c && c.InstanceId > 0
                && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst))
                inst.Stopped = false;
        }
        _stoppedBorders.Clear();
        _session.Log.Add(_session.TurnNumber, "Pystem", "All stopped cards are active again.");
    }

    private void SyncSessionToUi()
    {
        _activePlayer = _session.ActivePlayer;
        _turnNumber = _session.TurnNumber;
        UpdatePhaseControls();
        UpdateTurnTints();
        if (HistoryOverlay?.Visibility == Visibility.Visible)
            RefreshLegalMovesPanel();
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
            Filter = "STCCG Deck (*.stdeck)|*.stdeck",
            InitialDirectory = GamePaths.DecksRoot
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

        // Cryosatellite: only space missions (SeedRules).
        if (SeedRules.IsCryosatellite(card))
        {
            var spaces = legal
                .Where(m => m.Tag is Card mc && SeedRules.GetMissionLocation(mc).space)
                .ToList();
            if (spaces.Count == 0)
            {
                (player == 1 ? _seedCards : _oppSeedCards).Add(card);
                return;
            }
            legal = spaces;
        }

        var mission = WeightedPick(legal, Weight);
        var border = AddCardToTable(card, Canvas.GetLeft(mission), Canvas.GetTop(mission), TableCardWidth);
        AddSeedUnderMission(mission, border);
        SetBorderOwner(border, player);

        if (SeedRules.IsCryosatellite(card))
            AutoAttachCryosatellitePersonnel(mission, player);
    }

    /// <summary>
    /// Quick Game: seed up to 3 AU personnel from the facility seed pile under the same mission
    /// as Cryosatellite (deck-construction rule mirrored at the table).
    /// </summary>
    private void AutoAttachCryosatellitePersonnel(Border mission, int player)
    {
        int already = CountCryoPersonnelSeeded(player);
        int slots = SeedRules.CryosatellitePersonnelSlotsLeft(already);
        if (slots <= 0) return;

        var pile = player == 1 ? _facilitySeedCards : _oppFacilitySeedCards;
        var picks = pile.Where(SeedRules.IsAuPersonnelForCryosatellite).Take(slots).ToList();
        foreach (var p in picks)
        {
            pile.Remove(p);
            var b = AddCardToTable(p, Canvas.GetLeft(mission), Canvas.GetTop(mission), TableCardWidth);
            AddSeedUnderMission(mission, b);
            SetBorderOwner(b, player);
        }

        if (picks.Count > 0)
        {
            _session.Log.Add(_session.TurnNumber, $"P{player}",
                $"Cryosatellite: seeded {picks.Count} AU personnel under {(mission.Tag as Card)?.Name}");
        }
    }

    private int CountCryoPersonnelSeeded(int player)
    {
        int n = 0;
        foreach (var kv in _seedUnderMission)
        {
            foreach (var b in kv.Value)
            {
                if (b.Tag is not Card c) continue;
                if (GetBorderOwner(b) != player) continue;
                if (SeedRules.IsAuPersonnelForCryosatellite(c)) n++;
            }
        }
        return n;
    }

    private List<Card> CurrentSeedPileCards(int player)
    {
        var list = new List<Card>();
        switch (_seedSubPhase)
        {
            case SeedSubPhase.Doorway:
                list.AddRange(player == 1 ? _doorwayCards : _oppDoorwayCards);
                break;
            case SeedSubPhase.Mission:
                list.AddRange(player == 1 ? _missionSeedCards : _oppMissionSeedCards);
                break;
            case SeedSubPhase.Dilemma:
                list.AddRange(player == 1 ? _dilemmaSeedCards : _oppDilemmaSeedCards);
                if (player == 1) list.AddRange(_artifactSeedCards);
                break;
            case SeedSubPhase.Facility:
                list.AddRange(player == 1 ? _facilitySeedCards : _oppFacilitySeedCards);
                break;
        }
        return list;
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

        // AU personnel still in facility pile after Cryosatellite: attach under cryo mission if room
        if (SeedRules.IsAuPersonnelForCryosatellite(card)
            && SeedRules.CryosatellitePersonnelSlotsLeft(CountCryoPersonnelSeeded(player)) > 0)
        {
            var cryoMission = FindMissionWithCryosatellite(player);
            if (cryoMission != null)
            {
                var b = AddCardToTable(card, Canvas.GetLeft(cryoMission), Canvas.GetTop(cryoMission), TableCardWidth);
                AddSeedUnderMission(cryoMission, b);
                SetBorderOwner(b, player);
                return;
            }
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

    private Border? FindMissionWithCryosatellite(int player)
    {
        foreach (var kv in _seedUnderMission)
        {
            bool hasCryo = kv.Value.Any(b =>
                b.Tag is Card c
                && SeedRules.IsCryosatellite(c)
                && GetBorderOwner(b) == player);
            if (hasCryo) return kv.Key;
        }
        return null;
    }

    /// <summary>
    /// Locations on the spaceline only. Shared unique-mission copies sit on the canvas
    /// but are the same location — never a second seed/facility target.
    /// </summary>
    private List<Border> AllMissionBorders() =>
        _spacelineOrder
            .Where(b => b.Tag is Card c && IsMissionCard(c))
            .ToList();

    private void MenuSaveGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save game",
                Filter = "STCCG save (*.stsave)|*.stsave",
                FileName = $"game_{DateTime.Now:yyyyMMdd_HHmm}.stsave",
                InitialDirectory = GamePaths.SaveGamesRoot
            };
            if (dlg.ShowDialog() != true) return;
            // Sync UI counters into session before capture
            _session.ActivePlayer = _activePlayer is 1 or 2 ? _activePlayer : _session.ActivePlayer;
            if (_turnNumber > 0) _session.TurnNumber = _turnNumber;
            var snap = CaptureGameSave();
            var json = JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(dlg.FileName, json);
            _session.Log.Add(_session.TurnNumber, "System", $"Game saved: {System.IO.Path.GetFileName(dlg.FileName)}");
            DebugLog.Save(_session.TurnNumber, _activePlayer, "wrote " + dlg.FileName);
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
                Filter = "STCCG save (*.stsave)|*.stsave",
                InitialDirectory = GamePaths.SaveGamesRoot
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
            DebugLog.Divider("load " + System.IO.Path.GetFileName(dlg.FileName));
            DebugLog.Save(_session.TurnNumber, _activePlayer,
                $"loaded {System.IO.Path.GetFileName(dlg.FileName)} T{_turnNumber} P{_activePlayer}");
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
        Type = c.Type,
        InstanceId = c.InstanceId,
        Owner = c.OwnerPlayer,
        Controller = c.Controller,
        FaceUp = c.FaceUp
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
        if (hit == null) return null;
        int owner = r.Owner is 1 or 2 ? r.Owner : 0;
        var inst = CardFactory.Instantiate(hit, owner);
        if (r.InstanceId > 0)
        {
            inst.InstanceId = r.InstanceId;
            CardFactory.NoteHighestId(r.InstanceId);
        }
        if (r.Controller > 0) inst.Controller = r.Controller;
        inst.FaceUp = r.FaceUp;
        return inst;
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
                RedAlertPlaysLeft = _redAlertPlaysLeft,
                PointsToWin = _session.PointsToWin,
                Winner = _session.Winner,
                NextInstanceId = CardFactory.PeekNextId
            }
        };
        save.OncePerGame.AddRange(_session.OncePerGame);
        save.Stack = new StackWindowSnap
        {
            Open = _stack.IsOpen,
            ResponsePlayer = _stack.ResponsePlayer,
            ConsecutivePasses = _stack.ConsecutivePasses,
            Items = _stack.Items.Select(a => new StackItemSnap
            {
                Kind = a.Kind.ToString(),
                Controller = a.Controller,
                Card = a.Card?.Name,
                Target = a.TargetCard?.Name,
                Summary = a.Summary,
                IsResponse = a.IsResponse,
                Cancelled = a.Cancelled
            }).ToList()
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
        DedupTablePermanents(_tablePermanentCards);
        PutZone("p1.table", _tablePermanentCards);
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
        DedupTablePermanents(_oppTablePermanentCards);
        PutZone("p2.table", _oppTablePermanentCards);
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
                SolvedBy = _missionSolver.TryGetValue(b, out int sol) ? sol : null,
                InstanceId = card.InstanceId,
                Controller = card.Controller != 0 ? card.Controller : GetBorderOwner(b),
                FaceUp = card.FaceUp
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
                EspionageOn = ev.EspionageOn,
                TurnScope = ev.TurnScope.ToString(),
                PhasePoint = ev.PhasePoint.ToString(),
                ScopePlayer = ev.ScopePlayer
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
        _tablePermanentCards.Clear(); _oppTablePermanentCards.Clear();
        _stackOnHost.Clear(); _seedUnderMission.Clear();
        _revealedArtifactsUnderMission.Clear(); _lastEncounteredDilemma.Clear();
        RemoveBorgShipToken();
        _solvedMissions.Clear(); _missionSolver.Clear();
        _hullDamagePercent.Clear(); _stoppedBorders.Clear();
        _dockedAt.Clear(); _cloakedShips.Clear();
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
        _tablePermanentCards.AddRange(Fill("p1.table"));
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
        _oppTablePermanentCards.AddRange(Fill("p2.table"));
        _outOfPlayP2.AddRange(Fill("p2.oop"));

        var byId = new Dictionary<int, Border>();
        foreach (var snap in save.Table)
        {
            var card = ResolveCard(new CardRef
            {
                Name = snap.Name,
                Set = snap.Set,
                Type = snap.Type,
                InstanceId = snap.InstanceId,
                Owner = snap.Owner,
                Controller = snap.Controller,
                FaceUp = snap.FaceUp
            });
            if (card == null) continue;
            var border = AddCardToTable(card, snap.X, snap.Y, TableCardWidth);
            Panel.SetZIndex(border, snap.Z);
            SetBorderOwner(border, snap.Owner is 1 or 2 ? snap.Owner : 1);
            if (!snap.Visible) border.Visibility = Visibility.Collapsed;
            if (snap.Hull > 0)
            {
                SetHullDamagePercent(border, snap.Hull);
                if (snap.Hull >= 50 && snap.Hull < 100)
                {
                    border.RenderTransformOrigin = new Point(0.5, 0.5);
                    border.RenderTransform = new RotateTransform(180);
                }
                UpdateDamageBadge(border, snap.Hull);
            }
            if (snap.Stopped) MarkStopped(border);
            if (snap.RangeLeft.HasValue)
                SetShipRangeLeft(border, card, snap.RangeLeft.Value);
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
            var card = ResolveAttachedEventCard(ev.Card, ev.Owner);
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
                EspionageOn = ev.EspionageOn,
                TurnScope = Enum.TryParse(ev.TurnScope, out TimingRules.TurnScope ts)
                    ? ts : TimingRules.TurnScope.EveryTurn,
                PhasePoint = Enum.TryParse(ev.PhasePoint, out TimingRules.TurnPhasePoint pp)
                    ? pp : TimingRules.TurnPhasePoint.EndOfTurn,
                ScopePlayer = ev.ScopePlayer
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
        if (s.PointsToWin > 0) _session.PointsToWin = s.PointsToWin;
        _session.OncePerGame.Clear();
        if (save.OncePerGame != null)
            foreach (var k in save.OncePerGame)
                _session.OncePerGame.Add(k);
        if (s.NextInstanceId > 0)
            CardFactory.NoteHighestId(s.NextInstanceId - 1);
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

        bool tableZonesSaved = (save.Zones.ContainsKey("p1.table") && save.Zones["p1.table"].Count > 0)
                            || (save.Zones.ContainsKey("p2.table") && save.Zones["p2.table"].Count > 0);
        if (!tableZonesSaved)
        {
            foreach (var ev in _attachedEvents)
            {
                if (ev.Host != null || ev.Host2 != null) continue;
                if (ev.Card == null || !EventBelongsOnTableColumn(ev.Card)) continue;
                var list = ev.Owner == 2 ? _oppTablePermanentCards : _tablePermanentCards;
                if (!TableListHas(list, ev.Card))
                    list.Add(ev.Card);
            }
        }
        DedupTablePermanents(_tablePermanentCards);
        DedupTablePermanents(_oppTablePermanentCards);
        RebuildTablePermanentsPanel();
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
        SyncBoardFromTable();
        foreach (var line in BoardStore.Current.DumpLines())
            _session.Log.AddDebug(_session.TurnNumber, "Board", line);
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
    /// Starfield sits on BoardBackdrop (fixed in the frame). Cards pan/zoom on a transparent canvas.
    /// </summary>
    private void ApplyRandomBoardBackground()
    {
        if (TableCanvas != null)
            TableCanvas.Background = Brushes.Transparent;
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
                if (BoardBackdrop != null) BoardBackdrop.Source = null;
                return;
            }

            string pick = files[Random.Shared.Next(files.Count)];
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(pick, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            if (BoardBackdrop != null)
                BoardBackdrop.Source = bmp;
            StatusText.Text = (StatusText.Text ?? "") + $"  ·  board: {System.IO.Path.GetFileName(pick)}";
        }
        catch
        {
            if (BoardBackdrop != null) BoardBackdrop.Source = null;
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
        _energyVortexBlocked.Clear();
        _schismUsedBy.Clear();
        _schismRound = 0;
        _escapePods.Clear();
        _wormholeShip = null;
        _pendingExtraDraws = 0;
        _endTurnAfterDrawStack = false;
        _attachedEvents.Clear();
        _ionizationBeamsThisTurn = 0;
        _redAlertPlaysLeft = 0;
        _movedThisTurnAfterArrival.Clear();
        _arrivedMissionThisTurn.Clear();
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
        _cloakedShips.Clear();
        _dockedAt.Clear();
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

        CardFactory.ResetIdsForNewGame();

        // Seed-Karten nach Regelbuch-Phasen aufteilen (manuell legen)
        foreach (var e in deck.SeedCards.Where(x => x.Card != null))
        {
            for (int i = 0; i < e.Quantity; i++)
                ClassifySeedCard(CardFactory.Instantiate(e.Card!, 1));
        }

        // Draw-Deck
        foreach (var e in deck.DrawCards.Where(x => x.Card != null))
            for (int i = 0; i < e.Quantity; i++)
                _drawCards.Add(CardFactory.Instantiate(e.Card!, 1));

        // Side Decks (benannt) + Legacy
        void Expand(List<DeckEntry> entries, List<Card> target)
        {
            foreach (var e in entries.Where(x => x.Card != null))
                for (int i = 0; i < e.Quantity; i++)
                    target.Add(CardFactory.Instantiate(e.Card!, 1));
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
        switch (CardKinds.SeedPile(card))
        {
            case CardKinds.SeedBucket.Doorway:
                _doorwayCards.Add(card); break;
            case CardKinds.SeedBucket.Mission:
                _missionSeedCards.Add(card); break;
            case CardKinds.SeedBucket.Dilemma:
                _dilemmaSeedCards.Add(card); break;
            default:
                _facilitySeedCards.Add(card); break;
        }
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

        Card detail = card;
        if (IsMissionCard(card))
        {
            detail = MissionPrintedFor(border, _activePlayer);
            if (LocationIsYourMission(border, _activePlayer)
                && LocationIsOpponentMission(border, _activePlayer))
                StatusText.Text =
                    $"{detail.Name}: shared location — your printed side this turn (also opponent's mission).";
        }
        ShowCardDetail(detail);
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
            {
                if (!_seedPhaseActive
                    && cardBorder.Tag is Card rider
                    && host.Tag is Card hostCard
                    && !TreatyRules.CanOccupyHost(rider, hostCard, GetActiveTreaties(_activePlayer)))
                {
                    string why = CardKinds.IsMission(hostCard) && !MissionRules.IsPlanetMission(hostCard)
                        ? $"{rider.Name} cannot beam into space at {hostCard.Name} (7.1.1.0.1)."
                        : $"{rider.Name} cannot board {hostCard.Name} without a matching Treaty.";
                    ShowPlayError(why);
                }
                else
                    AddCardToHostStack(host, cardBorder);
            }
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
                int baryon = EventsOn(b).Count(e => e.Kind == EventRules.Persist.Baryon) * 2;
                SetShipRangeLeft(b, c, Math.Max(0, BattleRules.EffectiveRange(c, hull) - baryon));
            }
        }
    }

    /// <summary>E3: UI dict mirror + ShipInstance.RangeLeft when present.</summary>
    private void SetShipRangeLeft(Border shipBorder, Card ship, int left)
    {
        _shipRangeLeft[shipBorder] = left;
        if (ship.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(ship.InstanceId, out var inst)
            && inst is ShipInstance sh)
            sh.RangeLeft = left;
    }

    private void ClearShipRangeLeft(Border border)
    {
        _shipRangeLeft.Remove(border);
        if (border.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst)
            && inst is ShipInstance sh)
            sh.RangeLeft = -1;
    }

    private void ApplyUiStatusToStore(BoardStore store)
    {
        foreach (var kv in _shipRangeLeft)
        {
            if (kv.Key.Tag is not Card c || c.InstanceId <= 0) continue;
            if (store.ById.TryGetValue(c.InstanceId, out var inst) && inst is ShipInstance sh)
                sh.RangeLeft = kv.Value;
        }
        foreach (var b in _stoppedBorders)
        {
            if (b.Tag is not Card c || c.InstanceId <= 0) continue;
            if (store.ById.TryGetValue(c.InstanceId, out var inst))
                inst.Stopped = true;
        }
        foreach (var b in _cloakedShips)
        {
            if (b.Tag is not Card c || c.InstanceId <= 0) continue;
            if (store.ById.TryGetValue(c.InstanceId, out var inst) && inst is ShipInstance sh)
                sh.Cloaked = true;
        }
        foreach (var kv in _dockedAt)
        {
            if (kv.Key.Tag is not Card c || c.InstanceId <= 0) continue;
            int facId = kv.Value.Tag is Card fc ? fc.InstanceId : 0;
            if (store.ById.TryGetValue(c.InstanceId, out var inst) && inst is ShipInstance sh)
                sh.DockedAtId = facId;
        }
        foreach (var kv in _hullDamagePercent)
        {
            if (kv.Key.Tag is not Card c || c.InstanceId <= 0) continue;
            if (store.ById.TryGetValue(c.InstanceId, out var inst) && inst is ShipInstance sh)
                sh.HullPercent = kv.Value;
        }
    }

    private void SetShipCloaked(Border ship, bool cloaked)
    {
        if (cloaked) _cloakedShips.Add(ship);
        else _cloakedShips.Remove(ship);
        if (ship.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst)
            && inst is ShipInstance sh)
            sh.Cloaked = cloaked;
    }

    private void SetShipDockedAt(Border ship, Border? facility)
    {
        if (facility == null)
            _dockedAt.Remove(ship);
        else
            _dockedAt[ship] = facility;
        if (ship.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst)
            && inst is ShipInstance sh)
            sh.DockedAtId = facility?.Tag is Card fc ? fc.InstanceId : 0;
    }

    private void SetHullDamagePercent(Border border, int hullPercent)
    {
        hullPercent = Math.Clamp(hullPercent, 0, 100);
        _hullDamagePercent[border] = hullPercent;
        if (border.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst)
            && inst is ShipInstance sh)
            sh.HullPercent = hullPercent;
    }

    private int GetDockedAtInstanceId(Border ship)
    {
        if (ship.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst)
            && inst is ShipInstance sh
            && sh.DockedAtId > 0)
            return sh.DockedAtId;
        if (_dockedAt.TryGetValue(ship, out var fac) && fac?.Tag is Card fc)
            return fc.InstanceId;
        return 0;
    }

    private int GetRemainingRange(Border shipBorder, Card ship)
    {
        // E3: prefer instance (source of truth); UI dict is mirror.
        if (ship.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(ship.InstanceId, out var inst)
            && inst is ShipInstance sh
            && sh.RangeLeft >= 0)
        {
            _shipRangeLeft[shipBorder] = sh.RangeLeft;
            return sh.RangeLeft;
        }
        if (_shipRangeLeft.TryGetValue(shipBorder, out int left))
        {
            if (ship.InstanceId > 0
                && BoardStore.Current.ById.TryGetValue(ship.InstanceId, out var inst2)
                && inst2 is ShipInstance sh2)
                sh2.RangeLeft = left;
            return left;
        }
        int hull = GetHullDamage(shipBorder);
        int full = BattleRules.EffectiveRange(ship, hull);
        int junior = _attachedDilemmas.Count(a =>
            a.Kind == DilemmaRules.PersistKind.Junior && ReferenceEquals(a.Host, shipBorder));
        full = Math.Max(0, full - junior); // bereits abgezogene Züge: Countdown als Penalty-Zähler
        foreach (var j in _attachedDilemmas.Where(a =>
                     a.Kind == DilemmaRules.PersistKind.Junior && ReferenceEquals(a.Host, shipBorder)))
            full = Math.Max(0, full - Math.Max(0, j.Countdown));
        SetShipRangeLeft(shipBorder, ship, full);
        return full;
    }

    private int GetHullDamage(Border border)
    {
        // E3b: prefer instance (source of truth); UI dict is mirror.
        if (border.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst)
            && inst is ShipInstance sh
            && sh.HullPercent >= 0)
        {
            _hullDamagePercent[border] = sh.HullPercent;
            return sh.HullPercent;
        }
        if (_hullDamagePercent.TryGetValue(border, out int h))
        {
            if (border.Tag is Card c2 && c2.InstanceId > 0
                && BoardStore.Current.ById.TryGetValue(c2.InstanceId, out var inst2)
                && inst2 is ShipInstance sh2)
                sh2.HullPercent = h;
            return h;
        }
        return 0;
    }

    private bool IsBorderStopped(Border border)
    {
        if (border.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst))
            return inst.Stopped || _stoppedBorders.Contains(border);
        return _stoppedBorders.Contains(border);
    }

    private List<Card> GetCrewOnShip(Border shipBorder)
    {
        var list = new List<Card>();
        var seen = new HashSet<int>();
        if (shipBorder.Tag is Card ship)
        {
            foreach (var c in BoardStore.Current.CrewPersonnel(ship.InstanceId))
            {
                int id = c.InstanceId > 0 ? c.InstanceId : c.GetHashCode();
                if (seen.Add(id)) list.Add(c);
            }
        }

        if (_stackOnHost.TryGetValue(shipBorder, out var stacked))
        {
            foreach (var sb in stacked)
            {
                if (sb.Tag is not Card c) continue;
                if (!IsCrewType(c) && !(c.Type ?? "").Contains("personnel", StringComparison.OrdinalIgnoreCase))
                    continue;
                int id = c.InstanceId > 0 ? c.InstanceId : c.GetHashCode();
                if (seen.Add(id)) list.Add(c);
            }
        }
        return list;
    }

    /// <summary>Gaps is a landable span-4 location. Q-Net is a barrier, not a stop.</summary>
    private static bool IsLandableLocation(Card c) =>
        IsMissionCard(c) || EventRules.NameIs(c, "Gaps in Normal Space");

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

        var destCard = toMission.Tag as Card;
        var flyAuth = AuthorizePlay(GameAction.Fly(_activePlayer, ship, destCard));
        if (!flyAuth.Ok)
        {
            StatusText.Text = flyAuth.Message;
            return false;
        }

        int owner = GetBorderOwner(shipBorder);
        if (owner == 0) owner = _activePlayer;
        if (IsShipDocked(shipBorder))
        {
            StatusText.Text = $"{ship.Name} is docked — undock before flying.";
            return false;
        }
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

        var uiLine = MissionsOnSameSpaceline(fromMission ?? toMission);
        int uiFrom = fromMission == null ? -1 : uiLine.IndexOf(fromMission);
        int uiTo = uiLine.IndexOf(toMission);

        var boardLine = FlyBoardLine(fromMission ?? toMission);
        int fromIdx = BoardIndexOf(boardLine, fromMission);
        int toIdx = BoardIndexOf(boardLine, toMission);
        if (fromIdx < 0 || toIdx < 0)
        {
            StatusText.Text = "Location nicht auf dem Board.";
            return false;
        }

        var imBlock = IncomingMessageMoveCheck(shipBorder, fromMission, toMission, uiFrom, uiTo);
        if (imBlock != null)
        {
            ShowPlayError(imBlock);
            return false;
        }

        int remain = GetRemainingRange(shipBorder, ship);

        bool wrap = WnohgbRules.WrapAllowed(PlayerHasWnohgb(_activePlayer), boardLine.Count);
        int directCost = MovementRules.RangeCostBetween(boardLine, fromIdx, toIdx, wrapEnds: false);
        int wrapCost = MovementRules.RangeCostBetween(boardLine, fromIdx, toIdx, wrapEnds: true);
        var block = CheckEventMovementOnLine(
            shipBorder, ship, fromIdx, toIdx, crew,
            wrapEnds: wrap, lineCount: boardLine.Count,
            directCost: directCost, wrapCost: wrapCost,
            indexOfHost: h => BoardIndexOf(boardLine, h));
        if (block != null)
        {
            StatusText.Text = block;
            return false;
        }

        if (wrap)
            DebugLog.Move(_session.TurnNumber, _activePlayer,
                $"wnohgb wrap={(WnohgbRules.UseWrapPath(wrap, directCost, wrapCost))} direct={directCost} wrapCost={wrapCost} from[{fromIdx}] to[{toIdx}]");
        var move = MovementRules.CanMoveShip(ship, crew, remain, boardLine, fromIdx, toIdx,
            GetActiveTreaties(_activePlayer), wrapEnds: wrap,
            skipStaffing: ShipStaffedByRogueBorg(shipBorder));
        if (!move.Ok)
        {
            StatusText.Text = move.Reason;
            return false;
        }

        ApplyEventAfterMove(shipBorder, ship,
            fromMission != null ? IndexOfMission(fromMission) : -1,
            IndexOfMission(toMission), crew);

        SetShipRangeLeft(shipBorder, ship, move.RangeLeft);
        if (ship.InstanceId > 0)
            DebugLog.Move(_session.TurnNumber, _activePlayer,
                $"range #{ship.InstanceId} left={move.RangeLeft} source=instance");
        StatusText.Text =
            $"{ship.Name} → {((Card)toMission.Tag!).Name} · −{move.RangeCost} RANGE " +
            $"(noch {move.RangeLeft}/{MovementRules.GetShipRange(ship)}) · {staff.Reason}";
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"{ship.Name} moved, cost {move.RangeCost}, left {move.RangeLeft}");
        DebugLog.MoveShip(_session.TurnNumber, _activePlayer, ship,
            fromMission?.Tag as Card, toMission.Tag as Card, move.RangeCost, move.RangeLeft);
        var arrived = FindMissionForDockable(shipBorder);
        if (arrived != null) ResolveRequiredArrival(shipBorder, arrived);
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

    private static bool IsShipCard(Card c) => CardKinds.IsShip(c);

    private static bool IsFacilityCard(Card c) => CardKinds.IsFacility(c);

    private static bool IsCrewType(Card c) => CardKinds.IsPersonnel(c);

    private static bool IsEquipmentType(Card c) => CardKinds.IsEquipment(c);

    /// <summary>Personnel, Equipment, acquired Artifacts; Rogue Borg only with Lore Returns (handled via host).</summary>
    private static bool IsBeamableCard(Card c)
    {
        if (c == null) return false;
        if (CardKinds.IsBeamable(c)) return true;
        // Interrupt "Rogue Borg" is beamable after Lore Returns — caller checks ownership/Lore.
        if (InterruptRules.IsRogueBorg(c)) return true;
        return false;
    }

    private bool IsBeamableFromHost(Card c, Border host, Border cardBorder)
    {
        if (IsRogueBorgCard(c))
        {
            var unit = _rogueBorg.FirstOrDefault(r => ReferenceEquals(r.Visual, cardBorder)
                                                      || ReferenceEquals(r.Card, c) && SameHostShip(r.Host, host));
            int o = unit is { Controller: 1 or 2 } ? unit.Controller
                : (GetBorderOwner(cardBorder) is 1 or 2 ? GetBorderOwner(cardBorder) : CardOwner(cardBorder));
            if (o != _activePlayer) return false;
            if (host.Tag is Card hc && IsShipCard(hc) && ShipHasLoreReturns(host))
                return true;
            // Planet → commandeered ship at this location (Lore: "beam your Rogue Borg")
            var mission = host.Tag is Card mc && IsMissionCard(mc) ? host : FindMissionForDockable(host);
            if (mission == null) return false;
            foreach (var dock in GetDockablesUnderMission(mission))
            {
                if (dock.Tag is Card ds && IsShipCard(ds)
                    && ShipHasLoreReturns(dock)
                    && GetBorderOwner(dock) == _activePlayer)
                    return true;
            }
            return false;
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

        if (InterruptRules.IsHugh(card))
        {
            Border? best = null;
            double bestD = double.MaxValue;
            foreach (var b in TableCanvas.Children.OfType<Border>())
            {
                if (b.Tag is not Card hc) continue;
                if (!(IsShipCard(hc) && CountRogueBorgOn(b) > 0) && !TimingRules.IsHughBattleSource(hc))
                    continue;
                double bw = b.Width > 0 ? b.Width : TableCardWidth;
                double bh = b.Height > 0 ? b.Height : TableCardHeight;
                double bx = Canvas.GetLeft(b) + bw / 2.0;
                double by = Canvas.GetTop(b) + bh / 2.0;
                double d = Math.Sqrt((cx - bx) * (cx - bx) + (cy - by) * (cy - by));
                if (d < bestD) { bestD = d; best = b; }
            }
            if (_borgShipToken != null)
            {
                double bx = Canvas.GetLeft(_borgShipToken) + TableCardWidth / 2.0;
                double by = Canvas.GetTop(_borgShipToken) + TableCardHeight / 2.0;
                double d = Math.Sqrt((cx - bx) * (cx - bx) + (cy - by) * (cy - by));
                if (d < bestD) { bestD = d; best = _borgShipToken; }
            }
            if (best == null || bestD > ShipSnapRange)
            {
                HideSnapPreview();
                return;
            }
            target = best;
            previewLeft = Canvas.GetLeft(target);
            previewTop = Canvas.GetTop(target);
            goto DrawEventSnap;
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
        else if (TargetingRules.UsesBoardSnap(card))
        {
            int owner = GetBorderOwner(dragging);
            if (owner == 0) owner = _activePlayer;
            target = NearestLegalSnapHost(card, owner, cx, cy, out double dist);
            if (target == null || dist > ShipSnapRange * 1.6)
            {
                HideSnapPreview();
                return;
            }
            previewLeft = Canvas.GetLeft(target);
            previewTop = Canvas.GetTop(target);
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
        _currentSnapHost = host;
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
        _currentSnapHost = null;
        _tableColumnSnapCard = null;
        ClearZoneHighlight();
    }

    private static bool DragWantsStackPeek(Card drag) => TargetQuery.WantsBuriedPeek(drag);

    private static bool IsLegalPeekTarget(Card drag, Card buried)
    {
        if (InterruptRules.IsHugh(drag))
            return TimingRules.IsHughBattleSource(buried) && !InterruptRules.IsRogueBorg(buried);
        return TargetQuery.IsLegal(drag, buried);
    }

    private List<Border>? StackOnHost(Border host)
    {
        if (_stackOnHost.TryGetValue(host, out var list)) return list;
        foreach (var kv in _stackOnHost)
            if (SameHostShip(kv.Key, host)) return kv.Value;
        return null;
    }

    private List<Card> BuriedLegalOn(Border host, Card drag)
    {
        var list = new List<Card>();
        void Add(Card? c)
        {
            if (c == null || !IsLegalPeekTarget(drag, c)) return;
            if (!list.Contains(c)) list.Add(c);
        }
        if (host.Tag is Card self && EventRules.IsEvent(self))
            Add(self);
        var stacked = StackOnHost(host);
        if (stacked != null)
            foreach (var b in stacked)
                if (b.Tag is Card c) Add(c);
        foreach (var ae in EventsOn(host))
            Add(ae.Card);
        foreach (var rb in RogueBorgUnitsOn(host))
            Add(rb.Card);
        if (_lastEncounteredDilemma.TryGetValue(host, out var dil))
            Add(dil);
        if (_targetSites.Count > 0 && host.Tag is Card hc)
        {
            foreach (var s in _targetSites)
            {
                // Gaps/Q-Net sit BETWEEN missions. Never treat them as "on" a mission.
                if (s.Kind == TargetSiteKind.GapSpan) continue;
                if (s.Host2 != null) continue;
                if (s.Card != null && IsSpacelineSpanCard(s.Card)) continue;
                if (TargetQuery.SiteBelongsToHost(s, hc)) Add(s.Card);
            }
        }
        return list;
    }

    private List<TargetQuery.InPlay> CollectNullifyInPlay()
    {
        var raw = new List<TargetQuery.InPlay>();
        void Add(Card? c, Card? host, Card? host2, TargetSiteKind kind)
        {
            if (c != null) raw.Add(new TargetQuery.InPlay(c, host, host2, kind));
        }
        foreach (var ev in _tablePermanentCards)
            Add(ev, ev, null, TargetSiteKind.TableCard);
        foreach (var ev in _oppTablePermanentCards)
            Add(ev, ev, null, TargetSiteKind.TableCard);
        foreach (var ae in _attachedEvents)
        {
            var face = FindBorderForCard(ae.Card);
            TargetSiteKind kind;
            if (ae.Host != null && ae.Host2 != null && face == null)
                kind = TargetSiteKind.GapSpan;
            else if (face != null && _spacelineOrder.Contains(face))
                kind = TargetSiteKind.HostFace;
            else if (ae.Host != null)
                kind = TargetSiteKind.BuriedCard;
            else
                kind = TargetSiteKind.TableCard;
            Add(ae.Card, ae.Host?.Tag as Card, ae.Host2?.Tag as Card, kind);
        }
        foreach (var kv in _stackOnHost)
        {
            var hostCard = kv.Key.Tag as Card;
            foreach (var b in kv.Value)
                if (b.Tag is Card c)
                    Add(c, hostCard, null, TargetSiteKind.BuriedCard);
        }
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Visibility != Visibility.Visible || b.Tag is not Card c) continue;
            if (EventRules.IsEvent(c) && _spacelineOrder.Contains(b))
                Add(c, c, null, TargetSiteKind.HostFace);
        }
        return raw;
    }

    private List<TargetSite> CollectSitesForDrag(Card drag)
    {
        if (TargetQuery.IsNullifyDrag(drag))
            return TargetQuery.NullifySites(drag, CollectNullifyInPlay());
        var list = new List<TargetSite>();
        if (TargetQuery.IsGapPlay(drag))
        {
            SyncBoardFromTable(logDual: false);
            var boardGaps = TargetQuery.GapSites(BoardStore.Current.Spaceline.Locations);
            if (boardGaps.Count > 0)
            {
                DebugLog.Target(_session.TurnNumber, _activePlayer,
                    $"sites {DebugLog.Card(drag)} board-gaps={boardGaps.Count}");
                return boardGaps;
            }
            foreach (var (left, right) in ListSameQuadrantGaps())
            {
                if (left.Tag is not Card lc || right.Tag is not Card rc) continue;
                list.Add(new TargetSite(TargetSiteKind.GapSpan, lc, lc, rc, TargetWhy.PlayOn,
                    "Play between these missions."));
            }
            DebugLog.Target(_session.TurnNumber, _activePlayer,
                $"sites {DebugLog.Card(drag)} ui-gaps={list.Count}");
            return list;
        }
        if (TargetQuery.IsPlayOnDrag(drag))
        {
            int player = _activePlayer;
            SyncBoardFromTable(logDual: false);
            var pieces = BoardPlayOnPieces();
            var boardSites = TargetQuery.PlayOnSites(drag, player, pieces);
            if (boardSites.Count > 0)
            {
                DebugLog.Target(_session.TurnNumber, player,
                    $"sites {DebugLog.Card(drag)} board-playon={boardSites.Count}");
                return boardSites;
            }
            foreach (var b in TableCanvas.Children.OfType<Border>())
            {
                if (b.Visibility != Visibility.Visible || b.Tag is not Card c) continue;
                var chk = TargetQuery.CanPlayOn(drag, c, FactsFor(b), player);
                if (!chk.ok) continue;
                list.Add(new TargetSite(TargetSiteKind.HostFace, c, c, null, TargetWhy.PlayOn, chk.reason));
            }
            DebugLog.Target(_session.TurnNumber, player,
                $"sites {DebugLog.Card(drag)} ui-playon={list.Count}");
            return list;
        }

        bool seedNow = _seedPhaseActive || _devPlaySeedFromHand;
        if (seedNow && IsSeedableUnderMission(drag))
        {
            foreach (var m in AllMissionBorders())
            {
                if (m.Tag is not Card mc) continue;
                var chk = CanSeedCardUnderMission(drag, mc);
                if (!chk.ok) continue;
                list.Add(new TargetSite(TargetSiteKind.LocationSlot, mc, mc, null, TargetWhy.Seed, chk.reason));
            }
            return list;
        }
        if (seedNow && IsFacilityCard(drag))
        {
            foreach (var m in AllMissionBorders())
            {
                if (m.Tag is not Card mc) continue;
                var chk = CanSeedFacilityAtMission(drag, mc);
                if (!chk.ok) continue;
                list.Add(new TargetSite(TargetSiteKind.LocationSlot, mc, mc, null, TargetWhy.Seed, chk.reason));
            }
            return list;
        }
        if (!_seedPhaseActive && ReportingRules.MustReportForDuty(drag))
        {
            int player = _activePlayer;
            foreach (var b in CollectLegalReportHosts(drag, player))
            {
                if (b.Tag is not Card hc) continue;
                list.Add(new TargetSite(TargetSiteKind.HostFace, hc, hc, null, TargetWhy.Report,
                    "Report to this facility."));
            }
        }
        return list;
    }

    private IEnumerable<(Card card, TargetQuery.HostFacts facts)> BoardPlayOnPieces()
    {
        foreach (var loc in BoardStore.Current.Spaceline.Locations)
        {
            if (loc.Printed != null)
                yield return (loc.Printed, FactsForPrinted(loc.Printed));
            foreach (var occ in loc.Occupants)
                yield return (occ.Card.Printed, FactsForPrinted(occ.Card.Printed));
        }
    }

    private TargetQuery.HostFacts FactsForPrinted(Card c)
    {
        var face = FindBorderForCard(c);
        if (face != null)
            return FactsFor(face);

        bool ship = IsShipCard(c);
        bool fac = IsFacilityCard(c);
        bool mission = IsMissionCard(c);
        var occ = BoardStore.Current.FindOccupant(c.InstanceId);
        int owner = c.Controller != 0 ? c.Controller : (c.OwnerPlayer != 0 ? c.OwnerPlayer : _activePlayer);
        int crewN = occ?.Crew.Personnel.Count ?? 0;
        int sec = 0;
        if (occ != null)
            sec = EventRules.CountSkill(occ.Crew.Personnel.Select(p => p.Printed), "SECURITY");
        string tn = ((c.Type ?? "") + " " + (c.Name ?? "")).ToLowerInvariant();
        return new TargetQuery.HostFacts(
            Owner: owner,
            Exposed: false,
            Cloaked: false,
            Occupied: crewN > 0,
            EmptyOfPersonnel: crewN == 0,
            HasRogueBorg: false,
            SecurityCount: sec,
            IsShip: ship,
            IsFacility: fac,
            IsOutpost: fac && tn.Contains("outpost"),
            IsMission: mission,
            IsPlanetMission: mission && MissionRules.IsPlanetMission(c),
            IsNonAlignedShip: ship && IsNonAlignedShip(c),
            IsBorgShip: ship && IsBorgAffiliation(c));
    }

    private TargetQuery.HostFacts FactsFor(Border host)
    {
        var c = host.Tag as Card;
        int owner = GetBorderOwner(host);
        if (owner == 0) owner = 1;
        bool ship = c != null && IsShipCard(c);
        bool fac = c != null && IsFacilityCard(c);
        string tn = (((c?.Type ?? "") + " " + (c?.Name ?? "")).ToLowerInvariant());
        int sec = 0;
        if (ship)
            sec = EventRules.CountSkill(GetCrewOnShip(host), "SECURITY");
        return new TargetQuery.HostFacts(
            Owner: owner,
            Exposed: ship && IsShipExposed(host),
            Cloaked: ship && IsShipCloaked(host),
            Occupied: ShipIsOccupied(host),
            EmptyOfPersonnel: !HostHasPersonnelOf(host, 0),
            HasRogueBorg: CountRogueBorgOn(host) > 0,
            SecurityCount: sec,
            IsShip: ship,
            IsFacility: fac,
            IsOutpost: fac && tn.Contains("outpost"),
            IsMission: c != null && IsMissionCard(c),
            IsPlanetMission: c != null && IsMissionCard(c) && MissionRules.IsPlanetMission(c),
            IsNonAlignedShip: ship && c != null && IsNonAlignedShip(c),
            IsBorgShip: ship && c != null && IsBorgAffiliation(c));
    }

    private void BeginTargetSession(Card drag)
    {
        _targetSites = CollectSitesForDrag(drag);
        _snapSite = null;
    }

    private void EndTargetSession()
    {
        _targetSites.Clear();
        _snapSite = null;
    }

    private List<Card> CollectNullifyPool(Card drag, Border? hostFilter = null)
    {
        var sites = _targetSites.Count > 0 ? _targetSites : CollectSitesForDrag(drag);
        IEnumerable<TargetSite> q = sites;
        if (hostFilter?.Tag is Card hc)
        {
            var onHost = sites.Where(s => TargetQuery.SiteBelongsToHost(s, hc)).ToList();
            if (onHost.Count > 0) q = onHost;
        }
        var list = new List<Card>();
        foreach (var s in q)
            if (!list.Contains(s.Card)) list.Add(s.Card);
        return list;
    }

    private Card? _peekSnapCard;

    private Border? FindHostUnderWindow(Point windowPos)
    {
        Card? drag = _dragCard?.Tag as Card;
        Border? bestLegal = null;
        int bestLegalZ = int.MinValue;
        Border? bestAny = null;
        int bestAnyZ = int.MinValue;
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Tag is not Card hc) continue;
            if (b.Visibility != Visibility.Visible) continue;
            bool hostKind = IsShipCard(hc) || IsFacilityCard(hc)
                            || (EventRules.IsEvent(hc) && _spacelineOrder.Contains(b));
            if (!hostKind) continue;
            try
            {
                var tl = b.TransformToAncestor(this).Transform(new Point(0, 0));
                double w = b.ActualWidth > 1 ? b.ActualWidth
                    : (b.Width > 1 ? b.Width : TableCardWidth);
                double h = b.ActualHeight > 1 ? b.ActualHeight
                    : (b.Height > 1 ? b.Height : TableCardHeight);
                var rect = new Rect(tl, new Size(w, h));
                rect.Inflate(28, 36);
                if (!rect.Contains(windowPos)) continue;
            }
            catch { continue; }
            int z = Panel.GetZIndex(b);
            if (z >= bestAnyZ) { bestAny = b; bestAnyZ = z; }
            if (drag != null && BuriedLegalOn(b, drag).Count > 0 && z >= bestLegalZ)
            {
                bestLegal = b;
                bestLegalZ = z;
            }
        }
        return bestLegal ?? bestAny;
    }

    private void UpdateStackPeek(Point windowPos)
    {
        if (_dragCard?.Tag is not Card drag || !DragWantsStackPeek(drag))
        {
            CancelStackPeek(keepOverlay: false);
            return;
        }
        // Detail overlay covers the ship — keep mini-snap alive while the cursor
        // is over the overlay even if the canvas host is no longer under the pointer.
        if (CardDetailOverlay?.Visibility == Visibility.Visible && _peekLegalTargets.Count > 0)
        {
            if (PointOverPeekUi(windowPos))
            {
                UpdatePeekSnapAt(windowPos);
                return;
            }
            // Detail is open: ignore the board under/around it.
            return;
        }
        var host = _currentSnapHost != null && BuriedLegalOn(_currentSnapHost, drag).Count > 0
            ? _currentSnapHost
            : FindHostUnderWindow(windowPos);
        host = OwningHostOf(host) ?? host;
        if (host == null || BuriedLegalOn(host, drag).Count == 0)
        {
            if (_peekHoverHost != null)
                CancelStackPeek(keepOverlay: true);
            return;
        }
        if (ReferenceEquals(host, _peekHoverHost))
        {
            UpdatePeekSnapAt(windowPos);
            return;
        }
        CancelStackPeek(keepOverlay: true);
        _peekHoverHost = host;
        _peekHoverTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        var capturedHost = host;
        var capturedDrag = drag;
        _peekHoverTimer.Tick += (_, _) =>
        {
            _peekHoverTimer?.Stop();
            OpenStackPeek(capturedHost, capturedDrag);
        };
        _peekHoverTimer.Start();
        StatusText.Text = $"Hold over {((Card)host.Tag!).Name} to open stack (legal targets)…";
    }

    /// <summary>
    /// After peek opens: only this host's sites stay legal. Board-wide glows go away.
    /// </summary>
    private void NarrowSessionToPeekHost(Border host)
    {
        if (host.Tag is not Card hc) return;
        if (_targetSites.Count > 0)
            _targetSites = _targetSites.Where(s => TargetQuery.SiteBelongsToHost(s, hc)
                                                   && s.Kind != TargetSiteKind.GapSpan
                                                   && s.Host2 == null
                                                   && (s.Card == null || !IsSpacelineSpanCard(s.Card)))
                                       .ToList();
        ClearEventTargetHighlights();
        AddTargetHalo(host, Color.FromRgb(40, 255, 120));
        ApplyPeekSnapStyle();
    }

    private Border? OwningHostOf(Border? piece)
    {
        if (piece == null) return null;
        if (piece.Tag is Card c && (IsShipCard(c) || IsFacilityCard(c) || IsMissionCard(c)))
            return piece;
        foreach (var kv in _stackOnHost)
        {
            if (kv.Value.Contains(piece)) return kv.Key;
            if (piece.Tag is Card pc && kv.Value.Any(b => ReferenceEquals(b.Tag, pc)))
                return kv.Key;
        }
        if (piece.Tag is Card ev)
        {
            var ae = _attachedEvents.FirstOrDefault(e => ReferenceEquals(e.Card, ev) && e.Host != null);
            if (ae?.Host != null) return ae.Host;
        }
        return piece;
    }

    private void OpenStackPeek(Border host, Card drag)
    {
        host = OwningHostOf(host) ?? host;
        if (host.Tag is not Card hostCard) return;
        var legal = BuriedLegalOn(host, drag);
        _peekLegalTargets.Clear();
        foreach (var c in legal)
            _peekLegalTargets.Add(c);
        _peekSnapCard = null;
        _detailHost = host;
        ShowCardDetail(hostCard);
        OpenCardDetailPopup();
        if (legal.Count == 1 && (DetailStackCards == null || DetailStackCards.Children.Count == 0))
        {
            if (DetailStackSection != null) DetailStackSection.Visibility = Visibility.Visible;
            if (DetailStackCards != null)
            {
                var mini = CreateMiniCard(legal[0], faceDown: false);
                mini.Width = 72;
                mini.Height = 100;
                mini.Margin = new Thickness(3);
                mini.Tag = legal[0];
                DetailStackCards.Children.Add(mini);
            }
        }
        NarrowSessionToPeekHost(host);
        StatusText.Text = legal.Count > 0
            ? $"Drop on a highlighted card in {hostCard.Name} ({legal.Count} legal)."
            : $"No legal target in {hostCard.Name}.";
        ApplyPeekSnapStyle();
        Dispatcher.BeginInvoke(new Action(ApplyPeekSnapStyle),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>Run / other ContentElements are not Visuals — GetParent throws.</summary>
    private static DependencyObject? ParentOf(DependencyObject? node)
    {
        if (node == null) return null;
        if (node is Visual or Visual3D)
            return VisualTreeHelper.GetParent(node);
        if (node is FrameworkContentElement fce)
            return fce.Parent ?? LogicalTreeHelper.GetParent(node);
        try { return LogicalTreeHelper.GetParent(node); }
        catch { return null; }
    }

    private void UpdatePeekSnapAt(Point windowPos)
    {
        if (DetailStackCards == null || _peekLegalTargets.Count == 0) return;
        Card? hit = null;
        var tree = InputHitTest(windowPos) as DependencyObject;
        while (tree != null)
        {
            if (tree is FrameworkElement fe && fe.Tag is Card tagged
                && PeekSetContains(tagged))
            {
                hit = tagged;
                break;
            }
            tree = ParentOf(tree);
        }
        if (hit == null)
        {
            foreach (var child in DetailStackCards.Children.OfType<FrameworkElement>())
            {
                var mini = child as Border ?? (child as Panel)?.Children.OfType<Border>().FirstOrDefault();
                if (mini?.Tag is not Card c || !PeekSetContains(c)) continue;
                try
                {
                    var tl = mini.TransformToAncestor(this).Transform(new Point(0, 0));
                    double w = mini.ActualWidth > 1 ? mini.ActualWidth
                        : (mini.RenderSize.Width > 1 ? mini.RenderSize.Width : 72);
                    double h = mini.ActualHeight > 1 ? mini.ActualHeight
                        : (mini.RenderSize.Height > 1 ? mini.RenderSize.Height : 100);
                    var rect = new Rect(tl, new Size(w, h));
                    rect.Inflate(14, 14);
                    if (rect.Contains(windowPos))
                    {
                        hit = c;
                        break;
                    }
                }
                catch { }
            }
        }
        if (ReferenceEquals(hit, _peekSnapCard)) return;
        _peekSnapCard = hit;
        ApplyPeekSnapStyle();
        if (hit != null)
        {
            var hostCard = _peekHoverHost?.Tag as Card ?? _detailHost?.Tag as Card;
            _snapSite = new TargetSite(TargetSiteKind.BuriedCard, hit, hostCard, null,
                TargetWhy.Nullify, "Nullify that Event.");
            StatusText.Text = $"Snap: {hit.Name} — drop to target.";
        }
        else
            _snapSite = null;
    }

    private bool PointOverPeekUi(Point windowPos)
    {
        bool Over(FrameworkElement? el)
        {
            if (el == null || el.Visibility != Visibility.Visible) return false;
            try
            {
                var tl = el.TransformToAncestor(this).Transform(new Point(0, 0));
                var sz = el.RenderSize;
                if (sz.Width < 2 || sz.Height < 2)
                    sz = new Size(Math.Max(el.ActualWidth, 2), Math.Max(el.ActualHeight, 2));
                return new Rect(tl, sz).Contains(windowPos);
            }
            catch { return false; }
        }
        return Over(CardDetailOverlay) || Over(DetailStackSection) || Over(DetailStackCards);
    }

    private bool PeekSetContains(Card c) =>
        _peekLegalTargets.Contains(c)
        || _peekLegalTargets.Any(t => string.Equals(t.Name, c.Name, StringComparison.OrdinalIgnoreCase));

    private void ApplyPeekSnapStyle()
    {
        if (DetailStackCards == null) return;
        foreach (var child in DetailStackCards.Children.OfType<FrameworkElement>())
        {
            var mini = child as Border ?? (child as Panel)?.Children.OfType<Border>().FirstOrDefault();
            if (mini?.Tag is not Card c || !PeekSetContains(c)) continue;
            bool on = _peekSnapCard != null && PeekSetContains(_peekSnapCard)
                      && (ReferenceEquals(c, _peekSnapCard)
                          || string.Equals(c.Name, _peekSnapCard.Name, StringComparison.OrdinalIgnoreCase));
            mini.BorderBrush = new SolidColorBrush(on
                ? Color.FromRgb(40, 255, 120)
                : Color.FromRgb(80, 220, 255));
            mini.BorderThickness = new Thickness(on ? 5 : 4);
            mini.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = on ? Color.FromRgb(40, 255, 120) : Color.FromRgb(80, 220, 255),
                BlurRadius = on ? 18 : 14,
                ShadowDepth = 0,
                Opacity = 0.95
            };
        }
    }

    private void CancelStackPeek(bool keepOverlay)
    {
        _peekHoverTimer?.Stop();
        _peekHoverTimer = null;
        _peekHoverHost = null;
        if (!keepOverlay)
            _peekSnapCard = null;
        if (!keepOverlay)
        {
            _peekLegalTargets.Clear();
            if (_isDragging && CardDetailOverlay != null
                && CardDetailOverlay.Visibility == Visibility.Visible
                && _detailHost != null)
            {
                // leave overlay open so the drop can still hit a highlighted mini
            }
        }
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

    /// <summary>View only — pixel column under/over a mission face. Board occupants come from Sync.</summary>
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

    private readonly Dictionary<Border, Border> _dockableAtMission = new();

    private Border? FindMissionForDockable(Border dockable)
    {
        if (_dockableAtMission.TryGetValue(dockable, out var pinned)
            && pinned != null
            && TableCanvas.Children.Contains(pinned)
            && pinned.Tag is Card pc && IsLandableLocation(pc))
            return pinned;

        double left = Canvas.GetLeft(dockable);
        if (double.IsNaN(left)) left = 0;
        Border? best = null;
        double bestDx = double.MaxValue;
        foreach (var m in TableCanvas.Children.OfType<Border>())
        {
            if (m.Tag is not Card c || !IsLandableLocation(c)) continue;
            double mx = Canvas.GetLeft(m);
            if (double.IsNaN(mx)) continue;
            double dx = Math.Abs(mx - left);
            if (dx < bestDx)
            {
                bestDx = dx;
                best = m;
            }
        }
        if (best != null && bestDx < 90)
        {
            _dockableAtMission[dockable] = best;
            return best;
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
        if (host.Tag is Card hostPrinted && cardBorder.Tag is Card stackedCard)
        {
            int ctrl = GetBorderOwner(cardBorder);
            if (ctrl != 1 && ctrl != 2) ctrl = _activePlayer;
            BoardStore.Current.ApplyHosted(stackedCard, hostPrinted, ctrl);
        }
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
        if (cardBorder.Tag is Card left)
            BoardStore.Current.RemoveFromForces(left.InstanceId);
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
        IconCatalog.FillStaffing(DetailStaffRow, null);
        IconCatalog.Fill(DetailIconRow, null);
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
        bool isMultiPersonnel = DualAffiliationRules.IsMulti(card)
                               && ModifierRules.IsPersonnelCard(card);
        if (!isShip && !isFac && !(isMission && missionHasMyAway) && !isMultiPersonnel)
            return;

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

        // Skill-nullify is not an action (7.10.0.1) — offer even if the ship is stopped.
        if (isShip)
            AddShipSkillNullifyButtons(cardBorder, AddBtn);

        if (_session.Segment == GameSession.TurnSegment.Execute)
        {
            if (isMultiPersonnel)
            {
                var next = NextAffiliationMode(card);
                AddBtn(
                    $"Affiliation: {DualAffiliationRules.DisplayName(DualAffiliationRules.CurrentMode(card) ?? "?")} → {DualAffiliationRules.DisplayName(next)}",
                    (_, _) => TrySwitchAffiliation(cardBorder, card, next));
            }
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
                    if (IsShipDocked(cardBorder))
                    {
                        AddBtn("Undock", (_, _) => TryUndockShip(cardBorder, card));
                        if (EscapePodHere(cardBorder, GetBorderOwner(cardBorder) is int o and (1 or 2) ? o : _activePlayer) != null)
                            AddBtn("Board Escape Pod crew", (_, _) => RecoverEscapePodCrew(cardBorder));
                    }
                    else
                    {
                        foreach (var fac in FacilitiesHereForDock(cardBorder))
                        {
                            var facB = fac;
                            string fname = (facB.Tag as Card)?.Name ?? "facility";
                            AddBtn($"Dock at {fname}", (_, _) => TryDockShip(cardBorder, card, facB));
                        }
                        AddBtn("Fly (highlight destinations)", (_, _) => BeginFlyHighlight(cardBorder, card));
                        AddBtn("Attack ship…", (_, _) => BeginAttackMode(cardBorder, card));
                    }
                    if (CanOfferPersonnelBattleFromShip(cardBorder))
                        AddBtn("Attack crew…", (_, _) => BeginPersonnelAttackFromHost(cardBorder));
                    if (GetHullDamage(cardBorder) > 0 && GetHullDamage(cardBorder) < 100)
                        AddBtn("Repair status", (_, _) => ShowRepairStatus(cardBorder, card));
                    AddBtn("Solvable missions?", (_, _) => HighlightSolvableMissions(GetCrewOnShip(cardBorder)));
                    if (ShipHasCloakingDevice(card) && !_cloakLocked.Contains(cardBorder))
                    {
                        AddBtn(IsShipCloaked(cardBorder) ? "Decloak" : "Cloak", (_, _) =>
                            ToggleCloak(cardBorder, card));
                    }
                    if (HasAttachedNamedInterrupt(cardBorder, "Distortion of Space/Time Continuum"))
                    {
                        AddBtn("Use Distortion of S/T…", (_, _) =>
                            UseDistortionOnShip(cardBorder));
                    }
                    if (DualAffiliationRules.IsMulti(card))
                    {
                        var next = NextAffiliationMode(card);
                        AddBtn($"Affiliation → {DualAffiliationRules.DisplayName(next)}", (_, _) =>
                            TrySwitchAffiliation(cardBorder, card, next));
                    }
                    foreach (var sd in CrewWithSpecialDownload(cardBorder))
                    {
                        var src = sd;
                        AddBtn($"Special Download ({src.Name})", (_, _) =>
                            TrySpecialDownload(src, GetBorderOwner(cardBorder)));
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
        else if (_session.Segment == GameSession.TurnSegment.Play && isShip)
        {
            foreach (var sd in CrewWithSpecialDownload(cardBorder))
            {
                var src = sd;
                AddBtn($"Special Download ({src.Name})", (_, _) =>
                    TrySpecialDownload(src, GetBorderOwner(cardBorder)));
            }
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
        mission = MissionPrintedFor(missionBorder, _activePlayer);
        var auth = AuthorizePlay(GameAction.AttemptMission(_activePlayer, mission));
        if (!auth.Ok)
        {
            ShowPlayError(auth.Message);
            return;
        }
        if (_attachedDilemmas.Any(d =>
                d.Kind == DilemmaRules.PersistKind.EdoProbe && ReferenceEquals(d.Host, missionBorder)))
        {
            ShowPlayError("Edo Probe: cannot attempt this mission until any player solves a different mission.");
            return;
        }
        TryCureFrameOfMindAt(missionBorder);

        // Solved / Scow / Supernova already denied via BoardPiece flags in EngineAuthority.
        // Track discards for Temporal Causality Loop
        _attemptMission = missionBorder;
        _attemptDiscards.Clear();

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

        if (!MissionRules.TeamMatchesMissionAffiliation(mission, team, EspionageIconsOn(missionBorder, _activePlayer)))
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

            if (_seniorStaffArmed
                && !MissionRules.IsPlanetMission(mission)
                && MissionRules.IsDilemma(seedCard))
            {
                _seniorStaffArmed = false;
                seedStack.RemoveAt(idx);
                _seedUnderMission[missionBorder] = seedStack;
                UpdateSeedBadge(missionBorder);
                var disc = _activePlayer == 2 ? _oppDiscardCards : _discardCards;
                if (!disc.Contains(seedCard)) disc.Add(seedCard);
                ShowCardReveal(seedCard, "Senior Staff Meeting",
                    $"First dilemma discarded: {seedCard.Name}.",
                    RevealButtons.Ok, seedCard.Name);
                _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                    $"Senior Staff Meeting discards {seedCard.Name}");
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

            if (TryNullifyEncounteredWindDancer(seedCard, missionBorder, seedStack))
                continue;

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
            var encAuth = AuthorizePlay(GameAction.EncounterDilemma(_activePlayer, seedCard, mission));
            if (!encAuth.Ok)
            {
                ShowPlayError(encAuth.Message);
                ClearCardActionUi();
                return;
            }

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
                        RevealButtons.YesNo, "Your choice") == RevealAnswer.Yes,
                ThermalDeflectors = HasThermalDeflectors(),
                AtOwnOutpost = GetDockablesUnderMission(missionBorder).Any(b =>
                    b.Tag is Card fc
                    && GetBorderOwner(b) == _activePlayer
                    && ((fc.Name ?? "").Contains("outpost", StringComparison.OrdinalIgnoreCase)
                        || (fc.Type ?? "").Contains("outpost", StringComparison.OrdinalIgnoreCase)
                        || (fc.Type ?? "").Contains("facility", StringComparison.OrdinalIgnoreCase))),
                TravelerAffecting = HasTableCard(EventRules.IsTravelerTranscendence)
            });

            var wrapped = EngineAuthority.WrapDilemma(seedCard, dilResult);
            _session.Log.AddDebug(_session.TurnNumber, "Engine", EngineAuthority.FormatResult(wrapped));

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
        int missionOwner = LocationIsYourMission(missionBorder, _activePlayer)
            ? _activePlayer
            : GetBorderOwner(missionBorder);
        var result = MissionRules.CanSolve(mission, team, dilemmasRemaining: 0,
            attemptingPlayer: _activePlayer, missionOwner: missionOwner,
            extraMissionIcons: EspionageIconsOn(missionBorder, _activePlayer));
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
    private int CountWormholesInHand(int owner)
    {
        var hand = owner == 2 ? _oppHandCards : _handCards;
        return hand.Count(InterruptRules.IsWormhole);
    }

    /// <summary>
    /// Drag removes the card from hand before drop (~RemoveCardFromZone).
    /// Pair-start must still count the Wormhole being played.
    /// </summary>
    private int CountWormholesForPairStart(int owner, Card playing)
    {
        var hand = owner == 2 ? _oppHandCards : _handCards;
        int n = hand.Count(InterruptRules.IsWormhole);
        if (InterruptRules.IsWormhole(playing) && !hand.Contains(playing))
            n++;
        return n;
    }

    private bool IsWormholeLocation(Border b) =>
        b.Tag is Card c && InterruptRules.IsWormholeLocationCard(
            IsMissionCard(c), CardKinds.IsTimeLocation(c));

    private Border? FindWormholeLocationAt(Point windowPos)
    {
        var hit = InputHitTest(windowPos) as DependencyObject;
        while (hit != null)
        {
            if (hit is Border b && IsWormholeLocation(b))
                return b;
            hit = ParentOf(hit);
        }
        // Same window-space rects as other snap hit-tests (PointToScreen), not TransformToAncestor.
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (!IsWormholeLocation(b) || b.Visibility != Visibility.Visible) continue;
            if (!TryGetBorderWindowRect(b, out var rect)) continue;
            rect.Inflate(18, 18);
            if (rect.Contains(windowPos)) return b;
        }
        return null;
    }

    private void RelocateShipAlongSpaceline(Border ship, Border? from, Border dest)
    {
        int owner = GetBorderOwner(ship);
        if (owner == 0) owner = _activePlayer;
        Canvas.SetLeft(ship, Canvas.GetLeft(dest));
        Canvas.SetTop(ship, Canvas.GetTop(dest) + DockSlotOffsetY(0, owner));
        if (from != null && !ReferenceEquals(from, dest))
            RelayoutDockablesUnderMission(from);
        RelayoutDockablesUnderMission(dest);
        UpdateHostBadge(ship);
    }

    private void RelocateShipToLocation(Border ship, Border dest)
    {
        var from = FindMissionForDockable(ship);
        RelocateShipAlongSpaceline(ship, from, dest);
        MarkStopped(ship);
        // Keep BoardStore SoT in sync after interrupt relocate (Fly/IM read Locations).
        SyncBoardFromTable(logDual: false);
    }

    private bool TryPlayWormholeFromHand(Card card, Point windowPos, int owner)
    {
        if (_wormholeShip != null)
        {
            var dest = _currentSnapHost != null && IsWormholeLocation(_currentSnapHost)
                ? _currentSnapHost
                : FindWormholeLocationAt(windowPos);
            if (dest == null)
            {
                ShowPlayError("Wormhole: drop the second card on a location (mission or time location).");
                return false;
            }
            if (_wormholeShip.Tag is not Card shipCard || dest.Tag is not Card destCard)
                return false;
            RelocateShipToLocation(_wormholeShip, dest);
            StatusText.Text = $"Wormhole: {shipCard.Name} → {destCard.Name} (stopped).";
            _session.Log.Add(_session.TurnNumber, $"P{owner}",
                $"Wormhole {shipCard.Name} → {destCard.Name}");
            DebugLog.Move(_session.TurnNumber, owner,
                $"wormhole {DebugLog.Card(shipCard)} → {DebugLog.Card(destCard)}");
            _wormholeShip = null;
            BeginPlayCardStack(card, isResponse: false, controllerOverride: owner, target: destCard);
            return true;
        }

        // Drag already removed this card from hand — count it back in for the pair gate.
        int wormholes = CountWormholesForPairStart(owner, card);
        DebugLog.Move(_session.TurnNumber, owner,
            $"wormhole pair-check count={wormholes} (hand+playing)");
        if (!InterruptRules.CanStartWormholePair(wormholes))
        {
            ShowPlayError("Wormhole requires two Wormholes. Play one on your exposed ship, the other on a location.");
            return false;
        }

        var ship = _currentSnapHost != null
                   && HostMatchesInterruptTargetForCard(_currentSnapHost, owner, card)
            ? _currentSnapHost
            : FindTeamOrShipHostAt(windowPos, owner, InterruptRules.PlayTarget.OwnShip, card);
        if (ship == null || ship.Tag is not Card sc || !IsShipCard(sc))
        {
            ShowPlayError("Wormhole: play this copy on your exposed ship (not cloaked).");
            return false;
        }
        int shipOwner = GetBorderOwner(ship);
        if (shipOwner == 0) shipOwner = owner;
        if (!InterruptRules.CanWormholeFirstOnShip(
                isShip: true,
                ownedByPlayer: shipOwner == owner,
                exposed: IsShipExposed(ship)))
        {
            ShowPlayError(IsShipExposed(ship)
                ? "Wormhole: first copy plays on your exposed ship."
                : "Wormhole: that ship is not exposed (cloaked ships are not exposed).");
            return false;
        }

        _wormholeShip = ship;
        BeginPlayCardStack(card, isResponse: false, controllerOverride: owner, target: sc);
        StatusText.Text = $"Wormhole on {sc.Name}. Play the second Wormhole on a location.";
        return true;
    }

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

        if (InterruptRules.IsWormhole(card))
            return TryPlayWormholeFromHand(card, windowPos, owner);

        if (InterruptRules.IsEscapePod(card))
        {
            if (_stack.IsOpen && _stack.Top != null
                && TimingRules.CanRespond(card, _stack.Top, owner).ok)
            {
                BeginPlayCardStack(card, isResponse: true, controllerOverride: owner,
                    target: _stack.Top.Card);
                return true;
            }
            ShowPlayError("Escape Pod plays just after your ship is destroyed.");
            return false;
        }

        if (InterruptRules.IsHugh(card))
        {
            if (_stack.IsOpen && _stack.Top != null
                && TimingRules.CanRespond(card, _stack.Top, owner).ok)
            {
                BeginPlayCardStack(card, isResponse: true, controllerOverride: owner,
                    target: _stack.Top.AttackerCard ?? _stack.Top.Card);
                return true;
            }
            // Rogue Borg: drop on ship/location — no detail pick (Spock: map ship → location).
            var host = FindTeamOrShipHostAt(windowPos, owner, InterruptRules.PlayTarget.AnyShip, card);
            if (host == null && _peekHoverHost != null && CountRogueBorgOn(_peekHoverHost) > 0)
                host = _peekHoverHost;
            if (host == null)
            {
                var at = FindMissionOrShipAt(windowPos);
                if (at != null && (CountRogueBorgOn(at) > 0
                    || GetDockablesUnderMission(at).Any(d => CountRogueBorgOn(d) > 0)))
                    host = at;
            }
            Card? chosen = null;
            if (host != null && (CountRogueBorgOn(host) > 0
                || (host.Tag is Card hcMission && IsMissionCard(hcMission) && GetDockablesUnderMission(host).Any(d => CountRogueBorgOn(d) > 0))))
            {
                chosen = host.Tag as Card;
            }
            if (chosen == null)
            {
                var buried = FindHughTargetCardAt(windowPos);
                if (buried != null) chosen = buried;
            }
            // Borg Ship Dilemma only when revealed + present (token or spaceline face).
            if (chosen == null && _borgShipToken?.Tag is Card borgTok
                && TimingRules.IsBorgShipDilemma(borgTok))
                chosen = borgTok;
            if (chosen == null)
            {
                var pool = CollectHughTargetCards();
                if (pool.Count == 0)
                {
                    ShowPlayError("Hugh: no Borg Ship dilemma (revealed) or Rogue Borg location in play.");
                    return false;
                }
                if (pool.Count == 1)
                    chosen = pool[0];
                else
                    chosen = PickCardFromList(
                        "Hugh: Borg Ship dilemma or Rogue Borg location.",
                        pool, "Hugh", card);
            }
            if (chosen == null)
            {
                ShowPlayError("Hugh: cancelled — returned to hand.");
                return false;
            }
            BeginPlayCardStack(card, isResponse: _stack.IsOpen, controllerOverride: owner, target: chosen);
            return true;
        }

        if (InterruptRules.IsKevinNullify(card) || InterruptRules.IsDevil(card))
        {
            var handStrip = _activePlayer == 2 ? OppHandStripBorder : PlayerHandStripBorder;
            if (handStrip != null && IsPointOverElement(handStrip, windowPos))
                return false;
            bool devil = InterruptRules.IsDevil(card);
            Card? target = _snapSite?.Card ?? _peekSnapCard ?? _tableColumnSnapCard;
            if (target == null && _peekLegalTargets.Count == 1)
                target = _peekLegalTargets.First();
            if (target == null)
                target = FindEventTargetAt(windowPos, card);
            if (target != null && !TargetQuery.IsLegal(card, target))
                target = null;
            if (target != null && _peekLegalTargets.Count > 0 && !PeekSetContains(target))
                target = null;
            if (target == null)
            {
                // Prefer host under cursor; otherwise all Events in play (TABLE + attached).
                var host = _peekHoverHost ?? _currentSnapHost ?? FindHostUnderWindow(windowPos);
                var pool = CollectNullifyPool(card, host);
                if (pool.Count == 0 && host != null)
                    pool = CollectNullifyPool(card, null); // whole-game Events incl. TABLE
                if (pool.Count == 1)
                    target = pool[0];
                else if (pool.Count == 0)
                {
                    ShowPlayError(devil
                        ? $"{card.Name}: drop on the Treaty / Horga'hn / Wind Dancer (or return to hand)."
                        : $"{card.Name}: drop on the Event to nullify (or return to hand).");
                    return false;
                }
                else
                {
                    target = PickCardFromList(
                        devil ? "Nullify which card?" : "Nullify which Event in play?",
                        pool, card.Name ?? "Kevin", card);
                    if (target == null)
                    {
                        ShowPlayError($"{card.Name}: cancelled — returned to hand.");
                        return false;
                    }
                }
            }
            if (target == null)
            {
                ShowPlayError($"{card.Name}: no target chosen.");
                return false;
            }
            var chk = TargetQuery.CanTarget(card, target);
            if (!chk.ok)
            {
                ShowPlayError(chk.reason);
                return false;
            }
            TraceEngine(GameAction.Play(owner, card, target));
            BeginPlayCardStack(card, isResponse: _stack.IsOpen, controllerOverride: owner, target: target);
            EndTargetSession();
            return true;
        }

        var need = InterruptRules.GetPlayTarget(card);
        if (need is InterruptRules.PlayTarget.OwnCrew or InterruptRules.PlayTarget.AnyCrew
            or InterruptRules.PlayTarget.OwnShip or InterruptRules.PlayTarget.AnyShip)
        {
            var host = _currentSnapHost != null
                       && HostMatchesInterruptTargetForCard(_currentSnapHost, owner, card)
                ? _currentSnapHost
                : FindTeamOrShipHostAt(windowPos, owner, need, card);
            if (host == null)
            {
                ShowPlayError($"{card.Name}: no legal target under the cursor. "
                    + "Highlighted ships/crew only — card returns to hand.");
                return false;
            }
            if (InterruptRules.IsRogueBorg(card) && !ShipIsOccupied(host))
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
            hit = ParentOf(hit);
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
        if (InterruptRules.IsWormhole(interrupt))
        {
            if (_wormholeShip != null)
                return IsWormholeLocation(host);
            if (host.Tag is not Card wh || !IsShipCard(wh)) return false;
            int o = GetBorderOwner(host);
            if (o == 0) o = owner;
            return InterruptRules.CanWormholeFirstOnShip(true, o == owner, IsShipExposed(host));
        }
        var spec = PlayOnRules.Parse(interrupt);
        if (spec.Host != PlayOnRules.Host.None)
            return HostMatchesPlayOn(host, owner, spec);
        if (InterruptRules.IsRogueBorg(interrupt))
            return host.Tag is Card hc && IsShipCard(hc) && ShipIsOccupied(host);
        if (InterruptRules.IsHugh(interrupt))
        {
            if (host.Tag is not Card hh) return false;
            // Borg Ship Dilemma face/token only (not Borg-affiliation ships).
            if (TimingRules.IsBorgShipDilemma(hh))
                return true;
            // Rogue Borg aboard this ship — drop without detail pick.
            if (IsShipCard(hh) && CountRogueBorgOn(host) > 0)
                return true;
            if (IsMissionCard(hh))
            {
                foreach (var dock in GetDockablesUnderMission(host).Concat(new[] { host }))
                    if (CountRogueBorgOn(dock) > 0) return true;
            }
            return false;
        }
        if (InterruptRules.IsCrosis(interrupt))
            return host.Tag is Card hc2 && IsShipCard(hc2);
        if (InterruptRules.IsIncomingMessage(interrupt)
            && !interrupt.Name.Contains("Attack Authorization", StringComparison.OrdinalIgnoreCase))
        {
            if (host.Tag is not Card hs || !IsShipCard(hs)) return false;
            string? need = InterruptRules.IncomingMessageAffiliation(interrupt)
                           ?? PlayOnRules.Parse(interrupt).Affiliation;
            return string.IsNullOrEmpty(need) || CardMatchesAffiliation(hs, need);
        }
        return HostMatchesInterruptTarget(host, owner, InterruptRules.GetPlayTarget(interrupt));
    }

    private bool HostMatchesPlayOn(Border host, int owner, PlayOnRules.Spec spec)
    {
        if (host.Tag is not Card hc) return false;
        int ho = GetBorderOwner(host);
        if (ho == 0) ho = owner;
        bool ownHost = ho == owner;
        bool isShip = IsShipCard(hc);
        bool isFac = IsFacilityCard(hc);
        bool isMission = IsMissionCard(hc);
        string tn = ((hc.Type ?? "") + " " + (hc.Name ?? "")).ToLowerInvariant();
        bool isOutpost = isFac && tn.Contains("outpost");

        bool hostOk = spec.Host switch
        {
            PlayOnRules.Host.Ship => isShip,
            PlayOnRules.Host.Outpost => isOutpost,
            PlayOnRules.Host.Facility => isFac,
            PlayOnRules.Host.Mission => isMission,
            PlayOnRules.Host.PlanetMission => isMission && MissionRules.IsPlanetMission(hc),
            PlayOnRules.Host.Crew => spec.Own ? HostHasPersonnelOf(host, owner) : HostHasPersonnelOf(host, 0),
            _ => false
        };
        if (!hostOk) return false;

        if (spec.Host is PlayOnRules.Host.Ship or PlayOnRules.Host.Outpost or PlayOnRules.Host.Facility)
        {
            if (spec.Own && !ownHost) return false;
            if (spec.Opponent && ownHost) return false;
        }
        if (spec.Exposed && isShip && !IsShipExposed(host)) return false;
        if (spec.Cloaked && isShip && !IsShipCloaked(host)) return false;
        if (spec.Occupied && !ShipIsOccupied(host)) return false;
        if (spec.Empty && ShipIsOccupied(host)) return false;
        if (!string.IsNullOrEmpty(spec.Affiliation) && !CardMatchesAffiliation(hc, spec.Affiliation))
            return false;
        return true;
    }

    private static bool CardMatchesAffiliation(Card card, string need)
    {
        var have = ReportingRules.GetAffiliations(card);
        if (have.Contains(need)) return true;
        if (!string.IsNullOrWhiteSpace(card.CurrentAffiliation)
            && ReportingRules.NormalizeAffil(card.CurrentAffiliation) == need)
            return true;
        return MissionRules.ParseAffiliationTokens(card.Affiliation).Contains(need);
    }

    private bool HostMatchesInterruptTarget(Border host, int owner, InterruptRules.PlayTarget need)
    {
        if (host.Tag is not Card hc) return false;
        int ho = GetBorderOwner(host);
        if (ho == 0) ho = owner;

        bool isShip = IsShipCard(hc);
        bool isMission = IsMissionCard(hc);
        if (!isShip && !isMission) return false;

        bool ownHost = ho == owner;
        bool hasOwnPersonnel = HostHasPersonnelOf(host, owner);
        bool hasAnyPersonnel = HostHasPersonnelOf(host, 0);

        return need switch
        {
            InterruptRules.PlayTarget.OwnShip => isShip && ownHost,
            InterruptRules.PlayTarget.AnyShip => isShip,
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
            if (InterruptRules.IsRogueBorg(c))
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

    private static bool IsNullifyBoardTarget(Card c) =>
        EventRules.IsEvent(c) || TreatyRules.IsTreatyCard(c)
        || ArtifactRules.IsHorgahn(c)
        || (c.Name ?? "").Equals("Wind Dancer", StringComparison.OrdinalIgnoreCase);

    private Card? FindEventTargetAt(Point windowPos, Card drag)
    {
        var fromPeek = PeekTargetUnderDetail(windowPos);
        if (fromPeek != null) return fromPeek;

        if (_tableColumnSnapCard != null && TargetQuery.IsLegal(drag, _tableColumnSnapCard))
            return _tableColumnSnapCard;
        Card? fromTableCol = EventOnTableColumnAt(windowPos);
        if (fromTableCol != null && TargetQuery.IsLegal(drag, fromTableCol))
            return fromTableCol;

        var hit = InputHitTest(windowPos) as DependencyObject;
        while (hit != null)
        {
            if (hit is FrameworkElement fe)
            {
                if (fe.Tag is HostCardRef href && TargetQuery.IsLegal(drag, href.Card))
                    return href.Card;
                if (fe.Tag is Card c && TargetQuery.IsLegal(drag, c))
                    return c;
            }
            hit = ParentOf(hit);
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

            if (TargetQuery.IsLegal(drag, hc))
                return hc;

            var events = new List<Card>();
            var stackedOn = StackOnHost(b);
            if (stackedOn != null)
            {
                foreach (var mb in stackedOn)
                    if (mb.Tag is Card ec && TargetQuery.IsLegal(drag, ec))
                        events.Add(ec);
            }
            foreach (var ae in EventsOn(b))
            {
                if (TargetQuery.IsLegal(drag, ae.Card) && !events.Contains(ae.Card))
                    events.Add(ae.Card);
            }
            if (_targetSites.Count > 0)
            {
                foreach (var s in _targetSites)
                    if (TargetQuery.SiteBelongsToHost(s, hc) && !events.Contains(s.Card))
                        events.Add(s.Card);
            }
            if (events.Count == 1) return events[0];
            if (events.Count > 1)
                return null;
        }

        // Span events (Gaps / Q-Net) sit between Host and Host2 — hit the midpoint.
        foreach (var ae in _attachedEvents)
        {
            if (ae.Host == null || ae.Host2 == null) continue;
            if (!TargetQuery.IsLegal(drag, ae.Card)) continue;
            try
            {
                double lx = Canvas.GetLeft(ae.Host) + TableCardWidth;
                double rx = Canvas.GetLeft(ae.Host2);
                double midX = (lx + rx) / 2.0;
                double midY = Canvas.GetTop(ae.Host) + TableCardHeight / 2.0;
                var midWin = TableCanvas.TransformToAncestor(this).Transform(new Point(midX, midY));
                if (Math.Abs(windowPos.X - midWin.X) <= TableCardWidth
                    && Math.Abs(windowPos.Y - midWin.Y) <= TableCardHeight)
                    return ae.Card;
            }
            catch { /* transform not ready */ }
        }
        return null;
    }

    private Card? PeekTargetUnderDetail(Point windowPos)
    {
        if (_peekLegalTargets.Count == 0) return null;
        if (CardDetailOverlay == null || CardDetailOverlay.Visibility != Visibility.Visible)
            return null;
        try
        {
            var tl = CardDetailOverlay.TransformToAncestor(this).Transform(new Point(0, 0));
            var rect = new Rect(tl, CardDetailOverlay.RenderSize);
            if (!rect.Contains(windowPos)) return null;
        }
        catch { return null; }

        var hit = InputHitTest(windowPos) as DependencyObject;
        while (hit != null)
        {
            if (hit is FrameworkElement fe && fe.Tag is Card c && _peekLegalTargets.Contains(c))
                return c;
            hit = ParentOf(hit);
        }
        if (_peekSnapCard != null && _peekLegalTargets.Contains(_peekSnapCard))
            return _peekSnapCard;
        return _peekLegalTargets.Count == 1 ? _peekLegalTargets.First() : null;
    }

    private Card? EventOnTableColumnAt(Point windowPos)
    {
        foreach (var panel in new[] { TablePermanentsPanel, OppTablePermanentsPanel })
        {
            if (panel == null) continue;
            try
            {
                var tl = panel.TransformToAncestor(this).Transform(new Point(0, 0));
                var rect = new Rect(tl, panel.RenderSize);
                if (!rect.Contains(windowPos)) continue;
            }
            catch { continue; }
            var hit = InputHitTest(windowPos) as DependencyObject;
            while (hit != null)
            {
                if (hit is FrameworkElement fe && fe.Tag is Card c && IsNullifyBoardTarget(c))
                    return c;
                hit = ParentOf(hit);
            }
            var singles = CollectKevinTargetEvents()
                .Where(c => _tablePermanentCards.Contains(c) || _oppTablePermanentCards.Contains(c))
                .ToList();
            if (singles.Count == 1) return singles[0];
        }
        return null;
    }

    private void HighlightKevinTableMinis(Func<Card, bool> legal)
    {
        void Mark(Border el, Card ev)
        {
            if (!legal(ev)) return;
            el.Effect = null;
            el.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 210, 80));
            el.BorderThickness = new Thickness(3);
        }
        foreach (var mini in (TablePermanentsPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
            if (mini.Tag is Card ev) Mark(mini, ev);
        foreach (var mini in (OppTablePermanentsPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
            if (mini.Tag is Card ev) Mark(mini, ev);
    }

    private List<Card> CollectKevinTargetEvents()
    {
        var list = new List<Card>();
        foreach (var row in CollectNullifyInPlay())
        {
            if (!TimingRules.CanKevinTargetEvent(row.Card).ok) continue;
            if (!list.Contains(row.Card)) list.Add(row.Card);
        }
        return list;
    }

    private List<Card> CollectDevilTargetCards()
    {
        var list = new List<Card>();
        foreach (var row in CollectNullifyInPlay())
        {
            if (!TimingRules.CanDevilTarget(row.Card).ok) continue;
            if (!list.Contains(row.Card)) list.Add(row.Card);
        }
        foreach (var dil in _lastEncounteredDilemma.Values)
        {
            if (dil != null && TimingRules.CanDevilTarget(dil).ok && !list.Contains(dil))
                list.Add(dil);
        }
        return list;
    }

    private Border? ResolveHughRogueBorgHost(Card? target)
    {
        if (target == null) return null;
        var unit = _rogueBorg.FirstOrDefault(r => ReferenceEquals(r.Card, target));
        if (unit?.Host != null) return unit.Host;
        var border = FindBorderForCard(target);
        if (border != null && CountRogueBorgOn(border) > 0) return border;
        if (border != null)
        {
            var mission = FindMissionForDockable(border) ?? border;
            foreach (var dock in GetDockablesUnderMission(mission).Concat(new[] { mission }))
                if (CountRogueBorgOn(dock) > 0) return dock;
        }
        return border;
    }

    private Card? FindHughTargetCardAt(Point windowPos)
    {
        var hit = InputHitTest(windowPos) as DependencyObject;
        while (hit != null)
        {
            if (hit is FrameworkElement fe && fe.Tag is Card c
                && TimingRules.IsHughBattleSource(c)
                && !InterruptRules.IsRogueBorg(c) && !IsShipCard(c))
                return c;
            if (hit is Border b && b.Tag is Card hc && IsMissionCard(hc))
            {
                foreach (var dock in GetDockablesUnderMission(b).Concat(new[] { b }))
                    if (CountRogueBorgOn(dock) > 0) return hc;
            }
            hit = ParentOf(hit);
        }
        return null;
    }


    private Border? FindMissionOrShipAt(Point windowPos)
    {
        var hit = InputHitTest(windowPos) as DependencyObject;
        while (hit != null)
        {
            if (hit is Border b && b.Tag is Card c && (IsMissionCard(c) || IsShipCard(c)))
                return b;
            hit = ParentOf(hit);
        }
        return null;
    }

    private List<Card> CollectHughTargetCards()
    {
        var list = new List<Card>();
        void Add(Card? c)
        {
            if (c == null) return;
            if (list.Any(x => ReferenceEquals(x, c))) return;
            list.Add(c);
        }
        // Rogue Borg branch: spaceline locations (mission faces) with Rogue Borg present.
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Tag is not Card c || !IsMissionCard(c)) continue;
            foreach (var dock in GetDockablesUnderMission(b).Concat(new[] { b }))
                if (CountRogueBorgOn(dock) > 0) { Add(c); break; }
        }
        // Borg Ship Dilemma only if revealed and present here (token or visible span/face).
        if (_borgShipToken?.Tag is Card tok && TimingRules.IsBorgShipDilemma(tok))
            Add(tok);
        foreach (var d in _attachedDilemmas)
        {
            if (d.Kind != DilemmaRules.PersistKind.BorgShip) continue;
            if (!TimingRules.IsBorgShipDilemma(d.Card)) continue;
            var span = FindBorderForCard(d.Card);
            // Conservative: token or visible dilemma face only (not mere Host attach).
            bool present = (span != null && span.Visibility == Visibility.Visible)
                           || (_borgShipToken?.Tag is Card t2 && ReferenceEquals(t2, d.Card));
            if (present) Add(d.Card);
        }
        return list;
    }

    private bool ApplyHugh(Card hugh, int controller, Card? target)
    {
        var battle = _stack.Items.LastOrDefault(x =>
            x.Kind is TimingRules.ActionKind.InitiateShipBattle
                or TimingRules.ActionKind.InitiatePersonnelBattle
            && TimingRules.IsHughBattleSource(x.AttackerCard ?? x.Card));
        bool hasJustInitiated = battle != null;
        bool targetIsBorgShipDilemma = TimingRules.IsBorgShipDilemma(target);

        Border? loc = null;
        if (!hasJustInitiated && !targetIsBorgShipDilemma)
        {
            loc = ResolveHughRogueBorgHost(target);
            if (loc != null && CountRogueBorgOn(loc) == 0)
            {
                var mission = FindMissionForDockable(loc) ?? loc;
                foreach (var dock in GetDockablesUnderMission(mission).Concat(new[] { mission }))
                {
                    if (CountRogueBorgOn(dock) > 0) { loc = dock; break; }
                }
            }
        }
        bool roguePresent = loc != null && CountRogueBorgOn(loc) > 0;

        switch (InterruptRules.DecideHugh(hasJustInitiated, targetIsBorgShipDilemma, roguePresent))
        {
            case InterruptRules.HughResolveMode.CancelJustInitiatedBattle:
                battle!.Cancelled = true;
                battle.CancelledBy = "Hugh";
                StatusText.Text = "Hugh cancels the Borg / Borg Ship / Rogue Borg battle.";
                _session.Log.Add(_session.TurnNumber, $"P{controller}", "Hugh cancels battle");
                return true;

            case InterruptRules.HughResolveMode.BlockBorgShipPulse:
                _hughBlocksBorgShipAttack = true;
                StatusText.Text = "Hugh: Borg Ship dilemma will not attack this pulse.";
                _session.Log.Add(_session.TurnNumber, $"P{controller}", "Hugh blocks Borg Ship attack");
                return true;

            case InterruptRules.HughResolveMode.KillRogueBorgAtLocation:
            {
                var mission = FindMissionForDockable(loc!) ?? loc!;
                var victims = new List<RogueBorgUnit>();
                foreach (var dock in GetDockablesUnderMission(mission).Concat(new[] { mission }))
                    victims.AddRange(RogueBorgUnitsOn(dock).ToList());
                int n = victims.Count;
                var hosts = victims.Select(v => v.Host).Where(h => h != null).Distinct().ToList();
                foreach (var rb in victims)
                    DiscardRogueBorgUnit(rb, "Hugh");
                StatusText.Text = $"Hugh kills {n} Rogue Borg at this location.";
                _session.Log.Add(_session.TurnNumber, $"P{controller}", $"Hugh kills {n} Rogue Borg");
                foreach (var h in hosts)
                    UpdateHostBadge(h);
                if (_detailHost != null && hosts.Contains(_detailHost) && _detailHost.Tag is Card hc)
                    ShowHostContents(_detailHost, hc);
                return true;
            }

            default:
                ShowPlayError("Hugh: no just-initiated Borg battle and no Rogue Borg at the target.");
                return false;
        }
    }


    /// <summary>
    /// Wind Dancer is in play the moment it is encountered. The Devil may nullify it
    /// before the filter is checked (Glossary nullify + printed Devil text).
    /// </summary>
    private bool TryNullifyEncounteredWindDancer(Card seedCard, Border missionBorder, List<Border> seedStack)
    {
        if (!TimingRules.CanDevilTarget(seedCard).ok) return false;
        if (!(seedCard.Name ?? "").Equals("Wind Dancer", StringComparison.OrdinalIgnoreCase))
            return false;

        int attempter = _activePlayer;
        foreach (int p in new[] { opponentOf(attempter), attempter })
        {
            var hand = p == 1 ? _handCards : _oppHandCards;
            var devil = hand.FirstOrDefault(InterruptRules.IsDevil);
            if (devil == null) continue;
            var ans = ShowCardReveal(
                devil,
                "The Devil",
                $"Wind Dancer just encountered at this mission.\nP{p}: play The Devil to nullify it?",
                RevealButtons.YesNo,
                "The Devil");
            if (ans != RevealAnswer.Yes) continue;

            hand.Remove(devil);
            SendCardTo(devil, p, TimingRules.Destination.Discard);

            int seedOwner = 0;
            int ri = seedStack.FindLastIndex(b => b.Tag is Card c && ReferenceEquals(c, seedCard));
            if (ri >= 0)
            {
                seedOwner = GetBorderOwner(seedStack[ri]);
                seedStack.RemoveAt(ri);
            }
            if (seedOwner is not (1 or 2)) seedOwner = opponentOf(attempter);
            _seedUnderMission[missionBorder] = seedStack;
            UpdateSeedBadge(missionBorder);
            SendCardTo(seedCard, seedOwner, TimingRules.Destination.Discard);
            ShowCardReveal(seedCard, "Nullified",
                "The Devil nullifies Wind Dancer. Mission attempt continues.",
                RevealButtons.Ok, seedCard.Name);
            _session.Log.Add(_session.TurnNumber, $"P{p}",
                "The Devil nullifies encountered Wind Dancer");
            StatusText.Text = "Wind Dancer nullified (The Devil). Attempt continues.";
            ShowActivePlayerHand();
            RefreshZoneCounts();
            return true;
        }
        return false;
    }

    private void NullifyEventInPlay(Card ev, int byPlayer)
    {
        var attached = _attachedEvents.Where(x => ReferenceEquals(x.Card, ev)).ToList();
        int owner = _tablePermanentCards.Contains(ev) ? 1
            : _oppTablePermanentCards.Contains(ev) ? 2
            : attached.FirstOrDefault()?.Owner is int ao && ao is 1 or 2 ? ao
            : byPlayer;

        foreach (var ae in attached)
        {
            if (EventRules.IsLoreReturns(ev) && ae.Host != null)
                RevertLoreReturnsControl(ae.Host);
            if (ae.Host != null) UpdateHostBadge(ae.Host);
            if (ae.Host2 != null) UpdateHostBadge(ae.Host2);
            _attachedEvents.Remove(ae);
        }

        // Gaps / Q-Net are inserted into the spaceline as a visible card.
        var span = FindBorderForCard(ev);
        if (span != null)
        {
            _spacelineOrder.Remove(span);
            if (TableCanvas.Children.Contains(span))
                TableCanvas.Children.Remove(span);
            RelayoutMissionsOnSpaceline();
        }

        foreach (var kv in _stackOnHost.ToList())
        {
            foreach (var b in kv.Value.ToList())
            {
                if (b.Tag is not Card c || !ReferenceEquals(c, ev)) continue;
                kv.Value.Remove(b);
                if (TableCanvas.Children.Contains(b))
                    TableCanvas.Children.Remove(b);
                UpdateHostBadge(kv.Key);
            }
        }

        RemoveCardFromTableColumn(ev);
        SendCardTo(ev, owner, TimingRules.Destination.Discard);
        RebuildTablePermanentsPanel();
        RefreshTableBuffs();
        if (_hostStripHost != null && _hostStripHost.Tag is Card hc)
            ShowHostContents(_hostStripHost, hc);
        _session.Log.Add(_session.TurnNumber, $"P{byPlayer}",
            $"Nullified {ev.Name} (owner P{owner} → discard)");
        StatusText.Text = $"{ev.Name} nullified → P{owner} discard.";
    }

    private bool TryResolveInterruptPlay(Card card, int controller, bool isResponse, Card? target = null)
    {
        var act = isResponse
            ? new GameAction
            {
                Kind = GameActionKind.Respond,
                Player = controller,
                Card = card,
                Target = target ?? _interruptTargetHost?.Tag as Card
            }
            : GameAction.Play(controller, card, target ?? _interruptTargetHost?.Tag as Card);

        var auth = AuthorizePlay(act);
        if (!auth.Ok)
        {
            ShowPlayError(auth.Message);
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(card)) hand.Add(card);
            RefreshHandStrips();
            RefreshZoneCounts();
            return true;
        }

        var r = auth.Interrupt ?? InterruptRules.Resolve(card);
        if (r.Kind == InterruptRules.Kind.TimingOnly)
        {
            // Catalog + registry own the card list; UI only applies nullify when a target event is present.
            if ((InterruptRules.IsKevinNullify(card) || InterruptRules.IsDevil(card)) && target != null)
            {
                NullifyEventInPlay(target, controller);
            }
            if (InterruptRules.IsHugh(card))
            {
                if (!ApplyHugh(card, controller, target))
                {
                    var handBack = controller == 1 ? _handCards : _oppHandCards;
                    if (!handBack.Contains(card)) handBack.Add(card);
                    RefreshHandStrips();
                    RefreshZoneCounts();
                    StatusText.Text = "Hugh: no valid effect - returned to hand.";
                    return true;
                }
            }

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
            case InterruptRules.Effect.IncomingMessage:
                ApplyIncomingMessage(card, controller);
                break;
            case InterruptRules.Effect.SubspaceInterference:
                ApplySubspaceInterference(card, controller, target);
                break;
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
                    var pool = disc.Where(c => !ModifierRules.IsPersonnelCard(c)).ToList();
                    var pick = PickCardFromList(
                        "Click a non-Personnel card from your discard pile.",
                        pool, "Palor Toff", card);
                    if (pick != null)
                    {
                        disc.Remove(pick);
                        hand.Add(pick);
                        StatusText.Text = $"Palor Toff: {pick.Name} to hand.";
                    }
                    break;
                }
            case InterruptRules.Effect.TheJuggler:
                {
                    int who = AskPlayer(card, "The Juggler", "Whose draw deck is shuffled?");
                    var draw = who == 1 ? _drawCards : _oppDrawCards;
                    var shuffled = draw.OrderBy(_ => _autoSeedRng.Next()).ToList();
                    draw.Clear();
                    draw.AddRange(shuffled);
                    _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}", $"Juggler → P{who}");
                    break;
                }
            case InterruptRules.Effect.LifeformScan:
                {
                    int oppPlayer = controller == 1 ? 2 : 1;
                    BeginOpponentPileInteract(
                        oppPlayer,
                        TargetPileType.Hand,
                        PileInteraction.ViewOnly | PileInteraction.FullView,
                        "Life-form Scan — examine opponent's hand, then Done.");
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
                // Pair is resolved on drop (exposed ship, then location). Stack only announces.
                break;
            case InterruptRules.Effect.EscapePod:
                break;
            case InterruptRules.Effect.Sanctuary:
                ApplyAsteroidSanctuary(card, controller);
                break;
            case InterruptRules.Effect.Distortion:
                ApplyDistortionContinuum(card, controller);
                break;
            case InterruptRules.Effect.Tachyon:
                ApplyTachyonGrid(card, controller);
                break;
            case InterruptRules.Effect.Transwarp:
                {
                    Border? shipB = PickOwnShip(controller);
                    if (shipB != null && shipB.Tag is Card sc)
                    {
                        int printed = BattleRules.EffectiveRange(sc, GetHullDamage(shipB));
                        int left = GetRemainingRange(shipB, sc);
                        int used = Math.Max(0, printed - left);
                        // Full RANGE is doubled; RANGE already spent this turn still counts.
                        SetShipRangeLeft(shipB, sc, Math.Max(0, printed * 2 - used));
                        // discard end of turn via attached dilemma-like flag on attached events list reuse
                        _attachedEvents.Add(new AttachedEvent
                        {
                            Card = card,
                            Kind = EventRules.Persist.None,
                            Owner = controller,
                            Host = shipB,
                            Countdown = 0
                        });
                        TurnExpiry.Register(_session, new ExpiringEffect
                        {
                            Key = $"Transwarp|{card.InstanceId}",
                            Owner = controller,
                            Card = card,
                            Verb = "Discard",
                            Note = "Transwarp Conduit"
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
            default:
                ApplyNamedAuInterrupt(card, controller);
                break;
        }

        NoteAuPlay(card, controller);

        if (r.OutOfPlay)
            SendCardTo(card, controller, TimingRules.Destination.OutOfPlay);
        else if (r.DiscardAfter)
            SendCardTo(card, controller, TimingRules.Destination.Discard);

        // Instant interrupts must never remain as free-floating board cards
        // (skip Rogue Borg / Crosis that legitimately sit on a host stack)
        if (r.Effect is not InterruptRules.Effect.RogueBorg
            and not InterruptRules.Effect.Crosis
            and not InterruptRules.Effect.IncomingMessage)
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
        var ships = TableCanvas.Children.OfType<Border>()
            .Where(b => b.Tag is Card sc && IsShipCard(sc) && GetBorderOwner(b) == owner)
            .ToList();
        return PickBorderFromList(null, ships, "Choose ship");
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
               || _tablePermanentCards.Any(EventRules.IsGoddess)
               || _oppTablePermanentCards.Any(EventRules.IsGoddess);
    }

    /// <summary>Prefer <see cref="HasTableCard"/> with EventRules.Is* helpers.</summary>
    private bool HasTableEvent(string name) =>
        HasTableCard(c => (c?.Name ?? "").Equals(name, StringComparison.OrdinalIgnoreCase));

    private bool HasTableCard(Func<Card?, bool> pred) =>
        _tablePermanentCards.Concat(_oppTablePermanentCards).Any(pred);

    /// <summary>Event on that player's TABLE column only (WNOHGB "You may…").</summary>
    private bool PlayerHasTableCard(int player, Func<Card?, bool> pred) =>
        (player == 2 ? _oppTablePermanentCards : _tablePermanentCards).Any(pred);

    private bool PlayerHasWnohgb(int player) =>
        PlayerHasTableCard(player, EventRules.IsWhereNoOneHasGoneBefore)
        || _attachedEvents.Any(e =>
            e.Kind == EventRules.Persist.Table
            && e.Owner == player
            && EventRules.IsWhereNoOneHasGoneBefore(e.Card));

    private bool HasPatternEnhancers() =>
        _attachedEvents.Any(e => e.Kind == EventRules.Persist.PatternEnhancers)
        || HasTableCard(EventRules.IsPatternEnhancers);

    /// <summary>Espionage on this mission: add [As] icon for that player if mission is [On].</summary>
    private IEnumerable<string> EspionageIconsOn(Border mission, int player)
    {
        var printed = MissionRules.ParseAffiliationTokens((mission.Tag as Card)?.Affiliation);
        foreach (var e in EventsOn(mission))
        {
            if (e.Kind != EventRules.Persist.Espionage || e.Owner != player) continue;
            if (string.IsNullOrEmpty(e.EspionageAs) || string.IsNullOrEmpty(e.EspionageOn)) continue;
            if (!printed.Contains(MissionRules.NormalizeAffiliationToken(e.EspionageOn)))
                continue;
            yield return e.EspionageAs;
        }
    }

    private IEnumerable<AttachedEvent> EventsOn(Border? host) =>
        host == null
            ? Enumerable.Empty<AttachedEvent>()
            : _attachedEvents.Where(e =>
            {
                // Gaps / Q-Net live on the spaceline, not on the two neighbouring missions.
                if (e.Kind is EventRules.Persist.Gaps or EventRules.Persist.QNet)
                    return false;
                return SameHostShip(e.Host, host) || SameHostShip(e.Host2, host);
            });

    private bool TryResolveEventPlay(Card ev, int controller)
    {
        var auth = AuthorizePlay(GameAction.Play(controller, ev, _eventPreferredHost?.Tag as Card));
        if (!auth.Ok)
        {
            ShowPlayError(auth.Message);
            var handBack = controller == 1 ? _handCards : _oppHandCards;
            if (!handBack.Contains(ev)) handBack.Add(ev);
            RefreshHandStrips();
            RefreshZoneCounts();
            return true;
        }

        var r = auth.EventPlay ?? EventRules.ResolvePlay(ev);
        if (EventRules.IsRedAlert(ev) && HasYellowAlert())
        {
            ShowPlayError("Yellow Alert prevents Red Alert!");
            RemoveCardFromTableColumn(ev);
            foreach (var ae in _attachedEvents.Where(e => ReferenceEquals(e.Card, ev)).ToList())
                _attachedEvents.Remove(ae);
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(ev)) hand.Add(ev);
            RebuildTablePermanentsPanel();
            RefreshZoneCounts();
            RefreshHandStrips();
            return true;
        }
        if (EventRules.IsWartimeConditions(ev) && string.IsNullOrEmpty(_wartimeVsAffiliation))
        {
            ShowPlayError("Wartime Conditions: a Federation ship must have been attacked first.");
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(ev)) hand.Add(ev);
            return true;
        }
        if (r.NeedsToxUthat)
        {
            bool toxOnTable = HasTableCard(ArtifactRules.IsToxUthat);
            var ownTable = controller == 1 ? _tablePermanentCards : _oppTablePermanentCards;
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!toxOnTable && !ownTable.Any(ArtifactRules.IsToxUthat) && !hand.Any(ArtifactRules.IsToxUthat))
            {
                ShowPlayError("Supernova erfordert Tox Uthat.");
                if (!hand.Contains(ev)) hand.Add(ev);
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
                ae.TravelerPlayer = AskPlayer(ev, "The Traveler",
                    "Which player gets the Traveler draw benefit?");
            }
            if ((r.Persist == EventRules.Persist.QNet || r.Persist == EventRules.Persist.Gaps)
                && ae.Host2 == null)
            {
                ae.Host2 = PickAdjacentMission(host);
            }
            if (r.Persist is EventRules.Persist.QNet or EventRules.Persist.Gaps)
            {
                ClearEventTargetHighlights();
                RelayoutMissionsOnSpaceline();
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
                    + _tablePermanentCards.Count(EventRules.IsStaticWarpBubble)
                    + _oppTablePermanentCards.Count(EventRules.IsStaticWarpBubble);
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
            if (r.Persist == EventRules.Persist.WarpCore && host != null && host.Tag is Card wcbShip)
            {
                UpdateHostBadge(host);
                ShowHostContents(host, wcbShip);
                StatusText.Text =
                    $"Warp Core Breach on {wcbShip.Name}. Destroys at the end of that ship's controller's next turn. "
                    + "ENGINEER may nullify (button on the ship).";
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
            if (r.Persist == EventRules.Persist.NeuralServo && host != null)
            {
                if (!TryApplyNeuralServo(ev, host, controller, ae))
                {
                    RemoveCardFromHostStack(host, FindBorderForCard(ev) ?? host);
                    RemoveCardFromTableColumn(ev);
                    var h = controller == 1 ? _handCards : _oppHandCards;
                    if (!h.Contains(ev)) h.Add(ev);
                    return true;
                }
            }
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
            if (r.Persist == EventRules.Persist.YellowAlert)
                ApplyYellowAlert(controller, ev);
            RefreshTableBuffs();
        }

        NoteAuPlay(ev, controller);
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
            if (b.Visibility != Visibility.Visible || b.Tag is not Card c) continue;
            if (TargetQuery.CanPlayOn(ev, c, FactsFor(b), controller).ok)
                candidates.Add(b);
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

    /// <summary>View only — midpoint paint + fallback if BoardStore has no locations yet.</summary>
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
        return PickBorderFromList(ev, candidates, title);
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
        left = ResolveOnSpaceline(left);
        right = ResolveOnSpaceline(right);
        int i1 = _spacelineOrder.IndexOf(left);
        int i2 = _spacelineOrder.IndexOf(right);
        if (i1 < 0) i1 = 0;
        if (i2 < 0) i2 = Math.Min(i1 + 1, _spacelineOrder.Count);
        int insert = Math.Min(i1, i2) + 1;
        if (insert < 0) insert = 0;
        if (insert > _spacelineOrder.Count) insert = _spacelineOrder.Count;

        double guessX = Canvas.GetLeft(left);
        if (double.IsNaN(guessX)) guessX = 40;
        guessX += TableCardWidth + MissionGap;
        var border = AddCardToTable(ev, guessX, SpacelineY, TableCardWidth);
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(180, 120, 220));
        border.BorderThickness = new Thickness(2);
        border.ToolTip = (ev.Name ?? "Span") + "\nSpan event between missions";
        SetBorderOwner(border, owner);
        Panel.SetZIndex(border, 9);
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

    /// <summary>
    /// One list for halo + snap-in-field + drop. New cards join via PlayOnRules / named override.
    /// </summary>
    private List<Border> CollectLegalSnapHosts(Card card, int owner)
    {
        var list = new List<Border>();
        void Add(Border? b)
        {
            if (b == null || list.Contains(b)) return;
            if (b.Visibility != Visibility.Visible) return;
            list.Add(b);
        }

        if (InterruptRules.IsWormhole(card))
        {
            if (_wormholeShip != null)
            {
                foreach (var b in TableCanvas.Children.OfType<Border>())
                    if (IsWormholeLocation(b)) Add(b);
                return list;
            }
            foreach (var b in TableCanvas.Children.OfType<Border>())
            {
                if (b.Tag is not Card hc || !IsShipCard(hc)) continue;
                if (GetBorderOwner(b) != owner) continue;
                if (IsShipExposed(b)) Add(b);
            }
            return list;
        }

        if (InterruptRules.IsHugh(card))
        {
            foreach (var b in TableCanvas.Children.OfType<Border>())
            {
                if (b.Tag is not Card hc) continue;
                if (InterruptRules.IsRogueBorg(hc)) continue;
                if (TimingRules.IsHughBattleSource(hc) && !IsShipCard(hc))
                    Add(b);
                else if (IsMissionCard(hc))
                {
                    foreach (var dock in GetDockablesUnderMission(b).Concat(new[] { b }))
                        if (CountRogueBorgOn(dock) > 0) { Add(b); break; }
                }
            }
            foreach (var d in _attachedDilemmas)
            {
                if (d.Kind != DilemmaRules.PersistKind.BorgShip) continue;
                var span = FindBorderForCard(d.Card);
                if (span != null) Add(span);
                if (d.Host != null) Add(d.Host);
            }
            Add(_borgShipToken);
            return list;
        }

        if (InterruptRules.IsKevinNullify(card) || InterruptRules.IsDevil(card))
        {
            bool devil = InterruptRules.IsDevil(card);
            bool Legal(Card ev) => devil
                ? TimingRules.CanDevilTarget(ev).ok
                : TimingRules.CanKevinTargetEvent(ev).ok;
            foreach (var e in _attachedEvents)
            {
                if (!Legal(e.Card)) continue;
                var span = FindBorderForCard(e.Card);
                if (span != null) Add(span);
            }
            foreach (var b in TableCanvas.Children.OfType<Border>())
            {
                if (b.Tag is not Card hc) continue;
                if (Legal(hc)) { Add(b); continue; }
                var stacked = StackOnHost(b);
                bool has = stacked != null && stacked.Any(x => x.Tag is Card c && Legal(c));
                if (!has)
                {
                    // Halo the ship/mission only when the event has no face of its own
                    // (Q-Net / Gaps on the spaceline snap to that card, not the missions).
                    has = _attachedEvents.Any(e =>
                        Legal(e.Card)
                        && (SameHostShip(e.Host, b) || SameHostShip(e.Host2, b))
                        && FindBorderForCard(e.Card) == null);
                }
                if (has) Add(b);
            }
            return list;
        }

        if (EventRules.IsEvent(card) && !_seedPhaseActive)
        {
            var tk = EventRules.GetTargetKind(EventRules.ResolvePlay(card));
            if (EventRules.NeedsTableHost(tk) && tk != EventRules.TargetKind.GapBetweenMissions)
            {
                foreach (var h in CollectEventTargets(card, owner, tk, EventRules.ResolvePlay(card).Place))
                    Add(h);
                return list;
            }
        }

        if (InterruptRules.IsInterrupt(card) && InterruptRules.NeedsDropTarget(card))
        {
            foreach (var b in TableCanvas.Children.OfType<Border>())
            {
                if (b.Tag is not Card) continue;
                if (HostMatchesInterruptTargetForCard(b, owner, card))
                    Add(b);
            }
            return list;
        }

        return list;
    }

    private Border? NearestLegalSnapHost(Card card, int owner, double cx, double cy, out double distance)
    {
        Border? best = null;
        distance = double.MaxValue;
        foreach (var b in CollectLegalSnapHosts(card, owner))
        {
            double bw = b.Width > 0 ? b.Width : TableCardWidth;
            double bh = b.Height > 0 ? b.Height : TableCardHeight;
            double bx = Canvas.GetLeft(b) + bw / 2.0;
            double by = Canvas.GetTop(b) + bh / 2.0;
            double d = Math.Sqrt((cx - bx) * (cx - bx) + (cy - by) * (cy - by));
            if (d < distance) { distance = d; best = b; }
        }
        return best;
    }

    /// <summary>Highlight only legal drop targets for the card currently being played.</summary>
    private void ShowLegalPlayHighlights(Card card, bool fromHand)
    {
        ClearEventTargetHighlights();
        int snapOwner = _zoneDragRef?.Opponent == true ? 2 : _activePlayer;
        if (!_seedPhaseActive)
        {
            // Named overrides own their halo color + TABLE / gap extras.
            // Do not return after a generic cyan pass — that skipped Q-Net midpoints
            // and painted Hugh ships cyan instead of the ship target color.
            if (InterruptRules.IsKevinNullify(card) || InterruptRules.IsDevil(card))
            {
                BeginTargetSession(card);
                HighlightNullifySnapTargets(card, snapOwner);
                return;
            }
            if (InterruptRules.IsHugh(card))
            {
                HighlightHughSnapTargets(card, snapOwner);
                return;
            }
            if (TargetQuery.IsPlayOnDrag(card) || TargetQuery.IsGapPlay(card))
            {
                BeginTargetSession(card);
                HighlightPlayOnSites();
                return;
            }
            var snapHosts = CollectLegalSnapHosts(card, snapOwner);
            if (snapHosts.Count > 0)
            {
                var color = Color.FromRgb(80, 200, 220);
                foreach (var h in snapHosts)
                    AddTargetHalo(h, color);
                if (EventRules.IsEvent(card)
                    && EventRules.GetTargetKind(EventRules.ResolvePlay(card))
                       == EventRules.TargetKind.GapBetweenMissions)
                { /* gap midpoints still drawn below */ }
                else if (InterruptRules.IsInterrupt(card) || TargetingRules.UsesBoardSnap(card))
                    return;
            }
        }
        if (_seedPhaseActive || (_devPlaySeedFromHand && fromHand && IsSeedableUnderMission(card)))
        {
            BeginTargetSession(card);
            HighlightPlayOnSites();
            return;
        }

        if (EventRules.IsEvent(card))
        {
            ShowEventTargetHighlights(card);
            return;
        }

        if (InterruptRules.IsWormhole(card))
        {
            if (_wormholeShip != null)
            {
                foreach (var b in TableCanvas.Children.OfType<Border>().ToList())
                {
                    if (IsWormholeLocation(b))
                        AddTargetHalo(b, Color.FromRgb(180, 120, 220));
                }
            }
            else
            {
                int owner = _zoneDragRef?.Opponent == true ? 2 : _activePlayer;
                foreach (var b in TableCanvas.Children.OfType<Border>().ToList())
                {
                    if (b.Tag is not Card hc || !IsShipCard(hc)) continue;
                    if (GetBorderOwner(b) != owner) continue;
                    if (!IsShipExposed(b)) continue;
                    AddTargetHalo(b, Color.FromRgb(80, 200, 220));
                }
            }
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
            BeginTargetSession(card);
            HighlightPlayOnSites();
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

    /// <summary>
    /// Kevin / Devil: TABLE minis + ship stacks + Q-Net/Gaps face or midpoint.
    /// One path for halo (start-drag) so CollectLegalSnapHosts and this stay aligned.
    /// </summary>
    private void HighlightNullifySnapTargets(Card card, int owner)
    {
        bool devil = InterruptRules.IsDevil(card);
        bool Legal(Card ev) => devil
            ? TimingRules.CanDevilTarget(ev).ok
            : TimingRules.CanKevinTargetEvent(ev).ok;

        var gold = Color.FromRgb(220, 180, 60);
        foreach (var h in CollectLegalSnapHosts(card, owner))
            AddTargetHalo(h, gold);

        foreach (var e in _attachedEvents)
        {
            if (e.Host == null || e.Host2 == null) continue;
            if (!Legal(e.Card)) continue;
            if (FindBorderForCard(e.Card) != null) continue; // face already halo'd
            double lx = Canvas.GetLeft(e.Host) + TableCardWidth;
            double rx = Canvas.GetLeft(e.Host2);
            double x = (lx + rx) / 2.0 - TableCardWidth / 2.0;
            var rect = new Rectangle
            {
                Width = TableCardWidth,
                Height = TableCardHeight,
                Stroke = new SolidColorBrush(gold),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Fill = new SolidColorBrush(Color.FromArgb(50, gold.R, gold.G, gold.B)),
                IsHitTestVisible = false,
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, Canvas.GetTop(e.Host));
            Panel.SetZIndex(rect, 20);
            TableCanvas.Children.Add(rect);
            _targetHighlights.Add(rect);
        }

        HighlightKevinTableMinis(Legal);
        void MarkStrip(Border el, Card ev)
        {
            if (!Legal(ev)) return;
            el.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 210, 80));
            el.BorderThickness = new Thickness(2);
        }
        foreach (var mini in (OppStripPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
            if (mini.Tag is HostCardRef href) MarkStrip(mini, href.Card);
        foreach (var mini in (PlayerStripPanel?.Children.OfType<Border>() ?? Enumerable.Empty<Border>()).ToList())
            if (mini.Tag is HostCardRef href) MarkStrip(mini, href.Card);
    }

    private void HighlightHughSnapTargets(Card card, int owner)
    {
        var red = Color.FromRgb(180, 40, 40);
        foreach (var h in CollectLegalSnapHosts(card, owner))
            AddTargetHalo(h, red);
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

    private void HighlightPlayOnSites()
    {
        var hostColor = Color.FromRgb(80, 200, 220);
        var gapColor = Color.FromRgb(180, 120, 220);
        foreach (var s in _targetSites)
        {
            if (s.Kind == TargetSiteKind.GapSpan && s.Host != null && s.Host2 != null)
            {
                var left = FindBorderForCard(s.Host);
                var right = FindBorderForCard(s.Host2);
                if (left == null || right == null) continue;
                double lx = Canvas.GetLeft(left) + TableCardWidth;
                double rx = Canvas.GetLeft(right);
                double x = (lx + rx) / 2.0 - TableCardWidth / 2.0;
                var rect = new Rectangle
                {
                    Width = TableCardWidth,
                    Height = TableCardHeight,
                    Stroke = new SolidColorBrush(gapColor),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 3 },
                    Fill = new SolidColorBrush(Color.FromArgb(50, gapColor.R, gapColor.G, gapColor.B)),
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
            else
            {
                var b = FindBorderForCard(s.Card);
                if (b != null) AddTargetHalo(b, hostColor);
            }
        }
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
        HighlightHandReturn(false);
        RebuildTablePermanentsPanel();
        void ResetStrip(Panel? panel)
        {
            if (panel == null) return;
            foreach (var mini in panel.Children.OfType<Border>())
            {
                mini.Effect = null;
                if (mini.Tag is HostCardRef)
                {
                    mini.BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));
                    mini.BorderThickness = new Thickness(1);
                }
            }
        }
        ResetStrip(PlayerStripPanel);
        ResetStrip(OppStripPanel);
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
        var plan = InstantEventRules.Decide(r);
        if (plan.DrawCards > 0)
        {
            int who = AskPlayer(ev, ev.Name ?? "Event",
                $"Which player draws {plan.DrawCards} card(s)?");
            int saved = _activePlayer;
            _activePlayer = who;
            for (int i = 0; i < plan.DrawCards; i++)
                DrawOneToHand();
            _activePlayer = saved;
            ShowActivePlayerHand();
            _session.Log.Add(_session.TurnNumber, $"P{controller}",
                $"{ev.Name}: P{who} draws {plan.DrawCards}");
        }
        if (plan.Masaka)
        {
            int who = AskPlayer(ev, "Masaka Transformations",
                "Whose hand is placed under their draw deck and redrawn (same number)?");
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
                $"Effect applied to Player {who} ({n} cards redrawn).");
        }
        if (plan.ResQ)
        {
            var disc = controller == 1 ? _discardCards : _oppDiscardCards;
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (disc.Count == 0)
            {
                StatusText.Text = "Res-Q: Discard leer.";
                return;
            }
            Card? pick = PickCardFromList(
                "Click a card from your discard pile to take into hand.",
                disc.ToList(), "Res-Q", ev);
            pick ??= disc[^1];
            disc.Remove(pick);
            hand.Add(pick);
            ShowActivePlayerHand();
            RefreshZoneCounts();
            StatusText.Text = $"Res-Q: {pick.Name} to hand.";
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
        var auth = AuthorizePlay(GameAction.Play(controller, art));
        if (!auth.Ok)
        {
            ShowPlayError(auth.Message);
            var handBack = controller == 1 ? _handCards : _oppHandCards;
            if (!handBack.Contains(art)) handBack.Add(art);
            RefreshHandStrips();
            RefreshZoneCounts();
            return true;
        }
        // auth.Artifact is acquire payload; hand-play uses name helpers below.

        if (ArtifactRules.IsVulcanStoneOfGol(art))
        {
            // Away Team an gewählter Planet-Mission: ohne Youth und CUNNING≤7 sterben
            var planets = TableCanvas.Children.OfType<Border>()
                .Where(b => b.Tag is Card c
                            && CardKinds.IsMission(c)
                            && MissionRules.IsPlanetMission(c))
                .ToList();
            if (planets.Count == 0)
            {
                StatusText.Text = "Stone of Gol: keine Planet-Mission.";
                SendCardTo(art, controller, TimingRules.Destination.Discard);
                return true;
            }
            Border target = PickBorderFromList(art, planets, "Stone of Gol: attack away team at which planet?")
                            ?? planets[0];
            int kills = 0;
            if (_stackOnHost.TryGetValue(target, out var team))
            {
                foreach (var b in team.ToList())
                {
                    if (b.Tag is not Card p || !ModifierRules.IsPersonnelCard(p)) continue;
                    int own = GetBorderOwner(b); if (own == 0) own = 1;
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

        if (ArtifactRules.IsThoughtMaker(art))
        {
            var types = new[] { "Personnel", "Ship", "Event", "Interrupt", "Equipment", "Dilemma", "Doorway" };
            var typeCards = types.Select(t => new Card { Name = t, Type = "Card type" }).ToList();
            string chosen = PickCardFromList(
                "Click the card type to send to the bottom of opponent's draw deck.",
                typeCards, "Thought Maker", art)?.Name ?? types[0];
            var oppDraw = controller == 1 ? _oppDrawCards : _drawCards;
            var moved = oppDraw
                .Where(c => (c.Type ?? "").Contains(chosen, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var c in moved)
                oppDraw.Remove(c);
            foreach (var c in moved.OrderBy(_ => Guid.NewGuid()))
                oppDraw.Add(c);
            SendCardTo(art, controller, TimingRules.Destination.Discard);
            StatusText.Text = $"Thought Maker: {moved.Count}× {chosen} im Gegner-Draw nach unten.";
            RefreshZoneCounts();
            return true;
        }

        if (ArtifactRules.IsKurlanNaiskos(art))
        {
            var ownShips = TableCanvas.Children.OfType<Border>()
                .Where(b => b.Tag is Card sc && IsShipCard(sc) && GetBorderOwner(b) == controller)
                .ToList();
            Border? shipB = PickBorderFromList(art, ownShips, "Kurlan Naiskos: play on which ship?");
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

        if (ArtifactRules.IsToxUthat(art))
        {
            CommitCardToTable(art, controller);
            StatusText.Text = "Tox Uthat on table (Supernova protection / nullify).";
            return true;
        }

        if (ArtifactRules.IsHorgahn(art))
        {
            CommitCardToTable(art, controller);
            StatusText.Text =
                "Horga'hn on table. Each turn: extra normal card play OR extra card at end of turn.";
            return true;
        }

        return false;
    }

    private void ApplyArtifactAcquire(Card art, Border missionBorder, Card mission)
    {
        var acq = ArtifactRules.ResolveAcquire(art);
        ShowCardReveal(art, "Artifact verdient",
            (art.Text ?? "") + "\n\n→ " + acq.Message,
            RevealButtons.Ok, art.Name);

        var place = ArtifactRules.DecideAcquirePlacement(
            acq.Kind, MissionRules.IsPlanetMission(mission));

        switch (place)
        {
            case ArtifactRules.AcquirePlacement.ImmediateDiscard:
                if (ArtifactRules.ShouldDownloadOnAcquire(acq))
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

            case ArtifactRules.AcquirePlacement.PlaceOnTable:
                if (acq.GrantsHorgahn)
                {
                    if (_activePlayer == 1) _horgahnP1 = true;
                    else _horgahnP2 = true;
                }
                CommitCardToTable(art, _activePlayer);
                StatusText.Text = acq.Message;
                break;

            case ArtifactRules.AcquirePlacement.EquipmentOnPlanetMission:
                AttachCardToHost(art, missionBorder, _activePlayer);
                StatusText.Text = acq.Message;
                break;

            case ArtifactRules.AcquirePlacement.EquipmentPreferOwnShip:
            {
                Border host = missionBorder;
                foreach (var dock in GetDockablesUnderMission(missionBorder))
                {
                    if (dock.Tag is Card dc && IsShipCard(dc) && GetBorderOwner(dock) == _activePlayer)
                    {
                        host = dock;
                        break;
                    }
                }
                AttachCardToHost(art, host, _activePlayer);
                StatusText.Text = acq.Message;
                break;
            }

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

    private bool HasHorgahn(int player)
    {
        var table = player == 2 ? _oppTablePermanentCards : _tablePermanentCards;
        return table.Any(ArtifactRules.IsHorgahn);
    }

    /// <summary>Leaving play ends continuous effects (Horga'hn extra play/draw, …).</summary>
    private void OnCardLeftPlay(Card card)
    {
        if (ArtifactRules.IsHorgahn(card))
        {
            SetHorgahnFlag(1, false);
            SetHorgahnFlag(2, false);
        }
        if (EventRules.IsRedAlert(card))
            OfferYellowAlertDownload("Red Alert! left play.");
    }

    private void SetHorgahnFlag(int player, bool on)
    {
        if (player == 1) _horgahnP1 = on;
        else _horgahnP2 = on;
    }

    /// <summary>
    /// The Traveler: Transcendence nullifies Static Warp Bubble while in play (both stay on table).
    /// </summary>
    private bool IsTravelerInPlay()
    {
        if (_attachedEvents.Any(e => e.Kind == EventRules.Persist.Traveler))
            return true;
        return HasTableCard(EventRules.IsTravelerTranscendence);
    }

    private bool HasRedAlert(int player)
    {
        if (_attachedEvents.Any(e => e.Kind == EventRules.Persist.RedAlert && e.Owner == player))
            return true;
        var table = player == 2 ? _oppTablePermanentCards : _tablePermanentCards;
        return table.Any(EventRules.IsRedAlert);
    }

    private bool HasYellowAlert() =>
        _attachedEvents.Any(e => e.Kind == EventRules.Persist.YellowAlert)
        || _tablePermanentCards.Any(EventRules.IsYellowAlert)
        || _oppTablePermanentCards.Any(EventRules.IsYellowAlert);

    private bool HasThermalDeflectors() =>
        _attachedEvents.Any(e => e.Kind == EventRules.Persist.Thermal)
        || _tablePermanentCards.Any(EventRules.IsThermalDeflectors)
        || _oppTablePermanentCards.Any(EventRules.IsThermalDeflectors);

    private void RefreshTableBuffs()
    {
        ModifierRules.TableBuffs.YellowAlertPlayer =
            _attachedEvents.FirstOrDefault(e => e.Kind == EventRules.Persist.YellowAlert)?.Owner ?? 0;
        if (ModifierRules.TableBuffs.YellowAlertPlayer == 0)
        {
            if (_tablePermanentCards.Any(EventRules.IsYellowAlert))
                ModifierRules.TableBuffs.YellowAlertPlayer = 1;
            else if (_oppTablePermanentCards.Any(EventRules.IsYellowAlert))
                ModifierRules.TableBuffs.YellowAlertPlayer = 2;
        }
        ModifierRules.TableBuffs.LowerDecksPlayer =
            _attachedEvents.FirstOrDefault(e => e.Kind == EventRules.Persist.LowerDecks)?.Owner ?? 0;
    }

    private void ApplyYellowAlert(int controller, Card ev)
    {
        foreach (var ae in _attachedEvents.Where(e => e.Kind == EventRules.Persist.RedAlert).ToList())
        {
            _attachedEvents.Remove(ae);
            SendCardTo(ae.Card, ae.Owner, TimingRules.Destination.Discard);
        }
        foreach (var c in _tablePermanentCards.Where(EventRules.IsRedAlert).ToList())
        {
            _tablePermanentCards.Remove(c);
            SendCardTo(c, 1, TimingRules.Destination.Discard);
        }
        foreach (var c in _oppTablePermanentCards.Where(EventRules.IsRedAlert).ToList())
        {
            _oppTablePermanentCards.Remove(c);
            SendCardTo(c, 2, TimingRules.Destination.Discard);
        }
        _redAlertPlaysLeft = 0;
        ShowCardReveal(ev, "Yellow Alert",
            "Red Alert! is cancelled and cannot be played while Yellow Alert remains. Your personnel CUNNING +1.",
            RevealButtons.Ok, ev.Name);
        RebuildTablePermanentsPanel();
        RefreshTableBuffs();
    }

    /// <summary>Red Alert printed: when nullified, any player may immediately download Yellow Alert.</summary>
    private void OfferYellowAlertDownload(string why)
    {
        // "Any player" = each player may, not an exclusive P1-or-P2 pick.
        foreach (int who in new[] { 1, 2 })
        {
            string ans = AskChoice(null, $"P{who}: Download Yellow Alert?",
                why + " Any player may immediately download Yellow Alert.",
                "Download Yellow Alert", "Pass");
            if (!ans.StartsWith("Download", StringComparison.OrdinalIgnoreCase))
                continue;
            TryDownloadNamedCard(who, "Yellow Alert", toTable: true);
        }
    }

    private bool TryDownloadNamedCard(int owner, string name, bool toTable)
    {
        var draw = owner == 2 ? _oppDrawCards : _drawCards;
        var tent = owner == 2 ? _oppQsTentCards : _qsTentCards;
        var req = new DownloadRules.Request
        {
            Player = owner,
            Source = DownloadRules.Source.DrawDeck,
            Dest = toTable ? DownloadRules.Dest.TableCore : DownloadRules.Dest.Hand,
            NameEquals = name
        };
        var pool = DownloadRules.FilterSource(draw, req);
        if (pool.Count == 0 && IsSideDeckUnlocked("Q's Tent", owner == 2))
            pool = DownloadRules.FilterSource(tent, req);
        if (pool.Count == 0)
        {
            ShowPlayError($"Download {name}: none in draw deck"
                + (IsSideDeckUnlocked("Q's Tent", owner == 2) ? " or Q's Tent." : "."));
            return false;
        }
        Card? taken = pool.Count == 1 ? pool[0]
            : PickCardFromList($"Download {name}", pool, name);
        if (taken == null) return false;
        draw.Remove(taken);
        tent.Remove(taken);
        taken.FaceUp = true;
        if (toTable && EventRules.IsEvent(taken))
        {
            TryResolveEventPlay(taken, owner);
        }
        else
        {
            var hand = owner == 2 ? _oppHandCards : _handCards;
            if (!hand.Contains(taken)) hand.Add(taken);
        }
        ShowCardReveal(taken, "Download",
            $"P{owner} downloads {taken.Name}" + (toTable ? " to table." : " to hand."),
            RevealButtons.Ok, taken.Name);
        RefreshHandStrips();
        RefreshZoneCounts();
        RebuildTablePermanentsPanel();
        _session.Log.Add(_session.TurnNumber, $"P{owner}", $"Download {taken.Name}");
        StatusText.Text = $"Download: {taken.Name}.";
        return true;
    }

    private bool KlimBlocksDraw(int finishingPlayer)
    {
        bool klimOpp = _attachedEvents.Any(e =>
            e.Kind == EventRules.Persist.Klim && e.Owner != finishingPlayer);
        if (!klimOpp) return false;
        bool unique = finishingPlayer == 1 ? _uniquePersonnelPlayedP1 : _uniquePersonnelPlayedP2;
        if (!unique) return false;
        _session.Log.Add(_session.TurnNumber, "sys",
            "Klim Dokachin: opponent loses regular draw (unique personnel played).");
        StatusText.Text = "Klim Dokachin: no end-of-turn draw.";
        return true;
    }

    private void ApplyEdoEndOfTurnPenalties(int finishingPlayer)
    {
        foreach (var kv in _edoContinuePenalty.ToList())
        {
            if (kv.Value != finishingPlayer) continue;
            if (_solvedMissions.Contains(kv.Key)) continue;
            if (finishingPlayer == 1) _scoreP1 -= 10;
            else _scoreP2 -= 10;
            UpdateScoreDisplay();
            _session.Log.Add(_session.TurnNumber, $"P{finishingPlayer}",
                $"Edo Probe: −10 ({(kv.Key.Tag as Card)?.Name} not solved this turn).");
            _edoContinuePenalty.Remove(kv.Key);
        }
    }

    private void ApplyConundrumChase(Border shipOrMission, Card dilemma)
    {
        Border? ship = shipOrMission;
        if (ship.Tag is Card c && !IsShipCard(c))
        {
            ship = GetDockablesUnderMission(shipOrMission)
                .FirstOrDefault(b => b.Tag is Card sc && IsShipCard(sc) && GetBorderOwner(b) == _activePlayer);
        }
        if (ship == null || ship.Tag is not Card shipCard || !IsShipCard(shipCard))
        {
            StatusText.Text = "Conundrum: no ship to chase with.";
            return;
        }
        var oppShips = TableCanvas.Children.OfType<Border>()
            .Where(b => b.Tag is Card oc && IsShipCard(oc) && GetBorderOwner(b) != _activePlayer)
            .ToList();
        if (oppShips.Count == 0)
        {
            StatusText.Text = "Conundrum: no opposing ship on the spaceline.";
            return;
        }
        var target = oppShips.Count == 1
            ? oppShips[0]
            : ShowTargetPickDialog(dilemma, oppShips, "Conundrum: chase which ship?");
        if (target == null) target = oppShips[0];
        _conundrumChase[ship] = target;
        StatusText.Text =
            $"{shipCard.Name} must chase and attack {(target.Tag as Card)?.Name} (normal speed).";
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"Conundrum: {shipCard.Name} chases {(target.Tag as Card)?.Name}");
    }

    private void ApplyFrameOfMind(Card victim, Border mission, Border? ship)
    {
        var skills = MissionRules.ParsePersonnelSkills(victim).Keys.ToList();
        var keep = new List<string>();
        if (skills.Count <= 2)
            keep.AddRange(skills);
        else
        {
            for (int i = 0; i < 2 && skills.Count > 0; i++)
            {
                var pick = PickCardFromList(
                    $"Frame of Mind: opponent chooses skill {i + 1} for {victim.Name}",
                    skills.Select(s => new Card { Name = s, Type = "Skill" }).ToList(),
                    "Choose skill");
                string name = pick?.Name ?? skills[0];
                keep.Add(name);
                skills.RemoveAll(s => s.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }
        victim.FramedOfMind = true;
        victim.FrameSkills = keep;
        ShowCardReveal(victim, "Frame of Mind",
            $"{victim.Name} is Non-Aligned 3-3-3 with only: {string.Join(", ", keep)}.\nCure with 3 Empathy present.",
            RevealButtons.Ok, victim.Name);
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"Frame of Mind on {victim.Name} ({string.Join("/", keep)})");
    }

    private void TryCureFrameOfMindAt(Border host)
    {
        var framed = new List<Card>();
        void CollectFramed(Border? h)
        {
            if (h == null || !_stackOnHost.TryGetValue(h, out var list)) return;
            foreach (var b in list)
            {
                if (b.Tag is not Card person) continue;
                if (person.FramedOfMind)
                    framed.Add(person);
            }
        }
        CollectFramed(host);
        foreach (var dock in GetDockablesUnderMission(host))
            CollectFramed(dock);
        if (framed.Count == 0) return;
        var present = CollectPresentAtMission(host, host.Tag as Card ?? new Card());
        int empathy = present.Where(ModifierRules.IsPersonnelCard)
            .Sum(person => MissionRules.ParsePersonnelSkills(person)
                .Where(kv => kv.Key.Contains("Empathy", StringComparison.OrdinalIgnoreCase))
                .Sum(kv => kv.Value));
        if (empathy < 3) return;
        foreach (var person in framed)
        {
            person.FramedOfMind = false;
            person.FrameSkills = null;
            _session.Log.Add(_session.TurnNumber, "sys",
                $"Frame of Mind cured on {person.Name} (3 Empathy).");
        }
    }

    private void ApplyNamedAuInterrupt(Card card, int controller)
    {
        switch (NamedInterruptRules.Decide(card))
        {
            case NamedInterruptRules.NamedAuOutcome.KevinConvergence:
                ApplyKevinConvergence(controller, card);
                return;
            case NamedInterruptRules.NamedAuOutcome.Countermanda:
                foreach (var e in _attachedEvents.Where(x => x.Kind == EventRules.Persist.Kidnappers).ToList())
                {
                    _attachedEvents.Remove(e);
                    SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
                }
                StatusText.Text = "Countermanda: Telepathic Alien Kidnappers nullified.";
                return;
            case NamedInterruptRules.NamedAuOutcome.DestroyScow:
            {
                var scow = _attachedDilemmas.FirstOrDefault(d => d.Kind == DilemmaRules.PersistKind.Scow);
                if (scow == null)
                {
                    ShowPlayError("No Radioactive Garbage Scow in play.");
                    return;
                }
                var host = scow.Host;
                _attachedDilemmas.Remove(scow);
                SendCardTo(scow.Card, controller, TimingRules.Destination.Discard);
                if (!HasThermalDeflectors() && host != null)
                {
                    foreach (var b in GetPersonnelBordersAtHost(host, opponentOf: 0).ToList())
                    {
                        if (b.Tag is not Card p) continue;
                        // aboard a ship at this location — survive
                        bool onShip = GetDockablesUnderMission(host)
                            .Any(d => IsShipCard(d.Tag as Card ?? new Card())
                                      && _stackOnHost.TryGetValue(d, out var crew) && crew.Contains(b));
                        if (onShip) continue;
                        DiscardPersonnelBorder(b, p, GetBorderOwner(b) == 0 ? 1 : GetBorderOwner(b));
                    }
                }
                if (host != null && !_solvedMissions.Contains(host) && host.Tag is Card mis)
                {
                    AwardDilemmaPoints(-10);
                    StatusText.Text = $"Scow destroyed. Mission {mis.Name} −10 (unsolved). Personnel not aboard ships killed.";
                }
                return;
            }
            case NamedInterruptRules.NamedAuOutcome.SeniorStaffMeeting:
                _seniorStaffArmed = true;
                StatusText.Text = "Senior Staff Meeting: first dilemma of the next space attempt is discarded.";
                return;
            case NamedInterruptRules.NamedAuOutcome.Hail:
                StatusText.Text = "Hail: flying-by ship must stop here, or two ships cannot battle this turn (choose via ship orders).";
                return;
            default:
                return;
        }
    }


    private bool HasMatchingCommander(Border shipBorder, Card ship)
    {
        string shipName = ship.Name ?? "";
        if (string.IsNullOrEmpty(shipName)) return false;
        foreach (var p in GetCrewOnShip(shipBorder))
        {
            string blob = $"{p.Name} {p.Characteristics} {p.Text}";
            if (blob.Contains("Commander", StringComparison.OrdinalIgnoreCase)
                && blob.Contains(shipName, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!string.IsNullOrEmpty(p.Name)
                && (ship.Text ?? "").Contains(p.Name, StringComparison.OrdinalIgnoreCase)
                && (ship.Text ?? "").Contains("Commander", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private bool PlanetBeamBlockedAt(Border host)
    {
        Card? hc = host.Tag as Card;
        Border? mission = hc != null && CardKinds.IsMission(hc) ? host : FindMissionForDockable(host);
        if (mission?.Tag is not Card mc || !MissionRules.IsPlanetMission(mc))
            return false;
        return _attachedEvents.Any(e =>
            e.Kind == EventRules.Persist.ParticleScatter
            && e.Host != null
            && (ReferenceEquals(e.Host, host)
                || ReferenceEquals(FindMissionForDockable(e.Host), mission)));
    }

    private void TryNullifyBaryonBuildup(int turnPlayer)
    {
        foreach (var e in _attachedEvents.Where(x => x.Kind == EventRules.Persist.Baryon && x.Owner == turnPlayer).ToList())
        {
            if (e.Host == null) continue;
            bool empty = GetCrewOnShip(e.Host).Count == 0;
            var mission = FindMissionForDockable(e.Host);
            bool dockedOwn = mission != null && GetDockablesUnderMission(mission).Any(b =>
                b.Tag is Card f
                && ((f.Type ?? "").Contains("facility", StringComparison.OrdinalIgnoreCase)
                    || (f.Type ?? "").Contains("outpost", StringComparison.OrdinalIgnoreCase))
                && GetBorderOwner(b) == turnPlayer);
            if (!empty || !dockedOwn) continue;
            _attachedEvents.Remove(e);
            SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
            _session.Log.Add(_session.TurnNumber, $"P{turnPlayer}",
                $"Baryon Buildup nullified ({(e.Host.Tag as Card)?.Name} empty at own facility).");
            UpdateHostBadge(e.Host);
        }
    }

    private void ApplyKevinConvergence(int controller, Card card)
    {
        var locs = _spacelineOrder.ToList();
        if (locs.Count == 0)
        {
            ShowPlayError("Kevin Uxbridge: Convergence needs a spaceline location.");
            return;
        }
        var loc = locs.Count == 1 ? locs[0] : ShowTargetPickDialog(card, locs, "Destroy events at which location?");
        if (loc == null) loc = locs[0];
        int n = 0;
        foreach (var e in _attachedEvents.Where(ae =>
                     EventRules.KevinEventAtLocation(
                         ReferenceEquals(ae.Host, loc),
                         ReferenceEquals(ae.Host2, loc),
                         ae.Host != null && FindMissionForDockable(ae.Host) == loc)).ToList())
        {
            _attachedEvents.Remove(e);
            SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
            n++;
        }
        StatusText.Text = $"Kevin Uxbridge: Convergence destroyed {n} event(s) at {(loc.Tag as Card)?.Name}.";
        _session.Log.Add(_session.TurnNumber, $"P{controller}",
            $"Kevin Convergence @ {(loc.Tag as Card)?.Name}: {n} events");
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
    /// Visual card picker in the card-detail overlay (horizontal scroll strip).
    /// Click a mini to choose. Close / Esc / click outside = cancel (null).
    /// </summary>
    private Card? PickCardFromList(string prompt, IReadOnlyList<Card> pool, string title, Card? source = null)
    {
        if (pool == null || pool.Count == 0) return null;
        if (pool.Count == 1) return pool[0];
        if (CardDetailOverlay == null || DetailStackCards == null)
            return pool[0];

        _detailPickMode = true;
        _detailPickResult = null;
        _detailHost = null;

        ShowCardDetail(source ?? pool[0]);
        if (DetailName != null && source == null)
            DetailName.Text = title;
        if (DetailText != null && source == null)
            DetailText.Text = prompt;

        FillDetailPickStrip(pool, title, prompt);
        if (BtnDetailBack != null) BtnDetailBack.Visibility = Visibility.Collapsed;
        if (BtnDetailBeamSelect != null) BtnDetailBeamSelect.Visibility = Visibility.Collapsed;
        CardDetailOverlay.Visibility = Visibility.Visible;

        _detailPickFrame = new System.Windows.Threading.DispatcherFrame();
        try { System.Windows.Threading.Dispatcher.PushFrame(_detailPickFrame); }
        finally
        {
            _detailPickFrame = null;
            _detailPickMode = false;
            if (CardDetailOverlay != null)
                CardDetailOverlay.Visibility = Visibility.Collapsed;
            if (DetailStackCards != null)
                DetailStackCards.Children.Clear();
            if (DetailStackSection != null)
                DetailStackSection.Visibility = Visibility.Collapsed;
        }

        return _detailPickResult;
    }

    private Border? PickBorderFromList(Card? source, IReadOnlyList<Border> candidates, string title)
    {
        if (candidates == null || candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];
        var cards = candidates
            .Select(b => b.Tag as Card)
            .Where(c => c != null)
            .Cast<Card>()
            .ToList();
        var pick = PickCardFromList("Click a card to choose.", cards, title, source);
        if (pick == null) return null;
        return candidates.FirstOrDefault(b => ReferenceEquals(b.Tag, pick));
    }

    /// <summary>
    /// Player OR-choice (A or B or C). Reuses the detail-strip picker.
    /// Returns the chosen label, or null if cancelled.
    /// </summary>
    private string? PickOption(string title, string prompt, params string[] options)
    {
        if (options == null || options.Length == 0) return null;
        return AskChoice(null, title, prompt, options);
    }

    private void FillDetailPickStrip(IReadOnlyList<Card> pool, string title, string prompt)
    {
        if (DetailStackSection == null || DetailStackCards == null) return;
        DetailStackSection.Visibility = Visibility.Visible;
        if (DetailStackTitle != null)
            DetailStackTitle.Text = title;
        if (DetailStackStats != null)
            DetailStackStats.Text = prompt + "\nClick a card in the strip below.";
        DetailStackCards.Children.Clear();
        foreach (var c in pool)
        {
            var mini = CreateMiniCard(c, faceDown: false);
            mini.Width = 72;
            mini.Height = 100;
            mini.Margin = new Thickness(3);
            mini.Cursor = Cursors.Hand;
            mini.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 200, 120));
            mini.BorderThickness = new Thickness(2);
            mini.ToolTip = $"{c.Name}\nClick = choose";
            Card cRef = c;
            mini.MouseLeftButtonDown += (_, ev) =>
            {
                CompleteDetailPick(cRef);
                ev.Handled = true;
            };
            DetailStackCards.Children.Add(mini);
        }
    }

    private void CompleteDetailPick(Card? card)
    {
        if (!_detailPickMode) return;
        _detailPickResult = card;
        if (_detailPickFrame != null)
            _detailPickFrame.Continue = false;
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
        bool stasis = DilemmaRules.IsStasisPersist(r.Persist);
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

        if (r.Relocate != null
            && r.Persist != DilemmaRules.PersistKind.Abduction
            && r.Persist != DilemmaRules.PersistKind.FrameOfMind)
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

        bool removeFromSeed = DilemmaRules.ShouldRemoveFromSeed(r.Fate);

        if (removeFromSeed && seedStack.Count > 0)
        {
            // Track overcome/removed seeds for Temporal Causality Loop re-seed
            if (_attemptMission != null && ReferenceEquals(_attemptMission, missionBorder)
                && DilemmaRules.ShouldTrackOvercomeDiscard(
                    r.Fate, EventRules.IsTemporalCausalityLoop(seedCard)))
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
        if (DilemmaRules.ShouldRestoreTemporalLoop(
                EventRules.IsTemporalCausalityLoop(seedCard), r.Fate))
        {
            ApplyTemporalCausalityLoopRestore(missionBorder, seedCard);
        }

        if (r.Fate == DilemmaRules.Fate.AttachAndEnd)
        {
            Border host = missionBorder;
            switch (DilemmaRules.DecideAttachHost(r.Persist))
            {
                case DilemmaRules.AttachHostPreference.FurthestMission:
                    host = FindFurthestMission(missionBorder) ?? missionBorder;
                    // One-way trip: from furthest end back toward the encounter end, then off the spaceline
                    int farIdx = _spacelineOrder.IndexOf(host);
                    int nearIdx = _spacelineOrder.IndexOf(missionBorder);
                    if (farIdx < 0) farIdx = _spacelineOrder.Count - 1;
                    if (nearIdx < 0) nearIdx = 0;
                    _borgShipDir = farIdx >= nearIdx ? -1 : 1;
                    break;
                case DilemmaRules.AttachHostPreference.ShipOrMission:
                    host = shipBorder ?? missionBorder;
                    break;
                default:
                    host = missionBorder;
                    break;
            }
            _attachedDilemmas.Add(new AttachedDilemma
            {
                Card = seedCard,
                Kind = r.Persist,
                Countdown = r.Countdown,
                Host = host,
                Extra = r.Relocate,
                Dest = r.Persist == DilemmaRules.PersistKind.Cytherians
                    ? ResolveFarEndMission(host)
                    : null
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

        if (DilemmaRules.ShouldAwardScoreOnApply(r.Score, r.Fate))
            AwardDilemmaPoints(r.Score);

        if (DilemmaRules.IsEdoContinuePenalty(seedCard.Name, r.Fate))
        {
            _edoContinuePenalty[missionBorder] = _activePlayer;
            _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                "Edo Probe: −10 if this mission is not solved this turn.");
        }

        if (DilemmaRules.IsConundrumChase(seedCard.Name, r.Fate))
            ApplyConundrumChase(shipBorder ?? missionBorder, seedCard);

        if (r.Persist == DilemmaRules.PersistKind.FrameOfMind && r.Relocate != null)
            ApplyFrameOfMind(r.Relocate, missionBorder, shipBorder);
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
            if (mb.Tag is not Card) continue;
            if (_solvedMissions.Contains(mb)) continue;
            var mission = MissionPrintedFor(mb, _activePlayer);
            int dil = _seedUnderMission.TryGetValue(mb, out var seeds) ? seeds.Count : 0;
            int missionOwner = LocationIsYourMission(mb, _activePlayer)
                ? _activePlayer
                : GetBorderOwner(mb);
            var check = MissionRules.CanSolve(mission, teamList, dilemmasRemaining: 0,
                attemptingPlayer: _activePlayer, missionOwner: missionOwner,
                extraMissionIcons: EspionageIconsOn(mb, _activePlayer));
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
        var beamAuth = AuthorizePlay(GameAction.Beam(_activePlayer, hostCard));
        if (!beamAuth.Ok)
        {
            ShowPlayError(beamAuth.Message);
            return;
        }
        if (PlanetBeamBlockedAt(hostBorder))
        {
            ShowPlayError("Particle Scattering Field: no beaming to or from a planet here.");
            return;
        }
        if (ShipHasRequiredMove(hostBorder))
        {
            ShowPlayError("Incoming Message: crew may not leave the ship (7.10). Reporting aboard is allowed.");
            return;
        }
        _actionSourceHost = hostBorder;
        ClearTargetHighlights();

        bool sourceIsMission = CardKinds.IsMission(hostCard);
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
            // Planet surface only — 7.1.1.0.1 no beaming into space
            if (!sourceIsMission && mission.Tag is Card mc && MissionRules.IsPlanetMission(mc))
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
        var flyAuth = AuthorizePlay(GameAction.Fly(_activePlayer, ship));
        if (!flyAuth.Ok)
        {
            ShowPlayError(flyAuth.Message);
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
        var boardLine = FlyBoardLine(fromMission);
        int fromL = BoardIndexOf(boardLine, fromMission);
        if (fromL < 0)
        {
            ShowPlayError("Schiff-Location steht nicht auf dem Board.");
            return;
        }
        bool wnohgb = WnohgbRules.WrapAllowed(PlayerHasWnohgb(_activePlayer), boardLine.Count);
        int marked = 0;
        for (int i = 0; i < boardLine.Count; i++)
        {
            if (i == fromL) continue;
            var move = MovementRules.CanMoveShip(ship, crew, remain, boardLine, fromL, i,
                GetActiveTreaties(_activePlayer), wrapEnds: wnohgb,
                skipStaffing: ShipStaffedByRogueBorg(shipBorder));
            if (!move.Ok) continue;
            var face = FaceForLocation(boardLine[i]);
            if (face == null) continue;
            AddTargetHighlight(face, Color.FromArgb(90, 80, 160, 255));
            marked++;
        }

        DebugLog.Move(_session.TurnNumber, _activePlayer,
            $"fly-mark {DebugLog.Card(ship)} from[{fromL}]={boardLine[fromL].Label} remain={remain} marked={marked}/{boardLine.Count}");
        StatusText.Text = marked == 0
            ? $"No location in RANGE (left {remain})."
            : $"FLY: {marked} location(s) marked (RANGE {remain})"
              + (wnohgb ? " · WNOHGB: shorter wrap counts." : "")
              + ". Click a mission or Gaps.";
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
            if (clicked.Tag is not Card destCard || !IsLandableLocation(destCard))
                return false;
            if (_actionSourceHost.Tag is not Card ship) return false;
            var from = FindMissionForDockable(_actionSourceHost);
            if (!TryMoveShipWithRules(_actionSourceHost, ship, from, clicked))
                return true;
            RelocateShipAlongSpaceline(_actionSourceHost, from, clicked);
            SyncBoardFromTable();
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
        if (ShipHasRequiredMove(shipBorder))
        {
            ShowPlayError("Incoming Message: ship may not initiate battle (7.10). Return fire is allowed.");
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
        if (!BattleRules.HasLeader(crew) && !ShipStaffedByRogueBorg(shipBorder))
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
            GetHullDamage(attackerBorder), IsBorderStopped(attackerBorder),
            _wartimeVsAffiliation, ShipStaffedByRogueBorg(attackerBorder));

        if (check.Ok && ReportingRules.GetAffiliations(targetCard).Contains("FED"))
        {
            var atkAff = ReportingRules.GetAffiliations(attackerShip).FirstOrDefault();
            if (!string.IsNullOrEmpty(atkAff))
                _wartimeVsAffiliation = atkAff;
        }
        if (_conundrumChase.TryGetValue(attackerBorder, out var chaseT)
            && ReferenceEquals(chaseT, targetBorder))
            _conundrumChase.Remove(attackerBorder);

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
            logLines.Add($"DESTROYED: {defenderCard.Name} (P{defOwner}) — Escape Pod may respond.");
        if (atkDestroyed)
            logLines.Add($"DESTROYED: {attackerShip.Name} (P{atkOwner}) — Escape Pod may respond.");

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

        ShowCardReveal(defenderCard, "Ship Battle", summary, RevealButtons.Ok, attackerShip.Name);

        if (defDestroyed)
            DestroyShipOrFacility(defenderBorder, defenderCard, defOwner);
        if (atkDestroyed)
            DestroyShipOrFacility(attackerBorder, attackerShip, atkOwner);
    }

    private void ApplyHullDamage(Border border, Card card, int hullPercent)
    {
        hullPercent = Math.Clamp(hullPercent, 0, 100);
        SetHullDamagePercent(border, hullPercent);
        if (card.InstanceId > 0)
            DebugLog.Move(_session.TurnNumber, GetBorderOwner(border),
                $"hull #{card.InstanceId} pct={hullPercent} source=instance");

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
                    SetShipRangeLeft(border, card, eff);
                else if (!_shipRangeLeft.ContainsKey(border))
                    SetShipRangeLeft(border, card, eff);
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
        if (_hughBlocksBorgShipAttack)
        {
            _hughBlocksBorgShipAttack = false;
            hit.Add("attack cancelled by Hugh");
        }
        else
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

    private AttachedEvent? IncomingMessageOn(Border ship) =>
        _attachedEvents.FirstOrDefault(e =>
            e.Kind == EventRules.Persist.IncomingMessage && SameHostShip(e.Host, ship));

    private bool ShipHasRequiredMove(Border ship) => CollectRequiredMoveDests(ship).Count > 0;

    private string? IncomingMessageMoveCheck(Border ship, Border? fromMission, Border toMission,
        int fromIdx, int toIdx)
    {
        var dest = RequiredMoveDestination(ship);
        if (dest == null) return null;
        // E6: hop geometry on BoardStore Locations (same line / Span as Fly); paint list unused here.
        var line = FlyBoardLine(fromMission ?? dest);
        if (line.Count == 0) return "Required move: no spaceline.";
        int fromL = BoardIndexOf(line, fromMission ?? FindMissionForDockable(ship));
        int toL = BoardIndexOf(line, toMission);
        int destL = BoardIndexOf(line, dest);
        if (fromL < 0 || toL < 0 || destL < 0)
            return "Required move: destination is not on this spaceline (same quadrant).";
        int mover = GetBorderOwner(ship);
        if (mover == 0) mover = _activePlayer;
        bool wrap = WnohgbWrapFor(mover);
        int remain = ship.Tag is Card sc ? GetRemainingRange(ship, sc) : 0;
        var hop = RequiredMoveRules.NextAffordable(fromL, destL, line, wrap, remain);
        bool useWrap = wrap && hop is { Wrap: true };
        if (toL == destL) return null;
        if (hop != null && toL == hop.Value.Next) return null;
        if (RequiredMoveRules.IsToward(fromL, toL, destL, line.Count, useWrap))
            return null;
        return "Required move: take the shortest path you can pay for (WNOHGB wrap if shorter and in RANGE).";
    }

    private List<Location> FlyBoardLine(Border? piece)
    {
        SyncBoardFromTable(logDual: false);
        string q = piece != null ? LocationQuadrant(piece) : "Alpha";
        return BoardStore.Current.Spaceline.Locations
            .Where(l => l.Kind is LocationKind.Mission or LocationKind.Span or LocationKind.TimeLocation
                        && string.Equals(l.Quadrant ?? "Alpha", q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private int BoardIndexOf(IReadOnlyList<Location> line, Border? piece)
    {
        if (piece?.Tag is not Card card) return -1;
        for (int i = 0; i < line.Count; i++)
        {
            var p = line[i].Printed;
            if (p == null) continue;
            if (ReferenceEquals(p, card)) return i;
            if (p.InstanceId > 0 && p.InstanceId == card.InstanceId) return i;
        }
        return -1;
    }

    private Border? FaceForLocation(Location loc)
    {
        if (loc.Printed == null) return null;
        var face = FindBorderForCard(loc.Printed);
        if (face != null) return face;
        return _spacelineOrder.FirstOrDefault(b =>
            b.Tag is Card c && loc.Printed.InstanceId > 0 && c.InstanceId == loc.Printed.InstanceId);
    }

    /// <summary>E6: View/paint only (glow, far-end pick). IM/Required-Move hops use FlyBoardLine.</summary>
    private List<Border> MissionsOnSameSpaceline(Border? piece)
    {
        string q = piece != null ? LocationQuadrant(piece) : "Alpha";
        return _spacelineOrder
            .Where(b => b.Tag is Card c && IsLandableLocation(c)
                        && string.Equals(
                            IsMissionCard(c) ? GetNativeQuadrant(c) : GetSpacelineQuadrant(b),
                            q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private bool WnohgbWrap => PlayerHasWnohgb(_activePlayer);

    private bool WnohgbWrapFor(int player) => PlayerHasWnohgb(player);

    private int SpanEntering(Border mission)
    {
        if (mission.Tag is not Card c) return 1;
        int mo = GetBorderOwner(mission);
        bool own = mo == 0 || mo == _activePlayer;
        return MovementRules.GetMissionSpan(c, own);
    }

    /// <summary>E6: hop cost from BoardStore Location.Span (same as Fly CanMoveShip).</summary>
    private static int SpanEntering(Location loc) => Math.Max(0, loc.Span);

    private Border? ResolveFarEndMission(Border fromPiece)
    {
        var line = MissionsOnSameSpaceline(fromPiece);
        if (line.Count == 0) return FindMissionForDockable(fromPiece);
        var here = FindMissionForDockable(fromPiece) ?? fromPiece;
        int from = line.IndexOf(here);
        if (from < 0) from = 0;
        int far = RequiredMoveRules.FarEndIndex(from, line.Count, i => SpanEntering(line[i]));
        if (far < 0)
        {
            int opp = _activePlayer == 1 ? 2 : 1;
            string pick = AskChoice(null, "Far end of spaceline",
                "Tie (12.6). Opponent chooses the far end.",
                (line[0].Tag as Card)?.Name ?? "Left",
                (line[^1].Tag as Card)?.Name ?? "Right");
            far = pick != null && pick == ((line[^1].Tag as Card)?.Name ?? "Right") ? line.Count - 1 : 0;
        }
        return line[Math.Clamp(far, 0, line.Count - 1)];
    }

    private List<(string Label, Border Dest)> CollectRequiredMoveDests(Border ship)
    {
        var list = new List<(string, Border)>();
        var im = IncomingMessageOn(ship);
        var imD = im != null ? IncomingMessageDestMission(im) : null;
        if (imD != null) list.Add((im!.Card.Name ?? "Incoming Message", imD));

        foreach (var d in _attachedDilemmas.Where(x =>
                     x.Kind == DilemmaRules.PersistKind.Cytherians && SameHostShip(x.Host, ship)))
        {
            var dest = d.Dest ?? ResolveFarEndMission(ship);
            d.Dest = dest;
            if (dest != null) list.Add(("Cytherians", dest));
        }

        if (_conundrumChase.TryGetValue(ship, out var prey) && prey != null)
        {
            var dest = FindMissionForDockable(prey);
            if (dest != null) list.Add(("Conundrum", dest));
        }
        return list;
    }

    private Border? RequiredMoveDestination(Border ship)
    {
        var opts = CollectRequiredMoveDests(ship);
        if (opts.Count == 0) return null;
        if (opts.Count == 1) return opts[0].Dest;
        string picked = AskChoice(null, "Required actions (7.10)",
            "Several required actions apply. Resolve them in any order — pick one destination.",
            opts.Select(o => o.Label + " → " + ((o.Dest.Tag as Card)?.Name ?? "?")).ToArray());
        if (string.IsNullOrEmpty(picked)) return opts[0].Dest;
        var hit = opts.FirstOrDefault(o => picked.StartsWith(o.Label, StringComparison.OrdinalIgnoreCase));
        return hit.Dest ?? opts[0].Dest;
    }

    private Border? IncomingMessageDestMission(AttachedEvent im)
    {
        if (im.Host2 == null) return null;
        if (im.Host2.Tag is Card c && IsMissionCard(c)) return im.Host2;
        return FindMissionForDockable(im.Host2);
    }

    private void ProcessIncomingMessageMoves(int player)
    {
        var ships = TableCanvas.Children.OfType<Border>()
            .Where(b => b.Tag is Card c && IsShipCard(c) && CollectRequiredMoveDests(b).Count > 0)
            .ToList();
        foreach (var shipB in ships)
        {
            if (shipB.Tag is not Card ship) continue;
            int o = GetBorderOwner(shipB);
            if (o == 0) o = 1;
            if (o != player) continue;

            if (IsShipDocked(shipB))
            {
                SetShipDockedAt(shipB, null);
                _session.Log.Add(_session.TurnNumber, $"P{player}",
                    $"{ship.Name} undocks (required move 7.10)");
            }

            int guard = 0;
            while (guard++ < 20 && GetRemainingRange(shipB, ship) > 0)
            {
                var from = FindMissionForDockable(shipB);
                var dest = RequiredMoveDestination(shipB);
                if (from == null || dest == null)
                {
                    _session.Log.Add(_session.TurnNumber, "sys",
                        $"IM {ship.Name}: no from/dest (from={(from?.Tag as Card)?.Name ?? "—"})");
                    break;
                }
                if (ReferenceEquals(from, dest))
                {
                    ResolveRequiredArrival(shipB, dest);
                    break;
                }
                // E6: Required-Move hops on Locations (Span like Fly); UI face via FaceForLocation.
                var line = FlyBoardLine(from);
                int fromL = BoardIndexOf(line, from);
                int destL = BoardIndexOf(line, dest);
                if (fromL < 0 || destL < 0)
                {
                    _session.Log.Add(_session.TurnNumber, "sys",
                        $"IM {ship.Name}: dest not on this spaceline");
                    break;
                }
                var hop = RequiredMoveRules.NextAffordable(fromL, destL, line, WnohgbWrapFor(o),
                    GetRemainingRange(shipB, ship));
                if (hop == null) break;
                var stepLoc = line[hop.Value.Next];
                var step = FaceForLocation(stepLoc);
                if (step == null)
                {
                    _session.Log.Add(_session.TurnNumber, "sys",
                        $"IM {ship.Name}: no UI face for {stepLoc.Label}");
                    break;
                }
                if (!TryMoveShipWithRules(shipB, ship, from, step))
                {
                    _session.Log.Add(_session.TurnNumber, "sys",
                        $"IM hop failed {ship.Name} → {(step.Tag as Card)?.Name}: {StatusText.Text}");
                    break;
                }
                RelocateShipAlongSpaceline(shipB, from, step);
            }
        }
    }

    private void ResolveRequiredArrival(Border ship, Border dest)
    {
        CheckIncomingMessageArrival(ship);
        foreach (var d in _attachedDilemmas.Where(x =>
                     x.Kind == DilemmaRules.PersistKind.Cytherians && SameHostShip(x.Host, ship)).ToList())
        {
            if (d.Dest != null && !ReferenceEquals(d.Dest, dest) && !ReferenceEquals(FindMissionForDockable(ship), d.Dest))
                continue;
            int owner = GetBorderOwner(ship);
            if (owner == 0) owner = 1;
            if (owner == 1) _scoreP1 += 15; else _scoreP2 += 15;
            UpdateScoreDisplay();
            _attachedDilemmas.Remove(d);
            SendCardTo(d.Card, owner, TimingRules.Destination.Discard);
            StatusText.Text = $"Cytherians completed at {(dest.Tag as Card)?.Name}: +15.";
            _session.Log.Add(_session.TurnNumber, $"P{owner}", "Cytherians +15");
        }
        if (_conundrumChase.TryGetValue(ship, out var prey)
            && FindMissionForDockable(prey) != null
            && ReferenceEquals(FindMissionForDockable(ship), FindMissionForDockable(prey)))
        {
            StatusText.Text = "Conundrum: same location as prey — initiate attack (7.10).";
        }
    }

    private void CheckIncomingMessageArrival(Border ship)
    {
        var im = IncomingMessageOn(ship);
        if (im == null) return;
        var here = FindMissionForDockable(ship);
        var dest = IncomingMessageDestMission(im);
        if (here != null && dest != null && ReferenceEquals(here, dest))
            ResolveIncomingMessageArrival(ship, "arrived at the facility");
    }

    private string LocationQuadrant(Border? piece)
    {
        var m = piece != null && piece.Tag is Card c && IsMissionCard(c)
            ? piece
            : piece != null ? FindMissionForDockable(piece) : null;
        return m != null ? GetSpacelineQuadrant(m) : "Alpha";
    }

    private List<Border> CollectIncomingMessageFacilities(Border ship, int shipController, string needAffil)
    {
        string line = LocationQuadrant(ship);
        var list = new List<Border>();
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Tag is not Card fc || !IsFacilityCard(fc)) continue;
            int o = GetBorderOwner(b);
            if (o == 0) o = 1;
            bool sameQ = string.Equals(LocationQuadrant(b), line, StringComparison.OrdinalIgnoreCase);
            if (!TargetQuery.CanImFacility(fc, o, shipController, needAffil, sameQ).ok)
                continue;
            list.Add(b);
        }
        return list;
    }

    private void ApplyIncomingMessage(Card card, int playedBy)
    {
        var host = _interruptTargetHost;
        bool hostIsShip = host != null && host.Tag is Card shipCard && IsShipCard(shipCard);
        Card? ship = hostIsShip ? (Card)host!.Tag! : null;

        string? need = InterruptRules.IncomingMessageAffiliation(card)
                       ?? PlayOnRules.Parse(card).Affiliation;
        bool affiliationOk = !hostIsShip
            || string.IsNullOrEmpty(need)
            || CardMatchesAffiliation(ship!, need);

        int shipCtrl = 0;
        List<Border> facilities = new();
        if (hostIsShip && affiliationOk)
        {
            shipCtrl = GetBorderOwner(host!);
            if (shipCtrl == 0) shipCtrl = 1;
            facilities = CollectIncomingMessageFacilities(host!, shipCtrl, need ?? "");
        }

        var early = IncomingMessageRules.EarlyReject(hostIsShip, affiliationOk, facilities.Count);
        if (early == IncomingMessageRules.ImApplyOutcome.NeedShipHost)
        {
            ShowPlayError($"{card.Name}: drop on a matching ship.");
            var hand = playedBy == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(card)) hand.Add(card);
            return;
        }
        if (early == IncomingMessageRules.ImApplyOutcome.AffiliationMismatch)
        {
            ShowPlayError($"{card.Name}: target ship is not {need}.");
            var hand = playedBy == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(card)) hand.Add(card);
            return;
        }
        if (early == IncomingMessageRules.ImApplyOutcome.NullifyNoFacility)
        {
            ShowCardReveal(card, card.Name,
                "No matching facility on this spaceline (same quadrant). Interrupt is nullified.",
                RevealButtons.Ok, card.Name);
            SendCardTo(card, playedBy, TimingRules.Destination.Discard);
            return;
        }

        ClearEventTargetHighlights();
        foreach (var f in facilities)
            AddTargetHalo(f, Color.FromRgb(80, 200, 220));

        Border dest = facilities[0];
        if (facilities.Count > 1)
        {
            var pick = PickBorderFromList(card, facilities,
                $"P{shipCtrl}: choose your {need} facility on this spaceline.");
            if (pick != null) dest = pick;
        }
        ClearEventTargetHighlights();

        var mini = CreateFloatingCard(card);
        mini.Visibility = Visibility.Collapsed;
        if (!TableCanvas.Children.Contains(mini))
            TableCanvas.Children.Add(mini);
        AddCardToHostStack(host!, mini);

        _attachedEvents.Add(new AttachedEvent
        {
            Card = card,
            Kind = EventRules.Persist.IncomingMessage,
            Owner = playedBy,
            Host = host,
            Host2 = dest
        });

        string facName = (dest.Tag as Card)?.Name ?? "facility";
        ShowCardReveal(card, card.Name,
            $"{ship!.Name} must do nothing but move toward {facName} on this spaceline.\n"
            + "Nullified on arrival. Crew may not leave or initiate battle. Return fire allowed.",
            RevealButtons.Ok, ship.Name);
        StatusText.Text = $"{card.Name} on {ship.Name} → {facName}.";
        _session.Log.Add(_session.TurnNumber, $"P{playedBy}",
            $"{card.Name} on {ship.Name} → {facName}");
        UpdateHostBadge(host!);

        // Attach first, then arrival check (do not change FindMissionForDockable — parked false-already-at).
        var here = FindMissionForDockable(host!);
        var there = FindMissionForDockable(dest) ?? dest;
        if (IncomingMessageRules.IsAlreadyAtFacility(
                here != null,
                ReferenceEquals(here, there)))
            ResolveIncomingMessageArrival(host!, "already at the facility's location");
        else if (shipCtrl == _activePlayer && !_seedPhaseActive)
            ProcessIncomingMessageMoves(shipCtrl);
    }

    private void ResolveIncomingMessageArrival(Border ship, string why)
    {
        foreach (var ae in _attachedEvents.Where(e =>
                     e.Kind == EventRules.Persist.IncomingMessage && SameHostShip(e.Host, ship)).ToList())
        {
            _attachedEvents.Remove(ae);
            if (ae.Host != null)
            {
                var stacked = FindBorderForCard(ae.Card);
                if (stacked != null)
                {
                    RemoveCardFromHostStack(ae.Host, stacked);
                    stacked.Visibility = Visibility.Collapsed;
                    if (TableCanvas.Children.Contains(stacked))
                        TableCanvas.Children.Remove(stacked);
                }
            }
            var leftover = FindBorderForCard(ae.Card);
            if (leftover != null && TableCanvas.Children.Contains(leftover))
                TableCanvas.Children.Remove(leftover);
            SendCardTo(ae.Card, ae.Owner, TimingRules.Destination.Discard);
            StatusText.Text = $"{ae.Card.Name} nullified ({why}).";
            _session.Log.Add(_session.TurnNumber, "sys", $"{ae.Card.Name} nullified — {why}");
            if (ae.Host != null) UpdateHostBadge(ae.Host);
        }
    }

    private void ApplySubspaceInterference(Card card, int controller, Card? target)
    {
        Card? hit = target;
        if (hit == null || !InterruptRules.IsIncomingMessage(hit))
        {
            var pool = _attachedEvents
                .Where(e => InterruptRules.IsIncomingMessage(e.Card)
                            || (e.Card.Name ?? "").Equals("Hail", StringComparison.OrdinalIgnoreCase)
                            || (e.Card.Name ?? "").Equals("Subspace Schism", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Card)
                .Distinct()
                .ToList();
            if (pool.Count == 0)
            {
                ShowPlayError("Subspace Interference: no Incoming Message / Hail / Subspace Schism in play.");
                return;
            }
            hit = PickCardFromList("Nullify which card?", pool, card.Name, card);
        }
        if (hit == null) return;
        var ae = _attachedEvents.FirstOrDefault(e => ReferenceEquals(e.Card, hit));
        if (ae?.Host != null)
            ResolveIncomingMessageArrival(ae.Host, "Subspace Interference");
        else
        {
            _attachedEvents.RemoveAll(e => ReferenceEquals(e.Card, hit));
            SendCardTo(hit, ae?.Owner ?? controller, TimingRules.Destination.Discard);
        }
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
                                 && SameHostShip(e.Host, ship));

    private bool ShipStaffedByRogueBorg(Border ship) =>
        CountRogueBorgOn(ship) > 0 && ShipHasLoreReturns(ship);

    private bool HostHasCrosis(Border host) =>
        _crosisShips.Contains(host)
        || _attachedEvents.Any(e => ReferenceEquals(e.Host, host)
                                    && InterruptRules.IsCrosis(e.Card));

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
        bool hostIsShip = host.Tag is Card shipProbe && IsShipCard(shipProbe);
        int ho = GetBorderOwner(host);
        if (ho == 0) ho = 1;
        var deny = EventRules.LoreReturnsDenyReason(
            hostIsShip,
            hostIsShip && ho != controller,
            CountRogueBorgOn(host) > 0,
            HostHasPersonnelOf(host, 0));
        if (deny != null)
        {
            ShowPlayError(deny);
            return false;
        }
        var ship = (Card)host.Tag!;

        foreach (var rb in RogueBorgUnitsOn(host))
        {
            rb.Controller = controller;
            if (rb.Visual != null)
                SetBorderOwner(rb.Visual, controller);
        }
        SetBorderOwner(host, controller);
        ship.Controller = controller;
        ship.CurrentAffiliation = "NA";
        var at = FindMissionForDockable(host);
        if (at != null)
            RelayoutDockablesUnderMission(at);

        ShowCardReveal(ev, "Lore Returns",
            $"You control the Rogue Borg aboard {ship.Name}.\n"
            + "Ship commandeered (Non-Aligned). While Rogue Borg aboard, it is staffed "
            + "and may initiate battle / beam your Rogue Borg.",
            RevealButtons.Ok, ship.Name);
        StatusText.Text = $"Lore Returns: P{controller} commandeers {ship.Name} with Rogue Borg.";
        _session.Log.Add(_session.TurnNumber, $"P{controller}",
            $"Lore Returns commandeers {ship.Name} (was P{ship.OwnerPlayer})");
        UpdateHostBadge(host);
        return true;
    }

    /// <summary>Kevin/Devil nullify Lore Returns: printed owner resumes control even if Rogue Borg remain.</summary>
    private void RevertLoreReturnsControl(Border host)
    {
        if (host.Tag is not Card ship || !IsShipCard(ship)) return;
        int orig = ship.OwnerPlayer is 1 or 2 ? ship.OwnerPlayer : GetBorderOwner(host);
        if (orig is not (1 or 2)) orig = 1;
        SetBorderOwner(host, orig);
        ship.Controller = orig;
        ship.CurrentAffiliation = "";
        foreach (var rb in RogueBorgUnitsOn(host))
        {
            rb.Controller = 0;
            if (rb.Visual != null)
                SetBorderOwner(rb.Visual, 0);
        }
        var at = FindMissionForDockable(host);
        if (at != null)
            RelayoutDockablesUnderMission(at);
        _session.Log.Add(_session.TurnNumber, "sys",
            $"Lore Returns ends: {ship.Name} returns to P{orig}");
    }

    private void NoteAuPlay(Card? card, int player)
    {
        if (card == null || player is not (1 or 2)) return;
        if (!CardIcons.HasAlternateUniverse(card)
            && !(card.Icons ?? "").Contains("[AU]", StringComparison.OrdinalIgnoreCase))
            return;
        if (player == 1) _auPlayedThisTurnP1 = true;
        else _auPlayedThisTurnP2 = true;
    }

    private bool OpponentPlayedAuThisTurn(int me) =>
        me == 1 ? _auPlayedThisTurnP2 : _auPlayedThisTurnP1;

    private static bool IsNonAlignedShip(Card c)
    {
        var tokens = MissionRules.ParseAffiliationTokens(c.Affiliation);
        if (tokens.Contains("NA") || tokens.Contains("NON") || tokens.Contains("NON-ALIGNED"))
            return true;
        string blob = $"{c.Affiliation} {c.Icons} {c.Characteristics}";
        return blob.Contains("[Non]", StringComparison.OrdinalIgnoreCase)
               || blob.Contains("Non-Aligned", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShipHasCloakingDevice(Card ship)
    {
        string t = $"{ship.Text} {ship.Staff} {ship.Characteristics}";
        return t.Contains("Cloaking Device", StringComparison.OrdinalIgnoreCase)
               || t.Contains("[Cloak]", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsShipCloaked(Border ship)
    {
        if (ship.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst)
            && inst is ShipInstance sh)
            return sh.Cloaked || _cloakedShips.Contains(ship);
        return _cloakedShips.Contains(ship);
    }

    /// <summary>Glossary: exposed = in play and not cloaked (landed/phased later).</summary>
    private bool IsShipExposed(Border ship) => !IsShipCloaked(ship);

    private bool IsShipDocked(Border ship)
    {
        if (ship.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst)
            && inst is ShipInstance sh
            && sh.DockedAtId > 0)
            return true;
        return _dockedAt.ContainsKey(ship);
    }

    private IEnumerable<Border> FacilitiesHereForDock(Border ship)
    {
        var here = FindMissionForDockable(ship);
        if (here == null) yield break;
        int owner = GetBorderOwner(ship);
        if (owner == 0) owner = _activePlayer;
        foreach (var b in GetDockablesUnderMission(here))
        {
            if (ReferenceEquals(b, ship)) continue;
            if (b.Tag is not Card fc) continue;
            if (!IsFacilityCard(fc) && !IsRepairFacility(fc)) continue;
            int fo = GetBorderOwner(b);
            if (fo != 0 && fo != owner) continue;
            yield return b;
        }
    }

    private bool FacilityHasSpacedock(Border facility) =>
        _attachedEvents.Any(e => e.Kind == EventRules.Persist.Spacedock && ReferenceEquals(e.Host, facility));

    private void TryDockShip(Border ship, Card shipCard, Border facility)
    {
        var here = FindMissionForDockable(ship);
        var facHere = FindMissionForDockable(facility) ?? facility;
        bool same = here != null && ReferenceEquals(here, facHere);
        var chk = DockingRules.CanDock(shipCard, facility.Tag as Card ?? shipCard,
            same, IsShipDocked(ship), IsShipCloaked(ship));
        if (!chk.ok)
        {
            ShowPlayError(chk.reason);
            return;
        }
        SetShipDockedAt(ship, facility);
        if (shipCard.InstanceId > 0)
            DebugLog.Move(_session.TurnNumber, GetBorderOwner(ship),
                $"dock #{shipCard.InstanceId} at=#{(facility.Tag as Card)?.InstanceId ?? 0} source=instance");
        if (FacilityHasSpacedock(facility))
        {
            RepairShipFully(ship, shipCard);
            StatusText.Text = $"{shipCard.Name} docks at {(facility.Tag as Card)?.Name} — Spacedock fully repairs.";
        }
        else
            StatusText.Text = $"{shipCard.Name} docks at {(facility.Tag as Card)?.Name}.";
        _session.Log.Add(_session.TurnNumber, $"P{GetBorderOwner(ship)}",
            $"{shipCard.Name} docked");
        UpdateHostBadge(ship);
        RefreshCardActionPanel(ship);
    }

    private void TryUndockShip(Border ship, Card shipCard)
    {
        if (!IsShipDocked(ship))
        {
            ShowPlayError("That ship is not docked.");
            return;
        }
        SetShipDockedAt(ship, null);
        if (shipCard.InstanceId > 0)
            DebugLog.Move(_session.TurnNumber, GetBorderOwner(ship),
                $"dock #{shipCard.InstanceId} at=0 source=instance");
        StatusText.Text = $"{shipCard.Name} undocks.";
        _session.Log.Add(_session.TurnNumber, $"P{GetBorderOwner(ship)}",
            $"{shipCard.Name} undocked");
        UpdateHostBadge(ship);
        RefreshCardActionPanel(ship);
    }

    private void ToggleCloak(Border shipBorder, Card ship)
    {
        if (_cloakLocked.Contains(shipBorder))
        {
            ShowPlayError($"{ship.Name} may not cloak (Tachyon Detection Grid).");
            return;
        }
        if (!ShipHasCloakingDevice(ship))
        {
            ShowPlayError($"{ship.Name} has no Cloaking Device.");
            return;
        }
        if (!IsShipCloaked(shipBorder) && ShipHasRequiredMove(shipBorder))
        {
            ShowPlayError("Incoming Message: ship may not cloak (7.10).");
            return;
        }
        bool nowCloaked = !IsShipCloaked(shipBorder);
        SetShipCloaked(shipBorder, nowCloaked);
        StatusText.Text = nowCloaked
            ? $"{ship.Name} cloaks (exposed ships cannot be targeted the same way)."
            : $"{ship.Name} decloaks.";
        if (ship.InstanceId > 0)
            DebugLog.Move(_session.TurnNumber, _activePlayer,
                $"cloak #{ship.InstanceId} cloaked={(nowCloaked ? 1 : 0)} source=instance");
        _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
            $"{ship.Name} {(IsShipCloaked(shipBorder) ? "cloaked" : "decloaked")}.");
        UpdateHostBadge(shipBorder);
        ClearCardActionUi();
    }

    private int CountExposedShips(int player)
    {
        int n = 0;
        foreach (var b in TableCanvas.Children.OfType<Border>())
        {
            if (b.Tag is not Card c || !IsShipCard(c)) continue;
            int o = GetBorderOwner(b); if (o == 0) o = 1;
            if (c.Controller != 0) o = c.Controller;
            if (o != player) continue;
            if (IsShipCloaked(b)) continue;
            n++;
        }
        return n;
    }

    private bool HasAttachedNamedInterrupt(Border host, string name) =>
        _attachedEvents.Any(e =>
            e.Host == host
            && (e.Card.Name ?? "").Equals(name, StringComparison.OrdinalIgnoreCase));

    private bool TryApplyNeuralServo(Card ev, Border host, int controller, AttachedEvent ae)
    {
        if (host.Tag is not Card ship || !IsShipCard(ship))
        {
            ShowPlayError("Neural Servo Device: target must be a ship.");
            return false;
        }
        if (!IsNonAlignedShip(ship))
        {
            ShowPlayError("Neural Servo Device: ship must be Non-Aligned.");
            return false;
        }
        if (EventRules.HasSkill(GetCrewOnShip(host), "SECURITY", 2))
        {
            ShowPlayError("Neural Servo Device: ship has 2 SECURITY aboard.");
            return false;
        }
        int original = GetBorderOwner(host);
        if (original == 0) original = 1;
        ae.SavedHostOwner = original;
        SetBorderOwner(host, controller);
        ship.Controller = controller;
        if (_stackOnHost.TryGetValue(host, out var crew))
        {
            foreach (var b in crew)
            {
                if (b.Tag is not Card pc) continue;
                pc.Controller = controller;
            }
        }
        TurnExpiry.RegisterFlag(_session, controller, $"NeuralServo|{ev.InstanceId}", ship.Name);
        ShowCardReveal(ev, "Neural Servo Device",
            $"Until end of turn you control {ship.Name} and its crew.\n"
            + "They are not compatible with your other cards.",
            RevealButtons.Ok, ev.Name);
        StatusText.Text = $"Neural Servo: P{controller} controls {ship.Name} until end of turn.";
        _session.Log.Add(_session.TurnNumber, $"P{controller}",
            $"Neural Servo takes {ship.Name} (was P{original}).");
        UpdateHostBadge(host);
        return true;
    }

    private void RestoreNeuralServo(AttachedEvent e)
    {
        if (e.Host == null) return;
        int back = e.SavedHostOwner > 0 ? e.SavedHostOwner : (3 - e.Owner);
        SetBorderOwner(e.Host, back);
        if (e.Host.Tag is Card ship)
            ship.Controller = back;
        if (_stackOnHost.TryGetValue(e.Host, out var crew))
        {
            foreach (var b in crew)
            {
                if (b.Tag is Card pc)
                    pc.Controller = back;
            }
        }
        _attachedEvents.Remove(e);
        SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
        _session.Log.Add(_session.TurnNumber, $"P{e.Owner}",
            $"Neural Servo ends — {(e.Host.Tag as Card)?.Name} returns to P{back}.");
        UpdateHostBadge(e.Host);
    }

    private void ApplyAsteroidSanctuary(Card card, int controller)
    {
        Border? stackDef = null;
        if (_stack.IsOpen && _stack.Top?.DefenderCard is Card defCard)
            stackDef = FindBorderForCard(defCard);
        var host = _interruptTargetHost
                   ?? stackDef
                   ?? PickOwnShip(controller);
        if (host == null || host.Tag is not Card ship || !IsShipCard(ship))
        {
            ShowPlayError("Asteroid Sanctuary: play on your exposed ship.");
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(card)) hand.Add(card);
            return;
        }
        if (GetBorderOwner(host) != controller && (ship.Controller != controller))
        {
            ShowPlayError("Asteroid Sanctuary: must be your ship.");
            return;
        }
        if (IsShipCloaked(host))
        {
            ShowPlayError("Asteroid Sanctuary: ship must be exposed (not cloaked).");
            return;
        }
        bool nav = EventRules.HasSkill(GetCrewOnShip(host), "Navigation", 2);
        _attachedEvents.Add(new AttachedEvent
        {
            Card = card,
            Kind = EventRules.Persist.None,
            Owner = controller,
            Host = host,
            Countdown = 0
        });
        TurnExpiry.Register(_session, new ExpiringEffect
        {
            Key = $"Sanctuary|{card.InstanceId}",
            Owner = controller,
            Card = card,
            Verb = TurnExpiry.VerbDiscard,
            Note = "Asteroid Sanctuary"
        });
        StatusText.Text = nav
            ? $"Asteroid Sanctuary on {ship.Name}: battles initiated against it are cancelled (2 Navigation)."
            : $"Asteroid Sanctuary on {ship.Name} — needs 2 Navigation aboard to cancel battles.";
        _session.Log.Add(_session.TurnNumber, $"P{controller}",
            $"Asteroid Sanctuary on {ship.Name}");
        UpdateHostBadge(host);
    }

    private void ApplyDistortionContinuum(Card card, int controller)
    {
        var host = _interruptTargetHost ?? PickOwnShip(controller);
        if (host == null || host.Tag is not Card ship || !IsShipCard(ship))
        {
            ShowPlayError("Distortion of Space/Time Continuum: play on your non-AU ship.");
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(card)) hand.Add(card);
            return;
        }
        if (CardIcons.HasAlternateUniverse(ship))
        {
            ShowPlayError("Distortion: target ship must be non-AU.");
            return;
        }
        if (GetBorderOwner(host) != controller && ship.Controller != controller)
        {
            ShowPlayError("Distortion: must be your ship.");
            return;
        }
        if (!OpponentPlayedAuThisTurn(controller)
            && ShowCardReveal(card, "Distortion of Space/Time Continuum",
                "Printed timing: play just after opponent plays an AU card. Continue anyway?",
                RevealButtons.YesNo) != RevealAnswer.Yes)
        {
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(card)) hand.Add(card);
            return;
        }
        if (_attachedEvents.Any(e =>
                InterruptRules.NameIs(e.Card, "Distortion of Space/Time Continuum")))
        {
            ShowPlayError("Distortion of Space/Time Continuum is unique — already in play.");
            return;
        }
        _attachedEvents.Add(new AttachedEvent
        {
            Card = card,
            Kind = EventRules.Persist.None,
            Owner = controller,
            Host = host
        });
        StatusText.Text = $"Distortion on {ship.Name}. Use the ship button to unstop / restore RANGE / unstop Away Team (then discard).";
        _session.Log.Add(_session.TurnNumber, $"P{controller}", $"Distortion on {ship.Name}");
        UpdateHostBadge(host);
    }

    private void UseDistortionOnShip(Border host)
    {
        var ae = _attachedEvents.FirstOrDefault(e =>
            e.Host == host
            && InterruptRules.NameIs(e.Card, "Distortion of Space/Time Continuum"));
        if (ae == null) return;
        var shipName = (host.Tag as Card)?.Name ?? "ship";
        if (ShowCardReveal(ae.Card, "Distortion",
                $"Unstop {shipName} and crew?", RevealButtons.YesNo) == RevealAnswer.Yes)
        {
            UnstopBorder(host);
            if (_stackOnHost.TryGetValue(host, out var crew))
                foreach (var b in crew) UnstopBorder(b);
        }
        else if (ShowCardReveal(ae.Card, "Distortion",
                     $"Restore full RANGE on {shipName}?", RevealButtons.YesNo) == RevealAnswer.Yes)
        {
            if (host.Tag is Card sc)
            {
                int hull = GetHullDamage(host);
                SetShipRangeLeft(host, sc, BattleRules.EffectiveRange(sc, hull));
                StatusText.Text = $"Distortion: {shipName} RANGE restored.";
            }
        }
        else
        {
            var mission = FindMissionForDockable(host);
            if (mission != null && _stackOnHost.TryGetValue(mission, out var away))
            {
                foreach (var b in away) UnstopBorder(b);
                StatusText.Text = "Distortion: Away Team here unstopped.";
            }
            else
                StatusText.Text = "Distortion: no Away Team here to unstop.";
        }
        _attachedEvents.Remove(ae);
        SendCardTo(ae.Card, ae.Owner, TimingRules.Destination.Discard);
        _session.Log.Add(_session.TurnNumber, $"P{ae.Owner}", "Distortion discarded after use.");
        UpdateHostBadge(host);
        ClearCardActionUi();
    }

    private void ApplyTachyonGrid(Card card, int controller)
    {
        if (CountExposedShips(controller) < 4)
        {
            ShowPlayError("Tachyon Detection Grid: you must control four exposed ships.");
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(card)) hand.Add(card);
            return;
        }
        var host = _interruptTargetHost;
        if (host == null || host.Tag is not Card ship || !IsShipCard(ship))
        {
            // Prefer a cloaked ship; otherwise a ship that can cloak.
            host = TableCanvas.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag is Card c && IsShipCard(c) && IsShipCloaked(b));
            host ??= TableCanvas.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag is Card c && IsShipCard(c) && ShipHasCloakingDevice(c));
        }
        if (host == null || host.Tag is not Card target || !IsShipCard(target))
        {
            ShowPlayError("Tachyon Detection Grid: no cloaked / cloak-capable ship.");
            var hand = controller == 1 ? _handCards : _oppHandCards;
            if (!hand.Contains(card)) hand.Add(card);
            return;
        }
        SetShipCloaked(host, false);
        if (target.InstanceId > 0)
            DebugLog.Move(_session.TurnNumber, controller,
                $"cloak #{target.InstanceId} cloaked=0 source=instance");
        _cloakLocked.Add(host);
        _attachedEvents.Add(new AttachedEvent
        {
            Card = card,
            Kind = EventRules.Persist.None,
            Owner = controller,
            Host = host
        });
        StatusText.Text = $"{target.Name} de-cloaks and may not cloak while Tachyon Detection Grid remains.";
        _session.Log.Add(_session.TurnNumber, $"P{controller}",
            $"Tachyon Detection Grid on {target.Name}");
        UpdateHostBadge(host);
    }

    private void UnstopBorder(Border border)
    {
        if (_stoppedBorders.Remove(border))
            ApplyStoppedVisual(border, stopped: false);
        if (border.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst))
            inst.Stopped = false;
    }

    private void ApplyAntiTimeExpire(AttachedEvent e)
    {
        // Each player shuffles all personnel they own in play into their draw deck.
        for (int player = 1; player <= 2; player++)
        {
            var draw = player == 1 ? _drawCards : _oppDrawCards;
            int n = 0;
            foreach (var kv in _stackOnHost.ToList())
            {
                foreach (var b in kv.Value.ToList())
                {
                    if (b.Tag is not Card p || !ModifierRules.IsPersonnelCard(p)) continue;
                    int o = p.OwnerPlayer != 0 ? p.OwnerPlayer : GetBorderOwner(b);
                    if (o == 0) o = 1;
                    if (o != player) continue;
                    kv.Value.Remove(b);
                    if (TableCanvas.Children.Contains(b)) TableCanvas.Children.Remove(b);
                    draw.Add(p);
                    n++;
                }
            }
            for (int i = draw.Count - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);
                (draw[i], draw[j]) = (draw[j], draw[i]);
            }
            _session.Log.Add(_session.TurnNumber, "sys",
                $"Anti-Time Anomaly: P{player} shuffled {n} personnel into draw.");
        }
        _attachedEvents.Remove(e);
        SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
        RefreshZoneCounts();
        StatusText.Text = "Anti-Time Anomaly expires — personnel in play shuffled into owners' draw decks.";
    }

    private void OfferAntiTimeDevronFlip(AttachedEvent anti, int turnPlayer)
    {
        int opp = turnPlayer == 1 ? 2 : 1;
        var devron = _spacelineOrder.Where(b =>
            b.Tag is Card m
            && (m.Name ?? "").Contains("Devron", StringComparison.OrdinalIgnoreCase)).ToList();
        if (devron.Count == 0)
        {
            _session.Log.AddDebug(_session.TurnNumber, "Anti-Time",
                "No Devron System on the spaceline — start-of-turn flip skipped.");
            return;
        }
        var ships = new List<Border>();
        foreach (var loc in devron)
        {
            foreach (var dock in GetDockablesUnderMission(loc))
            {
                if (dock.Tag is Card c && IsShipCard(c))
                    ships.Add(dock);
            }
        }
        if (ships.Count == 0) return;
        if (ShowCardReveal(anti.Card, "Anti-Time Anomaly",
                $"P{opp}: flip one ship at a Devron System face-down/face-up?",
                RevealButtons.YesNo) != RevealAnswer.Yes)
            return;
        var pick = ships.Count == 1 ? ships[0] : ShowTargetPickDialog(anti.Card, ships, "Flip which ship?");
        if (pick == null) pick = ships[0];
        pick.Opacity = pick.Opacity < 0.9 ? 1.0 : 0.4;
        _session.Log.Add(_session.TurnNumber, $"P{opp}",
            $"Anti-Time: flipped {(pick.Tag as Card)?.Name} at Devron.");
    }

    private void DiscardRogueBorgUnit(RogueBorgUnit unit, string reason)
    {
        if (unit.Visual != null)
        {
            if (unit.Host != null)
                RemoveCardFromHostStack(unit.Host, unit.Visual);
            if (TableCanvas.Children.Contains(unit.Visual))
                TableCanvas.Children.Remove(unit.Visual);
        }
        _rogueBorg.Remove(unit);
        // Glossary discard pile: owner's pile, not the current controller / Hugh player.
        int owner = unit.Card.OwnerPlayer is 1 or 2
            ? unit.Card.OwnerPlayer
            : (unit.PlayedBy is 1 or 2 ? unit.PlayedBy : 1);
        SendCardTo(unit.Card, owner, TimingRules.Destination.Discard);
        _session.Log.Add(_session.TurnNumber, "sys",
            $"Rogue Borg discarded ({reason}) → P{owner} discard");
        if (unit.Host != null)
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
            int shipOwner = GetBorderOwner(host);
            if (shipOwner == 0) shipOwner = 1;
            if (n < 3 && _attachedEvents.Any(e =>
                    e.Kind == EventRules.Persist.IntruderField && e.Owner == shipOwner))
            {
                _session.Log.AddDebug(_session.TurnNumber, "Check",
                    $"Intruder Force Field: {n} Rogue Borg < 3 on {ship.Name} — no invasion.");
                continue;
            }

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
        InterruptRules.IsRogueBorg(c)
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
                // Printed: "At the end of its controller's next turn, destroys ship."
                ae.TurnScope = TimingRules.TurnScope.SpecificPlayerNextTurn;
                ae.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                int wcbOwner = host != null && GetBorderOwner(host) is int o and > 0 ? o : controller;
                ae.ScopePlayer = wcbOwner;
                // Skip the remainder of the controller's current turn when already their turn.
                ae.Countdown = _session.ActivePlayer == wcbOwner ? 2 : 1;
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
        foreach (var anti in _attachedEvents.Where(e => e.Kind == EventRules.Persist.AntiTime).ToList())
            OfferAntiTimeDevronFlip(anti, turnPlayer);
        TryNullifyBaryonBuildup(turnPlayer);

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
            if (InterruptRules.IsCrosis(ae.Card)
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
        // Legacy paint-index path — prefer overload with Location line + WNOHGB wrap.
        return CheckEventMovementOnLine(ship, shipCard, fromIdx, toIdx, crew, wrapEnds: false, lineCount: 0,
            directCost: int.MaxValue, wrapCost: int.MaxValue, indexOfHost: IndexOfMission);
    }

    private string? CheckEventMovementOnLine(
        Border ship, Card shipCard, int fromIdx, int toIdx, List<Card> crew,
        bool wrapEnds, int lineCount, int directCost, int wrapCost,
        Func<Border?, int> indexOfHost)
    {
        bool arrivedAtFrom = _arrivedMissionThisTurn.TryGetValue(ship, out int arrived) && arrived == fromIdx;
        var hazards = _attachedEvents.Select(e => new MovementHazardRules.HazardEvent(
            e.Kind, indexOfHost(e.Host), indexOfHost(e.Host2), DestIsGapsLocation: false));
        return MovementHazardRules.CheckMovement(
            hazards, fromIdx, toIdx, crew, arrivedAtFrom,
            wrapEnds, lineCount, directCost, wrapCost);
    }

    private void ApplyEventAfterMove(Border ship, Card shipCard, int fromIdx, int toIdx, List<Card> crew)
    {
        var dest = (toIdx >= 0 && toIdx < _spacelineOrder.Count) ? _spacelineOrder[toIdx] : null;
        foreach (var e in _attachedEvents.ToList())
        {
            int h1 = IndexOfMission(e.Host);
            if (e.Kind == EventRules.Persist.Rift && h1 >= 0)
            {
                bool arrivedAtRift = _arrivedMissionThisTurn.TryGetValue(ship, out int arrived) && arrived == h1;
                var (applyRift, flyBy) = MovementHazardRules.RiftDamage(fromIdx, toIdx, h1, arrivedAtRift);
                if (applyRift)
                {
                    ApplyHullDamage(ship, shipCard, Math.Min(100, GetHullDamage(ship) + 50));
                    _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                        flyBy ? "Subspace Warp Rift: fly-by damage"
                              : "Subspace Warp Rift: damaged for moving again after arriving");
                    StatusText.Text = flyBy
                        ? $"{shipCard.Name} damaged flying by Subspace Warp Rift."
                        : $"{shipCard.Name} damaged — moved again after arriving at Subspace Warp Rift.";
                }
            }
            // Gaps kill only on Gaps span (rules); View discards + logs.
            bool destIsGaps = dest?.Tag is Card dc && EventRules.NameIs(dc, "Gaps in Normal Space");
            bool destIsGapsFace = dest != null && ReferenceEquals(FindBorderForCard(e.Card), dest);
            if (e.Kind == EventRules.Persist.Gaps
                && MovementHazardRules.GapsKillOnArrival(destIsGaps || destIsGapsFace))
            {
                if (_stackOnHost.TryGetValue(ship, out var list))
                {
                    var crewB = list.Where(b => b.Tag is Card c && ModifierRules.IsPersonnelCard(c)).ToList();
                    if (crewB.Count > 0)
                    {
                        var victim = crewB[new Random().Next(crewB.Count)];
                        if (victim.Tag is Card vc)
                        {
                            int vo = GetBorderOwner(victim);
                            if (vo == 0) vo = _activePlayer;
                            DiscardPersonnelBorder(victim, vc, vo);
                            _session.Log.Add(_session.TurnNumber, $"P{vo}",
                                $"Gaps in Normal Space: {vc.Name} killed on {shipCard.Name}");
                            DebugLog.Move(_session.TurnNumber, vo,
                                $"gaps-kill {DebugLog.Card(vc)} on {DebugLog.Card(shipCard)}");
                            StatusText.Text = $"Gaps: {vc.Name} killed aboard {shipCard.Name}.";
                        }
                    }
                }
            }
        }
        _movedThisTurnAfterArrival.Add(ship);
        if (toIdx >= 0)
            _arrivedMissionThisTurn[ship] = toIdx;

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
                                            && EventRules.IsGenetronicReplicator(e.Card))
                   || HasTableCard(EventRules.IsGenetronicReplicator);
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

    /// <summary>
    /// "May be nullified by SKILL" on a ship event (Plasma Fire / Warp Core Breach).
    /// Meeting the condition is not an action (7.10.0.1). Uses NullifyEventInPlay so
    /// the card leaves the host stack, not only the discard list.
    /// </summary>
    private void AddShipSkillNullifyButtons(Border shipBorder, Action<string, RoutedEventHandler> addBtn)
    {
        var crew = GetCrewOnShip(shipBorder);
        var offered = new HashSet<EventRules.Persist>();
        foreach (var ae in EventsOn(shipBorder))
        {
            if (!offered.Add(ae.Kind)) continue;
            string? skill = EventRules.SkillNullifier(ae.Kind);
            if (skill == null) continue;
            if (!EventRules.HasSkill(crew, skill)) continue;
            string name = ae.Card.Name ?? ae.Kind.ToString();
            var kind = ae.Kind;
            addBtn($"Nullify {name} ({skill})", (_, _) =>
                TryNullifyShipSkillEvent(shipBorder, kind, skill, name));
        }
    }

    private void TryNullifyShipSkillEvent(
        Border shipBorder, EventRules.Persist kind, string skill, string cardName)
    {
        if (!EventRules.HasSkill(GetCrewOnShip(shipBorder), skill))
        {
            ShowPlayError($"Need {skill} aboard to nullify {cardName}.");
            return;
        }
        var hits = EventsOn(shipBorder).Where(ae => ae.Kind == kind).ToList();
        if (hits.Count == 0)
        {
            ShowPlayError($"No {cardName} on this ship.");
            return;
        }
        foreach (var e in hits)
            NullifyEventInPlay(e.Card, _activePlayer);
        if (shipBorder.Tag is Card sc)
            ShowHostContents(shipBorder, sc);
        StatusText.Text = $"{cardName} nullified ({skill}).";
    }

    private void ProcessEndOfTurnEvents(int owner)
    {
        foreach (var e in _attachedEvents.ToList())
        {
            if (InterruptRules.IsTranswarpConduit(e.Card) && e.Owner == owner)
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
                if (HasThermalDeflectors())
                {
                    _session.Log.AddDebug(_session.TurnNumber, "Check",
                        "Plasma Fire suppressed by Thermal Deflectors.");
                    continue;
                }
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
                // Destroy at end of controller's next turn. ENGINEER nullify is optional.
                int shipOwner = GetBorderOwner(e.Host);
                if (shipOwner == 0) shipOwner = e.Owner;
                e.ScopePlayer ??= shipOwner;
                e.TurnScope = TimingRules.TurnScope.SpecificPlayerNextTurn;
                e.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;

                if (!TimingRules.ShouldProcessOnTurn(
                        e.TurnScope, e.PhasePoint,
                        TimingRules.TurnPhasePoint.EndOfTurn, owner, e.ScopePlayer))
                    continue;

                int cd = e.Countdown;
                bool explode = TimingRules.TickCountdown(
                    ref cd, e.TurnScope, e.PhasePoint,
                    TimingRules.TurnPhasePoint.EndOfTurn, owner, e.ScopePlayer);
                e.Countdown = cd;
                if (explode && e.Host.Tag is Card ws)
                {
                    ShowCardReveal(e.Card, "Warp Core Breach",
                        $"{ws.Name} is destroyed (end of controller's next turn).",
                        RevealButtons.Ok, ws.Name, autoCloseMs: 4000);
                    DestroyShipOrFacility(e.Host, ws, shipOwner);
                    _attachedEvents.Remove(e);
                    if (!_discardCards.Contains(e.Card) && !_oppDiscardCards.Contains(e.Card))
                        SendCardTo(e.Card, e.Owner, TimingRules.Destination.Discard);
                    _session.Log.Add(_session.TurnNumber, "sys",
                        $"Warp Core Breach destroys {ws.Name}.");
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
                _pendingExtraDraws++;

            if (e.Kind == EventRules.Persist.Kidnappers && e.Owner == owner)
                RunKidnappers(owner, e.Card);

            if (e.Kind == EventRules.Persist.NeuralServo && e.Owner == owner && e.Host != null)
                RestoreNeuralServo(e);

            if (e.Kind == EventRules.Persist.AntiTime)
            {
                e.TurnScope = TimingRules.TurnScope.EveryTurn;
                e.PhasePoint = TimingRules.TurnPhasePoint.EndOfTurn;
                int cd = e.Countdown;
                bool done = TimingRules.TickCountdown(
                    ref cd, e.TurnScope, e.PhasePoint,
                    TimingRules.TurnPhasePoint.EndOfTurn, owner, e.ScopePlayer);
                e.Countdown = cd;
                if (done)
                {
                    ApplyAntiTimeExpire(e);
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
        SetHullDamagePercent(shipBorder, 0);
        _repairTurnsAtOutpost.Remove(shipBorder);
        shipBorder.RenderTransform = null;
        shipBorder.Opacity = 1.0;
        UpdateDamageBadge(shipBorder, 0);

        // RANGE wieder voll (nächster Zug / sofort für Rest des Spiels)
        int full = MovementRules.GetShipRange(ship);
        SetShipRangeLeft(shipBorder, ship, full);

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

    private void SyncShipCombatVisuals(Border border)
    {
        int hull = GetHullDamage(border);
        if (hull >= 50 && hull < 100)
        {
            border.RenderTransformOrigin = new Point(0.5, 0.5);
            border.RenderTransform = new RotateTransform(180);
        }
        else
        {
            border.RenderTransform = Transform.Identity;
        }
        UpdateDamageBadge(border, hull);
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
        if (border.Tag is Card c && c.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(c.InstanceId, out var inst))
            inst.Stopped = true;
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
    private bool HasEscapePodInHand(int owner)
    {
        var hand = owner == 2 ? _oppHandCards : _handCards;
        return hand.Any(InterruptRules.IsEscapePod);
    }

    private EscapePodState? EscapePodHere(Border ship, int owner)
    {
        var here = FindMissionForDockable(ship);
        if (here == null) return null;
        return _escapePods.FirstOrDefault(p =>
            p.Owner == owner && p.Mission != null && ReferenceEquals(p.Mission, here) && p.Crew.Count > 0);
    }

    private void ApplyEscapePodFromResponse(int owner, TimingRules.PendingAction destroyed)
    {
        var shipB = destroyed.AttackerHost as Border;
        var mission = destroyed.DefenderHost as Border ?? (shipB != null ? FindMissionForDockable(shipB) : null);
        var crew = new List<Card>();
        if (shipB != null && _stackOnHost.TryGetValue(shipB, out var stacked))
        {
            foreach (var sb in stacked.ToList())
            {
                if (sb.Tag is not Card sc) continue;
                if (!IsCrewType(sc) && !IsEquipmentType(sc) && !ModifierRules.IsPersonnelCard(sc))
                    continue;
                crew.Add(sc);
                stacked.Remove(sb);
                if (TableCanvas.Children.Contains(sb))
                    TableCanvas.Children.Remove(sb);
                if (mission != null)
                    AttachCardToHost(sc, mission, owner);
            }
        }
        var hand = owner == 2 ? _oppHandCards : _handCards;
        var pod = hand.FirstOrDefault(InterruptRules.IsEscapePod);
        if (pod != null)
        {
            hand.Remove(pod);
            if (mission != null)
            {
                AttachCardToHost(pod, mission, owner);
                _attachedEvents.Add(new AttachedEvent
                {
                    Card = pod,
                    Kind = EventRules.Persist.None,
                    Owner = owner,
                    Host = mission
                });
            }
        }
        _escapePods.Add(new EscapePodState
        {
            Pod = pod ?? destroyed.Card!,
            Mission = mission,
            Owner = owner,
            Crew = crew
        });
        StatusText.Text = crew.Count > 0
            ? $"Escape Pod: {crew.Count} card(s) saved at {(mission?.Tag as Card)?.Name}."
            : "Escape Pod: no crew to save.";
        _session.Log.Add(_session.TurnNumber, $"P{owner}",
            $"Escape Pod saved {crew.Count} from {destroyed.Card?.Name}");
        RefreshHandStrips();
        RefreshZoneCounts();
    }

    private void RecoverEscapePodCrew(Border ship)
    {
        int owner = GetBorderOwner(ship);
        if (owner == 0) owner = _activePlayer;
        var pod = EscapePodHere(ship, owner);
        if (pod == null)
        {
            ShowPlayError("No Escape Pod crew here.");
            return;
        }
        if (pod.Mission != null && _stackOnHost.TryGetValue(pod.Mission, out var at))
        {
            foreach (var c in pod.Crew.ToList())
            {
                var mini = at.FirstOrDefault(b => b.Tag is Card x && ReferenceEquals(x, c));
                if (mini != null)
                {
                    at.Remove(mini);
                    if (TableCanvas.Children.Contains(mini))
                        TableCanvas.Children.Remove(mini);
                }
                AttachCardToHost(c, ship, owner);
            }
        }
        else
        {
            foreach (var c in pod.Crew)
                AttachCardToHost(c, ship, owner);
        }
        if (pod.Pod != null)
        {
            if (pod.Mission != null)
            {
                foreach (var ae in _attachedEvents.Where(e => ReferenceEquals(e.Card, pod.Pod)).ToList())
                    _attachedEvents.Remove(ae);
                if (_stackOnHost.TryGetValue(pod.Mission, out var stack))
                {
                    foreach (var b in stack.Where(x => x.Tag is Card c && ReferenceEquals(c, pod.Pod)).ToList())
                    {
                        stack.Remove(b);
                        if (TableCanvas.Children.Contains(b))
                            TableCanvas.Children.Remove(b);
                    }
                }
            }
            SendCardTo(pod.Pod, owner, TimingRules.Destination.Discard);
        }
        _escapePods.Remove(pod);
        UpdateHostBadge(ship);
        if (pod.Mission != null) UpdateHostBadge(pod.Mission);
        StatusText.Text = $"Escape Pod crew boarded {(ship.Tag as Card)?.Name}.";
        RefreshZoneCounts();
    }

    private void DestroyShipOrFacility(Border border, Card card, int owner)
    {
        var mission = FindMissionForDockable(border);
        if (!_resolvingDestroy && IsShipCard(card)
            && HasEscapePodInHand(owner) && !_stack.IsOpen)
        {
            _stack.Push(new TimingRules.PendingAction
            {
                Kind = TimingRules.ActionKind.ShipDestroyed,
                Controller = owner,
                Card = card,
                AttackerHost = border,
                DefenderHost = mission,
                Summary = $"P{owner} ship destroyed: {card.Name}"
            });
            OpenResponseWindow(owner);
            ScheduleActionAnnounce(400);
            StatusText.Text = $"{card.Name} destroyed — Escape Pod response window.";
            return;
        }

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
                if (_stoppedBorders.Remove(sb) && sb.Tag is Card stopCard && stopCard.InstanceId > 0
                    && BoardStore.Current.ById.TryGetValue(stopCard.InstanceId, out var stopInst))
                    stopInst.Stopped = false;
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

        if (TableCanvas.Children.Contains(border))
            TableCanvas.Children.Remove(border);
        _tablePermanentCards.Remove(card);
        _oppTablePermanentCards.Remove(card);
        _hullDamagePercent.Remove(border);
        _cloakedShips.Remove(border);
        _dockedAt.Remove(border);
        if (_stoppedBorders.Remove(border) && card.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(card.InstanceId, out var deadStop))
            deadStop.Stopped = false;
        if (card.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(card.InstanceId, out var deadShipInst)
            && deadShipInst is ShipInstance deadSh)
        {
            deadSh.Cloaked = false;
            deadSh.DockedAtId = 0;
            deadSh.HullPercent = -1;
        }
        _repairTurnsAtOutpost.Remove(border);
        ClearShipRangeLeft(border);
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

        var check = BattleRules.CanInitiatePersonnelAttack(
            atkCards, defCards, atkOwner, defOwner, _wartimeVsAffiliation);
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
        if (_stoppedBorders.Remove(border) && border.Tag is Card remCard && remCard.InstanceId > 0
            && BoardStore.Current.ById.TryGetValue(remCard.InstanceId, out var remInst))
            remInst.Stopped = false;
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

        if (targetHost.Tag is Card destAsMission
            && CardKinds.IsMission(destAsMission)
            && !MissionRules.IsPlanetMission(destAsMission))
        {
            ShowPlayError("You may not beam cards into space (7.1.1.0.1).");
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

        if (targetHost.Tag is Card destHostCard)
        {
            var treaties = GetActiveTreaties(_activePlayer);
            var blocked = toMove
                .Select(b => b.Tag as Card)
                .Where(c => c != null && !TreatyRules.CanOccupyHost(c!, destHostCard, treaties))
                .Select(c => c!.Name)
                .ToList();
            if (blocked.Count > 0)
            {
                ShowPlayError(
                    $"Cannot beam {string.Join(", ", blocked)} onto {destHostCard.Name} — " +
                    "incompatible affiliation (need a Treaty). Equipment is unrestricted.");
                return true;
            }
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
            SyncBoardFromTable();
            ClearCardActionUi();
            SetSelection(targetHost);
            ShowHostContents(targetHost, tc);
        }
        else
        {
            _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}", $"Beam {toMove.Count} cards");
            SyncBoardFromTable();
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
            _dockableAtMission[below[i]] = mission;
            SyncShipCombatVisuals(below[i]);
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
            _dockableAtMission[above[i]] = mission;
            SyncShipCombatVisuals(above[i]);
            UpdateHostBadge(above[i]);
        }

        // Mission-Badge nur für Away-Team auf der Mission, nicht für Crew auf Schiffen
        UpdateHostBadge(mission);

        if (_selectedCard != null)
            UpdateSelectionFrame(_selectedCard);

        EnsureBoardExtents();
    }

    private bool _fittingBoard;
    private const double BoardMinWidth = 2800;
    private const double BoardMinHeight = 1400;
    private const double BoardPad = 80;

    /// <summary>
    /// Grow the canvas (and push the spaceline down) so every docked ship/facility
    /// stays inside the board and can be reached by pan / scroll when zoomed in.
    /// </summary>
    private void EnsureBoardExtents()
    {
        if (TableCanvas == null || _fittingBoard) return;
        _fittingBoard = true;
        try
        {
            double minX = double.PositiveInfinity, minY = double.PositiveInfinity;
            double maxX = 0, maxY = 0;
            bool any = false;
            foreach (UIElement el in TableCanvas.Children)
            {
                if (el.Visibility != Visibility.Visible) continue;
                double l = Canvas.GetLeft(el);
                double t = Canvas.GetTop(el);
                if (double.IsNaN(l)) l = 0;
                if (double.IsNaN(t)) t = 0;
                double w = TableCardWidth, h = TableCardHeight;
                if (el is FrameworkElement fe)
                {
                    if (fe.ActualWidth > 1) w = fe.ActualWidth;
                    else if (fe.Width > 1 && !double.IsNaN(fe.Width)) w = fe.Width;
                    if (fe.ActualHeight > 1) h = fe.ActualHeight;
                    else if (fe.Height > 1 && !double.IsNaN(fe.Height)) h = fe.Height;
                }
                minX = Math.Min(minX, l);
                minY = Math.Min(minY, t);
                maxX = Math.Max(maxX, l + w);
                maxY = Math.Max(maxY, t + h);
                any = true;
            }
            if (!any) return;

            double dx = minX < BoardPad ? BoardPad - minX : 0;
            double dy = minY < BoardPad ? BoardPad - minY : 0;
            if (dx != 0 || dy != 0)
            {
                ShiftCanvasContent(dx, dy);
                SpacelineY += dy;
                maxX += dx;
                maxY += dy;
            }

            double needW = Math.Max(BoardMinWidth, maxX + BoardPad);
            double needH = Math.Max(BoardMinHeight, maxY + BoardPad);
            if (Math.Abs(TableCanvas.Width - needW) > 2)
                TableCanvas.Width = needW;
            if (Math.Abs(TableCanvas.Height - needH) > 2)
                TableCanvas.Height = needH;
        }
        finally
        {
            _fittingBoard = false;
        }
    }

    private void ShiftCanvasContent(double dx, double dy)
    {
        if (dx == 0 && dy == 0) return;
        foreach (UIElement el in TableCanvas.Children)
        {
            double l = Canvas.GetLeft(el);
            double t = Canvas.GetTop(el);
            if (double.IsNaN(l)) l = 0;
            if (double.IsNaN(t)) t = 0;
            Canvas.SetLeft(el, l + dx);
            Canvas.SetTop(el, t + dy);
        }
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
        int hull = GetHullDamage(b);
        if (hull < 50 || hull >= 100)
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
        double viewH = TableScroll.ViewportHeight;
        double viewW = TableScroll.ViewportWidth;
        double minS = 0.25;
        if (viewH > 1 && TableCanvas.Height > 1)
            minS = Math.Min(minS, viewH / TableCanvas.Height);
        if (viewW > 1 && TableCanvas.Width > 1)
            minS = Math.Min(minS, viewW / TableCanvas.Width);
        minS = Math.Max(0.12, minS);
        if (newScale < minS) newScale = minS;
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
            src = ParentOf(src);
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

    /// <summary>
    /// Host-facing effect line. Uses live crew for class-scaled bonuses (Nutational / Metaphasic).
    /// Persist enum names are never shown — they just echoed the card title.
    /// </summary>
    private string FormatAttachedHostEffectLine(
        string kind, Card card, int countdown, string? persistKind, IEnumerable<Card>? aboard)
    {
        if (kind == "Event"
            && Enum.TryParse<EventRules.Persist>(persistKind, out var ek)
            && ek != EventRules.Persist.None)
            return EventRules.FormatHostEffectSummary(ek, card, aboard, countdown);

        if (kind == "Dilemma"
            && Enum.TryParse<DilemmaRules.PersistKind>(persistKind, out var dk)
            && dk != DilemmaRules.PersistKind.None)
            return DilemmaRules.FormatHostEffectSummary(dk, card, countdown);

        string line = $"{kind}: {card.Name}";
        if (countdown > 0)
            line += $"  ·  COUNTER {countdown}";
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
            DetailStaff.Text = "";

            // Printed special equipment lives in Text (Holodeck, Tractor Beam) — keep it with stats.
            string special = (card.Text ?? "").Trim();
            if (string.IsNullOrEmpty(special) && !string.IsNullOrWhiteSpace(card.Characteristics))
            {
                special = string.Join(", ",
                    card.Characteristics.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !s.EndsWith(" ship", StringComparison.OrdinalIgnoreCase)
                                    && !s.Contains("ship;", StringComparison.OrdinalIgnoreCase)));
            }
            if (!string.IsNullOrWhiteSpace(card.Icons))
                special = string.IsNullOrEmpty(special) ? $"Icons: {card.Icons}" : special + "\nIcons: " + card.Icons;
            if (!string.IsNullOrEmpty(special))
                DetailStaff.Text = string.IsNullOrEmpty(DetailStaff.Text)
                    ? special
                    : DetailStaff.Text + "\n" + special;
            DetailText.Text = ""; // do not repeat special equipment in the body

            var eventLines = new List<string>();
            Border? shipBorder = FindBorderForCard(card);
            IEnumerable<Card> aboard = shipBorder == null
                ? Enumerable.Empty<Card>()
                : GetAllCardsOnHost(shipBorder, GetBorderOwner(shipBorder) == 0 ? 1 : GetBorderOwner(shipBorder));
            if (shipBorder != null)
            {
                foreach (var ae in EventsOn(shipBorder))
                    eventLines.Add(FormatAttachedHostEffectLine("Event", ae.Card, ae.Countdown, ae.Kind.ToString(), aboard));
                foreach (var ad in _attachedDilemmas.Where(d => ReferenceEquals(d.Host, shipBorder)))
                    eventLines.Add(FormatAttachedHostEffectLine("Dilemma", ad.Card, ad.Countdown, ad.Kind.ToString(), aboard));
                if (_stackOnHost.TryGetValue(shipBorder, out var stacked))
                {
                    foreach (var b in stacked)
                    {
                        if (b.Tag is not Card ec || !EventRules.IsEvent(ec)) continue;
                        if (EventsOn(shipBorder).Any(ae => ReferenceEquals(ae.Card, ec))) continue;
                        eventLines.Add(FormatAttachedHostEffectLine("Event", ec, 0, null, aboard));
                    }
                }
            }
            DetailIcons.Text = string.Join("\n", eventLines);
        }
        else if (EventRules.IsEvent(card) || CardKinds.IsDilemma(card))
        {
            if (!string.IsNullOrWhiteSpace(card.Icons)) attrs.Add($"Icons: {card.Icons}");
            DetailAttributes.Text = string.Join("  •  ", attrs);
            DetailClass.Text = "";
            DetailStaff.Text = "";
            var counterLines = new List<string>();
            foreach (var ae in _attachedEvents.Where(e => ReferenceEquals(e.Card, card)))
            {
                string hostName = (ae.Host?.Tag as Card)?.Name ?? "(no host)";
                IEnumerable<Card>? aboard = ae.Host == null
                    ? null
                    : GetAllCardsOnHost(ae.Host, GetBorderOwner(ae.Host) == 0 ? 1 : GetBorderOwner(ae.Host));
                counterLines.Add(FormatAttachedHostEffectLine("Event", card, ae.Countdown, ae.Kind.ToString(), aboard)
                                 + $"  ·  on {hostName}");
            }
            foreach (var ad in _attachedDilemmas.Where(d => ReferenceEquals(d.Card, card)))
            {
                string hostName = (ad.Host?.Tag as Card)?.Name ?? "(no host)";
                counterLines.Add(FormatAttachedHostEffectLine("Dilemma", card, ad.Countdown, ad.Kind.ToString(), null)
                                 + $"  ·  on {hostName}");
            }
            if (EventRules.IsRedAlert(card))
                counterLines.Add(FormatRedAlertStatusLine(card));
            if (counterLines.Count == 0 && !string.IsNullOrWhiteSpace(card.Icons))
                counterLines.Add($"Icons: {card.Icons}");
            DetailIcons.Text = string.Join("\n", counterLines);
        }
        else if (CardKinds.IsPersonnel(card))
        {
            // Effektive Werte, falls Karte auf einem Host present ist
            var presentCtx = FindPresentContextForCard(card);
            string skillsLine = "";
            if (presentCtx != null)
            {
                var ep = ModifierRules.ResolvePersonnel(card, presentCtx.Value.cards, presentCtx.Value.owner);
                var classKeys = new HashSet<string>(MissionRules.Classifications, StringComparer.OrdinalIgnoreCase);
                DetailAttributes.Text = string.Join("\n",
                    ModifierRules.FormatProfileLines(ep)
                        .Split('\n')
                        .Where(l => !l.StartsWith("Classification:", StringComparison.OrdinalIgnoreCase)
                                    && !l.StartsWith("Skills:", StringComparison.OrdinalIgnoreCase)));
                var skillParts = ep.Skills.Where(kv => !classKeys.Contains(kv.Key))
                    .OrderBy(k => k.Key)
                    .Select(kv => kv.Value > 1 ? $"{kv.Key}×{kv.Value}" : kv.Key);
                if (skillParts.Any())
                    skillsLine = string.Join(", ", skillParts);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(card.IntegrityOrRange)) attrs.Add($"INTEGRITY {card.IntegrityOrRange}");
                if (!string.IsNullOrWhiteSpace(card.CunningOrWeapons)) attrs.Add($"CUNNING {card.CunningOrWeapons}");
                if (!string.IsNullOrWhiteSpace(card.StrengthOrShields)) attrs.Add($"STRENGTH {card.StrengthOrShields}");
                if (!string.IsNullOrWhiteSpace(card.Points)) attrs.Add($"POINTS {card.Points}");
                DetailAttributes.Text = string.Join("\n", attrs);
                var parsed = MissionRules.ParsePersonnelSkills(card);
                var classKeys = new HashSet<string>(MissionRules.Classifications, StringComparer.OrdinalIgnoreCase);
                skillsLine = string.Join(", ", parsed
                    .Where(kv => !classKeys.Contains(kv.Key))
                    .OrderBy(kv => kv.Key)
                    .Select(kv => kv.Value > 1 ? $"{kv.Key}×{kv.Value}" : kv.Key));
            }

            // Classification once (printed class). Icons next. Skills once. No card.Text dump.
            string classLine = string.IsNullOrWhiteSpace(card.Class)
                ? ""
                : "Classification: " + card.Class.Trim();
            DetailClass.Text = classLine;
            DetailStaff.Text = string.IsNullOrEmpty(skillsLine) ? "" : "Skills: " + skillsLine;
            DetailIcons.Text = "";
            DetailText.Text = "";
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(card.IntegrityOrRange)) attrs.Add($"INT/RNG {card.IntegrityOrRange}");
            if (!string.IsNullOrWhiteSpace(card.CunningOrWeapons)) attrs.Add($"CUN/WPN {card.CunningOrWeapons}");
            if (!string.IsNullOrWhiteSpace(card.StrengthOrShields)) attrs.Add($"STR/SHD {card.StrengthOrShields}");
            if (!string.IsNullOrWhiteSpace(card.Points)) attrs.Add($"POINTS {card.Points}");
            DetailAttributes.Text = string.Join("  •  ", attrs);
            DetailClass.Text = string.IsNullOrWhiteSpace(card.Class) ? "" : $"Classification: {card.Class}";
            DetailStaff.Text = "";
            DetailIcons.Text = string.IsNullOrWhiteSpace(card.Icons) ? "" : $"Icons: {card.Icons}";
        }

        IconCatalog.FillStaffing(DetailStaffRow, card, 22);
        IconCatalog.Fill(DetailIconRow, card, 22);
        UpdateDetailBackButton(card);

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
            if (_peekLegalTargets.Count > 0)
            {
                DetailStackSection.Visibility = Visibility.Visible;
                if (DetailStackTitle != null)
                    DetailStackTitle.Text = "Legal targets";
                foreach (var c in _peekLegalTargets)
                {
                    var mini = CreateMiniCard(c, faceDown: false);
                    mini.Width = 72;
                    mini.Height = 100;
                    mini.Margin = new Thickness(3);
                    mini.Tag = c;
                    DetailStackCards.Children.Add(mini);
                }
                return;
            }
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
        if (DetailStackStats != null)
        {
            DetailStackStats.Text = "";
            DetailStackStats.Inlines.Clear();
            var green = new SolidColorBrush(Color.FromRgb(0xB5, 0xCE, 0xA8));
            void Run(string s, bool nl = true)
            {
                if (string.IsNullOrEmpty(s)) return;
                DetailStackStats.Inlines.Add(new System.Windows.Documents.Run(s) { Foreground = green });
                if (nl)
                    DetailStackStats.Inlines.Add(new System.Windows.Documents.LineBreak());
            }

            if (IsShipCard(hostCard))
                Run(FormatShipEffectiveLine(hostCard));
            string rbLine = FormatRogueBorgDetailLine(host);
            if (!string.IsNullOrEmpty(rbLine))
                Run(rbLine);

            for (int p = 1; p <= 2; p++)
            {
                var present = GetAllCardsOnHost(host, p);
                if (!present.Any()) continue;
                Run($"— Player {p} —");
                if (present.Any(ModifierRules.IsPersonnelCard))
                {
                    var team = ModifierRules.SummarizeTeam(present, p);
                    Run($"S{p} Team — {team.PersonnelCount} Pers · {team.EquipmentCount} Eq", nl: false);
                    IconCatalog.AppendCounts(DetailStackStats.Inlines, present, 12);
                    DetailStackStats.Inlines.Add(new System.Windows.Documents.LineBreak());
                    var body = ModifierRules.FormatTeamSummary(team, p);
                    int cut = body.IndexOf('\n');
                    if (cut >= 0 && cut + 1 < body.Length)
                        Run(body[(cut + 1)..]);
                }
            }
        }

        void AddStackMini(Card c, string? badge = null, Border? cardBorder = null)
        {
            var mini = CreateMiniCard(c, faceDown: false);
            mini.Width = 72;
            mini.Height = 100;
            mini.Margin = new Thickness(3);
            mini.Cursor = Cursors.Hand;
            mini.Tag = c;
            if (_peekLegalTargets.Contains(c)
                || _peekLegalTargets.Any(t =>
                    string.Equals(t.Name, c.Name, StringComparison.OrdinalIgnoreCase)))
            {
                mini.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 220, 255));
                mini.BorderThickness = new Thickness(4);
                mini.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(80, 220, 255),
                    BlurRadius = 14,
                    ShadowDepth = 0,
                    Opacity = 0.95
                };
            }
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
                if (EventsOn(host).Any(ae => ReferenceEquals(ae.Card, c)))
                    continue;
                if (InterruptRules.IsCrosis(c) && HostHasCrosis(host))
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

    private void UpdateDetailBackButton(Card shown)
    {
        if (BtnDetailBack == null) return;
        bool hostOpen = _detailHost?.Tag is Card host && !ReferenceEquals(host, shown);
        BtnDetailBack.Visibility = hostOpen ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnDetailBack_Click(object sender, RoutedEventArgs e)
    {
        if (_detailHost?.Tag is not Card host) return;
        ShowCardDetail(host);
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
        if (_detailPickMode)
        {
            CompleteDetailPick(null);
            return;
        }
        if (CardDetailOverlay != null)
            CardDetailOverlay.Visibility = Visibility.Collapsed;
        if (DetailStackCards != null)
            DetailStackCards.Children.Clear();
        if (DetailStackSection != null)
            DetailStackSection.Visibility = Visibility.Collapsed;
        if (BtnDetailBeamSelect != null)
            BtnDetailBeamSelect.Visibility = Visibility.Collapsed;
        if (BtnDetailBack != null)
            BtnDetailBack.Visibility = Visibility.Collapsed;
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
