using System;
using System.Collections.Generic;
using System.Linq;
using StarTrekCCG.Models;

namespace StarTrekCCG;

/// <summary>
/// Central Glossary and Rulebook pipeline for Ship, Facility, and Site status terms:
/// - "your ship" / ownership
/// - "tractor beam" special equipment
/// - "exposed": undocked, uncloaked, unphased, and not landed or carried
/// - "occupied": at least one personnel aboard
/// - "unoccupied" / "empty": no personnel aboard
/// - "empty exposed ship": empty of personnel and exposed
/// </summary>
public static class ShipRules
{
    /// <summary>
    /// Glossary: occupied / unoccupied / empty (Updated 1 January 2024):
    /// "A ship, facility, or site is occupied if at least one personnel is aboard.
    /// An unoccupied ship, facility, or site is empty."
    /// Equipment or interrupts (e.g. Rogue Borg tokens) aboard do NOT make a host occupied.
    /// </summary>
    public static bool IsOccupied(int personnelCount) => personnelCount > 0;

    public static bool IsOccupied(IEnumerable<Card>? aboard) =>
        aboard != null && aboard.Any(ModifierRules.IsPersonnelCard);

    /// <summary>
    /// Glossary: unoccupied / empty (Updated 1 January 2024):
    /// "An unoccupied ship, facility, or site is empty."
    /// </summary>
    public static bool IsEmpty(int personnelCount) => personnelCount == 0;

    public static bool IsEmpty(IEnumerable<Card>? aboard) =>
        aboard == null || !aboard.Any(ModifierRules.IsPersonnelCard);

    public static bool IsUnoccupied(int personnelCount) => IsEmpty(personnelCount);

    public static bool IsUnoccupied(IEnumerable<Card>? aboard) => IsEmpty(aboard);

    /// <summary>
    /// Glossary: exposed (Updated 1 January 2024):
    /// "A ship is exposed when it is undocked, uncloaked, unphased, and not landed or carried."
    /// </summary>
    public static bool IsShipExposed(
        bool isDocked,
        bool isCloaked,
        bool isPhased = false,
        bool isLanded = false,
        bool isCarried = false) =>
        !isDocked && !isCloaked && !isPhased && !isLanded && !isCarried;

    /// <summary>
    /// Convenience helper for an empty and exposed ship.
    /// </summary>
    public static bool IsEmptyExposedShip(
        bool isShip,
        bool isEmpty,
        bool isExposed) =>
        isShip && isEmpty && isExposed;

    public static bool IsEmptyExposedShip(
        bool isShip,
        int personnelCount,
        bool isDocked,
        bool isCloaked,
        bool isPhased = false,
        bool isLanded = false,
        bool isCarried = false) =>
        isShip && IsEmpty(personnelCount) && IsShipExposed(isDocked, isCloaked, isPhased, isLanded, isCarried);

    /// <summary>
    /// Checks if a ship is owned/controlled by the player ("your ship").
    /// </summary>
    public static bool IsYourShip(bool isShip, int shipOwner, int player) =>
        isShip && (shipOwner == player);

    /// <summary>
    /// Checks if a ship has Tractor Beam special equipment (Text or Characteristics).
    /// </summary>
    public static bool HasTractorBeam(Card? ship)
    {
        if (ship == null) return false;
        if ((ship.Characteristics ?? "").Contains("Tractor Beam", StringComparison.OrdinalIgnoreCase))
            return true;
        return MovementRules.ShipHasSpecialEquipment(ship, "Tractor Beam");
    }

    /// <summary>
    /// Ship Seizure (Premiere 136 C):
    /// "Plays on your ship with Tractor Beam. Discard another empty exposed ship here."
    /// Validates the play-on tractor host.
    /// </summary>
    public static (bool ok, string reason) CanBeShipSeizureTractorHost(
        Card? ship,
        int hostOwner,
        int player)
    {
        if (ship == null || !(ship.Type ?? "").Equals("Ship", StringComparison.OrdinalIgnoreCase))
            return (false, "Ship Seizure: play on your ship with Tractor Beam.");
        if (hostOwner != player)
            return (false, "Ship Seizure: host must be your ship.");
        if (!HasTractorBeam(ship))
            return (false, "Ship Seizure: host must have a Tractor Beam.");
        return (true, "Legal Tractor Beam host.");
    }

