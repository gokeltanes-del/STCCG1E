# EXTRACT_REST — Persist + Battle + Rest-Wahrheit in TableWindow

**Geprüft:** 2026-09-21 gegen `artifacts/StarTrekCCG/TableWindow.xaml.cs` (26 735 Zeilen).  
**Vorheriger Stand in dieser Datei:** 2026-09-09, 21 541 Zeilen, SHA `e416170` — Zeilenangaben waren veraltet, die Tickets selbst nicht erledigt.  
**Zweck:** Einzige Abarbeitungsliste, was noch aus `TableWindow` in Rules/Board soll.  
Ist-Landkarte: `TABLEWINDOW_INVENTORY.md`. Ablauf: `IMPLEMENT.md`. Rangfolge der Arbeit: `FEATURES.md` (Premiere vor Extract, bis Captain anders sagt).

Slices 1–9 (Decide-Tore) liegen in `MovementHazardRules`, `IncomingMessageRules`, `InstantEventRules`, `NamedInterruptRules`, `InterruptShipEffectRules`, `EndOfTurnEventRules`, `EndOfTurnRestRules`. Diese Liste ist der Rest: Wirkung und Wahrheit noch im Fenster.

---

## Audit 2026-09-21 (kurz)

Noch **offen**, wie beschrieben: `AttachedDilemma` / `AttachedEvent` und die Listen `_attachedDilemmas` / `_attachedEvents` sitzen weiter in TableWindow. `BoardStore` hat keine Persist-Attachments. Hull, Cloak, Repair-Turns, Borg-Ship-EOT, Battle-Orchestrierung und Event-Apply-Schleifen sind weiter Fenster-Wahrheit. Die Decide-Methoden in den EOT-Rules-Dateien existieren weiterhin.

Nur Metadaten und ein Methodenname sind überholt:

- Fast alle Zeilennummern um 0–4 200 verschoben (Datei um ~5 200 Zeilen gewachsen).
- `TryCureAbductionsPresent` gibt es nicht mehr. Cure/Present für Abduction läuft über `CollectPresentAtMissionForCure` und den Mission-completed-Zweig (~L13530) plus `AttachedDilemma.Held`.
- `TableWindow.DetailGroups.cs` ist nicht leer (35 Zeilen), aber weiter keine Regelwahrheit.

---

## Wie abarbeiten

1. Ein Ticket = eine ID unten. Ein Commit-Thema.  
2. Zuerst Decide in der genannten `*Rules`-Datei (neue Datei nur bei neuem Verb-System, siehe `IMPLEMENT.md`).  
3. TableWindow nur verdrahten: Plan lesen, Side-Effects, UI.  
4. Keine Premiere-Karte im selben Commit.  
5. Keinen parked Bug stillschweigend mitfixen.  
6. Danach: Zeile `CHANGELOG.md`, Häkchen hier, `HANDOFF.md` offen/geschlossen.  
7. Pepsch smoke → Push `master`.

**Fertig-Kriterium:** TableWindow enthält für das Thema keine `if (kind == X)`-Wirkung mehr, nur `var plan = FooRules.Decide…(…); Apply(plan)`.

**Nicht anfassen in diesem Extract:** Paint/Snap/Halo/Zoom, DeckBuilder, Save-UI, Quick-Game-Seed-Chrome, `TableWindow.DetailGroups.cs`.

---

## P0 — Persist-Modell (Fundament, zuerst)

Ohne P0 bleibt jeder EOT-Tick an `Border`-Hosts kleben.

### P0-D1 — `AttachedDilemma` aus Window nach Board

| | |
|---|---|
| **TW heute** | Klasse `AttachedDilemma` L280–292; Liste `_attachedDilemmas` L294 |
| **Felder** | `Card`, `Kind` (`DilemmaRules.PersistKind`), `Countdown`, `Host` (**Border**), `Extra`, `Dest` (**Border**), `Held` |
| **Ziel** | Persist-Instanz auf Board-Seite: Host = `Location` / `instanceId`, nicht `Border`. |
| **Apply bleibt TW** | Token malen, Held-Visual (Stasis), Detail-Gruppen |
| **Done** | `_attachedDilemmas` ist Mirror oder tot; Capture/Save lesen Store |

