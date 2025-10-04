namespace PortalAcademico.Services
{
    public class RedisConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int    Port { get; set; }
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int    CacheTtlMinutes { get; set; } = 1; // 60s
    }
}
