using System.ComponentModel.DataAnnotations;

namespace MeteoriteLandings.Infrastructure.Configuration
{
    public class NasaApiOptions
    {
        public const string SectionName = "ExternalApis:NasaApi";
        
        [Required]
        [Url]
        public string BaseUrl { get; set; } = string.Empty;
        
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
        
        [Range(0, 10)]
        public int RetryCount { get; set; } = 3;
        
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        
        public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromMinutes(1);
        
        [Range(1, 100)]
        public int CircuitBreakerThreshold { get; set; } = 5;
    }
}
