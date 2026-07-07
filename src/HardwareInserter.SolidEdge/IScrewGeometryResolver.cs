using SolidEdgeAssembly;

namespace HardwareInserter.SolidEdge
{
    /// <summary>Caras resueltas de un tornillo recién insertado, necesarias para las restricciones 3D.</summary>
    public sealed class ScrewGeometryFaces
    {
        public ScrewGeometryFaces(object axisFace, object headBottomFace)
        {
            AxisFace = axisFace;
            HeadBottomFace = headBottomFace;
        }

        /// <summary>Cara cilíndrica del vástago, usada como eje en <c>Relations3d.AddAxial</c>.</summary>
        public object AxisFace { get; }

        /// <summary>Cara plana inferior de la cabeza, usada en <c>Relations3d.AddPlanar</c>.</summary>
        public object HeadBottomFace { get; }
    }

    /// <summary>
    /// Identifica, dentro del cuerpo de un tornillo recién insertado, cuál cara es el eje del
    /// vástago y cuál la cara de apoyo de la cabeza. No existe una API genérica de Solid Edge para
    /// esto: depende de la convención con la que se modelaron los archivos .par de tornillería.
    /// Punto de extensión: si las plantillas del servidor adoptan nombres de cara consistentes,
    /// añadir una implementación alternativa (ej. NamedFaceScrewGeometryResolver) sin tocar el resto del flujo.
    /// </summary>
    public interface IScrewGeometryResolver
    {
        ScrewGeometryFaces Resolve(Occurrence screwOccurrence);
    }
}
