using Microsoft.EntityFrameworkCore;
using TCBackend.Dtos.Home;
using TCBackend.Dtos.LeaveSystem;
using TCBackend.Dtos.Management;
using TCBackend.Dtos.Wrappers;
using TCBackend.Model.Content;
using TCBackend.Model.Employee;
using TCBackend.Model.LoginSecurity;
using TCBackend.Model.Masters;

namespace TCBackend.Data
{
    public class TCDbContext : DbContext
    {
        public TCDbContext(DbContextOptions<TCDbContext> option) : base(option) { }

        //-------------Roles & Permission-------------
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<EmployeeDetailView> EmployeeDetails { get; set; }
        public DbSet<Designation> DesignationMaster { get; set; }
        public DbSet<EmpMaster> EmpMaster { get; set; }
        public DbSet<StatusMasterDto> StatusMaster { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);
            
            modelBuilder.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId);

            modelBuilder.Entity<EmployeeDetailView>(eb =>
            {
                eb.HasNoKey();
                eb.ToView("vw_EmployeeDetails");
            });
            modelBuilder.Entity<GenericDropdownDto>().HasNoKey();
            modelBuilder.Entity<UserCalendarDataDto>().HasNoKey();
            modelBuilder.Entity<SpLeaveRequestResult>().HasNoKey();
            modelBuilder.Entity<LeaveProcessHistory>().HasNoKey();
            modelBuilder.Entity<SpLeaveRequestResultV2>().HasNoKey();

            //-------------Unified Content relationships-------------
            modelBuilder.Entity<ContentItem>(eb =>
            {
                eb.HasOne(c => c.Category)
                    .WithMany()
                    .HasForeignKey(c => c.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                eb.HasMany(c => c.Attachments)
                    .WithOne(a => a.Content)
                    .HasForeignKey(a => a.ContentId)
                    .OnDelete(DeleteBehavior.Restrict);

                eb.HasMany(c => c.Audiences)
                    .WithOne(a => a.Content)
                    .HasForeignKey(a => a.ContentId)
                    .OnDelete(DeleteBehavior.Restrict);

                eb.HasMany(c => c.Engagements)
                    .WithOne(e => e.Content)
                    .HasForeignKey(e => e.ContentId)
                    .OnDelete(DeleteBehavior.Restrict);

                eb.HasMany(c => c.Comments)
                    .WithOne(cm => cm.Content)
                    .HasForeignKey(cm => cm.ContentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContentComment>()
                .HasOne(cm => cm.ParentComment)
                .WithMany(cm => cm.Replies)
                .HasForeignKey(cm => cm.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
        }




        public DbSet<GenericDropdownDto> GenericDropdowns { get; set; }
        public DbSet<LoginToken> sp_CheckCredentials { get; set; }
        public DbSet<LeaveRequestMaster> LeaveRequestMaster { get; set; }
        public DbSet<LeaveProcessItem> LeaveProcessItem { get; set; }
        public DbSet<LeaveRequestDetails> LeaveRequestDetails { get; set; }
        public DbSet<Department> DepartmentMaster { get; set; }
        public DbSet<vwEmpList> vw_EmpList { get; set; }
        public DbSet<VW_LeaveReqGrid> sp_LeaveReqGrid { get; set; }
        public DbSet<LeaveProcessHistory> SpLeaveProcessHistoryResults { get; set; }
        public DbSet<LeaveTypeMaster> LeaveTypeMaster { get; set; }
        public DbSet<LeaveBalanceDto> LeaveBalance { get; set; }
        public DbSet<PageMaster> PageMaster { get; set; }
        public DbSet<LocationMaster> LocationMaster { get; set; }
        public DbSet<CompanyMaster> CompanyMaster { get; set; }
        public DbSet<HolidayMaster> HolidayMaster { get; set; }
        public DbSet<SpLeaveRequestResult> SpLeaveRequestResult { get; set; }
        public DbSet<SpLeaveRequestResultV2> SpLeaveRequestResultsV2 { get; set; }

        public DbSet<UserCalendarDataDto> sp_GetUserCalendarData { get; set; }

        //-------------Unified Content (Announcement | Policy | Post)-------------
        public DbSet<ContentCategory> ContentCategory { get; set; }
        public DbSet<ContentItem> ContentItem { get; set; }
        public DbSet<ContentAttachment> ContentAttachment { get; set; }
        public DbSet<ContentAudience> ContentAudience { get; set; }
        public DbSet<ContentEngagement> ContentEngagement { get; set; }
        public DbSet<ContentComment> ContentComment { get; set; }
    }
}
