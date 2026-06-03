using System.ComponentModel.DataAnnotations;

namespace TCBackend.Dtos.Home
{

    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Text.Json.Serialization;

    public class CreateEmployeeDto
    {
        [Required] public string Username { get; set; }
        [Required][MinLength(8)] public string Password { get; set; }
        [Required] public string EmpCode { get; set; }
        [Required] public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        [Required] public string LastName { get; set; }
        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        [Required] public DateTime DateOfBirth { get; set; }
        [Required] public string Gender { get; set; }
        public string? Address { get; set; }
        public IFormFile? Photo { get; set; }
        [Required][EmailAddress] public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        [Required] public DateTime HireDate { get; set; }
        [Required] public int DesignationId { get; set; }
        [Required] public int DepartmentId { get; set; }
        public List<int>? ReportingManagerIds { get; set; }
        public string? PAN { get; set; }
        public string? AadhaarCard { get; set; }
        public string? BankAccountNumber { get; set; }
    }


    public class UpdateEmployeeDto
    {
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? Gender { get; set; }
        public string? Mobile { get; set; }
        public int DepartmentId { get; set; }
        public int DesignationId { get; set; }
        public string? Address { get; set; }
        public string? PAN { get; set; }
        public string? AadhaarCard { get; set; }
        public string? BankAccountNumber { get; set; }
        public List<int>? ReportingManagerIds { get; set; }
    }
    public class EmployeeListDto
    {
        public int UserId { get; set; }
        public string? EmpCode { get; set; }
        public string? EmpName { get; set; }
        public string? EmailID { get; set; }
        public string? Mobile { get; set; }
        public string? DesignationName { get; set; }
        public string? DepartmentName { get; set; }
        public string? Status { get; set; }
    }

    public class EmployeeDetailsDto
    {
        public int UserId { get; set; }
        public string? EmpCode { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? EmailID { get; set; }
        public string? Mobile { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? PhotoUrl { get; set; }
        public int DepartmentId { get; set; }
        public int DesignationId { get; set; }
        public string? PAN { get; set; }
        public string? AadhaarCard { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? Status { get; set; }
        public List<int> ReportingManagerIds { get; set; } = new List<int>();
    }
    public class ManagerAssignmentDto
    {
        [Required]
        public int EmployeeId { get; set; }
        [Required]
        public int ManagerId { get; set; }
    }
    public class EmployeeGridDto
    {
        public int UserId { get; set; }
        public string? EmpCode { get; set; }
        public string? EmpName { get; set; }
        public string? EmailID { get; set; }
        public string? Mobile { get; set; }
        public string? Status { get; set; }
        public DateTime? HireDate { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? ManagerNames { get; set; }
    }
    public class EmployeeFilterDto
    {
        public string? SearchText { get; set; }
        public int? DepartmentId { get; set; } = 0;
        public int? DesignationId { get; set; } = 0;
        public string? Status { get; set; }
        public DateTime? HireDateFrom { get; set; }
        public DateTime? HireDateTo { get; set; }
    }
    public class EmployeeDetailView
    {
        public int EmployeeId { get; set; }
        public int UserId { get; set; }
        public string? EmpCode { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? EmailID { get; set; }
        public string? Mobile { get; set; }
        public DateTime DateofBirth { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Status { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int DesignationId { get; set; }
        public string? DesignationTitle { get; set; }
        public string? PAN { get; set; }
        public string? AadhaarCard { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? ManagerNames { get; set; }
        public string? ManagerIds { get; set; } // We will split this into a List<int>
    }


    [Table("EmpMaster")]
    public class EmpMaster
    {
        [Key]
        public int ID { get; set; }
        public int UserId { get; set; }
        public string? EmpCode { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        public string? PAN { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? AadhaarCard { get; set; }
        public DateTime DateofBirth { get; set; }
        public int DesignationId { get; set; }
        public int DepartmentId { get; set; }
        public string? Address { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Gender { get; set; }
        public string? EmailID { get; set; }
        public string? Mobile { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? HireDate { get; set; }
        public int? CompanyId { get; set; }
        public int? LocationId { get; set; }
    }
}
