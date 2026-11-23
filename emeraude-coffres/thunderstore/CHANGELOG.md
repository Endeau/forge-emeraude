# Changelog

## 1.4.0 — 2026-11-20
- The server sets the chest range for every player: setting `Server.ContainerRange` (15 m by default, 1.5 × Craft From Containers' 10 m) replaces Craft From Containers' `ContainerRange` while playing there, without changing the player's own file. `0` lets each player keep their own range.
- The server also sets the stonecutter's build range: setting `Server.StonecutterRangeMultiplier` (1.5 by default) multiplies it for every player. The no-spawn area around the stonecutter follows the build range, so it grows too.

## 1.3.1 — 2026-11-17
- The x10 and All buttons sit on the Craft button's row, left of it, instead of covering the ingredient list. Craft takes the whole row again for upgrades.

## 1.3.0 — 2026-11-17
- Shift + Craft makes 5 items again, like the base game.
- Two buttons above Craft: **x10**, and **All (N)** that makes as many as possible (ingredients, chests in range and free inventory space). Greyed out when not enough. Not shown for upgrades.
- Setting `Crafting.MultiCraftAmount` removed.

## 1.2.0 — 2026-11-17
- Shift + Craft makes as many items as possible at once (ingredients, chests in range and free inventory space), instead of 5. Setting `Crafting.MultiCraftAmount`: `0` for as many as possible, or a fixed number.

## 1.1.0 — 2026-11-16
- Setting `General.Language` (`English` by default, or `French`) for the in-game message, which was only in French.
- French version of the description.

## 1.0.0 — 2026-11-16
- Recipes that accept any one of several ingredients (like any fish at the food preparation table) now count that ingredient in the chests used by Craft From Containers, and pull it into the inventory when crafting.
