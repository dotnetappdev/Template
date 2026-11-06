#!/usr/bin/env bash
# Packs the template and installs it for the dotnet CLI and Visual Studio.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
dotnet pack "$ROOT/DotNetAppDev.RazorSolution.Templates.csproj" -c Release -o "$ROOT/artifacts"
PKG="$(ls -t "$ROOT"/artifacts/DotNetAppDev.RazorSolution.Templates.*.nupkg | head -1)"
dotnet new install "$PKG" --force
echo "Installed. Create a solution with: dotnet new razorsln -n MyCompany.Portal"
