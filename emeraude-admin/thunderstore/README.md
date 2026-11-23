# Emeraude Admin

Admin tools for **Valheim dedicated servers**, made for a small private server among friends:

- a **journal per player**, written on the server: connections, positions, every item gained or lost, deaths, character saves;
- **admin console commands** to give items to a player and teleport players, which vanilla Valheim no longer allows admins to do on a dedicated server.

> Available in **English** (default) and **French** — see [Configuration](#configuration). *(Version française plus bas.)*

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
| `adm:help` | Lists the commands |
| `adm:players` | Players in game and their positions |
| `adm:pos <player>` | Position of a player |
| `adm:inv <player>` | Current inventory of a player, as known by the server |
| `adm:give <player> <item> [amount] [quality]` | Puts items straight into the player's inventory (dropped at their feet if full) |
| `adm:tp <player> <player>` | Teleports the first player next to the second |
| `adm:tp <player> <x> <z>` | Teleports a player to coordinates (placed on the ground) |
| `adm:portal` | Lists the portals and their positions |
| `adm:portal <player> <portal name>` | Teleports a player in front of a portal (of two linked portals, the farthest from the player) |
| `adm:tomb <player> [number]` | Lists a player's tombstones (online or not), or teleports the player to one of them |
| `adm:take <player> <item> [amount]` | Removes items from a player's inventory |
| `adm:skill <player> <skill> <level>` | Sets a skill level, 0 to 100 (`Swords`, `Axes`, `Run`, `Pickaxes`…) |
| `adm:heal <player>` | Full health, stamina and eitr; removes burning, poison, frost, wet, cold, freezing, smoke, tar |
| `adm:repair <player>` | Repairs every item the player carries or wears |
| `adm:god [player] [on\|off]` | Invincible mode (yourself by default, toggles without on/off), kept after death and reconnection until off |
| `adm:ghost [player] [on\|off]` | Monsters ignore the player, kept like god |
| `adm:fly [player] [on\|off]` | Fly mode: jump to go up, crouch to go down |
| `adm:damage [player] <multiplier>` | Multiplies the damage the player deals (1 = normal), until the game is closed |
| `adm:journal <player> [lines]` | Last lines (20 by default) of a player's journal, online or not |
| `adm:ores [player]` | Ore leaderboard, or a player's details ([Emeraude Minerais](https://thunderstore.io/c/valheim/p/Emeraude/EmeraudeMinerais/) on the server) |
| `adm:say <message>` | Message in the middle of every player's screen |
| `adm:day` / `adm:night` | Skips to the morning / the evening, like sleeping |
| `adm:raid <player> <raid>` / `adm:raid stop` | Starts a raid near a player (without a name: lists the raids) / stops the current raid |
| `adm:kick <player> [reason]` | Kicks a player, showing them the reason |

- Player names are case-insensitive, and the start of a name is enough when unambiguous (`adm:inv clau`).
- Items use the English prefab name: `Copper`, `Tin`, `Bronze`, `Wood`, `FineWood`, `Stone`, `IronScrap`, `SwordIron`, `ArmorBronzeChest`…
- Every command is checked **by the server** against `adminlist.txt`. A non-admin gets *"You are not an admin of this server"* and the attempt is logged.
- French aliases also work: `adm:joueurs`, `adm:aide`, `adm:portail`, `adm:tombe`, `adm:retirer`, `adm:competence`, `adm:soigner`, `adm:reparer`, `adm:dieu`, `adm:fantome`, `adm:vol`, `adm:degats`, `adm:minerais`, `adm:dire`, `adm:jour`, `adm:nuit`, `adm:expulser`.
- `take`, `skill`, `heal`, `repair`, `god`, `ghost` and `fly` are applied by the player's own game: they need Emeraude Admin 1.4.0 there. God, ghost and fly modes end when the player leaves.

## The journal

On the server, in `BepInEx/config/journal/`:

| File | Content |
|---|---|
| `<Player>_<SteamID>.log` | Everything since the player's last character save |
| `<Player>_<SteamID>.1.log`, `.2.log`, `.3.log` | The previous save periods (the oldest is deleted) |
| `admin.log` | Every admin command, accepted or refused |

Example:

```
2026-11-12 14:21:21 | (73, 32, -399) | connected
2026-11-12 14:21:21 | (73, 32, -399) | spawned with: ArmorTrollLeatherChest ×1, ArrowWood ×106, Coins ×186, …
2026-11-12 14:21:36 | (74, 33, -415) | receives from Admin: 15 Copper
2026-11-12 14:21:36 | (74, 33, -415) | +15 Copper
2026-11-12 14:22:02 | (79, 34, -412) | +5 Bronze, -10 Copper, -5 Tin
2026-11-12 14:36:35 | (80, 34, -410) | character saved, inventory: Bronze ×22, Coins ×186, …
```

What is recorded:

- **connections / disconnections**, with the inventory when leaving;
- **position** every 60 seconds;
- every **inventory change** (`+15 Copper`, `-2 Wood`), checked every second, with the position;
- the **full inventory** every 5 minutes if it changed;
- **deaths**, with what was left in the tombstone;
- admin actions (`receives from…`, `teleported by…`).

### Rotation on save

Every world save (every 30 minutes by default), the server asks each connected player's game to save its character. When the player's game confirms that the character **was actually saved**, the server writes the saved inventory and starts a new journal file for that player. The last 3 save periods are kept.

If a player's game crashes before the save, their journal is **not** rotated: everything since their last successful save stays in the current file, which is exactly what you need to give back what was lost.

## Configuration

`BepInEx/config/emeraude.admin.cfg`:

| Setting | Default | Side | Description |
|---|---|---|---|
| `General.Language` | `English` | both | `English` or `French`: in-game messages and, on the server, the journal and the replies to admins |
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
| `adm:aide` (ou `adm:help`) | Liste des commandes |
| `adm:joueurs` (ou `adm:players`) | Joueurs en jeu et leur position |
| `adm:pos <joueur>` | Position d'un joueur |
| `adm:inv <joueur>` | Inventaire actuel d'un joueur |
| `adm:give <joueur> <objet> [quantité] [qualité]` | Donne un objet directement dans l'inventaire (nom anglais du prefab : `Copper`, `Tin`, `Bronze`…) |
| `adm:tp <joueur> <joueur>` | Téléporte le premier joueur à côté du second |
| `adm:tp <joueur> <x> <z>` | Téléporte un joueur à des coordonnées |
| `adm:portail [joueur] [nom]` | Sans argument : liste des portails. Sinon téléporte le joueur devant le portail (des deux liés, le plus loin de lui) |
| `adm:tombe <joueur> [numéro]` | Tombes d'un joueur (connecté ou non), ou le téléporte sur l'une d'elles |
| `adm:retirer <joueur> <objet> [quantité]` | Retire un objet de l'inventaire |
| `adm:competence <joueur> <compétence> <niveau>` | Règle une compétence de 0 à 100 (nom anglais : `Swords`, `Axes`, `Run`, `Pickaxes`…) |
| `adm:soigner <joueur>` | Vie, endurance et eitr au maximum, retire brûlure, poison, givre, mouillé, froid, gel, fumée, goudron |
| `adm:reparer <joueur>` | Répare tout ce que le joueur porte ou transporte |
| `adm:dieu [joueur] [on\|off]` | Mode invincible (toi par défaut ; sans on/off, bascule), gardé après une mort ou une reconnexion jusqu'à off |
| `adm:fantome [joueur] [on\|off]` | Les monstres ignorent le joueur, gardé comme dieu |
| `adm:vol [joueur] [on\|off]` | Mode vol : saut pour monter, accroupi pour descendre |
| `adm:degats [joueur] <multiplicateur>` | Multiplie les dégâts infligés par le joueur (1 = normal), jusqu'à la fermeture du jeu |
| `adm:journal <joueur> [lignes]` | Dernières lignes du journal d'un joueur, connecté ou non |
| `adm:minerais [joueur]` | Classement des minerais, ou le détail d'un joueur (Emeraude Minerais sur le serveur) |
| `adm:dire <message>` | Message au centre de l'écran de tous les joueurs |
| `adm:jour` / `adm:nuit` | Passe au matin / au soir, comme en dormant |
| `adm:raid <joueur> <raid>` / `adm:raid stop` | Lance un raid près d'un joueur (sans nom : la liste) / arrête le raid en cours |
| `adm:expulser <joueur> [raison]` | Expulse un joueur en lui affichant la raison |

Pas besoin de `devcommands` : ni le personnage ni les objets ne sont marqués « triche ».

Le mod est en anglais par défaut. Pour le passer en français, mets `Language = French` dans `BepInEx/config/emeraude.admin.cfg` : sur le serveur pour le journal et les réponses aux admins, chez chaque joueur pour les messages affichés dans son jeu.

Le journal est dans `BepInEx/config/journal/` sur le serveur. À chaque sauvegarde du monde, quand le jeu d'un joueur confirme que son personnage est bien enregistré, son journal est archivé (`.1.log`, `.2.log`, `.3.log`). Si son jeu plante avant, rien n'est archivé : on garde tout depuis sa dernière sauvegarde réussie, de quoi lui rendre ce qu'il a perdu.
