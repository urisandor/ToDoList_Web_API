using Microsoft.EntityFrameworkCore;
using ToDoList.Models;

namespace ToDoList.Data
{
    // Ez az osztály a DbContext, ami az adatbázis sémát képviseli.
    public class ApplicationDbContext : DbContext
    {
        // Konstruktor: a konfiguráció (pl. a connection string) átadásához
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet: Ezzel a tulajdonsággal tudja az EF Core, hogy ToDoItem-eket kell tárolnia.
        // A "TodoItems" lesz a tábla neve az adatbázisban.
        public DbSet<TodoItem> TodoItems { get; set; } = default!;
        public DbSet<User> Users { get; set; } = default!;
        
        
    }
}