### P0-E1 — `AttachedEvent` aus Window nach Board

| | |
|---|---|
| **TW heute** | Klasse `AttachedEvent` L380–403; Liste `_attachedEvents` L405 |
| **Felder** | `Card`, `Kind` (`EventRules.Persist`), `Owner`, `Host`/`Host2` (**Border**), `Countdown`, `FaceUp`, Espionage-Paar, `TravelerPlayer`, `TurnScope`, `PhasePoint`, `ScopePlayer`, `SavedHostOwner` |
| **Ziel** | Dieselbe Persist-Schicht wie P0-D1. `Host2` = Gaps/Q-Net zweites Ende → zwei Location-Ids. |
| **Done** | EOT-Schleifen iterieren Store, nicht `_attachedEvents` als Quelle |

### P0-S1 — Dual-Run abschließen

| | |
|---|---|
| **TW heute** | `CaptureEngineState` L1027; `ApplyUiStatusToStore` L10655; `_stackOnHost` L164; `_hullDamagePercent` L138; `_repairTurnsAtOutpost` L147; `_cloakedShips` L420; `_cloakLocked` L432 |
| **Ziel** | Hull / Cloak / RepairTurns / Occupants nur noch Store. Overlay liest Store. |
| **Datei** | `Game/Board/BoardStore.cs` + Capture verdünnen |
| **Done** | Ein Status hat eine Quelle |

**Reihenfolge P0:** D1 und E1 parallel ok, wenn gemeinsames Attachment-Record zuerst steht. S1 nach den ersten Persist-Ticks.

---

## P1 — Borg Ship EOT

Kompendium-Dilemma „Borg Ship“: WEAPONS 24, EOT angreifen, eine Mission weiter, runter von der Spaceline, +15 wenn zerstört.

| ID | TW-Methoden | Zeilen | Ziel-Decide | Apply bleibt in TW |
|---|---|---|---|---|
| **P1-B1** | `ProcessBorgShipEndOfTurn` | L20141 | `DilemmaRules` oder System-Decide EOT-Schritt | — |
| **P1-B2** | `StartBorgShipEotAttacks` | L20147 | Welche Schiffe hier legale Ziele sind | Queue + Reveal |
| **P1-B3** | `BeginNextBorgEotAttack` | L20187 | FireCalc: WEAPONS 24 vs SHIELDS über `BattleRules.ResolveFire` | `ApplyHullDamage`, Overlay |
| **P1-B4** | `FinishBorgShipEotMove` | L20222 | Nächster Index, off-spaceline, Richtung `_borgShipDir` L321 | Token umsetzen |
| **P1-B5** | `PlaceBorgShipToken` / `PositionBorgShipToken` / `RemoveBorgShipToken` | L21025 / L21077 / L21121 | — | **bleibt View** |
| **P1-B6** | Felder `_borgShipToken`, `_borgEotAttackQueue`, `_borgEotActive`, `_borgEotHitLog`, `_borgShipDir` | L175–185, L321 | Richtung + Location-Id in Persist-Instanz | Token-Ref darf Window bleiben |

**Parked (nicht in P1 mischen):** Hugh-Zweig (`ApplyHugh` L14409, `ResolveHughRogueBorgHost` L14329, `HostMatchesRogueBorgTarget` L14111). Eigenes Ticket **P4-H1**.

**Done:** Window kennt WEAPONS-24 und „eine Mission weiter“ nicht mehr als Literal-Logik.

---

## P2 — Battle-Ablauf (Decide-Gates schon in `BattleRules`)

Bereits in Rules: `CanInitiateShipAttack`, `CanReturnFire`, `CheckAffiliationAttackRestriction`, `ResolveFire`, `ApplyRotationDamage`, `DetermineWinner`, `CanInitiatePersonnelAttack`, `ResolvePersonnelBattle`, `HasLeader`.