    /// <summary>
    /// Ship Seizure (Premiere 136 C):
    /// Validates a potential victim ship to be discarded.
    /// "Discard another empty exposed ship here."
    /// </summary>
    public static (bool ok, string reason) CanBeShipSeizureVictim(
        bool isShip,
        bool isAnotherShip,
        bool sameLocation,
        bool isEmpty,
        bool isExposed)
    {
        if (!isShip)
            return (false, "Ship Seizure: victim must be a ship.");
        if (!isAnotherShip)
            return (false, "Ship Seizure: cannot discard the Tractor Beam ship itself.");
        if (!sameLocation)
            return (false, "Ship Seizure: victim must be at the same location.");
        if (!isEmpty)
            return (false, "Ship Seizure: victim must be an empty ship (no personnel aboard).");
        if (!isExposed)
            return (false, "Ship Seizure: victim must be an exposed ship (undocked, uncloaked, unphased, not landed/carried).");
        return (true, "Legal empty exposed victim ship.");
    }

    /// <summary>
    /// Mini-test validating the ship terms pipeline and Ship Seizure rules.
    /// Returns null if all tests pass, otherwise returns failure explanation.
    /// </summary>
    public static string? VerifyShipRules()
    {
        // 1. Occupied / Empty / Unoccupied
        var pers = new Card { Name = "Data", Type = "Personnel" };
        var equip = new Card { Name = "Phaser", Type = "Equipment" };
        var rogue = new Card { Name = "Rogue Borg", Type = "Interrupt" };

        if (!IsOccupied(1)) return "IsOccupied(1) should be true";
        if (IsOccupied(0)) return "IsOccupied(0) should be false";
        if (!IsEmpty(0)) return "IsEmpty(0) should be true";
        if (IsEmpty(1)) return "IsEmpty(1) should be false";
        if (!IsUnoccupied(0)) return "IsUnoccupied(0) should be true";
        if (IsUnoccupied(1)) return "IsUnoccupied(1) should be false";

        // Equipment or Interrupt alone does not make a ship occupied (Updated 1 Jan 2024)
        if (IsOccupied(new[] { equip })) return "Equipment aboard must not make ship occupied";
        if (!IsEmpty(new[] { equip })) return "Equipment aboard alone must be empty";
        if (IsOccupied(new[] { rogue })) return "Rogue Borg token alone must not make ship occupied";
        if (!IsEmpty(new[] { rogue })) return "Rogue Borg token alone must be empty";
        if (!IsOccupied(new[] { pers, equip })) return "Personnel aboard must make ship occupied";
        if (IsEmpty(new[] { pers, equip })) return "Personnel aboard must not be empty";

        // 2. Exposed
        if (!IsShipExposed(isDocked: false, isCloaked: false, isPhased: false, isLanded: false, isCarried: false))
            return "Undocked, uncloaked, unphased, not landed, not carried must be exposed";
        if (IsShipExposed(isDocked: true, isCloaked: false))
            return "Docked ship must not be exposed";
        if (IsShipExposed(isDocked: false, isCloaked: true))
            return "Cloaked ship must not be exposed";
        if (IsShipExposed(isDocked: false, isCloaked: false, isPhased: true))
            return "Phased ship must not be exposed";
        if (IsShipExposed(isDocked: false, isCloaked: false, isLanded: true))
            return "Landed ship must not be exposed";
        if (IsShipExposed(isDocked: false, isCloaked: false, isCarried: true))
            return "Carried ship must not be exposed";

        // 3. Empty exposed ship
        if (!IsEmptyExposedShip(isShip: true, isEmpty: true, isExposed: true))
            return "Empty exposed ship must pass IsEmptyExposedShip";
        if (IsEmptyExposedShip(isShip: false, isEmpty: true, isExposed: true))
            return "Non-ship must not pass IsEmptyExposedShip";
        if (IsEmptyExposedShip(isShip: true, isEmpty: false, isExposed: true))
            return "Occupied ship must not pass IsEmptyExposedShip";
        if (IsEmptyExposedShip(isShip: true, isEmpty: true, isExposed: false))
            return "Unexposed ship must not pass IsEmptyExposedShip";

        // 4. Your ship
        if (!IsYourShip(isShip: true, shipOwner: 1, player: 1))
            return "Own ship must pass IsYourShip";
        if (IsYourShip(isShip: true, shipOwner: 2, player: 1))
            return "Opponent ship must fail IsYourShip";
        if (IsYourShip(isShip: false, shipOwner: 1, player: 1))
            return "Non-ship must fail IsYourShip";

        // 5. Tractor Beam check
        var galaxy = new Card { Name = "U.S.S. Galaxy", Type = "Ship", Text = "Holodeck, Tractor Beam." };
        var bop = new Card { Name = "Bird-of-Prey", Type = "Ship", Characteristics = "Klingon ship; Tractor Beam;", Text = "Cloaking Device." };
        var shuttle = new Card { Name = "Type 6 Shuttlecraft", Type = "Ship", Text = "" };

        if (!HasTractorBeam(galaxy)) return "Galaxy with Tractor Beam in Text must have Tractor Beam";
        if (!HasTractorBeam(bop)) return "Bird-of-Prey with Tractor Beam in Characteristics must have Tractor Beam";
        if (HasTractorBeam(shuttle)) return "Shuttle without Tractor Beam must not have Tractor Beam";

        // 6. Ship Seizure Tractor Host
        var tractorOk = CanBeShipSeizureTractorHost(galaxy, hostOwner: 1, player: 1);
        if (!tractorOk.ok) return "Galaxy owned by P1 should be legal Tractor host: " + tractorOk.reason;
        var tractorWrongOwner = CanBeShipSeizureTractorHost(galaxy, hostOwner: 2, player: 1);
        if (tractorWrongOwner.ok) return "Opponent ship should fail Tractor host";
        var tractorNoBeam = CanBeShipSeizureTractorHost(shuttle, hostOwner: 1, player: 1);
        if (tractorNoBeam.ok) return "Ship without Tractor Beam should fail Tractor host";

        // 7. Ship Seizure Victim
        var victimOk = CanBeShipSeizureVictim(isShip: true, isAnotherShip: true, sameLocation: true, isEmpty: true, isExposed: true);
        if (!victimOk.ok) return "Valid empty exposed victim at same location should pass: " + victimOk.reason;
        if (CanBeShipSeizureVictim(isShip: false, isAnotherShip: true, sameLocation: true, isEmpty: true, isExposed: true).ok)
            return "Non-ship victim should fail";
        if (CanBeShipSeizureVictim(isShip: true, isAnotherShip: false, sameLocation: true, isEmpty: true, isExposed: true).ok)
            return "Tractor ship itself as victim should fail";
        if (CanBeShipSeizureVictim(isShip: true, isAnotherShip: true, sameLocation: false, isEmpty: true, isExposed: true).ok)
            return "Victim at different location should fail";
        if (CanBeShipSeizureVictim(isShip: true, isAnotherShip: true, sameLocation: true, isEmpty: false, isExposed: true).ok)
            return "Occupied victim should fail";
        if (CanBeShipSeizureVictim(isShip: true, isAnotherShip: true, sameLocation: true, isEmpty: true, isExposed: false).ok)
            return "Unexposed victim should fail";

        // 8. Tachyon Detection Grid & Scan decide rules
        var tachErr = InterruptShipEffectRules.VerifyTachyonDecide();
        if (tachErr != null) return "TachyonDecide failure: " + tachErr;
        var scanErr = InterruptShipEffectRules.VerifyScanDecide();
        if (scanErr != null) return "ScanDecide failure: " + scanErr;

        // 9. Temporal Rift decide rules
        var riftErr = InterruptRules.VerifyTemporalRiftDecide();
        if (riftErr != null) return "TemporalRiftDecide failure: " + riftErr;

        return null;
    }
}
