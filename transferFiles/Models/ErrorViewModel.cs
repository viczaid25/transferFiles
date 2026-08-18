namespace transferFiles.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        /// <summary>Ruta que falló (para poder correlacionar con el log).</summary>
        public string? Path { get; set; }

        /// <summary>
        /// Detalle de la excepción. Solo se llena si Diagnostics:ShowDetailedErrors
        /// es true — pensado para diagnosticar en el servidor sin tener que leer
        /// archivos de log a mano. Apagado por defecto.
        /// </summary>
        public string? Detail { get; set; }

        public bool ShowDetail => !string.IsNullOrEmpty(Detail);

        /// <summary>Dónde quedó escrito el log, para poder ir a buscarlo.</summary>
        public string? LogPath { get; set; }
    }
}
