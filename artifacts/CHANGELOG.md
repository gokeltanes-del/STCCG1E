# Changelog

Nur spielbare / engine-relevante Schritte. Keine Chat-Metadaten.

---

## 2026-09-02 (Foundation E1 — ToGameState)

**Engine** — `BoardStore.ToBoardPieces` / `ToGameState(GameStateSeed)`. Capture bevorzugt Store-Board + Crew, UI-Board nur wenn Spaceline leer (Seed). Status (RANGE/Stopped) Overlay. Log `state:` / `capture: source=store|fallback`. [C · Grundlage]

---

## 2026-09-02 (Board Schritt 6 — Aufräumen)

**Board** — tote Namenslisten-Helper weg; Dockables/IM als View markiert. [C]

---

## 2026-09-02 (Board Schritt 5 — Targeting vom Store)

**Target** — Gaps/Q-Net-Paare und Play-on-Hosts aus `BoardStore.Locations` / Occupants. Kevin-Snap bleibt UI. [C · Grundlage]

---

## 2026-09-02 (Fly-eval Log + Engine auf Board)

**7.1.5** — Engine-Fly nutzt Board-Locations. Log `fly-eval` / `fly-mark` mit from/to/hops. [C]

---

## 2026-09-02 (Board Schritt 4 — Beam schreibt Force)

**Board** — Add/Remove Host-Stapel schreibt `Force`. GetCrewOnShip = Board ∪ Stack. Fly-Staffing sieht frisch gebeamte Crew. [C · Grundlage]

---

## 2026-09-02 (Board Schritt 3 — Fly liest Board)

**7.1.5** — Fly-Highlight + RANGE aus `BoardStore.Locations` (Gaps-Span, Q-Net-Kante). Apply/Relayout unverändert. [C · Grundlage]

---

## 2026-09-02 (Board Sync Gaps/Q-Net)

**Board** — Sync liest Gaps/Q-Net aus AttachedEvent (Host/Host2), nicht nur `_spacelineOrder`. [C]

---

## 2026-09-02 (Board Schritt 2 — Sync)

**Board** — SyncBoardFromTable nach Seed/Fly/Beam + Dev Dump Board. Gaps = Location, Q-Net = Barriere. UI bleibt Quelle. [C · Grundlage]

---

## 2026-09-02 (Board Schritt 1 — leere Typen)

**Board** — `Game/Board/`: Spaceline, Location, Occupant, Force, *Instance, BoardStore.Wrap. Dev-Menü Dump Board. Kein Sync, kein Fly. [C · Grundlage]

---

## 2026-09-02 (Board Schritt 0 — File-Logger)

**Debug** — `Services/DebugLog.cs`: Session-Datei `Data/Logs/stccg-….txt`. ActionLog schreibt mit. Kanäle Play/Move/Beam/Target/Board/Layout/Engine/Save. CheckTrace.Cmp nicht in die Datei. [C · Grundlage]

---

## 2026-09-02 (Board-Modell Plan)

**Docs** — `BOARD_MODEL.md`: Dual-Run Spaceline/Location/Occupant/Force; Schritt 0 File-Logger. Kein Code. [C]

---

## 2026-09-02 (Targeting Step 4 + single-stack snap)

**Peek** — 1 Event auf dem Host: OwningHost + Strip + Snap-Rahmen. [B · nullify]

**Seed / Report** — TargetWhy.Seed / Report in CollectSitesForDrag, Glow über TargetSession. [B]

---

## 2026-09-02 (HasSkill freeze / Gaps click / span X)

**HasSkill** — kein per-Token CheckTrace. [A]

**Gaps-Fly** — Klick auf Landable; GameState.OrderedMissions enthält Span-Namen.

**Span-Anzeige** — keine P2-Rotation; nach Relayout in die Lücke.

---

## 2026-09-01 (Gaps location / IM wrap RANGE)

**Gaps** — Landbare Location Span 4; Fly-Glow + Anker. Q-Net bleibt Barriere, kein Stop. Nach Insert Relayout über ActualWidth. Event nicht mehr auf den zwei Nachbar-Missionen. [A · Plays on spaceline]

**7.10 + WNOHGB** — Kürzester Hop, den das Schiff diese Runde zahlen kann; Wrap nur wenn RANGE reicht, sonst die andere Seite.

---

## 2026-09-01 (Lore NA attack / IM discard visual)

