# STCCG 1E - Handoff
Last updated: 2026-09-05 (night handoff - morning test)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\Dev\StarTrekCCG\StarTrekCCG
Workflow: Josef edit locally -> Pepsch builds/tests in VS -> push only when green.

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

---

## Night section (2026-09-05) - will refresh when Data finishes

**Local tip (Josef):** `ca5372b` - Extract Slice 4: Lore/Hugh decide gates
**Origin tip:** `7bf128f` - Engine E6 (IM/Required-Move hops on Locations)
**Branch:** `master` ahead of `origin/master` by **9** commits. **Do not assume pushed.** NO PUSH until Pepsch morning test is green.

### Morning test (Pepsch)
1. Rebuild from local tip (ahead of origin - do not assume pushed)
2. Gaps: neighbor mission no kill; land on Gaps = exactly 1 kill + log
3. Wormhole: need 2 in hand; pair play + relocate
4. Smoke: Q-Net 2 Diplomacy, IM to facility, Red Alert
5. Optional: Lore Returns commandeer gates; Hugh cancel/kill modes

### Local unpushed stack (as of start of night run - verify with `git log -8 --oneline`)
```
ca5372b Extract Slice 4: Lore/Hugh decide gates
9f37417 Extract Slice 3: IncomingMessageRules gates
26db024 Docs: night handoff for morning test
db9c30b Extract Slice 2: Wormhole pair rules + relocate sync
65d152e Extract Slice 1: MovementHazardRules from TableWindow
8388d48 Docs: HANDOFF E6 done, TableWindow next, parked bugs
8bc5e98 Docs: TableWindow inventory for extract prep
f47469b Fix: Gaps kill only on Gaps location + log
```
Unpushed (~9): tip through `d5bbeb5`. Origin starts at `7bf128f`. Verify with `git log`.

### Parked still open
- IM Fed false already-at-facility (FindMissionForDockable)
- dump omits ships on Gaps
- (Gaps/Wormhole hopefully fixed in unpushed commits - retest)

### Next after green push
Continue TableWindow extract per TABLEWINDOW_INVENTORY.md; then Premiere A/B cards.

### Extract progress (Data - local, await Pepsch)
- Slice 1 done locally: `MovementHazardRules` (Q-Net/Tetryon/Rift/Gaps decide; View applies) - `65d152e` (+ Gaps kill Host/Host2 in `f47469b`)
- Slice 2 done locally: Wormhole pair (`InterruptRules` gates + hit-test/sync) - `db9c30b`
- Slice 3 done locally: Incoming Message apply gates (`IncomingMessageRules`) - `9f37417`
- Slice 4 done locally: Lore Returns + Hugh decide gates (`EventRules` / `InterruptRules`) - `ca5372b`
- Night extract run finished (Slices 3+4); await Pepsch morning test â€” **NO PUSH**

---

## Current foundation status (2026-09-04 / still true)
- Board 0-6 done
- **Foundation E1-E6 COMPLETE** and pushed; Pepsch tip when pushed was `7bf128f` (Engine E6: IM/Required-Move hops on Locations)
  - E1 ToGameState | E2 Capture-Fallback | E3 Status an Instanz | E3b
  - E4 Unique/InPlay by Owner/Persona (`eff6ce2`+)
  - E5 LegalMoves-Fly destinations from Locations (`484fd6e`+)
  - **E6 Incoming Message / Required-Move hops on Locations DONE** (`7bf128f`)
- **NEXT after green push of night stack:** TableWindow extract - pull card/rules truth out of TableWindow into `Game/*Rules`, BoardStore, templates, etc.
  - No big-bang UI rewrite; Premiere-first after extract
  - Solid foundation before adding new Premiere card waves
- Leave Engine/Game C# alone unless you are Data on the assigned extract (or an explicitly assigned bugfix)

## Parked bugs (must stay visible)
1. **Gaps in Normal Space:** Host/Host2 kill on wrong location - **hopefully fixed** in `f47469b` (unpushed) - morning retest
2. **IM Federation:** false nullify "already at the facility's location" (Nebula@Cultural vs Fed Outpost@Avert) - FindMissionForDockable - **still open**
3. **Wormhole** pair play/relocate - **hopefully fixed** in Slice 2 `db9c30b` (unpushed) - morning retest
4. **Engine dump** often omits ships sitting on Gaps (state still has them) - **still open**

Also parked (not current focus): Fed 7.4.1 battle initiation; E2b Treaty/Rogue staffing on store path; Lore fly staffing is not E4

## Goals
### Short-term
1. **Morning test** night stack; push only when green
2. **TableWindow extract** (card/rules truth -> Game/*Rules, BoardStore, etc.) - no big-bang UI rewrite
3. Premiere-first after extract
4. Keep parked bugs visible; fix when they block playtests
5. Keep A/B/C process; Josef edit, Pepsch test, push when green

### Long-term
Stable BoardStore+GameState truth; TableWindow view; Premiere then expansions; later net+AI; private non-commercial

## Docs map
PROJECT.md, ENGINE.md, RULES.md, RULES_CHECKLIST.md, CHANGELOG.md, FEATURES.md (Seven), TABLEWINDOW_INVENTORY.md, HANDOFF.md (this)

