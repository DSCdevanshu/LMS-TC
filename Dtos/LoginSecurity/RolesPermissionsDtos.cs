namespace TCBackend.Dtos.LoginSecurity
{


    public class UserAccessDto
    {
        public List<int> RoleIds { get; set; } = new List<int>();
        public List<int> CustomPermissionIds { get; set; } = new List<int>();
    }
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public class GroupedPermissionDto
    {
        public string Category { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
    }

    public class CreatePermissionDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
    }
    public class RoleResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>();
    }

    public class UpdateRolePermissionsDto
    {
        public string RoleName { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Route { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }   
        public List<MenuItemDto> Children { get; set; } = new List<MenuItemDto>();
    }
}
