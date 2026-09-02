# ENGINE.md

Dieses Dokument führt die früheren Einzeldateien **BOARD_MODEL.md** und **ENGINE_FOUNDATION.md** zusammen (Stand 2026-09-02, Klasse C). Inhaltlich unverändert übernommen; nur die Dokumentstruktur ist zusammengeführt.

# Board-Modell

# Board-Modell — Masterplan (neben der laufenden UI)

**Stand:** 2026-09-02  
**Klasse:** C · Grundlage (Ort / Instanz / Gruppe)  
**Nicht:** Big-Bang-Rewrite von `TableWindow.xaml.cs`.  
**Ziel:** Die Karte „lebt“ im Modell. Die UI zeigt InstanceIds. Alte Dictionaries bleiben, bis ein Verb umgezogen ist.

Regelbuch-Lookup bleibt `RULES.md`. Dieser Text ist nur Architektur + Reihenfolge.

---

## Warum

Heute: `Card` + WPF-`Border` + `_stackOnHost` / `_attachedEvents` / `_spacelineOrder`.  
LegalMoves und Apply rekonstruieren Ort und Crew aus der View. Deshalb Relayout-, Unique-, Staffing- und Peek-Bugs in derselben Schleife.

Soll:

```
Spaceline
  └── Location          Mission | Span (Gaps) | Time Location
        ├── Occupant    Ship | Facility
        │     └── Force   Crew (Personnel + Equipment + Tokens)
        └── Force       Away Team am Planeten
Table / Hand / Piles    eigene Zonen, keine Location
```

- Kartentyp = Klasse (`ShipInstance`, `PersonnelInstance`, …), nicht Kartennname.  
- Picard bleibt Personnel. Beamen = Force wechseln, nicht Typ wechseln.  
- Q-Net = Barriere **zwischen** zwei Locations (nicht landbar).  
- Gaps = **eigene Location**, Span 4.

Gedruckte `Card` bleibt JSON-Daten. Instanz wrappt `Card` + `InstanceId` + Status.

---

## Was bleibt

| Behalten | Warum |
|----------|--------|
| `Models/Card.cs` | Druck + InstanceId / Owner / Controller |
| `CardKinds`, `CardFactory`, `CardLifecycle` | Typ-Enum, Erzeugen |
| `GameState` / `BoardPiece` | Snapshot; später aus dem neuen Board füllen |
| `LegalMoves` + `EngineAuthority` + `*Rules` | Verben; nach und nach Board lesen |
| `TargetQuery` + TargetSession | View-Vertrag; Sites kommen später von Locations |
| `TableWindow` Relayout / Snap / Peek | so lange malen, bis Paint-from-Board sitzt |
| `.stsave` Schema 2 | additiv neue Ids, alte Namen weiter auflösbar |

Nicht anfassen in den ersten Schritten: Deck Builder, Borg 7.3, Sites/Tactics voll, Netz, KI, Battle-Zielwahl, Beam-Vollmodus.

---

## Anti-Muster

- Eine Klasse `Picard : Personnel`.  
- Personnel „wird“ Ship / AwayTeam.  
- `TableWindow` als Speicherort löschen, bevor Fly/Beam das Board lesen.  
- Alle Dictionaries in einem PR entfernen.  
- HasSkill wieder tokenweise in die Logdatei.

---

## Dual-Run (so behalten wir den alten Code)

1. Neue Typen unter `Game/Board/` **neben** der UI.  
2. Nach jeder Mutation, die schon existiert (Seed Mission, Report, Beam, Fly, Event attach), **ein** `BoardSync.FromTable(TableWindow)` *oder* besser: die Apply-Stelle schreibt zusätzlich ins Board.  
3. Debug-Logger schreibt `ui:` und `board:` für dieselbe Aktion. Weichen sie ab → Sync-Bug, nicht Relayout-Bug.  
4. Ein Verb umstellen: zuerst **lesen** vom Board (Fly-Ziele = `spaceline.Locations`), UI hängt noch.  
5. Wenn ein Verb 2 Probespiele grün ist: diese UI-Liste nicht mehr als Quelle nutzen.  
6. Erst dann toten Code löschen (`GetDockablesUnderMission` als Wahrheit, nicht als Paint).

Alte Methode umbenennen statt löschen: `GetDockablesUnderMission` → bleibt Paint-Helper, Kommentar `// view only`.

---

