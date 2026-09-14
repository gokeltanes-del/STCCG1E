# STCCG 1E — Card Expansion Tracker

Last updated: 2026-09-14 (Jadzia — Ship Seizure Spock Soll; blocked until Data tip after Scow)
Scope: **Premiere** (`PR`) + **Alternate Universe** only. Further expansions only on Captain/Pepsch Go.
Owner: Jadzia Dax (checklists). Seven keeps Glossary/Compendium/`FEATURES`. No Engine C# without Captain Go.

## Status legend

| Status | Meaning |
| --- | --- |
| `unknown` | Not yet assessed |
| `not-started` | Known gap, no work yet |
| `partial` | Some rules/UI wired, incomplete |
| `working` | Playable / green tip confirmed |
| `blocked` | Waiting on rules, extract, or Captain Go |

Columns: **Status** · **Notes** · **Source** (who / tip / date).
Source tip: when Data/Spock/Seven/Captain report, update the row.

## Summary counts

### Premiere ($(System.Collections.Hashtable.Folder)) — 363 cards

| Card Type | Count |
| --- | ---: |
| Artifact | 9 |
| Dilemma | 45 |
| Equipment | 11 |
| Event | 37 |
| Facility | 3 |
| Interrupt | 39 |
| Mission | 49 |
| Personnel | 136 |
| Q Event | 1 |
| Ship | 33 |
| **Total** | **363** |

### Alternate Universe ($(System.Collections.Hashtable.Folder)) — 122 cards

| Card Type | Count |
| --- | ---: |
| Artifact | 7 |
| Dilemma | 24 |
| Doorway | 2 |
| Equipment | 2 |
| Event | 16 |
| Facility | 1 |
| Interrupt | 29 |
| Mission | 10 |
| Personnel | 23 |
| Ship | 8 |
| **Total** | **122** |

---

## Premiere

Source JSON: `artifacts/sample_data/PR/cards.json`

### Artifact

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Betazoid Gift Box (1 R) | unknown |  |  |
| Horga'hn (2 R) | unknown |  |  |
| Interphase Generator (3 R) | unknown |  |  |
| Kurlan Naiskos (4 R) | unknown |  |  |
| Thought Maker (5 R) | unknown |  |  |
| Time Travel Pod (6 R) | unknown |  |  |
| Tox Uthat (7 R) | unknown |  |  |
| Varon-T Disruptor (8 R) | working | Pepsch getestet: Looten OK, verdoppelt STRENGTH auf Planet; Beamen auf Schiff ohne Treaty repariert (`TreatyRules.CanOccupyHost`). | Pepsch 2026-09-12 |
| Vulcan Stone of Gol (9 R) | unknown |  |  |

