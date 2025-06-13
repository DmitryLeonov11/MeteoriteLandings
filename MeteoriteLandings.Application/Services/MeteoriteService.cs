using AutoMapper;
using MeteoriteLandings.Application.DTOs;
using MeteoriteLandings.Application.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System;
using Microsoft.Extensions.Caching.Memory;

namespace MeteoriteLandings.Application.Services
{
    public class MeteoriteService : IMeteoriteService, ICacheClearer
    {
        private readonly IMeteoriteRepository _meteoriteRepository;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "MeteoriteLandings_";
        private const string UniqueRecClassesCacheKey = CacheKeyPrefix + "UniqueRecClasses";

        public MeteoriteService(IMeteoriteRepository meteoriteRepository, IMapper mapper, IMemoryCache cache)
        {
            _meteoriteRepository = meteoriteRepository;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<IEnumerable<MeteoriteLandingGroupedByYearDto>> GetFilteredAndGroupedLandingsAsync(
            MeteoriteLandingFilterDto filter)
        {
            var cacheKey = $"MeteoriteLandings_{JsonSerializer.Serialize(filter)}";

            if (_cache.TryGetValue(cacheKey, out IEnumerable<MeteoriteLandingGroupedByYearDto>? cachedResult))
            {
                Console.WriteLine($"Returning data from cache for key: {cacheKey}");
                return cachedResult!;
            }

            var allLandings = await _meteoriteRepository.GetAllAsync();

            var query = allLandings.AsQueryable();

            if (filter.StartYear.HasValue)
            {
                query = query.Where(m => m.Year >= filter.StartYear.Value);
            }

            if (filter.EndYear.HasValue)
            {
                query = query.Where(m => m.Year <= filter.EndYear.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.RecClass))
            {
                query = query.Where(m => m.RecClass.ToLower().Contains(filter.RecClass.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(filter.NameContains))
            {
                query = query.Where(m => m.Name.ToLower().Contains(filter.NameContains.ToLower()));
            }

            query = query.Where(m => m.Year.HasValue);

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
            Console.WriteLine($"Caching data for key: {cacheKey}");

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
            (_cache as MemoryCache)?.Compact(1.0);
            Console.WriteLine("Memory cache cleared.");
        }

        public async Task<IEnumerable<string>> GetUniqueRecClassesAsync()
        {
            if (_cache.TryGetValue(UniqueRecClassesCacheKey, out IEnumerable<string> cachedClasses))
            {
                return cachedClasses;
            }

            var allLandings = await _meteoriteRepository.GetAllAsync();
            var uniqueClasses = allLandings
                .Where(m => !string.IsNullOrWhiteSpace(m.RecClass))
                .Select(m => m.RecClass)
                .Distinct()
                .OrderBy(rc => rc)
                .ToList();

            _cache.Set(UniqueRecClassesCacheKey, uniqueClasses, TimeSpan.FromDays(1));

            return uniqueClasses;
        }
    }
}
