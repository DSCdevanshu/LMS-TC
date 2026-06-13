using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TCBackend.Model.LoginSecurity
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsSuperAdmin { get; set; }

        // Navigation property for the many-to-many relationship
        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<UserPermission> UserPermissions { get; set; }
    }
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Navigation properties
        public ICollection<UserRole>? UserRoles { get; set; }
        public ICollection<RolePermission>? RolePermissions { get; set; }
    }
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }

        // Navigation property
        public ICollection<RolePermission> RolePermissions { get; set; }
        public ICollection<UserPermission> UserPermissions { get; set; }
    }
    public class UserRole
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }
    }
    // This class represents the RolePermissions junction table
    public class RolePermission
    {
        public int RoleId { get; set; }
        public Role Role { get; set; }

        public int PermissionId { get; set; }
        public Permission Permission { get; set; }
    }
    public class UserPermission
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int PermissionId { get; set; }
        public Permission Permission { get; set; }
    }
    public class MenuItem
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Route { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public int? ParentId { get; set; }
        public int? RequiredPermissionId { get; set; }
        [ForeignKey("ParentId")]
        public MenuItem? Parent { get; set; }
        public ICollection<MenuItem> Children { get; set; }
        [ForeignKey("RequiredPermissionId")]
        public Permission? RequiredPermission { get; set; }
    }


}
