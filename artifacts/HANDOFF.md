# HANDOFF ├ö├ç├Â STCCG 1E

**Owner:** Captain Ôö¼├Ç **Stand:** 2026-09-28 Ôö¼├Ç **Ort:** Josef `C:\Dev\StarTrekCCG\StarTrekCCG`

Nur aktueller BrÔö£ÔòØckenstand. Historie: `CHANGELOG.md`. Status: `CARD_TRACKER.md` (Jadzia), `FEATURES.md` (Seven), Coverage.
Archiv der aufgeblÔö£├▒hten VorgÔö£├▒ngerdatei: `GROK_TEMP/HANDOFF_ARCHIVE_*.md`.

---

## Jetzt aktiv ├ö├ç├Â Multiplayer / Network-Modus

**Status:** Span overlay subsystem Option B getippt (2d7acc7). Order=Missionen; Spans=PaintSpans Overlay Endpoints; Capture ohne Spans. Pipeline 5b043b0/ab2e67a/a9af295. **Smoke Dual-EXE: zwei Q-Nets verschiedene Gaps -> gleiche Spaceline P1=P2, kein Float/Extra-Width/Stack. HOLD.**
**Ziel:** LAN, Internet (Direct IP / VPN), 2 Instanzen auf einem PC (Localhost).

### Architektur
- Client-Server: Host = P1 + autoritative Wahrheit; Gast = P2.
- TCP + JSON, kein externes Netzwerk-Framework.
- Nutzen: `GameAction`, `LegalMoves`, `EngineAuthority`, `GameSave`-JSON; Mode-Stub bereits in der UI.

### Roadmap
1. **Phase 1 ├ö├ç├Â Transport:** `NetMessage`, `NetServer`, `NetClient` unter `StarTrekCCG/Network/`.
2. **Phase 2 ├ö├ç├Â Lobby/UI:** Host / Join / Localhost. ├ö├Ñ├å Tip `2aa9790`.
3. **Phase 3 ├ö├ç├Â Sync:** GameAction ├ö├Ñ├å Host EngineAuthority ├ö├Ñ├å maskierter State. ├ö├Ñ├å Tip df904b4.
4. **Phase 4 — Dialoge/Timing:** ChoiceRequest / ChoiceResponse. → Tip e102b47.
5. **Phase 5 ├ö├ç├Â HÔö£├▒rtung:** Disconnect, Reconnect, Abbruch.

**Code-Stand (Data 2026-09-28):** Span overlay subsystem B 2d7acc7; Q-Net Extra-Width+SWB Face 5b043b0; Span gap InstanceIds fef7075; TAK+SWB AskChoice ab2e67a; Responsive Overlays a9af295. Kein Push durch Bots. **HOLD.**

---

