# Smoke — EOT stale-stack harden (after f522875 FAIL)

## Setup
- Horga'hn active on table for P1
- One normal play in PLAY, **no** 2nd play (unused → EOT extra draw)
- End PLAY → EXECUTE → Space End EXECUTE

## Expect
1. Hand **+2** vs start of EOT (normal EOT draw + Horga'hn extra)
2. Session log contains:
   - `Horga'hn extra draw`
   - `EOT complete` → P2 PLAY
3. Segment = **P2 PLAY** — never stuck EXECUTE
4. Multi Space does not stay on EXECUTE
5. If stale stack existed: log `EOT: abandon stale stack` and/or `EOT finally: clear leftover stack`

## Negative
- Extra play used in PLAY → only **1** EOT draw (no Horga extra), still P2 PLAY