using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly TCDbContext _context;
        public RolesController(TCDbContext context) { _context = context; }





        [HttpPost("postCreateRole")]
        [HasPermission("roles.manage")] // Only users who can manage roles can create them
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto createRoleDto)
        {
            // Check if a role with the same name already exists
            var roleExists = await _context.Roles.AnyAsync(r => r.Name == createRoleDto.Name);
            if (roleExists)
            {
                return BadRequest("A role with this name already exists.");
            }

            var role = new Role { Name = createRoleDto.Name };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Role created successfully.", Role = role });
        }



        [HttpPost("postAssignPermission")]
        [HasPermission("roles.manage")]
        public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionDto assignPermissionDto)
        {
            var rolePermission = new RolePermission
            {
                RoleId = assignPermissionDto.RoleId,
                PermissionId = assignPermissionDto.PermissionId
            };

            var exists = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == rolePermission.RoleId && rp.PermissionId == rolePermission.PermissionId);

            if (exists)
            {
                return BadRequest("Role already has this permission.");
            }

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Permission assigned to role successfully." });
        }
    }
}