## Schritte (ein Chat = ein Schritt, spielbar halten)

### Schritt 0 — File-Logger (klein, sofort nützlich) ✅ 2026-09-02

Datei: `Services/DebugLog.cs` + Haken in `ActionLog`.

- Stufen: `Play`, `Move`, `Beam`, `Target`, `Board`, `Layout`, `Engine`, `Save`.  
- Eine Zeile: `HH:mm:ss.fff T{turn} P{n} [{Stufe}] Text`.  
- Datei: `GamePaths` → `Data/Logs/stccg-yyyyMMdd-HHmmss.txt` (eine Session = eine Datei).  
- Action-List: Filter Debug an/aus (gibt es), plus Button **Log-Datei öffnen** / **Logging an**.  
- Default: File **an** im Dev, nicht jede Skill-Token-Zeile (`CheckTrace.Cmp` bleibt UI-sparsam).  
- Bewegungen: `Beam Picard #12 Facility:Klingon Outpost → Ship:I.K.S. Bortas` mit InstanceId.

Ohne Board-Modell compilierbar.

### Schritt 1 — Typen leer, noch unverdrahtet ✅ 2026-09-02

`Game/Board/`:

- `Spaceline` — geordnete `Location`s, Insert/Remove, Hop-Kosten inkl. Span  
- `Location` — Id, Kind (Mission / Span / TimeLocation), Quadrant, optional Printed `Card`  
- `Occupant` — Ship oder Facility an einer Location  
- `Force` — Liste Personnel + Equipment; `Kind = Crew | AwayTeam`  
- `PersonnelInstance` / `ShipInstance` / `FacilityInstance` / `EventInstance` — wrap `Card`  
- `BoardStore` — eine Spaceline + Table-Events + Hände als Listen von InstanceId  

Noch kein TableWindow-Import außer einem Dev-Button „Dump Board“ in die Logdatei.

### Schritt 2 — Sync aus dem Ist-Zustand ✅ 2026-09-02

Eine Funktion `BoardStore.RebuildFrom(CaptureEngineState + Host-Stapel)`.  
Aufruf nach Seed-Ende, nach Fly, nach Beam, nach Event-Place.  
Logger: Dump Locations + wer wo sitzt.  
Probespiel: Dump muss zur sichtbaren Spaceline passen (Gaps eigene Location, Q-Net Barriere).

### Schritt 3 — Erstes Verb liest das Board: Fly ✅ 2026-09-02

`MovementRules` / `RequiredMoveRules` nehmen `spaceline.Locations` statt `OrderedMissions`-Namensliste.  
UI-Glow bleibt TableWindow, Ziele kommen aus dem Board.  
Erst wenn Fly 2 Spiele hält: nächstes Verb.

### Schritt 4 — Beam + Report schreiben das Board direkt ✅ 2026-09-02

Apply Beam: `force.Remove(picard); destForce.Add(picard)`.  
UI verschiebt Borders anhand InstanceId.  
Unique/Staffing später gegen `Force` + `Controller`, nicht gegen Namens-String auf dem Canvas.

### Schritt 5 — Targeting-Sites aus dem Board ✅ 2026-09-02

`TargetQuery` bekommt Locations / Occupants / Attached Events vom Store.  
TableWindow malt nur noch Halos. Altcode `CollectLegalSnapHosts` löschen, wenn tot.

### Schritt 6 — Aufräumen ✅ 2026-09-02

Tote Helper, doppelte Listen, „mission X coincidence“ für Gaps.  
Checklist-Zellen nur ändern, wenn ein Verb nachweislich vom Board kommt.  
2026-09-02: `GetOrderedMissionCards` / `CountDockablesUnderMission` entfernt. Dockables + IM-Linie + Gap-Paare als View markiert. `CollectLegalSnapHosts` lebt (Kevin). LegalMoves-Fly noch Namensliste.

---

## Dateien (neu vs. alt)

| Neu | Alt bleibt bis Schritt |
|-----|-------------------------|
| `Game/Board/*.cs` | ab Schritt 1 |
| `Services/DebugLog.cs` | Schritt 0 |
| `BoardStore.RebuildFrom` | Schritt 2, ruft TableWindow-Daten |
| Fly liest Store | Schritt 3, `MovementRules` |
| Beam/Report schreiben Store | Schritt 4 |
| `TargetQuery` von Store | Schritt 5 |
| Löschen UI-als-Wahrheit | Schritt 6 |

