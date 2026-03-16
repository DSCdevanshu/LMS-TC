using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos;
using TCBackend.Dtos.LoginSecurity;
using TCBackend.Dtos.Wrappers;
using TCBackend.Model.LoginSecurity;

namespace TCBackend.Controllers.AuthSec
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly TCDbContext _context;
        public UsersController(TCDbContext context) { _context = context; }

        [HttpGet("access/{userId}")]
        [HasPermission("users.manage")]
        [ProducesResponseType(typeof(ApiResponse<UserAccessDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserAccess(int userId)
        {
            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                    return NotFound(new ApiResponse<UserAccessDto>(0, "User not found.", null));

                // Fetch current Roles and Custom Permissions in a single optimized query
                var userAccess = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new UserAccessDto
                    {
                        RoleIds = u.UserRoles.Select(ur => ur.RoleId).ToList(),
                        CustomPermissionIds = u.UserPermissions.Select(up => up.PermissionId).ToList()
                    })
                    .FirstOrDefaultAsync();

                return Ok(new ApiResponse<UserAccessDto>(1, "Success", userAccess));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpPut("access/{userId}")]
        [HasPermission("users.manage")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateUserAccess(int userId, [FromBody] UserAccessDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                    return NotFound(new ApiResponse<string>(0, "User not found.", null));

                // 1. --- Update Roles (Wipe & Replace) ---
                var oldRoles = await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
                _context.UserRoles.RemoveRange(oldRoles);

                if (dto.RoleIds != null && dto.RoleIds.Any())
                {
                    var newRoles = dto.RoleIds.Distinct().Select(rId => new UserRole
                    {
                        UserId = userId,
                        RoleId = rId
                    }).ToList();
                    _context.UserRoles.AddRange(newRoles);
                }

                // 2. --- Update Custom Permissions (Wipe & Replace) ---
                var oldPermissions = await _context.UserPermissions.Where(up => up.UserId == userId).ToListAsync();
                _context.UserPermissions.RemoveRange(oldPermissions);

                if (dto.CustomPermissionIds != null && dto.CustomPermissionIds.Any())
                {
                    var newPermissions = dto.CustomPermissionIds.Distinct().Select(pId => new UserPermission
                    {
                        UserId = userId,
                        PermissionId = pId
                    }).ToList();
                    _context.UserPermissions.AddRange(newPermissions);
                }

                // 3. Save everything safely
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new ApiResponse<string>(1, "User access updated successfully.", null));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }
    }
}
