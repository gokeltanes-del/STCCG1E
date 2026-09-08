from pathlib import Path

# CHANGELOG
cl = Path("artifacts/CHANGELOG.md")
text = cl.read_text(encoding="utf-8")
intro = '''# Changelog

Nur spielbare / engine-relevante Schritte. Keine Chat-Metadaten.

---

## 2026-09-07 (Fix - Hyper-Aging quarantine leave/beam block)

**Engine** - Hyper-Aging (PR 28 U): AT quarantined on place (AttachAndContinue, not stopped); no Leave/Beam away; anyone who joins the host is quarantined; cure SCIENCE + 2 MEDICAL before countdown 0 else Kill (inorganics exempt, existing). Status UX like Stasis leave-block. `LegalMoves` skips Beam from `QuarantineLeaveBlocked` hosts. Decide: `DilemmaRules.IsQuarantinePersist` / `IsLeaveBlockedPersist` + `VerifyHyperAgingQuarantine`. Apply: TW Held on attach, `IsCardLeaveBlocked` / `TryJoinQuarantineOnHost`, BoardPiece `QuarantineLeaveBlocked`. RemFatigue quarantine PARK (out of scope).

---
'''
rest = text.split("---", 2)[2]
cl.write_text(intro + rest, encoding="utf-8")
print("CHANGELOG OK")

ct = Path("artifacts/CARD_TRACKER.md")
ctext = ct.read_text(encoding="utf-8")
ctext = ctext.replace(
    "Last updated: 2026-09-07 (Jadzia — Alien Parasites Neg control min path)",
    "Last updated: 2026-09-07 (Jadzia — Hyper-Aging quarantine leave/beam + Parasites Neg f087866)",
    1)
# Parasites row - add tip SHA
ctext = ctext.replace(
    "| Alien Parasites (11 U) | partial | Pos OK. Neg Control min path: Opp wählt AT und/oder 1 Schiff+Crew; Control bis Opp-EOT. PARK: Hotseat-UI dual-window; deep affiliation-mix. Pending Pepsch green. | Josef 2026-09-07 |",
    "| Alien Parasites (11 U) | partial | Pos OK. Neg Control min path (tip Josef/`f087866`): Opp wählt AT und/oder 1 Schiff+Crew; Control bis Opp-EOT. PARK: Hotseat-UI dual-window; deep affiliation-mix. Pending Pepsch green. | Josef/`f087866` 2026-09-07 |",
    1)
old_ha = "| Hyper-Aging (28 U) | partial | Pos+Cure+Kill OK. Quarantäne+Beam-Block Fix läuft (Data). | Pepsch + Spock 2026-09-07 |"
new_ha = "| Hyper-Aging (28 U) | partial | Pos+Cure+Kill OK. Quarantäne leave/beam block + joiners (tip Josef pending). RemFatigue quarantine PARK. Pending Pepsch green. | Josef 2026-09-07 |"
if old_ha not in ctext:
    import re
    m = re.search(r"\| Hyper-Aging \(28 U\) \|.*\|", ctext)
    print("HA row:", m.group(0) if m else None)
    raise SystemExit("HA row missing")
ctext = ctext.replace(old_ha, new_ha, 1)
ct.write_text(ctext, encoding="utf-8")
print("CARD_TRACKER OK")
