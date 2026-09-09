# EXTRACT_REST — Persist + Battle + Rest-Wahrheit in TableWindow

**Stand:** 2026-09-09 (Analyse Grok-Cloud gegen `master` SHA `e416170d5bb8fddd2edc01a2a6950805efbe8866`)  
**Quelle:** `StarTrekCCG/TableWindow.xaml.cs` (21 541 Zeilen)  
**Zweck:** Abarbeitungsliste fürs lokale Grok-Team. Kein Big-Bang-Split der Window-Datei.  
**Prinzip:** Decide = `Game/*Rules` (kein WPF). Apply/View = TableWindow (Ask/Reveal/Damage/Relayout).  
**Board-Wahrheit:** Ort/Crew/Persist-Instanz → `BoardStore` / CardInstance, nicht `_attached*` als Autorität.

Welle 1 Slices 1–9 sind **DONE** (Decide-Gates). Diese Liste ist **Welle 2**.

---

## Wie abarbeiten

1. Ein Ticket = eine ID unten (`P2-B3`, `P0-D1`, …). Ein Commit-Thema.  
2. Zuerst Decide-Funktion in der genannten `*Rules`-Datei (oder neue Datei nur wenn Cluster ≥3 Karten / eigener Verb-Block).  
3. TableWindow nur verdrahten: Plan lesen, Side-Effects anwenden, UI.  
4. Keine Premiere-Kartenwelle in denselben Commit.  
5. Keinen parked Bug stillschweigend mitfixen.  
6. Danach: eine Zeile `CHANGELOG.md` + Häkchen hier + ggf. `HANDOFF.md` Active-Zeile.  
7. Pepsch smoke → Push `master`.

**Fertig-Kriterium je Ticket:** TableWindow enthält für dieses Thema keine `if (kind == X)`-Wirkung mehr, nur `var plan = FooRules.Decide…(…); Apply(plan)`.

**Nicht anfassen in Welle 2:** Paint/Snap/Halo/Zoom, DeckBuilder, Save-UI, Quick-Game-Seed-Chrome, `TableWindow.DetailGroups.cs` (leer, Revert 2026-09-08).

---

## P0 — Persist-Modell (Fundament, zuerst)

Ohne P0 bleibt jeder EOT-Tick an `Border`-Hosts kleben.

### P0-D1 — `AttachedDilemma` aus Window nach Board

| | |
|---|---|
| **TW heute** | Klasse `AttachedDilemma` L254–264; Liste `_attachedDilemmas` L266 |
| **Felder** | `Card`, `Kind` (`DilemmaRules.PersistKind`), `Countdown`, `Host` (**Border**), `Extra`, `Dest` (**Border**), `Held` |
| **Ziel** | Neue Persist-Instanz auf Board-Seite: Host = `Location` / `instanceId`, nicht `Border`. Datei-Vorschlag: `Game/Board/PersistAttachment.cs` **oder** Felder an `CardInstance` / `Location`. Lookup-API in `DilemmaRules` oder kleines `PersistStore`. |
| **Apply bleibt TW** | Token malen, Held-Visual (Stasis), Detail-Gruppen |
| **Done** | `_attachedDilemmas` ist Mirror oder tot; Capture/Save lesen Store |

### P0-E1 — `AttachedEvent` aus Window nach Board

| | |
|---|---|
| **TW heute** | Klasse `AttachedEvent` L323–344; Liste `_attachedEvents` L346 |
| **Felder** | `Card`, `Kind` (`EventRules.Persist`), `Owner`, `Host`/`Host2` (**Border**), `Countdown`, `FaceUp`, Espionage-Paar, `TravelerPlayer`, `TurnScope`, `PhasePoint`, `ScopePlayer`, `SavedHostOwner` |
| **Ziel** | Dieselbe Persist-Instanz-Schicht wie P0-D1. `Host2` = Gaps/Q-Net zweites Ende → zwei Location-Ids. |
| **Done** | EOT-Schleifen iterieren Store, nicht `_attachedEvents` als Quelle |

### P0-S1 — Dual-Run abschließen

