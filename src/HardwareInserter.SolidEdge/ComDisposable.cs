using System.Runtime.InteropServices;

namespace HardwareInserter.SolidEdge
{
    /// <summary>
    /// Envuelve un objeto COM en un <see cref="System.IDisposable"/> para poder liberarlo
    /// de forma determinista con <c>using</c> en vez de repetir Marshal.ReleaseComObject en cada finally.
    /// </summary>
    public sealed class ComDisposable<T> : System.IDisposable where T : class
    {
        public T Value { get; }

        public ComDisposable(T value)
        {
            Value = value;
        }

        public void Dispose()
        {
            if (Value != null && Marshal.IsComObject(Value))
            {
                Marshal.ReleaseComObject(Value);
            }
        }
    }
}
