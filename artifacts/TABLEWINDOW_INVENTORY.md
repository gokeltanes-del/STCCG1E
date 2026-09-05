# TableWindow Inventory (Extract prep)

Last updated: 2026-09-05  
Source: `StarTrekCCG/TableWindow.xaml.cs` (~19921 lines, ~581 private methods)  
Owner: Data Ã¢â‚¬â€ after Foundation E1Ã¢â‚¬â€œE6; before Premiere A/B card waves.  
Scope: catalog altcode / per-card Apply hooks to pull into `Game/*Rules`, BoardStore, templates. **No big-bang split.**

## Priority extract clusters (effect-ish)

| Area | Methods (line approx.) | Target layer |
|------|------------------------|--------------|
| Interrupts hand play | `TryPlayInterruptFromHand`, `TryPlayWormholeFromHand` | InterruptRules + TargetQuery |
| Events instant/named | `ApplyInstantEvent`, `ApplyNamedAuInterrupt` | EventRules / EffectRegistry |
| Kevin / Hugh / Lore | `ApplyKevinConvergence`, `ApplyHugh`, Rogue EOT | Interrupt/Dilemma Rules |
| Dilemmas | `ApplyDilemmaResult`, start/EOT dilemma processors | DilemmaRules |
| Artifacts | `ApplyArtifactAcquire` | ArtifactRules |
| IM / Required move | `ApplyIncomingMessage`, `ProcessIncomingMessageMoves`, arrival | InterruptRules + RequiredMoveRules (hops done E6) |
| Movement hazards | `CheckEventMovement`, `ApplyEventAfterMove` (Gaps/Rift/Q-Net/Tetryon) | EventRules onEnter |
| Battle | Ship/Personnel begin + resolve | BattleRules |
| Beam / Fly UI | `BeginBeamMode`, `BeginFlyHighlight` | stay View; Authority already |
| EOT processors | repairs, dilemmas, Borg ship, Rogue Borg, events | TurnExpiry / *Rules |

## Persist kinds still branched in TableWindow (32)

AntiTime, Baryon, CaptainsLog, Distortion, Espionage, Gaps, Goddess, IncomingMessage, IntruderField, Ionization, Kidnappers, Klim, LoreReturns, LowerDecks, NeuralServo, ParticleScatter, PatternEnhancers, PlasmaFire, QNet, RaiseStakes, RedAlert, Rift, Spacedock, StaticWarp, Supernova, Table, Tetryon, Thermal, Traveler, WarpCore, YellowAlert (+ None)

## NameIs hardcodes in TableWindow (5)

Asteroid Sanctuary, Distortion of Space/Time Continuum, Gaps in Normal Space, Q-Net, Tachyon Detection Grid

## Parked smoke bugs (not this inventory)


0. Hugh Borg Ship Dilemma branch (parked)
1. Gaps Host/Host2 kill Ã¢â‚¬â€ **fixed** `f47469b` (await Pepsch retest/push)
2. Dump omits ships on Gaps
3. IM false already-at-facility
4. Wormhole broken (Pepsch) Ã¢â‚¬â€ addressed in Slice 2 locally; retest
5. Fed 7.4.1; E2b Treaty/Rogue store staffing

## Suggested extract order (Captain may reorder)

1. Movement onEnter hazards Ã¢â‚¬â€ **Slice 1 DONE** (`MovementHazardRules`)
2. Wormhole pair play Ã¢â‚¬â€ **Slice 2 DONE** (`InterruptRules` gates + sync/hit-test)
3. Incoming Message apply/arrival (facility lookup) - **Slice 3 DONE** (`IncomingMessageRules`)
4. Kevin/Hugh/LoreReturns Apply blocks - **Slice 4 DONE** (`EventRules` Lore/Kevin + `InterruptRules.DecideHugh`)
5. ApplyInstantEvent / ApplyNamedAuInterrupt - **Slice 5 DONE** (`InstantEventRules` / `NamedInterruptRules`)
6. Dilemma/Artifact Apply remnants - **Slice 6 DONE** (`DilemmaRules` apply gates / `ArtifactRules` placement)
7. Only then Premiere A card waves

## Tip note after extracts

Slices 1-6 decide gates extracted; local tip ahead of origin. **NO PUSH** until Pepsch retest green. Parked: Hugh Borg Ship Dilemma; IM FindMissionForDockable; dump@Gaps.

## Deliberately not in first extract wave

Paint/layout, Snap/Halo, Deck Builder, Save UI, Hotseat chrome, card zoom.
