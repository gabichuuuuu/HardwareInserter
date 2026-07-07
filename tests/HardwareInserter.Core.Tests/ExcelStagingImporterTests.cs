using System;
using System.IO;
using FluentAssertions;
using HardwareInserter.Core.Excel;
using HardwareInserter.Core.Models;
using OfficeOpenXml;
using Xunit;

namespace HardwareInserter.Core.Tests
{
    public class ExcelStagingImporterTests : IDisposable
    {
        private readonly string _xlsxPath;

        public ExcelStagingImporterTests()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _xlsxPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");
        }

        public void Dispose()
        {
            if (File.Exists(_xlsxPath)) File.Delete(_xlsxPath);
        }

        [Fact]
        public void Import_ColumnasCompletas_PoblaFilasListasParaInsertar()
        {
            CrearExcel(new[] { "Tipo", "Metrica", "Longitud" }, new object[][]
            {
                new object[] { "DIN912", "M8", 20 },
                new object[] { "DIN933", "M6", 16 }
            });

            var rows = ExcelStagingImporter.Import(_xlsxPath);

            rows.Should().HaveCount(2);
            rows[0].Tipo.Should().Be("DIN912");
            rows[0].Metrica.Should().Be("M8");
            rows[0].Longitud.Should().Be(20);
            rows[0].Estado.Should().Be(StagingRowState.ListoParaInsertar);
        }

        [Fact]
        public void Import_ColumnasParciales_MarcaFilaPendienteDatos()
        {
            CrearExcel(new[] { "Tipo", "Metrica", "Longitud" }, new object[][]
            {
                new object[] { "DIN912", "", 20 }
            });

            var rows = ExcelStagingImporter.Import(_xlsxPath);

            rows.Should().HaveCount(1);
            rows[0].Metrica.Should().BeNull();
            rows[0].Estado.Should().Be(StagingRowState.PendienteDatos);
        }

        [Fact]
        public void Import_ColumnasEnOrdenDistinto_MapeaPorEncabezado()
        {
            CrearExcel(new[] { "Longitud", "Tipo", "Metrica" }, new object[][]
            {
                new object[] { 25, "DIN933", "M8" }
            });

            var rows = ExcelStagingImporter.Import(_xlsxPath);

            rows.Should().HaveCount(1);
            rows[0].Tipo.Should().Be("DIN933");
            rows[0].Metrica.Should().Be("M8");
            rows[0].Longitud.Should().Be(25);
        }

        [Fact]
        public void Import_ArchivoInexistente_LanzaFileNotFoundException()
        {
            Action accion = () => ExcelStagingImporter.Import(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            accion.Should().Throw<FileNotFoundException>();
        }

        private void CrearExcel(string[] headers, object[][] dataRows)
        {
            using var package = new ExcelPackage(new FileInfo(_xlsxPath));
            var sheet = package.Workbook.Worksheets.Add("Hardware");

            for (var c = 0; c < headers.Length; c++)
            {
                sheet.Cells[1, c + 1].Value = headers[c];
            }

            for (var r = 0; r < dataRows.Length; r++)
            {
                for (var c = 0; c < dataRows[r].Length; c++)
                {
                    sheet.Cells[r + 2, c + 1].Value = dataRows[r][c];
                }
            }

            package.Save();
        }
    }
}
