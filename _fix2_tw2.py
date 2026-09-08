from pathlib import Path
path = Path(r"StarTrekCCG\TableWindow.xaml.cs")
text = path.read_text(encoding="utf-8")

# Join quarantine after beam onto target
# Find beam block with TryCure after toMove loop
needle = '''            RemoveCardFromHostStack(source, b);
            SetBorderOwner(b, _activePlayer);
            AddCardToHostStack(targetHost, b);
            // Keep Rogue Borg unit host in sync so strength/battles track the new ship
            foreach (var rb in _rogueBorg.Where(r => ReferenceEquals(r.Visual, b)))
            {
                rb.Host = targetHost;
                rb.Controller = _activePlayer;
            }
        }

        TryCureAbductionsPresent();'''

repl = '''            RemoveCardFromHostStack(source, b);
            SetBorderOwner(b, _activePlayer);
            AddCardToHostStack(targetHost, b);
            TryJoinQuarantineOnHost(targetHost, b);
            // Keep Rogue Borg unit host in sync so strength/battles track the new ship
            foreach (var rb in _rogueBorg.Where(r => ReferenceEquals(r.Visual, b)))
            {
                rb.Host = targetHost;
                rb.Controller = _activePlayer;
            }
        }

        TryCureAbductionsPresent();'''

if needle not in text:
    raise SystemExit("beam AddCard block missing")
text = text.replace(needle, repl, 1)

# StasisCannotLeaveMessage -> add LeaveBlocked helpers near it
old_msg = '''    private static string StasisCannotLeaveMessage(Card card) =>
        $"{card.Name} is in stasis and cannot leave this location.";'''

new_msg = '''    private static string StasisCannotLeaveMessage(Card card) =>
        $"{card.Name} is in stasis and cannot leave this location.";

    private string LeaveBlockedMessage(Card card)
    {
        if (IsCardQuarantined(card))
            return $"{card.Name} is quarantined (Hyper-Aging) and cannot leave/beam away.";
        return StasisCannotLeaveMessage(card);
    }'''

if old_msg not in text:
    raise SystemExit("StasisCannotLeaveMessage missing")
text = text.replace(old_msg, new_msg, 1)

# Extend ClearStasisForDilemma / IsCardInStasis area
old_clear = '''    private void ClearStasisForDilemma(AttachedDilemma a)
    {
        if (!DilemmaRules.IsStasisPersist(a.Kind)) return;
        foreach (var card in a.Held.ToList())
        {
            var b = FindBorderForCard(card);
            if (b != null)
                ApplyStasisVisual(b, false);
        }
        a.Held.Clear();
    }

    private bool IsCardInStasis(Card card) =>
        _attachedDilemmas.Any(d =>
            DilemmaRules.IsStasisPersist(d.Kind)
            && d.Held.Any(h => ReferenceEquals(h, card)
                               || string.Equals(h.Name, card.Name, StringComparison.OrdinalIgnoreCase)));

    private AttachedDilemma? FindStasisDilemmaForCard(Card card) =>
        _attachedDilemmas.FirstOrDefault(d =>
            DilemmaRules.IsStasisPersist(d.Kind)
            && d.Held.Any(h => ReferenceEquals(h, card)
                               || string.Equals(h.Name, card.Name, StringComparison.OrdinalIgnoreCase)));'''

