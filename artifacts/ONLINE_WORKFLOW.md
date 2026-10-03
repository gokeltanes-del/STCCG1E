# Online-Arbeitsregeln

**Stand:** 2026-10-02

Gilt für Grok-Projekte, Cursor und ähnliche Online-Arbeitsplätze. Die lokale Wahrheit für Code und Commits bleibt Josef.

## Wo liegt was

| Was | Pfad |
|-----|------|
| Repo (GitHub, lesen; Push nur Pepsch) | https://github.com/gokeltanes-del/STCCG1E |
| Wahrheit Code + Commits (Josef) | `C:\Dev\StarTrekCCG\StarTrekCCG` |
| Branch | `master` |
| Quellcode | `StarTrekCCG/` |
| Dokumente | `artifacts/*.md` |
| Regelbuch-PDF | `artifacts/rules/Compendium_Rulebook.pdf` (2.7.4) |
| Kratz/Smoke | `GROK_TEMP/` |
| Debug-EXE | `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` |

Diese Online-Kopie ist kein Josef-Pfad. Push macht nur Pepsch.

## Zuerst lesen

1. `HANDOFF.md` (oberster aktiver Block)
2. `PROJECT.md`
3. `INSTRUCTION.md`
4. Diese Datei
5. `IMPLEMENT.md`
6. Je Aufgabe: `ENGINE.md`, `CARD_TRACKER.md`, `CHANGELOG.md`, Coverage, PDF

## Pflicht: vollständige Dateien

Keine Patch-only-Antwort als einzige Lieferform.

1. Pfad relativ zum Projektroot nennen.
2. Datei vollständig schreiben (Overwrite) oder als komplette Datei ausgeben.
3. Kein „… rest unchanged …“ als Ersatz, außer Pepsch fordert einen Diff.
4. Nach Tip, vor dem Test: `CHANGELOG.md` und kurzer Block in `HANDOFF.md`. Karte höchstens `partial`.
5. Nach Pepsch-Grün: Tracker `working`, Checkliste, Glossar, Appendix A, bei großem Thema `FEATURES.md`, bei neuem Code-Ort `ENGINE.md` / `TABLEWINDOW_INVENTORY.md`. Genau wie `IMPLEMENT.md` Abschnitt „Nach erfolgreichem Test“.

## Ablauf

Wie `IMPLEMENT.md`: Typ → Phrase → Kartenrest. Soll aus dem PDF. Vor Code ein Satz System-Ort. Entscheiden in `Game/*Rules`, nicht nur `if (name == …)` in TableWindow.
