param([string]$rid = 'all')
$ErrorActionPreference = 'Stop'

Write-Host 'dotnet SDK info'
dotnet --info

$proj = 'NatTypeTester'
$exe = "$proj.exe"
$net_tfm = 'net5.0-windows10.0.20348.0'
$configuration = 'Release'
$output_dir = "$PSScriptRoot\$proj\bin\$configuration"
$proj_path = "$PSScriptRoot\$proj\$proj.csproj"

$dllpatcher_tfm = 'net5.0'
$dllpatcher_dir = "$PSScriptRoot\Build\DotNetDllPathPatcher"
$dllpatcher_exe = "$dllpatcher_dir\bin\$configuration\$dllpatcher_tfm\DotNetDllPathPatcher.exe"

function Build-Generic
{
    Write-Host 'Building generic'

    $outdir = "$output_dir\$net_tfm\generic"
    $publishDir = "$outdir\publish"

    Remove-Item $publishDir -Recurse -Force -Confirm:$false -ErrorAction Ignore

    dotnet publish -c $configuration -f $net_tfm $proj_path -o $publishDir
    if ($LASTEXITCODE) { exit $LASTEXITCODE }

    & "$dllpatcher_exe" "$publishDir\$exe" bin
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

function Build-SelfContained
{
    param([string]$rid)

    Write-Host "Building $rid"

    $outdir = "$output_dir\$net_tfm\$rid"
    $publishDir = "$outdir\publish"

    Remove-Item $publishDir -Recurse -Force -Confirm:$false -ErrorAction Ignore

    dotnet publish -c $configuration -f $net_tfm -r $rid --self-contained true $proj_path
    if ($LASTEXITCODE) { exit $LASTEXITCODE }

    & "$dllpatcher_exe" "$publishDir\$exe" bin
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

dotnet build -c $configuration -f $dllpatcher_tfm $dllpatcher_dir\DotNetDllPathPatcher.csproj
if ($LASTEXITCODE) { exit $LASTEXITCODE }

if($rid -eq 'all' -or $rid -eq 'generic')
{
    Build-Generic
}

if($rid -eq 'all')
{
    Build-SelfContained win-x86
    Build-SelfContained win-x64
}
elseif($rid -ne 'generic')
{
    Build-SelfContained $rid
}
