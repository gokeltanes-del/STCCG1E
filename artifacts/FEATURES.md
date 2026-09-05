# STCCG 1E - Features backlog
Last updated: 2026-09-05

Living list - **Seven owns ranking.** Reorder freely when checklist coverage or Captain goals shift.
See also: HANDOFF.md, PROJECT.md, RULES_CHECKLIST.md.

## P0 - Foundation (do first)
- **TableWindow extract** (in progress, Data) - Welle 1 Slices 1-8 DONE (`447beac` green+push); **Slice 9 EOT-rest** tip `227aa65` + Repair Leave-Reset in Status-UX pack; then Persist then Battle
  - Premiere A card waves: **only Captain Go** (see CODE_PLACEMENT.md)
  - **Before new Premiere card waves**; no big-bang UI rewrite
- **Status-UX / Stasis-Held (Pepsch Go 2026-09-05, Data on Josef)** - one pack with Repair Leave-Reset:
  - Outpost/Spacedock repair: Undock/Leave/Fly → Counter 0; Detail amber EN `Outpost repair: clears end of next turn (N left)`
  - Detail Status-Block colors: Green Buff / Red Debuff / Amber Timer; Crew-Minis same badges
  - Stasis: Cyan/Violet glow + Detail `In stasis`; Mission Held-Panel (Dilemma + Personnel) for Alien Abduction etc.
  - Coverage: mark done after Pepsch green (Seven); Spock Soll stasis/Alien Abduction delivered
- **Glossary/Compendium full coverage (Seven+Spock)** - Welle 1 Timing/Actions/Nullify core done; **Hugh/Borg CanRespond DONE** (`447beac`); **Welle 2 Control/Owner/Present ACTIVE** (Docs only). Track FULL Glossary vs code; Ist/Soll with Spock.

## P1 - Parked
- IM Federation: false nullify already-at-facility / FindMissionForDockable
- Engine dump omits ships on Gaps
- Distortion (no AU to test)
- Fed 7.4.1 battle initiation (Spock/Data)
- E2b Treaty/Rogue staffing on store path

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
- Hugh/Borg Ship CanRespond + EOT battle window (`447beac` Pepsch green+push 2026-09-05): Dilemma-only, same-loc Attack, Direct Hit/docked shields, cloaked skip, no RF after Destroy
- Gaps nullify relocate (`bb163ed`, Pepsch green 2026-09-05)
- Extract Slice 7-8 (Ship effects + EOT/Gaps/Q-Net Is*)
- Extract Slice 5+6 on origin (`0acd027`): InstantEvent/NamedInterrupt + Dilemma/Artifact Apply
- Foundation E1-E6 complete

## Notes
- Private, non-commercial
- BoardStore + GameState = truth; TableWindow = view
- Captain sets goals; Seven maintains this ranking
- Workflow: Josef edit+commit, Pepsch test, push when green (no bot-push)
