using Microsoft.EntityFrameworkCore;
using MeteoriteLandings.Application.DTOs;
using MeteoriteLandings.Domain.Entities;
using MeteoriteLandings.Infrastructure.Data;
using MeteoriteLandings.Application.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeteoriteLandings.Infrastructure.Repositories
{
    public class MeteoriteRepository : IMeteoriteRepository
    {
        private readonly ApplicationDbContext _context;

        public MeteoriteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(MeteoriteLanding meteorite)
        {
            await _context.MeteoriteLandings.AddAsync(meteorite);
        }

        public async Task AddRangeAsync(IEnumerable<MeteoriteLanding> meteorites)
        {
            await _context.MeteoriteLandings.AddRangeAsync(meteorites);
        }

        public Task DeleteAsync(MeteoriteLanding meteorite)
        {
            _context.MeteoriteLandings.Remove(meteorite);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(IEnumerable<MeteoriteLanding> meteorites)
        {
            _context.MeteoriteLandings.RemoveRange(meteorites);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<MeteoriteLanding>> GetAllAsync()
        {
            return await _context.MeteoriteLandings.ToListAsync();
        }

        public async Task<IEnumerable<MeteoriteLanding>> GetFilteredAsync(MeteoriteLandingFilterDto filter)
        {
            var query = _context.MeteoriteLandings.AsQueryable();

            // Only include meteorites with valid years first for better performance
            query = query.Where(m => m.Year.HasValue);

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
                var normalizedRecClass = filter.RecClass.Trim();
                query = query.Where(m => EF.Functions.ILike(m.RecClass, $"%{normalizedRecClass}%"));
            }

            if (!string.IsNullOrWhiteSpace(filter.NameContains))
            {
                var normalizedName = filter.NameContains.Trim();
                query = query.Where(m => EF.Functions.ILike(m.Name, $"%{normalizedName}%"));
            }

            return await query.ToListAsync();
        }

        public async Task<MeteoriteLanding?> GetByExternalIdAsync(string externalId)
        {
            return await _context.MeteoriteLandings
                                 .FirstOrDefaultAsync(m => m.ExternalId == externalId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(MeteoriteLanding meteorite)
        {
            _context.MeteoriteLandings.Update(meteorite);
            return Task.CompletedTask;
        }

        public Task UpdateRangeAsync(IEnumerable<MeteoriteLanding> meteorites)
        {
            _context.MeteoriteLandings.UpdateRange(meteorites);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<string>> GetDistinctRecClassesAsync()
        {
            return await _context.MeteoriteLandings
                                 .Where(m => !string.IsNullOrWhiteSpace(m.RecClass))
                                 .Select(m => m.RecClass)
                                 .Distinct()
                                 .OrderBy(rc => rc)
                                 .ToListAsync();
        }
    }
}
