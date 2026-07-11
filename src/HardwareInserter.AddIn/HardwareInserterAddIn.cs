using System;
using System.Runtime.InteropServices;
using HardwareInserter.AddIn.UI;
using HardwareInserter.SolidEdge;
using SolidEdgeAssembly;
using SolidEdgeFramework;

namespace HardwareInserter.AddIn
{
    /// <summary>
    /// Add-in COM nativo de Solid Edge para la inserción de hardware (tornillería y anchors) desde
    /// una librería de archivos <c>.par</c> en disco. Se registra como add-in de Solid Edge y aparece
    /// como un botón en la ribbon del entorno de ensamblaje. Al pulsarlo, abre un diálogo WPF no modal
    /// (<see cref="HardwarePickerWindow"/>) que se acopla sobre la ventana de Solid Edge.
    /// </summary>
    /// <remarks>
    /// Implementa <see cref="ISolidEdgeAddIn"/>, el contrato COM que Solid Edge invoca al cargar/descargar
    /// el add-in. Las firmas se verificaron desensamblando <c>Interop.SolidEdge.dll 220.2.0</c> con
    /// <c>monodis</c>. El add-in se compila contra 220.2.0 (SE 2022) con <c>EmbedInteropTypes=true</c>,
    /// pero las interfaces <c>ISolidEdgeAddIn</c>/<c>ISEAddInEx</c> son estables y retrocompatibles con
    /// versiones posteriores de Solid Edge (2022-2026 y siguientes).
    /// </remarks>
    [ComVisible(true)]
    [Guid("DC87125A-6DBF-43A5-968B-2578CCBFC158")]
    [ProgId("HardwareInserter.AddIn")]
    public sealed class HardwareInserterAddIn : ISolidEdgeAddIn, IDisposable
    {
        /// <summary>GUID del entorno de ensamblaje de Solid Edge (SolidEdgeSDK.EnvironmentCategories.Assembly).</summary>
        private const string AssemblyEnvCatId = "26618395-09D6-11d1-BA07-080036230602";

        /// <summary>Nombre de la categoría/pestaña donde aparece el botón en la ribbon de Solid Edge.</summary>
        private const string RibbonCategory = "Hardware";

        /// <summary>Nombre del grupo/botón dentro de la categoría de la ribbon.</summary>
        private const string CommandBarName = "Inserción";

        /// <summary>ID numérico del comando "Insertar Hardware" (cualquier valor &gt; 32768 sirve; los &lt; 32768 son de SE).</summary>
        private const int CmdInsertarHardware = 33000;

        private Application? _application;
        private AddInEventsClass? _addInEvents;
        private HardwarePickerWindow? _pickerWindow;
        private SolidEdgeHelper? _solidEdgeHelper;
        private bool _disposed;

        /// <summary>
        /// Llamado por Solid Edge al cargar el add-in. Cachea la instancia de <see cref="Application"/>,
        /// suscribe los eventos de comando y registra el botón en la ribbon del entorno de ensamblaje.
        /// </summary>
        public void OnConnection(object Application, SeConnectMode ConnectMode, SolidEdgeFramework.AddIn AddInInstance)
        {
            _application = (Application)Application;

            _solidEdgeHelper = new SolidEdgeHelper(new IndexBasedScrewGeometryResolver());

            // Suscribir eventos de comando del add-in.
            var addInEx = (ISEAddInEx)AddInInstance;
            _addInEvents = (AddInEventsClass)addInEx.AddInEvents;
            _addInEvents.OnCommand += OnCommand;
            _addInEvents.OnCommandUpdateUI += OnCommandUpdateUI;

            // Registrar el comando en la ribbon del entorno de ensamblaje.
            Array commandNames = new string[] { "Insertar Hardware" };
            Array commandIds = new int[] { CmdInsertarHardware };

            addInEx.SetAddInInfoEx(
                ResourceFilename: GetType().Assembly.Location,
                EnvironmentCatID: AssemblyEnvCatId,
                CategoryName: RibbonCategory,
                IDColorBitmapMedium: 0,
                IDColorBitmapLarge: 0,
                IDMonochromeBitmapMedium: 0,
                IDMonochromeBitmapLarge: 0,
                NumberOfCommands: 1,
                CommandNames: ref commandNames,
                CommandIDs: ref commandIds);

            addInEx.AddCommandBarButton(AssemblyEnvCatId, CommandBarName, CmdInsertarHardware);
        }

