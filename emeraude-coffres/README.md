# Emeraude Coffres — sources

Mod BepInEx pour Valheim : complète Craft From Containers pour les recettes « un seul ingrédient au
choix » (`Recipe.m_requireOnlyOneIngredient`), comme les recettes de poisson de la table de
préparation. La description publique est dans [thunderstore/README.md](thunderstore/README.md),
l'historique dans [thunderstore/CHANGELOG.md](thunderstore/CHANGELOG.md).

## Arborescence

| Chemin | Rôle |
|---|---|
| `src/Plugin.cs` | Point d'entrée BepInEx, version, réglage de langue (`emeraude.coffres.cfg`) |
| `src/MultiCraft.cs` | Boutons « x10 » et « Tout » : copies du bouton Fabriquer posées sur sa ligne, qu'il partage (`InventoryGui.Awake`), mis à jour dans `UpdateRecipe`, nombre posé par le prefix d'`OnCraftPressed` |
| `src/L.cs` | Textes anglais / français (`General.Language`) |
| `src/Chests.cs` | `Player.HaveRequirementItems` (postfix), `InventoryGui.DoCrafting` (prefix) |
| `src/Range.cs` | Portées imposées par le serveur : RPC demandé à `Player.OnSpawned`, réponse avec `Server.ContainerRange` et `Server.StonecutterRangeMultiplier`, appliqués à `maxRange` de Craft From Containers (sans sauvegarde) et au `m_rangeBuild` de `piece_stonecutter` (prefab et instances), rétablis à `ZNet.OnDestroy` |
| `thunderstore/` | Manifeste, icône, README et changelog du paquet |
| `build.sh` / `publish.sh` | Compile et fabrique le paquet / publie sur Thunderstore |

## Fonctionnement

- Vanilla : pour ces recettes, `HaveRequirementItems` demande un seul ingrédient, en quantité
  suffisante dans une même qualité. `DoCrafting` le cherche avec `Recipe.GetAmount` →
  `Player.GetFirstRequiredItem`, dans l'inventaire du joueur seulement, et abandonne sinon.
- Craft From Containers 4.1.0 ignore le drapeau : son postfix de `HaveRequirementItems` exige tous les
  ingrédients, et son `ConsumeResources` n'est jamais appelé pour ces recettes.
- Le postfix ajoute les coffres de `BepInExPlugin.GetNearbyContainers` (Craft From Containers, appelé
  par réflexion, mêmes réglages). Le prefix de `DoCrafting` déplace la quantité manquante vers
  l'inventaire du joueur avant que le jeu ne la retire.

- Fabrication en lot : `OnCraftPressed` lance une fabrication multiple de `m_multiCraftAmount` (5) quand Maj est
  tenue ou que `m_touchMultiCrafting` est levé (bouton tactile du jeu). Les boutons lèvent ce drapeau et appellent
  `OnCraftPressed` ; son prefix pose leur nombre (10, ou le plus grand N), ou remet 5 pour un clic normal. `DoCrafting`
  relit `m_multiCraftAmount` en fin de barre. Le plus grand N vérifie `Player.HaveRequirements(recipe, false, 1, N)`
  (coffres compris grâce aux postfixes) et `Inventory.CanAddItem` : doublement puis dichotomie jusqu'à 999, gardé 0,5 s.

- Portée : Craft From Containers ne synchronise pas `ContainerRange`. À l'apparition du joueur local, `EmeraudeCoffres_AskRange`
  part vers le serveur, qui répond `EmeraudeCoffres_SetRange(portée)` si `Server.ContainerRange` > 0. Le joueur garde sa
  valeur, pose celle du serveur avec `SaveOnConfigSet` coupé (son fichier ne change pas), et vide `lastPosition` pour que
  `GetNearbyContainers` recalcule sa liste. La sienne revient quand `ZNet` est détruit (sortie du monde).
- Établi en pierre : le même RPC porte un multiplicateur. Le joueur multiplie le `m_rangeBuild` du prefab `piece_stonecutter`
  (valeur de base lue une fois) et des établis chargés. `CraftingStation.GetExtensions` en tire `m_buildRange` toutes les 2 s,
  qui sert au cercle affiché, à `HaveBuildStationInRange` et au rayon de la zone `PlayerBase` (pas d'apparition de monstres).

## Pré-requis

Les mêmes que pour `emeraude-admin`. `.env` et `tools/` sont des liens vers ceux d'emeraude-admin.
