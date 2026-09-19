<!-- tip: PROJECT_STATUS.md (Repo-Root) entfernt - Wahrheit = artifacts/HANDOFF.md + artifacts/PROJECT.md (2026-09-18) -->

# STCCG 1E - Handoff

Last updated: 2026-09-19 (Data - Holo existence gates Fix-Go)
Repo: https://github.com/gokeltanes-del/STCCG1E
Local VS: C:\\Dev\\StarTrekCCG\\
**Ein Branch: master.** GrokTest nicht nutzen.
**Workflow:** Agents edit+commit nur lokal auf Josef. **Nur Pepsch pusht** nach Gruen-Test.

## Current tip
Local Josef tip **638fde8** - Q + REM Fatigue (Pepsch smoke). Nicht gepusht.
Local Josef tip **bcf7f9d** - Holo existence gates (Pepsch Fix-Go; Spock precise Soll). Nicht gepusht. (Prior Holo-Projectors tip 3c50792; LF UI eaf0c24.)
**Pepsch EXE (Default Debug):**
`C:\Dev\StarTrekCCG\StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe`
Nicht `_build_holo*` / Release / alte Side-Builds.


## Tip detail (Data, Josef, 2026-09-19) - Q + REM Fatigue
Captain/Spock Soll LOCK (rtifacts/SOLL_Q_REM_2026-09-19.md). Continuum/Q-Flash PARK.
### Q (44 R)
- Pass: 2 Leadership + INTEGRITY>60 -> Overcome; purge remaining Dilemma seeds under mission; discard Q; attempt continues.
- Fail: opponent spaceline rearrange (location units, Left/Right/Done); AT/ship+crew stopped; discard Q.
- Verify: DilemmaRules.VerifyQDilemma.
### REM Fatigue (47 U)
- AttachContinue CD Icon-[4]; IsQuarantinePersist(RemFatigue); OriginalEncounter kill on CD0 (joiners live).
- Cure: 3 MEDICAL (group counts) or dock at Outpost (not HQ/Station) -> +5.
- Verify: DilemmaRules.VerifyRemFatigue + cure tests in DilemmaCureRules.
Files: Game/DilemmaRules.cs, Game/DilemmaCureRules.cs, Game/DockingRules.cs, TableWindow.xaml.cs, rtifacts/CARD_TRACKER.md, rtifacts/HANDOFF.md.
Exe: StarTrekCCG\\StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe
### Pepsch smoke
1. Q pass: 2 Leadership + INTEGRITY>60 at attempt -> overcome; other dilemma seeds under that mission discarded; attempt continues.
2. Q fail: no pass -> stop; opponent Left/Right/Done rearrange; Q discarded; no Q-Flash/Continuum.
3. REM: encounter without 3 MEDICAL -> quarantine CD4, attempt continues; cannot beam away; joiner quarantined but survives CD0 kill of originals only.
4. REM cure: 3 MEDICAL present -> +5 discard; OR dock ship at Outpost -> +5 discard (Station/HQ must NOT cure).

## Tip detail (Data, Josef, 2026-09-19) - Holo existence gates (Pepsch Fix-Go)
Captain confirms Spock precise Holo Soll. Standing Practice Glossary cites. CODE_PLACEMENT: EventRules decide + TW Apply (beam/kill/report).
Prior tip **3c50792** helpers/nullify; this tip wires report/beam gates + kill=deact + stranded erase + same-turn no-reactivate.

### Spock Soll (in-scope)
1. Activated: Holodeck ship/fac OR planet+Projectors OR MHE
2. Deactivated: any ship/fac OR planet+Projectors OR MHE
3. Illegal even deact: planet without Projectors/MHE
4. Illegal attempt → deactivate, do NOT complete relocate
5. Erase if illegally present; Projectors nullify dependents (MHE protects); ship destroy→discard; kill→deactivate
6. Holodeck=activate aboard; Projectors=planet only; MHE=exist+activate where allowed
7. Same-turn: no reactivate after deactivate this turn

