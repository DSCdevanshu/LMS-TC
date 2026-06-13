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

    // --- Holiday ---
    public class HolidayDto
    {
        [Required(ErrorMessage = "Holiday Date is required.")]
        public DateTime HolidayDate { get; set; }
        [Required(ErrorMessage = "Holiday Name is required.")]
        public string HolidayName { get; set; }
        [Required(ErrorMessage = "Company is required.")]
        public int CompanyId { get; set; }
        [Required(ErrorMessage = "Location is required.")]
        public int LocationId { get; set; }
        public bool IsRestricted { get; set; }
    }

    public class HolidayListDto
    {
        public int HolidayId { get; set; }
        public DateTime HolidayDate { get; set; }
        public string? HolidayName { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public bool IsRestricted { get; set; }
    }
}
