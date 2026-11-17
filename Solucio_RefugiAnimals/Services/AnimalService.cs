using Solucio_RefugiAnimals.Data;
using Solucio_RefugiAnimals.Models;

public class AnimalService
{
    private RefugiContext _context;

    public AnimalService(RefugiContext context)
    {
        _context = context;
    }

    // Nivell 1: el Program no consulta, ho demana al servei
    public List<Animal> GetTots()
    {
        List<Animal> llista = _context.Animal
            .OrderBy(a => a.AnimalID)
            .ToList();
        return llista;
    }

    // Nivell 2 · a
    public List<Animal> GetDisponibles(Especie especie)
    {
        List<Animal> llista = _context.Animal
            .Where(a => a.Especie == especie && !a.EstaAdoptat)
            .OrderBy(a => a.DataEntrada)        // la data més antiga primer: els que fa més que esperen
            .ToList();
        return llista;
    }

    // Nivell 2 · b, amb la millora del nivell 3 al principi
    public Animal? CercaPerXip(string codiXip)
    {
        // Nivell 3: comprovem el format ABANS d'anar a la BD
        if (codiXip.Length != 15 || !codiXip.All(char.IsDigit))
        {
            Console.WriteLine($"El codi {codiXip} no és vàlid: han de ser 15 dígits");
            return null;
        }

        // a.Xip.Codi dins el FirstOrDefault no és lazy loading: EF ho tradueix a un JOIN
        Animal? animal = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXip);
        if (animal == null)
        {
            Console.WriteLine($"No hi ha cap animal amb el xip {codiXip}");
            return null;
        }
        return animal;
    }

    // Nivell 3
    public List<Animal> CercaPerNom(string text)
    {
        // Així no: a SQLite, Contains es tradueix a instr(), que distingeix majúscules.
        //   _context.Animal.Where(a => a.Nom.Contains(text))           "lu" troba la Lluna però no la Luna
        // Així tampoc: ToList() porta TOTS els animals a memòria i després filtra.
        //   _context.Animal.ToList().Where(a => a.Nom.Contains(text, StringComparison.OrdinalIgnoreCase))

        // Així sí: el lower() el fa la BD i només arriben els que coincideixen
        string cerca = text.ToLower();
        List<Animal> llista = _context.Animal
            .Where(a => a.Nom.ToLower().Contains(cerca))
            .ToList();
        return llista;

        // També val: .Where(a => EF.Functions.Like(a.Nom, $"%{text}%"))
        // (el LIKE de SQLite no distingeix majúscules). Cap dels dos funciona amb accents: À ≠ à.
    }

    // ================================================================
    // P4 · FAMÍLIES
    // ================================================================

    // Nivell 1 · h
    // codiXipMare porta ?: pot arribar null, i null vol dir "deixa'l sense mare", NO "la mare no existeix"
    public bool CanviarMare(string codiXipCria, string? codiXipMare)
    {
        // Primer la cria: si no existeix i no fem return, cria.Mare peta (NullReferenceException)
        Animal? cria = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXipCria);
        if (cria == null)
        {
            Console.WriteLine($"No es pot canviar la mare: no hi ha cap animal amb el xip {codiXipCria}");
            return false;
        }

        // Si codiXipMare és null, NO la busquem: novaMare es queda a null i l'animal quedarà sense mare.
        // Error típic: buscar-la igualment, no trobar-la i retornar false amb "la mare no existeix"
        Animal? novaMare = null;
        if (codiXipMare != null)
        {
            novaMare = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXipMare);
            // Aquí el return false és obligatori: si continues, novaMare.EsFemella peta (NullReferenceException)
            if (novaMare == null)
            {
                Console.WriteLine($"No es pot canviar la mare: no hi ha cap animal amb el xip {codiXipMare}");
                return false;
            }

            // La comprovació de femella va DINS l'if: només té sentit si hi ha mare nova
            if (!novaMare.EsFemella)
            {
                Console.WriteLine($"No es pot canviar la mare: {novaMare.Nom} és mascle");
                return false;
            }
        }

        // Com a la pista. cria.Mare el carrega el lazy loading, i == compara objectes:
        // funciona perquè el context només té UN objecte per cada fila de la BD.
        // També cobreix el null: si ja estava sense mare i la hi tornem a treure, null == null i no es fa res.
        // I llegir cria.Mare ABANS de canviar-la fa que EF sàpiga quina relació treu (com a Retornar)
        if (cria.Mare == novaMare)
        {
            if (novaMare == null)
            {
                Console.WriteLine($"{cria.Nom} ja estava sense mare");
            }
            else
            {
                Console.WriteLine($"{cria.Nom} ja té {novaMare.Nom} de mare");
            }
            return false;
        }

        cria.Mare = novaMare;       // si novaMare és null, MareID = NULL: es queda sense mare. No s'esborra res
        _context.SaveChanges();     // sense això, a la BD no ha canviat res

        if (novaMare == null)
        {
            Console.WriteLine($"{cria.Nom} es queda sense mare");
        }
        else
        {
            Console.WriteLine($"{cria.Nom} ara és cria de {novaMare.Nom}");
        }
        // Deixar-lo sense mare TAMBÉ és un canvi fet: true.
        // Error típic: treure la mare (o no desar-ho) i retornar false
        return true;
    }

    // Nivell 2 · i
    // La cria és un animal NOU: arriba com a objecte i encara no és a la BD.
    // No la busquis a la BD amb First(): no la troba i peta (InvalidOperationException: Sequence contains no elements)
    public bool RegistrarCria(string codiXipMare, Animal cria)
    {
        Animal? mare = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXipMare);
        if (mare == null)
        {
            Console.WriteLine($"No es pot registrar la cria: no hi ha cap animal amb el xip {codiXipMare}");
            return false;
        }

        if (!mare.EsFemella)
        {
            Console.WriteLine($"No es pot registrar la cria: {mare.Nom} és mascle");
            return false;
        }

        // Especie és un enum: es compara amb ==, com a Adoptar.
        // Es COMPROVA, no s'arregla: res de cria.Especie = mare.Especie perquè quadri
        if (cria.Especie != mare.Especie)
        {
            Console.WriteLine($"No es pot registrar la cria: {cria.Nom} és {cria.Especie} i {mare.Nom} és {mare.Especie}");
            return false;
        }

        // Compte amb el sentit: el que NO es pot registrar és una cria nascuda ABANS que la mare (o el mateix dia).
        // Error típic: la condició girada, que rebutja les cries bones i accepta les dolentes
        if (cria.DataNaixement <= mare.DataNaixement)
        {
            Console.WriteLine($"No es pot registrar la cria: {cria.Nom} no ha nascut després de {mare.Nom}");
            return false;
        }

        // L'error més repetit: el xip repetit. Es mira a TOTA la BD (_context.Xip), no només a les cries de la mare.
        // Si no ho comproves, com que el Codi és únic, SaveChanges peta (DbUpdateException: UNIQUE constraint failed).
        // I la cria dolenta es queda pendent al context: el SaveChanges següent tornaria a petar
        string codi = cria.Xip.Codi;
        if (_context.Xip.Any(x => x.Codi == codi))
        {
            Console.WriteLine($"No es pot registrar la cria: ja hi ha un animal amb el xip {codi}");
            return false;
        }

        // Com a les dades del Program: n'hi ha prou amb afegir-la a la col·lecció de la mare.
        // EF fa l'INSERT de la cria i del seu xip i omple MareID. No cal _context.Animal.Add(cria).
        // També val: cria.Mare = mare; _context.Animal.Add(cria);
        mare.Cries.Add(cria);
        _context.SaveChanges();     // sense això, la cria no arriba a la BD
        Console.WriteLine($"S'ha registrat {cria.Nom} com a cria de {mare.Nom}");
        return true;
    }

    // Nivell 3 · j
    public List<Animal> Descendents(string codiXip)
    {
        // La llista es crea UN sol cop, aquí, i es passa a l'auxiliar perquè l'ompli.
        // Error típic: crear-la dins l'auxiliar. Cada crida en fa una de nova, el resultat es perd i surt buida
        List<Animal> descendents = new List<Animal>();

        Animal? animal = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXip);
        if (animal == null)
        {
            Console.WriteLine($"No hi ha cap animal amb el xip {codiXip}");
            return descendents;     // buida, no null: el foreach del Program no peta
        }

        AfegirDescendents(animal, descendents);
        return descendents;
    }

    // L'auxiliar RECURSIVA: rep UN animal (no una llista d'animals) i la llista on va afegint.
    // private: és una peça interna del servei, el Program no la crida
    private void AfegirDescendents(Animal animal, List<Animal> descendents)
    {
        // Cas base: si l'animal no té cries, el foreach no fa cap volta i la recursió s'acaba sola
        foreach (Animal cria in animal.Cries)       // animal.Cries el carrega el lazy loading
        {
            descendents.Add(cria);                  // primer la cria...
            AfegirDescendents(cria, descendents);   // ...i després les seves cries: aquí és on es crida a si mateixa
        }
        // Així NO: recórrer animal.Cries sense la crida recursiva. Surten les cries, però no les cries de les cries
    }
}
