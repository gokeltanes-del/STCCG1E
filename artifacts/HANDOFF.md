# STCCG 1E - Handoff

Last updated: 2026-09-13 (Captain — local commits; Pepsch pushes)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: `master`.** GrokTest nicht nutzen.
**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Gruen-Test. Pepsch: VS master, lokale Tips testen, dann Push.

## Current tip (2026-09-13)
Save/Load Game State & Ship Damage Fix:
- `BoardStore.Current.Clear()` am Anfang von `ApplyGameSave`: Verhindert, dass veraltete Schiffs- und Personaldaten aus früheren Sessions in geladenen Spielständen weiterleben.
- Schiffe mit `snap.Hull <= 0` setzen `SetHullDamagePercent(border, 0)` und `UpdateDamageBadge(border, 0)` explizit zurück, sodass keine veralteten Badges oder Schadenswerte fälschlicherweise angezeigt werden.
- `SyncBoardFromTable(logDual: false)` am Ende von `ApplyGameSave`: Synchronisiert den `BoardStore` nach dem Laden sauber mit dem rekonstruierten Tisch-Zustand.
- `SaveGame`: `Hull` und `RangeLeft` erfassen über `GetHullDamage(b)` und `GetRemainingRange(b, card)` direkt die echten Instanz-Werte.

Ktarian Game Dilemma Disabling & Cure (Glossary & Rules):
- Permanenter "Disabled"-Status für Ktarian Game implementiert (Kartentext: "Now and start of each turn, one personnel aboard (random selection) is disabled. Cure with CUNNING>30 OR any android."):
  - Deaktiviertes Personal kann nicht beamen, trägt nicht zu Schiffsbemannung (`MovementRules.IsShipStaffed`) bei und zählt nicht bei Missionsversuchen oder Heilungsanforderungen.
  - Beim Encounter ("Now") wird direkt eine zufällige Person an Bord deaktiviert.
  - Zu Beginn jedes Zuges des Schiffsbesitzers ("Start of Turn") wird vorab geprüft, ob nicht-deaktiviertes Personal das Dilemma heilen kann (CUNNING>30 oder Android). Falls nicht, wird eine weitere Person deaktiviert.
  - Bei Heilung (`ClearStasisForDilemma`) werden alle betroffenen Personen sofort wieder befreit und reaktiviert.
  - Save/Load (`GameSave.cs`): `AttachedDilemmaSnap.HeldIds` serialisiert betroffene Karten, sodass der Zustand auch nach Laden erhalten bleibt.
  - Unit-Tests: `DilemmaRules.VerifyKtarianGame` und `DilemmaCureRules.VerifyDilemmaCureRules` aktualisiert und erweitert.

Portal Guard & Hyper-Aging Quarantäne Interaktion (DRG & Glossary):
- Away Teams unter Quarantäne (Hyper-Aging) oder in Stasis können nicht gebeamt werden (`IsCardLeaveBlocked`).
- Flag-Kapselung: `Quarantined`, `InStasis` und `IsLeaveBlocked` direkt an `Card`, `PersonnelInstance` und `Force` (Away Team / Crew).
- `BeamBackAwayTeamToShipOrOutpost` prüft nun vor jedem Beamen, ob Personal unter Quarantäne oder in Stasis steht. Ist dies der Fall oder existiert kein Zielschiff/-außenposten, wird das Beamen abgebrochen.
- Portal Guard Dilemma Regelung:
  - CUNNING>7 oder Honor: Overcome, Dilemma abwerfen, Versuch geht weiter.
  - Fehlschlag + Beamen möglich: Team beamt auf Schiff/Außenposten und wird gestoppt; Dilemma bleibt unter der Mission (`WallFailed`).
  - Fehlschlag + Beamen unmöglich (z.B. durch Quarantäne von Hyper-Aging oder fehlendes Schiff/Außenposten): Das gesamte Away Team wird getötet! Dilemma bleibt unter der Mission (`WallFailed`).
