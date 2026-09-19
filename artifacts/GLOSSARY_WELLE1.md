> **Deprecated (2026-09-19):** Living tracker is `GLOSSARY_COVERAGE.md` (A–Z, Checklist-Format). This file is an archive of Welle-1 Ist/Soll notes; do not maintain it as the coverage matrix.

# Welle 1 — Timing / Actions / Nullify
ACTIVE 2026-09-05 · Seven (Code?) + Spock (Soll + Quellen)  
Parent: `GLOSSARY_COVERAGE.md` · Lookup: Checklist ? Glossary ? Temp Rulings ? App A/B  
Kein Code in dieser Welle.

**Code?-Snapshot Seven FINAL Welle1:** Gaps done; 1-4+6-7 partial; Hugh CanRespond DONE (Dilemma-only); Checklist ??; Batches 1-5 complete

---

## 1. actions (initiation / responses / results)
**Soll:** Jede Action = (1) Initiation ? (2) optional valid responses ? (3) results. Responses nur nach Initiation, bevor Results. Mehrere Responses möglich. Keine fremde Action unterbrechen außer valid response / „suspends play“ / Regel.
**Quellen:** Glossary *actions* / *actions - step 1: initiation* / *actions - step 2: responses* / *actions - step 3: results*; Compendium Actions; Checklist Timing-Stack.
**Code?** partial -- TimingRules.ActionStack + LegalMoves responses (Checklist Timing-Stack, kein Flip)

---

## 2. valid responses
**Soll:** Response nur wenn die Karte die laufende Action **spezifisch** modifiziert/cancelt/nullifiziert/prevented (meist namentlich). Nützlich ? gültig. Beispiel: Hugh = valid response auf Borg-Ship-Dilemma-Battle-Initiation; Temporal Rift ? valid response darauf.
**Quellen:** Glossary *actions - step 2: responses*; Compendium *Valid responses* (Hugh vs Temporal Rift); Checklist „Just / valid response“ ??.
**Code?** partial -- CanRespond whitelist; Hugh CanRespond Dilemma-only DONE; catalog thin

---

## 3. at-any-time
**Soll:** „At any time“-Spiele (z. B. Interrupt) sind **keine** automatic valid responses — nur wenn sie sich spezifisch auf die laufende Action beziehen. Karten mit **suspends play** (und Special Download) dürfen unrelated Actions temporär unterbrechen.
**Quellen:** Glossary *actions - interrupting*, *at any time*, *suspends play*; Compendium Interrupts / Valid responses; Checklist 6.5.1 ??.
**Code?** partial -- CollectOffTurn / IsAnytimeType; Gate vs Response duenn (CL 6.5.1)

---

## 4. nullify
**Soll:**
- **Karte nullify** = cancel + **discard** (Kevin Event, Amanda Interrupt), sofern Text nicht anders.
- **Effekt nullify ohne Karte** möglich (Hugh nullifiziert Attack, Dilemma bleibt).
- **Continuous „while in play, nullifies X“** (Traveler: Transcendence ? SWB): unterdrückt Effekt; Zielkarten dürfen gespielt werden / bleiben liegen; gilt eigene + gegnerische; endet wenn Nullifier leave play.
**Quellen:** Glossary *nullify*; *Hugh*; *The Traveler: Transcendence*; Spock-Ruling SWB 2026-09-05.
**Code?** partial -- Kevin/Devil ok-ish; Hugh attack-cancel Dilemma-only DONE; Traveler continuous partial

---

## 5. Gaps nullify (+ occupants)
**Soll:** Gaps nullified = discarded ? Gap schließt; Cards direkt auf Gaps discard; Schiffe/Cards an der Gaps-Location **sofort** relocate zu **einer** adjacent Spaceline-Location; Wahl = **Spieler der nullifiziert** (nicht Ship-Owner/Controller). Hängen zwischen Missionen = illegal.
**Quellen:** Glossary *Gaps in Normal Space*; Glossary *nullify*; Spock-Ruling 2026-09-05; Fix `bb163ed` Pepsch green.
**Status:** **done** · Code yes (`GapsNullifyRules`)

---

