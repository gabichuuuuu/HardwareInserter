# HardwareInserter

Solid Edge add-in that inserts fasteners and anchors (`.par` files) into the active assembly,
capturing the diameter of a selected hole and automatically applying the matching geometric
constraints.

*(Español más abajo / Spanish below)*

## Installation (end user)

1. Download `HardwareInserterSetup.exe` from the [Releases page](../../releases) of this
   repository, or copy it from a USB drive — it's a single file, with no external dependencies
   and no internet connection required during installation.
2. Double-click to run it. No administrator rights required (it installs under your user profile
   and registers only for your user).
   - Windows SmartScreen may show "Windows protected your PC" because the installer isn't signed
     with a code-signing certificate. Click **More info → Run anyway**.
3. Open Solid Edge (64-bit, 2020 or later) with an assembly **saved to disk**.
4. A **"Insert Hardware"** button should appear on the ribbon of the assembly environment, under
   the "Hardware" category. If it doesn't, see Troubleshooting below.

### Requirements

- Windows with Solid Edge 64-bit (2020+) installed.
- .NET Framework 4.8 (preinstalled on up-to-date Windows 10/11).

## First use

1. Click "Insert Hardware" to open the panel.
2. Click **"Folder..."** and select the root folder containing your `.par` fastener files (they
   can be organized into subfolders, each shown as a category). This path is remembered for next
   time.
3. In Solid Edge, Ctrl+click to select the inner cylindrical face of the hole where you want to
   insert the fastener (and optionally its flat support face).
4. Click **"Capture Hole"** — the detected diameter should be displayed. Verify the value looks
   reasonable (this is the first real-world validation of the add-in's unit conversion).
5. Select a `.par` file from the tree and click **"Insert"** (or double-click). If the "Apply
   constraints" checkbox is checked, the axis and seat are automatically constrained against the
   captured hole; unchecked, it inserts freely (useful for anchors).

## Troubleshooting

- **Button doesn't appear on the ribbon**: check that COM registration succeeded — open
  `regedit` and look under `HKEY_CURRENT_USER\Software\Solid Edge\Add-Ins`; the subkey
  `{DC87125A-6DBF-43A5-968B-2578CCBFC158}` should exist. If it doesn't, reinstall or run
  `dev/register-addin.ps1` manually from source.
- **"Save the assembly before inserting hardware"**: the add-in needs the active assembly to
  already have a path on disk (save it first with Ctrl+S).
- **Captured diameter looks wrong**: please report it — the unit conversion (meters→mm) isn't yet
  validated against every document configuration.

## Development

To build from source, run tests, or understand the project's architecture (a COM add-in, not a
standalone app), see [`AGENTS.md`](AGENTS.md) and [`CLAUDE.md`](CLAUDE.md).

To build the installer locally (Windows, with [Inno Setup](https://jrsoftware.org/isinfo.php)
installed):

```powershell
dotnet build HardwareInserter.slnx -c Release -p:Platform=x64
ISCC.exe installer\HardwareInserter.iss
# -> installer\Output\HardwareInserterSetup.exe
```

Pushing a `vX.Y.Z` tag triggers the `.github/workflows/release.yml` workflow, which builds,
generates the installer, and publishes it as a GitHub Release asset.

## License

[MIT](LICENSE)

---

# HardwareInserter (Español)

Add-in para Solid Edge que inserta tornillería y anchors (archivos `.par`) en un ensamblaje activo,
capturando el diámetro de un agujero seleccionado y aplicando las restricciones geométricas
correspondientes automáticamente.

## Instalación (usuario final)

1. Descarga `HardwareInserterSetup.exe` desde la
   [página de Releases](../../releases) de este repositorio, o cópialo desde un pendrive — es un
   único archivo, sin dependencias externas ni conexión a internet requerida durante la instalación.
2. Ejecútalo con doble clic. No requiere permisos de administrador (se instala en tu perfil de
   usuario y se registra solo para tu usuario).
   - Windows SmartScreen puede mostrar "Windows protegió su PC" porque el instalador no está
     firmado con un certificado de código. Pulsa **Más información → Ejecutar de todas formas**.
3. Abre Solid Edge (64-bit, 2020 o posterior) con un ensamblaje **guardado en disco**.
4. En la ribbon del entorno de ensamblaje debería aparecer un botón **"Insertar Hardware"** en la
   categoría "Hardware". Si no aparece, revisa la sección de solución de problemas más abajo.

### Requisitos

- Windows con Solid Edge 64-bit (2020+) instalado.
- .NET Framework 4.8 (viene preinstalado en Windows 10/11 actualizados).

## Primer uso

1. Pulsa "Insertar Hardware" para abrir el panel.
2. Pulsa **"Carpeta..."** y selecciona la carpeta raíz donde tienes tus archivos `.par` de
   tornillería (se pueden organizar en subcarpetas, cada una aparece como una categoría). Esta
   ruta se recuerda para la próxima vez.
3. En Solid Edge, selecciona con Ctrl+click la cara cilíndrica interior del agujero donde quieres
   insertar el tornillo (y opcionalmente su cara plana de apoyo).
4. Pulsa **"Capturar Agujero"** — debería mostrarse el diámetro detectado. Verifica que el valor
   sea razonable (esta es la primera validación real de la conversión de unidades del add-in).
5. Selecciona un archivo `.par` del árbol e pulsa **"Insertar"** (o doble clic). Si el checkbox
   "Aplicar restricciones" está marcado, se restringe automáticamente eje y asiento contra el
   agujero capturado; si lo desmarcas, se inserta libre (útil para anchors).

## Solución de problemas

- **No aparece el botón en la ribbon**: comprueba que el registro COM se completó — abre
  `regedit` y busca `HKEY_CURRENT_USER\Software\Solid Edge\Add-Ins`; debería existir la subclave
  `{DC87125A-6DBF-43A5-968B-2578CCBFC158}`. Si no está, reinstala o ejecuta manualmente
  `dev/register-addin.ps1` desde el código fuente.
- **"Guarde el ensamblaje antes de insertar hardware"**: el add-in necesita que el ensamblaje
  activo ya tenga una ruta en disco (guárdalo primero con Ctrl+S).
- **Diámetro capturado no parece correcto**: repórtalo — la conversión de unidades (metros→mm) aún
  no está validada contra todas las configuraciones de documento.

## Desarrollo

Para compilar desde el código fuente, ejecutar tests o entender la arquitectura del proyecto
(add-in COM, no app standalone), consulta [`AGENTS.md`](AGENTS.md) y [`CLAUDE.md`](CLAUDE.md).

Para generar el instalador localmente (Windows, con [Inno Setup](https://jrsoftware.org/isinfo.php)
instalado):

```powershell
dotnet build HardwareInserter.slnx -c Release -p:Platform=x64
ISCC.exe installer\HardwareInserter.iss
# -> installer\Output\HardwareInserterSetup.exe
```

Un push de un tag `vX.Y.Z` dispara el workflow `.github/workflows/release.yml`, que compila,
genera el instalador y lo publica como asset del Release de GitHub.

## Licencia

[MIT](LICENSE)
