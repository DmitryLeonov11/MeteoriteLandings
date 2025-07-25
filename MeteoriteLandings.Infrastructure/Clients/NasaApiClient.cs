using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MeteoriteLandings.Infrastructure.Configuration;
using MeteoriteLandings.Infrastructure.Services;

namespace MeteoriteLandings.Infrastructure.Clients
{
    public class NasaMeteoriteData
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
        public string? Nametype { get; set; }
        public string? Recclass { get; set; }
        public string? Fall { get; set; }
        public string? Year { get; set; }
        public string? Reclat { get; set; }
        public string? Reclong { get; set; }
        public string? Mass { get; set; }
        public GeolocationData? GeoLocation { get; set; }
    }

    public class GeolocationData
    {
        public string? Type { get; set; }
        public double[]? Coordinates { get; set; }
    }


    public class NasaApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NasaApiClient> _logger;
        private readonly NasaApiOptions _options;
        private readonly RetryPolicyService _retryPolicy;
        private readonly CircuitBreakerService _circuitBreaker;

        public NasaApiClient(HttpClient httpClient, ILogger<NasaApiClient> logger, IOptions<NasaApiOptions> options, RetryPolicyService retryPolicy, CircuitBreakerService circuitBreaker)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
            _retryPolicy = retryPolicy;
            _circuitBreaker = circuitBreaker;
            
            // Configure HttpClient timeout
            _httpClient.Timeout = _options.Timeout;
        }

        public async Task<IEnumerable<NasaMeteoriteData>?> GetMeteoriteLandingsAsync()
        {
            return await _circuitBreaker.ExecuteAsync(
                circuitName: "nasa-api",
                operation: async () => await _retryPolicy.ExecuteWithRetryAsync(
                    operation: async () => await FetchMeteoriteDataAsync(),
                    maxRetries: _options.RetryCount,
                    delay: _options.RetryDelay,
                    operationName: "NASA API call"
                ),
                failureThreshold: _options.CircuitBreakerThreshold,
                circuitOpenDuration: _options.CircuitBreakerDuration,
                operationName: "NASA API with Circuit Breaker"
            );
        }

        private async Task<IEnumerable<NasaMeteoriteData>?> FetchMeteoriteDataAsync()
        {
            _logger.LogDebug("Calling NASA API at: {BaseUrl}", _options.BaseUrl);
            
            var response = await _httpClient.GetAsync(_options.BaseUrl);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                _logger.LogWarning("Received empty response from NASA API");
                return null;
            }

            var serializerOptions = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };

            try
            {
                var data = JsonSerializer.Deserialize<IEnumerable<NasaMeteoriteData>>(jsonString, serializerOptions);
                
                _logger.LogInformation("Successfully deserialized {Count} meteorite records from NASA API", 
                    data?.Count() ?? 0);
                    
                return data;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize JSON response from NASA API. Response length: {Length}", 
                    jsonString.Length);
                throw;
            }
        }
    }
}
