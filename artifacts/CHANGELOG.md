## 2026-09-26 — EXTRACT_REST: P2 (Ship & Personnel Battle, Counter-Attack State & Escape Pod)

- *P2-S7 (Counter-Attack State in BoardStore & BattleRules)*:
  - Datenmodell `BattleRules.CounterAttackOpportunity` eingeführt (`EligiblePlayer`, `LocationMissionInstanceId`, `InvolvedOpponentInstanceIds`, `Armed`).
  - Helper `BattleRules.IsArmedCounterAttackAt`, `BattleRules.IsCounterAttackTarget`, `BattleRules.RegisterCounterAttack` und `BattleRules.UpdateCounterAttackWindow` implementiert.
  - `BoardStore.CounterAttack` als Single Source of Truth auf der Engine-Seite angelegt und in `Clear()` integriert.
  - In `TableWindow.xaml.cs` `IsArmedCounterAttackAt`, `IsCounterAttackTarget`, `RegisterCounterAttackOpportunity` und `UpdateCounterAttackWindow` so umgestellt, dass sie primär `BoardStore.Current.CounterAttack` und `BattleRules` nutzen.
- *P2-S1 & P2-S2 (Ship Battle Zielwahl-Filter & Initiierung)*:
  - In `BattleRules.cs` `CanShipInitiateBattleAtLocation` und `IsLegalShipAttackTarget` implementiert (prüft ungestoppt, ungedockt, ungetarnt, kein Required Move, WEAPONS > 0, Leader und Matching Affiliation).
  - In `TableWindow.xaml.cs` `BeginAttackMode` auf `BattleRules.CanShipInitiateBattleAtLocation` und `BattleRules.IsLegalShipAttackTarget` umgestellt.
- *P2-S3 & P2-S4 (Ship Battle Plan & Return Fire Orchestrierung)*:
  - In `BattleRules.cs` `DecideReturnFireEligibility` und `ExecuteShipBattlePlan` (`ShipBattlePlan`) implementiert. Berechnet Open Fire, Rotation Damage, Return Fire Checks & Boni, Winner und Folgestatus komplett als Regelplan.
  - In `TableWindow.xaml.cs` `AskReturnFireAndResolve` delegiert die Eignungsprüfung an `BattleRules.DecideReturnFireEligibility`.
  - In `TableWindow.xaml.cs` `ResolveShipBattle` delegiert die Gefechtsauflösung vollständig an `BattleRules.ExecuteShipBattlePlan` und führt die Wirkungen (Damage, Stopped, Discard/Destroy, Reveal) aus dem Plan aus.
- *P2-S6, P2-E1 & P2-E2 (Destroy-Policy & Escape Pod Checks)*:
  - In `BattleRules.cs` `CanEscapePodRespond` ausgelagert.
  - In `InterruptRules.cs` `IsLegalEscapePodCrew` ausgelagert (filtert Nicht-Personal, Equipment und gefangenes Gegner-Personal heraus).
  - In `TableWindow.xaml.cs` `DestroyShipOrFacility`, `ShipHasCrewForEscapePod` und `ApplyEscapePodFromResponse` auf die neuen Rules-Methoden umgestellt.
- *P2-P1..P2-P4 (Personnel Battle)*:
  - In `BattleRules.cs` `CanOfferPersonnelBattle` ausgelagert; `CanOfferPersonnelBattleFromShip` in `TableWindow.xaml.cs` darauf umgestellt.
- *Verifikation*:
  - Neuer Mini-Test `BattleRules.VerifyBattleRulesPlan()` in `ShipRules.VerifyShipRules()` eingehängt und erfolgreich verifiziert (PASS).

## 2026-09-26 — EXTRACT_REST: P0 (Persist-Modell & Dual-Run) & P1 (Borg Ship EOT)

- *P0-D1 (`AttachedDilemma` aus Window nach Board)*:
  - Datenmodell `BoardAttachedDilemma` in `StarTrekCCG/Game/Board/BoardAttachments.cs` eingeführt mit `HostInstanceId`, `DestInstanceId`, `Direction`, `Held` und `OriginalEncounter`.
  - `BoardStore.AttachedDilemmas` angelegt, in `Clear()` integriert und in `ToBoardPieces()` als Engine-Snapshot-Pieces (`PieceRole.DilemmaPersist`) serialisiert.
  - In `TableWindow.xaml.cs` Helper `AddAttachedDilemma`, `RemoveAttachedDilemma` und `SyncAttachmentsToStore` verdrahtet. Alle `_attachedDilemmas.Add`/`Remove` umgestellt. `CaptureEngineState()` liest Dilemma-Attachments und Quarantäne-Zustand direkt aus dem Store.
- *P0-E1 (`AttachedEvent` aus Window nach Board)*:
  - Datenmodell `BoardAttachedEvent` in `StarTrekCCG/Game/Board/BoardAttachments.cs` eingeführt mit `HostInstanceId`, `Host2InstanceId`, `TurnScope`, `PhasePoint`, `ScopePlayer` etc.
  - `BoardStore.AttachedEvents` angelegt, in `Clear()` integriert und in `ToBoardPieces()` als Engine-Snapshot-Pieces (`PieceRole.EventPersist`) serialisiert.
  - In `TableWindow.xaml.cs` Helper `AddAttachedEvent`, `RemoveAttachedEvent`, `RemoveAttachedEventsForCard` und EOT-Tick-Sync verdrahtet. Alle direkten Zugriffe auf `_attachedEvents.Add`/`Remove` umgestellt; `CaptureEngineState()` liest Store.
- *P0-S1 (Dual-Run abschließen)*:
  - `ShipInstance` in `StarTrekCCG/Game/Board/CardInstance.cs` um `RepairTurns` und `CloakLocked` erweitert.
  - In `TableWindow.xaml.cs` Store als Single Source of Truth für Hull, Cloak, RepairTurns und CloakLocked etabliert: `GetRepairTurns`/`SetRepairTurns`, `IsCloakLocked`/`SetCloakLocked`, `IsShipCloaked`, `ApplyHullDamage`/`GetHullDamage` operieren auf Store-Instanzen. `ApplyUiStatusToStore` synchronisiert die Felder auf die Instanzen.
- *P1 (Borg Ship EOT)*:
  - `BorgShipRules.cs` in `StarTrekCCG/Game/BorgShipRules.cs` erstellt mit `Weapons = 24`, `Shields = 24`, `PointsOnDestroyed = 15`, `IsLegalTarget(...)`, `BorgWeaponsBonus(...)`, `BorgShieldsBonus(...)`, `DecideInitialDirection(...)` und `DecideMove(...)`.
  - In `TableWindow.xaml.cs`:
    - `StartBorgShipEotAttacks`: Ziele via `BorgShipRules.IsLegalTarget` gefiltert.
    - `AskReturnFireAndResolve`, `ResolveShipBattle`, `TryDestroyBorgShipInBattle`: Literal-24 und Hardcoded-15 durch `BorgShipRules`-Konstanten und Boni ersetzt.
    - `FinishBorgShipEotMove`: Bewegungs- und Verlassens-Logik vollständig an `BorgShipRules.DecideMove(...)` delegiert.
    - `_borgShipDir` / `Direction` in `BoardAttachedDilemma` und `AttachedDilemma` abgelegt; Initialrichtung über `BorgShipRules.DecideInitialDirection` ermittelt.
  - In `artifacts/EXTRACT_REST.md`: Abschnitte P0 (P0-D1, P0-E1, P0-S1) und P1 als ERLEDIGT markiert.

