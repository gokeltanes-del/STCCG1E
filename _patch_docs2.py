from pathlib import Path
ct = Path("artifacts/CARD_TRACKER.md")
text = ct.read_text(encoding="utf-8")
# Replace parasites line carefully — match by prefix
lines = text.splitlines(keepends=True)
out = []
for line in lines:
    if line.startswith("| Alien Parasites (11 U) |"):
        # Preserve arrow char style from file if present
        out.append("| Alien Parasites (11 U) | partial | tip Data/`f087866` Neg Control + strip Choice labels (`CreateMiniNameLabel`). Hotseat dual-window PARK. Deep affiliation-mix PARK. Pending Pepsch green → working. | Data 2026-09-07 |\n")
        if line.endswith("\r\n") or ("\r\n" in text[:200]):
            out[-1] = out[-1].replace("\n", "\r\n") if not out[-1].endswith("\r\n") else out[-1]
    else:
        out.append(line)
ct.write_text("".join(out), encoding="utf-8")
print("CARD_TRACKER updated")
for line in ct.read_text(encoding="utf-8").splitlines():
    if "Alien Parasites (11 U)" in line:
        print(line)
