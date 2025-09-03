namespace TCBackend.Dtos.Management
{
    public class DepartmentDto
    {
        public string? DepCode { get; set; }
        public string? DepartmentName { get; set; }
        public int HOD { get; set; }
    }
    public class Designation
    {
        public int DesignationId { get; set; }
        public string? Title { get; set; }
    }
    public class DesignationDto
    {
        public string? Title { get; set; }
    }
}
