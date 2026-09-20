Local Josef tip **f522875** - Captain A-F EOT hang. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Captain A-F EOT hang

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
A PreviewKeyDown EOT Spaceâ†’Resume first. B ResolveEntireStack also _eotEndingInProgress.
C FinishExecute try/finally CompleteTurnChange. D EOT skipSchism. E OpenResponse no force-disable EndTurn.
F Log EOT complete â†’ P{n} PLAY. f522875

Local Josef tip **c328826** - Horga EXECUTE draw loop. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Horga EXECUTE draw loop

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Cause: Space re-entered full FinishExecuteAndEndTurn while still Execute â†’ draw every press, P2 skipped.
Fix: _eotEndingInProgress + ResumeEndOfTurnAfterDrawResponses; Space resumes only then CompleteTurnChange.
c328826

Local Josef tip **821dd53** - EOT EXECUTE stuck fix. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - EOT draw stuck in EXECUTE

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Cause: DrawCard response stack (Schism / Horga extra draw) left Segment=Execute with End Turn disabled.
Fix: End Turn stays enabled as Finish turn/Pass; immediate ShowActionAnnounce; Space completes turn.
821dd53

Local Josef tip **9770425** - Horga Play-phase timing. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Horga Play-phase timing

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Printed: 2nd normal play in Play OR EOT draw. End PLAY always free (no P2 skip).
Execute-extra from 5c1388d removed. Tip 9770425.

Local Josef tip **5c1388d** - Horga turn + Detail scroll. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Horga turn + Detail scroll

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1) Horga'hn: first normal play always -> Execute (no stay-in-Play wait). Extra card allowed in Execute OR EOT draw.
2) DetailStackScroll edge auto-scroll while dragging.
Stone/Kurlan Pepsch GREEN. 5c1388d

Local Josef tip **71b6552** - Affil ignore non-affil brackets. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Affil ignore [Event]/icons

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
ParseAffiliationIcon: EVENT/INTERRUPT/EQUIPMENT/UNIV/S/Cmd/Stf/P â†’ null.
Regex already @\[(?<a>[^\]]+)\] + try/catch (03f3ef9). 71b6552

Local Josef tip **03f3ef9** - Affil regex crash fix. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - ParseAffiliationIcon crash fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Regex was over-escaped (\\[...) â†’ RegexParseException on Stone click.
Fixed to @\[(?<a>[^\]]+)\] + try/catch â†’ null. 03f3ef9

Local Josef tip **8b7bd95** - F3 smoke list Captain-aligned. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - F3 smoke list aligned Captain Go

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
GROK_TEMP/SMOKE_PLAYSON_F3_2026-09-20.md â€” 5 Pflicht-Checks + extras.
8b7bd95 tip. Engine unchanged (F2 7719373).

Local Josef tip **6807bf5** - Plays-on F3 smoke matrix docs. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Plays-on F3 smoke matrix (docs)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
GROK_TEMP/SMOKE_PLAYSON_F3_2026-09-20.md â€” Premiere checklist for Pepsch.
Code F0â€“F2 already tipped; F3 = human smoke, no engine change this tip.

