using Solucio_RefugiAnimals.Data;
using Solucio_RefugiAnimals.Models;
using Microsoft.EntityFrameworkCore;

using var db = new RefugiContext();
db.Database.EnsureDeleted();    // primer esborrar...
db.Database.EnsureCreated();    // ...i després crear, amb les taules noves de cada sessió

// ============ Nivell 1 · Seed ============
// Cada animal porta el seu xip: no cal fer db.Xip.Add, EF segueix la navegació.
// No posem DataImplantacio: surt la d'avui, del constructor.
List<Animal> animals = new List<Animal>()
{
    new Animal() { Nom = "Luna", Especie = Especie.GOS, EsFemella = true, Pes = 22.5,
        DataNaixement = new DateTime(2020, 3, 14), DataEntrada = new DateTime(2025, 2, 10),
        Xip = new Xip() { Codi = "724098100000001" } },
    new Animal() { Nom = "Thor", Especie = Especie.GOS, EsFemella = false, Pes = 31,
        DataNaixement = new DateTime(2019, 7, 2), DataEntrada = new DateTime(2024, 11, 5),
        Xip = new Xip() { Codi = "724098100000002" } },
    new Animal() { Nom = "Kira", Especie = Especie.GOS, EsFemella = true, Pes = 12.8,
        DataNaixement = new DateTime(2023, 1, 20), DataEntrada = new DateTime(2025, 9, 1),
        Xip = new Xip() { Codi = "724098100000003" } },
    // Ja adoptat: per veure que GetDisponibles el deixa fora
    new Animal() { Nom = "Bruc", Especie = Especie.GOS, EsFemella = false, Pes = 18,
        DataNaixement = new DateTime(2021, 5, 9), DataEntrada = new DateTime(2024, 6, 15),
        EstaAdoptat = true, DataAdopcio = new DateTime(2025, 10, 3),
        Xip = new Xip() { Codi = "724098100000004" } },
    new Animal() { Nom = "Mixa", Especie = Especie.GAT, EsFemella = true, Pes = 3.9,
        DataNaixement = new DateTime(2022, 4, 11), DataEntrada = new DateTime(2025, 3, 22),
        Xip = new Xip() { Codi = "724098100000005" } },
    new Animal() { Nom = "Lluna", Especie = Especie.GAT, EsFemella = true, Pes = 4.2,
        DataNaixement = new DateTime(2021, 12, 1), DataEntrada = new DateTime(2024, 12, 30),
        Xip = new Xip() { Codi = "724098100000006" } },
    new Animal() { Nom = "Pelut", Especie = Especie.GAT, EsFemella = false, Pes = 5.6,
        DataNaixement = new DateTime(2018, 8, 17), DataEntrada = new DateTime(2025, 7, 7),
        Xip = new Xip() { Codi = "724098100000007" } },
    new Animal() { Nom = "Floc", Especie = Especie.CONILL, EsFemella = false, Pes = 1.8,
        DataNaixement = new DateTime(2024, 2, 3), DataEntrada = new DateTime(2025, 5, 12),
        Xip = new Xip() { Codi = "724098100000008" } },
    new Animal() { Nom = "Piu", Especie = Especie.AU, EsFemella = true, Pes = 0.3,
        DataNaixement = new DateTime(2023, 9, 25), DataEntrada = new DateTime(2025, 8, 19),
        Xip = new Xip() { Codi = "724098100000009" } },
};
db.Animal.AddRange(animals);
db.SaveChanges();               // fins aquí no s'ha fet cap INSERT

AnimalService animalService = new AnimalService(db);

Console.WriteLine("--- Tots els animals ---");
foreach (Animal a in animalService.GetTots())
{
    Console.WriteLine(a);
}

// ============ Nivell 2 ============
Console.WriteLine("\n--- Gossos disponibles (el Bruc no hi surt) ---");
foreach (Animal a in animalService.GetDisponibles(Especie.GOS))
{
    Console.WriteLine(a);
}

Console.WriteLine("\n--- ALTRES disponibles ---");
List<Animal> altres = animalService.GetDisponibles(Especie.ALTRES);
if (altres.Count == 0)
{
    Console.WriteLine("No hi ha cap animal disponible d'aquesta espècie");
}

Console.WriteLine("\n--- Cerca per xip ---");
Animal? trobat = animalService.CercaPerXip("724098100000003");
if (trobat != null)
{
    Console.WriteLine(trobat);
}
animalService.CercaPerXip("724098100000999");   // format bo, però no existeix

// ============ Nivell 3 ============
Console.WriteLine("\n--- Cerca per xip, codis mal formats ---");
animalService.CercaPerXip("12345");             // massa curt
animalService.CercaPerXip("72409810000000A");   // 15 caràcters, però amb una lletra

Console.WriteLine("\n--- Cerca per nom ---");
List<string> textos = new List<string>() { "lu", "LU", "Lu" };
foreach (string text in textos)
{
    Console.WriteLine($"\"{text}\":");
    foreach (Animal a in animalService.CercaPerNom(text))
    {
        Console.WriteLine($"   {a}");
    }
}

// ============ Extra · CHECK a la BD ============
Console.WriteLine("\n--- Restriccions CHECK ---");
List<Animal> dolents = new List<Animal>()
{
    new Animal() { Nom = "Gegant", Especie = Especie.GOS, EsFemella = false, Pes = 150,     // pes > 100
        DataNaixement = new DateTime(2020, 1, 1), Xip = new Xip() { Codi = "724098100000010" } },
    new Animal() { Nom = "Curt", Especie = Especie.GAT, EsFemella = true, Pes = 4,          // codi de 5
        DataNaixement = new DateTime(2020, 1, 1), Xip = new Xip() { Codi = "12345" } },
    new Animal() { Nom = "Lletra", Especie = Especie.GAT, EsFemella = true, Pes = 4,        // 15, però amb una lletra
        DataNaixement = new DateTime(2020, 1, 1), Xip = new Xip() { Codi = "72409810000001A" } },
};
foreach (Animal dolent in dolents)
{
    try
    {
        db.Animal.Add(dolent);
        db.SaveChanges();
        Console.WriteLine($"{dolent.Nom}: s'ha desat (no hauria!)");
    }
    catch (DbUpdateException e)
    {
        // El missatge de SQLite és a la InnerException: "CHECK constraint failed: CK_..."
        Console.WriteLine($"{dolent.Nom}: no s'ha desat · {e.InnerException?.Message}");
        db.ChangeTracker.Clear();   // si no, l'animal dolent continua pendent i el següent SaveChanges tornaria a petar
    }
}
