# HANDOFF â€” STCCG 1E

**Stand:** 2026-09-28 Â· **Ort:** Josef `C:\Dev\StarTrekCCG\StarTrekCCG` Â· **GitHub `master`:** `8083785`

Nur aktueller BrÃ¼ckenstand. Historie: `CHANGELOG.md`. Status: `CARD_TRACKER.md`, `FEATURES.md`, Coverage.

---

## Jetzt aktiv â€” Multiplayer / Network-Modus

**Status:** AttemptMission Guest→Host Apply getippt (`1194d42` merge). Dual-EXE Smoke HOLD. Phase-5 weiterhin offen. Spaceline Insert `8083785` Basis.
**Ziel:** LAN, Internet (Direct IP / VPN), 2 Instanzen auf einem PC (Localhost).

### Architektur
- Client-Server: Host = P1 + autoritative Wahrheit; Gast = P2.
- TCP + JSON in `StarTrekCCG/Network/`.
- Nutzen: `GameAction`, `LegalMoves`, `EngineAuthority`, `GameSave`-JSON.

### Roadmap
1. Phase 1 Transport: `NetMessage`, `NetServer`, `NetClient` â€” getippt.
2. Phase 2 Lobby/UI: Host / Join / Localhost â€” getippt `2aa9790`.
3. Phase 3 Sync: GameAction â†’ Host EngineAuthority â†’ maskierter State â€” getippt `df904b4`.
4. Phase 4 Dialoge/Timing: ChoiceRequest / ChoiceResponse â€” getippt `e102b47`.
5. Phase 5 HÃ¤rtung: Disconnect, Reconnect, Abbruch â€” offen.

### Letzte Tips auf `master` (2026-09-28)
- Spaceline Insert: Q-Net/Gaps eigene `_spacelineOrder`-Spalten (Mission | Span | Mission). Snapshot `ColumnInstanceIds`. `InsertSpanColumn` / `EnsureSpanColumnsInOrder`. Tip `8083785`.
- PlayCard nur aus Hand / frischem Seed; Host-Snapshot MissionIds + Spans. Tip `b4aca0e`.
- Dual-EXE Spaceline Hostâ†”Guest Capture/Apply. Tip `563d3eb`.
- Span overlay Option B (Ã¤lter, durch `8083785` Ã¼berholt fÃ¼r Order). Tip `2d7acc7`.

---

## Zuletzt (kurz)
- AttemptMission: Host-Spiegel eines Guest-Reveals geht zu, wenn der Guest bestaetigt (`ForceHideWatcherMirror` auf der ChoiceResponse und im Wait-finally) und noch einmal am Attempt-Ende / vor dem Broadcast. Guest-Spiegel schliesst weiter (09b6747). Mission solved auf beiden, OK nur beim Loeser. Scow/Borg Ship: ein Token auf der Seite von `EncounteredBy`, beide Fenster; kein Table-Schatten. Hotseat-Token bleibt unter der Mission. Kein Seed-Fly-in. Detail: revealed Dilemma noch unter der Mission + revealed Artifact noch nicht erworben; unrevealed gegnerische Seeds maskiert. **Weiter nur auf dem Host:** PickOpp, Alien Parasites wenn P1 entscheidet, The Devil fuer P1. TTP-Schiffsklick ohne Kanal (Schiff bleibt). **HOLD Dual-EXE.**
- Span overlay subsystem (Option B): _spacelineOrder mission-only; PaintSpans Gap-Mid Endpoints; Capture Spaceline ohne Spans; BoardStore aus AE-Paar; Seed/Index mission-only; BuildSpacelineDisplayOrder weg. Tip-Hash 2d7acc7. Pipeline 5b043b0/ab2e67a/a9af295 erhalten. **HOLD.**
- Q-Net second span no Extra-Width stack + SWB Face: Relayout missions-only columns; spans gap-mid Pin; AsMissionEndpointBorder; SWB handâ†’Face strip. Tip-Hash 5b043b0. Pipeline fef7075/ab2e67a/a9af295 erhalten. **HOLD.**
- Spaceline span gap = mission InstanceIds + Host dock recover (Q-Net): HostInstanceId/Host2InstanceId; Apply/SpanEndpoints/DisplayOrder per InstanceId; EnsureBoardExtents deferred; PinDockables after span Relayout; Fly-in no Host TargetNorm for spans. Tip-Hash fef7075. Pipeline ab2e67a/a9af295 erhalten. **HOLD.**
- TAK + SWB Choice via AskChoiceForPlayer (Owner-Fenster): RunKidnappers/PickHandCardToDiscard Phase-4 Gate; Host RNG+Discard/Reveal; kein KidnapOverlay PushFrame. Tip-Hash ab2e67a. Pipeline 6acacd4/a9af295 erhalten. **HOLD.**
- Spaceline span fly-in land + gap adjacency (Q-Net Nachzieher): landInst=own InstanceId; IsSpacelineSpanCard Attach-Guard; mission-only ListSameQuadrantGaps; PickAdjacent past spans; Pin gap-X barriers + after RelayoutAll. Tip-Hash 6acacd4. Pipeline a9af295 erhalten. **HOLD.**
- Spaceline Span Y SpacelineY-centered (Q-Net Nachzieher): IsSpacelineRowCard; Apply erzwingt SpacelineY; PinSpacelineSpanCardsY; Ghost Opacity soft-invalidate. Tip-Hash 0443f37. Pipeline a9af295 erhalten. **HOLD.**
- Responsive Detail/Choice-Overlays (Fensterbreite): MaxWidth+Margin statt fester Width/MinWidth; WrapPanel Typ-Buttons/Karten; ScrollViewer H=Disabled. Tip-Hash a9af295. Pipeline 3bc7e49 erhalten. **HOLD.**
- Spaceline Attach Y board-absolute + History RMB-only zoom: Sync Y P1+/P2-; kein Gaps AttachCardToHost; Relayout Owner absolut; History nur RMB Zoom. Tip-Hash 3bc7e49. Pipeline acfb554 erhalten. **HOLD.**

