# Compendium-Umsetzung — Prozess

**Quelle:** `rules/Compendium_Rulebook.pdf` (2.7.4, August 2026)  
**Feinliste:** `RULES_CHECKLIST.md` (Abschnitte x.x / x.x.x)  
**Karten-Scope:** Premiere zuerst; AU nur wo schon verdrahtet  
**Stand Prozess:** 2026-09-02  
**Board-Soll:** `BOARD_MODEL.md` (Spaceline/Location/Force). Kein Big-Bang; Dual-Run.

Legende Checklist: ✅ spielbar · 🟡 teilweise · ❌ offen · ➖ außerhalb Premiere / später

---

## Rollen der Dateien

| Datei | Aufgabe | Nicht tun |
|-------|---------|-----------|
| `PROJECT.md` | Architektur, Quellbaum, Chat-Startblock, „nicht jetzt“ | Keine Bugstories |
| `RULES.md` | **Dieses** Fix-/Karten-Protokoll + Lookup | Keine Changelog-Zeilen |
| `RULES_CHECKLIST.md` | Compendium 2.7.4 auf x.x.x **inkl. Sidebars**, Status + Code-Datei | Keine Implementierungsdetails |
| `GLOSSARY_COVERAGE.md` | Glossary A–Z, gleiche Status-Logik | Kein Engine-C# |
| `APPENDIX_A_COVERAGE.md` | Appendix A Errata, gleiche Status-Logik | Errata ≠ „Karte working“ |
| `CHANGELOG.md` | Eine Zeile pro **spielbarer** Änderung + §-Nummer | Keine Chat-Metadaten |
| Compendium-PDF | Norm. Glossary / Temp. Rulings / App. A Errata / App. B | Nicht aus dem Gedächtnis zitieren |

---

## Klassifikation (A / B / C)

Jedes Problem und jede neue Karte bekommt **eine** Klasse. Der Agent entscheidet das selbst; der Nutzer widerspricht nur bei Fehlklassifikation.

| Klasse | Wann | Wohin der Fix |
|--------|------|----------------|
| **A · Karte** | Nur dieser Druckname / dieser eine Text | `*Rules`-Katalogzeile oder `CardEffectMap`-Zeile |
| **B · Phrase** | Gleicher Satzbau auf mehreren Karten („Plays on…“, „nullifies…“, OR-Wahl) | Parser / Template (`PlayOnRules`, `AskChoice`, Effect-Template) |
| **C · Grundlage** | Compendium-Satz unabhängig vom Kartennamen (initiate battle, present, exposed, Federation attack) | `BattleRules` / `TimingRules` / `LegalMoves` / `MovementRules` / … |

**Upgrade:** Dieselbe Lücke zum **dritten** Mal (gleiche Phrase oder gleiche Checklist-Zeile) → A wird B, B wird C. Nicht vorher ein neues Verb erfinden.

**Nicht mitziehen:** Geschwister-Kapitel nur, wenn **derselbe Satz** gilt. 7.4.1 (Initiate) fixen heißt nicht 7.9 Infiltrate bauen. Premiere-only.

UI (Layout, Copy, Overlay, History) ist **kein** A/B/C — nur `TableWindow`, Checklist unberührt.

---

## Ablauf: Bug aus dem Probespiel

```
1. Ist / Soll in einem Satz.
2. Verb benennen: Seed | Play | Report | Move | Attempt | Battle |
   Nullify | Timing | Discard | Status (stopped/disabled).
3. Lookup (Pflicht, in dieser Reihenfolge):
     a. RULES_CHECKLIST.md  → § x.x.x + Status
     b. Glossary-Lemma      → Kartenname UND Begriff
        (exposed, present, here, initiate battle,
         affiliation attack restrictions, in play, nullify, …)
     c. TEMPORARY RULINGS
     d. APPENDIX A: ERRATA  → gilt der gedruckte JSON-Text noch?
     e. APPENDIX B          → nur wenn der § kürzlich geändert wurde
4. Klasse A / B / C. Datei wählen (siehe Tabelle unten).
5. Kleinster Fix. Engine nur bei wiederholtem Loch oder fehlendem Verb.
6. CHANGELOG eine Zeile mit § und Klasse.
7. Checklist-Zelle anpassen (❌→🟡 oder 🟡→✅), nicht „nebenbei grün“.
```

