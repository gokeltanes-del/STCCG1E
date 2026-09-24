# Compendium 2.7.4 — Checkliste

Quelle: `rules/Compendium_Rulebook.pdf` (2.7.4, August 2026, 322 Seiten). Ablauf: `IMPLEMENT.md`.
Stand: 2026-09-19 (TOC-Rebuild). Scope: Premiere; ➖ = nicht jetzt / Format / Referenz.

✅ spielbar · 🟡 Lücken · ❌ fehlt · ➖ später

Bei einem Fix nur die **betroffene** Zeile ändern und die Code-Datei nennen. **Kein ✅ ohne Pepsch-green / HANDOFF-Beleg.**

Begleit-Tracker (gleiche Tabellen-Logik): `FEATURES.md` · `GLOSSARY_COVERAGE.md` · `APPENDIX_A_COVERAGE.md`.

### Inventar (PDF-Body)

| Teil | Inhalt |
|------|--------|
| Kapitel 1–14 | Rulebook p. 1–103; nummerierte §§ inkl. Sidebars `x.x.0.y` |
| 13 Icon Legend | p. 96–98, **keine** `(13.x)`-Nummern im PDF |
| Glossary | p. 104–224 → `GLOSSARY_COVERAGE.md` |
| Temporary Rulings | p. 225 |
| Appendix A Errata | p. 226–321 → `APPENDIX_A_COVERAGE.md` |
| Appendix B Change Log | p. 322 (verweist auf Recent Rulings / Archive) |

Nummerierte §§ in dieser Datei: **349** (PDF-Body; False-Positives wie `(8)` als Kapitelverweis gestrichen).
Status-Zählung: ✅ 10 · 🟡 193 · ❌ 48 · ➖ 98.
Titel: PDF 2.7.4 + Abgleich CC-Compendium-HTML (2.6.2), wo die §-Nummer gleich blieb; bei Shift (z. B. 4.4.1–4.4.4) gilt das PDF.
PDF hat **kein §12.17** (HTML 2.6.2 `NEXT STEPS`); Sprung 12.16 → 12.18 ist buchstäblich.

---

## 1 Einleitung

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 1.1 | ABOUT THIS GAME | ➖ | PDF p.2 |
| 1.1.0.2 | Copyright Notice: Printing cards | ➖ | wie §1.1 · PDF p.2 |
| 1.1.0.1 | Tip: First vs. Second Edition | ➖ | Tip-Sidebar (Referenz) · PDF p.2 |
| 1.2 | ABOUT THIS RULEBOOK | ➖ | Wir spielen Traditional/Open-Nähe, kein OP-Format · PDF p.2 |
| 1.2.0.3 | The Borg: Special Rules (Sidebar-Hinweis) | ➖ | wie §1.2 · PDF p.3 |
| 1.1.0.-1 | Tip: This Is The Compendium | ➖ | Tip-Sidebar (Referenz) · PDF p.3 |
| 1.2.0.2 | Formats: Modern, Traditional, and Open Rules | ➖ | Open / Traditional / OP-Format · PDF p.3 |
| 1.2.0.1 | Tip: Try the Basic Rulebook | ➖ | Tip-Sidebar (Referenz) · PDF p.3 |

## 2 Kartentypen

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 2.0.1 | Clarifications: Double-Sided Cards | ❌ | TOC-Zeile neu; Status unbekannt (nicht grün markiert) · PDF p.4 |
| 2.1 | MISSIONS | 🟡 | Spaceline, Quadrant/Region, [P]/[S], Solve · PDF p.4 |
| 2.1.0.1 | Clarifications: Dual-Icon Missions | 🟡 | Icons gelesen · PDF p.4 |
| 2.1.0.3 | Clarifications: Two Kinds of Attemptability Icon | 🟡 | wie §2.1 (nicht feiner auditiert) · PDF p.5 |
| 2.1.0.2 | Tip: Homeworlds | ➖ | Tip-Sidebar (Referenz) · PDF p.5 |
| 2.2 | DILEMMAS | 🟡 | `DilemmaRules` PR-Katalog; AU Teil-Apply · PDF p.5 |
| 2.3 | ARTIFACTS | 🟡 | Acquire + Hand-Play PR; Horga'hn Extra-Play/Draw 2026-08-28 · PDF p.6 |
| 2.3.0.1 | Clarifications: Earning & Using Artifacts | 🟡 | Acquire bei Solve; Horga'hn auch Hand→TABLE · PDF p.6 |
| 2.4 | EVENTS, INCIDENTS, & OBJECTIVES | 🟡 | Events PR+AU Teil; Incidents/Objectives ➖; WCB/Plasma Skill-Nullify 2026-08-28 · PDF p.6 |
| 2.4.0.2 | Borg Rule: Objectives, Not Missions | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.7 |
| 2.4.0.1 | Tip: What's the Difference? | ➖ | Tip-Sidebar (Referenz) · PDF p.7 |
| 2.5 | DOORWAYS | 🟡 | Seed + Side-Deck-Cover · PDF p.7 |
| 2.6 | INTERRUPTS | 🟡 | Katalog + Stack + off-turn; Devil vs Encounter-Wind-Dancer · PDF p.7 |
| 2.7 | PERSONNEL | 🟡 | Report, Skills-Parser, Dual-Mode · PDF p.8 |
| 2.8 | EQUIPMENT | 🟡 | `ModifierRules` Premiere · PDF p.8 |
| 2.9 | SHIPS | 🟡 | Dock, Staff, RANGE, Battle, Cloak · PDF p.9 |
| 2.9.0.1 | Clarifications: List of Special Equipment | 🟡 | Detail-Text; Wirkung lückenhaft · PDF p.9 |
| 2.10 | FACILITIES | 🟡 | Seed Planet+Affil; Report-Host; ≠ Schiff · PDF p.9 |
| 2.10.0.1 | Tip: Facility Types Aren't Card Types | ✅ | Outpost/HQ als Facility-Kind (Scope-Hinweis: Tip-Sidebar (Referenz)) · PDF p.10 |
| 2.11 | SITES | ➖ | Sites / Nor — Premiere-Scope · PDF p.10 |
| 2.11.0.1 | Clarifications: Facilities Contain Their Sites | ➖ | Sites / Nor — Premiere-Scope · PDF p.10 |
| 2.12 | TIME LOCATIONS | ➖ | Time Locations / Time Travel — Premiere-Scope · PDF p.11 |
| 2.13 | TACTICS | ➖ | Tactics / Battle Bridge — Premiere-Scope (Rotation-Damage ist 7.5.1.2) · PDF p.11 |
| 2.14 | TRIBBLES & TROUBLES | ➖ | Tribbles-Stack — Premiere-Scope · PDF p.12 |
| 2.14.0.1 | Tip: Tribbles CCG | ➖ | Tip-Sidebar (Referenz) · PDF p.12 |
| 2.15 | Q-ICON CARDS | 🟡 | Continuum-Stapel; Q-Doorway dünn · PDF p.12 |
| 2.16 | BANNED CARDS | ➖ | Ban-Liste — privater Client · PDF p.13 |

