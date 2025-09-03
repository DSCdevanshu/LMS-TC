using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos.Management;

namespace TCBackend.Controllers.Employee
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly TCDbContext _context;
        public DepartmentsController(TCDbContext context) { _context = context; }

        [HttpGet]
        [HasPermission("departments.read")]
        public async Task<IActionResult> Get()
        {
            var departments = await _context.DepartmentMaster.FromSqlRaw("EXEC sp_GetDepartments").ToListAsync();
            return Ok(departments);
        }

        [HttpPost]
        [HasPermission("departments.create")]
        public async Task<IActionResult> Create([FromBody] DepartmentDto dto)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_CreateDepartment {dto.DepCode}, {dto.DepartmentName}, {dto.HOD}");
            return Ok(new { Message = "Department created." });
        }

        [HttpPut("{id}")]
        [HasPermission("departments.update")]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentDto dto)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_UpdateDepartment {id}, {dto.DepCode}, {dto.DepartmentName}, {dto.HOD}");
            return Ok(new { Message = "Department updated." });
        }

        [HttpDelete("{id}")]
        [HasPermission("departments.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_DeleteDepartment {id}");
            return Ok(new { Message = "Department deleted." });
        }
    }
}
