## 2026-09-29 - AttemptMission: host mirror closes; Scow/Borg on encounterer side

- *Auftrag*: Pepsch Dual-EXE. P2 trifft Radioactive Garbage Scow. Der Host-Spiegel "FAILED - attempt ends / Guest acknowledges" bleibt haengen. Zusaetzlich ein Schatten der Dilemma-Karte auf der anderen Spaceline-Seite, und auf P1 an der falschen Stelle. Scow und Borg Ship gehoeren auf die Seite des Spielers, der sie getroffen hat, auf beiden Fenstern, nur einmal. Shared Faces, Mission-solved beide, kein Seed-Fly-in. Hotseat-Layout unveraendert.
- *Ist vorher*: Host-Watcher ging nur ueber `HideHostEncounterMirror` zu, und das brach ab, solange ein Choice-Frame lief. Die letzte Karte hatte keinen Nachfolger. Der Token wurde immer unter die Mission (P1-Seite) gesetzt und zusaetzlich als Table-Snap mitgesendet. Guest baute daraus eine zweite Karte auf der Gegenseite.
- *Fix*: Guest-Ack ruft `ForceHideWatcherMirror` (sofort in der ChoiceResponse und im finally). Dasselbe am Attempt-Ende und vor dem Broadcast, wenn der Watcher noch offen ist. Klickbares OK/Yes/No bleibt. Token-Seite im Netz = `EncounteredBy` via `DockSlotOffsetY` (Hotseat weiter unter der Mission). Token nicht in `save.Table`. `RemoveStrayDilemmaCardBorder` auch fuer Scow.
- *Scope*: TableWindow.xaml.cs, GameSave.cs. Kein Push master.

## 2026-09-29 - AttemptMission: Mission solved on both windows

- *Auftrag*: Mission-solved-Dialog, der auf dem Loeser stimmt, soll wie Dilemma/Artifact auf beiden Fenstern stehen. Text nennt weiter den Loeser. Punkte nicht doppelt. Tag bleibt. Kein Seed-Fly-in. Guest-Spiegel schliesst weiter wie 09b6747. Hotseat unveraendert.
- *Ist vorher*: `IsEncounterRevealCard` galt nur fuer Dilemma/Artifact. "Mission solved" ging nur an den Loeser (`surfacePlayer`). Der andere sah den Dialog nicht.
- *Fix*: Dieselbe Spiegel-Regel. Loeser hat OK. Der andere sieht `ShowHostEncounterMirror` / `revealMirror` ohne Buttons ("Guest/Host acknowledges") und das schliesst mit der bestehenden Close-Strecke (Guest-Antwort → Host-Hide, bzw. awaited `revealMirrorClose` plus Hide beim maskierten Apply). Kein zweites `MarkMissionSolved`.
- *Scope*: TableWindow.xaml.cs. Kein Push master.

## 2026-09-29 - AttemptMission: guest encounter mirror closes

- *Auftrag*: Pepsch Dual-EXE. P1 (Host) loest: P2 sieht die Encounter-Faces. Die letzte Meldung bleibt auf P2 ("Artifact acquired / Kurlen Nimbus / Both players see this card. Host acknowledges."), auch nachdem der Versuch vorbei ist und P2 am Zug ist. Kein Dismiss. Spiegel zu, wenn der Host bestaetigt oder der Versuch endet. Ein spaeterer maskierter Broadcast darf den Dialog nicht stehen lassen. Shared Faces bleiben. Hotseat unveraendert.
- *Ist vorher*: Zwischenkarten wirkten geschlossen, weil das naechste `revealMirror` dasselbe Overlay ueberschrieb. Die letzte Karte hat keinen Nachfolger. `revealMirrorClose` war fire-and-forget. `ApplyGameSave` fasst `CardRevealOverlay` nicht an.
- *Fix*: Host wartet `revealMirrorClose`, wenn er OK klickt (`ShowCardReveal` finally). Vor dem maskierten Broadcast schickt der Host dasselbe Close noch einmal, falls das Spiegel-Flag noch steht, und wartet bis es auf der Leitung ist. Guest `OnNetStateReceived` ruft `HideHostEncounterMirror` vor und nach `ApplyGameSave` (kein interaktives Reveal, kein OK/Yes/No). `revealMirror` bleibt. Hotseat sendet kein Mirror.
- *Scope*: TableWindow.xaml.cs. Kein Push master.

## 2026-09-29 - AttemptMission: solved dialog + encounter both ways

- *Auftrag*: Seed-Fly-in fuer Dilemma/Artifact weg (Play-Fly-in bleibt). Mission-solved-Dialog auf das Fenster des Loesers, Text nennt den Loeser. Encounter-Faces in beide Richtungen, sobald die Karte wirklich aufgedeckt ist.
- *Ist vorher*: Fly-in von der Missions-Spalte. Dialog las `_activePlayer` beim Anzeigen. Nach einem EOT mitten im Modal (Banner danach P1 PLAY) stand "Player 1" auf dem Host, obwohl die Punkte an P2 gingen. Guest sah den Dialog nicht. Host-Versuch zeigte Encounters nur lokal; `kind=reveal` ging nur an den Guest, wenn der Guest klickt.
- *Fix*: Solver wird bei `MarkMissionSolved` festgehalten und als `surfacePlayer` geroutet (nicht der spaetere `_activePlayer`). Guest-Versuch: Dialog auf P2, Text "Player 2". Host-Versuch: Dialog auf P1, Text "Player 1". Tag auf beiden bleibt. Encounter: Guest klickt, Host spiegelt ohne Klick. Host klickt, Guest bekommt `revealMirror` ohne Antwort und `revealMirrorClose` wenn der Host schliesst. Unrevealed Seeds und das Detail (noch aktive revealed Dilemmas + unacquired Artifacts) unveraendert.
- *Scope*: TableWindow.xaml.cs, NetChoiceDto.cs. Hotseat ohne Fly-in und ohne Netz-Mirror. Kein Push master.

## 2026-09-29 - AttemptMission: both players see encountered cards

- *Auftrag*: P2-Versuch: Dilemma- und Artifact-Faces auf beiden Fenstern, sobald sie wirklich aufgedeckt sind. Solved-Tag auch auf P2. Missions-Detail zeigt noch aktive aufgedeckte Dilemmas und Artifacts fuer beide. Unrevealed Seeds bleiben verdeckt. Fly-in von der Missions-Spalte in die Mitte, auf dem Fenster das die Karte zeigt.
- *Ist vorher*: Guest-Reveal (`kind=reveal`) zeichnete nur auf P2. Host sah das Face nicht. `MarkMissionSolved` malte den Tag nur im Host-Prozess. `_revealedUnderMission` / `_revealedArtifactsUnderMission` waren RAM und wurden in Apply geleert; `NetStateMask` loeschte jede gegnerische Seed-Identitaet.
- *Fix*: Encounter-Face (Dilemma/Artifact) malt der Host mit, ohne Klick; Guest bestaetigt weiter. `RevealedSeeds` / `RevealedArtifacts` (InstanceIds) in der Save. Mask laesst genau diese Ids stehen. Apply baut die Listen neu und malt `✓ S{player} +{points}` aus `SolvedBy` + `MissionRules.ParsePoints` (kein zweites Punkte-Gutschreiben). Fly-in nutzt `PlayFlyInCard`: Start = Missions-Border, Ende = Bildschirmmitte, kein Seed-Ghost. Guest-Art ist die Katalog-Face (`ClonePrinted`), dieselbe wie das Reveal. Kein Origin-Border → kein Fly-in, das Face bleibt im Panel.
- *Regeln*: Bestehendes Detail (`Revealed — still under mission`, `Found artifacts`, Kommentar face-up for both) ist die Quelle. Keine neue Kartenregel. Eigene unrevealed Seeds bleiben fuer den Owner sichtbar; gegnerische unrevealed Seeds bleiben namenlos.
- *Bleibt auf dem Host, absichtlich*: PickOpp und Alien Parasites wenn P1 entscheidet. The Devil wenn P1 die Karte hat. TTP-Schiffs-Klick ohne Remote-Kanal (Schiff bleibt). Solved-Tag und Detail-Liste auf dem Guest kommen mit dem maskierten Broadcast am Ende des Versuchs (oder ETA-Suspend), nicht mitten im Dialog.
- *Scope*: TableWindow.xaml.cs, GameSave.cs, NetStateMask.cs, NetChoiceDto.cs. Hotseat-Legalitaet unveraendert. Kein Push master.

## 2026-09-29 - AttemptMission UI on the Guest window

- *Auftrag*: Host bleibt die einzige Wahrheit. Wenn P2 versucht, gehoeren Prompts, Reveals und die Wahl des Versuchenden auf das Guest-Fenster. Host oeffnet diese Oberflaeche nicht.
- *Ist vorher*: TryApplyNetAttemptMission rief TryAttemptMission im Host-Prozess auf, inklusive ShowCardReveal / MessageBox. Guest sah nur den spaeteren Broadcast.
- *Fix*: Waehrend eines Guest-Versuchs setzt der Host `_attemptRemoteSurface`. ShowCardReveal und ShowPlayError gehen als ChoiceRequest `kind=reveal` (Katalog-Face, kein Seed-Lesen). Yes/No und Kartenwahl des Versuchenden bleiben ChoiceRequest `kind=choice` (`Name #InstanceId`). Guest zeichnet nur und antwortet. Host wendet weiter an und broadcastet maskiert am Ende. Hotseat unveraendert (`_attemptRemoteSurface` false).
- *Bleibt auf dem Host, absichtlich*: PickOpp (Gegner waehlt) und Alien-Parasites-Wahl, wenn der Entscheider P1 ist. The Devil, wenn P1 die Karte hat (`surfacePlayer`). Time Travel Pod: Yes/No geht an den Guest; der anschliessende Klick auf ein gegnerisches Schiff hat keinen Remote-Kanal und wird nicht auf dem Host geoeffnet (Schiff wird nicht versetzt).
- *Scope*: TableWindow.xaml.cs, NetChoiceDto.cs. Basis AttemptMission-Branch. Kein Push master.

## 2026-09-29 - AttemptMission network (Guest intent, Host apply)

- *Auftrag*: AttemptMission netzfaehig. Guest laeuft den Versuch nicht lokal gegen maskierte Seeds. Host wertet echte Seeds / MissionRules / DilemmaRules ueber denselben TryAttemptMission-Pfad wie Hotseat. Danach maskierter Broadcast. Yes/No im Dilemma ueber AskChoiceForPlayer. Kein PickCardFromList-Refactor, kein PersonnelBattle, keine KI, keine Phase-5.
- *Fix*: GameAction.AttemptMission(player, mission, attemptingShip?). NetActionDto InstanceIds bleiben [card, target, target2]. IsNetSyncKindSupported + TryApplyNetAttemptMission. Guest SendGuestActionAsync und return. SEARCH: Verb: attempt-mission; Rule: 7.2.
- *Scope*: GameAction.cs, NetActionDto.cs, EngineAuthority.cs, TableWindow.xaml.cs. Basis master 8083785 + Josef docs f044338. Karten nicht auf working. Kein Push master.
## 2026-09-28 - Spaceline Insert: Q-Net/Gaps as columns (Dual-EXE)

- Q-Net und Gaps sind eigene `_spacelineOrder`-Spalten zwischen den Endpunkt-Missionen (Mission | Span | Mission), nicht nur Gap-Mid-Overlay.
- `InsertSpanColumn` / `EnsureSpanColumnsInOrder`. `GetSpacelineColumns` = nur Mission/Time/Pod als Endpunkte.
- `NetSpacelineSnapshot.ColumnInstanceIds` = volle Spaltenfolge; `MissionIds` ohne Spans; Guest Apply aus Snapshot, kein Canvas-X.
- Scope: `TableWindow.xaml.cs`, `GameSave.cs`. Tip-Hash `8083785`. Basis `b4aca0e`. Smoke Dual-EXE HOLD.



- PlayCard lÃ¶st nur aus der Hand des handelnden Spielers bzw. frischem Seed, nie Ã¼ber Table-Name (#268).
- Host-Wahrheit: `MissionIds` + `Spans[{SpanInstanceId,Kind,Left,Right}]`. Guest Apply nur aus Snapshot.
- `DumpSpacelineTruth` bleibt (`STCCG_DUMP_SPACELINE=1`). AskChoice-Pfade `ab2e67a` unverÃ¤ndert.
- Scope: TableWindow + Network. Tip-Hash `b4aca0e`. Basis `563d3eb`.

## 2026-09-28 - Dual-EXE spaceline Hostâ†”Guest Capture/Apply (SpacelineInstanceIds)

- Mission-Spalten-InstanceIds Hostâ†’Guest rund; Spans bleiben Overlay.
- AskChoiceForPlayer / RunKidnappers / PickHandCardToDiscard unverÃ¤ndert (`ab2e67a`).
- Tip-Hash `563d3eb`. Basis `2d7acc7`.

- *Auftrag (FREIGABE Option B)*: Analyse `GROK_TEMP/spaceline-span-sync-analysis.md` befolgen - eine Wahrheit: Order=Missionen; Spans=Overlay Endpoints; Capture ohne Spans; Relayout+PaintSpans; Seed/Index mission-only; BoardStore aus Paar. Kein Hybrid. TAK ab2e67a / Overlay a9af295 unangetastet. Kein Refactor C.
- *Root cause*: Hybrid seit 5b043b0 - Relayout missions-only Paint, aber PlaceSpan/Pin inserteten Spans weiter in `_spacelineOrder` + `save.Spaceline`; Seed/IndexOfMission span-verseucht; Dual-EXE Float/Desync.
- *Fix*: `SpacelineSpanRecord` + `PaintSpans`/`PurgeSpansFromSpacelineOrder`; PlaceSpan ohne Insert; Capture/Apply Spaceline mission-only; SyncBoard Barriers/Gaps nur aus AttachedEvent-Endpoints; `IndexOfMission`/Gaps/GetValid mission-only; `BuildSpacelineDisplayOrder` entfernt. SEARCH: Verb: plays-on spaceline-span; Glossary: adjacent.
- *Scope*: TableWindow.xaml.cs. Tip-Hash 2d7acc7. Basis 5b043b0 / Docs 24cf19c. Overlay/TAK unberuehrt. Kein Push.
## 2026-09-28 - Q-Net second span no Extra-Width stack + SWB Face choice

- *Auftrag (FREIGABE)*: Tip fef7075 unzureichend - Dual-EXE: zweites Q-Net stackt auf erstem + grosses Horizontal-Loch; SWB Choice Text/schwarz statt Face. Soll: Gap=Mission-InstanceIds; Render Gap-Mid/SpacelineY; Span kein Extra-Width; SWB Face-Strip. TAK ab2e67a / Overlay a9af295 unangetastet.
- *Root cause*: RelayoutMissionsOnSpaceline behandelte Q-Net/Gaps als volle Display-Spalten (TableCardWidth+MissionGap), Pin verschob Barrieren auf Gap-Mid â†’ leeres Loch + optischer Stack; SpanEndpoints konnte Non-Mission-Hosts akzeptieren. SWB AskChoiceLocal erzeugte Fake-Cards Type=Choice ohne FullImagePath â†’ Text/schwarz statt Face.
- *Fix*: Relayout-Spalten nur Missionen/Time; alle Spaceline-Spans overlay Gap-Mid (Pin); AsMissionEndpointBorder (nie Span-auf-Span); PlaceSpan vor rechter Mission. SWB: TryMapChoiceOptionsToHandCards â†’ PickCardFromList Face; Result ShowCardReveal Face. TAK Typ-YesNo unveraendert.
- *Scope*: TableWindow.xaml.cs. Tip-Hash 5b043b0. Basis fef7075 / Docs 53481c3. Overlay/TAK unberuehrt. Kein Push.

## 2026-09-28 - Spaceline span gap = mission InstanceIds + Host dock recover (Q-Net)

- *Auftrag (FREIGABE)*: Tip 6acacd4 unzureichend â€” Dual-EXE Screenshot: RECHTS (P2) 2 Q-Nets Row-Center OK + Facility unter Mission; LINKS (P1) Spaceline horizontal versetzt/clipped, nur 1 Q-Net + purple Highlight, Facility oben-links clipped. Soll: Gap = zwei Mission-InstanceIds; Render Row-Center gleich auf jedem Client. Generisch Spans. TAK ab2e67a / Overlay a9af295 unangetastet.
- *Root cause*: 6acacd4 pinte Span-Y/X und landInst=own, aber (1) CaptureGameSave speicherte Span-Endpoints nur als ephemeral save-local HostId (nicht Mission-InstanceIds); (2) Host-Lokal-Relayout nach PlaceSpan rief EnsureBoardExtents mitten in der Mission-Schleife und pinte Docks nie per Spalte (Guest ApplyGameSave schon via PinDockablesToSpacelineByColumn) â€” orphan Facility top-left â†’ Extents-Shift â†’ Spaceline clipped / 2. Q-Net weg; (3) Fly-in TargetNorm vom Host ist viewer-fenster-relativ.
- *Fix*: AttachedEventSnap HostInstanceId/Host2InstanceId board-absolut; Apply/SpanEndpoints/BuildSpacelineDisplayOrder per InstanceId; EnsureBoardExtents deferred + einmal am Ende; PinDockablesToSpacelineByColumn nach Span-Relayout (Host=Guest); Pin stellt Opacity wieder her; Fly-in ignoriert Host-TargetNorm fuer Spans.
- *Scope*: GameSave.cs + TableWindow.xaml.cs. Tip-Hash fef7075. Basis ab2e67a / Docs 2e20f8e. Overlay a9af295 unberuehrt. Kein Push.
## 2026-09-28 - TAK + SWB Choice via AskChoiceForPlayer (Owner-Fenster)

- *Auftrag (FREIGABE Option A)*: Telepathic Alien Kidnappers Typ-Wahl und Static Warp Bubble Hand-Discard netztauglich ueber AskChoiceForPlayer; Random/Discard/Reveal Host-Engine + Broadcast. Kein Refactor C; Richtung B spaeter Inventar. Overlay a9af295 / Span 6acacd4 unangetastet.
- *Root cause*: RunKidnappers / PickHandCardToDiscard oeffneten KidnapOverlay + Dispatcher.PushFrame lokal auf Host-UI-Thread (EOT nur Host via FinishExecuteAndEndTurn). Owner=P2 (Guest) -> Typ-/Hand-Buttons im P1-Fenster. Phase 4 (AskChoiceForPlayer / ChoiceRequest) existierte, war nicht verdrahtet.
- *Fix*: TAK Typ-Wahl AskChoiceForPlayer(owner, ...); RNG + FinishKidnappers Reveal/Discard Host; ShowCardReveal statt Overlay. SWB Optionen = Owner-Hand Labels/InstanceIds via AskChoiceForPlayer; Host applyt Discard. SEARCH-Kommentare gesetzt.
- *Scope*: TableWindow.xaml.cs. Tip-Hash ab2e67a. Basis 6acacd4 / Docs ba34d1c. Overlay/Span unberuehrt. Kein Push.
## 2026-09-28 - Spaceline span fly-in land + gap adjacency (Q-Net Nachzieher)

- *Auftrag (FREIGABE)*: Tip 0443f37 unzureichend - Dual-EXE Stacks M5/M9 wirken vertikal vertauscht (rechts=Wahrheit); Q-Net optisch im Romulan-Stack statt Span in Spaceline-Luecke. landInst=39 fuer beide Q-Nets. Generisch Gaps/Q-Net/Spans.
- *Root cause*: ResolvePlayFlyInTargetInstanceId lieferte ae.Host / stale _eventPreferredHost (Fly-in landet auf Mission/Facility-Stack); ListSameQuadrantGaps/PickAdjacentMission zaehlten Spans als Endpoints; PinSpacelineSpanCardsY nur Y, fehlte nach RelayoutAllDockables/ScheduleSettle.
- *Fix*: Span landInst = eigene InstanceId; AttachCardToHost via IsSpacelineSpanCard; mission-only Gap-Paare; PickAdjacent laeuft an Spans vorbei; Pin gap-midpoint X fuer Barrieren + nach RelayoutAll/ScheduleSettle.
- *Scope*: TableWindow.xaml.cs. Tip-Hash 6acacd4. Basis 0443f37 / Docs d307c09. Overlay a9af295 unberuehrt. Kein Push.
## 2026-09-28 - Spaceline Span Y always SpacelineY-centered (Q-Net Nachzieher)

- *Auftrag (FREIGABE)*: Tip 3bc7e49 unzureichend â€” Dual-EXE P2 Q-Net: P2 (rechts) vertikal zentriert in LÃ¼cke OK; P1 (links) dÃ¼nner Streifen am oberen Rand / massiver Y-Offset nach oben (Clipping), horizontal OK. Q-Net soll Spaceline-Y-zentriert sein, nicht Owner-Dock oben/unten. Generisch Gaps/Q-Net/Spaceline-Spans.
- *Root cause*: Board-absolute Dock-Y (3bc7e49) reicht nicht fÃ¼r Span-Zeile â€” `IsSpacelineSpanCard` nur ResolvePlay; Apply `FromBoardAbsoluteY(owner)` fÃ¼r Nicht-Row; orphan Spans fehlen in `_spacelineOrder` nach Load; Relayout soft-invalidate orphaned Fly-in-Ghost bei Opacity=0; Host PlaceSpan/Relayout ohne harten SpacelineY-Pin nach Dock/EnsureBoardExtents.
- *Fix*: `IsSpacelineSpanCard` Name-first (Q-Net/Gaps) + `IsSpacelineRowCard`; ApplyGameSave erzwingt `SpacelineY` fÃ¼r Row-Karten (kein Owner-FromBoardAbsoluteY); `PinSpacelineSpanCardsY` nach Relayout/Apply (Top=SpacelineY+Identity, Orphans aus AttachedEvent); PlaceSpan Identity; soft-invalidate restored Ghost Opacity.
- *Scope*: TableWindow.xaml.cs. Tip-Hash 0443f37. Basis a9af295 / Docs 3be0892. Overlay unberÃ¼hrt. Kein Push.

## 2026-09-28 - Responsive Detail/Choice-Overlays (Fensterbreite)

- *Auftrag (FREIGABE)*: Overlay TAK u.a. Detail/Choice-Anzeigen an Fensterbreite anpassen; bei wenig Platz Typ-Buttons und Kartenreihen umbrechen statt Clip/Overflow. Generisch, kein TAK-Hack.
- *Root cause*: Feste `Width` (Kidnap 920 / History 880 / Team 560) und CardDetail `MinWidth=720` groesser als schmales Viewport; ScrollViewer Horizontal=Auto mass WrapPanel mit Infinity -> keine zweite Zeile.
- *Fix*: Overlay-Innenrahmen `Width`->`MaxWidth` + `Margin=12`; CardDetail MinWidth entfernt; Reveal-Buttons `WrapPanel`; `DetailStackCards` und `KidnapCardsPanel` ScrollViewer `HorizontalScrollBarVisibility=Disabled` damit WrapPanel wrappt.
- *Scope*: TableWindow.xaml (+ Kommentar PickCardFromList). Kein Span-Y / PlaceSpan / CaptureGameSave / Sync. Tip-Hash a9af295. Basis 3bc7e49 / Docs e589a78. Kein Push.
## 2026-09-28 - Spaceline Attach Y board-absolute + History RMB-only zoom

- *Auftrag (FREIGABE)*: (1) Q-Net Symptom Dual-EXE: P2 spielt Q-Net -> horizontal ok, vertikal bei P1 falsch (Karten unter/ueber Mission gespiegelt); P2-Ansicht = Wahrheit. Fix generisch Spaceline-Attaches (Q-Net/Gaps/Plays-on-mission). (2) Action History Recently played: kein Click->Detail-Fenster; nur Right-Click Zoom wie Board-Karten.
- *Root Y*: CaptureGameSave speicherte Host-viewer-Y; Guest Q-Net Host-Apply rief nach PlaceSpanOnSpaceline zusaetzlich AttachCardToHost (Collapsed-Mini-Duplikat); RelayoutDockablesUnderMission schrieb Owner aus Viewer-Seite neu -> vertikaler Flip auf P1.
- *Fix Y*: ToBoardAbsoluteY/FromBoardAbsoluteY (P1 below=+/P2 above=-, viewer-unabhaengig Sync); Apply mappt board-absolut -> viewer-relativ; Gaps/Q-Net nie AttachCardToHost; Relayout behaelt absolute Owner; Seed/Snap DockSlotOffsetY.
- *Fix History*: PlayHistoryStrip nur RMB BeginHoldZoom; kein LMB OpenCardDetailPopup.
- *Scope*: TableWindow.xaml.cs. Tip-Hash 3bc7e49. Basis acfb554 / Docs df2dc85. Kein Push.

## 2026-09-28 - Action History Kartenreihe + Play-Detail-Popup weg

- *Auftrag (FREIGABE Punkt 3)*: Action History drittes Fenster = horizontale Reihe zuletzt gespielter Karten (Face, Scroll); P1=GrÃ¼n / P2=Blau; Klick = Detail/Text. GroÃŸe zentrale Detail/Reveal-Popup beim Ausspielen (z.B. Interrupt Long-Range Scan mit OK) entfernen â€” Fly-in ersetzt Reveal; Nachlesen Ã¼ber History-Kartenreihe.
- *Fix*: HistoryOverlay hÃ¶her (MinHeight 720); `PlayHistoryStrip` unten; `RecordPlayHistory` an `NotifyPlayReveal` (Host/Solo) + `OnNetPlayRevealReceived` (Guest) â€” Network-First gleiche Reihe ohne neues Net-Message. Interrupt-Splash `ShowCardReveal(card,"Interrupt",â€¦OK)` entfernt; Long-Range-Scan-Ergebnis â†’ StatusText+Log (kein modal OK). Fly-in unverÃ¤ndert.
- *Scope*: TableWindow.xaml + TableWindow.xaml.cs. Tip-Hash acfb554. Basis 5c29f7b / Docs 1720a73. Keine Gaps/Q-Net. Docs separat. Kein Push.

## 2026-09-28 - Host->Guest PlayReveal Face Catalog + Gaps Guest-Drop
- *Bugfix (Pepsch Dual-EXE Host=P1 Guest=P2)*: (1) P1 Pers/Eq/Event -> P2 Fly-in Face schwarz; P2->beide Face OK. (2) P2 Gaps Drop -> Slot-Picker auf Host statt Guest-Slot Apply.
- *Root Face*: PlayReveal Lookup by InstanceId trifft vor State oft maskierte Opp-Hand-Stubs (Name="", kein FullImagePath); Source=null -> Background #111 schwarz. P2-Play OK weil Karte noch in Guest-Hand mit Art.
- *Root Gaps*: Guest Note nur underInst:leftMission; Host PickGapEndpoint ohne Host2 -> ShowIndexPickDialog auf Host-UI. Placement-Choice darf nicht auf Zuschauer/Host aufgehen.
- *Fix*: Catalog Instantiate + DTO Name+Set Face (gleicher Pfad wie Hand nach ResolveCard); ResolvePlayFlyInImagePath Catalog-first; Guest gap:leftInst:rightInst -> Host preferred Host+Host2 Apply, kein Picker.
- *Scope*: TableWindow.xaml.cs. Tip-Hash 5c29f7b. Basis 591fba3 / Docs 96d4f42. Kein Action-History. Docs separat. Kein Push.

## 2026-09-28 - Guest/P2 Play-Pfad TAK Persist + Interrupt Fly-in + Face

- *Bugfix (Pepsch Dual-EXE Host=P1 Guest=P2)*: (1) Interrupt auf Schiff/Board â†’ kein Fly-in; (2) Telepathic Alien Kidnappers als P2 tot, P1 ok; (3) viele P2 Fly-ins schwarz/Face fehlt.
- *Root cause Aâ€“D*:
  - A) Guest Action-only + Host TryApply+State grundsÃ¤tzlich ok; TABLE-Events gingen Commit-only (kein gemeinsamer Resolve-Pfad).
  - B) Interrupt PlayReveal feuert (BeginPlayCardStack/NotifyPlayReveal; Respond synced); Guest-Kill durch ClearTableCards/RelayoutAllDockables â†’ InvalidatePlayFlyInTargets (State/Relayout-Race nach PlayReveal).
  - C) Face: Stub ohne FullImagePath; Lookup ohne Discard; Background #111 â†’ schwarz wenn Source null.
  - D) P2 TAK: Host TryApplyNetPlayCard CommitCardToTable ohne TryResolveEventPlay â†’ Persist.Kidnappers nie registriert (P1 Stackâ†’TryResolveEventPlay ok).
