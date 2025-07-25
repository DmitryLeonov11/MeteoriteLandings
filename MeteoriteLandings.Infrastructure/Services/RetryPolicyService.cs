using Microsoft.Extensions.Logging;
using System.Net;

namespace MeteoriteLandings.Infrastructure.Services
{
    public class RetryPolicyService
    {
        private readonly ILogger<RetryPolicyService> _logger;

        public RetryPolicyService(ILogger<RetryPolicyService> logger)
        {
            _logger = logger;
        }

        public async Task<T?> ExecuteWithRetryAsync<T>(
            Func<Task<T?>> operation,
            int maxRetries = 3,
            TimeSpan delay = default,
            string operationName = "Operation")
        {
            if (delay == default)
                delay = TimeSpan.FromSeconds(1);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("Executing {OperationName}, attempt {Attempt}/{MaxRetries}", 
                        operationName, attempt, maxRetries);
                        
                    var result = await operation();
                    
                    if (attempt > 1)
                    {
                        _logger.LogInformation("{OperationName} succeeded on attempt {Attempt}", 
                            operationName, attempt);
                    }
                    
                    return result;
                }
                catch (HttpRequestException ex) when (IsRetriableHttpException(ex) && attempt < maxRetries)
                {
                    _logger.LogWarning(ex, 
                        "{OperationName} failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms", 
                        operationName, attempt, maxRetries, delay.TotalMilliseconds);
                        
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5); // Exponential backoff
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException && attempt < maxRetries)
                {
                    _logger.LogWarning(ex, 
                        "{OperationName} timed out on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms", 
                        operationName, attempt, maxRetries, delay.TotalMilliseconds);
                        
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5);
                }
                catch (Exception ex) when (attempt == maxRetries)
                {
                    _logger.LogError(ex, 
                        "{OperationName} failed permanently after {MaxRetries} attempts", 
                        operationName, maxRetries);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "{OperationName} failed on attempt {Attempt}/{MaxRetries} with non-retriable error. Retrying in {Delay}ms", 
                        operationName, attempt, maxRetries, delay.TotalMilliseconds);
                        
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5);
                }
            }

            return default;
        }

        private static bool IsRetriableHttpException(HttpRequestException ex)
        {
            // Check if the exception indicates a retriable condition
            var message = ex.Message.ToLowerInvariant();
            
            return message.Contains("timeout") ||
                   message.Contains("connection") ||
                   message.Contains("network") ||
                   message.Contains("502") ||  // Bad Gateway
                   message.Contains("503") ||  // Service Unavailable
                   message.Contains("504");    // Gateway Timeout
        }
    }
}
