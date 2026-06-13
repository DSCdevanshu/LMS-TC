using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TCBackend.Dtos.LeaveSystem
{
    public class SpLeaveRequestResult
    {
        public string Message { get; set; }
        public int? LeaveRequestID { get; set; }
        public string? ErrorMessage { get; set; }
        public int? ErrorLine { get; set; }
        public int? ErrorNumber { get; set; }
    }
    public class SpLeaveRequestResultV2
    {
        public int Status { get; set; }
        public string? Message { get; set; }
        public int? LeaveReqID { get; set; }
    }

    public class LeaveRequestMaster
    {
        [Key]
        public int LeaveReqID { get; set; }
        public int? Tableid { get; set; }
        public int? UserId { get; set; }
        public string? EmpCode { get; set; }
        public int LeaveTypeid { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? RequestDate { get; set; }
        public string? RequestBy { get; set; }
        public string? Status { get; set; }
        public int ProcessId { get; set; }
    }
    public class LeaveTypemaster
    {
        [Key]
        public int LeaveTypeID { get; set; }
        public string? LeaveType { get; set; }
    }
    public class LeaveRequestDetails
    {
        [Key]
        public int LRDid { get; set; }
        public int LRHDID { get; set; }
        public DateTime LeaveDate { get; set; }
        public int LeaveTypeID { get; set; }
    }
    public class Calendar
    {
        [Key]
        public int DateID { get; set; }
        public DateTime Datevalue { get; set; }
        public int DateDW { get; set; }
        public string? DateHL { get; set; }
        public int? Frequency { get; set; }
    }

    public class LeaveProcessItem
    {
        [Key]
        public int ProcessStatusItemID { get; set; }
        public int TableID { get; set; }
        public int ItemID { get; set; }
        public int ProcessID { get; set; }
        public int? ProcessBy { get; set; }
        public DateTime ProcessDate { get; set; }
        public int? ReasonID { get; set; }
        public string? ReasonRemarks { get; set; }
        public string? Status { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    /// <summary>
    /// Models not created Yet
    /// </summary>

    public class LeaveReasons
    {
        [Key]
        public int ItemActionReasonID { get; set; }
        public string? Reason { get; set; }
        public string? ActionType { get; set; }

    }

    public class CalMonth
    {
        [Key]
        public int MonthNo { get; set; }
        public string? MonthSName { get; set; }
        public string? MonthFname { get; set; }
    }
    public class CalYear
    {
        [Key]
        public int ID { get; set; }
        public int? Year { get; set; }
    }
    public class VW_LeaveReqGrid
    {
        [Key]
        public int LeaveReqID { get; set; }
        public string EmpCode { get; set; }
        public int UserId { get; set; }
        public string EmployeeName { get; set; }
        public int? DepID { get; set; }
        public string? DepartmentName { get; set; }
        public int? DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public int LeaveTypeid { get; set; }
        public string LeaveTypeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public DateTime RequestDate { get; set; }
        public string? EmpRemarks { get; set; }
        public string? RequestBy { get; set; }
        public string RecordStatus { get; set; }
        public int ProcessId { get; set; }
        public string CurrentStatusName { get; set; }
        public string? RM_HOD_ApprovedBy { get; set; }
        public DateTime? RM_HOD_ApprovalDate { get; set; }
        public string? HR_ApprovedBy { get; set; }
        public DateTime? HR_ApprovalDate { get; set; }
    }
    public class LeaveReqGridParams
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? LeaveTypeid { get; set; }
        public int? LeaveUserId { get; set; }
        public int? DepId { get; set; }
        public int? ProcessID { get; set; }
    }

    public class ChangeProcessResult
    {
        public int Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public int LeaveReqID { get; set; }
    }
    public class LeaveProcessHistory
    {
        public DateTime? ProcessDate { get; set; }
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public int? ProcessByUserId { get; set; }
        public string? ProcessByCode { get; set; }
        public string? ProcessByName { get; set; }
        public string? Icon { get; set; }
        public string? ColorTheme { get; set; }
    }

    public class LeaveTypeMaster
    {
        [Key]
        public int LeaveTypeId { get; set; }
        public string? LeaveType { get; set; }
    }
    public class UserCalendarDataDto
    {
        public DateTime? Date { get; set; }
        public string? Title { get; set; }
        public string? Type { get; set; }
        public string? Status { get; set; }
    }
    public class LeaveDetailsResponseDto
    {
        public LeaveRequestHeaderDto? Header { get; set; }
        public List<LeaveRequestDayDto> Days { get; set; } = new List<LeaveRequestDayDto>();
    }

    public class LeaveRequestHeaderDto
    {
        public int LeaveReqID { get; set; }
        public int? UserId { get; set; }
        public string? EmpCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? Designation { get; set; }
        public string? PhotoUrl { get; set; }
        public int? LeaveTypeId { get; set; }
        public string? LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? CurrentStatus { get; set; }
        public string? EmpRemarks { get; set; }
        public DateTime RequestDate { get; set; }
    }

    public class LeaveRequestDayDto
    {
        public DateTime LeaveDate { get; set; }
        public string? DayName { get; set; }
        public string? DayLeaveType { get; set; }
    }

    public class ChangeLeaveProcessDto
    {
        public int LeaveReqId { get; set; }
        public int ProcessId { get; set; }
        public string? Remarks { get; set; }
    }
    public class LeaveActionButtonDto
    {
        public int ProcessId { get; set; }
        public string ButtonName { get; set; }
        public string ColorTheme { get; set; }
    }
}