### Core helpers (EventRules)
- HoloMayExistOnPlanet / HoloMayExistAboard(activated,…) / HoloMayExistHere / HoloMayActivateHere
- CanVoluntaryRelocateHolo / IllegalRelocateShouldDeactivate / MayReactivateHologram
- DependsOnThisHoloProjectorsForExistence / DeactivateHologram / ShouldEraseWhenStuckWithoutEnabler
- VerifyHoloProjectors expanded (act/deact/planet beam/same-turn)

### TW wire
- CompleteBeamTo → FilterHoloBeamAllowed (bare planet block; illegal act→deact stay)
- DiscardPersonnelBorder → [Holo] kill = MarkHologramDeactivated (ship destroy still discards)
- EraseStrandedHologramsOnHost after beam; report auto-deact without activate enabler
- PersonnelInstance.HologramDeactivated + _holoDeactivatedThisTurn (EOT clear)
- IsCardDisabled ORs hologram deactivated (does not wipe via Ktarian sync)

### Parked
- Captive / opponent Holodeck deep; Holodeck Door suite / Holoprograms
- Personnel-battle safety (holo cannot kill organics; holo-only STRENGTH force)
- Activate UI button (helper CanReactivateHologramNow ready)

### Bewusst nicht
No push; no Door/captive/battle safety; no activate UI chrome.

### Pepsch smoke
1. Cannot beam [Holo] to bare planet (no Projectors/MHE) — blocked; stays put.
2. Holo-Projectors on planet → [Holo] may beam there (act or deact).
3. MHE with/aboard → [Holo] may exist/activate where allowed.
4. Kill [Holo] → deactivated (not discard); ship destroy → discard crew incl. [Holo].
5. Nullify Projectors → dependents erased; MHE-protected survives.