- Unit-Tests in `DilemmaRules.VerifyPortalGuard` erweitert (Pass CUNNING>7, Pass Honor, Fail mit Beam, Fail ohne Beam/Quarantäne mit Tötung des gesamten Teams).

Genetronic Replicator Fix (Glossary & Rules):
- Opfer und gleichzeitig zum Tod ausgewählte Personen (`alsoTargetedToDie`) werden von den 2 geforderten MEDICAL-Punkten ausgeschlossen (Beverly Crusher mit 2 MEDICAL kann sich nicht selbst retten, wenn keine weiteren 2 MEDICAL anwesend sind).
- Gestoppte und in Stasis befindliche Personen können nicht gestoppt werden.
- Spieler-Auswahl via `PickBorderFromList`, wenn mehrere MEDICAL-Optionen existieren.
- Pure Regeln & Unit-Test in `EventRules.cs` (`VerifyGenetronicReplicator`).
- Weitergabe von `alsoTargetedToDie` bei Dilemma-Kills (z.B. Nausicaans, Crystalline Entity), Personnel Battles, Stone of Gol und EOT Kills in `TableWindow.xaml.cs`.

Doppelte "Artifact verdient" Meldung bei Mission Solve entfernt (nur noch "Artifact acquired").
Vollständige Übersetzung aller verbliebenen deutschen Texte in UI (TableWindow, DeckBuilderWindow), Game Rules und Services ins Englische.
Zentrales Dilemma Cure System (Compendium 7.2.2.3: `DilemmaCureRules`, Bedingungen zuerst, dann Cure).
Fix Team-Stop / Curable AttachContinue (7.2.2.3 & 7.2.6): Heilbare Dilemmas ohne Zugangsbedingung (Alien Abduction, Two-Dimensional Creatures, Tsiolkovsky, Frame of Mind) stoppen das Team nicht. Alien Abduction wird bei Cure sofort abgeworfen (volles Team weiter) bzw. verbleibt mit Opfer in Stasis, während das Rest-Team ungestoppt die Mission fortsetzt. Stasis-Release un-stoppt befreite Personen sofort (`UnstopBorder`).
Pepsch green bestätigt: Archer, Alien Abduction (Cure OR + Stasis), Phased Matter (Split + Stasis + Cure unphased).
Response Window Umbau (Stilles Window am Phase-Banner, Think Tray via [R]/Klick, Optionen 2s/3s/5s/10s, Presets Hotseat/Test).
Dateien: `StarTrekCCG/Game/DilemmaCureRules.cs`, `StarTrekCCG/Game/DilemmaRules.cs`, `StarTrekCCG/TableWindow.xaml.cs`.

Artifact Beaming ohne Treaty (`TreatyRules.CanOccupyHost` / `ReportingRules.AreCompatible`).
Pepsch green bestätigt: Hyper-Aging, Firestorm Kills + Continue, Overlay `EFFECT - attempt continues`, AT-Detail Debuff-Gruppierung, Varon-T Looten.

### ACTIVE
Premiere-Dilemmas einzeln. Persist/Battle extract deferred.

### Pending Pepsch
- Test Response Window UX (Silent Badge am Banner, [R] Think Tray, [Space] Pass, Optionen/Presets)
- Test Artifact Beaming (Varon-T vom Planeten auf Schiff beamen ohne Treaty-Fehlermeldung)

### Parked
Hugh Borg Ship; IM FindMission; dump@Gaps; Distortion; Parasites Hotseat-UI; REM Fatigue; Cure-Present-Scope Ship

## Workflow Pepsch
1. Unten links **master**
2. Git → Pull
3. Rebuild / starten
4. 3 lokale ungecommitete Dateien: nicht verwerfen wenn du sie brauchst; Pull kann Konflikte in Docs machen

## Docs map
HANDOFF · PROJECT · ENGINE · CODE_PLACEMENT · FEATURES · CARD_TRACKER · CHANGELOG
