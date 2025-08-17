using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TCBackend.Model.Employee
{
   
    public class vwEmpList
    {
        [Key]
        public int EmpId { get; set; }
        public string EmpCode { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? FathersName { get; set; }
        public string? MothersName { get; set; }
        public int Designation { get; set; }
        public int Department { get; set; }
        public string? Gender { get; set; }
        public string? EmailID { get; set; }
        public string? Mobile { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ReportingTo { get; set; }
        public int? DepId { get; set; }
        public string? DepCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? empname { get; set; }
    }

    [Keyless]
    public class LoginToken
    {
        public string? token { get; set; }
        public string? expirationKey { get; set; }
    }

    public class Department
    {
        [Key]
        public int DepId { get; set; }
        public string? DepCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? Status { get; set; }
        public string? HOD { get; set; }
    }

    public class PageMaster
    {
        [Key]
        public int PageId { get; set; }
        public string? PageName { get; set; }
        public string? PageURL { get; set; }
        public string? PageDescription { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
    }
    public class PageMasterDto
    {
        public int PageId { get; set; }
        public string? PageName { get; set; }
        public string? PageURL { get; set; }
        public string? PageDescription { get; set; }
    }

}
 					
