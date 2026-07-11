# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Comandos comunes

```bash
# Compilar toda la solución (formato .slnx, no .sln)
dotnet build HardwareInserter.slnx

# Compilar un proyecto concreto
dotnet build src/HardwareInserter.Core/HardwareInserter.Core.csproj
dotnet build src/HardwareInserter.SolidEdge/HardwareInserter.SolidEdge.csproj
dotnet build src/HardwareInserter.AddIn/HardwareInserter.AddIn.csproj -p:Platform=x64

# Ejecutar todos los tests
dotnet test tests/HardwareInserter.Core.Tests/HardwareInserter.Core.Tests.csproj

# Ejecutar un test concreto (por nombre parcial de método)
dotnet test tests/HardwareInserter.Core.Tests/ --filter "FullyQualifiedName~Refresh_CarpetaConPar"
```

> `dotnet test` requiere un testhost de .NET Framework 4.8, que no viene incluido en el SDK de
> dotnet en Linux. Si falla con `Cannot open assembly '.../testhost.net48.exe'`, compila el
> proyecto de tests y ejecuta el DLL resultante con Mono y el runner de xUnit:
> `mono ~/.nuget/packages/xunit.runner.console/<version>/tools/net48/xunit.console.exe bin/Debug/net48/HardwareInserter.Core.Tests.dll`
> (instala el paquete `xunit.runner.console` temporalmente con `dotnet add package` solo para
> esto y revierte el cambio en el `.csproj` después; no debe quedar como dependencia permanente).
> Los tests de `HardwareInserter.SolidEdge`/`HardwareInserter.AddIn` (si se añaden en el futuro)
> requerirían Solid Edge real instalado, ya que dependen de la interop COM.

## Arquitectura

**Target:** .NET Framework 4.8 (`net48`) con C# 9, `Nullable` habilitado (`WarningsAsErrors`).
Fijado en `Directory.Build.props` para máxima compatibilidad con la interop COM de Solid Edge.

**Forma de ejecución:** add-in COM registrado, NO macro/EXE standalone. Solid Edge carga el
ensamblado `HardwareInserter.AddIn` al arrancar (registrado en
`HKCU\Software\Solid Edge\Add-Ins\{CLSID}`) e invoca `ISolidEdgeAddIn.OnConnection`, recibiendo la
instancia de `Application` directamente (sin `Marshal.GetActiveObject`). El add-in registra un
botón en la ribbon del entorno de ensamblaje; al pulsarlo, abre el diálogo WPF no modal
`HardwarePickerWindow` que flota sobre Solid Edge.

### Proyectos

| Proyecto | Responsabilidad |
|---|---|
| `HardwareInserter.Core` | Modelos (`LibraryItem`), librería de hardware basada en FS (`FileSystemFastenerLibrary` con `FileSystemWatcher`), preferencias (`Settings`/`JsonSettingsStore` en `%AppData%`). Sin dependencia de COM. |
| `HardwareInserter.SolidEdge` | Toda la interop con `Interop.SolidEdge`: captura de geometría e inserción/restricciones (`SolidEdgeHelper`), resolución de caras del tornillo (`IScrewGeometryResolver`), gestión de archivos `.par` (`HardwareFileManager`). |
| `HardwareInserter.AddIn` | Add-in COM (`ISolidEdgeAddIn`) + WPF `HardwarePickerWindow`. Registrado en HKCU con `regasm /codebase`. `ComRegisterFunction`/`ComUnregisterFunction` escriven/borran la subclave de Solid Edge. |
| `HardwareInserter.Core.Tests` | Tests xUnit + FluentAssertions de `FileSystemFastenerLibrary` y `Settings`. Sin COM. |

### Flujo de la UI (`HardwarePickerWindow`)

1. **Cargar librería** → `FileSystemFastenerLibrary` escanea recursivamente `*.par` en la carpeta
   raíz configurada; el `FileSystemWatcher` refresca el árbol en vivo cuando se añaden/borran
   archivos. El usuario agrupa por subcarpeta (categoría) y filtra por texto libre.
2. **Configurar carpeta** → botón "Carpeta..." → `FolderBrowserDialog` → cambia `RootPath` y lo
   persiste en `settings.json`.
3. **Capturar Agujero** → `SolidEdgeHelper.CaptureSelectedHoleGeometry(application)` lee el
   `SelectSet` activo de Solid Edge. Se espera que el usuario haya seleccionado (Ctrl+click) la
   cara cilíndrica interior del agujero y, opcionalmente, su cara plana de apoyo, antes de pulsar
   el botón. Devuelve un `CapturedHoleGeometry` con el diámetro en mm.
