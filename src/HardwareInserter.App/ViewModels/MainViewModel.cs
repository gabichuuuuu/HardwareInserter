using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HardwareInserter.Core.Catalog;
using HardwareInserter.Core.Excel;
using HardwareInserter.Core.Models;
using HardwareInserter.SolidEdge;
using Microsoft.Win32;

namespace HardwareInserter.App.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IFastenerCatalog _catalog;
        private readonly SolidEdgeSession _session;
        private readonly SolidEdgeHelper _solidEdgeHelper;
        private readonly HardwareFileManager _fileManager;

        private CapturedHoleGeometry? _capturedHole;
        private string _statusMessage = string.Empty;
        private double? _diametroDetectadoMm;

        public MainViewModel(string catalogJsonPath)
        {
            _catalog = new JsonFastenerCatalog(catalogJsonPath);
            _session = new SolidEdgeSession();
            _solidEdgeHelper = new SolidEdgeHelper(_session, new IndexBasedScrewGeometryResolver());
            _fileManager = new HardwareFileManager();

            Filas = new ObservableCollection<StagingRowViewModel>();

            CargarExcelCommand = new RelayCommand(CargarExcel);
            SeleccionarAgujeroCommand = new RelayCommand(SeleccionarAgujero);
            InsertarCommand = new RelayCommand(param => Insertar(param as StagingRowViewModel), param => param is StagingRowViewModel);
        }

        public ObservableCollection<StagingRowViewModel> Filas { get; }

        public ICommand CargarExcelCommand { get; }

        public ICommand SeleccionarAgujeroCommand { get; }

        public ICommand InsertarCommand { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            private set { _statusMessage = value; OnPropertyChanged(); }
        }

        public double? DiametroDetectadoMm
        {
            get => _diametroDetectadoMm;
            private set { _diametroDetectadoMm = value; OnPropertyChanged(); }
        }

        private void CargarExcel(object? _)
        {
            var dialog = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var rows = ExcelStagingImporter.Import(dialog.FileName);
                Filas.Clear();
                foreach (var row in rows)
                {
                    Filas.Add(new StagingRowViewModel(row, _catalog));
                }
                StatusMessage = $"{rows.Count} filas cargadas desde '{Path.GetFileName(dialog.FileName)}'.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar Excel: {ex.Message}";
            }
        }

        private void SeleccionarAgujero(object? _)
        {
            try
            {
                _capturedHole?.Dispose();
                _capturedHole = _solidEdgeHelper.CaptureSelectedHoleGeometry();
                DiametroDetectadoMm = _capturedHole.DiameterMm;
                StatusMessage = $"Agujero capturado: Ø{_capturedHole.DiameterMm:0.##} mm.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al capturar el agujero: {ex.Message}";
            }
        }

        private void Insertar(StagingRowViewModel? fila)
        {
            if (fila == null)
            {
                return;
            }

            if (_capturedHole == null)
            {
                StatusMessage = "Capture primero un agujero con 'Seleccionar Agujero'.";
                return;
            }

            var catalogItem = fila.BuscarEnCatalogo();
            if (catalogItem == null)
            {
                fila.MarcarError("No existe en el catálogo de tornillería.");
                return;
            }

            try
            {
                var assemblyPath = _solidEdgeHelper.GetActiveAssemblyPath()
                    ?? throw new InvalidOperationException("Guarde el ensamblaje antes de insertar tornillería.");

                var localParPath = _fileManager.EnsureLocalCopy(assemblyPath, catalogItem.RutaServidor);
                _solidEdgeHelper.InsertOccurrenceAndConstrain(localParPath, _capturedHole);

                fila.MarcarInsertado();
                StatusMessage = $"Tornillo {catalogItem.Tipo} {catalogItem.Metrica}x{catalogItem.Longitud} insertado.";
            }
            catch (Exception ex)
            {
                fila.MarcarError(ex.Message);
            }
        }

        public void Dispose()
        {
            _capturedHole?.Dispose();
            _session.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