### Dilemma

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Alien Abduction (10 U) | working | Pepsch green (Cure OR + Stasis Beam-Block; zentrales Cure-System 7.2.2.3 via 3 Leadership oder Mission Completed; Fix: kein Stoppen des Teams, nahtlose Fortsetzung bei Cure oder Nicht-Cure lt. 7.2.2.3/7.2.6). | Pepsch 2026-09-13 |
| Alien Parasites (11 U) | partial | tip Data/`f087866` Neg Control + Strip/`2976b61`. Hotseat-Chooser PARK. Pending Pepsch green → working. | Data/`2976b61` 2026-09-09 |
| Anaphasic Organism (12 C) | partial | tip Data/`502d8e0` + Fix/`3532748` (Fail resigns=discard, not Kill). Ungetestet. | Data/`3532748` 2026-09-06 |
| Ancient Computer (13 R) | partial | tip Data/`bb551f1`. Wall: 2 Computer Skill OR 3 SCIENCE OR 3 ENGINEER; Fail Stop+unter Mission. Ungetestet. | Data/`bb551f1` 2026-09-06 |
| Archer (14 C) | working | Pepsch green bestätigt: Auswertung Attribute / Opponent-Choice bei Gleichstand und Stop-Verhalten funktionieren einwandfrei. | Pepsch 2026-09-13 |
| Armus: Skin Of Evil (15 R) | partial | tip Data/`31d0c73`. 1 AT random Kill; discard; Rest Continue (kein Stop). Ungetestet. | Data/`31d0c73` 2026-09-06 |
| Barclay's Protomorphosis Disease (16 R) | working | Pepsch getestet OK (Metamorphosis/Transformation). | Pepsch 2026-09-06 |
| Birth of "Junior" (17 U) | working | Pepsch green: Place+Continue; cumulative RANGE Countdown tip Data/`755242c`; destroy at 0; Cure 3 ENG nullify. | Pepsch 2026-09-14; Data/`755242c` |
| Borg Ship (18 R) | working | Captain/Pepsch „soweit“ — nicht nochmal Dilemma-Welle. | Captain 2026-09-06 |
| Chalnoth (19 U) | working | Pepsch green: Pass 3 SEC OR STR>40 → +5 + Continue; Fail Opp 1 Kill, AT stopped. tip Data/`b01d6dc`. | Pepsch 2026-09-13 |
| Cosmic String Fragment (20 U) | partial | tip Data/`85ca39a`. Pass Astrophysics OR ENG OR Navigation → +5 + Continue; Fail Ship destroy. Ungetestet. | Data/`85ca39a` 2026-09-06 |
| Crystalline Entity (21 R) | partial | tip Data/`6f11978`. Space/Planet +5; Fail kill-all life (nicht Stasis). Lore-Double geparkt. Ungetestet. | Data/`6f11978` 2026-09-06 |
| Cytherians (22 R) | partial | tip Data/`baa9fd5`. Place+Attempt-end; far-end +15. PARK: LegalMoves-toward-far-end; Borg no-pts; Mission Debriefing. Ungetestet. | Data/`baa9fd5` 2026-09-06 |
| El-Adrel Creature (23 U) | working | Pepsch green: 2 staerkste (Tie=Owner); STR>16 Continue (9+9 planet AT Overcome OK); Fail random Kill + AT stop. tip Data/`4b3467b` + live-feed/`ea1ff6d` (owner fallback, refresh Team/Present, ignore Kill on Overcome). | Pepsch 2026-09-13; Data/`ea1ff6d` |
| Female's Love Interest (24 C) | working | Pepsch getestet OK (Continue after Relocate). | Pepsch 2026-09-06 |
| Firestorm (25 U) | working | Pepsch green: INT<5 Kills OK, Versuch-Fortsetzung OK, Overlay EFFECT-Header OK. PARK: ETA-Escape. | Pepsch 2026-09-12 |
| Gravitic Mine (26 U) | partial | tip Data/`5c4f563`. Pass SCIENCE+Navigation Continue; Fail Damage + Ship/Crew stop. Ungetestet. | Data/`5c4f563` 2026-09-06 |
| Hologram Ruse (27 U) | working | Pepsch getestet OK. | Pepsch 2026-09-06 |
| Hyper-Aging (28 U) | working | Pepsch green Quarantäne+Beam-Block `46eab15`; Detailansicht Debuff-Gruppierung OK. RemFatigue PARK. | Pepsch 2026-09-12 |
| Iconian Computer Weapon (29 C) | partial | tip Data/`47984b5`. Pass SCIENCE Continue; Fail Stop + Non-Pers Hand discard+draw. Ungetestet. | Data/`47984b5` 2026-09-06 |
| Impassable Door (30 C) | working | Pepsch green (komplett); tip Data/`be5062b`. | Pepsch + Data/`be5062b` 2026-09-07 |
| Ktarian Game (31 R) | working | Pepsch green: Now + start-of-turn disable; Cure CUNNING>30 or Android (ship-hosted cure present = host crew only, tip Data/`3c7ee2d`). Unit tests `VerifyKtarianGame` / `VerifyDilemmaCureRules`. | Pepsch 2026-09-13; Data/`3c7ee2d` |
| Male's Love Interest (32 C) | working | Pepsch getestet OK (Continue after Relocate). | Pepsch 2026-09-06 |
| Matriarchal Society (33 U) | partial | tip Data/`d68f511`. Wall ≥2 Female Continue; Fail Stop+unter Mission. PARK: Borg gender. Ungetestet. | Data/`d68f511` 2026-09-06 |
| Menthar Booby Trap (34 C) | partial | tip Data/`01e5bb8`. Place immer; MED Continue else Kill+Stop; Cure 2 ENG. PARK: LegalMoves move-block. GAP Cure-Present-Scope (Ship): Skills an Bord, nicht ortsweit — Seven FEATURES `cd468fb`. Ungetestet. | Data/`01e5bb8` + Spock 2026-09-06 |
| Microbiotic Colony (35 C) | partial | tip Data/`f8c14d5`. SCI+ENG+OFF Continue; Fail Damage+Stop; immer discard. Ungetestet. | Data/`f8c14d5` 2026-09-06 |
| Microvirus (36 C) | working | Pos+neg Choose OK. PARK: UI Dilemma-groß + 2-Fenster Chooser/Beobachter. | Pepsch 2026-09-07 |
| Nagilum (37 R) | partial | tip Data/`b16e2b8`. 3 Diplomacy OR STR>40 → +5 Continue; Fail half-kill (abrunden)+Stop. Ungetestet. | Data/`b16e2b8` 2026-09-06 |
| Nanites (38 U) | partial | tip Data/`df0e3fa`. 2 SCIENCE OR Diplomacy → +5 Continue; Fail Damage+Stop. Ungetestet. | Data/`df0e3fa` 2026-09-06 |
| Nausicaans (39 U) | partial | tip Data/`485814c`. STR>44 Continue; Fail random Kill+Stop. PARK: Interphase/Zon nullify. Ungetestet. | Data/`485814c` 2026-09-06 |
| Nitrium Metal Parasites (40 U) | working | Pepsch green (AttachAndContinue). | Pepsch 2026-09-06 |
| Null Space (41 U) | partial | tip Data/`04f4bd6`. 2 Navigation → +5 Continue; Fail Damage+Stop. Ungetestet. | Data/`04f4bd6` 2026-09-06 |
| Phased Matter (42 C) | working | Pepsch green: AT-Split, Stasis der größeren Gruppe, Weiterführung der kleineren Gruppe und Cure (ENG+SCI unphased) via zentrales Cure-System bestätigt. | Pepsch 2026-09-13 |
| Portal Guard (43 U) | working | CUNN>7/Honor Continue; Fail: BeamBack+Stop wenn Beamen möglich (Schiff/Facility vorhanden, kein Stasis/Quarantäne); Kill wenn Beamen unmöglich (z.B. Hyper-Aging Quarantäne oder kein Schiff/Facility) + unter Mission. Unit-Test `VerifyPortalGuard`. | Captain 2026-09-13 |
| Q (44 R) | unknown |  |  |
| Radioactive Garbage Scow (45 U) | partial | tip Data/`2c6a3f9` + prior a4e4e5d/bf7a394/558fc4e. Pepsch severe display/tow FAIL after 2c6a3f9: art overlays ship on select; tow snap covers ship; Fly does not move Scow. Data Go thorough token-vs-ship. Stays partial — not working. | Pepsch FAIL 2026-09-14; Data Go token-vs-ship |
| Rebel Encounter (46 U) | unknown |  |  |
| REM Fatigue (47 U) | unknown |  |  |
| Sarjenka (48 R) | unknown |  |  |
| Shaka, When the Walls Fell (49 U) | unknown |  |  |
| Tarellian Plague Ship (50 U) | unknown |  |  |
| Temporal Causality Loop (51 R) | unknown |  |  |
| Tsiolkovsky Infection (52 R) | partial | AttachContinue: kein Team-Stop (Rulebook 7.2.2.3), Verlust 1. Skill, Versuch läuft weiter; Cure 3 MEDICAL. | Captain 2026-09-13 |
| Two-Dimensional Creatures (53 U) | partial | AttachContinue: kein Team-Stop (Rulebook 7.2.2.3), Schiff bewegungsunfähig, Versuch läuft weiter; Cure ENG+SCI. | Captain 2026-09-13 |
| Wind Dancer (54 R) | unknown |  |  |

