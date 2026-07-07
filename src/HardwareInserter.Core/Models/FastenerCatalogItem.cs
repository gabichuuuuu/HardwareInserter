namespace HardwareInserter.Core.Models
{
    /// <summary>Entrada del índice de tornillería (simula el catálogo del servidor).</summary>
    public class FastenerCatalogItem
    {
        public string Tipo { get; set; } = string.Empty;

        public string Metrica { get; set; } = string.Empty;

        public double Longitud { get; set; }

        public string RutaServidor { get; set; } = string.Empty;
    }
}
