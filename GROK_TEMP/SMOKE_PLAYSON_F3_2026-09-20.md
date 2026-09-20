# F3 Premiere Smoke-Matrix — Plays on/as Big-Bang
Stand: 2026-09-20 · Data · nach F0–F2 tips
EXE: `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`
Tips: F0 `5edbc6e` · F1 `548e573` · F2 `7719373`

Pepsch: bitte je Zeile GREEN / FAIL + Kurznotiz. Jadzia Tracker erst nach GREEN.

| # | Karte | Rolle | Soll-Verhalten | Smoke |
|---|-------|-------|----------------|-------|
| 1 | Vulcan Stone of Gol | ArtifactAsEvent | Drag: Halo nur Planet mit Away Team (kein Schiff/Crew). Drop auf Planet → Kill !Youth && CUNNING≤7; Discard. | |
| 2 | Kurlan Naiskos | ArtifactAsEvent | Snap any ship; Attach; RANGE/WEAPONS/SHIELDS ×3 wenn 7 Classifications. | |
| 3 | Kevin vs Kurlan | NativeInterrupt → ArtifactAsEvent | Kurlan am Schiff ist legales Kevin-Ziel (nicht nur printed Event). | |
| 4 | Kevin vs Tox (table) | ArtifactAsEvent | Tox on table nullifybar wie Event (wenn in play). | |
| 5 | Interphase Generator | ArtifactAsEquipment | Acquire Use-as Equipment → Attach; IPG-Dilemma nullify wenn present. | |
| 6 | Varon-T Disruptor | ArtifactAsEquipment | Acquire Use-as Equipment → Attach. | |
| 7 | Horga'hn | ImmediateTable | Immediately on table; extra play / EOT flag. | |
| 8 | ETA / Disruptor Overload | NativeInterrupt dual | Snap Crew **oder** Away Team; Disruptor ExcludeFacility. | |
| 9 | Alien Groupie | NativeInterrupt AwayTeam | Nur Planet-AT; kein Crew. | |

## Automatik (bereits in Code)
- `PlayOnRules.VerifyAwayTeamCrewSplit` — F0+F1 Stone/Groupie/ETA/Disruptor/Kurlan Role
- `TimingRules.IsEventEquivalentForKevin` — F2

## PARK
AU Artifact-as-Event; Tox dual Interrupt-Mode polish; TW restliche Name-if Effekte (Thought Maker etc.)