### Equipment

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Engineering Kit (55 C) | unknown |  |  |
| Engineering PADD (56 C) | unknown |  |  |
| Federation PADD (57 C) | unknown |  |  |
| Klingon Disruptor (58 C) | unknown |  |  |
| Klingon PADD (59 C) | unknown |  |  |
| Medical Kit (60 C) | unknown |  |  |
| Medical Tricorder (61 C) | unknown |  |  |
| Romulan Disruptor (62 C) | unknown |  |  |
| Romulan PADD (63 C) | unknown |  |  |
| Starfleet Type II Phaser (64 C) | unknown |  |  |
| Tricorder (65 C) | unknown |  |  |

### Event

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Alien Probe (66 U) | unknown |  |  |
| Atmospheric Ionization (68 C) | unknown |  |  |
| Bynars Weapon Enhancement (69 R) | working | Pepsch green. | Pepsch 2026-09-06 |
| Distortion Field (70 U) | unknown |  |  |
| Espionage: Federation on Klingon (71 C) | working | Pepsch green (Espionage-Familie). | Pepsch 2026-09-06 |
| Espionage: Klingon on Federation (72 C) | working | Pepsch green (Espionage-Familie). | Pepsch 2026-09-06 |
| Espionage: Romulan on Federation (73 C) | working | Pepsch green (Espionage-Familie). | Pepsch 2026-09-06 |
| Espionage: Romulan on Klingon (74 C) | working | Pepsch green (Espionage-Familie). | Pepsch 2026-09-06 |
| Gaps in Normal Space (75 U) | unknown |  |  |
| Genetronic Replicator (76 U) | working | Glossary: Opfer & gleichzeitig Getötete ausgeschlossen; Auswahl via PickBorder; Unit-Test in EventRules. | Agent 2026-09-13 |
| Goddess of Empathy (77 R) | unknown |  |  |
| Holo-Projectors (78 U) | unknown |  |  |
| Kivas Fajo: Collector (79 U) | working | Pepsch green. | Pepsch 2026-09-06 |
| Lore Returns (80 R) | working | Pepsch green. | Pepsch 2026-09-06 |
| Lore's Fingernail (81 R) | unknown |  |  |
| Masaka Transformations (82 U) | unknown |  |  |
| Metaphasic Shields (83 U) | working | Pepsch green. | Pepsch 2026-09-06 |
| Neural Servo Device (84 U) | working | Pepsch green; Side-Sync OK (Data/55d96ef). | Pepsch 2026-09-06 |
| Nutational Shields (85 U) | working | Pepsch green. | Pepsch 2026-09-06 |
| Pattern Enhancers (86 C) | unknown |  |  |
| Plasma Fire (87 C) | working | Pepsch green. | Pepsch 2026-09-06 |
| Q-Net (88 C) | unknown |  |  |
| Raise the Stakes (89 U) | unknown |  |  |
| Red Alert! (90 C) | working | Pepsch green. | Pepsch 2026-09-06 |
| Res-Q (91 C) | unknown |  |  |
| Spacedock (92 C) | working | Pepsch green. | Pepsch 2026-09-06 |
| Static Warp Bubble (93 C) | working | Pepsch green. | Pepsch 2026-09-06 |
| Subspace Warp Rift (94 C) | unknown |  |  |
| Supernova (95 R) | unknown |  |  |
| Telepathic Alien Kidnappers (96 U) | unknown |  |  |
| Tetryon Field (97 C) | unknown |  |  |
| The Traveler: Transcendence (98 U) | working | Pepsch green. | Pepsch 2026-09-06 |
| Treaty: Federation/Klingon (99 C) | unknown |  |  |
| Treaty: Federation/Romulan (100 C) | unknown |  |  |
| Treaty: Romulan/Klingon (101 C) | unknown |  |  |
| Warp Core Breach (102 R) | working | Pepsch green. | Pepsch 2026-09-06 |
| Where No One Has Gone Before (103 C) | working | Pepsch green. | Pepsch 2026-09-06 |

