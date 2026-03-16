using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos.Management;
using TCBackend.Dtos.Wrappers;
using TCBackend.Model.Employee;

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
        [HasPermission("departments.view")]
        public async Task<ActionResult<ApiResponse<List<Department>>>> GetAll()
        {
            var list = await _context.DepartmentMaster.ToListAsync();
            return Ok(new ApiResponse<List<Department>>(1, "Success", list));
        }

        // CREATE
        [HttpPost]
        [HasPermission("departments.create")]
        public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] DepartmentDto dto)
        {
            try
            {
                // Optional: Check for duplicate Code
                if (await _context.DepartmentMaster.AnyAsync(d => d.DepCode == dto.DepCode))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Department Code '{dto.DepCode}' already exists.", null));
                }

                var department = new Department
                {
                    DepCode = dto.DepCode,
                    DepartmentName = dto.DepartmentName,
                    HOD = dto.HOD
                };

                _context.DepartmentMaster.Add(department);
                await _context.SaveChangesAsync();

                // Return the ID of the created item in Data, or null
                return Ok(new ApiResponse<object>(1, "Department created successfully.", new { id = department.DepId }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }

        // UPDATE
        [HttpPut("{id}")]
        [HasPermission("departments.update")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] DepartmentDto dto)
        {
            try
            {
                var department = await _context.DepartmentMaster.FindAsync(id);

                if (department == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Department not found.", null));
                }

                // Update fields
                department.DepCode = dto.DepCode;
                department.DepartmentName = dto.DepartmentName;
                department.HOD = dto.HOD;

                _context.DepartmentMaster.Update(department);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Department updated successfully.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpDelete("{id}")]
        [HasPermission("departments.delete")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
        {
            try
            {
                var department = await _context.DepartmentMaster.FindAsync(id);

                if (department == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Department not found.", null));
                }

                _context.DepartmentMaster.Remove(department);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Department deleted successfully.", null));
            }
            catch (Exception ex)
            {
                // Handle Foreign Key constraint errors (e.g., if employees are assigned to this dept)
                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    return BadRequest(new ApiResponse<object>(0, "Cannot delete this department because it is assigned to employees.", null));
                }
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }
    }
}
