# SOLL Thought Maker — UPDATE Pepsch 2026-09-20 16:25

Pepsch FAIL-Smoke: „Plays as interrupt funktioniert nicht, ich kannst nicht während dem gegnerischen turn spielen.“

## Lock
- Plays as **[Interrupt]** (nicht Event). Pepsch überschreibt Spock/App-Event-Annahme.
- Timing: **at any time** wie Interrupt/Doorway — auch im Gegnerzug, auch außerhalb Play-Segment (Stack leer).
- Effekt unverändert: Name card type → alle Matches aus Opp-Draw shuffeln → bottom; Artifact discard.
- Nullify: **Amanda** (Interrupt), nicht Kevin.

## Ist-Bug
Thought Maker in `IsPlaysAsEventFromHand` → `UsesNormalCardPlay` true → nur eigener Zug / Play-Segment. PlaceOnTablePermanents nutzt `_activePlayer`.

## Fix (Data)
1. `IsPlaysAsInterruptFromHand` (Thought Maker); aus `IsPlaysAsEventFromHand` raus.
2. `TryAllowHandPlay`: Interrupt-Pfad wenn `IsPlaysAsInterruptFromHand` (wie `!UsesNormalCardPlay`).
3. Drop / `PlaceOnTablePermanents`: wie Interrupt → `BeginPlayCardStack(..., controllerOverride: handOwner)` — nicht nur `_activePlayer`.
4. `PlayOnRules` Role = ArtifactAsInterrupt; Message Pepsch Interrupt.
5. Resolve bleibt `TryResolveArtifactHandPlay` Thought-Maker-Block.
6. Smoke: Gegnerzug, Hand sichtbar, TM droppen → Typ wählen → Opp-Draw strip → Discard.

Tip: fix(premiere): Thought Maker Plays as Interrupt anytime
Josef kein Push.
