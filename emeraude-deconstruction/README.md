# Emeraude Deconstruction — sources

Mod BepInEx pour Valheim : déconstruire les bateaux et les charrettes au marteau, et garder dans une caisse
flottante les matériaux d'un bateau détruit.
La description publique est dans [thunderstore/README.md](thunderstore/README.md), l'historique dans
[thunderstore/CHANGELOG.md](thunderstore/CHANGELOG.md).

## Arborescence

| Chemin | Rôle |
|---|---|
| `src/Plugin.cs` | Point d'entrée BepInEx, version |
| `src/Patches.cs` | Patches Harmony (`Ship.Awake`, `Vagon.Awake`, `Player.RemovePiece`, `Piece.CanBeRemoved`, `Piece.DropResources`) |
| `thunderstore/` | Manifeste, icône, README et changelog du paquet |
| `thunderstore.toml` | Configuration de publication (`tcli`) |
| `build.sh` | Compile et fabrique `dist/EmeraudeDeconstruction-<version>.zip` |
| `publish.sh` | Compile puis publie sur Thunderstore |

## Fonctionnement

- Les prefabs des bateaux et charrettes ont `Piece.m_canBeRemoved = false` : `Player.RemovePiece`
  les ignore. Un postfix sur `Ship.Awake` et `Vagon.Awake` le remet à `true`.
- `Piece.CanBeRemoved` ne vérifie, pour un bateau avec cale, que la cale privée, et rien pour une
  charrette. Le postfix ajoute : personne à bord, charrette non attelée, cale fermée et vide.
- `Piece.CanBeRemoved` sert aussi à `WearNTear` pour ignorer les dégâts de cendres et de lave.
  Nos conditions ne s'appliquent donc que pendant `Player.RemovePiece` (drapeau posé en prefix,
  retiré en finalizer), pour ne pas rendre les bateaux chargés insensibles à ces dégâts.
- Toutes les cales du bateau (`GetComponentsInChildren<Container>`) doivent être fermées et vides.
- Build On Ship : refus si une autre construction (avec son propre `ZNetView`) est dans l'emprise de la
  coque, calculée dans le repère du bateau à partir de ses collisions solides, jusqu'à 12 m au-dessus.
- Chaque refus pose sa raison (`Removal.Refusal`) ; le finalizer de `Player.RemovePiece` l'affiche à la place
  du message du jeu (`$msg_cantremovenow`).
- Bateau détruit (dégâts ou marteau, les deux passent par `WearNTear.Destroy` → `Piece.DropResources`) : un
  prefix donne à la pièce le `m_destroyedLootPrefab` de sa cale (`CargoCrate`, celle du jeu pour le radeau).
  Le jeu range alors les matériaux dans cette caisse flottante au lieu de les lâcher en vrac.

## Pré-requis

Les mêmes que pour `emeraude-admin` : SDK .NET 9 (`~/.local/opt/dotnet-9`), Valheim installé,
profil r2modman `Reve_Emeraude`. `build.sh` copie les DLL du jeu et de BepInEx dans `lib/`.