`TableWindow.xaml.cs` wird in 0–2 nur Logger-Hooks und Dump bekommen, keine Struktur-OP.

---

## Danach

Board 0–6 ist durch. Nächster Block: `artifacts/ENGINE_FOUNDATION.md` (E1 ToGameState → E4 Unique, dann erst A-Karten).

---

## Logger — Kontrakt

```
12:07:04.112 T40 P1 [Target] Kevin Uxbridge snap Bynars Weapon Enhancement #88 host=I.K.S. Bortas #21
12:07:04.118 T40 P1 [Play]  Kevin Uxbridge → nullify #88
12:07:05.001 T40 P1 [Board] Event #88 TABLE→Discard P1
12:07:22.440 T40 P2 [Move]  I.K.S. Bortas #21 Hunt for DNA → Gaps #55 cost=4 left=5
```

Action-List zeigt weiter die kurzen Spielerzeilen. Debug-Zeilen filterbar. Datei enthält beides.

---

## Chat-Start (Copy-Paste)

```
Board-Modell laut artifacts/BOARD_MODEL.md
Nächster Schritt nur: ___ (0 Logger | 1 Typen | 2 Sync | 3 Fly-lesen | 4 Beam-schreiben)
Kein Big-Bang. TableWindow bleibt View.
Premiere. Fix-Protokoll RULES.md unverändert.
Antwort: Schritt, Dateien neu/geändert, was bewusst nicht angefasst.
```

Parked aus dem Probespiel (nicht in Schritt 0–1): USS Galaxy Staffing-False-Deny, Unique-Nebula vs Lore-Instanz — die hängen an Instanz+Controller und fallen in Schritt 4.

# Engine-Foundation

# Engine-Foundation — nach Board 0–6

**Stand:** 2026-09-02  
**Klasse:** C · Grundlage  
**Ziel:** Die Engine steht auf `BoardStore` + `GameState`. `TableWindow` ist View + Input.  
**Nicht:** Big-Bang-Split der 19k-`TableWindow.xaml.cs`. Keine Premiere-Katalogwelle. Keine einzelnen Karten, solange dieser Plan läuft.

Regelbuch-Lookup bleibt `RULES.md`. Karten-A erst nach E4 (Unique-Query) wieder öffnen.

---

## Ist (ehrlich)

```
Klick → TableWindow ändert _stackOnHost / _attachedEvents / Canvas
      → SyncBoardFromTable kopiert ins Board
      → CaptureEngineState liest wieder Borders
      → EngineAuthority / LegalMoves sehen den Snapshot
```

Das Board ist ein **Spiegel**. Fly RANGE, Beam-Force und Gaps-Sites lesen ihn schon. Der Snapshot für die Engine kommt noch aus Pixeln. Deshalb:

- Staffing-Deny direkt nach Beam (Snapshot ohne Crew)
- Unique/Lore über Namens-String
- Stopped / Cloak / RANGE / Dock an `Dictionary<Border, …>`
- LegalMoves-Fly noch `OrderedMissions()`-Namen

Parkplatz (nicht einzeln flicken, dieser Plan schluckt sie):

- USS Galaxy Staffing-False-Deny
- Unique-Nebula vs Lore-Kopie
- IM / Required-Move auf Namensliste

---

## Soll

```
Klick → GameAction → EngineAuthority(GameState)
                   → BoardStore ändert sich
                   → TableWindow malt InstanceId (Halo / Relayout)
```

`GameState` = lesbarer Schnitt der Engine. Quelle schrittweise: Store zuerst, Session-Felder (Zug, Stack, Score) bleiben `GameSession`. UI-only (Zoom, Pan, Overlay) bleibt TableWindow.

---

## Prinzip (unverändert)

- Ein Chat = ein Schritt, Spiel bleibt startbar.
- Dual-Run: `ui:` / `board:` / neu `state:` wo der Snapshot wechselt.
- Logger verstärken, sobald ein Verb umzieht — nicht vorher jedes Skill-Token.
- 3. gleiches Loch → Klasse hoch (A→B→C). Foundation-Arbeit ist immer C, außer ein reiner Halo-Bug (kein A/B/C).
- Checklist-Zelle nur wenn das Verb nachweislich vom Store/Snapshot kommt.
- `CollectLegalSnapHosts` nicht löschen, solange Kevin damit spielt.

---