**Lore / 7.4.1** — GetAffiliations achtet auf CurrentAffiliation (Commandeer = NA). Wartime nur nach gelungenem Angriff auf FED. [A]

**IM Arrival** — Mini vom Canvas, dann Discard (kein Stapel oben links). [A · 7.10]

---

## 2026-09-01 (TargetQuery PlayOn / Gaps / IM)

**PlayOn** — Event-Ziele über TargetQuery.CanPlayOn (Lore / Neural / Plasma / Espionage / Schiff / Planet / Mission / Outpost). [B · Plays on]

**Gaps / Q-Net** — Site GapSpan, Glow zwischen den Missionen.

**Incoming Message** — Schiff CanPlayOn; Facility CanImFacility + Glow + Place-Choose.

---

## 2026-09-01 (TargetQuery Nullify)

**Targeting** — Game/TargetQuery.cs: eine Legal-Liste (Sites). Kevin/Devil: Glow → 1s Peek → Snap; Drop auf Schiff ohne Snap = Place-Choose der Events auf diesem Host. [B · nullify]

**Zurückgestellt:** Beam-Vollmodus, Battle-Zielwahl, Response-Stack, Netz/KI.

---

## 2026-09-01 (Lore fly anchor / peek Run)

**Fly after Lore** — FindMissionForDockable: IsMissionCard + Pin nach Relayout, damit Engine HostName hat. [A · 7.1.1]

**Peek** — Parent-Walk verträgt Run (kein Visual). [UI]

---

## 2026-08-31 (Lore battle / Hugh mission / IM dock)

**Lore Battle** — CanInitiateShipAttack akzeptiert Lore-Staffing statt Leader. [A · Lore Returns]

**Hugh** — Snap nur auf die Mission, nicht auf ein Schiff. Kill bleibt alle RB beider Seiten. [A · location]

**Kevin vs Lore Returns** — Nullify gibt das Schiff dem Owner zurück (Seite wechseln), RB self-controlling. [A · 7.8]

**IM Buruk** — Required move dockt ab; Hop-Fail im Log. History 2500 Zeilen / größeres Fenster. [A · 7.10]

---

## 2026-08-31 (Lore / Hugh / IM / Crosis)

**Lore Returns** — Schiff staffed + Fly/Battle ohne Leader-Crew; Rogue Borg Planet→Schiff beamen; Relayout auf Controller-Seite. [A · Lore Returns / 7.8]

**Hugh Location** — Ziel ist die Spaceline-Location (Mission oder Schiff dort), nicht ein einzelner Rogue-Borg-Token. Kill weiter alle RB an der Location. [A · Hugh / Glossary location]

**Crosis Detail** — Interrupt nur einmal in der Leiste (nicht Stack + Attach). [UI]

**Incoming Message Hop** — RANGE-Index = Missions derselben Spaceline; Wrap nur Controller; Schiff nach Authority umsetzen. [A · 7.10]

---

## 2026-08-31 (Probespiel)

**Hugh / Rogue Borg** — Kill räumt Host-Stapel + Token; Discard auf *Owner*-Pile (nicht Hugh-Spieler / Controller). [A · Hugh / Glossary discard pile]

**Kevin vs Static Warp Bubble** — SWB ist kein Treaty und kein [Shield]; Kevin darf nullify. Traveler: Transcendence bleibt der andere Nullifier. [A · Kevin / printed]

**Kevin-Snap im Host-Detail** — Overlay überdeckt das Schiff nicht mehr den Mini-Snap; Rahmen folgt der Mini unter dem Cursor. [UI · Plays on]

**WNOHGB** — Wrap nur für den Controller (eigene TABLE-Spalte), nicht für beide. [A · printed „You may…“ / 7.1.7]

**Incoming Message UI** — Auto-Zug in Execute setzt das Schiff visuell hop-weise auf die nächste Location (RANGE war schon abgezogen). [A · IM / 7.10]

**Beam in den Weltraum** — Kein Away-Team auf Space-Missionen (7.1.1.0.1). Planet-AT unverändert. Staffing-Fehler nennt jetzt leere Crew vs. Affiliation. [C · 7.1.1.0.1]

**Subspace Schism** — im Probespiel bestätigt, von der Offenen-Liste.

---

## 2026-08-31

