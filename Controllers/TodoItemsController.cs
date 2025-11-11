using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoList.Data; // Szükséges a DbContext-hez
using ToDoList.Models; // Szükséges a TodoItem-hez

namespace ToDoList.Controllers
{
    // [ApiController] jelzi, hogy ez egy API kontroller
    // [Route("api/[controller]")] beállítja az útvonalat. 
    // Mivel a kontroller neve "TodoItemsController", az útvonal "api/TodoItems" lesz.
    [Route("api/[controller]")]
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        // Változó a DbContext tárolására
        private readonly ApplicationDbContext _context;

        // Konstruktor: Itt kapjuk meg a DbContext-et (Dependency Injection)
        public TodoItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TodoItems
        // Ez a metódus fog lefutni, amikor az Angular a getTodos()-t hívja
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {
            // Visszaadja az összes TodoItem-et a DbContext-en keresztül
            // Aszinkron hívás, hogy ne blokkolja a szervert
            return await _context.TodoItems.ToListAsync();
        }

        // Ide jönnek majd a többiek: POST (létrehozás), PUT (frissítés), DELETE (törlés)
        //Delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(long id)
        {
            // Megkeressük a teendõt az adatbázisban az ID alapján
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                // Ha nem találjuk, 404-es Not Found hibát küldünk
                return NotFound();
            }

            // Ha megvan, eltávolítjuk a DbContext-bõl
            _context.TodoItems.Remove(todoItem);

            // Elmentjük a változásokat az adatbázisba
            await _context.SaveChangesAsync();

            // Visszaküldünk egy 204-es "No Content" választ, ami jelzi a sikeres törlést
            return NoContent();
        }
    }
}