## Zuletzt (kurz)
- Personnel-Battle-Ergebnis: roter Rahmen um jedes Face eines Toten, darueber "Personnel died" (ueber der Face-Reihe, nicht ueber dem Dialog). Kein Tod: kein Face, keine Zeile. Andere Karten ohne diesen Rahmen. OK und Initiation unveraendert. Hotseat und beide Fenster. **HOLD Dual-EXE.**
- Personnel-Battle-Dialog: Face war `atkCards.First` (Stapelreihenfolge der Angreifer, im Smoke Koroth), nicht der Tote. Jetzt nur die Faces der wirklich Gestorbenen, bei mehreren jedes, bei keinem nur der Satz. OK beim Angreifer, das andere Fenster ohne Buttons schliesst mit. Hotseat und Guest. Initiation unveraendert. **HOLD Dual-EXE.**
- Premiere: Personnel Battle nur Away Team gegen Away Team auf derselben Planetenmission, oder Crews schon auf demselben Schiff/Facility. Zwei Schiffe am selben Ort kaempfen nicht Crew gegen Crew (Ship Battle WEAPONS/SHIELDS, wenn sonst legal). Federation initiiert keinen Personnel- und keinen Ship-Battle, auch nicht als Counter-Attack naechste Runde, ausser eine Karte im Spiel erlaubt es (Wartime Conditions, solange sie liegt). Return Fire und Verteidigen im schon geoeffneten Kampf bleiben. Borg-Ausnahme bleibt. Abgelehnt: nur Statuszeile, kein Stack, kein Glow, kein Dialog, Hotseat und Netz. Guest schickt weiter nur den Host. **HOLD Dual-EXE.**
- InitiatePersonnelBattle Guest→Host. Guest schickt nur den angreifenden Host (`InitiatePersonnelBattle`, Target leer). Host entscheidet Kraefte/`BattleRules`, oeffnet denselben Stack, broadcastet maskiert. Ein Ziel nimmt der Host. Mehrere: Choice `Name #InstanceId` auf dem Guest. Ergebnis (`ShowPublicCardResult`) auf beiden, OK nur beim Angreifer. Host-Glow fuer einen Guest-Kampf gibt es nicht. Hotseat-Klick bleibt. **Kein Kanal:** Brett-Klick bei Nebel, Ziel ohne eindeutige InstanceId (Kampf startet nicht), Gegner-Belegung bleibt zu. Response-Fenster bleibt beim Antwortenden. **HOLD Dual-EXE.**
- Ergebnis-Satz nach Kartenwahl: eine Zeile nennt Spieler, gewaehlte Karte, Effektkarte und deren Owner. SWB: "Player 1 discarded Q-Net to Player 2's Static Warp Bubble." TAK: "Player 1's Telepathic Alien Kidnappers revealed Data — guessed Personnel, discarded." Dieselbe Zeile auf beiden Fenstern, die das Ergebnis sehen. Handauswahl von SWB bleibt nur beim Owner. **Satz jetzt auch, Sichtbarkeit unveraendert:** Q's Tent, Special Download, Betazoid Gift Box, Frame of Mind, Mindmeld, Hidden Agenda. **Satz bleibt:** benannter Download, Alien Parasites, AnnounceChoiceResult (Timeout, verdeckte Hand). Ohne Ergebnis-Dialog unveraendert. Hotseat lokal. **HOLD Dual-EXE.**
- Ergebnis nach Kartenwahl: TAK (gezeigte Karte, Treffer und Fehlgriff) und SWB (erst nach dem Discard) auf beiden Fenstern. Entscheider hat OK, der andere `revealMirror` ohne Klick. SWB-Handauswahl bleibt nur beim Owner. **Meldung unveraendert, kein gezeigt/verdeckt-Fakt:** Q's Tent, Special Download, Betazoid Gift Box, benannter Download, Frame of Mind, Mindmeld-Ergebnis, Hidden Agenda, Alien Parasites Control, AnnounceChoiceResult (Timeout). Ohne Ergebnis-Dialog unveraendert: Hugh, Kevin/The Devil, Palor Toff, Res-Q, Thought Maker, Kurlan, Conundrum, Kevin Convergence, Incoming Message, Subspace Interference, Q-Continuum, Ship Seizure, Honor Challenge, eigenes Schiff, Event-Ziel, Raise the Stakes, Genetronic, Anti-Time. Hotseat lokal. **HOLD Dual-EXE.**
- Guest-Kartenliste: `PickCardForPlayer` / `PickBorderForPlayer`. Entscheider P2 bekommt ChoiceRequest kind=choice (`Name #InstanceId`, Typ/Skill nur Name). Host mappt und wendet an; maskierter Broadcast bleibt am Attempt-/Stack-Ende. P1 und Hotseat bleiben lokal. Dilemma PickOpp geht zum Gegner (Guest, wenn P1 versucht). Mindmeld-Picks des Guests nicht mehr auf dem Host. **Weiter nur auf dem Host:** P1-Wahl; TTP-Gegnerschiff (Board-Klick, Schiff bleibt); Gaps-Index; Liste ohne eindeutiges Label oder Ziel ohne Karte. **HOLD Dual-EXE.**
- AttemptMission: Scow-Tow ist `AttachedDilemmaSnap.TowShipInstanceId` (InstanceId des Schiffs, 0 = kein Tow). Token bleibt aus `save.Table`. Guest schickt `TowScow`; Host `CompleteTractorAttach` plus Broadcast; Fly zieht den Token ueber `SyncTowedScowAfterShipMove`. Beide Fenster legen ihn an die Mission des Schiffs, Seite `EncounteredBy`, kein Schatten. Hotseat-Tow bleibt lokal und unter der Mission. Host-Spiegel schliesst weiter (`ForceHideWatcherMirror`). Guest-Spiegel schliesst weiter (09b6747). Mission solved auf beiden, OK nur beim Loeser. Kein Seed-Fly-in. **Weiter nur auf dem Host:** PickOpp, Alien Parasites wenn P1 entscheidet, The Devil fuer P1. TTP-Schiffsklick ohne Kanal (Schiff bleibt). **HOLD Dual-EXE.**
- Span overlay subsystem (Option B): _spacelineOrder mission-only; PaintSpans Gap-Mid Endpoints; Capture Spaceline ohne Spans; BoardStore aus AE-Paar; Seed/Index mission-only; BuildSpacelineDisplayOrder weg. Tip-Hash 2d7acc7. Pipeline 5b043b0/ab2e67a/a9af295 erhalten. **HOLD.**
- Q-Net second span no Extra-Width stack + SWB Face: Relayout missions-only columns; spans gap-mid Pin; AsMissionEndpointBorder; SWB hand→Face strip. Tip-Hash 5b043b0. Pipeline fef7075/ab2e67a/a9af295 erhalten. **HOLD.**
- Spaceline span gap = mission InstanceIds + Host dock recover (Q-Net): HostInstanceId/Host2InstanceId; Apply/SpanEndpoints/DisplayOrder per InstanceId; EnsureBoardExtents deferred; PinDockables after span Relayout; Fly-in no Host TargetNorm for spans. Tip-Hash fef7075. Pipeline ab2e67a/a9af295 erhalten. **HOLD.**
- TAK + SWB Choice via AskChoiceForPlayer (Owner-Fenster): RunKidnappers/PickHandCardToDiscard Phase-4 Gate; Host RNG+Discard/Reveal; kein KidnapOverlay PushFrame. Tip-Hash ab2e67a. Pipeline 6acacd4/a9af295 erhalten. **HOLD.**
- Spaceline span fly-in land + gap adjacency (Q-Net Nachzieher): landInst=own InstanceId; IsSpacelineSpanCard Attach-Guard; mission-only ListSameQuadrantGaps; PickAdjacent past spans; Pin gap-X barriers + after RelayoutAll. Tip-Hash 6acacd4. Pipeline a9af295 erhalten. **HOLD.**
- Spaceline Span Y SpacelineY-centered (Q-Net Nachzieher): IsSpacelineRowCard; Apply erzwingt SpacelineY; PinSpacelineSpanCardsY; Ghost Opacity soft-invalidate. Tip-Hash 0443f37. Pipeline a9af295 erhalten. **HOLD.**
- Responsive Detail/Choice-Overlays (Fensterbreite): MaxWidth+Margin statt fester Width/MinWidth; WrapPanel Typ-Buttons/Karten; ScrollViewer H=Disabled. Tip-Hash a9af295. Pipeline 3bc7e49 erhalten. **HOLD.**
- Spaceline Attach Y board-absolute + History RMB-only zoom: Sync Y P1+/P2-; kein Gaps AttachCardToHost; Relayout Owner absolut; History nur RMB Zoom. Tip-Hash 3bc7e49. Pipeline acfb554 erhalten. **HOLD.**