**2 extra (im Buch verteilt, bei uns relevant)**

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 2 / 6.3.1 | Uniqueness / Persona | 🟡 | Name + Lore-Heuristik; BoardStore Unique-by-Owner (Welle 2) |
| 2 / 6.2 | Hidden Agenda | 🟡 | Flip via Authority |
| 2 / 4.0 | AU-Icon + enabling Doorway | ❌ | Checklist-Altbestand |

## 3 Deck

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 3.0.1 | Borg Rule: Non-Borg Borg Cards in Your Deck | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.14 |
| 3.1 | SEED DECK | 🟡 | `.stdeck` v2; 30/30/30 nicht erzwungen · PDF p.14 |
| 3.1.0.2 | Tip: A Typical Seed Deck | ➖ | Tip-Sidebar (Referenz) · PDF p.14 |
| 3.1.0.1 | Clarifications: Unique Seed Cards | 🟡 | wie §3.1 (nicht feiner auditiert) · PDF p.14 |
| 3.1.0.3 | Exception: Dilemma Seed Limit / Open Rules: No Dilemma Seed Limit | 🟡 | wie §3.1 (nicht feiner auditiert) · PDF p.14 |
| 3.1.1 | MISSION PILE | ✅ | PDF p.14 |
| 3.1.2 | SITE PILE | 🟡 | Zone da, nicht gespielt (Scope-Hinweis: Sites / Nor — Premiere-Scope) · PDF p.14 |
| 3.1.0.4 | Tip: Sites Without A Nor | ➖ | Tip-Sidebar (Referenz) · PDF p.15 |
| 3.2 | DRAW DECK | ✅ | PDF p.15 |
| 3.2.0.1 | Tip: There Is No Card Limit | ➖ | Tip-Sidebar (Referenz) · PDF p.15 |
| 3.3 | SIDE DECKS | 🟡 | Tent/BB/QC/Site/Tribble Zonen; Typ-Limits lückenhaft · PDF p.15 |
| 3.3.0.2 | Clarifications: Side Deck Draws & Plays | 🟡 | wie §3.3 (nicht feiner auditiert) · PDF p.15 |
| 3.3.0.1 | Tip: Q's Tent & Dyson Sphere as Download Warehouses | ➖ | Tip-Sidebar (Referenz) · PDF p.15 |

## 4 Seed-Phasen

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 4.0.1 | 4: THE SEED PHASES (Kapitel) | ❌ | TOC-Zeile neu; Status unbekannt (nicht grün markiert) · PDF p.16 |
| 4.0.2 | Clarification: Seed Phase Actions | ❌ | TOC-Zeile neu; Status unbekannt (nicht grün markiert) · PDF p.16 |
| 4.1 | DOORWAY PHASE | ✅ | sequential · PDF p.17 |
| 4.1.0.1 | Open Rules: Alternating Seeds | ➖ | Open / Traditional / OP-Format · PDF p.17 |
| 4.2 | MISSION PHASE | 🟡 | Quadrant/Region/Slots; Shared = 1 Location, Face = Zugspieler · PDF p.17 |
| 4.2.0.1 | Clarifications: Ambiguities — Regions | 🟡 | wie §4.2 (nicht feiner auditiert) · PDF p.17 |
| 4.2.0.4 | Tip: "Spaceline End" Isn't a New Location | ➖ | Tip-Sidebar (Referenz) · PDF p.18 |
| 4.2.0.3 | Clarifications: Shared Missions are Both Players' | 🟡 | your + opponent's; Drucktext der eigenen Kopie · PDF p.18 |
| 4.2.0.2 | Clarifications: Built-In Cards (Mission II) | 🟡 | wie §4.2 (nicht feiner auditiert) · PDF p.18 |
| 4.3 | DILEMMA PHASE | 🟡 | Dil+Art gleiche Phase, getrennte Stapel · PDF p.18 |
| 4.3.0.1 | Tip: Hurt Opponent, Help Yourself | ➖ | Tip-Sidebar (Referenz) · PDF p.19 |
| 4.3.0.2 | Tip: Strategic Mis-seeds | ➖ | Tip-Sidebar (Referenz) · PDF p.19 |
| 4.3.0.3 | Open Rules: Alternating, Again | ➖ | Open / Traditional / OP-Format · PDF p.19 |
| 4.4 | FACILITY PHASE | 🟡 | PDF p.19 |
| 4.4.1 | SEEDING AND PLAYING FACILITIES | 🟡 | PDF p.19 |
| 4.4.2 | WHERE CAN OUTPOSTS SEED AND PLAY? | 🟡 | Outpost nur Planet; Neutral=NA · PDF p.20 |
| 4.4.2.0.2 | Tip: "Any" on Outposts | ➖ | Tip-Sidebar (Referenz) · PDF p.20 |
| 4.4.2.0.3 | Tip: "Outposts" | ➖ | Tip-Sidebar (Referenz) · PDF p.20 |
| 4.4.2.0.4 | Tip: Be Careful with Homeworlds | ➖ | Tip-Sidebar (Referenz) · PDF p.20 |
| 4.4.2.0.1 | Clarifications: "Seed one" | 🟡 | wie §4.4.2 (nicht feiner auditiert) · PDF p.20 |
| 4.4.3 | SEEDING SITES | ➖ | Sites / Nor — Premiere-Scope · PDF p.20 |
| 4.4.4 | STARTING THE GAME | ✅ | Shuffle, Hand 7 · PDF p.20 |

`SeedRules`: Cryo=Space, 3 AU under Cryo.

## 5 The Play Phase

Im PDF **keine** `(5.x)`-Nummern — Kapitel 5 ist der Turn-Rahmen; Valid-Response-Detail steht als Clarification ohne eigene Hauptnummer (p. 22–24).

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 5 | Turn = Play → Execute orders → Draw | ✅ | `GameSession` Segmente |
| 5 | Actions / valid responses (Überblick) | 🟡 | Detail in TimingRules + Glossary *actions*; Response Window CODED, Pepsch-smoke offen |
| 5 | Group actions / just responses / Hidden Agenda as response | 🟡 | PDF p. 23–24 Clarifications: Valid responses; Katalog Amanda/Kevin/Q2/Hugh |

