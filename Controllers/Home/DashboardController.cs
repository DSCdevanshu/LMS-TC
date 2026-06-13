using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCBackend.Data;
using TCBackend.Dtos.Home;
using TCBackend.Dtos.Masters;
using TCBackend.Dtos.Wrappers;
using TCBackend.Services.IServices;

namespace TCBackend.Controllers.Home
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private const int WindowDays = 10;

        private readonly TCDbContext _context;
        private readonly IStorageService _storageService;

        public DashboardController(TCDbContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        // ==================== BIRTHDAY REMINDER ====================

        [HttpGet("birthdays")]
        [ProducesResponseType(typeof(ApiResponse<List<BirthdayDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingBirthdays()
        {
            try
            {
                var employees = await _context.EmployeeDetails
                    .Where(e => e.Status == "A")
                    .Select(e => new
                    {
                        e.UserId,
                        e.EmpCode,
                        e.FullName,
                        e.DepartmentName,
                        e.DesignationTitle,
                        e.PhotoUrl,
                        e.DateofBirth
                    })
                    .ToListAsync();

                var today = DateTime.Today;
                var windowEnd = today.AddDays(WindowDays);

                var result = new List<BirthdayDto>();
                foreach (var e in employees)
                {
                    var upcoming = GetUpcomingOccurrence(e.DateofBirth, today);
                    if (upcoming > windowEnd) continue;

                    result.Add(new BirthdayDto
                    {
                        UserId = e.UserId,
                        EmpCode = e.EmpCode,
                        FullName = e.FullName,
                        DepartmentName = e.DepartmentName,
                        DesignationName = e.DesignationTitle,
                        PhotoUrl = await _storageService.GetSecureFileUrlAsync(e.PhotoUrl),
                        DateOfBirth = e.DateofBirth,
                        UpcomingDate = upcoming,
                        DaysUntil = (upcoming - today).Days
                    });
                }

                var ordered = result.OrderBy(r => r.DaysUntil).ThenBy(r => r.FullName).ToList();
                return Ok(new ApiResponse<List<BirthdayDto>>(1, "Success", ordered));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // ==================== WORK ANNIVERSARY ====================

        [HttpGet("anniversaries")]
        [ProducesResponseType(typeof(ApiResponse<List<WorkAnniversaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingAnniversaries()
        {
            try
            {
                var employees = await _context.EmployeeDetails
                    .Where(e => e.Status == "A" && e.HireDate != null)
                    .Select(e => new
                    {
                        e.UserId,
                        e.EmpCode,
                        e.FullName,
                        e.DepartmentName,
                        e.DesignationTitle,
                        e.PhotoUrl,
                        e.HireDate
                    })
                    .ToListAsync();

                var today = DateTime.Today;
                var windowEnd = today.AddDays(WindowDays);

                var result = new List<WorkAnniversaryDto>();
                foreach (var e in employees)
                {
                    var hireDate = e.HireDate!.Value;
                    var upcoming = GetUpcomingOccurrence(hireDate, today);
                    if (upcoming > windowEnd) continue;

                    result.Add(new WorkAnniversaryDto
                    {
                        UserId = e.UserId,
                        EmpCode = e.EmpCode,
                        FullName = e.FullName,
                        DepartmentName = e.DepartmentName,
                        DesignationName = e.DesignationTitle,
                        PhotoUrl = await _storageService.GetSecureFileUrlAsync(e.PhotoUrl),
                        HireDate = hireDate,
                        UpcomingDate = upcoming,
                        DaysUntil = (upcoming - today).Days,
                        Years = upcoming.Year - hireDate.Year
                    });
                }

                var ordered = result.OrderBy(r => r.DaysUntil).ThenBy(r => r.FullName).ToList();
                return Ok(new ApiResponse<List<WorkAnniversaryDto>>(1, "Success", ordered));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // ==================== HOLIDAY CALENDAR ====================

        [HttpGet("holidays")]
        [ProducesResponseType(typeof(ApiResponse<List<HolidayListDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUpcomingHolidays()
        {
            try
            {
                var today = DateTime.Today;
                var yearEnd = new DateTime(today.Year, 12, 31);

                var holidays = await (from h in _context.HolidayMaster
                                      join c in _context.CompanyMaster on h.CompanyId equals c.CompanyId into cj
                                      from c in cj.DefaultIfEmpty()
                                      join l in _context.LocationMaster on h.LocationId equals l.LocationId into lj
                                      from l in lj.DefaultIfEmpty()
                                      where h.HolidayDate >= today && h.HolidayDate <= yearEnd
                                      orderby h.HolidayDate
                                      select new HolidayListDto
                                      {
                                          HolidayId = h.HolidayId,
                                          HolidayDate = h.HolidayDate,
                                          HolidayName = h.HolidayName,
                                          CompanyId = h.CompanyId,
                                          CompanyName = c.CompanyName,
                                          LocationId = h.LocationId,
                                          LocationName = l.LocationName,
                                          IsRestricted = h.IsRestricted
                                      }).ToListAsync();

                return Ok(new ApiResponse<List<HolidayListDto>>(1, "Success", holidays));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // ==================== HELPERS ====================

        // Returns the next occurrence (today or future) of the given date's month/day.
        // Feb 29 falls back to Feb 28 in non-leap years.
        private static DateTime GetUpcomingOccurrence(DateTime date, DateTime today)
        {
            DateTime BuildFor(int year)
            {
                int day = date.Day;
                if (date.Month == 2 && date.Day == 29 && !DateTime.IsLeapYear(year))
                {
                    day = 28;
                }
                return new DateTime(year, date.Month, day);
            }

            var thisYear = BuildFor(today.Year);
            return thisYear >= today ? thisYear : BuildFor(today.Year + 1);
        }
    }
}
