# TABLEWINDOW_INVENTORY — Ist der Tischdatei

**Owner:** Data  
**Stand:** 2026-09-21  
**Datei:** `StarTrekCCG/TableWindow.xaml.cs` (plus `TableWindow.xaml`, `TableWindow.DetailGroups.cs`)

Nur der **aktuelle** Stand: was die Datei tut und welche `*Rules` sie aufruft.  
Keine Slice-Geschichte, kein DONE, keine Ticketliste. Offenes Ziehen aus dieser Datei: `EXTRACT_REST.md`.  
Systeme im Rest der Engine: `ENGINE.md`. Ablauf: `IMPLEMENT.md`.

Verdrahtung = TableWindow ruft Decide in `Game/*Rules` auf und wendet das Ergebnis an.  
„Nicht verdrahtet“ = Entscheidung oder Wirkung steckt noch als Zweig in der Tischdatei.

---

## Dateien

| Datei | Rolle |
|-------|--------|
| `TableWindow.xaml` | Layout des Tisches |
| `TableWindow.xaml.cs` | Eingabe, Modi, Apply, Paint, Relayout, viele Kartenzweige |
| `TableWindow.DetailGroups.cs` | Detailgruppen; nicht die Regelwahrheit |

Ungefähre Größe der `.cs`: sehr groß (viele hundert private Methoden). Neue Wirkung gehört nicht als weiterer Namenszweig hierher.

---

## Was hier bleiben soll (View)

Klick, Drop, Snap, Halo, Zoom, Relayout, Overlay, AskPlayer, Reveal, Schadensanzeige, Zerstören sichtbar machen, `SyncBoardFromTable` / Paint.  
`LegalMoves` / `EngineAuthority` sagen, ob die Aktion geht. TableWindow führt sie am Bildschirm aus.

---

## Verdrahtung zu Rules (Ist)

Zählung = Vorkommen des Typnamens in `TableWindow.xaml.cs`. Hoch heißt: die Datei spricht oft mit diesem System, nicht dass Decide vollständig draußen ist.

| System | Aufrufe (ca.) | Rolle am Tisch | Verdrahtung |
|--------|---------------|----------------|-------------|
| `EventRules` | 266 | Event spielen, Persist, Instant | teilweise — Decide oft in Rules, Persist-Arten und Apply noch am Tisch |
| `TimingRules` | 231 | Stack, just, Response, Nullify-Fenster | teilweise |
| `InterruptRules` | 172 | Interrupt aus der Hand, auch plays-as | teilweise |
| `DilemmaRules` | 129 | Encounter, Outcome, Attach | teilweise — große Katalogteile in Rules, Apply/Reveal am Tisch |
| `ModifierRules` | 70 | Effektive Skills/Attribute in Detail und Attempt | teilweise |
| `BoardStore` | 67 | Board neben UI-Listen | teilweise — Dual-Stand, nicht jede Aktion liest nur Board |
| `BattleRules` | 55 | Schiff / Personal beginnen und auflösen | teilweise — Decide in Rules, Ablauf-UI am Tisch |
| `ArtifactRules` | 49 | Erwerb und Nutzung | teilweise |
| `PlayOnRules` | 46 | Plays on / Plays as, Host | teilweise (F0–F3 im Bau, typunabhängig) |
| `TargetQuery` | 40 | Halo / Picker / Snap-Ziele | teilweise |
| `ReportingRules` | 38 | Report | teilweise |
| `MissionRules` | 26 | Attempt / Solve | teilweise |
| `DownloadRules` | 25 | Download | teilweise |
| `DetailStatusRules` | 25 | Statuszeile Buff/Debuff/Timer | verdrahtet für Decide, Paint am Tisch |
| `DualAffiliationRules` | 23 | Mix / Dual | teilweise |
| `MovementRules` | 20 | Fly / Staff / RANGE | teilweise — Pfad und Highlight am Tisch |
| `SeedRules` | 16 | Seed | teilweise |
| `EndOfTurnEventRules` | 15 | EOT angehängte Events | teilweise |
| `EndOfTurnRestRules` | 15 | EOT-Rest-Tore | teilweise |
| `TreatyRules` | 9 | Treaty / occupy | teilweise |
| `DilemmaCureRules` | 9 | Cure | teilweise |
| `TurnExpiry` | 8 | Until end of turn | teilweise |
| `LegalMoves` | 6 | Aktionsliste | verdrahtet als Quelle, Sammlung noch nicht alles abdeckend |
| `EngineAuthority` | 6 | Validate/Apply-Eingang | verdrahtet, nicht jede Geste geht darüber |
| `PlayRules` | 6 | Entering play / free play | teilweise |
| `InterruptShipEffectRules` | 6 | Interrupt am Schiff | teilweise |
| `NamedInterruptRules` | 6 | Namens-Routing | teilweise — Altbestand |
| `WnohgbRules` | 5 | Where No One Has Gone Before | kartenbenannte Datei |
| `IncomingMessageRules` | 5 | Incoming Message | kartenbenannte Datei |
| `RequiredMoveRules` | 4 | Pflichtflug | teilweise |
| `MovementHazardRules` | 4 | Gaps / Q-Net / Rift / Tetryon beim Flug | teilweise |
| `TargetingRules` | 3 | Zielvertrag | teilweise |
| `GapsNullifyRules` | 3 | Gaps-Nullify | kartenbenannte Datei |
| `EffectRegistry` | 2 | Vorlagen | dünn am Tisch |
| `HailRules` | 2 | Hail | kartenbenannte Datei |
| `DockingRules` | 2 | Dock | dünn — viel Layout noch am Tisch |
| `InstantEventRules` | 1 | Instant-Event-Tore | dünn |