## 2026-09-26 — Vulcan Mindmeld (144 U) Bugfix & Generisches Buried-Target-Peek-System

- *ModifierRules & Vulcan Mindmeld Bugfix (Kein Stacking auf Engineer x2)*:
  - Equipment-Skill-Grant-Regel (1E Glossar "Equipment" & "skills — modifying"): Equipment, das eine Fähigkeit verleiht ("gain [skill]"), verleiht diese nur an Personal, das diese Fähigkeit noch nicht besitzt. In `ModifierRules.ResolvePersonnel` wurde die Prüfung `if (skills.GetValueOrDefault(def.GrantedSkill) > 0) continue;` ergänzt, sodass Data (gedruckt `ENGINEER: 1` und `OFFICER`) bei anwesendem *Engineering Kit* nicht fälschlich `ENGINEER x 2` erhält.
  - Classification-Filterung bei Skill-Kopieren: In `TableWindow.ApplyVulcanMindmeld` wird die gedruckte Classification des Donors (`MissionRules.PrintedClassificationParts(skillDonor)`, z. B. `OFFICER` bei Data) vor der Skill-Übertragung herausgefiltert, sodass nur reguläre Skills übertragen werden.
  - Saubere Initialisierung temporärer Skills: `ModifierRules.GrantTemporarySkills` erzeugt stets ein frisches Dictionary, um Nebeneffekte durch Mehrfachaufrufe auszuschließen.
