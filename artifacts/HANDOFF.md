# HANDOFF ├ö├ç├Â STCCG 1E

**Owner:** Captain Ôö¼├Ç **Stand:** 2026-09-27 Ôö¼├Ç **Ort:** Josef `C:\Dev\StarTrekCCG\StarTrekCCG`

Nur aktueller BrÔö£ÔòØckenstand. Historie: `CHANGELOG.md`. Status: `CARD_TRACKER.md` (Jadzia), `FEATURES.md` (Seven), Coverage.
Archiv der aufgeblÔö£├▒hten VorgÔö£├▒ngerdatei: `GROK_TEMP/HANDOFF_ARCHIVE_*.md`.

---

## Jetzt aktiv ├ö├ç├Â Multiplayer / Network-Modus

**Status:** Network Host->Guest Seed Sync getippt (ActivePlayer+Gate). **netztauglich.** Tip-Hash f020183. Naechster Tip: Phase 5 Disconnect oder Pepsch-Retest.
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

**Code-Stand (Data 2026-09-27):** Host->Guest Seed Sync getippt; Tip-Hash f020183. Viewer=LocalPlayer 1bd513f. Seed Sync ba2ee6f. Working tree dirty (fremde Aenderungen unberuehrt). Kein Push durch Bots.

---

## Zuletzt (kurz)

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
