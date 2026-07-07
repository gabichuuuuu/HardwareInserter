using System;
using System.Runtime.InteropServices;
using SolidEdgeAssembly;
using SolidEdgeFramework;
using SolidEdgeGeometry;

namespace HardwareInserter.SolidEdge
{
    /// <summary>
    /// Envuelve todas las llamadas a la API de Solid Edge: captura de geometría del agujero,
    /// inserción del tornillo como occurrence y restricción axial/planar contra el agujero.
    /// Todo objeto COM obtenido explícitamente se libera con Marshal.ReleaseComObject en finally.
    /// </summary>
    public sealed class SolidEdgeHelper
    {
        private readonly SolidEdgeSession _session;
        private readonly IScrewGeometryResolver _screwGeometryResolver;

        public SolidEdgeHelper(SolidEdgeSession session, IScrewGeometryResolver screwGeometryResolver)
        {
            _session = session;
            _screwGeometryResolver = screwGeometryResolver;
        }

        /// <summary>
        /// Lee el SelectSet activo del ensamblaje. Se espera que el usuario haya seleccionado (con
        /// Ctrl+click en Solid Edge) la cara cilíndrica interior del agujero y, opcionalmente, la
        /// cara plana de apoyo de la cabeza del tornillo, antes de pulsar "Seleccionar Agujero".
        /// RIESGO no verificado sin Solid Edge real: las longitudes de la API vienen en el sistema de
        /// unidades interno del documento (habitualmente metros); aquí se asume metros y se convierte
        /// a mm — validar contra un documento real antes de confiar en el filtrado del grid.
        /// </summary>
        public CapturedHoleGeometry CaptureSelectedHoleGeometry()
        {
            return _session.Execute(app =>
            {
                SelectSet? selectSet = null;
                Face? cylindricalFace = null;
                Face? supportFace = null;
                var retained = false;
                try
                {
                    selectSet = app.ActiveSelectSet;
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
            });
        }

        /// <summary>
        /// Inserta el tornillo (.par ya copiado localmente) como occurrence del ensamblaje activo y
        /// lo restringe: eje del vástago contra la cara cilíndrica del agujero (AddAxial), y cara
        /// inferior de la cabeza contra la cara de apoyo del agujero (AddPlanar, si fue capturada).
        /// </summary>
        public void InsertOccurrenceAndConstrain(string localParPath, CapturedHoleGeometry hole)
        {
            _session.Execute<object?>(app =>
            {
                var assemblyDocument = (AssemblyDocument)app.ActiveDocument;

                Occurrences? occurrences = null;
                Occurrence? occurrence = null;
                Relations3d? relations = null;
                object? axialRelation = null;
                object? planarRelation = null;
                ScrewGeometryFaces? screwFaces = null;
                try
                {
                    occurrences = assemblyDocument.Occurrences;
                    occurrence = occurrences.AddByFilename(localParPath, Type.Missing);

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

                    return null;
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
            });
        }

        /// <summary>Ruta del ensamblaje activo, o null si aún no ha sido guardado.</summary>
        public string? GetActiveAssemblyPath() =>
            _session.Execute(app =>
            {
                var path = ((AssemblyDocument)app.ActiveDocument).Path;
                return string.IsNullOrWhiteSpace(path) ? null : path;
            });
    }
}
