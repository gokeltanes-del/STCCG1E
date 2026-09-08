# STCCG 1E - Features backlog
Last updated: 2026-09-08 Abend (Pepsch test)
Living list — **Seven owns ranking.**
Siehe HANDOFF.md, PROJECT.md, RULES_CHECKLIST.md, CARD_TRACKER.md.

**Branch:** nur `master`. Captain Grok pusht auf master. Kein GrokTest mehr.

## Next bugfix (Pepsch 2026-09-08 — mitnehmen, nicht eigener Chat)
- **AT/Mission-Detail: Effekt-Gruppen** (Pepsch Vorschlag, Data-Soll)
  - Personal nach *Effekt-Set* gruppieren, nicht jede Mini einzeln beschriften.
  - Eine Gruppe = gleiche negative (und ggf. positive) Effekte. Beispiel: alle nur-Stopped zusammen; alle nur-Quarantined zusammen.
  - Karte mit mehreren Debuffs = eigene Gruppe (Stopped+Quarantined != nur Stopped).
  - Gruppen sortieren nach Anzahl negativer Effekte (mehr zuerst), dann Name.
  - Gruppenkopf z.B. `Stopped (7)` / `Quarantined (4)` — Label nicht unter jeder Karte wiederholen.
  - Positive Effekte analog, falls vorhanden.
  - Ist-Stand Screenshot: unter jeder Mini `Stopped` / `Quarantined`; Quarantine-Zeile oben rot ist ok als Mission-Summary.
- **Dilemma-Overlay Titel** `RELOCATED - attempt continues` bei Firestorm/Armus = falsch (Fate.EffectAndContinue-Label von Love Interest). Soll: `EFFECT - attempt continues`; Love Interest bleibt RELOCATED.

## P0
- **TableWindow extract** — Welle 1 Slices 1-9 DONE; Persist/Battle **DEFERRED**
  - Neue Premiere-Karte nur Captain Go
- **Status-UX / Debuff grouping** — Mini-Labels DONE 2026-09-06 (`bf1f2ab`). **Away-Team Gruppen-UI: OPEN** (siehe Next bugfix)
- **Staffing/Fly/Battle G2-G7** — **DONE** (G7 `2cd5bc8`)
- **Glossary/Compendium** — Welle 2 Control/Owner/Present ACTIVE (Docs)
- **Premiere Dilemma wave** — ACTIVE einzeln
  - Last green: Impassable Door `be5062b`, Hyper-Aging `46eab15`, **Alien Parasites** Pepsch 2026-09-08 (Dilemma + Anzeige)
  - Firestorm: Kills pepsch-ok; Overlay-Titel Relocated = UI-only

## P1 Parked
- Alien Parasites Hotseat-Chooser / volle Opp-Control-UI (Min-Pfad green)
- Microvirus Opp-Chooser / DNA-Filter (UI thin)
- REM Fatigue
- Cure-Present-Scope (Ship) — Menthar/Junior/Ktarian: Skills an Bord, nicht ortsweit
- IM Fed FindMissionForDockable / dump@Gaps / Distortion / Fed 7.4.1

## P2
- Premiere A/B aus Checklist-Luecken
- Unklare Karten parken fuer Pepsch
- Kein Fundamentalsystem auf Verdacht

## P3 Later
- Response Window UX
- Ship visual states
- Sets / Netz / KI

## Done recently
- Alien Parasites Pepsch green 2026-09-08 (engine `f087866` + Anzeige)
- Impassable Door DONE `be5062b`
- Hyper-Aging Quarantaene DONE `46eab15`
- Space-Attempt-Crew-Scope `170437a`
- Status-UX Labels `bf1f2ab` (Gruppen-UI noch offen)
- Staffing/Battle G2-G7
- Continue-Familie: Nitrium, Hyper-Aging, Love Interests, Alien Abduction
- Neural Servo + Side-Sync; Hologram Ruse; Barclay
- Hugh/Borg Ship; Gaps nullify relocate; E1-E6; Extract 5-8

## Notes
- Privat, nicht-kommerziell
- BoardStore + GameState = Wahrheit; TableWindow = View
- Workflow: alles auf master. Pepsch: VS auf master, Git Pull.
