from pathlib import Path

cl = Path("artifacts/CHANGELOG.md")
text = cl.read_text(encoding="utf-8")
entry = '''# Changelog

Nur spielbare / engine-relevante Schritte. Keine Chat-Metadaten.

---

## 2026-09-07 (Fix - Alien Parasites Neg control min path)

**Engine** - Alien Parasites Neg (Pepsch/Spock Soll): Fail INT<=32 still WallFailed + Stop + planet Beam-back; Opp chooses Away Team and/or **one** ship+crew here (not all ships); Controller transfer; Opp acts on their turn; restore at Opp EOT (= start of victim next turn). Allowed: legal actions with controlled cards. PARK: dual-window Hotseat chooser UI; deep "not compatible with Opp other cards" affiliation-mix enforcement. Decide: `DilemmaRules.DecideAlienParasites` + `GrantOpponentControl` + `ParseAlienParasitesControlChoice` + `ShouldRestoreAlienParasitesControl` + `VerifyAlienParasites1a`. Apply: TW `BeginAlienParasitesOpponentControl` / `RestoreAlienParasitesControlsIfDue`.

---
'''
# replace header
if not text.startswith("# Changelog"):
    raise SystemExit("bad changelog")
# insert after first ---
parts = text.split("---", 2)
if len(parts) < 3:
    raise SystemExit("changelog structure")
# parts[0] = "# Changelog\n\nNur...\n\n"
# Keep intro, then new entry, then rest after first ---
intro = '''# Changelog

Nur spielbare / engine-relevante Schritte. Keine Chat-Metadaten.

---

## 2026-09-07 (Fix - Alien Parasites Neg control min path)

**Engine** - Alien Parasites Neg (Pepsch/Spock Soll): Fail INT<=32 still WallFailed + Stop + planet Beam-back; Opp chooses Away Team and/or **one** ship+crew here (not all ships); Controller transfer; Opp acts on their turn; restore at Opp EOT (= start of victim next turn). Allowed: legal actions with controlled cards. PARK: dual-window Hotseat chooser UI; deep "not compatible with Opp other cards" affiliation-mix enforcement. Decide: `DilemmaRules.DecideAlienParasites` + `GrantOpponentControl` + `ParseAlienParasitesControlChoice` + `ShouldRestoreAlienParasitesControl` + `VerifyAlienParasites1a`. Apply: TW `BeginAlienParasitesOpponentControl` / `RestoreAlienParasitesControlsIfDue`.

---
'''
rest = text.split("---", 2)[2]
# rest starts with newline + next section
cl.write_text(intro + rest, encoding="utf-8")
print("CHANGELOG OK")

ct = Path("artifacts/CARD_TRACKER.md")
ctext = ct.read_text(encoding="utf-8")
ctext = ctext.replace(
    "Last updated: 2026-09-07 (Jadzia — Parasites Control-Scope + Hyper-Aging Quarantäne Fix laufend)",
    "Last updated: 2026-09-07 (Jadzia — Alien Parasites Neg control min path)",
    1)
old_row = "| Alien Parasites (11 U) | partial | Pos OK. Neg Control: Data nachziehen (Scope AT und/oder 1 Schiff+Crew, Timing Opp-Zug). | Pepsch + Spock 2026-09-07 |"
new_row = "| Alien Parasites (11 U) | partial | Pos OK. Neg Control min path: Opp wählt AT und/oder 1 Schiff+Crew; Control bis Opp-EOT. PARK: Hotseat-UI dual-window; deep affiliation-mix. Pending Pepsch green. | Josef tip (pending SHA) 2026-09-07 |"
if old_row not in ctext:
    # try fuzzy
    import re
    m = re.search(r"\| Alien Parasites \(11 U\) \|.*\|", ctext)
    print("row found:", m.group(0) if m else None)
    raise SystemExit("CARD_TRACKER row not found")
ctext = ctext.replace(old_row, new_row, 1)
ct.write_text(ctext, encoding="utf-8")
print("CARD_TRACKER OK")
