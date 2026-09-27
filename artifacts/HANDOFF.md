# HANDOFF ├ö├ç├Â STCCG 1E

**Owner:** Captain Ôö¼├Ç **Stand:** 2026-09-27 Ôö¼├Ç **Ort:** Josef `C:\Dev\StarTrekCCG\StarTrekCCG`

Nur aktueller BrÔö£ÔòØckenstand. Historie: `CHANGELOG.md`. Status: `CARD_TRACKER.md` (Jadzia), `FEATURES.md` (Seven), Coverage.
Archiv der aufgeblÔö£├▒hten VorgÔö£├▒ngerdatei: `GROK_TEMP/HANDOFF_ARCHIVE_*.md`.

---

## Jetzt aktiv ├ö├ç├Â Multiplayer / Network-Modus

**Status:** Artifact-Seed-Glow Nachzieher getippt (alle legalen [P]). **netztauglich.** Tip-Hash 041e059. Execute unveraendert / Smoke noch offen. Naechster: Pepsch Smoke Glow+Drop+Limit-Status+Execute. **HOLD.**
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

**Code-Stand (Data 2026-09-27):** Artifact Glow ALL [P] 041e059; prior seed+EXECUTE 23044e0; Seed-under Layout edc79d7. Kein Push durch Bots. **HOLD.**

---

## Zuletzt (kurz)

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
