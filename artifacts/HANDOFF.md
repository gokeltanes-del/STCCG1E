# STCCG 1E - Handoff

Last updated: 2026-09-08 (Captain Grok — nur noch master)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: `master`.** Branch `GrokTest` ist tot / nicht mehr nutzen (Pepsch 2026-09-08).
Captain Grok committet direkt auf `master`. Pepsch bleibt in VS auf master, holt mit Pull die neuen Dateien.
Lokale GrokBots: immer diese Datei zuerst lesen.

## Team
- Captain — dieses Grok-Chat (Ziele + HANDOFF/PROJECT/CHANGELOG/ENGINE). Pepsch = Mensch.
- Data — Engine / Klasse C (BoardStore/GameState/EngineAuthority/LegalMoves + TW-Extract). In diesem Chat darf Captain Data-Slices bauen.
- Spock — Regeln A/B + Karten text + Glossary Ist/Soll
- Seven — RULES_CHECKLIST + FEATURES; mit Spock volles Glossary/Compendium
- Jadzia Dax — CARD_TRACKER (Premiere+AU)

## On EVERY new chat / Bot
1. Diese HANDOFF.md ganz lesen
2. artifacts/PROJECT.md + Spezial-Docs
3. Data: ENGINE.md, CODE_PLACEMENT.md, TABLEWINDOW_INVENTORY.md
4. Spock/Seven: RULES.md, RULES_CHECKLIST.md, Compendium/Glossary
5. `git fetch` + `git log -5 --oneline master` — nur master
6. Unklar → Captain fragen
7. Kein fremdes Engine-WIP anfassen
8. Mit Pepsch: kurz, Deutsch (Schritt, Klasse, Dateien, bewusst nicht)

## Current tip (2026-09-08 Abend)
**master** war `8b4162b` (FEATURES Impassable Door DONE), plus dieser Docs-Commit.
Kein C#-Change in diesem Commit.

Letzter Engine-Stand auf master:
- Impassable Door `be5062b` — Pepsch **GRÜN** / working
- Hyper-Aging quarantine `46eab15` — Pepsch **GRÜN** / working
- Alien Parasites Neg-Control `f087866` — **partial**, Pepsch-Grün fehlt. Hotseat-Chooser PARK
- Status-UX / Debuff-Gruppierung — **DONE** 2026-09-06 (`bf1f2ab` + Cloak 0.45). Optional: Cloak noch transparenter; Mission-Button last-revealed PARK

### ACTIVE
Premiere-Dilemmas einzeln. Persist/Battle-Extract **liegt**. Kein Big-Bang-TW-Split.

### Pending Pepsch
- Alien Parasites (`f087866`) Neg-Control min path — Retest
- Viele Premiere-Dilemmas `partial` / ungetestet (Anaphasic … Portal Guard)

### Parked
1. Hugh Borg Ship Dilemma branch
2. IM FindMissionForDockable false already-at-facility
3. Engine dump lässt Schiffe auf Gaps weg
4. Distortion (kein AU zum Test)
5. Alien Parasites Hotseat-Chooser / volle Opp-Control-UI
6. REM Fatigue Welle
7. Cure-Present-Scope (Ship) — Menthar/Junior/Ktarian
8. Response Window UX (später)

## Foundation
Board 0–6 + Engine E1–E6 COMPLETE. Dual-run BoardStore.

## TableWindow extract
Welle 1 Slices 1–9 DONE. Persist/Battle weiter deferred. Neue Premiere-Karte nur nach Captain Go.

## Code placement
CODE_PLACEMENT.md — Decide in Rules, Apply in TableWindow, Board = Ort/Status.

## Fix protocol
Klasse A Karte / B Phrase / C Grundlage. Lookup: Checklist → Glossary → Temp Rulings → App A → App B.
Ein Chat ≈ ein Schritt. CHANGELOG eine Zeile pro spielbarer Änderung.

## Workflow Pepsch (VS) — nur master
1. In VS unten links muss **master** stehen.
2. **Git → Pull** (neue Docs/Code von GitHub holen).
3. Bauen / testen.
4. Eigene lokale Änderungen: **Git → Changes → Commit** + Push, oder Captain sagen was lokal ist.
5. **GrokTest nicht auschecken.** Dialog „3 Dateien ohne Commit“: Abbrechen. Nicht wechseln.
6. GrokTest auf GitHub löschen: Repo → Branches → GrokTest → Papierkorb.

## Goals
### Kurz
1. Nur master
2. Premiere-Dilemmas einzeln (Parasites grün oder als Nächstes Q)
3. Seven+Spock Glossary vs Code
4. Bei jeder Code-Änderung: HANDOFF + FEATURES + CARD_TRACKER + CHANGELOG

### Lang
BoardStore+GameState Wahrheit; TableWindow View; Premiere dann Sets; spaeter Netz+KI; privat nicht-kommerziell

## Docs map
HANDOFF (diese Datei) · PROJECT · ENGINE · CODE_PLACEMENT · TABLEWINDOW_INVENTORY · RULES · RULES_CHECKLIST · FEATURES · CARD_TRACKER · CHANGELOG