new_clear = '''    private void ClearStasisForDilemma(AttachedDilemma a)
    {
        if (!DilemmaRules.IsStasisPersist(a.Kind) && !DilemmaRules.IsQuarantinePersist(a.Kind))
            return;
        foreach (var card in a.Held.ToList())
        {
            var b = FindBorderForCard(card);
            if (b != null)
                ApplyStasisVisual(b, false);
        }
        a.Held.Clear();
    }

    private bool IsCardInStasis(Card card) =>
        _attachedDilemmas.Any(d =>
            DilemmaRules.IsStasisPersist(d.Kind)
            && d.Held.Any(h => ReferenceEquals(h, card)
                               || string.Equals(h.Name, card.Name, StringComparison.OrdinalIgnoreCase)));

    private bool IsCardQuarantined(Card card) =>
        _attachedDilemmas.Any(d =>
            DilemmaRules.IsQuarantinePersist(d.Kind)
            && (d.Held.Any(h => ReferenceEquals(h, card)
                                || string.Equals(h.Name, card.Name, StringComparison.OrdinalIgnoreCase))
                || IsPersonnelOnQuarantineHost(card, d.Host)));

    private bool IsPersonnelOnQuarantineHost(Card card, Border host)
    {
        if (!_stackOnHost.TryGetValue(host, out var stacked)) return false;
        return stacked.Any(b => b.Tag is Card c && ReferenceEquals(c, card));
    }

    private bool IsCardLeaveBlocked(Card card) => IsCardInStasis(card) || IsCardQuarantined(card);

    private AttachedDilemma? FindStasisDilemmaForCard(Card card) =>
        _attachedDilemmas.FirstOrDefault(d =>
            DilemmaRules.IsStasisPersist(d.Kind)
            && d.Held.Any(h => ReferenceEquals(h, card)
                               || string.Equals(h.Name, card.Name, StringComparison.OrdinalIgnoreCase)));

    private AttachedDilemma? FindQuarantineDilemmaForCard(Card card) =>
        _attachedDilemmas.FirstOrDefault(d =>
            DilemmaRules.IsQuarantinePersist(d.Kind)
            && (d.Held.Any(h => ReferenceEquals(h, card)
                                || string.Equals(h.Name, card.Name, StringComparison.OrdinalIgnoreCase))
                || IsPersonnelOnQuarantineHost(card, d.Host)));

    /// <summary>Anyone who joins a Hyper-Aging host becomes quarantined.</summary>
    private void TryJoinQuarantineOnHost(Border host, Border cardBorder)
    {
        if (cardBorder.Tag is not Card pc) return;
        if (!ModifierRules.IsPersonnelCard(pc)) return;
        foreach (var d in _attachedDilemmas.Where(x =>
                     ReferenceEquals(x.Host, host) && DilemmaRules.IsQuarantinePersist(x.Kind)))
        {
            if (d.Held.Any(h => ReferenceEquals(h, pc))) continue;
            d.Held.Add(pc);
            ApplyStasisVisual(cardBorder, true);
            _session.Log.Add(_session.TurnNumber, $"P{_activePlayer}",
                $"{pc.Name} joins Hyper-Aging quarantine.");
        }
    }'''

if old_clear not in text:
    raise SystemExit("ClearStasis block missing")
text = text.replace(old_clear, new_clear, 1)

# Detail status for quarantine personnel
old_detail = '''        // Personnel: In stasis line
        if (CardKinds.IsPersonnel(card))
        {
            var dil = FindStasisDilemmaForCard(card);
            if (dil != null)
            {
                AddDetailStatusLine(
                    DetailStatusRules.FormatInStasisLine(dil.Card.Name ?? "stasis",
                        DetailStatusRules.StasisCureHint(dil.Kind)),
                    DetailStatusTone.Stasis);
            }
        }'''

new_detail = '''        // Personnel: In stasis / quarantine line
        if (CardKinds.IsPersonnel(card))
        {
            var dil = FindStasisDilemmaForCard(card);
            if (dil != null)
            {
                AddDetailStatusLine(
                    DetailStatusRules.FormatInStasisLine(dil.Card.Name ?? "stasis",
                        DetailStatusRules.StasisCureHint(dil.Kind)),
                    DetailStatusTone.Stasis);
            }
            var qdil = FindQuarantineDilemmaForCard(card);
            if (qdil != null)
            {
                AddDetailStatusLine(
                    DetailStatusRules.FormatQuarantineLine(qdil.Card.Name ?? "quarantine",
                        DetailStatusRules.QuarantineCureHint(qdil.Kind)),
                    DetailStatusTone.Stasis);
            }
        }'''

if old_detail not in text:
    raise SystemExit("detail stasis missing")
text = text.replace(old_detail, new_detail, 1)

# Mission detail Held/Stasis section - also show quarantine Held
old_mission_held = '''                    if (DilemmaRules.IsStasisPersist(ad.Kind) && ad.Held.Count > 0)
                    {
                        string names = string.Join(", ", ad.Held.Select(h => h.Name ?? "?"));
                        AddDetailStatusLine(
                            DetailStatusRules.FormatHeldStasisSectionLine(ad.Card.Name ?? "?", names),
                            DetailStatusTone.Stasis);
                    }'''

new_mission_held = '''                    if (DilemmaRules.IsStasisPersist(ad.Kind) && ad.Held.Count > 0)
                    {
                        string names = string.Join(", ", ad.Held.Select(h => h.Name ?? "?"));
                        AddDetailStatusLine(
                            DetailStatusRules.FormatHeldStasisSectionLine(ad.Card.Name ?? "?", names),
                            DetailStatusTone.Stasis);
                    }
                    else if (DilemmaRules.IsQuarantinePersist(ad.Kind) && ad.Held.Count > 0)
                    {
                        string names = string.Join(", ", ad.Held.Select(h => h.Name ?? "?"));
                        AddDetailStatusLine(
                            DetailStatusRules.FormatHeldQuarantineSectionLine(ad.Card.Name ?? "?", names),
                            DetailStatusTone.Stasis);
                    }'''

# may appear twice (mission + ship?) - count
c = text.count(old_mission_held)
print("mission_held count", c)
if c == 0:
    raise SystemExit("mission held missing")
text = text.replace(old_mission_held, new_mission_held)

path.write_text(text, encoding="utf-8")
print("TW Fix2 helpers OK")
