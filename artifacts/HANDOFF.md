# STCCG 1E - Handoff

Last updated: 2026-09-18 (Data -- dock horizontal baseline; no Y cascade)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: master.** GrokTest nicht nutzen.
**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Gruen-Test.

## Current tip
Local Josef tip **4a61fa1** - Fix: Dockables horizontale Linie (X-Stagger, Y-Baseline). Nicht gepusht.
**Pepsch EXE (Default Debug, gerade gebaut):**
`C:\Dev\StarTrekCCG\StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`
Nicht `_build_docky*` / Release / alte Side-Builds.

## Warum diagonal / Smoke fail (vor diesem Tip)
6b6033f / 8c88b2d / 056f6b0 nutzten `DockSlotOffsetY(slot)` -> Y-Cascade (diagonale Tuerme). SOLL: eine gerade horizontale Linie pro Seite (konstantes Y), N Schiffe nur via `DockSlotOffsetX`.

## Pipeline (zwei Straenge, nie im selben Commit)

### Sofort -- Pepsch smoke
- Response Window UX (Silent Badge, [R] Think Tray, [Space] Pass, Presets)
- Artifact Beaming: Varon-T Planet auf Schiff ohne Treaty-Fehler
- **Dock horizontal** tip **4a61fa1**: Ships+Outposts gleiche Y-Baseline, X side-by-side; Load+live Relayout gleich

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

## Tip detail (Data, Josef, 2026-09-18) - 4a61fa1
Root: `DockSlotOffsetY(i)` in Relayout/Relocate -> diagonale/vertikale Stacks (Pepsch SOLL = horizontale Linie).
Fix:
- `DockSlotOffsetY`: konstante Baseline pro Seite (slot ignoriert); multi-ship nur `DockSlotOffsetX(i*18)`
- Relayout / Relocate / Snap-Preview: Top = missionTop + DockSlotOffsetY(0); Left += X-Stagger; Z steigt weiter
- Column-Tolerance 120 fuer X-Stagger; Load-Settle-Pfad unveraendert (gleiche Regel)
File: StarTrekCCG/TableWindow.xaml.cs. Build: 0 errors. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Prior tip (056f6b0) load settle
PinDockablesToSpacelineByColumn + Relayout after UpdateLayout + ScheduleRelayoutAfterLoadSettle; Top immer missionTop+DockSlotOffsetY, nie Save-Y.
