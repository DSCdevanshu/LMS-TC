using TCBackend.Services.IServices;
using Microsoft.EntityFrameworkCore;
using TCBackend.Data;

namespace TCBackend.Services
{

    public class PermissionService : IPermissionService
    {
        private readonly TCDbContext _context;

        public PermissionService(TCDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasPermissionAsync(int userId, string permission)
        {
            // First, check if the user is a Super Admin
            var user = await _context.Users.FindAsync(userId);
            if (user?.IsSuperAdmin == true)
            {
                return true;
            }

            var hasPermission = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .SelectMany(r => r.RolePermissions)
                .Select(rp => rp.Permission)
                .AnyAsync(p => p.Name == permission);

            return hasPermission;
        }
    }
}
