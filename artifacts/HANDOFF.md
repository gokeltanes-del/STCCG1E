# HANDOFF ÔÇö STCCG 1E

**Owner:** Captain ┬À **Stand:** 2026-09-27 ┬À **Ort:** Josef `C:\Dev\StarTrekCCG\StarTrekCCG`

Nur aktueller Br├╝ckenstand. Historie: `CHANGELOG.md`. Status: `CARD_TRACKER.md` (Jadzia), `FEATURES.md` (Seven), Coverage.
Archiv der aufgebl├ñhten Vorg├ñngerdatei: `GROK_TEMP/HANDOFF_ARCHIVE_*.md`.

---

## Jetzt aktiv ÔÇö Multiplayer / Network-Modus

**Status:** Lobby-Flow getippt (Deck/Ready/StartGame). **netztauglich.** Tip-Hash `81455f2`. Nächster Tip: Phase 4 Choice oder Pepsch-Go.
**Ziel:** LAN, Internet (Direct IP / VPN), 2 Instanzen auf einem PC (Localhost).

### Architektur
- Client-Server: Host = P1 + autoritative Wahrheit; Gast = P2.
- TCP + JSON, kein externes Netzwerk-Framework.
- Nutzen: `GameAction`, `LegalMoves`, `EngineAuthority`, `GameSave`-JSON; Mode-Stub bereits in der UI.

### Roadmap
1. **Phase 1 ÔÇö Transport:** `NetMessage`, `NetServer`, `NetClient` unter `StarTrekCCG/Network/`.
2. **Phase 2 ÔÇö Lobby/UI:** Host / Join / Localhost. ÔåÆ Tip `2aa9790`.
3. **Phase 3 ÔÇö Sync:** GameAction ÔåÆ Host EngineAuthority ÔåÆ maskierter State. ÔåÆ Tip df904b4.
4. **Phase 4 ÔÇö Dialoge/Timing:** `ChoiceRequest` / `ChoiceResponse`.
5. **Phase 5 ÔÇö H├ñrtung:** Disconnect, Reconnect, Abbruch.

**Code-Stand (Data 2026-09-27):** Lobby-Flow getippt; Tip-Hash `81455f2`. Phase 1–3 + Lobby-Raum. Working tree dirty (fremde Änderungen unberührt). Kein Push durch Bots.

---

## Zuletzt (kurz)

- Network Lobby-Flow getippt: Deck pick + Ready + StartGame → Session. Tip-Hash `81455f2`.
- Network Phase 3 Sync getippt: NetActionDto/NetStateMask/NetPlaySession + TableWindow EndPhase/EndTurn. Tip-Hash df904b4.
- Network Phase 2 Lobby getippt: `NetworkLobbyWindow` + ModeNetwork-Anbindung. Tip-Hash `2aa9790`.
- Network Phase 1 Scaffold getippt: `StarTrekCCG/Network/` (NetMessage/NetServer/NetClient). Tip-Hash `027f993`.
- EXTRACT P2ÔÇôP5 und viele Premiere-Tips: `CHANGELOG.md` (`EXTRACT_REST.md` fehlt lokal unter artifacts).
- Tracker bleibt aktiv (Jadzia). Behauptete L├Âschung vom 26.09. war falsch ÔÇö Datei und Docs-Verweise existieren weiter.

---

## Offen / Park

### Karten (Tracker, Jadzia 2026-09-27)
- Premiere: ~108 `working` / 6 `partial` / 249 `unknown`.
- Partials u. a.: *Vulcan Mindmeld*, *Crystalline Entity*, *Iconian Computer Weapon*, *Alien Probe*, *Escape Pod*, *Q* (Continuum-Park). CHANGELOG/alter HANDOFF hatten f├╝r die ersten drei ÔÇ×Pepsch gr├╝nÔÇ£ ÔÇö Tracker noch `partial`, bis Pepsch klar abnimmt.

### Park / Smoke (Seven + ONLINE_WORKFLOW)
- Continuum / Q
- Plays-on / Plays-as F3-Smoke
- AI Freundes-Report Beaming; ETA `21b2d52` Retest; Artifact-Y Load intermittent
- FEATURES Smoke offen: Beaming 7.1.1, Response-Window Hotseat, Occupancy Badge
- FEATURES-Zeilen Extract P0-D1/E1/S1 sind veraltet vs. CHANGELOG (P0ÔÇôP5 Tips) ÔÇö Seven zieht bei Gelegenheit nach

---

## Geschlossen (Verweis)

Working-Karten und Tip-Mikrobl├Âcke: `CHANGELOG.md` + `CARD_TRACKER.md`. Nicht zur├╝ck nach HANDOFF.

---

## Pflege-Regel

Nach Tip: kurze Zeile unter **Zuletzt** oder **Jetzt aktiv** + `CHANGELOG.md`. Keine Tip-Novellen. Keine ÔÇ×AktivÔÇ£-Bl├Âcke f├╝r bereits `working` Karten.