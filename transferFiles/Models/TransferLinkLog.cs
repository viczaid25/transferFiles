namespace transferFiles.Models
{
    public class TransferLinkLog
    {
        public int Id { get; set; }
        public string WindowsUser { get; set; } = "";   // DOMINIO\usuario
        public string Link { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public string FileName { get; set; } = "";      // si subes varios, guarda el principal o concatena
        public string Status { get; set; } = "Created"; // Created | Expired | Deleted | etc.
        public string? Password { get; set; }
        public string? Reason { get; set; }

    }
}
