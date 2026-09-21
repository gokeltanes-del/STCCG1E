# ENGINE — Ist-Landkarte des Codes

**Owner:** Data  
**Stand:** 2026-09-21  
**Pfad:** `StarTrekCCG/` (Live auf Josef, Kopie unter `artifacts/StarTrekCCG/`)

Nur der **aktuelle** Code. Keine Erledigt-Liste, kein Plan.  
Offenes: `EXTRACT_REST.md`, `FEATURES.md`, Tracker.  
Neuer Code: `IMPLEMENT.md`.  
Tischdatei: `TABLEWINDOW_INVENTORY.md`.

Stichworte in Klammern = Suchkommentare `Verb:` / `Rule:`.

---

## Schichten

| Schicht | Ort | Aufgabe |
|---------|-----|---------|
| Oberfläche | `TableWindow.xaml(.cs)`, `DeckBuilderWindow.xaml(.cs)` | Eingabe, Anzeige, Apply, Fragen |
| Session / Daten | `Services/` | JSON, Decks, Save, Sitzung |
| Druckdaten | `Models/` | Was auf der Karte steht |
| Absicht / Legalität | `GameAction`, `LegalMoves`, `EngineAuthority` | Will, darf, Apply-Eingang |
| Systeme | `Game/*Rules.cs` | Decide ohne WPF |
| Vorlagen | `CardEffectMap`, `EffectRegistry`, `IEffect` | Name oder Typ → Verhalten |
| Board | `Game/Board/` | Ort, Besatzung, Instanz |

```
UI → GameAction → EngineAuthority(GameState) → *Rules / EffectRegistry
LegalMoves.Collect / CollectBoth = gleiche Quelle Hotseat / später Netz / KI
```

---

## Start und Fenster

| Datei | Wofür |
|-------|--------|
| `App.xaml(.cs)` | Start |
| `TableWindow.xaml(.cs)` | Spieltisch — siehe Inventar |
| `TableWindow.DetailGroups.cs` | Detailgruppen (Begleitdatei) |
| `DeckBuilderWindow.xaml(.cs)` | Deckbau |

---

## Models

| Datei | Wofür |
|-------|--------|
| `Models/Card.cs` | Druck + InstanceId, Owner, Controller |
| `Models/Deck.cs`, `DeckEntry.cs` | Deckliste |
| `Models/ExpansionCatalog.cs` | Set-Namen |
| `Models/PersonnelSkillIndex.cs` | Skill-Lookup |

---

## Services

| Datei | Wofür |
|-------|--------|
| `CardDatabase.cs` | Set-JSON laden |
| `DeckService.cs` | Decks |
| `GameSave.cs` | Spielstand |
| `GameSession.cs` | Sitzung, Zug, Ziehen |
| `DebugLog.cs` | Log |

---

## Game — Kern

| Datei | Wofür | Stichworte |
|-------|--------|------------|
| `GameAction.cs` | Absicht aus der UI | play report beam fly attempt battle |
| `GameEvent.cs` | Ergebnis nach Apply | — |
| `GameState.cs` | Snapshot | in-play |
| `LegalMoves.cs` | Legale Aktionen einer Seite | play interrupt response |
| `EngineAuthority.cs` | Validate + Apply-Eingang | play |
| `CardFactory.cs` | Prototyp → Instanz | — |
| `CardLifecycle.cs` | Lebenszyklus | in-play |
| `CardKinds.cs` | Typ-Taxonomie | event interrupt dilemma artifact |
| `CardIcons.cs` | Icon-Token aus JSON | — |
| `IconCatalog.cs` | Icon-Grafiken | — |

---

## Game — Typ-Systeme

| Datei | Wofür | Stichworte |
|-------|--------|------------|
| `PlayRules.cs` | Ins Spiel kommen (6.1.1 / 6.2) | play |
| `InterruptRules.cs` | Interrupts; Ziel für **plays as Interrupt** | interrupt plays-as just response |
| `EventRules.cs` | Events + Persist; **plays as Event** | event plays-as countdown |
| `DilemmaRules.cs` | Begegnung / Outcome | dilemma attempt |
| `DilemmaCureRules.cs` | Cure (7.2.2.3) | dilemma cure present |
| `ArtifactRules.cs` | Erwerb und spätere Nutzung | artifact |
| `MissionRules.cs` | Attempt / Solve (7.2) | attempt solve |
| `SeedRules.cs` | Seed-Phase | seed |
| `DeckPlacementRules.cs` | Typ → Stapel | seed play |

---

## Game — Phrasen und Verben

