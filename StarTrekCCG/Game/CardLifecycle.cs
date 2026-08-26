using System;

namespace StarTrekCCG;

/// <summary>Where a card instance currently sits. Fog-of-war and LegalMoves key off this.</summary>
public enum CardZone
{
    Unknown,
    Hand,
    Draw,
    Discard,
    Seed,
    TableCore,
    Spaceline,
    AboardHost,
    UnderMission,
    OutOfPlay,
    QsTent,
    BattleBridge,
    QContinuum,
    SitePile,
    TribblePile,
    PointsArea
}

/// <summary>Compendium status flags. Combine with bitwise OR.</summary>
[Flags]
public enum CardStatus
{
    None = 0,
    Stopped = 1,
    Disabled = 2,
    InStasis = 4,
    Captured = 8,
    Cloaked = 16,
    Landed = 32,
    Quarantined = 64,
    InPlayFaceDown = 128
}

/// <summary>How often a printed ability may fire.</summary>
public enum EffectFrequency
{
    Unlimited,
    OncePerTurn,
    OncePerGame,
    UntilEndOfTurn
}