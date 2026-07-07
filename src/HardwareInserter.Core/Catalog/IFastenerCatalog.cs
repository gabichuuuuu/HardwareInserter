using System.Collections.Generic;
using HardwareInserter.Core.Models;

namespace HardwareInserter.Core.Catalog
{
    /// <summary>Consulta de tornillería disponible, usada para poblar los combos en cascada del grid.</summary>
    public interface IFastenerCatalog
    {
        IReadOnlyList<string> GetTipos();

        IReadOnlyList<string> GetMetricas(string tipo);

        IReadOnlyList<double> GetLongitudes(string tipo, string metrica);

        FastenerCatalogItem? Find(string tipo, string metrica, double longitud);
    }
}
