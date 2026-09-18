## 2026-09-17 — Hail (AU) + table UI chrome

## 2026-09-18 — Interphase Generator: may-nullify after just-encountered (Spock)

- Removed auto-nullify at reveal.
- After `DilemmaRules.Resolve` (targets + conditions), if [IPG] and IG present with attempting AT/crew: Yes/No to nullify before results. IG kept. Scope = planet surface AT or attempting-ship crew only.


## 2026-09-18 — Interphase Generator nullifies [IPG] dilemmas

- `CardIcons.HasIpg` / `IsIpgDilemma` from printed `[IPG]` tokens (no name list).
- On mission attempt: if Interphase Generator is present with the attempting team, revealed [IPG] dilemmas are discarded and the attempt continues (before Resolve).


## 2026-09-18 — Hail no-battle: Detail debuff (not under-card)

- Removed under-card "Hail: no battle" flags.
- Show as red **Debuff** line in card Detail status block (same place as Metaphasic / other host effects): "Hail: cannot battle X this turn".
- Status line kept; cleared at EOT with the restriction. Table-drop path unchanged.


## 2026-09-18 — Hail OR: table drop + no-battle flags

- **Drop:** Hail `GetPlayTarget` = None before PlayOnRules (printed "Plays on any ship" was forcing ship host). Play onto empty table/play area like Jaglom, then mark two ships.
- **Feedback:** after pair marked — status line + light "no battle" flags on both ships (partner name); cleared at EOT with the restriction.


## 2026-09-17 — Hail fly-by Pass: relocate after deferred move

- **Bug:** Pass / no Hail on `ShipFlyBy` spent RANGE and logged arrival, but left the ship token at the start (desync).
- **Fix:** `CompletePendingHailFly` now `RelocateShipAlongSpaceline` + sync after rules apply. Hail-played stop path unchanged.


## 2026-09-17 — Hail two-ship: spaceline click-mark (no Detail picker)

- **Hail** OR-mode UX (Pepsch): play Hail to table (no ship drop target), then click two ships on the spaceline — each lights up; second click applies no-battle-this-turn and discards Hail. Fly-by path unchanged (`ShipFlyBy`).
- Replaces DetailWindow list picker for identical ships.


- **Hail** (Alternate Universe interrupt, Spock Soll): fly-by response window (`ShipFlyBy`) when a ship span-passes a location with an opposing ship — play Hail to stop it there (no further move this turn, not game-stopped); discard Hail (no attach). OR one play selecting two ships — they cannot battle each other this turn (EOT clear). Subspace Interference still nullifies Hail on the stack.
- **UI:** removed inner `BoardInnerGlow` frame (outer `BoardFrameBorder` kept); ThinkTray chrome tightened (padding/margin/rail).

﻿# Changelog

Nur spielbare / engine-relevante Schritte. Keine Chat-Metadaten.

---

## 2026-09-17 (Feat - Subspace Interference)

**Engine** - Subspace Interference: CanRespond nullifies Incoming Message / Hail / Subspace Schism on the stack; Instant Apply picks attached Incoming Message or stack targets (Hail/Schism). CollectLegalResponses/Blink inherit CanRespond.

---
## 2026-09-16 (UX - ThinkTray vertical padding)

**UI** - ThinkTray Padding top/bottom + rail padding; MaxHeight raised so title and Pass are not clipped.

---
## 2026-09-16 (UX - ThinkTray compact left rail)

**UI** - ThinkTray content-width (centered, not full board). Header/Pass moved to vertical left rail; top-right Response banner and large Pass removed. Frame blink / hand-sized cards / hover unchanged.

---
## 2026-09-16 (UX - Response ThinkTray + Hand strip mockup)

