namespace MeteoriteLandings.Application.DTOs
{
    public class MeteoriteLandingGroupedByYearDto
    {
        public int Year { get; set; }

        public int Count { get; set; }

        public long TotalMass { get; set; }
    }
}
