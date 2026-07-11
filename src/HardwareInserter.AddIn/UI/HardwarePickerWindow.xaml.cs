using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HardwareInserter.Core.Library;
using HardwareInserter.Core.Preferences;
using HardwareInserter.SolidEdge;
using SolidEdgeFramework;

namespace HardwareInserter.AddIn.UI
{
    /// <summary>
    /// Diálogo WPF no modal del add-in. Muestra el árbol de la librería de hardware (archivos
    /// <c>.par</c> descubiertos en la carpeta raíz configurada), permite capturar la geometría del
    /// agujero seleccionado en Solid Edge e insertar el hardware elegido como occurrence del
    /// ensamblaje activo, con o sin restricciones según el checkbox.
    /// </summary>
    public partial class HardwarePickerWindow : System.Windows.Window
    {
        private readonly SolidEdgeFramework.Application _solidEdgeApp;
        private readonly SolidEdgeHelper _solidEdgeHelper;
        private readonly HardwareFileManager _fileManager;
        private IHardwareLibrary _library;
        private CapturedHoleGeometry? _capturedHole;
        private string _filter = string.Empty;

        /// <summary>Construye el diálogo con la instancia de Solid Edge y el helper de inserción.</summary>
        public HardwarePickerWindow(SolidEdgeFramework.Application solidEdgeApp, SolidEdgeHelper solidEdgeHelper)
        {
            _solidEdgeApp = solidEdgeApp ?? throw new ArgumentNullException(nameof(solidEdgeApp));
            _solidEdgeHelper = solidEdgeHelper ?? throw new ArgumentNullException(nameof(solidEdgeHelper));
            _fileManager = new HardwareFileManager();

            InitializeComponent();

            var settings = Settings.Load(Settings.GetDefaultFilePath());
            if (!string.IsNullOrWhiteSpace(settings.RootPath) && Directory.Exists(settings.RootPath))
            {
                _library = new FileSystemFastenerLibrary(settings.RootPath);
            }
            else
            {
                _library = new EmptyLibrary();
            }
            RootPathBox.Text = _library.RootPath;

            _library.Changed += OnLibraryChanged;
            RefreshTree();
        }

        private void OnLibraryChanged(object sender, EventArgs e)
        {
            // El FileSystemWatcher dispara en un hilo de pool; marshalar al hilo UI de WPF.
            Dispatcher.BeginInvoke(new Action(RefreshTree));
        }

        private void RefreshTree()
        {
            var items = _library.Items;
            var filtered = string.IsNullOrEmpty(_filter)
                ? items
                : items.Where(i => i.RelativePath.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            var grouped = filtered
                .GroupBy(i => i.Category)
                .OrderBy(g => g.Key)
                .Select(g => new CategoryNode(
                    g.Key,
                    g.OrderBy(i => i.Filename).Select(i => new LibraryItemNode(i)).ToList()))
                .ToList();

            LibraryTree.ItemsSource = grouped;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Seleccione la carpeta raíz de la librería de hardware (.par)",
                ShowNewFolderButton = false,
            };
            if (!string.IsNullOrWhiteSpace(_library.RootPath) && Directory.Exists(_library.RootPath))
            {
                dialog.SelectedPath = _library.RootPath;
            }
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            var newRoot = dialog.SelectedPath;
            try
            {
                if (_library is FileSystemFastenerLibrary fs)
                {
                    fs.SetRootPath(newRoot);
                }
                else
                {
                    _library.Changed -= OnLibraryChanged;
                    _library.Dispose();
                    _library = new FileSystemFastenerLibrary(newRoot);
                    _library.Changed += OnLibraryChanged;
                    RefreshTree();
                }
                RootPathBox.Text = newRoot;

                // Persistir la preferencia.
                var settings = new Settings { RootPath = newRoot };
                settings.Save(Settings.GetDefaultFilePath());
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error al cambiar carpeta: " + ex.Message;
            }
        }

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filter = FilterBox.Text ?? string.Empty;
            RefreshTree();
        }

        private void CaptureHoleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _capturedHole?.Dispose();
                _capturedHole = _solidEdgeHelper.CaptureSelectedHoleGeometry(_solidEdgeApp);
                HoleInfo.Text = $"Agujero: Ø{_capturedHole.DiameterMm:0.##} mm";
                UpdateInsertEnabled();
                StatusText.Text = "Agujero capturado.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error al capturar: " + ex.Message;
            }
        }

        private void LibraryTree_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (GetSelectedLibraryItem() != null)
            {
                InsertButton_Click(sender, e);
            }
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedLibraryItem();
            if (item == null)
            {
                StatusText.Text = "Seleccione un archivo .par del árbol.";
                return;
            }
            if (_capturedHole == null)
            {
                StatusText.Text = "Capture primero un agujero con 'Capturar Agujero'.";
                return;
            }

            try
            {
                var assemblyPath = SolidEdgeHelper.GetActiveAssemblyPath(_solidEdgeApp)
                    ?? throw new InvalidOperationException("Guarde el ensamblaje antes de insertar hardware.");

                var localParPath = _fileManager.EnsureLocalCopy(assemblyPath, item.FullPath);
                var applyConstraints = ApplyConstraintsCheckBox.IsChecked ?? true;
                _solidEdgeHelper.InsertOccurrenceAndConstrain(_solidEdgeApp, localParPath, _capturedHole, applyConstraints);

                StatusText.Text = $"Insertado: {item.Filename} (constraints: {applyConstraints})";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error al insertar: " + ex.Message;
            }
        }

        private LibraryItem? GetSelectedLibraryItem()
        {
            if (LibraryTree.SelectedItem is LibraryItemNode fileNode) return fileNode.Item;
            return null;
        }

        private void UpdateInsertEnabled()
        {
            InsertButton.IsEnabled = _capturedHole != null;
        }

        protected override void OnClosed(EventArgs e)
        {
            _capturedHole?.Dispose();
            _library.Changed -= OnLibraryChanged;
            _library.Dispose();
            base.OnClosed(e);
        }
    }

    /// <summary>Nodo de categoría (subcarpeta) en el árbol.</summary>
    public sealed class CategoryNode
    {
        public CategoryNode(string name, IReadOnlyList<LibraryItemNode> children)
        {
            Name = name;
            Children = children;
        }
        public string Name { get; }
        public IReadOnlyList<LibraryItemNode> Children { get; }
    }

    /// <summary>Nodo hoja del árbol: un archivo .par concreto.</summary>
    public sealed class LibraryItemNode
    {
        public LibraryItemNode(LibraryItem item)
        {
            Item = item;
            Label = item.Filename;
        }
        public LibraryItem Item { get; }
        public string Label { get; }
    }

    /// <summary>
    /// Implementación vacía de <see cref="IHardwareLibrary"/> para cuando el usuario aún no ha
    /// configurado una carpeta raíz válida. Evita que el diálogo falle al arrancar sin configuración.
    /// </summary>
    internal sealed class EmptyLibrary : IHardwareLibrary
    {
        public string RootPath => string.Empty;
        public IReadOnlyList<LibraryItem> Items => new List<LibraryItem>();
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
        public void Refresh() { }
        public void SetRootPath(string rootPath) { }
        public void Dispose() { }
    }
}
