# STCCG 1E - Handoff

Last updated: 2026-09-18 (Data - Occupancy Badge UX)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: master.** GrokTest nicht nutzen.
**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Gruen-Test.

## Current tip
Local Josef tip **TIPHASH** - Occupancy Badge UX (Pepsch lock). Nicht gepusht. (Prior tip a866bbe Distortion Field Pepsch green.)
**Pepsch EXE (Default Debug):**
`C:\Dev\StarTrekCCG\StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`
Nicht `_build_docky*` / Release / alte Side-Builds.

## Tip detail (Data, Josef, 2026-09-18) - Occupancy Badge UX
Captain/Spock Soll-OK. UX only (Host footer). Standing Practice: personnel-present chrome; no glow/split.
### Occupancy Badge (Pepsch lock)
- Host footer badges in P1 (cyan) / P2 (orange) color.
- Planet mission + personnel -> label `Away Team`; Ship/Outpost/Station (facility) + personnel -> `Crew`.
- Empty (no personnel) = no badge. Eq/Art/Event/Dilemma = detail only, no badge.
- Both players occupied = two badges side by side (same footer row).
- KEEP: Rogue Borg notice on badge when present; Distortion Field / prior tips untouched.
Files: TableWindow.xaml.cs (UpdateHostBadge / EnsureSideBadge / selection frame), artifacts/FEATURES.md (already ACTIVE), artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe
### Pepsch smoke
1. Planet mission: beam/report P1 personnel onto planet -> footer `Away Team` in P1 color; remove all personnel -> badge gone.
2. Same planet: add P2 personnel too -> two badges side by side (`Away Team` each color); Eq alone on planet -> still no occupancy badge.
3. Ship or Outpost/Station with crew -> footer `Crew` in owner color; dual crew both sides -> side by side; no card-glow / diagonal split.

