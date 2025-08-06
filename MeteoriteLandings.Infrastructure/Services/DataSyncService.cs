using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MeteoriteLandings.Infrastructure.Clients;
using MeteoriteLandings.Application.Repositories;
using MeteoriteLandings.Domain.Entities;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeteoriteLandings.Application.Services;
using System.ComponentModel.DataAnnotations;

namespace MeteoriteLandings.Infrastructure.Services
{
    public class DataSyncService : BackgroundService
    {
        private readonly ILogger<DataSyncService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly int _syncIntervalMinutes;
        private readonly SemaphoreSlim _syncSemaphore = new(1, 1);
        private volatile bool _isSyncing = false;

        public DataSyncService(ILogger<DataSyncService> logger, IServiceScopeFactory scopeFactory, int syncIntervalMinutes)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _syncIntervalMinutes = syncIntervalMinutes;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Sync Service started with interval: {intervalMinutes} minutes", _syncIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Check if already syncing
                if (_isSyncing)
                {
                    _logger.LogInformation("Sync already in progress, skipping this cycle");
                    await Task.Delay(TimeSpan.FromMinutes(_syncIntervalMinutes), stoppingToken);
                    continue;
                }

                // Use semaphore to prevent concurrent sync operations
                if (!await _syncSemaphore.WaitAsync(1000, stoppingToken))
                {
                    _logger.LogWarning("Failed to acquire sync lock, skipping this cycle");
                    await Task.Delay(TimeSpan.FromMinutes(_syncIntervalMinutes), stoppingToken);
                    continue;
                }

                try
                {
                    _isSyncing = true;
                    _logger.LogInformation("Starting data sync with NASA API at: {time}", DateTimeOffset.Now);
                    
                    await PerformSyncOperation(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Sync operation cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while syncing data with NASA API");
                }
                finally
                {
                    _isSyncing = false;
                    _syncSemaphore.Release();
                }

                // Wait for next sync cycle
                await Task.Delay(TimeSpan.FromMinutes(_syncIntervalMinutes), stoppingToken);
            }

            _logger.LogInformation("Data Sync Service stopped");
        }

        private async Task PerformSyncOperation(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            
            var nasaApiClient = scope.ServiceProvider.GetRequiredService<NasaApiClient>();
            var meteoriteRepository = scope.ServiceProvider.GetRequiredService<IMeteoriteRepository>();
            var cacheClearer = scope.ServiceProvider.GetRequiredService<ICacheClearer>();

            var nasaData = await nasaApiClient.GetMeteoriteLandingsAsync();

            if (nasaData == null || !nasaData.Any())
            {
                _logger.LogWarning("No data received from NASA API or data is empty");
                return;
            }

            _logger.LogInformation("Retrieved {count} records from NASA API", nasaData.Count());
            
            var existingData = (await meteoriteRepository.GetAllAsync()).ToList();
            var existingDataMap = existingData.ToDictionary(m => m.ExternalId);

            var meteoritesToAdd = new List<MeteoriteLanding>();
            var meteoritesToUpdate = new List<MeteoriteLanding>();
            var validationErrors = 0;

            foreach (var nasaItem in nasaData)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(nasaItem.Id))
                {
                    _logger.LogWarning("NASA data item with no ID found, skipping");
                    continue;
                }

