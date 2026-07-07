namespace HardwareInserter.Core.Models
{
    /// <summary>Fila del grid de staging, resultado de importar el Excel y/o edición manual del usuario.</summary>
    public class StagingRow
    {
        public string? Tipo { get; set; }

        public string? Metrica { get; set; }

        public double? Longitud { get; set; }

        public double? Cantidad { get; set; }

        public string? Referencia { get; set; }

        public StagingRowState Estado { get; set; } = StagingRowState.PendienteDatos;

        /// <summary>Diámetro (mm) del agujero capturado desde Solid Edge, para filtrar/validar la fila.</summary>
        public double? DiametroAgujeroDetectadoMm { get; set; }

        /// <summary>Mensaje de error de la última operación fallida (catálogo, copia, inserción, restricción).</summary>
        public string? Detalle { get; set; }

        public bool TieneDatosBasicosCompletos =>
            !string.IsNullOrWhiteSpace(Tipo) && !string.IsNullOrWhiteSpace(Metrica) && Longitud.HasValue;
    }
}
