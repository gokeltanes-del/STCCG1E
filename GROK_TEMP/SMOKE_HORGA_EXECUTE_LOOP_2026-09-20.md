# Smoke — Horga EXECUTE loop (2026-09-20)
Tip: PENDING · EXE Debug must be AFTER tip (check LastWriteTime)

**Ursache:** Space bei offenem/geschlossenem EOT-Draw-Stack rief erneut volles FinishExecuteAndEndTurn auf → jede Runde Draw, Segment blieb EXECUTE, P2 nie.

**Fix:** `_eotEndingInProgress` + ResumeEndOfTurnAfterDrawResponses; Space → Resume → CompleteTurnChange. Nie zweites Voll-EOT.

| # | Check | Soll | Smoke |
|---|-------|------|-------|
| 1 | Horga, 1 Play, End PLAY, Space in EXECUTE | Ein EOT-Draw (+optional Horga extra), dann **P2 PLAY** | |
| 2 | Mehrfach Space in EXECUTE | Kein weiterer Draw-Loop; nach Pass → P2 | |
| 3 | Extra nur Play | Unverändert | |
