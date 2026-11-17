# Emeraude Sanctuaire — sources

Mod BepInEx (Jötunn) pour Valheim : une statue d'émeraude qui empêche les monstres d'apparaître et
d'entrer dans le village, et qui écarte les raids. La description publique est dans
[thunderstore/README.md](thunderstore/README.md), l'historique dans [thunderstore/CHANGELOG.md](thunderstore/CHANGELOG.md).

## Arborescence

| Chemin | Rôle |
|---|---|
| `src/Plugin.cs` | Point d'entrée, réglages synchronisés (`emeraude.sanctuaire.cfg`), pièce `emeraude_sanctuaire` copiée de `guard_stone` sans son `PrivateArea`, teinte verte, traductions |
| `src/Sanctuary.cs` | Zone de la statue (sphère `EffectArea` `PlayerBase` + `NoMonsters`), infobulle, cercle du rayon, Maj + E |
| `src/Raids.cs` | Retire des raids les joueurs dans une zone de statue (`RandEventSystem.GetValidEventPoints`) |
| `thunderstore/` | Manifeste, icône, README et changelog du paquet |
| `build.sh` / `publish.sh` | Compile et fabrique le paquet / publie sur Thunderstore |

## Fonctionnement

- `PlayerBase` est le type de zone des établis : `SpawnSystem.IsSpawnPointGood` et `CreatureSpawner` refusent d'y
  faire apparaître un monstre. On le donne à une sphère du rayon de la statue.
- `NoMonsters` est la zone du marchand : `MonsterAI.UpdateAI` fait fuir un monstre sans cible qui s'en approche
  (boîte englobante + 15 m) et un monstre dont la cible est dedans. Les créatures apprivoisées l'ignorent.
- Raids : le serveur choisit un joueur dans `GetValidEventPoints`. Le postfix retire ceux qui sont à moins du
  rayon d'une statue, trouvées dans les ZDO (le serveur dédié n'a pas forcément les objets chargés).
- Rayon par statue : clé `emeraude_sanctuaire_radius` du ZDO (absente = `Sanctuary.Radius`). Maj + E (`Interact` avec `alt`)
  passe au palier suivant par un RPC au propriétaire du ZDO ; chaque client relit la clé
  chaque seconde et recrée la zone (la boîte `NoMonsters` est figée à l'`Awake` de l'`EffectArea`).
- La pierre de garde bloquait coffres et portes une fois activée : `AddStatue` retire `PrivateArea` du prefab et
  `Sanctuary` reprend son cercle (`CircleProjector`) et son modèle, lueur toujours allumée.
- La zone n'est pas créée sur le fantôme de construction (pas de ZDO), qui montre seulement le cercle.

## Pré-requis

Les mêmes que pour `emeraude-admin`, plus Jötunn dans le profil r2modman `Reve_Emeraude` (copié dans `lib/` par
`build.sh`). `.env` et `tools/` sont des liens vers ceux d'emeraude-admin.
