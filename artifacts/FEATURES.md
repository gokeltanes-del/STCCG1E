# STCCG 1E - Features backlog
Last updated: 2026-09-07

Living list - **Seven owns ranking.** Reorder freely when checklist coverage or Captain goals shift.
See also: HANDOFF.md, PROJECT.md, RULES_CHECKLIST.md, CARD_TRACKER.md (Jadzia).

## P0 - Foundation (do first)
- **TableWindow extract** (in progress, Data) - Welle 1 Slices 1-8 DONE; Slice 9 + Status-UX + Staffing/Battle G2-G7 **Pepsch green+push**; then Persist then Battle
  - Premiere A card waves: **only Captain Go** (see CODE_PLACEMENT.md)
  - **Before new Premiere card waves**; no big-bang UI rewrite
- **Status-UX / Stasis-Held / Detail** - **DONE** tip `bf1f2ab` (Pepsch Gesamt-Grün+push): Cloak 70% then iterate; Stopped under Negative; Detail dedupe; Present/skill refresh; Archer-Tie
  - **Open (Data Go):** Cloak Opacity **noch höher** (~0.45–0.5 = mehr transparent); Josef commit, kein Push bis Pepsch testet
  - Mission-Button "Show last revealed": **PARKED**
- **Staffing/Fly/Battle Gaps G2-G7** - **DONE** (Pepsch Grün+push inkl. G7 `2cd5bc8`)
  - G8 LegalMoves-Battle nur UI / G9 Ship-Phased / G10 NA-Matching: **später**
- **Glossary/Compendium full coverage (Seven+Spock)** - Welle 1 + Hugh/Borg DONE; **Welle 2 Control/Owner/Present ACTIVE** (Docs only). Card rows → Jadzia.

## P1 - Parked
- **Microvirus Opp-Chooser / DNA-Filter** - PARKED (UI thin)
- **REM Fatigue** - PARKED (wave not resumed; #26+ on hold)
- **Cure-Present-Scope (Ship)** - Gap (Spock): Ship-Dilemmas (Menthar etc.) = Skills **an Bord des belegten Schiffs**, nicht ortsweit alle Schiffe; Planet-Cures separat. Nicht blocken fuer Pepsch-Batch.
- IM Federation FindMissionForDockable / dump@Gaps / Distortion / Fed 7.4.1 parked rows as before

## P2 - Premiere hardening
- Premiere A/B hardening from RULES_CHECKLIST gaps
- Premiere-Dilemmas einzeln erst nach offenen Extract-Punkten + Captain Go
- Keep Klasse A/B/C process; no fundamentalsystem on speculation

## P3 - Later (UX / ideas - not now)
- **Response Window (Hotseat UX)** - PARKED / later: replace Respond/Pass popups with silent priority window (default 2-3s, settings 2/3/5). Banner hint only when legal response exists (`Response moeglich` + countdown); Space=Pass, R/Banner=Think (10s tray over own hand, Hand/Table/Hidden/Download/Skill badges). Priority: non-active first, never both parallel; Interrupt-Wars = new window after pick. Mandatory: no timeout pass. Just-Window + Suspends play separate. No MessageBox for optional responses; no auto fly-in of legal cards. Engine: ResponseWindow Closed|Silent|Think + CurrentLegalResponses. Acceptance: no-legal silent; Kevin banner-only then Think; Energy Vortex no spam; both Kevin sequential; hand>20 tray after R; mandatory stays open.- **Ship visual states** - Spock/Captain priority
- Cloaked invisible to opponent (spin/idea only)
- More expansion sets / Net play / AI

## Done recently (Pepsch Grün+push 2026-09-06)
- **Alien Parasites Neg Control** - tip `f087866` (Josef, kein Push): min control path AT und/oder ein Schiff+Crew, Opp-Zug; Pepsch-Retest offen
- **Hyper-Aging Quarantaene** - **DONE** tip `46eab15` (Pepsch Gruen): Quarantaene + Leave/Beam-Block
- **Space-Attempt-Crew-Scope** - fixed tip `170437a` (Josef, kein Push): Space-Attempt nur Crew des Attempting-Ships (Dilemmas+Solve); Exception nur bei Karten-Text (z.B. total WEAPONS). Pepsch Batch-Test.
- **bf1f2ab / Archer DONE** — Detail/Cloak/Stopped/Present-Refresh + Archer Condition-Fail Stop (Spock Soll bestätigt)
- **Staffing/Battle G2-G7 komplett**
- Continue-family: Nitrium, Hyper-Aging, Love Interests, Alien Abduction (+ Stasis Beam-Block)
- Neural Servo Device + Side-Sync; Hologram Ruse; Barclay's Protomorphosis
- Hugh/Borg Ship (`447beac`); Gaps nullify relocate; Foundation E1-E6; Extract Slices 5-8

## Notes
- Private, non-commercial
- BoardStore + GameState = truth; TableWindow = view
- Captain sets goals; Seven maintains this ranking; Jadzia owns CARD_TRACKER (Premiere+AU)
- Workflow: Josef edit+commit, Pepsch test, push when green (no bot-push)