- Action History Kartenreihe + Play-Detail-Popup weg: Strip P1=Gruen/P2=Blau Face; Interrupt-Splash+LRS-OK entfernt; Fly-in bleibt; PlayReveal→History Network-First. Tip-Hash acfb554. Pipeline 5c29f7b erhalten. **HOLD.**

- Host->Guest PlayReveal Face Catalog + Gaps Guest-Drop: Catalog/DTO Face (Fog-Stub-Race); gap:leftInst:rightInst Host Apply ohne Picker. Tip-Hash 5c29f7b. Pipeline 591fba3 erhalten. **HOLD.**

- Guest/P2 Play-Pfad (TAK Persist + Interrupt Fly-in + Face): Net Events→TryResolveEventPlay; ClearTable/Relayout Soft-Invalidate; FindLiveCardWithArt+Discard Lookup. Tip-Hash 591fba3. Pipeline f005c71 erhalten. **HOLD.**


- Play Fly-in Event/Interrupt Target + P2 Face: TargetInstanceId Live-Bounds Host/Guest; Face vor Anim; Net PlaysOnHost attach (nicht TABLE). Tip-Hash f005c71. Pipeline df1259e erhalten. **HOLD.**



- Play Fly-in Ziel stale (Outpost Relayout/Neuspield): Land=Host-Facility nach Layout; Stack AbsoluteLeft sync; TargetNorm at Loaded; Invalidate Clear/RelayoutAll. Tip-Hash df1259e. Pipeline 857ab9e erhalten. **HOLD.**

