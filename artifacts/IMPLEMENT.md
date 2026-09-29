# IMPLEMENT — Ablauf für neue Karte, Regel, Feature

**Stand:** 2026-09-28

Diese Datei ist die **einzige** Anleitung, wie etwas ins Spiel kommt.
Erledigt-Listen stehen nicht hier. Status: `FEATURES.md`, `CARD_TRACKER.md`, Checkliste, Glossar, Appendix A.
Was gerade passiert: `CHANGELOG.md` und kurz `HANDOFF.md`.
Wo der Code heute liegt: `ENGINE.md` und `TABLEWINDOW_INVENTORY.md`.
Prompt: `INSTRUCTION.md`.
Online-Kopie: geänderte `.cs`/`.md` vollständig schreiben (`ONLINE_WORKFLOW.md`).

`RULES.md` und `CODE_PLACEMENT.md` sind ersetzt. Nicht wieder anlegen.

---

## Drei Fragen vor jeder Zeile Code

In dieser Reihenfolge, ohne Ausnahme:

1. **Typ** — gedruckter Kartentyp *oder* „plays as [Typ]“. Die Karte läuft durch dieselbe Engine wie dieser Typ (Interrupt, Event, Dilemma, Artifact, Ship, Facility, Mission, Equipment, Personnel, Doorway). Schon die **erste** „plays as Interrupt“-Karte geht durch die Interrupt-Engine, nicht durch einen Sonderpfad.
2. **Phrase** — wiederkehrender Satz: plays on, nullify, just, countdown, cure, download, report, beam, fly, … Ab der **zweiten** Karte mit derselben Phrase gibt es einen gemeinsamen Dienst, den die Typ-Engine aufruft. Die erste darf noch am Typ hängen; die zweite darf nicht den ersten Sonderpfad kopieren.
3. **Kartenrest** — nur was nach Typ und Phrase einzigartig bleibt. Das darf eine Katalogzeile oder ein Parameter sein, keine zweite Engine.

Neue `*Rules.cs` nur für ein **neues Verb-System**. Dateiname = System (`MovementRules`), nicht Kartenname (`HailRules` nur als Altbestand, bis eine zweite Karte dasselbe Verb erzwingt).

---

## Regel und Errata vor jedem Tip

Nicht aus dem Gedächtnis. Jede Änderung an einer Karte oder Mechanik prüft:

1. Compendium 2.7.4 — der geltende Paragraph.
2. Glossary-Lemma zur Phrase (plays on, just, present, …).
3. Temporary Rulings, falls der Name dort steht.
4. Appendix A Errata **dieser** Karte (`APPENDIX_A_COVERAGE.md` → PDF-Seite).
5. Appendix B nur wenn der Fall dorthin zeigt.

Weicht der Drucktext vom Errata ab, gilt das Errata. Unklar → Pepsch, nicht raten.

---

## Wohin implementieren

Zuerst `ENGINE.md` Schnellindex. Gibt es ein System für Typ oder Phrase, dort erweitern.

Gibt es keins: neues Verb-System (`*Rules.cs`, Dateiname = System), Typ-Engine ruft es auf. Kein Kartennamen-File. Kein namens-`if` nur in `TableWindow`.

Zusätzlich immer der Netz-Roundtrip: eine GameState-Wahrheit auf dem Host; Gast sendet die Aktion; Host Apply; maskierter State zurück. Details: `INSTRUCTION.md` Abschnitt „Netz — eine Wahrheit“.

---

## Ablauf

1. **Pepsch** nennt eine Compendium-Regel, eine Karte, einen Bug oder ein Feature.
2. Steht das schon? `CARD_TRACKER.md`, `FEATURES.md`, `RULES_CHECKLIST.md`, Changelog, Handoff.
3. Soll aus PDF + Errata (Abschnitt oben). Ist aus `ENGINE.md` und dem betroffenen Code.
4. **Vor dem Code** ein Satz: bestehendes System / Merge zweier Pfade / neues System und warum.
5. **Bauen:** Decide in `Game/*Rules`. Apply in `TableWindow`. Legalität `LegalMoves` + `EngineAuthority`. Netz: Gast-Aktion an den Host, ein GameState, Broadcast zurück. Ohne Roundtrip kein fertiger Tip.
6. Nach dem Tip, **bevor Pepsch testet:** `CHANGELOG.md` (spielbare Zeile) + kurzes `HANDOFF.md` + Testbitte. Karte höchstens `partial`.
7. **Erst wenn Pepsch den Test als erfolgreich meldet:** Dokumente nachziehen (nächster Abschnitt).

Ohne den Satz aus Schritt 4 kein Tip.

---

## Nach erfolgreichem Test (Pepsch grün)

Dann, nicht vorher auf `working` / ✅ / DONE:

