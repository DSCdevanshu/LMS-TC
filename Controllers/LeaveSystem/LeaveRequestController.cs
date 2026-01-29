using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using TCBackend.Data;
using Microsoft.EntityFrameworkCore;
using TCBackend.Model.Employee;
using Microsoft.AspNetCore.Authorization;
using TCBackend.Services;
using System.Diagnostics;
using TCBackend.Dtos.LeaveSystem;
using System.Security.Claims;
using TCBackend.Authorization;
using Dapper;
using TCBackend.Dtos.Wrappers;
using TCBackend.Services.IServices;
namespace TCBackend.Controllers.LeaveSystem
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeaveRequestController : ControllerBase
    {
        private TCDbContext _dbContext;
        private readonly EmailService _emailService;
        private readonly IPermissionService _permissionService;
        public LeaveRequestController(TCDbContext context, EmailService emailService, IPermissionService permissionService)
        {
            _dbContext = context;
            _emailService = emailService;
            _permissionService = permissionService;
        }
        [HttpGet("test")]
        public async Task<IActionResult> test()
        {
            return Ok("Test is working");
        }

        private async Task<IActionResult> changeProcess(int LeaveReqId, int processId,string? remarks="")
        {
            try
            {
                var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();
                var loginUsr = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                if (leave == null)
                {
                    return NotFound("Leave request not found.");
                }
                var record = new LeaveProcessItem
                {
                    TableID = 3,
                    ItemID = LeaveReqId,
                    ProcessID = processId,
                    ProcessBy = loginUsr,
                    ProcessDate = DateTime.Now,
                    Status = "A",
                    CreatedBy = loginUsr,
                    CreatedOn = DateTime.Now,
                    ReasonRemarks = remarks

                };
                await _dbContext.LeaveProcessItem.AddAsync(record);
                leave.ProcessId = processId;
                _dbContext.LeaveRequestMaster.Update(leave);
                await _dbContext.SaveChangesAsync();

                return Ok(new ApiResponse<int>(1, "Success", LeaveReqId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<int>(0, $"Internal Error: {ex.Message}", 0));
            }
        }

        

        [HttpGet("getLeaveHistory/{leaveReqId}")]
        [HasPermission("leaves.view")]
        [ProducesResponseType(typeof(List<LeaveProcessHistory>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLeaveHistory(int leaveReqId)
        {
            try
            {
                var result = await _dbContext.SpLeaveProcessHistoryResults
                    .FromSqlRaw("EXEC sp_GetLeaveProcessHistory @LeaveReqID={0}", leaveReqId)
                    .ToListAsync();

                if (result == null || !result.Any())
                {
                    return Ok(new List<LeaveProcessHistory>());
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("getLeaveRequestDetails/{leaveReqId}")]
        [HasPermission("leaves.view")]
        [ProducesResponseType(typeof(LeaveDetailsResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLeaveRequestDetails(int leaveReqId)
        {
            var response = new LeaveDetailsResponseDto();
            var connection = _dbContext.Database.GetDbConnection();

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@LeaveReqID", leaveReqId);

                using (var multi = await connection.QueryMultipleAsync("sp_GetLeaveRequestDetails", parameters, commandType: CommandType.StoredProcedure))
                {
                    response.Header = await multi.ReadFirstOrDefaultAsync<LeaveRequestHeaderDto>();

                    if (response.Header == null)
                    {
                        return NotFound("Leave Request not found.");
                    }

                    var days = await multi.ReadAsync<LeaveRequestDayDto>();
                    response.Days = days.ToList();
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
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

        [HttpPost("updateLeaveStatus")]
        [HasPermission("leaves.approve")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateLeaveStatus([FromBody] ChangeLeaveProcessDto request)
        {
            // 1. Basic Validation
            if (request.LeaveReqId <= 0 || request.ProcessId <= 0)
            {
                return BadRequest(new ApiResponse<int>(0, "Invalid Data.", 0));
            }

            // 2. (Optional) Role Validation Logic
            // This ensures a Manager cannot perform an "HR Approval" action.
            // RM Actions: 3 (Approve), 4 (Reject), 5 (Hold)
            // HR Actions: 6 (Approve), 7 (Reject), 8 (Hold)

            /* Uncomment this block if you want strict security
            bool isHR = User.IsInRole("HR");
            if ((request.ProcessId >= 6 && request.ProcessId <= 8) && !isHR)
            {
                return StatusCode(403, new ApiResponse<int>(0, "Only HR can perform this action.", 0));
            }
            */
            return await changeProcess(request.LeaveReqId, request.ProcessId, request.Remarks);
        }



        //[HttpPost("postLeaveApprovedByRM")]
        //public async Task<IActionResult> LeaveApprovedByRM(int LeaveReqId)
        //{
        //    var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

        //    if (leave == null)
        //    {
        //        return NotFound("Leave request not found.");
        //    }
        //    return await changeProcess(LeaveReqId, 3);
        //    // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        //}


        //[HttpPost("postLeaveRejectedByRM")]
        //public async Task<IActionResult> LeaveRejectedByRM(int LeaveReqId)
        //{
        //    var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

        //    if (leave == null)
        //    {
        //        return NotFound("Leave request not found.");
        //    }
        //    return await changeProcess(LeaveReqId, 4);
        //    // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        //}


        //[HttpPost("postLeaveHoldByRM")]
        //public async Task<IActionResult> LeaveHoldByRM(int LeaveReqId)
        //{
        //    var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

        //    if (leave == null)
        //    {
        //        return NotFound("Leave request not found.");
        //    }
        //    return await changeProcess(LeaveReqId, 5);
        //    // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        //}


        //[HttpPost("postLeaveApprovedByHR")]
        //public async Task<IActionResult> LeaveApprovedByHR(int LeaveReqId)
        //{
        //    var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

        //    if (leave == null)
        //    {
        //        return NotFound("Leave request not found.");
        //    }
        //    return await changeProcess(LeaveReqId, 6);
        //    // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        //}



        //[HttpPost("postLeaveRejectedByHR")]
        //public async Task<IActionResult> LeaveRejectedByHR(int LeaveReqId)
        //{
        //    var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

        //    if (leave == null)
        //    {
        //        return NotFound("Leave request not found.");
        //    }
        //    return await changeProcess(LeaveReqId, 7);
        //    // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        //}



        //[HttpPost("postLeaveHoldByHR")]
        //public async Task<IActionResult> LeaveHoldByHR(int LeaveReqId)
        //{
        //    var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

        //    if (leave == null)
        //    {
        //        return NotFound("Leave request not found.");
        //    }
        //    return await changeProcess(LeaveReqId, 8);
        //    // await _emailService.SendEmailAsync("devanshu.singh@colorplast.in", "devanshu.singh@colorplast.in", "Leave Request", $"Please approve my leave.");
        //}





        [HttpPost("postLeaveReqEntry")]
        [HasPermission("leaves.create")]
        public async Task<IActionResult> SubmitLeaveRequest([FromBody] CreateLeaveRequestDto requestDto)
        {
            var loginUsr = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userIdString = (requestDto.UserId==null || requestDto.UserId == 0)? User.FindFirstValue(ClaimTypes.NameIdentifier):Convert.ToString(requestDto.UserId);
            var empCode =(await _dbContext.vw_EmpList.Where(id => id.UserId == Convert.ToInt64(userIdString)).FirstOrDefaultAsync())?.EmpCode;
            
            var parameters = new[]
            {
                new SqlParameter("@empCode", empCode),
                new SqlParameter("@UserId", userIdString),
                new SqlParameter("@leaveType", requestDto.LeaveTypeId),
                new SqlParameter("@startDate", requestDto.StartDate),
                new SqlParameter("@endDate", requestDto.EndDate),
                new SqlParameter("@loginUsr", loginUsr),
                new SqlParameter("@empRemarks", requestDto.EmpRemarks ?? (object)DBNull.Value)
            };

            try
            {
                var results = await _dbContext.SpLeaveRequestResult
                    .FromSqlRaw("EXEC sp_LeaveReqEntry @empcode,@userId, @leaveType, @startDate, @endDate, @loginUsr, @empRemarks", parameters)
                    .ToListAsync();

                var result = results.FirstOrDefault();

                if (result == null)
                {
                    return StatusCode(500, "An unexpected error occurred.");
                }

                return Ok(new { status = result.ErrorNumber, message = result.ErrorMessage, leaveRequestId = result.LeaveRequestID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An internal server error occurred: {ex.Message}");
            }
        }

        [HttpPost("postLeaveReqEntryV2")]
        [HasPermission("leaves.create")]
        public async Task<ActionResult<ApiResponse<int>>> SubmitLeaveRequestV2([FromBody] CreateLeaveRequestDtoV2 requestDto)
        {
            try
            {
                var loginUsrId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var targetUserId = (requestDto.UserId == 0 || requestDto.UserId==null) ? loginUsrId : requestDto.UserId;

                var empData = await _dbContext.vw_EmpList
                    .Where(x => x.UserId == targetUserId)
                    .FirstOrDefaultAsync();

                if (empData == null)
                    return BadRequest(new ApiResponse<int>(0, "Employee not found.", 0));

                int reqIdToPass = requestDto.LeaveRequestId ?? 0;
                var parameters = new[]
                {
                    new SqlParameter("@leavereqId", reqIdToPass),
                    new SqlParameter("@empCode", empData.EmpCode),
                    new SqlParameter("@UserId", targetUserId),
                    new SqlParameter("@leaveType", requestDto.LeaveTypeId),
                    new SqlParameter("@startDate", requestDto.StartDate),
                    new SqlParameter("@endDate", requestDto.EndDate),
                    new SqlParameter("@loginUsr", loginUsrId.ToString()),
                    new SqlParameter("@empRemarks", (object)requestDto.EmpRemarks ?? DBNull.Value)
                };

                var results = await _dbContext.SpLeaveRequestResultsV2
                    .FromSqlRaw("EXEC sp_LeaveReqEntry_V2 @leavereqId, @empcode, @userId, @leaveType, @startDate, @endDate, @loginUsr, @empRemarks", parameters)
                    .ToListAsync();

                var result = results.FirstOrDefault();

                if (result == null)
                    return StatusCode(500, new ApiResponse<object>(0, "No response from database.", new { leaveReqId = 0 }));

                return Ok(new ApiResponse<object>(result.Status, result.Message, new { leaveReqId = result.LeaveReqID ?? 0 }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Internal server error: {ex.Message}", new { leaveReqId = 0 }));
            }
        }



        //[HttpPost("postLeaveReqEntry")]
        //public async Task<IActionResult> LeaveReqEntry(string empCode, int leaveType, DateTime startDate, DateTime endDate, string empRemarks)
        //{
        //    // Validate inputs
        //    if (string.IsNullOrWhiteSpace(empCode) || empCode.Length > 10)
        //    {
        //        return BadRequest("Invalid employee code.");
        //    }
        //    if (leaveType <= 0)
        //    {
        //        return BadRequest("Invalid leave type.");
        //    }
        //    if (startDate > endDate)
        //    {
        //        return BadRequest("Start date must be before end date.");
        //    }
        //    if (string.IsNullOrWhiteSpace(empRemarks))
        //    {
        //        empRemarks = null; // Allow null remarks if empty
        //    }

        //    // Get loginUsr from session (or authentication context)
        //    var loginUsr = User.FindFirst("EmployeeCode")?.Value;
        //    var loginDepId = User.FindFirst("EmpDepId")?.Value;

        //    try
        //    {
        //        // Execute the stored procedure with parameters
        //        await _dbContext.Database.ExecuteSqlRawAsync(
        //            "EXEC sp_LeaveReqEntry @empcode, @leaveType, @startDate, @endDate, @loginUsr, @empRemarks",
        //            new SqlParameter("@empcode", empCode),
        //            new SqlParameter("@leaveType", leaveType),
        //            new SqlParameter("@startDate", startDate),
        //            new SqlParameter("@endDate", endDate),
        //            new SqlParameter("@loginUsr", loginUsr),
        //            new SqlParameter("@empRemarks", (object)empRemarks ?? DBNull.Value)
        //        );

        //        return Ok("Success");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error: {ex.Message}");
        //        return StatusCode(500, "An error occurred while processing the leave request.");
        //    }
        //}


        [HttpGet("my-balance")]
        [ProducesResponseType(typeof(LeaveBalanceDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyLeaveBalance()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User ID not found in token.");
            }

            var leaveBalance = await _dbContext.LeaveBalance
                .FirstOrDefaultAsync(lb => lb.UserId == userId);

            if (leaveBalance == null)
            {
                return NotFound("Leave balance not found for this user.");
            }

            var leaveBalanceDto = new LeaveBalanceDto
            {
                UserId = leaveBalance.UserId,
                PaidLeave = leaveBalance.PaidLeave,
                FreeLeave = leaveBalance.FreeLeave,
                ShortLeave = leaveBalance.ShortLeave
            };

            return Ok(leaveBalanceDto);
        }


        [HttpPost("getLeaveUserList")]
        [HasPermission("leaves.view")]
        [ProducesResponseType(typeof(List<VW_LeaveReqGrid>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LeaveUserList([FromBody] LeaveReqGridParams? gridParams)
        {
            gridParams ??= new LeaveReqGridParams();

            try
            {
                var loginUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool hasViewAllPermission = await _permissionService.HasPermissionAsync(loginUserId, "leaves.view_all");

                var parameters = new[]
                {
                    new SqlParameter("@DateFrom", (object?)gridParams.DateFrom ?? DBNull.Value),
                    new SqlParameter("@DateTo",   (object?)gridParams.DateTo   ?? DBNull.Value),
                    new SqlParameter("@LeaveTypeid", gridParams.LeaveTypeid ?? 0),
                    new SqlParameter("@LeaveUserId", gridParams.LeaveUserId ?? 0),
                    new SqlParameter("@depId",       gridParams.DepId ?? 0),
                    new SqlParameter("@ProcessID",   gridParams.ProcessID ?? 0),
                    new SqlParameter("@LoginUserId", loginUserId),
                    new SqlParameter("@ViewAll", hasViewAllPermission ? 1 : 0)
                };

                var model = await _dbContext.sp_LeaveReqGrid
                    .FromSqlRaw("EXEC sp_LeaveReqGrid @DateFrom, @DateTo, @LeaveTypeid, @LeaveUserId, @depId, @ProcessID, @LoginUserId, @ViewAll", parameters)
                    .ToListAsync();

                return Ok(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


        //[HttpPost("getLeaveRmList")]
        //public async Task<IActionResult> LeaveRmList(LeaveReqGridParams? gridParams = null)
        //{
        //    gridParams ??= new LeaveReqGridParams();

        //    List<VW_LeaveReqGrid> model = await _dbContext.sp_LeaveReqGrid.FromSqlRaw("sp_LeaveReqGrid @DateFrom={0},@DateTo={1},@LeaveTypeid={2},@UserCode={3},@processId = {4}", gridParams.DateFrom, gridParams.DateTo, gridParams.LeaveTypeid, gridParams.UserCode, gridParams.ProcessID).ToListAsync();
        //    return Ok(model);
        //}
        //[HttpPost("getLeaveHrList")]
        //public async Task<IActionResult> LeaveHrList(LeaveReqGridParams? gridParams = null)
        //{
        //    gridParams ??= new LeaveReqGridParams();

        //    List<VW_LeaveReqGrid> model = await _dbContext.sp_LeaveReqGrid.FromSqlRaw("sp_LeaveReqGrid @DateFrom={0},@DateTo={1},@LeaveTypeid={2},@UserCode={3},@processId = {4}", gridParams.DateFrom, gridParams.DateTo, gridParams.LeaveTypeid, gridParams.UserCode, gridParams.ProcessID).ToListAsync();
        //    return Ok(model);
        //}


        [HttpGet("getLeaveType")]
        public async Task<IActionResult> LeaveType()
        {
            var result = await _dbContext.LeaveTypeMaster.ToListAsync();
            return Ok(result);
        }

        [HttpGet("getUserCalendarData")]
        [ProducesResponseType(typeof(List<UserCalendarDataDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CalendarData(int month,int year)
        {

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized("User ID not found in token.");
            }
            var model = await _dbContext.sp_GetUserCalendarData.FromSqlRaw("sp_GetUserCalendarData @userid={0},@month={1},@year={2}", userId, month, year).ToListAsync();
            return Ok(model);
        }



    }
}
