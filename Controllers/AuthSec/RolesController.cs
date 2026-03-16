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
    public class RolesController : ControllerBase
    {
        private readonly TCDbContext _context;
        public RolesController(TCDbContext context) { _context = context; }

        [HttpGet]
        [HasPermission("roles.manage")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<RoleResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                // Returns all roles along with an array of their current Permission IDs
                var roles = await _context.Roles
                    .Select(r => new RoleResponseDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        PermissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList()
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<IEnumerable<RoleResponseDto>>(1, "Success", roles));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpPost]
        [HasPermission("roles.manage")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRole([FromBody] UpdateRolePermissionsDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Prevent duplicate role names
                var roleExists = await _context.Roles.AnyAsync(r => r.Name == dto.RoleName);
                if (roleExists)
                {
                    return BadRequest(new ApiResponse<int>(0, "A role with this name already exists.", 0));
                }

                // 2. Create the base role
                var newRole = new Role { Name = dto.RoleName };
                _context.Roles.Add(newRole);

                // Save immediately so EF Core generates the new Role ID for us to use below
                await _context.SaveChangesAsync();

                // 3. Bulk insert the permissions
                if (dto.PermissionIds != null && dto.PermissionIds.Any())
                {
                    var rolePermissions = dto.PermissionIds.Distinct().Select(pId => new RolePermission
                    {
                        RoleId = newRole.Id,
                        PermissionId = pId
                    }).ToList();

                    _context.RolePermissions.AddRange(rolePermissions);
                    await _context.SaveChangesAsync();
                }

                // 4. Commit everything
                await transaction.CommitAsync();

                return Ok(new ApiResponse<int>(1, "Role created successfully.", newRole.Id));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<int>(0, $"Internal Server Error: {ex.Message}", 0));
            }
        }

        [HttpPut("{roleId}")]
        [HasPermission("roles.manage")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRole(int roleId, [FromBody] UpdateRolePermissionsDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

                if (role == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Role not found.", null));
                }

                // Optional Guardrail: Prevent renaming core system roles (so your code doesn't break)
                if (role.Name == "Employee" || role.Name == "SuperAdmin")
                {
                    if (role.Name != dto.RoleName)
                    {
                        return BadRequest(new ApiResponse<string>(0, "Cannot rename core system roles.", null));
                    }
                }

                // 1. Update the Role Name
                role.Name = dto.RoleName;

                // 2. Wipe the old permissions for this role
                var oldPermissions = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
                _context.RolePermissions.RemoveRange(oldPermissions);

                // 3. Insert the newly selected permissions
                if (dto.PermissionIds != null && dto.PermissionIds.Any())
                {
                    var newPermissions = dto.PermissionIds.Distinct().Select(pId => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = pId
                    }).ToList();

                    _context.RolePermissions.AddRange(newPermissions);
                }

                // 4. Save and Commit
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new ApiResponse<string>(1, "Role updated successfully.", null));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }
    }
}