- *Fix*: Net Events via TryResolveEventPlay (TAK Persist/Instant); Soft-Invalidate (Ghost-only) bei ApplyGameSave/Relayout wenn Fly-in aktiv; FindLiveCardWithArt + Discard/OOP in Lookup; Face gleiche Source wie Hand.
- *Scope*: TableWindow.xaml.cs. Tip-Hash 591fba3. Basis f005c71 / Docs b8828fb. Kein Action-History; keine Gaps/Q-Net. Docs separat. Kein Push.

## 2026-09-28 - Play Fly-in Event/Interrupt Target + P2 Face

- *Bugfix (Pepsch, nach Tip df1259e)*: Event/Interrupt auf Spielziel (Bynars/Spacedock auf Schiff) â€” P1 ok, **P2 landet TABLE** rechts statt am Target-Ship/Slot; manchmal P2 einfliegende Karte **schwarz** (Face nicht geladen).
- *Root cause*: Net TryApplyNetPlayCard behandelte jedes Event als TABLE wegen IsTablePermanentType(Event)==true (Hosted-Branch unerreichbar); Fly-in Land nur eigene Slot/TABLE-Bounds, kein Play-Action Target; Guest Face oft Stub ohne FullImagePath / Source=null vor Anim.
- *Fix*: NetPlayRevealDto.TargetInstanceId; Land = Target-InstanceId â†’ aktuelle Bounds (Host/Guest gleich); EnsurePlayFlyInCardArt + TryLoadPlayFlyInFace vor BeginAnimation; Net PlaysOnHost via TryResolveEventPlay/AttachCardToHost. Network-First PlayReveal beibehalten. Pipeline df1259e erhalten.
- *Scope*: NetPlayRevealDto + TableWindow.xaml.cs. Tip-Hash f005c71. Basis df1259e / Docs 74dede6. Kein Action-History; keine Gaps/Q-Net. Docs separat. Kein Push.

## 2026-09-27 - Play Fly-in Ziel stale (Outpost nach Relayout/Neuspield)

- *Bugfix (Pepsch, nach Tip 857ab9e)*: Personnel fliegen zum **alten Outpost-Punkt vom vorigen Spiel**; Ziel folgt nicht dem aktuellen legalen Snap-Fenster / Facility-Bounds nach Relayout.
- *Root cause*: Stack-Pers/Eq bleiben Collapsed mit Host-AbsoluteLeft (Drop/Save); TargetNorm wurde vor Relayout gecacht und als Fallback genutzt; Guest mass Slot vor ScheduleRelayoutAfterLoadSettle â€” Facility schon verschoben, Kind-Border stale.
- *Fix*: FindPlayFlyInLandBorder = Host-Facility/Ship nach Layout (Pers/Eq), sonst eigener Face/TABLE; SyncPlayFlyInStackedCardBounds bei Relayout + AddCardToHostStack; TargetNorm erst zur Animation (Loaded); InvalidatePlayFlyInTargets bei ClearTableCards / RelayoutAllDockables. Ship/Event unveraendert Live-Land-Bounds. Pipeline 857ab9e erhalten.
- *Scope*: TableWindow.xaml.cs. Tip-Hash df1259e. Basis 857ab9e / Docs fd8450b. Kein Action-History; keine Gaps/Q-Net. Docs separat. Kein Push.

## 2026-09-27 - Play Fly-in Nachzieher Ziel/Perspektive/Doppel (Pepsch-Video)

- *Bugfix (Pepsch, Video nach Tip 034aec2)*: (1) Zielkoordinaten falsch â€” Ship/Event fliegen in leeren Spaceline-Raum/Ecke statt Outpost bzw. Core/TABLE; (2) P2-Play startet bei P1 von unterer Hand statt Gegner-Hand oben; (3) echte Karte am Ziel schon wÃ¤hrend Fly (Doppel); (4) Snap/Jump am Outpost (Endâ‰ Slot); (5) Mitte blanker schwarzer RÃ¼cken statt Face (Beverly P2); (6) Tempo ~4s zu langsam.
- *Root cause*: Host `TargetNorm` ist Host-viewer-relativ und wurde auf Guest bevorzugt â†’ falsche Y; `FindBorderForCard` sucht nur `TableCanvas` (TABLE-Events unsichtbar); Ghost nur Opacity-Ref ohne InstanceId â†’ Rebuild zeigt Slot wieder; `endScale=1` statt Slot-Bounds; Face nur `card.FullImagePath` (oft leer nach Net-Stub); Timing 1.7+1.2+1.3s.
- *Fix*: Landing **lokale** Slot-Bounds zuerst (`FindPlayFlyInSlotBorder` = Canvas + TABLE-Minis); Host-TargetNorm nur Fallback; `card.Controller` fÃ¼r Hand-Start viewer-relativ (kein Own-Strip-Fallback); Ghost `_playFlyInHiddenInstanceId` + Rebuild/AddCardToTable; End-Transform = Slot Center+Size; Face via DB-Prototype wie Hand-Reveal; Tempo ~1.0+0.7+0.9s (~2.6â€“2.8s). Network-First PlayReveal / Pipeline 034aec2 behalten.
- *Scope*: TableWindow.xaml.cs. Tip-Hash 857ab9e. Basis 034aec2 / Docs 594c023. Kein Action-History; keine Gaps/Q-Net. Docs separat. Kein Push.

## 2026-09-27 - Play Fly-in sichtbar (Nachzieher-Fix)

- *Bug (Pepsch)*: Nach Tip b1d5d3e kein Overlay â€” Animation startete nicht sichtbar (Host lokal + Guest).
- *Root cause*: `Storyboard.SetTarget` auf Transform-Freezables ohne zuverlÃ¤ssigen Clock; `PlayFlyInOverlay` Canvas nach Collapsed oft ohne Layout/Koordinaten â†’ Karte unsichtbar. Ghost Opacity 0 ohne sichtbare Overlay-Karte.
- *Fix*: `PlayFlyInCard` auf immer gelayoutetes `DragLayer` reparenten; Pfad/Scale/Rotate via `BeginAnimation` (Hover-Preview-Muster); Host `ShowPlayFlyIn` vor Network-IO; Debug `StatusText`/GameLog `Fly-in: P# Name`. TargetNorm + PlayReveal-Pipeline erhalten.
- *Scope*: TableWindow.xaml.cs. Tip-Hash 034aec2. Basis b1d5d3e / Docs fc8b5ca. Kein Action-History; keine Gaps/Q-Net. Docs separat. Kein Push.

## 2026-09-27 - Play Fly-in Nachzieher (Handâ†’Mitteâ†’Slot)

- *Feature (Pepsch, Referenz-Video ausgewertet)*: Fly-in anpassen â€” Hand â†’ Bildschirmmitte (~3â€“4Ã— Board, 100% Opacity, Drop-Shadow) â†’ Hold lesen â†’ Mitte â†’ Zielslot Board-GrÃ¶ÃŸe nahtlos. Gerader Pfad (linear/eased), Rotation aufrecht (Hand-Winkelâ†’0Â°); kein Bogen, kein Tumble, kein Neon-Glow.
- *UI*: kein Board-Dimmen, kein Name-Banner, kein Fullscreen-Overlay. Overlay = transparente Canvas + Karte mit DropShadow; landet und verschwindet ohne Fade.
- *Timing*: Handâ†’Mitte ~1.7s, Hold ~1.2s, Mitteâ†’Slot ~1.3s (gesamt ~4.2s).
- *Netzwerk*: PlayReveal-Pipeline 57c1a3e behalten; `NetPlayRevealDto.TargetNormX/Y` fÃ¼r Landepunkt; Host `NotifyNetworkBoardChanged` vor BroadcastPlayReveal (Guest Border fÃ¼r Ghost/Land). Board-Karte Opacity 0 wÃ¤hrend Ani, Restore on land.
- *Scope*: NetPlayRevealDto + TableWindow (+ XAML Overlay). Tip-Hash b1d5d3e. Basis 57c1a3e / Docs 642f53f. Kein Action-History; keine Gaps/Q-Net. Docs separat. Kein Push.

## 2026-09-27 - Play Fly-in Reveal (Network Hand/Interrupt)

- *Feature (Pepsch, Freigabe Punkt 2)*: Beide Spieler sehen sofort welche Karte gespielt wurde (kurze Fly-in Overlay-Animation wie digitale CCGs). Gilt Play aus Hand (Ship/Pers/Eq/Event/Interrupt).
- *Ursache*: Nach Guest PlayCard Host-apply / Board-Sync sah der Gegner oft nur Board-Diff ohne klares Card-Reveal; Guest durfte Animation nicht lokal vor Host-Apply zeigen (Desync).
- *Fix*: `NetMessage.Types.PlayReveal` + `NetPlayRevealDto`; Host `BroadcastPlayRevealAsync` nach erfolgreichem Play (`OnSuccessfulHandPlay` / Interrupt `BeginPlayCardStack`); beide Clients `ShowPlayFlyIn` (Scale/Opacity, non-modal ~1.4s, Hover-Preview-Muster). Guest nur `OnNetPlayRevealReceived` â€” kein Fly-in in `TrySubmitGuestNetworkPlay`. Host `NotifyPlayReveal` pusht auch masked State (Host-local Play Sync).
- *netztauglich*: Decide/Apply weiter Host; Animation Event nach Apply; Board-Sync Tip 8322b68 / Docs 665c014 nicht revertiert. Kein Action-History-Kartenreihe; keine Gaps/Q-Net P2 Spaceline.
- *Scope*: Network (NetMessage/NetPlaySession/NetPlayRevealDto) + TableWindow (+ XAML Overlay). Tip-Hash 57c1a3e. Docs separat. Kein Push.

## 2026-09-27 - Board-Sync Multiplayer (Fly / Beam / Attack / Interrupt)

- *Bugfix (Pepsch)*: P2 Fly / Ship-Attack / Beam nicht live bei P1; nach Rundenende Board auf P1-Stand (P2-Aktionen verloren); P1â†’P2 oft erst nach EndTurn; Interrupt-Effekte erreichen P2 nicht.
- *Ursache*: Fly/Beam/InitiateShipBattle/Respond nicht in IsNetSyncKindSupported; Guest mutierte Board lokal (phantom). Host BroadcastMaskedStateToGuest nur bei EndPhase/EndTurn/Seed/PlayCard â€” Host-Board-Aktionen ohne sofortigen Sync; EndTurn-Broadcast Host-Save ohne Guest-Phantom â†’ ApplyGameSave wischt P2.
- *Fix*: Muster wie Seed/PlayCard 5da7c3b â€” Guest Action only (Fly click/drag, Beam CompleteBeamTo, ShipBattle, Interrupt BeginPlayCardStackâ†’Respond); Host TryApplyNetFly/Beam/ShipBattle/Respond + Broadcast; Host NotifyNetworkBoardChanged nach lokalem Fly/Beam/Attack/Stack-Resolve; NetActionDto InstanceId-Lookup. Kein Fly-in Card-Effekt; keine Action-History drittes Fenster.
- *netztauglich*: Decide Host EngineAuthority; Guest UI aus ApplyGameSave. Visibility Probe/Occupancy Fog b2dfaf7 / Docs b0e8faa erhalten.
- *Scope*: TableWindow + GameAction.ShipBattle + NetActionDto. Docs separat. Kein Push.

## 2026-09-27 - Alien Probe Hand-Sync + Occupancy/AT Fog (Network Visibility)

- *Bugfix (Pepsch)*: Alien Probe on table â€” P1 sah beide Haende, P2 sah P1-Hand nicht. Parallel Spock: Gegner-Occupancy (Schiff/Facility) und Planet-AT frei einsehbar.
- *Ursache*: `NetStateMask.MaskForViewer` maskierte Opponent-Hand immer (kein Probe-Check); Stack-Kinder (Crew/AT/docked) behielten volle Identitaet im Guest-Save. UI `FillHostStrip`/`Detail` zeigte Occupancy face-up; Host-Strip-Hand absolut P1/P2 ohne Probe-Gate.
- *Fix*: `NetStateMask` â€” bei Alien Probe (AttachedEvents Kind Probe / p*.table / Table-Name) Hand-Zonen unmasked; Opponent-Stack-Occupancy nameless/FaceDown. UI FogViewerPlayer (Network=LocalPlayer, Hotseat=ActivePlayer); FillHostStrip/Detail/Badge Occupancy face-down; 12.12 Looking-at-cards Stub (Status+Log). Host-Strip-Hand viewer-relativ + Probe/Hotseat reveal.
- *netztauglich*: Host volle Wahrheit; Broadcast maskiert viewer-relativ; Host-UI gleiche Fog-Regel. Scope nur Probe-Hand + Occupancy/AT Fog â€” keine Ship/Facility Face-Visual-Umbauten darueber hinaus. 12.12 Ausnahmen MVP-Stub.
- *Scope*: NetStateMask + TableWindow. Prior Tips 5da7c3b / 4a981ce erhalten. Docs separat. Kein Push.

## 2026-09-27 - Guest PlayCard Host-apply (P2 Play->Execute Hand-Wipe)

- *Bugfix (Pepsch, Guest T3-T5 ~18:34)*: P2 spielt Qu'Vat / Medical Tricorder / Genetronic Replicator; nach End PLAY (EndPhase) Karten weg vom Board, wieder in Hand. Host-Save ships=0 / ohne Equipment.
- *Ursache*: Guest Hand-Play mutierte nur lokal (phantom). Host TryApplyNetAuthorizedAction PlayCard = P3-Stub (return false, kein UI-Apply). EndPhase Broadcast Host-Wahrheit ohne Guest-Plays -> Guest ApplyGameSave wischt Board, Karte wieder Hand.
- *Fix*: Guest TrySubmitGuestNetworkPlay (Action only, Karte bleibt in Hand bis Save); Host TryApplyNetPlayCard platziert Ship (Facility-Report) / Personnel+Equipment (Host-Stack) / Event+TABLE; OnSuccessfulHandPlay; ActivePlayer-Gate; GameAction.Play Note underInst. Interrupts weiter P4.
- *netztauglich*: Decide Play Host EngineAuthority; Guest UI nur aus ApplyGameSave (wie Seed). Tips 7c040df / 9abb843 erhalten.
- *Scope*: TableWindow + GameAction.Play Note. Docs separat. Kein Push.

## 2026-09-27 - Multiplayer Skip seed phase (Network Lobby)

- *Feature (Pepsch)*: Optional Auto-Seed wie Quick Game im Netz, wenn beide Skip seed phase akzeptieren.
- *Flow*: Beide Ready â†’ Lobby-Panel **Skip seed phase**; Propose â†’ beide Accept â†’ Host `AutoCompleteSeed`; Decline oder 45s-Timeout â†’ manuelle Seed-Phase.
- *Technik*: `LobbySkipSeed` Vote (propose/accept/decline); `StartGame.skipSeedPhase`; Host allein Auto-Seed + Broadcast; Guest UI aus Sync. Tip 9abb843 (Guest Segment) erhalten.
- *netztauglich*: Decide Host; Guest kein lokales AutoSeed.
- *Scope*: NetMessage/NetLobbyDto + NetworkLobbyWindow + TableWindow OnLobbyGameStarting. Docs separat. Kein Push.

## 2026-09-27 - Execute-Haenger P2 Guest Segment Sync (Network)

- *Bugfix (Pepsch, Guest ~18:03)*: Nach Treaty Segment -> Execute (Orders); Sent EndTurn -> Error **End Play phase first.** Soft-Lock: Guest-UI Execute, Host-Engine noch Play.
- *Ursache*: `OnSuccessfulHandPlay` rief `_session.AdvanceSegment()` lokal auf dem Guest nach Normal-Play (Treaty). Guest sandte EndTurn; Host `EngineAuthority` Deny weil Segment noch Play. Kein Host Action->Apply->Broadcast fuer Segment.
- *Fix*: Network Guest skippt lokales AdvanceSegment (Segment nur via Host EndPhase + ApplyGameSave); Network Host BroadcastMaskedStateToGuest nach lokalem Advance; Host remapped Guest EndTurn bei Segment==Play zu EndPhase (Desync-Recovery). Artifact-Glow unberuehrt.
- *netztauglich*: Decide EndPhase/EndTurn weiter Host EngineAuthority; Guest UI Segment aus ApplyGameSave.
- *Scope*: TableWindow only; Docs separat. Kein Push.

## 2026-09-27 - Artifact Seed Glow ALL [P] (Nachzieher)

- *Bugfix (Pepsch, nach 23044e0)*: Unter manchen Missionen kein Artifact-Snap/Glow (Stone of Gol); Glow zeigte nicht alle legalen [P].
- *Ursache (Captain-Punkt 1)*: CollectSitesForDrag lief IsPlayOnDrag vor Seed. Artifacts wie Vulcan Stone of Gol sind NeedsBoardSnap (spaeter play-as-Event) -> Glow = Away-Team-PlayOn (oft leer) statt ALLER legalen [P]-Missionen. Snap/Drop nearest-legal blieb OK; Glow nicht. Punkt 4: ShipSnapRange 280 knapp vs UnderMissionGap-dy. Punkt 3: Deny-StatusText bei Range/Limit oft leer/generisch. Punkt 2: ParseLocationIcons Lore-Fallback aboard/Away Team konnte falsch klassifizieren (abgehaertet). Punkt 5 Execute: unveraendert / Smoke noch offen.
- *Fix*: Seed-CollectTargets VOR PlayOn; gold Glow alle LocationSlot/Seed; SeedUnderMissionSnapRange (560); DescribeSeedUnderDeny (Limits/Planet-Space/Range); SeedRules ParseLocationIcons mdt-first ohne Lore-aboard. Execute unveraendert.
- *netztauglich*: Decide SeedRules + CollectSites; Apply TableWindow Host/Guest gleiche Deny-Texte.
- *Scope*: SeedRules + TableWindow; Docs separat. Bug-3-Ordner unberuehrt. Kein Push.

## 2026-09-27 - Artifact Seed Targets + P2 End EXECUTE (Network)

- *Bugfix (Pepsch)*: (1) Artifact (Vulcan Stone of Gol) nur unter Hunt for DNA Program seedbar; (2) P2 Turn 1 steckt in EXECUTE â€” Space/Button tot.
- *Ursache*: (1) Host-Drop `TrySnapToMission`/`FindNearestMission` nahm naechste beliebige Mission (oft [S]) â†’ CanSeed deny; Target-Glow/`FindNearestLegalSeedMission` nicht durchgaengig. (2) Stuck Response-Stack liess End-Turn disabled bei Label â€žEnd EXECUTEâ€œ; Guest EOT-Flags soft-lockten ohne Host-Flip; FinishExecute Early-Return no-op bei Guest EndTurn.
- *Fix*: Seed-Snap/Glow/Drop â†’ `FindNearestLegalSeedMission` + `AllMissionBorders` + Artifact-Limits; Layout-Pin unveraendert. Network EndPhase/EndTurn clear stuck stack; Guest EOT sendet EndTurn; Host force `CompleteTurnChange` wenn Flip ausbleibt; UpdatePhaseControls `netActiveEscape`.
- *netztauglich*: Decide weiter SeedRules/EngineAuthority; Apply Host+Broadcast.
- *Scope*: Tip TableWindow only; Docs separat. Kein P5; kein Push; kein Projektordner-Cleanup.

## 2026-09-27 - Network P2 Facility Seed-on-Outpost + Viewer Dock Layout

- *Bugfix (Pepsch, Screenshots)*: (A) P2 seed/report auf eigenen Klingon Outpost - Snapglow korrekt, Reject "Federation Outpost ... foreign facility" / aehnlich. (B) P2-Client: eigene Facilities noch oben; P1/P2 TABLE + Hand-Labels Hotseat-inkonsistent.
- *Ursache A*: Drop-Owner `zref.Opponent ? 2 : 1` (Guest bottom strip = falsch P1); `_dragCard=null` vor TrySnap -> FindNearestHost uebersprang CanReportToHost-Filter -> naechste P1-Facility; Guest Seed Personnel ohne underInst -> Host CommitCardToTable.
- *Ursache B*: RelayoutDockables/DockSlotOffsetY/GetBorderOwner-Fallback absolut P1 unten/P2 oben; TABLE-Panels + Labels fest P1=bottom.
- *netztauglich*: PlayerForStrip fuer Strip-Owner; CanReportToHost(reportingPlayer); FindNearestHost(reportingCard); Guest Seed Personnel/Ship Target+underInst:InstanceId; Host ResolveSeedFacilityHostTarget + AddCardToHostStack; Dock/TABLE/Labels ViewerPlayer-relativ (Host=P1 Guest=P2).
- *Scope*: Facility seed-on-outpost + viewer dock/TABLE; kein P5; kein Push.

## 2026-09-27 - Visual Seed-under-Mission Layout (Horga'hn column pin)

