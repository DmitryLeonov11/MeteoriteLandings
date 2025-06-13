using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
        private const string BaseUrl = "https://raw.githubusercontent.com/biggiko/nasa-dataset/refs/heads/main/y77d-th95.json";

        public NasaApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<NasaMeteoriteData>?> GetMeteoriteLandingsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(BaseUrl);

                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var data = JsonSerializer.Deserialize<IEnumerable<NasaMeteoriteData>>(jsonString, options);

                return data;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Ошибка при запросе к NASA API: {e.Message}");
                return null;
            }
            catch (JsonException e)
            {
                Console.WriteLine($"Ошибка при десериализации JSON из NASA API: {e.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Неожиданная ошибка при получении данных из NASA API: {e.Message}");
                return null;
            }
        }
    }
}