### Files
Game/EventRules.cs, Game/Board/CardInstance.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md, _VerifyHolo/*
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe (side-build _build_holo_gates green while Pepsch EXE locked)

## Tip detail (Data, Josef, 2026-09-19) - Lore's Fingernail UI (Pepsch Fix-Go)
Engine live-affil (980317a) OK for battle; Surface/Detail still showed printed Federation.
### Root
DetailType / RevealSubtitle / Icon row used printed `card.Affiliation` - not `GetAffiliations` live mode. No status naming Lore's Fingernail.
### Fix
- `ReportingRules.FormatLiveAffiliation` / `FormatAffiliationTypeSuffix` / `FormatLiveAffiliationBracket` / `BracketAffil` from GetAffiliations.
- DetailType + RevealSubtitle + IconCatalog badge: live Non-Aligned / [Non] under Fingernail.
- DetailStatus: `Lore's Fingernail: Non-Aligned` (Debuff); ToneForEvent.Fingernail Debuff.
- Dual-affil action badge shows live Non + rule name; RefreshTableBuffs refreshes open Detail.
- Verify harness asserts FormatLiveAffiliation / Bracket / FormatFingernailLine.
### Pepsch smoke
1. Play Lore's Fingernail -> select Data: Detail shows Non-Aligned [Non]; status `Lore's Fingernail: Non-Aligned`; icon badge [Non].
2. Nullify Fingernail -> Data Detail back to Federation; status/badge gone.
3. Battle path still Non (engine unchanged).
### Bewusst nicht
No push; no house-arrest deep UI; no DeckBuilder printed filter change; no new PNG affil icons.
### Files
Game/ReportingRules.cs, Game/DetailStatusRules.cs, Game/IconCatalog.cs, Game/EventRules.cs (verify), TableWindow.xaml.cs, artifacts/*
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe (side-build _build_lf_ui green)

## Tip detail (Data, Josef, 2026-09-19) - Lore's Fingernail (PR 81 R)
Captain Go / Spock Soll-OK (fold all). Standing Practice Glossary cites. CODE_PLACEMENT: EventRules + ReportingRules/DualAffiliation (+ TW ambient).

### Lore's Fingernail required
- Plays on table (Place.Table / Persist.Fingernail).
- While in play: inorganic (not [Holo]) **effective affiliation = Non only**; dual toggle off / ProfileFor not active as printed.
- Nullify/leave -> restore prior multi-affil mode (ambient clear; CurrentAffiliation preserved underneath).
- Treaties: Non mixing separate (existing NA rules); Fed battle limits lift (no longer Fed).
- Matching affiliation / house arrest: re-check as Non via GetAffiliations.
- Glossary: androids may report as Non.
- Classic: Soong-type + Exocomps; [Holo] excepted. Modern: all Inorganic except [Holo].

### Core helpers
- `DilemmaRules.IsInorganic` — Characteristics Inorganic and/or Android (central; whitelist verify-only).
- `EventRules.FingernailMakesNon` — IsInorganic && !CardIcons.IsHologram while FingernailInPlay.
- `EventRules.SetFingernailInPlay` / ambient refreshed in `RefreshTableBuffs` + CommitCardToTable.
- `ReportingRules.GetAffiliations` early NA override; DualAffiliationRules.TrySetMode/ProfileFor gated.
- `VerifyLoresFingernail` Premiere smoke.

### TW wire
- HasFingernail on GameState / BoardStore; TrySwitchAffiliation deny while affected.

### Pepsch smoke
1. Play Lore's Fingernail on table -> Data / Exocomp become Non (effective); Fed battle limit lifts for them.
2. Einstein / Brahms / Fek'lhr / K'Tesh / Jera / Tomek ([Holo]+Inorganic) stay printed affil (NOT Non).
3. Nullify Fingernail -> Data back to Federation; dual toggle works again.

### Bewusst nicht
No push; no house-arrest deep UI; no persona/report exhaustive matrix; K'Tesh/Jera/Tomek correctly [Holo]-excepted (brief smoke listing them as -> Non contradicted printed except [Holo]).

### Files
Game/DilemmaRules.cs, Game/EventRules.cs, Game/ReportingRules.cs, Game/DualAffiliationRules.cs, Game/GameState.cs, Game/Board/BoardStore.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md, _VerifyFingernail/*
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe (side-build _build_fingernail green)

## Tip detail (Data, Josef, 2026-09-18) - Holo-Projectors (PR 78 U)
Captain Go / Spock Premiere Holo bullet-Soll. Standing Practice Glossary cites. CODE_PLACEMENT: EventRules (+ TW nullify wire).

### Holo-Projectors required
- Plays on [P] (Place.OnPlanet / Persist.HoloProjectors).
- While in play: [Holo] may exist on that planet activated or deactivated.
- Nullify -> erase only [Holo] at THIS planet that depended on THIS copy (MHE / other enabler / other Projectors protect; other planets untouched).
- Not a ship Holodeck (Holodeck enables aboard only).

### Core helpers (EventRules)
- IsHoloProjectors / IsMobileHoloEmitter / HasHolodeck / HasMobileHoloEmitterPresent
- HoloMayExistOnPlanet / HoloMayExistAboard
- DependsOnThisHoloProjectorsForExistence (nullify erase gate)
- DeactivateHologram (kill/destroy -> Disabled, not erase)
- ShouldEraseWhenStuckWithoutEnabler
- FormatHostEffectSummary(Persist.HoloProjectors) + VerifyHoloProjectors

### TW wire
- NullifyEventInPlay: before detach, EraseHoloDependentsOfProjectors -> OutOfPlay for dependents of this copy.

### Parked (for Pepsch / later tips)
- Captive / opponent Holodeck deep; Holodeck Door suite / Holoprograms
- Post-PR existence cards; advanced Barclay
- Full report/beam illegal-location gates + same-turn reactivate tracking
- Personnel-battle safety (holo cannot kill organics; holo-only STRENGTH force)

### Fallen (avoided)
- Holodeck != planet existence; nullify != erase all [Holo]; kill != erase; planet [Holo] without enabler

### Bewusst nicht
No push; no Holodeck Door/captive deep; battle safety not wired this tip.

### Pepsch smoke
1. Play Holo-Projectors on planet -> [Holo] may exist there (act or deact).
2. Nullify Projectors with [Holo] only depending on it -> that [Holo] erased (out of play); MHE-protected survives; other planet untouched.
3. Ship Holodeck: [Holo] aboard OK; Holodeck alone does not enable planet surface.
Files: Game/EventRules.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md, _VerifyHolo/*
Exe: StarTrekCCG\\bin\\Debug\\net8.0-windows\\StarTrekCCG.exe (side-build _build_holo green while Pepsch EXE locked)

## Tip detail (Data, Josef, 2026-09-18) - Goddess of Empathy (Amanda response)
Captain/Spock Soll-OK. Standing Practice Glossary cites.
### Root
Goddess gate lived on normal hand-play / EngineAuthority / InterruptPlayEffect, but **response/nullify window** skipped it: `CollectAllLegalResponses` + stack-open `TryAllowHandPlay` only called `CanRespond`; `NullifyStackEffect` (Amanda) had no `HasGoddess` check. Amanda could nullify under Goddess.
### Fix
- `EventRules`: Glossary cite + `GoddessBlocksInterruptPlay` + `VerifyGoddessOfEmpathy` (Amanda NOT excepted; Kevin/Q2/[Q]/[Ref] ok).
- `LegalMoves` stack responses: filter via HasGoddess + IsGoddessException.
- `NullifyStackEffect.CanPlay`: Goddess gate (Amanda blocked; Q2 still exception).
- `TableWindow`: CollectAllLegalResponses + stack-open TryAllowHandPlay GoddessBlocksInterrupt.
- EngineAuthority cite tightened for Respond.
### Bewusst nicht
No change to Kevin/Q2/[Ref]/[Q] exceptions; no push; no Kevin Convergence rename as exception.
### Pepsch smoke
1. Goddess of Empathy on table -> play Interrupt (e.g. Q2 on stack) -> Amanda Rogers illegal (not in ThinkTray / deny on play).
2. Kevin Uxbridge and Q2 still legal under Goddess.
3. Without Goddess, Amanda still nullifies interrupts as before.
Files: EventRules.cs, EffectRegistry.cs, LegalMoves.cs, EngineAuthority.cs, TableWindow.xaml.cs, artifacts/*.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Gaps Host-Action-Panel stick
Captain Go: panel follows ship after Fly on/over Gaps in Normal Space. **NO** general auto-dismiss.
### Root
Relayout/Relocate moved ship + selection frame; `_actionPanel` stayed at mid/old column. Gaps span skipped dockable Relayout (missions only) so ships/panel desynced on span.
### Fix
- `RepositionHostActionPanelIfAny` - bind panel Canvas L/T to `_selectedCard` (reposition only, never dismisses).
- Call after `RelayoutDockablesUnderMission` + `RelocateShipAlongSpaceline` when ship selected.
- `RelayoutMissionsOnSpaceline` / `RelayoutAllDockables`: landables incl. Gaps (`IsLandableLocation`), pin+Relayout dockables under span.
- KEEP: FlyPick still Clear+SetSelection (exit Fly mode / rebuild buttons) - not dismiss-as-the-fix.
### Bewusst nicht
No Fake-Fly; no general menu auto-dismiss after every Fly.
### Pepsch smoke
1. Galaxy -> Gaps in Normal Space (Fly): Host action buttons stay glued to the ship (not mid/old).
2. Fly over Gaps to another mission: panel still on ship after arrive.
3. Enterprise / normal mission Fly still OK; Occupancy Badge / Distortion untouched.
Files: TableWindow.xaml.cs, artifacts/FEATURES.md, artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Occupancy Badge UX
Captain/Spock Soll-OK. UX only (Host footer). Standing Practice: personnel-present chrome; no glow/split.
### Occupancy Badge (Pepsch lock)
- Host footer badges in P1 (cyan) / P2 (orange) color.
- Planet mission + personnel -> label `Away Team`; Ship/Outpost/Station (facility) + personnel -> `Crew`.
- Empty (no personnel) = no badge. Eq/Art/Event/Dilemma = detail only, no badge.
- Both players occupied = two badges side by side (same footer row).
- KEEP: Rogue Borg notice on badge when present; Distortion Field / prior tips untouched.
Files: TableWindow.xaml.cs (UpdateHostBadge / EnsureSideBadge / selection frame), artifacts/FEATURES.md (already ACTIVE), artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe
### Pepsch smoke
1. Planet mission: beam/report P1 personnel onto planet -> footer `Away Team` in P1 color; remove all personnel -> badge gone.
2. Same planet: add P2 personnel too -> two badges side by side (`Away Team` each color); Eq alone on planet -> still no occupancy badge.
3. Ship or Outpost/Station with crew -> footer `Crew` in owner color; dual crew both sides -> side by side; no card-glow / diagonal split.

## Tip detail (Data, Josef, 2026-09-18) - Distortion Field (PR 70 U)
Captain/Spock Soll-OK. Standing Practice Glossary cites. CODE_PLACEMENT: Events in Rules; UI wire.
### Distortion Field (PR 70 U) - EN Distortion
- Unique (IsPrintedUniqueEvent / IsDistortionField); Plays on Planet; enters play **FACE UP** (blocks immediately); first EOT -> face-down.
- EOT each turn: flip (even while face-down) via EndOfTurnEventRules.ShouldFlipDistortion.
- Face-up: prevents ALL beaming to/from this planet incl. planet-vicinity (landed <-> facility); same-mission gate as Atmospheric Ionization.
- Face-down: beaming allowed.
- Fix: FormatHostEffectSummary was Interrupt confusion ("RANGE may be used to unstop") -> beaming block summary; DetailStatus Debuff (not Buff).
- Verify: EventRules.VerifyDistortionField + EndOfTurnEventRules.VerifyDistortionFlip.
- KEEP: Atmospheric Ionization (5c08269) untouched / no regress.
Files: Game/EventRules.cs, Game/DetailStatusRules.cs, Game/EndOfTurnEventRules.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Alien Probe + Atmospheric Ionization + IPG UX
Captain/Spock Soll-OK. Standing Practice Glossary cites folded in. CODE_PLACEMENT: Events in Rules; UI wire.
### IPG Detail-Overkill (UX only)
Armus detail showed 3Ã— IPG: green Icons:[IPG] (DetailAttributes), glyph (IconCatalog.Fill), purple Icons:[IPG] (DetailIcons fallback).
**Removed 2:** green Attributes Icons-line + purple DetailIcons Icons-fallback. **Kept:** glyph strip. Cmd/Staffing untouched (FillStaffing).
### Alien Probe (PR 66 U)
- Plays on table (Persist.Probe); continuous both hands revealed (HasAlienProbeInPlay â†’ hand strip).
- Hand cards not nullifiable until played (CanNullifyTargetCard in NullifyEventInPlay).
- Battle Bridge / used tactics NOT affected (faceDownAlways stays).
- Verify: EventRules.VerifyAlienProbe.
### Atmospheric Ionization (PR 68 C) â€” EN Ionization
- Unique (IsPrintedUniqueEvent); Plays on Planet; beam 1 at a time; max 3 personnel this way per controller per turn.
- Glossary-Add: to/from this planet includes planet-vicinity beams (landed ship â†” planet facility); same-mission gate covers.
- Count increments only after successful beam (NoteIonizationBeam); per-player save fields.
- Verify: EventRules.VerifyAtmosphericIonization.
Files: Game/EventRules.cs, Game/DetailStatusRules.cs, Game/EffectRegistry.cs, Services/GameSave.cs, TableWindow.xaml.cs, artifacts/CARD_TRACKER.md, artifacts/HANDOFF.md.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Temporal Causality Loop (Glossary-treu)
Captain/Pepsch Implement-Go. Lock: Glossary-true (NOT Seeds-only). Standing Practice rule cites folded in.
- Decide (`DilemmaRules.Loop`): SCIENCE + CUNNING>35 â†’ Overcome +5; else EffectAndEnd + EndTurn + StopTeam (no +5).
- Apply (TW): `_attemptDiscards` log (order+origin+seedOrderHint) from attempt start; holes closed (RemoveEquipmentFromHost, DestroyShipOrFacility, SeniorStaff, IpgNullify, DevilNullify, OvercomeSeed Zone-truth A via discard).
- Fail restore: seeds face-down Encounter-Order (`ReseedInsertIndex`); non-seeds re-play host / legal report / stay discarded; TCL not re-seeded; Attach*/WallFailed untouched.
- EndTurn: `_skipNormalEndOfTurn` skips normal EOT (Compendium 8 / _rb69).
- Verify: `_VerifyTemporal` â†’ `DilemmaRules.VerifyTemporalCausalityLoop`.
- Docs: CODE_PLACEMENT + ENGINE Standing Practice (rule cites); CARD_TRACKER partial until Pepsch green.
Files: `Game/DilemmaRules.cs`, `TableWindow.xaml.cs`, `_VerifyTemporal/*`, `artifacts/CODE_PLACEMENT.md`, `artifacts/ENGINE.md`, `artifacts/CARD_TRACKER.md`, `artifacts/HANDOFF.md`.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Foundation first-listed skill (Classification skip)
Captain Foundation-Fix-Go / Spock Rules-OK: first-listed skill â‰  classification box.
- Root cause: `FirstListedSkill` treated Lackey leading class token in `text` as first skill (Data OFFICERâ†’wrong).
- Fix shared parse: skip leading `Card.Class` echo(s); next skill (multi-word/xN) = first-listed. Assimilation: Class mismatch â†’ former class token is first-listed.
- Apply: strip that skill (multipliers together); restore printed classification if same-named (Bashir MEDICAL x2). Second skill does not slide up.
- ALL first-listed consumers already use `MissionRules.FirstListedSkill` / `ApplyFirstListedSkillLoss` (Tsiolkovsky Apply+Summary).
- Verify: Dataâ†’ENGINEER gone / OFFICER stays; Seskal SCIENCE; Bashir MEDICAL class remains; Sci Physics; cure 3 MEDICAL; not-cumulative.
- KEEP ship Events Positive-only from 948cf0f (untouched).
Files: `Game/MissionRules.cs`, `Game/ModifierRules.cs`, `Game/DilemmaRules.cs`. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Tsiolkovsky Infection Apply + Summary + Events UX
Captain Fix-Go / Spock: AttachContinue + Cure 3 MEDICAL + no StopTeam already OK.
- Bug Summary: `FormatHostEffectSummary` showed attributes -3 (wrong).
- Apply-Gap: `DisabledSkillsOnHost` only TwoDim Empathy â€” no first-listed strip for Tsiolkovsky.
- Fix Apply: `MissionRules.FirstListedSkill` + `ModifierRules.ApplyFirstListedSkillLoss` (not cumulative); TW `HostHasTsiolkovsky` / `LoseFirstListedOnHost` wired into Resolve/Summarize/CanSolve/Ctx.
- Fix Summary: host effect = personnel lose first-listed skill (cure: 3 MEDICAL).
- VerifyTsiolkovskyInfection: apply + summary + not-cumulative OK.
- UX: ship detail Events only under Positive (never under Personnel). Debuff Events stay Negative.
Files: `Game/MissionRules.cs`, `Game/ModifierRules.cs`, `Game/DilemmaRules.cs`, `TableWindow.xaml.cs`. CARD_TRACKER partial until Pepsch green. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Tarellian Step0 display polish
Captain Fix-Go / Pepsch: Logic A/B OK; Bug = dilemmacard missing in Step0 Choose dialog top-left.
- Cause: dilemma `PickYou`/`PickOpp` called `PickCardFromList` without `source`; synthetic Choice cards have no art -> empty slot
- Fix: pass `seedCard` as source (same as `AskChoice` / other card-choice dialogs). Display only; Rules/Picker flow unchanged.
File: `TableWindow.xaml.cs`. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Tip detail (Data, Josef, 2026-09-18) - Tarellian Overcome UX A/B
Captain Fix-Go / Pepsch Soll: two entry points, NOT one flat pool. Rules (Spock) unchanged.
- Schritt 0: A) Medical Personnel OR B) Equipment + Personnel (Choice cards)
- Path A: encountering crew filtered to printed usable MEDICAL (Class OR Skill); beam/sacrifice; no equipment discard
- Path B: MEDICAL-granting eq only (Medical Kit, Medical Tricorder via SkillEquipment); then personnel matching RequiredClass (Kit->OFFICER, Medical Tricorder->SCIENCE); person+eq discard +5
- Plain Tricorder still no MEDICAL / not offered
- VerifyTarellian: Path A then Path B (+ Kit-only, Tricorder class-filter, plain fail)
Files: `Game/DilemmaRules.cs`, `Game/ModifierRules.cs` (EquipmentRequiredClassForSkill). TW untouched. CARD_TRACKER partial until Pepsch green. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Warum 4a61fa1 falsch war
Pepsch meinte VERTIKALE Linie, nicht horizontal. 4a61fa1 (X-Stagger, Y-Baseline) war Missverstaendnis. SOLL jetzt: eine saubere Spalte unter (P1) / ueber (P2) der Spaceline; N Schiffe nur via `DockSlotOffsetY(slot)`; Left = mission-centered (`DockSlotOffsetX(0)`).

