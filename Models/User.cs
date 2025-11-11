using System.ComponentModel.DataAnnotations; // Ezt érdemes hozzáadni a validációk miatt

namespace ToDoList.Models
{
    public class User
    {
        // Elsõdleges kulcs
        public long Id { get; set; }

        [Required] // Jelzi, hogy ez a mezõ kötelezõ
        [MaxLength(100)] // Maximalizálja a hosszt
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress] // Ellenõrzi, hogy érvényes email formátum-e
        public string Email { get; set; } = string.Empty;

        /**
         * FONTOS: Soha ne tárolj jelszavakat sima szövegként!
         * Ezt a mezõt arra használjuk, hogy a "hash"-elt (titkosított)
         * jelszót tároljuk. A jelszó ellenõrzése késõbb a backenden történik.
         */
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Kapcsolat a TodoItem-ekhez (Egy felhasználónak több teendõje is lehet)
        // Ez egy "navigációs tulajdonság", az EF Core számára kell.
        public ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
    }
}