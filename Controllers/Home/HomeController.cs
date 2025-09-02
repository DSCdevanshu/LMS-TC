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
            // Get the logged-in user's ID from the JWT token
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var userDetails = await _context.vw_EmpList.Where(u => u.UserId == userId).FirstOrDefaultAsync();

            if (userDetails == null)
            {
                return NotFound("Employee details not found.");
            }

            return Ok(userDetails);
        }
    }
}
