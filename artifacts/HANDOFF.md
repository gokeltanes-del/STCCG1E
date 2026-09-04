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
- Board 0-6 done; E1-E5 on origin (E5 tip history through `484fd6e`+)
- **E4 Unique/InPlay DONE** (`eff6ce2`+): Unique by Owner/Persona (Spock Glossary), not Controller
- **E5 LegalMoves-Fly Locations DONE** (`484fd6e`+)
- **E6 IM/Required-Move Locations** (`7bf128f`+): Pepsch push in progress 2026-09-04
- **Next after E6 push:** TableWindow extract of altcode / card effects into `Game/*Rules`, BoardStore, templates — BEFORE new Premiere card waves
- Parked bugs (post-E6 smoke; see FEATURES P1):
  1. Gaps extra kill / missing kill log
  2. IM Fed false already-at-facility nullify
  3. Wormhole broken again
  4. Debug dump omits ships on Gaps
- Also parked: Fed 7.4.1 battle initiation; E2b Treaty/Rogue staffing on store path

## Goals
### Short-term
1. Pepsch: finish E6 push when green
2. TableWindow altcode / per-card effects extract — foundation before new cards
3. Parked smoke bugs via Data (ranked in FEATURES)
4. Keep A/B/C process; Premiere-first

### Long-term
Stable BoardStore+GameState truth; TableWindow view; Premiere then expansions; later net+AI; private non-commercial

## Docs map
PROJECT.md, ENGINE.md, RULES.md, RULES_CHECKLIST.md, CHANGELOG.md, FEATURES.md (Seven), HANDOFF.md (this)