### Facility

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Federation Outpost (104 C) | unknown |  |  |
| Klingon Outpost (105 C) | unknown |  |  |
| Romulan Outpost (106 C) | unknown |  |  |

### Interrupt

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Alien Groupie (107 R) | unknown |  |  |
| Amanda Rogers (108 U) | unknown |  |  |
| Asteroid Sanctuary (109 C) | unknown |  |  |
| Auto-Destruct Sequence (110 U) | unknown |  |  |
| Crosis (111 R) | working | Pepsch green. | Pepsch 2026-09-06 |
| Disruptor Overload (112 C) | unknown |  |  |
| Distortion of Space/Time Continuum (113 U) | unknown |  |  |
| Emergency Transporter Armbands (114 C) | unknown |  |  |
| Energy Vortex (115 U) | unknown |  |  |
| Escape Pod (116 C) | unknown |  |  |
| Full Planet Scan (117 U) | unknown |  |  |
| Honor Challenge (118 R) | unknown |  |  |
| Hugh (119 R) | unknown |  |  |
| Incoming Message: Federation (120 U) | unknown |  |  |
| Incoming Message: Klingon (121 U) | unknown |  |  |
| Incoming Message: Romulan (122 U) | unknown |  |  |
| Jaglom Shrek: Information Broker (123 R) | unknown |  |  |
| Kevin Uxbridge (124 U) | unknown |  |  |
| Klingon Death Yell (125 R) | unknown |  |  |
| Klingon Right of Vengeance (126 C) | unknown |  |  |
| Life-form Scan (127 U) | unknown |  |  |
| Long-Range Scan (128 C) | unknown |  |  |
| Loss of Orbital Stability (129 C) | unknown |  |  |
| Near-Warp Transport (130 U) | unknown |  |  |
| Palor Toff: Alien Trader (131 C) | unknown |  |  |
| Particle Fountain (132 C) | unknown |  |  |
| Q2 (133 U) | unknown |  |  |
| Rogue Borg (134 C) | working | Pepsch green (Rogue Borg Mercenaries). | Pepsch 2026-09-06 |
| Scan (135 C) | unknown |  |  |
| Ship Seizure (136 C) | blocked | Spock Soll 2026-09-14: player picks own Tractor ship + empty exposed ship here (not random). Data Go after Scow thorough layout/token-vs-ship. Flip blocked->partial when Data tip lands; not working until Pepsch smoke. | Spock Soll + Captain 2026-09-14 |
| Subspace Interference (137 C) | unknown |  |  |
| Subspace Schism (138 U) | unknown |  |  |
| Tachyon Detection Grid (139 C) | unknown |  |  |
| Temporal Rift (140 U) | unknown |  |  |
| The Devil (141 R) | unknown |  |  |
| The Juggler (142 U) | unknown |  |  |
| Transwarp Conduit (143 U) | working | Pepsch green. | Pepsch 2026-09-06 |
| Vulcan Mindmeld (144 U) | unknown |  |  |
| Wormhole (145 C) | working | Pepsch green. | Pepsch 2026-09-06 |

