# STCCG 1E - Handoff
Last updated: 2026-09-04
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\StarTrekCCG
Workflow: Captain edits locally → Pepsch builds/tests in VS → push only when green.

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
- Board 0-6 done; E1–E3b on GitHub (through d9877bc)
- **E4 Unique/InPlay DONE and on origin tip `eff6ce2`**
  - Unique by **Owner/Persona** (not Controller) — Spock Glossary ruling
  - Restriction remains if owner loses control
  - Related local history already on origin with it: `56e6790` (E4 Unique/InPlay), `2166c03` (HANDOFF+FEATURES docs), `eff6ce2` (Owner fix)
- **E5 LegalMoves-Fly reading Locations — NEXT / IN PROGRESS**
- E6 IM/Required-Move Locations local — awaiting Pepsch retest; no push until green
  - Data has local commit `484fd6e` (Engine E5: LegalMoves Fly destinations from Locations) ahead of origin — **not pushed**; Pepsch build/test then push when green
  - Leave E5 Engine/Game C# alone unless you are Data on that task
- Then: **E6** Incoming Message / Required-Move on Locations
- Before new cards: extract TableWindow altcode / card effects into proper layers (`Game/*Rules`, BoardStore, templates)
- Parked: Fed 7.4.1 battle initiation; E2b Treaty/Rogue staffing on store path; Lore fly staffing is not E4

## Goals
### Short-term
1. Pepsch: retest E5 Fly Locations locally → push when green; then E6 IM/Required-Move Locations
2. E6 after E5 green
3. Extract TableWindow altcode / per-card effects into Game/*Rules, BoardStore, templates — solid foundation before adding new cards
4. Keep A/B/C process; Premiere-first

### Long-term
Stable BoardStore+GameState truth; TableWindow view; Premiere then expansions; later net+AI; private non-commercial

## Docs map
PROJECT.md, ENGINE.md, RULES.md, RULES_CHECKLIST.md, CHANGELOG.md, FEATURES.md (Seven), HANDOFF.md (this)