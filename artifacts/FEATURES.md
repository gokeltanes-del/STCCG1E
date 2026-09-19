# Compendium 2.7.4 — Features backlog

Quelle: `rules/Compendium_Rulebook.pdf`. Prozess: `RULES.md`. Feinliste: `RULES_CHECKLIST.md`.
Stand: 2026-09-19. Living list — **Seven owns ranking.**

✅ spielbar (Pepsch green) · 🟡 CODED / ACTIVE / partial · ❌ offen · ➖ geparkt / später

Siehe auch: `HANDOFF.md`, `PROJECT.md`, `CARD_TRACKER.md` (Jadzia), `EXTRACT_REST.md`.
Status-Updates nur aus bekanntem Pepsch-green / HANDOFF — **kein DONE ohne Beleg.**
Ranking (Pepsch/Captain 2026-09-19): **Premiere CARD_TRACKER-Welle ist P0-top**; Extract P0-D1/E1/S1 nachrangig, bis der Premiere-Tracker leer von unknown/partial ist.

---

## P0 — Premiere wave zuerst (dann Extract / Smoke)

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 7.2 | Premiere Dilemma wave | 🟡 | **P0-top.** ACTIVE einzeln; Pause bei **#26 Q** bis Captain Go. Next unknown: Q; REM Fatigue skip/park. (RGS / Rebel / Sarjenka / Shaka / TCL already working in CARD_TRACKER — nicht neu grün markiert.) |
| — | Premiere A card waves | 🟡 | **P0-top / Captain Go.** Aktive Premiere-Prio (nicht ➖). CARD_TRACKER unknown/partial zuerst; nicht in Extract-Commits mischen |
| P0-D1 | TableWindow extract: `AttachedDilemma` → Board | ❌ | Nachrangig bis Premiere-Tracker leer von unknown/partial (Captain 2026-09-19). `EXTRACT_REST.md`; Welle 1 Slices 1–9 DONE |
| P0-E1 | TableWindow extract: `AttachedEvent` → Board | ❌ | Nachrangig bis Premiere-Tracker leer von unknown/partial (Captain 2026-09-19). Persist/Battle ticketed (not deferred) |
| P0-S1 | Dual-Run BoardStore abschließen | ❌ | Nachrangig bis Premiere-Tracker leer von unknown/partial (Captain 2026-09-19). EXTRACT_REST; nach ersten Persist-Ticks |
| UX | Status-UX / Stasis-Held / Detail | ✅ | DONE tip `bf1f2ab` |
| UX / 7.2.2 | AT-Detail Effekt-Gruppen + Firestorm EFFECT | ✅ | Pepsch green 2026-09-12; Parasites-Strip `2976b61`; Stopped/Quarantined/Stasis `702f644`; Firestorm Overlay `87e297d` |
| 7.1.1 | Artifact Beaming / Affiliation-Free | 🟡 | CODED `TreatyRules.CanOccupyHost` / `CardsCompatibleUnderTreaties`; Varon-T Karte Pepsch green 2026-09-12; **Beaming-smoke laut FEATURES weiter offen** |
| 6.5.1 / 5 | Response Window (Hotseat UX) | 🟡 | CODED Silent Badge, Think Tray `[R]`, Pass Space, Presets 2s/3s/5s/10s; **Pepsch smoke still open** |
| 7.2.2.3 | Dilemma Cure System | ✅ | DONE Pepsch green: Archer, Alien Abduction, Phased Matter 2026-09-13; Fix Team-Stop / Curable AttachContinue 7.2.2.3 & 7.2.6 |
| 7.1.3 / 7.4 | Staffing/Fly/Battle Gaps G2–G7 | ✅ | DONE `2cd5bc8` |
| 12.3 / 12.4 | Glossary/Compendium Welle 2 Control/Owner/Present | 🟡 | ACTIVE (Docs); Seven + Spock |
| — | Rule cites (Code + Detailfenster) | 🟡 | ACTIVE Pepsch 2026-09-18 Standing Practice; Decide/Apply mit Compendium-§ / Glossary-Lemma; Retrofit kein Big-Bang; Coverage bei Seven |
| UX / 7.0.1 | Occupancy Badge UX | 🟡 | CODED tip `034ee39` (Pepsch lock, smoke pending). Host footer P1 cyan / P2 orange; Planet=Away Team, Ship/Outpost/Station=Crew; personnel only; dual badges; no glow. **DONE only after Pepsch green** |
| Glossary TCL | Temporal Causality Loop | ✅ | DONE Pepsch green `e88860e` (Glossary-treu, skip EOT / Compendium 8) |
| UX / 7.1.4 | Dock vertikal (Ships+Outposts gleiche X-Spalte) | 🟡 | Handoff Sofort-Smoke; Korrektur nach 4a61fa1; nicht als Pepsch-green in FEATURES geführt |

## P1 — Parked

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 7.2.2 / Microvirus | Microvirus Opp-Chooser / DNA-Filter | ➖ | Parked; Karte selbst CARD_TRACKER working (Pos+neg Choose OK) |
| 7.2.2 / REM | REM Fatigue | ➖ | Parked; Pause der Dilemma-Welle |
| 7.2.2.3 | Cure-Present-Scope (Ship) | ➖ | Parked; Ktarian ship-hosted cure already narrowed |
| 7.2.2 / Parasites | Alien Parasites Hotseat-Chooser | ➖ | Parked; Neg Control `f087866` Pepsch green, Chooser PARK |
| 11.1 | Hugh vs Borg Ship (extract P1 / P4-H1) | ➖ | Hugh CanRespond Dilemma-only DONE; Borg Ship EOT still EXTRACT_REST |
| 7.10 | IM FindMission | ➖ | Parked |
| 7.1.5 | dump@Gaps | ➖ | Parked |

## Done recently (Pepsch green / belegt)

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| Glossary TCL | Temporal Causality Loop | ✅ | `e88860e` Pepsch green 2026-09-18 |
| 7.2.2 | Impassable Door | ✅ | `be5062b` |
| 10.2.7 | Hyper-Aging Quarantäne | ✅ | `46eab15` (Karte); allgemeine §10.2.7 weiter 🟡 |
| 7.2.2 | Alien Parasites Neg Control | ✅ | `f087866` (Chooser bleibt parked) |
| 7.2.1 | Space-Attempt-Crew-Scope | ✅ | `170437a` |
| UX | Status-UX | ✅ | `bf1f2ab` |
| 7.1.3 / 7.4 | Staffing/Battle G2–G7 | ✅ | `2cd5bc8` |
| 7.1.1 / Distortion | Distortion Field (PR 70 U) | ✅ | `a866bbe` Pepsch green; EOT flip; vicinity beam block |
| 2.4 / Ionization | Atmospheric Ionization | ✅ | `5c08269` Pepsch green |
| 2.4 / Probe | Alien Probe | 🟡 | `5c08269` CODED; CARD_TRACKER partial — no Pepsch green yet |
| 7.2.2 | Tarellian / Tsiolkovsky / TwoDim / Wind Dancer / Shaka / Sarjenka / Rebel / TCL | ✅ | CARD_TRACKER working, Pepsch 2026-09-18 |

## Notes

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| — | Private, non-commercial | ➖ | — |
| — | BoardStore + GameState = truth; TableWindow = view | ➖ | Standing architecture |
| — | Workflow Josef edit+commit; Pepsch tests then pushes | ➖ | no bot-push |
| — | Rule cites | 🟡 | Code-Kommentar = wo welche Regel sitzt; Detailfenster = welche Regel gerade gegriffen hat. Kein DONE ohne Pepsch green wenn UX betroffen. |