Local Josef tip **7719373** - Plays-on F2 Kevin + Spec-Host. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Plays-on F2 Kevin + Spec-Host

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
### F2

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- TimingRules.IsEventEquivalentForKevin + CanKevinTargetEvent (ArtifactAsEvent / IsPlaysAsEventFromHand)
- CollectLegalSnapHosts: IsPlayOnDrag Spec path (Stone planet / Kurlan ship)
- Drop sets _snapSite for Artifact-as-Event; PlaceOnTablePermanents â†’ BeginPlayCardStack(target)
- TryResolveArtifactHandPlay(preferredHost): Stone filter AT to planet; Kurlan use ship
### Deferred F3

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Premiere smoke matrix (Stone snap, Kurlan+Kevin, IG/Varon Use-as, Horga'hn Immediate)

Local Josef tip **548e573** - Plays-on F1 Spec+Snap typ-agnostisch. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Plays-on F1 Spec+Snap typ-agnostisch

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Go Big-Bang. Docs PLAN/IST/NOTE_PLAYSON.
### F1

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- CardPlayRole: NativeEvent|NativeInterrupt|ArtifactAsEvent|ArtifactAsInterrupt|ArtifactAsEquipment|ImmediateTable
- ResolvePlayOn(card) zentral; Parse plays-as-[Event]-on (Stone/Kurlan)
- Spec.NeedsBoardSnap; TargetingRules.UsesBoardSnap + TargetQuery.IsPlayOnDrag typ-agnostisch
- CanPlayOn fallthrough uses ResolvePlayOn
### Verify

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
VerifyAwayTeamCrewSplit covers Stone AwayTeam+ArtifactAsEvent, Kurlan Ship, Groupie/ETA/Disruptor
### Next F2

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Kevin CanKevinTargetEvent Role-Aequivalenz; TW Name-if Targeting raus; Artifact commit on Spec host

Local Josef tip **5edbc6e** - Plays-on F0 AwayTeamâ‰ Crew. Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Plays-on F0 AwayTeamâ‰ Crew

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Go Big-Bang. Docs: PLAN/IST/NOTE_PLAYSON + SOLL_AWAYTEAM.
### F0

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- PlayOnRules.Host.AwayTeam split from Crew; Spec.Host2 for "crew or Away Team".
- Ownership enum Any/Your/Opponent; ExcludeFacility gate (Disruptor).
- HostMatchesPlayOn + TargetQuery.MatchPlayOnSpec updated.
- VerifyAwayTeamCrewSplit (Groupie AT-only; ETA dual; Disruptor ExcludeFacility).
### Next

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
F1 ResolvePlayOn + typ-agnostic Snap/Sites.

Local Josef tip **6d15780** - Stone + Kurlan Plays-on (Pepsch smoke). Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Stone + Kurlan Plays-on

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Go / Spock \GROK_TEMP/SOLL_AWAYTEAM_PLAYSON_2026-09-20.md\.
### Stone

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Target: planet Away Teams only (no ship/outpost/facility crew).
- Plays-as-Event: not IsStackable (no inert host attach); BeginPlayCardStack â†’ kill â†’ Discard.
### Kurlan

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Play on any ship incl. opponent; x3 RANGE/W/S kept (GetAllStackedCardsOnHost for foreign artifact).
### Pipeline

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- ArtifactRules.IsPlaysAsEventFromHand only â€” no Extract big-bang.
### Smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Stone: cannot stick on ship/outpost; only planet AT chooser; kills then discard.
2. Kurlan: chooser includes opp ships; x3 when 7 classes aboard that ship.

Local Josef tip docs: Gift Box Pepsch green â†’ working (adac172). Kein Code.

Local Josef tip **389f950** - Detail Name x3 fix (Pepsch smoke). Nicht gepusht.

## Tip detail (Data, Josef, 2026-09-20) - Detail Name x3 fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Go (separater Tip; Stone/Kurlan still waiting Spock).
### Fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- FormatHostEffectSummary (Event + Dilemma): effect-only, no Event:/Dilemma: + Name prefix.
- FormatAttachedHostEffectLine fallback: kind/COUNTER only, no card.Name.
- ShowCardDetail Event/Dilemma: DetailIcons empty (glyph strip only); effects only in DetailStatusBlock.
- Red Alert status moved into RefreshDetailStatusBlock (same single channel).
### Smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Open attached Event/Dilemma detail: white Name once; effect line once in status; no Name under type.
2. Open host with attached Event: status shows effect text without repeating event title.
3. Red Alert detail: status lines still present; Name only in white header.

## Note (Data, 2026-09-20) - Detail Name x3 (Ursache, kein Fix-Tip)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Pepsch smoke: weisser Name + Typ OK, Name 3x darunter (auch andere Typen); aehnlich IPG-Ueberkill.
### Ursache (Event/Dilemma-Pfad)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. \DetailName\ = card.Name (weiss, OK)
2. \ShowCardDetail\ Event/Dilemma-Zweig: \DetailIcons\ = \FormatAttachedHostEffectLine\ / \FormatHostEffectSummary\ â†’ immer Prefix \Event: {Name}\ / \Dilemma: {Name}3. \RefreshDetailStatusBlock\: dieselbe Summary-Zeile nochmals in \DetailStatusBlock= Name 3x (Header + Icons + Status). IPG-Analog: gleiche Info in mehreren Detail-Kanaelen.
### Nicht die Ursache

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- cards.json Text/Icons fuer Stone/Gift/Kurlan (kein Name-Tripel in Daten)
- IconCatalog.Fill Glyph-Strip (wie bei IPG behalten)
### Fix-Richtung (warten Captain Go / kein Big-Bang)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Summary-Zeilen effect-only (ohne Name-Prefix), ODER nur StatusBlock (Icons-Kanal leer) â€” analog IPG: einen Kanal behalten.
- Artifact else-Zweig hat diesen Doppel-Pfad nicht; wenn Artifact-only auch 3x: Smoke welcher View (Karte vs Host-Contents).
- Plays-as / Stone/Kurlan Fixes: warte Spock Away-Team + Plays-on Soll.

<!-- tip: PROJECT_STATUS.md (Repo-Root) entfernt - Wahrheit = artifacts/HANDOFF.md + artifacts/PROJECT.md (2026-09-18) -->

# STCCG 1E - Handoff

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`

Last updated: 2026-09-19 (Data - Holo existence gates Fix-Go)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: master.** GrokTest nicht nutzen.
**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Gruen-Test.

## Current tip

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Local Josef tip **c77d0d1** - Kurlan RANGE x3 (Pepsch smoke). Nicht gepusht.

Local Josef tip **adac172** - Betazoid Gift Box (Pepsch smoke). Nicht gepusht.

Local Josef tip **e954f03** - Vulcan Stone of Gol (Pepsch smoke). Nicht gepusht.
Local Josef tip **bcf7f9d** - Holo existence gates (Pepsch Fix-Go; Spock precise Soll). Nicht gepusht. (Prior Holo-Projectors tip 3c50792; LF UI eaf0c24.)
**Pepsch EXE (Default Debug):**
`C:\Dev\StarTrekCCG\StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`
Nicht `_build_holo*` / Release / alte Side-Builds.




## Tip detail (Data, Josef, 2026-09-20) - Kurlan Naiskos RANGE x3

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain queue after Gift Box. Gap: WEAPONS/SHIELDS used KurlanMultiplier; RANGE did not.
### Fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- \BattleRules.ApplyKurlan\ + \VerifyKurlanMultiplier\ (9x3=27).
- \ComputeShipTurnRange\: base RANGE = EffectiveRange * Kurlan when artifact + 7 classifications aboard.
- \FormatShipEffectiveLine\: RANGE full also * Kurlan (same order as W/S).
- Repair / move status use ComputeShipTurnRange.
### Smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Play Kurlan on ship with all 7 classifications aboard â†’ RANGE display x3; turn pool x3; W/S still x3.
2. Missing a classification â†’ RANGE stays printed (mult 1).
3. Remove classification mid-game â†’ next turn reset drops to printed (RangeLeft from ResetShipRangesForTurn).

## Tip detail (Data, Josef, 2026-09-20) - Betazoid Gift Box

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Go / Spock \GROK_TEMP/SOLL_BETAZOID_GIFT_BOX_2026-09-20.md\. Printed = truth.
### Gift Box (1 R)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Acquire ImmediateDiscard + DownloadFromDraw=3 + IgnoreOpponentDownloadPrevention.
- UI: real search picker 0..3 from own draw (AskChoice Download/Done + PickCardFromList); not top-N.
- Shuffle draw after; discard artifact even at 0 picks / empty deck.
- DownloadRules.GiftBoxAcquireRequest + MayDownloadDespiteOpponentPrevention + ClampDownloadCount.
- Opp prevent stub OpponentDownloadPreventionActive=false; Ignore flag wired for later cards.
- Verify: ArtifactRules.VerifyBetazoidGiftBox / DownloadRules.VerifyBetazoidGiftBoxDownload.
### Smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Solve with Gift Box â†’ picker; choose up to 3 â†’ hand; artifact discarded; draw shuffled.
2. Fewer than 3 in draw â†’ only that many choosable.
3. Done at 0 â†’ artifact still discarded.
4. (If prevent spoofable) opp prevent does not block this download.

## Tip detail (Data, Josef, 2026-09-20) - Vulcan Stone of Gol

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Go / Spock `GROK_TEMP/SOLL_STONE_OF_GOL_2026-09-20.md`. Printed = truth.
### Stone (9 R)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Acquire ToHand unchanged.
- Hand-play as Event: pick **any** planet Away Team (P1/P2 @ mission); **not** ship crew.
- Kill present with that AT: `!Youth && CUNNING<=7` via `ArtifactRules.IsKilledByStoneOfGol`.
- Survivors: Youth OR CUNNING>7. Discard artifact after resolve.
- Verify: `ArtifactRules.VerifyVulcanStoneOfGol`.
- PARK: Kevin-as-Event-Nullify unless generic.
### Also

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Alien Parasites â†’ **working** (Pepsch green 79a7612).
Files: `Game/ArtifactRules.cs`, `TableWindow.xaml.cs`, CARD_TRACKER, HANDOFF.
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe
### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Solve â†’ Stone in hand.
2. Play on opp AT: Youth-free CUNNING=7 die; Youth or CUNNING>7 live; Stone discarded.
3. No ship-crew target offered.
4. Own AT selectable (any).

## Tip detail (Data, Josef, 2026-09-19) - Alien Parasites Neg-Control Fix2

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Fix-Go after Pepsch smoke on e8cf505.
### Bugs

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Away Team only: auto BeamBack put AT on a ship. SOLL: stay on planet + Neg-Control.
- Ship only: BeamBack put AT aboard chosen ship so they got Control too. SOLL: planet AT untouched.
### Fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- `DecideAlienParasites` fail: **BeamBackTeam=false** when GrantOpponentControl (chooser owns placement).
- Dual still uses `MoveAlienParasitesAwayTeamOntoShip` onto chosen ship only.
### Also

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- REM Fatigue â†’ CARD_TRACKER **working** (Pepsch green Dock+Planet/3 MED; no code).
Files: `Game/DilemmaRules.cs`, CARD_TRACKER, HANDOFF.
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe
### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Away Team only â†’ AT stays on **planet**, Opp Neg-Control; no beam.
2. Ship + crew only â†’ only ship+crew controlled; planet AT unmoved/uncontrolled.
3. Away Team + ship&crew â†’ AT on **chosen** ship under Opp control (still OK from e8cf505).

## Tip detail (Data, Josef, 2026-09-19) - Alien Parasites Neg-Control Fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Fix-Go. Pepsch: Away Team+Ship&Crew beamed AT onto wrong opp ship.
### Fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Dual choice: `MoveAlienParasitesAwayTeamOntoShip` onto **chosen** ship, then same Neg-Control as ship/crew.
- No relocate onto any other ship at the location.
- Away-Team-only: controller in place (no foreign-ship beam).
### Also

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Armus â†’ CARD_TRACKER **working** (Pepsch green; no code).
- Q-Flash full verb remains PARK (smoke after Continuum expansion).
Files: `TableWindow.xaml.cs`, `DilemmaRules.cs` (verify note), `CARD_TRACKER`, `HANDOFF`.
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe
### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Parasites fail planet: Opp picks Away Team AND one ship+crew â†’ pick ship â†’ AT ends **aboard that ship** under Opp control (not another opp ship).
2. Opp picks Away Team only â†’ AT stays (planet/prior host); controller Opp; no beam to foreign ship.
3. Opp picks ship only â†’ ship+crew control OK.
4. Armus: already green / tracker working.

## Tip detail (Data, Josef, 2026-09-19) - Q Printed rework

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Pepsch: cards.json/Compendium text = truth. Spock `GROK_TEMP/SOLL_Q_PRINTED_2026-09-19.md`.
Supersedes Glossary Rearrange/Purge for Q from tip 638fde8.
### Q (44 R) Printed

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Pass: 2 Leadership + INTEGRITY>60 -> Overcome; no seed-purge; no rearrange; attempt continues.
- Fail: Opp may download 0..2 [Q] from draw/Q's Tent atop Continuum; Q-Flash of 4 **pending** (flag+reveal); team stopped; discard Q.
- Verify: `DilemmaRules.VerifyQDilemma` (Printed).
- PARK: full Q-Flash Continuum resolve verb.
### REM Fatigue

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Unchanged (still tip 638fde8 quarantine/dock/original-group).
Files: DilemmaRules.cs, TableWindow.xaml.cs, CARD_TRACKER, HANDOFF.
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe
### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Q pass: seeds under mission remain; no spaceline rearrange.
2. Q fail: optional Continuum download; Q-Flash pending message; team stopped; no rearrange.
3. REM smoke unchanged.

## Tip detail (Data, Josef, 2026-09-19) - Q + REM Fatigue

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain/Spock Soll LOCK (`GROK_TEMP/SOLL_Q_REM_2026-09-19.md`). Continuum/Q-Flash PARK.
### Q (44 R)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Pass: 2 Leadership + INTEGRITY>60 -> Overcome; purge remaining Dilemma seeds under mission; discard Q; attempt continues.
- Fail: opponent spaceline rearrange (location units, Left/Right/Done); AT/ship+crew stopped; discard Q.
- Verify: DilemmaRules.VerifyQDilemma.
### REM Fatigue (47 U)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- AttachContinue CD Icon-[4]; IsQuarantinePersist(RemFatigue); OriginalEncounter kill on CD0 (joiners live).
- Cure: 3 MEDICAL (group counts) or dock at Outpost (not HQ/Station) -> +5.
- Verify: DilemmaRules.VerifyRemFatigue + cure tests in DilemmaCureRules.
Files: Game/DilemmaRules.cs, Game/DilemmaCureRules.cs, Game/DockingRules.cs, TableWindow.xaml.cs, rtifacts/CARD_TRACKER.md, rtifacts/HANDOFF.md.
Exe: StarTrekCCG\\StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe
### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Q pass: 2 Leadership + INTEGRITY>60 at attempt -> overcome; other dilemma seeds under that mission discarded; attempt continues.
2. Q fail: no pass -> stop; opponent Left/Right/Done rearrange; Q discarded; no Q-Flash/Continuum.
3. REM: encounter without 3 MEDICAL -> quarantine CD4, attempt continues; cannot beam away; joiner quarantined but survives CD0 kill of originals only.
4. REM cure: 3 MEDICAL present -> +5 discard; OR dock ship at Outpost -> +5 discard (Station/HQ must NOT cure).

## Tip detail (Data, Josef, 2026-09-19) - Holo existence gates (Pepsch Fix-Go)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain confirms Spock precise Holo Soll. Standing Practice Glossary cites. CODE_PLACEMENT: EventRules decide + TW Apply (beam/kill/report).
Prior tip **3c50792** helpers/nullify; this tip wires report/beam gates + kill=deact + stranded erase + same-turn no-reactivate.

### Spock Soll (in-scope)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Activated: Holodeck ship/fac OR planet+Projectors OR MHE
2. Deactivated: any ship/fac OR planet+Projectors OR MHE
3. Illegal even deact: planet without Projectors/MHE
4. Illegal attempt â†’ deactivate, do NOT complete relocate
5. Erase if illegally present; Projectors nullify dependents (MHE protects); ship destroyâ†’discard; killâ†’deactivate
6. Holodeck=activate aboard; Projectors=planet only; MHE=exist+activate where allowed
7. Same-turn: no reactivate after deactivate this turn

### Core helpers (EventRules)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- HoloMayExistOnPlanet / HoloMayExistAboard(activated,â€¦) / HoloMayExistHere / HoloMayActivateHere
- CanVoluntaryRelocateHolo / IllegalRelocateShouldDeactivate / MayReactivateHologram
- DependsOnThisHoloProjectorsForExistence / DeactivateHologram / ShouldEraseWhenStuckWithoutEnabler
- VerifyHoloProjectors expanded (act/deact/planet beam/same-turn)

### TW wire

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- CompleteBeamTo â†’ FilterHoloBeamAllowed (bare planet block; illegal actâ†’deact stay)
- DiscardPersonnelBorder â†’ [Holo] kill = MarkHologramDeactivated (ship destroy still discards)
- EraseStrandedHologramsOnHost after beam; report auto-deact without activate enabler
- PersonnelInstance.HologramDeactivated + _holoDeactivatedThisTurn (EOT clear)
- IsCardDisabled ORs hologram deactivated (does not wipe via Ktarian sync)

### Parked

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Captive / opponent Holodeck deep; Holodeck Door suite / Holoprograms
- Personnel-battle safety (holo cannot kill organics; holo-only STRENGTH force)
- Activate UI button (helper CanReactivateHologramNow ready)

### Bewusst nicht

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
No push; no Door/captive/battle safety; no activate UI chrome.

### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Cannot beam [Holo] to bare planet (no Projectors/MHE) â€” blocked; stays put.
2. Holo-Projectors on planet â†’ [Holo] may beam there (act or deact).
3. MHE with/aboard â†’ [Holo] may exist/activate where allowed.
4. Kill [Holo] â†’ deactivated (not discard); ship destroy â†’ discard crew incl. [Holo].
5. Nullify Projectors â†’ dependents erased; MHE-protected survives.

### Files

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Game/EventRules.cs, Game/Board/CardInstance.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md, _VerifyHolo/*
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe (side-build _build_holo_gates green while Pepsch EXE locked)

## Tip detail (Data, Josef, 2026-09-19) - Lore's Fingernail UI (Pepsch Fix-Go)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Engine live-affil (980317a) OK for battle; Surface/Detail still showed printed Federation.
### Root

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
DetailType / RevealSubtitle / Icon row used printed `card.Affiliation` - not `GetAffiliations` live mode. No status naming Lore's Fingernail.
### Fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- `ReportingRules.FormatLiveAffiliation` / `FormatAffiliationTypeSuffix` / `FormatLiveAffiliationBracket` / `BracketAffil` from GetAffiliations.
- DetailType + RevealSubtitle + IconCatalog badge: live Non-Aligned / [Non] under Fingernail.
- DetailStatus: `Lore's Fingernail: Non-Aligned` (Debuff); ToneForEvent.Fingernail Debuff.
- Dual-affil action badge shows live Non + rule name; RefreshTableBuffs refreshes open Detail.
- Verify harness asserts FormatLiveAffiliation / Bracket / FormatFingernailLine.
### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Play Lore's Fingernail -> select Data: Detail shows Non-Aligned [Non]; status `Lore's Fingernail: Non-Aligned`; icon badge [Non].
2. Nullify Fingernail -> Data Detail back to Federation; status/badge gone.
3. Battle path still Non (engine unchanged).
### Bewusst nicht

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
No push; no house-arrest deep UI; no DeckBuilder printed filter change; no new PNG affil icons.
### Files

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Game/ReportingRules.cs, Game/DetailStatusRules.cs, Game/IconCatalog.cs, Game/EventRules.cs (verify), TableWindow.xaml.cs, artifacts/*
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe (side-build _build_lf_ui green)

## Tip detail (Data, Josef, 2026-09-19) - Lore's Fingernail (PR 81 R)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Go / Spock Soll-OK (fold all). Standing Practice Glossary cites. CODE_PLACEMENT: EventRules + ReportingRules/DualAffiliation (+ TW ambient).

### Lore's Fingernail required

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Plays on table (Place.Table / Persist.Fingernail).
- While in play: inorganic (not [Holo]) **effective affiliation = Non only**; dual toggle off / ProfileFor not active as printed.
- Nullify/leave -> restore prior multi-affil mode (ambient clear; CurrentAffiliation preserved underneath).
- Treaties: Non mixing separate (existing NA rules); Fed battle limits lift (no longer Fed).
- Matching affiliation / house arrest: re-check as Non via GetAffiliations.
- Glossary: androids may report as Non.
- Classic: Soong-type + Exocomps; [Holo] excepted. Modern: all Inorganic except [Holo].

### Core helpers

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- `DilemmaRules.IsInorganic` â€” Characteristics Inorganic and/or Android (central; whitelist verify-only).
- `EventRules.FingernailMakesNon` â€” IsInorganic && !CardIcons.IsHologram while FingernailInPlay.
- `EventRules.SetFingernailInPlay` / ambient refreshed in `RefreshTableBuffs` + CommitCardToTable.
- `ReportingRules.GetAffiliations` early NA override; DualAffiliationRules.TrySetMode/ProfileFor gated.
- `VerifyLoresFingernail` Premiere smoke.

### TW wire

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- HasFingernail on GameState / BoardStore; TrySwitchAffiliation deny while affected.

### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Play Lore's Fingernail on table -> Data / Exocomp become Non (effective); Fed battle limit lifts for them.
2. Einstein / Brahms / Fek'lhr / K'Tesh / Jera / Tomek ([Holo]+Inorganic) stay printed affil (NOT Non).
3. Nullify Fingernail -> Data back to Federation; dual toggle works again.

### Bewusst nicht

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
No push; no house-arrest deep UI; no persona/report exhaustive matrix; K'Tesh/Jera/Tomek correctly [Holo]-excepted (brief smoke listing them as -> Non contradicted printed except [Holo]).

### Files

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Game/DilemmaRules.cs, Game/EventRules.cs, Game/ReportingRules.cs, Game/DualAffiliationRules.cs, Game/GameState.cs, Game/Board/BoardStore.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md, _VerifyFingernail/*
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe (side-build _build_fingernail green)

## Tip detail (Data, Josef, 2026-09-18) - Holo-Projectors (PR 78 U)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Go / Spock Premiere Holo bullet-Soll. Standing Practice Glossary cites. CODE_PLACEMENT: EventRules (+ TW nullify wire).

### Holo-Projectors required

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Plays on [P] (Place.OnPlanet / Persist.HoloProjectors).
- While in play: [Holo] may exist on that planet activated or deactivated.
- Nullify -> erase only [Holo] at THIS planet that depended on THIS copy (MHE / other enabler / other Projectors protect; other planets untouched).
- Not a ship Holodeck (Holodeck enables aboard only).

### Core helpers (EventRules)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- IsHoloProjectors / IsMobileHoloEmitter / HasHolodeck / HasMobileHoloEmitterPresent
- HoloMayExistOnPlanet / HoloMayExistAboard
- DependsOnThisHoloProjectorsForExistence (nullify erase gate)
- DeactivateHologram (kill/destroy -> Disabled, not erase)
- ShouldEraseWhenStuckWithoutEnabler
- FormatHostEffectSummary(Persist.HoloProjectors) + VerifyHoloProjectors

### TW wire

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- NullifyEventInPlay: before detach, EraseHoloDependentsOfProjectors -> OutOfPlay for dependents of this copy.

### Parked (for Pepsch / later tips)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Captive / opponent Holodeck deep; Holodeck Door suite / Holoprograms
- Post-PR existence cards; advanced Barclay
- Full report/beam illegal-location gates + same-turn reactivate tracking
- Personnel-battle safety (holo cannot kill organics; holo-only STRENGTH force)

### Fallen (avoided)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Holodeck != planet existence; nullify != erase all [Holo]; kill != erase; planet [Holo] without enabler

### Bewusst nicht

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
No push; no Holodeck Door/captive deep; battle safety not wired this tip.

### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Play Holo-Projectors on planet -> [Holo] may exist there (act or deact).
2. Nullify Projectors with [Holo] only depending on it -> that [Holo] erased (out of play); MHE-protected survives; other planet untouched.
3. Ship Holodeck: [Holo] aboard OK; Holodeck alone does not enable planet surface.
Files: Game/EventRules.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md, _VerifyHolo/*
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe (side-build _build_holo green while Pepsch EXE locked)

## Tip detail (Data, Josef, 2026-09-18) - Goddess of Empathy (Amanda response)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain/Spock Soll-OK. Standing Practice Glossary cites.
### Root

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Goddess gate lived on normal hand-play / EngineAuthority / InterruptPlayEffect, but **response/nullify window** skipped it: `CollectAllLegalResponses` + stack-open `TryAllowHandPlay` only called `CanRespond`; `NullifyStackEffect` (Amanda) had no `HasGoddess` check. Amanda could nullify under Goddess.
### Fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- `EventRules`: Glossary cite + `GoddessBlocksInterruptPlay` + `VerifyGoddessOfEmpathy` (Amanda NOT excepted; Kevin/Q2/[Q]/[Ref] ok).
- `LegalMoves` stack responses: filter via HasGoddess + IsGoddessException.
- `NullifyStackEffect.CanPlay`: Goddess gate (Amanda blocked; Q2 still exception).
- `TableWindow`: CollectAllLegalResponses + stack-open TryAllowHandPlay GoddessBlocksInterrupt.
- EngineAuthority cite tightened for Respond.
### Bewusst nicht

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
No change to Kevin/Q2/[Ref]/[Q] exceptions; no push; no Kevin Convergence rename as exception.
### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Goddess of Empathy on table -> play Interrupt (e.g. Q2 on stack) -> Amanda Rogers illegal (not in ThinkTray / deny on play).
2. Kevin Uxbridge and Q2 still legal under Goddess.
3. Without Goddess, Amanda still nullifies interrupts as before.
Files: EventRules.cs, EffectRegistry.cs, LegalMoves.cs, EngineAuthority.cs, TableWindow.xaml.cs, artifacts/*.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Gaps Host-Action-Panel stick

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Go: panel follows ship after Fly on/over Gaps in Normal Space. **NO** general auto-dismiss.
### Root

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Relayout/Relocate moved ship + selection frame; `_actionPanel` stayed at mid/old column. Gaps span skipped dockable Relayout (missions only) so ships/panel desynced on span.
### Fix

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- `RepositionHostActionPanelIfAny` - bind panel Canvas L/T to `_selectedCard` (reposition only, never dismisses).
- Call after `RelayoutDockablesUnderMission` + `RelocateShipAlongSpaceline` when ship selected.
- `RelayoutMissionsOnSpaceline` / `RelayoutAllDockables`: landables incl. Gaps (`IsLandableLocation`), pin+Relayout dockables under span.
- KEEP: FlyPick still Clear+SetSelection (exit Fly mode / rebuild buttons) - not dismiss-as-the-fix.
### Bewusst nicht

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
No Fake-Fly; no general menu auto-dismiss after every Fly.
### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Galaxy -> Gaps in Normal Space (Fly): Host action buttons stay glued to the ship (not mid/old).
2. Fly over Gaps to another mission: panel still on ship after arrive.
3. Enterprise / normal mission Fly still OK; Occupancy Badge / Distortion untouched.
Files: TableWindow.xaml.cs, artifacts/FEATURES.md, artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Occupancy Badge UX

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain/Spock Soll-OK. UX only (Host footer). Standing Practice: personnel-present chrome; no glow/split.
### Occupancy Badge (Pepsch lock)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Host footer badges in P1 (cyan) / P2 (orange) color.
- Planet mission + personnel -> label `Away Team`; Ship/Outpost/Station (facility) + personnel -> `Crew`.
- Empty (no personnel) = no badge. Eq/Art/Event/Dilemma = detail only, no badge.
- Both players occupied = two badges side by side (same footer row).
- KEEP: Rogue Borg notice on badge when present; Distortion Field / prior tips untouched.
Files: TableWindow.xaml.cs (UpdateHostBadge / EnsureSideBadge / selection frame), artifacts/FEATURES.md (already ACTIVE), artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe
### Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
1. Planet mission: beam/report P1 personnel onto planet -> footer `Away Team` in P1 color; remove all personnel -> badge gone.
2. Same planet: add P2 personnel too -> two badges side by side (`Away Team` each color); Eq alone on planet -> still no occupancy badge.
3. Ship or Outpost/Station with crew -> footer `Crew` in owner color; dual crew both sides -> side by side; no card-glow / diagonal split.

## Tip detail (Data, Josef, 2026-09-18) - Distortion Field (PR 70 U)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain/Spock Soll-OK. Standing Practice Glossary cites. CODE_PLACEMENT: Events in Rules; UI wire.
### Distortion Field (PR 70 U) - EN Distortion

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Unique (IsPrintedUniqueEvent / IsDistortionField); Plays on Planet; enters play **FACE UP** (blocks immediately); first EOT -> face-down.
- EOT each turn: flip (even while face-down) via EndOfTurnEventRules.ShouldFlipDistortion.
- Face-up: prevents ALL beaming to/from this planet incl. planet-vicinity (landed <-> facility); same-mission gate as Atmospheric Ionization.
- Face-down: beaming allowed.
- Fix: FormatHostEffectSummary was Interrupt confusion ("RANGE may be used to unstop") -> beaming block summary; DetailStatus Debuff (not Buff).
- Verify: EventRules.VerifyDistortionField + EndOfTurnEventRules.VerifyDistortionFlip.
- KEEP: Atmospheric Ionization (5c08269) untouched / no regress.
Files: Game/EventRules.cs, Game/DetailStatusRules.cs, Game/EndOfTurnEventRules.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Alien Probe + Atmospheric Ionization + IPG UX

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain/Spock Soll-OK. Standing Practice Glossary cites folded in. CODE_PLACEMENT: Events in Rules; UI wire.
### IPG Detail-Overkill (UX only)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Armus detail showed 3Ãƒâ€” IPG: green Icons:[IPG] (DetailAttributes), glyph (IconCatalog.Fill), purple Icons:[IPG] (DetailIcons fallback).
**Removed 2:** green Attributes Icons-line + purple DetailIcons Icons-fallback. **Kept:** glyph strip. Cmd/Staffing untouched (FillStaffing).
### Alien Probe (PR 66 U)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Plays on table (Persist.Probe); continuous both hands revealed (HasAlienProbeInPlay Ã¢â€ â€™ hand strip).
- Hand cards not nullifiable until played (CanNullifyTargetCard in NullifyEventInPlay).
- Battle Bridge / used tactics NOT affected (faceDownAlways stays).
- Verify: EventRules.VerifyAlienProbe.
### Atmospheric Ionization (PR 68 C) Ã¢â‚¬â€ EN Ionization

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Unique (IsPrintedUniqueEvent); Plays on Planet; beam 1 at a time; max 3 personnel this way per controller per turn.
- Glossary-Add: to/from this planet includes planet-vicinity beams (landed ship Ã¢â€ â€ planet facility); same-mission gate covers.
- Count increments only after successful beam (NoteIonizationBeam); per-player save fields.
- Verify: EventRules.VerifyAtmosphericIonization.
Files: Game/EventRules.cs, Game/DetailStatusRules.cs, Game/EffectRegistry.cs, Services/GameSave.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Temporal Causality Loop (Glossary-treu)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain/Pepsch Implement-Go. Lock: Glossary-true (NOT Seeds-only). Standing Practice rule cites folded in.
- Decide (`DilemmaRules.Loop`): SCIENCE + CUNNING>35 Ã¢â€ â€™ Overcome +5; else EffectAndEnd + EndTurn + StopTeam (no +5).
- Apply (TW): `_attemptDiscards` log (order+origin+seedOrderHint) from attempt start; holes closed (RemoveEquipmentFromHost, DestroyShipOrFacility, SeniorStaff, IpgNullify, DevilNullify, OvercomeSeed Zone-truth A via discard).
- Fail restore: seeds face-down Encounter-Order (`ReseedInsertIndex`); non-seeds re-play host / legal report / stay discarded; TCL not re-seeded; Attach*/WallFailed untouched.
- EndTurn: `_skipNormalEndOfTurn` skips normal EOT (Compendium 8 / _rb69).
- Verify: `_VerifyTemporal` Ã¢â€ â€™ `DilemmaRules.VerifyTemporalCausalityLoop`.
- Docs: CODE_PLACEMENT + ENGINE Standing Practice (rule cites); CARD_TRACKER partial until Pepsch green.
Files: `Game/DilemmaRules.cs`, `TableWindow.xaml.cs`, `_VerifyTemporal/*`, `artifacts/CODE_PLACEMENT.md`, `artifacts/ENGINE.md`, `artifacts/CARD_TRACKER.md`, `artifacts/HANDOFF.md`.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Foundation first-listed skill (Classification skip)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Foundation-Fix-Go / Spock Rules-OK: first-listed skill Ã¢â€°Â  classification box.
- Root cause: `FirstListedSkill` treated Lackey leading class token in `text` as first skill (Data OFFICERÃ¢â€ â€™wrong).
- Fix shared parse: skip leading `Card.Class` echo(s); next skill (multi-word/xN) = first-listed. Assimilation: Class mismatch Ã¢â€ â€™ former class token is first-listed.
- Apply: strip that skill (multipliers together); restore printed classification if same-named (Bashir MEDICAL x2). Second skill does not slide up.
- ALL first-listed consumers already use `MissionRules.FirstListedSkill` / `ApplyFirstListedSkillLoss` (Tsiolkovsky Apply+Summary).
- Verify: DataÃ¢â€ â€™ENGINEER gone / OFFICER stays; Seskal SCIENCE; Bashir MEDICAL class remains; Sci Physics; cure 3 MEDICAL; not-cumulative.
- KEEP ship Events Positive-only from 948cf0f (untouched).
Files: `Game/MissionRules.cs`, `Game/ModifierRules.cs`, `Game/DilemmaRules.cs`. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Tsiolkovsky Infection Apply + Summary + Events UX

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Fix-Go / Spock: AttachContinue + Cure 3 MEDICAL + no StopTeam already OK.
- Bug Summary: `FormatHostEffectSummary` showed attributes -3 (wrong).
- Apply-Gap: `DisabledSkillsOnHost` only TwoDim Empathy Ã¢â‚¬â€ no first-listed strip for Tsiolkovsky.
- Fix Apply: `MissionRules.FirstListedSkill` + `ModifierRules.ApplyFirstListedSkillLoss` (not cumulative); TW `HostHasTsiolkovsky` / `LoseFirstListedOnHost` wired into Resolve/Summarize/CanSolve/Ctx.
- Fix Summary: host effect = personnel lose first-listed skill (cure: 3 MEDICAL).
- VerifyTsiolkovskyInfection: apply + summary + not-cumulative OK.
- UX: ship detail Events only under Positive (never under Personnel). Debuff Events stay Negative.
Files: `Game/MissionRules.cs`, `Game/ModifierRules.cs`, `Game/DilemmaRules.cs`, `TableWindow.xaml.cs`. CARD_TRACKER partial until Pepsch green. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Tarellian Step0 display polish

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Fix-Go / Pepsch: Logic A/B OK; Bug = dilemmacard missing in Step0 Choose dialog top-left.
- Cause: dilemma `PickYou`/`PickOpp` called `PickCardFromList` without `source`; synthetic Choice cards have no art -> empty slot
- Fix: pass `seedCard` as source (same as `AskChoice` / other card-choice dialogs). Display only; Rules/Picker flow unchanged.
File: `TableWindow.xaml.cs`. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Tarellian Overcome UX A/B

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Captain Fix-Go / Pepsch Soll: two entry points, NOT one flat pool. Rules (Spock) unchanged.
- Schritt 0: A) Medical Personnel OR B) Equipment + Personnel (Choice cards)
- Path A: encountering crew filtered to printed usable MEDICAL (Class OR Skill); beam/sacrifice; no equipment discard
- Path B: MEDICAL-granting eq only (Medical Kit, Medical Tricorder via SkillEquipment); then personnel matching RequiredClass (Kit->OFFICER, Medical Tricorder->SCIENCE); person+eq discard +5
- Plain Tricorder still no MEDICAL / not offered
- VerifyTarellian: Path A then Path B (+ Kit-only, Tricorder class-filter, plain fail)
Files: `Game/DilemmaRules.cs`, `Game/ModifierRules.cs` (EquipmentRequiredClassForSkill). TW untouched. CARD_TRACKER partial until Pepsch green. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Warum 4a61fa1 falsch war

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Pepsch meinte VERTIKALE Linie, nicht horizontal. 4a61fa1 (X-Stagger, Y-Baseline) war Missverstaendnis. SOLL jetzt: eine saubere Spalte unter (P1) / ueber (P2) der Spaceline; N Schiffe nur via `DockSlotOffsetY(slot)`; Left = mission-centered (`DockSlotOffsetX(0)`).