- Play Fly-in Nachzieher Ziel/Perspektive/Doppel (Pepsch-Video): lokale Slot-Bounds (Canvas+TABLE); Controller-Hand; Ghost-InstanceId; End=Slot Size; Face-DB; Tempo ~2.6s. Tip-Hash 857ab9e. Pipeline 034aec2 erhalten. **HOLD.**


- Play Fly-in sichtbar (Nachzieher-Fix): DragLayer + BeginAnimation; Storyboard/Collapsed-Canvas Root Cause; Debug StatusText. Tip-Hash 034aec2. Pipeline b1d5d3e erhalten. **HOLD.**

- Play Fly-in Nachzieher (Hand→Mitte→Slot): Gerade Bahn Hand→~3.5× Mitte Hold→Slot; DropShadow; kein Dim/Banner/Neon; DTO TargetNorm; Host State vor Reveal. Tip-Hash b1d5d3e. Pipeline 57c1a3e erhalten. **HOLD.**

- Play Fly-in Reveal (Network): Host nach erfolgreichem Hand-Play / Interrupt BeginPlayCardStack → BroadcastPlayReveal + lokale Animation; Guest nur nach PlayReveal-Message (kein lokales Pre-Apply). Overlay non-modal ~1.4s. Tip-Hash 57c1a3e. Board-Sync 8322b68 erhalten. **HOLD.**

- Board-Sync Multiplayer (Fly/Beam/Attack/Interrupt): Guest Action-only; Host TryApplyNet* + sofort Broadcast; kein Phantom-EndTurn-Wipe. Tip-Hash 8322b68. Visibility b2dfaf7 erhalten. **HOLD.**
- Alien Probe Hand-Sync + Occupancy/AT Fog: NetStateMask Probe-Hand + Stack-Occupancy mask; UI FogViewer; 12.12 stub. Prior 5da7c3b erhalten. **HOLD.**

- Guest PlayCard Host-apply (P2 Hand-Wipe Play->Execute): Guest Action-only; Host TryApplyNetPlayCard Ship/Pers/Eq/Event; underInst Note. Tip-Hash 5da7c3b. **HOLD.**

- Skip seed phase (Network Lobby): LobbySkipSeed Propose/Accept/Decline; StartGame.skipSeedPhase; Host AutoCompleteSeed. Tip-Hash 7c040df. **HOLD.**

- Execute-Haenger P2 (Guest Segment): OnSuccessfulHandPlay kein lokales AdvanceSegment auf Guest; Host Broadcast nach Advance; EndTurn-while-Play Remap->EndPhase. Tip-Hash 9abb843. **HOLD.**

- Artifact Seed Glow ALL [P] (Nachzieher): CollectSites seed-before-PlayOn; gold glow; SeedUnderMissionSnapRange; DescribeSeedUnderDeny; ParseLocationIcons harden. Tip-Hash 041e059. Execute unveraendert / Smoke offen. **HOLD.**

- Artifact Seed Targets + P2 End EXECUTE (Network): legal mission snap/glow; Guest EndTurn flip. Tip-Hash 23044e0. **HOLD.**

