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
    public class PermissionsController : ControllerBase
    {
        private readonly TCDbContext _context;
        public PermissionsController(TCDbContext context) { _context = context; }



        [HttpGet("getAllPermissions")]
        [HasPermission("roles.manage")]
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _context.Permissions
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            return Ok(permissions);
        }

        [HttpPost("postCreatePermission")]
        [HasPermission("roles.manage")] // Creating permissions is part of role management
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionDto createPermissionDto)
        {
            // Check if a permission with the same name already exists
            var permissionExists = await _context.Permissions.AnyAsync(p => p.Name == createPermissionDto.Name);
            if (permissionExists)
            {
                return BadRequest("A permission with this name already exists.");
            }

            var permission = new Permission { Name = createPermissionDto.Name };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Permission created successfully.", Permission = permission });
        }
    }
}
