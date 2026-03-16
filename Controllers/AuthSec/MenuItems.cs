using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TCBackend.Data;
using TCBackend.Dtos.LoginSecurity;
using TCBackend.Dtos.Wrappers;

namespace TCBackend.Controllers.AuthSec
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MenuItemsController : ControllerBase
    {
        private readonly TCDbContext _context;

        public MenuItemsController(TCDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetMyMenu")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<MenuItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyMenu()
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                    return Unauthorized(new ApiResponse<string>(0, "Invalid user token.", null));

                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                    .Include(u => u.UserPermissions)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return NotFound(new ApiResponse<string>(0, "User not found.", null));

                var allowedPermissionIds = new HashSet<int>();
                if (!user.IsSuperAdmin)
                {
                    var rolePerms = user.UserRoles
                        .SelectMany(ur => ur.Role.RolePermissions)
                        .Select(rp => rp.PermissionId);

                    foreach (var pId in rolePerms) allowedPermissionIds.Add(pId);

                    var customPerms = user.UserPermissions.Select(up => up.PermissionId);
                    foreach (var pId in customPerms) allowedPermissionIds.Add(pId);
                }

                var allMenuItems = await _context.MenuItems
                    .AsNoTracking()
                    .OrderBy(m => m.DisplayOrder)
                    .ToListAsync();

                var authorizedItems = allMenuItems.Where(m =>
                    user.IsSuperAdmin ||                                   
                    m.RequiredPermissionId == null ||                      
                    allowedPermissionIds.Contains(m.RequiredPermissionId.Value) 
                ).ToList();

                var lookup = authorizedItems.ToDictionary(m => m.Id, m => new MenuItemDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Route = m.Route,
                    Icon = m.Icon,
                    DisplayOrder = m.DisplayOrder
                });

                var menuTree = new List<MenuItemDto>();

                foreach (var item in authorizedItems)
                {
                    var dto = lookup[item.Id];

                    if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parentDto))
                    {
                        parentDto.Children.Add(dto);
                    }
                    else if (!item.ParentId.HasValue)
                    {
                        menuTree.Add(dto);
                    }
                }

                return Ok(new ApiResponse<IEnumerable<MenuItemDto>>(1, "Success", menuTree));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }
    }
}
