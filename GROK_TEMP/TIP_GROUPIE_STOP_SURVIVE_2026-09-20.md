# Tip Groupie stop survives UnstopAllCards

tip: HASH
EXE: 2026-09-20 21:17:35
msg: fix(premiere): Groupie stop survives UnstopAllCards until countdown

UnstopAllCards skips borders held by active Alien Groupie (Countdown>0 + Extra).
Unstop only in ExpireAlienGroupie.
