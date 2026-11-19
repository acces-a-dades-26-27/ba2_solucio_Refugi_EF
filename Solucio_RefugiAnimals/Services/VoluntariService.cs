using Solucio_RefugiAnimals.Data;
using Solucio_RefugiAnimals.Models;

public class VoluntariService
{
    private const int MAX_ANIMALS = 4;
    private RefugiContext _context;

    public VoluntariService(RefugiContext context)
    {
        _context = context;
    }

    // Nivell 1: per mostrar els voluntaris amb els seus animals, el Program ho demana al servei
    public List<Voluntari> GetTots()
    {
        List<Voluntari> llista = _context.Voluntari
            .OrderBy(v => v.VoluntariID)
            .ToList();
        return llista;
    }

    // Nivell 1 · e
    // Rep el NOM del voluntari i el CODI del xip, no els ID.
    // Cada comprovació, el seu missatge: així al Program es veu quin return false s'ha provat.
    public bool AssignarAnimal(string nomVoluntari, string codiXip)
    {
        Voluntari? voluntari = _context.Voluntari.FirstOrDefault(v => v.Nom == nomVoluntari);
        if (voluntari == null)
        {
            Console.WriteLine($"No es pot assignar: no hi ha cap voluntari amb el nom {nomVoluntari}");
            return false;
        }

        Animal? animal = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXip);
        if (animal == null)
        {
            Console.WriteLine($"No es pot assignar: no hi ha cap animal amb el xip {codiXip}");
            return false;
        }

        // Un animal adoptat no pot tenir voluntaris
        if (animal.EstaAdoptat)
        {
            Console.WriteLine($"No es pot assignar: {animal.Nom} ja està adoptat");
            return false;
        }

        // voluntari.Animals el carrega el lazy loading.
        // Contains compara objectes: funciona perquè el context només té UN objecte per cada fila de la BD
        if (voluntari.Animals.Contains(animal))
        {
            Console.WriteLine($"No es pot assignar: {voluntari.Nom} ja té cura de {animal.Nom}");
            return false;
        }

        // El màxim de 4 només es comprova en AFEGIR. A DesassignarAnimal no hi va:
        // si hi fos, un voluntari amb 4 animals no en podria treure cap
        if (voluntari.Animals.Count >= MAX_ANIMALS)
        {
            Console.WriteLine($"No es pot assignar: {voluntari.Nom} ja té cura de {MAX_ANIMALS} animals");
            return false;
        }

