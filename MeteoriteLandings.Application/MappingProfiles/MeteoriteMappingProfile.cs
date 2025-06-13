using AutoMapper;
using MeteoriteLandings.Application.DTOs;
using MeteoriteLandings.Domain.Entities;

namespace MeteoriteLandings.Application.MappingProfiles
{
    public class MeteoriteMappingProfile : Profile
    {
        public MeteoriteMappingProfile()
        {
            CreateMap<MeteoriteLanding, MeteoriteLandingDto>();
        }
    }
}
