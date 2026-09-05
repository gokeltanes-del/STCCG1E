# Changelog

Nur spielbare / engine-relevante Schritte. Keine Chat-Metadaten.

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

