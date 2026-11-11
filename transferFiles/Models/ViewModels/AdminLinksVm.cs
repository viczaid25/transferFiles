namespace transferFiles.Models.ViewModels
{
    /// <summary>
    /// Vista de administración: todos los links del sistema, no solo los propios.
    /// </summary>
    public sealed class AdminLinksVm
    {
        public List<Row> Items { get; set; } = new();

        public int Total { get; set; }
        public int Activos { get; set; }
        public int Expirados { get; set; }
        public int UsuariosDistintos { get; set; }
        public int DepartamentosDistintos { get; set; }

        /// <summary>Departamentos presentes, para el filtro de esa columna.</summary>
        public List<string> Departamentos { get; set; } = new();

        public sealed class Row
        {
            public int Id { get; set; }
            public string Login { get; set; } = "";
            public string? Nombre { get; set; }
            public string? Departamento { get; set; }
            public string FileName { get; set; } = "";
            public string Link { get; set; } = "";
            public string? Reason { get; set; }
            public DateTime CreatedAtUtc { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
            public bool IsExpired { get; set; }

            /// <summary>Lo que se muestra en la columna de usuario cuando no hay nombre (histórico sin resolver).</summary>
            public string NombreOLogin => string.IsNullOrWhiteSpace(Nombre) ? Login : Nombre!;
        }
    }
}
