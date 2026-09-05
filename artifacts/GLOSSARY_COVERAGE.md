# STCCG 1E - Glossary / Compendium coverage
Last updated: 2026-09-05
Owners: **Seven** (matrix + checklist sync) + **Spock** (Ist/Soll + sources). Data only on Captain Go.
Truth for playable cells: `RULES_CHECKLIST.md`. This file tracks FULL Glossary/rulebook Sonderfaelle vs code (yes/partial/no).

Status: done | partial | open | later | n/a

## 6-Wellen-Plan
1. Timing / Actions / Nullify  <-- Hugh/Borg CanRespond **DONE** (`447beac` green+push); core docs done; remaining partials open
2. Control / Owner / Present  <-- **ACTIVE**
3. Movement / Hazards
4. Ship states
5. Battle / Damage
6. Rest Glossary A-Z + leftover Compendium

## Welle 1 - Timing / Actions / Nullify
Scope: Glossary *actions*, valid responses, at-any-time vs response window, nullify timing; Compendium play-phase / card-play timing (checklist Kap. 5-6, 12 timing rows).

| Lemma / Thema | Code? | Checklist | Notes / files | Spock source |
|---|---|---|---|---|
| actions (initiating) | partial | Kap 5/6 | EngineAuthority / LegalMoves | Glossary actions |
| valid responses | partial | 6.5.1 | response windows thin generally; **Hugh DONE** | Glossary actions; valid responses |
| Hugh CanRespond (Borg Ship Dilemma) | **done** | Hugh / Borg Ship | Dilemma-only (not [Bor]); EOT InitiateShipBattle + Response-Window; cloaked not target; tip `447beac` Pepsch green+push | Glossary Hugh, Borg Ship; Spock 2026-09-05 |
| at-any-time vs response | open | Offene Premiere-Prio | | Glossary actions |
| nullify (general) | partial | Events/Interrupts | card-by-card | Glossary nullify |
| nullify Gaps relocate | done | Gaps | GapsNullifyRules `bb163ed` | Glossary Gaps in Normal Space |
| affiliation attack restrictions | open | 7.4.1 parked | Fed initiate after attacked | Glossary; Compendium 7.4.1 |
| here and present | partial | 12.4 | Location-Host; see Welle 2 | Glossary present/here |

## Welle 2 - Control / Owner / Present
Scope: Glossary owner, controller, unique/universal, persona, in play, present, here; control change (commandeer/capture/assimilation). Spock Batch 1 seed 2026-09-05.

| Lemma / Thema | Code? | Checklist | Notes / files | Spock source |
|---|---|---|---|---|
| owner | done | E4 / Unique | Owner on CardInstance; Unique gate by Owner | Glossary unique/universal; E4 ruling |
| controller | partial | E4 | Controller tracked; Unique intentionally NOT by Controller | Glossary; E4 |
| unique / persona | done | E4 | BoardStore InPlay + FindConflictingUnique Owner+persona | Glossary unique/universal, persona; E4 |
| in play | done | E4 | BoardStore.InPlay | Glossary in play |
| present / here | partial | 12.4 | Location-Host; hosts/attachments not glossary-complete | Glossary present, here |
| commandeer / control change | partial | Lore / Capture | Lore Slice4 path; reclaim/capture edges thin | Glossary commandeer; control |

### Spock TODO (Welle 2)
- Extend Batch 1 lemmas with one-line Soll + source cite where thin.
- Seven marks Code? after each Soll batch; sync checklist cells when status changes.

## Later waves
Stubs 3-6 filled after Welle 2 closes.

## Process
1. Spock fills Soll + sources for active wave.
2. Seven marks Code? from checklist + known `Game/*Rules` / HANDOFF (Data only with Captain Go).
3. Update matching `RULES_CHECKLIST.md` cells when status changes.
4. No Engine C# from Seven.
