from pathlib import Path
path = Path(r"StarTrekCCG\TableWindow.xaml.cs")
text = path.read_text(encoding="utf-8")
text = text.replace(
    "_borderOwner.Clear(); _attachedDilemmas.Clear(); _attachedEvents.Clear();",
    "_borderOwner.Clear(); _attachedDilemmas.Clear(); _alienParasiteControls.Clear(); _attachedEvents.Clear();",
    1)
# second clear - need context
old = '''        _attachedDilemmas.Clear();
'''
# only replace the one near PlaceDeck - check around 8693
idx = text.find("_attachedDilemmas.Clear();", text.find("_alienParasiteControls.Clear()"))
# find remaining clears
import re
count = text.count("_attachedDilemmas.Clear();")
print("clears left pattern count", count)
# Add after every _attachedDilemmas.Clear() that doesn't already have parasite clear after
text2 = text.replace(
    "_attachedDilemmas.Clear();\n",
    "_attachedDilemmas.Clear();\n        _alienParasiteControls.Clear();\n")
# might double-clear on first site - fix
text2 = text2.replace(
    "_alienParasiteControls.Clear(); _attachedEvents.Clear();\n        _alienParasiteControls.Clear();",
    "_alienParasiteControls.Clear(); _attachedEvents.Clear();")
path.write_text(text2, encoding="utf-8")
print("clear sites updated", text2.count("_alienParasiteControls.Clear()"))
