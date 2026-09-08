from pathlib import Path

# CHANGELOG — prepend new entry after the intro header
cl = Path("artifacts/CHANGELOG.md")
text = cl.read_text(encoding="utf-8")
entry = """## 2026-09-07 (Fix - Alien Parasites control chooser strip labels)

**UI** - Alien Parasites Neg control AskChoice (3+ options): synthetic `Type=Choice` cards had no `FullImagePath`, so `CreateMiniCard` rendered black empty strip slots. Fallback `CreateMiniNameLabel` shows option text (Away Team only / One ship + crew / …). PARK: dual-window Hotseat chooser; deep \"not compatible with Opp other cards\" affiliation-mix.

---

"""
# Insert after first --- block following title
marker = "Nur spielbare / engine-relevante Schritte. Keine Chat-Metadaten.\n\n---\n\n"
if marker not in text:
    # try CRLF
    marker = "Nur spielbare / engine-relevante Schritte. Keine Chat-Metadaten.\r\n\r\n---\r\n\r\n"
    entry = entry.replace("\n", "\r\n")
if marker not in text:
    raise SystemExit("CHANGELOG marker not found")
if "Alien Parasites control chooser strip labels" in text:
    print("CHANGELOG already has entry")
else:
    text = text.replace(marker, marker + entry, 1)
    cl.write_text(text, encoding="utf-8")
    print("CHANGELOG updated")

# CARD_TRACKER
ct = Path("artifacts/CARD_TRACKER.md")
ctext = ct.read_text(encoding="utf-8")
old_line = None
for line in ctext.splitlines():
    if "Alien Parasites (11 U)" in line:
        old_line = line
        break
if not old_line:
    raise SystemExit("CARD_TRACKER Parasites line not found")
# Keep status partial; note strip-label fix tip (SHA filled after commit — use placeholder then amend? Better: update after commit with known message; tip SHA in commit itself can say tip TBD — actually update with relative note and amend SHA after, OR put tip SHA after commit in same commit by two-step. User wants one commit. Put note without SHA first then amend? User said no amend rules typically. Put "tip pending" then second docs commit? User said one commit for Parasites. Include note referencing this fix; SHA will be the commit itself — chicken/egg. Pattern in tracker uses tip Data/`SHA`. Common approach: commit code+docs with note "this tip", then docs tip SHA is approximate via `git rev-parse HEAD` after... actually other commits put the SHA of the code commit in a FOLLOW-UP docs commit. Looking at history: sometimes same commit updates tracker with its own intended tip. Looking at f087866 - it updated CARD_TRACKER in same commit with tip Data/`f087866` — they must have known or used placeholder. Looking at the commit content...
print("OLD:", old_line)
