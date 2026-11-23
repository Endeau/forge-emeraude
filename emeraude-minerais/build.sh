#!/usr/bin/env bash
# Compile le mod et fabrique le paquet Thunderstore dans dist/.
#   ./build.sh
# Variables (facultatives) :
#   VALHEIM_MANAGED  dossier valheim_Data/Managed du jeu
#   BEPINEX_CORE     dossier BepInEx/core d'un profil r2modman
set -euo pipefail
cd "$(dirname "$0")"

VALHEIM_MANAGED=${VALHEIM_MANAGED:-$HOME/.steam/debian-installation/steamapps/common/Valheim/valheim_Data/Managed}
BEPINEX_CORE=${BEPINEX_CORE:-$HOME/.config/r2modmanPlus-local/Valheim/profiles/Reve_Emeraude/BepInEx/core}
export DOTNET_ROOT=${DOTNET_ROOT:-$HOME/.local/opt/dotnet-9}

VERSION=$(grep -oP 'Version = "\K[0-9.]+' src/Plugin.cs)
for f in src/EmeraudeMinerais.csproj thunderstore/manifest.json; do
  grep -q "$VERSION" "$f" || { echo "Version $VERSION absente de $f : mets-la à jour partout." >&2; exit 1; }
done
grep -q "^## $VERSION" thunderstore/CHANGELOG.md || { echo "Ajoute la version $VERSION dans thunderstore/CHANGELOG.md" >&2; exit 1; }

mkdir -p lib
cp "$VALHEIM_MANAGED"/{assembly_valheim,assembly_utils,UnityEngine,UnityEngine.CoreModule}.dll lib/
cp "$BEPINEX_CORE"/{BepInEx,0Harmony}.dll lib/

dotnet build src -c Release -nologo -v quiet

rm -rf dist/pkg && mkdir -p dist/pkg/plugins
cp src/bin/Release/netstandard2.1/EmeraudeMinerais.dll dist/pkg/plugins/
cp thunderstore/{manifest.json,icon.png,README.md,CHANGELOG.md} dist/pkg/
ZIP=dist/EmeraudeMinerais-$VERSION.zip
rm -f "$ZIP"
(cd dist/pkg && python3 -c "import shutil; shutil.make_archive('../EmeraudeMinerais-$VERSION', 'zip', '.')")
echo "Paquet : $ZIP"
