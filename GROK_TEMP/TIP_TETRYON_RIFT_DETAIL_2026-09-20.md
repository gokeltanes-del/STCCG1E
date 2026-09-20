# Tip Tetryon / Rift UX (Josef)

tip: 2d6ea3b
EXE: 2026-09-20 20:08:20
msg: fix(ui): Tetryon fly-by ShowPlayError; Rift+Tetryon Debuff tone

## Changes
- DetailStatusRules.ToneForEvent: Persist.Tetryon + Persist.Rift → Debuff (planet Negative)
- TryMoveShip: CheckEventMovement block → ShowPlayError (not StatusText-only)
- Tracker: Q-Net, Rift, Treaties F/K F/R R/K, Tetryon → working
