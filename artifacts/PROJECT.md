# STCCG 1E — Projekt

Private, nicht-kommerzielle C# / .NET 8 / WPF-App für Star Trek Customizable Card Game First Edition.  
Zuerst Hotseat, später Netz und Gegner-KI.

Repo (nur lesen, nur Pepsch schiebt): https://github.com/gokeltanes-del/STCCG1E  
**Stand dieser Datei:** 2026-09-23

Diese Datei beschreibt das Repo, die Arbeitsorte und **welche Markdown-Datei wofür da ist**.  
Kein Tip-Protokoll, keine Erledigt-Liste, kein Implementierungsablauf.

---

## Arbeitsorte

| Ort | Gilt |
|-----|------|
| Josef, lokal | Wahrheit für laufenden Code und Commits: `C:\Dev\StarTrekCCG\StarTrekCCG`. Tippen und committen nur dort. GitHub nur lesen. Nur Pepsch schiebt nach Grün-Test. Ein Branch: `master`. Kratzdateien nur `GROK_TEMP`. Programmdatei: `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` (keine `_build_*`). |
| Online-Kopie / Cursor / `artifacts/` | Dokumente und diese Kopie. `artifacts/StarTrekCCG/` ist **kein** Live-Pfad. Änderungen hier nicht als Josef-Tip ausgeben. |

Quellbaum (VS / Git): `StarTrekCCG/` am Repo-Root.  
Karten-JSON und Bilder: Lackey-Sets, oft `C:\STCCG_Data` / `GamePaths`.  
Karten-Scope: Premiere zuerst. Andere Expansions nur nach Captain oder Pepsch, Liste aus den Set-JSON-Dateien.

---


## Online-Einzel-KI (Grok / Cursor)

Wenn Pepsch mit **einer** Online-KI weiterarbeitet (kein Bot-Team):

1. Startprompt: \rtifacts/UEBERGABE_PROMPT.md2. Arbeitsregeln Online: \rtifacts/ONLINE_WORKFLOW.md3. Rollen in einem Kopf: \BOTS.md\ → Gemeinsame Regeln + Gesamtbeschreibung
4. **Jede** geänderte \.cs\ / \.md\ wird **vollständig** in den Projektordner geschrieben (kein Patch-only als Lieferform).

Repo: https://github.com/gokeltanes-del/STCCG1E — lokale Wahrheit Josef \C:\\Dev\\StarTrekCCG\\StarTrekCCG\. Push nur Pepsch.


## Bot-Team oder eine KI

Rollen-Prompts: `BOTS.md`.

- **Bot-Team:** Jeder Chat eine Rolle. Gemeinsame Regeln plus eine Einzelbeschreibung aus `BOTS.md`.
- **Eine KI:** Gemeinsame Regeln plus Gesamtbeschreibung in `BOTS.md`. Eine Stimme, intern fünf Rollen, Ablauf in `IMPLEMENT.md`.

| Rolle | Tut |
|-------|-----|
| Captain | Ziele, Freigabe, Pepsch, Handoff/Projekt/Bots |
| Data | Einziger C#-Implementierer |
| Spock | Gültige Regel aus dem Compendium-PDF, Ist gegen Soll |
| Seven | Features + Coverage-Status |
| Jadzia | Nur `CARD_TRACKER.md` |

---

## Markdown — eine Tatsache, eine Datei

| Datei | Korb | Inhalt |
|-------|------|--------|
| `PROJECT.md` | Statisch | Diese Übersicht |
| `BOTS.md` | Statisch | Prompts |
| `IMPLEMENT.md` | Statisch | Wie eine Karte, Regel oder ein Feature gebaut wird |
| `ENGINE.md` | Statisch | Ist-Landkarte der `.cs`-Dateien |
| `TABLEWINDOW_INVENTORY.md` | Statisch | Ist von `TableWindow.xaml.cs` und Verdrahtung |
| `EXTRACT_REST.md` | Status | Was noch aus der Tischdatei gezogen wird |
| `FEATURES.md` | Status | Seven: Rangfolge der Themen |
| `RULES_CHECKLIST.md` | Status | Compendium-§ → Fortschritt |
| `GLOSSARY_COVERAGE.md` | Status | Einziger Glossar-Tracker |
| `APPENDIX_A_COVERAGE.md` | Status | Appendix-A-Errata |
| `CARD_TRACKER.md` | Status | Karte → `unknown` / `not-started` / `partial` / `working` / `blocked` |
| `CHANGELOG.md` | Log | Jeder schreibt, was spielbar geändert wurde |
| `HANDOFF.md` | Brücke | Nur jetzt aktiv, zuletzt, offen, geschlossen |

Regelbuch-Norm: `artifacts/rules/Compendium_Rulebook.pdf` (2.7.4).  
Lookup-Reihenfolge steht in `IMPLEMENT.md` und bei Spock in `BOTS.md`.

Nicht anlegen: `PROJECT_STATUS.md`, `RULES.md`, `CODE_PLACEMENT.md`, `GLOSSARY_WELLE1.md`, `BOARD_MODEL.md`, `ENGINE_FOUNDATION.md`.

---

## Code-Baum (kurz)

```
StarTrekCCG/
  TableWindow.xaml(.cs)     Tisch
  DeckBuilderWindow.*       Deckbau
  Models/                   Card, Deck
  Game/                     LegalMoves, EngineAuthority, *Rules, Board/
  Services/                 Database, Save, Session
```

Suche nach Mechanik: `ENGINE.md`, dann `TABLEWINDOW_INVENTORY.md`, dann Kommentare `Rule:` / `Glossary:` / `Verb:` im Code.

---

## Nicht jetzt

Netz, eigene KI-Gegner, Sites und Tactics vollständig, Borg, Mirror, Big-Bang-Split der Tischdatei.

Archivierte Starter-Bäume (`Phase0_Starter` und ähnlich) nicht editieren.
