# STCCG 1E - Features backlog
Last updated: 2026-09-06

Living list - **Seven owns ranking.** Reorder freely when checklist coverage or Captain goals shift.
See also: HANDOFF.md, PROJECT.md, RULES_CHECKLIST.md, CARD_TRACKER.md (Jadzia).

## P0 - Foundation (do first)
- **TableWindow extract** (in progress, Data) - Welle 1 Slices 1-8 DONE (`447beac`); Slice 9 + Status-UX + dilemma Continue pack tips `3c2704c`/`8c25d96` awaiting Pepsch Gesamt-Grün+push; then Persist then Battle
  - Premiere A card waves: **only Captain Go** (see CODE_PLACEMENT.md)
  - **Before new Premiere card waves**; no big-bang UI rewrite
- **Status-UX / Stasis-Held** (Data `3c2704c` + `8c25d96`, Pepsch testing)
  - Layout Teil-Grün: Damage badge-only; Repair „this turn“; Detail Positive|Negative|Personnel|Equipment; Stasis rot / Buff grün
  - Cloak: inward Nebel/Vignette overlay (`8c25d96`) — iterate if Pepsch wants Show-Look polish
  - Mission-Button „Show last revealed“: **PARKED** (not mechanics-critical)
  - Stasis: Beam/Leave-Block + Alien Abduction Cure **OR** (3 Leadership OR mission completed)
  - Coverage: Status-UX DONE only after Pepsch Gesamt-Grün
- **Glossary/Compendium full coverage (Seven+Spock)** - Welle 1 + Hugh/Borg DONE; **Welle 2 Control/Owner/Present ACTIVE** (Docs only). Card rows → Jadzia CARD_TRACKER.

## P1 - Parked / in test
- Continue-family dilemmas (Data tip `3c2704c`/`8c25d96`, **partial** until Pepsch green):
  - Nitrium Metal Parasites, Hyper-Aging (`AttachAndContinue`)
  - Female's / Male's Love Interest (`EffectAndContinue` after Relocate)
  - Alien Abduction (Stasis hold + Cure OR + no Beam)
- IM Federation: false nullify already-at-facility / FindMissionForDockable
- Engine dump omits ships on Gaps
- Distortion (no AU to test)
- Fed 7.4.1 battle initiation (Spock/Data)
- E2b/G4 store staffing one-truth Treaties+Lore (no ui||store OR; tips G5/G4 2026-09-06)
- Mission-Button last-revealed (parked)

## P2 - Premiere hardening
- Premiere A/B hardening from RULES_CHECKLIST gaps
- Premiere-Dilemmas einzeln erst nach offenen Extract-Punkten + Captain Go
- Keep Klasse A/B/C process; no fundamentalsystem on speculation

## P3 - Later (UX / ideas - not now)
- **Ship visual states (both players always see)** - Spock/Captain priority (2026-09-05); do not implement until Captain prioritizes:
  1. Premiere: Cloaked (badge), Stopped (~half-transparent), Damaged (~red / Rotation-HULL), Docked, RANGE-left, Controller!=Owner
  2. After that: Staffed-Warnung, Commandeered, Phased, Landed, Carried, Tractor, Off-spaceline (Engage Cloak/Rift), Attachments-Badges
  - Sources (Spock): Glossary cloaking/phasing, movement, damage/HULL, mission attempt, unique/owner
- **Spin / idea (Rules+Gameplay first):** Cloaked ships truly invisible to the opponent. Park only - no implementation.
- More expansion sets (after Premiere solid)
- Net play
- AI

## Done recently
- Hugh/Borg Ship CanRespond + EOT battle window (`447beac` Pepsch green+push 2026-09-05)
- Gaps nullify relocate (`bb163ed`, Pepsch green 2026-09-05)
- Extract Slice 7-8 (Ship effects + EOT/Gaps/Q-Net Is*)
- Extract Slice 5+6 on origin (`0acd027`): InstantEvent/NamedInterrupt + Dilemma/Artifact Apply
- Foundation E1-E6 complete

## Notes
- Private, non-commercial
- BoardStore + GameState = truth; TableWindow = view
- Captain sets goals; Seven maintains this ranking; Jadzia owns CARD_TRACKER (Premiere+AU)
- Workflow: Josef edit+commit, Pepsch test, push when green (no bot-push)
