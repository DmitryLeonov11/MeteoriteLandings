using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MeteoriteLandings.Infrastructure.Configuration;

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

        public NasaApiClient(HttpClient httpClient, ILogger<NasaApiClient> logger, IOptions<NasaApiOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<IEnumerable<NasaMeteoriteData>?> GetMeteoriteLandingsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_options.BaseUrl);

                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var data = JsonSerializer.Deserialize<IEnumerable<NasaMeteoriteData>>(jsonString, options);

                return data;
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "HTTP request error when calling NASA API");
                return null;
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "JSON deserialization error when processing NASA API response");
                return null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error when fetching data from NASA API");
                return null;
            }
        }
    }
}
