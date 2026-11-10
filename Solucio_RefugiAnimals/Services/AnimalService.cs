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
}
