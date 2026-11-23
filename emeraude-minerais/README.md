# Emeraude Minerais — sources

Mod BepInEx (Harmony, sans Jötunn) pour Valheim : compte par joueur le minerai mis au four, les barres produites et
les barres dépensées. Le serveur écrit les totaux dans un fichier lu par le bot Discord du Rêve d'émeraude
(`~/Workspace/reve-emeraude-discord/classement.py`). Description publique : [thunderstore/README.md](thunderstore/README.md).

## Arborescence

| Chemin | Rôle |
|---|---|
| `src/Plugin.cs` | Plugin, appel de la sauvegarde côté serveur |
| `src/Stats.cs` | RPC `EM_Count` joueur → serveur, totaux en mémoire, fichier `classement.tsv` |
| `src/Patches.cs` | Fours (`Smelter`), barres dépensées (`Player.ConsumeResources`), minerai ramassé (`Humanoid.Pickup`, `ItemDrop.DropItem`) |
| `thunderstore/` | Manifeste, icône, README et changelog du paquet |
| `build.sh` / `publish.sh` | Compile et fabrique le paquet / publie sur Thunderstore |

## Fonctionnement

- Joueur compté : `Player.GetPlayerID()` (personnage) et son nom.
- Minerai mis au four : `Smelter.RPC_AddOre` tourne chez le propriétaire du four ; le déposant est le joueur dont le
  ZDO appartient à l'expéditeur du RPC. Seuls les minerais dont la conversion donne une barre (`Bars`) comptent, pas
  ceux marqués « cheated ».
- Barres produites : le ZDO du four garde `emeraude_by<i>` à côté de chaque `item<i>` de la file, décalé dans
  `RemoveOneOre` comme la file ; `QueueProcessed` crédite le déposant. Le minerai déjà en file avant l'installation
  n'a pas de déposant et n'est pas crédité.
- Minerai récolté : `Humanoid.Pickup` du joueur local, pile lue dans le ZDO avant le ramassage. Les objets créés par
  `ItemDrop.DropItem` (lâchés par un joueur, coffre ou bateau détruit) sont marqués `emeraude_dropped` et ne comptent
  pas ; le minage passe par `DropTable` + `Instantiate`.
- Barres utilisées : préfixe de `Player.ConsumeResources` (fabrication, amélioration, construction), mêmes conditions
  que le jeu pour l'établi améliorant. Non appelé en mode sans coût.
- Serveur : `classement.tsv` dans `BepInEx/config/emeraude-minerais/`, écrit au plus toutes les 30 s s'il a changé
  et à l'arrêt, via un fichier `.tmp` remplacé d'un coup.

## Pré-requis

Les mêmes que pour `emeraude-admin` (BepInEx du profil `Reve_Emeraude`, copié dans `lib/` par `build.sh`).
`.env` et `tools/` sont des liens vers ceux d'emeraude-admin.