- *Generisches Buried-Target-Peek- und Drop-System*:
  - Generische Erkennung verdeckter Ziele: `TargetQuery.IsCardTargetingBuried` erkennt neben *Vulcan Mindmeld* und *Disruptor Overload* per Regex alle Karten mit Zielformulierungen auf Personal, Equipment oder Mindmeld (`plays on ... personnel/equipment/mindmeld`).
  - Erweiterung von `WantsBuriedPeek` und `CanTarget`: Ermöglicht Stack-Peek beim Draggen über Wirtselemente (Schiffe, Außenposten/Facilities, Planeten/Missionen mit Away Teams).
  - Hover & Detailfenster-Anzeige (`IsLegalPeekTarget`, `BuriedLegalOn`, `FindHostUnderWindow`, `UpdatePeekSnapAt`):
    - Beim Halten über einem Wirt mit legalen Zielen öffnet sich nach 1s Haltezeit das Detailfenster (`CardDetailOverlay`).
    - Legale Ziele im Stapel leuchten cyan auf (`Color.FromRgb(80, 220, 255)`).
    - Beim Bewegen über das Mini rastet der Snap ein (`Color.FromRgb(40, 255, 120)` grün).
    - SnapSite erzeugt für Play-On-Karten saubere Status-Meldungen (`Play on {hit.Name}`).
  - Drop-Unterstützung für Hand- und entsperrte Sidedeck-Karten (`ZoneMini_MouseUp`):
    - `isHandOrUnlockedSide` integriert (gilt für Hand und entsperrte Sidedecks wie *Q's Tent*).
    - Bei Vulcan Mindmeld: Droppen auf ein Personal im Detailfenster übernimmt dieses direkt als `preselectedPersonnel` (überspringt den Auswahldialog für den Mindmeld-Anwender) und schließt das Detailfenster sauber.
    - Bei Events: Ermittelt bei offenem Detailfenster das Ziel bzw. den Wirt (`_eventPreferredHost`), schließt das Detailfenster und platziert das Event regelkonform.
    - Bei Disruptor Overload: Droppen auf ein konkretes Equipment zerstört dieses direkt (`RemoveEquipmentFromHost`).
- *Tests & Verifikation*:
  - `InterruptRules.VerifyVulcanMindmeldDecide` um vollständigen Sarek/Data/Engineering Kit-Fall erweitert:
    - Data behält `ENGINEER = 1` trotz anwesendem `Engineering Kit`.
    - Sarek erhält via Mindmeld `ENGINEER = 1` (nicht 2), `Computer Skill = 2`, `Music = 1`, `Astrophysics = 1`, `Exobiology = 1`.
    - Sarek behält seine eigenen Skills `Diplomacy = 3` und `Mindmeld = 1`.
    - Sarek erhält kein `OFFICER`.
    - Nach Expiry sind alle temporären Skills sauber bereinigt.
  - In `ShipRules.VerifyShipRules` eingehängt und verifiziert.

## 2026-09-26 — Interrupt Temporal Rift (140 U) & The Juggler (142 U)

- *SpacelineLocationRules* (Neue Architektur-Pipeline):
  - Zentralisierte Klassifizierung und Pipeline für alle Arten von Spaceline-Locations geschaffen (`IsTimeLocation`, `IsSpacelineLocation`, `PlaysAsSpacelineLocation`, `IsLandableSpacelineLocation`, `IsDifferentTimeContinuum`).
  - Standardisiert Karten, die als Spaceline Location fungieren (*Time Travel Pod*, *Temporal Rift*, zukünftige Zeit- und Raumlinienkarten).
  - Verhindert reguläre Warp-Flüge zwischen Zeitorten und der regulären Raumlinie (`IsDifferentTimeContinuum`).
  - Vereinheitlichung in `TableWindow`: `IsLandableLocation`, `BuildSpacelineDisplayOrder`, `GetSpacelineQuadrant` und `IsWormholeLocation` greifen nun auf `SpacelineLocationRules` zu.
  - Dedizierte, wiederverwendbare Platzierungspipeline `PlaceSpacelineTimeLocation` geschaffen, die von `PlaceTimeTravelPod` und `PlaceTemporalRift` geteilt wird.
- *Temporal Rift* (Premiere 140 U / 322 C):
  - Regelkonforme Umsetzung nach aktuellem Errata & Rulings:
    - Text: *"Plays on table as a universal space time location; relocate one of your exposed ships OR a dilemma here. Counts down only at the start of your turn. When nullified, return that ship or dilemma to its former location."*
    - Response- & Flucht-Sperre: `TimingRules.CanRespond` verbietet *Temporal Rift* als Antwort auf Kampf (`InitiateShipBattle`, `InitiatePersonnelBattle`) oder Dilemma-Begegnung (`EncounterDilemma`).
    - Exposed-Bedingung: Nur exposed Schiffe (`ShipRules.IsShipExposed`: ungedockt, ungetarnt, unphased, nicht gelandet/getragen) können versetzt werden.
    - Zielauswahl: Unterstützt Drag & Drop auf exposed Schiffe, On-Board-Auswahl via gelbem Glow (`BeginBoardPickShip`) oder Auswahl eines aktiven Dilemmas im Spiel.
    - Dilemma-Relocate: Versetzt Dilemmas (inkl. Borg Ship / Scow Token) an den Zeitort und stellt sie bei Ablauf/Nullify an ihren vorherigen Wirtsort zurück.
    - Timing & Countdown: Zählt nur zu Beginn des Zuges des Besitzers herunter (`ProcessTemporalRiftCountdowns` in `ProcessStartOfTurnTimedEffects`).
    - Rückkehr: Bei Nullify (via Kevin Uxbridge o. Ä., `OnCardLeftPlay`) oder nach Ablauf von Countdown 2 kehrt das Schiff bzw. das Dilemma an den ursprünglichen Ort zurück.
    - Immunität / Pausierung: Schaden und Countdowns von Schiffseffekten (z. B. *Plasma Fire*, *Warp Core Breach*) pausieren am Zeitort (`IsShipAtTimeLocation`).
    - Detailstatus: Zeigt Countdown und anwesende Schiffe/Dilemmas im Detailblock an.
- *The Juggler* (Premiere 142 U / 326 C):
  - Verifiziert und verbessert: Wählt Spieler aus (`AskPlayer`), mischt dessen Nachziehstapel per RNG neu und protokolliert dies detailliert im Log und der Statuszeile.
  - In `CARD_TRACKER.md` als funktionierend (`working`) verifiziert.
- Tests & Verifikation:
  - `InterruptRules.VerifyTemporalRiftDecide` implementiert und in `ShipRules.VerifyShipRules` integriert (alle Checks PASS).
  - `SpacelineLocationRules.VerifySpacelineLocationRules` validiert Zeitort- und Raumlinienregeln.
  - `dotnet build /p:EnableWindowsTargeting=true` erfolgreich (0 Fehler).

## 2026-09-25 — Interrupt Scan (295 C) & Tachyon Detection Grid (318 U)

- *Scan* (Premiere 295 C):
  - Regelkonforme Implementierung als Gegenstück zu *Full Planet Scan* für Weltraummissionen:
    - Timing-Gate: Spielbar zu Beginn des Zuges (`TimingRules.RequiresStartOfTurnWindow`, Segment 1, vor Ausspielen der regulären Karte).
    - Ziel: Eigenes Schiff an einer [S]-Mission (`!MissionCountsAsPlanetCard(mc)`) mit mindestens zwei gedruckten Staffing-Icons (`[Cmd]` / `[Stf]`).
    - Kosten: Stoppen von ungestopptem `Computer Skill` und `Stellar Cartography` an Bord (bevorzugt zwei getrennte Crew-Mitglieder; unterstützt auch Einzelpersonal mit beiden Fähigkeiten).
    - Effekt: Unterste Seed-Karte der Mission wird aufgedeckt (`ShowCardReveal`) und untersucht, Personal wird gestoppt, Karte wird abgelegt.
  - On-Board Picking & Snap-Glow:
    - Bei Ausspielen ohne Drop-Ziel werden alle legalen Schiffe am Tisch ermittelt (`FindLegalScanShips`) und via `PickBoardTarget` mit grünem Glow hervorgehoben und direkt auf dem Tisch auswählbar gemacht.
    - Drag & Drop Snap-Glow (`HostMatchesInterruptTargetForCard`, `GetLegalInterruptPlayHosts`) hebt nur eigene Schiffe an Space-Missions mit >=2 Staffing hervor.
- *Tachyon Detection Grid* (Premiere 318 U):
  - Standardisierung & Korrektur auf offizielle Regeln:
    - Voraussetzung: Spieler muss mindestens 4 exposed Schiffe im Spiel kontrollieren (`CountExposedShips >= 4`).
    - Exposed-Definition aus `ShipRules.IsShipExposed` verwendet: ungedockt, ungetarnt, unphased, nicht gelandet, nicht getragen. Getarnte oder gedockte Schiffe zählen nicht zu den 4 Schiffen.
    - Ziel: Ein beliebiges getarntes Schiff auf dem Tisch (Gegner oder eigenes).
    - Effekt: Schiff enttarnt sich sofort (`SetShipCloaked(host, false)`), selbst wenn es gestoppt ist oder sich in diesem Zug bereits getarnt hat.
    - Cloak-Lock: Wirtschiff wird bis zum Ende des Zuges für erneutes Tarnen gesperrt (`_cloakLocked`, via `TurnExpiry`).
  - Target-Selection Pipeline:
    - Wenn nicht direkt auf ein getarntes Schiff abgelegt, werden alle getarnten Schiffe auf dem Tisch ermittelt. Bei mehreren Schiffen leuchtet `PickBoardTarget` mit violettem Glow für direkte Klick-Auswahl.
    - Drag & Drop Snap-Glow hebt nur getarnte Schiffe hervor und wird sofort unterdrückt, falls der Spieler weniger als 4 exposed Schiffe besitzt.
    - Pre-Stack Validierung in `CanPlayCardWithReason` verhindert illegales Ausspielen ohne 4 exposed Schiffe oder ohne getarnte Schiffe.
- *ReturnInterruptToHand*:
  - Bereinigt bei Abbruch oder Fehlern die Karte zusätzlich aus dem Ablagestapel (`_discardCards` / `_oppDiscardCards`), um doppelte Kartenreferenzen zu verhindern.
- Tests & Verifikation:
  - `InterruptShipEffectRules.VerifyTachyonDecide` und `VerifyScanDecide` implementiert und in `ShipRules.VerifyShipRules` integriert (alle PASS).
  - Status von *Q2* und *Subspace Schism* in `CARD_TRACKER.md` als funktionierend (`working`) verifiziert und dokumentiert.
  - `dotnet build` erfolgreich (0 Fehler).

## 2026-09-25 — Einheitliche On-Board Zielauswahl-Pipeline & Ship Seizure (136 C) Board-Pick

- UX-Architektur & Einheitliche Pipeline:
  - `PickBoardTarget` in `TableWindow.xaml.cs` als zentrale Pipeline für die direkte Auswahl von Karten/Objekten auf dem Spielfeld (Schiffe, Spaceline Locations, Außenposten/Facilities etc.) implementiert:
    - Legale Ziele werden direkt auf dem `TableCanvas` mit einem animierten Halo/Glow hervorgehoben (`AddBoardTargetGlow`).
    - Mauszeiger wechselt über Zielobjekten auf `Cursors.Hand`.
    - Das erste Ziel wird bei Bedarf automatisch in den sichtbaren Bildbereich gescrollt.
    - Modale Interaktion via `DispatcherFrame`, sodass Karteneffekte synchron auf die getroffene Wahl warten können, ohne den UI-Thread zu blockieren.
    - Ein Klick auf ein markiertes Ziel wählt es aus; Klick auf leere Tischfläche oder Rechtsklick bricht die Auswahl ab und setzt das Ziel auf `null`.
    - Escape-Taste bricht die Auswahl ebenfalls sauber ab.
    - Vollständiges Aufräumen aller Glow-Rechtecke und Wiederherstellen der ursprünglichen Mauszeiger im `finally`-Block.
  - `PickBorderFromList` modernisiert:
    - Wenn die übergebenen Zielgrenzen (`candidates`) sichtbare Karten auf dem `TableCanvas` sind (z. B. Schiffe, Missionen, Einrichtungen), leitet `PickBorderFromList` automatisch an `PickBoardTarget` weiter, statt ein Detailfenster/Popup-Streifen (`PickCardFromList`) zu öffnen.
    - Nicht auf dem Tisch liegende Auswahlen (z. B. Personal in Crew-Stapeln bei *Genetronic Replicator*) nutzen weiterhin sicher die Scroll-Streifen-Detailansicht.
  - `PickCardOnBoard`: Komfort-Methode zur Auflösung von `Card`-Listen auf dem Spielfeld in Border-Ziele für `PickBoardTarget`.
- Integration bei Karten:
  - *Ship Seizure* (136 C):
    - Wählt das zu zerstörende leere, ungeschützte Schiff (`victim`) nicht mehr über ein Detailfenster (`PickCardFromList`), sondern lässt alle legalen Opfer am Ort auf dem Spielfeld mit bernsteinfarbenem Glow erstrahlen.
    - Spieler klickt das Zielschiff direkt auf dem Spielplan an.
    - Bei ungedropptem Ausspielen (z. B. Klick auf Ausspielen) werden auch die eigenen Schiffe mit Tractor Beam direkt auf dem Spielfeld grün markiert und zur Auswahl angeboten.
    - Bei Abbruch (Rechtsklick) wandert *Ship Seizure* sauber auf die Hand zurück (`ReturnInterruptToHand`).
  - *Incoming Message*: Auswahl der Ziel-Facility auf der Spaceline läuft nun über `PickBoardTarget` mit zyanfarbenem Glow direkt auf dem Tisch.
  - *Kurlan Naiskos*, *Alien Parasites*, *Kevin Uxbridge: Convergence* und `ShowTargetPickDialog` (*Conundrum*, *Anti-Matter Pod*, etc.): Nutzen via `PickBorderFromList` nun alle die einheitliche Board-Target-Pipeline.
  - Drag-and-Drop Snap-Glow bleibt für das direkte Ziehen von Karten aus der Hand oder dem Side-Deck auf Hosts unverändert intakt.
- Tests & Build:
  - `dotnet build` erfolgreich (0 Fehler, 2 bestehende Warnungen).

## 2026-09-25 — Ship Rules Pipeline & Ship Seizure (136 C) Standardisierung

- Architektur & Pipeline:
  - `ShipRules.cs`: Zentrale, wiederverwendbare Pipeline für Ship-, Facility- und Site-Begriffe nach aktuellem Regelbuch/Glossar (Stand 1. Januar 2024) implementiert:
    - `exposed`: Ein Schiff ist exposed, wenn es ungedockt (`!isDocked`), ungetarnt (`!isCloaked`), unphased (`!isPhased`) und weder gelandet noch getragen ist (`!isLanded && !isCarried`).
    - `occupied`: Ein Schiff, eine Einrichtung oder eine Site ist occupied, wenn mindestens ein Personnel an Bord ist (`aboard.Any(ModifierRules.IsPersonnelCard)`). Equipment oder Interrupts (z. B. Rogue Borg Tokens) allein machen einen Host gemäß Ruling vom 1. Jan. 2024 nicht occupied.
    - `unoccupied` / `empty`: Ein Schiff/Facility/Site ohne Personnel an Bord ist empty.
    - `empty exposed ship`: Kombinierte Bedingung für leere und ungeschützte Schiffe.
    - `your ship`: Prüfung auf Schiffsbesitz/Kontrolle (`shipOwner == player`).
    - `tractor beam`: Erkennt Tractor Beam sowohl im Text als auch in den `Characteristics` eines Schiffes.
    - `CanBeShipSeizureTractorHost`: Validiert das Wirtschiff für *Ship Seizure* (eigenes Schiff mit Tractor Beam).
    - `CanBeShipSeizureVictim`: Validiert das Zielschiff (ein anderes Schiff am selben Ort, leer und exposed).
    - `VerifyShipRules`: Umfassender Mini-Test für alle Permutationen, Grenzfälle und Rulings.
- Vereinheitlichung bestehender Karten & Mechaniken:
  - `MovementRules.cs`: `ShipHasSpecialEquipment` prüft neben `Text` auch `ship.Characteristics`.
  - `PlayOnRules.cs`: `Spec` um `TractorBeam` erweitert; `BuildSpecFromClause` erkennt "tractor beam" automatisch in Play-On-Klauseln.
  - `TargetQuery.cs`: `HostFacts` um `HasTractorBeam` erweitert; `MatchPlayOnSpec` prüft `facts.HasTractorBeam`.
  - `TableWindow.xaml.cs`:
    - `IsShipExposed(Border ship)` delegiert direkt an `ShipRules.IsShipExposed(IsShipDocked(ship), IsShipCloaked(ship))`.
    - `CountExposedShips` (*Tachyon Detection Grid*): Prüfte zuvor nur Cloak und ignorierte Docking; nun vereinheitlicht auf `IsShipExposed`.
    - `DefenderExposed` (*Asteroid Sanctuary*): Prüfte zuvor nur Cloak; nun vereinheitlicht auf `IsShipExposed`.
    - `CollectLegalSnapHosts`: Berücksichtigt `ShipRules.CanBeShipSeizureTractorHost` für Halos und Drop-Targets.
    - `HostMatchesInterruptTargetForCard` & `HostMatchesPlayOn`: Nutzen `ShipRules.CanBeShipSeizureTractorHost` bzw. `ShipRules.HasTractorBeam`.
    - `ApplyShipSeizure`: Validiert Tractor-Wirt mit `ShipRules.CanBeShipSeizureTractorHost`, filtert Opfer mit `ShipRules.CanBeShipSeizureVictim` und gibt den Interrupt bei illegalem Ziel oder Abbruch sauber auf die Hand zurück (`ReturnInterruptToHand`). Lokales Duplikat `IsShipSeizureExposed` entfernt.
  - `InterruptRules.cs`: `IsLegalShipSeizureTractor` und `IsLegalShipSeizureVictim` an `ShipRules` angebunden; `VerifyShipSeizureDecide` führt `ShipRules.VerifyShipRules` aus.
- Tests:
  - `ShipRules.VerifyShipRules` und `InterruptRules.VerifyShipSeizureDecide` erfolgreich ausgeführt (PASS).
  - Projekt erfolgreich gebaut (`dotnet build`, 0 Fehler).

## 2026-09-25 — Particle Fountain (132 C)

- Feature: Premiere-Interrupt *Particle Fountain* (132 C) implementiert.
- Gametext: *"Plays if your Away Team just solved a planet mission. If 2 ENGINEER in Away Team, score points. 5"*
- Rulings & Regeln:
  - Trigger: Spielt direkt im Anschluss an das Lösen einer Planeten-Mission durch das eigene Away Team. Nutzt die bestehende `MissionJustSolved` Action-/Response-Pipeline (analog zu *Alien Groupie*).
  - Bedingung: Mindestens 2 ENGINEER im lösenden Away Team erforderlich. Effektive Fertigkeitslevel (`DilemmaRules.CountEffectiveSkill`) berücksichtigen gedruckte Fähigkeiten, Klassifikation und Ausrüstung (z. B. Engineering Kit, Engineering PADD).
  - Effekt: Verleiht dem ausspielenden Spieler sofort 5 Punkte (`_scoreP1 += 5` bzw. `_scoreP2 += 5`), aktualisiert das Scoreboard (`UpdateScoreDisplay()`), loggt das Ereignis und legt die Karte auf den Ablagestapel.
- Implementierung:
  - `InterruptRules.cs`: `IsParticleFountain(Card? c)` und Gate-Validierung `CanPlayParticleFountain(justSolvedPlanet, isOwnSolve, engineerCount)` hinzugefügt; Mini-Test `VerifyParticleFountainDecide` prüft alle Gates und Response-Fälle.
  - `TimingRules.cs`: `Particle Fountain` in `IsCatalogResponse` aufgenommen; `CanRespond` validiert `ActionKind.MissionJustSolved`, eigene Mission, Planeten-Typ und 2 effektive ENGINEER.
  - `TableWindow.xaml.cs`:
    - `ResolveTopOfStack`: Erkennt `IsParticleFountain` als Stack-Response und leitet an `TryResolveInterruptPlay` weiter.
    - `TryResolveInterruptPlay`: Schreibt 5 Punkte für `controller` gut, ruft `UpdateScoreDisplay()` auf und loggt die Wertung.
    - `TryPlayInterruptFromHand`: Erlaubt das Ausspielen sowohl als direkte Stack-Response als auch während des offenen Just-Solved-Fensters mit 2-ENGINEER-Prüfung.
    - `OpenMissionJustSolvedResponse` & `ArmJustSolvedPlanet`: Berücksichtigen auch Equipment-Karten im Away Team für `CountEffectiveSkill`.
- Mini-Test: `VerifyParticleFountainDecide` erfolgreich ausgeführt (PASS).

## 2026-09-24 — Near-Warp Transport (130 U) UI & Beaming-Mechanik-Refactoring

- UX/Mechanik: Interaktions-Flow für *Near-Warp Transport* (130 U) auf die reguläre Beam-Mechanik umgestellt:
  - Verwendet nun dieselbe Ansicht wie die normale Beam-Mechanik: Schiffsdetail-Overlay im Beam-Auswahlmodus (`ShowHostContents(shipB, sc, beamSelectMode: true)`).
  - Crew und Equipment an Bord des Schiffes werden mit Checkboxen angezeigt (`_hostStripBeam = true`, `_beamSelected`).
  - Karten können durch Klick auf die Checkbox oder direkt durch Klick auf die Mini-Karte an-/abgewählt werden.
  - Begrenzung auf maximal 6 Karten (gemäß Kartentext "up to six cards"): Bei Auswahl von mehr als 6 Karten wird die Auswahl verhindert und ein Hinweisdialog angezeigt.
  - Button "Select max (6)" / "Select none" im Detailfenster zur schnellen Auswahl.
  - Schließen-Button im Detailfenster zeigt während Near-Warp Transport `"Beam Crew"` an; erfordert mindestens 1 ausgewählte Karte und schließt das Fenster für die Zielauswahl auf der Spaceline.
  - Alle legalen Ziele auf benachbarten Spaceline-Locations (eigene Schiffe/Einrichtungen und Planetenoberflächen) werden simultan mit grünem Halo hervorgehoben.
  - Zielwahl erfolgt direkt per Klick auf das hervorgehobene Ziel auf der Spaceline; Klick auf das Ausgangsschiff öffnet das Detailfenster zur Anpassung erneut; Rechtsklick/Leerklick bricht ab und nimmt die Karte zurück auf die Hand.
  - Transport prüft Spaceline-Adjazenz, Verbot von Beamen ins freie All (7.1.1.0.1), Hindernisse (`CanBeamAtMission`), Verträge (`TreatyRules.CanOccupyHost`), Hologramme (`FilterHoloBeamAllowed`), heilt Dilemmata und aktualisiert Badges, Quarantäne und Logs.

## 2026-09-24 — Fix: Compiler-Kompatibilität DetailStatusRules, EventRules & TableWindow (Loss of Orbital Stability)

- Bugfix (Build/Compiler): Visual Studio meldete 8 Compilerfehler beim Kompilieren von `TableWindow.xaml.cs`:
  - `"DetailStatusRules" enthält keine Definition für "IsDebuff"` (2x)
  - `"EventRules.Persist" enthält keine Definition für "LossOfOrbitalStability"` (2x)
  - `Keine Überladung für die ToneForEvent-Methode nimmt 3 Argumente an` (4x)
- Root Cause:
  - `TableWindow.xaml.cs` rief `DetailStatusRules.IsDebuff` und `DetailStatusRules.ToneForEvent(ae.Kind, ae.Countdown, ae.Card)` mit 3 Argumenten auf und setzte `Kind = EventRules.Persist.LossOfOrbitalStability`.
  - Wenn `TableWindow.xaml.cs` gegen die Standardversion von `DetailStatusRules.cs` und `EventRules.cs` kompiliert wurde (wo `LossOfOrbitalStability` als Interrupt nicht in `EventRules.Persist` existiert und `ToneForEvent` 2 Argumente hat), schlug der Build fehl.
- Fix:
  - `TableWindow.xaml.cs`:
    - Eigene private Hilfsmethoden `ToneForAttachedEvent(ae)` und `IsAttachedEffectDebuff(ae)` eingeführt, die `Loss of Orbital Stability` direkt über `InterruptRules.IsLossOfOrbitalStability(ae.Card)` erkennen und für Events den standardmäßigen 2-Argument-Aufruf `DetailStatusRules.ToneForEvent(ae.Kind, ae.Countdown)` bzw. `DetailStatusRules.ToneForEvent(ae.Kind, 0)` verwenden.
    - `ApplyLossOfOrbitalStability`: Verwendet wieder `Kind = EventRules.Persist.None` wie auf `master`.
    - `FormatAttachedHostEffectLine`: Formatiert die Zusammenfassung für `Loss of Orbital Stability` direkt ohne Abhängigkeit von `EventRules.Persist.LossOfOrbitalStability`.
  - `DetailStatusRules.cs`:
    - Beide Überladungen von `ToneForEvent` bereitgestellt: `(EventRules.Persist kind, int countdown)` (2 Argumente) sowie `(EventRules.Persist kind, int countdown, Card? card)` (3 Argumente).
    - `IsDebuff(EventRules.Persist kind)` und `IsDebuff(EventRules.Persist kind, Card? card = null)` bereitgestellt.
    - Abhängigkeit von `EventRules.Persist.LossOfOrbitalStability` entfernt.
    - `VerifyLossOfOrbitalStabilityNegative`: Verwendet `EventRules.Persist.None`.
- Ergebnis: Saubere Kompatibilität sowohl mit altem als auch neuem `DetailStatusRules`/`EventRules`, 0 Compilerfehler.

## 2026-09-24 — Near-Warp Transport (130 U)

- Feature: Premiere-Interrupt *Near-Warp Transport* (130 U) implementiert.
- Gametext: *"Plays to beam up to six cards (personnel and/or [Equipment]) from your exposed ship with transporters to an adjacent spaceline location (if possible)."*
- Rulings & Regeln:
  - *Glossary: exposed*: Ein Schiff ist exposed, wenn es ungedockt (`!IsShipDocked`), nicht getarnt (`!IsShipCloaked`), unphased und nicht gelandet/getragen ist. `IsShipExposed` in `TableWindow.xaml.cs` prüft nun sauber auf `!IsShipCloaked(ship) && !IsShipDocked(ship)`.
  - *Glossary: adjacent*: Zwei Spaceline-Locations sind benachbart, wenn keine andere Location zwischen ihnen liegt — auch wenn eine Nicht-Location-Karte wie Q-Net dazwischen liegt. `GetAdjacentSpacelineLocations` filtert Spaceline-Span-Barrieren heraus.
  - *Rulebook 7.1.1.0.2 Card-Activated Transport*: Q-Net blockiert Near-Warp Transport nicht, Hindernisse für Beaming (z. B. Distortion Field, Atmospheric Ionization) gelten jedoch weiterhin und werden über `CanBeamAtMission` geprüft.
  - *Rulebook 7.1.1.0.1*: Beamen ins freie All an Space-Missionen ist verboten; an Space-Locations wird ein eigenes Schiff oder eine eigene Station als Ziel verlangt.
- Implementierung:
  - `InterruptRules.cs`: `IsNearWarpTransport` hinzugefügt; `GetPlayTarget` liefert `PlayTarget.OwnShip`; `Resolve` mappt auf `Kind.Instant`, `Effect.NearWarp`, `DiscardAfter: true`.
  - `InterruptShipEffectRules.cs`: `NearWarpTransportDeny` mit Validierung für Schiff, Eignerschaft, Exposed-Status, Transporter, beamfähige Crew/Equipment und benachbarte Spaceline-Locations im selben Quadranten; Mini-Test `VerifyNearWarpTransportDecide`.
  - `TargetQuery.cs`: `CanPlayOn` validiert `facts.IsShip`, `facts.Owner == player` und `facts.Exposed`.
  - `TableWindow.xaml.cs`:
    - `HostMatchesInterruptTargetForCard` und `CollectLegalSnapHosts` filtern auf eigene exposed Schiffe.
    - `ExecuteInterruptAction`: Behandelt `Effect.NearWarp` via `ApplyNearWarpTransport`.
    - `ApplyNearWarpTransport`: Führt Kartenauswahl (bis zu 6 Personnel/Equipment), Wahl der benachbarten Spaceline-Location (links/rechts Dialog bei Verzweigung), Wahl des Ziel-Hosts (Planetenoberfläche oder eigenes Schiff/Einrichtung), Treaty- und Holo-Checks durch, führt den Transport durch und aktualisiert Badges, Visuals und Logs.
- Smoke: `GROK_TEMP/SMOKE_NEAR_WARP_TRANSPORT.md`. Tracker `working`.

## 2026-09-24 — Loss of Orbital Stability (129 C) Debuff/Negative-Fix

- Bugfix (UX/Classification): *Loss of Orbital Stability* wurde nach dem Anheften an ein Schiff im Schiffsdetail fälschlicherweise als "Positive" mit grünem Label und als "Event" angezeigt.
- Root Cause:
  - `DetailStatusRules.ToneForEvent` lieferte bei `countdown > 0` pauschal `DetailStatusTone.Timer`, und für `Persist.None` `DetailStatusTone.Info`.
  - In `TableWindow.xaml.cs` filterte `positiveEvents` auf `!= DetailStatusTone.Debuff`, wodurch der zerstörerische Interrupt unter "Positive" einsortiert und mit dem Präfix "Event" versehen wurde.
- Fix:
  - `EventRules.cs`: `EventRules.Persist.LossOfOrbitalStability` hinzugefügt und in `FormatHostEffectSummary` als `"ship has NO RANGE; destroyed at end of owner's next turn"` definiert.
  - `DetailStatusRules.cs`: `IsDebuff` eingeführt, welches `LossOfOrbitalStability` sowie alle schädlichen persistierenden Effekte (`PlasmaFire`, `WarpCore`, `Baryon`, etc.) und per Card-Name erkennt. `ToneForEvent` priorisiert `IsDebuff` (liefert `DetailStatusTone.Debuff` / rot auch bei Countdown).
  - `TableWindow.xaml.cs`:
    - `positiveEvents` und `negativeEvents` trennen sauber nach `DetailStatusRules.IsDebuff`.
    - Mini-Karten und Statuszeilen erkennen `Interrupt` (zeigen `"Interrupt — countdown 1"` bzw. `"Interrupt (debuff)"` statt `"Event"`).
    - `FormatAttachedHostEffectLine` unterstützt Interrupts und formatiert die Effektzusammenfassung.
    - `ApplyLossOfOrbitalStability` setzt `Kind = EventRules.Persist.LossOfOrbitalStability`.
  - Tests: `DetailStatusRules.VerifyLossOfOrbitalStabilityNegative()` als Regressions-Check hinzugefügt.

## 2026-09-24 — Loss of Orbital Stability (129 C) Target-Fix

- Bugfix (Targeting): *Loss of Orbital Stability* gab fälschlicherweise eine Planeten-Mission als Snap/Target an anstatt ein Schiff.
- Root Cause: In `PlayOnRules.BuildSpecFromClause` wurde `planet`/`[p]` vor `ship` geprüft, wodurch Clauses wie `"a ship orbiting a [p]"` zu `Host.PlanetMission` statt `Host.Ship` evaluierten.
- Fix:
  - `PlayOnRules.cs`: `c.Contains("ship")` vor `c.Contains("planet") || c.Contains("[p]")` priorisiert, sodass Schiffe mit Ortsangaben als `Host.Ship` geparst werden.
  - `InterruptRules.cs`: `GetPlayTarget` liefert für *Loss of Orbital Stability* `PlayTarget.AnyShip`.
  - `TargetQuery.cs`: `CanPlayOn` verifiziert für *Loss of Orbital Stability* `facts.IsShip` und `facts.IsOrbitingPlanet`. `HostFacts` um `IsOrbitingPlanet` erweitert.
  - `TableWindow.xaml.cs`: `FactsFor` und `FactsForPrinted` berechnen `IsOrbitingPlanet`. `HostMatchesInterruptTargetForCard` und `CollectLegalSnapHosts` prüfen `IsShipOrbitingPlanet`.
- Smoke: `GROK_TEMP/SMOKE_LOSS_OF_ORBITAL_STABILITY.md` aktualisiert. Tracker `partial`.

## 2026-09-24 — Loss of Orbital Stability (129 C)

- Feature: Premiere-Interrupt *Loss of Orbital Stability* (129 C) implementiert.
- Bedingung: Spielt auf ein Schiff im Orbit eines Planeten [P] (Glossary "in orbit": im Weltall, ungedockt, an einer Planeten-Mission; `InterruptShipEffectRules.LossOfOrbitalStabilityDeny`).
- Soforteffekt: Zielschiff hat für den restlichen Zug keine Reichweite (`SetShipRangeLeft = 0`).
- Schilde-Prüfung:
  - Falls effektive SHIELDS > 4: Interrupt wird sofort nach Reichweitenverlust auf den Discard gelegt.
  - Falls effektive SHIELDS <= 4: Interrupt wird an das Schiff angehängt (`_attachedEvents`), und das Schiff wird am Ende des nächsten Zuges seines Eigners zerstört (`TimingRules.TurnScope.SpecificPlayerNextTurn` / `TurnPhasePoint.EndOfTurn` via `ProcessEndOfTurnEvents` / `DestroyShipOrFacility`).
- Smoke: `GROK_TEMP/SMOKE_LOSS_OF_ORBITAL_STABILITY.md`. Tracker `partial`. Kein Push.

## 2026-09-24 — Klingon Right of Vengeance & Life-form Scan working (Pepsch green)

- *Klingon Right of Vengeance* (126 C) und *Life-form Scan* (127 U) im Probespiel verifiziert und grün gemeldet. Tracker auf working gesetzt.

## 2026-09-23 — Klingon Right of Vengeance (§ 7.4.2, § 7.4.4, Klasse B/A)

- Feature: Premiere-Interrupt *Klingon Right of Vengeance* (126 C) implementiert als `JustAfter(PersonnelBattleKlingonDied)`-Response.
- Timing & Trigger: Öffnet sich unmittelbar nach einer Personnel Battle, in der mindestens ein Klingone gestorben und im Discard gelandet ist (Genetronic Save schließt Trigger aus; sequentiell nach ggf. anstehendem Death Yell).
- Effekt: Entstoppt eigene Klingonen am Host (`UnstopBorder`), initiiert unmittelbaren Gegenangriff gegen die überlebenden Kombatanten der Gegenseite ("same opponents"). Bypasst Leader-Pflicht (§ 7.4.1 / `HasLeader`). Verdoppelt STRENGTH aller angreifenden Klingonen für diese Schlacht (`sa *= 2` bei Pairings und Live-STRENGTH).
- Smoke: `GROK_TEMP/SMOKE_KLINGON_RIGHT_OF_VENGEANCE.md`. Tracker partial. Kein Push.

## 2026-09-23 — Death Yell after Escape Pod Pass (ResolveEntireStack drain)

- Root: Plasma/WCB Destroy → Escape Pod ShipDestroyed Pass → ResolveEntireStack while-loop re-entered Destroy Results, TryFlushJustAfterDeathWindows opened JustAfter mid-loop, same loop immediately popped JustAfter as passed (~4838) → Yell UI never stayed open. Without Pod, Destroy runs inline (not under drain) → Yell OK.
- Fix: ResolveEntireStack breaks when a new ActionKind.JustAfter is on top after ResolveTopOfStack (Escape Pod must not eat Yell; sequential 1 Yell/Klingon still works via TryFlush-after-Pass). Order: Destroy → Pod window → unresected die → Death-Yell window; rescue = no death = no Yell.
- Smoke: GROK_TEMP/SMOKE_KLINGON_DEATH_YELL_ESCAPE_POD.md. Tracker stays partial. No push.

## 2026-09-23 — Death Yell: ship/facility destroy→crew (WCB gap)

- Root: `3e140a1` enqueued JustAfter only from `DiscardPersonnelBorder`; `DestroyShipOrFacility` crew wipe discarded without Note → WCB/battle/Plasma/etc. Honor-Klingon deaths missed Death Yell.
- Fix: after Destroy Results, note each dying personnel via `NoteHonorKlingonDeathForJustAfter` (batch `_deferJustAfterDeathFlush`); same for `DiscardShipSeizureVictim`. 1 Yell per Honor Klingon; Escape Pod survivors reloc’d before wipe → no note. `ActionKind.ShipDestroyed` stays Escape-Pod-only.
- Smoke: `GROK_TEMP/SMOKE_KLINGON_DEATH_YELL_WCB.md` (+ inventur `INVENTUR_DEATH_YELL_WCB_GAP.md`). Tracker partial until Pepsch green. No push.

## 2026-09-23 — Klingon Death Yell / shared JustAfter
- Shared `TimingRules.ActionKind.JustAfter` + `JustAfterTrigger` + `IsJustAfter` (SoT/AtStartOfBattle-style gate).
- First consumer: Klingon Death Yell = `JustAfter(KlingonWithHonorDied)`; either player; one Yell per such death; +5 to Yell controller.
- Opens only after actual death/Results (HC/battle batch deferred); Amanda nullify before Results → no trigger.
- No Death-Yell ad-hoc; no Battle Stage-2 misuse.

﻿## 2026-09-23 — Interrupt-Play Responses before Results (HC / Stage 2)

- Root: `BeginPlayCardStack` called `ApplyResponseEffect` (HC kills) before `OpenResponseWindow` — Amanda saw Results already done.
- Fix (shared, not HC-only): Initiation = Push; Responses open; Results in `ResolveTopOfStack` (`ApplyResponseEffect` + `TryResolveInterruptPlay`). Armbands/Hugh pattern; HC kills only via `Effect.HonorChallenge` after nullify window.
- Smoke: `GROK_TEMP/SMOKE_HONOR_CHALLENGE_AT_START_OF_BATTLE.md` (Amanda-before-kill step). Tracker stays partial. No push.
## 2026-09-23 — Honor Challenge (personnel Stage 2)

- Shared gate `TimingRules.IsAtStartOfBattle` / `IsBattleStageResponses` (per battle Stage 2; not SoT/EoT; ETA stays broader).
- Honor Challenge: `CanRespond` only `InitiatePersonnelBattle`; TW apply kills without cancelling battle.
- LegalMoves/EngineAuthority: Respond already via `CanRespond`.
- Smoke: `GROK_TEMP/SMOKE_HONOR_CHALLENGE_AT_START_OF_BATTLE.md`

## 2026-09-22 - Quick Game: Artifacts stay under missions (no spaceline orphans)

- Root: after `be7d6a7`, `AddSeedUnderMission` limit-fail left the card Visible at mission X/SpacelineY → looked like a spaceline node (Thought Maker / Interphase Generator).
- `AutoSeedDilemma` filters with artifact seed limits; sets owner before seed; on fail removes border and leftovers the card.
- `AddSeedUnderMission` returns bool; helpers `CollectSeededUnderMission` / `MissionAllowsArtifactSeed`.
- Smoke: `GROK_TEMP/SMOKE_QUICKGAME_ARTIFACT_SPACELINE.md` (Quick Game x3). No push.

## 2026-09-22 — Multi-artifact earn (AT equipment + mis-seed)

- Solver earns all legal artifacts after planet/space solve; chooses order when several.
- Use-as-Equipment (e.g. Interphase Generator) joins solving Away Team/crew host (not orphaned).
- Duplicate titles under one mission → mis-seed out-of-play; seed limit 1 artifact/player/mission.
- Smoke: `GROK_TEMP/SMOKE_MULTI_ARTIFACT_EARN_2026-09-22.md`. Partial until Pepsch green. No push.

## 2026-09-22 — Pegasus Search OR + {Interphase Generator}

- MissionRules: OR-first requirement groups (7.2.5.0.3); AlternativeMet accepts {CardName} present.
- Pegasus Search solvable with earned IG aboard attempting crew without skill path.
- Smoke: `GROK_TEMP/SMOKE_PEGASUS_SEARCH_IG_2026-09-22.md`. Tracker partial until Pepsch green. No push.

## 2026-09-22 — Atmospheric Ionization scope (planet/vicinity)

- Ionization limit only when origin or dest is planet surface (or landed-ship vicinity).
- Free: Ship↔Ship (orbit), Outpost↔Ship, Space-Facility↔Ship.
- Distortion / Pattern Enhancers unchanged.
- Smoke: `GROK_TEMP/SMOKE_ATMOSPHERIC_IONIZATION_SCOPE_2026-09-22.md`. Tracker stays partial until Pepsch green. No push.

## 2026-09-22 — ETA escapees owner + no battle-stop

- `CompleteBeamTo`: `SetBorderOwner(beamWho)`; track `_etaEscapeeBorders`.
- `StopCrewOnHost`: skip ETA escapees; StackOnHost only remaining aboard (7.4.3 / 10.2.1).
- `BeginBeamMode` error text distinguishes empty/owner/stopped.
- Smoke: `GROK_TEMP/SMOKE_ETA_ESCAPEE_UNSTOPPED_2026-09-22.md`. No push.

## 2026-09-22 — ETA T96 selection hold + battle defer

- `CompleteBeamTo`: keep checkbox selection; filter with `_beamModePlayer` / StackOnHost+GetCrewOnShip; Card-match fallback after detail refresh.
- Ship battle `AskReturnFireAndResolve` deferred while `_etaBeamHoldsBattle` / BeamPickTarget; resume after beam complete/cancel.
- Smoke: `GROK_TEMP/SMOKE_ETA_T96_SELECTION_2026-09-22.md`. No stop bypass. No push.

## 2026-09-22 — ETA T94 Force-Host / beamPlayer

- `IsBeamableFromHost`: ownership vs ETA controller / `_beamModePlayer` (not only `_activePlayer`) — fixes defender ETA in opponent turn.
- `HostHasBeamablePersonnel` / `BeginBeamMode`: `GetCrewOnShip` for facility/ship BoardStore crew.
- `ResolveArmbandsBeamHost`: controller force Ship|Facility; never opponent (T94 Khazara).
- Smoke: `GROK_TEMP/SMOKE_ETA_T94_FORCE_HOST_2026-09-22.md`. No stop bypass. No push.

## 2026-09-22 — ETA Armbands Host/Crew (Facility + Load)

- `ResolveArmbandsBeamHost`: Facility/Outpost-Battle ohne Crew-Stack → Spieler-Schiff an derselben Mission mit beambarer Crew.
- `BeginBeamMode`: Crew über `StackOnHost` (Load/SameHostShip), nicht nur Dictionary-Key.
- Kein Stop-Bypass (Spock). Smoke: `GROK_TEMP/SMOKE_ETA_NO_CREW_HOST_2026-09-22.md`. Kein Push.

## 2026-09-21 — ETA Armbands Beam-Destination (response → BeginBeamMode)

- Response resolve: Emergency Transporter Armbands enters `TryResolveInterruptPlay` / EmergencyBeam even without TargetCard (was SendCardTo-only → no picker).
- `BeginBeamMode` destination filter uses `beamPlayer` (not `_activePlayer`) so ETA as non-active still lists own same-location targets.
- Search comments Rule 7.1.1 / 7.1.1.0.2 / 7.4.2 / 10.2.1; Glossary ETA · equipment · battle; Verb BeginBeamMode · Beam · CanRespond.
- Smoke: `GROK_TEMP/SMOKE_ETA_ARMBANDS_BEAM_DEST_2026-09-21.md`. Partial-escape stop-status parked. Kurlan/FPS untouched. No push.

## 2026-09-21 — Kurlan ×3 S.A.M. (printed+Adds)×3

- `BattleRules.AttributeAfterSam` / `AttributeBonusOverPrinted`: Rulebook §12.11 S.A.M. — (printed + adds) × Kurlan, not printed×k + adds.
- Ship battle Open Fire / Return Fire / predict bonuses use same Decide helper as UI `FormatShipEffectiveLine`.
- Verify asserts (8+3)×3=33. Types route bfe0de9 untouched. Smoke: `GROK_TEMP/SMOKE_KURLAN_SAM_2026-09-21.md`. No push.

## 2026-09-21 — Classification vs Skill Route (Kurlan Naiskos)

- Shared Decide helper `MissionRules.PersonnelTypePresent` / `RequiresClassificationOnly`: without the word classification → Class box OR effective skill (incl. equipment grants via `EventRules.HasSkill`); with classification → Class box only.
- `ArtifactRules.KurlanFullyStaffed` uses that route (seven personnel types); one personnel may cover two types (Class+Skill).
- `BattleRules.VerifyKurlanMultiplier` covers dual Class+Skill and Medical Kit MEDICAL grant.
- Search comments Rule 10.1.0.1 / 10.1 / 10.3.0.5 / 2.7 / 2.8; Glossary personnel type · classification · skills · use (skills|equipment).
- Smoke: `GROK_TEMP/SMOKE_KURLAN_CLASS_SKILL_2026-09-21.md`. No push. FPS untouched. Continuum/Q parked. Plays on/as F3 unchanged.

## 2026-09-21 — Docs: IMPLEMENT statt RULES/CODE_PLACEMENT

- Neue Canon-Trennung: `IMPLEMENT.md` (Ablauf), `ENGINE.md` + `TABLEWINDOW_INVENTORY.md` (Ist-Landkarte), Status nur in Seven/Jadzia/Extract, Log in Changelog, Brücke in Handoff.
- `RULES.md` und `CODE_PLACEMENT.md` entfernt. Klasse A/B/C entfällt.

## 2026-09-17 — Hail (AU) + table UI chrome

## 2026-09-18 — TwoDim: full Disabled for Empathy aboard (Spock)

- Upgrade from Empathy skill-strip: Empathy personnel are Disabled while aboard TwoDim ship (live; clears when beamed off).
- Disabled may beam (Glossary); IsBeamableFromHost no longer blocks Disabled.
- Cure ENG+SCI + move-block unchanged. Visuals sync on attach/beam/cure.


## 2026-09-18 — Two-Dimensional Creatures: Empathy disabled

- While TwoDim persist on ship: Empathy stripped in ResolvePersonnel (Detail skills, Contents team sum, dilemma Skill(), CanSolve).
- Move-block unchanged. Cure ENGINEER+SCIENCE unchanged. Detail debuff line when printed Empathy present.


## 2026-09-18 — FINAL: IG after Resolve (Spock Glossary)

- Pepsch decided strict: reveal → Resolve (targets+conditions) → optional IG Yes/No → else effects.
- Reverted house UX (4cbac3d). May-nullify + IG kept + [IPG] icon path unchanged. No further order flips.


## 2026-09-18 — HOLD: keep house UX IG pre-Resolve

- Restored Pepsch house UX (IG Yes/No before Resolve for all [IPG]); undid Glossary revert 17f468e.
- No further IG-order changes until Pepsch picks strict vs house. Spock still flags Glossary conflict.


## 2026-09-18 — HOLD: IG back to post-Resolve (Glossary)

- Reverted Pepsch pre-filter IG order (d32da9c). Strict Spock: Resolve (targets+conditions) → optional IG → else effects. Waiting Pepsch: strict vs house UX.


## 2026-09-18 — IG nullify BEFORE Resolve for all [IPG]

- Pepsch: Interphase Generator Yes/No runs once at start of encounter handling for every IsIpgDilemma, before DilemmaRules.Resolve (before Rebel destroy-Equipment, filters, kills). Yes = discard dilemma + continue, IG kept. No = normal resolve.


## 2026-09-18 — Mission badge strip + Rebel Encounter destroy-Equipment

- Mission under-card status strip (Away/Eq/Art counts) removed; ship/facility badges unchanged; Rogue Borg badge kept.
- Rebel Encounter: when STRENGTH not >44, offer destroy one Equipment present (Equipment type OR Artifact-as-Equipment via IsEquipmentCard). Destroy uses Discard so Overcome applies it. CollectPresentAtMission now includes Artifact-as-Equipment.


## 2026-09-18 — Mission detail: revealed-still-under (no last-revealed)

- Removed "Last revealed under Mission" (could show discarded cards).
- Detail shows all cards revealed under that mission that are still on the under-mission seed pile (visible to all). Face-down count excludes those.


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


## 2026-09-21 — StartOfTurnWindow (Phrase) + Full Planet Scan Gate
- Neu: `TimingRules.RequiresStartOfTurnWindow` / `IsStartOfTurnWindowOpen` / `CanPlayStartOfTurnCard` (Compendium 6.1 Exception; Segmente SoT→NormalPlay→Execute→EoT→Draw).
- Gate vor Stack/Responses: `LegalMoves.AddHandPlays`, `EngineAuthority.EvaluatePlay`, `TableWindow.TryAllowHandPlay`.
- Full Planet Scan = erster Phrase-Consumer (Gametext + Katalog); Apply-Pfad nur Safety-Net.
- Pepsch-Fail: FPS nach Normal-Play öffnete Amanda-Fenster; Illegal kam zu spät → jetzt sofort Deny.

