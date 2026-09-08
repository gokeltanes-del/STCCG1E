from pathlib import Path
p = Path("artifacts/CHANGELOG.md")
t = p.read_text(encoding="utf-8")
t2 = t.replace(" / �?�).", " / ...).")
# also try other mojibake forms
import re
t2 = re.sub(r" / .{1,8}\)\. PARK: dual-window Hotseat chooser", " / ...). PARK: dual-window Hotseat chooser", t2, count=1)
# More precise: fix the strip labels entry line
lines = t.splitlines(keepends=True)
out = []
for line in lines:
    if "CreateMiniNameLabel" in line and "Away Team only" in line:
        line = '**UI** - Alien Parasites Neg control AskChoice (3+ options): synthetic `Type=Choice` cards had no `FullImagePath`, so `CreateMiniCard` rendered black empty strip slots. Fallback `CreateMiniNameLabel` shows option text (Away Team only / One ship + crew / AT+ship). PARK: dual-window Hotseat chooser; deep "not compatible with Opp other cards" affiliation-mix.\n'
        if "\r\n" in t[:300]:
            line = line.replace("\n", "\r\n")
    out.append(line)
p.write_text("".join(out), encoding="utf-8")
print("fixed")
print(p.read_text(encoding="utf-8").splitlines()[7])