| Datei | Wann |
|-------|------|
| `CHANGELOG.md` | Zeile auf Grün setzen oder Grün-Datum ergänzen |
| `CARD_TRACKER.md` | betroffene Karte → `working` (oder `partial`, wenn Pepsch nur teilweise bestätigt) |
| `FEATURES.md` | nur bei einem abgeschlossenen großen Thema (Cure-System, Netz-Phase, Plays-on-Vorlage) |
| `RULES_CHECKLIST.md` | Compendium-§-Zelle |
| `GLOSSARY_COVERAGE.md` | Lemma, das die Änderung trifft |
| `APPENDIX_A_COVERAGE.md` | Errata-Zeile der Karte |
| `ENGINE.md` | neuer Code-Ort, neue Datei, neues Verb |
| `TABLEWINDOW_INVENTORY.md` | neuer Apply-Einstieg oder Span/Netz-Pfad |
| `HANDOFF.md` | aktiv → geschlossen; Offen-Liste |
| `ONLINE_WORKFLOW.md` | nur wenn sich die Lieferform ändert |
| `INSTRUCTION.md` / `PROJECT.md` | nur wenn sich Auftrag oder Dateikarte ändert |

Nichts auslassen, nur weil die Datei groß ist. Unberührtes Lemma nicht anfassen.

---
## Suchkommentare im Code

Kommentar an den Decide- oder Apply-Eingang der Mechanik. Kein Aufsatz, kein Big-Bang über unberührte Dateien.

```csharp
// Rule: 7.2.2 dilemma cure present
// Glossary: nullify
// Verb: plays-as interrupt beam battle
```

| Feld | Inhalt |
|------|--------|
| `Rule:` | Compendium-§ (`7.4.1`, `6.5.1`) |
| `Glossary:` / `Errata:` | Lemma oder Errata-Name |
| `Verb:` | Typ und Aktion, klein, mit Bindestrich |

Wortliste (erweitern nur hier, dann in `ENGINE.md` an der Datei wiederholen):

`seed` `play` `plays-as` `plays-on` `report` `download` `beam` `fly` `dock` `staff` `cloak` `attempt` `solve` `dilemma` `cure` `artifact` `event` `interrupt` `battle` `damage` `nullify` `just` `response` `stack` `end-of-turn` `countdown` `stop` `disable` `present` `here` `aboard` `in-play` `unique` `treaty`

Suche: zuerst `ENGINE.md` / `TABLEWINDOW_INVENTORY.md`, dann im Code `Rule:`, `Glossary:`, `Verb:` plus die Wörter aus Pepschs Meldung.

---

## Wohin neuer Code

| Was | Wohin |
|-----|--------|
| Darf ich das jetzt? | `LegalMoves.cs`, `EngineAuthority.cs` |
| Typ Interrupt / plays as Interrupt | `InterruptRules.cs` (Timing-Nullifier: `TimingRules.cs`) |
| Typ Event / plays as Event | `EventRules.cs` |
| Typ Dilemma | `DilemmaRules.cs`, Cure: `DilemmaCureRules.cs` |
| Typ Artifact | `ArtifactRules.cs` |
| Plays on / Host / Ziel | `PlayOnRules.cs`, `TargetQuery.cs`, `TargetingRules.cs` |
| Report / Mix / Treaty | `ReportingRules.cs`, `DualAffiliationRules.cs`, `TreatyRules.cs` |
| Download | `DownloadRules.cs` |
| Fly / RANGE / Staff | `MovementRules.cs` |
| Bewegungshindernis | `MovementHazardRules.cs` |
| Dock | `DockingRules.cs` |
| Battle / Schaden | `BattleRules.cs` |
| Mission versuchen / lösen | `MissionRules.cs` |
| Seed | `SeedRules.cs`, `DeckPlacementRules.cs` |
| Stack / just / Response | `TimingRules.cs` |
| Zugende / until end of turn | `TurnExpiry.cs`, `EndOfTurnEventRules.cs`, `EndOfTurnRestRules.cs` |
| Attribute / Skills am Ort | `ModifierRules.cs` |
| Name → Vorlage | `CardEffectMap.cs`, `EffectRegistry.cs` |
| Ort / Crew / Instanz | `Game/Board/*` |
| Klick, Drop, Overlay, Frage, Paint | `TableWindow.xaml.cs` — nur Anwenden |

Altbestand mit Kartennamen in der Datei (`HailRules`, `WnohgbRules`, `GapsNullifyRules`, `IncomingMessageRules`, `NamedInterruptRules`): nicht vermehren. Nächste gleiche Phrase in das System der Typ-Engine ziehen.

---

## Schicht

```
TableWindow  →  GameAction  →  EngineAuthority(GameState)
                                    → *Rules / EffectRegistry
LegalMoves.Collect / CollectBoth  = dieselbe Quelle für Hotseat, später Netz und KI
```

Decide = reine C#-Entscheidung. Apply in der Oberfläche. Board-Ist in `BoardStore` / Instanzen, sobald die Stelle schon schreibt; UI-Listen sind Spiegel.
