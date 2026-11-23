#!/usr/bin/env bash
# Publie la version courante sur Thunderstore (communauté Valheim).
#   ./publish.sh
# Le jeton de l'équipe Thunderstore est lu dans .env : THUNDERSTORE_TOKEN=tss_...
set -euo pipefail
cd "$(dirname "$0")"

grep -q 'namespace = "A_REMPLIR"' thunderstore.toml && { echo "Renseigne le nom de l'équipe Thunderstore dans thunderstore.toml" >&2; exit 1; }
[ -f .env ] || { echo "Crée .env avec THUNDERSTORE_TOKEN=..." >&2; exit 1; }
set -a; . ./.env; set +a
[ -n "${THUNDERSTORE_TOKEN:-}" ] || { echo "THUNDERSTORE_TOKEN vide dans .env" >&2; exit 1; }

./build.sh
VERSION=$(grep -oP 'Version = "\K[0-9.]+' src/Plugin.cs)
grep -q "versionNumber = \"$VERSION\"" thunderstore.toml || { echo "Version $VERSION absente de thunderstore.toml" >&2; exit 1; }

export DOTNET_ROOT=${DOTNET_ROOT:-$HOME/.local/opt/dotnet-9} DOTNET_ROLL_FORWARD=Major
[ -x tools/tcli ] || dotnet tool install tcli --version 0.2.4 --tool-path ./tools
TCLI_AUTH_TOKEN="$THUNDERSTORE_TOKEN" ./tools/tcli publish --file "dist/EmeraudeMinerais-$VERSION.zip"
