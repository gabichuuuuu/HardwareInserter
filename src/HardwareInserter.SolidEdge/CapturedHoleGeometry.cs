using System;
using System.Runtime.InteropServices;
using SolidEdgeGeometry;

namespace HardwareInserter.SolidEdge
{
    /// <summary>
    /// Geometría del agujero capturada desde la selección activa de Solid Edge: la cara cilíndrica
    /// interior (para la restricción axial) y, opcionalmente, la cara plana de apoyo (para la
    /// restricción planar). Mantiene vivos los objetos COM hasta ser liberado.
    /// </summary>
    public sealed class CapturedHoleGeometry : IDisposable
    {
        public CapturedHoleGeometry(Face cylindricalFace, Face? supportFace, double diameterMm)
        {
            CylindricalFace = cylindricalFace;
            SupportFace = supportFace;
            DiameterMm = diameterMm;
        }

        /// <summary>Cara cilíndrica interior del agujero, usada como eje en <c>Relations3d.AddAxial</c>.</summary>
        public Face CylindricalFace { get; }

        /// <summary>Cara plana de apoyo del agujero, usada en <c>Relations3d.AddPlanar</c>. Null si el usuario no la seleccionó.</summary>
        public Face? SupportFace { get; }

        public double DiameterMm { get; }

        public void Dispose()
        {
            if (Marshal.IsComObject(CylindricalFace))
            {
                Marshal.ReleaseComObject(CylindricalFace);
            }
            if (SupportFace != null && Marshal.IsComObject(SupportFace))
            {
                Marshal.ReleaseComObject(SupportFace);
            }
        }
    }
}
