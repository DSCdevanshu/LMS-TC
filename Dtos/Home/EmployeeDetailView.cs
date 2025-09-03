namespace TCBackend.Dtos.Home
{
    public class EmployeeDetailView
    {
        public int EmployeeId { get; set; }
        public int UserId { get; set; }
        public string? EmpCode { get; set; }
        public string? FullName { get; set; }
        public string? EmailID { get; set; }
        public string? Mobile { get; set; }
        public string? Status { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationTitle { get; set; }
        public string? ManagerNames { get; set; }
    }
}
