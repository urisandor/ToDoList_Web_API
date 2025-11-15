using Microsoft.AspNetCore.Mvc;
using ToDoList.Data;
using ToDoList.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ToDoList.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Egy "DTO" (Data Transfer Object) osztály, ami meghatározza,
        // milyen adatokat várunk a regisztráció során az Angulartól.
        public class RegisterDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            // 1. Ellenőrizzük, hogy létezik-e már ilyen felhasználó
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Ez az email cím már foglalt.");
            }

            // 2. Hash-eljük a jelszót a BCrypt segítségével
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Létrehozzuk az új User objektumot
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash // A hash-t mentjük el, nem a jelszót!
            };

            // 4. Mentsük a felhasználót az adatbázisba
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Visszaküldünk egy "Ok" választ (később itt küldhetnénk tokent is)
            return Ok(new { message = "Sikeres regisztráció!" });
        }
        
    }
}
