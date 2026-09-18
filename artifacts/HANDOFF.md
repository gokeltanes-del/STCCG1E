# STCCG 1E - Handoff

Last updated: 2026-09-18 (Data -- Tarellian Overcome UX Equipment-then-Person)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: master.** GrokTest nicht nutzen.
**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Gruen-Test.

## Current tip
Local Josef tip **4575df4** - Fix: Tarellian Overcome UX (Equipment-then-Person for MEDICAL Kit/Tricorder). Nicht gepusht.
**Pepsch EXE (Default Debug):**
`C:\Dev\StarTrekCCG\StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`
Nicht `_build_docky*` / Release / alte Side-Builds.

## Tip detail (Data, Josef, 2026-09-18) - Tarellian Overcome UX Nacharbeit
Captain Fix-Go / Pepsch Gap (Spock): Overcome-Picker UX only â€” Rules unveraendert.
- Step 1: MEDICAL-granting Equipment present (ModifierRules SkillEquipment catalog: Medical Kit, Medical Tricorder; plain Tricorder NOT MEDICAL)
- Step 2: Person aus full encountering crew; usable MEDICAL via TryUsableMedicalOnArrival (Kit via Present)
- Kit+OFFICER / Medical Tricorder+SCIENCE: beide discard +5; Fail kill unchanged
- VerifyTarellian: kit + tricorder + plain-Tricorder-fail extended
Files: `Game/DilemmaRules.cs`, `Game/ModifierRules.cs` (EquipmentGrantsSkill query). TW untouched. Not pushed.
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
Done this tip: **Tarellian Plague Ship** (Pepsch hybrid Glossary beam + Premiere +5).

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
- Column-Tolerance 45; Z steigt mit Slot; PinÃ¢Ë†ÂªPixel Membership; CountDockablesForOwner
- KEEP Load-Pfad 8c88b2d/056f6b0: X-Pin, Relayout after settle, Top nie Save-Y+offset, Pins clear, SpacelineYDefault
File: StarTrekCCG/TableWindow.xaml.cs. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Prior tip (056f6b0) load settle
PinDockablesToSpacelineByColumn + Relayout after UpdateLayout + ScheduleRelayoutAfterLoadSettle; Top immer missionTop+DockSlotOffsetY, nie Save-Y.