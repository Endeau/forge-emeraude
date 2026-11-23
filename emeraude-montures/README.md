# Emeraude Montures — sources

Mod BepInEx pour Valheim : réglages des montures de Mount Up Restored (montures autorisées, PV, caméra).
La description publique est dans [thunderstore/README.md](thunderstore/README.md), l'historique dans
[thunderstore/CHANGELOG.md](thunderstore/CHANGELOG.md).

## Arborescence

| Chemin | Rôle |
|---|---|
| `src/Plugin.cs` | Point d'entrée BepInEx, version, réglage (`emeraude.montures.cfg`) |
| `src/Mounts.cs` | Montures autorisées : loup, sanglier et lox ; PV du cavalier : loup et sanglier seulement (`Tameable.UseItem`, `Sadle.Interact`) |
| `src/Health.cs` | PV des montures (`Sadle.FixedUpdate`, `Character.SetupMaxHealth`) |
| `src/Camera.cs` | Caméra en selle (`GameCamera.GetOffsetedEyePos`, `GameCamera.ApplyCameraTilt`) |
| `thunderstore/` | Manifeste, icône, README et changelog du paquet |
| `build.sh` / `publish.sh` | Compile et fabrique le paquet / publie sur Thunderstore |

## Fonctionnement

- PV : quand un joueur prend la selle, le propriétaire de la bête la lui cède (`Sadle.RPC_RequestControl`).
  Le jeu du cavalier, devenu propriétaire, aligne les PV max sur les siens dans `Sadle.FixedUpdate` et
  les enregistre dans le ZDO (`emeraude_mount_maxhealth`). `Character.Awake` remet les PV d'origine
  à une bête chargée en pleine santé (`SetupMaxHealth`) : le postfix rétablit la valeur enregistrée.
- Caméra : quand le joueur est attaché, `GameCamera.GetOffsetedEyePos` part de la position brute du
  joueur, que Mount Up colle à un os du dos (`Player.Update`). Le postfix la remplace par la position
  du cavalier dans le repère de la monture, lissée (`SmoothDamp`). `ApplyCameraTilt` penchait la
  caméra avec le corps du cavalier : elle reste droite.

## Pré-requis

Les mêmes que pour `emeraude-admin`. `.env` et `tools/` sont des liens vers ceux d'emeraude-admin.
