from pathlib import Path
path = Path(r"StarTrekCCG\TableWindow.xaml.cs")
text = path.read_text(encoding="utf-8")

old1 = '''    private void ApplyStatusVisualToMini(Border mini, Card card)
    {
        if (IsCardInStasis(card))
        {
            ApplyStasisVisual(mini, true);
            return;
        }'''
new1 = '''    private void ApplyStatusVisualToMini(Border mini, Card card)
    {
        if (IsCardLeaveBlocked(card))
        {
            ApplyStasisVisual(mini, true);
            return;
        }'''
if old1 not in text: raise SystemExit("ApplyStatusVisualToMini missing")
text = text.replace(old1, new1, 1)

old2 = '''                if (IsCardInStasis(pc) || IsBorderStopped(b))
                    negPersonnel.Add((b, pc));'''
new2 = '''                if (IsCardLeaveBlocked(pc) || IsBorderStopped(b))
                    negPersonnel.Add((b, pc));'''
if old2 not in text: raise SystemExit("negPersonnel missing")
text = text.replace(old2, new2, 1)

old3 = '''                string negLabel = IsCardInStasis(pc) ? "Stasis / quarantine" : "Stopped";'''
new3 = '''                string negLabel = IsCardQuarantined(pc) ? "Quarantined"
                    : IsCardInStasis(pc) ? "Stasis" : "Stopped";'''
if old3 not in text: raise SystemExit("negLabel missing")
text = text.replace(old3, new3, 1)

path.write_text(text, encoding="utf-8")
print("TW polish OK")