## 6 Karte spielen

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 6.1 | YOUR NORMAL CARD PLAY | 🟡 | 1× / Zug; Interrupts extra · PDF p.25 |
| 6.1.0.1 | Tip: Interrupts and Doorways Don't Use Your Card Play | ➖ | Tip-Sidebar (Referenz) · PDF p.25 |
| 6.1.1 | PLAYING "FOR FREE" | 🟡 | `PlayRules.PlaysForFree` · PDF p.25 |
| 6.1.1.0.1 | Tip: You Need Free Plays | ➖ | Tip-Sidebar (Referenz) · PDF p.25 |
| 6.2 | ENTERING PLAY | 🟡 | PDF p.25 |
| 6.2.0.2 | Clarifications: Not Yet Played | 🟡 | wie §6.2 (nicht feiner auditiert) · PDF p.25 |
| 6.2.0.1 | Clarifications: Cards Played as Costs | 🟡 | wie §6.2 (nicht feiner auditiert) · PDF p.26 |
| 6.2.0.3 | Borg Rule: Counterpart Limit | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.26 |
| 6.2.0.4 | Clarifications: Showing your Hidden Agenda cards | 🟡 | wie §6.2 (nicht feiner auditiert) · PDF p.26 |
| 6.3 | REPORTING FOR DUTY | 🟡 | Facility + Treaty-Mix + NA · PDF p.26 |
| 6.3.0.1 | Tip: Headquarters Aren't Restricted | ➖ | Tip-Sidebar (Referenz) · PDF p.27 |
| 6.3.0.2 | Exception: Non-Aligned compatibility | 🟡 | wie §6.3 (nicht feiner auditiert) · PDF p.27 |
| 6.3.1 | DUPLICATION AND PERSONAS | 🟡 | PDF p.28 |
| 6.3.1.0.1 | Clarifications: Personas and Requirements | 🟡 | wie §6.3.1 (nicht feiner auditiert) · PDF p.28 |
| 6.3.1.0.2 | Tip: Be careful with personas | ➖ | Tip-Sidebar (Referenz) · PDF p.28 |
| 6.3.1.0.3 | Clarifications: Bold Italics is Not Plain Bold | 🟡 | wie §6.3.1 (nicht feiner auditiert) · PDF p.28 |
| 6.3.2 | HOLOGRAPHIC PERSONNEL AND EQUIPMENT | ❌ | PDF p.29 |
| 6.3.2.1 | ACTIVATION AND DEACTIVATION | ❌ | wie §6.3.2 — Feinregel unbekannt · PDF p.29 |
| 6.3.2.0.2 | Clarification: Using Opponent's Holodecks; Captive Holograms | ❌ | wie §6.3.2 — Feinregel unbekannt · PDF p.30 |
| 6.3.2.0.1 | Clarification: Allowing Holograms to "Exist" Elsewhere | ❌ | wie §6.3.2 — Feinregel unbekannt · PDF p.30 |
| 6.3.2.2 | DEATH AND ERASURE | ❌ | wie §6.3.2 — Feinregel unbekannt · PDF p.30 |
| 6.3.2.3 | HOLOGRAPHIC SAFETY PROTOCOLS | ❌ | wie §6.3.2 — Feinregel unbekannt · PDF p.30 |
| 6.3.3 | MULTI-AFFILIATION CARDS | 🟡 | `DualAffiliationRules`; kein separates AT · PDF p.30 |
| 6.3.3.0.1 | Clarifications: Ambiguities — Multi-affiliation cards | 🟡 | wie §6.3.3 (nicht feiner auditiert) · PDF p.31 |
| 6.3.4 | DUAL-PERSONNEL CARDS | ❌ | PDF p.31 |
| 6.3.4.0.4 | Clarifications: Dual-Personnel Couples | ❌ | wie §6.3.4 — Feinregel unbekannt · PDF p.31 |
| 6.3.4.0.3 | Clarifications: Dual-Personnel Attributes and Icons | ❌ | wie §6.3.4 — Feinregel unbekannt · PDF p.31 |
| 6.3.4.0.2 | Clarifications: Dual-Personnel Downloads | ❌ | wie §6.3.4 — Feinregel unbekannt · PDF p.31 |
| 6.3.4.0.1 | Clarifications: Random Selections and Dual-Personnel Cards | ❌ | wie §6.3.4 — Feinregel unbekannt · PDF p.32 |
| 6.3.5 | MIRROR OPPOSITES AND IMPERSONATORS | ➖ | Mirror Universe — Premiere-Scope · PDF p.32 |
| 6.4 | LEAVING PLAY | 🟡 | Discard / OOP; nicht alle Fälle · PDF p.33 |
| 6.4.0.1 | Open Rules: Discard Dilemmas | ➖ | Open / Traditional / OP-Format · PDF p.34 |
| 6.4.0.2 | Borg Rule: Borg Points | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.34 |
| 6.4.0.3 | Clarifications: Other bonus points | 🟡 | wie §6.4 (nicht feiner auditiert) · PDF p.34 |
| 6.5 | OTHER WAYS TO PLAY A CARD | 🟡 | PDF p.34 |
| 6.5.1 | PLAYING "AT ANY TIME" | 🟡 | Interrupts; `CollectOffTurn` seit 2026-08-28 · PDF p.34 |
| 6.5.1.1 | PLAYING A DOORWAY | 🟡 | meist nur eigener Zug · PDF p.35 |
| 6.5.2 | PERSONA REPLACEMENT | ❌ | PDF p.35 |
| 6.5.0.1 | Clarifications: No Clone Swaps | 🟡 | wie §6.5 (nicht feiner auditiert) · PDF p.35 |
| 6.5.3 | DOWNLOADING | 🟡 | Tent + Verb · PDF p.35 |
| 6.5.3.0.10 | Clarifications: Side Deck Downloads | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.35 |
| 6.5.3.0.9 | Clarifications: Discard Pile Downloads | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.35 |
| 6.5.3.0.8 | Clarifications: DownloadingHidden Agenda Hidden Agendas | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.35 |
| 6.5.3.0.7 | Clarifications: Downloading Tactics | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.35 |
| 6.5.3.0.6 | Clarifications: Where Facilities May Download | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.36 |
| 6.5.3.0.5 | Clarifications: Where Personnel May Download | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.36 |
| 6.5.3.0.4 | Clarifications: Required Downloads | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.36 |
| 6.5.3.0.3 | Clarifications: Showing Your Downloads / Open Rules: No Personnel Download Limit | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.36 |
| 6.5.3.0.2 | Clarifications: Invalid Downloads | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.36 |
| 6.5.3.0.1 | Clarifications: Download Timing | 🟡 | wie §6.5.3 (nicht feiner auditiert) · PDF p.36 |
| 6.5.4 | SPECIAL DOWNLOADING | 🟡 | SD-Verb / UI · PDF p.37 |
| 6.5.4.0.2 | Tip: Special Download Suspends Play | ➖ | Tip-Sidebar (Referenz) · PDF p.37 |
| 6.5.4.0.1 | Clarifications: Ambiguities — Special Downloads | 🟡 | wie §6.5.4 (nicht feiner auditiert) · PDF p.37 |

Report an Schiffe (nicht nur Special) noch ❌.

