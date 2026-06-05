using System.ComponentModel.DataAnnotations;

namespace TCBackend.Dtos.Management
{
   
    public class DepartmentDto
    {
        [Required(ErrorMessage = "Department Code is required.")]
        public string DepCode { get; set; }
        [Required(ErrorMessage = "Department Name is required.")]
        public string DepartmentName { get; set; }
        public int HOD { get; set; }
    }
    public class DepartmentListDto
    {
        public int DepId { get; set; }
        public string? DepCode { get; set; }
        public string? DepartmentName { get; set; }
        public int? HOD { get; set; }
        public string? HODName { get; set; }
        public string? Status { get; set; }
        public int TotalEmployees { get; set; }
    }
    public class Designation
    {
        public int DesignationId { get; set; }
        public string? Title { get; set; }
    }
    public class DesignationListDto
    {
        public int DesignationId { get; set; }
        public string? Title { get; set; }
        public int TotalEmployees { get; set; }
    }
    public class DesignationDto
    {
        public string? Title { get; set; }
    }
}
