namespace MeteoriteLandings.Application.DTOs
{
    public class MeteoriteLandingDto
    {
        public string Name { get; set; } = string.Empty;

        public string RecClass { get; set; } = string.Empty;

        public long? Mass { get; set; }

        public int? Year { get; set; }

        public double? Reclat { get; set; }

        public double? Reclong { get; set; }
    }
}