## 7 Execute Orders

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 7.1 | MOVE | 🟡 | PDF p.39 |
| 7.0.1 | Clarifications: Crews, Away Teams, and Movement | ❌ | TOC-Zeile neu; Status unbekannt (nicht grün markiert) · PDF p.39 |
| 7.0.2 | Tip: Separate Crews and Away Teams | ➖ | Tip-Sidebar (Referenz) · PDF p.39 |
| 7.0.3 | Borg Rule: Extraneous Factors are Irrelevant | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.40 |
| 7.1.1 | BEAM | 🟡 | gleiche Location; Host-Affiliation/Treaty (Equipment & Artifacts frei); Planet-AT frei; **kein Beam auf Space-Mission** (7.1.1.0.1, 2026-08-31) · PDF p.40 |
| 7.1.1.0.4 | Clarifications: Compatibility | 🟡 | wie §7.1.1 (nicht feiner auditiert) · PDF p.40 |
| 7.1.1.0.3 | Clarifications: "Unshielded" | 🟡 | wie §7.1.1 (nicht feiner auditiert) · PDF p.40 |
| 7.1.1.0.2 | Clarifications: Card-Activated Transport | 🟡 | wie §7.1.1 (nicht feiner auditiert) · PDF p.40 |
| 7.1.1.0.1 | Clarifications: Transporter Limits | 🟡 | wie §7.1.1 (nicht feiner auditiert) · PDF p.40 |
| 7.1.1.0.5 | Tip: Boarding? Have a Plan! | ➖ | Tip-Sidebar (Referenz) · PDF p.41 |
| 7.1.2 | WALK | ➖ | Sites / Nor — Premiere-Scope · PDF p.41 |
| 7.1.3 | STAFF A SHIP | 🟡 | `MovementRules.IsShipStaffed` · PDF p.41 |
| 7.1.3.0.1 | Clarifications: Ship Staffing | 🟡 | wie §7.1.3 (nicht feiner auditiert) · PDF p.41 |
| 7.1.4 | DOCK & UNDOCK | 🟡 | PDF p.41 |
| 7.1.4.0.4 | Clarification: No Docking at Stations without docking site (u. ä.) | 🟡 | wie §7.1.4 (nicht feiner auditiert) · PDF p.42 |
| 7.1.4.0.3 | Clarification: Crew Need Not Be Compatible To Dock | 🟡 | wie §7.1.4 (nicht feiner auditiert) · PDF p.42 |
| 7.1.4.0.2 | Clarification: No Undocking from Opponent's Facilities | 🟡 | wie §7.1.4 (nicht feiner auditiert) · PDF p.42 |
| 7.1.4.0.1 | Clarification: "Return To Outpost" and Docking | 🟡 | wie §7.1.4 (nicht feiner auditiert) · PDF p.42 |
| 7.1.5 | FLY A STARSHIP | 🟡 | RANGE + Glow aus `BoardStore.Locations` (Gaps-Span, Q-Net-Kante). LegalMoves-Liste noch Namens-Snapshot · PDF p.42 |
| 7.1.5.0.3 | Clarifications: RANGE Boosts in Regions | 🟡 | wie §7.1.5 (nicht feiner auditiert) · PDF p.42 |
| 7.1.5.0.2 | Clarifications: No Default Docking | 🟡 | wie §7.1.5 (nicht feiner auditiert) · PDF p.42 |
| 7.1.5.0.1 | Clarifications: Warp Speed Immunity | 🟡 | wie §7.1.5 (nicht feiner auditiert) · PDF p.43 |
| 7.1.6 | LAND & TAKE OFF | ❌ | PDF p.44 |
| 7.1.6.1 | CARRIED SHIPS | ❌ | wie §7.1.6 — Feinregel unbekannt · PDF p.44 |
| 7.1.6.1.0.1 | Clarifications: Carried Ships | ❌ | wie §7.1.6 — Feinregel unbekannt · PDF p.44 |
| 7.1.7 | MOVE BETWEEN QUADRANTS | 🟡 | WNOHGB: Ring-RANGE nur für **Controller** (eigene TABLE-Spalte); Gaps-Span mitzählen; Wormhole 2 Karten exposed→Location+Stop · PDF p.44 |
| 7.1.7.0.1 | Clarifications: Which Cards Count? / Tip: Inter-Quadrant Strategies | 🟡 | wie §7.1.7 (nicht feiner auditiert) · PDF p.44 |
| 7.1.8 | TIME TRAVEL | ➖ | Time Locations / Time Travel — Premiere-Scope · PDF p.45 |
| 7.1.8.0.2 | Tip: Time Travel Strategies | ➖ | Tip-Sidebar (Referenz) · PDF p.45 |
| 7.1.8.0.1 | Clarifications: "Corresponding" Time Locations | ➖ | Time Locations / Time Travel — Premiere-Scope · PDF p.45 |
| 7.2 | ATTEMPT A MISSION | 🟡 | Pipeline vorhanden · PDF p.45 |
| 7.2.1 | BEGINNING AN ATTEMPT | 🟡 | PDF p.45 |
| 7.2.1.0.1 | Open Rules: Attempt Opponent's Missions | ➖ | Open / Traditional / OP-Format · PDF p.46 |
| 7.2.1.0.5 | Clarification: "Any Crew" / "Any Away Team" | 🟡 | wie §7.2.1 (nicht feiner auditiert) · PDF p.46 |
| 7.2.1.0.3 | Clarification: Meeting Requirements is Mandatory | 🟡 | wie §7.2.1 (nicht feiner auditiert) · PDF p.46 |
| 7.2.1.0.2 | Clarification: Attempting aDual-Icon Mission | 🟡 | wie §7.2.1 (nicht feiner auditiert) · PDF p.46 |
| 7.2.2 | ENCOUNTERING DILEMMAS | 🟡 | Katalog ersetzt Heuristik; 7.2.2.3 Zentrales Cure-System (`DilemmaCureRules`, Bedingungen zuerst, dann Cure auf Attachment/Refresh/Solve); Curable Dilemmas ohne Condition stoppen Team nicht (AttachContinue); Pepsch green: Archer, Alien Abduction, Phased Matter (2026-09-13) · PDF p.46 |
| 7.2.2.0.4 | Clarification: Dilemma Triggers | 🟡 | wie §7.2.2 (nicht feiner auditiert) · PDF p.47 |
| 7.2.2.0.3 | Clarification: Dilemma Targets | 🟡 | wie §7.2.2 (nicht feiner auditiert) · PDF p.47 |
| 7.2.2.0.2 | Borg Rule: Some Dilemmas are Irrelevant | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.47 |
| 7.2.2.0.1 | Clarifications: Reading and Responding to Dilemmas | 🟡 | wie §7.2.2 (nicht feiner auditiert) · PDF p.48 |
| 7.2.2.0.7 | Clarifications: "Combo" Dilemmas | 🟡 | wie §7.2.2 (nicht feiner auditiert) · PDF p.48 |
| 7.2.2.0.6 | Dilemmas | 🟡 | wie §7.2.2 (nicht feiner auditiert) · PDF p.49 |
| 7.2.2.0.5 | Clarifications: Dilemmas Entering Play | 🟡 | wie §7.2.2 (nicht feiner auditiert) · PDF p.49 |
| 7.2.2.1 | AUTOMATIC EFFECTS | 🟡 | wie §7.2.2 (nicht feiner auditiert) · PDF p.49 |
| 7.2.2.2 | CONDITIONAL EFFECTS | 🟡 | wie §7.2.2 (nicht feiner auditiert) · PDF p.49 |
| 7.2.2.2.0.1 | Tip: Condition Examples | ➖ | Tip-Sidebar (Referenz) · PDF p.50 |
| 7.2.2.3 | CURABLE EFFECTS | ✅ | zentrales Cure-System (`DilemmaCureRules`); Pepsch green Archer / Alien Abduction / Phased Matter 2026-09-13; AttachContinue ohne Team-Stop · PDF p.50 |
| 7.2.2.3.0.1 | Tip: Cure Examples | ➖ | Tip-Sidebar (Referenz) · PDF p.51 |
| 7.2.2.4 | NULLIFIABLE EFFECTS | 🟡 | wie §7.2.2 (nicht feiner auditiert) · PDF p.51 |
| 7.2.2.4.0.1 | Tip: Nullifier Examples | ➖ | Tip-Sidebar (Referenz) · PDF p.51 |
| 7.2.3 | OTHER SEEDS | ✅ | face-up mid-attempt, acquire on solve · PDF p.51 |
| 7.2.4 | MIS-SEEDS | 🟡 | entfernt / unten zuerst · PDF p.52 |
| 7.2.4.0.1 | Clarifications: Becoming Mis-seeded | 🟡 | wie §7.2.4 (nicht feiner auditiert) · PDF p.52 |
| 7.2.5 | SOLVING THE MISSION | 🟡 | OR + xN; Espionage [As] für Besitzer; Punkte; einige Bonus-Boxen · PDF p.52 |
| 7.2.5.0.3 | Clarification: Individual Requirements | 🟡 | wie §7.2.5 (nicht feiner auditiert) · PDF p.52 |
| 7.2.5.0.2 | Clarification: Alternative Mission Requirements (from Objectives) | 🟡 | wie §7.2.5 (nicht feiner auditiert) · PDF p.53 |
| 7.2.5.0.1 | Clarification: Mission Points | 🟡 | wie §7.2.5 (nicht feiner auditiert) · PDF p.53 |
| 7.2.6 | MISSION FAILURE | 🟡 | Team stopped nur bei gescheiterter Bedingung ("unless", "to get past") oder explizitem Stop; Cure-Fehlschlag stoppt Team nicht (7.2.2.3) · PDF p.53 |
| 7.2.6.0.3 | Clarification: Failing aDual-Icon Mission | 🟡 | wie §7.2.6 (nicht feiner auditiert) · PDF p.53 |
| 7.2.6.0.2 | Tip: Mission Failures Don't Stop the Team | ➖ | Tip-Sidebar (Referenz) · PDF p.53 |
| 7.2.6.0.1 | Clarification: Reseed After Escapes | 🟡 | wie §7.2.6 (nicht feiner auditiert) · PDF p.54 |
| 7.3 | COMPLETE BORG OBJECTIVES | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.55 |
| 7.3.1 | IN GENERAL | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.55 |
| 7.3.2 | SCOUTING | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.55 |
| 7.3.2.1 | Missions Are Irrelevant: Scouting Locations | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.55 |
| 7.3.2.1.1 | Planetary Assimilation | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.56 |
| 7.3.2.2 | Scouting Ships | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.56 |
| 7.3.2.2.1 | Ship Assimilation | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.56 |
| 7.4 | BATTLE | 🟡 | PDF p.56 |
| 7.4.1 | INITIATING A BATTLE | 🟡 | Leader; **Fed nur vs Borg** (2026-08-28); Wartime-Ausnahme · PDF p.57 |
| 7.4.1.0.4 | Borg Rule: Combat is Irrelevant | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.57 |
| 7.4.1.0.3 | Clarification: Battle "Opponent" | 🟡 | wie §7.4.1 (nicht feiner auditiert) · PDF p.57 |
| 7.4.1.0.2 | Clarification: Cancelled Battles | 🟡 | wie §7.4.1 (nicht feiner auditiert) · PDF p.57 |
| 7.4.1.0.1 | Reminder:Actions and Valid Responses | 🟡 | wie §7.4.1 (nicht feiner auditiert) · PDF p.57 |
| 7.4.1.0.5 | Borg Rule: Leaders are Irrelevant | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.57 |
| 7.4.1.0.6 | Clarifications: Affiliation Attack Restrictions | 🟡 | wie §7.4.1 (nicht feiner auditiert) · PDF p.58 |
| 7.4.2 | PERSONNEL BATTLE | 🟡 | Stun/Mortal auto; UX grob · PDF p.58 |
| 7.4.2.0.3 | Reminder:Holographic Safety Protocols | 🟡 | wie §7.4.2 (nicht feiner auditiert) · PDF p.58 |
| 7.4.2.0.2 | Clarifications: Response Precedence in the Combat Stage | 🟡 | wie §7.4.2 (nicht feiner auditiert) · PDF p.58 |
| 7.4.2.0.1 | Clarification: Dual-Personnel Cards in Combat | 🟡 | wie §7.4.2 (nicht feiner auditiert) · PDF p.59 |
| 7.4.3 | SHIP BATTLE | 🟡 | Open/Return Fire, Rotation Damage · PDF p.59 |
| 7.4.3.0.1 | Clarifications: Multitargeting | 🟡 | wie §7.4.3 (nicht feiner auditiert) · PDF p.59 |
| 7.4.3.0.2 | Tip: Always Return Fire | ➖ | Tip-Sidebar (Referenz) · PDF p.60 |
| 7.4.3.0.3 | Clarifications: Applying Tactics | ➖ | Tactics / Battle Bridge — Premiere-Scope (Rotation-Damage ist 7.5.1.2) · PDF p.60 |
| 7.4.3.0.4 | Clarifications: Downloading Tactics | ➖ | Tactics / Battle Bridge — Premiere-Scope (Rotation-Damage ist 7.5.1.2) · PDF p.60 |
| 7.4.3.0.5 | Clarifications: Opponent Always Applies Damage To You | 🟡 | wie §7.4.3 (nicht feiner auditiert) · PDF p.61 |
| 7.4.4 | AFTER THE BATTLE | 🟡 | Survivors stopped · PDF p.61 |
| 7.4.4.0.1 | Borg Rule: Borg Counter-Attacks | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.61 |
| 7.5 | DAMAGE AND REPAIRS | 🟡 | PDF p.61 |
| 7.5.1 | DAMAGE | 🟡 | Rotation 50/100; Tactics ➖ · PDF p.62 |
| 7.5.1.1 | TACTICS DAMAGE | ➖ | Tactics / Battle Bridge — Premiere-Scope (Rotation-Damage ist 7.5.1.2) · PDF p.62 |
| 7.5.1.1.0.1 | Clarifications: Tactics Damage | 🟡 | wie §7.5.1 (nicht feiner auditiert) · PDF p.62 |
| 7.5.1.2 | ROTATION DAMAGE | 🟡 | wie §7.5.1 (nicht feiner auditiert) · PDF p.63 |
| 7.5.1.3 | SYSTEMS OFF-LINE | 🟡 | wie §7.5.1 (nicht feiner auditiert) · PDF p.63 |
| 7.5.2 | REPAIR | ✅ | 2 volle eigene Züge am eigenen Outpost/HQ · PDF p.63 |
| 7.6 | CLOAK | 🟡 | Toggle + Tachyon-Lock; Fog of War ❌ · PDF p.63 |
| 7.6.0.1 | Clarifications: Cloaking and Phasing are Distinct | 🟡 | wie §7.6 (nicht feiner auditiert) · PDF p.63 |
| 7.7 | CAPTURE | ❌ | PDF p.64 |
| 7.7.0.3 | Borg Rule: Abduction | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.64 |
| 7.7.0.2 | Borg Rule: Personnel Assimilation | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.65 |
| 7.7.0.1 | Clarifications: Capture | ❌ | wie §7.7 — Feinregel unbekannt · PDF p.66 |
| 7.8 | COMMANDEER | 🟡 | Lore Returns: Fly/Battle ohne Leader; Kevin gibt Owner die Kontrolle zurück · PDF p.66 |
| 7.8.0.3 | Borg Rule: Commandeering is Irrelevant | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.66 |
| 7.8.0.2 | Clarifications: Affiliation After Commandeering | 🟡 | wie §7.8 (nicht feiner auditiert) · PDF p.66 |
| 7.8.0.1 | Clarifications: Can't Commandeer Your Cards | 🟡 | wie §7.8 (nicht feiner auditiert) · PDF p.66 |
| 7.9 | INFILTRATE | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.66 |
| 7.9.0.1 | Tip: Opponent*Must*Play Affiliation | ➖ | Tip-Sidebar (Referenz) · PDF p.67 |
| 7.9.1 | EXPOSURE | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.67 |
| 7.9.1.0.1 | Clarification: Infiltrators and House Arrest | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.67 |
| 7.10 | REQUIRED ACTIONS | 🟡 | IM Auto-Zug in Execute (volle RANGE + **visueller Hop**); Cytherians/Conundrum; WNOHGB nur eigene TABLE. Snare/Clock ❌ · PDF p.67 |
| 7.10.0.1 | Clarification: Hazards and Shortcuts in Required Moves | 🟡 | wie §7.10 (nicht feiner auditiert) · PDF p.68 |

