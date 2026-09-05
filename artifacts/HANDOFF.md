# STCCG 1E - Handoff
Last updated: 2026-09-05 (Extract Slice 7 Sanctuary/Distortion/Tachyon/Transwarp)
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

## Pepsch retest section (2026-09-05) - Data fixes COMPLETE locally

### Status tip (local, NOT pushed)
**Local tip (Josef):** verify with `git log -1 --oneline` (Hugh fail/hand + prior Wormhole/WNOHGB/Kevin/Hugh stack)
**Origin tip:** `7bf128f` - Engine E6 (IM/Required-Move hops on Locations)
**Branch:** `master` ahead of `origin/master`. **Do not assume pushed.** NO PUSH until Pepsch retest is green.

### Retest checklist (Pepsch) - push blocked until green
1. Rebuild local tip (`dotnet build StarTrekCCG/StarTrekCCG.csproj -c Debug`)
2. Gaps/Q-Net/Red Alert still GREEN (regression)
3. **Wormhole:** 2 copies in hand â†’ drop first on exposed ship â†’ second on location + relocate/stop
4. **WNOHGB:** play on TABLE â†’ wrap-around fly / IM on WNOHGB spaceline (Q-Net path uses shorter wrap when clear)
5. **Kevin:** miss target â†’ card back in hand (not destroyed); can nullify TABLE + attached Events (picker if multi)
6. **Hugh:** Rogue Borg â†’ drop on ship/location, no detail-pick; Borg Ship option only if Dilemma revealed+present
7. Green â†’ push entire local stack; Red â†’ Ist/Soll no push

### Unpushed fix stack (newest first - verify `git log -20 --oneline`)
Look for: Hugh fail/hand; Hugh host-match; WNOHGB PathBlocked; Kevin/Hugh Spock; WNOHGB wrap; Wormhole pair-check; night extract slices.

### Root causes (for Pepsch notes)
1. Wormhole: drag `RemoveCardFromZone` before pair-count â†’ counted 1 of 2; fixed `CountWormholesForPairStart`
2. WNOHGB: hazard/Q-Net treated wrap as crossing whole line; wrap path + PathBlocked fallback
3. Kevin: miss still committed/discarded; removed hover-only UX; TABLE+attached pool + cancelâ†’hand
4. Hugh: Borg-affil ships wrongly in picker; Spock = Dilemma only when revealed; Rogue Borg direct drop

### Still parked
- Hugh Borg Ship Dilemma branch
- IM FindMissionForDockable false already-at-facility
- dump omits ships on Gaps

### Next after green push + Captain Go
Inventar: Slices 1-7 DONE (hazards, Wormhole, IM, Lore/Hugh, InstantEvent/NamedAu, Dilemma/Artifact, Sanctuary/Distortion/Tachyon/Transwarp). Next after push: Premiere A waves / remaining TW clusters. No big-bang. No new Premiere cards until Captain Go.

---

### Tip note after extracts
Local tip includes Slice 7 decide-gate extract (ahead of origin). **NO PUSH.** Parked unchanged: Hugh Borg Ship Dilemma; IM FindMissionForDockable false already-at; dump omits ships on Gaps.

## Current foundation status (2026-09-04 / still true)
- Board 0-6 done
- **Foundation E1-E6 COMPLETE** and pushed; Pepsch tip when pushed was `7bf128f`
- Leave Engine/Game C# alone unless you are Data on the assigned extract (or an explicitly assigned bugfix)

## Parked bugs (must stay visible)
1. **Gaps in Normal Space:** hopefully fixed in `f47469b` - keep watching
2. **IM Federation:** false nullify already-at-facility - **still open**
3. **Wormhole** - fixed locally (pair-check after drag) - retest
4. **Engine dump** omits ships on Gaps - **still open**

## Goals
### Short-term
1. **Pepsch retest** this fix stack; push only when green
2. **TableWindow extract** continues after push
3. Premiere-first after extract
4. Keep parked bugs visible

### Long-term
Stable BoardStore+GameState truth; TableWindow view; Premiere then expansions; later net+AI; private non-commercial

## Docs map
See PROJECT.md / ENGINE.md / RULES_CHECKLIST.md / FEATURES.md / CHANGELOG.md / TABLEWINDOW_INVENTORY.md
