from pathlib import Path
path = Path(r"StarTrekCCG\Game\DilemmaRules.cs")
text = path.read_text(encoding="utf-8")
old = '''    public enum AlienParasitesControlChoice
    {
        None = 0,
        AwayTeam = 1,
        OneShipAndCrew = 2,
        AwayTeamAndShip = AwayTeam | OneShipAndCrew
    }'''
new = '''    [Flags]
    public enum AlienParasitesControlChoice
    {
        None = 0,
        AwayTeam = 1,
        OneShipAndCrew = 2,
        AwayTeamAndShip = AwayTeam | OneShipAndCrew
    }'''
if old not in text:
    raise SystemExit("enum not found")
# ensure System is imported - Flags is in System
path.write_text(text.replace(old, new, 1), encoding="utf-8")
print("Flags OK")
head = path.read_text(encoding="utf-8")[:200]
print(head)
