# Smoke — Horga 2nd PLAY (turn flip already GREEN)

## 1) Path B (extra draw)
Horga on table, 1 play only, End PLAY → Execute → EOT: **2 cards**, log `Horga'hn extra draw`. Stay GREEN.

## 2) Path A (2nd play) — was FAIL
Horga on table:
1. Play card A in Play → log `Horga'hn: 2nd play available or End PLAY for extra EOT draw`; banner stays **PLAY**
2. Play card B in Play → log `Horga'hn extra play`; advance to Execute
3. EOT: **1 card**, **no** `Horga'hn extra draw`

## Notes
HasHorgahn = table OR `_horgahnP1/P2`; gates use `_session.ActivePlayer`.