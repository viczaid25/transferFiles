namespace transferFiles.Models.ViewModels
{
    public sealed class HomeIndexVm
    {
        public string CurrentUser { get; set; } = "";
        public int ValidityDays { get; set; } = 7;

        // Stats simples
        public int TotalMyLinks { get; set; }
        public int ActiveMyLinks { get; set; }
        public int ExpiredMyLinks { get; set; }

        // Últimos 5
        public List<LinkRow> Recent { get; set; } = new();

        public sealed class LinkRow
        {
            public int Id { get; set; }
            public string FileName { get; set; } = "";
            public string Link { get; set; } = "";
            public DateTime CreatedAtUtc { get; set; }
            public DateTime ExpiresAtUtc { get; set; }
            public bool IsExpired { get; set; }
        }
    }
}
