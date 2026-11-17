# Changelog

## 1.1.0 — 2026-11-17
- The statue is no longer a ward: it no longer locks chests, doors and buildings, and has no activate button or player list. It only keeps monsters away, always on. Use a regular ward to protect a house.
- Every player needs this version (statues placed before keep working, without the ward part).

## 1.0.1 — 2026-11-17
- The statue no longer glows as a blinding blob: it keeps the ward's glow intensity, in green.

## 1.0.0 — 2026-11-17
- New piece: the Emerald Statue (hammer, Misc tab, near a workbench), a green copy of the ward.
- No monster spawns within its radius, including during a raid.
- Monsters flee its area, like around the trader, and stop chasing a player inside it. Tamed creatures are not affected.
- No raid starts on a player standing inside a statue's area.
- It also protects buildings like a ward, with the same radius.
- Each statue has its own radius: its builder cycles it with Shift + E (16, 24, 32, 48, 64 m). The hover text shows it.
- Settings `Sanctuary.Radius`, `Sanctuary.RepelMonsters`, `Sanctuary.BlockRaids` and `Sanctuary.Recipe`, synced from the server.
