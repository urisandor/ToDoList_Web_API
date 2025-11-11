namespace ToDoList.Models
{
	// Ez az osztály fogja képviselni az adatbázisban egy sor "tennivalót".
	public class TodoItem
	{
		// Az EF Core ezt automatikusan elsõdleges kulcsként (Primary Key) fogja kezelni.
		public long Id { get; set; }

		// A feladat neve vagy leírása
		public string? Name { get; set; } // A '?' jelzi, hogy lehet null (opcionális)
		public string? Description { get; set; }

		// Jelzi, hogy a feladatot befejezték-e
		public bool IsComplete { get; set; }


		// Ez lesz az idegen kulcs (Foreign Key), ami a User táblára mutat
		public long UserId { get; set; }

		// Navigációs tulajdonság a kapcsolódó User objektumhoz
		public User? User { get; set; }
	}
}