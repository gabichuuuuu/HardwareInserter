using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HardwareInserter.Core.Models;
using Newtonsoft.Json;

namespace HardwareInserter.Core.Catalog
{
    /// <summary>
    /// Catálogo de tornillería cargado desde un archivo JSON local que simula el índice del servidor.
    /// Se carga una única vez en el constructor; no es estático para poder inyectar rutas distintas en tests.
    /// </summary>
    public sealed class JsonFastenerCatalog : IFastenerCatalog
    {
        private readonly List<FastenerCatalogItem> _items;

        public JsonFastenerCatalog(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException("Catálogo de tornillería no encontrado.", jsonFilePath);
            }

            var json = File.ReadAllText(jsonFilePath);
            _items = JsonConvert.DeserializeObject<List<FastenerCatalogItem>>(json) ?? new List<FastenerCatalogItem>();
        }

        public IReadOnlyList<string> GetTipos() =>
            _items.Select(i => i.Tipo)
                  .Distinct(StringComparer.OrdinalIgnoreCase)
                  .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                  .ToList();

        public IReadOnlyList<string> GetMetricas(string tipo) =>
            _items.Where(i => string.Equals(i.Tipo, tipo, StringComparison.OrdinalIgnoreCase))
                  .Select(i => i.Metrica)
                  .Distinct(StringComparer.OrdinalIgnoreCase)
                  .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
                  .ToList();

        public IReadOnlyList<double> GetLongitudes(string tipo, string metrica) =>
            _items.Where(i => string.Equals(i.Tipo, tipo, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(i.Metrica, metrica, StringComparison.OrdinalIgnoreCase))
                  .Select(i => i.Longitud)
                  .Distinct()
                  .OrderBy(l => l)
                  .ToList();

        public FastenerCatalogItem? Find(string tipo, string metrica, double longitud) =>
            _items.FirstOrDefault(i =>
                string.Equals(i.Tipo, tipo, StringComparison.OrdinalIgnoreCase)
                && string.Equals(i.Metrica, metrica, StringComparison.OrdinalIgnoreCase)
                && Math.Abs(i.Longitud - longitud) < 0.001);
    }
}
