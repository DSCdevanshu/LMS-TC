using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos;
using TCBackend.Model.Employee;
using TCBackend.Model.LoginSecurity;

namespace TCBackend.Controllers.Employee
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class EmpAuthorityController : ControllerBase
    {
        private readonly TCDbContext _context;
        public EmpAuthorityController(TCDbContext tCDb)
        {
            _context = tCDb;
        }

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

        [HttpGet("getAllPermissions")]
        [HasPermission("roles.manage")] 
        public async Task<IActionResult> GetAllPermissions()
        {
            var permissions = await _context.Permissions
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            return Ok(permissions);
        }

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


        [HttpGet("getDepartmentList")]
        public async Task<IActionResult> DepartmentList()
        {
            var res = await _context.DepartmentMaster.ToListAsync();
            return Ok(res);
        }

        [HttpGet("getAllEmployeeList")]
        [HasPermission("employees.list.all")]
        public async Task<IActionResult> EmployeeList()
        {
            var res = await _context.vw_EmpList.ToListAsync();
            return Ok(res);
        }

        [HttpGet("my-team")]
        public async Task<IActionResult> GetMyTeam()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var userIdParam = new SqlParameter("@TopLevelUserId", userId);

            var teamList = await _context.vw_EmpList
                .FromSqlRaw("EXEC sp_GetEmployeeHierarchy @TopLevelUserId", userIdParam)
                .ToListAsync();

            return Ok(teamList);
        }

        [HttpGet("getAuthDepartmentList")]
        public async Task<IActionResult> AuthDepartmentList()
        {
            return Ok("Future Updates");
        }
        
        
        [HttpGet("my-menu")]
        public async Task<IActionResult> GetUserMenu()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var isSuperAdmin = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.IsSuperAdmin)
                .FirstOrDefaultAsync();

            var userPermissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.PermissionId)
                .Distinct()
                .ToListAsync();

            var allMenuItems = await _context.MenuItems
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            var accessibleMenu = allMenuItems.Where(item =>
                    item.RequiredPermissionId == null ||
                    isSuperAdmin ||
                    userPermissions.Contains(item.RequiredPermissionId.Value)
                ).ToList();


            return Ok(accessibleMenu);
        }

    }
}