### Mission

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Avert Disaster (146 R) | unknown |  |  |
| Cloaked Mission (147 U) | unknown |  |  |
| Covert Installation (148 C) | unknown |  |  |
| Covert Rescue (149 U) | unknown |  |  |
| Cultural Observation (150 R) | unknown |  |  |
| Diplomacy Mission (151 U) | unknown |  |  |
| Evacuation (152 U) | unknown |  |  |
| Evaluate Terraforming (153 R) | unknown |  |  |
| Excavation (154 C) | unknown |  |  |
| Explore Black Cluster (155 R) | unknown |  |  |
| Explore Dyson Sphere (156 R) | unknown |  |  |
| Explore Typhon Expanse (157 R) | unknown |  |  |
| Expose Covert Supply (158 U) | unknown |  |  |
| Extraction (159 R) | unknown |  |  |
| Fever Emergency (160 C) | unknown |  |  |
| First Contact (161 U) | unknown |  |  |
| Hunt for DNA Program (162 R) | unknown |  |  |
| Iconia Investigation (163 R) | unknown |  |  |
| Investigate "Shattered Space" (164 R) | unknown |  |  |
| Investigate Alien Probe (165 R) | unknown |  |  |
| Investigate Anomaly (166 C) | unknown |  |  |
| Investigate Disappearance (167 R) | unknown |  |  |
| Investigate Disturbance (168 R) | unknown |  |  |
| Investigate Massacre (169 R) | unknown |  |  |
| Investigate Raid (170 R) | unknown |  |  |
| Investigate Rogue Comet (171 R) | unknown |  |  |
| Investigate Sighting (172 R) | unknown |  |  |
| Investigate Time Continuum (173 R) | unknown |  |  |
| Khitomer Research (174 R) | unknown |  |  |
| Krios Suppression (175 U) | unknown |  |  |
| Medical Relief (176 R) | unknown |  |  |
| New Contact (177 R) | unknown |  |  |
| Pegasus Search (178 R) | unknown |  |  |
| Plunder Site (179 U) | unknown |  |  |
| Relief Mission (180 C) | unknown |  |  |
| Repair Mission (181 C) | unknown |  |  |
| Restore Errant Moon (182 U) | unknown |  |  |
| Sarthong Plunder (183 R) | unknown |  |  |
| Secret Salvage (184 U) | unknown |  |  |
| Seek Life-form (185 R) | unknown |  |  |
| Strategic Diversion (186 U) | unknown |  |  |
| Study "Hole in Space" (187 R) | unknown |  |  |
| Study Lonka Pulsar (188 R) | unknown |  |  |
| Study Nebula (189 R) | unknown |  |  |
| Study Plasma Streamer (190 C) | unknown |  |  |
| Study Stellar Collision (191 C) | unknown |  |  |
| Survey Mission (192 R) | unknown |  |  |
| Test Mission (193 C) | unknown |  |  |
| Wormhole Negotiations (194 R) | unknown |  |  |

