using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos;
using TCBackend.Model.LoginSecurity;

namespace TCBackend.Controllers.AuthSec
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly TCDbContext _context;
        public UsersController(TCDbContext context) { _context = context; }

        [HttpPost("postAssignRole")]
        [HasPermission("users.manage")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto assignRoleDto)
        {
            var userRole = new UserRole
            {
                UserId = assignRoleDto.UserId,
                RoleId = assignRoleDto.RoleId
            };

            // Check if the assignment already exists
            var exists = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userRole.UserId && ur.RoleId == userRole.RoleId);

            if (exists)
            {
                return BadRequest("User already has this role.");
            }

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Role assigned to user successfully." });
        }
    }
}