## 8 End of Turn / Countdown / Draw

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 8.0.1 | Clarification: Cards That End Your Turn | ❌ | TOC-Zeile neu; Status unbekannt (nicht grün markiert) · PDF p.69 |
| 8.1 | COUNTDOWNS | 🟡 | Crosis, Plasma, Warp Core (next-turn, kein Auto-Nullify), Anti-Time, RB · PDF p.69 |
| 8.1.0.1 | Tip: Tracking Ticks | ➖ | Tip-Sidebar (Referenz) · PDF p.69 |
| 8.2 | PROBING | ❌ | PDF p.69 |
| 8.2.0.1 | Clarifications: Multiple Outcomes | ❌ | wie §8.2 — Feinregel unbekannt · PDF p.69 |
| 8.3 | DRAW A CARD | ✅ | + `SuppressEndOfTurnDraw` (Klim / Goddess) · PDF p.70 |
| 8.3.0.2 | Clarifications: Card Draws | 🟡 | Parent §8.3 spielbar; diese Feinregel nicht separat Pepsch-green. + `SuppressEndOfTurnDraw` (Klim / Goddess) · PDF p.70 |
| 8.3.0.1 | Tip: You Need Extra Draws | ➖ | Tip-Sidebar (Referenz) · PDF p.70 |

