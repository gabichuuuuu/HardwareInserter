# Unregister-HardwareInserter.ps1
#
# Desregistra el add-in HardwareInserter.AddIn de Solid Edge.
# Borra el CLSID de HKCU y la subclave HKCU\Software\Solid Edge\Add-Ins\{CLSID}.
#
# Uso:
#   .\dev\unregister-addin.ps1
#   .\dev\unregister-addin.ps1 -Config Release

param(
    [string]$Config = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$addinDll = Join-Path $repoRoot "src\HardwareInserter.AddIn\bin\$Config\net48\HardwareInserter.AddIn.dll"

$regasm = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
if (-not (Test-Path $regasm)) {
    throw "No se encontró RegAsm 64-bit en '$regasm'."
}

Write-Host "Desregistrando add-in COM..." -ForegroundColor Cyan
& $regasm /unregister $addinDll
if ($LASTEXITCODE -ne 0) {
    Write-Warning "RegAsm /unregister devolvió $LASTEXITCODE (puede que ya estuviera desregistrado)."
}

# Borra la subclave de Solid Edge por si quedó (ComUnregisterFunction debería hacerlo, pero por seguridad).
$clsid = "{DC87125A-6DBF-43A5-968B-2578CCBFC158}"
$addInKey = "HKCU:\Software\Solid Edge\Add-Ins\$clsid"
if (Test-Path $addInKey) {
    Remove-Item $addInKey -Recurse -Force
    Write-Host "Subclave de Solid Edge borrada." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Add-in desregistrado." -ForegroundColor Green
