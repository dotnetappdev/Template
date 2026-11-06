# Packs the template and installs it for the dotnet CLI and Visual Studio.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
dotnet pack (Join-Path $root 'DotNetAppDev.RazorSolution.Templates.csproj') -c Release -o (Join-Path $root 'artifacts')
$pkg = Get-ChildItem (Join-Path $root 'artifacts') -Filter 'DotNetAppDev.RazorSolution.Templates.*.nupkg' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
dotnet new install $pkg.FullName --force
Write-Host "Installed. Restart Visual Studio, then File > New > Project and search for 'Razor Pages Solution'."
Write-Host "Or from the command line: dotnet new razorsln -n MyCompany.Portal"
