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

    public class LeaveBalanceDto
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal PaidLeave { get; set; }
        public decimal FreeLeave { get; set; }
        public decimal ShortLeave { get; set; }
    }

}
