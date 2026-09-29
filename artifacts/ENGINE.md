# ENGINE — Ist-Landkarte des Codes

**Stand:** 2026-09-28 (`8083785`)  
**Pfad:** `StarTrekCCG/` (Live Josef, Kopie `artifacts/StarTrekCCG/`)

Suchindex zuerst, dann Dateien. Offenes steht in `HANDOFF.md`, `FEATURES.md`, `CARD_TRACKER.md`.  
Neuer Code: `IMPLEMENT.md`. Tisch-Apply: `TABLEWINDOW_INVENTORY.md`.

---

## Request-Pfad (immer so lesen)

```
Geste (Drop/Klick/Taste)
  → GameAction (Kind + Card + Target, keine Pixel)
    → LegalMoves.Collect / CollectBoth     darf die Seite das?
    → EngineAuthority.Evaluate             Validate + Apply-Eingang
      → Game/*Rules Decide                 ohne WPF
      → EffectRegistry / CardEffectMap     Name → Vorlage
    → TableWindow Apply                    Paint, AskPlayer, Schaden, Relayout
    → CaptureGameSave
      → Host: BroadcastMaskedStateToGuest (NetStateMask)
      → Guest: Action-only, ApplyGameSave / ApplyNet*
```

Hotseat und Netz nutzen dieselbe Engine. Gast entscheidet nicht.

---

## Schnellindex — wo suche ich?

Zuerst diese Tabelle, dann `Rule:` / `Glossary:` / `Verb:` im Code.

| Thema / Phrase | Zuerst | Dann |
|---|---|---|
| Darf ich das jetzt? | `LegalMoves.Collect` | `EngineAuthority.Evaluate` |
| Typ der Karte | `CardKinds.Of` | `UsesNormalCardPlay`, `IsAnytimeType` |
| Ins Spiel kommen | `PlayRules.CanEnterPlay` | Unique/`PersonaKey` |
| Interrupt / plays as Interrupt | `InterruptRules` | `TimingRules.IsInterrupt`, `BeginPlayCardStack` |
| Event / plays as Event | `EventRules.ResolvePlay` | Persist in TableWindow + `BoardStore` |
| Plays on / Host / Zielhalo | `PlayOnRules.ResolvePlayOn` | `TargetQuery`, `TargetingRules.UsesBoardSnap` |
| Report for Duty / Mix | `ReportingRules` | `DualAffiliationRules`, `TreatyRules` |
| Download / Tent / Gift Box | `DownloadRules` | TableWindow `TryDownloadFromTent` |
| Seed / under Mission | `SeedRules`, `DeckPlacementRules` | `TryApplyNetSeedCard`, `RelayoutSeedUnderMissions` |
| Mission attempt / solve | `MissionRules` | TableWindow Attempt-UI |
| Dilemma encounter / wall | `DilemmaRules` (~250k, Katalog) | `DilemmaCureRules`, `WrapDilemma` |
| Artifact acquire / later use | `ArtifactRules` | Thought Maker plays-as, TTP, Stone |
| Fly / RANGE / Staff | `MovementRules`, `ShipRules` | `TryApplyNetFly` |
| Q-Net / Gaps / Tetryon / Rift | `MovementHazardRules` | `PlaceSpanOnSpaceline`, `PaintSpans`, `ApplyNetSpanFromSnapshot` |
| Spaceline-Lage / Time Location | `SpacelineLocationRules` | `Game/Board/Spaceline.cs` |
| Dock | `DockingRules` | `RelayoutAllDockables`, `PinDockablesToSpacelineByColumn` |
| Beam | TableWindow `BeginBeamMode` | `IsBeamableFromHost`, `TryApplyNetBeam` |
| Battle Schiff / Personal | `BattleRules` | `BeginShipBattleStack`, `TryApplyNetShipBattle` |
| Borg Ship Dilemma | `BorgShipRules` | `TryDestroyBorgShipInBattle` |
| just / Response / Stack / Nullify | `TimingRules` | `CollectAllLegalResponses`, `AskChoiceForPlayer` |
| Start of turn window | `TimingRules.RequiresStartOfTurnWindow` | LegalMoves + EngineAuthority vor BeginPlay |
| EOT Events | `EndOfTurnEventRules` | PlasmaFire, WCB, SWB, TAK, Anti-Time |
| EOT Rest | `EndOfTurnRestRules` | Repair, Rogue, Junior, Countdown |
| Until end of turn | `TurnExpiry` | TableWindow tick |
| Skills / Attribute am Ort | `ModifierRules` | Detail + Attempt |
| Statuszeile Buff/Stop/CD | `DetailStatusRules` | `TableWindow.DetailGroups.cs` |
| Name → Vorlage | `CardEffectMap.TemplateIdFor` | `EffectRegistry` |
| Unique / in play | `BoardStore.FindConflictingUnique` | `PlayRules.GetUniqueness` |
| Treaty occupy | `TreatyRules.CanOccupyHost` | `CardsCompatibleUnderTreaties` |
| Netz Host Apply | `TryApplyNetPlayCard` u. a. | `NetPlaySession.BroadcastStateAsync` |
| Netz Choice | `AskChoiceForPlayer` | `NetChoiceDto`, `OnNetChoiceRequestReceived` |
| Play-Fly-in Face | `ShowPlayFlyIn`, `NetPlayRevealDto` | `FindLiveCardWithArt` |
| Save / Dual-EXE Board | `CaptureGameSave` / `ApplyGameSave` | `GameSave.cs`, Spans-Snapshot |
| Lobby | `NetworkLobbyWindow` | `NetLobbyDto`, `TryStartNetSessionFromLobby` |
| Deckbau | `DeckBuilderWindow` | `DeckService`, `CardDatabase` |

