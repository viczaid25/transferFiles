namespace transferFiles.Options
{
    public sealed class TransferNowOptions
    {
        public string ApiKey { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public string? Region { get; set; }
        public int DefaultValidityDays { get; set; } = 7;
        public bool AllowPreview { get; set; } = true;
    }
}
