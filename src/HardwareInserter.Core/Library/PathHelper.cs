using System;
using System.IO;
using System.Text;

namespace HardwareInserter.Core.Library
{
    /// <summary>
    /// Utilidades de ruta para net48, que no dispone de <c>Path.GetRelativePath</c> (añadido en .NET Core 2.1).
    /// Implementación equivalente al algoritmo de <c>Path.GetRelativePath</c> de .NET Core.
    /// </summary>
    internal static class PathHelper
    {
        /// <summary>
        /// Devuelve la ruta relativa desde <paramref name="relativeTo"/> hasta <paramref name="path"/>,
        /// usando separadores del SO. Ambas rutas deben ser absolutas y estar normalizadas.
        /// </summary>
        public static string GetRelativePath(string relativeTo, string path)
        {
            if (string.IsNullOrEmpty(relativeTo)) throw new ArgumentException("relativeTo vacía", nameof(relativeTo));
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("path vacío", nameof(path));

            relativeTo = Path.GetFullPath(relativeTo);
            path = Path.GetFullPath(path);

            // Caso trivial: misma ruta → ".".
            if (string.Equals(relativeTo, path, StringComparison.Ordinal))
            {
                return ".";
            }

            var relativeToHasTrailing = EndsInSeparator(relativeTo);
            if (!relativeToHasTrailing)
            {
                relativeTo += Path.DirectorySeparatorChar;
            }

            var commonLength = GetCommonPathLength(relativeTo, path);
            if (commonLength == 0)
            {
                return path;
            }

            var sb = new StringBuilder();
            for (var i = commonLength; i < relativeTo.Length; i++)
            {
                var c = relativeTo[i];
                if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
                {
                    sb.Append("..").Append(Path.DirectorySeparatorChar);
                }
            }

            if (path.Length > commonLength)
            {
                sb.Append(path.Substring(commonLength));
            }
            else if (sb.Length > 0 && sb[sb.Length - 1] == Path.DirectorySeparatorChar)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        private static bool EndsInSeparator(string path)
        {
            if (path.Length == 0) return false;
            var c = path[path.Length - 1];
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }

        private static int GetCommonPathLength(string a, string b)
        {
            var length = Math.Min(a.Length, b.Length);
            for (var i = 0; i < length; i++)
            {
                var c1 = a[i];
                var c2 = b[i];
                if (c1 == c2) continue;
                if (IsSeparator(c1) && IsSeparator(c2)) continue;
                return i;
            }
            return length;
        }

        private static bool IsSeparator(char c) =>
            c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
    }
}
