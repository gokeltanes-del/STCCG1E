# STCCG 1E — Projekt

Private, nicht-kommerzielle C# / .NET 8 / WPF-App für Star Trek Customizable Card Game First Edition.
Hotseat und Netz (Host = P1 autoritativ, Gast = P2) sind im Bau. Später Gegner-KI.

Repo (nur lesen, nur Pepsch schiebt): https://github.com/gokeltanes-del/STCCG1E
**Stand dieser Datei:** 2026-10-02
**Code-Stand GitHub `master`:** `4ec69fe`

Diese Datei beschreibt das Repo, die Arbeitsorte und **welche Markdown-Datei wofür da ist**.
Kein Tip-Protokoll, keine Erledigt-Liste, kein Implementierungsablauf.

---

## Arbeitsorte

| Ort | Gilt |
|-----|------|
| Josef, lokal | Wahrheit für laufenden Code und Commits: `C:\Dev\StarTrekCCG\StarTrekCCG`. Tippen und committen nur dort. GitHub nur lesen. Nur Pepsch schiebt nach Grün-Test. Ein Branch: `master`. Kratzdateien nur `GROK_TEMP`. Programmdatei: `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` (keine `_build_*`). |
| Online-Kopie / dieser `artifacts/`-Baum | Dokumente und diese Kopie. `artifacts/StarTrekCCG/` ist **kein** Live-Pfad. Änderungen hier nicht als Josef-Tip ausgeben. |

Quellbaum (VS / Git): `StarTrekCCG/` am Repo-Root.
Karten-JSON und Bilder: Lackey-Sets, oft `C:\STCCG_Data` / `GamePaths`, plus `StarTrekCCG/Data` und `StarTrekCCG/Assets`.
Karten-Scope: Premiere zuerst. Andere Expansions nur nach Pepsch, Liste aus den Set-JSON-Dateien.

---

## Prompt und Ablauf

Instruction Prompt: `INSTRUCTION.md`.
Online-Lieferform (vollständige Dateien): `ONLINE_WORKFLOW.md`.
Ablauf neuer Arbeit: `IMPLEMENT.md`.
Aktueller Brückenstand: `HANDOFF.md`.

---

## Markdown — eine Tatsache, eine Datei

| Datei | Korb | Inhalt |
|-------|------|--------|
| `PROJECT.md` | Statisch | Diese Übersicht |
| `INSTRUCTION.md` | Statisch | Instruction Prompt |
| `ONLINE_WORKFLOW.md` | Statisch | Lieferform Online-Kopie |
| `UEBERGABE_PROMPT.md` | Statisch | Verweis auf `INSTRUCTION.md` |
| `IMPLEMENT.md` | Statisch | Wie eine Karte, Regel oder ein Feature gebaut wird |
| `ENGINE.md` | Statisch | Ist-Landkarte der `.cs`-Dateien |
| `TABLEWINDOW_INVENTORY.md` | Statisch | Ist von `TableWindow.xaml.cs` und Verdrahtung |
| `FEATURES.md` | Status | Rangfolge der Themen |
| `RULES_CHECKLIST.md` | Status | Compendium-§ → Fortschritt |
| `GLOSSARY_COVERAGE.md` | Status | Einziger Glossar-Tracker |
| `APPENDIX_A_COVERAGE.md` | Status | Appendix-A-Errata |
| `CARD_TRACKER.md` | Status | Karte → `unknown` / `not-started` / `partial` / `working` / `blocked` |
| `CHANGELOG.md` | Log | Was spielbar geändert wurde |
| `HANDOFF.md` | Brücke | Nur jetzt aktiv, zuletzt, offen, geschlossen |

Regelbuch-Norm: `artifacts/rules/Compendium_Rulebook.pdf` (2.7.4).
Lookup-Reihenfolge steht in `IMPLEMENT.md` und `INSTRUCTION.md`.

Nicht anlegen: `PROJECT_STATUS.md`, `RULES.md`, `CODE_PLACEMENT.md`, `GLOSSARY_WELLE1.md`, `BOARD_MODEL.md`, `ENGINE_FOUNDATION.md`, `BOTS.md`.

---

## Code-Baum (kurz)

```
StarTrekCCG/
  TableWindow.xaml(.cs)        Tisch
  DeckBuilderWindow.*          Deckbau
  MainMenuWindow.*             Startbildschirm & integrierte Match-Lobby
  Models/                      Card, Deck
  Game/                        LegalMoves, EngineAuthority, *Rules, Board/
  Network/                     TCP/JSON Host-Gast
  Services/                    Database, Save, Session
  Data/  Assets/  Decks/
```

Suche nach Mechanik: `ENGINE.md`, dann `TABLEWINDOW_INVENTORY.md`, dann Kommentare `Rule:` / `Glossary:` / `Verb:` im Code.

---

## Nicht jetzt

Eigene KI-Gegner, Sites und Tactics vollständig, Borg als Volk, Mirror, Big-Bang-Split der Tischdatei.

Archivierte Starter-Bäume (`Phase0_Starter` und ähnlich) nicht editieren.