## Pipeline (zwei Straenge, nie im selben Commit)

### Sofort -- Pepsch smoke
- Response Window UX (Silent Badge, [R] Think Tray, [Space] Pass, Presets)
- Artifact Beaming: Varon-T Planet auf Schiff ohne Treaty-Fehler
- **Dock vertikal** tip: Ships+Outposts gleiche X-Spalte, Y-Slots; Load+live Relayout gleich

### Strang A -- Premiere-Dilemmas (ACTIVE, Pause)
**Q + REM Fatigue** Data tip landed (partial) — Pepsch smoke. Continuum/Q-Flash still PARK.
Next unknown: Q, Radioactive Garbage Scow, Rebel Encounter, (REM Fatigue skip), Sarjenka, Shaka (TCL tip landed â€” Pepsch smoke). Reminder: Tsiolkovsky/Two-Dim/Wind Dancer already green.
Done prior: **Tarellian Plague Ship** Pepsch green; **Tsiolkovsky** Foundation tip; **Temporal Causality Loop** Data Implement-Go (partial, tip e88860e).

### Strang B -- Welle 2 Extract (artifacts/EXTRACT_REST.md)
Welle 1 Slices 1-9 DONE. Naechstes Ticket wenn Captain Go: **P0-D1 + P0-E1**.

