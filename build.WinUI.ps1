param([string]$platform = 'x64')
$ErrorActionPreference = 'Stop'

dotnet --info

$proj = 'NatTypeTester.WinUI'
$net_tfm = 'net8.0-windows10.0.22621.0'
$configuration = 'Release'
$proj_path = "$PSScriptRoot\$proj\$proj.csproj"

$rid = "win-$platform"
Write-Host "Building $rid"

$publishDir = "$PSScriptRoot\$proj\bin\$platform\$configuration\$net_tfm\$rid\publish"

Remove-Item $publishDir -Recurse -Force -Confirm:$false -ErrorAction Ignore

dotnet publish -c $configuration -f $net_tfm -p:Platform=$platform -r $rid --self-contained $proj_path
if ($LASTEXITCODE) { exit $LASTEXITCODE }
