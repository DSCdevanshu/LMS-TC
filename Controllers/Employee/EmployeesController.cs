using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos.Home;
using TCBackend.Dtos.Wrappers;
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

        public EmployeesController(TCDbContext context, IEncryptionService encryptionService,IWebHostEnvironment env)
        {
            _context = context;
            _encryptionService = encryptionService;
            _env = env;
        }

        [HttpPost("CreateEmployee")]
        [HasPermission("employees.create")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
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
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var encryptedPan = dto.PAN == null ? null : _encryptionService.Encrypt(dto.PAN);
                var encryptedAadhaar = dto.AadhaarCard == null ? null : _encryptionService.Encrypt(dto.AadhaarCard);
                var encryptedBankAccount = dto.BankAccountNumber == null ? null : _encryptionService.Encrypt(dto.BankAccountNumber);
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var newEmployeeIdParam = new SqlParameter("@NewEmployeeId", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
                var statusIdParam = new SqlParameter("@StatusId", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
                var messageParam = new SqlParameter("@Message", System.Data.SqlDbType.NVarChar, 255) { Direction = System.Data.ParameterDirection.Output };

                var createParams = new List<SqlParameter>
                    {
                        new SqlParameter("@Username", dto.Username),
                        new SqlParameter("@PasswordHash", passwordHash),
                        new SqlParameter("@EmpCode", dto.EmpCode),
                        new SqlParameter("@FirstName", dto.FirstName),
                        new SqlParameter("@MiddleName", (object)dto.MiddleName ?? DBNull.Value),
                        new SqlParameter("@LastName", dto.LastName),
                        new SqlParameter("@FathersName", (object)dto.FathersName ?? DBNull.Value),
                        new SqlParameter("@MothersName", (object)dto.MothersName ?? DBNull.Value),
                        new SqlParameter("@DateOfBirth", dto.DateOfBirth),
                        new SqlParameter("@Gender", dto.Gender),
                        new SqlParameter("@Address", (object)dto.Address ?? DBNull.Value),
                        new SqlParameter("@PhotoUrl", DBNull.Value),
                        new SqlParameter("@Email", dto.Email),
                        new SqlParameter("@PhoneNumber", (object)dto.PhoneNumber ?? DBNull.Value),
                        new SqlParameter("@HireDate", dto.HireDate),
                        new SqlParameter("@DesignationId", dto.DesignationId),
                        new SqlParameter("@DepartmentId", dto.DepartmentId),
                        new SqlParameter("@PAN_Encrypted", (object)encryptedPan ?? DBNull.Value),
                        new SqlParameter("@Aadhaar_Encrypted", (object)encryptedAadhaar ?? DBNull.Value),
                        new SqlParameter("@BankAccount_Encrypted", (object)encryptedBankAccount ?? DBNull.Value),
                        newEmployeeIdParam,
                        statusIdParam,
                        messageParam
                    };

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_CreateNewEmployee @Username, @PasswordHash, @EmpCode, @FirstName, @MiddleName, @LastName, @FathersName, @MothersName, @DateOfBirth, @Gender, @Address, @PhotoUrl, @Email, @PhoneNumber, @HireDate, @DesignationId, @DepartmentId, @PAN_Encrypted, @Aadhaar_Encrypted, @BankAccount_Encrypted, @NewEmployeeId OUT, @StatusId OUT, @Message OUT",
                    createParams
                );

                if ((int)statusIdParam.Value != 1)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new ApiResponse<int>(0, messageParam.Value.ToString(), 0));
                }
                int newEmployeeId = (int)newEmployeeIdParam.Value;


                if (dto.Photo != null && dto.Photo.Length > 0)
                {
                    try
                    {
                        string folderPath = Path.Combine(_env.WebRootPath, "EmployeePhotos", newEmployeeId.ToString());

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Photo.FileName);
                        string fullPath = Path.Combine(folderPath, uniqueFileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await dto.Photo.CopyToAsync(stream);
                        }
                        string dbPath = $"/EmployeePhotos/{newEmployeeId}/{uniqueFileName}";
                        await _context.Database.ExecuteSqlInterpolatedAsync(
                            $"UPDATE EmpMaster SET PhotoUrl = {dbPath} WHERE UserId = {newEmployeeId}"
                        );
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw new Exception("Failed to upload photo. Employee creation aborted.");
                    }
                }

                if (dto.ReportingManagerId.HasValue)
                {
                    var managerStatusIdParam = new SqlParameter("@statusId", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
                    var managerMessageParam = new SqlParameter("@Message", System.Data.SqlDbType.NVarChar, 256) { Direction = System.Data.ParameterDirection.Output };

                    var assignParams = new[] {
                        new SqlParameter("@EmployeeId", newEmployeeId),
                        new SqlParameter("@ManagerId", dto.ReportingManagerId.Value),
                        managerStatusIdParam,
                        managerMessageParam
                    };

                    await _context.Database.ExecuteSqlRawAsync("EXEC sp_AssignManager @EmployeeId, @ManagerId, @statusId OUT, @Message OUT", assignParams);

                    if ((int)managerStatusIdParam.Value != 1)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { Message = managerMessageParam.Value.ToString() });
                    }
                }

                await transaction.CommitAsync();
                return Ok(new ApiResponse<int>(1, messageParam.Value.ToString(), newEmployeeId));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ApiResponse<int>(0, $"Internal Server Error: {ex.Message}", 0));
            }
        }




        [HttpPut("updateEmployee{employeeId}")]
        [HasPermission("employees.update.all")]
        public async Task<IActionResult> UpdateEmployee(int employeeId, [FromBody] UpdateEmployeeDto dto)
        {
            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@UserId", employeeId),
                    new SqlParameter("@FirstName", dto.FirstName),
                    new SqlParameter("@LastName", dto.LastName),
                    new SqlParameter("@Email", dto.Email),
                    new SqlParameter("@Mobile", (object)dto.Mobile ?? DBNull.Value),
                    new SqlParameter("@DepartmentId", dto.DepartmentId),
                    new SqlParameter("@DesignationId", dto.DesignationId)
                };

                await _context.Database
                    .ExecuteSqlRawAsync("EXEC sp_UpdateEmployeeDetails @EmployeeId, @FirstName, @LastName, @Email, @Mobile, @DepartmentId, @DesignationId", parameters);

                return Ok(new { Message = "Employee details updated successfully." });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "An internal server error occurred.");
            }
        }


        [HttpGet("getAllEmployeeList")]
        [HasPermission("employees.list.all")]
        public async Task<IActionResult> EmployeeList()
        {
            var res = await _context.vw_EmpList.ToListAsync();
            return Ok(res);
        }


        [HttpGet("my-team")]
        public async Task<IActionResult> GetMyTeam()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var userIdParam = new SqlParameter("@TopLevelUserId", userId);

            var teamList = await _context.vw_EmpList
                .FromSqlRaw("EXEC sp_GetEmployeeHierarchy @TopLevelUserId", userIdParam)
                .ToListAsync();

            return Ok(teamList);
        }


        [HttpGet("my-menu")]
        public async Task<IActionResult> GetUserMenu()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var isSuperAdmin = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.IsSuperAdmin)
                .FirstOrDefaultAsync();

            var userPermissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.PermissionId)
                .Distinct()
                .ToListAsync();

            var allMenuItems = await _context.MenuItems
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            var accessibleMenu = allMenuItems.Where(item =>
                    item.RequiredPermissionId == null ||
                    isSuperAdmin ||
                    userPermissions.Contains(item.RequiredPermissionId.Value)
                ).ToList();


            return Ok(accessibleMenu);
        }

    }
}