### Ship Battle

| ID | TW-Methoden | Zeilen | Was noch Wahrheit ist | Ziel |
|---|---|---|---|---|
| **P2-S1** | `BeginAttackMode` | L19585 | Zielwahl-Filter, Dock/Cloak/Stop-Recheck | Filter → `BattleRules`. Highlight bleibt TW |
| **P2-S2** | `CompleteShipAttack` | L19686 | Wer schießt, Facility-½-SHIELDS wenn docked | `BattleRules.ResolveFire` aus Store |
| **P2-S3** | `ResolveShipBattle` | L19749 | Open Fire → RF-Ask → Damage → Winner → Stopped → Destroy | Orchestrierung als Plan in `BattleRules` |
| **P2-S4** | `AskReturnFireAndResolve` | L4973 | Dialog + zweiter `CanReturnFire` + Schaden | Decide schon Rules; Dialog = Apply |
| **P2-S5** | `ApplyHullDamage` / `SetHullDamagePercent` / `GetHullDamage` | L19937 / L10813 / L10860 | Prozent in `_hullDamagePercent` L138 | Store-Feld; Badge = View |
| **P2-S6** | `DestroyShipOrFacility` | L24631 | Crew-Verbleib, Cytherians-Discard ohne Punkte, Escape-Pod-Fenster | Destroy-Policy nach Rules |
| **P2-S7** | `PendingCounterAttack` L455, `IsArmedCounterAttackAt` L19531, `IsCounterAttackTarget` L19536, `UpdateCounterAttackWindow` L19559 | Fenster-State in TW | EligiblePlayer + Location-Id → Session/Store |

### Personnel Battle

| ID | TW-Methoden | Zeilen | Ziel |
|---|---|---|---|
| **P2-P1** | `BeginPersonnelAttackFromHost` L24904, `CanOfferPersonnelBattleFromShip` L24890 | TW sammelt Host-Karten und fragt; Initiate-Filter schon Rules |
| **P2-P2** | `CompletePersonnelAttack` L24977 | Pairing aus `ResolvePersonnelBattle` — prüfen ob TW noch Stun/Mortal selbst setzt |
| **P2-P3** | `ResolvePendingPersonnelBattle` L5049 | Apply: Kill/Stop/Reveal |
| **P2-P4** | `BeginShipBattleStack` L3769 / `BeginPersonnelBattleStack` L3797 | Timing-Stack = `TimingRules`; Window nur Push/Highlight |

### Escape Pod / Aftermath

| ID | TW-Methoden | Zeilen | Ziel |
|---|---|---|---|
| **P2-E1** | `HasEscapePodInHand` L24342, `EscapePodHere` L12965 | Legal „darf jetzt Pod“ → `InterruptRules` / `BattleRules` |
| **P2-E2** | `ApplyEscapePodFromResponse` L24356, `RecoverEscapePodCrew` L24415 | Crew-Rettung Apply; welche Crew legal = Rules |

**Nicht in P2:** Battle Bridge / Tactics.

---

## P3 — Event-Persist Apply (Gates in Slice 8/9, Wirkung noch Window)

`EndOfTurnEventRules` / `EndOfTurnRestRules` liefern weiter die Decide-Enums (`DecidePlasmaFire`, `DecideWarpCore`, `DecideStaticWarp`, `ShouldFlipDistortion`, `ShouldGrantTravelerExtraDraw`, `ShouldRunKidnappers`, `ShouldRestoreNeuralServo`, `DecideAntiTime`, `DecideRepair`).

Schleife: `ProcessEndOfTurnEvents` L23260.