| | |
|---|---|
| **TW heute** | `CaptureEngineState` L886–1155; `ApplyUiStatusToStore` L9293; `_stackOnHost`, `_hullDamagePercent` L138, `_repairTurnsAtOutpost` L147, `_cloakedShips` L352, `_cloakLocked` L364 |
| **Ziel** | Hull / Cloak / RepairTurns / Occupants nur noch Store. Overlay liest Store (G4-Muster, kein `ui || store`). |
| **Datei** | `Game/Board/BoardStore.cs` + Capture verdünnen |
| **Done** | Ein Status hat eine Quelle |

**Reihenfolge P0:** D1 und E1 parallel ok, wenn gemeinsames Attachment-Record zuerst steht. S1 nach den ersten Persist-Ticks, nicht als Letztes.

---

## P1 — Borg Ship EOT (explizit aus Slice 9 rausgehalten)

Kompendium-Dilemma „Borg Ship“: WEAPONS 24, EOT angreifen, eine Mission weiter, runter von der Spaceline, +15 wenn zerstört.

| ID | TW-Methoden | Zeilen | Ziel-Decide | Apply bleibt in TW |
|---|---|---|---|---|
| **P1-B1** | `ProcessBorgShipEndOfTurn` | L17162 | `DilemmaRules` oder neu `BorgShipRules.DecideEotStep` | — |
| **P1-B2** | `StartBorgShipEotAttacks` | L17164+ | Welche Schiffe hier legal Ziele sind (uncloaked / same location / not stasis) | Queue + Reveal |
| **P1-B3** | `BeginNextBorgEotAttack` | L17195–17224 | FireCalc: WEAPONS 24 vs SHIELDS → Hit/Direct via `BattleRules.ResolveFire` | `ApplyHullDamage`, Overlay |
| **P1-B4** | `FinishBorgShipEotMove` | L17226–17263 | Nächster Index, off-spaceline ja/nein, Richtung `_borgShipDir` L287 | Token umsetzen, Liste updaten |
| **P1-B5** | `PlaceBorgShipToken` / `PositionBorgShipToken` / `RemoveBorgShipToken` | L17697–17762 | — | **bleibt View** (Token) |
| **P1-B6** | Felder `_borgShipToken`, `_borgEotAttackQueue`, `_borgEotActive`, `_borgEotHitLog`, `_borgShipDir` | L174–179, L287 | Richtung + Location-Id in Persist-Instanz | Token-Ref darf Window bleiben |

**Parked (nicht in P1 mischen):** Hugh-Zweig „Borg Ship Dilemma this pulse“ (`ApplyHugh` L12806, `ResolveHughRogueBorgHost` L12726). Eigenes Ticket **P4-H1**.

**Done:** Window kennt WEAPONS-24 und „eine Mission weiter“ nicht mehr als Literal-Logik.

---

## P2 — Battle-Ablauf (Decide-Gates sind schon in `BattleRules`)

Bereits in Rules: `CanInitiateShipAttack`, `CanReturnFire`, `CheckAffiliationAttackRestriction`, `ResolveFire`, `ApplyRotationDamage`, `DetermineWinner`, `CanInitiatePersonnelAttack`, `ResolvePersonnelBattle`, `HasLeader`, G1–G7.

### Ship Battle

