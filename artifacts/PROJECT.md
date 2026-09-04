# STCCG 1E – Projekt

Private, nicht-kommerzielle C# / .NET 8 / WPF-App (Hotseat zuerst, später Netz + KI).  
Repo: https://github.com/gokeltanes-del/STCCG1E  
**Stand:** 2026-09-04

Docs live under `artifacts/`; board+foundation are in `ENGINE.md`.  
**Resume / bot handoff:** `artifacts/HANDOFF.md` (read first on every new chat).  
**Team:** Captain (goals + docs), Data (engine / Klasse C), Spock (rules A/B + Compendium), Seven (checklist + `FEATURES.md` backlog). Feature backlog: `artifacts/FEATURES.md`.

Daten: Lackey → `split_lackey_sets.py` → pro Set `cards.json` + Bilder (`GamePaths` / `C:\STCCG_Data`).  
Regelbuch: `rules/Compendium_Rulebook.pdf` (2.7.4).  
Prozess: `RULES.md` (Klasse A/B/C) · Feinliste: `RULES_CHECKLIST.md`.  
Karten-Scope: **Premiere zuerst**, AU Katalog/Apply in Arbeit, weitere Sets später.

**Quellbaum (VS und hier):** `artifacts/StarTrekCCG/` = Projektmappe `StarTrekCCG`.  
`Phase0_Starter` / `Phase1_Starter` / `Phase2_Starter` sind Altstand — nicht mehr editieren.

---

## Programmstruktur

```
StarTrekCCG/                          # C# / .NET 8 / WPF, ein Projekt
  App.xaml, AssemblyInfo.cs           # nur auf dem PC / GitHub
  CardBack.cs, GamePaths.cs           # Kartenrücken, C:\STCCG_Data + Data/
  TableWindow.xaml[.cs]               # Tisch = View + Input + Apply-UI
  DeckBuilderWindow.xaml[.cs]         # Deck Builder
  Assets/                             # card_back.jpg, Board-Hintergründe
  Data/Sets/<Set>/  oder Data/PR, Data/Alternate_Universe
  Data/Decks/                         # .stdeck (GamePaths.DecksRoot)
  Data/SaveGames/                     # .stsave
  Assets/Icons/Icon_{Token}.png       # Staffing/Icons, Text-Fallback
  Models/
    Card.cs                           # Instanz: Owner, Controller, FaceUp,
                                      # CurrentAffiliation, FramedOfMind
    Deck.cs, DeckEntry.cs
    ExpansionCatalog.cs               # Set-Anzeigenamen [P]/[V]
    PersonnelSkillIndex.cs            # Filter-Index Deck Builder
  Game/                               # Regeln + Engine (kein WPF)
    GameState.cs, GameAction.cs, GameEvent.cs
    EngineAuthority.cs, LegalMoves.cs
    EffectRegistry.cs, CardEffectMap.cs, IEffect.cs
    TargetQuery.cs, TargetingRules.cs, PlayOnRules.cs
    SeedRules, DownloadRules, TurnExpiry
    DualAffiliationRules, CardKinds, CardIcons
    CardFactory, CardLifecycle
    DeckPlacementRules
    *Rules: Event Interrupt Dilemma Artifact
            Reporting Movement Battle Treaty
            Mission Modifier Play Timing
  Services/
    CardDatabase.cs                   # JSON laden
    DeckService.cs                    # .stdeck
    GameSave.cs                       # .stsave
    GameSession.cs                    # Zug, Segmente, Punkte, Log
  ViewModels/, Views/                 # vorbereitet, fast leer
```

Suche so: Karten-Felder → `Models/Card.cs` · Legalität → `Game/LegalMoves.cs` + `EngineAuthority.cs` · Effekttext → passende `*Rules.cs` · Speichern → `Services/` · UI-Klick → `TableWindow.xaml.cs`.

---

## Nächster Chat (Copy-Paste)

