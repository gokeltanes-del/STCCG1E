# STCCG 1E - Features backlog
Last updated: 2026-09-05

Living list - **Seven owns ranking.** Reorder freely when checklist coverage or Captain goals shift.
See also: HANDOFF.md, PROJECT.md, RULES_CHECKLIST.md.

## P0 - Foundation (do first)
- **TableWindow extract** (in progress, Data) - remaining TW clusters after Slices 1-6
  - Slices 1-6 DONE on origin (`0acd027`): hazards, Wormhole, IM, Lore/Hugh, InstantEvent/NamedAu, Dilemma/Artifact Apply
  - Slice 7 local (`c910ac9`+): Sanctuary/Distortion/Tachyon/Transwarp - Pepsch Transwarp+Tachyon green 2026-09-05
  - Next: further extract slices (Captain Go); Premiere A waves only after extract enough
  - **Before new Premiere card waves**; no big-bang UI rewrite

## P1 - Parked
- Hugh <-> Borg Ship Dilemma (Spock: Dilemma only, not Borg-affiliation ships)
- IM Federation: false nullify already-at-facility / FindMissionForDockable
- Engine dump omits ships on Gaps
- Fed 7.4.1 battle initiation (Spock/Data)
- E2b Treaty/Rogue staffing on store path

## P2 - Premiere hardening
- Premiere A/B hardening from RULES_CHECKLIST gaps
- Keep Klasse A/B/C process; no fundamentalsystem on speculation

## P3 - Later (UX / ideas - not now)
- **Ship visual states (both players always see)** — Spock Premiere priority (2026-09-05); do not implement until Captain prioritizes:
  - Today-ish: Stopped ~ half-transparent, Damaged ~ red tint (Rotation/HULL)
  - Needs clear cue: **Cloaked** (e.g. badge)
  - Premiere next: Docked, RANGE-left, Controller≠Owner
  - After that: Staffed-Warnung, Commandeered, Phased, Landed, Carried, Tractor, Off-spaceline (Engage Cloak/Rift), Attachments-Badges
  - Sources (Spock): Glossary cloaking/phasing, movement, damage/HULL, mission attempt, unique/owner
- **Idea (Rules+Gameplay first, not UX-first):** Cloaked ships truly invisible to the opponent. Park as concept only - no implementation.
- More expansion sets (after Premiere solid)
- Net play
- AI

## Done recently
- Extract Slice 5+6 on origin (`6a077c3` / `0acd027`, 2026-09-05): InstantEvent/NamedInterrupt + Dilemma/Artifact Apply gates
- Fix pack + Nacht-Extract (Wormhole, WNOHGB, Kevin, Hugh Rogue; Gaps kill-on-Gaps)
- E6 IM / Required-Move (`7bf128f`+) - Foundation E1-E6 complete
- E5 LegalMoves-Fly (`484fd6e`+)
- E4 Unique/InPlay/Persona by Owner (`eff6ce2`+)

## Notes
- Private, non-commercial
- BoardStore + GameState = truth; TableWindow = view
- Captain sets goals; Seven maintains this ranking
- Workflow: Josef edit, Pepsch test, push when green