## 6. initiating a battle (Checklist § 7.4.1)
**Soll (Premiere-first):**
- Nur eigener Zug, Execute-Orders (außer Card).
- Target present / same location (Ship-Battle: ships/facilities/Borg Ship dilemma; Personnel: same planet/ship/facility/site).
- Initiating force braucht **Leader** (Leadership skill oder OFFICER); affiliated ship/facility zusätzlich matching personnel aboard (NA/Neutral: compatible).
- Affiliation restrictions: normalerweise nicht eigene Affiliation; **Federation darf niemanden angreifen außer Borg**; Klingon darf Klingon; mixed force trägt restriktivste Member-Restriction (Fed+NA = Fed-Force ? kein Initiate außer Borg).
- Counter-attack (nächster Zug am Attack-Location): kein Leader / keine Affiliation-Restriction nötig; optional.
- Nach Battle: Beteiligte **stopped**.
**Quellen:** Glossary *battle* / *battle - initiating*; Rulebook/Compendium battle; Checklist 7.4.1 ?? (Fed nur vs Borg notiert).
**Code?** partial -- BattleRules Leader + Fed/Borg + wartimeVs partial; Counter-attack gate open (Batch 4)

---

## 7. present / here (Checklist § 12.4)
**Soll:**
- **present (Personnel/Equipment miteinander):** gleiches Crew oder Away Team; „stopped“/disabled/stasis/house arrest = separates Team während des Zuges.
- **present mit Opponent:** gleiche Planet-Oberfläche (außerhalb Facility/landed ship) oder gleiches Ship/Facility/Site.
- **aboard** ˜ present für viele Space-Dilemmas (nur attempting Crew während Attempt).
- **here / there:** im Site-Kontext = an diesem Site; im Spaceline/Timeline-Kontext = **anywhere at that location** (Planet-Oberfläche, Orbit-Ship, Facility dort).
**Quellen:** Glossary *present*; *here*; Checklist 12.4 ??.
**Code?** partial -- Location-Host; Glossary present/here incomplete - deepen Welle 2 (Batch 5)

---

## Deliverable
Seven: `Code?` Spalten + Checklist-Zellen sync.  
Spock: weitere Ist/Soll on demand.  
Data: nur Captain Go. Kein Engine-C# aus dieser Datei.

---

## Ist/Soll-Batches (Timing-Kern) — Spock 2026-09-05

### Batch 1 — Just / valid response + Action stack
**Ist:** `TimingRules.ActionStack` + `CanRespond` / `LegalResponsesInHand` — **Namenkatalog** (Amanda, Kevin, Q2, Schism, Escape Pod, Energy Vortex, Devil, Hugh, Sanctuary). Default: keine Response. Kein generisches „specific-modifies“-Parsing.
**Soll:** Response nur wenn Karte die **laufende** Action spezifisch modifiziert/cancelt/nullifiziert/prevented (Glossary *actions - step 2*). Katalog ok als Premiere-Whitelist, aber Gate-Text/Just muss zur Action passen (nicht nur „Interrupt auf Stack“). Unbekannte Karten: deny bis Whitelist-Eintrag (kein spekulatives Allow).
**Quellen:** Glossary *actions* steps 1–3; Compendium Valid responses; CL Just/valid response ??.
**Engine-Risiko:** Mittel — falsche Positives (Hugh vs falsche Battle-Quelle) schlimmer als Deny.

### Batch 2 — 6.5.1 at-any-time vs gültige Response
**Ist:** `IsAnytimeType` + `CollectOffTurn` listet Anytime-Karten off-turn; `ShouldOpenStackForPlay` öffnet Stack für Interrupt/Event/Doorway/Normal-Play. Trennung „neue Action zwischen Actions“ vs „Response auf Stack-Top“ dünn (CL 6.5.1).
**Soll:** Anytime-Spiel als **eigene** Action zwischen Actions erlaubt (wenn Typ passt). Als Response **nur** wenn specific-modifies Top. Suspends-play / Special Download: unrelated Interrupt ok. Useful ? valid.
**Quellen:** Glossary *at any time*, *actions - interrupting*, *suspends play*; CL 6.5.1.
**Engine-Risiko:** Hoch für Timing-Bugs (Temporal-Rift-während-Battle-Muster).