## Pipeline (zwei Straenge, nie im selben Commit)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`

### Sofort -- Pepsch smoke

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
- Response Window UX (Silent Badge, [R] Think Tray, [Space] Pass, Presets)
- Artifact Beaming: Varon-T Planet auf Schiff ohne Treaty-Fehler
- **Dock vertikal** tip: Ships+Outposts gleiche X-Spalte, Y-Slots; Load+live Relayout gleich

### Strang A -- Premiere-Dilemmas (ACTIVE, Pause)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
**Q + REM Fatigue** Data tip landed (partial) â€” Pepsch smoke. Continuum/Q-Flash still PARK.
Next unknown: Q, Radioactive Garbage Scow, Rebel Encounter, (REM Fatigue skip), Sarjenka, Shaka (TCL tip landed Ã¢â‚¬â€ Pepsch smoke). Reminder: Tsiolkovsky/Two-Dim/Wind Dancer already green.
Done prior: **Tarellian Plague Ship** Pepsch green; **Tsiolkovsky** Foundation tip; **Temporal Causality Loop** Data Implement-Go (partial, tip e88860e).

### Strang B -- Welle 2 Extract (artifacts/EXTRACT_REST.md)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Welle 1 Slices 1-9 DONE. Naechstes Ticket wenn Captain Go: **P0-D1 + P0-E1**.