**UI** - Hand labels vertical left (P1 #B5CEA8 / P2 #8EC8D8); hand strips Height ~102. Response: ThinkTray opens immediately with hand-sized minis + hover zoom; tray frame blinks 3s in owner accent (not cards); [R] only extends Think; Space cancels blink / Pass.

---
## 2026-09-16 (Fix - Asteroid Sanctuary ship-only response)

**Engine** - Asteroid Sanctuary CanRespond requires defending ship (not facility/outpost), your ship, exposed. Uses SanctuaryDeny. CollectLegalResponses/Blink inherit the gate. Apply path rejects facility defenders (no PickOwnShip fallback). Hugh still OK vs facility Borg battles. ActionKind unchanged.

---
## 2026-09-16 (UX - Legal-action blink cue)

**UI** - When legal response/action cards are known: blink those cards 3s (1 smooth cycle/sec) in owner P1/P2 accent; dim board slightly; Space cancels early; playable without waiting. Hooked on response window + Think mode.

---
## 2026-09-16 (UX - Cloak more transparent)

**UI** - ApplyCloakVisual Opacity 0.45 -> 0.28 (more see-through; still below stopped 0.55).

---
## 2026-09-16 (UX - Borg Ship EOT silent unless attack)

**UI** - Borg Ship EOT: Detail/Reveal only when an attack actually started (ships or outposts at location). Silent move along spaceline if nothing to attack; leave-play / destroy messages unchanged.

---
## 2026-09-15 (Fix - Response window above CardDetail)

**UI** - Think Tray / Response UI Z above CardDetailOverlay (220); hide detail while response open so Escape Pod etc. stay clickable; restore detail after close.

---
## 2026-09-15 (Fix - Escape Pod excludes captives)

**Engine/UI** - Escape Pod saves only your personnel/crew (not equipment, not opponent captives aboard). Empty / captive-only ships get no Escape Pod window.

---
## 2026-09-15 (Fix - Escape Pod Spock: crew only + empty skip)

**Engine/UI** - Escape Pod saves personnel/crew only (not equipment). Window only if ship has crew + Pod in hand (empty Ship Seizure path unchanged via DiscardShipSeizureVictim). Stack-open destroy still offers Pod.

---
## 2026-09-15 (Fix - Borg Ship load single token)

**UI** - After Load: strip stray Borg dilemma table Border; re-Place/Position Borg token after spaceline layout (Scow hygiene). One token only; double-click on live token.

---
## 2026-09-15 (Fix - Escape Pod on all DestroyShip paths)

**Engine/UI** - ShipDestroyed Escape Pod response opens even if another stack action is open (e.g. Borg/battle resolve). No silent DestroyShipOrFacility when Pod in hand.

---
## 2026-09-15 (Fix - Borg Ship encounter no stop)

**Engine** - Borg Ship Decide: StopTeam=false on encounter (App B). Place furthest end + end attempt only; stop survivors after battle participation (EOT), not on reveal.

---
## 2026-09-15 (Fix - Spock effective-skill gates)

**Engine** - Kit skill grants once per equipment type (covers all matching Class; no multi-kit stack on one person). Present excludes stopped/disabled/stasis. Mission text "X-classification" uses printed Class only (Kit skill does not satisfy).

---
## 2026-09-15 (Fix - Cloak vs Beam Compendium 7.6)

**Engine/UI** - Deny beam to/from cloaked ships (EvaluateBeam + LegalMoves); hide Beam on cloaked ships; do not highlight cloaked destinations. Decloak first. Docking already denied cloaked.

---
## 2026-09-15 (Fix - Effective skills for mission solve + EventRules)

**Engine** - Mission CanSolve present includes equipment (CollectPresentAtMission) so Kit/PADD grants count; EventRules.CountSkill uses ModifierRules.ResolvePersonnel (printed + classification + equipment). Kit does not change classification. Solvable-missions QoL uses GetAllCardsOnHost.

---
## 2026-09-15 (UX - Revealed under mission in Detailfenster)

**UX** - Remove mission side button "Show last revealed card under mission"; when a mission is selected, Detail stack lists last encountered/revealed dilemma under that mission (artifacts already listed). Display only.

---

## 2026-09-14 (Fix - Ship Seizure play-on Tractor host)

**Engine/UI** - Ship Seizure: drop/play-on ship is the Tractor host (no own-ship picker); only AskChoice/Pick among empty exposed ships at FindMissionForDockable(host); discard victim; HostMatches requires Tractor Beam.

---


## 2026-09-14 (Feat - Ship Seizure player pick)

**Engine/UI** - Ship Seizure: player picks own Tractor Beam ship + another empty exposed ship at same location (`PickCardFromList`); discard victim only (not first-canvas / Escape Pod destroy). `InterruptRules.IsLegalShipSeizure*` + Verify.

---


## 2026-09-14 (Fix - Scow/Borg token vs ship layout + tow follow)

**UI** - Tow hang no longer paints Scow on ship Left/Top (column after dockables + X nudge, Z=8); every `RelocateShipAlongSpaceline` pins dest then `SyncTowedScowAfterShipMove`; Relayout never `PositionScowToken` while towing; dilemma tokens excluded from dockables; Borg Ship token same Z/column helper (was Z=30+ over ships). AttemptBlocked / effective ENG / Destroy Scow unchanged.

---


## 2026-09-14 (UX - Scow Tractor attach-Fly-EOT)

**Engine/UI** - Tractor Beam... attach Scow to ship (tow state); normal Fly relocates Scow with ship; EOT towing player drops Scow at current mission (AttemptBlocked); no cloak while towing; tow != cure.

---


## 2026-09-14 (Fix - Scow Tractor effective ENGINEER)

**Engine/UI** - CanTowScow ENG via ModifierRules present=`GetAllCardsOnHost` (skill + class + Kit/PADD); Tractor withheld debug log if 2 printed ENG at Scow.

---

## 2026-09-14 (Fix - Scow token layout/Z/load)

**UI** - Scow token after all dockables under host (Relayout + load re-Position); Z below ships so dockables stay clickable; X aligns to host mission column after spaceline layout.

---

## 2026-09-14 (Feat - Tractor Beam + Radioactive Garbage Scow tow)

**Engine/UI** - `ShipHasSpecialEquipment` (Text comma-list); `CanTowScow` (Tractor + 2 ENG at Scow mission); ship side **Tractor** = legal fly then relocate Scow spaceline token (AttemptBlocked moves; not discard/cure). Destroy Scow interrupt unchanged.

---


## 2026-09-14 (Fix - Birth of "Junior" RANGE)

**Engine** - Junior RANGE penalty = attached Countdown (EOT ticks): `ResetShipRangesForTurn` / fallback / EOT apply EffectiveRange - Baryon - Junior.Countdown; RANGE<1 destroys; attach turn full RANGE; 3 ENG nullify unchanged.

---


## 2026-09-13 (Fix - El-Adrel Creature AT/Eff diag)

**Engine** - El-Adrel: encounter message + debug log show selected names, printed STRENGTH, Eff.Strength (dual StrengthDelta/framed), sum; planet CollectTeamBorders owner fallback (Controller/OwnerPlayer); refresh Team/Present before each dilemma; ApplyDilemmaResult ignores Kill on Overcome. Verify covers 9+9=18 pass.

---


## 2026-09-13 (Fix - Cure-Present-Scope Ship)

**Engine** - `TryCureAttachedDilemmas`: ship-hosted persist (Junior, Menthar, Nitrium, Ktarian, etc.) cures vs crew aboard **host ship only** (`GetAllCardsOnHost`); planet/mission-hosted stays location/AT. EOT Abduction/Phased location-present only when host is not a ship. Fixes false post-attach cure from second own ship at same mission (Pepsch Ktarian: host CUNNING==30 must not cure).

---

## 2026-09-13 (Fix - Save/Load Game State & Ship Hull Damage Persistence)

**Engine & Save/Load (`TableWindow.xaml.cs`)**:
- **Problem**: Bei Spielständen, in denen Schiffe unbeschädigt waren, konnte alter Rumpfschaden (`HullPercent`) aus `BoardStore.Current` oder früheren Spielzuständen fortbestehen und nach dem Laden fälschlicherweise Badges (z. B. 50% DMG) sowie Schadenswerte anzeigen.
- **Lösung**:
  - `BoardStore.Current.Clear()` wird am Anfang von `ApplyGameSave` ausgeführt, um alle veralteten Instanzen vor dem Neuaufbau zu verwerfen.
  - Explizites Zurücksetzen für unbeschädigte Schiffe (`snap.Hull <= 0`): `SetHullDamagePercent(border, 0)` und `UpdateDamageBadge(border, 0)`.
  - `SyncBoardFromTable(logDual: false)` wird am Ende von `ApplyGameSave` aufgerufen, um den `BoardStore` exakt mit dem rekonstruierten Spielstand zu synchronisieren.
  - In `SaveGame`: `Hull` und `RangeLeft` erfassen über `GetHullDamage(b)` und `GetRemainingRange(b, card)` direkt die verbindlichen Instanz-Werte der Schiffe.

---

## 2026-09-13 (Fix & Rule Implementation - Ktarian Game Dilemma Disabling & Cure)

**Engine & Rules (DilemmaRules, TableWindow, BoardStore, CardInstance & Models)**:
- **Glossary & Kartentext-Konformität (Ktarian Game)**:
  - Kartentext: *"Place on ship. Now and start of each turn, one personnel aboard (random selection) is disabled. Cure with CUNNING>30 OR any android."*
  - Bisheriger Bug: Die Start-of-Turn-Logik verwendete fälschlicherweise `MarkStopped`, welches durch den regulären Rundenwechsel (`UnstopAllCards`) direkt zu Beginn der Runde wieder aufgehoben wurde. Zudem fehlte das initiale Deaktivieren einer Person beim Encounter ("Now") sowie ein echter "Disabled"-Zustand.
  - Implementierung von echtem permanentem `Disabled`-Status:
    - `Card.cs`: `Disabled`-Flag und Einbeziehung in `IsLeaveBlocked`.
    - `CardInstance.cs`: `PersonnelInstance.Disabled` und `override bool IsLeaveBlocked => Quarantined || InStasis || Disabled;`.
    - `MovementRules.cs`: Deaktiviertes oder in Stasis befindliches Personal zählt nicht mehr zu Staffing-Requirements (`IsShipStaffed`).
    - `TableWindow.xaml.cs`:
      - Neues `ApplyDisabledVisual` mit amber-orange Glow/Border und reduzierter Opazität.
      - `ApplyKtarianDisable`: Wählt beim Encounter ("Now") und zu jedem Rundenbeginn ("Start of Turn") eine zufällige, noch nicht deaktivierte Person an Bord des Wirtsschiffs aus, markiert sie als `Disabled` und trägt sie in `attached.Held` ein.
      - `ProcessStartOfTurnDilemmas`: Prüft zuerst die Heilung mit un-deaktiviertem Personal (CUNNING>30 oder Android). Falls nicht geheilt, wird eine weitere Person deaktiviert.
      - `ClearStasisForDilemma`: Hebt beim Heilen von `Ktarian Game` den `Disabled`-Status aller betroffenen Personen auf, stellt die Visuals wieder her und leert `Held`.
      - Mission-Versuche und Beamen: Deaktiviertes Personal ist vom Beamen ausgeschlossen und zählt nicht bei Missionsversuchen/Skills.
      - UI Details & Gruppen: Deaktivierte Personen werden in der Detailansicht mit amber Status und unter der "Disabled"-Negativgruppe aufgeführt.
      - Save/Load (`GameSave.cs`): `AttachedDilemmaSnap.HeldIds` speichert die betroffenen Karten-IDs, sodass der `Disabled`-Zustand auch nach Speichern und Laden exakt erhalten bleibt.
  - Unit-Tests:
    - `DilemmaRules.VerifyKtarianGame`: Erweiterte Tests bezüglich CUNNING>30, Android, Nicht-Zählen von Held/Disabled-Personal bei Cure-Checks und `IsLeaveBlocked`-Verhalten.
    - `DilemmaCureRules.VerifyDilemmaCureRules`: Zusätzliche Tests für Ktarian Game Heilung mit CUNNING>30, Android und Fehlschlag bei CUNNING<=30.

---

## 2026-09-13 (Fix & Rule Implementation - Portal Guard & Quarantine Interaction)

**Engine & Rules (DilemmaRules, TableWindow, BoardStore & Models)**:
- **Glossary & Kartentext-Konformität (Portal Guard & Hyper-Aging Quarantäne)**:
  - Kartentext: *"Unless one Away Team member has CUNNING>7 or Honor, immediately beam entire Away Team off planet surface OR kills entire Away Team."*
  - DRG: Wenn die Bedingung nicht erfüllt ist, muss das gesamte Away Team sofort vom Planeten gebeamt werden. Ist das Beamen erfolgreich, wird das Away Team gestoppt und das Dilemma verbleibt unter der Mission (`WallFailed`). Kann jedoch auch nur ein einziges Mitglied des Away Teams nicht beamen (z. B. wegen Quarantäne durch Hyper-Aging oder Stasis) oder existiert kein Schiff oder Facility vor Ort zum Hinbeamen, wird das **gesamte Away Team getötet**!
- **Zentrale Kapselung von Quarantäne & Stasis**:
  - `Card.cs`: Laufzeit-Properties `Quarantined`, `InStasis` und `IsLeaveBlocked`.
  - `CardInstance.cs`: `PersonnelInstance` besitzt `Quarantined`, `InStasis` und `override bool IsLeaveBlocked => Quarantined || InStasis;`.
  - `Force.cs` (Away Team / Crew): `IsQuarantined`, `IsLeaveBlocked` und `CanBeamAway`.
  - Synchronisation im `BoardStore` bei `ApplyUiStatusToStore` sowie beim Beitritt oder Anheften von Quarantäne-Dilemmas.
- **Dilemma-Regeln & Beam-Verdrahtung**:
  - `DilemmaRules.Ctx`: Übermittlung von `CanBeamOffPlanet` (prüft, ob das Team auf einem Planeten steht, kein Mitglied blockiert ist und ein eigenes Schiff bzw. eine Facility am Ort existiert).
  - `DilemmaRules.DecidePortalGuard`: Berücksichtigt `canBeamOffPlanet`. Führt bei unerfülltem Filter und blockiertem Beamen zu `KillTeam = true` (alle Team-Mitglieder in `r.Kill`) und `BeamBackTeam = false`.
  - `TableWindow.BeamBackAwayTeamToShipOrOutpost`: Verhindert das Beamen, sobald auch nur ein Team-Mitglied `IsCardLeaveBlocked` ist, und liefert einen booleschen Status zurück. Scheitert das Beamen bei Portal Guard, greift der Fallback und das gesamte Team wird verworfen (unter Berücksichtigung von Genetronic Replicator).
  - `VerifyPortalGuard`: Ausführlicher Unit-Test mit Pass-, Fail-mit-Beam- und Fail-ohne-Beam-(Quarantäne/No-Dest)-Szenarien.

---

## 2026-09-13 (Fix - Genetronic Replicator Event & Target Exclusion Rules)

**Engine & Rules (EventRules & TableWindow)**:
- **Glossary & Kartentext-Konformität**: "When a personnel is targeted to die, you may stop 2 MEDICAL present (who are not also targeted to die) to return that personnel to hand instead."
  - Das zu rettende Personal (Victim) und sämtliche weitere gleichzeitig zum Tod ausgewählte Personen (`alsoTargetedToDie`) dürfen nicht für die 2 geforderten MEDICAL-Punkte gezählt oder gestoppt werden (z. B. wenn Beverly Crusher mit 2 MEDICAL getötet wird, kann sie sich nicht selbst retten, sofern nicht mindestens 2 weitere ungestoppte MEDICAL-Fertigkeiten anwesend sind).
  - Bereits gestoppte (`IsBorderStopped`) oder in Stasis befindliche (`IsCardInStasis`) Personen können nicht zum Zahlen der Rettungskosten gestoppt werden.
  - Nur eigenes, ungestopptes Personal am selben Host mit MEDICAL-Fähigkeiten ist qualifiziert.
- **Interaktive Auswahl**:
  - Sind mehr als 2 MEDICAL-Fähigkeiten anwesend, kann der Spieler über `PickBorderFromList` interaktiv wählen, welche medizinischen Fachkräfte gestoppt werden sollen.
  - Automatisches Stoppen, wenn die verfügbaren Kandidaten genau den Anforderungen entsprechen.
- **Regel-Zentralisierung in `EventRules.cs`**:
  - `GetPersonnelMedicalSkill`: Ermittelt effektive MEDICAL-Stufe (inklusive Ausrüstung wie Medical Kit).
  - `IsEligibleForGenetronicStop`: Validiert Berechtigung einzelner Karten unter Ausschluss von Opfern und gestopptem Personal.
  - `GetAvailableGenetronicMedical` & `CanGenetronicSave`: Pure Decide-Logik.
  - `VerifyGenetronicReplicator`: Vollständiger Regel-Unit-Test (Selbstrettungs-Ausschluss von Beverly Crusher, Ausschluss von gleichzeitig Getöteten, gestopptes Personal ignoriert, Fremdrettung mit Crusher/Toby Russell).
- **TableWindow Verdrahtung**:
  - `DiscardPersonnelBorder` akzeptiert jetzt `alsoTargetedToDie`.
  - Weitergabe von `alsoTargetedToDie` bei Dilemma-Kills (`ApplyDilemmaResult`, Crystalline Entity), Personnel Battles (`CompletePersonnelAttack`, Rogue Borg Battles), Artifact Kills (`Stone of Gol`) und EOT Countdown-Kills (`Hyper-Aging Quarantine`).
  - Korrekte Board-Bereinigung (`_stackOnHost`, `_stoppedBorders`, `BoardStore`), Ablage auf die Hand und Aktualisierung der Zonen-/Host-Badges.

---

## 2026-09-13 (UI & Localization - Remove Duplicate 'Artifact verdient' & Full English Translation)

**UI & Cleanup**:
- **Artifact Acquire Reveal Cleanup**: Entfernen des redundanten "Artifact verdient" `ShowCardReveal`-Overlays beim Lösen einer Mission (`ApplyArtifactAcquire`). Es verbleibt ausschließlich die konsistente englische Einzelkarten-Meldung "Artifact acquired" in `ResolveMissionSolve`.
- **Vollständige Lokalisierung auf Englisch**:
  - `TableWindow.xaml` & `TableWindow.xaml.cs`: Sämtliche verbliebenen deutschen Texte, Tooltips, Statusmeldungen, Aktionshinweise, Fehlermeldungen und Dialoge auf Englisch übersetzt (z. B. Response-Badges, Think-Tray-Titel und Hinweise, Seed-Phasenmeldungen, Action-Stack-Status, Scan-Reveals).
  - `DeckBuilderWindow.xaml` & `DeckBuilderWindow.xaml.cs`: Lokalisierung aller Filter, Tab-Header ("Side legacy"), Tooltips und Meldungen auf Englisch.
  - Game Rules (`DilemmaRules`, `InterruptRules`, `MovementRules`, `PlayRules`, `ReportingRules`, `SeedRules`, `TimingRules`, `ModifierRules`, `BattleRules`, `MissionRules`, `TreatyRules`): Übersetzung aller internen und spielerseitigen Fehlermeldungen, Check-Ergebnisse, Action-Stack-Zusammenfassungen und Prompt-Texte (z. B. "Which equipment?").
  - Services & Models (`GameSession`, `DeckService`, `CardDatabase`, `ExpansionCatalog`): Übersetzung der Zugprotokolle (P1/P2 statt S1/S2), Phasenlabels, Datei-Ausnahmemeldungen und Katalog-Fallbacks.

---

## 2026-09-13 (Fix & Rule Refactor - Curable Dilemmas & Team Stop Prevention 7.2.2.3 / 7.2.6)

**Engine & Rules** - Trennung von Bedingung (Condition) und Heilung (Cure) & Stopp-Verhalten:
- **Allgemeine Regel (Compendium 7.2.2.2, 7.2.2.3, 7.2.6 & Glossary)**:
  - Ein Away Team / eine Crew wird durch ein Dilemma nur dann gestoppt, wenn eine Zugangsbedingung ("unless", "to get past", "cannot get past") fehlschlägt, der Kartentext dies explizit befiehlt ("Away Team is stopped"), oder niemand mehr übrig ist.
  - Eine Heilungsanforderung ("Cure with...") ist ausdrücklich **keine** Zugangsbedingung. Weder das Heilen noch das Nicht-Heilen einer heilbaren Dilemma-Wirkung ohne Vorbedingung führt zum Abbruch der Mission oder zum Stoppen des restlichen Teams ("Failing to immediately meet a cure requirement does not cause mission failure").
- **Alien Abduction (PR 10 U)**:
  - Bei Begegnung: Ziel mit höchstem CUNNING wird in Stasis gesetzt (kann eigene Fähigkeiten nicht zur Heilung beitragen).
  - Wenn verbleibendes Away Team 3x Leadership hat: Sofort geheilt (`Fate.Overcome, StopTeam = false`), Dilemma abgeworfen, Versuch läuft mit vollem Team weiter.
  - Wenn verbleibendes Away Team keine 3x Leadership hat: Dilemma wird an die Mission angehängt (`Fate.AttachAndContinue, StopTeam = false`), Opfer bleibt in Stasis. Das restliche ungestoppte Team setzt den Missionsversuch nahtlos fort!
  - Bei Befreiung (Mission gelöst oder spätere Heilung): `ClearStasisForDilemma` ruft `UnstopBorder` auf, sodass die Person vollständig ungestoppt wieder zum Team stößt.
- **Konsistente Anwendung auf weitere Curable Dilemmas**:
  - `Two-Dimensional Creatures`: Verwendet nun `AttachContinue` (`StopTeam = false`). Schiff kann sich nicht bewegen, aber Crew ist nicht gestoppt und Missionsversuch läuft weiter.
  - `Tsiolkovsky Infection`: Verwendet nun `AttachContinue` (`StopTeam = false`). Personal verliert erste Fertigkeit, ist aber nicht gestoppt und Versuch läuft weiter.
  - `Frame of Mind`: Verwendet nun `AttachContinue` (`StopTeam = false`) mit Sofort-Heilung bei 3 Empathy im verbleibenden Team.
  - `Quantum Singularity Lifeforms` & `Rascals`: Auf `AttachContinue` umgestellt.
  - `TryCureAttachedDilemmas` & `ProcessEndOfTurnDilemmas`: Schließen bei `a.Held.Count > 0` alle in Stasis gehaltenen Karten für Heilungs-Checks aus (Opfer können sich nicht selbst heilen).
- **Automatisierte Regeltests**:
  - Neue Verifikationsmethoden: `VerifyAlienAbduction()`, `VerifyTwoDimensionalCreatures()`, `VerifyTsiolkovskyInfection()` in `DilemmaRules.cs`.
  - Erweiterung von `VerifyDilemmaCureRules()` in `DilemmaCureRules.cs` um Blockade von Selbstheilung aus der Stasis und Heilungstests für TwoDim und Tsiolkovsky.

---

## 2026-09-13 (Retest Green - Archer, Alien Abduction, Phased Matter)

**Retest (Pepsch green):**
- **Archer (PR 14 C)**: Auswertung der höchsten Gesamtattribute, Tie-Break-Wahl durch den Gegner und Stop-Verhalten bei Nichterfüllung verifiziert und bestätigt.
- **Alien Abduction (PR 10 U)**: Stasis-Handling und zentrales Cure-System (7.2.2.3) via 3 Leadership präsent oder Mission Completed verifiziert und bestätigt.
- **Phased Matter (PR 42 C)**: Aufteilung des Away Teams, Stasis/Phasing der größeren Gruppe, Fortführung der kleineren Gruppe und Entphasen/Heilen durch unphased ENGINEER + SCIENCE am Ort bestätigt.

---

## 2026-09-12 (Feat - Centralized Dilemma Cure System according to Rulebook 7.2.2.3)

**Engine** - Dilemma Cure System (Compendium 7.2.2.3):
- **Decide in Rules (`DilemmaCureRules`)**: Reine Regel-Engine für Dilemma-Heilung (`DecideCure` / `CanCure` / `VerifyDilemmaCureRules`). Trennung von Bedingung und Heilung: Zuerst werden die Bedingungen des Dilemmas ausgewertet/angehängt, danach wird der Cure-Check durchgeführt (anwendbar auf Alien Abduction, Menthar Booby Trap, Hyper-Aging, REM Fatigue, Nitrium Metal Parasites, Tsiolkovsky Infection, Two-Dimensional Creatures, Ktarian Game, Birth of "Junior", Frame of Mind).
- **Zentraler Apply in `TableWindow`**: `TryCureAbductionsPresent` und fragmentierte Cure-Prüfungen wurden durch die zentrale Routine `TryCureAttachedDilemmas` ersetzt. Aufgerufen direkt nach Attachment in `ApplyDilemmaResult`, beim Lösen einer Mission in `ApplyMissionSolved` (für Heilen durch Mission Completed), sowie bei Crew-Änderungen (`AddCardToHostStack`, `BeamCardsToHostStack`) und Unstop zu Zugbeginn.
- **Dilemma-Resolution Angleichung**: `DilemmaRules` für Menthar, Tsiolkovsky, Two-Dimensional Creatures und REM Fatigue nutzen `DilemmaCureRules.CanCure` konsistent.

---

## 2026-09-12 (Feat - Silent Response Window & Think Tray UX)

**UX / Hotseat Rules** - Response Window Umbau:
- **Weg vom modalen Popup (`CardRevealOverlay`)**: Keine blockierenden modalen Vollbild-Dialoge mehr bei normalen Card Plays / Reaktionen.
- **Stilles Window mit Banner-Hinweis**:
  - Kurzes Standardzeitfenster (Default: 3s; konfigurierbar im Options-Menü auf 2s / 3s / 5s; Presets für Hotseat Standard 3s/10s und Test schnell 2s/10s).
  - Wenn keine legale Response existiert: Sofortiges Schließen / Auto-Pass, der aktive Spieler kann ohne Verzögerung weiterspielen.
  - Wenn legale Response existiert: Dezenter violettes Badge am Phase-Banner (`ActivePlayerBanner`): `⚡ Response möglich (P1/P2) · 3s` inkl. Hotkey-Hinweis `· [R] Details  [Space] Pass`.
- **Think-Modus (Opt-in via [R] oder Klick auf Banner/Badge)**:
  - Verlängert das Window auf 10s Countdown.
  - Zeigt horizontal scrollbares `ThinkTray` über der Hand des Responders (P1 unten, P2 oben).
  - Volle Handkartengröße mit Herkunfts-Badge (`HAND`, `TABLE`, etc.) und Kartendetails.
  - Klick auf Karte führt Response sofort aus; [Space] oder Timeout führt Pass aus.
- **Priority & Mandatory**:
  - Optionale Responses: Zuerst nicht-aktiver Spieler, danach aktiver Spieler. Gewählte Response erzeugt neue Aktion auf dem Stack und neues Window für den Gegner.
  - Mandatory / required Responses: Kein Pass per Timeout, Space-Pass deaktiviert, Fenster bleibt bis Karte gewählt wurde.

---

## 2026-09-12 (Fix - Artifact Beaming without Treaty & Retest Green: Hyper-Aging, Firestorm, Detail Groups)

**Engine** - Artifact Beaming / Affiliation-Free: Artifacts (inkl. Varon-T Disruptor, Interphase Generator, Data's Head etc.) haben keine Affiliation-Sperre und benötigen keinen Treaty, um auf Schiffe/Facilities gebeamt oder dort platziert zu werden (analog zu Equipment). Decide: `TreatyRules.CanOccupyHost` / `CardsCompatibleUnderTreaties` / `ForceCompatible` erlauben Artifacts affiliationsfrei; `ReportingRules.AreCompatible` / `CheckReportRules` erweitert; `ModifierRules.IsEquipmentCard` um Data's Head ergänzt. Apply: Detailansicht `FillDetailStackSection` gruppiert Artifacts unter Equipment/Artifacts statt Personnel; Fehlermeldung bei Beam aktualisiert ("Equipment and Artifacts are unrestricted").

**Retest (Pepsch green):**
- **Hyper-Aging**: Quarantäne auf Planet, Beam-Block für Quarantänisierte bestätigt.
- **Firestorm**: INT<5 Kills und Versuch-Fortsetzung bestätigt.
- **Dilemma-Continue Overlay**: Platzhalter-Header `EFFECT - attempt continues` (statt irreführendem `RELOCATED`) für Firestorm und nicht-relocate Dilemmas bestätigt (Love Interest bleibt `RELOCATED`).
- **Detailansicht Debuff-Gruppierung**: Gruppierung von Stopped / Quarantined / Stasis mit Sammel-Header `DetailStatusRules.FormatEffectGroupHeader` ohne redundante Per-Card-Labels bestätigt.
- **Varon-T Disruptor**: Looten auf Planet und STRENGTH ×2 für eigenes Personal bestätigt.

---

**Engine** - Hyper-Aging (PR 28 U): AT quarantined on place (AttachAndContinue, not stopped); no Leave/Beam away; anyone who joins the host is quarantined; cure SCIENCE + 2 MEDICAL before countdown 0 else Kill (inorganics exempt, existing). Status UX like Stasis leave-block. `LegalMoves` skips Beam from `QuarantineLeaveBlocked` hosts. Decide: `DilemmaRules.IsQuarantinePersist` / `IsLeaveBlockedPersist` + `VerifyHyperAgingQuarantine`. Apply: TW Held on attach, `IsCardLeaveBlocked` / `TryJoinQuarantineOnHost`, BoardPiece `QuarantineLeaveBlocked`. RemFatigue quarantine PARK (out of scope).

---



## 2026-09-06 (Feat - Portal Guard Premiere)

**Engine** - Portal Guard (PR 43 U): Planet - Unless one Away Team member has CUNNING>7 OR Honor: WallFailed (dilemma stays under mission) + Away Team beams up (BeamBackTeam) + stopped; else Overcome + Continue (discard). Spock #25 Soll / DRG Portal Guard. Decide: `DilemmaRules.DecidePortalGuard` + `VerifyPortalGuard`. Boundary CUNNING==7 fails. PARK: kill if beam impossible/partially blocked; Borg abort edges.

---

## 2026-09-06 (Feat - Phased Matter Premiere)

**Engine** - Phased Matter (PR 42 C): Planet - Owner divides Away Team; larger group phased/stasis (tie: owner picks which is larger; Solo=1+0). Smaller AT Continue (AttachAndContinue, not stopped). Cure: ENGINEER + SCIENCE from another unphased AT at planet (Held/phased do not count via ExcludeHeld/CanCurePhased). Spock #24 Soll / DRG + Glossary Errata Phased Matter. Decide: `DilemmaRules.Phased` + `VerifyPhasedMatter`. PARK: deep phasing LegalMoves (Sheliak etc.); Beam/leave already gated via IsCardInStasis.

---
## 2026-09-06 (Feat - Null Space Premiere)

**Engine** - Null Space (PR 41 U): Space - Unless 2 Navigation present: Ship damaged (DamageShip / ApplyHullDamage +50 / Rotation badge) + Ship/Crew stopped (EffectAndEnd+StopTeam); else Overcome +5 Bonus-Area + Continue. Always discard dilemma. Boundary: 1 Navigation fails. Spock #23 Soll / DRG Null Space. Decide: `DilemmaRules.NullSpace` + `VerifyNullSpace`.

---
## 2026-09-06 (Feat - Nausicaans Premiere)

**Engine** - Nausicaans (PR 39 U): Planet - Unless STRENGTH>44: kills one Away Team member (random selection) + AT stopped (EffectAndEnd+StopTeam); else Overcome + Continue. Always discard dilemma. Boundary STRENGTH==44 fails. Spock #22 Soll / DRG Nausicaans. Decide: `DilemmaRules.Nausicaans` + `VerifyNausicaans`. PARK: Interphase Generator / Zon nullify not wired.

---

## 2026-09-06 (Feat - Nanites Premiere)

**Engine** - Nanites (PR 38 U): Space - Unless 2 SCIENCE OR Diplomacy present: Ship damaged (DamageShip / ApplyHullDamage +50 / Rotation badge) + Ship/Crew stopped (EffectAndEnd+StopTeam); else Overcome +5 Bonus-Area + Continue. Always discard dilemma. Card+DRG Decide: `DilemmaRules.Nanites` + `VerifyNanites`.

---

## 2026-09-06 (Feat - Nagilum Premiere)

**Engine** - Nagilum (PR 37 R): Space - Unless 3 Diplomacy OR STRENGTH>40 present: kills half of crew (random, round down); else Overcome +5 Bonus-Area + Continue. Always discard dilemma. Fail -> EffectAndEnd+StopTeam. Spock #20 Soll / DRG Nagilum. Decide: `DilemmaRules.Nagilum` + `VerifyNagilum`. Boundary STRENGTH==40 fails; 1 crew -> 0 kills. Combo Anaphasic&Nagilum=EP ignore.

---

## 2026-09-06 (Feat - Microvirus Premiere)

**Engine** - Microvirus (PR 36 C): Planet - Unless MEDICAL AND SECURITY present: opponent kills one Away Team member except inorganic (Android/Exocomp/Holo via IsInorganic); else Overcome +5 Bonus-Area + Continue. Always discard dilemma. Fail -> EffectAndEnd+StopTeam. Spock #19 Soll / DRG Microvirus. Decide: `DilemmaRules.Microvirus` + `VerifyMicrovirus`. PARK: opponent-choice UI filter thin (engine pool excludes inorganic).

---
## 2026-09-06 (Feat - Microbiotic Colony Premiere)

**Engine** - Microbiotic Colony (PR 35 C): Space - Unless SCIENCE AND ENGINEER AND OFFICER present: Ship damaged (DamageShip / ApplyHullDamage +50 / Rotation badge) + Ship/Crew stopped (EffectAndEnd+StopTeam); else Overcome Continue. Dilemma always discarded. No bonus points. Spock #18 Soll / DRG Microbiotic Colony. Decide: DilemmaRules.MicrobioticColony + VerifyMicrobioticColony.

---## 2026-09-06 (Feat - Menthar Booby Trap Premiere)

**Engine** - Menthar Booby Trap (PR 34 C): Space - ALWAYS place on ship (Persist Menthar; no Move until Cure). MEDICAL missing -> 1 crew random kill + AttachAndEnd + StopTeam; MEDICAL present -> no kill + AttachAndContinue (still placed). Cure/discard: 2 ENGINEER. Spock #17 Soll / DRG + Glossary Errata Menthar Booby Trap. Decide: `DilemmaRules.Menthar` + `VerifyMentharBoobyTrap`. PARK: LegalMoves-level move-block beyond existing TW Menthar/TwoDim gate if thin.

---
## 2026-09-06 (Feat - Matriarchal Society Premiere)

**Engine** - Matriarchal Society (PR 33 U): Planet - Cannot get past unless at least two female Away Team members are present. Pass >=2 Female (Characteristics) -> Overcome Continue (dilemma discard); Fail -> WallFailed+StopTeam (dilemma stays under Mission). No kills / score / damage. Spock #16 Soll / DRG Matriarchal Society. Decide: DilemmaRules.MatriarchalSociety + VerifyMatriarchalSociety. PARK: gender-related Borg immediate discard (no Borg edge).

---
## 2026-09-06 (Feat - Ktarian Game Premiere)

**Engine** - Ktarian Game (PR 31 R): Space - Place on ship. Cure CUNNING>30 OR any android -> Overcome Continue (discard). Else AttachAndContinue + StopTeam=false (crew not stopped). PersistKind.Ktarian. Spock #15 Soll / DRG Ktarian Game / Major Rakal. Decide: `DilemmaRules.KtarianGame` + `VerifyKtarianGame`. PARK: Lefler nullify (QC); Now+SOT random Disable Apply (no Disable list / no SOT tick).

---
## 2026-09-06 (Feat - Impassable Door Premiere)

**Engine** - Impassable Door (PR 30 C): Planet   To get past requires Computer Skill. Pass -> Overcome Continue (dilemma discard); Fail -> `WallFailed`+`StopTeam` (dilemma stays). No kills / score / damage. Spock #14 Soll / DRG Impassable Door. Decide: `DilemmaRules.ImpassableDoor` + `VerifyImpassableDoor`.

---
## 2026-09-06 (Feat - Iconian Computer Weapon Premiere)

**Engine** - Iconian Computer Weapon (PR 29 C): Space — Unless SCIENCE present: Ship+Crew stopped (`EffectAndEnd`+`StopTeam`); reveal hand, discard ALL non-personnel (personnel stay); draw equal number from draw deck (`DrawForDiscarded` / Apply `DiscardNonPersonnelFromHand`+`DrawOneToHand`); else Overcome Continue. Always discard dilemma. No bonus points. Spock #13 Soll / DRG Iconian Computer Weapon (standalone). Decide: `DilemmaRules.IconianComputerWeapon` + `VerifyIconianComputerWeapon`.

---

## 2026-09-06 (Feat - Gravitic Mine Premiere)

**Engine** - Gravitic Mine (PR 26 U): Space — Unless SCIENCE AND Navigation present: DamageShip (ApplyHullDamage +50 / Rotation badge) + Ship+Crew stopped (`EffectAndEnd`+`StopTeam`); else Overcome Continue. Always discard. No bonus points. Spock #12 Soll / DRG Gravitic Mine. Decide: `DilemmaRules.GraviticMine` + `VerifyGraviticMine`.

---
## 2026-09-06 (Feat - Firestorm Premiere)

**Engine** - Firestorm (PR 25 U): Planet — no Condition-Wall. Personnel with INT<5 after Enhancements (`Eff`) die; Rest Continue; dilemma discard (`EffectAndContinue`). Boundary INT==5 survives. Thermal Deflectors in play -> nullify/discard + Continue (`Overcome`). Spock #11 Soll / DRG Firestorm (TD/ETA != Conditions). Decide: `DilemmaRules.Firestorm` + `VerifyFirestorm`. PARK: ETA-Escape Response timing (UI thin).

---
## 2026-09-06 (Feat - El-Adrel Creature Premiere)

**Engine** - El-Adrel Creature (PR 23 U): Planet — Targets two strongest AT (Tie = Dilemma-Owner / `PickOpp`). Pass combined STR >16 → Overcome Continue + discard (no points). Fail → 1 of the two random killed; rest of AT stopped; discard (`EffectAndEnd`+`StopTeam`). Boundary STR==16 fails. Spock #10 Soll / DRG El-Adrel Creature. Decide: `DilemmaRules.ElAdrel` + `VerifyElAdrelCreature`.

---
## 2026-09-06 (Feat - Cytherians Premiere)

**Engine** - Cytherians (PR 22 R): Space — Place on ship; Attempt ends; Crew **NOT** stopped (`AttachAndEnd` + `StopTeam=false`). Far end fixed once (TW `Dest` / `RequiredMoveRules.FarEndIndex` 12.6). Arrival → discard +15; ship destroy → discard (no points). No instant relocate. Spock #9 Soll / Glossary Cytherians + actions-required. Decide: `DilemmaRules.Cytherians` + `VerifyCytherians`. PARK: full LegalMoves-only-toward-far-end beyond existing `ShipHasRequiredMove` gates; Borg play-out no-points / Mission Debriefing if unclear.

---
## 2026-09-06 (Feat - Crystalline Entity Premiere)

**Engine** - Crystalline Entity (PR 21 R): Dual [S/P]. Planet - SCIENCE+MEDICAL -> Overcome +5 Continue; else entire AT killed. Space - Music OR SHIELDS>6 -> Overcome +5 Continue; else ALL life aboard dies (Stopped/Disabled/Intruder; NOT Stasis) via `KillAllLifeAboardExceptStasis` (Apply beyond encounter crew); Ship stopped; does **not** destroy ship. Always discard. Spock #8 Soll/DRG/Glossary. PARK: Lore-Double. Decide: `DilemmaRules.Crystalline` + `VerifyCrystallineEntity`.

---
## 2026-09-06 (Feat - Cosmic String Fragment Premiere)

**Engine** - Cosmic String Fragment (PR 20 U): Space — Unless Astrophysics OR ENGINEER OR Navigation present: destroy ship (everything aboard via Apply); else Overcome +5 Bonus-Area + Continue. Always discard dilemma. Fail -> EffectAndEnd+StopTeam+DestroyShip. Spock #7 Soll/DRG. Decide: `DilemmaRules.CosmicStringFragment` + `VerifyCosmicStringFragment`.

---
## 2026-09-06 (Feat - Chalnoth Premiere)

**Engine** - Chalnoth (PR 19 U): Planet — Unless 3 SECURITY OR STRENGTH>40 present: opponent kills one Away Team member; else Overcome +5. Always discard dilemma. Fail -> EffectAndEnd+StopTeam. Spock #6 Soll/DRG (Points 5 Bonus-Area). Decide: `DilemmaRules.Chalnoth` + `VerifyChalnoth`.

---
## 2026-09-06 (Feat - Birth of "Junior" Premiere)

**Engine** - Birth of "Junior" (PR 17 U): Space — place on ship. Encounter: 3 ENGINEER → nullify Overcome (discard+Continue); else AttachAndContinue (crew not stopped; countdown 0, RANGE −1 only on your EOTs). Destroy when RANGE after countdown ≤0 via `EndOfTurnRestRules.JuniorDestroysShip`. Later cure 3 ENGINEER → discard (RANGE restores via host recalc). Spock/DRG/Glossary. Decide: `DilemmaRules.BirthOfJunior` + `VerifyBirthOfJunior`. Pup-disable ≠ 0 RANGE: PARK (no guess).

---
## 2026-09-06 (Feat - Armus: Skin Of Evil Premiere)

**Engine** - Armus: Skin Of Evil (PR 15 R): kills one Away Team member (random selection); dilemma discarded; survivors continue (EffectAndContinue, no StopTeam / not under mission). Spock/DRG Soll. Decide: `DilemmaRules.ArmusSkinOfEvil` + `VerifyArmusSkinOfEvil`.

---

## 2026-09-06 (Fix - Anaphasic Organism discard not kill)

**Engine** - Anaphasic Organism fail: selected female **resigns = discard**, not killed (DRG: discarded female is not killed). `Result.Discard` + TW Apply/Format; Genetronic does not save. Pass MED+SEC / no-female / StopTeam / dilemma discard / opp-tie unchanged. Decide: `DilemmaRules.Anaphasic` + `VerifyAnaphasicOrganism`.

---
## 2026-09-06 (Feat - Ancient Computer Premiere)

**Engine** - Ancient Computer (PR 13 R): Wall — pass 2 Computer Skill OR 3 SCIENCE OR 3 ENGINEER -> Overcome (discard+continue). Fail -> WallFailed + StopTeam (dilemma stays under mission). Matches printed Premiere text. Decide: `DilemmaRules.AncientComputer` + `VerifyAncientComputer`.

---
## 2026-09-06 (Feat - Anaphasic Organism Premiere)

**Engine** - Anaphasic Organism (PR 12 C): Pass MEDICAL+SECURITY -> Overcome (discard+continue). No female present (requires Female) -> Overcome no-effect. Fail -> discard female with highest total attributes (opp choose on tie, Archer parity), EffectAndEnd+StopTeam; dilemma always discarded. Decide: `DilemmaRules.Anaphasic` + `VerifyAnaphasicOrganism`. Hotseat Alien Parasites untouched.

---
## 2026-09-06 (Feat - Alien Parasites #1a Pass/Fail + Beam-back)

**Engine** - Alien Parasites #1a (Spock Soll): Pass INTEGRITY>32 → Overcome (discard + continue). Fail → WallFailed (dilemma stays under mission), StopTeam; planet Beam-back AT to ship/outpost then stop; Space stops crew+ship. No opponent control / hotseat / next-turn timer (PARK). Decide: `DilemmaRules.DecideAlienParasites` + `VerifyAlienParasites1a`; Apply: TW `BeamBackAwayTeamToShipOrOutpost`.

---
## 2026-09-06 (Fix - Cloak opacity more transparent ~0.45)

**UX** - Cloak Opacity 0.45 (was 0.7); Decloak 1.0; Stopped alone 0.55; stopped+cloaked uses 0.45 (cloak preferred).

---
## 2026-09-06 (Fix - Detail pane dupes + Cloak 0.7 + Stopped Negative + Archer tie)

**UX** - Card detail: colored StatusBlock keeps Buff/Timer/Debuff once; ship path no longer dumps Events/Dilemmas into DetailIcons under staffing; Contents skips repeating RANGE/WEAPONS/SHIELDS + Modifiers. Stopped shows once as red Status line and explicit "Stopped" text under Negative. Cloak: Opacity 0.7 only (nebula overlay + black glow removed); decloak 1.0 / stopped stays 0.55.

**Engine** - After walk/drag onto host: SyncBoardFromTable + TryCureAbductionsPresent (beam parity) so mission/dilemma skills see present crew immediately. Archer: HighestAttr ties → opponent choice (enhancements via Eff unchanged); fail still StopTeam + EffectAndEnd.

---
## 2026-09-06 (Fix - G7 Counter-Attack next turn)

**Engine/UX** - After an opponent ship battle, on **your next turn** you may initiate Counter-Attack(s) at the **same location** vs involved/still-there opponent cards. Relaxations for Counter-Attack only: **no Leader**, **no affiliation restriction** (Fed may hit back). Still required: WEAPONS>0, Matching Affiliation aboard, undocked, uncloaked, unstopped, same location. `BattleRules.CanInitiateShipAttack(..., counterAttack:)`; TW `PendingCounterAttack` arms on turn change, expires after that turn. Return Fire in the current battle is not Counter-Attack.

---
## 2026-09-06 (Fix - G4 BoardStore staffing aligned with UI Treaties)

**Engine** - One staffing truth: `BoardStore.ToBoardPieces` calls `MovementRules.IsShipStaffed(+Treaties)` (G2 Match / G3 empty-icon) and folds Rogue+Lore via `GameStateSeed.LoreStaffedShipIds`. `OverlayStatus` uses **store Staffed only** (no `ui || store` drift). `EngineAuthority` Fly rechecks `IsShipStaffed` with `state.TreatiesOf`; lore still allowed when store marked Staffed. TW seeds lore ship InstanceIds.

---
## 2026-09-06 (Fix - G3 empty-icon ship needs crew)

**Engine** - `MovementRules.IsShipStaffed`: ships with no staffing icons are no longer Ok with zero crew. Require >=1 matching-affiliation personnel aboard (Treaty/NA != Match per G2). Deny Fly with empty crew. Text-staffing sandbox path also requires Match when crew present.

---
## 2026-09-06 (Fix - G6 Return Fire Matching HARD)

**Engine** - Return Fire (Spock Soll): Matching Affiliation HARD on **defender** ship (`HasMatchingAffiliation`; Treaty/NA != Match). **No Leader** required. Needs WEAPONS>0, undocked, uncloaked. `BattleRules.CanReturnFire` + `AskReturnFireAndResolve` deny RF without Match; `ResolveShipBattle` re-checks Match/dock/cloak before RF damage. Rogue/loreStaffed bypass. Clear error.

---
## 2026-09-06 (Fix - G5/E2b store staffing Treaty+Rogue)

**Engine** - E2b: store-first Capture staffing. BoardStore.ToBoardPieces passes owner treaties into IsShipStaffed (Fly-aligned; Match still ignores Treaty/NA per G2). CaptureEngineState storeHasHost ships fill UI Staffed via treaties + ShipStaffedByRogueBorg so OverlayStatus OR picks up Rogue/Lore. Closes false deny when Occupant exists but only Rogue staffs.

---
## 2026-09-06 (Fix - Stopped no-beam + Abduction Cure OR present)

**Engine/UX** - Stopped personnel excluded from beam pool (IsBeamableFromHost / BeginBeam preselect / checkboxes / toMove). Unstopped may beam; stopped stay behind; clear error if none beamable. Detail Status: stopped personnel under Negative/red (like stasis). Alien Abduction: CanCure remains Leadership x3 OR mission completed; present path via CollectPresentAtMissionForCure + TryCureAbductionsPresent (EOT, unstop, after beam); MarkMissionSolved cure unchanged. Stasis leave-block unchanged.

---
## 2026-09-06 (Fix - G1 Battle Matching HARD)

**Engine** - Ship Battle initiate: `HasMatchingAffiliation` is HARD (deny if missing). Leader+WEAPONS alone not enough. Full staffing icons (Cmd/Stf) not required for Open Fire. Reuses G2 Match rule (Treaty/NA != Match). Wired in `BattleRules.CanInitiateShipAttack` + `BeginAttackMode` (Rogue-Borg loreStaffed bypass). Dead unused `staff.Ok` soft-block removed.

---
## 2026-09-06 (Fix - Cloak card ~50% opacity)

**UX** - Cloaked ships: `ApplyCloakVisual` sets card `Opacity = 0.55` (~50%) on cloak and restores on decloak (keeps 0.55 if still stopped). `ApplyStoppedVisual` unstop keeps cloak opacity. Nebula overlay/border unchanged. (`Opacity=0.55` near stopped path was stopped-only, not cloak.)

---
## 2026-09-06 (Fix - G2 Treaty≠Matching Affiliation Fly)

**Engine** - `MovementRules.HasMatchingAffiliation`: Matching Affiliation für Staffing = echte gemeinsame Affiliation mit dem Schiff. Treaty/NA-Kompatibilität zählt **nicht** als Match (Spock G2). Treaty-/NA-Personal darf weiterhin nur Staffing-Icons (Cmd/Stf) füllen, sobald Matching-Affiliation an Bord ist. Fly-Pfad (`IsShipStaffed` / `CanMoveShip`).

---
## 2026-09-06 (Fix - Neural Servo Seiten-Sync)

**UX** - Neural Servo Device: nach gültigem Play/Resolve (und EOT-Restore) Schiff sofort auf Controller-Seite neu legen (P1 unter / P2 über Mission) via `RelayoutDockablesUnderMission` — nicht erst nach Fly/Move. Helper `SyncDockableSideAfterOwnerChange` (wie Lore Returns).

---
## 2026-09-06 (Fix - Love Interest + Stasis/Abduction + Cloak nebula)

**Engine** - Female's/Male's Love Interest: Fate.EffectAndContinue - relocate matching gender to furthest other planet; victim leaves AT (not stopped); rest continue; dilemma discarded. TW failed excludes EffectAndContinue.

**Engine** - Stasis: cannot beam/drag-leave while IsCardInStasis. Alien Abduction cure = OR (3 Leadership present OR mission completed) - release Held + clear stasis; discard on mission solve.

**UX** - Cloaked ships: black fog/nebula overlay on ship art (plus existing black border/glow).

---
## 2026-09-06 (Fix - Status-UX retest + Nitrium/Hyper-Aging continue)

**UX** - Damaged ships: red DMG badge only (no 180 flip). Outpost repair timer: `1 left` = clears end of **this** turn. Detail crew rows: Positive (green) / Negative (red) / Personnel / Equipment; last-dilemma mini removed (mission action `Show last revealed card under mission`). Stasis/Negativ glow **red**; Cloaked **black**; Buff green; Timer amber.

**Engine** - `Fate.AttachAndContinue`: Nitrium (countdown **2**, cure 2 SCI|2 ENG) + Hyper-Aging (countdown **3**, cure SCI+MED×2) place without stop/fail; attempt continues. Encounter cure → Overcome (Hyper-Aging +5). RemFatigue unchanged. Menthar/Abduction etc. stay AttachAndEnd.

---
## 2026-09-05 (Fix - Outpost repair leave-reset + Status UX)

**Engine/UX** - Outpost repair progress only while **docked** at own repair facility; undock/fly clears counter immediately (Spock Soll). Card detail Status-Block: Green Buff / Red Debuff / Amber Timer; repair amber EN line; stasis cyan/violet glow + `In stasis` / `placed in stasis`; mission Held/Stasis (dilemma + personnel). Crew-minis same badge colors. Bewusst nicht: UX EN Timer nick, Persist/Battle extract, Premiere waves. [C]

---

## 2026-09-05 (Extract Slice 9 - EOT-rest decide gates)

**Engine** - `EndOfTurnRestRules` decide gates: outpost repair 2-turn, Rogue Borg invade (+IFF), Edo continue -10, SOT Crosis/NextTurn discard, dilemma cure award / Junior destroy / countdown expired. TableWindow keeps Apply (repair/battle/discard/UI). Borg Ship EOT battle stays in TW. Next: Persist branches, then Battle-Decide. [C - Grundlage]

---

## 2026-09-05 (Fix - Borg Ship stack + no RF after destroy)

**Engine** - Borg Ship token stacks in next free slot under mission (not same Y as ships). Return Fire prompt skipped when Open Fire would destroy defender. [C]

---

## 2026-09-05 (Fix - Borg Ship EOT battle + Hugh window)

**Engine** - EOT Borg Ship: queue InitiateShipBattle (cloaked skipped); Hugh response cancels rest of turn; Resolve WEAPONS 24 + docked half facility shields; BeginNext after cancel/resolve. [C]

---

## 2026-09-05 (Fix - Borg Ship placement / attack gate / EOT damage)

**Engine** - Borg Ship token sits like a ship under mission (not overlapping). Attack Borg Ship button only same location. EOT attack uses `BattleRules.ResolveFire` (Hit W>S / Direct Hit W>2xS + docked facility half-shields). Hugh CanRespond Dilemma-only (prior). EOT response-window battle path still open (await Spock 6-8). [C]

---

## 2026-09-05 (Fix - Hugh CanRespond Borg Ship Dilemma only)

**Engine** - Hugh valid response / battle-cancel only when **Borg Ship Dilemma** initiates battle (`IsHughBattleSource` => `IsBorgShipDilemma`; CanRespond no longer [Bor]/Rogue). Rogue Borg remains own-action location kill. [C]

---

## 2026-09-05 (Docs - spawn + CODE_PLACEMENT)

**Docs** - HANDOFF spawn-ready tip `bb163ed`; `CODE_PLACEMENT.md` (Decide=Rules / Apply=TW / Board); PROJECT pointers. Gaps nullify Pepsch green. [C]

---
## 2026-09-05 (Fix - Gaps nullify relocate)

**Engine** - Kevin/nullify Gaps: before span remove, discard cards on Gaps event; nullifier chooses one of two adjacent locations; relocate ships/facilities there (`GapsNullifyRules` + RelocateOccupantsAfterGapsNullify). No hanging between missions. [C]

---
## 2026-09-05 (Extract Slice 8 - EndOfTurn attached-event decide)

**Engine** - `EndOfTurnEventRules` decide gates for ProcessEndOfTurnEvents (Transwarp discard, Distortion flip, Plasma Fire, Warp Core, Static Warp, Traveler draw, Kidnappers, NeuralServo, Anti-Time). `EventRules.IsGapsInNormalSpace` / `IsQNet`. TableWindow keeps damage/destroy/UI/pick. [C - Grundlage]

---
## 2026-09-05 (Fix - Tachyon gate + Transwarp drop host)

**Engine** - Tachyon Detection Grid: gate >=4 Controller ships in play (cloaked count; not "four exposed"); target cloaked only (Phased != cloaked); force-decloak + cloak-lock rest of turn via TurnExpiry. Transwarp Conduit: RANGE x2 on drop/stack host, no ship picker. [C]

---
## 2026-09-05 (Extract Slice 7 - Sanctuary/Distortion/Tachyon decide gates)

**Engine** - Premiere interrupt ship-effect gates in InterruptShipEffectRules (SanctuaryDeny / DistortionDeny / TachyonDeny / TranswarpDeny); NameIs helpers on InterruptRules. TableWindow keeps AskPlayer / attach / TurnExpiry / cloak / status. [C - Grundlage]

---
## 2026-09-05 (Extract Slice 6 - Dilemma/Artifact Apply gates)

**Engine** - `DilemmaRules` apply gates (seed-remove, attach-host preference, stasis, score, TCL/Edo/Conundrum); `ArtifactRules.DecideAcquirePlacement` / `ShouldDownloadOnAcquire`. TableWindow keeps kills/attach/UI/side effects. [C - Grundlage]

---
## 2026-09-05 (Extract Slice 5 - InstantEvent/NamedAu decide gates)

**Engine** - Instant draw/Masaka/Res-Q plan in `InstantEventRules.Decide`; named AU interrupt routing in `NamedInterruptRules.Decide` (Countermanda, Destroy Scow, Senior Staff Meeting, Hail, Kevin Convergence). TableWindow keeps AskPlayer/draw/discard/UI. [C - Grundlage]

---

## 2026-09-05 (Fix - Hugh fail returns to hand + battle source)

**Engine** - ApplyHugh uses DecideHugh; Fail restores Hugh to hand (no discard). Battle-cancel matches IsHughBattleSource (Borg Ship Dilemma or Rogue Borg). [C]

---
## 2026-09-05 (Fix â€” Hugh Rogue ship host-match)

**Engine** â€” Hugh HostMatches ships with Rogue Borg; Borg Ship Dilemma pool only when token/face visible (not mere Host attach). [C]

---

## 2026-09-05 (Fix â€” WNOHGB PathBlocked wrap fallback)

**Engine** â€” PathBlocked: prefer clear shorter wrap; if wrap arc Q-Net-blocked fall back to linear. Hazard walk uses WnohgbRules.UseWrapPath costs. [C]

---

## 2026-09-05 (Fix â€” Kevin hand restore + Hugh Spock)

**Engine** â€” Kevin/Devil: miss returns to hand; multi Event uses picker (TABLE+attached). Hugh: Rogue Borg ship/location without detail-pick; Borg Ship = Dilemma only when revealed/present; no Borg-affiliation ships. [C]

---

## 2026-09-05 (Fix â€” WNOHGB wrap path)

**Engine** â€” WnohgbRules: ends adjacent for controller; hazard check uses wrap path (Q-Net no longer blocks Endâ†”End as if crossing the middle). Debug wnohgb wrap=. [C]

---

## 2026-09-05 (Fix â€” Wormhole pair-check after drag)

**Engine** â€” Pair-start counts the Wormhole being played; drag removes it from hand before drop so a 2-copy hand no longer fails as count=1. [C]

---

## 2026-09-05 (Extract Slice 4 - Lore/Hugh decide gates)

**Engine** - Lore Returns deny reasons in `EventRules.LoreReturnsDenyReason`; Hugh resolve modes in `InterruptRules.DecideHugh`; optional `KevinEventAtLocation` for Convergence filter. TableWindow keeps discard/UI/side effects. [C - Grundlage]

---

## 2026-09-05 (Extract Slice 3 - IncomingMessageRules)

**Engine** - Incoming Message apply early gates in `IncomingMessageRules` (`EarlyReject` / `IsAlreadyAtFacility` / `SameLocation`); TableWindow still attaches, highlights, picks facility, and moves. Attach-before-arrival order preserved; FindMissionForDockable parked bug untouched. [C - Grundlage]

---

## 2026-09-05 (Extract Slice 2 â€” Wormhole pair)

**Engine** â€” Wormhole pair gates in `InterruptRules` (`CanStartWormholePair` needs 2 in hand, `CanWormholeFirstOnShip`, `IsWormholeLocationCard`). Second-drop hit-test uses window rects; relocate syncs BoardStore. [C â€” Grundlage]

---

## 2026-09-05 (Extract Slice 1 â€” MovementHazardRules)

**Engine** â€” Q-Net/Tetryon check + Rift/Gaps after-move decisions live in `MovementHazardRules` (no WPF); TableWindow applies damage/discard/status. Gaps kill still only on Gaps location. [C â€” Grundlage]

---

## 2026-09-05 (Fix â€” Gaps kill only on Gaps location)

**Engine** â€” `ApplyEventAfterMove` Gaps random kill only when destination is the Gaps span (not Host/Host2 neighbor missions). Kill writes Action History + `gaps-kill` debug. [C â€” Grundlage]

---

## 2026-09-04 (Foundation E6 â€” IM/Required-Move Locations)

**Engine** â€” Incoming Message / required-move hops use `FlyBoardLine` + `Location.Span` (same as Fly); `MissionsOnSameSpaceline` remains paint. [C â€” Grundlage]

---

## 2026-09-04 (Foundation E5 â€” LegalMoves-Fly Locations)

**Engine** â€” `LegalMoves` Fly Collect uses `BoardStore` Location line via `EngineAuthority.FlyLineForPiece` (same as `CanMoveShip(Location[])` / `TryEvaluateFlyPath`); `OrderedMissions()` name list only as fallback. [C â€” Grundlage]

---

## 2026-09-04 (Fix â€” E4 Unique/Persona by Owner)

**Engine** â€” Unique/Enigma/Persona deny uses `BoardStore.InPlay(..., Owner)` (Glossary: restrict stays with owner under Lore/capture/commandeer). Opponent may still field their own copy. Log `unique deny â€¦ owner=`. [C â€” Grundlage]

---

## 2026-09-04 (Foundation E4 â€” InPlay/Unique/Persona by instance)

**Engine** â€” `BoardStore.InPlay` / `InPlayInstances` query spaceline+TABLE by Controller (Owner separate for Lore). `PlayRules`/Report unique deny by InstanceId+persona; log `unique deny Nebula #283 have=#240 controller=2`. Attempt/HiddenAgenda InstanceId-first. [C â€” Grundlage]

---

## 2026-09-03 (Foundation E3b â€” Cloak + Dock + Hull on instance)

**Engine** â€” `ShipInstance.Cloaked` / `DockedAtId` / `HullPercent` are source of truth; UI `_cloakedShips` / `_dockedAt` / `_hullDamagePercent` stay mirrors. [C â€” Grundlage]

---

## 2026-09-03 (Fix â€” Fly ship lookup by InstanceId)

**Engine** â€” Fly resolves ship by InstanceId not name (two U.S.S. Nebula). Klasse C.

---

## 2026-09-03 (Foundation E3 â€” RangeLeft + Stopped on instance)

**Engine** â€” `ShipInstance.RangeLeft` + `CardInstance.Stopped` are source of truth for Capture/`ToGameState`/Overlay; UI `_shipRangeLeft` / `_stoppedBorders` stay mirrors (write-through + Sync copy). Log `range #id left=N source=instance` on Fly. Cloak/Dock/Hull deferred to E3b. Dual-run kept; E2 hang fix untouched. [C â€” Grundlage]

---

## 2026-09-03 (Fix â€” E2 Beam hang)

**Engine** â€” Beam no longer freezes the WPF UI. Root cause: `Log.Changed` â†’ `RefreshActionHistory` â†’ `LegalMoves` fly-eval `DebugLog.Move` â†’ `HistorySink` â†’ `AddDebug` â†’ `Changed` again (dispatcher flood). E2 store-first made ships Staffed so Fly-eval ran after Beam's log. Guard + coalesce refresh; skip HistorySink while refreshing. E2 store-first Capture kept. [C]

---
## 2026-09-02 (Foundation E2 â€” Capture store-first)

**Engine** â€” `CaptureEngineState`: HostName/Staffed/Aboard from BoardStore Occupant when present; UI border crew/staff walks only as fallback, logged `state-fallback:` / `capture: source=store|fallback`. RangeLeft/Stopped still UI (E3). Dual-run kept. [C â€” Grundlage]

---

## 2026-09-02 (Foundation E1 â€” ToGameState)

**Engine** â€” `BoardStore.ToBoardPieces` / `ToGameState(GameStateSeed)`. Capture bevorzugt Store-Board + Crew, UI-Board nur wenn Spaceline leer (Seed). Status (RANGE/Stopped) Overlay. Log `state:` / `capture: source=store|fallback`. [C Â· Grundlage]

---

## 2026-09-02 (Board Schritt 6 â€” AufrÃ¤umen)

**Board** â€” tote Namenslisten-Helper weg; Dockables/IM als View markiert. [C]

---

## 2026-09-02 (Board Schritt 5 â€” Targeting vom Store)

**Target** â€” Gaps/Q-Net-Paare und Play-on-Hosts aus `BoardStore.Locations` / Occupants. Kevin-Snap bleibt UI. [C Â· Grundlage]

---

## 2026-09-02 (Fly-eval Log + Engine auf Board)

**7.1.5** â€” Engine-Fly nutzt Board-Locations. Log `fly-eval` / `fly-mark` mit from/to/hops. [C]

---

## 2026-09-02 (Board Schritt 4 â€” Beam schreibt Force)

**Board** â€” Add/Remove Host-Stapel schreibt `Force`. GetCrewOnShip = Board âˆª Stack. Fly-Staffing sieht frisch gebeamte Crew. [C Â· Grundlage]

---

## 2026-09-02 (Board Schritt 3 â€” Fly liest Board)

**7.1.5** â€” Fly-Highlight + RANGE aus `BoardStore.Locations` (Gaps-Span, Q-Net-Kante). Apply/Relayout unverÃ¤ndert. [C Â· Grundlage]

---

## 2026-09-02 (Board Sync Gaps/Q-Net)

**Board** â€” Sync liest Gaps/Q-Net aus AttachedEvent (Host/Host2), nicht nur `_spacelineOrder`. [C]

---

## 2026-09-02 (Board Schritt 2 â€” Sync)

**Board** â€” SyncBoardFromTable nach Seed/Fly/Beam + Dev Dump Board. Gaps = Location, Q-Net = Barriere. UI bleibt Quelle. [C Â· Grundlage]

---

## 2026-09-02 (Board Schritt 1 â€” leere Typen)

**Board** â€” `Game/Board/`: Spaceline, Location, Occupant, Force, *Instance, BoardStore.Wrap. Dev-MenÃ¼ Dump Board. Kein Sync, kein Fly. [C Â· Grundlage]

---

## 2026-09-02 (Board Schritt 0 â€” File-Logger)

**Debug** â€” `Services/DebugLog.cs`: Session-Datei `Data/Logs/stccg-â€¦.txt`. ActionLog schreibt mit. KanÃ¤le Play/Move/Beam/Target/Board/Layout/Engine/Save. CheckTrace.Cmp nicht in die Datei. [C Â· Grundlage]

---

## 2026-09-02 (Board-Modell Plan)

**Docs** â€” `BOARD_MODEL.md`: Dual-Run Spaceline/Location/Occupant/Force; Schritt 0 File-Logger. Kein Code. [C]

---

## 2026-09-02 (Targeting Step 4 + single-stack snap)

**Peek** â€” 1 Event auf dem Host: OwningHost + Strip + Snap-Rahmen. [B Â· nullify]

**Seed / Report** â€” TargetWhy.Seed / Report in CollectSitesForDrag, Glow Ã¼ber TargetSession. [B]

---

## 2026-09-02 (HasSkill freeze / Gaps click / span X)

**HasSkill** â€” kein per-Token CheckTrace. [A]

**Gaps-Fly** â€” Klick auf Landable; GameState.OrderedMissions enthÃ¤lt Span-Namen.

**Span-Anzeige** â€” keine P2-Rotation; nach Relayout in die LÃ¼cke.

---

## 2026-09-01 (Gaps location / IM wrap RANGE)

**Gaps** â€” Landbare Location Span 4; Fly-Glow + Anker. Q-Net bleibt Barriere, kein Stop. Nach Insert Relayout Ã¼ber ActualWidth. Event nicht mehr auf den zwei Nachbar-Missionen. [A Â· Plays on spaceline]

**7.10 + WNOHGB** â€” KÃ¼rzester Hop, den das Schiff diese Runde zahlen kann; Wrap nur wenn RANGE reicht, sonst die andere Seite.

---

## 2026-09-01 (Lore NA attack / IM discard visual)

**Lore / 7.4.1** â€” GetAffiliations achtet auf CurrentAffiliation (Commandeer = NA). Wartime nur nach gelungenem Angriff auf FED. [A]

**IM Arrival** â€” Mini vom Canvas, dann Discard (kein Stapel oben links). [A Â· 7.10]

---

## 2026-09-01 (TargetQuery PlayOn / Gaps / IM)

**PlayOn** â€” Event-Ziele Ã¼ber TargetQuery.CanPlayOn (Lore / Neural / Plasma / Espionage / Schiff / Planet / Mission / Outpost). [B Â· Plays on]

**Gaps / Q-Net** â€” Site GapSpan, Glow zwischen den Missionen.

**Incoming Message** â€” Schiff CanPlayOn; Facility CanImFacility + Glow + Place-Choose.

---

## 2026-09-01 (TargetQuery Nullify)

**Targeting** â€” Game/TargetQuery.cs: eine Legal-Liste (Sites). Kevin/Devil: Glow â†’ 1s Peek â†’ Snap; Drop auf Schiff ohne Snap = Place-Choose der Events auf diesem Host. [B Â· nullify]

**ZurÃ¼ckgestellt:** Beam-Vollmodus, Battle-Zielwahl, Response-Stack, Netz/KI.

---

## 2026-09-01 (Lore fly anchor / peek Run)

**Fly after Lore** â€” FindMissionForDockable: IsMissionCard + Pin nach Relayout, damit Engine HostName hat. [A Â· 7.1.1]

**Peek** â€” Parent-Walk vertrÃ¤gt Run (kein Visual). [UI]

---

## 2026-08-31 (Lore battle / Hugh mission / IM dock)

**Lore Battle** â€” CanInitiateShipAttack akzeptiert Lore-Staffing statt Leader. [A Â· Lore Returns]

**Hugh** â€” Snap nur auf die Mission, nicht auf ein Schiff. Kill bleibt alle RB beider Seiten. [A Â· location]

**Kevin vs Lore Returns** â€” Nullify gibt das Schiff dem Owner zurÃ¼ck (Seite wechseln), RB self-controlling. [A Â· 7.8]

**IM Buruk** â€” Required move dockt ab; Hop-Fail im Log. History 2500 Zeilen / grÃ¶ÃŸeres Fenster. [A Â· 7.10]

---

## 2026-08-31 (Lore / Hugh / IM / Crosis)

**Lore Returns** â€” Schiff staffed + Fly/Battle ohne Leader-Crew; Rogue Borg Planetâ†’Schiff beamen; Relayout auf Controller-Seite. [A Â· Lore Returns / 7.8]

**Hugh Location** â€” Ziel ist die Spaceline-Location (Mission oder Schiff dort), nicht ein einzelner Rogue-Borg-Token. Kill weiter alle RB an der Location. [A Â· Hugh / Glossary location]

**Crosis Detail** â€” Interrupt nur einmal in der Leiste (nicht Stack + Attach). [UI]

**Incoming Message Hop** â€” RANGE-Index = Missions derselben Spaceline; Wrap nur Controller; Schiff nach Authority umsetzen. [A Â· 7.10]

---

## 2026-08-31 (Probespiel)

**Hugh / Rogue Borg** â€” Kill rÃ¤umt Host-Stapel + Token; Discard auf *Owner*-Pile (nicht Hugh-Spieler / Controller). [A Â· Hugh / Glossary discard pile]

**Kevin vs Static Warp Bubble** â€” SWB ist kein Treaty und kein [Shield]; Kevin darf nullify. Traveler: Transcendence bleibt der andere Nullifier. [A Â· Kevin / printed]

**Kevin-Snap im Host-Detail** â€” Overlay Ã¼berdeckt das Schiff nicht mehr den Mini-Snap; Rahmen folgt der Mini unter dem Cursor. [UI Â· Plays on]

**WNOHGB** â€” Wrap nur fÃ¼r den Controller (eigene TABLE-Spalte), nicht fÃ¼r beide. [A Â· printed â€žYou mayâ€¦â€œ / 7.1.7]

**Incoming Message UI** â€” Auto-Zug in Execute setzt das Schiff visuell hop-weise auf die nÃ¤chste Location (RANGE war schon abgezogen). [A Â· IM / 7.10]

**Beam in den Weltraum** â€” Kein Away-Team auf Space-Missionen (7.1.1.0.1). Planet-AT unverÃ¤ndert. Staffing-Fehler nennt jetzt leere Crew vs. Affiliation. [C Â· 7.1.1.0.1]

**Subspace Schism** â€” im Probespiel bestÃ¤tigt, von der Offenen-Liste.

---

## 2026-08-31

**Mission OR / xN** â€” `Diplomacy x5 OR Honor x4` nicht mehr still Ã¼bersprungen. `+` = UND, `OR` = eine Alternative. Skill-Count exakt (kein StartsWith). [A Â· 7.2.5]

**Battle-Overlay** â€” Ship/Personnel-Ergebnis + Return Fire Ã¼ber Reveal/AskChoice, nicht Windows-MessageBox. Destroy erst nach OK, damit Escape-Pod-Response offen bleibt. [UI Â· 7.4.3 / Escape Pod]

**Espionage (PR 4)** â€” Nur Mission mit [On]-Icon. FÃ¼r den Besitzer zÃ¤hlt die Mission zusÃ¤tzlich als [As]. Discard beim LÃ¶sen (war schon da). [A Â· Espionage / 7.2]

**Incoming Message Auto-Zug** â€” Beim Wechsel Playâ†’Execute: volle RANGE, hop-weise zur Facility. Fly nur in Execute (darum vorher tot). [A Â· IM / 7.10]

**WNOHGB RANGE** â€” KÃ¼rzerer Ring-Pfad (nicht nur Endeâ†”Ende). Gaps-Span aus Text (`span 4`) zÃ¤hlt mit. `HasTableCard` nur TABLE-Spalte, nicht Attached Events. [A Â· 7.1.7]

**Beam Affiliation** â€” Personnel nur auf kompatibles Schiff/Facility (Treaty/NA). Equipment frei. Planet-AT frei. [A Â· 7.1.1 / compatible]

**Data-Pfade** â€” `GamePaths.DecksRoot` / `SaveGamesRoot` (`Data/Decks`, `Data/SaveGames`). Dialoge starten dort. csproj: Content-Copy fÃ¼r beide Ordner.

**Icons** â€” `Assets/Icons/Icon_{Token}.png`, Text-Fallback. Staffing-Zeile + Host-Badge Icon-Counts.

---

## 2026-08-30

**Spacedock / Dock** â€” Schiff-MenÃ¼ Dock at [Facility] / Undock. Flag `_dockedAt`. Fly erst nach Undock. Spacedock am Outpost: Dock = volle Reparatur. [A Â· 7.1.4 / Spacedock]

**Yellow / Red Alert** â€” Yellow Alert verhindert Red Alert (Karte bleibt nicht auf TABLE). Bei Nullify von Red Alert: Download Yellow Alert (Draw / Tent). Personnel-SD unverÃ¤ndert. [A Â· Yellow Alert / 6.5.3]

**Target snap (C)** â€” Eine Liste `CollectLegalSnapHosts`: Halo + Snap-Feld + Drop. Interrupts fielen vorher aus `UpdateSnapPreviewFromWindow`. Neue Karten: `PlayOnRules.Parse` oder ein Named Override. [C Â· Plays on]

**Wormhole** â€” Zwei Karten Pflicht. Erste nur auf eigenes **exposed** Schiff (`!cloaked`). Zweite nur auf Location (Mission / Time Location), Snap-Halo. Schiff dockt dort, wird gestoppt. Kein TABLE-Drop, keine Detail-Picker. [A Â· Wormhole / exposed]

---

## 2026-08-29

**Shared unique missions** â€” Zweite Kopie derselben Unique-Mission (Quick Game, gleiche Decks) bleibt eine Location: unsichtbar, kein eigener Seed-Host. `AllMissionBorders` = nur Spaceline-Primaries. [C Â· unique and universal]

**Shared Face / 4.2.0.3** â€” Ein Stapel. Bild + Rotation = Zugspieler (dessen Drucktext). Attempt/Solve Ã¼ber `MissionPrintedFor`. Shared ist your mission und opponent's mission. Kein Mission-II, kein 4.3-Staging.

**Lore Returns staff** â€” Host-Match `SameHostShip`; Schiff NA + Controller; Fly/Attack mit Rogue Borg ohne Officer. [A Â· 7.8 / Lore Returns]

**Hugh** â€” [Bor]-Schiff, Borg-Ship-Dilemma, Rogue Borg. Response bricht Battle; Drop auf RB tÃ¶tet alle RB der Location; Drop auf Dilemma blockt den nÃ¤chsten Puls. [A Â· Hugh / 7.4.1.0.2]

**Mission-Drehung** â€” Nur Shared-Locations folgen dem Zugspieler.

**Horga'hn leave-play** â€” Effekt nur solange die Karte auf dem Tisch liegt. `OnCardLeftPlay` bei Discard/OOP/Hand. [B Â· in play]

**Board extents** â€” Canvas wÃ¤chst mit gestapelten Schiffen; Spaceline rutscht nach unten wenn P2-Stapel Ã¼ber Y=0 ginge. Scrollbars Auto, Zoom-Min passt sich der FeldgrÃ¶ÃŸe an, RMB-Pan erreicht jede Karte.

**Incoming Message** â€” Matching-Affiliation-Schiff (beliebiger Besitzer). Controller wÃ¤hlt eigene passende Facility auf **dieser** Spaceline (gleicher Quadrant). Bleibt am Schiff; nur Bewegung dorthin (7.10). Nullify bei Ankunft / keiner Facility. Subspace Interference im Spiel; Amanda nur just-played. [A Â· IM / 7.10 / 12.10]

**Required-move path** â€” KÃ¼rzester Weg auf derselben Spaceline (Quadrant). WNOHGB-Wrap wenn kÃ¼rzer. Cytherians-Far-End einmal fest (12.6). Mehrere Required Actions: Spieler wÃ¤hlt Reihenfolge. Conundrum nutzt denselben Pfad. [C Â· 7.10 / 12.6]

**Energy Vortex** â€” ZurÃ¼ck auf die Hand, Ersatz-Play erlaubt, **dieselbe Kopie** diesen Zug gesperrt. Andere Karte (auch gleicher Titel, andere Kopie) ok. [A Â· printed EV]

**Subspace Schism** â€” Jeder Draw Ã¶ffnet 10s-Response wenn Schism auf einer Hand. Einmal/Zug. Discard der gezogenen, nÃ¤chste Karte ohne zweites Fenster. [A Â· printed Schism]

## 2026-08-27

**Host-Detail** â€” Schiff: Special Equipment (Holodeck, Tractor Beam) oben bei den Stats; angehÃ¤ngte Events/Dilemmas darunter mit Live-Effekt statt Persist-Enum (`Event: Nutational Shields â€” SHIELDS +2 (1 ENGINEER aboard)`). `EventRules.FormatHostEffectSummary` / `DilemmaRules.FormatHostEffectSummary`. Personnel: keine doppelte Classification, kein Skill-Dump aus `card.Text`, Icons an die Classification-Stelle. Overlay: Kartenleiste unten mit horizontalem Scroll, mehr Text darÃ¼ber, Back + Select/Close rechts.

**Kartenwahl** â€” Keine Ja/Nein-Karte-fÃ¼r-Karte-Schleifen mehr. `PickCardFromList` / `PickBorderFromList` nutzen die Detail-Leiste (horizontal scroll). Umgestellt: Res-Q, Palor Toff, Wormhole, PickOwnShip, Stone of Gol, Thought Maker, Kurlan, Event-Target-Dialog, Dilemma-Picks.

**Subspace Warp Rift** â€” Fly-by beide Richtungen; Schaden beim Weiterfliegen nach Ankunft im selben Zug. Tetryon analog. Kevin trifft Gaps-Span + Fallback-Galerie. `PickOption` fÃ¼r OR-Textwahlen.

**Nullify / Devil / Rift-Badge** â€” Kevin/Devil nicht mehr als TABLE-Permanent ohne Ziel spielen. Nullify entfernt Span auf der Spaceline, Owner-Discard, Kevin OOP-Zone. Relayout behÃ¤lt Rotation + DMG-Badge am bewegten Schiff. Action History: Mehrfachauswahl + Ctrl+C.

## 2026-08-28

**Horga'hn** â€” Flag folgt der Tischkarte (Hand-Play + Acquire). Extra Normal-Play bleibt im Play-Segment; sonst Extra-Draw am EOT aus dem Deck des Controllers. Devil lÃ¶scht nur den Owner-Flag. [A Â· 2.3.0.1]

**The Devil / Wind Dancer** â€” Beim Encounter (vor Filter) Yes/No, wenn The Devil auf der Hand liegt. `EncounterDilemma` als Stack-Kind; CollectDevil sieht Last-Encounter. [A Â· 6.5.1 / Glossary nullify]

**Stapel-Peek (UI)** â€” Kevin/Devil 2s Ã¼ber Schiff/Mission/Outpost mit legalem Ziel Ã¶ffnet Detail; legale Karten gold umrandet, Drop trifft die Mini.

**Warp Core Breach / â€žMay be nullified by SKILLâ€œ** â€” Nullify ist optional (Schiff-Button, auch gestoppt), nicht automatisch am EOT. `NullifyEventInPlay` rÃ¤umt Host-Stapel + Detail. ZerstÃ¶rung erst am EOT des *nÃ¤chsten* Controller-Zugs (Countdown 2 im laufenden Controller-Zug). Plasma Fire derselbe Discard-Pfad. [B Â· Glossary nullify / 7.10.0.1 / 8.1]

**AskChoice** â€” OR-Dialoge mit beschrifteten Buttons (z.B. Klingon / Romulan, nicht Yes/No). Timeout = gleichverteilt zufÃ¤llig + Result-Overlay. `AskPlayer` fÃ¼r P1/P2. Umgestellt: Dual-Affiliation-Report, Juggler, Traveler, Draw-3, Masaka, Q's Tent. `PickOption` nutzt dasselbe.

**PlayOnRules** â€” â€žPlays on â€¦â€œ-Parser (Ship/Outpost/Facility/Mission/Crew/Table + Own/Exposed/Occupied/Cloaked). Interrupt-Drop nutzt Spec; Schiff â‰  Outpost. Asteroid Sanctuary nur eigenes uncloaked Schiff.

**Fed Battle / Devil / LegalMoves** â€” Federation initiiert Battle nur vs Borg (NA/Kli im selben Force hebt das nicht); Wartime bleibt Ausnahme. The Devil = `nullify-inplay`. Off-turn-Interrupts erscheinen in LegalMoves. [C Â· Â§7.4.1 / 6.5.1]

**Prozess** â€” `RULES.md` = Fix-Protokoll Klasse A/B/C + Lookup Glossary/Errata. `RULES_CHECKLIST.md` = Compendium 2.7.4 auf x.x.x.

---

## 2026-08-26

**LegalMoves** â€” `Collect(player)` nur dieser Sitz; Stack nur `ResponsePlayer`; `CollectBoth` fÃ¼r Netz/KI; Beam-VorschlÃ¤ge von Schiff, Facility und Mission.

**Dual-Affiliation (6.3.3)** â€” `Card.CurrentAffiliation`, `DualAffiliationRules`, Mode-Skills (Rakal/DeSeve), Report wÃ¤hlt kompatiblen Mode, Switch zwischen Actions (nicht im Attempt, nicht inkompatibel an Bord).

**AU Apply** â€” Yellow Alert (kill/prevent Red Alert, CUNN+1), Baryon RANGEâˆ’2 + SOT-Nullify, Klim kein EOT-Draw, Thermal vs Firestorm/Thought Fire/Plasma, Captain's Log / Lower Decks, Particle Scatter kein Planet-Beam, Intruder FF Rogue Borg &lt;3, Wartime nach Fed-Angriff. Interrupts: Kevin Convergence, Countermanda, Destroy Scow, Senior Staff Meeting.

**Dil Apply** â€” Edo Probe Lock oder âˆ’10 am Zugende; Conundrum chase; Frame of Mind 3-3-3 + 2 Skills, Cure 3 Empathy.

**PR Apply** â€” Neural Servo Control bis EOT; Distortion Unstop/RANGE/Away-Team; Tachyon + Cloak-Button; Anti-Time Shuffle beider Spieler; Sanctuary EOT-Discard.

**Katalogtexte** â€” PR Events/Interrupts/Artifacts an Drucktext; Kevin OOP; Auto-Destruct CD=1, Groupie/Temporal Rift CD=2; Life-form Scan = Gegnerhand.

**Engine-Fundament** â€” `GameState` / `GameAction` / `EngineAuthority` / `EffectRegistry` / `CardEffectMap` Â· `SeedRules` (Cryo Space, Neutral Outpost NA, 3 AU) Â· `TurnExpiry` Â· Download/HA-Flip-Verben Â· LegalMoves Seed Â· Until-EOT nur finishing player Â· Stopped im Snapshot.

---

## 2026-08-21â€“25

Opponent-Pile-Inspector Â· Mission Owner-Orientation / asymmetric reqs Â· Crew-Interrupt-Ziele Â· Rogue Borg + Crosis + Lore Returns Â· TurnScope Â· Skill-Parser `Diplomacy x 2`.

---

## 2026-08-20

`.stsave` JSON Â· Event-Targets / Gaps / Q-Net Â· TABLE volle rechte Spalte Â· Red Alert 5er-Play Â· Kidnappers Â· Host-Events bleiben am Ziel.

---

## 2026-08-19

Deck Builder EN, Inline-Move, `DeckPlacementRules`, Skill-Multifilter.

---

## 2026-08-18

Ship Battle + Rotation Damage Â· Dilemmaâ†’Stopped Â· Repair Â· Equipment/Modifier Â· Treaties Â· Interrupt ActionStack Â· Premiere Dil/Art/Event/Interrupt-Kataloge Â· Card Reveal Â· Action History.

---

## 2026-08-16â€“17

Phase-2-Tisch: Seed-Phasen, Hotseat feste Spaceline, Report/Staff/RANGE/Beam, Mission Attempt, Hybrid-Orders-UI.

---

## 2026-08-15

Phase 0 Modelle + JSON-Loader Â· Phase 1 Deck Builder `.stdeck` Â· Lackey-Split in Set-Ordner.