| ID | Persist / Thema | TW-Einstieg | Decide schon | Noch in TW (Wahrheit) | Ziel-Datei |
|---|---|---|---|---|---|
| **P3-01** | Plasma Fire | EOT-Schleife L23260+ | `DecidePlasmaFire` | Thermal-Scan, Host-Schiff, Destroy | EventRules + EOT-File |
| **P3-02** | Warp Core Breach | EOT-Schleife | `DecideWarpCore` | ENGINEER-Nullify-Button, Countdown-Owner | EventRules |
| **P3-03** | Static Warp Bubble | EOT-Schleife | `DecideStaticWarp` | Welche Hand, Traveler-Kopplung | EventRules |
| **P3-04** | Kidnappers | `RunKidnappers` L666, `FinishKidnappers` L754 | `ShouldRunKidnappers` | Typ + Random aus Opp-Hand | EventRules.DecideKidnappers |
| **P3-05** | Traveler extra draw | `IsTravelerInPlay` L17207 | `ShouldGrantTravelerExtraDraw` | `_pendingExtraDraws` L358 + `FinishEndOfTurnDrawExtras` L8378 | TurnExpiry / EventRules |
| **P3-06** | Neural Servo restore | EOT-Schleife, `SavedHostOwner` | `ShouldRestoreNeuralServo` | Controller zurück | EventRules + Store Owner |
| **P3-07** | Anti-Time expire | `ApplyAntiTimeExpire` L22571 | `DecideAntiTime` | Shuffle-all-personnel-owned | EventRules ohne WPF außer Pile-UI |
| **P3-08** | Distortion flip | EOT-Schleife | `ShouldFlipDistortion` | `FaceUp` am Attachment | EventRules + Beam-Gate |
| **P3-09** | Ionization beam cap | `_ionizationBeamsThisTurnByPlayer` L407 | — dünn | max 3 Personnel je Controller/Zug noch TW | MovementRules oder EventRules.BeamGate |
| **P3-10** | Rift / Gaps on-move | `ApplyEventAfterMove` L22963, `CheckEventMovement` L22943 | `MovementHazardRules` | Gaps-Kill + Rift-Damage Apply; Host2 | MovementHazardRules Plan + Persist Host ids (P0) |
| **P3-11** | Gaps nullify relocate | `RelocateOccupantsAfterGapsNullify` L14612, `NullifyEventInPlay` L14721 | `GapsNullifyRules` | Wer wohin (adjacent pick) | GapsNullifyRules Decide + TW Relayout |
| **P3-12** | Q-Net / Tetryon pass | Fly-Pfad + Event persist | MovementHazardRules / EventRules.IsQNet | Diplomacy×2 / Navigation-again | kein zweiter Check nur in TW |
| **P3-13** | Spacedock repair host | Repair-Pfad + Event persist | `EndOfTurnRestRules.DecideRepair` | „ist diese Facility Repair-Host“ | EventRules.IsSpacedock + DockingRules |
| **P3-14** | Incoming Message | `ApplyIncomingMessage` L20544, `ProcessIncomingMessageMoves` L20417, `ResolveIncomingMessageArrival` L20638, `CollectIncomingMessageFacilities` L20527 | `IncomingMessageRules` + `RequiredMoveRules` | Facility-Lookup, Affil-Filter, Arrival-Discard | **Parked Bug:** IM FindMission — eigenes P3-14b |
| **P3-15** | Required move / Cytherians dest | `CollectRequiredMoveDests` L20374, `ResolveFarEndMission` L20354, `ShipHasRequiredMove` L20267 | `RequiredMoveRules.FarEndIndex` | Dest als Border | Dest = Location-Id (P0) |
| **P3-16** | Lore Returns / Rogue staff | `ShipHasLoreReturns` L21422, `ShipStaffedByRogueBorg` L21426, `ProcessRogueBorgEndOfTurn` L22664, `ProcessCrosisStartOfTurn` L22878 | `DecideRogueInvade` | STR-Summe Rogue, Commandeer, Side-Wechsel | EventRules + BattleRules |
| **P3-17** | Subspace Interference | `ApplySubspaceInterference` L20961 | InterruptRules.Effect | RANGE-Lock Apply | InterruptRules Plan |
| **P3-18** | Asteroid Sanctuary / Distortion Continuum / Tachyon / Transwarp | `ApplyAsteroidSanctuary` L21939, `ApplyDistortionContinuum` L22118, `ApplyTachyonGrid` L22241 + `_cloakLocked` L432 | `InterruptShipEffectRules` | Cloak-Lock als Window-HashSet | Lock-Flag an ShipInstance (P0-S1) |
| **P3-19** | Instant / named AU | `ApplyInstantEvent` L16614, `ApplyNamedAuInterrupt` L17471, `ApplySupernova` L16675 | InstantEventRules / NamedInterruptRules | Supernova-Zerstör-Radius | EventRules.SupernovaPlan |
| **P3-20** | Kevin Convergence | `ApplyKevinConvergence` L17589 | InterruptRules.IsKevin | Welche Events an Location sterben | TimingRules + Event scan Store |
| **P3-21** | Red Alert / Yellow Alert | `ApplyYellowAlert` L17265 | EventRules.Is* | Plays-left Zähler | GameSession / TurnExpiry |

