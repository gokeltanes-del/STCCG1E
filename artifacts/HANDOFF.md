# STCCG 1E - Handoff

Last updated: 2026-09-09 (Captain — local commits; Pepsch pushes)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: `master`.** GrokTest nicht nutzen.
**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Gruen-Test. Pepsch: VS master, lokale Tips testen, dann Push.

## Current tip (2026-09-12)
Response Window Umbau (Stilles Window am Phase-Banner, Think Tray via [R]/Klick, Optionen 2s/3s/5s/10s, Presets Hotseat/Test).
Dateien: `StarTrekCCG/Game/TimingRules.cs`, `StarTrekCCG/TableWindow.xaml`, `StarTrekCCG/TableWindow.xaml.cs`.

Artifact Beaming ohne Treaty (`TreatyRules.CanOccupyHost` / `ReportingRules.AreCompatible`).
Pepsch green bestätigt: Hyper-Aging, Firestorm Kills + Continue, Overlay `EFFECT - attempt continues`, AT-Detail Debuff-Gruppierung, Varon-T Looten.

### ACTIVE
Premiere-Dilemmas einzeln. Persist/Battle extract deferred.

### Pending Pepsch
- Test Response Window UX (Silent Badge am Banner, [R] Think Tray, [Space] Pass, Optionen/Presets)
- Test Artifact Beaming (Varon-T vom Planeten auf Schiff beamen ohne Treaty-Fehlermeldung)

### Parked
Hugh Borg Ship; IM FindMission; dump@Gaps; Distortion; Parasites Hotseat-UI; REM Fatigue; Cure-Present-Scope Ship

## Workflow Pepsch
1. Unten links **master**
2. Git → Pull
3. Rebuild / starten
4. 3 lokale ungecommitete Dateien: nicht verwerfen wenn du sie brauchst; Pull kann Konflikte in Docs machen

## Docs map
HANDOFF · PROJECT · ENGINE · CODE_PLACEMENT · FEATURES · CARD_TRACKER · CHANGELOG
