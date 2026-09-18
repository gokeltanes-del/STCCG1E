# STCCG 1E - Handoff

Last updated: 2026-09-18 (Data -- Tsiolkovsky Apply+Summary + ship Events Positive)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: master.** GrokTest nicht nutzen.
**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Gruen-Test.

## Current tip
Local Josef tip **948cf0f** - Fix: Tsiolkovsky first-listed skill apply+summary; ship Events under Positive. Nicht gepusht.
**Pepsch EXE (Default Debug):**
`C:\Dev\StarTrekCCG\StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`
Nicht `_build_docky*` / Release / alte Side-Builds.

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
Next unknown: Q, Radioactive Garbage Scow, Rebel Encounter, (REM Fatigue skip), Sarjenka, Shaka, Temporal Causality Loop, Tsiolkovsky Infection, Two-Dimensional Creatures, Wind Dancer.
Done prior: **Tarellian Plague Ship** Pepsch green (incl. Step0 003f811). Fix-Go tip: **Tsiolkovsky Infection** Apply+Summary + ship Events Positive UX.

### Strang B -- Welle 2 Extract (artifacts/EXTRACT_REST.md)
Welle 1 Slices 1-9 DONE. Naechstes Ticket wenn Captain Go: **P0-D1 + P0-E1**.

### Spaeter -- Netz (nach P0)
BoardStore Persist-Wahrheit zuerst. Localhost zwei Exes.

## Parked
Hugh Borg Ship; IM FindMission; dump@Gaps; Distortion; Parasites Hotseat-UI; REM Fatigue; Cure-Present-Scope Ship

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