Until-EOT: `TurnExpiry` 🟡 nur finishing player.

## 9 Gewinn / Zeit

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 9 | Winning the game (100) | ✅ | `CheckVictory` |
| 9.0.1 | Open Rules: 100 Points, Period | ➖ | Open / Traditional / OP-Format · PDF p.71 |
| 9.0.2 | Organized Play: Time Limit | ➖ | Open / Traditional / OP-Format · PDF p.71 |

## 10 Life in Space (Skills & Schaden-Status)

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 10.1 | USING SKILLS | 🟡 | Parser + Mission/Battle effektiv · PDF p.72 |
| 10.1.0.7 | Clarifications: Undefined and Variable Attributes | 🟡 | wie §10.1 (nicht feiner auditiert) · PDF p.72 |
| 10.1.0.5 | Clarifications: Ships with Skills | 🟡 | wie §10.1 (nicht feiner auditiert) · PDF p.72 |
| 10.1.0.4 | Clarifications: "First-Listed Skill" | 🟡 | First-listed skill ≠ Classification (Foundation tip `98aec60`, Pepsch green Tsiolkovsky); Parser-Kantenfälle möglich · PDF p.73 |
| 10.1.0.3 | Clarifications: Skill Multipliers | 🟡 | wie §10.1 (nicht feiner auditiert) · PDF p.73 |
| 10.1.0.2 | Borg Rule: Sharing Skills | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.74 |
| 10.1.0.1 | Clarifications: Classifications vs. Skills | 🟡 | wie §10.1 (nicht feiner auditiert) · PDF p.75 |
| 10.1.1 | LOADED SKILLS | 🟡 | PDF p.75 |
| 10.1.1.1 | OFFICER AND LEADERSHIP: "LEADER" | 🟡 | wie §10.1.1 (nicht feiner auditiert) · PDF p.75 |
| 10.1.1.1.0.1 | Clarifications: Leaders | 🟡 | wie §10.1.1 (nicht feiner auditiert) · PDF p.75 |
| 10.1.1.2 | "ANY INTELLIGENCE" | 🟡 | wie §10.1.1 (nicht feiner auditiert) · PDF p.75 |
| 10.1.1.3 | GURAMBA / Borg Rule: Borg and Guramba | 🟡 | wie §10.1.1 (nicht feiner auditiert) · PDF p.75 |
| 10.1.1.4 | TRANSPORTER SKILL | 🟡 | wie §10.1.1 (nicht feiner auditiert) · PDF p.75 |
| 10.2 | GETTING HURT | 🟡 | PDF p.75 |
| 10.2.1 | STOPPED | ✅ | Unstop Zugwechsel · PDF p.76 |
| 10.2.1.0.2 | Clarifications: Unstopping & Long-Term Stops | 🟡 | Parent §10.2.1 spielbar; diese Feinregel nicht separat Pepsch-green. Unstop Zugwechsel · PDF p.76 |
| 10.2.1.0.1 | Clarifications: What Stopped Cards*Can*Do | 🟡 | Parent §10.2.1 spielbar; diese Feinregel nicht separat Pepsch-green. Unstop Zugwechsel · PDF p.76 |
| 10.2.2 | KILLED OR DESTROYED | 🟡 | Discard; nicht alle Replacement · PDF p.76 |
| 10.2.2.0.2 | Clarification: Docked Ships Not Destroyed | 🟡 | wie §10.2.2 (nicht feiner auditiert) · PDF p.76 |
| 10.2.2.0.1 | Clarification: Death Terminates Disability | 🟡 | wie §10.2.2 (nicht feiner auditiert) · PDF p.76 |
| 10.2.3 | DISABLED | 🟡 | Two-Dimensional Creatures: Empathy Disabled Pepsch green; allgemeine Disabled-Regel nicht vollständig · PDF p.77 |
| 10.2.3.0.1 | Clarification: "Disabled" Ship Systems | 🟡 | wie §10.2.3 (nicht feiner auditiert) · PDF p.77 |
| 10.2.4 | STASIS | 🟡 | Alien Abduction / Phased Matter Stasis Pepsch green; früher als Stopped-Sandbox notiert · PDF p.77 |
| 10.2.4.0.3 | Clarification: Global Effects Affect Stasis Cards | 🟡 | wie §10.2.4 (nicht feiner auditiert) · PDF p.77 |
| 10.2.4.0.2 | Clarification: Ship in Stasis, Crew Not | 🟡 | wie §10.2.4 (nicht feiner auditiert) · PDF p.77 |
| 10.2.4.0.1 | Tip: Disabled vs. In Stasis | ➖ | Tip-Sidebar (Referenz) · PDF p.77 |
| 10.2.5 | SEPARATED | ❌ | PDF p.77 |
| 10.2.6 | RELOCATED | 🟡 | Einzelfälle · PDF p.78 |
| 10.2.7 | QUARANTINED | 🟡 | Hyper-Aging Quarantäne Pepsch green `46eab15`; allgemeine Quarantine-Regel nicht vollständig · PDF p.78 |
| 10.2.7.0.1 | Exception: Dilemma-Forced Relocation | 🟡 | wie §10.2.7 (nicht feiner auditiert) · PDF p.78 |
| 10.2.8 | IN PLAY 'FOR UNIQUENESS ONLY' | ❌ | PDF p.78 |
| 10.2.9 | NEMESIS DESTRUCTION | ❌ | PDF p.78 |
| 10.2.10 | HOUSE ARREST | ❌ | PDF p.78 |
| 10.2.10.0.1 | Tip: Don't Worry About House Arrest | ➖ | Tip-Sidebar (Referenz) · PDF p.79 |
| 10.3 | CHARACTERISTICS | 🟡 | Heuristik (Gul etc.) lückenhaft · PDF p.79 |
| 10.3.0.12 | Clarifications: Named In Lore | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.79 |
| 10.3.0.11 | Automatic Characteristic: Ship Origin | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.80 |
| 10.3.0.11.1 | Exceptions: Ship Origin | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.80 |
| 10.3.0.10 | Automatic Characteristic: Ship Class | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.80 |
| 10.3.0.9 | Automatic Characteristic: Species | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.81 |
| 10.3.0.9.1 | Exceptions: Species | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.82 |
| 10.3.0.8 | Automatic Characteristic: Gender | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.83 |
| 10.3.0.8.1 | Borg Rule: Gender is Irrelevant | ➖ | Borg-only / Infiltrate — Premiere-Scope laut PROJECT.md · PDF p.83 |
| 10.3.0.7 | Clarifications: Ignore Card Image | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.83 |
| 10.3.0.6 | Clarifications: Ignore Information Outside the Card | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.83 |
| 10.3.0.5 | Clarifications: Characteristics Aren't Skills | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.83 |
| 10.3.0.4 | Clarifications: Disguises as Characteristics | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.83 |
| 10.3.0.3 | Clarifications: Former & Future Characteristics | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.83 |
| 10.3.0.2 | Clarifications: Characteristics: Context Matters | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.83 |
| 10.3.0.1 | Tip: Characteristics Are Usually Obvious | ➖ | Tip-Sidebar (Referenz) · PDF p.84 |
| 10.3.0.13 | Clarifications: Matching Commanders | 🟡 | wie §10.3 (nicht feiner auditiert) · PDF p.84 |

