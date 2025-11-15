using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using ToDoList.Data;
using ToDoList.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer; // JWT-hez
using Microsoft.IdentityModel.Tokens; // JWT-hez
using System.Text; // JWT-hez

namespace ToDoList
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Adatbázis kapcsolat string beolvasása
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // 2. DbContext regisztrálása PostgreSQL-hez
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // --- Szolgáltatások (Services) ---
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            // Swashbuckle (Swagger) regisztrálása
            builder.Services.AddSwaggerGen();

            // 3. JWT Hitelesítés (Authentication) beállítása
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
            });

            // 4. Hitelesítés (Authorization) hozzáadása
            builder.Services.AddAuthorization();

            // 5. CORS beállítása az Angularhoz
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: "AllowAngularApp",
                                  policy =>
                                  {
                                      policy.WithOrigins("http://localhost:4200") // Az Angular app címe
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                                  });
            });

            var app = builder.Build();

            // --- Adatbázis automata létrehozása (a migráció helyett) ---
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    // Létrehozza az adatbázist és sémát, ha még nem létezik
                    context.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Hiba történt az adatbázis létrehozása (EnsureCreated) közben.");
                }
            }
            // --- Adatbázis létrehozás vége ---

            // --- HTTP Pipeline konfiguráció (Middleware) ---
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowAngularApp");

            // FONTOS: Elõbb 'Authentication', utána 'Authorization'!
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}