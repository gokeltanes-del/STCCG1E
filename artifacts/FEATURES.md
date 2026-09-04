# STCCG 1E - Features backlog
Last updated: 2026-09-04

Living list - **Seven owns ranking.** Reorder freely when checklist coverage or Captain goals shift.
See also: HANDOFF.md, PROJECT.md, RULES_CHECKLIST.md.

## P0 - Foundation (do first)
- **TableWindow extract** (card/rules truth out of TableWindow into Game/*Rules, BoardStore, templates) — **before new Premiere card waves**; no big-bang UI rewrite; Premiere-first after extract

## P1 - Parked bugs (must stay visible)
1. Gaps in Normal Space: Buruk mystery kill on Shattered→Lonka before Gaps; kill events missing from debug log
2. IM Federation: false nullify "already at the facility's location" (Nebula@Cultural vs Fed Outpost@Avert)
3. Wormhole no longer works (Pepsch 2026-09-04)
4. Engine dump often omits ships sitting on Gaps (state still has them)
- Fed 7.4.1 battle initiation (Spock/Data)
- E2b Treaty/Rogue staffing on store path

## P2 - Premiere hardening
- Premiere A/B hardening from RULES_CHECKLIST gaps
- Keep Klasse A/B/C process; no fundamentalsystem on speculation

## P3 - Later
- More expansion sets (after Premiere solid)
- Net play
- AI

## Done recently
- E6 IM / Required-Move locations (`7bf128f`, pushed, 2026-09-04) — Foundation E1–E6 complete
- E5 LegalMoves-Fly locations (`484fd6e`+, on origin, 2026-09-04)
- E4 Unique/InPlay/Persona by Owner (`eff6ce2`+, on origin, 2026-09-04)

## Notes
- Private, non-commercial
- BoardStore + GameState = truth; TableWindow = view
- Captain sets goals; Seven maintains this ranking
- Workflow: Josef edit, Pepsch test, push when green
