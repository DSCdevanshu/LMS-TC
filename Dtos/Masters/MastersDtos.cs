using System.ComponentModel.DataAnnotations;

namespace TCBackend.Dtos.Masters
{
    // --- Location ---
    public class LocationDto
    {
        [Required(ErrorMessage = "Location Name is required.")]
        public string LocationName { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Coordinates { get; set; }
    }

    public class LocationListDto
    {
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? Coordinates { get; set; }
        public int TotalEmployees { get; set; }
    }

    // --- Company ---
    public class CompanyDto
    {
        [Required(ErrorMessage = "Company Name is required.")]
        public string CompanyName { get; set; }
        [Required(ErrorMessage = "Company Code is required.")]
        public string CompanyCode { get; set; }
    }

    public class CompanyListDto
    {
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyCode { get; set; }
        public int TotalEmployees { get; set; }
    }
}
