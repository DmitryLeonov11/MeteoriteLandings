namespace MeteoriteLandings.Domain.Entities
{
    public class MeteoriteLanding
    {
        public Guid Id { get; set; }

        public string ExternalId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string NameType { get; set; } = string.Empty;

        public string RecClass { get; set; } = string.Empty;

        public long? Mass { get; set; }

        public string Fall { get; set; } = string.Empty;

        public int? Year { get; set; }

        public double? Reclat { get; set; }

        public double? Reclong { get; set; }

        public string GeoLocation { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