**Mission OR / xN** — `Diplomacy x5 OR Honor x4` nicht mehr still übersprungen. `+` = UND, `OR` = eine Alternative. Skill-Count exakt (kein StartsWith). [A · 7.2.5]

**Battle-Overlay** — Ship/Personnel-Ergebnis + Return Fire über Reveal/AskChoice, nicht Windows-MessageBox. Destroy erst nach OK, damit Escape-Pod-Response offen bleibt. [UI · 7.4.3 / Escape Pod]

**Espionage (PR 4)** — Nur Mission mit [On]-Icon. Für den Besitzer zählt die Mission zusätzlich als [As]. Discard beim Lösen (war schon da). [A · Espionage / 7.2]

**Incoming Message Auto-Zug** — Beim Wechsel Play→Execute: volle RANGE, hop-weise zur Facility. Fly nur in Execute (darum vorher tot). [A · IM / 7.10]

**WNOHGB RANGE** — Kürzerer Ring-Pfad (nicht nur Ende↔Ende). Gaps-Span aus Text (`span 4`) zählt mit. `HasTableCard` nur TABLE-Spalte, nicht Attached Events. [A · 7.1.7]

**Beam Affiliation** — Personnel nur auf kompatibles Schiff/Facility (Treaty/NA). Equipment frei. Planet-AT frei. [A · 7.1.1 / compatible]

**Data-Pfade** — `GamePaths.DecksRoot` / `SaveGamesRoot` (`Data/Decks`, `Data/SaveGames`). Dialoge starten dort. csproj: Content-Copy für beide Ordner.

**Icons** — `Assets/Icons/Icon_{Token}.png`, Text-Fallback. Staffing-Zeile + Host-Badge Icon-Counts.

---

## 2026-08-30

**Spacedock / Dock** — Schiff-Menü Dock at [Facility] / Undock. Flag `_dockedAt`. Fly erst nach Undock. Spacedock am Outpost: Dock = volle Reparatur. [A · 7.1.4 / Spacedock]

**Yellow / Red Alert** — Yellow Alert verhindert Red Alert (Karte bleibt nicht auf TABLE). Bei Nullify von Red Alert: Download Yellow Alert (Draw / Tent). Personnel-SD unverändert. [A · Yellow Alert / 6.5.3]

**Target snap (C)** — Eine Liste `CollectLegalSnapHosts`: Halo + Snap-Feld + Drop. Interrupts fielen vorher aus `UpdateSnapPreviewFromWindow`. Neue Karten: `PlayOnRules.Parse` oder ein Named Override. [C · Plays on]

**Wormhole** — Zwei Karten Pflicht. Erste nur auf eigenes **exposed** Schiff (`!cloaked`). Zweite nur auf Location (Mission / Time Location), Snap-Halo. Schiff dockt dort, wird gestoppt. Kein TABLE-Drop, keine Detail-Picker. [A · Wormhole / exposed]

---

## 2026-08-29

**Shared unique missions** — Zweite Kopie derselben Unique-Mission (Quick Game, gleiche Decks) bleibt eine Location: unsichtbar, kein eigener Seed-Host. `AllMissionBorders` = nur Spaceline-Primaries. [C · unique and universal]

**Shared Face / 4.2.0.3** — Ein Stapel. Bild + Rotation = Zugspieler (dessen Drucktext). Attempt/Solve über `MissionPrintedFor`. Shared ist your mission und opponent's mission. Kein Mission-II, kein 4.3-Staging.

**Lore Returns staff** — Host-Match `SameHostShip`; Schiff NA + Controller; Fly/Attack mit Rogue Borg ohne Officer. [A · 7.8 / Lore Returns]

**Hugh** — [Bor]-Schiff, Borg-Ship-Dilemma, Rogue Borg. Response bricht Battle; Drop auf RB tötet alle RB der Location; Drop auf Dilemma blockt den nächsten Puls. [A · Hugh / 7.4.1.0.2]

**Mission-Drehung** — Nur Shared-Locations folgen dem Zugspieler.

**Horga'hn leave-play** — Effekt nur solange die Karte auf dem Tisch liegt. `OnCardLeftPlay` bei Discard/OOP/Hand. [B · in play]

**Board extents** — Canvas wächst mit gestapelten Schiffen; Spaceline rutscht nach unten wenn P2-Stapel über Y=0 ginge. Scrollbars Auto, Zoom-Min passt sich der Feldgröße an, RMB-Pan erreicht jede Karte.