## Tip detail (Data, Josef, 2026-09-18) - Distortion Field (PR 70 U)
Captain/Spock Soll-OK. Standing Practice Glossary cites. CODE_PLACEMENT: Events in Rules; UI wire.
### Distortion Field (PR 70 U) - EN Distortion
- Unique (IsPrintedUniqueEvent / IsDistortionField); Plays on Planet; enters play **FACE UP** (blocks immediately); first EOT -> face-down.
- EOT each turn: flip (even while face-down) via EndOfTurnEventRules.ShouldFlipDistortion.
- Face-up: prevents ALL beaming to/from this planet incl. planet-vicinity (landed <-> facility); same-mission gate as Atmospheric Ionization.
- Face-down: beaming allowed.
- Fix: FormatHostEffectSummary was Interrupt confusion ("RANGE may be used to unstop") -> beaming block summary; DetailStatus Debuff (not Buff).
- Verify: EventRules.VerifyDistortionField + EndOfTurnEventRules.VerifyDistortionFlip.
- KEEP: Atmospheric Ionization (5c08269) untouched / no regress.
Files: Game/EventRules.cs, Game/DetailStatusRules.cs, Game/EndOfTurnEventRules.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Alien Probe + Atmospheric Ionization + IPG UX
Captain/Spock Soll-OK. Standing Practice Glossary cites folded in. CODE_PLACEMENT: Events in Rules; UI wire.
### IPG Detail-Overkill (UX only)
Armus detail showed 3× IPG: green Icons:[IPG] (DetailAttributes), glyph (IconCatalog.Fill), purple Icons:[IPG] (DetailIcons fallback).
**Removed 2:** green Attributes Icons-line + purple DetailIcons Icons-fallback. **Kept:** glyph strip. Cmd/Staffing untouched (FillStaffing).
### Alien Probe (PR 66 U)
- Plays on table (Persist.Probe); continuous both hands revealed (HasAlienProbeInPlay → hand strip).
- Hand cards not nullifiable until played (CanNullifyTargetCard in NullifyEventInPlay).
- Battle Bridge / used tactics NOT affected (faceDownAlways stays).
- Verify: EventRules.VerifyAlienProbe.
### Atmospheric Ionization (PR 68 C) — EN Ionization
- Unique (IsPrintedUniqueEvent); Plays on Planet; beam 1 at a time; max 3 personnel this way per controller per turn.
- Glossary-Add: to/from this planet includes planet-vicinity beams (landed ship ↔ planet facility); same-mission gate covers.
- Count increments only after successful beam (NoteIonizationBeam); per-player save fields.
- Verify: EventRules.VerifyAtmosphericIonization.
Files: Game/EventRules.cs, Game/DetailStatusRules.cs, Game/EffectRegistry.cs, Services/GameSave.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Temporal Causality Loop (Glossary-treu)
Captain/Pepsch Implement-Go. Lock: Glossary-true (NOT Seeds-only). Standing Practice rule cites folded in.
- Decide (`DilemmaRules.Loop`): SCIENCE + CUNNING>35 → Overcome +5; else EffectAndEnd + EndTurn + StopTeam (no +5).
- Apply (TW): `_attemptDiscards` log (order+origin+seedOrderHint) from attempt start; holes closed (RemoveEquipmentFromHost, DestroyShipOrFacility, SeniorStaff, IpgNullify, DevilNullify, OvercomeSeed Zone-truth A via discard).
- Fail restore: seeds face-down Encounter-Order (`ReseedInsertIndex`); non-seeds re-play host / legal report / stay discarded; TCL not re-seeded; Attach*/WallFailed untouched.
- EndTurn: `_skipNormalEndOfTurn` skips normal EOT (Compendium 8 / _rb69).
- Verify: `_VerifyTemporal` → `DilemmaRules.VerifyTemporalCausalityLoop`.
- Docs: CODE_PLACEMENT + ENGINE Standing Practice (rule cites); CARD_TRACKER partial until Pepsch green.
Files: `Game/DilemmaRules.cs`, `TableWindow.xaml.cs`, `_VerifyTemporal/*`, `artifacts/CODE_PLACEMENT.md`, `artifacts/ENGINE.md`, `artifacts/CARD_TRACKER.md`, `artifacts/HANDOFF.md`.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Foundation first-listed skill (Classification skip)
Captain Foundation-Fix-Go / Spock Rules-OK: first-listed skill ≠ classification box.
- Root cause: `FirstListedSkill` treated Lackey leading class token in `text` as first skill (Data OFFICER→wrong).
- Fix shared parse: skip leading `Card.Class` echo(s); next skill (multi-word/xN) = first-listed. Assimilation: Class mismatch → former class token is first-listed.
- Apply: strip that skill (multipliers together); restore printed classification if same-named (Bashir MEDICAL x2). Second skill does not slide up.
- ALL first-listed consumers already use `MissionRules.FirstListedSkill` / `ApplyFirstListedSkillLoss` (Tsiolkovsky Apply+Summary).
- Verify: Data→ENGINEER gone / OFFICER stays; Seskal SCIENCE; Bashir MEDICAL class remains; Sci Physics; cure 3 MEDICAL; not-cumulative.
- KEEP ship Events Positive-only from 948cf0f (untouched).
Files: `Game/MissionRules.cs`, `Game/ModifierRules.cs`, `Game/DilemmaRules.cs`. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Tsiolkovsky Infection Apply + Summary + Events UX
Captain Fix-Go / Spock: AttachContinue + Cure 3 MEDICAL + no StopTeam already OK.
- Bug Summary: `FormatHostEffectSummary` showed attributes -3 (wrong).
- Apply-Gap: `DisabledSkillsOnHost` only TwoDim Empathy — no first-listed strip for Tsiolkovsky.
- Fix Apply: `MissionRules.FirstListedSkill` + `ModifierRules.ApplyFirstListedSkillLoss` (not cumulative); TW `HostHasTsiolkovsky` / `LoseFirstListedOnHost` wired into Resolve/Summarize/CanSolve/Ctx.
- Fix Summary: host effect = personnel lose first-listed skill (cure: 3 MEDICAL).
- VerifyTsiolkovskyInfection: apply + summary + not-cumulative OK.
- UX: ship detail Events only under Positive (never under Personnel). Debuff Events stay Negative.
Files: `Game/MissionRules.cs`, `Game/ModifierRules.cs`, `Game/DilemmaRules.cs`, `TableWindow.xaml.cs`. CARD_TRACKER partial until Pepsch green. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Tarellian Step0 display polish
Captain Fix-Go / Pepsch: Logic A/B OK; Bug = dilemmacard missing in Step0 Choose dialog top-left.
- Cause: dilemma `PickYou`/`PickOpp` called `PickCardFromList` without `source`; synthetic Choice cards have no art -> empty slot
- Fix: pass `seedCard` as source (same as `AskChoice` / other card-choice dialogs). Display only; Rules/Picker flow unchanged.
File: `TableWindow.xaml.cs`. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Tarellian Overcome UX A/B
Captain Fix-Go / Pepsch Soll: two entry points, NOT one flat pool. Rules (Spock) unchanged.
- Schritt 0: A) Medical Personnel OR B) Equipment + Personnel (Choice cards)
- Path A: encountering crew filtered to printed usable MEDICAL (Class OR Skill); beam/sacrifice; no equipment discard
- Path B: MEDICAL-granting eq only (Medical Kit, Medical Tricorder via SkillEquipment); then personnel matching RequiredClass (Kit->OFFICER, Medical Tricorder->SCIENCE); person+eq discard +5
- Plain Tricorder still no MEDICAL / not offered
- VerifyTarellian: Path A then Path B (+ Kit-only, Tricorder class-filter, plain fail)
Files: `Game/DilemmaRules.cs`, `Game/ModifierRules.cs` (EquipmentRequiredClassForSkill). TW untouched. CARD_TRACKER partial until Pepsch green. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Warum 4a61fa1 falsch war
Pepsch meinte VERTIKALE Linie, nicht horizontal. 4a61fa1 (X-Stagger, Y-Baseline) war Missverstaendnis. SOLL jetzt: eine saubere Spalte unter (P1) / ueber (P2) der Spaceline; N Schiffe nur via `DockSlotOffsetY(slot)`; Left = mission-centered (`DockSlotOffsetX(0)`).

