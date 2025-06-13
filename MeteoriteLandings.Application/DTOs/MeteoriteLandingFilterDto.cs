using System.ComponentModel.DataAnnotations;

namespace MeteoriteLandings.Application.DTOs
{
    public class MeteoriteLandingFilterDto
    {

        [Range(1, 9999, ErrorMessage = "StartYear must be between 1 and 9999.")]
        public int? StartYear { get; set; }

        [Range(1, 9999, ErrorMessage = "EndYear must be between 1 and 9999.")]
        public int? EndYear { get; set; }

        public string? RecClass { get; set; }

        public string? NameContains { get; set; }

        public string? SortBy { get; set; }

        public string? SortOrder { get; set; } = "asc";
    }
}
