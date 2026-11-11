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


// ================================================================
// ============ SESSIÓ 2 · ADOPTANTS ==============================
// ================================================================

// ============ Nivell 1 · Dades ============
// El Bruc ja estava adoptat a P1 (EstaAdoptat = true i DataAdopcio): ara li posem adoptant, la Marta.
// Un animal adoptat ha de tenir adoptant: si no, la Marta no tindria cap gos.
// El llegim de la BD: després del ChangeTracker.Clear() de l'extra, la variable de la llista animals ja no la segueix EF
Animal bruc = db.Animal.First(a => a.Xip.Codi == "724098100000004");

Adoptant marta = new Adoptant() { DNI = "12345678Z", Nom = "Marta", Cognoms = "Puig Soler", TeJardi = true };
Adoptant pau = new Adoptant() { DNI = "87654321X", Nom = "Pau", Cognoms = "Ferrer Vidal", TeJardi = false };
bruc.Adoptant = marta;

// Crear l'objecte no n'hi ha prou: cal afegir-lo al context i desar-lo.
// I ABANS de cridar Adoptar, que busca l'adoptant a la BD.
db.Adoptant.AddRange(marta, pau);
db.SaveChanges();

AdoptantService adoptantService = new AdoptantService(db);

// Una funció local per poder mostrar-ho abans i després de cada canvi
void MostrarAdoptants()
{
    // Ho llegim de la BD amb el servei, no de les variables marta i pau
    foreach (Adoptant ad in adoptantService.GetTots())
    {
        Console.WriteLine(ad);
        if (ad.Animals.Count == 0)
        {
            Console.WriteLine("   (cap animal adoptat)");
        }
        foreach (Animal an in ad.Animals)      // cada adoptant AMB els seus animals
        {
            Console.WriteLine($"   {an}");
        }
    }
}

Console.WriteLine("\n--- Adoptants amb els seus animals ---");
MostrarAdoptants();

// ============ Nivell 1 · Adoptar ============
// Una adopció correcta i un cas per cada return false.
// Cada prova ha de fallar NOMÉS per la condició que vols provar.
Console.WriteLine("\n--- Adoptar ---");
adoptantService.Adoptar("12345678Z", "724098100000002");    // bé: la Marta té jardí i adopta el Thor (gos)
adoptantService.Adoptar("87654321X", "724098100000008");    // bé: el Pau no té jardí, però el Floc és un conill
adoptantService.Adoptar("00000000T", "724098100000005");    // false: no hi ha cap adoptant amb aquest DNI
adoptantService.Adoptar("12345678Z", "724098100000999");    // false: no hi ha cap animal amb aquest xip
adoptantService.Adoptar("12345678Z", "724098100000004");    // false: el Bruc ja està adoptat
adoptantService.Adoptar("87654321X", "724098100000003");    // false: la Kira és un gos i el Pau no té jardí
// Per provar el jardí cal un gos NO adoptat: amb el Bruc fallaria abans per "ja adoptat" i mai arribaria al jardí

// ============ Nivell 2 · Màxim 3 animals ============
// La Marta té el Bruc i el Thor: el 3r entra i el 4t no
Console.WriteLine("\n--- Màxim 3 animals per adoptant ---");
adoptantService.Adoptar("12345678Z", "724098100000005");    // bé: la Mixa, 3r animal de la Marta
adoptantService.Adoptar("12345678Z", "724098100000006");    // false: la Lluna seria el 4t

Console.WriteLine("\n--- Adoptants després de les adopcions ---");
MostrarAdoptants();

// ============ Nivell 2 · Retornar ============
Console.WriteLine("\n--- Retornar ---");
adoptantService.Retornar("724098100000002");    // bé: la Marta retorna el Thor
adoptantService.Retornar("724098100000003");    // false: la Kira no està adoptada
adoptantService.Retornar("724098100000999");    // false: no hi ha cap animal amb aquest xip

Console.WriteLine("\n--- Adoptants després de retornar (la Marta ja no té el Thor) ---");
MostrarAdoptants();

// ============ Nivell 3 · Llistat ============
// Ara la Marta en té 2 (Bruc i Mixa) i el Pau 1 (Floc)
Console.WriteLine("\n--- Adoptants amb 1 animal o més ---");
adoptantService.LlistatAdoptants(1);            // Marta (2) i després Pau (1)
Console.WriteLine("\n--- Adoptants amb 3 animals o més ---");
adoptantService.LlistatAdoptants(3);            // cap
