using AutoMapper;
using MeteoriteLandings.Application.DTOs;
using MeteoriteLandings.Application.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System;
using System.Security.Cryptography;
using System.Text;

namespace MeteoriteLandings.Application.Services
{
    public class MeteoriteService : IMeteoriteService, ICacheClearer
    {
        private readonly IMeteoriteRepository _meteoriteRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MeteoriteService> _logger;
        private const string CacheKeyPrefix = "MeteoriteLandings_";
        private const string UniqueRecClassesCacheKey = CacheKeyPrefix + "UniqueRecClasses";

        public MeteoriteService(IMeteoriteRepository meteoriteRepository, IMapper mapper, IMemoryCache cache, ILogger<MeteoriteService> logger)
        {
            _meteoriteRepository = meteoriteRepository;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<IEnumerable<MeteoriteLandingGroupedByYearDto>> GetFilteredAndGroupedLandingsAsync(
            MeteoriteLandingFilterDto filter)
        {
            var cacheKey = GenerateCacheKey(filter);

            if (_cache.TryGetValue(cacheKey, out IEnumerable<MeteoriteLandingGroupedByYearDto>? cachedResult))
            {
                _logger.LogInformation("Cache hit for filter query");
                return cachedResult!;
            }

            var filteredLandings = await _meteoriteRepository.GetFilteredAsync(filter);

            var query = filteredLandings.AsQueryable();

            var groupedData = query
                .GroupBy(m => m.Year!.Value)
                .Select(g => new MeteoriteLandingGroupedByYearDto
                {
                    Year = g.Key,
                    Count = g.Count(),
                    TotalMass = g.Sum(m => m.Mass ?? 0)
                });

            switch (filter.SortBy?.ToLower())
            {
                case "year":
                    groupedData = (filter.SortOrder?.ToLower() == "desc")
                        ? groupedData.OrderByDescending(g => g.Year)
                        : groupedData.OrderBy(g => g.Year);
                    break;
                case "count":
                    groupedData = (filter.SortOrder?.ToLower() == "desc")
                        ? groupedData.OrderByDescending(g => g.Count)
                        : groupedData.OrderBy(g => g.Count);
                    break;
                case "mass":
                case "totalmass":
                    groupedData = (filter.SortOrder?.ToLower() == "desc")
                        ? groupedData.OrderByDescending(g => g.TotalMass)
                        : groupedData.OrderBy(g => g.TotalMass);
                    break;
                default:
                    groupedData = groupedData.OrderBy(g => g.Year);
                    break;
            }

            var result = groupedData.ToList();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(1));
            _cache.Set(cacheKey, result, cacheEntryOptions);
            _logger.LogInformation("Caching data for key: {CacheKey}", cacheKey);

            return result;
        }

        public Task<List<string>> ValidateFilterAsync(MeteoriteLandingFilterDto filter)
        {
            var errors = new List<string>();

            if (filter.StartYear.HasValue && filter.EndYear.HasValue && filter.StartYear > filter.EndYear)
            {
                errors.Add("StartYear cannot be greater than EndYear.");
            }

            var allowedSortByValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "year", "count", "totalmass", "mass"
            };

            if (!string.IsNullOrWhiteSpace(filter.SortBy) && !allowedSortByValues.Contains(filter.SortBy))
            {
                errors.Add($"Invalid SortBy value: '{filter.SortBy}'. Allowed values are: {string.Join(", ", allowedSortByValues)}.");
            }

            return Task.FromResult(errors);
        }

        public void ClearCache()
        {
            try
            {
                if (_cache is MemoryCache memoryCache)
                {
                    memoryCache.Compact(1.0);
                    _logger.LogInformation("Memory cache cleared successfully");
                }
                else
                {
                    _logger.LogWarning("Cache is not MemoryCache type, cannot clear");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while clearing cache");
            }
        }

        public async Task<IEnumerable<string>> GetUniqueRecClassesAsync()
        {
            if (_cache.TryGetValue(UniqueRecClassesCacheKey, out IEnumerable<string> cachedClasses))
            {
                _logger.LogInformation("Cache hit for unique rec classes");
                return cachedClasses;
            }

            _logger.LogInformation("Cache miss for unique rec classes, fetching from database");
            var uniqueClasses = await _meteoriteRepository.GetDistinctRecClassesAsync();

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(UniqueRecClassesCacheKey, uniqueClasses, cacheOptions);

            return uniqueClasses;
        }

        private static string GenerateCacheKey(MeteoriteLandingFilterDto filter)
        {
            // Create a deterministic string representation of the filter
            var keyBuilder = new StringBuilder();
            keyBuilder.Append($"start:{filter.StartYear?.ToString() ?? "null"}");
            keyBuilder.Append($"|end:{filter.EndYear?.ToString() ?? "null"}");
            keyBuilder.Append($"|class:{filter.RecClass?.Trim() ?? "null"}");
            keyBuilder.Append($"|name:{filter.NameContains?.Trim() ?? "null"}");
            keyBuilder.Append($"|sort:{filter.SortBy?.ToLowerInvariant() ?? "null"}");
            keyBuilder.Append($"|order:{filter.SortOrder?.ToLowerInvariant() ?? "null"}");

            var keyString = keyBuilder.ToString();
            
            // Generate SHA256 hash for consistent, shorter cache keys
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            var hashString = Convert.ToBase64String(hashBytes).Replace("/", "_").Replace("+", "-").TrimEnd('=');
            
            return $"{CacheKeyPrefix}{hashString}";
        }
    }
}
