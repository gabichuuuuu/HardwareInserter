using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HardwareInserter.Core.Library
{
    /// <summary>
    /// Implementación de <see cref="IHardwareLibrary"/> que descubre los <c>.par</c> recorriendo
    /// recursivamente una carpeta raíz del sistema de archivos, y los mantiene sincronizados vía
    /// <see cref="System.IO.FileSystemWatcher"/>: cualquier <c>.par</c> añadido a la raíz aparece
    /// automáticamente sin reiniciar Solid Edge.
    /// </summary>
    public sealed class FileSystemFastenerLibrary : IHardwareLibrary
    {
        private const string RootCategoryLabel = "(Raíz)";

        private readonly FileSystemWatcher? _watcher;
        private readonly object _sync = new object();
        private List<LibraryItem> _items = new List<LibraryItem>();
        private bool _disposed;

        /// <summary>
        /// Construye la librería apuntando a <paramref name="rootPath"/>. Si la ruta no existe se lanza
        /// <see cref="DirectoryNotFoundException"/>; la librería no funciona sin una raíz válida.
        /// </summary>
        public FileSystemFastenerLibrary(string rootPath)
        {
            SetRootPathInternal(rootPath);

            _watcher = new FileSystemWatcher(RootPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
            };
            _watcher.Created += OnWatcherEvent;
            _watcher.Deleted += OnWatcherEvent;
            _watcher.Renamed += OnWatcherEvent;
        }

        /// <inheritdoc />
        public string RootPath { get; private set; } = string.Empty;

        /// <inheritdoc />
        public IReadOnlyList<LibraryItem> Items
        {
            get
            {
                lock (_sync)
                {
                    return _items.ToList();
                }
            }
        }

        /// <inheritdoc />
        public event EventHandler? Changed;

        /// <inheritdoc />
        public void Refresh() => ScanAndNotify();

        /// <inheritdoc />
        public void SetRootPath(string rootPath)
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
            }
            SetRootPathInternal(rootPath);
            if (_watcher != null)
            {
                _watcher.Path = RootPath;
                _watcher.EnableRaisingEvents = true;
            }
            ScanAndNotify();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnWatcherEvent;
                _watcher.Deleted -= OnWatcherEvent;
                _watcher.Renamed -= OnWatcherEvent;
                _watcher.Dispose();
            }
        }

        private void SetRootPathInternal(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("La carpeta raíz de la librería no puede estar vacía.", nameof(rootPath));
            }
            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException($"No existe la carpeta de librería '{rootPath}'.");
            }
            RootPath = Path.GetFullPath(rootPath);
        }

        private void OnWatcherEvent(object sender, FileSystemEventArgs e) => ScanAndNotify();

        private void ScanAndNotify()
        {
            if (_disposed) return;
            List<LibraryItem> snapshot;
            try
            {
                snapshot = Scan(RootPath);
            }
            catch (Exception)
            {
                // Si el escaneo falla (carpeta borrada, acceso denegado...) no rompemos la UI:
                // mantenemos el snapshot anterior y no lanzamos el evento.
                return;
            }

            bool changed;
            lock (_sync)
            {
                changed = !SequenceEqual(_items, snapshot);
                if (changed)
                {
                    _items = snapshot;
                }
            }

            if (changed)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Enumera todos los <c>.par</c> bajo <paramref name="rootPath"/>, ordenados por ruta relativa.
        /// La categoría es la subcarpeta inmediata relativa a la raíz (o "(Raíz)" si está en la raíz).
        /// El filtrado por extensión es case-insensitive para que el comportamiento sea consistente
        /// en Windows (donde <c>*.par</c> coge <c>.PAR</c>) y en Linux/Mono (donde es case-sensitive).
        /// </summary>
        private static List<LibraryItem> Scan(string rootPath)
        {
            var root = Path.GetFullPath(rootPath);
            var rootDir = new DirectoryInfo(root);
            var items = new List<LibraryItem>();

            foreach (var file in rootDir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (!IsParFile(file))
                {
                    continue;
                }
                var relativePath = PathHelper.GetRelativePath(root, file.FullName);
                var category = ResolveCategory(root, file);
                items.Add(new LibraryItem(file.FullName, relativePath, category, file.Name));
            }

            items.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.Ordinal));
            return items;
        }

        private static bool IsParFile(FileInfo file)
        {
            var ext = file.Extension;
            return string.Equals(ext, ".par", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveCategory(string root, FileInfo file)
        {
            var relativePath = PathHelper.GetRelativePath(root, file.DirectoryName ?? root);
            if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
            {
                return RootCategoryLabel;
            }
            var segments = relativePath.Split(Path.DirectorySeparatorChar);
            return segments[0];
        }

        /// <summary>Comparación por valor de la lista (mismo orden y mismos FullPaths).</summary>
        private static bool SequenceEqual(List<LibraryItem> a, List<LibraryItem> b)
        {
            if (a.Count != b.Count) return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i].FullPath, b[i].FullPath, StringComparison.Ordinal)) return false;
            }
            return true;
        }
    }
}