- Visual Seed-under-Mission Layout (Horga'hn): Relayout pin + orphan scrub. Tip-Hash edc79d7. **HOLD.**

- Network P2 Facility Seed-on-Outpost + Viewer Dock: PlayerForStrip owner; underInst Facility Apply; eigene Facilities unten auf P2-Client. Tip-Hash dabaecc. **HOLD.**

- Network P2 Facility Seed: Guest Target/underInst → Host dockt an Mission (nicht CommitCardToTable/P2 TABLE). Tip-Hash 493fc0b. **HOLD.**

- Network Seed-under-Mission: gleicher Stack Host/Guest; Owner face-up / Opp face-down+Zaehler; underInst+force restore. Tip-Hash 6da1c61. **HOLD.**


- Network P2 Mission-Seed: Guest after:/before: Note → Host engine insert; Slot-Hover glow. Tip-Hash 141c4df.

- Network UI Nachzieher Seed: Hand/Seed face-up LocalPlayer; Mission Glow/Snap; Spaceline Viewer-Orientierung. Tip-Hash 7747bf8.


- Network Guest->Host Seed Authority: Guest Seed nur Action an Host; kein lokales Apply vor Auth; Notify-Gate-Fix. Tip-Hash ccd9aa5.

- Network Host->Guest Seed Sync: EnsureNetworkMode + ActivePlayer-Broadcast/Apply + LocalPlayer-Input-Gate. Tip-Hash f020183.

- Network Viewer=LocalPlayer: Guest sieht P2-Seed/Hand unten; Fremdzonen maskiert; Host-Broadcast nach Seed bleibt. Tip-Hash 1bd513f.

- Network Seed/Mission Sync getippt: Host broadcast nach Seed + SeedCard Guest→Host. Tip-Hash ba2ee6f.
- Network Phase 4 ChoiceRequest/Response getippt: NetChoiceDto + AskChoiceForPlayer + response window routing. Tip-Hash `e102b47`.
- Network Lobby-Flow getippt: Deck pick + Ready + StartGame ÔåÆ Session. Tip-Hash `81455f2`.
- Network Phase 3 Sync getippt: NetActionDto/NetStateMask/NetPlaySession + TableWindow EndPhase/EndTurn. Tip-Hash df904b4.
- Network Phase 2 Lobby getippt: `NetworkLobbyWindow` + ModeNetwork-Anbindung. Tip-Hash `2aa9790`.
- Network Phase 1 Scaffold getippt: `StarTrekCCG/Network/` (NetMessage/NetServer/NetClient). Tip-Hash `027f993`.
- EXTRACT P2├ö├ç├┤P5 und viele Premiere-Tips: `CHANGELOG.md` (`EXTRACT_REST.md` fehlt lokal unter artifacts).
- Tracker bleibt aktiv (Jadzia). Behauptete LÔö£├éschung vom 26.09. war falsch ├ö├ç├Â Datei und Docs-Verweise existieren weiter.

---

## Offen / Park

### Karten (Tracker, Jadzia 2026-09-27)
- Premiere: ~108 `working` / 6 `partial` / 249 `unknown`.
- Partials u. a.: *Vulcan Mindmeld*, *Crystalline Entity*, *Iconian Computer Weapon*, *Alien Probe*, *Escape Pod*, *Q* (Continuum-Park). CHANGELOG/alter HANDOFF hatten fÔö£ÔòØr die ersten drei ├ö├ç├ùPepsch grÔö£ÔòØn├ö├ç┬ú ├ö├ç├Â Tracker noch `partial`, bis Pepsch klar abnimmt.

### Park / Smoke (Seven + ONLINE_WORKFLOW)
- Continuum / Q
- Plays-on / Plays-as F3-Smoke
- AI Freundes-Report Beaming; ETA `21b2d52` Retest; Artifact-Y Load intermittent
- FEATURES Smoke offen: Beaming 7.1.1, Response-Window Hotseat, Occupancy Badge
- FEATURES-Zeilen Extract P0-D1/E1/S1 sind veraltet vs. CHANGELOG (P0├ö├ç├┤P5 Tips) ├ö├ç├Â Seven zieht bei Gelegenheit nach

---

## Geschlossen (Verweis)

Working-Karten und Tip-MikroblÔö£├écke: `CHANGELOG.md` + `CARD_TRACKER.md`. Nicht zurÔö£ÔòØck nach HANDOFF.

---

## Pflege-Regel

Nach Tip: kurze Zeile unter **Zuletzt** oder **Jetzt aktiv** + `CHANGELOG.md`. Keine Tip-Novellen. Keine ├ö├ç├ùAktiv├ö├ç┬ú-BlÔö£├écke fÔö£ÔòØr bereits `working` Karten.