## 11 Strange Encounters

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 11.1 | Self]SELF-CONTROLLING CARDS | 🟡 | Borg Ship Token, Rogue Borg · PDF p.85 |
| 11.1.0.1 | Clarification: Self-controlling Battling Multiple Players in Space | 🟡 | wie §11.1 (nicht feiner auditiert) · PDF p.85 |
| 11.2 | PLANETARY DESTRUCTION | 🟡 | Supernova / Tox Uthat angebunden · PDF p.85 |
| 11.2.0.1 | Clarification: Converting a planet mission after destruction | 🟡 | wie §11.2 (nicht feiner auditiert) · PDF p.86 |
| 11.3 | Tribble]TRIBBLES | ➖ | Tribbles-Stack — Premiere-Scope · PDF p.86 |
| 11.3.0.2 | Tip: Tribbles CCG Cards | ➖ | Tip-Sidebar (Referenz) · PDF p.86 |
| 11.3.0.1 | Clarification: Tribbles are Non-Cumulative | ➖ | Tribbles-Stack — Premiere-Scope · PDF p.86 |
| 11.4 | BB]BOTANY BAY CARDS | ❌ | PDF p.87 |
| 11.4.0.1 | Clarification: Botany Bay Botany Bay reseeds | ❌ | wie §11.4 — Feinregel unbekannt · PDF p.87 |
| 11.5 | THE MIRROR UNIVERSE | ➖ | Mirror Universe — Premiere-Scope · PDF p.87 |

