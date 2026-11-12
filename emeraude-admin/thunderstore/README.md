# Emeraude Admin

Admin tools for **Valheim dedicated servers**, made for a small private server among friends:

- a **journal per player**, written on the server: connections, positions, every item gained or lost, deaths, character saves;
- **admin console commands** to give items to a player and teleport players, which vanilla Valheim no longer allows admins to do on a dedicated server.

> The in-game texts and the journal are in **French**. *(Version française plus bas.)*

## Why

Since Valheim 1.0, cheat commands such as `spawn` and `goto` only work for the host, never for an admin connected to a dedicated server. When a player loses items (game crash before their character was saved, bug…), there is no way to give them back, and no record of what they had.

Emeraude Admin keeps that record on the server and gives admins the tools to fix it, without enabling cheats.

## Installation

Install it **on the server and on every player's game** (r2modman / Thunderstore Mod Manager, or copy `plugins/EmeraudeAdmin.dll` into `BepInEx/plugins/`).

| Where | What it does there |
|---|---|
| Dedicated server | Writes the journals, checks admin rights, runs the commands |
| Players | Report their inventory changes to the server, apply `tp` and `give` |

A player without the mod can still join: they just won't appear in item journals and can't be targeted by `adm:give` / `adm:tp`. Their positions are still logged.

Admins are the players listed in the server's vanilla `adminlist.txt` (one SteamID per line). Nothing else to configure.

## Admin commands (F5 console)

No `devcommands` needed, no cheat flag on your character or on the items.

| Command | Effect |
|---|---|
| `adm:aide` | Lists the commands |
| `adm:joueurs` | Players in game and their positions |
| `adm:pos <player>` | Position of a player |
| `adm:inv <player>` | Current inventory of a player, as known by the server |
| `adm:give <player> <item> [amount] [quality]` | Puts items straight into the player's inventory (dropped at their feet if full) |
| `adm:tp <player> <player>` | Teleports the first player next to the second |
| `adm:tp <player> <x> <z>` | Teleports a player to coordinates (placed on the ground) |

- Player names are case-insensitive, and the start of a name is enough when unambiguous (`adm:inv clau`).
- Items use the English prefab name: `Copper`, `Tin`, `Bronze`, `Wood`, `FineWood`, `Stone`, `IronScrap`, `SwordIron`, `ArmorBronzeChest`…
- Every command is checked **by the server** against `adminlist.txt`. A non-admin gets *"Tu n'es pas admin du serveur"* and the attempt is logged.

## The journal

On the server, in `BepInEx/config/journal/`:

| File | Content |
|---|---|
| `<Player>_<SteamID>.log` | Everything since the player's last character save |
| `<Player>_<SteamID>.1.log`, `.2.log`, `.3.log` | The previous save periods (the oldest is deleted) |
| `admin.log` | Every admin command, accepted or refused |

Example:

```
2026-11-12 14:21:21 | (73, 32, -399) | connexion
2026-11-12 14:21:21 | (73, 32, -399) | apparition : ArmorTrollLeatherChest ×1, ArrowWood ×106, Coins ×186, …
2026-11-12 14:21:36 | (74, 33, -415) | reçoit de Admin : 15 Copper
2026-11-12 14:21:36 | (74, 33, -415) | +15 Copper
2026-11-12 14:22:02 | (79, 34, -412) | +5 Bronze, -10 Copper, -5 Tin
2026-11-12 14:36:35 | (80, 34, -410) | personnage sauvegardé, inventaire : Bronze ×22, Coins ×186, …
```

What is recorded:

- **connexion / déconnexion**, with the inventory at departure;
- **position** every 60 seconds;
- every **inventory change** (`+15 Copper`, `-2 Wood`), checked every second, with the position;
- the **full inventory** every 5 minutes if it changed;
- **deaths**, with what was left in the tombstone;
- admin actions (`reçoit de…`, `téléporté par…`).

### Rotation on save

Every world save (every 30 minutes by default), the server asks each connected player's game to save its character. When the player's game confirms that the character **was actually saved**, the server writes the saved inventory and starts a new journal file for that player. The last 3 save periods are kept.

If a player's game crashes before the save, their journal is **not** rotated: everything since their last successful save stays in the current file, which is exactly what you need to give back what was lost.

## Configuration

`BepInEx/config/emeraude.admin.cfg`:

| Setting | Default | Side | Description |
|---|---|---|---|
| `Journal.PositionEverySeconds` | `60` | server | Interval between two logged positions |
| `Journal.SnapshotEveryMinutes` | `5` | player | Interval between two full inventories sent (only if changed) |
| `Journal.KeptSaves` | `3` | server | Number of archived save periods per player |

## Compatibility

- Valheim **1.0.21** (network version 41), BepInExPack Valheim **5.4.2360**.
- No Jotunn dependency, no change to save files, no change to gameplay.
- Used on a server running Valheim Plus, Epic Loot, Equipment and Quick Slots, Network Performance System, Craft From Containers, Better Archery, Mount Up Restored, Plant Everything, ZenRaids.
- With Equipment and Quick Slots, items moved to the equipment/quick slots may show as `-1` / `+1` in the journal.
- Works on worlds hosted from the game too (the host is always admin).

## Privacy

The journals contain player positions and inventories. They stay on the server; nothing is sent anywhere else.

---

# Version française

Outils d'admin pour **serveur dédié Valheim** :

- un **journal par joueur** sur le serveur : connexions, positions, chaque objet gagné ou perdu, morts, sauvegardes du personnage ;
- des **commandes admin** dans la console F5 pour donner des objets et téléporter des joueurs, ce que Valheim ne permet plus aux admins d'un serveur dédié depuis la 1.0.

À installer **sur le serveur et chez tous les joueurs**. Les admins sont ceux de `adminlist.txt`.

| Commande | Effet |
|---|---|
| `adm:aide` | Liste des commandes |
| `adm:joueurs` | Joueurs en jeu et leur position |
| `adm:pos <joueur>` | Position d'un joueur |
| `adm:inv <joueur>` | Inventaire actuel d'un joueur |
| `adm:give <joueur> <objet> [quantité] [qualité]` | Donne un objet directement dans l'inventaire (nom anglais du prefab : `Copper`, `Tin`, `Bronze`…) |
| `adm:tp <joueur> <joueur>` | Téléporte le premier joueur à côté du second |
| `adm:tp <joueur> <x> <z>` | Téléporte un joueur à des coordonnées |

Pas besoin de `devcommands` : ni le personnage ni les objets ne sont marqués « triche ».

Le journal est dans `BepInEx/config/journal/` sur le serveur. À chaque sauvegarde du monde, quand le jeu d'un joueur confirme que son personnage est bien enregistré, son journal est archivé (`.1.log`, `.2.log`, `.3.log`). Si son jeu plante avant, rien n'est archivé : on garde tout depuis sa dernière sauvegarde réussie, de quoi lui rendre ce qu'il a perdu.
