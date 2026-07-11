using System;
using System.IO;
using FluentAssertions;
using HardwareInserter.Core.Preferences;
using Xunit;

namespace HardwareInserter.Core.Tests
{
    public class SettingsTests : IDisposable
    {
        private readonly string _settingsPath;

        public SettingsTests()
        {
            _settingsPath = Path.Combine(Path.GetTempPath(), "HardwareInserter_settings_" + Guid.NewGuid().ToString("N") + ".json");
        }

        public void Dispose()
        {
            if (File.Exists(_settingsPath)) File.Delete(_settingsPath);
        }

        [Fact]
        public void Load_ArchivoInexistente_DevuelveInstanciaVacia()
        {
            var settings = Settings.Load(_settingsPath);
            settings.RootPath.Should().BeEmpty();
        }

        [Fact]
        public void Save_CreaArchivo_CreaDirectorioYContenido()
        {
            var settings = new Settings { RootPath = @"C:\Tornilleria" };

            settings.Save(_settingsPath);

            File.Exists(_settingsPath).Should().BeTrue();
            var json = File.ReadAllText(_settingsPath);
            json.Should().Contain("rootPath");
            json.Should().Contain("Tornilleria");
        }

        [Fact]
        public void SaveYLoad_RoundTrip_PreservaRootPath()
        {
            var settings = new Settings { RootPath = @"C:\Tornilleria" };
            settings.Save(_settingsPath);

            var loaded = Settings.Load(_settingsPath);

            loaded.RootPath.Should().Be(@"C:\Tornilleria");
        }

        [Fact]
        public void GetDefaultFilePath_DevuelveRutaBajoAppData()
        {
            var path = Settings.GetDefaultFilePath();

            path.Should().EndWith("settings.json");
            Path.GetDirectoryName(path).Should().Contain("HardwareInserter");
        }
    }
}
