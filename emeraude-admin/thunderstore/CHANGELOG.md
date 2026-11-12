# Changelog

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