**Incoming Message** — Matching-Affiliation-Schiff (beliebiger Besitzer). Controller wählt eigene passende Facility auf **dieser** Spaceline (gleicher Quadrant). Bleibt am Schiff; nur Bewegung dorthin (7.10). Nullify bei Ankunft / keiner Facility. Subspace Interference im Spiel; Amanda nur just-played. [A · IM / 7.10 / 12.10]

**Required-move path** — Kürzester Weg auf derselben Spaceline (Quadrant). WNOHGB-Wrap wenn kürzer. Cytherians-Far-End einmal fest (12.6). Mehrere Required Actions: Spieler wählt Reihenfolge. Conundrum nutzt denselben Pfad. [C · 7.10 / 12.6]

**Energy Vortex** — Zurück auf die Hand, Ersatz-Play erlaubt, **dieselbe Kopie** diesen Zug gesperrt. Andere Karte (auch gleicher Titel, andere Kopie) ok. [A · printed EV]

**Subspace Schism** — Jeder Draw öffnet 10s-Response wenn Schism auf einer Hand. Einmal/Zug. Discard der gezogenen, nächste Karte ohne zweites Fenster. [A · printed Schism]

## 2026-08-27

**Host-Detail** — Schiff: Special Equipment (Holodeck, Tractor Beam) oben bei den Stats; angehängte Events/Dilemmas darunter mit Live-Effekt statt Persist-Enum (`Event: Nutational Shields — SHIELDS +2 (1 ENGINEER aboard)`). `EventRules.FormatHostEffectSummary` / `DilemmaRules.FormatHostEffectSummary`. Personnel: keine doppelte Classification, kein Skill-Dump aus `card.Text`, Icons an die Classification-Stelle. Overlay: Kartenleiste unten mit horizontalem Scroll, mehr Text darüber, Back + Select/Close rechts.

**Kartenwahl** — Keine Ja/Nein-Karte-für-Karte-Schleifen mehr. `PickCardFromList` / `PickBorderFromList` nutzen die Detail-Leiste (horizontal scroll). Umgestellt: Res-Q, Palor Toff, Wormhole, PickOwnShip, Stone of Gol, Thought Maker, Kurlan, Event-Target-Dialog, Dilemma-Picks.

**Subspace Warp Rift** — Fly-by beide Richtungen; Schaden beim Weiterfliegen nach Ankunft im selben Zug. Tetryon analog. Kevin trifft Gaps-Span + Fallback-Galerie. `PickOption` für OR-Textwahlen.

**Nullify / Devil / Rift-Badge** — Kevin/Devil nicht mehr als TABLE-Permanent ohne Ziel spielen. Nullify entfernt Span auf der Spaceline, Owner-Discard, Kevin OOP-Zone. Relayout behält Rotation + DMG-Badge am bewegten Schiff. Action History: Mehrfachauswahl + Ctrl+C.

## 2026-08-28

**Horga'hn** — Flag folgt der Tischkarte (Hand-Play + Acquire). Extra Normal-Play bleibt im Play-Segment; sonst Extra-Draw am EOT aus dem Deck des Controllers. Devil löscht nur den Owner-Flag. [A · 2.3.0.1]

**The Devil / Wind Dancer** — Beim Encounter (vor Filter) Yes/No, wenn The Devil auf der Hand liegt. `EncounterDilemma` als Stack-Kind; CollectDevil sieht Last-Encounter. [A · 6.5.1 / Glossary nullify]

**Stapel-Peek (UI)** — Kevin/Devil 2s über Schiff/Mission/Outpost mit legalem Ziel öffnet Detail; legale Karten gold umrandet, Drop trifft die Mini.

**Warp Core Breach / „May be nullified by SKILL“** — Nullify ist optional (Schiff-Button, auch gestoppt), nicht automatisch am EOT. `NullifyEventInPlay` räumt Host-Stapel + Detail. Zerstörung erst am EOT des *nächsten* Controller-Zugs (Countdown 2 im laufenden Controller-Zug). Plasma Fire derselbe Discard-Pfad. [B · Glossary nullify / 7.10.0.1 / 8.1]

