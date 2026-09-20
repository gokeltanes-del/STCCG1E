# F3 Premiere Smoke-Matrix — Plays on/as Big-Bang
Stand: 2026-09-20 · Data · Captain F3 Go
EXE: `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`
Engine: F0 `5edbc6e` · F1 `548e573` · F2 `7719373` · F3-docs this tip

Pepsch: je Zeile GREEN / FAIL + Kurznotiz. Jadzia Tracker erst nach GREEN.

## Captain-Pflicht (5)

| # | Check | Soll | Smoke |
|---|-------|------|-------|
| 1 | **Stone of Gol** | Planet-AT Snap/Halo (kein Crew/Schiff). Kill `!Youth && CUNNING≤7` + Discard. **Kevin auf Stack**, solange als Event resolved wird. | |
| 2 | **Kurlan Naiskos** | Snap **any ship**; Attach; RANGE/W/S ×3 bei 7 Classifications. **Kevin nullify** attached Artifact-as-Event. | |
| 3 | **IG / Varon-T** | Use-as Equipment → Attach (Acquire). IG: IPG-Dilemma nullify wenn present. | |
| 4 | **Horga'hn** | ImmediateTable — immediately on table; extra play / EOT. | |
| 5 | **ETA / Disruptor Overload** | Crew **oder** Away Team (F0 dual). Disruptor: ExcludeFacility. | |

## Extra (Regression)

| # | Karte | Soll | Smoke |
|---|-------|------|-------|
| 6 | Alien Groupie | Nur Planet-AT; kein Crew | |
| 7 | Kevin vs Tox (table) | Tox on table nullifybar wie Event | |

## Auto (Code)
- `PlayOnRules.VerifyAwayTeamCrewSplit` (F0+F1)
- `TimingRules.IsEventEquivalentForKevin` (F2)

## PARK
AU Artifact-as-Event; Tox Interrupt-Mode polish; Thought Maker Name-if Effekt
