using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using TCBackend.Data;
using Microsoft.EntityFrameworkCore;
using TCBackend.Model.LeaveSystem;
using TCBackend.Model.Employee;
using Microsoft.AspNetCore.Authorization;
using TCBackend.Services;
using System.Diagnostics;
namespace TCBackend.Controllers.LeaveSystem
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private TCDbContext _dbContext;
        private readonly EmailService _emailService;
        public LeaveRequestController(TCDbContext context, EmailService emailService)
        {
            _dbContext = context;
            _emailService = emailService;
        }
        [HttpGet("test")]
        public async Task<IActionResult> test()
        {
            return Ok("Test is working");
        }

        private async Task<IActionResult> changeProcess(int LeaveReqId, int processId)
        {
            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

            if (leave == null)
            {
                return NotFound("Leave request not found.");
            }
            var record = new LeaveProcessItem
            {
                ItemID=LeaveReqId,
                ProcessID=processId,
                ProcessBy = User.FindFirst("EmployeeCode")?.Value,
                ProcessDate = DateTime.Now,
                Status="A",
                CreatedBy = User.FindFirst("EmployeeCode")?.Value,
                CreatedOn=DateTime.Now,

            };
            await _dbContext.LeaveProcessItem.AddAsync(record);
            leave.ProcessId = processId;
            _dbContext.LeaveRequestMaster.Update(leave);
            await _dbContext.SaveChangesAsync();
           
            return Ok("Success");
        }

        /*[HttpPost("postLeaveProcessHistory")]
        public async Task<IActionResult> LeaveProcessHistory(int LeaveReqID)
        {
            var history = await _dbContext.sp_LeaveReqGrid.FromSqlRaw("sp_LeaveProcessHistory @LeaveReqID={0}", LeaveReqID).ToListAsync();
            return Ok(history);
        }*/
        [HttpPost("postLeaveProcessHistory")]
        public async Task<IActionResult> LeaveProcessHistory(int LeaveReqID)
        {
            try
            {
                // Input validation
                if (LeaveReqID <= 0)
                {
                    return BadRequest(new
                    {
                        Status = "Error",
                        Message = "LeaveReqID must be a positive integer.",
                        Data = (object)null
                    });
                }

                // Call the stored procedure
                var history = await _dbContext.sp_LeaveReqGrid
                    .FromSqlRaw("sp_LeaveProcessHistory @LeaveReqID={0}", LeaveReqID)
                    .ToListAsync();

                // Check if history is empty or contains a message
                if (history == null || !history.Any())
                {
                    return NotFound(new
                    {
                        Status = "Error",
                        Message = $"No process history found for LeaveReqID: {LeaveReqID}",
                        Data = (object)null
                    });
                }

                // Return success response
                return Ok(new
                {
                    Status = "Success",
                    Message = "Process history retrieved successfully.",
                    Data = history
                });
            }
            catch (Exception ex)
            {
                // Return a generic error response
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "An unexpected error occurred while retrieving the process history. Please try again later.",
                    Data = (object)null
                });
            }
        }


        [HttpPost("postLeaveReqSend")]
        public async Task<IActionResult> LeaveReqSend(int LeaveReqId)
        {
            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();
            
            if (leave == null)
            {
                return NotFound("Leave request not found.");
            }
            return await changeProcess(LeaveReqId, 2);            
            // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        }

        [HttpPost("postLeaveApprovedByRM")]
        public async Task<IActionResult> LeaveApprovedByRM(int LeaveReqId)
        {
            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

            if (leave == null)
            {
                return NotFound("Leave request not found.");
            }
            return await changeProcess(LeaveReqId, 3);
            // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        }


        [HttpPost("postLeaveRejectedByRM")]
        public async Task<IActionResult> LeaveRejectedByRM(int LeaveReqId)
        {
            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

            if (leave == null)
            {
                return NotFound("Leave request not found.");
            }
            return await changeProcess(LeaveReqId, 4);
            // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        }


        [HttpPost("postLeaveHoldByRM")]
        public async Task<IActionResult> LeaveHoldByRM(int LeaveReqId)
        {
            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

            if (leave == null)
            {
                return NotFound("Leave request not found.");
            }
            return await changeProcess(LeaveReqId, 5);
            // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        }


        [HttpPost("postLeaveApprovedByHR")]
        public async Task<IActionResult> LeaveApprovedByHR(int LeaveReqId)
        {
            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

            if (leave == null)
            {
                return NotFound("Leave request not found.");
            }
            return await changeProcess(LeaveReqId, 6);
            // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        }



        [HttpPost("postLeaveRejectedByHR")]
        public async Task<IActionResult> LeaveRejectedByHR(int LeaveReqId)
        {
            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

            if (leave == null)
            {
                return NotFound("Leave request not found.");
            }
            return await changeProcess(LeaveReqId, 7);
            // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        }



        [HttpPost("postLeaveHoldByHR")]
        public async Task<IActionResult> LeaveHoldByHR(int LeaveReqId)
        {
            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

            if (leave == null)
            {
                return NotFound("Leave request not found.");
            }
            return await changeProcess(LeaveReqId, 8);
            // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        }



        private async Task<bool> ValidateLeaveReq(string empCode, int leaveType, DateTime startDate, DateTime endDate, string empRemarks)
        {
            return false;
        }

        [HttpPost("postLeaveReqEntry")]
        public async Task<IActionResult> LeaveReqEntry(string empCode, int leaveType, DateTime startDate, DateTime endDate, string empRemarks)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(empCode) || empCode.Length > 10)
            {
                return BadRequest("Invalid employee code.");
            }
            if (leaveType <= 0)
            {
                return BadRequest("Invalid leave type.");
            }
            if (startDate > endDate)
            {
                return BadRequest("Start date must be before end date.");
            }
            if (string.IsNullOrWhiteSpace(empRemarks))
            {
                empRemarks = null; // Allow null remarks if empty
            }

            // Get loginUsr from session (or authentication context)
            var loginUsr = User.FindFirst("EmployeeCode")?.Value;
            var loginDepId = User.FindFirst("EmpDepId")?.Value;
            
            try
            {
                // Execute the stored procedure with parameters
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC sp_LeaveReqEntry @empcode, @leaveType, @startDate, @endDate, @loginUsr, @empRemarks",
                    new SqlParameter("@empcode", empCode),
                    new SqlParameter("@leaveType", leaveType),
                    new SqlParameter("@startDate", startDate),
                    new SqlParameter("@endDate", endDate),
                    new SqlParameter("@loginUsr", loginUsr),
                    new SqlParameter("@empRemarks", (object)empRemarks ?? DBNull.Value)
                );

                return Ok("Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, "An error occurred while processing the leave request.");
            }
        }


        [HttpPost("getLeaveUserList")]
        public async Task<IActionResult> LeaveUserList(LeaveReqGridParams? gridParams = null)
        {
            gridParams ??= new LeaveReqGridParams();
            
            List<VW_LeaveReqGrid> model = await _dbContext.sp_LeaveReqGrid.FromSqlRaw("sp_LeaveReqGrid @DateFrom={0},@DateTo={1},@LeaveTypeid={2},@UserCode={3},@processId = {4}", gridParams.DateFrom, gridParams.DateTo, gridParams.LeaveTypeid, gridParams.UserCode, gridParams.ProcessID).ToListAsync();
            return Ok(model);
        }
        [HttpPost("getLeaveRmList")]
        public async Task<IActionResult> LeaveRmList(LeaveReqGridParams? gridParams = null)
        {
            gridParams ??= new LeaveReqGridParams();

            List<VW_LeaveReqGrid> model = await _dbContext.sp_LeaveReqGrid.FromSqlRaw("sp_LeaveReqGrid @DateFrom={0},@DateTo={1},@LeaveTypeid={2},@UserCode={3},@processId = {4}", gridParams.DateFrom, gridParams.DateTo, gridParams.LeaveTypeid, gridParams.UserCode, gridParams.ProcessID).ToListAsync();
            return Ok(model);
        }
        [HttpPost("getLeaveHrList")]
        public async Task<IActionResult> LeaveHrList(LeaveReqGridParams? gridParams = null)
        {
            gridParams ??= new LeaveReqGridParams();

            List<VW_LeaveReqGrid> model = await _dbContext.sp_LeaveReqGrid.FromSqlRaw("sp_LeaveReqGrid @DateFrom={0},@DateTo={1},@LeaveTypeid={2},@UserCode={3},@processId = {4}", gridParams.DateFrom, gridParams.DateTo, gridParams.LeaveTypeid, gridParams.UserCode, gridParams.ProcessID).ToListAsync();
            return Ok(model);
        }


        [HttpGet("getLeaveType")]
        public async Task<IActionResult> LeaveType()
        {
            var result = await _dbContext.LeaveTypeMaster.ToListAsync();
            return Ok(result);
        }





    }
}
