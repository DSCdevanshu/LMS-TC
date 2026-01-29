using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos.Management;
using TCBackend.Dtos.Wrappers;

namespace TCBackend.Controllers.Employee
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DesignationsController : ControllerBase
    {
        private readonly TCDbContext _context;
        public DesignationsController(TCDbContext context) { _context = context; }

        //[HttpGet]
        //[HasPermission("designations.read")]
        //public async Task<IActionResult> Get()
        //{
        //    var designations = await _context.DesignationMaster.FromSqlRaw("EXEC sp_GetDesignations").ToListAsync();
        //    return Ok(designations);
        //}

        //[HttpPost]
        //[HasPermission("designations.create")]
        //public async Task<IActionResult> Create([FromBody] DesignationDto dto)
        //{
        //    await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_CreateDesignation {dto.Title}");
        //    return Ok(new { Message = "Designation created." });
        //}

        //[HttpPut("{id}")]
        //[HasPermission("designations.update")]
        //public async Task<IActionResult> Update(int id, [FromBody] DesignationDto dto)
        //{
        //    await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_UpdateDesignation {id}, {dto.Title}");
        //    return Ok(new { Message = "Designation updated." });
        //}

        //[HttpDelete("{id}")]
        //[HasPermission("designations.delete")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    await _context.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_DeleteDesignation {id}");
        //    return Ok(new { Message = "Designation deleted." });
        //}






        [HttpGet]
        [HasPermission("designations.read")]
        [ProducesResponseType(typeof(ApiResponse<List<Designation>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<Designation>>>> Get()
        {
            try
            {
                var list = await _context.DesignationMaster
                    .OrderBy(d => d.Title) // Optional: Sort alphabetically
                    .ToListAsync();

                return Ok(new ApiResponse<List<Designation>>(1, "Success", list));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<Designation>>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpPost]
        [HasPermission("designations.create")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] DesignationDto dto)
        {
            try
            {
                if (await _context.DesignationMaster.AnyAsync(d => d.Title == dto.Title))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Designation '{dto.Title}' already exists.", null));
                }

                var designation = new Designation
                {
                    Title = dto.Title
                };

                _context.DesignationMaster.Add(designation);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Designation created successfully.", new { id = designation.DesignationId }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }

        // UPDATE
        [HttpPut("{id}")]
        [HasPermission("designations.update")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] DesignationDto dto)
        {
            try
            {
                var designation = await _context.DesignationMaster.FindAsync(id);

                if (designation == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Designation not found.", null));
                }

                // Optional: Check for duplicates if name is changing
                if (designation.Title != dto.Title && await _context.DesignationMaster.AnyAsync(d => d.Title == dto.Title))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Designation '{dto.Title}' already exists.", null));
                }

                designation.Title = dto.Title;

                _context.DesignationMaster.Update(designation);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Designation updated successfully.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpDelete("{id}")]
        [HasPermission("designations.delete")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            try
            {
                var designation = await _context.DesignationMaster.FindAsync(id);

                if (designation == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Designation not found.", null));
                }

                // Optional: Check if used in Employee Master before deleting
                // bool isUsed = await _context.EmpMaster.AnyAsync(e => e.DesignationId == id);
                // if (isUsed) return BadRequest(new ApiResponse<object>(0, "Cannot delete: In use by employees.", null));

                _context.DesignationMaster.Remove(designation);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Designation deleted successfully.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }


    }
}
