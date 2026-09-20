# Tip Pattern Enhancers (Josef)

tip: 500c94e
EXE: 2026-09-20 19:12:12
msg: feat(premiere): Pattern Enhancers owner-scoped beam ignore

## Code
- `HasPatternEnhancers(player)` via `_attachedEvents.Owner` / `PlayerHasTableCard`
- `CanBeamAtMission(..., beamingPlayer)` early-OK only for PE owner
- `PlanetBeamBlockedAt` ignores Particle Scattering for PE owner
- Barclay beam-block ignored for PE owner (just-beamed / beam-target effects)
- EventRules Message = Pepsch printed text
- Kevin nullify unchanged (table Event)

## Smoke
See SMOKE_PATTERN_ENHANCERS_2026-09-20.md
