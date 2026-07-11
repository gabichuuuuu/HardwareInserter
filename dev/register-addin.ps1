# Register-HardwareInserter.ps1
#
# Registra el add-in HardwareInserter.AddIn en Solid Edge (HKCU, por usuario).
# Requiere Solid Edge 64-bit (2020+) y .NET Framework 4.8.
#
# Uso:
#   .\dev\register-addin.ps1              # compila + registra (Debug x64)
#   .\dev\register-addin.ps1 -Config Release
#
# No requiere administrador (registro HKCU).

param(
    [string]$Config = "Debug"
)

$ErrorActionPreference = "Stop"

# Resolver rutas relativas al directorio del script.
$repoRoot = Split-Path -Parent $PSScriptRoot
$addinCsproj = Join-Path $repoRoot "src\HardwareInserter.AddIn\HardwareInserter.AddIn.csproj"
$addinDll = Join-Path $repoRoot "src\HardwareInserter.AddIn\bin\$Config\net48\HardwareInserter.AddIn.dll"

# Compilar el add-in (x64).
Write-Host "Compilando HardwareInserter.AddIn ($Config, x64)..." -ForegroundColor Cyan
dotnet build $addinCsproj -c $Config -p:Platform=x64
if ($LASTEXITCODE -ne 0) {
    throw "La compilación falló."
}

if (-not (Test-Path $addinDll)) {
    throw "No se encontró '$addinDll' tras compilar. ¿Es la configuración correcta?"
}

# Localizar regasm 64-bit.
$regasm = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe"
if (-not (Test-Path $regasm)) {
    throw "No se encontró RegAsm 64-bit en '$regasm'. ¿Está .NET Framework 4.8 instalado?"
}

# Registrar el add-in con regasm /codebase (invoca ComRegisterFunction → escribe la subclave de SE).
Write-Host "Registrando add-in COM en HKCU..." -ForegroundColor Cyan
& $regasm /codebase $addinDll
if ($LASTEXITCODE -ne 0) {
    throw "RegAsm falló al registrar el add-in."
}

Write-Host ""
Write-Host "Add-in registrado correctamente." -ForegroundColor Green
Write-Host "DLL: $addinDll"
Write-Host "CLSID: {DC87125A-6DBF-43A5-968B-2578CCBFC158}"
Write-Host "ProgID: HardwareInserter.AddIn"
Write-Host ""
Write-Host "Abra Solid Edge con un ensamblaje activo; debería aparecer el botón 'Insertar Hardware' en la ribbon." -ForegroundColor Yellow