**AskChoice** — OR-Dialoge mit beschrifteten Buttons (z.B. Klingon / Romulan, nicht Yes/No). Timeout = gleichverteilt zufällig + Result-Overlay. `AskPlayer` für P1/P2. Umgestellt: Dual-Affiliation-Report, Juggler, Traveler, Draw-3, Masaka, Q's Tent. `PickOption` nutzt dasselbe.

**PlayOnRules** — „Plays on …“-Parser (Ship/Outpost/Facility/Mission/Crew/Table + Own/Exposed/Occupied/Cloaked). Interrupt-Drop nutzt Spec; Schiff ≠ Outpost. Asteroid Sanctuary nur eigenes uncloaked Schiff.

**Fed Battle / Devil / LegalMoves** — Federation initiiert Battle nur vs Borg (NA/Kli im selben Force hebt das nicht); Wartime bleibt Ausnahme. The Devil = `nullify-inplay`. Off-turn-Interrupts erscheinen in LegalMoves. [C · §7.4.1 / 6.5.1]

**Prozess** — `RULES.md` = Fix-Protokoll Klasse A/B/C + Lookup Glossary/Errata. `RULES_CHECKLIST.md` = Compendium 2.7.4 auf x.x.x.

---

## 2026-08-26

**LegalMoves** — `Collect(player)` nur dieser Sitz; Stack nur `ResponsePlayer`; `CollectBoth` für Netz/KI; Beam-Vorschläge von Schiff, Facility und Mission.

**Dual-Affiliation (6.3.3)** — `Card.CurrentAffiliation`, `DualAffiliationRules`, Mode-Skills (Rakal/DeSeve), Report wählt kompatiblen Mode, Switch zwischen Actions (nicht im Attempt, nicht inkompatibel an Bord).

**AU Apply** — Yellow Alert (kill/prevent Red Alert, CUNN+1), Baryon RANGE−2 + SOT-Nullify, Klim kein EOT-Draw, Thermal vs Firestorm/Thought Fire/Plasma, Captain's Log / Lower Decks, Particle Scatter kein Planet-Beam, Intruder FF Rogue Borg &lt;3, Wartime nach Fed-Angriff. Interrupts: Kevin Convergence, Countermanda, Destroy Scow, Senior Staff Meeting.

**Dil Apply** — Edo Probe Lock oder −10 am Zugende; Conundrum chase; Frame of Mind 3-3-3 + 2 Skills, Cure 3 Empathy.

**PR Apply** — Neural Servo Control bis EOT; Distortion Unstop/RANGE/Away-Team; Tachyon + Cloak-Button; Anti-Time Shuffle beider Spieler; Sanctuary EOT-Discard.

**Katalogtexte** — PR Events/Interrupts/Artifacts an Drucktext; Kevin OOP; Auto-Destruct CD=1, Groupie/Temporal Rift CD=2; Life-form Scan = Gegnerhand.

**Engine-Fundament** — `GameState` / `GameAction` / `EngineAuthority` / `EffectRegistry` / `CardEffectMap` · `SeedRules` (Cryo Space, Neutral Outpost NA, 3 AU) · `TurnExpiry` · Download/HA-Flip-Verben · LegalMoves Seed · Until-EOT nur finishing player · Stopped im Snapshot.

---

## 2026-08-21–25

Opponent-Pile-Inspector · Mission Owner-Orientation / asymmetric reqs · Crew-Interrupt-Ziele · Rogue Borg + Crosis + Lore Returns · TurnScope · Skill-Parser `Diplomacy x 2`.

---

## 2026-08-20

`.stsave` JSON · Event-Targets / Gaps / Q-Net · TABLE volle rechte Spalte · Red Alert 5er-Play · Kidnappers · Host-Events bleiben am Ziel.

---

## 2026-08-19

Deck Builder EN, Inline-Move, `DeckPlacementRules`, Skill-Multifilter.

---

## 2026-08-18

Ship Battle + Rotation Damage · Dilemma→Stopped · Repair · Equipment/Modifier · Treaties · Interrupt ActionStack · Premiere Dil/Art/Event/Interrupt-Kataloge · Card Reveal · Action History.

---

## 2026-08-16–17

Phase-2-Tisch: Seed-Phasen, Hotseat feste Spaceline, Report/Staff/RANGE/Beam, Mission Attempt, Hybrid-Orders-UI.

---

## 2026-08-15

Phase 0 Modelle + JSON-Loader · Phase 1 Deck Builder `.stdeck` · Lackey-Split in Set-Ordner.
