using MeteoriteLandings.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MeteoriteLandings.Application.Services
{
    public interface IMeteoriteService
    {
        Task<IEnumerable<MeteoriteLandingGroupedByYearDto>> GetFilteredAndGroupedLandingsAsync(
            MeteoriteLandingFilterDto filter);
        Task<List<string>> ValidateFilterAsync(MeteoriteLandingFilterDto filter);
        void ClearCache();
        Task<IEnumerable<string>> GetUniqueRecClassesAsync();
    }
}
