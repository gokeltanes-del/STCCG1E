# Star Trek CCG 1E – Private App – PROJECT STATUS

**Last updated:** 2026-08-15  
**Current Phase:** 0 – Data Models + JSON Loader + first display

---

## COPY-PASTE BLOCK FOR NEW CHAT (immer aktuell halten)

```
Projekt: Star Trek CCG 1E private C# WPF App (nicht-kommerziell)
Aktuelle Phase: 0 – Models + CardDatabase + einfache Anzeige einer Karte
Tech: C# / .NET 8 / WPF / Visual Studio 2022 / CommunityToolkit.Mvvm (geplant)
Daten: Lackey Physical.txt + Virtual.txt → split_lackey_sets.py → pro Set ein Ordner mit cards.json + Bilder (Set_Type_Name.jpg)
Ziel Phase 0: Karte aus JSON laden und in einem Fenster anzeigen (Name, Typ, Bild, Text)
Nächster Schritt nach Phase 0: Phase 1 – Deck Builder UI
AI-Plan: Hybrid (Rules Engine für legale Züge + lokales LLM via LM Studio / Qwen3.6 für Strategie). Später API austauschbar.
GitHub: [hier deinen Repo-Link eintragen, sobald vorhanden]
Wichtige Dateien: Models/Card.cs, Services/CardDatabase.cs, PROJECT_STATUS.md
Bitte setze den aktuellen Stand fort.
```

---

## 1. Projekt-Übersicht

- **Name:** StarTrekCCG (Arbeitsname)
- **Zweck:** Privater Deck-Builder + Table-Simulator + Hotseat für Star Trek CCG 1E. Später AI-Gegner und Online.
- **Nicht kommerziell.** Nur für privaten Gebrauch.
- **Tech-Stack:**
  - C# / .NET 8
  - WPF (Windows)
  - Visual Studio 2022
  - MVVM (CommunityToolkit.Mvvm)
  - JSON für Kartendaten
  - Später: lokales LLM (LM Studio + Qwen3.6) über OpenAI-kompatible API

## 2. Datenquelle

- Original: LackeyCCG Plugin (Physical.txt + Virtual.txt + setimages)
- Verarbeitung: `artifacts/split_lackey_sets.py`
- Ergebnis pro Expansion: Ordner mit `cards.json` + umbenannten Bildern (`Set_Type_CardName.jpg`)
- Empfohlener Output-Pfad auf dem PC: `C:\STCCG_Data\` (oder ähnlich)

## 3. Aktueller Stand (Phase 0)

- [ ] GitHub Repository erstellt
- [ ] Neues WPF-Projekt in VS2022 angelegt
- [ ] Ordnerstruktur Models / Services / ViewModels / Views / Data
- [ ] Card-Modell implementiert
- [ ] CardDatabase-Service (lädt alle cards.json)
- [ ] Einfache Test-Anzeige einer Karte (Name + Bild + Text)
- [ ] PROJECT_STATUS.md ins Projekt kopiert

## 4. Architektur-Entscheidungen

- Daten bleiben flach in JSON (kein SQL nötig am Anfang)
- Card-Klasse ist bewusst erweiterbar (später Attributes, Skills, Icons als eigene Typen)
- CardDatabase lädt lazy / einmalig alle Sets
- Für AI später: GameState → JSON → LLM → Action-JSON

## 5. Nächste Schritte (nach Abschluss Phase 0)

1. Phase 1: Deck Builder (Liste + Filter + Deck speichern/laden)
2. Phase 2: Sandbox Table (Karten auf den Tisch legen, bewegen)
3. Phase 3+: Rules Engine inkrementell (Missionen, Dilemmas, Battles …)

## 6. Bekannte offene Punkte

- User muss den split_lackey_sets.py noch einmal auf dem eigenen Rechner laufen lassen, falls noch nicht geschehen.
- Bildpfade relativ zum cards.json speichern.
- Git von Anfang an nutzen.

---

**Hinweis für Grok:**  
Bei jedem größeren Schritt diesen Status aktualisieren und den COPY-PASTE BLOCK anpassen.
