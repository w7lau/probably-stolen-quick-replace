param([string]$ManagedDir = 'R:\SteamLibrary\steamapps\common\Probably Stolen\Probably Stolen_Data\Managed')
$ErrorActionPreference = 'Stop'
& dotnet build "$PSScriptRoot\src\QuickReplace.csproj" -c Release --nologo "-p:ManagedDir=$ManagedDir" -p:NuGetAudit=false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
$package = Join-Path $PSScriptRoot 'package\QuickReplace'
Copy-Item -LiteralPath "$PSScriptRoot\src\bin\Release\netstandard2.1\QuickReplace.dll" -Destination $package
Copy-Item -LiteralPath "$PSScriptRoot\README.md" -Destination "$package\README.txt"
Copy-Item -LiteralPath "$PSScriptRoot\LICENSE" -Destination "$package\LICENSE.txt"
New-Item -ItemType Directory -Force "$PSScriptRoot\dist" | Out-Null
Compress-Archive -LiteralPath $package -DestinationPath "$PSScriptRoot\dist\QuickReplace-0.1.1.zip" -Force
Write-Output 'Built QuickReplace-0.1.1.zip'