### Personnel

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Albert Einstein (195 R) | unknown |  |  |
| Alexander Rozhenko (196 U) | unknown |  |  |
| Alidar Jarok (304 R) | unknown |  |  |
| Alynna Nechayev (197 R) | unknown |  |  |
| Alyssa Ogawa (198 U) | unknown |  |  |
| Amarie (289 U) | unknown |  |  |
| Ba'el (254 U) | unknown |  |  |
| Baran (290 U) | unknown |  |  |
| Batrell (255 C) | unknown |  |  |
| Benjamin Maxwell (199 U) | unknown |  |  |
| B'Etor (252 R) | unknown |  |  |
| Beverly Crusher (200 R) | unknown |  |  |
| B'iJik (253 C) | unknown |  |  |
| Bochra (305 U) | unknown |  |  |
| Bok (291 U) | unknown |  |  |
| Calloway (201 C) | unknown |  |  |
| Christopher Hobson (202 C) | unknown |  |  |
| Darian Wallace (203 C) | unknown |  |  |
| Data (204 R) | unknown |  |  |
| Deanna Troi (205 R) | unknown |  |  |
| Devinoni Ral (292 U) | unknown |  |  |
| Divok (256 C) | unknown |  |  |
| Dr. Farek (293 C) | unknown |  |  |
| Dr. La Forge (206 R) | unknown |  |  |
| Dr. Leah Brahms (207 R) | unknown |  |  |
| Dr. Reyga (294 U) | unknown |  |  |
| Dr. Selar (208 U) | unknown |  |  |
| Dukath (257 C) | unknown |  |  |
| Duras (258 R) | unknown |  |  |
| Erik Pressman (209 U) | unknown |  |  |
| Etana Jol (295 U) | unknown |  |  |
| Evek (296 U) | unknown |  |  |
| Exocomp (210 U) | unknown |  |  |
| Fek'lhr (259 U) | unknown |  |  |
| Fleet Admiral Shanthi (211 U) | unknown |  |  |
| Galathon (306 C) | unknown |  |  |
| Geordi La Forge (212 R) | unknown |  |  |
| Giusti (213 C) | unknown |  |  |
| Gorath (260 C) | unknown |  |  |
| Gorta (297 C) | unknown |  |  |
| Gowron (261 R) | unknown |  |  |
| Hannah Bates (214 U) | unknown |  |  |
| Ishara Yar (298 U) | unknown |  |  |
| Jaron (307 C) | unknown |  |  |
| J'Dan (262 C) | unknown |  |  |
| Jean-Luc Picard (215 R) | unknown |  |  |
| Jenna D'Sora (216 U) | unknown |  |  |
| Jera (308 C) | unknown |  |  |
| Jo'Bril (299 U) | unknown |  |  |
| Kahless (267 R) | unknown |  |  |
| Kareel Odan (218 U) | unknown |  |  |
| Kargan (268 R) | unknown |  |  |
| K'Ehleyr (217 R) | unknown |  |  |
| Kell (269 U) | unknown |  |  |
| Klag (270 C) | unknown |  |  |
| Kle'eg (271 C) | unknown |  |  |
| K'mpec (263 U) | unknown |  |  |
| Konmel (272 U) | unknown |  |  |
| Koral (273 U) | unknown |  |  |
| Koroth (274 U) | unknown |  |  |
| Korris (275 U) | unknown |  |  |
| Kromm (276 C) | unknown |  |  |
| K'Tal (264 U) | unknown |  |  |
| K'Tesh (265 C) | unknown |  |  |
| Kurak (277 R) | unknown |  |  |
| Kurn (278 R) | unknown |  |  |
| K'Vada (266 U) | unknown |  |  |
| Leah Brahms (219 R) | unknown |  |  |
| Linda Larson (220 C) | unknown |  |  |
| L'Kor (279 U) | unknown |  |  |
| Lursa (280 R) | unknown |  |  |
| Lwaxana Troi (221 R) | unknown |  |  |
| McKnight (222 C) | unknown |  |  |
| Mendak (309 R) | unknown |  |  |
| Mendon (223 C) | unknown |  |  |
| Mirok (310 U) | unknown |  |  |
| Morag (281 U) | unknown |  |  |
| Morgan Bateson (224 R) | unknown |  |  |
| Mot the Barber (225 U) | unknown |  |  |
| Movar (311 U) | unknown |  |  |
| Narik (300 C) | unknown |  |  |
| Nella Daren (226 R) | unknown |  |  |
| Neral (313 U) | unknown |  |  |
| Nikolai Rozhenko (227 U) | unknown |  |  |
| Norah Satie (228 U) | unknown |  |  |
| Nu'Daq (282 U) | unknown |  |  |
| N'Vek (312 U) | unknown |  |  |
| Ocett (301 U) | unknown |  |  |
| Palteth (314 C) | unknown |  |  |
| Pardek (315 U) | unknown |  |  |
| Parem (316 U) | unknown |  |  |
| Reginald Barclay (229 R) | unknown |  |  |
| Richard Galen (230 R) | unknown |  |  |
| Riva (231 U) | unknown |  |  |
| Ro Laren (232 R) | unknown |  |  |
| Roga Danar (302 R) | unknown |  |  |
| Sarek (233 R) | unknown |  |  |
| Satelk (234 R) | unknown |  |  |
| Sela (317 R) | unknown |  |  |
| Selok (318 C) | unknown |  |  |
| Shelby (235 R) | unknown |  |  |
| Simon Tarses (236 C) | unknown |  |  |
| Sir Isaac Newton (237 R) | unknown |  |  |
| Sirna Kolrami (238 U) | unknown |  |  |
| Sito Jaxa (239 C) | unknown |  |  |
| Soren (240 U) | unknown |  |  |
| Taibak (319 U) | unknown |  |  |
| Taitt (242 C) | unknown |  |  |
| Takket (320 C) | unknown |  |  |
| Tallus (321 C) | unknown |  |  |
| Tam Elbrun (243 R) | unknown |  |  |
| Tarus (322 C) | unknown |  |  |
| Tasha Yar (244 R) | unknown |  |  |
| Taul (323 C) | unknown |  |  |
| Taurik (245 C) | unknown |  |  |
| Tebok (324 U) | unknown |  |  |
| Thei (325 C) | unknown |  |  |
| Thomas Riker (246 R) | unknown |  |  |
| Toby Russell (247 U) | unknown |  |  |
| Tokath (326 U) | unknown |  |  |
| Tomalak (327 R) | unknown |  |  |
| Tomek (328 C) | unknown |  |  |
| Toq (283 U) | unknown |  |  |
| Torak (284 U) | unknown |  |  |
| Toral (285 U) | unknown |  |  |
| Toreth (329 R) | unknown |  |  |
| Torin (286 C) | unknown |  |  |
| T'Pan (241 U) | unknown |  |  |
| Vagh (287 U) | unknown |  |  |
| Varel (330 C) | unknown |  |  |
| Vash (248 R) | unknown |  |  |
| Vekma (288 C) | unknown |  |  |
| Vekor (303 C) | unknown |  |  |
| Wesley Crusher (249 R) | unknown |  |  |
| William T. Riker (250 R) | unknown |  |  |
| Worf (251 R) | unknown |  |  |

