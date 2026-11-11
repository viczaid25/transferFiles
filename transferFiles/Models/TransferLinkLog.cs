namespace transferFiles.Models
{
    /// <summary>
    /// Link de transferencia generado por un usuario.
    /// Se mapea a it.transferfiles_links en MeaxDB (ver AppDbContext): los
    /// nombres de columna siguen el estándar snake_case de la base, y las
    /// propiedades se quedan en PascalCase como es costumbre en C#.
    /// </summary>
    public class TransferLinkLog
    {
        /// <summary>PK — columna link_id.</summary>
        public int LinkId { get; set; }

        /// <summary>
        /// Login de red de quien generó el link (claim PcLoginId del hub).
        /// Antes se llamaba WindowsUser y guardaba "DOMINIO\usuario"; al migrar
        /// se normalizó para que todo el histórico quede sin el prefijo.
        /// </summary>
        public string CreatedByLogin { get; set; } = "";

        /// <summary>Nombre para mostrar (claim DisplayName). Null en filas anteriores a la migración que no se pudieron resolver.</summary>
        public string? CreatedByName { get; set; }

        /// <summary>Departamento (claim Department). Null en filas que no se pudieron resolver.</summary>
        public string? CreatedByDepartment { get; set; }

        public string Link { get; set; } = "";

        /// <summary>Si se suben varios archivos, se guarda el principal.</summary>
        public string FileName { get; set; } = "";

        /// <summary>Created | Expired | Deleted | etc.</summary>
        public string Status { get; set; } = "Created";

        public string? Password { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// Vigencia real del link. Antes se calculaba al vuelo como
        /// CreatedAtUtc + DefaultValidityDays; ahora se persiste, para que un
        /// cambio de vigencia no recorra el histórico.
        /// </summary>
        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAtUtc { get; set; }
    }
}