- Action History Kartenreihe + Play-Detail-Popup weg: Strip P1=Gruen/P2=Blau Face; Interrupt-Splash+LRS-OK entfernt; Fly-in bleibt; PlayRevealâ†’History Network-First. Tip-Hash acfb554. Pipeline 5c29f7b erhalten. **HOLD.**

- Host->Guest PlayReveal Face Catalog + Gaps Guest-Drop: Catalog/DTO Face (Fog-Stub-Race); gap:leftInst:rightInst Host Apply ohne Picker. Tip-Hash 5c29f7b. Pipeline 591fba3 erhalten. **HOLD.**

- Guest/P2 Play-Pfad (TAK Persist + Interrupt Fly-in + Face): Net Eventsâ†’TryResolveEventPlay; ClearTable/Relayout Soft-Invalidate; FindLiveCardWithArt+Discard Lookup. Tip-Hash 591fba3. Pipeline f005c71 erhalten. **HOLD.**


- Play Fly-in Event/Interrupt Target + P2 Face: TargetInstanceId Live-Bounds Host/Guest; Face vor Anim; Net PlaysOnHost attach (nicht TABLE). Tip-Hash f005c71. Pipeline df1259e erhalten. **HOLD.**



- Play Fly-in Ziel stale (Outpost Relayout/Neuspield): Land=Host-Facility nach Layout; Stack AbsoluteLeft sync; TargetNorm at Loaded; Invalidate Clear/RelayoutAll. Tip-Hash df1259e. Pipeline 857ab9e erhalten. **HOLD.**

- Play Fly-in Nachzieher Ziel/Perspektive/Doppel (Pepsch-Video): lokale Slot-Bounds (Canvas+TABLE); Controller-Hand; Ghost-InstanceId; End=Slot Size; Face-DB; Tempo ~2.6s. Tip-Hash 857ab9e. Pipeline 034aec2 erhalten. **HOLD.**


- Play Fly-in sichtbar (Nachzieher-Fix): DragLayer + BeginAnimation; Storyboard/Collapsed-Canvas Root Cause; Debug StatusText. Tip-Hash 034aec2. Pipeline b1d5d3e erhalten. **HOLD.**

- Play Fly-in Nachzieher (Handâ†’Mitteâ†’Slot): Gerade Bahn Handâ†’~3.5Ã— Mitte Holdâ†’Slot; DropShadow; kein Dim/Banner/Neon; DTO TargetNorm; Host State vor Reveal. Tip-Hash b1d5d3e. Pipeline 57c1a3e erhalten. **HOLD.**

- Play Fly-in Reveal (Network): Host nach erfolgreichem Hand-Play / Interrupt BeginPlayCardStack â†’ BroadcastPlayReveal + lokale Animation; Guest nur nach PlayReveal-Message (kein lokales Pre-Apply). Overlay non-modal ~1.4s. Tip-Hash 57c1a3e. Board-Sync 8322b68 erhalten. **HOLD.**

- Board-Sync Multiplayer (Fly/Beam/Attack/Interrupt): Guest Action-only; Host TryApplyNet* + sofort Broadcast; kein Phantom-EndTurn-Wipe. Tip-Hash 8322b68. Visibility b2dfaf7 erhalten. **HOLD.**
- Alien Probe Hand-Sync + Occupancy/AT Fog: NetStateMask Probe-Hand + Stack-Occupancy mask; UI FogViewer; 12.12 stub. Prior 5da7c3b erhalten. **HOLD.**

- Guest PlayCard Host-apply (P2 Hand-Wipe Play->Execute): Guest Action-only; Host TryApplyNetPlayCard Ship/Pers/Eq/Event; underInst Note. Tip-Hash 5da7c3b. **HOLD.**

- Skip seed phase (Network Lobby): LobbySkipSeed Propose/Accept/Decline; StartGame.skipSeedPhase; Host AutoCompleteSeed. Tip-Hash 7c040df. **HOLD.**

- Execute-Haenger P2 (Guest Segment): OnSuccessfulHandPlay kein lokales AdvanceSegment auf Guest; Host Broadcast nach Advance; EndTurn-while-Play Remap->EndPhase. Tip-Hash 9abb843. **HOLD.**