4. **Insertar** (por item seleccionado) → `HardwareFileManager.EnsureLocalCopy` copia el `.par` de
   la librería a una carpeta `Hardware/` junto al ensamblaje activo (si no existe ya);
   `SolidEdgeHelper.InsertOccurrenceAndConstrain` inserta la occurrence (`Occurrences.AddByFilename`)
   y, si el checkbox "Aplicar restricciones" está marcado, aplica `Relations3d.AddAxial`/`AddPlanar`
   contra la geometría capturada. Desmarcado = inserción libre (anchors).

### Puntos de riesgo/extensión conocidos (no verificables sin Solid Edge real instalado)

- **Unidades**: `SolidEdgeHelper` asume que la API devuelve longitudes en metros y convierte a mm
  (`MetrosAMilimetros = 1000.0`). Validar contra un documento real antes de confiar en el
  diámetro mostrado.
- **Resolución de geometría del tornillo** (`IndexBasedScrewGeometryResolver`): no existe API
  genérica de Solid Edge para saber qué cara de un `.par` recién insertado es el eje del vástago
  o el asiento de la cabeza. La implementación por defecto usa una heurística (mayor cara
  cilíndrica = eje, mayor cara plana = asiento) sobre `Occurrence.Body.Faces[FeatureTopologyQueryTypeConstants.igQueryAll]`.
  Si las plantillas `.par` del servidor de tornillería usan una convención de nombres de cara
  consistente, sustituir esta clase por una que implemente `IScrewGeometryResolver` resolviendo
  por nombre, sin tocar el resto del flujo.
- **Registro del botón en la ribbon**: `SetAddInInfoEx`/`AddCommandBarButton` están verificados por
  `monodis` pero NO probados en runtime. Si el botón no aparece, revisar: (1) registro en HKCU,
  (2) IDs de bitmap (actualmente 0 = sin icono), (3) CatID del entorno coincide con el tipo de
  documento activo.
- **`EmbedInteropTypes=false`**: el `Interop.SolidEdge.dll` (NuGet comunidad, 220.2.0 / SE 2022)
  se distribuye con el add-in. Es compatible con SE 2022-2026+ porque las interfaces de add-in
  (`ISolidEdgeAddIn`, `ISEAddInEx`) son estables. Si una versión futura de SE añade métodos
  obligatorios a estas interfaces, el addín necesitaría recompilación contra un interop más nuevo.

### Catálogo de tornillería: escaneo de sistema de archivos (sin JSON/Excel)

El catálogo se construye dinámicamente escaneando una carpeta local recursivamente en busca de
`*.par`. No hay índice JSON ni importación Excel. La carpeta raíz la configura el usuario con el
botón "Carpeta..." y se persiste en `%AppData%\HardwareInserter\settings.json` (solo `RootPath`).
Un `FileSystemWatcher` mantiene el árbol vivo: añadir un `.par` a la carpeta lo hace aparecer sin
reiniciar Solid Edge. Cada `.par` se muestra tal cual (sin parseo de nombre); `Category` = nombre
de la subcarpeta inmediata, `Filename` = nombre del archivo con extensión.

### Firmas de Interop.SolidEdge verificadas

Las firmas de `Occurrences.AddByFilename`, `Relations3d.AddAxial/AddPlanar`, `Occurrence.Body`,
`Body.Faces[FeatureTopologyQueryTypeConstants]` (es un indexador C#, no un método — el compilador
lo confirma con el error `CS1955` si se llama como método), `Face.Geometry/Area`,
`AssemblyDocument.Path/Occurrences/Relations3d`, `Application.ActiveDocument/ActiveSelectSet`,
`ISolidEdgeAddIn.OnConnection/OnDisconnection`, `ISEAddInEx.SetAddInInfoEx/AddCommandBarButton/
AddInEvents`, `AddInEventsClass.OnCommand/OnCommandUpdateUI` fueron confirmadas desensamblando
`~/.nuget/packages/interop.solidedge/220.2.0/lib/net45/Interop.SolidEdge.dll` con `monodis` (no hay
Solid Edge real instalado en el entorno de desarrollo). Si se añaden nuevas llamadas a la API,
verificar igual contra ese DLL antes de asumir una firma.

CatID del entorno Assembly: `26618395-09D6-11d1-BA07-080036230602`
(extraído del `.cctor` de `SolidEdgeSDK.EnvironmentCategories` con `monodis`).
