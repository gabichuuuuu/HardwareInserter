using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HardwareInserter.Core.Models;
using OfficeOpenXml;

namespace HardwareInserter.Core.Excel
{
    /// <summary>
    /// Importa el listado de tornillería a insertar desde un archivo Excel a filas de staging.
    /// Tolera columnas en cualquier orden y datos faltantes (la fila queda PendienteDatos para edición manual).
    /// </summary>
    public static class ExcelStagingImporter
    {
        private const string ColTipo = "Tipo";
        private const string ColMetrica = "Metrica";
        private const string ColLongitud = "Longitud";
        private const string ColCantidad = "Cantidad";
        private const string ColReferencia = "Referencia";

        public static List<StagingRow> Import(string xlsxPath)
        {
            if (!File.Exists(xlsxPath))
            {
                throw new FileNotFoundException("Archivo Excel no encontrado.", xlsxPath);
            }

            var rows = new List<StagingRow>();

            using var package = new ExcelPackage(new FileInfo(xlsxPath));
            if (package.Workbook.Worksheets.Count == 0)
            {
                throw new InvalidDataException("El archivo Excel no contiene ninguna hoja.");
            }

            var sheet = package.Workbook.Worksheets[0];
            var lastRow = sheet.Dimension?.End.Row ?? 1;
            if (lastRow < 2)
            {
                return rows;
            }

            var lastColumn = sheet.Dimension!.End.Column;
            var headerMap = BuildHeaderMap(sheet, lastColumn);

            for (var r = 2; r <= lastRow; r++)
            {
                var tipo = ReadString(sheet, r, headerMap, ColTipo);
                var metrica = ReadString(sheet, r, headerMap, ColMetrica);
                var longitud = ReadDouble(sheet, r, headerMap, ColLongitud);

                var row = new StagingRow
                {
                    Tipo = tipo,
                    Metrica = metrica,
                    Longitud = longitud,
                    Cantidad = ReadDouble(sheet, r, headerMap, ColCantidad),
                    Referencia = ReadString(sheet, r, headerMap, ColReferencia)
                };
                row.Estado = row.TieneDatosBasicosCompletos
                    ? StagingRowState.ListoParaInsertar
                    : StagingRowState.PendienteDatos;

                rows.Add(row);
            }

            return rows;
        }

        private static Dictionary<string, int> BuildHeaderMap(ExcelWorksheet sheet, int lastColumn)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var c = 1; c <= lastColumn; c++)
            {
                var header = sheet.Cells[1, c].Text?.Trim();
                if (!string.IsNullOrEmpty(header) && !map.ContainsKey(header!))
                {
                    map[header!] = c;
                }
            }
            return map;
        }

        private static string? ReadString(ExcelWorksheet sheet, int row, Dictionary<string, int> headerMap, string columnName)
        {
            if (!headerMap.TryGetValue(columnName, out var col))
            {
                return null;
            }
            var text = sheet.Cells[row, col].Text?.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static double? ReadDouble(ExcelWorksheet sheet, int row, Dictionary<string, int> headerMap, string columnName)
        {
            if (!headerMap.TryGetValue(columnName, out var col))
            {
                return null;
            }
            var text = sheet.Cells[row, col].Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : (double?)null;
        }
    }
}