- Artifact Seed Glow ALL [P] (Nachzieher): CollectSites seed-before-PlayOn; gold glow; SeedUnderMissionSnapRange; DescribeSeedUnderDeny; ParseLocationIcons harden. Tip-Hash 041e059. Execute unveraendert / Smoke offen. **HOLD.**

- Artifact Seed Targets + P2 End EXECUTE (Network): legal mission snap/glow; Guest EndTurn flip. Tip-Hash 23044e0. **HOLD.**

- Visual Seed-under-Mission Layout (Horga'hn): Relayout pin + orphan scrub. Tip-Hash edc79d7. **HOLD.**

- Network P2 Facility Seed-on-Outpost + Viewer Dock: PlayerForStrip owner; underInst Facility Apply; eigene Facilities unten auf P2-Client. Tip-Hash dabaecc. **HOLD.**

- Network P2 Facility Seed: Guest Target/underInst â†’ Host dockt an Mission (nicht CommitCardToTable/P2 TABLE). Tip-Hash 493fc0b. **HOLD.**

- Network Seed-under-Mission: gleicher Stack Host/Guest; Owner face-up / Opp face-down+Zaehler; underInst+force restore. Tip-Hash 6da1c61. **HOLD.**


- Network P2 Mission-Seed: Guest after:/before: Note â†’ Host engine insert; Slot-Hover glow. Tip-Hash 141c4df.

- Network UI Nachzieher Seed: Hand/Seed face-up LocalPlayer; Mission Glow/Snap; Spaceline Viewer-Orientierung. Tip-Hash 7747bf8.


- Network Guest->Host Seed Authority: Guest Seed nur Action an Host; kein lokales Apply vor Auth; Notify-Gate-Fix. Tip-Hash ccd9aa5.

- Network Host->Guest Seed Sync: EnsureNetworkMode + ActivePlayer-Broadcast/Apply + LocalPlayer-Input-Gate. Tip-Hash f020183.

- Network Viewer=LocalPlayer: Guest sieht P2-Seed/Hand unten; Fremdzonen maskiert; Host-Broadcast nach Seed bleibt. Tip-Hash 1bd513f.

- Network Seed/Mission Sync getippt: Host broadcast nach Seed + SeedCard Guestâ†’Host. Tip-Hash ba2ee6f.
- Network Phase 4 ChoiceRequest/Response getippt: NetChoiceDto + AskChoiceForPlayer + response window routing. Tip-Hash `e102b47`.
- Network Lobby-Flow getippt: Deck pick + Ready + StartGame Ã”Ã¥Ã† Session. Tip-Hash `81455f2`.
- Network Phase 3 Sync getippt: NetActionDto/NetStateMask/NetPlaySession + TableWindow EndPhase/EndTurn. Tip-Hash df904b4.
- Network Phase 2 Lobby getippt: `NetworkLobbyWindow` + ModeNetwork-Anbindung. Tip-Hash `2aa9790`.
- Network Phase 1 Scaffold getippt: `StarTrekCCG/Network/` (NetMessage/NetServer/NetClient). Tip-Hash `027f993`.
- Ã„ltere Extract- und Premiere-Tips: `CHANGELOG.md`. `EXTRACT_REST.md` ist entfernt.
- Tracker bleibt aktiv. Datei und Docs-Verweise existieren weiter.

---

## Offen / Park

### Karten (Tracker 2026-09-27)
- Premiere: ~108 `working` / 6 `partial` / 249 `unknown`.
- Partials u. a.: *Vulcan Mindmeld*, *Crystalline Entity*, *Iconian Computer Weapon*, *Alien Probe*, *Escape Pod*, *Q* (Continuum-Park). Tracker bleibt `partial`, bis Pepsch klar abnimmt.

### Park / Smoke
- Continuum / Q
- Plays-on / Plays-as F3-Smoke
- Freundes-Report Beaming; ETA `21b2d52` Retest; Artifact-Y Load intermittent
- FEATURES Smoke offen: Beaming 7.1.1, Response-Window Hotseat, Occupancy Badge
- FEATURES-Zeilen Extract P0-D1/E1/S1 sind veraltet vs. CHANGELOG â€” bei Gelegenheit nachziehen

---

## Geschlossen (Verweis)

Working-Karten und Tip-MikroblÃ”Ã¶Â£â”œÃ©cke: `CHANGELOG.md` + `CARD_TRACKER.md`. Nicht zurÃ”Ã¶Â£Ã”Ã²Ã˜ck nach HANDOFF.

---

## Pflege-Regel

Nach Tip: kurze Zeile unter **Zuletzt** oder **Jetzt aktiv** + `CHANGELOG.md`. Keine Tip-Novellen. Keine â”œÃ¶â”œÃ§â”œÃ¹Aktivâ”œÃ¶â”œÃ§â”¬Ãº-BlÃ”Ã¶Â£â”œÃ©cke fÃ”Ã¶Â£Ã”Ã²Ã˜r bereits `working` Karten.