Kein Treffer: `ENGINE.md` Wortliste unten, dann Dateiname `*Rules`, dann TableWindow-Methode mit gleichem Verb.

---

## Schichten

| Schicht | Ort | Aufgabe |
|---------|-----|---------|
| Oberfläche | `TableWindow*`, `DeckBuilderWindow*`, `NetworkLobbyWindow*` | Geste, Paint, Ask, Lobby |
| Netz | `Network/` | TCP/JSON, Host-Gast, Choice, Reveal, Mask |
| Session | `Services/` | JSON, Decks, Save, Zug |
| Druck | `Models/` | Was auf der Karte steht |
| Absicht | `GameAction`, `LegalMoves`, `EngineAuthority` | Will, darf, Eingang |
| Decide | `Game/*Rules.cs` | ohne WPF |
| Vorlagen | `CardEffectMap`, `EffectRegistry`, `IEffect` | Name → Verhalten |
| Board | `Game/Board/` | Ort, Crew, Instanz, Attach |

---

## Fenster und Start

| Datei | Wofür | Größe ca. |
|-------|--------|-----------|
| `App.xaml(.cs)` | WPF-Start, leer | klein |
| `TableWindow.xaml(.cs)` | Tisch, ~974 Methoden, ~33k Zeilen | Apply-Ort |
| `TableWindow.DetailGroups.cs` | Detailgruppen-Text | nicht Regelwahrheit |
| `DeckBuilderWindow.*` | Deckbau | — |
| `NetworkLobbyWindow.*` | Host / Join / Localhost / Skip-Seed | — |

---

## Models / Data / Services

| Datei | Wofür |
|-------|--------|
| `Models/Card.cs` | Druck + `InstanceId`, Owner, Controller |
| `Models/Deck.cs`, `DeckEntry.cs` | Deckliste |
| `Models/ExpansionCatalog.cs` | Set-Namen |
| `Models/PersonnelSkillIndex.cs` | Skill-Tags aus Text |
| `Data/GamePaths.cs` | Lackey-/Kartenpfade |
| `Data/CardBack.cs` | Kartenrücken |
| `CardDatabase.cs` | Set-JSON `LoadAll` / `FindByName` |
| `DeckService.cs` | Deck speichern/laden |
| `GameSave.cs` | Snapshot inkl. Spaceline/Spans |
| `GameSession.cs` | `GameMode`, `MatchPhase`, `TurnSegment`; Draw/Play/Victory |
| `DebugLog.cs` | Kanäle Play/Move/Beam/Board/Engine/Seed/Dual |

`GameActionKind`: Pass, EndPhase, EndTurn, PlayCard, Respond, ChooseTarget, Beam, Fly, AttemptMission, EncounterDilemma, InitiateShipBattle, ActivateInPlay, Draw, Download, FlipHiddenAgenda, PlayTactic, BuildSite, SeedCard.

---

## Network

Host = P1 + Engine. Gast sendet `NetActionDto`, rendert maskierten State.

| Datei | API |
|-------|-----|
| `NetMessage` | `Serialize` / `Deserialize` / `Create` |
| `NetServer` / `NetClient` | TCP |
| `NetPlaySession` | `CreateHost`/`CreateGuest`, `BroadcastStateAsync`, `SendActionAsync`, Choice, PlayReveal |
| `NetActionDto` | Guest-Aktion |
| `NetStateMask.MaskForViewer` | Fog; `AlienProbeInPlay` |
| `NetChoiceDto` | Phase-4 Dialog |
| `NetPlayRevealDto` | Fly-in Face + TargetInstanceId |
| `NetLobbyDto` | Ready / StartGame / skipSeed |

