# TABLEWINDOW_INVENTORY — Ist der Tischdatei

**Stand:** 2026-09-28 (`8083785`)  
**Dateien:** `TableWindow.xaml`, `TableWindow.xaml.cs` (~33 440 Zeilen, ~974 Methoden), `TableWindow.DetailGroups.cs`

Keine Slice-Geschichte. Systeme außerhalb: `ENGINE.md`. Ablauf: `IMPLEMENT.md`.

Verdrahtung = Tisch ruft Decide in `Game/*Rules` und wendet an.  
Nicht verdrahtet = Wirkung steckt noch als Zweig in dieser Datei.

---

## Was hier bleiben soll (View)

Klick, Drop, Snap, Halo, Zoom, Relayout, Overlay, AskPlayer, Reveal, Schadensanzeige, Zerstören sichtbar, Fly-in, History-Strip, `CaptureGameSave` / `ApplyGameSave`, Netz-Broadcast.

`LegalMoves` / `EngineAuthority` sagen, ob die Aktion geht.

Neue Wirkung nicht als `if (name == …)` hier anlegen. Zuerst `ENGINE.md`.

---

## Einstieg nach Geste

| Geste | Typischer Einstieg |
|-------|-------------------|
| Karte aus der Hand spielen | `BeginPlayCardStack` → Host `EngineAuthority` / `TryApplyNetPlayCard` |
| Interrupt / Response | `TryPlayAsResponse`, `CollectAllLegalResponses` |
| Seed | `TryApplyNetSeedCard`, `SeedMissionFromNetNote` |
| Fly Schiff | `TryApplyNetFly` + `MovementRules` / `MovementHazardRules` |
| Beam | `BeginBeamMode` |
| Battle | `BeginShipBattleStack` / `BeginPersonnelBattleStack` |
| Q-Net / Gaps legen | `PlaceSpanOnSpaceline` + `PaintSpans` |
| Frage an einen Spieler | `AskChoiceForPlayer` (Netz: `NetChoiceDto`) |
| Zug / Phase | `GameSession.AdvanceSegment` / `EndTurn` + Broadcast |
| Dual-EXE Board | `CaptureGameSave` → `BroadcastMaskedStateToGuest` → Guest `ApplyGameSave` |

---

## Spaceline (Netz-kritisch)

`_spacelineOrder` = Missionen, Time Locations **und** Span-Spalten (Q-Net/Gaps) in der Folge Mission | Span | Mission.  
Endpunkte einer Span sind nur Mission/Time/Pod (`GetSpacelineColumns`).

| Methode | Tut |
|---------|-----|
| `InsertSpanColumn` | Span vor die rechte Mission setzen |
| `EnsureSpanColumnsInOrder` | zwei Gaps = zwei Spalten |
| `PlaceSpanOnSpaceline` | anlegen + Insert + Relayout |
| `PaintSpans` | Y / Face / Opacity |
| `ApplyNetSpanFromSnapshot` | Guest aus `ColumnInstanceIds` + `Spans` |
| `RelayoutMissionsOnSpaceline` | Spalten inkl. Span-Slots |
| `DumpSpacelineTruth` | Debug `STCCG_DUMP_SPACELINE=1` |

---

## Rules-Aufrufe (Richtung)

Decide in Rules, Apply am Tisch. Häufig angesprochen:

`EventRules`, `TimingRules`, `InterruptRules`, `DilemmaRules`, `ModifierRules`, `BoardStore`, `BattleRules`, `ArtifactRules`, `PlayOnRules`, `TargetQuery`, `ReportingRules`, `MissionRules`, `DownloadRules`, `MovementRules`, `SeedRules`, `EndOfTurnEventRules`, `EndOfTurnRestRules`, `TreatyRules`, `DilemmaCureRules`, `LegalMoves`, `EngineAuthority`.

Altbestand am Tisch noch verdrahtet: `NamedInterruptRules`, `WnohgbRules`, `IncomingMessageRules`, `GapsNullifyRules`, `HailRules`, `InstantEventRules`.

---

## Detail

`TableWindow.DetailGroups.cs` formatiert Gruppen. Regelwahrheit bleibt in `*Rules` / `DetailStatusRules`.
