namespace MeteoriteLandings.Infrastructure.Configuration
{
    public class NasaApiOptions
    {
        public const string SectionName = "ExternalApis:NasaApi";
        
        public string BaseUrl { get; set; } = string.Empty;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        public int RetryCount { get; set; } = 3;
    }
}
