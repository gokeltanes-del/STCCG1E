# STCCG 1E — Projekt

Private, nicht-kommerzielle C# / .NET 8 / WPF-App (Hotseat zuerst, später Netz + KI).  
Repo: https://github.com/gokeltanes-del/STCCG1E  
**Stand:** 2026-09-19

## Docs-Wahrheit

- **Resume / Bot-Handoff:** `artifacts/HANDOFF.md` (jeder neue Chat zuerst)
- **Projekt-Überblick:** `artifacts/PROJECT.md` (diese Datei)
- `PROJECT_STATUS.md` (Repo-Root) ist **entfernt** — nicht wieder anlegen

Canon unter `artifacts/`:

| Datei | Inhalt | Owner (Team) |
|-------|--------|--------------|
| HANDOFF.md | Current tip, Smoke, Workflow | Captain + Data (tip) |
| PROJECT.md | Architektur, Ist/Offen, Team | Captain + Data |
| ENGINE.md | Board + Foundation | Data |
| CODE_PLACEMENT.md | Wo neuer Code hingehört | Data |
| TABLEWINDOW_INVENTORY.md | TW-Extract-Inventar | Data |
| FEATURES.md | Feature-Backlog / Coverage-Hinweise | Seven |
| CARD_TRACKER.md | Premiere+AU Kartenstatus | Jadzia |
| RULES.md | Fix-Protokoll A/B/C | Spock + Team |
| RULES_CHECKLIST.md | Checklist-Zellen | Spock / Seven |
| GLOSSARY_COVERAGE.md | Glossary-Coverage | Seven / Spock |
| GLOSSARY_WELLE1.md | Welle-1 Notes | Spock / Seven |
| EXTRACT_REST.md | TW-Extract Rest | Data / Captain |
| CHANGELOG.md | Playable Changes | Data (Zeile) + Team |

Regelbuch: `artifacts/rules/Compendium_Rulebook.pdf` (2.7.4).  
Lookup: Checklist → Glossary → Temporary Rulings → Appendix A Errata → Appendix B.

## Team

| Agent | Rolle |
|-------|--------|
| **Captain** | Goals, Go/Stop, Docs-Koordination, Pepsch-Kontakt |
| **Data** | Alleiniger Code-Implementierer (Rules/Engine/UI) auf Josef: tip+commit, **nie push**; Pepsch testet/pusht |
| **Spock** | Rules Ist/Soll + Glossary-Quellen |
| **Seven** | FEATURES-Backlog + Glossary/Compendium Coverage |
| **Jadzia** | CARD_TRACKER (Premiere+AU) |

**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Grün-Test.  
**Ein Branch: master.**

## Code-Root (wichtig)

**Quellbaum (VS / Git):** `StarTrekCCG/` am **Repo-Root**  
(`C:\Dev\StarTrekCCG\StarTrekCCG\StarTrekCCG\` für das .csproj).  

**Nicht** `artifacts/StarTrekCCG/` — das ist kein Live-Code-Pfad.

**Pepsch Default-EXE (nicht Side-Builds `_build_*`):**  
`C:\Dev\StarTrekCCG\StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`

Daten: Lackey → Sets → `cards.json` + Bilder (`GamePaths` / oft `C:\STCCG_Data`).  
Karten-Scope: **Premiere zuerst**; AU nur wo verdrahtet.

## Programmstruktur (Kurz)

```
StarTrekCCG/                    # C# / .NET 8 / WPF
  TableWindow.xaml[.cs]         # Tisch = View + Input + Apply-UI
  DeckBuilderWindow.xaml[.cs]
  Models/                       # Card, Deck, …
  Game/                         # Regeln + Engine (kein WPF)
    GameState, EngineAuthority, LegalMoves, EffectRegistry, *Rules
  Services/                     # CardDatabase, DeckService, GameSave, GameSession
  Assets/, Data/Sets/, Data/Decks/, Data/SaveGames/
```

Suche: Kartenfelder → `Models/Card.cs` · Legalität → `LegalMoves` + `EngineAuthority` · Effekttext → `Game/*Rules` · UI → `TableWindow.xaml.cs`.

Neue Karte/Verb: Struktur nach `CODE_PLACEMENT.md` (Rules, nicht TW-Big-Bang).

## Architektur

```
UI (TableWindow) → GameAction → EngineAuthority(GameState)
                                  → *Rules / EffectRegistry
LegalMoves.Collect / CollectBoth  = gleiche Quelle Hotseat / Netz / KI
```

Board 0–6 + Foundation E1–E6: siehe `ENGINE.md` (fertig). Dual-Run neben UI-Dicts. Kein Big-Bang-TW-Split.

## Fix-Protokoll (kurz)

Ist/Soll → Verb → Lookup Checklist/Glossary/Errata → Klasse A Karte | B Phrase | C Grundlage  
→ kleinste Datei → CHANGELOG + Checklist-Zelle.  
Antwort: Klasse, Dateien, bewusst nicht.

Standing Practice (FEATURES): Decide/Apply mit Glossary/Compendium-Kommentar; Detail/Status nennt die Regel.

## Als Nächstes (siehe HANDOFF)

1. Pepsch Grün-Tests / Push für lokale Josef-Tips (Holo gates, Fingernail, …).  
2. TableWindow-Extract schrittweise (EXTRACT_REST / Inventory) — Premiere-first.  
3. Weitere Premiere-Karten einzeln nach Captain Go.  
4. Persist/Battle-Foundation deferred bis Captain sagt.

**Nicht jetzt:** Netz, KI, Sites/Tactics voll, Borg, Mirror, Big-Bang-UI.

## Archiv / Altstand

`Phase0_Starter` / `Phase1_Starter` / `Phase2_Starter` und ähnliche Starter-Bäume: **archiviert — nicht editieren.**  
Ältere „Phase“-Warnungen in alten Chats gelten nicht mehr als Live-Pfad.