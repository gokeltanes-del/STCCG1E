# Compendium 2.7.4 — Checkliste

Quelle: `rules/Compendium_Rulebook.pdf`. Prozess: `RULES.md`.  
Stand: 2026-08-28. Scope: Premiere; ➖ = nicht jetzt.

✅ spielbar · 🟡 Lücken · ❌ fehlt · ➖ später

Bei einem Fix nur die **betroffene** Zeile ändern und die Code-Datei nennen.

---

## 1 Einleitung

| § | Thema | Status | Code / Hinweis |
|---|--------|--------|----------------|
| 1.1 | About this game | ➖ | — |
| 1.2 | About this rulebook / Modern | ➖ | Wir spielen Traditional/Open-Nähe, kein OP-Format |

---

## 2 Kartentypen

| § | Thema | Status | Code |
|---|--------|--------|------|
| 2.1 | Missions | 🟡 | Spaceline, Quadrant/Region, [P]/[S], Solve |
| 2.1.0.1 | Dual-Icon Missions | 🟡 | Icons gelesen |
| 2.2 | Dilemmas | 🟡 | `DilemmaRules` PR-Katalog; AU Teil-Apply |
| 2.3 | Artifacts | 🟡 | Acquire + Hand-Play PR; Horga'hn Extra-Play/Draw 2026-08-28 |
| 2.3.0.1 | Earning & using artifacts | 🟡 | Acquire bei Solve; Horga'hn auch Hand→TABLE |
| 2.4 | Events / Incidents / Objectives | 🟡 | Events PR+AU Teil; Incidents/Objectives ➖; WCB/Plasma Skill-Nullify 2026-08-28 |
| 2.5 | Doorways | 🟡 | Seed + Side-Deck-Cover |
| 2.6 | Interrupts | 🟡 | Katalog + Stack + off-turn; Devil vs Encounter-Wind-Dancer |
| 2.7 | Personnel | 🟡 | Report, Skills-Parser, Dual-Mode |
| 2.8 | Equipment | 🟡 | `ModifierRules` Premiere |
| 2.9 | Ships | 🟡 | Dock, Staff, RANGE, Battle, Cloak |
| 2.9.0.1 | Special Equipment | 🟡 | Detail-Text; Wirkung lückenhaft |
| 2.10 | Facilities | 🟡 | Seed Planet+Affil; Report-Host; ≠ Schiff |
| 2.10.0.1 | Facility types ≠ card types | ✅ | Outpost/HQ als Facility-Kind |
| 2.11 | Sites | ➖ | nur Side-Stapel |
| 2.12 | Time Locations | ➖ | |
| 2.13 | Tactics | ➖ | Battle-Bridge-Stapel leer |
| 2.14 | Tribbles | ➖ | Stapel |
| 2.15 | Q-icon cards | 🟡 | Continuum-Stapel; Q-Doorway dünn |
| 2.16 | Banned cards | ➖ | privater Client, keine Ban-Liste |

**2 extra (im Buch verteilt, bei uns relevant)**

| Thema | Status | Code |
|--------|--------|------|
| Uniqueness / Persona | 🟡 | Name + Lore-Heuristik |
| Hidden Agenda | 🟡 | Flip via Authority |
| AU-Icon + enabling Doorway | ❌ | |

---

## 3 Deck

| § | Thema | Status | Code |
|---|--------|--------|------|
| 3.1 | Seed deck | 🟡 | `.stdeck` v2; 30/30/30 nicht erzwungen |
| 3.1.1 | Mission pile | ✅ | |
| 3.1.2 | Site pile | 🟡 | Zone da, nicht gespielt |
| 3.2 | Draw deck | ✅ | |
| 3.3 | Side decks | 🟡 | Tent/BB/QC/Site/Tribble Zonen; Typ-Limits lückenhaft |

---

## 4 Seed-Phasen

| § | Thema | Status | Code |
|---|--------|--------|------|
| 4.1 | Doorway phase | ✅ | sequential |
| 4.2 | Mission phase | 🟡 | Quadrant/Region/Slots; Shared = 1 Location, Face = Zugspieler |
| 4.2.0.3 | Shared missions both players' | 🟡 | your + opponent's; Drucktext der eigenen Kopie |
| 4.3 | Dilemma phase | 🟡 | Dil+Art gleiche Phase, getrennte Stapel |
| 4.4 | Facility phase | 🟡 | |
| 4.4.1 | Seeding and playing facilities | 🟡 | |
| 4.4.2 | Where outposts seed/play | 🟡 | Outpost nur Planet; Neutral=NA |
| 4.4.3 | Seeding sites | ❌ | |
| 4.4.4 | Starting the game | ✅ | Shuffle, Hand 7 |

`SeedRules`: Cryo=Space, 3 AU under Cryo.

---

## 5 The Play Phase

| § | Thema | Status | Code |
|---|--------|--------|------|
| 5 | Turn = Play → Execute orders → Draw | ✅ | `GameSession` Segmente |
| 5 | Actions / valid responses (Überblick) | 🟡 | Detail in 9 / TimingRules |

---

## 6 Karte spielen

