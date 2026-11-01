param([string]$ManagedDir = 'R:\SteamLibrary\steamapps\common\Probably Stolen\Probably Stolen_Data\Managed')
$ErrorActionPreference = 'Stop'
& dotnet build "$PSScriptRoot\src\QuickReplace.csproj" -c Release --nologo "-p:ManagedDir=$ManagedDir" -p:NuGetAudit=false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
$package = Join-Path $PSScriptRoot 'package\QuickReplace'
Copy-Item -LiteralPath "$PSScriptRoot\src\bin\Release\netstandard2.1\QuickReplace.dll" -Destination $package
Copy-Item -LiteralPath "$PSScriptRoot\README.md" -Destination "$package\README.txt"
Copy-Item -LiteralPath "$PSScriptRoot\LICENSE" -Destination "$package\LICENSE.txt"
New-Item -ItemType Directory -Force "$PSScriptRoot\dist" | Out-Null
$manifest = [xml][IO.File]::ReadAllText((Join-Path $package 'manifest.xml'))
$archiveName = 'QuickReplace-' + $manifest.Manifest.ModVersion + '.zip'
Compress-Archive -LiteralPath $package -DestinationPath (Join-Path "$PSScriptRoot\dist" $archiveName) -Force
Write-Output "Built $archiveName"
