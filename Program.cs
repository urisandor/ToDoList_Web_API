using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using ToDoList.Data;
using ToDoList.Models;

namespace ToDoList
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            // Add services to the container.
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

            // --- (Services) ---
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: "AllowAngularApp",
                                  policy =>
                                  {
                                      policy.WithOrigins("http://localhost:4200") // Az Angular app alapértelmezett címe
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                                  });
            });

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    // Kérjük el a DbContext-et
                    var context = services.GetRequiredService<ApplicationDbContext>();

                    // Ez a parancs ellenõrzi, és ha kell, létrehozza az adatbázist a modelljeink alapján
                    context.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    // Hiba esetén írjuk ki a konzolra, hogy mi történt
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Hiba történt az adatbázis létrehozása (EnsureCreated) közben.");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                
            }

            app.UseHttpsRedirection();

            app.UseCors("AllowAngularApp");

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