EOT-Repair:

| ID | Methoden | Ziel |
|---|---|---|
| **P3-R1** | `ProcessEndOfTurnRepairs` L19976 | Nur `DecideRepair` + Store `RepairTurns`; Badge `FormatOutpostRepairStatusLine` L24074 |

---

## P4 — Dilemma-Persist Apply

Schleife: `ProcessEndOfTurnDilemmas` L20019; SOT: `ProcessStartOfTurnDilemmas` L22919.

`DilemmaRules.PersistKind` in TW genutzt: BorgShip, Abduction, Cytherians, Scow, Junior, Nitrium, HyperAging, EdoProbe, FrameOfMind, Phased, RemFatigue, Menthar, TwoDim, Ktarian.

| ID | Kind | TW-Einstieg | Decide schon | Rest-Wahrheit in TW | Ziel |
|---|---|---|---|---|---|
| **P4-01** | Junior | EOT in ProcessEndOfTurnDilemmas | `JuniorDestroysShip` | RANGE−1 nur Controller-EOT | DilemmaRules + MovementRules.EffectiveRange |
| **P4-02** | Nitrium / HyperAging / RemFatigue | EOT countdown + cure | `CountdownExpired`, `CureAward`, `IsQuarantinePersist` | Wer zählt als present — **FEATURES Cure-Present-Scope**, nicht raten | DilemmaRules.CanCure*(present) |
| **P4-03** | Menthar / TwoDim | Move-Block | dünn | Gate oft nur TW beim Fly | MovementRules.IsMoveBlocked(persist) |
| **P4-04** | Ktarian | SOT L22919 | Encounter in DilemmaRules | SOT disable-random **PARK** | nicht erfinden |
| **P4-05** | Abduction | `CollectPresentAtMissionForCure` L18790; Mission-completed-Cure ~L13530; Held an `AttachedDilemma` | Cure OR in DilemmaRules | Present-Menge und Held-Liste | DilemmaRules + Store present |
| **P4-06** | Phased | Held + stasis leave-block | DilemmaRules.Phased | Held-Liste an Attachment (P0-D1) | |
| **P4-07** | Cytherians | Dest + arrival +15; Destroy ohne Punkte (P2-S6) | FarEnd / VerifyCytherians | Arrival-Detect noch TW | RequiredMoveRules + DilemmaRules.OnArrival |
| **P4-08** | Edo Probe | `ApplyEdoEndOfTurnPenalties` L17364 | `ShouldApplyEdoContinuePenalty` | −10 wenn unsolved | DilemmaRules |
| **P4-09** | Frame of Mind | `ApplyFrameOfMind` L17411, `TryCureFrameOfMindAt` L17439 | AU-Decide | Affiliation 3-3-3 + cure present | DualAffiliationRules + DilemmaRules |
| **P4-10** | Conundrum | `ApplyConundrumChase` L17379 | AU-Decide | Chase-Ziel wählen | DilemmaRules Plan + TW Ask |
| **P4-11** | Scow | Attempt-Block am Host | MissionRules | Abschlepp-Check Tractor+2 ENG | MovementRules oder DilemmaRules.CanTowScow |
| **P4-12** | BorgShip | → P1 | | | |
| **P4-H1** | Hugh vs Borg Ship / Rogue | `ApplyHugh` L14409, `HostMatchesRogueBorgTarget` L14111, `ResolveHughRogueBorgHost` L14329 | `InterruptRules.DecideHugh` | Ziel legal (Dilemma-Token vs Rogue-Location) | **parked, Captain Go** |

