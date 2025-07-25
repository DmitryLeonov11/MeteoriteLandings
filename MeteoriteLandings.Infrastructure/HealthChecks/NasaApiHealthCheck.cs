using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MeteoriteLandings.Infrastructure.Configuration;
using System.Diagnostics;

namespace MeteoriteLandings.Infrastructure.HealthChecks
{
    public class NasaApiHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NasaApiHealthCheck> _logger;
        private readonly NasaApiOptions _options;

        public NasaApiHealthCheck(HttpClient httpClient, ILogger<NasaApiHealthCheck> logger, IOptions<NasaApiOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
            
            // Configure timeout for health check
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Shorter timeout for health checks
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogDebug("Checking NASA API health at: {BaseUrl}", _options.BaseUrl);
                
                using var response = await _httpClient.GetAsync(_options.BaseUrl, cancellationToken);
                
                stopwatch.Stop();
                
                if (response.IsSuccessStatusCode)
                {
                    var responseTime = stopwatch.ElapsedMilliseconds;
                    var data = new Dictionary<string, object>
                    {
                        ["url"] = _options.BaseUrl,
                        ["status_code"] = (int)response.StatusCode,
                        ["response_time_ms"] = responseTime,
                        ["content_length"] = response.Content.Headers.ContentLength ?? 0
                    };

                    var status = responseTime > 5000 ? HealthStatus.Degraded : HealthStatus.Healthy;
                    var description = status == HealthStatus.Degraded 
                        ? $"NASA API is slow (${responseTime}ms)" 
                        : $"NASA API is healthy ({responseTime}ms)";

                    return HealthCheckResult.Healthy(description, data);
                }
                else
                {
                    var data = new Dictionary<string, object>
                    {
                        ["url"] = _options.BaseUrl,
                        ["status_code"] = (int)response.StatusCode,
                        ["response_time_ms"] = stopwatch.ElapsedMilliseconds,
                        ["reason_phrase"] = response.ReasonPhrase ?? "Unknown"
                    };

                    return HealthCheckResult.Unhealthy($"NASA API returned {response.StatusCode}: {response.ReasonPhrase}", data: data);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                
                var data = new Dictionary<string, object>
                {
                    ["url"] = _options.BaseUrl,
                    ["timeout_ms"] = _httpClient.Timeout.TotalMilliseconds,
                    ["elapsed_ms"] = stopwatch.ElapsedMilliseconds
                };

                _logger.LogWarning("NASA API health check timed out after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return HealthCheckResult.Unhealthy("NASA API request timed out", ex, data);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                
                var data = new Dictionary<string, object>
                {
                    ["url"] = _options.BaseUrl,
                    ["elapsed_ms"] = stopwatch.ElapsedMilliseconds,
                    ["error"] = ex.Message
                };

                _logger.LogWarning(ex, "NASA API health check failed due to HTTP error");
                return HealthCheckResult.Unhealthy("NASA API is unreachable", ex, data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                var data = new Dictionary<string, object>
                {
                    ["url"] = _options.BaseUrl,
                    ["elapsed_ms"] = stopwatch.ElapsedMilliseconds,
                    ["error"] = ex.Message,
                    ["error_type"] = ex.GetType().Name
                };

                _logger.LogError(ex, "NASA API health check failed with unexpected error");
                return HealthCheckResult.Unhealthy("NASA API health check failed", ex, data);
            }
        }
    }
}
