from pathlib import Path
lm = Path(r"StarTrekCCG\Game\LegalMoves.cs")
lt = lm.read_text(encoding="utf-8")
# normalize check
needle = "if (ship.Occupied || ship.Aboard.Any(ModifierRules.IsPersonnelCard))"
print("found", needle in lt)
print("CRLF", "\r\n" in lt)
idx = lt.find(needle)
print(repr(lt[idx:idx+200]))
