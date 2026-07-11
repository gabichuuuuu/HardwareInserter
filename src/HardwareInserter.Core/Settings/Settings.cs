using System.IO;
using System.Text.Json;

namespace HardwareInserter.Core.Preferences
{
    /// <summary>
    /// Preferencias persistentes del add-in, almacenadas como JSON en
    /// <c>%AppData%\HardwareInserter\settings.json</c>. De momento solo guarda la carpeta raíz
    /// de la librería de hardware seleccionada por el usuario.
    /// </summary>
    public sealed class Settings
    {
        /// <summary>Separador de archivos JSON usado para serializar/deserializar <see cref="Settings"/>.</summary>
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        /// <summary>Carpeta raíz de la librería de hardware escaneada por <see cref="Library.IHardwareLibrary"/>.</summary>
        public string RootPath { get; set; } = string.Empty;

        /// <summary>Devuelve la ruta canónica del archivo de preferencias en <c>%AppData%</c>.</summary>
        public static string GetDefaultFilePath()
        {
            var appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "HardwareInserter");
            return Path.Combine(folder, "settings.json");
        }

        /// <summary>Carga las preferencias desde <paramref name="filePath"/>; si no existe, devuelve una instancia vacía.</summary>
        public static Settings Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new Settings();
            }
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Settings>(json, SerializerOptions) ?? new Settings();
        }

        /// <summary>Guarda las preferencias en <paramref name="filePath"/>, creando el directorio si hace falta.</summary>
        public void Save(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var json = JsonSerializer.Serialize(this, SerializerOptions);
            File.WriteAllText(filePath, json);
        }
    }
}
