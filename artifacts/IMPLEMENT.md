
## Online-Einzel-KI

Bei Arbeit in Grok Projekten / Cursor: geänderte .cs/.md immer **vollständig** in den Projektordner schreiben (siehe ONLINE_WORKFLOW.md). Ablauf unten bleibt gleich.


# IMPLEMENT — Ablauf für neue Karte, Regel, Feature

**Owner:** Captain (Ablauf) + Data (Code-Ort)  
**Stand:** 2026-09-23

Diese Datei ist die **einzige** Anleitung, wie etwas ins Spiel kommt.  
Erledigt-Listen stehen nicht hier. Status: `FEATURES.md`, `CARD_TRACKER.md`, Checkliste, Glossar, Appendix A, `EXTRACT_REST.md`.  
Was gerade passiert: `CHANGELOG.md` und kurz `HANDOFF.md`.  
Wo der Code heute liegt: `ENGINE.md` und `TABLEWINDOW_INVENTORY.md`.

`RULES.md` und `CODE_PLACEMENT.md` sind ersetzt. Nicht wieder anlegen.

---

## Drei Fragen vor jeder Zeile Code

In dieser Reihenfolge, ohne Ausnahme:

1. **Typ** — gedruckter Kartentyp *oder* „plays as [Typ]“. Die Karte läuft durch dieselbe Engine wie dieser Typ (Interrupt, Event, Dilemma, Artifact, Ship, Facility, Mission, Equipment, Personnel, Doorway). Schon die **erste** „plays as Interrupt“-Karte geht durch die Interrupt-Engine, nicht durch einen Sonderpfad.
2. **Phrase** — wiederkehrender Satz: plays on, nullify, just, countdown, cure, download, report, beam, fly, … Ab der **zweiten** Karte mit derselben Phrase gibt es einen gemeinsamen Dienst, den die Typ-Engine aufruft. Die erste darf noch am Typ hängen; die zweite darf nicht den ersten Sonderpfad kopieren.
3. **Kartenrest** — nur was nach Typ und Phrase einzigartig bleibt. Das darf eine Katalogzeile oder ein Parameter sein, keine zweite Engine.

Neue `*Rules.cs` nur für ein **neues Verb-System**. Dateiname = System (`MovementRules`), nicht Kartenname (`HailRules` nur als Altbestand, bis eine zweite Karte dasselbe Verb erzwingt).

---

## Ablauf

1. **Pepsch** sagt Captain: neue Compendium-Regel, neue Karte oder neues Feature.
2. **Captain** mit **Jadzia** und **Seven**: steht das schon? Karte → `CARD_TRACKER.md` (`working` / `partial` / `unknown` / …). Thema → `FEATURES.md`. Regelabschnitt → `RULES_CHECKLIST.md`. Fertige Systeme nicht noch einmal bauen.
3. **Spock**: gültige Regel aus `artifacts/rules/Compendium_Rulebook.pdf`. Lookup nur als Wegweiser ins PDF:
   - `RULES_CHECKLIST.md`
   - Glossar im PDF / Index `GLOSSARY_COVERAGE.md`
   - Temporary Rulings im PDF
   - Appendix A im PDF / Index `APPENDIX_A_COVERAGE.md`
   - Appendix B im PDF  
   Spock nennt Ist gegen Soll und die Suchwörter (`Rule:`, `Glossary:`, `Verb:`).
4. **Data vor dem Code**, schriftlich ein Satz:
   - bestehendes System (Datei + Verb), oder
   - zwei Einzelpfade, die jetzt zusammengeführt werden, oder
   - neues System und warum keine vorhandene `*Rules` reicht.
5. **Data baut:** Entscheiden in `Game/*Rules` (kein WPF). Anwenden, Fragen, Aufdecken, Schaden, Neuzeichnen in `TableWindow`. Legalität über `LegalMoves` + `EngineAuthority`. Ort/Crew möglichst `Game/Board`. Keine neue Wirkung nur als `if (name == …)` in der Tischdatei.
6. **Nachziehen:**
   - `CHANGELOG.md` — eine Zeile, was spielbar geändert wurde
   - betroffene Statusdatei (Tracker, Features, Checkliste, Glossar, Appendix A, Extract)
   - `HANDOFF.md` — aktiv / offen / geschlossen
   - `ENGINE.md` und `TABLEWINDOW_INVENTORY.md` nur wenn sich die Landkarte ändert

Ohne den Satz aus Schritt 4 kein Tip.

---

## Suchkommentare im Code

Data schreibt sie an den Decide- oder Apply-Eingang der Mechanik. Die Wörter liefert Spock. Kein Aufsatz, kein Big-Bang über unberührte Dateien.

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
