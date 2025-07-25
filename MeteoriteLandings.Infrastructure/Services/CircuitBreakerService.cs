using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MeteoriteLandings.Infrastructure.Services
{
    public enum CircuitBreakerState
    {
        Closed,    // Circuit is closed, requests flow normally
        Open,      // Circuit is open, all requests fail immediately
        HalfOpen   // Circuit is half-open, allowing limited requests to test if service recovered
    }

    public class CircuitBreakerService
    {
        private readonly ILogger<CircuitBreakerService> _logger;
        private readonly ConcurrentDictionary<string, CircuitBreakerInstance> _circuitBreakers;

        public CircuitBreakerService(ILogger<CircuitBreakerService> logger)
        {
            _logger = logger;
            _circuitBreakers = new ConcurrentDictionary<string, CircuitBreakerInstance>();
        }

        public async Task<T?> ExecuteAsync<T>(
            string circuitName,
            Func<Task<T?>> operation,
            int failureThreshold = 5,
            TimeSpan circuitOpenDuration = default,
            string operationName = "Operation")
        {
            if (circuitOpenDuration == default)
                circuitOpenDuration = TimeSpan.FromMinutes(1);

            var circuitBreaker = _circuitBreakers.GetOrAdd(circuitName, 
                _ => new CircuitBreakerInstance(failureThreshold, circuitOpenDuration, _logger));

            return await circuitBreaker.ExecuteAsync(operation, operationName);
        }

        public CircuitBreakerState GetCircuitState(string circuitName)
        {
            return _circuitBreakers.TryGetValue(circuitName, out var circuit) 
                ? circuit.State 
                : CircuitBreakerState.Closed;
        }

        public void ResetCircuit(string circuitName)
        {
            if (_circuitBreakers.TryGetValue(circuitName, out var circuit))
            {
                circuit.Reset();
                _logger.LogInformation("Circuit breaker '{CircuitName}' has been manually reset", circuitName);
            }
        }
    }

    internal class CircuitBreakerInstance
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _circuitOpenDuration;
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        private int _failureCount;
        private DateTime _lastFailureTime;
        private CircuitBreakerState _state;

        public CircuitBreakerState State 
        { 
            get 
            { 
                lock (_lock) 
                { 
                    return _state; 
                } 
            } 
        }

        public CircuitBreakerInstance(int failureThreshold, TimeSpan circuitOpenDuration, ILogger logger)
        {
            _failureThreshold = failureThreshold;
            _circuitOpenDuration = circuitOpenDuration;
            _logger = logger;
            _state = CircuitBreakerState.Closed;
        }

        public async Task<T?> ExecuteAsync<T>(Func<Task<T?>> operation, string operationName)
        {
            lock (_lock)
            {
                if (_state == CircuitBreakerState.Open)
                {
                    if (DateTime.UtcNow - _lastFailureTime < _circuitOpenDuration)
                    {
                        _logger.LogWarning("Circuit breaker is OPEN for {OperationName}. Request rejected immediately", operationName);
                        throw new CircuitBreakerOpenException($"Circuit breaker is open for {operationName}");
                    }
                    else
                    {
                        _logger.LogInformation("Circuit breaker transitioning to HALF-OPEN for {OperationName}", operationName);
                        _state = CircuitBreakerState.HalfOpen;
                    }
                }
            }

            try
            {
                var result = await operation();
                
                OnSuccess(operationName);
                return result;
            }
            catch (Exception ex)
            {
                OnFailure(ex, operationName);
                throw;
            }
        }

        private void OnSuccess(string operationName)
        {
            lock (_lock)
            {
                var previousState = _state;
                _failureCount = 0;
                _state = CircuitBreakerState.Closed;

                if (previousState != CircuitBreakerState.Closed)
                {
                    _logger.LogInformation("Circuit breaker CLOSED for {OperationName} after successful request", operationName);
                }
            }
        }

        private void OnFailure(Exception exception, string operationName)
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                _logger.LogWarning(exception, "Circuit breaker failure {FailureCount}/{Threshold} for {OperationName}", 
                    _failureCount, _failureThreshold, operationName);

                if (_failureCount >= _failureThreshold)
                {
                    _state = CircuitBreakerState.Open;
                    _logger.LogError("Circuit breaker OPENED for {OperationName} after {FailureCount} failures. Will remain open for {Duration}", 
                        operationName, _failureCount, _circuitOpenDuration);
                }
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _failureCount = 0;
                _state = CircuitBreakerState.Closed;
            }
        }
    }

    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException) { }
    }
}