| ID | TW-Methoden | Zeilen | Was noch Wahrheit ist | Ziel |
|---|---|---|---|---|
| **P2-S1** | `BeginAttackMode` | L16634–16733 | Zielwahl-Filter, Dock/Cloak/Stop-Recheck, Wartime-Lookup, loreStaffed-Bypass | Filter → `BattleRules` (+ Location from Store). Highlight bleibt TW |
| **P2-S2** | `CompleteShipAttack` | L16735–16796 | Wer schießt mit, Facility-½-SHIELDS wenn docked | `BattleRules.ResolveFire` Args vollständig aus Store |
| **P2-S3** | `ResolveShipBattle` | L16798–16975 | Open Fire → RF-Ask → Damage-Reihenfolge → Winner → Stopped → Destroy-am-Ende | Orchestriere nur noch `FireCalc`/`DamageOutcome`. Reihenfolge als `BattleRules.PlanShipBattle` |
| **P2-S4** | `AskReturnFireAndResolve` | L4207–4281 | Dialog + zweiter `CanReturnFire` + Schaden | Decide schon Rules; Dialog = Apply |
| **P2-S5** | `ApplyHullDamage` / `SetHullDamagePercent` / `GetHullDamage` | L16977–17009, L9442, L9495 | Prozent in `_hullDamagePercent` | Store-Feld; Badge = View |
| **P2-S6** | `DestroyShipOrFacility` | L19695–19804 | Crew-Verbleib, Cytherians-Discard ohne Punkte, Escape-Pod-Fenster | Destroy-Policy nach `BattleRules`/`DilemmaRules` (Cytherians-Zweig!) |
| **P2-S7** | Counter-Attack: `PendingCounterAttack` L376, `IsArmedCounterAttackAt` L16580, `IsCounterAttackTarget` L16585, `UpdateCounterAttackWindow` L16608 | Fenster-State in TW | EligiblePlayer + Location-Id + InvolvedIds → Session/Store. `BattleRules` hat schon `counterAttack:` |

### Personnel Battle

| ID | TW-Methoden | Zeilen | Ziel |
|---|---|---|---|
| **P2-P1** | `BeginPersonnelAttackFromHost` L19887–19958, `CanOfferPersonnelBattleFromShip` L19873 | Initiate-Filter (present / leader / affil) schon `CanInitiatePersonnelAttack` — TW darf nur Host-Karten sammeln und Ask |
| **P2-P2** | `CompletePersonnelAttack` L19960–20027 | Pairing kommt aus `ResolvePersonnelBattle` — prüfen ob TW noch Stun/Mortal selbst setzt |
| **P2-P3** | `ResolvePendingPersonnelBattle` L4283–4328 | Apply: Kill/Stop/Reveal |
| **P2-P4** | `BeginShipBattleStack` L3551 / `BeginPersonnelBattleStack` L3572 | Timing-Stack = `TimingRules`; Window nur Push/Highlight |

### Escape Pod / Aftermath

| ID | TW-Methoden | Zeilen | Ziel |
|---|---|---|---|
| **P2-E1** | `HasEscapePodInHand` L19574, `EscapePodHere` L19580 | Legal „darf jetzt Pod“ → `InterruptRules` / `BattleRules` | |
| **P2-E2** | `ApplyEscapePodFromResponse` L19588, `RecoverEscapePodCrew` L19641 | Crew-Rettung Apply; welche Crew legal = Rules | |

**Nicht in P2:** Battle Bridge / Tactics (Premiere-Kern laut `BattleRules`-Header bewusst raus). `_battleBridgeCards` L40 nur Stapel-UI.

---

## P3 — Event-Persist Apply (Gates Slice 8/9 da, Wirkung noch Window)

`EndOfTurnEventRules` / `EndOfTurnRestRules` liefern Enums. Fehlt: Wirkung + Lebenszyklus zentral.

Schleife: `ProcessEndOfTurnEvents` L18975–19133.

