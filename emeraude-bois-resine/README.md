# Emeraude Bois Resine — sources

Mod BepInEx (Jötunn) pour Valheim : une copie résinée de chaque pièce de construction en bois simple, qui ne
s'abîme plus avec l'eau. La description publique est dans [thunderstore/README.md](thunderstore/README.md),
l'historique dans [thunderstore/CHANGELOG.md](thunderstore/CHANGELOG.md).

## Arborescence

| Chemin | Rôle |
|---|---|
| `src/Plugin.cs` | Tout le mod : choix des pièces, copies `<prefab>_resine`, recette au double du bois, teinte ambrée, traductions |
| `thunderstore/` | Manifeste, icône, README et changelog du paquet |
| `build.sh` / `publish.sh` | Compile et fabrique le paquet / publie sur Thunderstore |

## Fonctionnement

- Usure par l'eau : `WearNTear.UpdateWear` retire 5 de vie toutes les 60 s à une pièce mouillée (pluie sans toit
  ou `IsUnderWater`) tant qu'elle est au-dessus de 50 %, seulement si `m_noRoofWear` est vrai. La copie l'a à faux.
- Pièces copiées : celles du marteau (`Hammer`, `m_buildPieces`) de catégorie `BuildingWorkbench`, dont toutes les
  ressources sont du `Wood`, avec `m_noRoofWear` vrai et `m_materialType` `Wood`. La liste est écrite dans
  `LogOutput.log` au lancement.
- Nom : `"$piece_xxx ($emeraude_resine)"`, `Localization.Localize` traduit chaque jeton (`)` termine un jeton).
- Onglet : catégorie Jötunn `Resined wood`, jeton `jotunn_cat_resined_wood`, traduit en français par le mod.
- Teinte : matériaux copiés une fois chacun, `_Color` multipliée par `Amber`.
- `m_rainWet` reste faux sur la copie : pas d'aspect mouillé, et le feu la traite comme sèche sous la pluie.

## Pré-requis

Les mêmes que pour `emeraude-sanctuaire` (Jötunn dans le profil `Reve_Emeraude`, copié dans `lib/` par `build.sh`).
`.env` et `tools/` sont des liens vers ceux d'emeraude-admin.