## Schritte

### E1 — `BoardStore.ToGameState` ✅ 2026-09-02

Boardstücke + Spaceline-Namen + Hände/TABLE-Ids aus dem Store.  
Session-Felder (Turn, Segment, Stack, Score, Treaties, WNOHGB, OncePerGame, EOT-Bag) kommen weiter aus `GameSession` / TableWindow-Listen, die keine Locations sind.

- Neu: `BoardStore.ToBoardPieces()` und/oder `ToGameState(GameStateSeed session)`.
- `CaptureEngineState` **danach** aufrufen: Store-Board bevorzugen, UI-Board nur füllen wo Store leer (Seed-Mitte).
- Log: `state: ships=N crewOn{id}=k` vs Dump. Weichen sie ab → Sync, nicht Relayout.
- Nicht: Relayout, Unique, Statusfelder, LegalMoves umschreiben.

Probe: Beam 3 Leute auf die Nebula, sofort Fly. `state:` muss `crewOn=3` zeigen. Dump und Engine-Staffing gleich.

### E2 — Capture nur noch Session + Fallback ✓ 2026-09-02

`CaptureEngineState` schrumpft: keine 80-Zeilen-Schleife über `_borderOwner` für Schiffe, wenn der Store Occupants hat.

- HostName / Staffed / Aboard / RangeLeft aus Occupant + Session-Dicts (RangeLeft darf in E2 noch UI-Dict sein).
- Fallback-Pfad mit `state-fallback:` loggen, damit wir sehen wann die UI noch einspringt.
- Nicht: Dicts löschen.

Probe: gleiches Beam→Fly. Log ohne `state-fallback:` für das Schiff.


### E2b — Store-Staffing Treaty / Rogue Borg (offen)

Nach E2: `BoardStore.ToBoardPieces` / store-first Capture nutzt `IsShipStaffed(printed, aboard)` **ohne** Treaties und ohne Rogue-Borg-Pfad. UI-Fallback hatte die volleren Args. Premiere-Normalcrew ok; Overlay kann Treaty/Rogue nicht nachziehen, solange Occupant im Store ist.

- Nicht E3 vorziehen.
- Schließen wenn Probespiel Treaty-Mix oder Rogue-Borg-Staffing falsch denied/allowed.

### E3 — Status an der Instanz / am Occupant

Felder (minimal):

| Feld | Heute | Ziel |
|------|--------|------|
| Stopped | `_stoppedBorders` | `CardInstance` oder Id-Set am Store |
| Cloaked | `_cloakedShips` | `ShipInstance.Cloaked` |
| RangeLeft | `_shipRangeLeft` | `ShipInstance.RangeLeft` |
| DockedAt | `_dockedAt` | `ShipInstance.DockedAtId` |
| Hull | `_hullDamagePercent` | `ShipInstance.HullPercent` |

Ein Chat = **eine** Spalte (zuerst RangeLeft + Stopped — die Fly/Staffing brauchen).  
UI-Dict bleibt Spiegel bis 2 Spiele grün.

Logger: `[Move] range #275 left=8 source=instance`.

### E4 — In-Play / Unique / Persona

`PlayRules` + Report fragen `BoardStore.InPlay(player, name|persona)`, nicht Canvas-Namen.

- Owner vs Controller getrennt (Lore).
- Universal / Enigma / unique wie `PlayRules.GetUniqueness` schon kann.
- Dann erst das geparkte Nebula/Lore-Loch.

Logger: `[Play] unique deny Nebula #283 have=#240 controller=2`.

### E5 — LegalMoves-Fly liest Locations

`state.OrderedMissions()` nur noch Fallback. Collect nutzt dieselbe Location-Liste wie `MovementRules.CanMoveShip(Location[])`.

### E6 — IM / Required-Move auf Locations

`MissionsOnSameSpaceline` bleibt Paint. Hop-Kosten wie Fly.

---

## Danach wieder Karten (A)

Erst wenn E1+E2 grün (E3/E4 empfohlen): Premiere-Karten wieder als A in `*Rules`.  
Apply-Haken in TableWindow nur wenn kein Template existiert. Kein neues Verb „weil die Karte es schön hätte“.

---

## Fehler während der Umbauten

