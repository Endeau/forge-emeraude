# Forge Émeraude

Mods Valheim faits maison pour le serveur **Rêve Émeraude**. Ils sont publiés sur Thunderstore sous l'équipe
[Emeraude](https://thunderstore.io/c/valheim/p/Emeraude/) et s'installent avec r2modman.

Chaque mod a son propre dépôt, avec son code et l'historique de ses versions : cette page les rassemble.

## Les mods

| Mod | Version | Ce qu'il fait | Où l'installer |
|---|---|---|---|
| [emeraude-admin](https://github.com/Endeau/emeraude-admin) | 1.4.2 | Journal par joueur sur le serveur et commandes `adm:` (téléportation, objets, dieu, fantôme, vol…) | serveur + joueurs |
| [emeraude-sanctuaire](https://github.com/Endeau/emeraude-sanctuaire) | 1.1.0 | Statue émeraude : pas de monstres ni de raids dans son rayon | serveur + joueurs |
| [emeraude-prestige](https://github.com/Endeau/emeraude-prestige) | 1.0.1 | Les monstres ignorent un joueur dont l'armure a un biome d'avance | serveur + joueurs |
| [emeraude-coffres](https://github.com/Endeau/emeraude-coffres) | 1.4.0 | Complète Craft From Containers : ingrédients au choix, x10 / Tout, portée imposée par le serveur | serveur + joueurs |
| [emeraude-bois-resine](https://github.com/Endeau/emeraude-bois-resine) | 1.1.0 | Pièces en bois résiné qui ne s'usent plus à la pluie | serveur + joueurs |
| [emeraude-montures](https://github.com/Endeau/emeraude-montures) | 1.1.0 | Réglages de Mount Up Restored : montures permises, vie, caméra | serveur + joueurs |
| [emeraude-repos](https://github.com/Endeau/emeraude-repos) | 1.0.0 | Buff Reposé 1,5 fois plus long | joueurs |
| [emeraude-minerais](https://github.com/Endeau/emeraude-minerais) | 1.1.0 | Compteurs de minerai par joueur pour le classement Discord | serveur + joueurs |
| [emeraude-deconstruction](https://github.com/Endeau/emeraude-deconstruction) | 1.2.0 | Déconstruire bateaux et charrettes au marteau | serveur + joueurs |
| [emeraude-construction](https://github.com/Endeau/emeraude-construction) | 1.0.0 | Retour arrière remet l'accroche automatique en construction | joueurs (en test) |
| [emeraude-bucheron](https://github.com/Endeau/emeraude-bucheron) | 1.0.0 | Résine et graines garanties sur les grands arbres | serveur + joueurs (en test) |

Chaque dépôt a son `README.md` (fonctionnement, réglages) et son `thunderstore/CHANGELOG.md` (détail des versions).

## Compiler et publier

Chaque mod se construit seul, depuis son dépôt :

```bash
./build.sh     # compile et fabrique dist/<Mod>-<version>.zip
./publish.sh   # build puis publication sur Thunderstore
```

Il faut le SDK .NET (9, dans `~/.local/opt/dotnet-9` par défaut), Valheim et un profil r2modman avec BepInEx :
`build.sh` copie les DLL du jeu et de BepInEx dans `lib/`, qui n'est pas versionné. Les chemins se changent avec
`VALHEIM_MANAGED` et `BEPINEX_CORE`. `publish.sh` lit le jeton de l'équipe Thunderstore dans `.env`
(`THUNDERSTORE_TOKEN=…`, jamais commité).

## Journal des versions

Un commit par version, dans le dépôt du mod. Toutes les sorties, dans l'ordre :

| Date | Commit |
|---|---|
| 12/11 | emeraude-admin 1.2.0 : journal des joueurs et commandes admin |
| 12/11 | emeraude-admin : publication sous l'équipe Thunderstore Emeraude |
| 12/11 | emeraude-admin 1.3.0 : anglais par défaut, français en option |
| 17/11 | emeraude-sanctuaire 1.1.0 : statue émeraude contre les monstres et les raids |
| 18/11 | emeraude-prestige 1.0.1 : les monstres ignorent les joueurs bien équipés |
| 23/11 | emeraude-coffres 1.4.0 : fabrication depuis les coffres, portée imposée par le serveur |
| 23/11 | emeraude-bois-resine 1.1.0 : bois résiné qui ne pourrit plus |
| 23/11 | emeraude-montures 1.1.0 : réglages des montures de Mount Up Restored |
| 23/11 | emeraude-repos 1.0.0 : buff Reposé 1,5 fois plus long |
| 23/11 | emeraude-admin 1.4.0 : 16 nouvelles commandes admin, hôte solo ciblable |
| 23/11 | emeraude-minerais 1.1.0 : compteurs de minerai par joueur pour le classement |
| 23/11 | emeraude-admin 1.4.2 : dieu et fantôme gardés après la mort, adm:degats |
| 23/11 | emeraude-deconstruction 1.2.0 : déconstruire bateaux et charrettes au marteau |
| 23/11 | emeraude-construction 1.0.0 : Retour arrière remet l'accroche automatique |
| 24/11 | emeraude-bucheron 1.0.0 : résine et graines garanties sur les grands arbres |

Pour une nouvelle version : un commit `<mod> <version> : <ce qui change>` dans le dépôt du mod, avec le détail dans le corps du message, et une ligne de plus ici.
