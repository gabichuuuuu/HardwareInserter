using System;
using System.Runtime.InteropServices;
using SolidEdgeAssembly;
using SolidEdgeFramework;
using SolidEdgeGeometry;

namespace HardwareInserter.SolidEdge
{
    /// <summary>
    /// Envuelve todas las llamadas a la API de Solid Edge: captura de geometría del agujero,
    /// inserción del hardware (.par) como occurrence y, opcionalmente, restricción axial/planar
    /// contra el agujero. Todo objeto COM obtenido explícitamente se libera con
    /// <see cref="Marshal.ReleaseComObject"/> en finally.
    /// </summary>
    /// <remarks>
    /// A diferencia de la versión anterior (macro standalone que resolvía la sesión vía
    /// <c>Marshal.GetActiveObject</c>), esta clase recibe <see cref="Application"/> por parámetro:
    /// el add-in COM ya la recibe en <c>OnConnection</c> y se la inyecta. No hay gestión de sesión.
    /// </remarks>
    public sealed class SolidEdgeHelper
    {
        private readonly IScrewGeometryResolver _screwGeometryResolver;

        public SolidEdgeHelper(IScrewGeometryResolver screwGeometryResolver)
        {
            _screwGeometryResolver = screwGeometryResolver;
        }

        /// <summary>
        /// Lee el <c>SelectSet</c> activo del ensamblaje. Se espera que el usuario haya seleccionado
        /// (con Ctrl+click en Solid Edge) la cara cilíndrica interior del agujero y, opcionalmente,
        /// la cara plana de apoyo de la cabeza del tornillo, antes de pulsar "Capturar Agujero".
        /// RIESGO no verificado sin Solid Edge real: las longitudes de la API vienen en el sistema de
        /// unidades interno del documento (habitualmente metros); aquí se asume metros y se convierte
        /// a mm — validar contra un documento real antes de confiar en el diámetro mostrado.
        /// </summary>
        public CapturedHoleGeometry CaptureSelectedHoleGeometry(Application application)
        {
            if (application == null) throw new ArgumentNullException(nameof(application));

            SelectSet? selectSet = null;
            Face? cylindricalFace = null;
            Face? supportFace = null;
            var retained = false;
            try
            {
                selectSet = application.ActiveSelectSet;
                if (selectSet.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Seleccione la cara cilíndrica interior del agujero (y opcionalmente su cara de apoyo) antes de capturar.");
                }

                for (var i = 1; i <= selectSet.Count; i++)
                {
                    var item = selectSet.Item(i);
                    if (item is not Face face)
                    {
                        if (Marshal.IsComObject(item)) Marshal.ReleaseComObject(item);
                        continue;
                    }

                    object? geometry = null;
                    try
                    {
                        geometry = face.Geometry;
                        if (geometry is Cylinder && cylindricalFace == null)
                        {
                            cylindricalFace = face;
                        }
                        else if (geometry is Plane && supportFace == null)
                        {
                            supportFace = face;
                        }
                        else if (Marshal.IsComObject(face))
                        {
                            Marshal.ReleaseComObject(face);
                        }
                    }
                    finally
                    {
                        if (geometry != null && Marshal.IsComObject(geometry)) Marshal.ReleaseComObject(geometry);
                    }
                }

                if (cylindricalFace == null)
                {
                    throw new InvalidOperationException("Ninguna de las caras seleccionadas es cilíndrica.");
                }

                var cylinder = (Cylinder)cylindricalFace.Geometry;
                const double MetrosAMilimetros = 1000.0;
                double diameterMm;
                try
                {
                    diameterMm = cylinder.Radius * 2.0 * MetrosAMilimetros;
                }
                finally
                {
                    if (Marshal.IsComObject(cylinder)) Marshal.ReleaseComObject(cylinder);
                }

                retained = true; // CapturedHoleGeometry se hace responsable de liberar las caras
                return new CapturedHoleGeometry(cylindricalFace, supportFace, diameterMm);
            }
            finally
            {
                if (!retained)
                {
                    if (cylindricalFace != null && Marshal.IsComObject(cylindricalFace)) Marshal.ReleaseComObject(cylindricalFace);
                    if (supportFace != null && Marshal.IsComObject(supportFace)) Marshal.ReleaseComObject(supportFace);
                }
                if (selectSet != null) Marshal.ReleaseComObject(selectSet);
            }
        }

        /// <summary>
        /// Inserta el archivo <c>.par</c> (ya copiado localmente) como occurrence del ensamblaje activo.
        /// Si <paramref name="applyConstraints"/> es <c>true</c>, lo restringe: eje del vástago contra
        /// la cara cilíndrica del agujero (<c>AddAxial</c>), y cara inferior de la cabeza contra la cara
        /// de apoyo del agujero (<c>AddPlanar</c>, si fue capturada). Si es <c>false</c> (anchors),
        /// inserta la occurrence sin aplicar restricciones — el usuario la posiciona libremente.
        /// </summary>
        public void InsertOccurrenceAndConstrain(
            Application application,
            string localParPath,
            CapturedHoleGeometry hole,
            bool applyConstraints)
        {
            if (application == null) throw new ArgumentNullException(nameof(application));
            if (hole == null) throw new ArgumentNullException(nameof(hole));

            Occurrences? occurrences = null;
            Occurrence? occurrence = null;
            Relations3d? relations = null;
            object? axialRelation = null;
            object? planarRelation = null;
            ScrewGeometryFaces? screwFaces = null;
            try
            {
                var assemblyDocument = (AssemblyDocument)application.ActiveDocument;

                occurrences = assemblyDocument.Occurrences;
                occurrence = occurrences.AddByFilename(localParPath, Type.Missing);

                if (!applyConstraints)
                {
                    return; // Anchor: inserción libre, sin restricciones
                }

                screwFaces = _screwGeometryResolver.Resolve(occurrence);

                relations = assemblyDocument.Relations3d;

                axialRelation = relations.AddAxial(screwFaces.AxisFace, hole.CylindricalFace, NormalsAligned: true);

                if (hole.SupportFace != null)
                {
                    Array? constrainingPoint1 = null;
                    Array? constrainingPoint2 = null;
                    planarRelation = relations.AddPlanar(
                        screwFaces.HeadBottomFace, hole.SupportFace, true,
                        ref constrainingPoint1!, ref constrainingPoint2!);
                }
            }
            finally
            {
                if (planarRelation != null && Marshal.IsComObject(planarRelation)) Marshal.ReleaseComObject(planarRelation);
                if (axialRelation != null && Marshal.IsComObject(axialRelation)) Marshal.ReleaseComObject(axialRelation);
                if (screwFaces != null)
                {
                    if (Marshal.IsComObject(screwFaces.AxisFace)) Marshal.ReleaseComObject(screwFaces.AxisFace);
                    if (Marshal.IsComObject(screwFaces.HeadBottomFace)) Marshal.ReleaseComObject(screwFaces.HeadBottomFace);
                }
                if (relations != null) Marshal.ReleaseComObject(relations);
                if (occurrence != null) Marshal.ReleaseComObject(occurrence);
                if (occurrences != null) Marshal.ReleaseComObject(occurrences);
            }
        }

        /// <summary>Ruta del ensamblaje activo, o null si aún no ha sido guardado.</summary>
        public static string? GetActiveAssemblyPath(Application application)
        {
            if (application == null) throw new ArgumentNullException(nameof(application));
            var path = ((AssemblyDocument)application.ActiveDocument).Path;
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
    }
}
