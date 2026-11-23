# Emeraude Minerais

Who feeds the furnaces? This mod counts, for each player:

- **Ore mined:** ore and scrap picked up from the ground after mining. Items dropped by a player, or spilled from a destroyed chest or ship, don't count when picked up again.
- **Ore smelted:** ore and scrap put in a smelter or blast furnace (copper, tin, iron, silver, black metal, flametal…).
- **Bars produced:** bars that come out of the furnace, credited to the player who put the ore in, even if someone else empties it.
- **Bars used:** copper, tin, bronze, iron, silver, black metal and flametal bars spent crafting, upgrading or building, including bars taken from chests by mods such as Craft From Containers.

Charcoal kilns, spinning wheels, windmills and other "smelters" are ignored. Items spawned with cheats and free building/crafting are not counted.

## Where are the totals?

There is no in-game display: the server writes the totals to `BepInEx/config/emeraude-minerais/classement.tsv` (tab-separated: player id, name, counter, item, amount), saved every 30 seconds. Use it to build a leaderboard, for example on Discord.

Players are identified by their character, so renaming or having several characters gives several lines.

## Installation

Install it on the **server and every player's game**. Counting happens on the machine that runs the furnace and on the player's own game: a player without the mod is not counted.
