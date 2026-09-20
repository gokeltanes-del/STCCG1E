# Tip Auto-Destruct Sequence (Josef)

tip: HASH
EXE: 2026-09-20 21:12:14
msg: feat(premiere): Auto-Destruct Sequence countdown destroy + splash

## Behavior
- Snap/drop on your ship (OwnShip); countdown 1
- EOT expire: destroy host ship, then damage other ships present with effective SHIELDS<8 (+50% hull)
- Not Nitrium-hack; AttachedEvent path