| ID | Persist / Thema | TW-Einstieg | Decide schon | Noch in TW (Wahrheit) | Ziel-Datei |
|---|---|---|---|---|---|
| **P3-01** | Plasma Fire | L18994+, L18614 | `DecidePlasmaFire` | Thermal-Scan, Host-Schiff finden, Destroy-Pfad | EventRules + bestehendes EOT-File |
| **P3-02** | Warp Core Breach | L19038+, L18605 | `DecideWarpCore` | ENGINEER-Nullify-Button-Pfad, Countdown-Owner | EventRules (Nullify optional bleibt Ask in TW) |
| **P3-03** | Static Warp Bubble | L19076+, L18625 | `DecideStaticWarp` | Welche Hand, Traveler-Kopplung | EventRules |
| **P3-04** | Kidnappers | `RunKidnappers` L530–604, `FinishKidnappers` L618 | `ShouldRunKidnappers` | Typ nennen + Random aus Opp-Hand = Regel | EventRules.DecideKidnappers(type, hand) |
| **P3-05** | Traveler extra draw | L19102, `IsTravelerInPlay` L14941 | `ShouldGrantTravelerExtraDraw` | `_pendingExtraDraws` L315 + `FinishEndOfTurnDrawExtras` L7243 | TurnExpiry / EventRules |
| **P3-06** | Neural Servo restore | L19110, `SavedHostOwner` | `ShouldRestoreNeuralServo` | Controller zurück, Side-Sync | EventRules + Store Owner |
| **P3-07** | Anti-Time expire | `ApplyAntiTimeExpire` L18398, SOT L18656 | `DecideAntiTime` | Shuffle-all-personnel-owned — das ist Regeltext | EventRules.ApplyPlan / eigene Methode ohne WPF außer Draw-Pile-UI |
| **P3-08** | Distortion flip | L18987 | `ShouldFlipDistortion` | `FaceUp` am Attachment; Beam-Block L18857 | EventRules + Beam-Gate in Movement/Reporting |
| **P3-09** | Ionization beam cap | L18862 | — dünn | „max 3 personnel this turn“ noch TW? Prüfen `CompleteBeamTo` L20080 | MovementRules oder EventRules.BeamGate |
| **P3-10** | Rift / Gaps on-move | `ApplyEventAfterMove` L18786, `CheckEventMovement` L18766 | Slice 1 `MovementHazardRules` | Gaps-Kill + Rift-Damage Apply; Host2 | MovementHazardRules Plan + Persist Host ids (P0) |
| **P3-11** | Gaps nullify relocate | `RelocateOccupantsAfterGapsNullify` L12929, `NullifyEventInPlay` L13038 | `GapsNullifyRules` | Wer wohin (adjacent pick) | GapsNullifyRules Decide adjacent + TW Relayout |
| **P3-12** | Q-Net / Tetryon pass | Fly-Pfad + Event persist | MovementHazardRules / EventRules.IsQNet | Diplomacy×2 / Navigation-again | sicherstellen: kein zweiter Check nur in TW |
| **P3-13** | Spacedock repair host | L18008, L18844 | `EndOfTurnRestRules.DecideRepair` | „ist diese Facility Repair-Host“ | EventRules.IsSpacedock + DockingRules |
| **P3-14** | Incoming Message | `ApplyIncomingMessage` L17546, `ProcessIncomingMessageMoves` L17419, `ResolveIncomingMessageArrival` L17640, `CollectIncomingMessageFacilities` L17529 | `IncomingMessageRules` + `RequiredMoveRules` | Facility-Lookup, Affil-Filter, Arrival-Discard | **Parked Bug:** IM FindMission/false-already-at — nicht still mitfixen, eigenes P3-14b nach Extract |
| **P3-15** | Required move / Cytherians dest | `CollectRequiredMoveDests` L17376, `ResolveFarEndMission` L17356, `ShipHasRequiredMove` L17269 | `RequiredMoveRules.FarEndIndex` | Dest als Border | Dest = Location-Id (P0-D1 `Dest`) |
| **P3-16** | Lore Returns / Rogue staff | `ShipHasLoreReturns` L17786, `ShipStaffedByRogueBorg` L17790, `ProcessRogueBorgEndOfTurn` L18491, `ProcessCrosisStartOfTurn` L18701 | `DecideRogueInvade` | STR-Summe Rogue, Commandeer, Side-Wechsel | EventRules + BattleRules (Invade → P2) |
| **P3-17** | Subspace Interference | `ApplySubspaceInterference` L17667 | InterruptRules.Effect | RANGE-Lock Apply | InterruptRules Plan |
| **P3-18** | Asteroid Sanctuary / Distortion Continuum / Tachyon / Transwarp | `ApplyAsteroidSanctuary` L18198, `ApplyDistortionContinuum` L18249, `ApplyTachyonGrid` L18342 + `_cloakLocked` | Slice 7 `InterruptShipEffectRules` | Cloak-Lock als Window-HashSet | Lock-Flag an ShipInstance (P0-S1) |
| **P3-19** | Instant / named AU | `ApplyInstantEvent` L14621, `ApplyNamedAuInterrupt` L15187, `ApplySupernova` L14682 | InstantEventRules / NamedInterruptRules | Supernova+Tox Uthat Zerstör-Radius | EventRules.SupernovaPlan |
| **P3-20** | Kevin Convergence | `ApplyKevinConvergence` L15298 | InterruptRules.IsKevin | Welche Events an Location sterben | TimingRules + Event scan Store |
| **P3-21** | Red Alert / Yellow Alert | Red Alert play-count; `ApplyYellowAlert` L14981 | EventRules.Is* | Plays-left Zähler | GameSession / TurnExpiry, nicht Window-Feld |