| § | Thema | Status | Code |
|---|--------|--------|------|
| 6.1 | Normal card play | 🟡 | 1× / Zug; Interrupts extra |
| 6.1.1 | Playing for free | 🟡 | `PlayRules.PlaysForFree` |
| 6.2 | Entering play | 🟡 | |
| 6.3 | Reporting for duty | 🟡 | Facility + Treaty-Mix + NA |
| 6.3.1 | Duplication and personas | 🟡 | |
| 6.3.2 | Holographic personnel/equipment | ❌ | |
| 6.3.3 | Multi-affiliation cards | 🟡 | `DualAffiliationRules`; kein separates AT |
| 6.3.4 | Dual-personnel cards | ❌ | |
| 6.3.5 | Mirror / impersonators | ➖ | |
| 6.4 | Leaving play | 🟡 | Discard / OOP; nicht alle Fälle |
| 6.5 | Other ways to play | 🟡 | |
| 6.5.1 | Playing at any time | 🟡 | Interrupts; `CollectOffTurn` seit 2026-08-28 |
| 6.5.1.1 | Playing a doorway | 🟡 | meist nur eigener Zug |
| 6.5.2 | Persona replacement | ❌ | |
| 6.5.3 | Downloading | 🟡 | Tent + Verb |
| 6.5.4 | Special downloading | 🟡 | SD-Verb / UI |

Report an Schiffe (nicht nur Special) noch ❌.

---

## 7 Execute Orders

| § | Thema | Status | Code |
|---|--------|--------|------|
| 7.1 | Move (Überbegriff) | 🟡 | |
| 7.1.1 | Beam | 🟡 | gleiche Location; Host-Affiliation/Treaty (Equipment & Artifacts frei); Planet-AT frei; **kein Beam auf Space-Mission** (7.1.1.0.1, 2026-08-31) |
| 7.1.2 | Walk | ❌ | Site/Nor |
| 7.1.3 | Staff a ship | 🟡 | `MovementRules.IsShipStaffed` |
| 7.1.4 | Dock & undock | 🟡 | |
| 7.1.5 | Fly a starship | 🟡 | RANGE + Glow aus `BoardStore.Locations` (Gaps-Span, Q-Net-Kante). LegalMoves-Liste noch Namens-Snapshot |
| 7.1.6 | Land & take off | ❌ | |
| 7.1.7 | Move between quadrants | 🟡 | WNOHGB: Ring-RANGE nur für **Controller** (eigene TABLE-Spalte); Gaps-Span mitzählen; Wormhole 2 Karten exposed→Location+Stop |
| 7.1.8 | Time travel | ➖ | |
| 7.2 | Attempt a mission | 🟡 | Pipeline vorhanden |
| 7.2.1 | Beginning an attempt | 🟡 | |
| 7.2.2 | Encountering dilemmas | 🟡 | Katalog ersetzt Heuristik; 7.2.2.3 Zentrales Cure-System (`DilemmaCureRules`, Bedingungen zuerst, dann Cure auf Attachment/Refresh/Solve); Curable Dilemmas ohne Condition stoppen Team nicht (AttachContinue); Pepsch green: Archer, Alien Abduction, Phased Matter (2026-09-13) |
| 7.2.3 | Other seeds (artifacts) | ✅ | face-up mid-attempt, acquire on solve |
| 7.2.4 | Mis-seeds | 🟡 | entfernt / unten zuerst |
| 7.2.5 | Solving the mission | 🟡 | OR + xN; Espionage [As] für Besitzer; Punkte; einige Bonus-Boxen |
| 7.2.6 | Mission failure | 🟡 | Team stopped nur bei gescheiterter Bedingung ("unless", "to get past") oder explizitem Stop; Cure-Fehlschlag stoppt Team nicht (7.2.2.3) |
| 7.3 | Borg objectives | ➖ | |
| 7.3.1 | In general | ➖ | |
| 7.3.2 | Scouting | ➖ | |
| 7.4 | Battle | 🟡 | |
| 7.4.1 | Initiating a battle | 🟡 | Leader; **Fed nur vs Borg** (2026-08-28); Wartime-Ausnahme |
| 7.4.2 | Personnel battle | 🟡 | Stun/Mortal auto; UX grob |
| 7.4.3 | Ship battle | 🟡 | Open/Return Fire, Rotation Damage |
| 7.4.4 | After the battle | 🟡 | Survivors stopped |
| 7.5 | Damage and repairs | 🟡 | |
| 7.5.1 | Damage | 🟡 | Rotation 50/100; Tactics ➖ |
| 7.5.2 | Repair | ✅ | 2 volle eigene Züge am eigenen Outpost/HQ |
| 7.6 | Cloak | 🟡 | Toggle + Tachyon-Lock; Fog of War ❌ |
| 7.7 | Capture | ❌ | |
| 7.8 | Commandeer | 🟡 | Lore Returns: Fly/Battle ohne Leader; Kevin gibt Owner die Kontrolle zurück |
| 7.9 | Infiltrate | ➖ | |
| 7.9.1 | Exposure | ➖ | |
| 7.10 | Required actions | 🟡 | IM Auto-Zug in Execute (volle RANGE + **visueller Hop**); Cytherians/Conundrum; WNOHGB nur eigene TABLE. Snare/Clock ❌ |

