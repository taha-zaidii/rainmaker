namespace Digi.Shared.SharedLibrary.Options
{
    public class FileStorageSettings
    {
        public string? RootPath { get; set; }
        public string? BaseUrl { get; set; }
        public int TempExpirationHours { get; set; } = 24;
        public bool IsCentralized { get; set; } = true;
    }
}

