# STCCG 1E - Features backlog
Last updated: 2026-09-08 21:30
Living list — **Seven owns ranking.**

**Branch:** nur `master`.

## P0
- **AT/Mission-Detail Effekt-Gruppen** — **DONE** 2026-09-08 (Pepsch Soll)
  - Header je Effekt-Set: `Stopped (7)`, `Quarantined (4)`, `Quarantined + Stopped (2)`
  - Sort: mehr Negative zuerst. Kein Label unter jeder Mini.
  - Dilemma-Karten bleiben unter `Negative`. Mission-Quarantine-Zeile oben bleibt.
  - Decide: `DetailStatusRules`; Apply: `TableWindow` host-strip
- **Dilemma-Overlay EffectAndContinue** — **DONE**: Love Interest / relocate-Text = `RELOCATED`; sonst `EFFECT - attempt continues`
- **TableWindow extract** — Persist/Battle DEFERRED
- **Premiere Dilemma wave** ACTIVE
  - Pepsch green: Impassable Door, Hyper-Aging, Alien Parasites (2026-09-08), Firestorm kills OK

## P1 Parked
- Alien Parasites Hotseat-Chooser / volle Opp-Control-UI
- Microvirus Opp-Chooser UI thin
- REM Fatigue; Cure-Present-Scope (Ship)
- Response Window UX; Cloak optional transparenter; Mission last-revealed Button

## Notes
- Workflow: master only. Pepsch: VS master → Git Pull → Rebuild.