### Spaeter -- Netz (nach P0)
BoardStore Persist-Wahrheit zuerst. Localhost zwei Exes.

## Parked
Hugh Borg Ship; IM FindMission; dump@Gaps; Parasites Hotseat-UI; REM Fatigue; Cure-Present-Scope Ship

## Docs map (canon)
| File | Owner |
|------|--------|
| HANDOFF.md | Captain + Data (tips) |
| PROJECT.md | Captain + Data |
| ENGINE.md | Data |
| CODE_PLACEMENT.md | Data |
| TABLEWINDOW_INVENTORY.md | Data |
| FEATURES.md | Seven |
| CARD_TRACKER.md | Jadzia |
| RULES.md / RULES_CHECKLIST.md | Spock (+ Seven coverage) |
| GLOSSARY_COVERAGE.md / GLOSSARY_WELLE1.md | Seven / Spock |
| EXTRACT_REST.md | Data / Captain |
| CHANGELOG.md | Data (playable lines) |

Wahrheit: HANDOFF + PROJECT (PROJECT_STATUS entfernt).
Pepsch EXE: Default Debug `StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe` — **nicht** `_build_*`.


## Tip detail (Data, Josef, 2026-09-18) - dock vertical correction
Root: 4a61fa1 X-cascade war falsch (Pepsch misspoke "horizontal").
Fix:
- `DockSlotOffsetY(slot)` wieder: Y-Stufen; `DockSlotOffsetX` = 0 (mission-centered)
- Relayout / Relocate / Snap-Preview / RelayoutAll: Top = missionTop + DockSlotOffsetY(i); Left = missionLeft
- Column-Tolerance 45; Z steigt mit Slot; PinÃƒÆ’Ã‚Â¢Ãƒâ€¹Ã¢â‚¬Â Ãƒâ€šÃ‚ÂªPixel Membership; CountDockablesForOwner
- KEEP Load-Pfad 8c88b2d/056f6b0: X-Pin, Relayout after settle, Top nie Save-Y+offset, Pins clear, SpacelineYDefault
File: StarTrekCCG/TableWindow.xaml.cs. Not pushed.
Exe: StarTrekCCG\StarTrekCCG\bin\Debug\net8.0-windows\StarTrekCCG.exe

## Prior tip (056f6b0) load settle
PinDockablesToSpacelineByColumn + Relayout after UpdateLayout + ScheduleRelayoutAfterLoadSettle; Top immer missionTop+DockSlotOffsetY, nie Save-Y.
