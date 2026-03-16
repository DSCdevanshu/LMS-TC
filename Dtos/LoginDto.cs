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
}
