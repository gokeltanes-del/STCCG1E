# STCCG 1E - Features backlog
Last updated: 2026-09-18

Living list - **Seven owns ranking.**
See also: HANDOFF.md, PROJECT.md, RULES_CHECKLIST.md, CARD_TRACKER.md (Jadzia).

## P0 - Foundation (do first)
- **TableWindow extract** - Welle 1 Slices 1-9 DONE; Welle 2 tickets in `artifacts/EXTRACT_REST.md` (next: **P0-D1 + P0-E1** when Captain Go). Persist/Battle **ticketed** (not deferred).
  - Premiere A card waves: **only Captain Go**
- **Status-UX / Stasis-Held / Detail** - **DONE** tip `bf1f2ab`
- **AT-Detail Effekt-Gruppen + Firestorm EFFECT** - **DONE** (Pepsch green 2026-09-12)
  - Parasites-Strip name labels: `2976b61`
  - Stopped/Quarantined/Stasis Gruppen (kein Label unter jeder Mini): `702f644`
  - Firestorm Overlay `EFFECT - attempt continues` (Love Interest bleibt RELOCATED): `87e297d`
- **Artifact Beaming / Affiliation-Free** - **CODED** (`TreatyRules.CanOccupyHost` / `CardsCompatibleUnderTreaties`); Pepsch smoke still open (not fully green)
- **Response Window (Hotseat UX)** - **CODED** (Silent Badge, Think Tray `[R]`, Pass Space, Presets 2s/3s/5s/10s); Pepsch smoke still open (not fully green)
- **Dilemma Cure System 7.2.2.3** - **DONE** (Pepsch green: Archer, Alien Abduction, Phased Matter 2026-09-13; Fix Team-Stop / Curable AttachContinue 7.2.2.3 & 7.2.6)
- **Staffing/Fly/Battle Gaps G2-G7** - **DONE** (`2cd5bc8`)
- **Glossary/Compendium** - Welle 2 Control/Owner/Present ACTIVE (Docs)
- **Rule cites (Code + Detailfenster)** - **ACTIVE** (Pepsch 2026-09-18)
  - Stehende Praxis ab sofort: Decide/Apply kommentieren mit Compendium-§ und/oder Glossary-Eintrag (z.B. `// Compendium 7.1.2` / `// Glossary: beaming`).
  - Detailfenster / Action-Hinweis: wenn die Engine handelt, kurz sagen nach welcher Regel (Glossary/Compendium) — nicht nur was passiert.
  - Retrofit: bestehende Stellen nachziehen (teilweise schon da); kein Big-Bang, mit Karten-/Extract-Arbeit mitführen. Data bei jedem neuen Tip; Coverage bei Seven.
- **Occupancy Badge UX** - **CODED** tip `034ee39` (Pepsch smoke pending; DONE only after Pepsch green)
- **Gaps Host-Action-Panel stick** - **CODED** (Pepsch smoke: Galaxy->Gaps; panel follows ship; NO auto-dismiss)
  - Host footer badges in P1/P2 color; Planet = Away Team, Ship/Outpost/Station = Crew; personnel only; dual badges if both sides; no glow.
- **Premiere Dilemma wave** ACTIVE (einzeln; next Captain Go). **Temporal Causality Loop** **DONE** Pepsch green `e88860e`

## P1 - Parked
- Microvirus Opp-Chooser / DNA-Filter
- REM Fatigue
- Cure-Present-Scope (Ship)
- Alien Parasites Hotseat-Chooser

## Done recently
- Temporal Causality Loop DONE `e88860e` (Pepsch green 2026-09-18)
- Impassable Door DONE `be5062b`
- Hyper-Aging Quarantaene DONE `46eab15`
- Alien Parasites Neg Control `f087866` (Pepsch green)
- Space-Attempt-Crew-Scope `170437a`
- Status-UX `bf1f2ab`
- Staffing/Battle G2-G7

## Notes
- Private, non-commercial
- BoardStore + GameState = truth; TableWindow = view
- Workflow: Josef edit+commit only; Pepsch tests then pushes (no bot-push)
- **Rule cites:** Code-Kommentar = wo welche Regel sitzt; Detailfenster = welche Regel gerade gegriffen hat. Kein DONE ohne Pepsch green wenn UX betroffen.
