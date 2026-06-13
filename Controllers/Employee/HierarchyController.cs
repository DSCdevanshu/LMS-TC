using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos.Home;

namespace TCBackend.Controllers.Employee
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HierarchyController : ControllerBase
    {
        private readonly TCDbContext _context;

        public HierarchyController(TCDbContext context)
        {
            _context = context;
        }

        [HttpPost("assign-manager")]
        [HasPermission("employees.update")]
        public async Task<IActionResult> AssignManager([FromBody] ManagerAssignmentDto dto)
        {
            try
            {
                var parameters = new[]
                {
                new SqlParameter("@EmployeeId", dto.EmployeeId),
                new SqlParameter("@ManagerId", dto.ManagerId)
            };

                await _context.Database.ExecuteSqlRawAsync("EXEC sp_AssignManager @EmployeeId, @ManagerId", parameters);
                return Ok(new { Message = "Manager assigned successfully." });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("remove-manager")]
        [HasPermission("employees.update")]
        public async Task<IActionResult> RemoveManager([FromBody] ManagerAssignmentDto dto)
        {
            try
            {
                var parameters = new[]
                {
                new SqlParameter("@EmployeeId", dto.EmployeeId),
                new SqlParameter("@ManagerId", dto.ManagerId)
            };

                await _context.Database.ExecuteSqlRawAsync("EXEC sp_RemoveManager @EmployeeId, @ManagerId", parameters);
                return Ok(new { Message = "Manager removed successfully." });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
