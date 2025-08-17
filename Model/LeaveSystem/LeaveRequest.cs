using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TCBackend.Model.LeaveSystem
{
    
    public class LeaveRequestMaster
    {
        [Key]
        public int LeaveReqID { get; set; }
        public int? Tableid { get; set; }
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
        public string? ProcessBy { get; set; }
        public DateTime ProcessDate { get; set; }
        public int? ReasonID { get; set; }
        public string? ReasonRemarks { get; set; }
        public string? Status { get; set; }
        public string? CreatedBy { get; set; }
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

    [Keyless]
    public class ModelLeaveBalance
    {
        public decimal? CL { get; set; }
        public decimal? TakenCL { get; set; }
        public decimal? BalCL { get; set; }
        public decimal? EL { get; set; }
        public decimal? TakenEL { get; set; }
        public decimal? BalEL { get; set; }
        public int? RH { get; set; }
        public int? TakenRH { get; set; }
        public int? BalRH { get; set; }
        public int? SL { get; set; }
        public int? ShBal { get; set; }
        public int? TSL { get; set; }
        public int? APCL { get; set; }
        public int? APEL { get; set; }
        public int? APSH { get; set; }
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
        public string? LeaveReqCD { get; set; }
        public string? empname { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? EmpCode { get; set; }
        public int? DepId { get; set; }
        public string? DepCode { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime? RequestDate { get; set; }
        public string? EmpRemarks { get; set; }
        public string? RequestedBy { get; set; }
        public int? ndays { get; set; }
        public int? ReasonID { get; set; }
        public string? ReasonRemarks { get; set; }
        public DateTime? ProcessDate { get; set; }
        public string? ProcessBy { get; set; }
        public string? ProcessName { get; set; }
        public int? processId { get; set; }
        public int? LeaveTypeId { get; set; }
        public string? LeaveType { get; set; }

    }
    [Keyless]
    public class LeaveReqGridParams
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int LeaveTypeid { get; set; }
        public int depId { get; set; }
        public int ProcessID { get; set; }
        public string? UserCode { get; set; }

        public LeaveReqGridParams()
        {
            DateFrom = new DateTime(2025, 1, 1);
            DateTo = new DateTime(2025, 12, 31);
            LeaveTypeid = 0;
            UserCode = "0";
            depId = 0;
            ProcessID = 0;
        }
    }
    [Keyless]
    public class spLeaveProcessHistory
    {
        public int ProcessStatusItemID { get; set; }
        public int LeaveReqID { get; set; }
        public string? LeaveReqCD { get; set; }
        public string? ProcessName { get; set; }
        public string? Status { get; set; }
        public DateTime ProcessDate { get; set; }
        public string? ProcessBy { get; set; }
        public string? ProcessByName { get; set; }
        public int ReasonID { get; set; }
        public string? ReasonRemarks { get; set; }
    }

    public class LeaveTypeMaster
    {
        [Key]
        public int LeaveTypeId { get; set; }
        public string? LeaveType { get; set; }
    }

}
