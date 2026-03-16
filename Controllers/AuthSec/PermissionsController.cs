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
    public class PermissionsController : ControllerBase
    {
        private readonly TCDbContext _context;
        public PermissionsController(TCDbContext context) { _context = context; }



        [HttpGet]
        [HasPermission("roles.manage")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<GroupedPermissionDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                // Fetch permissions and sort them nicely
                var permissions = await _context.Permissions
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                // Group them in memory so the frontend gets a clean, nested JSON structure
                var groupedPermissions = permissions
                    .GroupBy(p => p.Category ?? "General") // Fallback to "General" if category is null
                    .Select(g => new GroupedPermissionDto
                    {
                        Category = g.Key,
                        Permissions = g.Select(p => new PermissionDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Description = p.Description
                        }).ToList()
                    })
                    .ToList();

                return Ok(new ApiResponse<IEnumerable<GroupedPermissionDto>>(1, "Success", groupedPermissions));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpPost]
        [HasPermission("roles.manage")]
        [ProducesResponseType(typeof(ApiResponse<PermissionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionDto dto)
        {
            try
            {
                var exists = await _context.Permissions.AnyAsync(p => p.Name == dto.Name);
                if (exists)
                {
                    return BadRequest(new ApiResponse<string>(0, "A permission with this internal name already exists.", null));
                }

                // Create the entity
                var permission = new Permission
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Category = dto.Category
                };

                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();

                // Map back to DTO for the response
                var responseDto = new PermissionDto
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    Description = permission.Description
                };

                return Ok(new ApiResponse<PermissionDto>(1, "Permission created successfully.", responseDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }
    }
}
