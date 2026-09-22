# Smoke: Quick Game — Artifacts nicht als Spaceline-Knoten

**Tip:** (lokal, nach Commit) Quick-Game Seed respektiert Artifact-Limits; kein Orphan auf SpacelineY.
**Regression:** `be7d6a7` ließ bei Limit-Fail die Karte sichtbar auf Mission-Koordinaten liegen.

## Vorbereitung
1. Laufende `StarTrekCCG.exe` beenden, neu bauen/starten (sonst alte DLL).
2. Quick Game ×3 mit Decks, die Thought Maker und/oder Interphase Generator seeden.

## Checks (jedes der 3 Spiele)
- [ ] Thought Maker erscheint **nicht** als eigener Spaceline-Knoten (kein Span wie Mission).
- [ ] Interphase Generator erscheint **nicht** als eigener Spaceline-Knoten.
- [ ] Artifacts nur unter Planeten-Missionen (Seed-Badge / aufdecken unter Mission).
- [ ] Outposts weiterhin normal unter Missionen (Dock).
- [ ] Keine sichtbare Artifact-Karte auf der Spaceline-Y-Höhe neben Missionen.

## Ergebnis
| Lauf | Pass/Fail | Notiz |
|------|-----------|-------|
| 1 |  |  |
| 2 |  |  |
| 3 |  |  |

Pepsch: Grün → Jadzia/Captain; Fail → Screenshot + welcher Artifact wo.
