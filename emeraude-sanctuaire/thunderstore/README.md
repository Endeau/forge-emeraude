# Emeraude Sanctuaire

An emerald statue to keep monsters out of your village, without torches everywhere.

## The statue

Built with the hammer, *Misc* tab, near a workbench. Default cost: 20 stone, 10 greydwarf eyes, 2 surtling cores. It looks like a green ward, but only keeps monsters away: it is always on, and doesn't lock chests, doors or buildings. Its radius shows as a circle while placing it or looking at it.

Each statue has its own radius, 32 m by default. Its builder changes it with **Shift + E**: 16, 24, 32, 48, 64 m, then back to 16. The circle and the hover text follow.

Within its radius:

- **No monster spawns**, at night or during a raid.
- **Monsters flee**, like around the trader, and stop chasing a player inside. Tamed creatures are not affected.
- **No raid starts** on a player standing inside.
- Items left on the ground don't despawn (same as near a workbench).

Monsters can still spawn just outside the radius: place several statues to cover a large village.

## Installation

Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/). Install it on the **server and every player's game**: the statue is a new piece, players without the mod can't join.

## Configuration

`BepInEx/config/emeraude.sanctuaire.cfg`, set on the server and pushed to the players.

| Setting | Default | Effect |
|---|---|---|
| `Sanctuary.Radius` | `32` | Radius of a statue whose builder never changed it, in meters (8 to 64). |
| `Sanctuary.RepelMonsters` | `true` | Monsters flee the statue's area. |
| `Sanctuary.BlockRaids` | `true` | No raid starts on a player inside a statue's area. |
| `Sanctuary.Recipe` | `Stone:20,GreydwarfEye:10,SurtlingCore:2` | Cost of the statue. Requires a restart. |
