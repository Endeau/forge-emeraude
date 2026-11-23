# Emeraude Coffres

Two crafting improvements for [Craft From Containers](https://thunderstore.io/c/valheim/p/ValheimPortMods/Craft_Build_Smelt_Cook_Fuel_Pull_From_Containers/) users:

- recipes that accept any one of several ingredients, like any fish at the food preparation table, now find that ingredient in nearby chests;
- two buttons next to Craft: **x10** and **All**, which makes as many items as possible at once;
- the server sets the chest range and the stonecutter's build range for every player.

> Available in **English** (default) and **French**, see [Configuration](#configuration). *(Version française plus bas.)*

## The problem

Some recipes accept any one of several ingredients: at the food preparation table, a fish recipe takes any kind of fish. The game needs only one of them, in a single quality (the same number of stars). Craft From Containers asks for all of them, and the game only looks for the chosen ingredient in the player's inventory: fish stored in a chest was never found.

## The fix

- The craft button is enabled when one of the ingredients is available in a single quality, counting the player's inventory and the chests in Craft From Containers' range.
- When crafting, the missing amount is moved from the chests into the player's inventory, then the game uses it as usual. If the inventory is full, a message says so and nothing is crafted.

Craft From Containers keeps doing everything else. Its settings apply here too: range, ignored chest types, `Enabled`, and the key that prevents pulling.

## Crafting many at once

Two buttons appear left of Craft, on the same row:

- **x10** makes 10 items at once;
- **All (N)** makes as many as possible: as many as the ingredients allow, chests in range included, and as fit in the inventory. N is that number.

A button is greyed out when there isn't enough. They are hidden for upgrades. **Shift + Craft** still makes 5 items, like the base game.

## Chest range set by the server

Craft From Containers reads its range (`ContainerRange`, 10 m by default) in each player's own settings. With this mod on the server, the server sends its `Server.ContainerRange` (15 m by default) to every player when they spawn. It replaces their range while they play there, without changing their file, and their own range comes back when they leave. `0` lets each player keep their own.

The server also multiplies the stonecutter's build range by `Server.StonecutterRangeMultiplier` (1.5 by default) for every player. The area where monsters don't spawn around the stonecutter follows its build range, so it grows too.

## Installation

Install it on **every player's game**, next to Craft From Containers. Install it on the server too for the server to set these ranges; without it on the server, each player keeps the base ranges.

## Configuration

`BepInEx/config/emeraude.coffres.cfg`

| Setting | Default | Effect |
|---|---|---|
| `General.Language` | `English` | `English` or `French`: language of the in-game messages |
| `Server.ContainerRange` | `15` | Server only: chest range in meters sent to every player (0 to 100). `0` = each player keeps their own |
| `Server.StonecutterRangeMultiplier` | `1.5` | Server only: multiplies the stonecutter's build range for every player (1 to 5). `1` = base game |

---

# Version française

Deux améliorations de fabrication pour les joueurs de [Craft From Containers](https://thunderstore.io/c/valheim/p/ValheimPortMods/Craft_Build_Smelt_Cook_Fuel_Pull_From_Containers/) :

- les recettes qui acceptent un ingrédient au choix, comme n'importe quel poisson à la table de préparation, trouvent désormais cet ingrédient dans les coffres proches ;
- deux boutons à côté de « Fabriquer » : **x10** et **Tout**, qui fabrique autant d'exemplaires que possible d'un coup ;
- le serveur fixe la portée des coffres et la portée de construction de l'établi en pierre pour tous les joueurs.

## Le problème

Certaines recettes acceptent un ingrédient au choix parmi plusieurs : à la table de préparation, une recette de poisson prend n'importe quel poisson. Le jeu n'en demande qu'un, d'une seule qualité (le même nombre d'étoiles). Craft From Containers les exigeait tous, et le jeu ne cherche l'ingrédient choisi que dans l'inventaire du joueur : le poisson rangé dans un coffre n'était jamais trouvé.

## Le correctif

- Le bouton « Fabriquer » s'active dès qu'un des ingrédients est disponible en quantité suffisante dans une même qualité, en comptant l'inventaire du joueur et les coffres à portée de Craft From Containers.
- À la fabrication, la quantité manquante passe des coffres à l'inventaire du joueur, puis le jeu l'utilise normalement. Si l'inventaire est plein, un message le signale et rien n'est fabriqué.

Craft From Containers continue de gérer tout le reste, et ses réglages s'appliquent aussi ici : portée, types de coffres ignorés, `Enabled` et la touche qui empêche de puiser dans les coffres.

## Fabriquer en lot

Deux boutons apparaissent à gauche de « Fabriquer », sur la même ligne :

- **x10** fabrique 10 objets d'un coup ;
- **Tout (N)** en fabrique autant que possible : autant que les ingrédients le permettent, coffres à portée compris, et que l'inventaire peut en contenir. N est ce nombre.

Un bouton est grisé quand il n'y a pas assez. Ils sont cachés pour les améliorations. **Maj + Fabriquer** fabrique toujours 5 objets, comme dans le jeu de base.

## Portée des coffres fixée par le serveur

Craft From Containers lit sa portée (`ContainerRange`, 10 m par défaut) dans les réglages de chaque joueur. Avec ce mod sur le serveur, le serveur envoie son `Server.ContainerRange` (15 m par défaut) à chaque joueur quand il apparaît. Elle remplace sa portée tant qu'il joue sur ce serveur, sans modifier son fichier, et il retrouve la sienne en partant. `0` laisse chacun garder la sienne.

Le serveur multiplie aussi la portée de construction de l'établi en pierre par `Server.StonecutterRangeMultiplier` (1,5 par défaut) pour chaque joueur. La zone où les monstres n'apparaissent pas autour de l'établi suit sa portée, elle s'agrandit donc aussi.

## Installation et réglages

À installer **chez chaque joueur**, à côté de Craft From Containers. À installer aussi sur le serveur pour qu'il fixe ces portées ; sans lui, chaque joueur garde celles du jeu de base.

Le mod est en anglais par défaut. Pour le passer en français, mets `Language = French` dans `BepInEx/config/emeraude.coffres.cfg`.