### Batch 3 — Nullify effect-vs-card (Hugh / Kevin / Traveler)
**Ist:**
- Kevin: `CanKevinTargetEvent` — Event nullify+discard (Shield/Treaty immun laut Kommentar); Stack-Response auf PlayCard; In-Play-Nullify über LegalMoves/`nullify-inplay`.
- Hugh: `CanRespond` auf Ship/Personnel-Battle-Initiation wenn Attacker `[Bor]` / Borg Ship / Rogue Borg — **zu weit** vs Spock-Ruling (Battle-Cancel nur **Borg Ship Dilemma**); Rogue-Zweig ist Location-Destroy, nicht generisch „Borg-Battle cancel“.
- Traveler: Transcendence continuous vs SWB — App-Verhalten = Soll (SWB bleibt, Effekt tot); Code „partial“.
**Soll:**
| Muster | Was | Ziel-Karte |
|--------|-----|------------|
| Kevin | nullify Event | discard Event |
| Amanda | nullify Interrupt | discard Interrupt (bzw. OOP laut SelfDestination) |
| Hugh Attack | nullify **attack** | Dilemma bleibt; targets stopped; rest of turn |
| Traveler continuous | while in play nullifies SWB | SWB bleibt; eigene+gegnerische |
**Quellen:** Glossary *nullify*, *Hugh*, *The Traveler: Transcendence*; Spock Hugh + SWB Rulings.
**Engine-Risiko:** Hoch — Hugh `IsHughBattleSource` Affil-Borg nachziehen (Captain Go ? Data).

### Batches 4–5
Später / Welle 2: `7.4.1` Fed wartime + Counter; `12.4` present/here Hosts.

---

## Ist/Soll-Batches 4–5 — Spock 2026-09-05 (Capt: weiter mit Seven; Hugh parked)

### Batch 4 — § 7.4.1 Initiate / Fed wartime / Counter-attack
**Ist:** `BattleRules.CanInitiateShipAttack` — Leader (OFFICER/Leadership); Affil-Check: Fed-Force nur vs Borg **oder** `wartimeVs` Matching (Wartime Conditions); sonst block. Staffing/WEAPONS grob. Counter-attack / mixed-force-Feinheiten / Personnel-Initiate parallel: Checklist ?? „Counter/mixed offen“.
**Soll:**
- Initiate: eigener Zug Execute-Orders; Leader (bzw. Borg [Def]); matching/compatible aboard; present/same location.
- **Fed:** kein Initiate außer vs Borg, **außer** Karte erlaubt explizit (Wartime Conditions ? named affiliation; Mission/andere Card-Text). Mixed Fed+X = Fed-Force ? gleiche Restriction.
- **Counter-attack** (optional, dein nächster Zug, am Ort des gegnerischen Attacks): **kein** Leader, **keine** Affil-Restriction; neues Battle, keine Continuation.
- Nach Battle: Beteiligte stopped.
**Quellen:** Glossary *battle* / initiating; Rulebook battle restrictions; CL 7.4.1.
**Engine-Risiko:** Mittel — Wartime-Pfad teilweise da; Counter-attack-Gate prüfen/nachziehen nur Capt Go.
**Hugh CanRespond:** DONE Dilemma-only (Data 2026-09-05, Capt Go).

### Batch 5 — § 12.4 present / here (Hosts)
**Ist:** Location-Host / BoardStore-Location; Checklist 12.4 ?? „nicht glossary-vollständig“. Wenig zentrale `IsPresent`/`HereAt`-API in Game/*.
**Soll:**
- Personnel/Equipment **present miteinander** = gleiches Crew/AT; stopped/disabled/stasis/house arrest = separates Team im Zug.
- **Present mit Opponent** = gleiche Planet-Oberfläche (außer Facility/landed ship) oder gleiches Ship/Facility/Site.
- **here** @ Site = dieser Site; @ Spaceline-Location = **anywhere at that location** (Oberfläche + Orbit + Facility).
- Während Mission Attempt: Dilemma „aboard/present“ ˜ attempting Crew — nicht Intruder/disabled/HA.
**Quellen:** Glossary *present*, *here*; CL 12.4.
**Engine-Risiko:** Hoch für Card-Text „present“/„here“ — Host-Heuristik ? Glossary; Welle 2 vertieft Control/Owner/Present.
CODE6 partial BattleRules Fed/Leader
CODE7 partial Location-Host
Checklist leave ?? for partial; Gaps done
## Nachschaerfung Spock 2026-09-05
1 anytime vs response: not auto; only if text hits current action; else after resolution; suspends-play/SD exception
2 Fed 7.4.1: no initiate except Borg unless card allows; mixed Fed+X = Fed force; counter-attack no leader/affil restrict
3 present/here: spaceline here = anywhere at location; site = site only; attempting crew excludes disabled/house-arrest/intruder for dilemma aboard
4 Traveler continuous: Transcendence while in play nullifies all SWB both players; SWB stay; leave play = SWB active again; not Kevin discard
Gaps nullify unchanged done. No Welle 2 until Captain Go.