---

## Noch in der Tischdatei verzweigt (Persist-Arten)

Diese Namen hängen als Persist-Kind oder Apply-Zweig noch an TableWindow, auch wenn einzelne Tore schon in `EventRules` / EOT-Rules liegen:

AntiTime, Baryon, CaptainsLog, Distortion, Espionage, Gaps, Goddess, IncomingMessage, IntruderField, Ionization, Kidnappers, Klim, LoreReturns, LowerDecks, NeuralServo, ParticleScatter, PatternEnhancers, PlasmaFire, QNet, RaiseStakes, RedAlert, Rift, Spacedock, StaticWarp, Supernova, Table, Tetryon, Thermal, Traveler, WarpCore, YellowAlert.

Das ist Ist, keine Abarbeitungsliste. Zum Abbau: `EXTRACT_REST.md`.

---

## Typische Einstiege in der Datei

| Thema | Typische Methoden / Orte | Rules |
|-------|--------------------------|--------|
| Interrupt aus der Hand | `TryPlayInterruptFromHand` und Verwandte | `InterruptRules`, `TimingRules` |
| Event | `ApplyInstantEvent` und Event-Apply | `EventRules`, `InstantEventRules` |
| Dilemma | `ApplyDilemmaResult`, Encounter | `DilemmaRules`, `DilemmaCureRules` |
| Artifact | Acquire-Apply | `ArtifactRules` |
| Fly / Beam | `BeginFlyHighlight`, `BeginBeamMode` | `MovementRules`; Legalität Authority |
| Bewegungshindernis | nach dem Flug | `MovementHazardRules`, `EventRules` |
| Battle | Begin/Resolve-UI | `BattleRules` |
| Zugende | Process-EOT | `EndOfTurnEventRules`, `EndOfTurnRestRules`, `TurnExpiry` |
| Plays on / Host | Drop / Place / Parse | `PlayOnRules`, `TargetQuery` |

Namensvergleiche (`NameIs` / `IsGaps` / `IsQNet`) sollen in den `*Rules.Is*`-Helfern liegen; Reste in der Tischdatei sind Ist.

---

## Bewusst nicht Rules

Paint, Snap, Halo, Zoom, DeckBuilder, Save-Dialog, Hotseat-Chrome. Bleiben View.
