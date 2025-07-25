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

namespace MeteoriteLandings.Infrastructure.Services
{
    public class DataSyncService : BackgroundService
    {
        private readonly ILogger<DataSyncService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly int _syncIntervalMinutes;

        public DataSyncService(ILogger<DataSyncService> logger, IServiceScopeFactory scopeFactory, int syncIntervalMinutes)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _syncIntervalMinutes = syncIntervalMinutes;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Sync Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Attempting to sync data with NASA API at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var nasaApiClient = scope.ServiceProvider.GetRequiredService<NasaApiClient>();
                        var meteoriteRepository = scope.ServiceProvider.GetRequiredService<IMeteoriteRepository>();
                        var cacheClearer = scope.ServiceProvider.GetRequiredService<ICacheClearer>();

                        var nasaData = await nasaApiClient.GetMeteoriteLandingsAsync();

                        if (nasaData != null && nasaData.Any())
                        {
                            var existingData = (await meteoriteRepository.GetAllAsync()).ToList();
                            var existingDataMap = existingData.ToDictionary(m => m.ExternalId);

                            var meteoritesToAdd = new List<MeteoriteLanding>();
                            var meteoritesToUpdate = new List<MeteoriteLanding>();
                            var existingExternalIds = new HashSet<string>(existingData.Select(m => m.ExternalId));

                            foreach (var nasaItem in nasaData)
                            {
                                if (string.IsNullOrWhiteSpace(nasaItem.Id))
                                {
                                    _logger.LogWarning("NASA data item with no ID found. Skipping.");
                                    continue;
                                }

                                if (existingDataMap.TryGetValue(nasaItem.Id, out var existingMeteorite))
                                {
                                    UpdateMeteoriteFromNasaData(existingMeteorite, nasaItem);
                                    existingMeteorite.UpdatedAt = DateTimeOffset.UtcNow;
                                    meteoritesToUpdate.Add(existingMeteorite);
                                }
                                else
                                {
                                    var newMeteorite = CreateMeteoriteFromNasaData(nasaItem);
                                    if (newMeteorite != null)
                                    {
                                        meteoritesToAdd.Add(newMeteorite);
                                    }
                                }
                            }

                            var externalIdsInNasa = new HashSet<string>(nasaData
                                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                                .Select(x => x.Id!));

                            var meteoritesToDelete = existingData
                                .Where(m => !externalIdsInNasa.Contains(m.ExternalId))
                                .ToList();

                            bool changesMade = false;

                            if (meteoritesToAdd.Any())
                            {
                                await meteoriteRepository.AddRangeAsync(meteoritesToAdd);
                                _logger.LogInformation("Added {count} new meteorite landings.", meteoritesToAdd.Count);
                                changesMade = true;
                            }
                            if (meteoritesToUpdate.Any())
                            {
                                await meteoriteRepository.UpdateRangeAsync(meteoritesToUpdate);
                                _logger.LogInformation("Updated {count} meteorite landings.", meteoritesToUpdate.Count);
                                changesMade = true;
                            }
                            if (meteoritesToDelete.Any())
                            {
                                await meteoriteRepository.DeleteRangeAsync(meteoritesToDelete);
                                _logger.LogInformation("Deleted {count} meteorite landings.", meteoritesToDelete.Count);
                                changesMade = true;
                            }

                            if (changesMade)
                            {
                                await meteoriteRepository.SaveChangesAsync();
                                _logger.LogInformation("Data synchronization completed. Changes saved to database.");
                                cacheClearer.ClearCache();
                            }
                            else
                            {
                                _logger.LogInformation("No changes detected. Database is up to date.");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("No data received from NASA API or data is empty.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while syncing data with NASA API.");
                }
                var syncInterval = TimeSpan.FromMinutes(_syncIntervalMinutes);

                await Task.Delay(syncInterval, stoppingToken);
            }

            _logger.LogInformation("Data Sync Service stopped.");
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
    }
}
