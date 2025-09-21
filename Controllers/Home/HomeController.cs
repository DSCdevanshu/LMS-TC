using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TCBackend.Data;
using TCBackend.Dtos.Home;

namespace TCBackend.Controllers.Home
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HomeController : ControllerBase
    {
        private readonly TCDbContext _context;

        public HomeController(TCDbContext context)
        {
            _context = context;
        }


        [HttpGet("mydetails")]
        public async Task<IActionResult> GetMyDetails()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var myDetails = await _context.EmployeeDetails
                .FirstOrDefaultAsync(emp => emp.UserId == userId);

            if (myDetails == null)
            {
                return NotFound("Your employee details could not be found.");
            }

            return Ok(myDetails);
        }

        [HttpGet]
        public async Task<IActionResult> GetDropdownData([FromQuery] string flag)
        {
            if (string.IsNullOrEmpty(flag))
            {
                return BadRequest("A 'flag' parameter is required.");
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var flagParam = new SqlParameter("@Flag", flag);
            var userIdParam = new SqlParameter("@LoginUserId", userId);

            var results = await _context.Set<GenericDropdownDto>()
                .FromSqlRaw("EXEC sp_GetDropdownData @Flag, @LoginUserId", flagParam, userIdParam)
                .ToListAsync();

            return Ok(results);
        }
    }
}
