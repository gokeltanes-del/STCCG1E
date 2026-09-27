# HANDOFF — STCCG 1E

**Owner:** Captain · **Stand:** 2026-09-27 · **Ort:** Josef `C:\Dev\StarTrekCCG\StarTrekCCG`

Nur aktueller Brückenstand. Historie: `CHANGELOG.md`. Status: `CARD_TRACKER.md` (Jadzia), `FEATURES.md` (Seven), Coverage.
Archiv der aufgeblähten Vorgängerdatei: `GROK_TEMP/HANDOFF_ARCHIVE_*.md`.

---

## Jetzt aktiv — Multiplayer / Network-Modus

**Status:** Phase 2 Lobby/UI getippt — Host / Join / Localhost (`NetworkLobbyWindow`) an `NetServer`/`NetClient`. Tip-Hash `PHASE2_TIP_PENDING`.
**Ziel:** LAN, Internet (Direct IP / VPN), 2 Instanzen auf einem PC (Localhost).

### Architektur
- Client-Server: Host = P1 + autoritative Wahrheit; Gast = P2.
- TCP + JSON, kein externes Netzwerk-Framework.
- Nutzen: `GameAction`, `LegalMoves`, `EngineAuthority`, `GameSave`-JSON; Mode-Stub bereits in der UI.

### Roadmap
1. **Phase 1 — Transport:** `NetMessage`, `NetServer`, `NetClient` unter `StarTrekCCG/Network/`.
2. **Phase 2 — Lobby/UI:** Host / Join / Localhost. → Tip `PHASE2_TIP_PENDING`.
3. **Phase 3 — Sync:** `GameAction` → Host `EngineAuthority` → maskierter State an Gast.
4. **Phase 4 — Dialoge/Timing:** `ChoiceRequest` / `ChoiceResponse`.
5. **Phase 5 — Härtung:** Disconnect, Reconnect, Abbruch.

**Code-Stand (Data 2026-09-27):** Phase 2 Lobby getippt; Tip-Hash `PHASE2_TIP_PENDING`. Kein Game-Sync. Working tree bleibt dirty (fremde Änderungen unberührt). Kein Push durch Bots.

---

## Zuletzt (kurz)

- Network Phase 2 Lobby getippt: `NetworkLobbyWindow` + ModeNetwork-Anbindung. Tip-Hash `PHASE2_TIP_PENDING`.
- Network Phase 1 Scaffold getippt: `StarTrekCCG/Network/` (NetMessage/NetServer/NetClient). Tip-Hash `027f993`.
- EXTRACT P2–P5 und viele Premiere-Tips: `CHANGELOG.md` (`EXTRACT_REST.md` fehlt lokal unter artifacts).
- Tracker bleibt aktiv (Jadzia). Behauptete Löschung vom 26.09. war falsch — Datei und Docs-Verweise existieren weiter.

---

## Offen / Park

### Karten (Tracker, Jadzia 2026-09-27)
- Premiere: ~108 `working` / 6 `partial` / 249 `unknown`.
- Partials u. a.: *Vulcan Mindmeld*, *Crystalline Entity*, *Iconian Computer Weapon*, *Alien Probe*, *Escape Pod*, *Q* (Continuum-Park). CHANGELOG/alter HANDOFF hatten für die ersten drei „Pepsch grün“ — Tracker noch `partial`, bis Pepsch klar abnimmt.

### Park / Smoke (Seven + ONLINE_WORKFLOW)
- Continuum / Q
- Plays-on / Plays-as F3-Smoke
- AI Freundes-Report Beaming; ETA `21b2d52` Retest; Artifact-Y Load intermittent
- FEATURES Smoke offen: Beaming 7.1.1, Response-Window Hotseat, Occupancy Badge
- FEATURES-Zeilen Extract P0-D1/E1/S1 sind veraltet vs. CHANGELOG (P0–P5 Tips) — Seven zieht bei Gelegenheit nach

---

## Geschlossen (Verweis)

Working-Karten und Tip-Mikroblöcke: `CHANGELOG.md` + `CARD_TRACKER.md`. Nicht zurück nach HANDOFF.

---

## Pflege-Regel

Nach Tip: kurze Zeile unter **Zuletzt** oder **Jetzt aktiv** + `CHANGELOG.md`. Keine Tip-Novellen. Keine „Aktiv“-Blöcke für bereits `working` Karten.