---

## 8 End of Turn / Countdown / Draw

| § | Thema | Status | Code |
|---|--------|--------|------|
| 8.1 | Countdowns | 🟡 | Crosis, Plasma, Warp Core (next-turn, kein Auto-Nullify), Anti-Time, RB |
| 8.2 | Probing | ❌ | |
| 8.3 | Draw a card | ✅ | + `SuppressEndOfTurnDraw` (Klim / Goddess) |

Until-EOT: `TurnExpiry` 🟡 nur finishing player.

---

## 9 Gewinn / Zeit

| § | Thema | Status | Code |
|---|--------|--------|------|
| 9 | Winning the game (100) | ✅ | `CheckVictory` |
| 9.0.2 | Time limit (OP) | ➖ | |

---

## 10 Skills & Schaden-Status

| § | Thema | Status | Code |
|---|--------|--------|------|
| 10.1 | Using skills | 🟡 | Parser + Mission/Battle effektiv |
| 10.1.1 | Loaded skills | 🟡 | |
| 10.2 | Getting hurt | 🟡 | |
| 10.2.1 | Stopped | ✅ | Unstop Zugwechsel |
| 10.2.2 | Killed or destroyed | 🟡 | Discard; nicht alle Replacement |
| 10.2.3 | Disabled | ❌ | |
| 10.2.4 | Stasis | 🟡 | als Stopped-Sandbox |
| 10.2.5 | Separated | ❌ | |
| 10.2.6 | Relocated | 🟡 | Einzelfälle |
| 10.2.7 | Quarantined | ❌ | |
| 10.2.8 | In play for uniqueness only | ❌ | |
| 10.2.9 | Nemesis destruction | ❌ | |
| 10.2.10 | House arrest | ❌ | |
| 10.3 | Characteristics | 🟡 | Heuristik (Gul etc.) lückenhaft |

---

## 11 Sonder-Mechaniken

| § | Thema | Status | Code |
|---|--------|--------|------|
| 11.1 | Self-controlling cards | 🟡 | Borg Ship Token, Rogue Borg |
| 11.2 | Planetary destruction | 🟡 | Supernova / Tox Uthat angebunden |
| 11.3 | Tribbles | ➖ | |
| 11.4 | Botany Bay cards | ❌ | |
| 11.5 | Mirror universe | ➖ | |

---

## 12 Begriffe / Timing-Feinheiten

| § | Thema | Status | Code |
|---|--------|--------|------|
| 12.1 | Selection | 🟡 | `AskChoice` / `PickCardFromList` |
| 12.2 | Turns: each / every / full | 🟡 | `TimingRules.TurnScope` |
| 12.3 | Control and ownership | 🟡 | Felder da; Servo / Lore nutzen sie |
| 12.4 | Here and present | 🟡 | Location-Host; nicht glossary-vollständig |
| 12.5 | Ties | ❌ | |
| 12.6 | Far and near | ❌ | |
| 12.7 | Copies and different | 🟡 | |
| 12.8 | Once per game (and similar) | 🟡 | Session-Flags |
| 12.9 | The cumulative rule | ❌ | |
| 12.10 | The colon rule | ❌ | |
| 12.11 | Set, add, multiply (S.A.M.) | ❌ | |
| 12.12 | Looking at cards | 🟡 | Scan / Peek / Dev |
| 12.13 | Related | ❌ | |
| 12.13.1–3 | Gender / Infiltration / Capturing-related | ➖ | |
| 12.14 | Equivalents | ❌ | |

**Timing-Stack (Buch Kap. Actions im Glossary, bei uns Kap. 9 alt)**

| Thema | Status | Code |
|--------|--------|------|
| Action stack + responses | 🟡 | `TimingRules.ActionStack` |
| Just / valid response | 🟡 | Katalog Amanda/Kevin/Q2/Hugh/… |
| Mandatory actions | ❌ | = 7.10 |

---

## UI / Infra (kein Compendium)

| Thema | Status | Code |
|--------|--------|------|
| Deck Builder | ✅ | |
| Table Snap / Host / Detail / Back | ✅ | |
| `.stsave` | 🟡 | Schema 2; Stack-Fenster dünn |
| LegalMoves beide Sitze | ✅ | `CollectBoth` + off-turn Interrupts |
| PlayOn-Parser | 🟡 | Interrupts ja; Events noch Katalog-`Place` |
| Netz / KI | ❌ | Hybrid geplant |

---

## Offene Premiere-Priorität (wenn kein konkreter Bug)

1. 7.4.1 / 7.4.2 Initiate vollständig (Karten, die Fed explizit erlauben)  
2. 6.5.1 at-any-time vs. gültige Response (Glossary *actions*)  
3. 12.4 present / here an Hosts festziehen  
4. 6.3.4 Dual-Personnel erst wenn eine PR-Karte es braucht  
5. Events auf `PlayOnRules` nur als Fallback, Katalog bleibt Override