Encounter-Apply:

| ID | Methode | Zeilen | Hinweis |
|---|---|---|---|
| **P4-A1** | `ApplyDilemmaResult` | L17830 | Soll nur `Result`-Flags ausführen. Keine neuen Sonderzweige in TW |
| **P4-A2** | `BeginAlienParasitesOpponentControl` | L18456 | Neg-Control-Pfad da; Hotseat-Chooser **PARK** (`FEATURES`) |

---

## P5 — Battle-benachbarte Orchestrierung (nur wenn P2 grün)

| ID | Methode | Zeilen | Aktion |
|---|---|---|---|
| **P5-01** | `TryResolveInterruptPlay` | L15023 | Apply-`switch` auf `InterruptRules.Effect`. Keine neuen `case`s. Case mit Decide → InterruptRules |
| **P5-02** | `ResolveTopOfStack` | L4650 | Timing bleibt `TimingRules` |
| **P5-03** | `LegalResponsesFor` L3759 / `ApplyResponseEffect` L3890 | Responses aus TimingRules/InterruptRules |
| **P5-04** | Schism `SyncSchismRound` L3824 | Draw-discard nach InterruptRules; Zähler darf Session sein |

---

## Empfohlene Ticket-Reihenfolge

```
P0-D1 + P0-E1     Attachment-Record
P0-S1             Hull/Cloak/Repair in Store
P3-R1             Repair-Tick
P3-01 / P3-02     Plasma + Warp Core
P3-03 / P3-04     Static Warp + Kidnappers
P4-01 / P4-02     Junior + Countdown-Dilemmas
P4-07 / P3-15     Cytherians + Required-Move Dest als Location
P1-B1 … P1-B4     Borg Ship EOT
P2-S3 + P2-S4     Ship-Battle Plan + RF
P2-S7             Counter-Attack State
P2-P1 … P2-P3     Personnel Battle verdünnen
P3-14             Incoming Message (nach P0; Bug separat)
P4-H1             Hugh (parked, Captain Go)
P3-18 / P0-S1     Cloak-Lock an Instance
```

Nicht parallel: P1 und P2-S3 (beide feuern Damage/Destroy).  
Nicht vor P0: P3-14, P4-07, P1-B4 (brauchen Location-Id statt Border-Host).

---

## Bewusst NICHT auf dieser Liste

- Premiere-Karten (`CARD_TRACKER.md` / `FEATURES.md`).  
- Sites / Tactics / Battle Bridge spielen.  
- Netz, KI, Borg 7.3 außer dem einen Dilemma-Token.  
- Window in Partial Classes splitten.

---

## Parked smoke (sichtbar halten, nicht an Extract hängen)

1. Hugh / Borg Ship Dilemma-Zweig  
2. IM FindMission / false already-at-facility  
3. Dump lässt Gaps-Schiffe weg  
4. Distortion ohne AU-Kante  
5. Parasites Hotseat-Chooser  
6. REM Fatigue voll  
7. Cure-Present-Scope (Skills an Bord ≠ ortsweit)  
8. Response-Window UX  

---

## Copy-Paste für HANDOFF.md (Active)

```
ACTIVE Extract: artifacts/EXTRACT_REST.md
Als Nächstes wenn Captain Go: P0-D1/E1 Attachment-Record (kein Karten-Text).
Persist/Battle nicht in dieselbe Arbeit wie eine Premiere-Karte.
```
