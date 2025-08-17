using Microsoft.EntityFrameworkCore;
using TCBackend.Model.Employee;
using TCBackend.Model.LeaveSystem;
using TCBackend.Model.LoginSecurity;

namespace TCBackend.Data
{
    public class TCDbContext:DbContext
    {
        public TCDbContext(DbContextOptions<TCDbContext> option):base(option) { }

        //-------------Roles & Permission-------------
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the composite primary key for UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            // Configure the many-to-many relationship between User and Role
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Configure the composite primary key for RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // Configure the many-to-many relationship between Role and Permission
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);
        }





        public DbSet<LoginToken> sp_CheckCredentials { get; set; }
        public DbSet<LeaveRequestMaster> LeaveRequestMaster { get; set; }
        public DbSet<LeaveProcessItem> LeaveProcessItem { get; set; }
        public DbSet<LeaveRequestDetails> LeaveRequestDetails { get; set; }
        public DbSet<Department> DepartmentMaster { get; set; }
        public DbSet<vwEmpList> vw_EmpList { get; set; }
        public DbSet<VW_LeaveReqGrid> sp_LeaveReqGrid { get; set; }
        public DbSet<spLeaveProcessHistory> sp_LeaveProcessHistory { get; set; }
        public DbSet<LeaveTypeMaster> LeaveTypeMaster { get; set; }
        public DbSet<PageMaster> PageMaster { get; set; }
    }
}
