using System.ComponentModel.DataAnnotations;

namespace MeteoriteLandings.Application.DTOs
{
    public class MeteoriteLandingFilterDto
    {
        private const int MIN_YEAR = 1;
        private const int MAX_YEAR = 9999;
        private const int MAX_STRING_LENGTH = 200;

        [Range(MIN_YEAR, MAX_YEAR, ErrorMessage = "StartYear must be between {1} and {2}.")]
        public int? StartYear { get; set; }

        [Range(MIN_YEAR, MAX_YEAR, ErrorMessage = "EndYear must be between {1} and {2}.")]
        public int? EndYear { get; set; }

        [StringLength(MAX_STRING_LENGTH, ErrorMessage = "RecClass cannot exceed {1} characters.")]
        public string? RecClass { get; set; }

        [StringLength(MAX_STRING_LENGTH, ErrorMessage = "NameContains cannot exceed {1} characters.")]
        public string? NameContains { get; set; }

        [RegularExpression(@"^(year|count|totalmass|mass)?$", 
            ErrorMessage = "SortBy must be one of: year, count, totalmass, mass or empty.")]
        public string? SortBy { get; set; }

        [RegularExpression(@"^(asc|desc)?$", 
            ErrorMessage = "SortOrder must be 'asc', 'desc', or empty.")]
        public string? SortOrder { get; set; } = "asc";
    }
}
