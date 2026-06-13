namespace TCBackend.Dtos.Home
{
    public class BirthdayDto
    {
        public int UserId { get; set; }
        public string? EmpCode { get; set; }
        public string? FullName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime UpcomingDate { get; set; }
        public int DaysUntil { get; set; }
    }

    public class WorkAnniversaryDto
    {
        public int UserId { get; set; }
        public string? EmpCode { get; set; }
        public string? FullName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime UpcomingDate { get; set; }
        public int DaysUntil { get; set; }
        public int Years { get; set; }
    }
}
