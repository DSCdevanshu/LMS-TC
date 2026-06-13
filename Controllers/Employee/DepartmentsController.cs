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
        [HasPermission("masters.departments.manage")]
        [ProducesResponseType(typeof(ApiResponse<List<DepartmentListDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<DepartmentListDto>>>> GetAll()
        {
            var list = await (from d in _context.DepartmentMaster
                             join e in _context.EmpMaster on d.HOD equals e.UserId into hod
                             from h in hod.DefaultIfEmpty()
                             select new DepartmentListDto
                             {
                                 DepId = d.DepId,
                                 DepCode = d.DepCode,
                                 DepartmentName = d.DepartmentName,
                                 HOD = d.HOD,
                                 HODName = h == null ? null : (h.FirstName + " " + h.LastName).Trim(),
                                 Status = d.Status,
                                 TotalEmployees = _context.EmpMaster.Count(emp => emp.DepartmentId == d.DepId)
                             }).ToListAsync();
            return Ok(new ApiResponse<List<DepartmentListDto>>(1, "Success", list));
        }

        // CREATE
        [HttpPost]
        [HasPermission("masters.departments.manage")]
        public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] DepartmentDto dto)
        {
            try
            {
                // Optional: Check for duplicate Code
                if (await _context.DepartmentMaster.AnyAsync(d => d.DepCode == dto.DepCode))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Department Code '{dto.DepCode}' already exists.", null));
                }

                if (await _context.DepartmentMaster.AnyAsync(d => d.DepartmentName == dto.DepartmentName))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Department Name '{dto.DepartmentName}' already exists.", null));
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
        [HasPermission("masters.departments.manage")]
        public async Task<ActionResult<ApiResponse<object>>> Update(int id, [FromBody] DepartmentDto dto)
        {
            try
            {
                var department = await _context.DepartmentMaster.FindAsync(id);

                if (department == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Department not found.", null));
                }

                if (await _context.DepartmentMaster.AnyAsync(d => d.DepCode == dto.DepCode && d.DepId != id))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Department Code '{dto.DepCode}' already exists.", null));
                }

                if (await _context.DepartmentMaster.AnyAsync(d => d.DepartmentName == dto.DepartmentName && d.DepId != id))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Department Name '{dto.DepartmentName}' already exists.", null));
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
        [HasPermission("masters.departments.manage")]
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
