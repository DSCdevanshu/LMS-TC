using System.ComponentModel.DataAnnotations;

namespace TCBackend.Dtos.Home
{

    public class CreateEmployeeDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        public string EmpCode { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public DateTime HireDate { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public int DesignationId { get; set; }
    }


public class UpdateEmployeeDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Mobile { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public int DesignationId { get; set; }
    }

    public class ManagerAssignmentDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int ManagerId { get; set; }
    }
}
