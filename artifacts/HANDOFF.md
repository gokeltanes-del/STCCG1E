# STCCG 1E — Handoff
Last updated: 2026-09-04
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\StarTrekCCG
Workflow: Captain edits locally → Pepsch builds/tests in VS → push only when green.

## Team
- Captain — Project Captain (Pepsch talks mainly here); goals + HANDOFF/PROJECT/CHANGELOG/ENGINE
- Data — Engine Designer + Klasse C coder (was STCCG_Engine)
- Spock — Rules A/B + card text + Compendium (was STCCG_Rules)
- Seven — RULES_CHECKLIST coverage + FEATURES.md backlog
Channel: STCCG Team
Grok project: Star Trek CCG 1E (stccg-1e)

## On every new chat (all bots)
1. Read this HANDOFF.md
2. Read artifacts/PROJECT.md + your specialty docs
3. Check git tip / local ahead commits
4. Ask Captain only if status unclear

## Current foundation status
- Board 0–6 done; E1–E3b on GitHub tip d9877bc
- E4 Unique/InPlay local tip 56e6790 — awaiting Pepsch two-Nebula unique test then push
- Next after E4 green: E5 LegalMoves-Fly locations, E6 IM/Required-Move locations
- Then: TableWindow extraction of misplaced card/rules altcode BEFORE new Premiere card waves
- Parked: E2b Treaty/Rogue staffing on store path; Fed 7.4.1 battle initiation → Spock/Data later

## Goals
### Short-term
1. Finish E4 Owner-fix Lore retest + push
2. E5, E6
3. Extract TableWindow altcode / per-card effects into Game/*Rules, BoardStore, templates — solid foundation before adding new cards
4. Keep A/B/C process; Premiere-first

### Long-term
Stable BoardStore+GameState truth; TableWindow view; Premiere then expansions; later net+AI; private non-commercial

## Docs map
PROJECT.md, ENGINE.md, RULES.md, RULES_CHECKLIST.md, CHANGELOG.md, FEATURES.md (Seven), HANDOFF.md (this)