### Q Event

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Anti-Time Anomaly (67 R) | unknown |  |  |

### Ship

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Combat Vessel (352 C) | unknown |  |  |
| D'deridex (357 C) | unknown |  |  |
| Devoras (358 R) | unknown |  |  |
| Haakona (359 R) | unknown |  |  |
| Husnock Ship (353 U) | unknown |  |  |
| I.K.S. Bortas (344 R) | unknown |  |  |
| I.K.S. Buruk (345 R) | unknown |  |  |
| I.K.S. Hegh'ta (346 R) | unknown |  |  |
| I.K.S. K'Vort (347 C) | unknown |  |  |
| I.K.S. Pagh (348 R) | unknown |  |  |
| I.K.S. Qu'Vat (349 R) | unknown |  |  |
| I.K.S. Vor'Cha (350 C) | unknown |  |  |
| I.K.S. Vorn (351 U) | unknown |  |  |
| Khazara (360 R) | unknown |  |  |
| Mercenary Ship (354 C) | unknown |  |  |
| Pi (361 R) | unknown |  |  |
| Runabout (331 C) | unknown |  |  |
| Science Vessel (362 C) | unknown |  |  |
| Scout Vessel (363 C) | unknown |  |  |
| Type VI Shuttlecraft (332 C) | unknown |  |  |
| U.S.S. Brattain (333 R) | unknown |  |  |
| U.S.S. Enterprise (334 R) | unknown |  |  |
| U.S.S. Excelsior (335 C) | unknown |  |  |
| U.S.S. Galaxy (336 C) | unknown |  |  |
| U.S.S. Hood (337 R) | unknown |  |  |
| U.S.S. Miranda (338 C) | unknown |  |  |
| U.S.S. Nebula (339 C) | unknown |  |  |
| U.S.S. Oberth (340 C) | unknown |  |  |
| U.S.S. Phoenix (341 R) | unknown |  |  |
| U.S.S. Sutherland (342 U) | unknown |  |  |
| U.S.S. Yamato (343 R) | unknown |  |  |
| Yridian Shuttle (355 C) | unknown |  |  |
| Zibalian Transport (356 C) | unknown |  |  |

## Alternate Universe

Source JSON: `artifacts/sample_data/Alternate_Universe/cards.json`

### Artifact

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Cryosatellite (R) | unknown |  |  |
| Data's Head (R) | unknown |  |  |
| Iconian Gateway (R) | unknown |  |  |
| Ophidian Cane (R) | unknown |  |  |
| Receptacle Stones (R) | unknown |  |  |
| Ressikan Flute (R) | unknown |  |  |
| Samuel Clemens' Pocketwatch (R) | unknown |  |  |

### Dilemma

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Alien Labyrinth (C) | unknown |  |  |
| Cardassian Trap (U) | unknown |  |  |
| Coalescent Organism (R) | unknown |  |  |
| Conundrum (C) | unknown |  |  |
| Edo Probe (U) | unknown |  |  |
| Empathic Echo (C) | unknown |  |  |
| Ferengi Attack (C) | unknown |  |  |
| Frame of Mind (U) | unknown |  |  |
| Hidden Entrance (C) | unknown |  |  |
| Hunter Gangs (C) | unknown |  |  |
| Interphasic Plasma Creatures (C) | unknown |  |  |
| Malfunctioning Door (C) | unknown |  |  |
| Maman Picard (U) | unknown |  |  |
| Outpost Raid (C) | unknown |  |  |
| Parallel Romance (U) | unknown |  |  |
| Punishment Zone (C) | unknown |  |  |
| Quantum Singularity Lifeforms (U) | unknown |  |  |
| Rascals (U) | unknown |  |  |
| Royale Casino: Blackjack (U) | unknown |  |  |
| The Gatherers (C) | unknown |  |  |
| The Higher... The Fewer (U) | unknown |  |  |
| Thought Fire (C) | unknown |  |  |
| Worshiper (C) | unknown |  |  |
| Zaldan (U) | unknown |  |  |

### Doorway

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Alternate Universe Door (C) | unknown |  |  |
| Devidian Door (R) | unknown |  |  |

### Equipment

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Echo Papa 607 Killer Drone (R) | unknown |  |  |
| I.P. Scanner (C) | unknown |  |  |