EOT-Repair (nicht Event-Karte, aber Persist-artig):

| ID | Methoden | Ziel |
|---|---|---|
| **P3-R1** | `ProcessEndOfTurnRepairs` L17016–17057 | Nur `DecideRepair` + Store `RepairTurns`; Badge-Text existiert schon `FormatOutpostRepairStatusLine` |

---

## P4 — Dilemma-Persist Apply (Resolve ist in `DilemmaRules`, Tick noch Window)

Schleife: `ProcessEndOfTurnDilemmas` L17059–17154; SOT: `ProcessStartOfTurnDilemmas` L18742.

`DilemmaRules.PersistKind` in TW genutzt: BorgShip, Abduction, Cytherians, Scow, Junior, Nitrium, HyperAging, EdoProbe, FrameOfMind, Phased, RemFatigue, Menthar, TwoDim, Ktarian.

| ID | Kind | TW-Einstieg | Decide schon | Rest-Wahrheit in TW | Ziel |
|---|---|---|---|---|---|
| **P4-01** | Junior | EOT in ProcessEndOfTurnDilemmas | `JuniorDestroysShip` | RANGE−1 nur Controller-EOT, Host-Recalc | DilemmaRules + MovementRules.EffectiveRange |
| **P4-02** | Nitrium / HyperAging / RemFatigue | EOT countdown + cure | `CountdownExpired`, `CureAward`, `IsQuarantinePersist` | Wer zählt als present (Ship vs Location) = **FEATURES-GAP Cure-Present-Scope** — nicht raten, eigenes Ticket nach Spock | DilemmaRules.CanCure*(present) |
| **P4-03** | Menthar / TwoDim | Move-Block | dünn | Gate oft nur TW beim Fly | MovementRules.IsMoveBlocked(persist) |
| **P4-04** | Ktarian | SOT L18746 | Encounter in DilemmaRules | SOT disable-random **PARK** (kein Disable-System) | nicht erfinden |
| **P4-05** | Abduction | `TryCureAbductionsPresent` L16282, `CollectPresentAtMissionForCure` L16239 | Cure OR in DilemmaRules | Present-Menge | DilemmaRules + Store present |
| **P4-06** | Phased | Held + stasis leave-block | DilemmaRules.Phased | Held-Liste an Attachment (P0-D1) | |
| **P4-07** | Cytherians | Dest + arrival +15; Destroy ohne Punkte (P2-S6) | `VerifyCytherians` / FarEnd | Arrival-Detect noch TW | RequiredMoveRules + DilemmaRules.OnArrival |
| **P4-08** | Edo Probe | `ApplyEdoEndOfTurnPenalties` L15080 | `ShouldApplyEdoContinuePenalty` | −10 wenn unsolved | DilemmaRules |
| **P4-09** | Frame of Mind | `ApplyFrameOfMind` L15127, `TryCureFrameOfMindAt` L15155 | AU-Decide | Affiliation 3-3-3 + cure present | DualAffiliationRules + DilemmaRules |
| **P4-10** | Conundrum | `ApplyConundrumChase` L15095 | AU-Decide | Chase-Ziel wählen | DilemmaRules Plan + TW Ask |
| **P4-11** | Scow | Attempt-Block am Host | MissionRules / piece.AttemptBlocked | Abschlepp-Check Tractor+2 ENG | MovementRules oder DilemmaRules.CanTowScow |
| **P4-12** | BorgShip | → P1 | | | |
| **P4-H1** | Hugh vs Borg Ship / Rogue | `ApplyHugh` L12806, `HostMatchesRogueBorgTarget` L12512, `ResolveHughRogueBorgHost` L12726 | `InterruptRules.DecideHugh` | Welches Ziel legal (Dilemma-Token vs Rogue-Location) | InterruptRules.HughTarget — **parked smoke, Captain priorisieren** |

