# Smoke — EOT double-flip + EndTurn Match (after e71ec12 FAIL)

## History must show
1. P1 EXECUTE Space → `Turn ended` + `EOT complete Match=… (was P1) → P2 PLAY` (+ `Horga'hn extra draw` if unused)
2. Later P2 EXECUTE Space → `was P2 → P1 PLAY`
3. Never `was P1 → P1 PLAY`
4. Never `CompleteTurnChange re-entrancy ignored` on happy path (ok if rare)

## Banner
After one Space from P1 EXECUTE: **P2 PLAY**. Over rounds both players get EOT draws.