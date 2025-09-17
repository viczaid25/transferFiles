namespace transferFiles.Models.ViewModels
{
    public sealed class TransferLinkRowVm
    {
        public int Id { get; set; }
        public string WindowsUser { get; set; } = "";
        public string Link { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public string FileName { get; set; } = "";
        public bool IsExpired { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public TimeSpan Remaining => IsExpired ? TimeSpan.Zero : (ExpiresAtUtc - DateTime.UtcNow);
        public string Status => IsExpired ? "Expired" : "Active";
    }
}