Encounter-Apply (kein Persist, aber noch kartenspezifisch in TW):

| ID | Methode | Zeilen | Hinweis |
|---|---|---|---|
| **P4-A1** | `ApplyDilemmaResult` | L15539–15753 | Soll nur `Result`-Flags ausführen. Sonderzweige (Crystalline all-life, Temporal Loop restore L15760, AttachHost furthest) nach `DilemmaRules.DecideAttachHost` sind schon teildrin — Restflags nicht neu in TW erfinden |
| **P4-A2** | `BeginAlienParasitesOpponentControl` | L15947–16087 | Neg-Control Min-Pfad da; Hotseat-Chooser **PARK** (FEATURES P1) |

---

## P5 — Battle-benachbarte Orchestrierung (nur wenn P2 grün)

| ID | Methode | Zeilen | Aktion |
|---|---|---|---|
| **P5-01** | `TryResolveInterruptPlay` | L13092–13444 | Bleibt Apply-`switch` auf `InterruptRules.Effect`. Keine neuen `case`s. Wenn ein case >15 Zeilen Decide hat → in InterruptRules ziehen |
| **P5-02** | `ResolveTopOfStack` | L3947–4113 | Timing bleibt `TimingRules`; Window resolvedestination |
| **P5-03** | `LegalResponsesFor` / `ApplyResponseEffect` | L3620 / L3627 | Responses aus TimingRules/InterruptRules, nicht neu in TW |
| **P5-04** | Schism | L3599–3618 | Draw-discard Regel nach InterruptRules; Zähler darf Session sein |

---

## Empfohlene Ticket-Reihenfolge

```
P0-D1 + P0-E1     Attachment-Record (gemeinsames Modell)
P0-S1             Hull/Cloak/Repair in Store (kann nach erstem Tick-Extract kommen)
P3-R1             Repair-Tick (klein, übt das Muster)
P3-01 / P3-02     Plasma + Warp Core (EOT schon gated)
P3-03 / P3-04     Static Warp + Kidnappers
P4-01 / P4-02     Junior + Countdown-Dilemmas
P4-07 / P3-15     Cytherians + Required-Move Dest als Location
P1-B1 … P1-B4     Borg Ship EOT
P2-S3 + P2-S4     Ship-Battle Plan + RF
P2-S7             Counter-Attack State
P2-P1 … P2-P3     Personnel Battle verdünnen
P3-14             Incoming Message (nach P0 Host-Ids; Bug separat)
P4-H1             Hugh (parked, Captain Go)
P3-18 / P0-S1     Cloak-Lock an Instance
```

Nicht parallel: P1 und P2-S3 (beide feuern Damage/Destroy).  
Nicht vor P0: P3-14, P4-07, P1-B4 (brauchen Location-Id statt Border-Host).

---

## Bewusst NICHT auf dieser Liste

- Premiere-Kartenwelle A/B (FEATURES: only Captain Go).  
- Sites / Tactics / Battle Bridge spielen.  
- Netz, KI, Borg 7.3 außer dem einen Dilemma-Token.  
- Window in Partial Classes splitten.  
- `CARD_TRACKER` unknown→working (anderes Team: Spock/Jadzia).  
- Docs-only vs Code: `PROJECT.md` Board-Satz ist veraltet; nicht in Extract-Commits mitreparieren außer eine HANDOFF-Zeile.

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
ACTIVE (Welle 2 Extract): artifacts/EXTRACT_REST.md
Als Nächstes: P0-D1/E1 Attachment-Record (kein Karten-Text).
Persist/Battle nicht in dieselbe PR wie eine Premiere-Karte.
```
