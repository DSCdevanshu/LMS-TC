using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Security.Claims;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos.Home;
using TCBackend.Dtos.Wrappers;
using TCBackend.Services;
using TCBackend.Services.IServices;

namespace TCBackend.Controllers.Employee
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly TCDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly IWebHostEnvironment _env;
        private readonly IPermissionService _permissionService;
        private readonly IStorageService _storageService;

        public EmployeesController(TCDbContext context, IEncryptionService encryptionService,IWebHostEnvironment env, IPermissionService permissionService, IStorageService storageService)
        {
            _context = context;
            _encryptionService = encryptionService;
            _env = env;
            _permissionService = permissionService;
            _storageService = storageService;
        }

        [HttpPost("CreateEmployee")]
        [HasPermission("employees.create")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<int>>> CreateEmployee([FromForm] CreateEmployeeDto dto)
        {
            if (dto.Photo != null)
            {
                if (dto.Photo.Length > 2 * 1024 * 1024)
                {
                    return BadRequest(new ApiResponse<int>(0, "File size must be less than 2MB.", 0));
                }
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedTypes.Contains(dto.Photo.ContentType.ToLower()))
                {
                    return BadRequest(new ApiResponse<int>(0, "Invalid file type. Only JPG, JPEG, and PNG are allowed.", 0));
                }
            }
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var encryptedPan = dto.PAN == null ? null : _encryptionService.Encrypt(dto.PAN);
                var encryptedAadhaar = dto.AadhaarCard == null ? null : _encryptionService.Encrypt(dto.AadhaarCard);
                var encryptedBankAccount = dto.BankAccountNumber == null ? null : _encryptionService.Encrypt(dto.BankAccountNumber);
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var p = new DynamicParameters();
                p.Add("@Username", dto.Username);
                p.Add("@PasswordHash", passwordHash);
                p.Add("@FirstName", dto.FirstName);
                p.Add("@MiddleName", dto.MiddleName);
                p.Add("@LastName", dto.LastName);
                p.Add("@FathersName", dto.FathersName);
                p.Add("@MothersName", dto.MothersName);
                p.Add("@DateOfBirth", dto.DateOfBirth);
                p.Add("@Gender", dto.Gender);
                p.Add("@Address", dto.Address);
                p.Add("@PhotoUrl", null);
                p.Add("@Email", dto.Email);
                p.Add("@PhoneNumber", dto.PhoneNumber);
                p.Add("@HireDate", dto.HireDate);
                p.Add("@DesignationId", dto.DesignationId);
                p.Add("@DepartmentId", dto.DepartmentId);
                p.Add("@CompanyId", dto.CompanyId);
                p.Add("@LocationId", dto.LocationId);
                p.Add("@CanWorkFromHome", dto.CanWorkFromHome);
                p.Add("@PAN_Encrypted", encryptedPan);
                p.Add("@Aadhaar_Encrypted", encryptedAadhaar);
                p.Add("@BankAccount_Encrypted", encryptedBankAccount);

                p.Add("@NewEmployeeId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@StatusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@Message", dbType: DbType.String, size: 255, direction: ParameterDirection.Output);



                await connection.ExecuteAsync("sp_CreateNewEmployee", p, transaction, commandType: CommandType.StoredProcedure);
                int statusId = p.Get<int>("@StatusId");
                string message = p.Get<string>("@Message");

                if (statusId != 1)
                {
                    transaction.Rollback();
                    return BadRequest(new ApiResponse<int>(0, message, 0));
                }
                int newEmployeeId = p.Get<int>("@NewEmployeeId");

                var newEmpCode = await connection.QuerySingleOrDefaultAsync<string>(
                    "SELECT EmpCode FROM EmpMaster WHERE UserId = @UserId",
                    new { UserId = newEmployeeId },
                    transaction);

                if (dto.Photo != null && dto.Photo.Length > 0)
                {
                    string folderName = $"EmployeePhotos/{newEmployeeId}";
                    string dbPath = await _storageService.SaveFileAsync(dto.Photo, folderName);
                    await connection.ExecuteAsync(
                            "UPDATE EmpMaster SET PhotoUrl = @PhotoUrl WHERE UserId = @UserId",
                            new { PhotoUrl = dbPath, UserId = newEmployeeId },
                            transaction
                        );
                    
                }

                if (dto.ReportingManagerIds != null && dto.ReportingManagerIds.Any())
                {
                    foreach (var managerId in dto.ReportingManagerIds.Distinct())
                    {
                        var mgrParams = new DynamicParameters();
                        mgrParams.Add("@EmployeeId", newEmployeeId);
                        mgrParams.Add("@ManagerId", managerId);
                        mgrParams.Add("@statusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                        mgrParams.Add("@Message", dbType: DbType.String, size: 256, direction: ParameterDirection.Output);

                        await connection.ExecuteAsync("sp_AssignManager", mgrParams, transaction, commandType: CommandType.StoredProcedure);

                        if (mgrParams.Get<int>("@statusId") != 1)
                        {
                            transaction.Rollback();
                            return BadRequest(new ApiResponse<int>(0, $"Manager Assignment Failed: {mgrParams.Get<string>("@Message")}", 0));
                        }
                    }
                }

                transaction.Commit();
                return Ok(new ApiResponse<object>(1, message, new { userId = newEmployeeId, empCode = newEmpCode }));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<int>(0, $"Internal Server Error: {ex.Message}", 0));
            }
        }



        [HttpPut("UpdateEmployee/{employeeId}")]
        [HasPermission("employees.update")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateEmployee(int employeeId, [FromBody] UpdateEmployeeDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var connection = _context.Database.GetDbConnection();
                var dbTransaction = transaction.GetDbTransaction();

                // Pass pre-encrypted values, or null to keep the existing value (SP uses COALESCE/NULLIF).
                var encryptedPan = string.IsNullOrEmpty(dto.PAN) ? null : _encryptionService.Encrypt(dto.PAN);
                var encryptedAadhaar = string.IsNullOrEmpty(dto.AadhaarCard) ? null : _encryptionService.Encrypt(dto.AadhaarCard);
                var encryptedBankAccount = string.IsNullOrEmpty(dto.BankAccountNumber) ? null : _encryptionService.Encrypt(dto.BankAccountNumber);

                var p = new DynamicParameters();
                p.Add("@UserId", employeeId);
                p.Add("@FirstName", dto.FirstName);
                p.Add("@MiddleName", dto.MiddleName);
                p.Add("@LastName", dto.LastName);
                p.Add("@FathersName", dto.FathersName);
                p.Add("@MothersName", dto.MothersName);
                p.Add("@DateOfBirth", dto.DateOfBirth);
                p.Add("@HireDate", dto.HireDate);
                p.Add("@Email", dto.Email);
                p.Add("@Mobile", dto.Mobile);
                p.Add("@Gender", dto.Gender);
                p.Add("@Address", dto.Address);
                p.Add("@DepartmentId", dto.DepartmentId);
                p.Add("@DesignationId", dto.DesignationId);
                p.Add("@CompanyId", dto.CompanyId);
                p.Add("@LocationId", dto.LocationId);
                p.Add("@CanWorkFromHome", dto.CanWorkFromHome);
                p.Add("@PAN_Encrypted", encryptedPan);
                p.Add("@Aadhaar_Encrypted", encryptedAadhaar);
                p.Add("@BankAccount_Encrypted", encryptedBankAccount);
                p.Add("@StatusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@Message", dbType: DbType.String, size: 255, direction: ParameterDirection.Output);

                await connection.ExecuteAsync("sp_UpdateEmployeeDetails", p, dbTransaction, commandType: CommandType.StoredProcedure);

                int statusId = p.Get<int>("@StatusId");
                string message = p.Get<string>("@Message");

                if (statusId != 1)
                {
                    await transaction.RollbackAsync();
                    return statusId == 0
                        ? BadRequest(new ApiResponse<int>(0, message, 0))
                        : StatusCode(500, new ApiResponse<int>(0, message, 0));
                }

                if (dto.ReportingManagerIds != null)
                {
                    var newManagerIds = dto.ReportingManagerIds.Distinct().ToList();

                    var currentManagerIds = await connection.QueryAsync<int>(
                        "SELECT ManagerId FROM ReportingHierarchy WHERE EmployeeId = @EmployeeId",
                        new { EmployeeId = employeeId },
                        dbTransaction);

                    var managersToRemove = currentManagerIds.Except(newManagerIds).ToList();
                    foreach (var rmId in managersToRemove)
                    {
                        await connection.ExecuteAsync("sp_RemoveManager",
                            new { EmployeeId = employeeId, ManagerId = rmId },
                            dbTransaction,
                            commandType: CommandType.StoredProcedure);
                    }

                    var managersToAdd = newManagerIds.Except(currentManagerIds).ToList();
                    foreach (var addId in managersToAdd)
                    {
                        var mgrParams = new DynamicParameters();
                        mgrParams.Add("@EmployeeId", employeeId);
                        mgrParams.Add("@ManagerId", addId);
                        mgrParams.Add("@statusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                        mgrParams.Add("@Message", dbType: DbType.String, size: 256, direction: ParameterDirection.Output);

                        await connection.ExecuteAsync("sp_AssignManager", mgrParams, dbTransaction, commandType: CommandType.StoredProcedure);

                        if (mgrParams.Get<int>("@statusId") != 1)
                        {
                            throw new Exception(mgrParams.Get<string>("@Message"));
                        }
                    }
                }

                await transaction.CommitAsync();
                return Ok(new ApiResponse<int>(1, message, employeeId));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<int>(0, $"Internal Server Error: {ex.Message}", 0));
            }
        }


        [HttpPut("UpdateEmployeePhoto/{employeeId}")]
        [HasPermission("employees.update")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEmployeePhoto(int employeeId, IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
                return BadRequest(new ApiResponse<string>(0, "No photo uploaded.", null));

            if (photo.Length > 2 * 1024 * 1024)
                return BadRequest(new ApiResponse<string>(0, "File size must be less than 2MB.", null));

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
            if (!allowedTypes.Contains(photo.ContentType.ToLower()))
                return BadRequest(new ApiResponse<string>(0, "Invalid file type. Only JPG, JPEG, and PNG are allowed.", null));

            try
            {
                var employee = await _context.EmpMaster.FirstOrDefaultAsync(e => e.UserId == employeeId);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Employee not found.", null));
                }
                string folderName = $"EmployeePhotos/{employeeId}";
                string newDbPath = await _storageService.SaveFileAsync(photo, folderName);

                employee.PhotoUrl = newDbPath;
                await _context.SaveChangesAsync();
                string secureUrl = await _storageService.GetSecureFileUrlAsync(newDbPath);

                return Ok(new ApiResponse<string>(1, "Photo updated successfully.", secureUrl));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }



        [HttpDelete("RemoveEmployeePhoto/{employeeId}")]
        [HasPermission("employees.update")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RemoveEmployeePhoto(int employeeId)
        {
            try
            {
                var employee = await _context.EmpMaster.FirstOrDefaultAsync(e => e.UserId == employeeId);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Employee not found.", null));
                }

                if (string.IsNullOrEmpty(employee.PhotoUrl))
                {
                    return Ok(new ApiResponse<string>(1, "Employee does not have a photo.", null));
                }

                await _storageService.DeleteFileAsync(employee.PhotoUrl);

                employee.PhotoUrl = null;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<string>(1, "Photo removed successfully.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }



        [HttpPost("GetEmployeeList")]
        [HasPermission("employees.read.team")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeGridDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmployeeList([FromBody] EmployeeFilterDto filter)
        {
            try
            {
                int loginUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                bool viewAll = await _permissionService.HasPermissionAsync(loginUserId, "employees.read.all");

                var p = new DynamicParameters();
                p.Add("@LoginUserId", loginUserId);
                p.Add("@ViewAll", viewAll ? 1 : 0);
                p.Add("@SearchText", filter.SearchText);
                p.Add("@DepartmentId", filter.DepartmentId);
                p.Add("@DesignationId", filter.DesignationId);
                p.Add("@Status", filter.Status);
                p.Add("@HireDateFrom", filter.HireDateFrom);
                p.Add("@HireDateTo", filter.HireDateTo);

                var connection = _context.Database.GetDbConnection();
                var list = await connection.QueryAsync<EmployeeGridDto>(
                    "sp_GetEmployeeGrid",
                    p,
                    commandType: CommandType.StoredProcedure
                );

                return Ok(new ApiResponse<IEnumerable<EmployeeGridDto>>(1, "Success", list));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpGet("GetEmployeeById/{employeeId}")]
        [HasPermission("employees.read.team")]
        [ProducesResponseType(typeof(ApiResponse<EmployeeDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmployeeById(int employeeId)
        {
            try
            {
                var employeeView = await _context.EmployeeDetails
                    .FirstOrDefaultAsync(e => e.UserId == employeeId);

                if (employeeView == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Employee not found.", null));
                }

                var dto = new EmployeeDetailsDto
                {
                    UserId = employeeView.UserId,
                    EmpCode = employeeView.EmpCode,
                    FirstName = employeeView.FirstName,
                    MiddleName = employeeView.MiddleName,
                    LastName = employeeView.LastName,
                    FathersName = employeeView.FathersName,
                    MothersName = employeeView.MothersName,
                    EmailID = employeeView.EmailID,
                    Mobile = employeeView.Mobile,
                    DateOfBirth = employeeView.DateofBirth,
                    HireDate = employeeView.HireDate,
                    Gender = employeeView.Gender,
                    Address = employeeView.Address,
                    PhotoUrl = await _storageService.GetSecureFileUrlAsync(employeeView.PhotoUrl),
                    DepartmentId = employeeView.DepartmentId,
                    DepartmentName = employeeView.DepartmentName,
                    DesignationId = employeeView.DesignationId,
                    DesignationName = employeeView.DesignationTitle,
                    CompanyId = employeeView.CompanyId,
                    CompanyName = employeeView.CompanyName,
                    LocationId = employeeView.LocationId,
                    LocationName = employeeView.LocationName,
                    CanWorkFromHome = employeeView.CanWorkFromHome,
                    Status = employeeView.Status,

                    PAN = string.IsNullOrEmpty(employeeView.PAN) ? null : _encryptionService.Decrypt(employeeView.PAN),
                    AadhaarCard = string.IsNullOrEmpty(employeeView.AadhaarCard) ? null : _encryptionService.Decrypt(employeeView.AadhaarCard),
                    BankAccountNumber = string.IsNullOrEmpty(employeeView.BankAccountNumber) ? null : _encryptionService.Decrypt(employeeView.BankAccountNumber),

                    ReportingManagerIds = string.IsNullOrEmpty(employeeView.ManagerIds)
                        ? new List<int>()
                        : employeeView.ManagerIds.Split(',').Select(int.Parse).ToList(),

                    ManagerNames = employeeView.ManagerNames
                };

                return Ok(new ApiResponse<EmployeeDetailsDto>(1, "Success", dto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

        [HttpDelete("deleteEmployee/{employeeId}")]
        [HasPermission("employees.delete")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmployee(int employeeId)
        {
            try
            {
                var employee = await _context.EmpMaster.FirstOrDefaultAsync(e => e.UserId == employeeId);

                if (employee == null)
                {
                    return NotFound(new ApiResponse<string>(0, "Employee not found.", null));
                }

                if (employee.Status == "I")
                {
                    return Ok(new ApiResponse<string>(1, "Employee is already inactive.", null));
                }

                employee.Status = "I";
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<string>(1, "Employee successfully deactivated.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Internal Server Error: {ex.Message}", null));
            }
        }

    }
}
