using System.ComponentModel.DataAnnotations;

namespace TCBackend.Dtos
{
    public class LoginDto
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }


    public class RegisterDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }
    }

    public class AssignRoleDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int RoleId { get; set; }
    }
    public class AssignPermissionDto
    {
        [Required]
        public int RoleId { get; set; }
        [Required]
        public int PermissionId { get; set; }
    }

    public class CreateRoleDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
    }
    public class CreatePermissionDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }
}
