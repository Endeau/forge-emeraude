# Emeraude Prestige — sources

Mod BepInEx (Jötunn) pour Valheim : les monstres ignorent un joueur dont l'armure a de l'avance sur leur
biome. La description publique est dans [thunderstore/README.md](thunderstore/README.md), l'historique dans
[thunderstore/CHANGELOG.md](thunderstore/CHANGELOG.md).

## Arborescence

| Chemin | Rôle |
|---|---|
| `src/Plugin.cs` | Point d'entrée, réglages synchronisés (`emeraude.prestige.cfg`), listes par défaut des armures et des monstres par niveau |
| `src/Prestige.cs` | Décision « ce monstre ignore ce joueur », niveau du joueur lu dans son ZDO, provocation, contrôle des noms au chargement |
| `src/Patches.cs` | Branchement sur `MonsterAI.UpdateTarget`, `BaseAI.IsEnemy`, `MonsterAI.OnDamaged`, `Character.Damage` |
| `thunderstore/` | Manifeste, icône, README et changelog du paquet |
| `build.sh` / `publish.sh` | Compile et fabrique le paquet / publie sur Thunderstore |

## Fonctionnement

- L'IA d'un monstre tourne chez le propriétaire de sa zone (un joueur proche), pas sur le serveur : le mod
  doit être chez tout le monde (`EveryoneMustHaveMod`).
- `BaseAI.IsEnemy` sert aussi à décider si un coup blesse (`Attack`, `Projectile`, `Aoe`). Le postfix ne le
  fausse que pendant `MonsterAI.UpdateTarget` (le prefix note le monstre jugé), où il décide de la nouvelle
  cible (`FindEnemy`, y compris le repli `HuntPlayer`) et lâche la cible courante. Un monstre qui lâche ainsi
  un joueur est désalerté, sinon il irait le chercher à sa dernière position.
- Niveau du joueur : `s_helmetItem`, `s_chestItem`, `s_legItem` du ZDO du joueur, hachage du nom de l'objet
  écrit par `VisEquipment`, donc lisible pour les autres joueurs. Le minimum des trois ; absent ou hors liste = 0.
- Niveau du monstre : hachage du prefab de son ZDO. Hors liste, boss, étoilé (`GetLevel() > 1`) ou de raid
  (`IsEventCreature`, posé par `SpawnSystem`, que ZenRaids utilise) = attaque toujours.
- Provocation : `MonsterAI.OnDamaged` (le joueur frappe) et `Character.Damage` côté attaquant (le monstre
  frappe) écrivent l'heure réseau dans le ZDO du monstre, clé `emeraude_prestige_provoked_<UserID du joueur>`.
  Elle survit aux changements de propriétaire (NetworkPerformanceSystem en fait souvent).
- Au chargement, le journal liste les noms inconnus du jeu dans les listes, les armures sans niveau et les
  créatures sans niveau.

## Pré-requis

Les mêmes que pour `emeraude-sanctuaire`. `.env` et `tools/` sont des liens vers ceux d'emeraude-admin.
