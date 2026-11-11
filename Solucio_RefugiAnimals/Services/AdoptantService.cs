using Solucio_RefugiAnimals.Data;
using Solucio_RefugiAnimals.Models;

public class AdoptantService
{
    private const int MAX_ANIMALS = 3;
    private RefugiContext _context;

    // El context arriba pel constructor: si no s'inicialitza, _context és null i peta (NullReferenceException)
    public AdoptantService(RefugiContext context)
    {
        _context = context;
    }

    // Nivell 1: per mostrar els adoptants, el Program ho demana al servei (com GetTots d'animals)
    public List<Adoptant> GetTots()
    {
        List<Adoptant> llista = _context.Adoptant
            .OrderBy(a => a.AdoptantID)
            .ToList();
        return llista;
    }

    // Nivell 1 · c (+ regla dels 3 animals del nivell 2)
    // Rep el DNI i el CODI del xip (string), no els ID: és el que coneix qui ve al refugi.
    // Cada comprovació, el seu missatge: així al Program es veu quin return false s'ha provat.
    public bool Adoptar(string dni, string codiXip)
    {
        // Busquem per un camp que no és la clau, com a la pista (Nom del curs)
        Adoptant? adoptant = _context.Adoptant.FirstOrDefault(a => a.DNI == dni);
        if (adoptant == null)
        {
            Console.WriteLine($"No es pot adoptar: no hi ha cap adoptant amb el DNI {dni}");
            return false;
        }

        Animal? animal = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXip);
        if (animal == null)
        {
            Console.WriteLine($"No es pot adoptar: no hi ha cap animal amb el xip {codiXip}");
            return false;
        }

        // N'hi ha prou amb EstaAdoptat. Res de animal.Adoptant.Animals.Contains(animal):
        // si l'animal no està adoptat, animal.Adoptant és null i peta (NullReferenceException)
        if (animal.EstaAdoptat)
        {
            Console.WriteLine($"No es pot adoptar: {animal.Nom} ja està adoptat");
            return false;
        }

        // Especie és un enum: es compara amb Especie.GOS.
        // animal.Especie.Equals("GOS") compara un enum amb un string i sempre dona false
        if (animal.Especie == Especie.GOS && !adoptant.TeJardi)
        {
            Console.WriteLine($"No es pot adoptar: {animal.Nom} és un gos i {adoptant.Nom} no té jardí");
            return false;
        }

        // Nivell 2. adoptant.Animals el carrega el lazy loading.
        // També val: _context.Animal.Count(a => a.Adoptant == adoptant) (el recompte el fa la BD)
        if (adoptant.Animals.Count >= MAX_ANIMALS)
        {
            Console.WriteLine($"No es pot adoptar: {adoptant.Nom} ja té {MAX_ANIMALS} animals adoptats");
            return false;
        }

        animal.Adoptant = adoptant;             // relacionar: EF omple la columna AdoptantID
        animal.EstaAdoptat = true;
        animal.DataAdopcio = DateTime.Today;
        _context.SaveChanges();                 // el servei desa: sense això, a la BD no ha canviat res
        Console.WriteLine($"{adoptant.Nom} {adoptant.Cognoms} ha adoptat {animal.Nom}");
        return true;
    }

    // Nivell 2 · d
    public bool Retornar(string codiXip)
    {
        Animal? animal = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXip);
        if (animal == null)
        {
            Console.WriteLine($"No es pot retornar: no hi ha cap animal amb el xip {codiXip}");
            return false;
        }

        // Compte amb la condició: el que NO es pot retornar és un animal que NO està adoptat
        if (!animal.EstaAdoptat)
        {
            Console.WriteLine($"No es pot retornar: {animal.Nom} no està adoptat");
            return false;
        }

        // Llegim l'adoptant (el lazy loading el carrega) abans de treure'l: el fem servir al missatge
        // i així EF té carregada la relació que canviem
        string adoptant;
        if (animal.Adoptant != null)
        {
            adoptant = $"{animal.Adoptant.Nom} {animal.Adoptant.Cognoms}";
        }
        else
        {
            adoptant = "(sense adoptant)";
        }
        

        animal.Adoptant = null;                 // treure la relació: AdoptantID = NULL. No s'esborra res
        animal.EstaAdoptat = false;
        animal.DataAdopcio = null;
        animal.DataEntrada = DateTime.Today;    // torna a entrar al refugi avui
        _context.SaveChanges();
        Console.WriteLine($"{animal.Nom} torna al refugi (l'havia adoptat {adoptant})");
        return true;
    }

    // Nivell 3
    public void LlistatAdoptants(int minAnimals)
    {
        // UNA sola consulta: el filtre i l'ordre els fa la BD amb el recompte de la col·lecció
        // (EF ho tradueix a un SELECT COUNT(*) FROM Animal WHERE AdoptantID = ...).
        // De MÉS a menys: OrderByDescending. I no .ToList() abans del Where: això portaria tots els adoptants a memòria
        List<Adoptant> llista = _context.Adoptant
            .Where(a => a.Animals.Count >= minAnimals)
            .OrderByDescending(a => a.Animals.Count)
            .ToList();

        // Si vols que el recompte també surti de la mateixa consulta (sense lazy loading en el foreach):
        //   .Select(a => new { NomComplet = a.Nom + " " + a.Cognoms, NumAnimals = a.Animals.Count })

        if (llista.Count == 0)
        {
            Console.WriteLine($"Cap adoptant té {minAnimals} animals o més");
            return;
        }
        // La funció és void: ha d'escriure ella per consola (Console, no Debug.WriteLine, que no surt a la consola)
        foreach (Adoptant a in llista)
        {
            Console.WriteLine($"{a.Nom} {a.Cognoms}: {a.Animals.Count} animals");
        }
    }
}
