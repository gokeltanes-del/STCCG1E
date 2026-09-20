# SOLL Pattern Enhancers (Pepsch Go 2026-09-20) — Spock Lock

Pepsch Text:
Plays on table. You may ignore any effect on a dilemma, event, or mission that prevents beaming or targets your just-beamed personnel or equipment.

## Regeln (Spock)
1. Plays on table. Kevin nullify OK.
2. Owner-scoped („You“ / „your just-beamed“).
3. Prevent beaming Premiere: Distortion Field = ja. Atmospheric Ionization = nein (Limit, kein Prevent) — PE bypassed Ionization NICHT.
4. Just-beamed targets Premiere: keine Printed-Treffer; Hook trotzdem. Nicht Portal Guard / Tarellian.
5. Keine free beam range / andere Restrictions bleiben.

## Ist
Partial: Persist + HasPatternEnhancers (global!) + CanBeamAtMission bypass Distortion UND Ionization (Ionization falsch).

## Fix
- HasPatternEnhancers(player) owner-scoped
- Bypass nur echte Prevents (Distortion); Ionization unverändert
- just-beamed Hook (no-op Premiere)
- Message Pepsch-Text; HANDOFF tip SHORT

Tip: feat(premiere): Pattern Enhancers owner-scoped beam ignore
Josef kein Push.
