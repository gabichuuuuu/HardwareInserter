using System;
using System.IO;

namespace HardwareInserter.SolidEdge
{
    /// <summary>
    /// Gestiona la carpeta local "Hardware" (junto al ensamblaje activo) donde se copian
    /// los archivos .par de tornillería antes de insertarlos, para no depender del servidor en tiempo real.
    /// </summary>
    public sealed class HardwareFileManager
    {
        private const string HardwareFolderName = "Hardware";

        /// <summary>
        /// Garantiza que exista una copia local del .par de <paramref name="rutaServidor"/> junto al
        /// ensamblaje activo, copiándola si aún no existe. Devuelve la ruta local resultante.
        /// </summary>
        public string EnsureLocalCopy(string assemblyPath, string rutaServidor)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                throw new InvalidOperationException(
                    "El ensamblaje activo no tiene una ruta guardada. Guarde el ensamblaje antes de insertar tornillería.");
            }

            var assemblyDirectory = Path.GetDirectoryName(assemblyPath)
                ?? throw new InvalidOperationException($"No se pudo determinar el directorio de '{assemblyPath}'.");

            var hardwareDirectory = Path.Combine(assemblyDirectory, HardwareFolderName);
            Directory.CreateDirectory(hardwareDirectory);

            var destino = Path.Combine(hardwareDirectory, Path.GetFileName(rutaServidor));
            if (!File.Exists(destino))
            {
                if (!File.Exists(rutaServidor))
                {
                    throw new FileNotFoundException("No se encuentra el archivo maestro en el servidor.", rutaServidor);
                }
                File.Copy(rutaServidor, destino, overwrite: false);
            }

            return destino;
        }
    }
}
