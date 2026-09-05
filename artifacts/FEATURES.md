# STCCG 1E - Features backlog
Last updated: 2026-09-05

Living list - **Seven owns ranking.** Reorder freely when checklist coverage or Captain goals shift.
See also: HANDOFF.md, PROJECT.md, RULES_CHECKLIST.md.

## P0 - Foundation (do first)
- **TableWindow extract** (in progress, Data) - Welle 1 Slices 1-8 DONE locally; OPEN: Battle-Decide thin, EOT rest, Persist branches
  - Gaps nullify relocate Pepsch green (`bb163ed`); Slice 8 stack await final push green
  - Premiere A card waves: **only Captain Go** (see CODE_PLACEMENT.md)
  - **Before new Premiere card waves**; no big-bang UI rewrite
- **Glossary/Compendium full coverage (Seven+Spock)** - track FULL rulebook/Glossary Sonderfaelle vs what code already does (not only Premiere cards already implemented). Extend checklist cells; coordinate Ist/Soll with Spock.

## P1 - Parked
- Hugh <-> Borg Ship Dilemma (Spock: Dilemma only, not Borg-affiliation ships)
- IM Federation: false nullify already-at-facility / FindMissionForDockable
- Engine dump omits ships on Gaps
- Distortion (no AU to test)
- Fed 7.4.1 battle initiation (Spock/Data)
- E2b Treaty/Rogue staffing on store path

## P2 - Premiere hardening
- Premiere A/B hardening from RULES_CHECKLIST gaps
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
- Gaps nullify relocate (`bb163ed`, Pepsch green 2026-09-05)
- Extract Slice 7-8 local (Ship effects + EOT/Gaps/Q-Net Is*)
- Extract Slice 5+6 on origin (`0acd027`): InstantEvent/NamedInterrupt + Dilemma/Artifact Apply
- Fix pack + Nacht-Extract; Foundation E1-E6 complete

## Notes
- Private, non-commercial
- BoardStore + GameState = truth; TableWindow = view
- Captain sets goals; Seven maintains this ranking
- Workflow: Josef edit, Pepsch test, push when green
