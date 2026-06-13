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
using TCBackend.Model.LoginSecurity;
using System.Reflection.PortableExecutable;
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
        private readonly IWebHostEnvironment _env;
        private readonly IStorageService _storageService;
        public LeaveRequestController(TCDbContext context, EmailService emailService, IPermissionService permissionService, IWebHostEnvironment env, IStorageService storageService)
        {
            _dbContext = context;
            _emailService = emailService;
            _permissionService = permissionService;
            _env = env;
            _storageService = storageService;
        }
        [HttpGet("test")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> test()
        {
            return Ok(new ApiResponse<string>(1, "Success", "Test is working"));
        }


        private async Task<IActionResult> changeProcess(int LeaveReqId, int processId, string? remarks = "")
        {
            try
            {
                var loginUsr = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                var results = await _dbContext.Database
                    .SqlQueryRaw<ChangeProcessResult>(
                        "EXEC dbo.sp_ChangeLeaveProcess @LeaveReqId={0}, @ProcessId={1}, @LoginUsr={2}, @Remarks={3}",
                        LeaveReqId, processId, loginUsr, remarks ?? (object)DBNull.Value)
                    .ToListAsync();

                var result = results.FirstOrDefault();

                if (result is null || result.Status == 0)
                {
                    var msg = result?.Message ?? "Unknown error";
                    return msg.Contains("not found", StringComparison.OrdinalIgnoreCase)
                        ? NotFound(new ApiResponse<int>(0, msg, 0))
                        : StatusCode(500, new ApiResponse<int>(0, msg, 0));
                }

                return Ok(new ApiResponse<int>(1, "Success", result.LeaveReqID));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<int>(0, $"Internal Error: {ex.Message}", 0));
            }
        }



        [HttpGet("getLeaveHistory/{leaveReqId}")]
        [HasPermission("leaves.read.own")]
        [ProducesResponseType(typeof(ApiResponse<List<LeaveProcessHistory>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLeaveHistory(int leaveReqId)
        {
            try
            {
                var result = await _dbContext.SpLeaveProcessHistoryResults
                    .FromSqlRaw("EXEC sp_GetLeaveProcessHistory @LeaveReqID={0}", leaveReqId)
                    .ToListAsync();

                return Ok(new ApiResponse<List<LeaveProcessHistory>>(1, "Success", result ?? new List<LeaveProcessHistory>()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<LeaveProcessHistory>>(0, $"Internal server error: {ex.Message}", null));
            }
        }
        [HttpGet("getLeaveRequestDetails/{leaveReqId}")]
        [HasPermission("leaves.read.own")]
        [ProducesResponseType(typeof(ApiResponse<LeaveDetailsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
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
                    var header = await multi.ReadFirstOrDefaultAsync<LeaveRequestHeaderDto>();

                    if (header == null)
                    {
                        return NotFound(new ApiResponse<string>(0, "Leave Request not found.", null));
                    }

                    var days = await multi.ReadAsync<LeaveRequestDayDto>();
                    response.Days = days.ToList();

                    header.PhotoUrl = await _storageService.GetSecureFileUrlAsync(header.PhotoUrl);
                    response.Header = header;

                }

                return Ok(new ApiResponse<LeaveDetailsResponseDto>(1, "Success", response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpPost("postLeaveReqSend")]
        [HasPermission("leaves.create")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LeaveReqSend(int LeaveReqId)
        {
            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == LeaveReqId).FirstOrDefaultAsync();

            if (leave == null)
            {
                return NotFound(new ApiResponse<string>(0, "Leave request not found.", null));
            }
            return await changeProcess(LeaveReqId, 2);
        }

        [HttpPost("updateLeaveStatus")]
        [HasPermission("leaves.approve.team")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateLeaveStatus([FromBody] ChangeLeaveProcessDto request)
        {
            if (request.LeaveReqId <= 0 || request.ProcessId <= 0)
            {
                return BadRequest(new ApiResponse<int>(0, "Invalid Data.", 0));
            }

            var leave = await _dbContext.LeaveRequestMaster.Where(id => id.LeaveReqID == request.LeaveReqId).FirstOrDefaultAsync();

            if (leave == null) return NotFound(new ApiResponse<int>(0, "Leave not found", 0));

            var approverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (request.ProcessId == 3)
            {
                bool isAdmin = false;
                bool isAuthorizedManager = await _dbContext.Database
                    .SqlQueryRaw<bool>("SELECT dbo.fn_IsManagerInHierarchy({0}, {1}) AS [Value]", leave.UserId, approverId)
                    .FirstOrDefaultAsync();
                
                var user = await _dbContext.Users.FindAsync(approverId);
                if (user?.IsSuperAdmin == true)
                {
                    isAdmin=true;
                }

                if (!isAuthorizedManager && !isAdmin)
                {
                    return StatusCode(200, new ApiResponse<int>(0, "Access Denied: You are not in the reporting hierarchy for this employee.", 0));
                }
            }
            
            /* 
            bool isHR = User.IsInRole("HR");
            if ((request.ProcessId >= 6 && request.ProcessId <= 8) && !isHR)
            {
                return StatusCode(403, new ApiResponse<int>(0, "Only HR can perform this action.", 0));
            }
            */
            return await changeProcess(request.LeaveReqId, request.ProcessId, request.Remarks);
        }


        [HttpGet("getAvailableActions/{leaveReqId}")]
        [HasPermission("leaves.read.own")]
        [ProducesResponseType(typeof(ApiResponse<List<LeaveActionButtonDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableActions(int leaveReqId)
        {
            try
            {
                var leave = await _dbContext.LeaveRequestMaster
                    .Where(l => l.LeaveReqID == leaveReqId)
                    .Select(l => new { l.UserId, l.ProcessId })
                    .FirstOrDefaultAsync();

                if (leave == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Leave request not found.", null));
                }

                int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool isApplicant = (currentUserId == leave.UserId);
                bool isAdmin = false;

                var user = await _dbContext.Users.FindAsync(currentUserId);
                if (user?.IsSuperAdmin == true)
                {
                    isAdmin = true;
                }
                bool isHR = User.IsInRole("HR") || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

                bool isManager = false;
                if (!isApplicant)
                {
                    isManager = await _dbContext.Database
                        .SqlQueryRaw<bool>("SELECT CAST(dbo.fn_IsManagerInHierarchy({0}, {1}) AS BIT) AS [Value]", leave.UserId, currentUserId)
                        .FirstOrDefaultAsync();
                }

                var buttons = new List<LeaveActionButtonDto>();

                switch (leave.ProcessId)
                {
                    case 1:
                        if (isApplicant)
                        {
                            buttons.Add(new LeaveActionButtonDto { ProcessId = 1, ButtonName = "Edit Request", ColorTheme = "#94a3b8" });
                            buttons.Add(new LeaveActionButtonDto { ProcessId = 2, ButtonName = "Send Request", ColorTheme = "#3b82f6" });
                        }
                        break;

                    case 2:
                        if (isManager || isAdmin)
                        {
                            buttons.Add(new LeaveActionButtonDto { ProcessId = 3, ButtonName = "Approve", ColorTheme = "#22c55e" });
                            buttons.Add(new LeaveActionButtonDto { ProcessId = 4, ButtonName = "Reject", ColorTheme = "#ef4444" });
                            buttons.Add(new LeaveActionButtonDto { ProcessId = 5, ButtonName = "Hold", ColorTheme = "#f59e0b" });
                        }
                        break;

                    case 3:
                        if (isHR)
                        {
                            buttons.Add(new LeaveActionButtonDto { ProcessId = 6, ButtonName = "Final Approve", ColorTheme = "#22c55e" });
                            buttons.Add(new LeaveActionButtonDto { ProcessId = 7, ButtonName = "Reject", ColorTheme = "#ef4444" });
                            buttons.Add(new LeaveActionButtonDto { ProcessId = 8, ButtonName = "Hold", ColorTheme = "#f59e0b" });
                        }
                        break;

                    case 5: // HOLD BY RM
                    case 8: // HOLD BY HR
                        if (isApplicant)
                        {
                            buttons.Add(new LeaveActionButtonDto { ProcessId = 1, ButtonName = "Modify (Move to Draft)", ColorTheme = "#94a3b8" });
                        }
                        break;

                    case 4: // REJECTED BY RM
                    case 6: // APPROVED BY HR (Final)
                    case 7: // REJECTED BY HR
                            // Terminal states - No actions available usually.
                        break;
                }

                return Ok(new ApiResponse<List<LeaveActionButtonDto>>(1, "Success", buttons));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }


        [HttpGet("canCreateForOthers")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CanCreateForOthers()
        {
            try
            {
                int loginUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool hasPermission = await _permissionService.HasPermissionAsync(loginUserId, "leaves.create.all");
                return Ok(new ApiResponse<int>(1, "Success", hasPermission ? 1 : 0));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<int>(0, $"Internal Error: {ex.Message}", 0));
            }
        }

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
                    return StatusCode(500, new ApiResponse<int>(0, "An unexpected error occurred.", 0));
                }

                return Ok(new ApiResponse<object>(result.ErrorNumber ?? 0, result.ErrorMessage, new { leaveRequestId = result.LeaveRequestID }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<int>(0, $"An internal server error occurred: {ex.Message}", 0));
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
        [ProducesResponseType(typeof(ApiResponse<List<UserLeaveBalanceDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyLeaveBalance()
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new ApiResponse<string>(0, "User ID not found in token.", null));
                }

                var leaveBalances = await (from lb in _dbContext.LeaveBalance
                                           join lt in _dbContext.LeaveTypeMaster on lb.LeaveTypeId equals lt.LeaveTypeId
                                           where lb.UserId == userId
                                           select new UserLeaveBalanceDto
                                           {
                                               LeaveTypeId = lb.LeaveTypeId,
                                               LeaveType = lt.LeaveType,
                                               Balance = lb.Balance,
                                               IsUnlimited = lb.IsUnlimited
                                           })
                                           .ToListAsync();

                if (leaveBalances.Count == 0)
                {
                    return NotFound(new ApiResponse<string>(0, "Leave balance not found for this user.", null));
                }

                return Ok(new ApiResponse<List<UserLeaveBalanceDto>>(1, "Success", leaveBalances));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Error: {ex.Message}", null));
            }
        }


        [HttpPost("getLeaveUserList")]
        [HasPermission("leaves.read.own")]
        [ProducesResponseType(typeof(ApiResponse<List<VW_LeaveReqGrid>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LeaveUserList([FromBody] LeaveReqGridParams? gridParams)
        {
            gridParams ??= new LeaveReqGridParams();

            try
            {
                var loginUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                bool hasViewAllPermission = await _permissionService.HasPermissionAsync(loginUserId, "leaves.read.all");

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

                return Ok(new ApiResponse<List<VW_LeaveReqGrid>>(1, "Success", model));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
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
        [ProducesResponseType(typeof(ApiResponse<List<LeaveTypeMaster>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LeaveType()
        {
            try
            {
                var result = await _dbContext.LeaveTypeMaster.ToListAsync();
                return Ok(new ApiResponse<List<LeaveTypeMaster>>(1, "Success", result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Error: {ex.Message}", null));
            }
        }

        [HttpGet("getUserCalendarData")]
        [ProducesResponseType(typeof(ApiResponse<List<UserCalendarDataDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CalendarData(int month,int year)
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new ApiResponse<string>(0, "User ID not found in token.", null));
                }
                var model = await _dbContext.sp_GetUserCalendarData.FromSqlRaw("sp_GetUserCalendarData @userid={0},@month={1},@year={2}", userId, month, year).ToListAsync();
                return Ok(new ApiResponse<List<UserCalendarDataDto>>(1, "Success", model));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Error: {ex.Message}", null));
            }
        }



    }
}
