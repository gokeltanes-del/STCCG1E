# CODE_PLACEMENT.md — where new rules / cards / mechanics go

**Stand:** 2026-09-05  
**Owner:** Data (Klasse C) + Captain (process)  
**Warum:** Nach TableWindow-Extract (Slices 1–8) sollen neue Karten, Kartentypen und Regelupdates **nicht** wieder in `TableWindow.xaml.cs` landen.

## Prinzip (immer)

1. **Decide in `Game/*Rules`** — pure C#, kein WPF: Deny-Strings, Pläne, Counts, Timing-Ticks, Adjacent-Indices.
2. **Apply/UI in `TableWindow`** — AskPlayer, Reveal, Drag/Snap, Damage, Destroy, Relayout, `SyncBoardFromTable`.
3. **Board truth** — Ort/Crew/Status wo möglich über `BoardStore` / Instances lesen; UI-Dictionaries nur Mirror/Paint.
4. **Ein Schritt = ein Commit-Thema** — Klasse A (Karte) / B (Phrase) / C (Grundlage) laut `RULES.md`.
5. **Kein big-bang** — keine TableWindow-Split-PR; keine Premiere-Wellen ohne Captain Go.
6. **Lookup-Reihenfolge (Spock):** Checklist → Glossary → Temp Rulings → App A Errata → App B.

## Schicht-Karte (Mechanik → Code)

| Mechanik / Verb | Decide / Rules | Apply / View | Board |
|-----------------|----------------|--------------|-------|
| Seed / Mission layout | `SeedRules`, `DeckPlacementRules` | TableWindow seed UI | Spaceline Locations |
| Report / download | `ReportingRules`, `DownloadRules` | TableWindow report/download | Occupant / Force |
| Play Event | `EventRules` (+ specialized: Instant, EOT, GapsNullify) | Attach, TABLE, TurnExpiry | Persist attach |
| Play Interrupt | `InterruptRules`, `InterruptShipEffectRules`, `NamedInterruptRules`, `IncomingMessageRules`, `WnohgbRules` | Drop/host, picker, relocate | Locations / Range |
| Play Dilemma | `DilemmaRules` | Reveal, kill, attach | Mission seed stack |
| Artifact | `ArtifactRules` | Acquire UI | — |
| Fly / RANGE / Span | `MovementRules`, `MovementHazardRules`, `LegalMoves` | Fly highlight, path | Locations, RangeLeft |
| Dock / Undock | `DockingRules` | Dock UI | ShipInstance.DockedAt |
| Cloak / Decloak | (gates in ship-effect / TW ToggleCloak) | Cloak visual + lock | ShipInstance.Cloaked |
| Required move / IM | `RequiredMoveRules`, `IncomingMessageRules` | Process moves | Locations |
| Battle ship/personnel | `BattleRules` | Begin/resolve UI | Stopped / Hull |
| Treaty / staffing | `TreatyRules`, Modifier/staff helpers | — | Occupants |
| Unique / Persona | Engine unique (Owner) | Play deny UI | In-play scan |
| Timing / responses | `TimingRules`, stack | Response UI | — |
| Turn expiry / EOT | `TurnExpiry`, `EndOfTurnEventRules` | Process* EOT | — |
| Nullify (Kevin etc.) | `TimingRules` + card-specific (`GapsNullifyRules`) | `NullifyEventInPlay` | Relayout / relocate |
| Targets / sites | `TargetQuery`, `TargetingRules`, `PlayOnRules` | Halo/snap | Prefer Locations |

## Neue Karte (Premiere / später Set)

1. Spock: Kartentext + Glossary/Checklist Ist/Soll (Klasse A).
2. Data: Decide-Gates in die **passende** `*Rules`-Datei (oder kleine neue `FooRules.cs` wenn Cluster ≥3 Karten / eigener Verb-Block).
3. `InterruptRules.Is*` / `EventRules.Is*` Name-Helper statt String-Equals in TW.
4. TableWindow nur verdrahten: Host-Match, Apply-Aufruf, Side-Effects.
5. Seven: Checklist/FEATURES + **Glossary-Sonderfälle** markieren (Code ja/nein).
6. Pepsch smoke → Push; CHANGELOG eine Zeile.

## Neuer Kartentyp / neues Verb

1. Captain Go + Spock Scope.
2. Prefer extend existing Rules; new file only if clean seam (wie `GapsNullifyRules`, `EndOfTurnEventRules`).
3. Board: neuer Status auf Instance, nicht neues UI-Dictionary als Wahrheit.
4. ENGINE.md kurz erweitern wenn Foundation betroffen.

## Regelupdate (Glossary / Errata)

1. Spock Ist/Soll + Quellen.
2. Data ändert **Rules zuerst**, dann TW-Wire; kein Soft-Fix nur in UI.
3. Checklist/Glossary-Coverage bei Seven mitführen.

## Wohin NICHT

- Keine neuen Effekt-Verzweigungen als 200-Zeilen-Block nur in `TableWindow`.
- Kein Paint/Snap/DeckBuilder/Save als „Rules“.
- Keine parked-Bug-Fixes stillschweigend in Extract-Commits mischen (Captain Priorität).

## Extract-Rest (nach Welle 1)

Noch OPEN laut Inventar: Battle-Decide verdünnen; EOT-Rest (Rogue Borg, Borg Ship, Repairs, Dilemma-EOT); Persist-Zweige schrittweise. Dann Premiere A nur mit Captain Go.

## Spawn / Clone

Jeder neue Chat: `artifacts/HANDOFF.md` → dieses File (`CODE_PLACEMENT.md`) wenn Code-Ort unklar → Specialty-Docs.
