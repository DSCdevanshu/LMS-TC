using System.ComponentModel.DataAnnotations;

namespace TCBackend.Dtos.LeaveSystem
{
    public class CreateLeaveRequestDto
    {
        public int? UserId { get; set; }
        [Required]
        public int LeaveTypeId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string? EmpRemarks { get; set; }
    }
    public class CreateLeaveRequestDtoV2
    {
        public int? LeaveRequestId { get; set; }
        public int? UserId { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? EmpRemarks { get; set; }
    }

    public class LeaveBalanceDto
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int LeaveTypeId { get; set; }
        public decimal Balance { get; set; }
        public bool IsUnlimited { get; set; }
    }

    public class UserLeaveBalanceDto
    {
        public int LeaveTypeId { get; set; }
        public string? LeaveType { get; set; }
        public decimal Balance { get; set; }
        public bool IsUnlimited { get; set; }
    }

}