## Pipeline (zwei Straenge, nie im selben Commit)

### Sofort -- Pepsch smoke
- Response Window UX (Silent Badge, [R] Think Tray, [Space] Pass, Presets)
- Artifact Beaming: Varon-T Planet auf Schiff ohne Treaty-Fehler
- **Dock vertikal** tip: Ships+Outposts gleiche X-Spalte, Y-Slots; Load+live Relayout gleich

### Strang A -- Premiere-Dilemmas (ACTIVE, Pause)
Pause bei **#26 Q** bis Captain Go. REM Fatigue bleibt parked.
Next unknown: Q, Radioactive Garbage Scow, Rebel Encounter, (REM Fatigue skip), Sarjenka, Shaka (TCL tip landed — Pepsch smoke). Reminder: Tsiolkovsky/Two-Dim/Wind Dancer already green.
Done prior: **Tarellian Plague Ship** Pepsch green; **Tsiolkovsky** Foundation tip; **Temporal Causality Loop** Data Implement-Go (partial, tip e88860e).

### Strang B -- Welle 2 Extract (artifacts/EXTRACT_REST.md)
Welle 1 Slices 1-9 DONE. Naechstes Ticket wenn Captain Go: **P0-D1 + P0-E1**.

### Spaeter -- Netz (nach P0)
BoardStore Persist-Wahrheit zuerst. Localhost zwei Exes.

## Parked
Hugh Borg Ship; IM FindMission; dump@Gaps; Parasites Hotseat-UI; REM Fatigue; Cure-Present-Scope Ship

## Docs map
HANDOFF, PROJECT, ENGINE, CODE_PLACEMENT, EXTRACT_REST, FEATURES, CARD_TRACKER, CHANGELOG

## Tip detail (Data, Josef, 2026-09-18) - dock vertical correction
Root: 4a61fa1 X-cascade war falsch (Pepsch misspoke "horizontal").
Fix:
- `DockSlotOffsetY(slot)` wieder: Y-Stufen; `DockSlotOffsetX` = 0 (mission-centered)
- Relayout / Relocate / Snap-Preview / RelayoutAll: Top = missionTop + DockSlotOffsetY(i); Left = missionLeft
- Column-Tolerance 45; Z steigt mit Slot; PinÃƒÂ¢Ã‹â€ Ã‚ÂªPixel Membership; CountDockablesForOwner
- KEEP Load-Pfad 8c88b2d/056f6b0: X-Pin, Relayout after settle, Top nie Save-Y+offset, Pins clear, SpacelineYDefault
File: StarTrekCCG/TableWindow.xaml.cs. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Prior tip (056f6b0) load settle
PinDockablesToSpacelineByColumn + Relayout after UpdateLayout + ScheduleRelayoutAfterLoadSettle; Top immer missionTop+DockSlotOffsetY, nie Save-Y.