| Datei | Wofür | Stichworte |
|-------|--------|------------|
| `PlayOnRules.cs` | Plays on / Plays as, Host | plays-on plays-as |
| `TargetQuery.cs` | Eine Zielliste für die UI | plays-on |
| `TargetingRules.cs` | Zielvertrag | plays-on |
| `ReportingRules.cs` | Reporting for Duty (6.3) | report |
| `DownloadRules.cs` | Download (6.5.3–6.5.4) | download |
| `DualAffiliationRules.cs` | Mehrfach-Affiliation (6.3.3) | report |
| `TreatyRules.cs` | Treaty, Mix | treaty |
| `MovementRules.cs` | Staff + RANGE / Fly (7.1.3 / 7.1.5) | staff fly |
| `MovementHazardRules.cs` | Q-Net, Tetryon, Rift, Gaps | fly |
| `DockingRules.cs` | Dock / Undock | dock |
| `RequiredMoveRules.cs` | Pflichtbewegung (7.10) | fly end-of-turn |
| `BattleRules.cs` | Schiff / Personal / Rotation (7.4 / 7.5) | battle damage |
| `TimingRules.cs` | Actions, Responses, Stack | just response stack nullify |
| `TurnExpiry.cs` | Until end of turn | end-of-turn |
| `EndOfTurnEventRules.cs` | Events am Zugende | end-of-turn event |
| `EndOfTurnRestRules.cs` | Übrige EOT/SOT-Tore | end-of-turn |
| `ModifierRules.cs` | Effektive Werte am Ort | present aboard |
| `DetailStatusRules.cs` | Statuszeile | stop disable countdown |

---

## Game — Vorlagen und Altbestand

Existieren. Nicht vermehren. Gleiche Phrase in die Typ- oder Verb-Datei.

| Datei | Wofür |
|-------|--------|
| `CardEffectMap.cs` | Premiere-Name → Vorlagen-Id |
| `EffectRegistry.cs` | Karte → IEffect, ruft Kataloge auf |
| `IEffect.cs` | Vorlagenvertrag |
| `GapsNullifyRules.cs` | Gaps / Nullify-Nebenwirkung |
| `HailRules.cs` | Hail (AU) |
| `IncomingMessageRules.cs` | Incoming Message |
| `InstantEventRules.cs` | Tore für sofortige Events |
| `InterruptShipEffectRules.cs` | Interrupt am Schiff |
| `NamedInterruptRules.cs` | Namens-Routing einzelner Interrupts |
| `WnohgbRules.cs` | Where No One Has Gone Before |

---

## Game/Board

| Datei | Wofür |
|-------|--------|
| `Spaceline.cs` | Reihe der Locations |
| `Location.cs` | Eine Spalte (Mission, Span/Gaps, Time Location) |
| `Occupant.cs` | Schiff oder Facility |
| `Force.cs` | Crew oder Away Team |
| `CardInstance.cs` | Kopie im Spiel + Status |
| `BoardStore.cs` | Board neben der UI |

Gedruckte `Card` = JSON. Instanz = Identität + Status. Beamen wechselt die Force, nicht den Typ. Q-Net zwischen Locations. Gaps = eigene Location.

---

## Schnellsuche

| Mechanik | Zuerst |
|----------|--------|
| Plays as Interrupt / Event | `InterruptRules` / `EventRules` + `PlayOnRules` |
| Plays on Host | `PlayOnRules` + `TargetQuery` |
| Nullify / just / Stack | `TimingRules` |
| Beam / Fly / Staff / Dock | `MovementRules`, `DockingRules`, Apply in TableWindow |
| Dilemma + Cure | `DilemmaRules`, `DilemmaCureRules` |
| Battle | `BattleRules` |
| Unique / in play | `BoardStore`, Play/Report-Gates |
| Detail / Statusfarbe | `DetailStatusRules` + TableWindow |

### StartOfTurnWindow (2026-09-21)
- `TimingRules`: Phrase `plays at / at the start of your turn` → `RequiresStartOfTurnWindow` / `CanPlayStartOfTurnCard`.
- Gates: `LegalMoves` + `EngineAuthority` + `TryAllowHandPlay` **before** BeginPlay/stack/responses.
- First consumer: Full Planet Scan. Not name-if in Apply; Apply is safety net only.
- Distinct from `TurnPhasePoint.StartOfTurn` (delayed/countdown ticks only).

## 2026-09-21 — PersonnelTypePresent
- `Game/MissionRules.PersonnelTypePresent`: shared Class vs Skill Decide (classification word → Class box only; else Class|Skill|Equipment via `EventRules.HasSkill`).
- First consumer: `ArtifactRules.KurlanFullyStaffed` → `BattleRules.KurlanMultiplier` / `ApplyKurlan` (TableWindow RANGE/W/S).