## 12 Miscellaneous Rules / Timing-Feinheiten

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 12.1 | SELECTION | 🟡 | `AskChoice` / `PickCardFromList` · PDF p.88 |
| 12.1.0.1 | Clarification: Zero-Target Selections | 🟡 | wie §12.1 (nicht feiner auditiert) · PDF p.88 |
| 12.2 | TURNS: "EACH", "EVERY" & "FULL" | 🟡 | `TimingRules.TurnScope` · PDF p.88 |
| 12.2.0.1 | Clarification: Who's the Subject? | 🟡 | wie §12.2 (nicht feiner auditiert) · PDF p.88 |
| 12.3 | CONTROL AND OWNERSHIP | 🟡 | Felder da; Servo / Lore nutzen sie · PDF p.88 |
| 12.3.0.2 | Clarification: Affiliation Control | 🟡 | wie §12.3 (nicht feiner auditiert) · PDF p.89 |
| 12.3.0.1 | Clarification: Start of Control | 🟡 | wie §12.3 (nicht feiner auditiert) · PDF p.89 |
| 12.4 | ""HERE" AND "PRESENT" | 🟡 | Location-Host; nicht glossary-vollständig · PDF p.89 |
| 12.4.0.1 | Clarification: A Planet's Surface | 🟡 | wie §12.4 (nicht feiner auditiert) · PDF p.89 |
| 12.4.0.2 | Clarification: "Present" with Seeded Cards | 🟡 | wie §12.4 (nicht feiner auditiert) · PDF p.89 |
| 12.4.0.3 | Exception: Sites and Facilities Mean Themselves | 🟡 | wie §12.4 (nicht feiner auditiert) · PDF p.90 |
| 12.5 | TIES | ❌ | PDF p.90 |
| 12.6 | FAR AND NEAR | ❌ | PDF p.90 |
| 12.6.0.1 | Clarification: "Far end of spaceline" | ❌ | wie §12.6 — Feinregel unbekannt · PDF p.90 |
| 12.7 | COPIES AND "DIFFERENT" | 🟡 | PDF p.90 |
| 12.8 | ""ONCE PER GAME" AND SIMILAR LIMITS | 🟡 | Session-Flags · PDF p.90 |
| 12.9 | THE CUMULATIVE RULE | ❌ | PDF p.90 |
| 12.9.0.3 | Clarification: Targets | ❌ | wie §12.9 — Feinregel unbekannt · PDF p.91 |
| 12.9.0.2 | Clarification: Timing | ❌ | wie §12.9 — Feinregel unbekannt · PDF p.91 |
| 12.9.0.1 | Clarification: Effects | ❌ | wie §12.9 — Feinregel unbekannt · PDF p.91 |
| 12.9.0.4 | Tip: Why Are Old Cards Marked "Not Cumulative"? | ➖ | Tip-Sidebar (Referenz) · PDF p.92 |
| 12.10 | THE COLON RULE | ❌ | PDF p.92 |
| 12.11 | SET, ADD, MULTIPLY (S.A.M.) | ❌ | PDF p.92 |
| 12.12 | LOOKING AT CARDS | 🟡 | Scan / Peek / Dev · PDF p.92 |
| 12.12.0.1 | Clarification: Necessity and Card Inspection | 🟡 | wie §12.12 (nicht feiner auditiert) · PDF p.92 |
| 12.13 | ""RELATED" | ❌ | PDF p.93 |
| 12.13.1 | Exception: Gender-related | ❌ | wie §12.13 — Feinregel unbekannt · PDF p.93 |
| 12.13.2 | Exception: Infiltration-related | ❌ | wie §12.13 — Feinregel unbekannt · PDF p.93 |
| 12.13.3 | Exception: Capturing-related | ❌ | wie §12.13 — Feinregel unbekannt · PDF p.93 |
| 12.14 | EQUIVALENTS | ❌ | PDF p.93 |
| 12.14.0.2 | Clarification: "Like" or "As" Examples | ❌ | wie §12.14 — Feinregel unbekannt · PDF p.94 |
| 12.14.0.1 | Tip: "In Place Of" Not Equivalent | ➖ | Tip-Sidebar (Referenz) · PDF p.94 |
| 12.15 | WHAT DOES THIS CARD MEAN? | ❌ | TOC-Zeile neu; Status unbekannt (nicht grün markiert) · PDF p.94 |
| 12.16 | WHAT HAPPENS WHEN I BREAK A RULE? | ❌ | TOC-Zeile neu; Status unbekannt (nicht grün markiert) · PDF p.94 |
| 12.15.0.1 | Tip: Common Quick Fixes | ➖ | Tip-Sidebar (Referenz) · PDF p.95 |
| 12.18 | CLOSING | ❌ | TOC-Zeile neu; Status unbekannt (nicht grün markiert) · PDF p.95 |

**Timing-Stack (Buch Kap. 5 + Glossary *actions*)**

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 5 / Glossary | Action stack + responses | 🟡 | `TimingRules.ActionStack` |
| 5 / Glossary | Just / valid response | 🟡 | Katalog Amanda/Kevin/Q2/Hugh/…; Hugh DONE Dilemma-only |
| 7.10 | Mandatory / required actions | 🟡 | IM Auto-Zug; Cytherians/Conundrum; Snare/Clock ❌ |

## 13 Icon Legend

PDF p. 96–98. Keine `(13.x)`-Nummern.

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 13 | ICONS WITH BUILT-IN RULES | 🟡 | AU / Staffing / Quadrant / Affiliation-Icons gelesen; Wirkung lückenhaft |
| 13 | Staffing | 🟡 | `MovementRules.IsShipStaffed`; G2–G7 Pepsch-green Staffing/Fly/Battle |
| 13 | Quadrants | 🟡 | Spaceline-Quadrant; Native-Quadrant-Feinheiten dünn |
| 13 | Affiliations | 🟡 | Parser + Treaty |
| 13 | ICONS WITHOUT BUILT-IN RULES | ➖ | Eras / Factions / Other — nur wenn Karte referenziert |
| 13 | Expansion Icons | ➖ | Referenz |

## 14 Listen

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 14.1 | Affiliations (Liste) | ➖ | Referenzliste / Icon-Legend (keine eigene Engine-Regel) · PDF p.99 |
| 14.2 | Card Types (Liste) | ➖ | Referenzliste / Icon-Legend (keine eigene Engine-Regel) · PDF p.99 |
| 14.3 | Personnel Types / Classifications (Liste) | ➖ | Referenzliste / Icon-Legend (keine eigene Engine-Regel) · PDF p.100 |
| 14.4 | Regular Skills (Liste) | ➖ | Referenzliste / Icon-Legend (keine eigene Engine-Regel) · PDF p.100 |
| 14.5 | Ship Special Equipment (Liste) | ➖ | Referenzliste / Icon-Legend (keine eigene Engine-Regel) · PDF p.102 |
| 14.6 | Homeworlds (Liste) | ➖ | Referenzliste / Icon-Legend (keine eigene Engine-Regel) · PDF p.102 |
| 14.7 | Nemesis Icons (Liste) | ➖ | Referenzliste / Icon-Legend (keine eigene Engine-Regel) · PDF p.102 |

## Temporary Rulings / Appendix B

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| TR | Temporary Rulings (p. 225) | 🟡 | Nur zwei Verweise: leaves play ohne entering play; Borg Outpost auf assimiliertem Homeworld. Lookup laut IMPLEMENT.md vor jedem Fix. |
| B | Appendix B: Change Log (p. 322) | ➖ | Verweist auf Recent Rulings Document / Starship Excelsior Archive — keine Engine-Regel |

## UI / Infra (kein Compendium)

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| UI | Deck Builder | ✅ | — |
| UI | Table Snap / Host / Detail / Back | ✅ | — |
| UI | Occupancy Badge (Host footer Away Team / Crew) | 🟡 | CODED tip `034ee39`; Pepsch smoke pending — nicht DONE |
| UI | Response Window (Silent Badge / Think Tray / Pass) | 🟡 | CODED; Pepsch smoke pending |
| UI | `.stsave` | 🟡 | Schema 2; Stack-Fenster dünn |
| UI | LegalMoves beide Sitze | ✅ | `CollectBoth` + off-turn Interrupts |
| UI | PlayOn-Parser | 🟡 | Interrupts ja; Events noch Katalog-`Place` |
| UI | Netz / KI | ❌ | Hybrid geplant |
| UI | TableWindow extract P0-D1 / P0-E1 | ❌ | `EXTRACT_REST.md`; Welle 1 Slices 1–9 DONE; nächstes Ticket Captain Go |

---

## Offene Premiere-Priorität (wenn kein konkreter Bug)

1. 7.4.1 / 7.4.2 Initiate vollständig (Karten, die Fed explizit erlauben)
2. 6.5.1 at-any-time vs. gültige Response (Glossary *actions*) — Response Window smoke
3. 12.4 present / here an Hosts festziehen (ACTIVE; Tracker `GLOSSARY_COVERAGE.md`)
4. 6.3.4 Dual-Personnel erst wenn eine PR-Karte es braucht
5. Events auf `PlayOnRules` nur als Fallback, Katalog bleibt Override