- *Bugfix (Pepsch, Screenshot)*: Artifact (Horga'hn) nach Seed versetzt links/oben ueber Ziel-Mission, ueberlappt Nachbar-Mission; Badge "1" korrekt unter Slot.
- *Ursache*: `RelayoutMissionsOnSpaceline` / `ApplyPerspective` (TryAlternate) verschoben Missionen + Badges, pinnten Seed-Borders aber nicht nach â†’ AbsoluteLeft blieb Drop/alt; Visible-Orphans/DragLayer-Kopien moeglich; `RemoveOrphanTableCopies` konnte SeedUnder-Borders strippen.
- *Fix (Visual layout, Sync-Authority unveraendert)*: `RelayoutSeedUnderMissions` am Ende von `RelayoutMissionsOnSpaceline`; `PinSeedUnderMissionBorder` (Mission-Spalte + viewer-rel. DockSlotOffsetY); `ScrubSeedUnderDuplicates`; SeedUnder-Schutz in `RemoveOrphanTableCopies`. Host/Guest identisch.
- *Scope*: Seed-under Layout only; kein P5; kein Push.

## 2026-09-27 - Network P2 Facility Seed docks Spaceline (not TABLE)

- *Bugfix (Pepsch, Screenshot)*: P2 Facility Seed (Remote Supply Depot) landet in â€žP2 TABLEâ€œ Sidebar statt unter gewÃ¤hlter Mission auf der Spaceline; beide Clients gleich (Engine-Wahrheit falsch). Nor von P1 lag korrekt unter Space-Mission. Status TURN 1 PLAY.
- *Ursache*: Guest `TrySubmitGuestNetworkSeed` setzte Mission-Target fÃ¼r Facilities, aber Host `TryApplyNetSeedCard` hatte keinen Facility-Zweig â†’ else `CommitCardToTable` â†’ `_oppTablePermanentCards` (P2 TABLE). Dilemma/Artifact-Pfad (AddSeedUnderMission) und Host-lokaler Facility-Drop (DockSlot) waren ok; Events wie Q's Planet auf TABLE bleiben regelkonform (SeedRules Facility-Phase).
- *netztauglich*: Host `TryApplyNetSeedCard` dockt Facility via `ResolveSeedUnderMissionTarget` + `CanSeedFacilityAtMission` + `AddCardToTable`/`RelayoutDockablesUnderMission` (wie AutoSeedFacility); illegal â†’ zurÃ¼ck Facility-Pile. Guest Note `underInst:InstanceId` fÃ¼r Facilities; Resolve name-legal nutzt `CanSeedFacilityAtMission` fÃ¼r Facilities. Sync-Authority unverÃ¤ndert (Guest Actionâ†’Host Applyâ†’Broadcast).
- *Scope*: P2/Guest Facility Seed Placement; kein PlayCard-UI (weiter deferred); kein Ship-Facility-Phase TABLE-Residual; kein P5; kein Push.

## 2026-09-27 - Network Seed-under-Mission Host/Guest Sync + Owner Face-up

- *Bugfix (Pepsch, Screenshot)*: Dilemma/Artifact Seed-Anzeige Host vs Guest divergiert (Vulcan Stone of Gol unter Mission nur auf einem Client; Clumping/falsche X; Zaehler 3/1 vs fehlend). Status beide SEED 3/4 Dilemma P2 (TOP vs BOTTOM).
- *Klarstellung Pepsch*: Stack-Layout beiderseits identisch; Owner sieht eigene Seeds face-up; Opponent face-down/Zaehler (nicht beide face-up).
- *Ursache*: (1) `ApplyGameSave` SeedUnder rief `AddSeedUnderMission` mit CanSeed-Re-Check - Mask-Stubs/Drift  Visible-Orphans bei Host-AbsoluteLeft (Clumping) bzw. fehlende Stacks; (2) `FillHostStrip` im Seed-Phase reveal=ALL (Host sah Guest-Artifact face-up); (3) `UpdateSeedBadge` ownCount via `_activePlayer` statt ViewerPlayer; (4) Guest Dilemma-Seed nur Target-Name, kein Mission-InstanceId.
- *netztauglich / UI+Restore*: Sync-Authority (Guest ActionHost ApplyBroadcast) unveraendert. `AddSeedUnderMission(force)` + FaceUp=false + Pin an Mission-X; ApplyGameSave force + `RelayoutSeedUnderMissions`; Guest Note `underInst:InstanceId`; Host `ResolveSeedUnderMissionTarget`; FillHostStrip/Detail owner-only face-up; `NetStateMask` cleared Opponent-SeedUnder Table-Identitaet (Counts bleiben).
- *Scope*: kein P5 Disconnect; kein Push.

## 2026-09-27 - Network P2 Mission-Seed Insert-Index + Slot-Hover

- *Bugfix (Pepsch, Screenshot)*: P2 (Guest) legt Khitomer zwischen Wormhole Negotiations und Avert Disaster â†’ landet rechts am Ende; Slot-Rahmen da, Hover mit Karte in Hand leuchtet nicht.
- *Ursache*: (1) Guest `TrySubmitGuestNetworkSeed` sandte Mission ohne Note; Host `TryApplyNetSeedCard` nutzte `AutoSeedMission` (Zufalls-X) â€” ba2ee6f Partial ohne after:Name. (2) `ShowMissionSlotPreviews` zeichnete alle Slots gleich dim; MouseMove rief Preview ohne hoverDropX â€” kein Hot-Slot wie sonstige Snap-Targets.
- *netztauglich*: Guest Note `after:`/`before:`/`insert:` (Nachbar-Name = Engine-Index, absolute Lâ†’R Spaceline); Host `SeedMissionFromNetNote` + `PlaceMissionOnSpaceline(..., forcedInsertIndex)`; Hover brightened nearest legal slot (Hotseat + Network). Sync Authority unverÃ¤ndert (Guest Actionâ†’Host Applyâ†’Broadcast).
- *Scope*: nur P2 Mission-Seed Index+Hover; kein Dilemma-Target-Umbau; kein Push.
## 2026-09-27 - Network UI Nachzieher Seed (Hand face-up / Mission Glow / Viewer-Orientierung)

- *Bugfix (Pepsch, Screenshot)*: Guest eigene Missionen als RÃ¼cken auÃŸer Zug; Mission-Drop-Glow/Snap fehlte vs Hotseat; Spaceline auf P2-Instanz noch Host-orientiert (P2 auf dem Kopf).
- *Ursache*: (1) `FillStrip` maskierte eigene private Zonen face-down wenn `!isActiveSide` (Hand ausgenommen, Missions nicht) â€” nach ApplyGameSave/Gegnerzug RÃ¼cken; (2) Seed-Highlight rief fÃ¼r Missionen nur leeres `HighlightPlayOnSites`, Slot-Glow hing allein am MouseMove; `snapOwner` noch `Opponent?2:1`; (3) `ApplyMissionFaceVisual` rotierte fest `face==2` statt ViewerPlayer-relativ.
- *netztauglich / UI-only*: Sync-Pfad (Guest Actionâ†’Host Applyâ†’Broadcast, ActivePlayer-Gate, session-first Notify) unverÃ¤ndert. `ownNetworkFaceUp` + Hand immer face-up fÃ¼r LocalPlayer; `ShowMissionSlotPreviews` im Seed-Highlight + `PlayerForStrip` snapOwner; Mission-Rotation `faceToward == ViewerPlayer`.
- *Scope*: kein P5 Disconnect; kein Sync-Umbau; kein Push.

## 2026-09-27 - Network Guest->Host Seed Authority Loop

- *Bugfix (Pepsch, Screenshot)*: P2 Mission auf Guest -> P1-Board blieb alt (Spaceline 2 vs 1; Zaehler 5 vs 6 left). Host->Guest nach f020183 ok.
- *Ursache*: Guest Drop applyte lokal, dann TryAlternateSeedPlayer flipte _activePlayer P2->P1 *vor* NotifyNetworkAfterSeedPlacement. f020183-Gate _activePlayer != LocalPlayer skippte SendGuestActionAsync â€” Host bekam nie SeedCard.
- *netztauglich*: Guest Seed-Pile-Drop = nur Action (TrySubmitGuestNetworkSeed â†’ Host OnNetActionReceived â†’ TryApplyNetSeedCard â†’ BroadcastMaskedStateToGuest); kein lokales Guest-Board ohne Host-Wahrheit; Karte bleibt im Guest-Stapel bis ApplyGameSave. Notify-Gate entfernt (Fallback sendet immer LocalPlayer).
- *Scope*: kein P5 Disconnect; kein Push.

## 2026-09-27 - Network Host->Guest Seed Sync (ActivePlayer + input gate)

- *Bugfix (Pepsch, Screenshot 27.09.)*: P1 Mission auf Host -> Guest-Board blieb alt; Banner "P1 dran"; Guest konnte fremde/ungehoerige Karten legen (paralleles Hotseat).
- *Ursache*: (1) `NotifyNetwork*` / Viewer hingen am ModeNetwork-Radio -- bei Live-`NetPlaySession` ohne Radio-Check kein Broadcast; (2) `ApplySelectedGameMode` setzte `_activePlayer=1` zurueck; (3) Drag-Gate nur Hotseat + Owner `Opponent?2:1` statt `PlayerForStrip` -- Guest ohne `LocalPlayer==ActivePlayer`-Gate; (4) SeedCard ohne Turn-Check auf Host.
- *netztauglich*: `EnsureNetworkModeFromSession` erzwingt Network sobald Session lebt; Broadcast/Notify session-first; Guest `OnNetStateReceived` refreshed Seed-Banner/Stack aus Save-ActivePlayer; Drag nur eigene Zone + nur wenn LocalPlayer==ActivePlayer; `SetBorderOwner` via `PlayerForStrip`; SeedCard ActivePlayer-Deny; `ApplySelectedGameMode` clobbert ActivePlayer nicht waehrend Live-Match/Session.
- *Scope*: kein P5 Disconnect; kein Push.

## 2026-09-27 - Network Viewer = LocalPlayer (Seed/Hand UI)

- *Bugfix (Pepsch)*: Beide Localhost-Instanzen zeigten P1-Ansicht (`SEED Player 1 (BOTTOM)`, `P1 Missions`); Guest sah P1-Missionen statt eigener.
- *Ursache*: UI fest P1=unten/P2=oben; Network-Seed baute nur P1-Zonen; `ShowCurrentSeedStack` zeigte immer ActivePlayer-Stapel auf Hotseat-Layout; `ApplyPerspective` ohne Viewer-Spiegelung.
- *netztauglich*: `ViewerPlayer` = `NetPlaySession.LocalPlayer`; Bottom-Strip/Zonen = LocalPlayer, Top = Gegner (`PlayerForStrip` / `GetZoneList` / `GetCardsForZone`); Fremd-Hand/Seed face-down (`FillStrip` + `NetStateMask`); Host-Broadcast nach Seed unverÃ¤ndert (`NotifyNetworkAfterSeedPlacement`); `CaptureGameSave.ActivePlayer` seed-aware; masked `ResolveCard`-Stubs halten ZÃ¤hlungen.
- *Scope*: kein P5 Disconnect; kein Push.
## 2026-09-27 â€” Network Seed/Mission Sync (Host broadcast)

- *Bugfix (Pepsch/Captain)*: Nach Lobby+Start seedet P1 eine Mission â€” P2 sah nichts; beide spielten getrennt. Ursache: kein `BroadcastMaskedStateToGuest` nach Seed-Drop; Seed-Spielerwechsel nur Hotseat; `SeedCard` nicht in Net-Sync.
- *netztauglich*: Seed/Mission Ã¼ber Host-Wahrheit + `BroadcastMaskedStateToGuest` / Guest `ApplyGameSave`; Seed-Spielerwechsel auch in Network; Guest-Seed via `GameAction.Seed` + Host `TryApplyNetSeedCard`.
- *TableWindow*: `IsSeedMultiPlayerMode` Ã¶ffnet Alternate/Sequential + Facility-Handoff fÃ¼r Network; `NotifyNetworkSeedChanged` / `NotifyNetworkAfterSeedPlacement` nach Seed-TischÃ¤nderungen; Initial-Broadcast in `OnLobbyGameStarting`; `SeedCard` in `IsNetSyncKindSupported` + Seed-Pile Lookup; Phase Next/Finish Network (`EndPhase` Note=`SeedAdvance`, `EndTurn` Note=`SeedFinish`).
- *Partial*: Guest-Mission-Insert ohne Pixel/`after:Name` (Host `AutoSeedMission`); Dilemma-Target best-effort; Respond/PlayCard-UI unverÃ¤ndert.
- *Scope*: kein P5 Disconnect, kein PlayCard-UI voll, kein Push.

## 2026-09-27 â€” Network Phase 4 ChoiceRequest/Response + Response-Fenster

- *netztauglich*: Wahlen/Response Ã¼ber `ChoiceRequest`/`ChoiceResponse` JSON auf bestehendem Framing; Host autoritativ; Decide bleibt Engine/TimingRules; TableWindow zeigt Dialoge / wartet / sendet Antwort; Sync nach Resolve weiter Ã¼ber Phase-3 State (`BroadcastStateAsync(MaskForViewer(2))`).
- *NetMessage.Types*: `ChoiceRequest`, `ChoiceResponse` + `Network/NetChoiceDto.cs` (correlationId, kind `choice`|`responseWindow`|`responsePass`, targetPlayer, title/prompt/options, selectedOption/passed/timeoutMs).
- *NetPlaySession*: `SendChoiceRequestAsync` / `SendChoiceResponseAsync`; Events `ChoiceRequestReceived` / `ChoiceResponseReceived`; HandleMessage-Routing.
- *TableWindow AskChoiceForPlayer*: Hotseat lokal; Host+LocalPlayer lokal; Host+P2 â†’ ChoiceRequest + DispatcherFrame-Wait; Host-Timeout = random option lokal (Guest-Timer kann frÃ¼her antworten). Guest beantwortet inbound ChoiceRequest, startet keine eigene Engine-Wahl.
- *Response-Fenster*: Host Ã¶ffnet fÃ¼r remote Responder Warte-Status + `responseWindow`-Request (kein Pass-Timer fÃ¼r falschen Spieler); Guest ThinkTray/Pass â†’ ChoiceResponse(passed); Host ruft `PassCurrentResponseWindow`. Respond-mit-Karte: Action-Pfad partial.
- *Verdrahtung*: Return Fire (defOwner), Gaps (nullifier), Q Continuum/rearrange (opp), Yellow Alert (who), Alien Parasites (opp).
- *Scope*: kein P5 Disconnect; kein Push; TimingRules unangetastet.

## 2026-09-27 â€” Network Lobby-Flow (Deck pick + Ready handshake + StartGame)

- *netztauglich*: Lobby-Entscheidungen (Deck/Ready/Start) Ã¼ber JSON `NetMessage` auf demselben Framing; Host autoritativ fÃ¼r Start; Tisch-Ãœbergang nutzt bestehendes `DetachTransport` â†’ `NetPlaySession`; keine UI-only Regel fÃ¼r Spielstart.
- *NetMessage.Types*: `LobbyDeck`, `LobbyReady`, `LobbyStatus`, `StartGame` + `Network/NetLobbyDto.cs` Payloads.
- *NetworkLobbyWindow*: nach Handshake Lobby-Raum (Deck-Combo + Browse, Start game, Peer-Status); eigener Receive-Loop; Start erst wenn beide Ready; Event `GameStarting`.
- *TableWindow*: `ConnectionChanged` startet **nicht** mehr sofort die Session; erst `GameStarting` â†’ Detach + NetPlaySession + Decks aus JSON (`LoadAndLinkDeckFromJson` / `DeckService.LoadFromJson`) platzieren.
- *Scope*: kein Choice/Timing (P4), keine Disconnect-HÃ¤rtung (P5), keine PlayCard-UI-Nachzieher.
## 2026-09-27 â€” Network Phase 3 GameAction-Sync (Host authority + masked State)

- *NetActionDto / NetStateMask / NetPlaySession*: JSON-DTO for GameAction (names only), fog-of-war mask for Guest (opp hand + private decks FaceDown/name cleared), session owns NetServer XOR NetClient after lobby with receive-loop + Dispatcher callbacks.
- *Lobby DetachTransport*: `NetworkLobbyWindow` exposes IsHost/Server/Client; Closing does not dispose when transport detached to session.
- *TableWindow vertical slice*: After lobby Connected â†’ `NetPlaySession`; Host `ActionReceived` â†’ `AuthorizePlay` â†’ EndPhase/EndTurn apply + `CaptureGameSave` â†’ `MaskForViewer(2)` â†’ Broadcast; Guest End PLAY/End turn sends Action and `ApplyGameSave` on State. Live kinds: EndPhase, EndTurn (+ Pass/Draw/PlayCard authorize stubs). No ChoiceRequest (P4), no Disconnect harden (P5).
## 2026-09-27 â€” Network Phase 2 Lobby (Host / Join / Localhost)

- *Lobby-UI*: `StarTrekCCG/NetworkLobbyWindow.xaml` + `.xaml.cs` â€” Dark UI (#252528 / #0E639C), Port (Default 7777), Host-Adresse, Buttons Host / Join / Localhost / Disconnect.
- *Flows*: Host â†’ `NetServer.StartAsync` + `AcceptClientAsync` + Handshake â†’ â€žConnected as Host (P1)â€œ; Join â†’ `NetClient.ConnectAsync` + Handshake â†’ â€žConnected as Guest (P2)â€œ; Localhost = Host mit `loopbackOnly=true`.
- *TableWindow-Anbindung (minimal)*: `ModeNetwork` enabled; Status â€žMode: Network (lobby|connected)â€œ; Button â€žOpen lobbyâ€¦â€œ / Checked Ã¶ffnet Lobby (`Show`). Kein GameMode-Spielstand-Sync, keine GameAction-Pipeline.
- *Scope*: Nur Connect/Listen/Accept/Handshake. Kein Engine-Sync.
## 2026-09-27 â€” Network Phase 1 Scaffold (Transport)

- *TCP+JSON Transport-Scaffold*: `StarTrekCCG/Network/NetMessage.cs`, `NetServer.cs`, `NetClient.cs` â€” Envelope + Listen/Accept/Connect/Send/Receive; kein Lobby/UI, kein GameMode-Anbinden.

## 2026-09-27 â€” Konzept & Handoff: Multiplayer-Modus (Architektur & Roadmap)

- *Multiplayer-Architektur konzipiert*:
  - Client-Server Modell (Host/Gast) Ã¼ber TCP-Sockets und JSON-Nachrichten (`System.Net.Sockets`).
  - UnterstÃ¼tzung fÃ¼r LAN, Internet und 2 Instanzen auf demselben Rechner (Localhost).
  - 5-Phasen-Roadmap in `HANDOFF.md` und `UEBERGABE_PROMPT.md` hinterlegt.
  - Test-Erfolge von Pepsch (*Crystalline Entity*, *Iconian computer weapon*, *Vulcan Mindmeld* grÃ¼n) erfasst.

## 2026-09-26 â€” Bereinigung: Abschluss & Entfernung von CARD_TRACKER.md

- *LÃ¶schung von `artifacts/CARD_TRACKER.md`*:
  - Nach Abschluss der Karten-Einbindung wurde die Datei `CARD_TRACKER.md` planmÃ¤ÃŸig gelÃ¶scht.
- *Entfernung aller Referenzen & Hinweise*:
  - Bereinigung aller ErwÃ¤hnungen, Spalten und TabelleneintrÃ¤ge zu `CARD_TRACKER.md` und `CARD_TRACKER` in:
    - `PROJECT.md` (Rolle Jadzia auf Karten-Status & Set-Abdeckung umgestellt, Tabellen bereinigt)
    - `BOTS.md` (Aufgabenbeschreibungen von Captain, Spock, Seven, Jadzia und PrÃ¼fliste neutralisiert)
    - `IMPLEMENT.md` (Statusabfragen und Ablauf bereinigt)
    - `UEBERGABE_PROMPT.md` (Lesereihenfolge und Pflichten bereinigt)
    - `EXTRACT_REST.md` (Verweise entfernt)
    - `FEATURES.md` (P0-Rangfolge und Header bereinigt)
    - `ONLINE_WORKFLOW.md` (Pflichtenliste bereinigt)
    - `APPENDIX_A_COVERAGE.md` (Ãœber 330 Fundstellen bereinigt)
    - `GLOSSARY_COVERAGE.md` (Ãœber 100 Spalten-/Statuszeilen bereinigt)
    - `HANDOFF.md` (Statusbeschreibungen und Canon-Tabellen bereinigt)

## 2026-09-26 â€” EXTRACT_REST: P5 (Battle-benachbarte Orchestrierung)

- *P5-04 (Subspace Schism SyncSchismRound & Draw-Discard)*:
  - In `GameSession.cs` die Schism-ZustÃ¤nde (`SchismUsedBy`, `SchismRound`, `SyncSchismRound()`, `IsSchismAvailable(player)`, `MarkSchismUsed(player)`) integriert. Bei Zugwechsel wird `SchismUsedBy` sauber zurÃ¼ckgesetzt.
  - In `InterruptRules.cs` `DecideSubspaceSchismResponse` (`SubspaceSchismPlan`) implementiert (prÃ¼ft Draw-Aktion, VerfÃ¼gbarkeit und steuert Discard der gezogenen Karte sowie Ersatz-Draw).
  - In `TableWindow.xaml.cs` `SyncSchismRound`, `SchismAvailable` und `MarkSchismUsed` an `GameSession` delegiert und die DrawCard-AbbruchauflÃ¶sung an `InterruptRules.DecideSubspaceSchismResponse` angebunden.
- *P5-03 (LegalResponsesFor & ApplyResponseEffect nach Rules)*:
  - In `TimingRules.cs` `ResponseEvaluationContext`, `IsResponseItemLegal` und `DecideResponseEffect` (`ResponseEffectPlan`) implementiert.
  - Ermittelt cancel/side-effects (Cancel-Target, CancelledBy, Tox Uthat Discard, Schism Mark, Escape Pod, Hail Fly-By) als reinen Regelplan ohne UI-AbhÃ¤ngigkeit.
  - In `TableWindow.xaml.cs` `CollectAllLegalResponses` und `ApplyResponseEffect` auf `TimingRules.IsResponseItemLegal` und `TimingRules.DecideResponseEffect` umgestellt.
- *P5-02 (ResolveTopOfStack Timing-Ablauf)*:
  - In `TimingRules.cs` `DecideCancelledPlayCard` (`CancelledPlayCardPlan`) implementiert (ermittelt Destination `ReturnToHand` vs `Discard`, Energy-Vortex-Flag und Status/Log-Texte).
  - In `TimingRules.cs` `ShouldExecuteResponsePlay` implementiert (entscheidet, ob eine Response als voller Interrupt aufgelÃ¶st werden muss oder an `SelfDestination` geht).
  - In `TimingRules.cs` `DecideShipBattleCancel` und `DecidePersonnelBattleCancel` implementiert (Borg-EOT-Attacker-Schutz, Stopped-Flags).
  - In `TableWindow.xaml.cs` `ResolveTopOfStack` verdÃ¼nnt und an die neuen `TimingRules`-Methoden angebunden.
- *P5-01 (TryResolveInterruptPlay Apply-Switch verdÃ¼nnt)*:
  - In `InterruptRules.cs` `CanPlayRogueBorg`, `CanPlayCrosis`, `IsLegalDisruptorOverloadTarget`, `DecideDisruptorOverloadVictim`, `IsLegalPalorToffCard`, `ParticleFountainPoints` und `DeathYellPoints` implementiert.
  - In `InterruptShipEffectRules.cs` `CalculateTranswarpRange` implementiert.
  - In `TableWindow.xaml.cs` `TryResolveInterruptPlay` bei Rogue Borg, Crosis, Disruptor Overload, Palor Toff, Particle Fountain, Death Yell und Transwarp an die Rules-Decide-Methoden angebunden.
- *Verifikation & Mini-Tests*:
  - In `TableWindow.xaml.cs` Schism-Reset auf `_session.SchismUsedBy` / `_session.SchismRound` korrigiert und `ship` Variablen-Scope in Crosis bereinigt.
  - VollstÃ¤ndiger Roslyn-Kompilierdurchlauf aller C#-Dateien der Solution erfolgreich (0 Fehler).
  - Mini-Tests `InterruptRules.VerifyP5InterruptRules()` und `TimingRules.VerifyP5TimingRules()` implementiert und in `ShipRules.VerifyShipRules()` eingehÃ¤ngt. Alle Tests bestehen (PASS).

## 2026-09-26 â€” EXTRACT_REST: P3 (Event-Persist Apply) & P4 (Dilemma-Persist Apply)

- *P3-R1 & P3-13 (Outpost & Spacedock Repair)*:
  - In `DockingRules.cs` `IsRepairFacility(Card? c)` und `FacilityRepairsImmediatelyOnDock(bool facilityHasSpacedock)` ausgelagert.
  - In `TableWindow.xaml.cs` `IsRepairFacility` und `TryDockShip` darauf umgestellt; `ProcessEndOfTurnRepairs` nutzt `EndOfTurnRestRules.DecideRepair` und Store `RepairTurns`.
  - Mini-Test `DockingRules.VerifyDockingRules()` hinzugefÃ¼gt.
- *P3-01 & P3-03 (Thermal Deflectors & The Traveler)*:
  - In `EventRules.cs` `HasThermalDeflectors` und `IsTravelerInPlay` als Engine-PrÃ¼fungen implementiert, die Store-Attachments und Tischkarten beider Spieler auswerten.
  - In `TableWindow.xaml.cs` `HasThermalDeflectors()` und `IsTravelerInPlay()` auf `EventRules` umgestellt.
- *P3-04 (Telepathic Alien Kidnappers)*:
  - In `EventRules.cs` `KidnapperValidCardTypes` und `DecideKidnappers(string? namedType, Card? revealedCard)` als pure Regelentscheidung eingefÃ¼hrt.
  - In `TableWindow.xaml.cs` `RunKidnappers` und `FinishKidnappers` auf die neuen `EventRules`-Definitionen umgestellt.
- *P3-05 & P3-09 (Traveler Extra Draws & Atmospheric Ionization)*:
  - In `BoardStore.cs` die Felder `PendingExtraDraws` und `IonizationBeamsThisTurnByPlayer` hinzugefÃ¼gt und in `Clear()` integriert.
  - In `EventRules.cs` `CanBeamUnderAtmosphericIonization(int plannedCount, int beamsThisTurnByController)` implementiert.
  - In `TableWindow.xaml.cs` `CanBeamAtMission` und `NoteIonizationBeam` an `EventRules` und `BoardStore` angebunden.
- *P3-06 & P3-07 (Neural Servo Device & Anti-Time Anomaly)*:
  - In `EventRules.cs` `DecideNeuralServoRestoredOwner` und `IsPersonnelOwnedByPlayer` implementiert.
  - In `TableWindow.xaml.cs` `RestoreNeuralServo` und `ApplyAntiTimeExpire` auf `EventRules` umgestellt.
- *P3-08 (Distortion Field)*:
  - In `EventRules.cs` `CanBeamThroughDistortionField(bool isDistortionFaceUp, bool hasPatternEnhancers)` implementiert.
  - In `TableWindow.xaml.cs` Beaming-PrÃ¼fung in `CanBeamAtMission` darauf umgestellt.
- *P3-10, P3-11 & P3-12 (Movement Hazards & Gaps Nullify)*:
  - `MovementHazardRules.VerifyMovementHazardRules()` und `GapsNullifyRules.VerifyGapsNullifyRules()` als Mini-Tests implementiert.
- *P3-15 & P3-16 (Cytherians Dest & Rogue Borg / Lore Returns)*:
  - In `EndOfTurnRestRules.cs` `RogueBorgTotalStrength`, `RogueBorgIndividualStrength`, `CanRogueBorgStaffShip` und Mini-Test `VerifyEndOfTurnRestRules()` implementiert.
  - In `TableWindow.xaml.cs` `ShipStaffedByRogueBorg`, `RogueBorgStrengthOn` und `RogueBorgStrengthEach` angebunden.
  - `BoardAttachedDilemma.DestInstanceId` bei Cytherians-Zuweisung synchronisiert.
- *P3-19 & P3-21 (Supernova & Red/Yellow Alert)*:
  - In `EventRules.cs` `DecideSupernova`, `DecideSupernovaCardAction` und `CanPlayRedAlertUnderYellowAlert` implementiert.
  - In `TableWindow.xaml.cs` `ApplySupernova` auf `EventRules.DecideSupernova` und `EventRules.DecideSupernovaCardAction` umgestellt.
- *P4 (Dilemma-Persist Apply)*:
  - Junior Officer, Nitrium/HyperAging/RemFatigue, Abduction, Phased, Cytherians, Edo Probe, Frame of Mind, Conundrum und Scow in `EXTRACT_REST.md` als ERLEDIGT markiert.
- *Verifikation*:
  - Alle neuen Mini-Tests (`DockingRules`, `EndOfTurnRestRules`, `MovementHazardRules`, `GapsNullifyRules`, `VerifyP3EventRules`) in `ShipRules.VerifyShipRules()` verankert und integriert.

## 2026-09-26 â€” EXTRACT_REST: P2 (Ship & Personnel Battle, Counter-Attack State & Escape Pod)

- *P2-S7 (Counter-Attack State in BoardStore & BattleRules)*:
  - Datenmodell `BattleRules.CounterAttackOpportunity` eingefÃ¼hrt (`EligiblePlayer`, `LocationMissionInstanceId`, `InvolvedOpponentInstanceIds`, `Armed`).
  - Helper `BattleRules.IsArmedCounterAttackAt`, `BattleRules.IsCounterAttackTarget`, `BattleRules.RegisterCounterAttack` und `BattleRules.UpdateCounterAttackWindow` implementiert.
  - `BoardStore.CounterAttack` als Single Source of Truth auf der Engine-Seite angelegt und in `Clear()` integriert.
  - In `TableWindow.xaml.cs` `IsArmedCounterAttackAt`, `IsCounterAttackTarget`, `RegisterCounterAttackOpportunity` und `UpdateCounterAttackWindow` so umgestellt, dass sie primÃ¤r `BoardStore.Current.CounterAttack` und `BattleRules` nutzen.
- *P2-S1 & P2-S2 (Ship Battle Zielwahl-Filter & Initiierung)*:
  - In `BattleRules.cs` `CanShipInitiateBattleAtLocation` und `IsLegalShipAttackTarget` implementiert (prÃ¼ft ungestoppt, ungedockt, ungetarnt, kein Required Move, WEAPONS > 0, Leader und Matching Affiliation).
  - In `TableWindow.xaml.cs` `BeginAttackMode` auf `BattleRules.CanShipInitiateBattleAtLocation` und `BattleRules.IsLegalShipAttackTarget` umgestellt.
- *P2-S3 & P2-S4 (Ship Battle Plan & Return Fire Orchestrierung)*:
  - In `BattleRules.cs` `DecideReturnFireEligibility` und `ExecuteShipBattlePlan` (`ShipBattlePlan`) implementiert. Berechnet Open Fire, Rotation Damage, Return Fire Checks & Boni, Winner und Folgestatus komplett als Regelplan.
  - In `TableWindow.xaml.cs` `AskReturnFireAndResolve` delegiert die EignungsprÃ¼fung an `BattleRules.DecideReturnFireEligibility`.
  - In `TableWindow.xaml.cs` `ResolveShipBattle` delegiert die GefechtsauflÃ¶sung vollstÃ¤ndig an `BattleRules.ExecuteShipBattlePlan` und fÃ¼hrt die Wirkungen (Damage, Stopped, Discard/Destroy, Reveal) aus dem Plan aus.
- *P2-S6, P2-E1 & P2-E2 (Destroy-Policy & Escape Pod Checks)*:
  - In `BattleRules.cs` `CanEscapePodRespond` ausgelagert.
  - In `InterruptRules.cs` `IsLegalEscapePodCrew` ausgelagert (filtert Nicht-Personal, Equipment und gefangenes Gegner-Personal heraus).
  - In `TableWindow.xaml.cs` `DestroyShipOrFacility`, `ShipHasCrewForEscapePod` und `ApplyEscapePodFromResponse` auf die neuen Rules-Methoden umgestellt.
- *P2-P1..P2-P4 (Personnel Battle)*:
  - In `BattleRules.cs` `CanOfferPersonnelBattle` ausgelagert; `CanOfferPersonnelBattleFromShip` in `TableWindow.xaml.cs` darauf umgestellt.
- *Verifikation*:
  - Neuer Mini-Test `BattleRules.VerifyBattleRulesPlan()` in `ShipRules.VerifyShipRules()` eingehÃ¤ngt und erfolgreich verifiziert (PASS).

## 2026-09-26 â€” EXTRACT_REST: P0 (Persist-Modell & Dual-Run) & P1 (Borg Ship EOT)

- *P0-D1 (`AttachedDilemma` aus Window nach Board)*:
  - Datenmodell `BoardAttachedDilemma` in `StarTrekCCG/Game/Board/BoardAttachments.cs` eingefÃ¼hrt mit `HostInstanceId`, `DestInstanceId`, `Direction`, `Held` und `OriginalEncounter`.
  - `BoardStore.AttachedDilemmas` angelegt, in `Clear()` integriert und in `ToBoardPieces()` als Engine-Snapshot-Pieces (`PieceRole.DilemmaPersist`) serialisiert.
  - In `TableWindow.xaml.cs` Helper `AddAttachedDilemma`, `RemoveAttachedDilemma` und `SyncAttachmentsToStore` verdrahtet. Alle `_attachedDilemmas.Add`/`Remove` umgestellt. `CaptureEngineState()` liest Dilemma-Attachments und QuarantÃ¤ne-Zustand direkt aus dem Store.
- *P0-E1 (`AttachedEvent` aus Window nach Board)*:
  - Datenmodell `BoardAttachedEvent` in `StarTrekCCG/Game/Board/BoardAttachments.cs` eingefÃ¼hrt mit `HostInstanceId`, `Host2InstanceId`, `TurnScope`, `PhasePoint`, `ScopePlayer` etc.
  - `BoardStore.AttachedEvents` angelegt, in `Clear()` integriert und in `ToBoardPieces()` als Engine-Snapshot-Pieces (`PieceRole.EventPersist`) serialisiert.
  - In `TableWindow.xaml.cs` Helper `AddAttachedEvent`, `RemoveAttachedEvent`, `RemoveAttachedEventsForCard` und EOT-Tick-Sync verdrahtet. Alle direkten Zugriffe auf `_attachedEvents.Add`/`Remove` umgestellt; `CaptureEngineState()` liest Store.
- *P0-S1 (Dual-Run abschlieÃŸen)*:
  - `ShipInstance` in `StarTrekCCG/Game/Board/CardInstance.cs` um `RepairTurns` und `CloakLocked` erweitert.
  - In `TableWindow.xaml.cs` Store als Single Source of Truth fÃ¼r Hull, Cloak, RepairTurns und CloakLocked etabliert: `GetRepairTurns`/`SetRepairTurns`, `IsCloakLocked`/`SetCloakLocked`, `IsShipCloaked`, `ApplyHullDamage`/`GetHullDamage` operieren auf Store-Instanzen. `ApplyUiStatusToStore` synchronisiert die Felder auf die Instanzen.
- *P1 (Borg Ship EOT)*:
  - `BorgShipRules.cs` in `StarTrekCCG/Game/BorgShipRules.cs` erstellt mit `Weapons = 24`, `Shields = 24`, `PointsOnDestroyed = 15`, `IsLegalTarget(...)`, `BorgWeaponsBonus(...)`, `BorgShieldsBonus(...)`, `DecideInitialDirection(...)` und `DecideMove(...)`.
  - In `TableWindow.xaml.cs`:
    - `StartBorgShipEotAttacks`: Ziele via `BorgShipRules.IsLegalTarget` gefiltert.
    - `AskReturnFireAndResolve`, `ResolveShipBattle`, `TryDestroyBorgShipInBattle`: Literal-24 und Hardcoded-15 durch `BorgShipRules`-Konstanten und Boni ersetzt.
    - `FinishBorgShipEotMove`: Bewegungs- und Verlassens-Logik vollstÃ¤ndig an `BorgShipRules.DecideMove(...)` delegiert.
    - `_borgShipDir` / `Direction` in `BoardAttachedDilemma` und `AttachedDilemma` abgelegt; Initialrichtung Ã¼ber `BorgShipRules.DecideInitialDirection` ermittelt.
  - In `artifacts/EXTRACT_REST.md`: Abschnitte P0 (P0-D1, P0-E1, P0-S1) und P1 als ERLEDIGT markiert.

## 2026-09-26 â€” Vulcan Mindmeld (144 U) Bugfix & Generisches Buried-Target-Peek-System

- *ModifierRules & Vulcan Mindmeld Bugfix (Kein Stacking auf Engineer x2)*:
  - Equipment-Skill-Grant-Regel (1E Glossar "Equipment" & "skills â€” modifying"): Equipment, das eine FÃ¤higkeit verleiht ("gain [skill]"), verleiht diese nur an Personal, das diese FÃ¤higkeit noch nicht besitzt. In `ModifierRules.ResolvePersonnel` wurde die PrÃ¼fung `if (skills.GetValueOrDefault(def.GrantedSkill) > 0) continue;` ergÃ¤nzt, sodass Data (gedruckt `ENGINEER: 1` und `OFFICER`) bei anwesendem *Engineering Kit* nicht fÃ¤lschlich `ENGINEER x 2` erhÃ¤lt.
  - Classification-Filterung bei Skill-Kopieren: In `TableWindow.ApplyVulcanMindmeld` wird die gedruckte Classification des Donors (`MissionRules.PrintedClassificationParts(skillDonor)`, z. B. `OFFICER` bei Data) vor der Skill-Ãœbertragung herausgefiltert, sodass nur regulÃ¤re Skills Ã¼bertragen werden.
  - Saubere Initialisierung temporÃ¤rer Skills: `ModifierRules.GrantTemporarySkills` erzeugt stets ein frisches Dictionary, um Nebeneffekte durch Mehrfachaufrufe auszuschlieÃŸen.
- *Generisches Buried-Target-Peek- und Drop-System*:
  - Generische Erkennung verdeckter Ziele: `TargetQuery.IsCardTargetingBuried` erkennt neben *Vulcan Mindmeld* und *Disruptor Overload* per Regex alle Karten mit Zielformulierungen auf Personal, Equipment oder Mindmeld (`plays on ... personnel/equipment/mindmeld`).
  - Erweiterung von `WantsBuriedPeek` und `CanTarget`: ErmÃ¶glicht Stack-Peek beim Draggen Ã¼ber Wirtselemente (Schiffe, AuÃŸenposten/Facilities, Planeten/Missionen mit Away Teams).
  - Hover & Detailfenster-Anzeige (`IsLegalPeekTarget`, `BuriedLegalOn`, `FindHostUnderWindow`, `UpdatePeekSnapAt`):
    - Beim Halten Ã¼ber einem Wirt mit legalen Zielen Ã¶ffnet sich nach 1s Haltezeit das Detailfenster (`CardDetailOverlay`).
    - Legale Ziele im Stapel leuchten cyan auf (`Color.FromRgb(80, 220, 255)`).
    - Beim Bewegen Ã¼ber das Mini rastet der Snap ein (`Color.FromRgb(40, 255, 120)` grÃ¼n).
    - SnapSite erzeugt fÃ¼r Play-On-Karten saubere Status-Meldungen (`Play on {hit.Name}`).
  - Drop-UnterstÃ¼tzung fÃ¼r Hand- und entsperrte Sidedeck-Karten (`ZoneMini_MouseUp`):
    - `isHandOrUnlockedSide` integriert (gilt fÃ¼r Hand und entsperrte Sidedecks wie *Q's Tent*).
    - Bei Vulcan Mindmeld: Droppen auf ein Personal im Detailfenster Ã¼bernimmt dieses direkt als `preselectedPersonnel` (Ã¼berspringt den Auswahldialog fÃ¼r den Mindmeld-Anwender) und schlieÃŸt das Detailfenster sauber.
    - Bei Events: Ermittelt bei offenem Detailfenster das Ziel bzw. den Wirt (`_eventPreferredHost`), schlieÃŸt das Detailfenster und platziert das Event regelkonform.
    - Bei Disruptor Overload: Droppen auf ein konkretes Equipment zerstÃ¶rt dieses direkt (`RemoveEquipmentFromHost`).
- *Tests & Verifikation*:
  - `InterruptRules.VerifyVulcanMindmeldDecide` um vollstÃ¤ndigen Sarek/Data/Engineering Kit-Fall erweitert:
    - Data behÃ¤lt `ENGINEER = 1` trotz anwesendem `Engineering Kit`.
    - Sarek erhÃ¤lt via Mindmeld `ENGINEER = 1` (nicht 2), `Computer Skill = 2`, `Music = 1`, `Astrophysics = 1`, `Exobiology = 1`.
    - Sarek behÃ¤lt seine eigenen Skills `Diplomacy = 3` und `Mindmeld = 1`.
    - Sarek erhÃ¤lt kein `OFFICER`.
    - Nach Expiry sind alle temporÃ¤ren Skills sauber bereinigt.
  - In `ShipRules.VerifyShipRules` eingehÃ¤ngt und verifiziert.

## 2026-09-26 â€” Interrupt Temporal Rift (140 U) & The Juggler (142 U)

- *SpacelineLocationRules* (Neue Architektur-Pipeline):
  - Zentralisierte Klassifizierung und Pipeline fÃ¼r alle Arten von Spaceline-Locations geschaffen (`IsTimeLocation`, `IsSpacelineLocation`, `PlaysAsSpacelineLocation`, `IsLandableSpacelineLocation`, `IsDifferentTimeContinuum`).
  - Standardisiert Karten, die als Spaceline Location fungieren (*Time Travel Pod*, *Temporal Rift*, zukÃ¼nftige Zeit- und Raumlinienkarten).
  - Verhindert regulÃ¤re Warp-FlÃ¼ge zwischen Zeitorten und der regulÃ¤ren Raumlinie (`IsDifferentTimeContinuum`).
  - Vereinheitlichung in `TableWindow`: `IsLandableLocation`, `BuildSpacelineDisplayOrder`, `GetSpacelineQuadrant` und `IsWormholeLocation` greifen nun auf `SpacelineLocationRules` zu.
  - Dedizierte, wiederverwendbare Platzierungspipeline `PlaceSpacelineTimeLocation` geschaffen, die von `PlaceTimeTravelPod` und `PlaceTemporalRift` geteilt wird.
- *Temporal Rift* (Premiere 140 U / 322 C):
  - Regelkonforme Umsetzung nach aktuellem Errata & Rulings:
    - Text: *"Plays on table as a universal space time location; relocate one of your exposed ships OR a dilemma here. Counts down only at the start of your turn. When nullified, return that ship or dilemma to its former location."*
    - Response- & Flucht-Sperre: `TimingRules.CanRespond` verbietet *Temporal Rift* als Antwort auf Kampf (`InitiateShipBattle`, `InitiatePersonnelBattle`) oder Dilemma-Begegnung (`EncounterDilemma`).
    - Exposed-Bedingung: Nur exposed Schiffe (`ShipRules.IsShipExposed`: ungedockt, ungetarnt, unphased, nicht gelandet/getragen) kÃ¶nnen versetzt werden.
    - Zielauswahl: UnterstÃ¼tzt Drag & Drop auf exposed Schiffe, On-Board-Auswahl via gelbem Glow (`BeginBoardPickShip`) oder Auswahl eines aktiven Dilemmas im Spiel.
    - Dilemma-Relocate: Versetzt Dilemmas (inkl. Borg Ship / Scow Token) an den Zeitort und stellt sie bei Ablauf/Nullify an ihren vorherigen Wirtsort zurÃ¼ck.
    - Timing & Countdown: ZÃ¤hlt nur zu Beginn des Zuges des Besitzers herunter (`ProcessTemporalRiftCountdowns` in `ProcessStartOfTurnTimedEffects`).
    - RÃ¼ckkehr: Bei Nullify (via Kevin Uxbridge o. Ã„., `OnCardLeftPlay`) oder nach Ablauf von Countdown 2 kehrt das Schiff bzw. das Dilemma an den ursprÃ¼nglichen Ort zurÃ¼ck.
    - ImmunitÃ¤t / Pausierung: Schaden und Countdowns von Schiffseffekten (z. B. *Plasma Fire*, *Warp Core Breach*) pausieren am Zeitort (`IsShipAtTimeLocation`).
    - Detailstatus: Zeigt Countdown und anwesende Schiffe/Dilemmas im Detailblock an.
- *The Juggler* (Premiere 142 U / 326 C):
  - Verifiziert und verbessert: WÃ¤hlt Spieler aus (`AskPlayer`), mischt dessen Nachziehstapel per RNG neu und protokolliert dies detailliert im Log und der Statuszeile.
  - Als funktionierend (`working`) verifiziert.
- Tests & Verifikation:
  - `InterruptRules.VerifyTemporalRiftDecide` implementiert und in `ShipRules.VerifyShipRules` integriert (alle Checks PASS).
  - `SpacelineLocationRules.VerifySpacelineLocationRules` validiert Zeitort- und Raumlinienregeln.
  - `dotnet build /p:EnableWindowsTargeting=true` erfolgreich (0 Fehler).

## 2026-09-25 â€” Interrupt Scan (295 C) & Tachyon Detection Grid (318 U)

- *Scan* (Premiere 295 C):
  - Regelkonforme Implementierung als GegenstÃ¼ck zu *Full Planet Scan* fÃ¼r Weltraummissionen:
    - Timing-Gate: Spielbar zu Beginn des Zuges (`TimingRules.RequiresStartOfTurnWindow`, Segment 1, vor Ausspielen der regulÃ¤ren Karte).
    - Ziel: Eigenes Schiff an einer [S]-Mission (`!MissionCountsAsPlanetCard(mc)`) mit mindestens zwei gedruckten Staffing-Icons (`[Cmd]` / `[Stf]`).
    - Kosten: Stoppen von ungestopptem `Computer Skill` und `Stellar Cartography` an Bord (bevorzugt zwei getrennte Crew-Mitglieder; unterstÃ¼tzt auch Einzelpersonal mit beiden FÃ¤higkeiten).
    - Effekt: Unterste Seed-Karte der Mission wird aufgedeckt (`ShowCardReveal`) und untersucht, Personal wird gestoppt, Karte wird abgelegt.
  - On-Board Picking & Snap-Glow:
    - Bei Ausspielen ohne Drop-Ziel werden alle legalen Schiffe am Tisch ermittelt (`FindLegalScanShips`) und via `PickBoardTarget` mit grÃ¼nem Glow hervorgehoben und direkt auf dem Tisch auswÃ¤hlbar gemacht.
    - Drag & Drop Snap-Glow (`HostMatchesInterruptTargetForCard`, `GetLegalInterruptPlayHosts`) hebt nur eigene Schiffe an Space-Missions mit >=2 Staffing hervor.
- *Tachyon Detection Grid* (Premiere 318 U):
  - Standardisierung & Korrektur auf offizielle Regeln:
    - Voraussetzung: Spieler muss mindestens 4 exposed Schiffe im Spiel kontrollieren (`CountExposedShips >= 4`).
    - Exposed-Definition aus `ShipRules.IsShipExposed` verwendet: ungedockt, ungetarnt, unphased, nicht gelandet, nicht getragen. Getarnte oder gedockte Schiffe zÃ¤hlen nicht zu den 4 Schiffen.
    - Ziel: Ein beliebiges getarntes Schiff auf dem Tisch (Gegner oder eigenes).
    - Effekt: Schiff enttarnt sich sofort (`SetShipCloaked(host, false)`), selbst wenn es gestoppt ist oder sich in diesem Zug bereits getarnt hat.
    - Cloak-Lock: Wirtschiff wird bis zum Ende des Zuges fÃ¼r erneutes Tarnen gesperrt (`_cloakLocked`, via `TurnExpiry`).
  - Target-Selection Pipeline:
    - Wenn nicht direkt auf ein getarntes Schiff abgelegt, werden alle getarnten Schiffe auf dem Tisch ermittelt. Bei mehreren Schiffen leuchtet `PickBoardTarget` mit violettem Glow fÃ¼r direkte Klick-Auswahl.
    - Drag & Drop Snap-Glow hebt nur getarnte Schiffe hervor und wird sofort unterdrÃ¼ckt, falls der Spieler weniger als 4 exposed Schiffe besitzt.
    - Pre-Stack Validierung in `CanPlayCardWithReason` verhindert illegales Ausspielen ohne 4 exposed Schiffe oder ohne getarnte Schiffe.
- *ReturnInterruptToHand*:
  - Bereinigt bei Abbruch oder Fehlern die Karte zusÃ¤tzlich aus dem Ablagestapel (`_discardCards` / `_oppDiscardCards`), um doppelte Kartenreferenzen zu verhindern.
- Tests & Verifikation:
  - `InterruptShipEffectRules.VerifyTachyonDecide` und `VerifyScanDecide` implementiert und in `ShipRules.VerifyShipRules` integriert (alle PASS).
  - Status von *Q2* und *Subspace Schism* als funktionierend (`working`) verifiziert und dokumentiert.
  - `dotnet build` erfolgreich (0 Fehler).

## 2026-09-25 â€” Einheitliche On-Board Zielauswahl-Pipeline & Ship Seizure (136 C) Board-Pick

- UX-Architektur & Einheitliche Pipeline:
  - `PickBoardTarget` in `TableWindow.xaml.cs` als zentrale Pipeline fÃ¼r die direkte Auswahl von Karten/Objekten auf dem Spielfeld (Schiffe, Spaceline Locations, AuÃŸenposten/Facilities etc.) implementiert:
    - Legale Ziele werden direkt auf dem `TableCanvas` mit einem animierten Halo/Glow hervorgehoben (`AddBoardTargetGlow`).
    - Mauszeiger wechselt Ã¼ber Zielobjekten auf `Cursors.Hand`.
    - Das erste Ziel wird bei Bedarf automatisch in den sichtbaren Bildbereich gescrollt.
    - Modale Interaktion via `DispatcherFrame`, sodass Karteneffekte synchron auf die getroffene Wahl warten kÃ¶nnen, ohne den UI-Thread zu blockieren.
    - Ein Klick auf ein markiertes Ziel wÃ¤hlt es aus; Klick auf leere TischflÃ¤che oder Rechtsklick bricht die Auswahl ab und setzt das Ziel auf `null`.
    - Escape-Taste bricht die Auswahl ebenfalls sauber ab.
    - VollstÃ¤ndiges AufrÃ¤umen aller Glow-Rechtecke und Wiederherstellen der ursprÃ¼nglichen Mauszeiger im `finally`-Block.
  - `PickBorderFromList` modernisiert:
    - Wenn die Ã¼bergebenen Zielgrenzen (`candidates`) sichtbare Karten auf dem `TableCanvas` sind (z. B. Schiffe, Missionen, Einrichtungen), leitet `PickBorderFromList` automatisch an `PickBoardTarget` weiter, statt ein Detailfenster/Popup-Streifen (`PickCardFromList`) zu Ã¶ffnen.
    - Nicht auf dem Tisch liegende Auswahlen (z. B. Personal in Crew-Stapeln bei *Genetronic Replicator*) nutzen weiterhin sicher die Scroll-Streifen-Detailansicht.
  - `PickCardOnBoard`: Komfort-Methode zur AuflÃ¶sung von `Card`-Listen auf dem Spielfeld in Border-Ziele fÃ¼r `PickBoardTarget`.
- Integration bei Karten:
  - *Ship Seizure* (136 C):
    - WÃ¤hlt das zu zerstÃ¶rende leere, ungeschÃ¼tzte Schiff (`victim`) nicht mehr Ã¼ber ein Detailfenster (`PickCardFromList`), sondern lÃ¤sst alle legalen Opfer am Ort auf dem Spielfeld mit bernsteinfarbenem Glow erstrahlen.
    - Spieler klickt das Zielschiff direkt auf dem Spielplan an.
    - Bei ungedropptem Ausspielen (z. B. Klick auf Ausspielen) werden auch die eigenen Schiffe mit Tractor Beam direkt auf dem Spielfeld grÃ¼n markiert und zur Auswahl angeboten.
    - Bei Abbruch (Rechtsklick) wandert *Ship Seizure* sauber auf die Hand zurÃ¼ck (`ReturnInterruptToHand`).
  - *Incoming Message*: Auswahl der Ziel-Facility auf der Spaceline lÃ¤uft nun Ã¼ber `PickBoardTarget` mit zyanfarbenem Glow direkt auf dem Tisch.
  - *Kurlan Naiskos*, *Alien Parasites*, *Kevin Uxbridge: Convergence* und `ShowTargetPickDialog` (*Conundrum*, *Anti-Matter Pod*, etc.): Nutzen via `PickBorderFromList` nun alle die einheitliche Board-Target-Pipeline.
  - Drag-and-Drop Snap-Glow bleibt fÃ¼r das direkte Ziehen von Karten aus der Hand oder dem Side-Deck auf Hosts unverÃ¤ndert intakt.
- Tests & Build:
  - `dotnet build` erfolgreich (0 Fehler, 2 bestehende Warnungen).

## 2026-09-25 â€” Ship Rules Pipeline & Ship Seizure (136 C) Standardisierung

- Architektur & Pipeline:
  - `ShipRules.cs`: Zentrale, wiederverwendbare Pipeline fÃ¼r Ship-, Facility- und Site-Begriffe nach aktuellem Regelbuch/Glossar (Stand 1. Januar 2024) implementiert:
    - `exposed`: Ein Schiff ist exposed, wenn es ungedockt (`!isDocked`), ungetarnt (`!isCloaked`), unphased (`!isPhased`) und weder gelandet noch getragen ist (`!isLanded && !isCarried`).
    - `occupied`: Ein Schiff, eine Einrichtung oder eine Site ist occupied, wenn mindestens ein Personnel an Bord ist (`aboard.Any(ModifierRules.IsPersonnelCard)`). Equipment oder Interrupts (z. B. Rogue Borg Tokens) allein machen einen Host gemÃ¤ÃŸ Ruling vom 1. Jan. 2024 nicht occupied.
    - `unoccupied` / `empty`: Ein Schiff/Facility/Site ohne Personnel an Bord ist empty.
    - `empty exposed ship`: Kombinierte Bedingung fÃ¼r leere und ungeschÃ¼tzte Schiffe.
    - `your ship`: PrÃ¼fung auf Schiffsbesitz/Kontrolle (`shipOwner == player`).
    - `tractor beam`: Erkennt Tractor Beam sowohl im Text als auch in den `Characteristics` eines Schiffes.
    - `CanBeShipSeizureTractorHost`: Validiert das Wirtschiff fÃ¼r *Ship Seizure* (eigenes Schiff mit Tractor Beam).
    - `CanBeShipSeizureVictim`: Validiert das Zielschiff (ein anderes Schiff am selben Ort, leer und exposed).
    - `VerifyShipRules`: Umfassender Mini-Test fÃ¼r alle Permutationen, GrenzfÃ¤lle und Rulings.
- Vereinheitlichung bestehender Karten & Mechaniken:
  - `MovementRules.cs`: `ShipHasSpecialEquipment` prÃ¼ft neben `Text` auch `ship.Characteristics`.
  - `PlayOnRules.cs`: `Spec` um `TractorBeam` erweitert; `BuildSpecFromClause` erkennt "tractor beam" automatisch in Play-On-Klauseln.
  - `TargetQuery.cs`: `HostFacts` um `HasTractorBeam` erweitert; `MatchPlayOnSpec` prÃ¼ft `facts.HasTractorBeam`.
  - `TableWindow.xaml.cs`:
    - `IsShipExposed(Border ship)` delegiert direkt an `ShipRules.IsShipExposed(IsShipDocked(ship), IsShipCloaked(ship))`.
    - `CountExposedShips` (*Tachyon Detection Grid*): PrÃ¼fte zuvor nur Cloak und ignorierte Docking; nun vereinheitlicht auf `IsShipExposed`.
    - `DefenderExposed` (*Asteroid Sanctuary*): PrÃ¼fte zuvor nur Cloak; nun vereinheitlicht auf `IsShipExposed`.
    - `CollectLegalSnapHosts`: BerÃ¼cksichtigt `ShipRules.CanBeShipSeizureTractorHost` fÃ¼r Halos und Drop-Targets.
    - `HostMatchesInterruptTargetForCard` & `HostMatchesPlayOn`: Nutzen `ShipRules.CanBeShipSeizureTractorHost` bzw. `ShipRules.HasTractorBeam`.
    - `ApplyShipSeizure`: Validiert Tractor-Wirt mit `ShipRules.CanBeShipSeizureTractorHost`, filtert Opfer mit `ShipRules.CanBeShipSeizureVictim` und gibt den Interrupt bei illegalem Ziel oder Abbruch sauber auf die Hand zurÃ¼ck (`ReturnInterruptToHand`). Lokales Duplikat `IsShipSeizureExposed` entfernt.
  - `InterruptRules.cs`: `IsLegalShipSeizureTractor` und `IsLegalShipSeizureVictim` an `ShipRules` angebunden; `VerifyShipSeizureDecide` fÃ¼hrt `ShipRules.VerifyShipRules` aus.
- Tests:
  - `ShipRules.VerifyShipRules` und `InterruptRules.VerifyShipSeizureDecide` erfolgreich ausgefÃ¼hrt (PASS).
  - Projekt erfolgreich gebaut (`dotnet build`, 0 Fehler).

## 2026-09-25 â€” Particle Fountain (132 C)

- Feature: Premiere-Interrupt *Particle Fountain* (132 C) implementiert.
- Gametext: *"Plays if your Away Team just solved a planet mission. If 2 ENGINEER in Away Team, score points. 5"*
- Rulings & Regeln:
  - Trigger: Spielt direkt im Anschluss an das LÃ¶sen einer Planeten-Mission durch das eigene Away Team. Nutzt die bestehende `MissionJustSolved` Action-/Response-Pipeline (analog zu *Alien Groupie*).
  - Bedingung: Mindestens 2 ENGINEER im lÃ¶senden Away Team erforderlich. Effektive Fertigkeitslevel (`DilemmaRules.CountEffectiveSkill`) berÃ¼cksichtigen gedruckte FÃ¤higkeiten, Klassifikation und AusrÃ¼stung (z. B. Engineering Kit, Engineering PADD).
  - Effekt: Verleiht dem ausspielenden Spieler sofort 5 Punkte (`_scoreP1 += 5` bzw. `_scoreP2 += 5`), aktualisiert das Scoreboard (`UpdateScoreDisplay()`), loggt das Ereignis und legt die Karte auf den Ablagestapel.
- Implementierung:
  - `InterruptRules.cs`: `IsParticleFountain(Card? c)` und Gate-Validierung `CanPlayParticleFountain(justSolvedPlanet, isOwnSolve, engineerCount)` hinzugefÃ¼gt; Mini-Test `VerifyParticleFountainDecide` prÃ¼ft alle Gates und Response-FÃ¤lle.
  - `TimingRules.cs`: `Particle Fountain` in `IsCatalogResponse` aufgenommen; `CanRespond` validiert `ActionKind.MissionJustSolved`, eigene Mission, Planeten-Typ und 2 effektive ENGINEER.
  - `TableWindow.xaml.cs`:
    - `ResolveTopOfStack`: Erkennt `IsParticleFountain` als Stack-Response und leitet an `TryResolveInterruptPlay` weiter.
    - `TryResolveInterruptPlay`: Schreibt 5 Punkte fÃ¼r `controller` gut, ruft `UpdateScoreDisplay()` auf und loggt die Wertung.
    - `TryPlayInterruptFromHand`: Erlaubt das Ausspielen sowohl als direkte Stack-Response als auch wÃ¤hrend des offenen Just-Solved-Fensters mit 2-ENGINEER-PrÃ¼fung.
    - `OpenMissionJustSolvedResponse` & `ArmJustSolvedPlanet`: BerÃ¼cksichtigen auch Equipment-Karten im Away Team fÃ¼r `CountEffectiveSkill`.
- Mini-Test: `VerifyParticleFountainDecide` erfolgreich ausgefÃ¼hrt (PASS).

## 2026-09-24 â€” Near-Warp Transport (130 U) UI & Beaming-Mechanik-Refactoring

- UX/Mechanik: Interaktions-Flow fÃ¼r *Near-Warp Transport* (130 U) auf die regulÃ¤re Beam-Mechanik umgestellt:
  - Verwendet nun dieselbe Ansicht wie die normale Beam-Mechanik: Schiffsdetail-Overlay im Beam-Auswahlmodus (`ShowHostContents(shipB, sc, beamSelectMode: true)`).
  - Crew und Equipment an Bord des Schiffes werden mit Checkboxen angezeigt (`_hostStripBeam = true`, `_beamSelected`).
  - Karten kÃ¶nnen durch Klick auf die Checkbox oder direkt durch Klick auf die Mini-Karte an-/abgewÃ¤hlt werden.
  - Begrenzung auf maximal 6 Karten (gemÃ¤ÃŸ Kartentext "up to six cards"): Bei Auswahl von mehr als 6 Karten wird die Auswahl verhindert und ein Hinweisdialog angezeigt.
  - Button "Select max (6)" / "Select none" im Detailfenster zur schnellen Auswahl.
  - SchlieÃŸen-Button im Detailfenster zeigt wÃ¤hrend Near-Warp Transport `"Beam Crew"` an; erfordert mindestens 1 ausgewÃ¤hlte Karte und schlieÃŸt das Fenster fÃ¼r die Zielauswahl auf der Spaceline.
  - Alle legalen Ziele auf benachbarten Spaceline-Locations (eigene Schiffe/Einrichtungen und PlanetenoberflÃ¤chen) werden simultan mit grÃ¼nem Halo hervorgehoben.
  - Zielwahl erfolgt direkt per Klick auf das hervorgehobene Ziel auf der Spaceline; Klick auf das Ausgangsschiff Ã¶ffnet das Detailfenster zur Anpassung erneut; Rechtsklick/Leerklick bricht ab und nimmt die Karte zurÃ¼ck auf die Hand.
  - Transport prÃ¼ft Spaceline-Adjazenz, Verbot von Beamen ins freie All (7.1.1.0.1), Hindernisse (`CanBeamAtMission`), VertrÃ¤ge (`TreatyRules.CanOccupyHost`), Hologramme (`FilterHoloBeamAllowed`), heilt Dilemmata und aktualisiert Badges, QuarantÃ¤ne und Logs.

## 2026-09-24 â€” Fix: Compiler-KompatibilitÃ¤t DetailStatusRules, EventRules & TableWindow (Loss of Orbital Stability)

- Bugfix (Build/Compiler): Visual Studio meldete 8 Compilerfehler beim Kompilieren von `TableWindow.xaml.cs`:
  - `"DetailStatusRules" enthÃ¤lt keine Definition fÃ¼r "IsDebuff"` (2x)
  - `"EventRules.Persist" enthÃ¤lt keine Definition fÃ¼r "LossOfOrbitalStability"` (2x)
  - `Keine Ãœberladung fÃ¼r die ToneForEvent-Methode nimmt 3 Argumente an` (4x)
- Root Cause:
  - `TableWindow.xaml.cs` rief `DetailStatusRules.IsDebuff` und `DetailStatusRules.ToneForEvent(ae.Kind, ae.Countdown, ae.Card)` mit 3 Argumenten auf und setzte `Kind = EventRules.Persist.LossOfOrbitalStability`.
  - Wenn `TableWindow.xaml.cs` gegen die Standardversion von `DetailStatusRules.cs` und `EventRules.cs` kompiliert wurde (wo `LossOfOrbitalStability` als Interrupt nicht in `EventRules.Persist` existiert und `ToneForEvent` 2 Argumente hat), schlug der Build fehl.
- Fix:
  - `TableWindow.xaml.cs`:
    - Eigene private Hilfsmethoden `ToneForAttachedEvent(ae)` und `IsAttachedEffectDebuff(ae)` eingefÃ¼hrt, die `Loss of Orbital Stability` direkt Ã¼ber `InterruptRules.IsLossOfOrbitalStability(ae.Card)` erkennen und fÃ¼r Events den standardmÃ¤ÃŸigen 2-Argument-Aufruf `DetailStatusRules.ToneForEvent(ae.Kind, ae.Countdown)` bzw. `DetailStatusRules.ToneForEvent(ae.Kind, 0)` verwenden.
    - `ApplyLossOfOrbitalStability`: Verwendet wieder `Kind = EventRules.Persist.None` wie auf `master`.
    - `FormatAttachedHostEffectLine`: Formatiert die Zusammenfassung fÃ¼r `Loss of Orbital Stability` direkt ohne AbhÃ¤ngigkeit von `EventRules.Persist.LossOfOrbitalStability`.
  - `DetailStatusRules.cs`:
    - Beide Ãœberladungen von `ToneForEvent` bereitgestellt: `(EventRules.Persist kind, int countdown)` (2 Argumente) sowie `(EventRules.Persist kind, int countdown, Card? card)` (3 Argumente).
    - `IsDebuff(EventRules.Persist kind)` und `IsDebuff(EventRules.Persist kind, Card? card = null)` bereitgestellt.
    - AbhÃ¤ngigkeit von `EventRules.Persist.LossOfOrbitalStability` entfernt.
    - `VerifyLossOfOrbitalStabilityNegative`: Verwendet `EventRules.Persist.None`.
- Ergebnis: Saubere KompatibilitÃ¤t sowohl mit altem als auch neuem `DetailStatusRules`/`EventRules`, 0 Compilerfehler.

## 2026-09-24 â€” Near-Warp Transport (130 U)

- Feature: Premiere-Interrupt *Near-Warp Transport* (130 U) implementiert.
- Gametext: *"Plays to beam up to six cards (personnel and/or [Equipment]) from your exposed ship with transporters to an adjacent spaceline location (if possible)."*
- Rulings & Regeln:
  - *Glossary: exposed*: Ein Schiff ist exposed, wenn es ungedockt (`!IsShipDocked`), nicht getarnt (`!IsShipCloaked`), unphased und nicht gelandet/getragen ist. `IsShipExposed` in `TableWindow.xaml.cs` prÃ¼ft nun sauber auf `!IsShipCloaked(ship) && !IsShipDocked(ship)`.
  - *Glossary: adjacent*: Zwei Spaceline-Locations sind benachbart, wenn keine andere Location zwischen ihnen liegt â€” auch wenn eine Nicht-Location-Karte wie Q-Net dazwischen liegt. `GetAdjacentSpacelineLocations` filtert Spaceline-Span-Barrieren heraus.
  - *Rulebook 7.1.1.0.2 Card-Activated Transport*: Q-Net blockiert Near-Warp Transport nicht, Hindernisse fÃ¼r Beaming (z. B. Distortion Field, Atmospheric Ionization) gelten jedoch weiterhin und werden Ã¼ber `CanBeamAtMission` geprÃ¼ft.
  - *Rulebook 7.1.1.0.1*: Beamen ins freie All an Space-Missionen ist verboten; an Space-Locations wird ein eigenes Schiff oder eine eigene Station als Ziel verlangt.
- Implementierung:
  - `InterruptRules.cs`: `IsNearWarpTransport` hinzugefÃ¼gt; `GetPlayTarget` liefert `PlayTarget.OwnShip`; `Resolve` mappt auf `Kind.Instant`, `Effect.NearWarp`, `DiscardAfter: true`.
  - `InterruptShipEffectRules.cs`: `NearWarpTransportDeny` mit Validierung fÃ¼r Schiff, Eignerschaft, Exposed-Status, Transporter, beamfÃ¤hige Crew/Equipment und benachbarte Spaceline-Locations im selben Quadranten; Mini-Test `VerifyNearWarpTransportDecide`.
  - `TargetQuery.cs`: `CanPlayOn` validiert `facts.IsShip`, `facts.Owner == player` und `facts.Exposed`.
  - `TableWindow.xaml.cs`:
    - `HostMatchesInterruptTargetForCard` und `CollectLegalSnapHosts` filtern auf eigene exposed Schiffe.
    - `ExecuteInterruptAction`: Behandelt `Effect.NearWarp` via `ApplyNearWarpTransport`.
    - `ApplyNearWarpTransport`: FÃ¼hrt Kartenauswahl (bis zu 6 Personnel/Equipment), Wahl der benachbarten Spaceline-Location (links/rechts Dialog bei Verzweigung), Wahl des Ziel-Hosts (PlanetenoberflÃ¤che oder eigenes Schiff/Einrichtung), Treaty- und Holo-Checks durch, fÃ¼hrt den Transport durch und aktualisiert Badges, Visuals und Logs.
- Smoke: `GROK_TEMP/SMOKE_NEAR_WARP_TRANSPORT.md`. Tracker `working`.

## 2026-09-24 â€” Loss of Orbital Stability (129 C) Debuff/Negative-Fix

- Bugfix (UX/Classification): *Loss of Orbital Stability* wurde nach dem Anheften an ein Schiff im Schiffsdetail fÃ¤lschlicherweise als "Positive" mit grÃ¼nem Label und als "Event" angezeigt.
- Root Cause:
  - `DetailStatusRules.ToneForEvent` lieferte bei `countdown > 0` pauschal `DetailStatusTone.Timer`, und fÃ¼r `Persist.None` `DetailStatusTone.Info`.
  - In `TableWindow.xaml.cs` filterte `positiveEvents` auf `!= DetailStatusTone.Debuff`, wodurch der zerstÃ¶rerische Interrupt unter "Positive" einsortiert und mit dem PrÃ¤fix "Event" versehen wurde.
- Fix:
  - `EventRules.cs`: `EventRules.Persist.LossOfOrbitalStability` hinzugefÃ¼gt und in `FormatHostEffectSummary` als `"ship has NO RANGE; destroyed at end of owner's next turn"` definiert.
  - `DetailStatusRules.cs`: `IsDebuff` eingefÃ¼hrt, welches `LossOfOrbitalStability` sowie alle schÃ¤dlichen persistierenden Effekte (`PlasmaFire`, `WarpCore`, `Baryon`, etc.) und per Card-Name erkennt. `ToneForEvent` priorisiert `IsDebuff` (liefert `DetailStatusTone.Debuff` / rot auch bei Countdown).
  - `TableWindow.xaml.cs`:
    - `positiveEvents` und `negativeEvents` trennen sauber nach `DetailStatusRules.IsDebuff`.
    - Mini-Karten und Statuszeilen erkennen `Interrupt` (zeigen `"Interrupt â€” countdown 1"` bzw. `"Interrupt (debuff)"` statt `"Event"`).
    - `FormatAttachedHostEffectLine` unterstÃ¼tzt Interrupts und formatiert die Effektzusammenfassung.
    - `ApplyLossOfOrbitalStability` setzt `Kind = EventRules.Persist.LossOfOrbitalStability`.
  - Tests: `DetailStatusRules.VerifyLossOfOrbitalStabilityNegative()` als Regressions-Check hinzugefÃ¼gt.

## 2026-09-24 â€” Loss of Orbital Stability (129 C) Target-Fix

- Bugfix (Targeting): *Loss of Orbital Stability* gab fÃ¤lschlicherweise eine Planeten-Mission als Snap/Target an anstatt ein Schiff.
- Root Cause: In `PlayOnRules.BuildSpecFromClause` wurde `planet`/`[p]` vor `ship` geprÃ¼ft, wodurch Clauses wie `"a ship orbiting a [p]"` zu `Host.PlanetMission` statt `Host.Ship` evaluierten.
- Fix:
  - `PlayOnRules.cs`: `c.Contains("ship")` vor `c.Contains("planet") || c.Contains("[p]")` priorisiert, sodass Schiffe mit Ortsangaben als `Host.Ship` geparst werden.
  - `InterruptRules.cs`: `GetPlayTarget` liefert fÃ¼r *Loss of Orbital Stability* `PlayTarget.AnyShip`.
  - `TargetQuery.cs`: `CanPlayOn` verifiziert fÃ¼r *Loss of Orbital Stability* `facts.IsShip` und `facts.IsOrbitingPlanet`. `HostFacts` um `IsOrbitingPlanet` erweitert.
  - `TableWindow.xaml.cs`: `FactsFor` und `FactsForPrinted` berechnen `IsOrbitingPlanet`. `HostMatchesInterruptTargetForCard` und `CollectLegalSnapHosts` prÃ¼fen `IsShipOrbitingPlanet`.
- Smoke: `GROK_TEMP/SMOKE_LOSS_OF_ORBITAL_STABILITY.md` aktualisiert. Tracker `partial`.

## 2026-09-24 â€” Loss of Orbital Stability (129 C)

- Feature: Premiere-Interrupt *Loss of Orbital Stability* (129 C) implementiert.
- Bedingung: Spielt auf ein Schiff im Orbit eines Planeten [P] (Glossary "in orbit": im Weltall, ungedockt, an einer Planeten-Mission; `InterruptShipEffectRules.LossOfOrbitalStabilityDeny`).
- Soforteffekt: Zielschiff hat fÃ¼r den restlichen Zug keine Reichweite (`SetShipRangeLeft = 0`).
- Schilde-PrÃ¼fung:
  - Falls effektive SHIELDS > 4: Interrupt wird sofort nach Reichweitenverlust auf den Discard gelegt.
  - Falls effektive SHIELDS <= 4: Interrupt wird an das Schiff angehÃ¤ngt (`_attachedEvents`), und das Schiff wird am Ende des nÃ¤chsten Zuges seines Eigners zerstÃ¶rt (`TimingRules.TurnScope.SpecificPlayerNextTurn` / `TurnPhasePoint.EndOfTurn` via `ProcessEndOfTurnEvents` / `DestroyShipOrFacility`).
- Smoke: `GROK_TEMP/SMOKE_LOSS_OF_ORBITAL_STABILITY.md`. Tracker `partial`. Kein Push.

## 2026-09-24 â€” Klingon Right of Vengeance & Life-form Scan working (Pepsch green)

- *Klingon Right of Vengeance* (126 C) und *Life-form Scan* (127 U) im Probespiel verifiziert und grÃ¼n gemeldet. Tracker auf working gesetzt.

## 2026-09-23 â€” Klingon Right of Vengeance (Â§ 7.4.2, Â§ 7.4.4, Klasse B/A)

- Feature: Premiere-Interrupt *Klingon Right of Vengeance* (126 C) implementiert als `JustAfter(PersonnelBattleKlingonDied)`-Response.
- Timing & Trigger: Ã–ffnet sich unmittelbar nach einer Personnel Battle, in der mindestens ein Klingone gestorben und im Discard gelandet ist (Genetronic Save schlieÃŸt Trigger aus; sequentiell nach ggf. anstehendem Death Yell).
- Effekt: Entstoppt eigene Klingonen am Host (`UnstopBorder`), initiiert unmittelbaren Gegenangriff gegen die Ã¼berlebenden Kombatanten der Gegenseite ("same opponents"). Bypasst Leader-Pflicht (Â§ 7.4.1 / `HasLeader`). Verdoppelt STRENGTH aller angreifenden Klingonen fÃ¼r diese Schlacht (`sa *= 2` bei Pairings und Live-STRENGTH).
- Smoke: `GROK_TEMP/SMOKE_KLINGON_RIGHT_OF_VENGEANCE.md`. Tracker partial. Kein Push.

## 2026-09-23 â€” Death Yell after Escape Pod Pass (ResolveEntireStack drain)

- Root: Plasma/WCB Destroy â†’ Escape Pod ShipDestroyed Pass â†’ ResolveEntireStack while-loop re-entered Destroy Results, TryFlushJustAfterDeathWindows opened JustAfter mid-loop, same loop immediately popped JustAfter as passed (~4838) â†’ Yell UI never stayed open. Without Pod, Destroy runs inline (not under drain) â†’ Yell OK.
- Fix: ResolveEntireStack breaks when a new ActionKind.JustAfter is on top after ResolveTopOfStack (Escape Pod must not eat Yell; sequential 1 Yell/Klingon still works via TryFlush-after-Pass). Order: Destroy â†’ Pod window â†’ unresected die â†’ Death-Yell window; rescue = no death = no Yell.
- Smoke: GROK_TEMP/SMOKE_KLINGON_DEATH_YELL_ESCAPE_POD.md. Tracker stays partial. No push.

## 2026-09-23 â€” Death Yell: ship/facility destroyâ†’crew (WCB gap)

- Root: `3e140a1` enqueued JustAfter only from `DiscardPersonnelBorder`; `DestroyShipOrFacility` crew wipe discarded without Note â†’ WCB/battle/Plasma/etc. Honor-Klingon deaths missed Death Yell.
- Fix: after Destroy Results, note each dying personnel via `NoteHonorKlingonDeathForJustAfter` (batch `_deferJustAfterDeathFlush`); same for `DiscardShipSeizureVictim`. 1 Yell per Honor Klingon; Escape Pod survivors relocâ€™d before wipe â†’ no note. `ActionKind.ShipDestroyed` stays Escape-Pod-only.
- Smoke: `GROK_TEMP/SMOKE_KLINGON_DEATH_YELL_WCB.md` (+ inventur `INVENTUR_DEATH_YELL_WCB_GAP.md`). Tracker partial until Pepsch green. No push.

## 2026-09-23 â€” Klingon Death Yell / shared JustAfter
- Shared `TimingRules.ActionKind.JustAfter` + `JustAfterTrigger` + `IsJustAfter` (SoT/AtStartOfBattle-style gate).
- First consumer: Klingon Death Yell = `JustAfter(KlingonWithHonorDied)`; either player; one Yell per such death; +5 to Yell controller.
- Opens only after actual death/Results (HC/battle batch deferred); Amanda nullify before Results â†’ no trigger.
- No Death-Yell ad-hoc; no Battle Stage-2 misuse.

## 2026-09-23 â€” Interrupt-Play Responses before Results (HC / Stage 2)

- Root: `BeginPlayCardStack` called `ApplyResponseEffect` (HC kills) before `OpenResponseWindow` â€” Amanda saw Results already done.
- Fix (shared, not HC-only): Initiation = Push; Responses open; Results in `ResolveTopOfStack` (`ApplyResponseEffect` + `TryResolveInterruptPlay`). Armbands/Hugh pattern; HC kills only via `Effect.HonorChallenge` after nullify window.
- Smoke: `GROK_TEMP/SMOKE_HONOR_CHALLENGE_AT_START_OF_BATTLE.md` (Amanda-before-kill step). Tracker stays partial. No push.
## 2026-09-23 â€” Honor Challenge (personnel Stage 2)

- Shared gate `TimingRules.IsAtStartOfBattle` / `IsBattleStageResponses` (per battle Stage 2; not SoT/EoT; ETA stays broader).
- Honor Challenge: `CanRespond` only `InitiatePersonnelBattle`; TW apply kills without cancelling battle.
- LegalMoves/EngineAuthority: Respond already via `CanRespond`.
- Smoke: `GROK_TEMP/SMOKE_HONOR_CHALLENGE_AT_START_OF_BATTLE.md`

## 2026-09-22 - Quick Game: Artifacts stay under missions (no spaceline orphans)

- Root: after `be7d6a7`, `AddSeedUnderMission` limit-fail left the card Visible at mission X/SpacelineY â†’ looked like a spaceline node (Thought Maker / Interphase Generator).
- `AutoSeedDilemma` filters with artifact seed limits; sets owner before seed; on fail removes border and leftovers the card.
- `AddSeedUnderMission` returns bool; helpers `CollectSeededUnderMission` / `MissionAllowsArtifactSeed`.
- Smoke: `GROK_TEMP/SMOKE_QUICKGAME_ARTIFACT_SPACELINE.md` (Quick Game x3). No push.

## 2026-09-22 â€” Multi-artifact earn (AT equipment + mis-seed)

- Solver earns all legal artifacts after planet/space solve; chooses order when several.
- Use-as-Equipment (e.g. Interphase Generator) joins solving Away Team/crew host (not orphaned).
- Duplicate titles under one mission â†’ mis-seed out-of-play; seed limit 1 artifact/player/mission.
- Smoke: `GROK_TEMP/SMOKE_MULTI_ARTIFACT_EARN_2026-09-22.md`. Partial until Pepsch green. No push.

## 2026-09-22 â€” Pegasus Search OR + {Interphase Generator}

- MissionRules: OR-first requirement groups (7.2.5.0.3); AlternativeMet accepts {CardName} present.
- Pegasus Search solvable with earned IG aboard attempting crew without skill path.
- Smoke: `GROK_TEMP/SMOKE_PEGASUS_SEARCH_IG_2026-09-22.md`. Tracker partial until Pepsch green. No push.

## 2026-09-22 â€” Atmospheric Ionization scope (planet/vicinity)

- Ionization limit only when origin or dest is planet surface (or landed-ship vicinity).
- Free: Shipâ†”Ship (orbit), Outpostâ†”Ship, Space-Facilityâ†”Ship.
- Distortion / Pattern Enhancers unchanged.
- Smoke: `GROK_TEMP/SMOKE_ATMOSPHERIC_IONIZATION_SCOPE_2026-09-22.md`. Tracker stays partial until Pepsch green. No push.

## 2026-09-22 â€” ETA escapees owner + no battle-stop

- `CompleteBeamTo`: `SetBorderOwner(beamWho)`; track `_etaEscapeeBorders`.
- `StopCrewOnHost`: skip ETA escapees; StackOnHost only remaining aboard (7.4.3 / 10.2.1).
- `BeginBeamMode` error text distinguishes empty/owner/stopped.
- Smoke: `GROK_TEMP/SMOKE_ETA_ESCAPEE_UNSTOPPED_2026-09-22.md`. No push.

## 2026-09-22 â€” ETA T96 selection hold + battle defer

- `CompleteBeamTo`: keep checkbox selection; filter with `_beamModePlayer` / StackOnHost+GetCrewOnShip; Card-match fallback after detail refresh.
- Ship battle `AskReturnFireAndResolve` deferred while `_etaBeamHoldsBattle` / BeamPickTarget; resume after beam complete/cancel.
- Smoke: `GROK_TEMP/SMOKE_ETA_T96_SELECTION_2026-09-22.md`. No stop bypass. No push.

## 2026-09-22 â€” ETA T94 Force-Host / beamPlayer

- `IsBeamableFromHost`: ownership vs ETA controller / `_beamModePlayer` (not only `_activePlayer`) â€” fixes defender ETA in opponent turn.
- `HostHasBeamablePersonnel` / `BeginBeamMode`: `GetCrewOnShip` for facility/ship BoardStore crew.
- `ResolveArmbandsBeamHost`: controller force Ship|Facility; never opponent (T94 Khazara).
- Smoke: `GROK_TEMP/SMOKE_ETA_T94_FORCE_HOST_2026-09-22.md`. No stop bypass. No push.

## 2026-09-22 â€” ETA Armbands Host/Crew (Facility + Load)

- `ResolveArmbandsBeamHost`: Facility/Outpost-Battle ohne Crew-Stack â†’ Spieler-Schiff an derselben Mission mit beambarer Crew.
- `BeginBeamMode`: Crew Ã¼ber `StackOnHost` (Load/SameHostShip), nicht nur Dictionary-Key.
- Kein Stop-Bypass (Spock). Smoke: `GROK_TEMP/SMOKE_ETA_NO_CREW_HOST_2026-09-22.md`. Kein Push.

## 2026-09-21 â€” ETA Armbands Beam-Destination (response â†’ BeginBeamMode)

- Response resolve: Emergency Transporter Armbands enters `TryResolveInterruptPlay` / EmergencyBeam even without TargetCard (was SendCardTo-only â†’ no picker).
- `BeginBeamMode` destination filter uses `beamPlayer` (not `_activePlayer`) so ETA as non-active still lists own same-location targets.
- Search comments Rule 7.1.1 / 7.1.1.0.2 / 7.4.2 / 10.2.1; Glossary ETA Â· equipment Â· battle; Verb BeginBeamMode Â· Beam Â· CanRespond.
- Smoke: `GROK_TEMP/SMOKE_ETA_ARMBANDS_BEAM_DEST_2026-09-21.md`. Partial-escape stop-status parked. Kurlan/FPS untouched. No push.

## 2026-09-21 â€” Kurlan Ã—3 S.A.M. (printed+Adds)Ã—3

- `BattleRules.AttributeAfterSam` / `AttributeBonusOverPrinted`: Rulebook Â§12.11 S.A.M. â€” (printed + adds) Ã— Kurlan, not printedÃ—k + adds.
- Ship battle Open Fire / Return Fire / predict bonuses use same Decide helper as UI `FormatShipEffectiveLine`.
- Verify asserts (8+3)Ã—3=33. Types route bfe0de9 untouched. Smoke: `GROK_TEMP/SMOKE_KURLAN_SAM_2026-09-21.md`. No push.

## 2026-09-21 â€” Classification vs Skill Route (Kurlan Naiskos)

- Shared Decide helper `MissionRules.PersonnelTypePresent` / `RequiresClassificationOnly`: without the word classification â†’ Class box OR effective skill (incl. equipment grants via `EventRules.HasSkill`); with classification â†’ Class box only.
- `ArtifactRules.KurlanFullyStaffed` uses that route (seven personnel types); one personnel may cover two types (Class+Skill).
- `BattleRules.VerifyKurlanMultiplier` covers dual Class+Skill and Medical Kit MEDICAL grant.
- Search comments Rule 10.1.0.1 / 10.1 / 10.3.0.5 / 2.7 / 2.8; Glossary personnel type Â· classification Â· skills Â· use (skills|equipment).
- Smoke: `GROK_TEMP/SMOKE_KURLAN_CLASS_SKILL_2026-09-21.md`. No push. FPS untouched. Continuum/Q parked. Plays on/as F3 unchanged.

## 2026-09-21 â€” Docs: IMPLEMENT statt RULES/CODE_PLACEMENT

- Neue Canon-Trennung: `IMPLEMENT.md` (Ablauf), `ENGINE.md` + `TABLEWINDOW_INVENTORY.md` (Ist-Landkarte), Status nur in Seven/Jadzia/Extract, Log in Changelog, BrÃ¼cke in Handoff.
- `RULES.md` und `CODE_PLACEMENT.md` entfernt. Klasse A/B/C entfÃ¤llt.

## 2026-09-17 â€” Hail (AU) + table UI chrome

## 2026-09-18 â€” TwoDim: full Disabled for Empathy aboard (Spock)

- Upgrade from Empathy skill-strip: Empathy personnel are Disabled while aboard TwoDim ship (live; clears when beamed off).
- Disabled may beam (Glossary); IsBeamableFromHost no longer blocks Disabled.
- Cure ENG+SCI + move-block unchanged. Visuals sync on attach/beam/cure.


## 2026-09-18 â€” Two-Dimensional Creatures: Empathy disabled

- While TwoDim persist on ship: Empathy stripped in ResolvePersonnel (Detail skills, Contents team sum, dilemma Skill(), CanSolve).
- Move-block unchanged. Cure ENGINEER+SCIENCE unchanged. Detail debuff line when printed Empathy present.


## 2026-09-18 â€” FINAL: IG after Resolve (Spock Glossary)

- Pepsch decided strict: reveal â†’ Resolve (targets+conditions) â†’ optional IG Yes/No â†’ else effects.
- Reverted house UX (4cbac3d). May-nullify + IG kept + [IPG] icon path unchanged. No further order flips.


## 2026-09-18 â€” HOLD: keep house UX IG pre-Resolve

- Restored Pepsch house UX (IG Yes/No before Resolve for all [IPG]); undid Glossary revert 17f468e.
- No further IG-order changes until Pepsch picks strict vs house. Spock still flags Glossary conflict.


## 2026-09-18 â€” HOLD: IG back to post-Resolve (Glossary)

- Reverted Pepsch pre-filter IG order (d32da9c). Strict Spock: Resolve (targets+conditions) â†’ optional IG â†’ else effects. Waiting Pepsch: strict vs house UX.


## 2026-09-18 â€” IG nullify BEFORE Resolve for all [IPG]

- Pepsch: Interphase Generator Yes/No runs once at start of encounter handling for every IsIpgDilemma, before DilemmaRules.Resolve (before Rebel destroy-Equipment, filters, kills). Yes = discard dilemma + continue, IG kept. No = normal resolve.


## 2026-09-18 â€” Mission badge strip + Rebel Encounter destroy-Equipment

- Mission under-card status strip (Away/Eq/Art counts) removed; ship/facility badges unchanged; Rogue Borg badge kept.
- Rebel Encounter: when STRENGTH not >44, offer destroy one Equipment present (Equipment type OR Artifact-as-Equipment via IsEquipmentCard). Destroy uses Discard so Overcome applies it. CollectPresentAtMission now includes Artifact-as-Equipment.


## 2026-09-18 â€” Mission detail: revealed-still-under (no last-revealed)

- Removed "Last revealed under Mission" (could show discarded cards).
- Detail shows all cards revealed under that mission that are still on the under-mission seed pile (visible to all). Face-down count excludes those.


## 2026-09-18 â€” Interphase Generator: may-nullify after just-encountered (Spock)

- Removed auto-nullify at reveal.
- After `DilemmaRules.Resolve` (targets + conditions), if [IPG] and IG present with attempting AT/crew: Yes/No to nullify before results. IG kept. Scope = planet surface AT or attempting-ship crew only.


## 2026-09-18 â€” Interphase Generator nullifies [IPG] dilemmas

- `CardIcons.HasIpg` / `IsIpgDilemma` from printed `[IPG]` tokens (no name list).
- On mission attempt: if Interphase Generator is present with the attempting team, revealed [IPG] dilemmas are discarded and the attempt continues (before Resolve).


## 2026-09-18 â€” Hail no-battle: Detail debuff (not under-card)

- Removed under-card "Hail: no battle" flags.
- Show as red **Debuff** line in card Detail status block (same place as Metaphasic / other host effects): "Hail: cannot battle X this turn".
- Status line kept; cleared at EOT with the restriction. Table-drop path unchanged.


## 2026-09-18 â€” Hail OR: table drop + no-battle flags

- **Drop:** Hail `GetPlayTarget` = None before PlayOnRules (printed "Plays on any ship" was forcing ship host). Play onto empty table/play area like Jaglom, then mark two ships.
- **Feedback:** after pair marked â€” status line + light "no battle" flags on both ships (partner name); cleared at EOT with the restriction.


## 2026-09-17 â€” Hail fly-by Pass: relocate after deferred move

- **Bug:** Pass / no Hail on `ShipFlyBy` spent RANGE and logged arrival, but left the ship token at the start (desync).
- **Fix:** `CompletePendingHailFly` now `RelocateShipAlongSpaceline` + sync after rules apply. Hail-played stop path unchanged.


## 2026-09-17 â€” Hail two-ship: spaceline click-mark (no Detail picker)

- **Hail** OR-mode UX (Pepsch): play Hail to table (no ship drop target), then click two ships on the spaceline â€” each lights up; second click applies no-battle-this-turn and discards Hail. Fly-by path unchanged (`ShipFlyBy`).
- Replaces DetailWindow list picker for identical ships.


- **Hail** (Alternate Universe interrupt, Spock Soll): fly-by response window (`ShipFlyBy`) when a ship span-passes a location with an opposing ship â€” play Hail to stop it there (no further move this turn, not game-stopped); discard Hail (no attach). OR one play selecting two ships â€” they cannot battle each other this turn (EOT clear). Subspace Interference still nullifies Hail on the stack.
- **UI:** removed inner `BoardInnerGlow` frame (outer `BoardFrameBorder` kept); ThinkTray chrome tightened (padding/margin/rail).

ï»¿# Changelog

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
- **Problem**: Bei SpielstÃ¤nden, in denen Schiffe unbeschÃ¤digt waren, konnte alter Rumpfschaden (`HullPercent`) aus `BoardStore.Current` oder frÃ¼heren SpielzustÃ¤nden fortbestehen und nach dem Laden fÃ¤lschlicherweise Badges (z. B. 50% DMG) sowie Schadenswerte anzeigen.
- **LÃ¶sung**:
  - `BoardStore.Current.Clear()` wird am Anfang von `ApplyGameSave` ausgefÃ¼hrt, um alle veralteten Instanzen vor dem Neuaufbau zu verwerfen.
  - Explizites ZurÃ¼cksetzen fÃ¼r unbeschÃ¤digte Schiffe (`snap.Hull <= 0`): `SetHullDamagePercent(border, 0)` und `UpdateDamageBadge(border, 0)`.
  - `SyncBoardFromTable(logDual: false)` wird am Ende von `ApplyGameSave` aufgerufen, um den `BoardStore` exakt mit dem rekonstruierten Spielstand zu synchronisieren.
  - In `SaveGame`: `Hull` und `RangeLeft` erfassen Ã¼ber `GetHullDamage(b)` und `GetRemainingRange(b, card)` direkt die verbindlichen Instanz-Werte der Schiffe.

---

## 2026-09-13 (Fix & Rule Implementation - Ktarian Game Dilemma Disabling & Cure)

**Engine & Rules (DilemmaRules, TableWindow, BoardStore, CardInstance & Models)**:
- **Glossary & Kartentext-KonformitÃ¤t (Ktarian Game)**:
  - Kartentext: *"Place on ship. Now and start of each turn, one personnel aboard (random selection) is disabled. Cure with CUNNING>30 OR any android."*
  - Bisheriger Bug: Die Start-of-Turn-Logik verwendete fÃ¤lschlicherweise `MarkStopped`, welches durch den regulÃ¤ren Rundenwechsel (`UnstopAllCards`) direkt zu Beginn der Runde wieder aufgehoben wurde. Zudem fehlte das initiale Deaktivieren einer Person beim Encounter ("Now") sowie ein echter "Disabled"-Zustand.
  - Implementierung von echtem permanentem `Disabled`-Status:
    - `Card.cs`: `Disabled`-Flag und Einbeziehung in `IsLeaveBlocked`.
    - `CardInstance.cs`: `PersonnelInstance.Disabled` und `override bool IsLeaveBlocked => Quarantined || InStasis || Disabled;`.
    - `MovementRules.cs`: Deaktiviertes oder in Stasis befindliches Personal zÃ¤hlt nicht mehr zu Staffing-Requirements (`IsShipStaffed`).
    - `TableWindow.xaml.cs`:
      - Neues `ApplyDisabledVisual` mit amber-orange Glow/Border und reduzierter OpazitÃ¤t.
      - `ApplyKtarianDisable`: WÃ¤hlt beim Encounter ("Now") und zu jedem Rundenbeginn ("Start of Turn") eine zufÃ¤llige, noch nicht deaktivierte Person an Bord des Wirtsschiffs aus, markiert sie als `Disabled` und trÃ¤gt sie in `attached.Held` ein.
      - `ProcessStartOfTurnDilemmas`: PrÃ¼ft zuerst die Heilung mit un-deaktiviertem Personal (CUNNING>30 oder Android). Falls nicht geheilt, wird eine weitere Person deaktiviert.
      - `ClearStasisForDilemma`: Hebt beim Heilen von `Ktarian Game` den `Disabled`-Status aller betroffenen Personen auf, stellt die Visuals wieder her und leert `Held`.
      - Mission-Versuche und Beamen: Deaktiviertes Personal ist vom Beamen ausgeschlossen und zÃ¤hlt nicht bei Missionsversuchen/Skills.
      - UI Details & Gruppen: Deaktivierte Personen werden in der Detailansicht mit amber Status und unter der "Disabled"-Negativgruppe aufgefÃ¼hrt.
      - Save/Load (`GameSave.cs`): `AttachedDilemmaSnap.HeldIds` speichert die betroffenen Karten-IDs, sodass der `Disabled`-Zustand auch nach Speichern und Laden exakt erhalten bleibt.
  - Unit-Tests:
    - `DilemmaRules.VerifyKtarianGame`: Erweiterte Tests bezÃ¼glich CUNNING>30, Android, Nicht-ZÃ¤hlen von Held/Disabled-Personal bei Cure-Checks und `IsLeaveBlocked`-Verhalten.
    - `DilemmaCureRules.VerifyDilemmaCureRules`: ZusÃ¤tzliche Tests fÃ¼r Ktarian Game Heilung mit CUNNING>30, Android und Fehlschlag bei CUNNING<=30.

---

## 2026-09-13 (Fix & Rule Implementation - Portal Guard & Quarantine Interaction)

**Engine & Rules (DilemmaRules, TableWindow, BoardStore & Models)**:
- **Glossary & Kartentext-KonformitÃ¤t (Portal Guard & Hyper-Aging QuarantÃ¤ne)**:
  - Kartentext: *"Unless one Away Team member has CUNNING>7 or Honor, immediately beam entire Away Team off planet surface OR kills entire Away Team."*
  - DRG: Wenn die Bedingung nicht erfÃ¼llt ist, muss das gesamte Away Team sofort vom Planeten gebeamt werden. Ist das Beamen erfolgreich, wird das Away Team gestoppt und das Dilemma verbleibt unter der Mission (`WallFailed`). Kann jedoch auch nur ein einziges Mitglied des Away Teams nicht beamen (z. B. wegen QuarantÃ¤ne durch Hyper-Aging oder Stasis) oder existiert kein Schiff oder Facility vor Ort zum Hinbeamen, wird das **gesamte Away Team getÃ¶tet**!
- **Zentrale Kapselung von QuarantÃ¤ne & Stasis**:
  - `Card.cs`: Laufzeit-Properties `Quarantined`, `InStasis` und `IsLeaveBlocked`.
  - `CardInstance.cs`: `PersonnelInstance` besitzt `Quarantined`, `InStasis` und `override bool IsLeaveBlocked => Quarantined || InStasis;`.
  - `Force.cs` (Away Team / Crew): `IsQuarantined`, `IsLeaveBlocked` und `CanBeamAway`.
  - Synchronisation im `BoardStore` bei `ApplyUiStatusToStore` sowie beim Beitritt oder Anheften von QuarantÃ¤ne-Dilemmas.
- **Dilemma-Regeln & Beam-Verdrahtung**:
  - `DilemmaRules.Ctx`: Ãœbermittlung von `CanBeamOffPlanet` (prÃ¼ft, ob das Team auf einem Planeten steht, kein Mitglied blockiert ist und ein eigenes Schiff bzw. eine Facility am Ort existiert).
  - `DilemmaRules.DecidePortalGuard`: BerÃ¼cksichtigt `canBeamOffPlanet`. FÃ¼hrt bei unerfÃ¼lltem Filter und blockiertem Beamen zu `KillTeam = true` (alle Team-Mitglieder in `r.Kill`) und `BeamBackTeam = false`.
  - `TableWindow.BeamBackAwayTeamToShipOrOutpost`: Verhindert das Beamen, sobald auch nur ein Team-Mitglied `IsCardLeaveBlocked` ist, und liefert einen booleschen Status zurÃ¼ck. Scheitert das Beamen bei Portal Guard, greift der Fallback und das gesamte Team wird verworfen (unter BerÃ¼cksichtigung von Genetronic Replicator).
  - `VerifyPortalGuard`: AusfÃ¼hrlicher Unit-Test mit Pass-, Fail-mit-Beam- und Fail-ohne-Beam-(QuarantÃ¤ne/No-Dest)-Szenarien.

---

## 2026-09-13 (Fix - Genetronic Replicator Event & Target Exclusion Rules)

**Engine & Rules (EventRules & TableWindow)**:
- **Glossary & Kartentext-KonformitÃ¤t**: "When a personnel is targeted to die, you may stop 2 MEDICAL present (who are not also targeted to die) to return that personnel to hand instead."
  - Das zu rettende Personal (Victim) und sÃ¤mtliche weitere gleichzeitig zum Tod ausgewÃ¤hlte Personen (`alsoTargetedToDie`) dÃ¼rfen nicht fÃ¼r die 2 geforderten MEDICAL-Punkte gezÃ¤hlt oder gestoppt werden (z. B. wenn Beverly Crusher mit 2 MEDICAL getÃ¶tet wird, kann sie sich nicht selbst retten, sofern nicht mindestens 2 weitere ungestoppte MEDICAL-Fertigkeiten anwesend sind).
  - Bereits gestoppte (`IsBorderStopped`) oder in Stasis befindliche (`IsCardInStasis`) Personen kÃ¶nnen nicht zum Zahlen der Rettungskosten gestoppt werden.
  - Nur eigenes, ungestopptes Personal am selben Host mit MEDICAL-FÃ¤higkeiten ist qualifiziert.
- **Interaktive Auswahl**:
  - Sind mehr als 2 MEDICAL-FÃ¤higkeiten anwesend, kann der Spieler Ã¼ber `PickBorderFromList` interaktiv wÃ¤hlen, welche medizinischen FachkrÃ¤fte gestoppt werden sollen.
  - Automatisches Stoppen, wenn die verfÃ¼gbaren Kandidaten genau den Anforderungen entsprechen.
- **Regel-Zentralisierung in `EventRules.cs`**:
  - `GetPersonnelMedicalSkill`: Ermittelt effektive MEDICAL-Stufe (inklusive AusrÃ¼stung wie Medical Kit).
  - `IsEligibleForGenetronicStop`: Validiert Berechtigung einzelner Karten unter Ausschluss von Opfern und gestopptem Personal.
  - `GetAvailableGenetronicMedical` & `CanGenetronicSave`: Pure Decide-Logik.
  - `VerifyGenetronicReplicator`: VollstÃ¤ndiger Regel-Unit-Test (Selbstrettungs-Ausschluss von Beverly Crusher, Ausschluss von gleichzeitig GetÃ¶teten, gestopptes Personal ignoriert, Fremdrettung mit Crusher/Toby Russell).
- **TableWindow Verdrahtung**:
  - `DiscardPersonnelBorder` akzeptiert jetzt `alsoTargetedToDie`.
  - Weitergabe von `alsoTargetedToDie` bei Dilemma-Kills (`ApplyDilemmaResult`, Crystalline Entity), Personnel Battles (`CompletePersonnelAttack`, Rogue Borg Battles), Artifact Kills (`Stone of Gol`) und EOT Countdown-Kills (`Hyper-Aging Quarantine`).
  - Korrekte Board-Bereinigung (`_stackOnHost`, `_stoppedBorders`, `BoardStore`), Ablage auf die Hand und Aktualisierung der Zonen-/Host-Badges.

---

## 2026-09-13 (UI & Localization - Remove Duplicate 'Artifact verdient' & Full English Translation)

**UI & Cleanup**:
- **Artifact Acquire Reveal Cleanup**: Entfernen des redundanten "Artifact verdient" `ShowCardReveal`-Overlays beim LÃ¶sen einer Mission (`ApplyArtifactAcquire`). Es verbleibt ausschlieÃŸlich die konsistente englische Einzelkarten-Meldung "Artifact acquired" in `ResolveMissionSolve`.
- **VollstÃ¤ndige Lokalisierung auf Englisch**:
  - `TableWindow.xaml` & `TableWindow.xaml.cs`: SÃ¤mtliche verbliebenen deutschen Texte, Tooltips, Statusmeldungen, Aktionshinweise, Fehlermeldungen und Dialoge auf Englisch Ã¼bersetzt (z. B. Response-Badges, Think-Tray-Titel und Hinweise, Seed-Phasenmeldungen, Action-Stack-Status, Scan-Reveals).
  - `DeckBuilderWindow.xaml` & `DeckBuilderWindow.xaml.cs`: Lokalisierung aller Filter, Tab-Header ("Side legacy"), Tooltips und Meldungen auf Englisch.
  - Game Rules (`DilemmaRules`, `InterruptRules`, `MovementRules`, `PlayRules`, `ReportingRules`, `SeedRules`, `TimingRules`, `ModifierRules`, `BattleRules`, `MissionRules`, `TreatyRules`): Ãœbersetzung aller internen und spielerseitigen Fehlermeldungen, Check-Ergebnisse, Action-Stack-Zusammenfassungen und Prompt-Texte (z. B. "Which equipment?").
  - Services & Models (`GameSession`, `DeckService`, `CardDatabase`, `ExpansionCatalog`): Ãœbersetzung der Zugprotokolle (P1/P2 statt S1/S2), Phasenlabels, Datei-Ausnahmemeldungen und Katalog-Fallbacks.

---

## 2026-09-13 (Fix & Rule Refactor - Curable Dilemmas & Team Stop Prevention 7.2.2.3 / 7.2.6)

**Engine & Rules** - Trennung von Bedingung (Condition) und Heilung (Cure) & Stopp-Verhalten:
- **Allgemeine Regel (Compendium 7.2.2.2, 7.2.2.3, 7.2.6 & Glossary)**:
  - Ein Away Team / eine Crew wird durch ein Dilemma nur dann gestoppt, wenn eine Zugangsbedingung ("unless", "to get past", "cannot get past") fehlschlÃ¤gt, der Kartentext dies explizit befiehlt ("Away Team is stopped"), oder niemand mehr Ã¼brig ist.
  - Eine Heilungsanforderung ("Cure with...") ist ausdrÃ¼cklich **keine** Zugangsbedingung. Weder das Heilen noch das Nicht-Heilen einer heilbaren Dilemma-Wirkung ohne Vorbedingung fÃ¼hrt zum Abbruch der Mission oder zum Stoppen des restlichen Teams ("Failing to immediately meet a cure requirement does not cause mission failure").
- **Alien Abduction (PR 10 U)**:
  - Bei Begegnung: Ziel mit hÃ¶chstem CUNNING wird in Stasis gesetzt (kann eigene FÃ¤higkeiten nicht zur Heilung beitragen).
  - Wenn verbleibendes Away Team 3x Leadership hat: Sofort geheilt (`Fate.Overcome, StopTeam = false`), Dilemma abgeworfen, Versuch lÃ¤uft mit vollem Team weiter.
  - Wenn verbleibendes Away Team keine 3x Leadership hat: Dilemma wird an die Mission angehÃ¤ngt (`Fate.AttachAndContinue, StopTeam = false`), Opfer bleibt in Stasis. Das restliche ungestoppte Team setzt den Missionsversuch nahtlos fort!
  - Bei Befreiung (Mission gelÃ¶st oder spÃ¤tere Heilung): `ClearStasisForDilemma` ruft `UnstopBorder` auf, sodass die Person vollstÃ¤ndig ungestoppt wieder zum Team stÃ¶ÃŸt.
- **Konsistente Anwendung auf weitere Curable Dilemmas**:
  - `Two-Dimensional Creatures`: Verwendet nun `AttachContinue` (`StopTeam = false`). Schiff kann sich nicht bewegen, aber Crew ist nicht gestoppt und Missionsversuch lÃ¤uft weiter.
  - `Tsiolkovsky Infection`: Verwendet nun `AttachContinue` (`StopTeam = false`). Personal verliert erste Fertigkeit, ist aber nicht gestoppt und Versuch lÃ¤uft weiter.
  - `Frame of Mind`: Verwendet nun `AttachContinue` (`StopTeam = false`) mit Sofort-Heilung bei 3 Empathy im verbleibenden Team.
  - `Quantum Singularity Lifeforms` & `Rascals`: Auf `AttachContinue` umgestellt.
  - `TryCureAttachedDilemmas` & `ProcessEndOfTurnDilemmas`: SchlieÃŸen bei `a.Held.Count > 0` alle in Stasis gehaltenen Karten fÃ¼r Heilungs-Checks aus (Opfer kÃ¶nnen sich nicht selbst heilen).
- **Automatisierte Regeltests**:
  - Neue Verifikationsmethoden: `VerifyAlienAbduction()`, `VerifyTwoDimensionalCreatures()`, `VerifyTsiolkovskyInfection()` in `DilemmaRules.cs`.
  - Erweiterung von `VerifyDilemmaCureRules()` in `DilemmaCureRules.cs` um Blockade von Selbstheilung aus der Stasis und Heilungstests fÃ¼r TwoDim und Tsiolkovsky.

---

## 2026-09-13 (Retest Green - Archer, Alien Abduction, Phased Matter)

**Retest (Pepsch green):**
- **Archer (PR 14 C)**: Auswertung der hÃ¶chsten Gesamtattribute, Tie-Break-Wahl durch den Gegner und Stop-Verhalten bei NichterfÃ¼llung verifiziert und bestÃ¤tigt.
- **Alien Abduction (PR 10 U)**: Stasis-Handling und zentrales Cure-System (7.2.2.3) via 3 Leadership prÃ¤sent oder Mission Completed verifiziert und bestÃ¤tigt.
- **Phased Matter (PR 42 C)**: Aufteilung des Away Teams, Stasis/Phasing der grÃ¶ÃŸeren Gruppe, FortfÃ¼hrung der kleineren Gruppe und Entphasen/Heilen durch unphased ENGINEER + SCIENCE am Ort bestÃ¤tigt.

---

## 2026-09-12 (Feat - Centralized Dilemma Cure System according to Rulebook 7.2.2.3)

**Engine** - Dilemma Cure System (Compendium 7.2.2.3):
- **Decide in Rules (`DilemmaCureRules`)**: Reine Regel-Engine fÃ¼r Dilemma-Heilung (`DecideCure` / `CanCure` / `VerifyDilemmaCureRules`). Trennung von Bedingung und Heilung: Zuerst werden die Bedingungen des Dilemmas ausgewertet/angehÃ¤ngt, danach wird der Cure-Check durchgefÃ¼hrt (anwendbar auf Alien Abduction, Menthar Booby Trap, Hyper-Aging, REM Fatigue, Nitrium Metal Parasites, Tsiolkovsky Infection, Two-Dimensional Creatures, Ktarian Game, Birth of "Junior", Frame of Mind).
- **Zentraler Apply in `TableWindow`**: `TryCureAbductionsPresent` und fragmentierte Cure-PrÃ¼fungen wurden durch die zentrale Routine `TryCureAttachedDilemmas` ersetzt. Aufgerufen direkt nach Attachment in `ApplyDilemmaResult`, beim LÃ¶sen einer Mission in `ApplyMissionSolved` (fÃ¼r Heilen durch Mission Completed), sowie bei Crew-Ã„nderungen (`AddCardToHostStack`, `BeamCardsToHostStack`) und Unstop zu Zugbeginn.
- **Dilemma-Resolution Angleichung**: `DilemmaRules` fÃ¼r Menthar, Tsiolkovsky, Two-Dimensional Creatures und REM Fatigue nutzen `DilemmaCureRules.CanCure` konsistent.

---

## 2026-09-12 (Feat - Silent Response Window & Think Tray UX)

**UX / Hotseat Rules** - Response Window Umbau:
- **Weg vom modalen Popup (`CardRevealOverlay`)**: Keine blockierenden modalen Vollbild-Dialoge mehr bei normalen Card Plays / Reaktionen.
- **Stilles Window mit Banner-Hinweis**:
  - Kurzes Standardzeitfenster (Default: 3s; konfigurierbar im Options-MenÃ¼ auf 2s / 3s / 5s; Presets fÃ¼r Hotseat Standard 3s/10s und Test schnell 2s/10s).
  - Wenn keine legale Response existiert: Sofortiges SchlieÃŸen / Auto-Pass, der aktive Spieler kann ohne VerzÃ¶gerung weiterspielen.
  - Wenn legale Response existiert: Dezenter violettes Badge am Phase-Banner (`ActivePlayerBanner`): `âš¡ Response mÃ¶glich (P1/P2) Â· 3s` inkl. Hotkey-Hinweis `Â· [R] Details  [Space] Pass`.
- **Think-Modus (Opt-in via [R] oder Klick auf Banner/Badge)**:
  - VerlÃ¤ngert das Window auf 10s Countdown.
  - Zeigt horizontal scrollbares `ThinkTray` Ã¼ber der Hand des Responders (P1 unten, P2 oben).
  - Volle HandkartengrÃ¶ÃŸe mit Herkunfts-Badge (`HAND`, `TABLE`, etc.) und Kartendetails.
  - Klick auf Karte fÃ¼hrt Response sofort aus; [Space] oder Timeout fÃ¼hrt Pass aus.
- **Priority & Mandatory**:
  - Optionale Responses: Zuerst nicht-aktiver Spieler, danach aktiver Spieler. GewÃ¤hlte Response erzeugt neue Aktion auf dem Stack und neues Window fÃ¼r den Gegner.
  - Mandatory / required Responses: Kein Pass per Timeout, Space-Pass deaktiviert, Fenster bleibt bis Karte gewÃ¤hlt wurde.

---

## 2026-09-12 (Fix - Artifact Beaming without Treaty & Retest Green: Hyper-Aging, Firestorm, Detail Groups)

**Engine** - Artifact Beaming / Affiliation-Free: Artifacts (inkl. Varon-T Disruptor, Interphase Generator, Data's Head etc.) haben keine Affiliation-Sperre und benÃ¶tigen keinen Treaty, um auf Schiffe/Facilities gebeamt oder dort platziert zu werden (analog zu Equipment). Decide: `TreatyRules.CanOccupyHost` / `CardsCompatibleUnderTreaties` / `ForceCompatible` erlauben Artifacts affiliationsfrei; `ReportingRules.AreCompatible` / `CheckReportRules` erweitert; `ModifierRules.IsEquipmentCard` um Data's Head ergÃ¤nzt. Apply: Detailansicht `FillDetailStackSection` gruppiert Artifacts unter Equipment/Artifacts statt Personnel; Fehlermeldung bei Beam aktualisiert ("Equipment and Artifacts are unrestricted").

**Retest (Pepsch green):**
- **Hyper-Aging**: QuarantÃ¤ne auf Planet, Beam-Block fÃ¼r QuarantÃ¤nisierte bestÃ¤tigt.
- **Firestorm**: INT<5 Kills und Versuch-Fortsetzung bestÃ¤tigt.
- **Dilemma-Continue Overlay**: Platzhalter-Header `EFFECT - attempt continues` (statt irrefÃ¼hrendem `RELOCATED`) fÃ¼r Firestorm und nicht-relocate Dilemmas bestÃ¤tigt (Love Interest bleibt `RELOCATED`).
- **Detailansicht Debuff-Gruppierung**: Gruppierung von Stopped / Quarantined / Stasis mit Sammel-Header `DetailStatusRules.FormatEffectGroupHeader` ohne redundante Per-Card-Labels bestÃ¤tigt.
- **Varon-T Disruptor**: Looten auf Planet und STRENGTH Ã—2 fÃ¼r eigenes Personal bestÃ¤tigt.

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

**Engine** - Iconian Computer Weapon (PR 29 C): Space â€” Unless SCIENCE present: Ship+Crew stopped (`EffectAndEnd`+`StopTeam`); reveal hand, discard ALL non-personnel (personnel stay); draw equal number from draw deck (`DrawForDiscarded` / Apply `DiscardNonPersonnelFromHand`+`DrawOneToHand`); else Overcome Continue. Always discard dilemma. No bonus points. Spock #13 Soll / DRG Iconian Computer Weapon (standalone). Decide: `DilemmaRules.IconianComputerWeapon` + `VerifyIconianComputerWeapon`.

---

## 2026-09-06 (Feat - Gravitic Mine Premiere)

**Engine** - Gravitic Mine (PR 26 U): Space â€” Unless SCIENCE AND Navigation present: DamageShip (ApplyHullDamage +50 / Rotation badge) + Ship+Crew stopped (`EffectAndEnd`+`StopTeam`); else Overcome Continue. Always discard. No bonus points. Spock #12 Soll / DRG Gravitic Mine. Decide: `DilemmaRules.GraviticMine` + `VerifyGraviticMine`.

---
## 2026-09-06 (Feat - Firestorm Premiere)

**Engine** - Firestorm (PR 25 U): Planet â€” no Condition-Wall. Personnel with INT<5 after Enhancements (`Eff`) die; Rest Continue; dilemma discard (`EffectAndContinue`). Boundary INT==5 survives. Thermal Deflectors in play -> nullify/discard + Continue (`Overcome`). Spock #11 Soll / DRG Firestorm (TD/ETA != Conditions). Decide: `DilemmaRules.Firestorm` + `VerifyFirestorm`. PARK: ETA-Escape Response timing (UI thin).

---
## 2026-09-06 (Feat - El-Adrel Creature Premiere)

**Engine** - El-Adrel Creature (PR 23 U): Planet â€” Targets two strongest AT (Tie = Dilemma-Owner / `PickOpp`). Pass combined STR >16 â†’ Overcome Continue + discard (no points). Fail â†’ 1 of the two random killed; rest of AT stopped; discard (`EffectAndEnd`+`StopTeam`). Boundary STR==16 fails. Spock #10 Soll / DRG El-Adrel Creature. Decide: `DilemmaRules.ElAdrel` + `VerifyElAdrelCreature`.

---
## 2026-09-06 (Feat - Cytherians Premiere)

**Engine** - Cytherians (PR 22 R): Space â€” Place on ship; Attempt ends; Crew **NOT** stopped (`AttachAndEnd` + `StopTeam=false`). Far end fixed once (TW `Dest` / `RequiredMoveRules.FarEndIndex` 12.6). Arrival â†’ discard +15; ship destroy â†’ discard (no points). No instant relocate. Spock #9 Soll / Glossary Cytherians + actions-required. Decide: `DilemmaRules.Cytherians` + `VerifyCytherians`. PARK: full LegalMoves-only-toward-far-end beyond existing `ShipHasRequiredMove` gates; Borg play-out no-points / Mission Debriefing if unclear.

---
## 2026-09-06 (Feat - Crystalline Entity Premiere)

**Engine** - Crystalline Entity (PR 21 R): Dual [S/P]. Planet - SCIENCE+MEDICAL -> Overcome +5 Continue; else entire AT killed. Space - Music OR SHIELDS>6 -> Overcome +5 Continue; else ALL life aboard dies (Stopped/Disabled/Intruder; NOT Stasis) via `KillAllLifeAboardExceptStasis` (Apply beyond encounter crew); Ship stopped; does **not** destroy ship. Always discard. Spock #8 Soll/DRG/Glossary. PARK: Lore-Double. Decide: `DilemmaRules.Crystalline` + `VerifyCrystallineEntity`.

---
## 2026-09-06 (Feat - Cosmic String Fragment Premiere)

**Engine** - Cosmic String Fragment (PR 20 U): Space â€” Unless Astrophysics OR ENGINEER OR Navigation present: destroy ship (everything aboard via Apply); else Overcome +5 Bonus-Area + Continue. Always discard dilemma. Fail -> EffectAndEnd+StopTeam+DestroyShip. Spock #7 Soll/DRG. Decide: `DilemmaRules.CosmicStringFragment` + `VerifyCosmicStringFragment`.

---
## 2026-09-06 (Feat - Chalnoth Premiere)

**Engine** - Chalnoth (PR 19 U): Planet â€” Unless 3 SECURITY OR STRENGTH>40 present: opponent kills one Away Team member; else Overcome +5. Always discard dilemma. Fail -> EffectAndEnd+StopTeam. Spock #6 Soll/DRG (Points 5 Bonus-Area). Decide: `DilemmaRules.Chalnoth` + `VerifyChalnoth`.

---
## 2026-09-06 (Feat - Birth of "Junior" Premiere)

**Engine** - Birth of "Junior" (PR 17 U): Space â€” place on ship. Encounter: 3 ENGINEER â†’ nullify Overcome (discard+Continue); else AttachAndContinue (crew not stopped; countdown 0, RANGE âˆ’1 only on your EOTs). Destroy when RANGE after countdown â‰¤0 via `EndOfTurnRestRules.JuniorDestroysShip`. Later cure 3 ENGINEER â†’ discard (RANGE restores via host recalc). Spock/DRG/Glossary. Decide: `DilemmaRules.BirthOfJunior` + `VerifyBirthOfJunior`. Pup-disable â‰  0 RANGE: PARK (no guess).

---
## 2026-09-06 (Feat - Armus: Skin Of Evil Premiere)

**Engine** - Armus: Skin Of Evil (PR 15 R): kills one Away Team member (random selection); dilemma discarded; survivors continue (EffectAndContinue, no StopTeam / not under mission). Spock/DRG Soll. Decide: `DilemmaRules.ArmusSkinOfEvil` + `VerifyArmusSkinOfEvil`.

---

## 2026-09-06 (Fix - Anaphasic Organism discard not kill)

**Engine** - Anaphasic Organism fail: selected female **resigns = discard**, not killed (DRG: discarded female is not killed). `Result.Discard` + TW Apply/Format; Genetronic does not save. Pass MED+SEC / no-female / StopTeam / dilemma discard / opp-tie unchanged. Decide: `DilemmaRules.Anaphasic` + `VerifyAnaphasicOrganism`.

---
## 2026-09-06 (Feat - Ancient Computer Premiere)

**Engine** - Ancient Computer (PR 13 R): Wall â€” pass 2 Computer Skill OR 3 SCIENCE OR 3 ENGINEER -> Overcome (discard+continue). Fail -> WallFailed + StopTeam (dilemma stays under mission). Matches printed Premiere text. Decide: `DilemmaRules.AncientComputer` + `VerifyAncientComputer`.

---
## 2026-09-06 (Feat - Anaphasic Organism Premiere)

**Engine** - Anaphasic Organism (PR 12 C): Pass MEDICAL+SECURITY -> Overcome (discard+continue). No female present (requires Female) -> Overcome no-effect. Fail -> discard female with highest total attributes (opp choose on tie, Archer parity), EffectAndEnd+StopTeam; dilemma always discarded. Decide: `DilemmaRules.Anaphasic` + `VerifyAnaphasicOrganism`. Hotseat Alien Parasites untouched.

---
## 2026-09-06 (Feat - Alien Parasites #1a Pass/Fail + Beam-back)

**Engine** - Alien Parasites #1a (Spock Soll): Pass INTEGRITY>32 â†’ Overcome (discard + continue). Fail â†’ WallFailed (dilemma stays under mission), StopTeam; planet Beam-back AT to ship/outpost then stop; Space stops crew+ship. No opponent control / hotseat / next-turn timer (PARK). Decide: `DilemmaRules.DecideAlienParasites` + `VerifyAlienParasites1a`; Apply: TW `BeamBackAwayTeamToShipOrOutpost`.

---
## 2026-09-06 (Fix - Cloak opacity more transparent ~0.45)

**UX** - Cloak Opacity 0.45 (was 0.7); Decloak 1.0; Stopped alone 0.55; stopped+cloaked uses 0.45 (cloak preferred).

---
## 2026-09-06 (Fix - Detail pane dupes + Cloak 0.7 + Stopped Negative + Archer tie)

**UX** - Card detail: colored StatusBlock keeps Buff/Timer/Debuff once; ship path no longer dumps Events/Dilemmas into DetailIcons under staffing; Contents skips repeating RANGE/WEAPONS/SHIELDS + Modifiers. Stopped shows once as red Status line and explicit "Stopped" text under Negative. Cloak: Opacity 0.7 only (nebula overlay + black glow removed); decloak 1.0 / stopped stays 0.55.

**Engine** - After walk/drag onto host: SyncBoardFromTable + TryCureAbductionsPresent (beam parity) so mission/dilemma skills see present crew immediately. Archer: HighestAttr ties â†’ opponent choice (enhancements via Eff unchanged); fail still StopTeam + EffectAndEnd.

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
## 2026-09-06 (Fix - G2 Treatyâ‰ Matching Affiliation Fly)

**Engine** - `MovementRules.HasMatchingAffiliation`: Matching Affiliation fÃ¼r Staffing = echte gemeinsame Affiliation mit dem Schiff. Treaty/NA-KompatibilitÃ¤t zÃ¤hlt **nicht** als Match (Spock G2). Treaty-/NA-Personal darf weiterhin nur Staffing-Icons (Cmd/Stf) fÃ¼llen, sobald Matching-Affiliation an Bord ist. Fly-Pfad (`IsShipStaffed` / `CanMoveShip`).

---
## 2026-09-06 (Fix - Neural Servo Seiten-Sync)

**UX** - Neural Servo Device: nach gÃ¼ltigem Play/Resolve (und EOT-Restore) Schiff sofort auf Controller-Seite neu legen (P1 unter / P2 Ã¼ber Mission) via `RelayoutDockablesUnderMission` â€” nicht erst nach Fly/Move. Helper `SyncDockableSideAfterOwnerChange` (wie Lore Returns).

---
## 2026-09-06 (Fix - Love Interest + Stasis/Abduction + Cloak nebula)

**Engine** - Female's/Male's Love Interest: Fate.EffectAndContinue - relocate matching gender to furthest other planet; victim leaves AT (not stopped); rest continue; dilemma discarded. TW failed excludes EffectAndContinue.

**Engine** - Stasis: cannot beam/drag-leave while IsCardInStasis. Alien Abduction cure = OR (3 Leadership present OR mission completed) - release Held + clear stasis; discard on mission solve.

**UX** - Cloaked ships: black fog/nebula overlay on ship art (plus existing black border/glow).

---
## 2026-09-06 (Fix - Status-UX retest + Nitrium/Hyper-Aging continue)

**UX** - Damaged ships: red DMG badge only (no 180 flip). Outpost repair timer: `1 left` = clears end of **this** turn. Detail crew rows: Positive (green) / Negative (red) / Personnel / Equipment; last-dilemma mini removed (mission action `Show last revealed card under mission`). Stasis/Negativ glow **red**; Cloaked **black**; Buff green; Timer amber.

**Engine** - `Fate.AttachAndContinue`: Nitrium (countdown **2**, cure 2 SCI|2 ENG) + Hyper-Aging (countdown **3**, cure SCI+MEDÃ—2) place without stop/fail; attempt continues. Encounter cure â†’ Overcome (Hyper-Aging +5). RemFatigue unchanged. Menthar/Abduction etc. stay AttachAndEnd.

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
## 2026-09-05 (Fix Ã¢â‚¬â€ Hugh Rogue ship host-match)

**Engine** Ã¢â‚¬â€ Hugh HostMatches ships with Rogue Borg; Borg Ship Dilemma pool only when token/face visible (not mere Host attach). [C]

---

## 2026-09-05 (Fix Ã¢â‚¬â€ WNOHGB PathBlocked wrap fallback)

**Engine** Ã¢â‚¬â€ PathBlocked: prefer clear shorter wrap; if wrap arc Q-Net-blocked fall back to linear. Hazard walk uses WnohgbRules.UseWrapPath costs. [C]

---

## 2026-09-05 (Fix Ã¢â‚¬â€ Kevin hand restore + Hugh Spock)

**Engine** Ã¢â‚¬â€ Kevin/Devil: miss returns to hand; multi Event uses picker (TABLE+attached). Hugh: Rogue Borg ship/location without detail-pick; Borg Ship = Dilemma only when revealed/present; no Borg-affiliation ships. [C]

---

## 2026-09-05 (Fix Ã¢â‚¬â€ WNOHGB wrap path)

**Engine** Ã¢â‚¬â€ WnohgbRules: ends adjacent for controller; hazard check uses wrap path (Q-Net no longer blocks EndÃ¢â€ â€End as if crossing the middle). Debug wnohgb wrap=. [C]

---

## 2026-09-05 (Fix Ã¢â‚¬â€ Wormhole pair-check after drag)

**Engine** Ã¢â‚¬â€ Pair-start counts the Wormhole being played; drag removes it from hand before drop so a 2-copy hand no longer fails as count=1. [C]

---

## 2026-09-05 (Extract Slice 4 - Lore/Hugh decide gates)

**Engine** - Lore Returns deny reasons in `EventRules.LoreReturnsDenyReason`; Hugh resolve modes in `InterruptRules.DecideHugh`; optional `KevinEventAtLocation` for Convergence filter. TableWindow keeps discard/UI/side effects. [C - Grundlage]

---

## 2026-09-05 (Extract Slice 3 - IncomingMessageRules)

**Engine** - Incoming Message apply early gates in `IncomingMessageRules` (`EarlyReject` / `IsAlreadyAtFacility` / `SameLocation`); TableWindow still attaches, highlights, picks facility, and moves. Attach-before-arrival order preserved; FindMissionForDockable parked bug untouched. [C - Grundlage]

---

## 2026-09-05 (Extract Slice 2 Ã¢â‚¬â€ Wormhole pair)

**Engine** Ã¢â‚¬â€ Wormhole pair gates in `InterruptRules` (`CanStartWormholePair` needs 2 in hand, `CanWormholeFirstOnShip`, `IsWormholeLocationCard`). Second-drop hit-test uses window rects; relocate syncs BoardStore. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-05 (Extract Slice 1 Ã¢â‚¬â€ MovementHazardRules)

**Engine** Ã¢â‚¬â€ Q-Net/Tetryon check + Rift/Gaps after-move decisions live in `MovementHazardRules` (no WPF); TableWindow applies damage/discard/status. Gaps kill still only on Gaps location. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-05 (Fix Ã¢â‚¬â€ Gaps kill only on Gaps location)

**Engine** Ã¢â‚¬â€ `ApplyEventAfterMove` Gaps random kill only when destination is the Gaps span (not Host/Host2 neighbor missions). Kill writes Action History + `gaps-kill` debug. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-04 (Foundation E6 Ã¢â‚¬â€ IM/Required-Move Locations)

**Engine** Ã¢â‚¬â€ Incoming Message / required-move hops use `FlyBoardLine` + `Location.Span` (same as Fly); `MissionsOnSameSpaceline` remains paint. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-04 (Foundation E5 Ã¢â‚¬â€ LegalMoves-Fly Locations)

**Engine** Ã¢â‚¬â€ `LegalMoves` Fly Collect uses `BoardStore` Location line via `EngineAuthority.FlyLineForPiece` (same as `CanMoveShip(Location[])` / `TryEvaluateFlyPath`); `OrderedMissions()` name list only as fallback. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-04 (Fix Ã¢â‚¬â€ E4 Unique/Persona by Owner)

**Engine** Ã¢â‚¬â€ Unique/Enigma/Persona deny uses `BoardStore.InPlay(..., Owner)` (Glossary: restrict stays with owner under Lore/capture/commandeer). Opponent may still field their own copy. Log `unique deny Ã¢â‚¬Â¦ owner=`. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-04 (Foundation E4 Ã¢â‚¬â€ InPlay/Unique/Persona by instance)

**Engine** Ã¢â‚¬â€ `BoardStore.InPlay` / `InPlayInstances` query spaceline+TABLE by Controller (Owner separate for Lore). `PlayRules`/Report unique deny by InstanceId+persona; log `unique deny Nebula #283 have=#240 controller=2`. Attempt/HiddenAgenda InstanceId-first. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-03 (Foundation E3b Ã¢â‚¬â€ Cloak + Dock + Hull on instance)

**Engine** Ã¢â‚¬â€ `ShipInstance.Cloaked` / `DockedAtId` / `HullPercent` are source of truth; UI `_cloakedShips` / `_dockedAt` / `_hullDamagePercent` stay mirrors. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-03 (Fix Ã¢â‚¬â€ Fly ship lookup by InstanceId)

**Engine** Ã¢â‚¬â€ Fly resolves ship by InstanceId not name (two U.S.S. Nebula). Klasse C.

---

## 2026-09-03 (Foundation E3 Ã¢â‚¬â€ RangeLeft + Stopped on instance)

**Engine** Ã¢â‚¬â€ `ShipInstance.RangeLeft` + `CardInstance.Stopped` are source of truth for Capture/`ToGameState`/Overlay; UI `_shipRangeLeft` / `_stoppedBorders` stay mirrors (write-through + Sync copy). Log `range #id left=N source=instance` on Fly. Cloak/Dock/Hull deferred to E3b. Dual-run kept; E2 hang fix untouched. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-03 (Fix Ã¢â‚¬â€ E2 Beam hang)

**Engine** Ã¢â‚¬â€ Beam no longer freezes the WPF UI. Root cause: `Log.Changed` Ã¢â€ â€™ `RefreshActionHistory` Ã¢â€ â€™ `LegalMoves` fly-eval `DebugLog.Move` Ã¢â€ â€™ `HistorySink` Ã¢â€ â€™ `AddDebug` Ã¢â€ â€™ `Changed` again (dispatcher flood). E2 store-first made ships Staffed so Fly-eval ran after Beam's log. Guard + coalesce refresh; skip HistorySink while refreshing. E2 store-first Capture kept. [C]

---
## 2026-09-02 (Foundation E2 Ã¢â‚¬â€ Capture store-first)

**Engine** Ã¢â‚¬â€ `CaptureEngineState`: HostName/Staffed/Aboard from BoardStore Occupant when present; UI border crew/staff walks only as fallback, logged `state-fallback:` / `capture: source=store|fallback`. RangeLeft/Stopped still UI (E3). Dual-run kept. [C Ã¢â‚¬â€ Grundlage]

---

## 2026-09-02 (Foundation E1 Ã¢â‚¬â€ ToGameState)

**Engine** Ã¢â‚¬â€ `BoardStore.ToBoardPieces` / `ToGameState(GameStateSeed)`. Capture bevorzugt Store-Board + Crew, UI-Board nur wenn Spaceline leer (Seed). Status (RANGE/Stopped) Overlay. Log `state:` / `capture: source=store|fallback`. [C Ã‚Â· Grundlage]

---

## 2026-09-02 (Board Schritt 6 Ã¢â‚¬â€ AufrÃƒÂ¤umen)

**Board** Ã¢â‚¬â€ tote Namenslisten-Helper weg; Dockables/IM als View markiert. [C]

---

## 2026-09-02 (Board Schritt 5 Ã¢â‚¬â€ Targeting vom Store)

**Target** Ã¢â‚¬â€ Gaps/Q-Net-Paare und Play-on-Hosts aus `BoardStore.Locations` / Occupants. Kevin-Snap bleibt UI. [C Ã‚Â· Grundlage]

---

## 2026-09-02 (Fly-eval Log + Engine auf Board)

**7.1.5** Ã¢â‚¬â€ Engine-Fly nutzt Board-Locations. Log `fly-eval` / `fly-mark` mit from/to/hops. [C]

---

## 2026-09-02 (Board Schritt 4 Ã¢â‚¬â€ Beam schreibt Force)

**Board** Ã¢â‚¬â€ Add/Remove Host-Stapel schreibt `Force`. GetCrewOnShip = Board Ã¢Ë†Âª Stack. Fly-Staffing sieht frisch gebeamte Crew. [C Ã‚Â· Grundlage]

---

## 2026-09-02 (Board Schritt 3 Ã¢â‚¬â€ Fly liest Board)

**7.1.5** Ã¢â‚¬â€ Fly-Highlight + RANGE aus `BoardStore.Locations` (Gaps-Span, Q-Net-Kante). Apply/Relayout unverÃƒÂ¤ndert. [C Ã‚Â· Grundlage]

---

## 2026-09-02 (Board Sync Gaps/Q-Net)

**Board** Ã¢â‚¬â€ Sync liest Gaps/Q-Net aus AttachedEvent (Host/Host2), nicht nur `_spacelineOrder`. [C]

---

## 2026-09-02 (Board Schritt 2 Ã¢â‚¬â€ Sync)

**Board** Ã¢â‚¬â€ SyncBoardFromTable nach Seed/Fly/Beam + Dev Dump Board. Gaps = Location, Q-Net = Barriere. UI bleibt Quelle. [C Ã‚Â· Grundlage]

---

## 2026-09-02 (Board Schritt 1 Ã¢â‚¬â€ leere Typen)

**Board** Ã¢â‚¬â€ `Game/Board/`: Spaceline, Location, Occupant, Force, *Instance, BoardStore.Wrap. Dev-MenÃƒÂ¼ Dump Board. Kein Sync, kein Fly. [C Ã‚Â· Grundlage]

---

## 2026-09-02 (Board Schritt 0 Ã¢â‚¬â€ File-Logger)

**Debug** Ã¢â‚¬â€ `Services/DebugLog.cs`: Session-Datei `Data/Logs/stccg-Ã¢â‚¬Â¦.txt`. ActionLog schreibt mit. KanÃƒÂ¤le Play/Move/Beam/Target/Board/Layout/Engine/Save. CheckTrace.Cmp nicht in die Datei. [C Ã‚Â· Grundlage]

---

## 2026-09-02 (Board-Modell Plan)

**Docs** Ã¢â‚¬â€ `BOARD_MODEL.md`: Dual-Run Spaceline/Location/Occupant/Force; Schritt 0 File-Logger. Kein Code. [C]

---

## 2026-09-02 (Targeting Step 4 + single-stack snap)

**Peek** Ã¢â‚¬â€ 1 Event auf dem Host: OwningHost + Strip + Snap-Rahmen. [B Ã‚Â· nullify]

**Seed / Report** Ã¢â‚¬â€ TargetWhy.Seed / Report in CollectSitesForDrag, Glow ÃƒÂ¼ber TargetSession. [B]

---

## 2026-09-02 (HasSkill freeze / Gaps click / span X)

**HasSkill** Ã¢â‚¬â€ kein per-Token CheckTrace. [A]

**Gaps-Fly** Ã¢â‚¬â€ Klick auf Landable; GameState.OrderedMissions enthÃƒÂ¤lt Span-Namen.

**Span-Anzeige** Ã¢â‚¬â€ keine P2-Rotation; nach Relayout in die LÃƒÂ¼cke.

---

## 2026-09-01 (Gaps location / IM wrap RANGE)

**Gaps** Ã¢â‚¬â€ Landbare Location Span 4; Fly-Glow + Anker. Q-Net bleibt Barriere, kein Stop. Nach Insert Relayout ÃƒÂ¼ber ActualWidth. Event nicht mehr auf den zwei Nachbar-Missionen. [A Ã‚Â· Plays on spaceline]

**7.10 + WNOHGB** Ã¢â‚¬â€ KÃƒÂ¼rzester Hop, den das Schiff diese Runde zahlen kann; Wrap nur wenn RANGE reicht, sonst die andere Seite.

---

## 2026-09-01 (Lore NA attack / IM discard visual)

**Lore / 7.4.1** Ã¢â‚¬â€ GetAffiliations achtet auf CurrentAffiliation (Commandeer = NA). Wartime nur nach gelungenem Angriff auf FED. [A]

**IM Arrival** Ã¢â‚¬â€ Mini vom Canvas, dann Discard (kein Stapel oben links). [A Ã‚Â· 7.10]

---

## 2026-09-01 (TargetQuery PlayOn / Gaps / IM)

**PlayOn** Ã¢â‚¬â€ Event-Ziele ÃƒÂ¼ber TargetQuery.CanPlayOn (Lore / Neural / Plasma / Espionage / Schiff / Planet / Mission / Outpost). [B Ã‚Â· Plays on]

**Gaps / Q-Net** Ã¢â‚¬â€ Site GapSpan, Glow zwischen den Missionen.

**Incoming Message** Ã¢â‚¬â€ Schiff CanPlayOn; Facility CanImFacility + Glow + Place-Choose.

---

## 2026-09-01 (TargetQuery Nullify)

**Targeting** Ã¢â‚¬â€ Game/TargetQuery.cs: eine Legal-Liste (Sites). Kevin/Devil: Glow Ã¢â€ â€™ 1s Peek Ã¢â€ â€™ Snap; Drop auf Schiff ohne Snap = Place-Choose der Events auf diesem Host. [B Ã‚Â· nullify]

**ZurÃƒÂ¼ckgestellt:** Beam-Vollmodus, Battle-Zielwahl, Response-Stack, Netz/KI.

---

## 2026-09-01 (Lore fly anchor / peek Run)

**Fly after Lore** Ã¢â‚¬â€ FindMissionForDockable: IsMissionCard + Pin nach Relayout, damit Engine HostName hat. [A Ã‚Â· 7.1.1]

**Peek** Ã¢â‚¬â€ Parent-Walk vertrÃƒÂ¤gt Run (kein Visual). [UI]

---

## 2026-08-31 (Lore battle / Hugh mission / IM dock)

**Lore Battle** Ã¢â‚¬â€ CanInitiateShipAttack akzeptiert Lore-Staffing statt Leader. [A Ã‚Â· Lore Returns]

**Hugh** Ã¢â‚¬â€ Snap nur auf die Mission, nicht auf ein Schiff. Kill bleibt alle RB beider Seiten. [A Ã‚Â· location]

**Kevin vs Lore Returns** Ã¢â‚¬â€ Nullify gibt das Schiff dem Owner zurÃƒÂ¼ck (Seite wechseln), RB self-controlling. [A Ã‚Â· 7.8]

**IM Buruk** Ã¢â‚¬â€ Required move dockt ab; Hop-Fail im Log. History 2500 Zeilen / grÃƒÂ¶ÃƒÅ¸eres Fenster. [A Ã‚Â· 7.10]

---

## 2026-08-31 (Lore / Hugh / IM / Crosis)

**Lore Returns** Ã¢â‚¬â€ Schiff staffed + Fly/Battle ohne Leader-Crew; Rogue Borg PlanetÃ¢â€ â€™Schiff beamen; Relayout auf Controller-Seite. [A Ã‚Â· Lore Returns / 7.8]

**Hugh Location** Ã¢â‚¬â€ Ziel ist die Spaceline-Location (Mission oder Schiff dort), nicht ein einzelner Rogue-Borg-Token. Kill weiter alle RB an der Location. [A Ã‚Â· Hugh / Glossary location]

**Crosis Detail** Ã¢â‚¬â€ Interrupt nur einmal in der Leiste (nicht Stack + Attach). [UI]

**Incoming Message Hop** Ã¢â‚¬â€ RANGE-Index = Missions derselben Spaceline; Wrap nur Controller; Schiff nach Authority umsetzen. [A Ã‚Â· 7.10]

---

## 2026-08-31 (Probespiel)

**Hugh / Rogue Borg** Ã¢â‚¬â€ Kill rÃƒÂ¤umt Host-Stapel + Token; Discard auf *Owner*-Pile (nicht Hugh-Spieler / Controller). [A Ã‚Â· Hugh / Glossary discard pile]

**Kevin vs Static Warp Bubble** Ã¢â‚¬â€ SWB ist kein Treaty und kein [Shield]; Kevin darf nullify. Traveler: Transcendence bleibt der andere Nullifier. [A Ã‚Â· Kevin / printed]

**Kevin-Snap im Host-Detail** Ã¢â‚¬â€ Overlay ÃƒÂ¼berdeckt das Schiff nicht mehr den Mini-Snap; Rahmen folgt der Mini unter dem Cursor. [UI Ã‚Â· Plays on]

**WNOHGB** Ã¢â‚¬â€ Wrap nur fÃƒÂ¼r den Controller (eigene TABLE-Spalte), nicht fÃƒÂ¼r beide. [A Ã‚Â· printed Ã¢â‚¬Å¾You mayÃ¢â‚¬Â¦Ã¢â‚¬Å“ / 7.1.7]

**Incoming Message UI** Ã¢â‚¬â€ Auto-Zug in Execute setzt das Schiff visuell hop-weise auf die nÃƒÂ¤chste Location (RANGE war schon abgezogen). [A Ã‚Â· IM / 7.10]

**Beam in den Weltraum** Ã¢â‚¬â€ Kein Away-Team auf Space-Missionen (7.1.1.0.1). Planet-AT unverÃƒÂ¤ndert. Staffing-Fehler nennt jetzt leere Crew vs. Affiliation. [C Ã‚Â· 7.1.1.0.1]

**Subspace Schism** Ã¢â‚¬â€ im Probespiel bestÃƒÂ¤tigt, von der Offenen-Liste.

---

## 2026-08-31

**Mission OR / xN** Ã¢â‚¬â€ `Diplomacy x5 OR Honor x4` nicht mehr still ÃƒÂ¼bersprungen. `+` = UND, `OR` = eine Alternative. Skill-Count exakt (kein StartsWith). [A Ã‚Â· 7.2.5]

**Battle-Overlay** Ã¢â‚¬â€ Ship/Personnel-Ergebnis + Return Fire ÃƒÂ¼ber Reveal/AskChoice, nicht Windows-MessageBox. Destroy erst nach OK, damit Escape-Pod-Response offen bleibt. [UI Ã‚Â· 7.4.3 / Escape Pod]

**Espionage (PR 4)** Ã¢â‚¬â€ Nur Mission mit [On]-Icon. FÃƒÂ¼r den Besitzer zÃƒÂ¤hlt die Mission zusÃƒÂ¤tzlich als [As]. Discard beim LÃƒÂ¶sen (war schon da). [A Ã‚Â· Espionage / 7.2]

**Incoming Message Auto-Zug** Ã¢â‚¬â€ Beim Wechsel PlayÃ¢â€ â€™Execute: volle RANGE, hop-weise zur Facility. Fly nur in Execute (darum vorher tot). [A Ã‚Â· IM / 7.10]

**WNOHGB RANGE** Ã¢â‚¬â€ KÃƒÂ¼rzerer Ring-Pfad (nicht nur EndeÃ¢â€ â€Ende). Gaps-Span aus Text (`span 4`) zÃƒÂ¤hlt mit. `HasTableCard` nur TABLE-Spalte, nicht Attached Events. [A Ã‚Â· 7.1.7]

**Beam Affiliation** Ã¢â‚¬â€ Personnel nur auf kompatibles Schiff/Facility (Treaty/NA). Equipment frei. Planet-AT frei. [A Ã‚Â· 7.1.1 / compatible]

**Data-Pfade** Ã¢â‚¬â€ `GamePaths.DecksRoot` / `SaveGamesRoot` (`Data/Decks`, `Data/SaveGames`). Dialoge starten dort. csproj: Content-Copy fÃƒÂ¼r beide Ordner.

**Icons** Ã¢â‚¬â€ `Assets/Icons/Icon_{Token}.png`, Text-Fallback. Staffing-Zeile + Host-Badge Icon-Counts.

---

## 2026-08-30

**Spacedock / Dock** Ã¢â‚¬â€ Schiff-MenÃƒÂ¼ Dock at [Facility] / Undock. Flag `_dockedAt`. Fly erst nach Undock. Spacedock am Outpost: Dock = volle Reparatur. [A Ã‚Â· 7.1.4 / Spacedock]

**Yellow / Red Alert** Ã¢â‚¬â€ Yellow Alert verhindert Red Alert (Karte bleibt nicht auf TABLE). Bei Nullify von Red Alert: Download Yellow Alert (Draw / Tent). Personnel-SD unverÃƒÂ¤ndert. [A Ã‚Â· Yellow Alert / 6.5.3]

**Target snap (C)** Ã¢â‚¬â€ Eine Liste `CollectLegalSnapHosts`: Halo + Snap-Feld + Drop. Interrupts fielen vorher aus `UpdateSnapPreviewFromWindow`. Neue Karten: `PlayOnRules.Parse` oder ein Named Override. [C Ã‚Â· Plays on]

**Wormhole** Ã¢â‚¬â€ Zwei Karten Pflicht. Erste nur auf eigenes **exposed** Schiff (`!cloaked`). Zweite nur auf Location (Mission / Time Location), Snap-Halo. Schiff dockt dort, wird gestoppt. Kein TABLE-Drop, keine Detail-Picker. [A Ã‚Â· Wormhole / exposed]

---

## 2026-08-29

**Shared unique missions** Ã¢â‚¬â€ Zweite Kopie derselben Unique-Mission (Quick Game, gleiche Decks) bleibt eine Location: unsichtbar, kein eigener Seed-Host. `AllMissionBorders` = nur Spaceline-Primaries. [C Ã‚Â· unique and universal]

**Shared Face / 4.2.0.3** Ã¢â‚¬â€ Ein Stapel. Bild + Rotation = Zugspieler (dessen Drucktext). Attempt/Solve ÃƒÂ¼ber `MissionPrintedFor`. Shared ist your mission und opponent's mission. Kein Mission-II, kein 4.3-Staging.

**Lore Returns staff** Ã¢â‚¬â€ Host-Match `SameHostShip`; Schiff NA + Controller; Fly/Attack mit Rogue Borg ohne Officer. [A Ã‚Â· 7.8 / Lore Returns]

**Hugh** Ã¢â‚¬â€ [Bor]-Schiff, Borg-Ship-Dilemma, Rogue Borg. Response bricht Battle; Drop auf RB tÃƒÂ¶tet alle RB der Location; Drop auf Dilemma blockt den nÃƒÂ¤chsten Puls. [A Ã‚Â· Hugh / 7.4.1.0.2]

**Mission-Drehung** Ã¢â‚¬â€ Nur Shared-Locations folgen dem Zugspieler.

**Horga'hn leave-play** Ã¢â‚¬â€ Effekt nur solange die Karte auf dem Tisch liegt. `OnCardLeftPlay` bei Discard/OOP/Hand. [B Ã‚Â· in play]

**Board extents** Ã¢â‚¬â€ Canvas wÃƒÂ¤chst mit gestapelten Schiffen; Spaceline rutscht nach unten wenn P2-Stapel ÃƒÂ¼ber Y=0 ginge. Scrollbars Auto, Zoom-Min passt sich der FeldgrÃƒÂ¶ÃƒÅ¸e an, RMB-Pan erreicht jede Karte.

**Incoming Message** Ã¢â‚¬â€ Matching-Affiliation-Schiff (beliebiger Besitzer). Controller wÃƒÂ¤hlt eigene passende Facility auf **dieser** Spaceline (gleicher Quadrant). Bleibt am Schiff; nur Bewegung dorthin (7.10). Nullify bei Ankunft / keiner Facility. Subspace Interference im Spiel; Amanda nur just-played. [A Ã‚Â· IM / 7.10 / 12.10]

**Required-move path** Ã¢â‚¬â€ KÃƒÂ¼rzester Weg auf derselben Spaceline (Quadrant). WNOHGB-Wrap wenn kÃƒÂ¼rzer. Cytherians-Far-End einmal fest (12.6). Mehrere Required Actions: Spieler wÃƒÂ¤hlt Reihenfolge. Conundrum nutzt denselben Pfad. [C Ã‚Â· 7.10 / 12.6]

**Energy Vortex** Ã¢â‚¬â€ ZurÃƒÂ¼ck auf die Hand, Ersatz-Play erlaubt, **dieselbe Kopie** diesen Zug gesperrt. Andere Karte (auch gleicher Titel, andere Kopie) ok. [A Ã‚Â· printed EV]

**Subspace Schism** Ã¢â‚¬â€ Jeder Draw ÃƒÂ¶ffnet 10s-Response wenn Schism auf einer Hand. Einmal/Zug. Discard der gezogenen, nÃƒÂ¤chste Karte ohne zweites Fenster. [A Ã‚Â· printed Schism]

## 2026-08-27

**Host-Detail** Ã¢â‚¬â€ Schiff: Special Equipment (Holodeck, Tractor Beam) oben bei den Stats; angehÃƒÂ¤ngte Events/Dilemmas darunter mit Live-Effekt statt Persist-Enum (`Event: Nutational Shields Ã¢â‚¬â€ SHIELDS +2 (1 ENGINEER aboard)`). `EventRules.FormatHostEffectSummary` / `DilemmaRules.FormatHostEffectSummary`. Personnel: keine doppelte Classification, kein Skill-Dump aus `card.Text`, Icons an die Classification-Stelle. Overlay: Kartenleiste unten mit horizontalem Scroll, mehr Text darÃƒÂ¼ber, Back + Select/Close rechts.

**Kartenwahl** Ã¢â‚¬â€ Keine Ja/Nein-Karte-fÃƒÂ¼r-Karte-Schleifen mehr. `PickCardFromList` / `PickBorderFromList` nutzen die Detail-Leiste (horizontal scroll). Umgestellt: Res-Q, Palor Toff, Wormhole, PickOwnShip, Stone of Gol, Thought Maker, Kurlan, Event-Target-Dialog, Dilemma-Picks.

**Subspace Warp Rift** Ã¢â‚¬â€ Fly-by beide Richtungen; Schaden beim Weiterfliegen nach Ankunft im selben Zug. Tetryon analog. Kevin trifft Gaps-Span + Fallback-Galerie. `PickOption` fÃƒÂ¼r OR-Textwahlen.

**Nullify / Devil / Rift-Badge** Ã¢â‚¬â€ Kevin/Devil nicht mehr als TABLE-Permanent ohne Ziel spielen. Nullify entfernt Span auf der Spaceline, Owner-Discard, Kevin OOP-Zone. Relayout behÃƒÂ¤lt Rotation + DMG-Badge am bewegten Schiff. Action History: Mehrfachauswahl + Ctrl+C.

## 2026-08-28

**Horga'hn** Ã¢â‚¬â€ Flag folgt der Tischkarte (Hand-Play + Acquire). Extra Normal-Play bleibt im Play-Segment; sonst Extra-Draw am EOT aus dem Deck des Controllers. Devil lÃƒÂ¶scht nur den Owner-Flag. [A Ã‚Â· 2.3.0.1]

**The Devil / Wind Dancer** Ã¢â‚¬â€ Beim Encounter (vor Filter) Yes/No, wenn The Devil auf der Hand liegt. `EncounterDilemma` als Stack-Kind; CollectDevil sieht Last-Encounter. [A Ã‚Â· 6.5.1 / Glossary nullify]

**Stapel-Peek (UI)** Ã¢â‚¬â€ Kevin/Devil 2s ÃƒÂ¼ber Schiff/Mission/Outpost mit legalem Ziel ÃƒÂ¶ffnet Detail; legale Karten gold umrandet, Drop trifft die Mini.

**Warp Core Breach / Ã¢â‚¬Å¾May be nullified by SKILLÃ¢â‚¬Å“** Ã¢â‚¬â€ Nullify ist optional (Schiff-Button, auch gestoppt), nicht automatisch am EOT. `NullifyEventInPlay` rÃƒÂ¤umt Host-Stapel + Detail. ZerstÃƒÂ¶rung erst am EOT des *nÃƒÂ¤chsten* Controller-Zugs (Countdown 2 im laufenden Controller-Zug). Plasma Fire derselbe Discard-Pfad. [B Ã‚Â· Glossary nullify / 7.10.0.1 / 8.1]

**AskChoice** Ã¢â‚¬â€ OR-Dialoge mit beschrifteten Buttons (z.B. Klingon / Romulan, nicht Yes/No). Timeout = gleichverteilt zufÃƒÂ¤llig + Result-Overlay. `AskPlayer` fÃƒÂ¼r P1/P2. Umgestellt: Dual-Affiliation-Report, Juggler, Traveler, Draw-3, Masaka, Q's Tent. `PickOption` nutzt dasselbe.

**PlayOnRules** Ã¢â‚¬â€ Ã¢â‚¬Å¾Plays on Ã¢â‚¬Â¦Ã¢â‚¬Å“-Parser (Ship/Outpost/Facility/Mission/Crew/Table + Own/Exposed/Occupied/Cloaked). Interrupt-Drop nutzt Spec; Schiff Ã¢â€°Â  Outpost. Asteroid Sanctuary nur eigenes uncloaked Schiff.

**Fed Battle / Devil / LegalMoves** Ã¢â‚¬â€ Federation initiiert Battle nur vs Borg (NA/Kli im selben Force hebt das nicht); Wartime bleibt Ausnahme. The Devil = `nullify-inplay`. Off-turn-Interrupts erscheinen in LegalMoves. [C Ã‚Â· Ã‚Â§7.4.1 / 6.5.1]

**Prozess** Ã¢â‚¬â€ `RULES.md` = Fix-Protokoll Klasse A/B/C + Lookup Glossary/Errata. `RULES_CHECKLIST.md` = Compendium 2.7.4 auf x.x.x.

---

## 2026-08-26

**LegalMoves** Ã¢â‚¬â€ `Collect(player)` nur dieser Sitz; Stack nur `ResponsePlayer`; `CollectBoth` fÃƒÂ¼r Netz/KI; Beam-VorschlÃƒÂ¤ge von Schiff, Facility und Mission.

**Dual-Affiliation (6.3.3)** Ã¢â‚¬â€ `Card.CurrentAffiliation`, `DualAffiliationRules`, Mode-Skills (Rakal/DeSeve), Report wÃƒÂ¤hlt kompatiblen Mode, Switch zwischen Actions (nicht im Attempt, nicht inkompatibel an Bord).

**AU Apply** Ã¢â‚¬â€ Yellow Alert (kill/prevent Red Alert, CUNN+1), Baryon RANGEÃ¢Ë†â€™2 + SOT-Nullify, Klim kein EOT-Draw, Thermal vs Firestorm/Thought Fire/Plasma, Captain's Log / Lower Decks, Particle Scatter kein Planet-Beam, Intruder FF Rogue Borg &lt;3, Wartime nach Fed-Angriff. Interrupts: Kevin Convergence, Countermanda, Destroy Scow, Senior Staff Meeting.

**Dil Apply** Ã¢â‚¬â€ Edo Probe Lock oder Ã¢Ë†â€™10 am Zugende; Conundrum chase; Frame of Mind 3-3-3 + 2 Skills, Cure 3 Empathy.

**PR Apply** Ã¢â‚¬â€ Neural Servo Control bis EOT; Distortion Unstop/RANGE/Away-Team; Tachyon + Cloak-Button; Anti-Time Shuffle beider Spieler; Sanctuary EOT-Discard.

**Katalogtexte** Ã¢â‚¬â€ PR Events/Interrupts/Artifacts an Drucktext; Kevin OOP; Auto-Destruct CD=1, Groupie/Temporal Rift CD=2; Life-form Scan = Gegnerhand.

**Engine-Fundament** Ã¢â‚¬â€ `GameState` / `GameAction` / `EngineAuthority` / `EffectRegistry` / `CardEffectMap` Ã‚Â· `SeedRules` (Cryo Space, Neutral Outpost NA, 3 AU) Ã‚Â· `TurnExpiry` Ã‚Â· Download/HA-Flip-Verben Ã‚Â· LegalMoves Seed Ã‚Â· Until-EOT nur finishing player Ã‚Â· Stopped im Snapshot.

---

## 2026-08-21Ã¢â‚¬â€œ25

Opponent-Pile-Inspector Ã‚Â· Mission Owner-Orientation / asymmetric reqs Ã‚Â· Crew-Interrupt-Ziele Ã‚Â· Rogue Borg + Crosis + Lore Returns Ã‚Â· TurnScope Ã‚Â· Skill-Parser `Diplomacy x 2`.

---

## 2026-08-20

`.stsave` JSON Ã‚Â· Event-Targets / Gaps / Q-Net Ã‚Â· TABLE volle rechte Spalte Ã‚Â· Red Alert 5er-Play Ã‚Â· Kidnappers Ã‚Â· Host-Events bleiben am Ziel.

---

## 2026-08-19

Deck Builder EN, Inline-Move, `DeckPlacementRules`, Skill-Multifilter.

---

## 2026-08-18

Ship Battle + Rotation Damage Ã‚Â· DilemmaÃ¢â€ â€™Stopped Ã‚Â· Repair Ã‚Â· Equipment/Modifier Ã‚Â· Treaties Ã‚Â· Interrupt ActionStack Ã‚Â· Premiere Dil/Art/Event/Interrupt-Kataloge Ã‚Â· Card Reveal Ã‚Â· Action History.

---

## 2026-08-16Ã¢â‚¬â€œ17

Phase-2-Tisch: Seed-Phasen, Hotseat feste Spaceline, Report/Staff/RANGE/Beam, Mission Attempt, Hybrid-Orders-UI.

---

## 2026-08-15

Phase 0 Modelle + JSON-Loader Ã‚Â· Phase 1 Deck Builder `.stdeck` Ã‚Â· Lackey-Split in Set-Ordner.


## 2026-09-21 â€” StartOfTurnWindow (Phrase) + Full Planet Scan Gate
- Neu: `TimingRules.RequiresStartOfTurnWindow` / `IsStartOfTurnWindowOpen` / `CanPlayStartOfTurnCard` (Compendium 6.1 Exception; Segmente SoTâ†’NormalPlayâ†’Executeâ†’EoTâ†’Draw).
- Gate vor Stack/Responses: `LegalMoves.AddHandPlays`, `EngineAuthority.EvaluatePlay`, `TableWindow.TryAllowHandPlay`.
- Full Planet Scan = erster Phrase-Consumer (Gametext + Katalog); Apply-Pfad nur Safety-Net.
- Pepsch-Fail: FPS nach Normal-Play Ã¶ffnete Amanda-Fenster; Illegal kam zu spÃ¤t â†’ jetzt sofort Deny.