        /// <summary>Llamado por Solid Edge al conectar el add-in a un entorno concreto.</summary>
        public void OnConnectToEnvironment(string EnvCatID, object pEnvironmentDispatch, bool bFirstTime)
        {
            // No-op: solo actuamos en el entorno de ensamblaje, que ya se registra en OnConnection.
        }

        /// <summary>Llamado por Solid Edge al descargar el add-in. Libera todos los recursos COM.</summary>
        public void OnDisconnection(SeDisconnectMode DisconnectMode)
        {
            Dispose();
        }

        private void OnCommand(int CommandID)
        {
            if (CommandID != CmdInsertarHardware) return;
            ShowPickerWindow();
        }

        private void OnCommandUpdateUI(
            int CommandID,
            ref int CommandFlags,
            out string MenuItemText,
            ref int BitmapID)
        {
            MenuItemText = string.Empty;
            if (CommandID != CmdInsertarHardware) return;

            // Habilitar el botón solo si hay un ensamblaje activo.
            var enabled = IsAssemblyActive();
            const int SeCmdActive_Enabled = 0x00000001;
            const int SeCmdActive_Remove = 0x00000040;
            CommandFlags = enabled ? SeCmdActive_Enabled : SeCmdActive_Remove;
            MenuItemText = "Insertar Hardware";
        }

        private bool IsAssemblyActive()
        {
            if (_application == null) return false;
            try
            {
                return _application.ActiveDocument is AssemblyDocument;
            }
            catch
            {
                return false;
            }
        }

        private void ShowPickerWindow()
        {
            if (_application == null || _solidEdgeHelper == null) return;

            if (_pickerWindow != null && _pickerWindow.IsLoaded)
            {
                _pickerWindow.Activate();
                return;
            }

            _pickerWindow = new HardwarePickerWindow(_application, _solidEdgeHelper);
            _pickerWindow.Closed += (_, _) => _pickerWindow = null;
            _pickerWindow.Show();
        }

        /// <summary>
        /// Función de registro invocada por <c>regasm /codebase</c>. Escribe la subclave
        /// <c>HKCU\Software\Solid Edge\Add-Ins\{CLSID}</c> con la descripción y visibilidad del add-in.
        /// </summary>
        [ComRegisterFunction]
        public static void RegisterAddIn(Type t)
        {
            var clsid = t.GUID.ToString("B");
            using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                $@"Software\Solid Edge\Add-Ins\{clsid}");
            key.SetValue("Description", "HardwareInserter: inserción de tornillería y anchors desde librería local.");
            key.SetValue("Summary", "Add-in para inserción automática de hardware en ensamblajes de Solid Edge.");
            key.SetValue("Visible", 1, Microsoft.Win32.RegistryValueKind.DWord);
        }

        /// <summary>Función de desregistro invocada por <c>regasm /u</c>. Borra la subclave del add-in.</summary>
        [ComUnregisterFunction]
        public static void UnregisterAddIn(Type t)
        {
            var clsid = t.GUID.ToString("B");
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKey(
                $@"Software\Solid Edge\Add-Ins\{clsid}", throwOnMissingSubKey: false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _pickerWindow?.Close();
            _pickerWindow = null;

            if (_addInEvents != null)
            {
                try
                {
                    _addInEvents.OnCommand -= OnCommand;
                    _addInEvents.OnCommandUpdateUI -= OnCommandUpdateUI;
                }
                catch { /* best-effort al desuscribir */ }
                if (Marshal.IsComObject(_addInEvents)) Marshal.ReleaseComObject(_addInEvents);
                _addInEvents = null;
            }

            if (_application != null && Marshal.IsComObject(_application))
            {
                Marshal.ReleaseComObject(_application);
            }
            _application = null;
        }
    }
}