TableWindow-Eingang Netz: `TryApplyNetAuthorizedAction`, `TryApplyNetPlayCard`, `TryApplyNetSeedCard`, `TryApplyNetFly`, `TryApplyNetBeam`, `TryApplyNetShipBattle`, `TryApplyNetRespond`, `ApplyNetSpanFromSnapshot`, `BroadcastMaskedStateToGuest`, `AskChoiceForPlayer`.

---

## Game — Kern

| Datei | Wofür | Einstieg |
|-------|--------|----------|
| `LegalMoves.cs` | Liste legaler Aktionen | `Collect` |
| `EngineAuthority.cs` | Validate + Apply-Tor | `Evaluate`, `WrapDilemma` |
| `GameState.cs` | Snapshot für Decide | — |
| `GameEvent.cs` | Ergebnis nach Apply | — |
| `CardFactory.cs` | Prototyp → Instanz | `Instantiate` |
| `CardLifecycle.cs` | in-play Leben | — |
| `CardKinds.cs` | Typ-Taxonomie inkl. Q-*, Site, Tactic | `Of` |
| `CardIcons.cs` | Icon-Token | AU, SD, Hidden Agenda, Holo, IPG, ETA |
| `IconCatalog.cs` | Icon-Grafiken | — |

---

## Game — Typ-Systeme

| Datei | Wofür | Einstieg |
|-------|--------|----------|
| `PlayRules.cs` | 6.1.1 / 6.2 Enter Play | `CanEnterPlay`, `PersonaKey` |
| `InterruptRules.cs` | Interrupts + plays-as Interrupt | `IsInterrupt`, Namens-`Is*` |
| `EventRules.cs` | Events + Persist + plays-as Event | `ResolvePlay`, viele `Is*` |
| `DilemmaRules.cs` | Encounter-Katalog | `Decide*` / `Verify*` pro Karte |
| `DilemmaCureRules.cs` | 7.2.2.3 Cure | `CanCure` |
| `ArtifactRules.cs` | Acquire + spätere Nutzung | `ResolveAcquire`, Thought Maker, TTP |
| `MissionRules.cs` | Attempt / Solve | `PersonnelTypePresent` |
| `SeedRules.cs` | Seed-Phase | — |
| `DeckPlacementRules.cs` | Typ → Seed/Play-Stapel | — |
| `ShipRules.cs` | Schiff allgemein | — |
| `BorgShipRules.cs` | Borg-Ship-Dilemma | `DecideMove`, `IsLegalTarget` |
| `SpacelineLocationRules.cs` | Time Location / Span-Lage | `PlaysAsSpacelineLocation` |

---

## Game — Phrasen

| Datei | Wofür | Einstieg |
|-------|--------|----------|
| `PlayOnRules.cs` | Plays on / as, Host | `ResolvePlayOn`, `VerifyAwayTeamCrewSplit` |
| `TargetQuery.cs` | Zielliste für Halo/Picker | — |
| `TargetingRules.cs` | Board-Snap ja/nein | `UsesBoardSnap` |
| `ReportingRules.cs` | 6.3 Report | — |
| `DownloadRules.cs` | 6.5.3–6.5.4 | `FilterSource`, Gift Box |
| `DualAffiliationRules.cs` | 6.3.3 Mix | — |
| `TreatyRules.cs` | Treaty / occupy | `CanOccupyHost` |
| `MovementRules.cs` | Staff + RANGE / Fly | — |
| `MovementHazardRules.cs` | Q-Net, Gaps, Tetryon, Rift | `CheckMovement`, `QNetCrossesPath` |
| `DockingRules.cs` | Dock / Undock | — |
| `RequiredMoveRules.cs` | 7.10 Pflichtflug | Cytherians FarEnd |
| `BattleRules.cs` | 7.4 / 7.5 | `ResolveFire`, `ResolvePersonnelBattle`, Kurlan/SAM |
| `TimingRules.cs` | Stack, just, Response, SoT | `Push`/`Pop`, `CollectLegalResponses`, `IsJustAfter` |
| `TurnExpiry.cs` | until end of turn | `Register`, `Due` |
| `EndOfTurnEventRules.cs` | Events am EOT | TAK, SWB, Plasma, WCB, Anti-Time |
| `EndOfTurnRestRules.cs` | übrige EOT/SOT | Repair, Junior, Countdown |
| `ModifierRules.cs` | effektive Skills/Werte | — |
| `DetailStatusRules.cs` | Statuszeile | — |

---

## Vorlagen und Altbestand

Nicht vermehren. Gleiche Phrase in die Typ- oder Verb-Datei.

