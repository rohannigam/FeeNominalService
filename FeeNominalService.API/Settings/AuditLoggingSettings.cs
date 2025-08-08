namespace FeeNominalService.Settings
{
    public class AuditLoggingSettings
    {
        public bool Enabled { get; set; } = true;
        public Dictionary<string, bool> Endpoints { get; set; } = new();
    }
} 