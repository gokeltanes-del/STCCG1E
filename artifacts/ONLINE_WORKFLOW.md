# Online-Einzel-KI (Grok Projekte / Cursor / Chat)

**Stand:** 2026-09-23  
**Owner:** Captain  
**Zweck:** Eine KI ersetzt Captain+Data+Spock+Seven+Jadzia. Gilt für Grok Projekte, Cursor Cloud/IDE und ähnliche Online-Arbeitsplätze.

## Wo liegt was

| Was | Pfad |
|-----|------|
| Repo (GitHub, lesen; Push nur Pepsch) | https://github.com/gokeltanes-del/STCCG1E |
| Wahrheit Code + Commits (Josef) | `C:\Dev\StarTrekCCG\StarTrekCCG` |
| Branch | `master` |
| Quellcode | `StarTrekCCG/` (unter Repo-Root) |
| Dokumente | `artifacts/*.md` |
| Regelbuch-PDF | `artifacts/rules/Compendium_Rulebook.pdf` (2.7.4) |
| Kratz/Smoke | `GROK_TEMP/` (nicht committen wenn nur Temp) |
| Debug-EXE | `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` |

Wenn du in einem **Online-Projektordner** arbeitest: das ist eine Kopie des Repos. Tip/Commit auf Josef macht Pepsch nach Grün-Test. Du schreibst Dateien **vollständig** in diesen Projektordner (siehe unten).

## Zuerst lesen (jeder neue Chat)

1. `artifacts/HANDOFF.md` (nur der oberste aktive Block)
2. `artifacts/PROJECT.md`
3. `artifacts/BOTS.md` → Block **Gemeinsame Regeln** + **Gesamtbeschreibung**
4. `artifacts/ONLINE_WORKFLOW.md` (diese Datei)
5. `artifacts/IMPLEMENT.md`
6. Je Aufgabe: `ENGINE.md`, `CARD_TRACKER.md`, `CHANGELOG.md`, Coverage-Dateien, PDF

Fertiger Startprompt: `artifacts/UEBERGABE_PROMPT.md` (kopieren und bei Bedarf Auftrag anhängen).

## Pflicht: vollständige Dateien ausgeben

Online-KIs dürfen **keine** Patch-only-Antworten als einzige Lieferform nutzen.

Bei jeder Programmänderung oder Doku-Änderung:

1. Nenne den **Pfad relativ zum Repo-Root** (z. B. `StarTrekCCG/Game/TimingRules.cs` oder `artifacts/HANDOFF.md`).
2. Schreibe die Datei **vollständig** in den Projektordner (Overwrite), **oder** liefere im Chat **eine komplette Datei** in einem Codeblock mit Pfad-Hinweis, die Pepsch 1:1 speichern kann.
3. Keine „… rest unchanged …“, keine verkürzten Diffs als Ersatz für die Datei, außer Pepsch fordert ausdrücklich einen Diff.
4. Mehrere Dateien = mehrere vollständige Ausgaben, jede mit Pfad.
5. Nach Tip: `artifacts/CHANGELOG.md` und kurzer Block in `artifacts/HANDOFF.md` mitziehen (ebenfalls vollständig speichern).
6. Kartenstatus: `artifacts/CARD_TRACKER.md` aktualisieren (`partial` bis Pepsch Grün → `working`).

## Ablauf (eine Stimme)

1. Pepsch nennt Karte/Bug/Feature.
2. Du (Spock): Soll aus PDF + Lookup-Reihenfolge in `IMPLEMENT.md`.
3. Du (Data): ein Satz System-Ort vor Code; dann Code in `Game/*Rules` / Timing / LegalMoves — nicht nur `if (name == …)` in TableWindow.
4. Phrase ab zweiter Karte = shared Gate/Service (siehe JustAfter, Plays-on, …).
5. Smoke-Hinweis unter `GROK_TEMP/SMOKE_….md` (vollständig).
6. Pepsch testet lokal auf Josef; Push nur Pepsch.

## Aktueller Stand (2026-09-23 Abend)

Geschlossen u. a.: Honor Challenge; Klingon Death Yell; shared `JustAfter(trigger)`-Gate.  
Park: Continuum/Q; Plays on/as F3 Smoke; AI Freundes-Report Beaming; ETA 21b2d52 Retest; Artifact-Y Load intermittent.  
Letzter Tip-Hash (lokal Josef, ggf. noch nicht gepusht): `c42ec0f`.

## Sprache

Mit Pepsch: Deutsch, vollständige Sätze. Code/Dateinamen: Englisch.
