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
    public class DesignationsController : ControllerBase
    {
        private readonly TCDbContext _context;
        public DesignationsController(TCDbContext context) { _context = context; }

        [HttpGet]
        [HasPermission("designations.read")]
        public async Task<IActionResult> Get()
        {
            var designations = await _context.DesignationMaster.FromSqlRaw("EXEC sp_GetDesignations").ToListAsync();
            return Ok(designations);
        }

        [HttpPost]
        [HasPermission("designations.create")]
        public async Task<IActionResult> Create([FromBody] DesignationDto dto)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_CreateDesignation {dto.Title}");
            return Ok(new { Message = "Designation created." });
        }

        [HttpPut("{id}")]
        [HasPermission("designations.update")]
        public async Task<IActionResult> Update(int id, [FromBody] DesignationDto dto)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_UpdateDesignation {id}, {dto.Title}");
            return Ok(new { Message = "Designation updated." });
        }

        [HttpDelete("{id}")]
        [HasPermission("designations.delete")]
        public async Task<IActionResult> Delete(int id)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_DeleteDesignation {id}");
            return Ok(new { Message = "Designation deleted." });
        }
    }
}
