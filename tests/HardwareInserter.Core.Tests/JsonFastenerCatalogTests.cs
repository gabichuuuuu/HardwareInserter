using System;
using System.IO;
using FluentAssertions;
using HardwareInserter.Core.Catalog;
using Xunit;

namespace HardwareInserter.Core.Tests
{
    public class JsonFastenerCatalogTests : IDisposable
    {
        private readonly string _jsonPath;

        public JsonFastenerCatalogTests()
        {
            _jsonPath = Path.GetTempFileName();
            File.WriteAllText(_jsonPath, @"
            [
              { ""Tipo"": ""DIN912"", ""Metrica"": ""M8"", ""Longitud"": 20, ""RutaServidor"": ""\\\\srv\\DIN912_M8x20.par"" },
              { ""Tipo"": ""DIN912"", ""Metrica"": ""M8"", ""Longitud"": 25, ""RutaServidor"": ""\\\\srv\\DIN912_M8x25.par"" },
              { ""Tipo"": ""DIN912"", ""Metrica"": ""M6"", ""Longitud"": 16, ""RutaServidor"": ""\\\\srv\\DIN912_M6x16.par"" },
              { ""Tipo"": ""DIN933"", ""Metrica"": ""M8"", ""Longitud"": 25, ""RutaServidor"": ""\\\\srv\\DIN933_M8x25.par"" }
            ]");
        }

        public void Dispose() => File.Delete(_jsonPath);

        [Fact]
        public void Constructor_ArchivoInexistente_LanzaFileNotFoundException()
        {
            Action accion = () => new JsonFastenerCatalog(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            accion.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void GetTipos_DevuelveTiposDistintosOrdenados()
        {
            var catalog = new JsonFastenerCatalog(_jsonPath);
            catalog.GetTipos().Should().Equal("DIN912", "DIN933");
        }

        [Fact]
        public void GetMetricas_FiltraPorTipo()
        {
            var catalog = new JsonFastenerCatalog(_jsonPath);
            catalog.GetMetricas("DIN912").Should().Equal("M6", "M8");
        }

        [Fact]
        public void GetMetricas_TipoInexistente_DevuelveListaVacia()
        {
            var catalog = new JsonFastenerCatalog(_jsonPath);
            catalog.GetMetricas("NO_EXISTE").Should().BeEmpty();
        }

        [Fact]
        public void GetLongitudes_FiltraPorTipoYMetrica()
        {
            var catalog = new JsonFastenerCatalog(_jsonPath);
            catalog.GetLongitudes("DIN912", "M8").Should().Equal(20.0, 25.0);
        }

        [Fact]
        public void Find_ExistenteConLongitudFlotante_LaEncuentra()
        {
            var catalog = new JsonFastenerCatalog(_jsonPath);
            var item = catalog.Find("DIN912", "M8", 20.0000001);
            item.Should().NotBeNull();
            item!.RutaServidor.Should().Be(@"\\srv\DIN912_M8x20.par");
        }

        [Fact]
        public void Find_Inexistente_DevuelveNull()
        {
            var catalog = new JsonFastenerCatalog(_jsonPath);
            catalog.Find("DIN912", "M10", 40).Should().BeNull();
        }
    }
}
