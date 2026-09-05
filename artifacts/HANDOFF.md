# STCCG 1E - Handoff
Last updated: 2026-09-05 (spawn-ready: Gaps nullify green; Slice 8 + Gaps fix local)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\Dev\StarTrekCCG\StarTrekCCG
Workflow: Josef edit locally -> Pepsch builds/tests in VS -> push only when green.

## Team
- Captain - Project Captain (Pepsch talks mainly here); goals + HANDOFF/PROJECT/CHANGELOG/ENGINE
- Data - Engine Designer + Klasse C coder (BoardStore/GameState/EngineAuthority/LegalMoves + TableWindow extract). **No separate coder.**
- Spock - Rules A/B + card text + Compendium/Glossary Ist/Soll
- Seven - RULES_CHECKLIST + FEATURES backlog; **with Spock: full Glossary/Compendium coverage (not only "what we coded")**
Channel: STCCG Team
Grok project: Star Trek CCG 1E (stccg-1e)

## On EVERY new chat / bot clone (all bots)
1. Read **this HANDOFF.md** end-to-end
2. Read `artifacts/PROJECT.md` + your specialty docs
3. Data also: `artifacts/ENGINE.md`, `artifacts/CODE_PLACEMENT.md`, `artifacts/TABLEWINDOW_INVENTORY.md`
4. Spock/Seven also: `artifacts/RULES.md`, `RULES_CHECKLIST.md`, Compendium PDF / Glossary
5. `git status` + `git log -5 --oneline` on Josef — tip may be **ahead of origin**
6. Ask Captain only if status unclear
7. Do NOT touch another bot's in-progress Engine work without Captain OK
8. German short replies with Pepsch (Data/Captain style: Schritt, Klasse, Dateien, bewusst nicht)

## Current tip (2026-09-05) — VERIFY on Josef
**Local tip:** `bb163ed` Fix Gaps nullify relocate (and beneath: Slice 8 `d5bd830`, Tachyon/Transwarp `2ad73e1`, Slice 7…)
**Origin:** may lag — `git status` may show ahead N. **NO PUSH** until Pepsch says green for the whole pending stack.
Pepsch confirmed **Gaps nullify relocate works** (2026-09-05). Still confirm Slice-8 items not yet signed off (Transwarp EOT discard etc.) before push if not already green.

### Pending / recently green
- Gaps nullify relocate — **Pepsch GRÜN** (`bb163ed`)
- Slice 7 Sanctuary + Tachyon/Transwarp fix — earlier green
- Slice 8 EOT extract — Plasma/WarpCore/SWB(+Traveler) green per Captain notes; confirm Transwarp EOT + Gaps/Q-Net regression with Gaps fix
- Distortion — deferred (no AU)

### Parked (do not silently "fix" in extract)
1. Hugh Borg Ship Dilemma branch
2. IM FindMissionForDockable false already-at-facility
3. Engine dump omits ships on Gaps
4. Distortion (no AU to test)

## Foundation
Board 0–6 + Engine E1–E6 **COMPLETE** (pushed earlier). Dual-run BoardStore; no big-bang TableWindow split.

## TableWindow extract status
**Welle 1 (Inventar suggested order) Slices 1–8 DONE locally** — decide gates in `Game/*Rules`.
- OPEN after Welle 1: Battle-Decide thin; EOT rest (Rogue Borg, Borg Ship, repairs, dilemma EOT); Persist branches (~32) stepwise
- Premiere A card waves: **only Captain Go** — see `CODE_PLACEMENT.md`
- Inventory: `artifacts/TABLEWINDOW_INVENTORY.md`

## Where new code goes
**`artifacts/CODE_PLACEMENT.md`** — Decide in Rules, Apply in TableWindow, Board for location/status. Read before any new card/verb.

## Fix protocol
Class A card / B phrase / C foundation. Lookup: Checklist → Glossary → Temp Rulings → App A → App B. One chat ≈ one step. CHANGELOG one line per playable change.

## Goals
### Short-term
1. Pepsch finish Slice-8 / stack retest → push when green
2. Continue extract OPEN clusters on Captain Go
3. Premiere-first after extract Welle 1 solid
4. Seven+Spock: Glossary/Compendium full pass vs code

### Long-term
BoardStore+GameState truth; TableWindow view; Premiere then expansions; later net+AI; private non-commercial

## Docs map
HANDOFF (this) · PROJECT · ENGINE · CODE_PLACEMENT · TABLEWINDOW_INVENTORY · RULES · RULES_CHECKLIST · FEATURES · CHANGELOG