| Datei | Wofür |
|-------|--------|
| `CardEffectMap.cs` | Premiere-Name → Vorlagen-Id |
| `EffectRegistry.cs` | Karte → `IEffect` |
| `IEffect.cs` | Vertrag |
| `GapsNullifyRules.cs` | Gaps-Nullify |
| `HailRules.cs` | Hail (AU) |
| `IncomingMessageRules.cs` | Incoming Message |
| `InstantEventRules.cs` | `Decide` Sofort-Event |
| `InterruptShipEffectRules.cs` | Interrupt am Schiff |
| `NamedInterruptRules.cs` | `Decide` Namens-Routing |
| `WnohgbRules.cs` | Where No One Has Gone Before Wrap |

---

## Game/Board

| Datei | Wofür |
|-------|--------|
| `Spaceline.cs` | Locations; `HopCost`, `PathBlocked` |
| `Location.cs` | Spalte (Mission / Span / Time) |
| `Occupant.cs` | Schiff oder Facility |
| `Force.cs` | Crew oder Away Team |
| `CardInstance.cs` | Kopie + Status |
| `BoardStore.cs` | Attachments, Unique, Dump; `HasAttachedEvent`/`Dilemma` |
| `BoardAttachments.cs` | Attach-Helfer |

Gedruckte `Card` = JSON. Instanz = Identität + Status. Beamen wechselt die Force.  
Spans (Q-Net/Gaps): eigene Spalte in `_spacelineOrder` zwischen den Endpunkt-Missionen (`InsertSpanColumn`).  
`GetSpacelineColumns` = nur Mission/Time/Pod. Snapshot: `MissionIds` + `ColumnInstanceIds` + `Spans`. `PaintSpans` hält Y/Face.

---

## TableWindow — Cluster (Suche nach dem Namen)

~33 440 Zeilen, ~974 Methoden, keine `#region`. Apply bleibt hier; Decide nicht hier erfinden.

| Cluster | Typische Namen |
|---------|----------------|
| Spaceline / Span | `PlaceSpanOnSpaceline`, `PaintSpans`, `TryBindSpanEndpoints`, `ApplyNetSpanFromSnapshot`, `RelayoutMissionsOnSpaceline`, `DumpSpacelineTruth` |
| Dock / Relayout | `RelayoutAllDockables`, `RelayoutDockablesUnderMission`, `RelayoutGapsDockables` |
| Seed | `TryApplyNetSeedCard`, `SeedMissionFromNetNote`, `RelayoutSeedUnderMissions`, `CollectSitesForDrag` |
| Play / Stack | `BeginPlayCardStack`, `TryApplyNetPlayCard`, `TryPlayAsResponse`, `ResolveEntireStack` |
| Fly-in | `ShowPlayFlyIn`, `FindPlayFlyInLandBorder`, `EnsurePlayFlyInCardArt`, `InvalidatePlayFlyInTargets` |
| Choice / Response | `AskChoiceForPlayer`, `AskChoiceLocal`, `CollectAllLegalResponses`, `ShowThinkTrayForResponder` |
| Beam | `BeginBeamMode`, `BeamBackAwayTeamToShipOrOutpost`, `ResolveArmbandsBeamHost` |
| Fly ship | `TryApplyNetFly` + MovementRules |
| Battle | `BeginShipBattleStack`, `BeginPersonnelBattleStack`, `ResolveShipBattle` |
| Netz Session | `EnsureNetworkModeFromSession`, `OpenNetworkLobby`, `BroadcastMaskedStateToGuest` |
| Save | `CaptureGameSave`, `ApplyGameSave` |
| Overlays | History-Strip, Kidnap/Team/Reveal, Detail |

---

## Suchkommentare

```csharp
// Rule: 7.2.2 dilemma cure present
// Glossary: nullify
// Verb: plays-as interrupt beam battle
```

Wortliste (erweitern nur in `IMPLEMENT.md`, hier an der Datei wiederholen):

`seed` `play` `plays-as` `plays-on` `report` `download` `beam` `fly` `dock` `staff` `cloak` `attempt` `solve` `dilemma` `cure` `artifact` `event` `interrupt` `battle` `damage` `nullify` `just` `response` `stack` `end-of-turn` `countdown` `stop` `disable` `present` `here` `aboard` `in-play` `unique` `treaty` `spaceline-span` `adjacent`

### Nachträge 2026-09-21 (weiter gültig)
- SoT-Fenster: `TimingRules.RequiresStartOfTurnWindow` — erster Nutzer Full Planet Scan.
- `MissionRules.PersonnelTypePresent` — Class vs Skill; Kurlan über `BattleRules.AttributeAfterSam`.
