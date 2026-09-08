from pathlib import Path
path = Path(r"StarTrekCCG\TableWindow.xaml.cs")
text = path.read_text(encoding="utf-8")

# 1) BoardPiece: QuarantineLeaveBlocked on mission/ship/facility hosts
old = '''            board.Add(new BoardPiece
            {
                Card = c,
                Kind = kind,
                Owner = owner,
                Controller = c.Controller != 0 ? c.Controller : owner,
                InstanceId = c.InstanceId,
                FaceUp = c.FaceUp,
                HostName = hostName,
                Occupied = (kind == BoardPieceKind.Ship && !storeHasHost && ShipIsOccupied(kv.Key))
                           || ((kind == BoardPieceKind.Facility || kind == BoardPieceKind.Mission)
                               && aboard.Any(ModifierRules.IsPersonnelCard)),
                HasSecurityAboard = QuietHasSkill(aboard, "SECURITY"),
                HasEngineerAboard = QuietHasSkill(aboard, "ENGINEER"),
                MissionSolved = missionSolved,
                AttemptBlocked = attemptBlocked,
                AttemptBlockReason = attemptBlock,
                RangeLeft = rangeLeft,
                Stopped = stopped,
                Cloaked = cloaked,
                DockedAtId = dockedAtId,
                HullPercent = hullPercent,
                Staffed = staffed,
                StaffReason = staffReason,
                SpacelineIndex = spacelineIndex,
                Aboard = crewSnap
            });'''

# Need quarantine flag computed before board.Add - insert before the Add
# Find attemptBlock assignment area for missions and add quarantine for any host

# Simpler: add QuarantineLeaveBlocked = ... in the object initializer
new = '''            bool quarantineLeave = _attachedDilemmas.Any(a =>
                DilemmaRules.IsQuarantinePersist(a.Kind) && ReferenceEquals(a.Host, kv.Key));
            board.Add(new BoardPiece
            {
                Card = c,
                Kind = kind,
                Owner = owner,
                Controller = c.Controller != 0 ? c.Controller : owner,
                InstanceId = c.InstanceId,
                FaceUp = c.FaceUp,
                HostName = hostName,
                Occupied = (kind == BoardPieceKind.Ship && !storeHasHost && ShipIsOccupied(kv.Key))
                           || ((kind == BoardPieceKind.Facility || kind == BoardPieceKind.Mission)
                               && aboard.Any(ModifierRules.IsPersonnelCard)),
                HasSecurityAboard = QuietHasSkill(aboard, "SECURITY"),
                HasEngineerAboard = QuietHasSkill(aboard, "ENGINEER"),
                MissionSolved = missionSolved,
                AttemptBlocked = attemptBlocked,
                AttemptBlockReason = attemptBlock,
                QuarantineLeaveBlocked = quarantineLeave,
                RangeLeft = rangeLeft,
                Stopped = stopped,
                Cloaked = cloaked,
                DockedAtId = dockedAtId,
                HullPercent = hullPercent,
                Staffed = staffed,
                StaffReason = staffReason,
                SpacelineIndex = spacelineIndex,
                Aboard = crewSnap
            });'''

if old not in text:
    raise SystemExit("BoardPiece block missing")
text = text.replace(old, new, 1)

# 2) Attach HyperAging: populate Held + visual (not stopped)
old_attach = '''            if (DilemmaRules.IsStasisPersist(r.Persist))
            {
                foreach (var h in stasisHeld.Distinct())
                    attached.Held.Add(h);
                if (r.Relocate != null && !attached.Held.Contains(r.Relocate))
                    attached.Held.Add(r.Relocate);
            }
            _attachedDilemmas.Add(attached);'''

new_attach = '''            if (DilemmaRules.IsStasisPersist(r.Persist))
            {
                foreach (var h in stasisHeld.Distinct())
                    attached.Held.Add(h);
                if (r.Relocate != null && !attached.Held.Contains(r.Relocate))
                    attached.Held.Add(r.Relocate);
            }
            if (DilemmaRules.IsQuarantinePersist(r.Persist))
            {
                foreach (var b in teamBorders)
                {
                    if (b.Tag is not Card pc) continue;
                    if (!ModifierRules.IsPersonnelCard(pc)) continue;
                    if (!attached.Held.Contains(pc))
                        attached.Held.Add(pc);
                    ApplyStasisVisual(b, true); // quarantine share stasis leave-block UX
                }
            }
            _attachedDilemmas.Add(attached);'''

if old_attach not in text:
    raise SystemExit("attach Held block missing")
text = text.replace(old_attach, new_attach, 1)

# 3) CanDragOffHost - quarantine
old_drag = '''        // Stasis (Abduction / Phased): cannot leave location via drag/walk/beam
        if (IsCardInStasis(card)) return false;'''
new_drag = '''        // Stasis / Quarantine (Hyper-Aging): cannot leave location via drag/walk/beam
        if (IsCardLeaveBlocked(card)) return false;'''
if old_drag not in text:
    raise SystemExit("CanDragOffHost missing")
text = text.replace(old_drag, new_drag, 1)

# 4) MiniCard stasis check
old_mini = '''        // Stasis personnel: cannot leave location
        if (IsCardInStasis(href.Card))
        {
            ShowPlayError(StasisCannotLeaveMessage(href.Card));'''
new_mini = '''        // Stasis / Quarantine personnel: cannot leave location
        if (IsCardLeaveBlocked(href.Card))
        {
            ShowPlayError(LeaveBlockedMessage(href.Card));'''
if old_mini not in text:
    raise SystemExit("MiniCard stasis missing")
text = text.replace(old_mini, new_mini, 1)

# 5) IsBeamable check
old_beamable = '''        if (IsCardInStasis(c)) return false;
        if (IsBorderStopped(cardBorder)) return false; // Spock: Stopped cannot beam'''
new_beamable = '''        if (IsCardLeaveBlocked(c)) return false;
        if (IsBorderStopped(cardBorder)) return false; // Spock: Stopped cannot beam'''
if old_beamable not in text:
    raise SystemExit("IsBeamable missing")
text = text.replace(old_beamable, new_beamable, 1)

# 6) Beam move loop stasis check - may appear once
old_beam_move = '''            if (b.Tag is Card bc && IsCardInStasis(bc))
            {
                ShowPlayError(StasisCannotLeaveMessage(bc));
                continue;
            }'''
new_beam_move = '''            if (b.Tag is Card bc && IsCardLeaveBlocked(bc))
            {
                ShowPlayError(LeaveBlockedMessage(bc));
                continue;
            }'''
count = text.count(old_beam_move)
print("beam_move occurrences", count)
if count == 0:
    raise SystemExit("beam move stasis missing")
text = text.replace(old_beam_move, new_beam_move)

# 7) After AddCardToHostStack in beam - TryJoinQuarantine
# Find the beam completion TryCureAbductionsPresent after move
old_join = '''        TryCureAbductionsPresent();

        UpdateHostBadge(source);
        UpdateHostBadge(targetHost);
        if (targetHost.Tag is Card tc)
        {
            string fromName = (source.Tag as Card)?.Name ?? "?";
            string names = string.Join(", ",
                toMove.Select(b => (b.Tag as Card)?.Name ?? "?").Where(n => n != "?"));'''

# Need exact - search
idx = text.find("TryCureAbductionsPresent();")
# find the one after beam foreach
print("TryCure count", text.count("TryCureAbductionsPresent();"))

path.write_text(text, encoding="utf-8")
print("TW partial written")
