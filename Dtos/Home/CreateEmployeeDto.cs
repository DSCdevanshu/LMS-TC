using System.ComponentModel.DataAnnotations;

namespace TCBackend.Dtos.Home
{

    using System.ComponentModel.DataAnnotations;

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
        public string? MiddleName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public string Gender { get; set; }
        public string? Address { get; set; }
        public string? PhotoUrl { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        [Required]
        public DateTime HireDate { get; set; }
        [Required]
        public int DesignationId { get; set; }
        [Required]
        public int DepartmentId { get; set; }
        public int? ReportingManagerId { get; set; }
        public string? PAN { get; set; }
        public string? AadhaarCard { get; set; }
        public string? BankAccountNumber { get; set; }
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