### Spaeter -- Netz (nach P0)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
BoardStore Persist-Wahrheit zuerst. Localhost zwei Exes.

## Parked

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Hugh Borg Ship; IM FindMission; dump@Gaps; Parasites Hotseat-UI; REM Fatigue; Cure-Present-Scope Ship

## Docs map (canon)

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
| File | Owner |
|------|--------|
| HANDOFF.md | Captain + Data (tips) |
| PROJECT.md | Captain + Data |
| ENGINE.md | Data |
| CODE_PLACEMENT.md | Data |
| TABLEWINDOW_INVENTORY.md | Data |
| FEATURES.md | Seven |
| CARD_TRACKER.md | Jadzia |
| RULES.md / RULES_CHECKLIST.md | Spock (+ Seven coverage) |
| GLOSSARY_COVERAGE.md / GLOSSARY_WELLE1.md | Seven / Spock |
| EXTRACT_REST.md | Data / Captain |
| CHANGELOG.md | Data (playable lines) |

Wahrheit: HANDOFF + PROJECT (PROJECT_STATUS entfernt).
Pepsch EXE: Default Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` â€” **nicht** `_build_*`.


## Tip detail (Data, Josef, 2026-09-18) - dock vertical correction

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
Root: 4a61fa1 X-cascade war falsch (Pepsch misspoke "horizontal").
Fix:
- `DockSlotOffsetY(slot)` wieder: Y-Stufen; `DockSlotOffsetX` = 0 (mission-centered)
- Relayout / Relocate / Snap-Preview / RelayoutAll: Top = missionTop + DockSlotOffsetY(i); Left = missionLeft
- Column-Tolerance 45; Z steigt mit Slot; PinÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã¢â‚¬Â¹ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ÃƒÆ’Ã¢â‚¬Å¡Ãƒâ€šÃ‚ÂªPixel Membership; CountDockablesForOwner
- KEEP Load-Pfad 8c88b2d/056f6b0: X-Pin, Relayout after settle, Top nie Save-Y+offset, Pins clear, SpacelineYDefault
File: StarTrekCCG/TableWindow.xaml.cs. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Prior tip (056f6b0) load settle

## Tip (2026-09-20) â€” EOT stale-stack harden
- **tip:** `eec4918`
- **EXE:** Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` LastWriteTime **2026-09-20 13:02:46**
- **Fix:** Start of EOT abandons stale `_stack`; removed DrawCard early-returns before Horga extras; `finally` clears leftover stack + `FinishEndOfTurnDrawExtras` + `CompleteTurnChange`; Resume never leaves Pepsch on resolve-stack during EOT.
- **Smoke:** `GROK_TEMP\SMOKE_EOT_STALE_STACK_2026-09-20.md`
PinDockablesToSpacelineByColumn + Relayout after UpdateLayout + ScheduleRelayoutAfterLoadSettle; Top immer missionTop+DockSlotOffsetY, nie Save-Y.
