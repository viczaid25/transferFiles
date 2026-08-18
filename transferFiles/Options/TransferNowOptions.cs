namespace transferFiles.Options
{
    public sealed class TransferNowOptions
    {
        public string ApiKey { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public string? Region { get; set; }
        public int DefaultValidityDays { get; set; } = 7;
        public bool AllowPreview { get; set; } = true;

        /// <summary>
        /// Proxy de salida para llegar a TransferNow (API y URLs de subida).
        /// Vacío = conexión directa. Es necesario donde el servidor no tiene
        /// salida a internet por 443: MEAXS066 la tiene bloqueada y sin esto la
        /// creación del link falla con SocketException 10060 (timeout).
        /// </summary>
        public string? ProxyUrl { get; set; }
    }
}
