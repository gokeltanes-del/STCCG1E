from pathlib import Path

p = Path(r"StarTrekCCG\Game\DetailStatusRules.cs")
t = p.read_text(encoding="utf-8")
print("Tone has Quarantine?", "IsQuarantinePersist" in t)
print("END:", repr(t[-120:]))

# fix dash characters - file may use special dash
idx = t.find("FormatHeldStasisSectionLine")
print(repr(t[idx:idx+120]))