```
Projekt: Star Trek CCG 1E private C# WPF App (nicht-kommerziell)
GitHub: https://github.com/gokeltanes-del/STCCG1E
Tech: C# / .NET 8 / WPF / VS2022
Docs: artifacts/HANDOFF.md (resume) · artifacts/PROJECT.md · artifacts/ENGINE.md · artifacts/RULES.md · artifacts/RULES_CHECKLIST.md · artifacts/CHANGELOG.md · artifacts/FEATURES.md
Regelbuch: artifacts/rules/Compendium_Rulebook.pdf (2.7.4)
  Lookup-Pflicht: Checklist-§ → Glossary → Temporary Rulings → Appendix A Errata → Appendix B

Scope: Premiere zuerst; AU nur wo schon verdrahtet.
Kein Fundamentalsystem auf Vorrat. Keine Geschwister-Kapitel „weil thematisch nah“.

Architektur: TableWindow = View/Input; Services/GameSession + Game/*Rules = Apply.
Engine: GameState + GameAction + EngineAuthority + LegalMoves.Collect / CollectBoth
Board 0–6: artifacts/ENGINE.md (fertig).
Foundation: artifacts/ENGINE.md
  E1 ToGameState | E2 Capture-Fallback | E3 Status an Instanz |
  E4 Unique-Query | E5 LegalMoves-Fly | E6 IM-Locations  (E1-E6 COMPLETE, tip 7bf128f)
  Dual-Run neben UI-Dicts. TableWindow = View.

Karten: Models/Card.cs · Legalität: LegalMoves + EngineAuthority · Texte: Game/*Rules
Save/Deck: Services/ · UI: TableWindow.xaml.cs
PlayOnRules parse „Plays on …“ (Schiff ≠ Outpost). AskChoice = OR-Buttons + Timeout-Zufall.

Fix-Protokoll (RULES.md):
  Ist/Soll → Verb → Lookup §/Glossary/Errata → Klasse A Karte | B Phrase | C Grundlage
  → kleinste Datei → CHANGELOG + Checklist-Zelle.
  3. gleiches Loch: A→B oder B→C. Engine nur dann.
Antwort immer: Klasse, §, Dateien, was bewusst nicht angefasst.

Start: Foundation E1-E6 done; Nutzer nennt TableWindow-Extract-Schritt oder den Bug / die Karte.
Änderungen in allen bearbeiteten Dateien nennen.
```

### Als Nächstes

**Kurzfristig (siehe auch `HANDOFF.md`):**  
1. **Foundation E1–E6 COMPLETE** (Pepsch tip when pushed `7bf128f`; E6 IM/Required-Move DONE).  
2. **NEXT: TableWindow extract** — card/rules truth out of TableWindow into `Game/*Rules`, BoardStore, Templates — no big-bang UI rewrite; Premiere-first after extract.  
3. Keep parked bugs visible (Gaps Buruk mystery kill; IM Fed false nullify; Wormhole broken; dump omits Gaps ships) — see HANDOFF.  
4. A/B/C-Prozess halten; Premiere-first. Team: Captain / Data / Spock / Seven. Workflow: Josef edit, Pepsch test, push when green.
Probespiel-Löcher, die das Board später schluckt (nicht einzeln flicken):

- USS Galaxy Staffing-False-Deny  
- Unique-Nebula vs zweite Kopie unter Lore (E4 done/pushed Owner-based)  
- Time Location / Snare / Clock als Required Action ?

**Nicht jetzt:** Netz, KI, Sites/Tactics voll, Borg 7.3, Mirror, Big-Bang-UI-Refactor, volles stopped/disabled/stasis-System. Targeting-Feinschliff nur wenn ein Probespiel-Bug blockiert. Neue Premiere-Wellen erst nach TableWindow-Extraktion.
---

## Architektur

```
UI (TableWindow)  →  GameAction  →  EngineAuthority(GameState)
                                      → *Rules / EffectRegistry
                                      → ApplyResult
LegalMoves.Collect(player) / CollectBoth   gleiche Quelle für Hotseat, Netz, KI
```

| Schicht | Dateien | Rolle |
|---------|---------|--------|
| View | `TableWindow.*`, `DeckBuilderWindow.*` | Drag/Drop, Snap, Overlays, Hotseat, `CaptureEngineState` |
| Session | `Services/GameSession.cs` | Turn/Segment, Punkte, 100-pt Win, OncePerGame, Expiring |
| Timing | `Game/TimingRules.cs` | ActionStack, Responses, TurnScope / TickCountdown |
| Kataloge | `Game/EventRules`, `InterruptRules`, `DilemmaRules`, `ArtifactRules` | Printed text + Persist/Fate |
| Board-Regeln | `Game/ReportingRules`, `MovementRules`, `BattleRules`, `TreatyRules`, `MissionRules`, `ModifierRules`, `PlayRules` | Report, Fly, Battle, Mix, Skills |
| Engine | `Game/GameState`, `GameAction`, `EngineAuthority`, `LegalMoves`, `EffectRegistry`, `CardEffectMap` | Snapshot + Legalität |
| Verben | `Game/SeedRules`, `DownloadRules`, `TurnExpiry`, `DualAffiliationRules`, `CardKinds`, `CardIcons`, `CardFactory`, `CardLifecycle` | Seed, Tent/SD, EOT-Bag, Modes |
| Persistenz | `Services/GameSave.cs` | `.stsave` Schema 2 |
| Deck | `DeckBuilderWindow` + `Game/DeckPlacementRules` + `Services/DeckService` | `.stdeck` v2, Side Decks, Cryo-Quota |

