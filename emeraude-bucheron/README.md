# Emeraude Bûcheron — sources

Mod BepInEx (Harmony + Jötunn pour EveryoneMustHaveMod) pour Valheim : quand un grand arbre tombe
(`TreeBase.RPC_Damage`), le postfix de `DropTable.GetDropList` complète la liste tirée dans `m_dropWhenDestroyed` :
1 à 3 résines au moins (arbres qui en ont dans leur table) et les graines à leur quantité vanilla si le tirage
n'en a pas sorti. Au démarrage, le journal liste les arbres et leurs tables (`Arbres : …`).
Description publique : [thunderstore/README.md](thunderstore/README.md). Build et publication comme emeraude-minerais.
