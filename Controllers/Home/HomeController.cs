using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TCBackend.Data;

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
    }
}
