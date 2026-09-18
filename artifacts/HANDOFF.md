# STCCG 1E - Handoff

Last updated: 2026-09-18 (Data -- load dock-Y settle; unstaged tip for Pepsch)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: master.** GrokTest nicht nutzen.
**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Gruen-Test.

## Current tip
Local Josef tip **056f6b0** — Fix: Load Dock-Y settle Relayout + X-Pin. Nicht gepusht.
**Pepsch EXE (Default Debug, gerade gebaut):**
`C:\Dev\StarTrekCCG\StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`
Nicht `_build_docky*` / Release / alte Side-Builds.

## Warum 8c88b2d bei Pepsch nicht sichtbar
1) **Bin-Pfad:** Fix lag in `_build_docky2` (12:49); Default-Debug war teils noch stale bis Rebuild. Side-Build ist nicht der VS-F5/Doppelklick-Pfad.
2) **Code noch lueckenhaft:** 8c88b2d pinnte nur Snapshot aus GetDockables (Pixel-Y). Wenn Save-Y ausserhalb Fenster -> leerer Snapshot -> nach Mission->SpacelineY bleiben Docks auf Save-Y (Y-Versatz). Kein Relayout nach Layout-Settle.

## Pipeline (zwei Straenge, nie im selben Commit)

### Sofort -- Pepsch smoke
- Response Window UX (Silent Badge, [R] Think Tray, [Space] Pass, Presets)
- Artifact Beaming: Varon-T Planet auf Schiff ohne Treaty-Fehler
- **Load Dock-Y** tip **056f6b0** (nach 8c88b2d / 6b6033f): gerade Baseline P1 unten / P2 oben

### Strang A -- Premiere-Dilemmas (ACTIVE, Pause)
Pause bei **#26 Q** bis Captain Go. REM Fatigue bleibt parked.
Next unknown: Q, Radioactive Garbage Scow, Rebel Encounter, (REM Fatigue skip), Sarjenka, Shaka, Tarellian Plague Ship, Temporal Causality Loop, Tsiolkovsky Infection, Two-Dimensional Creatures, Wind Dancer.

### Strang B -- Welle 2 Extract (artifacts/EXTRACT_REST.md)
Welle 1 Slices 1-9 DONE. Naechstes Ticket wenn Captain Go: **P0-D1 + P0-E1**.

### Spaeter -- Netz (nach P0)
BoardStore Persist-Wahrheit zuerst. Localhost zwei Exes.

## Parked
Hugh Borg Ship; IM FindMission; dump@Gaps; Distortion; Parasites Hotseat-UI; REM Fatigue; Cure-Present-Scope Ship

## Docs map
HANDOFF, PROJECT, ENGINE, CODE_PLACEMENT, EXTRACT_REST, FEATURES, CARD_TRACKER, CHANGELOG

## Tip detail (Data, Josef, 2026-09-18) — 056f6b0
Root cause: Load orphaned docks when pixel Y-window missed save Y; plus Pepsch often not on Default Debug exe.
Fix:
- PinDockablesToSpacelineByColumn (X only) before Relayout
- RelayoutMissionsOnSpaceline + UpdateLayout + second Relayout + RelayoutAllDockables
- ScheduleRelayoutAfterLoadSettle (Dispatcher Loaded)
- Top = missionTop + DockSlotOffsetY only (never save Y + offset)
File: StarTrekCCG/TableWindow.xaml.cs. Build: 0 errors. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe
