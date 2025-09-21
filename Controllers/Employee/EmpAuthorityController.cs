using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos;
using TCBackend.Dtos.Home;
using TCBackend.Model.Employee;
using TCBackend.Model.LoginSecurity;
using TCBackend.Services.IServices;

namespace TCBackend.Controllers.Employee
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class EmpAuthorityController : ControllerBase
    {
        private readonly TCDbContext _context;
        private readonly IEncryptionService _encryptionService;
        public EmpAuthorityController(TCDbContext tCDb, IEncryptionService encryptionService)
        {
            _context = tCDb;
            _encryptionService = encryptionService;
        }

        [HttpGet("getDepartmentList")]
        public async Task<IActionResult> DepartmentList()
        {
            var res = await _context.DepartmentMaster.ToListAsync();
            return Ok(res);
        }

        [HttpGet("getAuthDepartmentList")]
        public async Task<IActionResult> AuthDepartmentList()
        {
            return Ok("Future Updates");
        }
        
        [HttpGet("GetEmployeeDetails")]
        [HasPermission("employees.read.all")]
        public async Task<IActionResult> GetEmployeeDetails()
        {
            var employees = await _context.EmployeeDetails.ToListAsync();
            return Ok(employees);
        }

        


        


    }
}
