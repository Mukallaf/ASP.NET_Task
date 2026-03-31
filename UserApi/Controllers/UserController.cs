using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserApi.Data;
using UserApi.DTOs;

namespace UserApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public UsersController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }


        [AllowAnonymous]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers([FromHeader(Name = "X-Admin-Key")] string? adminKey)
        {
            
            var secretKey = _config["AdminSettings:SecretKey"];
            if (adminKey != secretKey)
                return Unauthorized("Admin access only.");

            var users = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone
                })
                .ToListAsync();

            return Ok(new
            {
                totalUsers = users.Count,
                users
            });
        }

        

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone
            });
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

          
            user.FullName = dto.FullName;
            user.Phone = dto.Phone;

           
            if (!string.IsNullOrWhiteSpace(dto.NewEmail) && dto.NewEmail != user.Email)
            {
                
                var emailTaken = await _db.Users.AnyAsync(u => u.Email == dto.NewEmail && u.Id != id);
                if (emailTaken)
                    return BadRequest("Email already in use by another account.");

                user.Email = dto.NewEmail;
            }

            
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                    return BadRequest("Current password is required to set a new password.");

                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                    return BadRequest("Current password is incorrect.");

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully!" });
        }
    }
}