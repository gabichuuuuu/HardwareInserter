namespace HardwareInserter.Core.Models
{
    /// <summary>Estado de una fila del grid de staging a lo largo del flujo de inserción.</summary>
    public enum StagingRowState
    {
        /// <summary>Faltan Tipo, Metrica o Longitud; requiere edición manual en el grid.</summary>
        PendienteDatos,

        /// <summary>Tipo, Metrica y Longitud completos y existentes en el catálogo.</summary>
        ListoParaInsertar,

        /// <summary>El tornillo ya fue insertado y restringido en el ensamblaje.</summary>
        Insertado,

        /// <summary>La fila falló en algún paso (catálogo, copia de archivo, inserción o restricción).</summary>
        Error
    }
}
