# Emeraude Prestige

Monsters leave you alone once your armor outclasses their biome. Wear Iron and the Black Forest stops bothering you while you gather wood.

## The rule

Every armor set and every monster has the tier of its biome. A monster ignores you when your armor tier is at least the monster's tier + 1 (setting `Prestige.Gap`).

Your tier is the **weakest** of your helmet, chest and legs. A missing piece, or a hat or circlet that is not an armor piece, counts as 0.

| You wear… | Monsters that ignore you |
|---|---|
| Rags, Leather | none |
| Troll, Bronze, Bear | Greyling, Neck, **Greydwarf** |
| Iron, Root | + all of the Black Forest: Greydwarf Brute and Shaman, Troll, Skeleton, Ghost, Bear |
| Wolf, Fenris | + the Swamp |
| Padded, Lox, Vilebone | + the Mountains |
| Carapace, Eitr-weave | + the Plains |
| Flametal, Ask, Embla | + the Mistlands |
| Deep North armor | + the Ashlands |

A monster's tier comes from its species, wherever it roams: a Greydwarf in the Meadows still ignores you in Bronze.

- **Hit a monster and it fights back.** It forgets you after 30 s without a blow either way (`Prestige.ForgetSeconds`).
- **Always aggressive:** boars, starred monsters, raid monsters, bosses, sea serpents, and any creature not listed in the config.
- Other players are judged on their own armor: a monster ignores you and still attacks your friend in Bronze.

## Installation

Requires [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/). Install it on the **server and every player's game**: monsters are run by the game of a nearby player, so everyone needs the same rules.

## Configuration

`BepInEx/config/emeraude.prestige.cfg`, set on the server and pushed to the players.

| Setting | Default | Effect |
|---|---|---|
| `Prestige.Enabled` | `true` | Turns the mod on or off. |
| `Prestige.Gap` | `1` | Biomes of advance needed (0 to 8). |
| `Prestige.ForgetSeconds` | `30` | How long a provoked monster keeps fighting without a blow. |
| `Prestige.StarredStayAggressive` | `true` | Starred monsters always attack. |
| `Prestige.RaidsStayAggressive` | `true` | Raid monsters always attack. |
| `Armor.Tier1` … `Tier8` | vanilla sets | Armor pieces of each tier (item names). |
| `Monsters.Tier1` … `Tier8` | vanilla monsters | Monsters of each tier (prefab names). Move one to another tier to change when it ignores you. |

At startup the log lists unknown names in these lists, armor pieces without a tier, and creatures without a tier.
