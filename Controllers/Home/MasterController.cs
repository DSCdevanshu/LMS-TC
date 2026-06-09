using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos.Masters;
using TCBackend.Dtos.Wrappers;
using TCBackend.Model.Masters;

namespace TCBackend.Controllers.Home
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MasterController : ControllerBase
    {
        private readonly TCDbContext _context;
        public MasterController(TCDbContext context)
        {
            _context = context;
        }

        // ==================== LOCATION CRUD ====================

        [HttpGet("locations")]
        [HasPermission("masters.locations.manage")]
        [ProducesResponseType(typeof(ApiResponse<List<LocationListDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<LocationListDto>>>> GetAllLocations()
        {
            try
            {
                var list = await _context.LocationMaster
                    .Select(l => new LocationListDto
                    {
                        LocationId = l.LocationId,
                        LocationName = l.LocationName,
                        State = l.State,
                        Country = l.Country,
                        Coordinates = l.Coordinates,
                        TotalEmployees = _context.EmpMaster.Count(e => e.LocationId == l.LocationId)
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<LocationListDto>>(1, "Success", list));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpPost("locations")]
        [HasPermission("masters.locations.manage")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> CreateLocation([FromBody] LocationDto dto)
        {
            try
            {
                if (await _context.LocationMaster.AnyAsync(l => l.LocationName == dto.LocationName))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Location '{dto.LocationName}' already exists.", null));
                }

                var location = new LocationMaster
                {
                    LocationName = dto.LocationName,
                    State = dto.State,
                    Country = dto.Country,
                    Coordinates = dto.Coordinates
                };

                _context.LocationMaster.Add(location);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Location created successfully.", new { id = location.LocationId }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpPut("locations/{id}")]
        [HasPermission("masters.locations.manage")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> UpdateLocation(int id, [FromBody] LocationDto dto)
        {
            try
            {
                var location = await _context.LocationMaster.FindAsync(id);

                if (location == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Location not found.", null));
                }

                if (await _context.LocationMaster.AnyAsync(l => l.LocationName == dto.LocationName && l.LocationId != id))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Location '{dto.LocationName}' already exists.", null));
                }

                location.LocationName = dto.LocationName;
                location.State = dto.State;
                location.Country = dto.Country;
                location.Coordinates = dto.Coordinates;

                _context.LocationMaster.Update(location);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Location updated successfully.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpDelete("locations/{id}")]
        [HasPermission("masters.locations.manage")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteLocation(int id)
        {
            try
            {
                var location = await _context.LocationMaster.FindAsync(id);

                if (location == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Location not found.", null));
                }

                _context.LocationMaster.Remove(location);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Location deleted successfully.", null));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    return BadRequest(new ApiResponse<object>(0, "Cannot delete this location because it is assigned to employees.", null));
                }
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }

        // ==================== COMPANY CRUD ====================

        [HttpGet("companies")]
        [HasPermission("masters.companies.manage")]
        [ProducesResponseType(typeof(ApiResponse<List<CompanyListDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<List<CompanyListDto>>>> GetAllCompanies()
        {
            try
            {
                var list = await _context.CompanyMaster
                    .Select(c => new CompanyListDto
                    {
                        CompanyId = c.CompanyId,
                        CompanyName = c.CompanyName,
                        CompanyCode = c.CompanyCode,
                        TotalEmployees = _context.EmpMaster.Count(e => e.CompanyId == c.CompanyId)
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<CompanyListDto>>(1, "Success", list));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpPost("companies")]
        [HasPermission("masters.companies.manage")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> CreateCompany([FromBody] CompanyDto dto)
        {
            try
            {
                if (await _context.CompanyMaster.AnyAsync(c => c.CompanyCode == dto.CompanyCode))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Company Code '{dto.CompanyCode}' already exists.", null));
                }

                if (await _context.CompanyMaster.AnyAsync(c => c.CompanyName == dto.CompanyName))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Company Name '{dto.CompanyName}' already exists.", null));
                }

                var company = new CompanyMaster
                {
                    CompanyName = dto.CompanyName,
                    CompanyCode = dto.CompanyCode
                };

                _context.CompanyMaster.Add(company);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Company created successfully.", new { id = company.CompanyId }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpPut("companies/{id}")]
        [HasPermission("masters.companies.manage")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> UpdateCompany(int id, [FromBody] CompanyDto dto)
        {
            try
            {
                var company = await _context.CompanyMaster.FindAsync(id);

                if (company == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Company not found.", null));
                }

                if (await _context.CompanyMaster.AnyAsync(c => c.CompanyCode == dto.CompanyCode && c.CompanyId != id))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Company Code '{dto.CompanyCode}' already exists.", null));
                }

                if (await _context.CompanyMaster.AnyAsync(c => c.CompanyName == dto.CompanyName && c.CompanyId != id))
                {
                    return BadRequest(new ApiResponse<object>(0, $"Company Name '{dto.CompanyName}' already exists.", null));
                }

                company.CompanyName = dto.CompanyName;
                company.CompanyCode = dto.CompanyCode;

                _context.CompanyMaster.Update(company);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Company updated successfully.", null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }

        [HttpDelete("companies/{id}")]
        [HasPermission("masters.companies.manage")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteCompany(int id)
        {
            try
            {
                var company = await _context.CompanyMaster.FindAsync(id);

                if (company == null)
                {
                    return NotFound(new ApiResponse<object>(0, "Company not found.", null));
                }

                _context.CompanyMaster.Remove(company);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>(1, "Company deleted successfully.", null));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    return BadRequest(new ApiResponse<object>(0, "Cannot delete this company because it is assigned to employees.", null));
                }
                return StatusCode(500, new ApiResponse<object>(0, $"Error: {ex.Message}", null));
            }
        }
    }
}
