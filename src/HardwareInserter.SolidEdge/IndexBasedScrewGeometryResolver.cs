using System;
using System.Runtime.InteropServices;
using SolidEdgeAssembly;
using SolidEdgeGeometry;

namespace HardwareInserter.SolidEdge
{
    /// <summary>
    /// Implementación por defecto de <see cref="IScrewGeometryResolver"/>: asume que todas las
    /// plantillas .par del servidor de tornillería siguen el mismo orden/convención de modelado
    /// (revolución del vástago primero, cabeza después), y localiza:
    ///   - AxisFace: la cara cilíndrica de mayor área del cuerpo (el vástago; en un tornillo típico
    ///     es la cara cilíndrica de mayor superficie frente a filetes/chaflanes menores).
    ///   - HeadBottomFace: la cara plana de mayor área (el asiento inferior de la cabeza).
    /// Firmas usadas verificadas por desensamblado del paquete NuGet Interop.SolidEdge 220.2.0:
    /// Occurrence.Body (SolidEdgeGeometry.Body), Body.Faces(FeatureTopologyQueryTypeConstants) y
    /// Face.Geometry/Face.Area (SolidEdgeGeometry.Face).
    /// RIESGO: la heurística "mayor área cilíndrica = vástago" / "mayor área plana = asiento de
    /// cabeza" NO está validada contra archivos .par reales (no hay Solid Edge instalado en el
    /// entorno de desarrollo). Debe verificarse con 2-3 tornillos reales antes de usarla en
    /// producción; si las plantillas no comparten convención, sustituir esta clase por una que
    /// resuelva por nombre de cara.
    /// </summary>
    public sealed class IndexBasedScrewGeometryResolver : IScrewGeometryResolver
    {
        public ScrewGeometryFaces Resolve(Occurrence screwOccurrence)
        {
            object? bodyObj = null;
            object? facesObj = null;
            try
            {
                bodyObj = screwOccurrence.Body;
                var body = (Body)bodyObj;

                facesObj = body.Faces[FeatureTopologyQueryTypeConstants.igQueryAll];
                var faces = (Faces)facesObj;

                Face? bestCylinder = null;
                double bestCylinderArea = -1;
                Face? bestPlane = null;
                double bestPlaneArea = -1;

                for (var i = 1; i <= faces.Count; i++)
                {
                    var faceObj = faces.Item(i);
                    var face = (Face)faceObj;
                    var retained = false;
                    object? geometry = null;
                    try
                    {
                        geometry = face.Geometry;
                        if (geometry is Cylinder)
                        {
                            var area = face.Area;
                            if (area > bestCylinderArea)
                            {
                                bestCylinderArea = area;
                                ReleaseIfSet(bestCylinder);
                                bestCylinder = face;
                                retained = true;
                            }
                        }
                        else if (geometry is Plane)
                        {
                            var area = face.Area;
                            if (area > bestPlaneArea)
                            {
                                bestPlaneArea = area;
                                ReleaseIfSet(bestPlane);
                                bestPlane = face;
                                retained = true;
                            }
                        }
                    }
                    finally
                    {
                        if (geometry != null && Marshal.IsComObject(geometry)) Marshal.ReleaseComObject(geometry);
                        if (!retained && Marshal.IsComObject(face)) Marshal.ReleaseComObject(face);
                    }
                }

                if (bestCylinder == null || bestPlane == null)
                {
                    ReleaseIfSet(bestCylinder);
                    ReleaseIfSet(bestPlane);
                    throw new InvalidOperationException(
                        "No se pudo identificar el eje del vástago y/o la cara de apoyo de la cabeza del tornillo. " +
                        "Verifique la convención de modelado del archivo .par o use un resolver alternativo.");
                }

                return new ScrewGeometryFaces(bestCylinder, bestPlane);
            }
            finally
            {
                if (facesObj != null && Marshal.IsComObject(facesObj)) Marshal.ReleaseComObject(facesObj);
                if (bodyObj != null && Marshal.IsComObject(bodyObj)) Marshal.ReleaseComObject(bodyObj);
            }
        }

        private static void ReleaseIfSet(object? comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                Marshal.ReleaseComObject(comObject);
            }
        }
    }
}
