# Emeraude Admin — sources

Mod BepInEx pour serveur dédié Valheim : journal des joueurs et commandes admin `adm:*`.
La description publique (fonctionnement, commandes, journal, config) est dans
[thunderstore/README.md](thunderstore/README.md), l'historique dans
[thunderstore/CHANGELOG.md](thunderstore/CHANGELOG.md).

## Arborescence

| Chemin | Rôle |
|---|---|
| `src/Plugin.cs` | Point d'entrée BepInEx, version, réglages (`emeraude.admin.cfg`) |
| `src/Net.cs` | Noms des RPC et format texte des inventaires |
| `src/Patches.cs` | Patches Harmony (démarrage, console, déconnexion, sauvegarde, mort) |
| `src/Commands.cs` | Commandes `adm:*` de la console F5 |
| `src/Server.cs` | Côté serveur : journal, archivage, droits admin, exécution des commandes |
| `src/Client.cs` | Côté joueur : suivi de l'inventaire, tp, give |
| `thunderstore/` | Manifeste, icône, README et changelog du paquet |
| `thunderstore.toml` | Configuration de publication (`tcli`) |
| `build.sh` | Compile et fabrique `dist/EmeraudeAdmin-<version>.zip` |
| `publish.sh` | Compile puis publie sur Thunderstore |

## Pré-requis

- SDK .NET 9 (`~/.local/opt/dotnet-9`) ;
- Valheim installé (DLL du jeu dans `valheim_Data/Managed`) ;
- un profil r2modman avec BepInEx (DLL dans `BepInEx/core`).

`build.sh` copie ces DLL dans `lib/` (ignoré par git) ; les chemins se changent avec
`VALHEIM_MANAGED` et `BEPINEX_CORE`.

## Compiler

```bash
./build.sh
```

## Publier une nouvelle version

1. Mets le nouveau numéro de version dans `src/Plugin.cs`, `src/EmeraudeAdmin.csproj`,
   `thunderstore/manifest.json` et `thunderstore.toml` (le build refuse s'il en manque un).
2. Ajoute une section `## <version>` dans `thunderstore/CHANGELOG.md`.
3. `./publish.sh` (jeton de l'équipe Thunderstore dans `.env`, voir `.env.example`).
4. Sur le serveur : change la version dans `/srv/valheim/mods.lock`, puis
   `docker compose stop && python3 scripts/install-mods.py && docker compose up -d`.
5. Relance le bot Discord (`reve-emeraude-discord`) avec le nouveau profil : il publie
   un nouveau code r2modman dans #🛠️-serveur.

## Notes techniques

- Valheim 1.0.21 : `Terminal.IsCheatsEnabled()` exige d'être le serveur, d'où des commandes
  à nous plutôt que `spawn`/`goto`.
- Le serveur demande la sauvegarde des personnages via la RPC vanilla `SavePlayerProfile`
  à chaque sauvegarde du monde ; le patch sur `Game.SavePlayerProfile` confirme au serveur
  (`m_saveTimer` n'est remis à zéro que si la sauvegarde a eu lieu).
- Les objets donnés passent par `Inventory.AddItem(..., cheated: false)`.