Konvention: neue Expansionskarte = JSON + ggf. eine Registry-Zeile. Neues *Verb* (Download, Flip HA, Tactic) nur einmal.

---

## Ist-Stand (Code, nicht Wunsch)

**Fertig genug zum Spielen (Premiere-Kern)**  
Phase 0/1 Deck Builder · Seed Doorway→Mission→Dil/Art→Facility · Spaceline P1 unten / P2 oben · Report / Staffing / RANGE / Beam (Treaty auf Schiff) · Mission Attempt (OR/xN + Espionage) · Ship + Personnel Battle (Reveal-Overlay, Escape Pod) · Dock/Undock + Spacedock · Stopped / Repair · Equipment-Modifier · Treaties · Interrupt-Stack · Premiere-Kataloge Dil/Art/Event/Interrupt · Reveal / AskChoice · Action History · `.stsave` / `.stdeck` unter Data/SaveGames + Data/Decks · Quick Game · Gaps/Q-Net Span · Rogue Borg / Crosis / Lore Returns · WNOHGB Ring-RANGE (nur TABLE) · Incoming Message Required Move · Icons `Assets/Icons` · 100-Punkte-Sieg

**Engine**  
Authority für Play / Attempt / Encounter / Fly / Beam / Download / HA-Flip / Seed · LegalMoves pro Spieler + `CollectBoth` · Fly mit RANGE-Pfad · Beam-Vorschlag von Schiff **und** Facility **und** Mission · Seed über `SeedRules` · Until-EOT-Bag nur finishing player · Stopped im Snapshot

**AU / Katalog 2026-08-26**  
Drucktexte PR Events/Interrupts/Artifacts · Apply: Neural Servo, Distortion, Tachyon+Cloak, Anti-Time, Sanctuary EOT · AU Events (Yellow Alert, Baryon, Klim, Thermal, Captain's Log, Lower Decks, Particle Scatter, Intruder FF, Wartime) · AU Dil Apply: Edo −10 / Lock, Conundrum chase, Frame 3-3-3 · Dual-Affiliation Foundation (Rakal / DeSeve)

**Foundation-Slots (F1–F10)**

| ID | Thema | Status |
|----|--------|--------|
| F1 | CardKinds / CardIcons | da |
| F2 | InstanceId / Owner / Controller / FaceUp | da (`Card.cs` in Phase0 **und** Phase2) |
| F3 | CardStatus / CardZone Enums | da; UI-Dicts noch parallel |
| F4 | Save additiv | Schema 2; Stack-Fenster im Save noch dünn |
| F5 | EOT-Bag + TurnScope | da; Off-Turn-Instants Hook leer |
| F6 | Download / Special Download | Verb + UI |
| F7 | Hidden Agenda Flip | Verb + UI; Response-Fenster später |
| F8 | Side-Deck-Zonen | UI da; Battle Bridge / Sites / Q nicht gespielt |
| F9 | Owner ≠ Controller | Feld da; Lore Returns / Servo nutzen es |
| F10 | Win 100 / Raise the Stakes | CheckVictory da |

---

## Katalog → Templates

`CardEffectMap` + `EffectRegistry`: Name → Template (`nullify-stack`, `attach-eot`, `ship-mod`, …).  
`*Rules` bleiben Parameter-Kataloge. TableWindow-`if (name == …)` nur löschen, wenn Template + UI-Apply stehen.

---

## Daten / VS

- Live-Projekt: `C:\Dev\StarTrekCCG\…` und hier `artifacts/StarTrekCCG/`  
- Kartenbilder: PC `Data/PR`, `Data/Alternate_Universe` bzw. `C:\STCCG_Data`; hier nur `cards.json`  
- `Card.cs` = `StarTrekCCG/Models/Card.cs` (`FramedOfMind`, `FrameSkills`, `CurrentAffiliation`)

---

## Offen (ehrlich)

- `CollectOffTurn` (Instants ohne Stack) noch leer  
- Dual-Personnel-Karten (6.3.4)  
- Board-Modell (`ENGINE.md`) noch nicht im Code — Ort lebt in der UI  
- Separate Away Team nach Affiliation-Switch auf dem Planeten  
- Side-Deck Typ-Limits (Tent 13, Site 6)  
- `.stsave` ohne volles Stack-Fenster  
- Personnel-Battle-UX Feinschliff  
- Netz, KI, Borg, Sites, Tactics
