using System;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using HardwareInserter.Core.Library;
using Xunit;

namespace HardwareInserter.Core.Tests
{
    public class FileSystemFastenerLibraryTests : IDisposable
    {
        private readonly string _root;

        public FileSystemFastenerLibraryTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "HardwareInserterTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        [Fact]
        public void Constructor_CarpetaInexistente_LanzaDirectoryNotFoundException()
        {
            var rutaInexistente = Path.Combine(Path.GetTempPath(), "no_existe_" + Guid.NewGuid().ToString("N"));
            Action accion = () => new FileSystemFastenerLibrary(rutaInexistente);
            accion.Should().Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void Items_CarpetaVacia_DevuelveListaVacia()
        {
            using var lib = new FileSystemFastenerLibrary(_root);
            lib.Items.Should().BeEmpty();
        }

        [Fact]
        public void Refresh_CarpetaConPar_DevuelveItemConRutaYCategoria()
        {
            var subDir = Path.Combine(_root, "DIN912");
            Directory.CreateDirectory(subDir);
            var par = Path.Combine(subDir, "DIN912_M6x16.par");
            File.WriteAllText(par, "placeholder");

            using var lib = new FileSystemFastenerLibrary(_root);
            lib.Refresh();

            lib.Items.Should().HaveCount(1);
            var item = lib.Items.Single();
            item.Filename.Should().Be("DIN912_M6x16.par");
            item.Category.Should().Be("DIN912");
            item.FullPath.Should().Be(par);
            item.RelativePath.Should().Be(Path.Combine("DIN912", "DIN912_M6x16.par"));
        }

        [Fact]
        public void Refresh_SubcarpetasAnidadas_AgrupaPorCarpetaInmediata()
        {
            // Raíz / Tornillería / DIN912 / M6 / DIN912_M6x16.par
            var nestedDir = Path.Combine(_root, "Tornillería", "DIN912", "M6");
            Directory.CreateDirectory(nestedDir);
            File.WriteAllText(Path.Combine(nestedDir, "DIN912_M6x16.par"), "x");
            File.WriteAllText(Path.Combine(_root, "root.par"), "x");

            using var lib = new FileSystemFastenerLibrary(_root);
            lib.Refresh();

            lib.Items.Should().HaveCount(2);
            var rootItem = lib.Items.Single(i => i.Filename == "root.par");
            rootItem.Category.Should().Be("(Raíz)");
            var nestedItem = lib.Items.Single(i => i.Filename == "DIN912_M6x16.par");
            nestedItem.Category.Should().Be("Tornillería");
        }

        [Fact]
        public void Refresh_SoloPar_OtrosArchivosSonIgnorados()
        {
            File.WriteAllText(Path.Combine(_root, "doc.txt"), "x");
            File.WriteAllText(Path.Combine(_root, "drawing.dxf"), "x");
            File.WriteAllText(Path.Combine(_root, "screw.PAR"), "x");

            using var lib = new FileSystemFastenerLibrary(_root);
            lib.Refresh();

            lib.Items.Should().HaveCount(1);
            lib.Items.Single().Filename.Should().Be("screw.PAR");
        }

        [Fact]
        public void SetRootPath_NuevaCarpeta_ReescaneaItems()
        {
            File.WriteAllText(Path.Combine(_root, "a.par"), "x");
            using var lib = new FileSystemFastenerLibrary(_root);
            lib.Refresh();
            lib.Items.Should().HaveCount(1);

            var otraRaiz = Path.Combine(Path.GetTempPath(), "HardwareInserterTests_otra_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(otraRaiz);
            try
            {
                File.WriteAllText(Path.Combine(otraRaiz, "b.par"), "x");
                File.WriteAllText(Path.Combine(otraRaiz, "c.par"), "x");

                lib.SetRootPath(otraRaiz);

                lib.Items.Should().HaveCount(2);
                lib.RootPath.Should().Be(Path.GetFullPath(otraRaiz));
            }
            finally
            {
                if (Directory.Exists(otraRaiz)) Directory.Delete(otraRaiz, recursive: true);
            }
        }

        [Fact]
        public void Changed_NuevoPar_ApareceTrasWatch()
        {
            using var lib = new FileSystemFastenerLibrary(_root);
            lib.Refresh();

            var fired = false;
            lib.Changed += (s, e) => fired = true;

            // Crear un .par nuevo en la raíz debe disparar el FileSystemWatcher.
            File.WriteAllText(Path.Combine(_root, "nuevo.par"), "x");

            // Esperar a que el watcher procese (hasta 2s).
            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (!fired && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(50);
                lib.Refresh();
            }

            fired.Should().BeTrue("el FileSystemWatcher debería haber disparado Changed al crear un .par");
            lib.Items.Should().Contain(i => i.Filename == "nuevo.par");
        }

        [Fact]
        public void Items_OrdenadoPorRutaRelativa()
        {
            Directory.CreateDirectory(Path.Combine(_root, "z"));
            Directory.CreateDirectory(Path.Combine(_root, "a"));
            File.WriteAllText(Path.Combine(_root, "z", "tornillo.par"), "x");
            File.WriteAllText(Path.Combine(_root, "a", "tornillo.par"), "x");
            File.WriteAllText(Path.Combine(_root, "root.par"), "x");

            using var lib = new FileSystemFastenerLibrary(_root);
            lib.Refresh();

            var relativas = lib.Items.Select(i => i.RelativePath).ToList();
            relativas.Should().BeInAscendingOrder();
        }

        [Fact]
        public void Changed_EliminarPar_DesapareceTrasWatch()
        {
            var parPath = Path.Combine(_root, "eliminar.par");
            File.WriteAllText(parPath, "x");
            using var lib = new FileSystemFastenerLibrary(_root);
            lib.Refresh();
            lib.Items.Should().HaveCount(1);

            var fired = false;
            lib.Changed += (s, e) => fired = true;

            File.Delete(parPath);

            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (!fired && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(50);
            }

            fired.Should().BeTrue("el FileSystemWatcher debería haber disparado Changed al borrar un .par");
            lib.Refresh();
            lib.Items.Should().BeEmpty();
        }
    }
}
