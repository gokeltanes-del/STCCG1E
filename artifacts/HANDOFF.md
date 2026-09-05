# STCCG 1E - Handoff
Last updated: 2026-09-05 (push-ready docs; Gaps green)
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
5. `git status` + `git log -5 --oneline` on Josef â€” tip may be **ahead of origin**
6. Ask Captain only if status unclear
7. Do NOT touch another bot's in-progress Engine work without Captain OK
8. German short replies with Pepsch (Data/Captain style: Schritt, Klasse, Dateien, bewusst nicht)

## Current tip (2026-09-05) â€” VERIFY on Josef
**Local tip:** verify with git log -1 --oneline (Docs: FEATURES + GLOSSARY_COVERAGE + HANDOFF push-ready)
**Beneath:** `fbf0c2b` CODE_PLACEMENT/spawn HANDOFF; `bb163ed` Gaps nullify (Pepsch **GRÃœN**); `d5bd830` Slice 8; `2ad73e1` Tachyon/Transwarp; `c910ac9` Slice 7; â€¦  
**Push:** Pepsch when ready â€” Gaps green; Slice 8 stack included. After push: bot clones / context restart OK.

### Pending / recently green
- Gaps nullify relocate â€” **Pepsch GRÃœN** (`bb163ed`)
- Slice 7 Sanctuary + Tachyon/Transwarp fix â€” earlier green
- Slice 8 EOT extract â€” Plasma/WarpCore/SWB(+Traveler) green per Captain notes; confirm Transwarp EOT + Gaps/Q-Net regression with Gaps fix
- Distortion â€” deferred (no AU)

### Parked (do not silently "fix" in extract)
1. Hugh Borg Ship Dilemma branch
2. IM FindMissionForDockable false already-at-facility
3. Engine dump omits ships on Gaps
4. Distortion (no AU to test)

## Foundation
Board 0â€“6 + Engine E1â€“E6 **COMPLETE** (pushed earlier). Dual-run BoardStore; no big-bang TableWindow split.

## TableWindow extract status
**Welle 1 Slices 1–9 DONE locally** — decide gates in `Game/*Rules` (Slice 9: `EndOfTurnRestRules` repair/Rogue/Edo/SOT/dilemma EOT).
- OPEN after Slice 9: Persist branches (~32) stepwise; then Battle-Decide thin; Borg Ship EOT battle Apply stays in TW
- Premiere A card waves: **only Captain Go** â€” see `CODE_PLACEMENT.md`
- Inventory: `artifacts/TABLEWINDOW_INVENTORY.md`

## Where new code goes
**`artifacts/CODE_PLACEMENT.md`** â€” Decide in Rules, Apply in TableWindow, Board for location/status. Read before any new card/verb.

## Fix protocol
Class A card / B phrase / C foundation. Lookup: Checklist â†’ Glossary â†’ Temp Rulings â†’ App A â†’ App B. One chat â‰ˆ one step. CHANGELOG one line per playable change.

## Goals
### Short-term
1. Pepsch finish Slice-8 / stack retest â†’ push when green
2. Continue extract OPEN clusters on Captain Go
3. Premiere-first after extract Welle 1 solid
4. Seven+Spock: Glossary/Compendium full pass vs code

### Long-term
BoardStore+GameState truth; TableWindow view; Premiere then expansions; later net+AI; private non-commercial

## Docs map
HANDOFF (this) Â· PROJECT Â· ENGINE Â· CODE_PLACEMENT Â· TABLEWINDOW_INVENTORY Â· RULES Â· RULES_CHECKLIST Â· FEATURES Â· CHANGELOG
