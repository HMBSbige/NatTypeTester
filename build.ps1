$ErrorActionPreference = 'Stop'

dotnet --info

$proj = 'NatTypeTester'
$exe = "$proj.exe"
$net_tfm = 'net8.0-windows10.0.22621.0'
$configuration = 'Release'
$output_dir = "$PSScriptRoot\$proj\bin\$configuration"
$proj_path = "$PSScriptRoot\$proj\$proj.csproj"
$generic_outdir = "$output_dir\$net_tfm\generic"

function Build-Generic {
    Write-Host 'Building generic'

    $outdir = $generic_outdir
    $publishDir = "$outdir\publish"

    Remove-Item $publishDir -Recurse -Force -Confirm:$false -ErrorAction Ignore

    dotnet publish -c $configuration -f $net_tfm $proj_path -o $publishDir
    if ($LASTEXITCODE) { exit $LASTEXITCODE }

    & "$PSScriptRoot\Build\DotNetDllPathPatcher.ps1" "$publishDir\$exe" bin
    if ($LASTEXITCODE) { exit $LASTEXITCODE }

    Remove-Item "$publishDir\$exe"
}

function Build {
    param([string]$arch)

    $rid = "win-$arch"
    Write-Host "Building $rid"

    $outdir = "$output_dir\$net_tfm\$rid"
    $publishDir = "$outdir\publish"

    Remove-Item $publishDir -Recurse -Force -Confirm:$false -ErrorAction Ignore

    dotnet publish -c $configuration -f $net_tfm -r $rid --no-self-contained true $proj_path
    if ($LASTEXITCODE) { exit $LASTEXITCODE }

    & "$PSScriptRoot\Build\DotNetDllPathPatcher.ps1" "$publishDir\$exe" bin
    if ($LASTEXITCODE) { exit $LASTEXITCODE }

    Move-Item "$publishDir\$exe" "$generic_outdir\publish\$proj-$arch.exe"
}

Build-Generic
Build x64
Build x86
Build arm64
