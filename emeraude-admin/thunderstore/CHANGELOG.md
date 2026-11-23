# Changelog

## 1.4.2 — 2026-11-23
- `adm:god` and `adm:ghost` now stay on after death and reconnection, until turned off (saved with the character).
- New `adm:damage` (`adm:degats`) `[player] <multiplier>`: multiplies the damage dealt by a player (weapons, tools, arrows), until the game is closed.

## 1.4.1 — 2026-11-23
- `adm:ores` also shows ore mined (Emeraude Minerais 1.1.0).

## 1.4.0 — 2026-11-23
- New admin commands: `adm:portal` (teleport to a portal by name, or list them), `adm:take`, `adm:skill`, `adm:heal`, `adm:repair`, `adm:god`, `adm:ghost`, `adm:fly`, `adm:tomb`, `adm:journal`, `adm:ores`, `adm:say`, `adm:day`, `adm:night`, `adm:raid`, `adm:kick`. French aliases for most of them.
- The host of a game started from the game (not a dedicated server) can now be targeted by the commands, like any other player.
- Commands applied on a player's game (take, skill, heal, repair, god, ghost, fly) need Emeraude Admin 1.4.0 on that player's game.

## 1.3.0 — 2026-11-12
- English and French: new setting `General.Language` (`English` by default, or `French`) for in-game messages and, on the server, the journal and the replies to admins.
- Commands now have English names: `adm:players`, `adm:help`. The French names `adm:joueurs` and `adm:aide` still work.
- Configuration descriptions in English.
- Compatible with 1.2.0 players and servers (same network messages).

## 1.2.0 — 2026-11-12
- Journal rotation on character save: when the server saves the world, each connected player's game saves its character and confirms it to the server, which then archives that player's journal (`.1.log`, `.2.log`, `.3.log`).
- Settings in `emeraude.admin.cfg`: `PositionEverySeconds`, `SnapshotEveryMinutes`, `KeptSaves`.
- `adm:aide` command.
- Journal files are written in UTF-8 without BOM.
- First public release on Thunderstore.

## 1.1.0 — 2026-11-12
- Renamed to Emeraude Admin, commands now use the `adm:` prefix.

## 1.0.0 — 2026-11-12
- Per-player journal on the server: connections, positions every minute, inventory changes, full inventory every 5 minutes, deaths.
- Admin commands checked against `adminlist.txt`: give items, teleport a player to another player or to coordinates, position, inventory, player list.
