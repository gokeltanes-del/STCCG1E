# STCCG 1E - Handoff
Last updated: 2026-09-04
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\StarTrekCCG
Workflow: Josef edit locally → Pepsch builds/tests in VS → push only when green.

## Team
- Captain - Project Captain (Pepsch talks mainly here); goals + HANDOFF/PROJECT/CHANGELOG/ENGINE
- Data - Engine Designer + Klasse C coder (was STCCG_Engine)
- Spock - Rules A/B + card text + Compendium (was STCCG_Rules)
- Seven - RULES_CHECKLIST coverage + FEATURES.md backlog
Channel: STCCG Team
Grok project: Star Trek CCG 1E (stccg-1e)

## On every new chat (all bots)
1. Read this HANDOFF.md
2. Read artifacts/PROJECT.md + your specialty docs
3. Check git tip / local ahead commits (`git status`, `git log -3 --oneline`)
4. Ask Captain only if status unclear
5. Do NOT touch uncommitted / in-progress Engine work belonging to another bot without Captain OK

## Current foundation status (2026-09-04)
- Board 0-6 done
- **Foundation E1–E6 COMPLETE** and pushed; Pepsch tip when pushed was `7bf128f` (Engine E6: IM/Required-Move hops on Locations)
  - E1 ToGameState | E2 Capture-Fallback | E3 Status an Instanz | E3b
  - E4 Unique/InPlay by Owner/Persona (`eff6ce2`+)
  - E5 LegalMoves-Fly destinations from Locations (`484fd6e`+)
  - **E6 Incoming Message / Required-Move hops on Locations DONE** (`7bf128f`)
- **NEXT: TableWindow extract** — pull card/rules truth out of TableWindow into `Game/*Rules`, BoardStore, templates, etc.
  - No big-bang UI rewrite; Premiere-first after extract
  - Solid foundation before adding new Premiere card waves
- Leave Engine/Game C# alone unless you are Data on the assigned extract (or an explicitly assigned bugfix)

## Parked bugs (must stay visible)
1. **Gaps in Normal Space:** Buruk mystery kill on Shattered→Lonka before Gaps; kill events missing from debug log
2. **IM Federation:** false nullify "already at the facility's location" (Nebula@Cultural vs Fed Outpost@Avert)
3. **Wormhole** no longer works (Pepsch 2026-09-04)
4. **Engine dump** often omits ships sitting on Gaps (state still has them)

Also parked (not current focus): Fed 7.4.1 battle initiation; E2b Treaty/Rogue staffing on store path; Lore fly staffing is not E4

## Goals
### Short-term
1. **TableWindow extract** (card/rules truth → Game/*Rules, BoardStore, etc.) — no big-bang UI rewrite
2. Premiere-first after extract
3. Keep parked bugs visible; fix when they block playtests
4. Keep A/B/C process; Josef edit, Pepsch test, push when green

### Long-term
Stable BoardStore+GameState truth; TableWindow view; Premiere then expansions; later net+AI; private non-commercial

## Docs map
PROJECT.md, ENGINE.md, RULES.md, RULES_CHECKLIST.md, CHANGELOG.md, FEATURES.md (Seven), HANDOFF.md (this)