Antwortformat im Chat (kurz):

```
Klasse: B · Phrase
§: 7.4.1 + Glossary „affiliation attack restrictions“
Dateien: Game/BattleRules.cs, RULES_CHECKLIST.md
Nicht angefasst: 7.9 / Borg
```

---

## Ablauf: neue Karte (Premiere / schon verdrahtetes AU)

```
1. JSON-Text (Name, Type, Icons, Gametext).
2. Glossary-Eintrag dieses Namens + Errata.
3. Host aus Text: zuerst PlayOnRules.Parse.
4. Effekt:
     — trifft bestehendes Template (nullify-inplay, ship-mod, …)
       → nur Map-Zeile + Katalog-Parameter
     — neuer Persist, aber altes Verb
       → eine Katalogzeile in *Rules
     — neues Verb (Download-Art, Flip, Scout, …)
       → nur wenn Checklist das Verb noch ❌ hat
         UND Premiere es braucht. Sonst zurückstellen.
5. LegalMoves muss die Aktion listen können (auch off-turn bei Interrupts).
6. Test: eine Situation, nicht die ganze Engine.
```

---

## Wohin der Code

| Symptom | Erste Datei |
|---------|-------------|
| Klick / Drop / Overlay / Text im Detail | `TableWindow.xaml(.cs)` |
| Darf ich das jetzt? | `LegalMoves.cs` + `EngineAuthority.cs` |
| Drucktext / Persist / Countdown | `EventRules` `InterruptRules` `DilemmaRules` `ArtifactRules` |
| Plays on / exposed / ship≠outpost | `PlayOnRules.cs` |
| Report / Mix / Dual-Affil | `ReportingRules` `DualAffiliationRules` `TreatyRules` |
| Fly / Beam / Staff / Dock | `MovementRules.cs` |
| Ort / Crew / Snapshot | `Game/Board/BoardStore.cs` `GameState.cs` (`ENGINE_FOUNDATION.md`) |
| Initiate / Affiliation / Damage | `BattleRules.cs` |
| Stack / Response / just played | `TimingRules.cs` |
| Skills / Phaser / Kits | `ModifierRules.cs` `MissionRules.cs` |
| Name → Template | `CardEffectMap.cs` `EffectRegistry.cs` |
| Seed-Reihenfolge / Cryo | `SeedRules.cs` |

Kein zweites Fundamentalsystem. `TableWindow` wendet an, was Authority erlaubt.  
Foundation-Schritte E1–E6: `ENGINE_FOUNDATION.md` — immer Klasse C, ein Chat ein Schritt.

---

## Lookup-Begriffe (Glossary, oft)

`actions` (just / interrupting / required / three steps) · `affiliation attack restrictions` · `in play` · `present` · `here` · `aboard` · `exposed` · `cloaked` · `docked` · `nullify` · `disabled` · `stopped` · `damage` · `repair` · `report` · `seed` · `spaceline` · `location` · `uniqueness` · `persona` · `treaty` · `downloads` · `once per game` · `cumulative` · `colon rule`

Kartenname **immer** zusätzlich nachschlagen (Errata ändert Drucktext).

---

## Was bewusst nicht passiert

- Compendium von 1.1 bis Ende „einführen“, bevor Premiere steht.
- Borg-only, Sites-Nor, Tactics-Deck, Mirror, Time Travel — ➖ bis Scope wechselt.
- Drei thematisch benachbarte Kapitel auf Vorrat, weil ein Bug in einem lag.
