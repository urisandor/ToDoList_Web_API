using Microsoft.AspNetCore.Mvc;
using ToDoList.Data; // Szükséges a DbContext-hez
using ToDoList.Models; // Szükséges a User modellhez
using Microsoft.EntityFrameworkCore; // Az Aszinkron lekérdezésekhez (AnyAsync, FirstOrDefaultAsync)
using BCrypt.Net; // A jelszó hash-eléshez (BCrypt.Net-Next csomag)

// --- Szükséges using-ok a JWT (Token) generáláshoz ---
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
// -----------------------------------------------------

namespace ToDoList.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration; // Szükséges a titkos kulcs kiolvasásához

        // Konstruktor: Megkapjuk a DbContext-et és a Configuration-t
        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // --- Adatátviteli Objektum (DTO) a Regisztrációhoz ---
        // Ez írja le, milyen adatokat várunk az Angulartól
        public class RegisterDto
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        // --- Adatátviteli Objektum (DTO) a Bejelentkezéshez ---
        public class LoginDto
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }


        // --- REGISZTRÁCIÓS VÉGPONT ---
        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto request)
        {
            // 1. Ellenőrizzük, foglalt-e az email
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest("Ez az email cím már foglalt.");
            }

            // 2. Jelszó hash-elése (titkosítása)
            // Soha ne tárolj sima jelszót!
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 3. Új felhasználó létrehozása
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = passwordHash // A hash-t mentjük el
            };

            // 4. Mentés az adatbázisba
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Sikeres válasz küldése
            return Ok(new { message = "Sikeres regisztráció!" });
        }


        // --- BEJELENTKEZÉSI VÉGPONT ---
        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            // 1. Keressük a felhasználót email alapján
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // 2. Ellenőrzés: Létezik a felhasználó? A jelszó helyes?
            // A BCrypt.Verify hasonlítja össze a kapott jelszót az adatbázisban tárolt hash-sel.
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                // Általános hibaüzenetet küldünk, hogy ne lehessen kitalálni, mi volt a baj.
                return Unauthorized("Hibás email cím vagy jelszó.");
            }

            // 3. Ha minden rendben, generálunk egy JWT tokent
            string token = CreateToken(user);

            // 4. Visszaküldjük a tokent az Angularnak
            return Ok(new { token = token, message = "Sikeres bejelentkezés!" });
        }


        // --- SEGÉDFÜGGVÉNY: TOKEN LÉTREHOZÁSA ---
        private string CreateToken(User user)
        {
            // A token "tartalma", (Payload / Claims)
            // Ezek azok az adatok, amiket a token magában hordoz
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // A felhasználó egyedi azonosítója
                new Claim(ClaimTypes.Name, user.Username), // A felhasználó neve
                new Claim(ClaimTypes.Email, user.Email)
                // Ide lehet tenni jogosultságokat (Roles) is, ha lennének
            };

            // 1. Titkos kulcs kiolvasása az appsettings.json-ből
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key nem található az appsettings.json-ben!");

            // 2. Kulcs átalakítása
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            // 3. Titkosítási algoritmus megadása (ez a legbiztonságosabb)
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // 4. Token leírójának összeállítása
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims), // A token tartalma
                Expires = DateTime.Now.AddDays(1), // Lejárat (pl. 1 nap múlva)
                Issuer = _configuration["Jwt:Issuer"], // Ki adta ki? (Beállítottuk)
                Audience = _configuration["Jwt:Audience"], // Kinek szól? (Beállítottuk)
                SigningCredentials = creds // A titkosítási adatok
            };

            // 5. Token létrehozása és visszaadása string-ként
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}