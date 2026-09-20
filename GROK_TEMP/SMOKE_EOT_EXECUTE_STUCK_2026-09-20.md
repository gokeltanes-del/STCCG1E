# Smoke — EXECUTE stuck / Horga EOT draw (2026-09-20)
Tip: PENDING

**Ursache:** End EXECUTE startet EOT-Draw (bzw. Horga'hn-Extra-Draw). Wenn Subspace Schism o.ä. einen DrawCard-Response-Stack öffnet, blieb Segment=EXECUTE und End Turn disabled → P1 „hängt in EXECUTE“.

**Fix:** End Turn bleibt „Finish turn / Pass“; Space passt den Draw-Stack und CompleteTurnChange → P2.

| # | Check | Soll | Smoke |
|---|-------|------|-------|
| 1 | Horga am Tisch, 1 Play, End PLAY, End EXECUTE | P2 Play (ggf. nach Pass auf Draw-Response) | |
| 2 | Während Draw-Response | Button „Finish turn / Pass (Space)“ aktiv | |
| 3 | Extra nur Play | Unverändert 9770425 | |