### Event

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Baryon Buildup (C) | unknown |  |  |
| Captain's Log (U) | unknown |  |  |
| Engage Shuttle Operations (U) | unknown |  |  |
| Interrogation (R) | unknown |  |  |
| Intruder Force Field (U) | unknown |  |  |
| Klim Dokachin (U) | unknown |  |  |
| Lower Decks (U) | unknown |  |  |
| Mot's Advice (U) | unknown |  |  |
| Particle Scattering Field (C) | unknown |  |  |
| Revolving Door (R) | unknown |  |  |
| Rishon Uxbridge (C) | unknown |  |  |
| The Charybdis (U) | unknown |  |  |
| The Mask of Korgano (C) | unknown |  |  |
| Thermal Deflectors (U) | unknown |  |  |
| Wartime Conditions (R) | unknown |  |  |
| Yellow Alert (C) | unknown |  |  |

### Facility

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Neutral Outpost (C) | unknown |  |  |

### Interrupt

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Anti-Matter Spread (C) | unknown |  |  |
| Barclay Transporter Phobia (U) | unknown |  |  |
| Brain Drain (U) | unknown |  |  |
| Countermanda (C) | unknown |  |  |
| Dead in Bed (U) | unknown |  |  |
| Destroy Radioactive Garbage Scow (C) | unknown |  |  |
| Devidian Foragers (C) | unknown |  |  |
| Eyes in the Dark (C) | unknown |  |  |
| Fire Sculptor (C) | unknown |  |  |
| Hail (C) | unknown |  |  |
| Howard Heirloom Candle (C) | unknown |  |  |
| Humuhumunukunukuapua'a (C) | unknown |  |  |
| Incoming Message: Attack Authorization (U) | unknown |  |  |
| Isabella (U) | unknown |  |  |
| Jamaharon (C) | unknown |  |  |
| Kevin Uxbridge: Convergence (C) | unknown |  |  |
| La Forge Maneuver (U) | unknown |  |  |
| Latinum Payoff (C) | unknown |  |  |
| Phaser Burns (C) | unknown |  |  |
| Rescue Captives (U) | unknown |  |  |
| Romulan Ambush (U) | unknown |  |  |
| Security Sacrifice (C) | unknown |  |  |
| Seize Wesley (R) | unknown |  |  |
| Senior Staff Meeting (U) | unknown |  |  |
| Temporal Narcosis (U) | unknown |  |  |
| Thine Own Self (C) | unknown |  |  |
| Vorgon Raiders (R) | unknown |  |  |
| Vulcan Nerve Pinch (C) | unknown |  |  |
| Wolf (U) | unknown |  |  |

### Mission

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Brute Force (R) | unknown |  |  |
| Compromised Mission (R) | unknown |  |  |
| Diplomatic Conference (R) | unknown |  |  |
| FGC-47 Research (R) | unknown |  |  |
| Fissure Research (R) | unknown |  |  |
| Qualor II Rendezvous (U) | unknown |  |  |
| Quash Conspiracy (R) | unknown |  |  |
| Reunion (R) | unknown |  |  |
| Risa Shore Leave (R) | unknown |  |  |
| Warped Space (R) | unknown |  |  |

### Personnel

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Ajur (U) | unknown |  |  |
| Berlingoff Rasmussen (R) | unknown |  |  |
| Beverly Picard (R) | unknown |  |  |
| Boratus (U) | unknown |  |  |
| Commander Tomalak (R) | unknown |  |  |
| Dathon (R) | unknown |  |  |
| D'Tan (U) | unknown |  |  |
| Governor Worf (R) | unknown |  |  |
| Ian Andrew Troi (R) | unknown |  |  |
| Jack Crusher (R) | unknown |  |  |
| K'mtar (R) | unknown |  |  |
| Lakanta (U) | unknown |  |  |
| Lt. (j.g.) Picard (U) | unknown |  |  |
| Major Rakal (R) | unknown |  |  |
| Maques (U) | unknown |  |  |
| Mickey D. (U) | unknown |  |  |
| Montgomery Scott (C) | unknown |  |  |
| Paul Rice (U) | unknown |  |  |
| Rachel Garrett (R) | unknown |  |  |
| Richard Castillo (U) | unknown |  |  |
| Stefan DeSeve (R) | unknown |  |  |
| Targ (C) | unknown |  |  |
| Tasha Yar - Alternate (R) | unknown |  |  |

### Ship

| Card | Status | Notes | Source |
| --- | --- | --- | --- |
| Decius (R) | unknown |  |  |
| Edo Vessel (R) | unknown |  |  |
| Future Enterprise (UR) | unknown |  |  |
| Gomtuu (R) | unknown |  |  |
| I.K.C. Fek'lhr (R) | unknown |  |  |
| I.K.C. K'Ratak (C) | unknown |  |  |
| Tama (U) | unknown |  |  |
| U.S.S. Enterprise-C (R) | unknown |  |  |

