using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HardwareInserter.Core.Catalog;
using HardwareInserter.Core.Models;

namespace HardwareInserter.App.ViewModels
{
    /// <summary>Envuelve un <see cref="StagingRow"/> con notificación de cambios y combos en cascada.</summary>
    public sealed class StagingRowViewModel : INotifyPropertyChanged
    {
        private readonly IFastenerCatalog _catalog;
        private readonly StagingRow _row;

        public StagingRowViewModel(StagingRow row, IFastenerCatalog catalog)
        {
            _row = row;
            _catalog = catalog;

            Tipos = new ObservableCollection<string>(_catalog.GetTipos());
            Metricas = new ObservableCollection<string>(
                string.IsNullOrEmpty(_row.Tipo) ? new System.Collections.Generic.List<string>() : _catalog.GetMetricas(_row.Tipo!));
            Longitudes = new ObservableCollection<double>(
                string.IsNullOrEmpty(_row.Tipo) || string.IsNullOrEmpty(_row.Metrica)
                    ? new System.Collections.Generic.List<double>()
                    : _catalog.GetLongitudes(_row.Tipo!, _row.Metrica!));
        }

        public ObservableCollection<string> Tipos { get; }

        public ObservableCollection<double> Longitudes { get; }

        public ObservableCollection<string> Metricas { get; }

        public string? Tipo
        {
            get => _row.Tipo;
            set
            {
                if (_row.Tipo == value) return;
                _row.Tipo = value;
                OnPropertyChanged();

                Metricas.Clear();
                if (!string.IsNullOrEmpty(value))
                {
                    foreach (var m in _catalog.GetMetricas(value!)) Metricas.Add(m);
                }
                Metrica = null;
                RecalcularEstado();
            }
        }

        public string? Metrica
        {
            get => _row.Metrica;
            set
            {
                if (_row.Metrica == value) return;
                _row.Metrica = value;
                OnPropertyChanged();

                Longitudes.Clear();
                if (!string.IsNullOrEmpty(_row.Tipo) && !string.IsNullOrEmpty(value))
                {
                    foreach (var l in _catalog.GetLongitudes(_row.Tipo!, value!)) Longitudes.Add(l);
                }
                Longitud = null;
                RecalcularEstado();
            }
        }

        public double? Longitud
        {
            get => _row.Longitud;
            set
            {
                if (_row.Longitud == value) return;
                _row.Longitud = value;
                OnPropertyChanged();
                RecalcularEstado();
            }
        }

        public double? Cantidad
        {
            get => _row.Cantidad;
            set { _row.Cantidad = value; OnPropertyChanged(); }
        }

        public string? Referencia
        {
            get => _row.Referencia;
            set { _row.Referencia = value; OnPropertyChanged(); }
        }

        public StagingRowState Estado
        {
            get => _row.Estado;
            private set { _row.Estado = value; OnPropertyChanged(); }
        }

        public double? DiametroAgujeroDetectadoMm
        {
            get => _row.DiametroAgujeroDetectadoMm;
            set { _row.DiametroAgujeroDetectadoMm = value; OnPropertyChanged(); }
        }

        public string? Detalle
        {
            get => _row.Detalle;
            set { _row.Detalle = value; OnPropertyChanged(); }
        }

        public StagingRow ToModel() => _row;

        public FastenerCatalogItem? BuscarEnCatalogo() =>
            _row.TieneDatosBasicosCompletos ? _catalog.Find(_row.Tipo!, _row.Metrica!, _row.Longitud!.Value) : null;

        private void RecalcularEstado()
        {
            if (Estado == StagingRowState.Insertado)
            {
                return; // no revertir el estado de una fila ya insertada por edición posterior
            }
            Estado = _row.TieneDatosBasicosCompletos ? StagingRowState.ListoParaInsertar : StagingRowState.PendienteDatos;
        }

        public void MarcarError(string detalle)
        {
            Detalle = detalle;
            Estado = StagingRowState.Error;
        }

        public void MarcarInsertado()
        {
            Detalle = null;
            Estado = StagingRowState.Insertado;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
