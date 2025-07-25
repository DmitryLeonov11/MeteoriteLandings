using MeteoriteLandings.Application.DTOs;
using MeteoriteLandings.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MeteoriteLandings.Application.Repositories
{
    public interface IMeteoriteRepository
    {
        Task<IEnumerable<MeteoriteLanding>> GetAllAsync();
        Task<IEnumerable<MeteoriteLanding>> GetFilteredAsync(MeteoriteLandingFilterDto filter);
        Task AddRangeAsync(IEnumerable<MeteoriteLanding> meteorites);
        Task UpdateRangeAsync(IEnumerable<MeteoriteLanding> meteorites);
        Task DeleteRangeAsync(IEnumerable<MeteoriteLanding> meteorites);
        Task SaveChangesAsync();
        Task<IEnumerable<string>> GetDistinctRecClassesAsync();
    }
}