| Symptom | Klasse | Wohin |
|---------|--------|--------|
| Halo / Snap / Text / Zoom | — (UI) | `TableWindow` |
| Engine sieht andere Crew als Dump | C | E1/E2, Sync, nicht die Karte |
| RANGE 20 auf Nachbar | C | Locations vs Namensliste (E5, schon Fly-Apply) |
| Zweite Unique-Kopie | C | E4 |
| Falscher Drucktext einer Karte | A | `*Rules` / Map |
| Gleicher Satz auf 3 Karten | B | Parser / Template |
| Relayout verschiebt, Dump stimmt | UI | Paint-Helper, Board nicht anfassen |

Jeder Chat: Ist/Soll → Verb → Lookup Checklist/Glossary → Klasse → kleinste Datei → CHANGELOG + ggf. Checklist.

---

## Logger in dieser Phase

Schon da: `[Board] ui:/board:`, `[Move] fly-eval hops`, `[Target] board-gaps=`.

Dazu, sobald E1 lebt:

```
[Engine] state: ships=2 nebula#275 aboard=3 staffed=1 host=Hunt#28
[Engine] capture: source=store|fallback
```

`CheckTrace.Cmp` bleibt aus der Datei.

---

## Todos (lebendig)

- [x] E1 ToGameState + Capture bevorzugt Store
- [x] E2 Capture ohne Border-Schleife für Occupants (2026-09-02)
- [ ] E3 RangeLeft + Stopped auf Instanz
- [ ] E3b Cloak / Dock / Hull (nach E3)
- [ ] E4 InPlay-Query, dann Nebula/Lore
- [ ] E5 LegalMoves-Fly
- [ ] E6 IM auf Locations
- [ ] Kevin-Snap von Store (nur wenn Probespiel blockiert)
- [ ] Attached Events als Store-Liste (nicht E1)

Nicht auf diese Liste: Netz, KI, Borg 7.3, Sites/Tactics voll, Paint-from-Board, TableWindow-Split, Disabled/Stasis-Vollsystem.

---

## Dateien je Schritt

| Schritt | Neu / geändert | Nicht anfassen |
|---------|----------------|----------------|
| E1 | `Game/Board/BoardStore.cs`, `Game/GameState.cs`, `CaptureEngineState` (Aufruf) | Relayout, *Rules-Kataloge |
| E2 | `TableWindow.CaptureEngineState` kürzen | Snap, Kevin |
| E3 | `Game/Board/CardInstance.cs` / Occupant, ein UI-Dict Spiegel | Unique |
| E4 | `PlayRules.cs`, `BoardStore`, Report-Aufruf | Galaxy-Staffing einzeln |
| E5 | `LegalMoves.cs` | RequiredMove |
| E6 | `RequiredMoveRules` + ein TableWindow-Hop | Battle-Ziele |

---

## Chat-Start (Copy-Paste)

```
Projekt: Star Trek CCG 1E private C# WPF App (nicht-kommerziell)
GitHub: https://github.com/gokeltanes-del/STCCG1E
Tech: C# / .NET 8 / WPF / VS2022
Docs: artifacts/PROJECT.md · artifacts/ENGINE_FOUNDATION.md · artifacts/BOARD_MODEL.md
      artifacts/RULES.md · artifacts/RULES_CHECKLIST.md · artifacts/CHANGELOG.md
Regelbuch: artifacts/rules/Compendium_Rulebook.pdf (2.7.4)
  Lookup: Checklist-§ → Glossary → Temporary Rulings → App A Errata → App B

Scope: Premiere zuerst. Foundation-Block E1–E6 vor einzelnen Karten.
Kein Fundamentalsystem auf Vorrat. Keine Geschwister-Kapitel.

Architektur: TableWindow = View/Input. Wahrheit = BoardStore + GameSession.
  Dual-Run neben UI-Dicts. Ein Schritt = ein Chat.
Plan: artifacts/ENGINE_FOUNDATION.md
  E1 ToGameState | E2 Capture-Fallback | E3 Status an Instanz |
  E4 Unique-Query | E5 LegalMoves-Fly | E6 IM-Locations

Fix-Protokoll RULES.md unverändert (A Karte / B Phrase / C Grundlage).
3. gleiches Loch hebt die Klasse. Logger nur für das aktuelle Verb verstärken.

Antwort immer: Schritt, Klasse, Dateien neu/geändert, was bewusst nicht angefasst.

Nächster Schritt nur: E3 (Status an Instanz — RangeLeft + Stopped)
Kein Big-Bang. Premiere.
```
