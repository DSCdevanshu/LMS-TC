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

        [HttpGet]
        [HasPermission("masters.designations.manage")]
        [ProducesResponseType(typeof(ApiResponse<List<DesignationListDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<DesignationListDto>>>> Get()
        {
            try
            {
                var list = await _context.DesignationMaster
                    .OrderBy(d => d.Title)
                    .Select(d => new DesignationListDto
                    {
                        DesignationId = d.DesignationId,
                        Title = d.Title,
                        TotalEmployees = _context.EmpMaster.Count(emp => emp.DesignationId == d.DesignationId)
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<DesignationListDto>>(1, "Success", list));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<DesignationListDto>>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpPost]
        [HasPermission("masters.designations.manage")]
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
        [HasPermission("masters.designations.manage")]
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
        [HasPermission("masters.designations.manage")]
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
