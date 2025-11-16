using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoList.Data; // Szükséges a DbContext-hez
using ToDoList.Models; // Szükséges a TodoItem-hez
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ToDoList.Controllers
{
    // [ApiController] jelzi, hogy ez egy API kontroller
    // [Route("api/[controller]")] beállítja az útvonalat. 
    // Mivel a kontroller neve "TodoItemsController", az útvonal "api/TodoItems" lesz.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 3. Ezzel az egész kontrollert "lezárjuk" bejelentkezéshez
    public class TodoItemsController : ControllerBase
    {
        // Változó a DbContext tárolására
        private readonly ApplicationDbContext _context;

        // Konstruktor: Itt kapjuk meg a DbContext-et (Dependency Injection)
        public TodoItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private long GetUserIDFromToken()
        {
            var userIDClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if(userIDClaim == null)
            {
                throw new Exception("Felhasználói azonosító nem található a tokenben.");
            }

            return long.Parse(userIDClaim.Value);   

        }


        // GET: api/TodoItems
        // Ez a metódus fog lefutni, amikor az Angular a getTodos()-t hívja
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems()
        {

            var userId = GetUserIDFromToken();


            // Visszaadja az összes TodoItem-et a DbContext-en keresztül
            // Aszinkron hívás, hogy ne blokkolja a szervert
            return await _context.TodoItems
                .Where(item => item.UserId == userId) //Fontos SZÛRÉS!
                .ToListAsync();
        }

        public class CreateTodoItemDTO
        {
            public string Name { get; set; } = string.Empty;

        }
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<TodoItem>> PostTodoItem(CreateTodoItemDTO todoDto)
        {
            // 1. Kinyerjük a felhasználó ID-ját a tokenbõl
            var userId = GetUserIDFromToken();

            // 2. Létrehozzuk az új TodoItem-et
            var todoItem = new TodoItem
            {
                Name = todoDto.Name,
                IsComplete = false,
                UserId = userId // 3. Hozzárendeljük a bejelentkezett felhasználóhoz
            };

            _context.TodoItems.Add(todoItem);
            await _context.SaveChangesAsync();

            // Visszaküldjük a létrehozott objektumot
            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(long id)
        {
            var userId = GetUserIDFromToken();
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                return NotFound();
            }

            // Ellenõrizzük, hogy a teendõ az övé-e
            if (todoItem.UserId != userId)
            {
                return Forbid(); // 403-as hiba: Látja, de nincs joga hozzáférni
            }

            return todoItem;
        }
        // Ide jönnek majd a többiek: POST (létrehozás), PUT (frissítés), DELETE (törlés)
        //Delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(long id)
        {
            var userId = GetUserIDFromToken();

            // Megkeressük a teendõt az adatbázisban az ID alapján
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                // Ha nem találjuk, 404-es Not Found hibát küldünk
                return NotFound();
            }

            if (todoItem.UserId != userId)
            {
                return Forbid(); // 403-as hiba: Látja, de nincs joga hozzáférni
            }
             // Ha megvan, eltávolítjuk a DbContext-bõl
             _context.TodoItems.Remove(todoItem);

            // Elmentjük a változásokat az adatbázisba
            await _context.SaveChangesAsync();

            // Visszaküldünk egy 204-es "No Content" választ, ami jelzi a sikeres törlést
            return NoContent();
        }
        // Egy DTO (Data Transfer Object) az IsComplete állapot fogadásához
        public class UpdateTodoStatusDto
        {
            public bool IsComplete { get; set; }
        }

        // PUT: api/TodoItems/5/status
        // Ez a végpont csak az 'IsComplete' állapotot frissíti
        [HttpPut("{id}/status")]
        [IgnoreAntiforgeryToken] // Ezt adjuk hozzá itt is, a biztonság kedvéért
        public async Task<IActionResult> UpdateTodoStatus(long id, [FromBody] UpdateTodoStatusDto statusDto)
        {
            // 1. Kinyerjük a felhasználó ID-ját a tokenbõl
            var userId = GetUserIDFromToken();

            // 2. Megkeressük a teendõt
            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null)
            {
                return NotFound("A teendõ nem található.");
            }

            // 3. Ellenõrizzük, hogy a teendõ az övé-e
            if (todoItem.UserId != userId)
            {
                return Forbid("Nincs jogosultsága módosítani ezt a teendõt.");
            }

            // 4. Frissítjük az állapotot
            todoItem.IsComplete = statusDto.IsComplete;

            // 5. Elmentjük a változást az adatbázisba
            await _context.SaveChangesAsync();

            // Visszaküldünk egy "Ok" választ
            return Ok(todoItem); // Visszaküldhetjük a frissített objektumot
        }
    }
}