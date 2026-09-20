# Smoke — EOT nuclear simplify (after 16faa2a FAIL)

## Setup
Horga'hn on table P1. One normal play in PLAY, no 2nd. End PLAY → EXECUTE → **one** Space.

## Expect (History / banner)
1. `Horga'hn extra draw` (if unused 2nd play)
2. `EOT complete (was P1) → P2 PLAY`
3. Banner shows **P2 PLAY** immediately after that Space (not stuck EXECUTE)
4. Hand +2 vs start of EOT when unused play

## Negative
Extra play used in PLAY → only 1 EOT draw, still P2 PLAY banner.