                try
                {
                    if (existingDataMap.TryGetValue(nasaItem.Id, out var existingMeteorite))
                    {
                        UpdateMeteoriteFromNasaData(existingMeteorite, nasaItem);
                        existingMeteorite.UpdatedAt = DateTimeOffset.UtcNow;
                        
                        if (ValidateMeteoriteData(existingMeteorite))
                        {
                            meteoritesToUpdate.Add(existingMeteorite);
                        }
                        else
                        {
                            validationErrors++;
                            _logger.LogWarning("Validation failed for updated meteorite {ExternalId}", nasaItem.Id);
                        }
                    }
                    else
                    {
                        var newMeteorite = CreateMeteoriteFromNasaData(nasaItem);
                        if (newMeteorite != null && ValidateMeteoriteData(newMeteorite))
                        {
                            meteoritesToAdd.Add(newMeteorite);
                        }
                        else
                        {
                            validationErrors++;
                            _logger.LogWarning("Validation failed for new meteorite {ExternalId}", nasaItem.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing meteorite data for {ExternalId}", nasaItem.Id);
                    validationErrors++;
                }
            }

            if (validationErrors > 0)
            {
                _logger.LogWarning("{validationErrors} validation errors occurred during sync", validationErrors);
            }

            // Handle deletions for records no longer in NASA API
            var externalIdsInNasa = new HashSet<string>(nasaData
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .Select(x => x.Id!));

            var meteoritesToDelete = existingData
                .Where(m => !externalIdsInNasa.Contains(m.ExternalId))
                .ToList();

            await ApplyDatabaseChanges(meteoriteRepository, meteoritesToAdd, meteoritesToUpdate, meteoritesToDelete, cacheClearer);
        }

        private MeteoriteLanding? CreateMeteoriteFromNasaData(NasaMeteoriteData nasaItem)
        {
            if (string.IsNullOrWhiteSpace(nasaItem.Id)) return null;

            return new MeteoriteLanding
            {
                Id = Guid.NewGuid(),
                ExternalId = nasaItem.Id,
                Name = nasaItem.Name ?? string.Empty,
                NameType = nasaItem.Nametype ?? string.Empty,
                RecClass = nasaItem.Recclass ?? string.Empty,
                Mass = ParseLong(nasaItem.Mass),
                Fall = nasaItem.Fall ?? string.Empty,
                Year = ParseYear(nasaItem.Year),
                Reclat = ParseDouble(nasaItem.Reclat),
                Reclong = ParseDouble(nasaItem.Reclong),
                GeoLocation = nasaItem.GeoLocation != null ? System.Text.Json.JsonSerializer.Serialize(nasaItem.GeoLocation) : string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        private void UpdateMeteoriteFromNasaData(MeteoriteLanding existingMeteorite, NasaMeteoriteData nasaItem)
        {
            existingMeteorite.Name = nasaItem.Name ?? string.Empty;
            existingMeteorite.NameType = nasaItem.Nametype ?? string.Empty;
            existingMeteorite.RecClass = nasaItem.Recclass ?? string.Empty;
            existingMeteorite.Mass = ParseLong(nasaItem.Mass);
            existingMeteorite.Fall = nasaItem.Fall ?? string.Empty;
            existingMeteorite.Year = ParseYear(nasaItem.Year);
            existingMeteorite.Reclat = ParseDouble(nasaItem.Reclat);
            existingMeteorite.Reclong = ParseDouble(nasaItem.Reclong);
            existingMeteorite.GeoLocation = nasaItem.GeoLocation != null ? System.Text.Json.JsonSerializer.Serialize(nasaItem.GeoLocation) : string.Empty;
        }

        private static int? ParseYear(string? yearString)
        {
            if (string.IsNullOrWhiteSpace(yearString)) return null;
            if (DateTime.TryParse(yearString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date.Year;
            }
            if (int.TryParse(yearString, out var year))
            {
                return year;
            }
            return null;
        }

        private static long? ParseLong(string? longString)
        {
            if (string.IsNullOrWhiteSpace(longString)) return null;
            if (long.TryParse(longString, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            return null;
        }

        private static double? ParseDouble(string? doubleString)
        {
            if (string.IsNullOrWhiteSpace(doubleString)) return null;
            if (double.TryParse(doubleString, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }
            return null;
        }

        private static bool ValidateMeteoriteData(MeteoriteLanding meteorite)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(meteorite.ExternalId))
                return false;

            // Validate name length
            if (meteorite.Name.Length > 500)
                return false;

            // Validate coordinates if present
            if (meteorite.Reclat.HasValue && (meteorite.Reclat < -90 || meteorite.Reclat > 90))
                return false;

            if (meteorite.Reclong.HasValue && (meteorite.Reclong < -180 || meteorite.Reclong > 180))
                return false;

            // Validate year if present
            if (meteorite.Year.HasValue && (meteorite.Year < 1 || meteorite.Year > DateTime.Now.Year + 1))
                return false;

            // Validate mass if present (can't be negative)
            if (meteorite.Mass.HasValue && meteorite.Mass < 0)
                return false;

            return true;
        }

        private async Task ApplyDatabaseChanges(
            IMeteoriteRepository repository,
            List<MeteoriteLanding> meteoritesToAdd,
            List<MeteoriteLanding> meteoritesToUpdate,
            List<MeteoriteLanding> meteoritesToDelete,
            ICacheClearer cacheClearer)
        {
            bool changesMade = false;

            if (meteoritesToAdd.Any())
            {
                try
                {
                    await repository.AddRangeAsync(meteoritesToAdd);
                    _logger.LogInformation("Prepared to add {count} new meteorite landings", meteoritesToAdd.Count);
                    changesMade = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding new meteorite landings");
                    throw;
                }
            }

            if (meteoritesToUpdate.Any())
            {
                try
                {
                    await repository.UpdateRangeAsync(meteoritesToUpdate);
                    _logger.LogInformation("Prepared to update {count} meteorite landings", meteoritesToUpdate.Count);
                    changesMade = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating meteorite landings");
                    throw;
                }
            }

            if (meteoritesToDelete.Any())
            {
                try
                {
                    await repository.DeleteRangeAsync(meteoritesToDelete);
                    _logger.LogInformation("Prepared to delete {count} meteorite landings", meteoritesToDelete.Count);
                    changesMade = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting meteorite landings");
                    throw;
                }
            }

            if (changesMade)
            {
                try
                {
                    await repository.SaveChangesAsync();
                    _logger.LogInformation("Data synchronization completed successfully. Changes saved to database");
                    
                    // Clear cache after successful database update
                    cacheClearer.ClearCache();
                    _logger.LogInformation("Cache cleared after successful sync");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving changes to database");
                    throw;
                }
            }
            else
            {
                _logger.LogInformation("No changes detected. Database is up to date");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _syncSemaphore?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