        // N'hi ha prou amb afegir-lo a UN costat: EF crea la fila a Cura i omple l'altra col·lecció sol.
        // No cal (ni s'ha de) fer també animal.Voluntaris.Add(voluntari)
        voluntari.Animals.Add(animal);
        _context.SaveChanges();     // sense això, a Cura no hi ha cap fila nova: comprovar-ho no és assignar-lo
        Console.WriteLine($"{voluntari.Nom} té cura de {animal.Nom}");
        return true;
    }

    // Nivell 2 · f
    public bool DesassignarAnimal(string nomVoluntari, string codiXip)
    {
        Voluntari? voluntari = _context.Voluntari.FirstOrDefault(v => v.Nom == nomVoluntari);
        if (voluntari == null)
        {
            Console.WriteLine($"No es pot desassignar: no hi ha cap voluntari amb el nom {nomVoluntari}");
            return false;
        }

        Animal? animal = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXip);
        if (animal == null)
        {
            Console.WriteLine($"No es pot desassignar: no hi ha cap animal amb el xip {codiXip}");
            return false;
        }

        if (!voluntari.Animals.Contains(animal))
        {
            Console.WriteLine($"No es pot desassignar: {voluntari.Nom} no té cura de {animal.Nom}");
            return false;
        }

        // Es treu de la COL·LECCIÓ: EF esborra la fila de Cura i l'animal continua a la BD.
        // _context.Animal.Remove(animal) esborraria l'animal, que és just el que no volem
        voluntari.Animals.Remove(animal);
        _context.SaveChanges();
        Console.WriteLine($"{voluntari.Nom} ja no té cura de {animal.Nom}");
        return true;
    }

    // Nivell 3 · g
    public bool RegistrarPasseig(string nomVoluntari, string codiXip, int minuts)
    {
        // Primer comprovar que existeixen: si no, voluntari.Animals peta (NullReferenceException)
        Voluntari? voluntari = _context.Voluntari.FirstOrDefault(v => v.Nom == nomVoluntari);
        if (voluntari == null)
        {
            Console.WriteLine($"No es pot registrar el passeig: no hi ha cap voluntari amb el nom {nomVoluntari}");
            return false;
        }

        Animal? animal = _context.Animal.FirstOrDefault(a => a.Xip.Codi == codiXip);
        if (animal == null)
        {
            Console.WriteLine($"No es pot registrar el passeig: no hi ha cap animal amb el xip {codiXip}");
            return false;
        }

        if (!voluntari.Animals.Contains(animal))
        {
            Console.WriteLine($"No es pot registrar el passeig: {voluntari.Nom} no té cura de {animal.Nom}");
            return false;
        }

        // Especie és un enum: es compara amb Especie.GOS, no amb el text "GOS"
        if (animal.Especie != Especie.GOS)
        {
            Console.WriteLine($"No es pot registrar el passeig: {animal.Nom} és un {animal.Especie} i només es passegen els gossos");
            return false;
        }

        // Es guarda el valor ABANS de sumar per poder mostrar els minuts previs
        int minutsPrevis = voluntari.MinutsPasseig;
        voluntari.MinutsPasseig = voluntari.MinutsPasseig + minuts;   // se SUMEN: no es substitueixen
        _context.SaveChanges();
        Console.WriteLine($"{voluntari.Nom} ha passejat {animal.Nom} {minuts} minuts · abans {minutsPrevis}, ara {voluntari.MinutsPasseig}");
        return true;
    }

    // ================================================================
    // P6 · ESTADÍSTIQUES
    // ================================================================

    // Nivell 2 · q
    public void LlistatVoluntaris(int minAnimals)
    {
        // v.Animals.Count dins la consulta no és lazy loading: EF ho tradueix a un COUNT de la taula Cura.
        // Es fa servir al Where, a l'OrderBy i al Select, i tot és UNA consulta
        var llista = _context.Voluntari
            .Where(v => v.Animals.Count >= minAnimals)
            .OrderByDescending(v => v.Animals.Count)
            .Select(v => new
            {
                v.Nom,
                v.Torn,
                Animals = v.Animals.Count,
                v.MinutsPasseig
            })
            .ToList();

        if (llista.Count == 0)
        {
            Console.WriteLine($"Cap voluntari té cura de {minAnimals} animals o més");
        }

        // El número d'ordre no és a la BD: es posa en recórrer la llista
        int n = 1;
        foreach (var x in llista)
        {
            Console.WriteLine($"{n}. {x.Nom} ({x.Torn}) - animals: {x.Animals} - {x.MinutsPasseig} min");
            n++;
        }
    }

    // Nivell 3 · r
    public List<Voluntari> VoluntarisDeSeniors()
    {
        // La data límit es calcula ABANS: dins el Where, EF no sap traduir un mètode nostre (Edat()) a SQL
        // i peta en executar la consulta (InvalidOperationException: "could not be translated").
        // Sènior = 8 anys o més = nascut el dia d'avui de fa 8 anys o abans.
        // (A la P5 restàvem els anys, però allà el càlcul es feia en C#, no a la BD)
        DateTime limit = DateTime.Today.AddYears(-8);

        // Any: algun dels seus animals és un gos nascut abans del límit. Es tradueix a un EXISTS
        List<Voluntari> llista = _context.Voluntari
            .Where(v => v.Animals.Any(a => a.Especie == Especie.GOS && a.DataNaixement <= limit))
            .ToList();
        return llista;
    }
}
