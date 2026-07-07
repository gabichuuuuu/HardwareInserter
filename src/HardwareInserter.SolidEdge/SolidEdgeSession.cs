using System;
using System.Runtime.InteropServices;
using SolidEdgeFramework;

namespace HardwareInserter.SolidEdge
{
    /// <summary>
    /// Conexión a la instancia activa de Solid Edge para uso como macro/EXE standalone
    /// (no add-in COM registrado). Solid Edge debe estar ya abierto con un documento activo.
    /// </summary>
    public sealed class SolidEdgeSession : IDisposable
    {
        private const int MK_E_UNAVAILABLE = unchecked((int)0x800401E3);
        private const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
        private const int RPC_E_SERVERFAULT = unchecked((int)0x80010105);

        private Application? _application;

        public Application Application => _application ?? Connect();

        /// <summary>Conecta (o reconecta) con la instancia activa de Solid Edge.</summary>
        public Application Connect()
        {
            try
            {
                _application = (Application)Marshal.GetActiveObject("SolidEdge.Application");
                return _application;
            }
            catch (COMException ex) when (ex.HResult == MK_E_UNAVAILABLE)
            {
                throw new InvalidOperationException(
                    "Solid Edge no está abierto o no responde. Abra Solid Edge con un ensamblaje antes de continuar.", ex);
            }
        }

        /// <summary>
        /// Ejecuta <paramref name="action"/> contra la instancia conectada; si la sesión se perdió
        /// (RPC_E_DISCONNECTED/RPC_E_SERVERFAULT), intenta reconectar una vez antes de propagar el error.
        /// </summary>
        public T Execute<T>(Func<Application, T> action)
        {
            try
            {
                return action(Application);
            }
            catch (COMException ex) when (ex.HResult == RPC_E_DISCONNECTED || ex.HResult == RPC_E_SERVERFAULT)
            {
                _application = null;
                return action(Connect());
            }
        }

        public void Dispose()
        {
            if (_application != null && Marshal.IsComObject(_application))
            {
                Marshal.ReleaseComObject(_application);
            }
            _application = null;
        }
    }
}
