using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Emit;
using System.Security.Claims;
using System.Text;
using TCBackend.Authorization;
using TCBackend.Data;
using TCBackend.Dtos;
using TCBackend.Model;
using TCBackend.Model.LoginSecurity;
using TCBackend.Services.IServices;

namespace TCBackend.Controllers.AuthSec
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _jwtKey;
        private readonly TCDbContext _context;
        private readonly ITokenService _tokenService;

        public LoginController(IConfiguration config,TCDbContext DbContext, ITokenService tokenService)
        {
            _config = config;
            _context = DbContext;
            _jwtKey = config["Jwt:Key"];
            _tokenService = tokenService;
            
        }

        private string GenerateJwtToken(string empCode)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim("EmpCode", empCode)
            };
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // 1. Check if username or email already exists
            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == registerDto.Username || u.Email == registerDto.Email);

            if (existingUser)
            {
                return BadRequest("Username or email is already taken.");
            }

            // 2. Create a new User object
            var user = new User
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                // 3. Hash the password using BCrypt
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password)
            };

            // 4. Add user to the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5. Return a success response
            return Ok(new { Message = "User registered successfully." });
        }

        [HttpPost("postLogin")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                //if (string.IsNullOrWhiteSpace(empCode) || string.IsNullOrWhiteSpace(passwd))
                //{
                //    return BadRequest(new { error = "Employee code and Password are required" });
                //}
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);
                // var user = await _context.vw_EmpList.FirstOrDefaultAsync(usr => usr.EmpCode == empCode);

                if (user == null)
                {
                    return Unauthorized("Invalid username or password.");
                }

                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);

                if (!isPasswordValid && user.IsSuperAdmin)
                {
                    isPasswordValid = (user.PasswordHash == "hashed_password_super"); // Placeholder!
                }

                if (!isPasswordValid)
                {
                    return Unauthorized("Invalid username or password.");
                }

                var token = _tokenService.CreateToken(user);

                return Ok(new { Token = token });

                //var sessionId = Guid.NewGuid().ToString();

                //var claims = new[]
                //{
                //    new Claim(ClaimTypes.Name, user.empname ?? string.Empty),
                //    new Claim("EmployeeCode", user.EmpCode ?? string.Empty),
                //    new Claim("EmpDepId", user.DepId?.ToString() ?? string.Empty),
                //    new Claim("EmpDepartName", user.DepartmentName ?? string.Empty),
                //    new Claim("RM", user.ReportingTo ?? string.Empty),
                //    new Claim("SessionId", sessionId),
                //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                //    new Claim("RefreshCount", "0")
                //};

                //var jwtSettings = _config.GetSection("Jwt");
                //var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
                //var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                //var token = new JwtSecurityToken(
                //    issuer: jwtSettings["Issuer"],
                //    audience: jwtSettings["Audience"],
                //    claims: claims,
                //    expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["TokenExpiryInMinutes"])),
                //    signingCredentials: creds);

                //return Ok(new
                //{
                //    token = new JwtSecurityTokenHandler().WriteToken(token),
                //    sessionId,
                //});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpPost("postRefresh")]
        public IActionResult Refresh(string? token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new { error = "Token is required" });
                }

                // Parse the expired JWT (ignore lifetime validation)
                var jwtSettings = _config.GetSection("Jwt");
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false, // Allow expired tokens
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"])),
                    ClockSkew = TimeSpan.FromMinutes(1)
                }, out SecurityToken validatedToken);

                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken == null)
                {
                    return Unauthorized(new { error = "Invalid token" });
                }

                // Check expiry within grace period
                var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
                if (expClaim == null || !long.TryParse(expClaim, out var expUnix))
                {
                    return Unauthorized(new { error = "Invalid token" });
                }

                var expDate = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                var gracePeriodMinutes = int.Parse(jwtSettings["RefreshGracePeriodMinutes"]);
                var now = DateTime.UtcNow;
                if (expDate < now.AddMinutes(-gracePeriodMinutes) || expDate > now)
                {
                    return Unauthorized(new { error = "Token expired beyond grace period or not yet expired" });
                }

                // Check refresh count
                var refreshCountClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "RefreshCount")?.Value;
                if (!int.TryParse(refreshCountClaim, out var refreshCount) || refreshCount >= 3)
                {
                    return Unauthorized(new { error = "Refresh limit exceeded" });
                }

                // Generate new JWT
                var claims = jwtToken.Claims
                    .Where(c => c.Type != JwtRegisteredClaimNames.Jti && c.Type != "RefreshCount")
                    .ToList();
                claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
                claims.Add(new Claim("RefreshCount", (refreshCount + 1).ToString()));

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var newToken = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(int.Parse(jwtSettings["TokenExpiryInMinutes"])),
                    signingCredentials: creds);

                var newTokenString = tokenHandler.WriteToken(newToken);


                return Ok(new
                {
                    token = newTokenString,
                    sessionId = claims.FirstOrDefault(c => c.Type == "SessionId")?.Value,
                });
            }
            catch (SecurityTokenException ex)
            {
                return Unauthorized(new { error = "Invalid token" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error refreshing token: " + ex.Message });
            }
        }



    }
}
