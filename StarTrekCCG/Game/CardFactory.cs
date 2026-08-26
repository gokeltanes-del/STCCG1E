using System;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Database cards are prototypes. Every copy in a game is a new instance
/// so two universals, two Hidden Agendas, or two tactics can be addressed.
/// </summary>
public static class CardFactory
{
    private static int _nextId = 1;

    public static int PeekNextId => _nextId;

    public static void ResetIdsForNewGame() => _nextId = 1;

    public static void NoteHighestId(int id)
    {
        if (id >= _nextId) _nextId = id + 1;
    }

    public static Card Instantiate(Card prototype, int owner)
    {
        var c = ClonePrinted(prototype);
        c.InstanceId = _nextId++;
        c.OwnerPlayer = owner is 1 or 2 ? owner : 0;
        c.Controller = c.OwnerPlayer;
        c.FaceUp = !CardIcons.HasHiddenAgenda(c);
        // HA defaults face-down only when it actually enters the core; piles stay "unknown".
        if (CardIcons.HasHiddenAgenda(c))
            c.FaceUp = false;
        return c;
    }

    public static Card ClonePrinted(Card src)
    {
        return new Card
        {
            Name = src.Name,
            Type = src.Type,
            Affiliation = src.Affiliation,
            SetFolder = src.SetFolder,
            ReleaseRaw = src.ReleaseRaw,
            RarityInfo = src.RarityInfo,
            Uniqueness = src.Uniqueness,
            Class = src.Class,
            IntegrityOrRange = src.IntegrityOrRange,
            CunningOrWeapons = src.CunningOrWeapons,
            StrengthOrShields = src.StrengthOrShields,
            Points = src.Points,
            Icons = src.Icons,
            Staff = src.Staff,
            Characteristics = src.Characteristics,
            Text = src.Text,
            Image = src.Image,
            OldImageFile = src.OldImageFile,
            MissionDilemmaType = src.MissionDilemmaType,
            Region = src.Region,
            Quadrant = src.Quadrant,
            Span = src.Span,
            FullImagePath = src.FullImagePath
        };
    }
}