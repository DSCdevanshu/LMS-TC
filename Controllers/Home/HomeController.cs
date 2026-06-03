using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos.Home;
using TCBackend.Dtos.Wrappers;
using TCBackend.Services;
using TCBackend.Services.IServices;

namespace TCBackend.Controllers.Home
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HomeController : ControllerBase
    {
        private readonly TCDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly IWebHostEnvironment _env;
        private readonly IStorageService _storageService;

        public HomeController(TCDbContext context, IWebHostEnvironment env, IEncryptionService encryptionService, IStorageService storageService)
        {
            _context = context;
            _env = env;
            _encryptionService = encryptionService;
            _storageService = storageService;
        }


        [HttpGet("getmydetails")]
        [HasPermission("employees.read.own")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDetailView>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyDetails()
        {
            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out var userId))
                {
                    return Unauthorized(new ApiResponse<string>(0, "Invalid user token.", null));
                }

                var myDetails = await _context.EmployeeDetails
                    .FirstOrDefaultAsync(emp => emp.UserId == userId);

                if (myDetails == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Your employee details could not be found.", null));
                }

                if (!string.IsNullOrEmpty(myDetails.PAN))
                    myDetails.PAN = _encryptionService.Decrypt(myDetails.PAN);

                if (!string.IsNullOrEmpty(myDetails.AadhaarCard))
                    myDetails.AadhaarCard = _encryptionService.Decrypt(myDetails.AadhaarCard);

                if (!string.IsNullOrEmpty(myDetails.BankAccountNumber))
                    myDetails.BankAccountNumber = _encryptionService.Decrypt(myDetails.BankAccountNumber);
                myDetails.PhotoUrl = await _storageService.GetSecureFileUrlAsync(myDetails.PhotoUrl);
                return Ok(new ApiResponse<EmployeeDetailView>(1, "Success", myDetails));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

       

        [HttpGet("GetDropdownData")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<GenericDropdownDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDropdownData([FromQuery] string flag, [FromQuery] string? id = null, [FromQuery] string? others = null)
        {
            if (string.IsNullOrEmpty(flag))
            {
                return BadRequest(new ApiResponse<string>(0, "A 'flag' parameter is required.", null));
            }

            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var p = new DynamicParameters();
                p.Add("@Flag", flag);
                p.Add("@LoginUserId", userId);
                p.Add("@Id", id);
                p.Add("@Others", others);
                var connection = _context.Database.GetDbConnection();
                var results = await connection.QueryAsync<GenericDropdownDto>(
                    "sp_GetDropdownData",
                    p,
                    commandType: CommandType.StoredProcedure
                );

                    return Ok(new ApiResponse<IEnumerable<GenericDropdownDto>>(1, "Success", results));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }
    }
}
