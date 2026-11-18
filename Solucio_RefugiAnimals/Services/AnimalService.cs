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

    // ================================================================
    // P5 · DESPESA DEL MES
    // ================================================================

    // La despesa base de cada espècie. private: com AfegirDescendents, és una peça interna del servei.
    // Els literals porten m: són decimal. 60 sense m és un int i passa, però 1.3 sense m és double i no compila
    private decimal DespesaBase(Especie especie)
    {
        // Especie és un enum: es compara amb Especie.GOS, no amb el text "GOS"
        switch (especie)
        {
            case Especie.GOS:
                return 60m;
            case Especie.GAT:
                return 40m;
            case Especie.CONILL:
                return 15m;
            case Especie.AU:
                return 10m;
            default:
                return 20m;     // ALTRES. Sense default no compila ("not all code paths return a value"),
                                // encara que hi hagi un case per cada valor de l'enum
        }
    }

    // Nivell 1 · k, amb els modificadors del nivell 2 i el resum del nivell 3
    public void CalcularDespesaMes()
    {
        // ToList() primer, com a la pista: dins el bucle fem servir navegacions (Voluntaris, Cries)
        // i el lazy loading les carrega amb consultes noves mentre la primera encara s'estaria llegint.
        // Només els NO adoptats, i el filtre el fa la BD.
        // Error típic: recórrer tots els animals i cobrar el mes també als adoptats
        List<Animal> animals = _context.Animal
            .Where(a => !a.EstaAdoptat)
            .OrderBy(a => a.AnimalID)
            .ToList();

        // Nivell 3 · Es calcula DINS el mateix bucle: al final no es torna a consultar la BD
        decimal total = 0;
        Animal? mesCar = null;
        decimal despesaMesCar = 0;

        foreach (Animal a in animals)
        {
            // Es guarda el valor ABANS de sumar, com els minuts previs de RegistrarPasseig.
            // Si es llegeix després de sumar, la prèvia i l'actual surten iguals
            decimal previ = a.DespesaAcumulada;
            decimal despesa = DespesaBase(a.Especie);

            // Nivell 2 · Els modificadors MULTIPLIQUEN, un sobre l'altre: 60 × 1,3 × 1,1.
            // Error típic: sumar els percentatges, 60 × (1 + 0,3 + 0,1), que no dona el mateix
            List<string> modificadors = new List<string>();

            // a.Voluntaris el carrega el lazy loading. Count > 0, no != null: la col·lecció mai és null (constructor)
            if (a.Voluntaris.Count > 0)
            {
                // −10% és per 0,9. Error típic: despesa - 0.1m, que treu 10 cèntims, no el 10%
                despesa = despesa * 0.9m;
                modificadors.Add("voluntaris -10%");
            }

            // Com diu l'enunciat, n'hi ha prou amb restar els anys. >= 8: amb 8 anys justos ja és sènior
            int anys = DateTime.Today.Year - a.DataNaixement.Year;
            if (anys >= 8)
            {
                despesa = despesa * 1.3m;
                modificadors.Add("sènior +30%");
            }

            // Només les cries NO adoptades. Error típic: a.Cries.Count, que compta també les adoptades
            // (i aleshores adoptar la Coco no canvia res a la Nala)
            int criesNoAdoptades = a.Cries.Count(c => !c.EstaAdoptat);
            if (criesNoAdoptades > 0)
            {
                // +10% PER CRIA: amb 2 cries, × 1,2. No × 1,1 una sola vegada sense mirar quantes n'hi ha
                despesa = despesa * (1 + 0.1m * criesNoAdoptades);
                modificadors.Add($"{criesNoAdoptades} cries +{criesNoAdoptades * 10}%");
            }

            // Als cèntims: amb tres modificadors surten tres decimals (77,220) i la columna és (8,2)
            despesa = Math.Round(despesa, 2);

            // ACUMULA: se SUMA al que ja hi havia. Amb a.DespesaAcumulada = despesa
            // cada mes substitueix l'anterior i la segona crida no dona el doble
            a.DespesaAcumulada = previ + despesa;

            string textModificadors;
            if (modificadors.Count == 0)
            {
                textModificadors = "sense modificadors";
            }
            else
            {
                textModificadors = string.Join(", ", modificadors);
            }
            Console.WriteLine($"{a.Nom}: {previ:F2} € + {despesa:F2} € = {a.DespesaAcumulada:F2} € · {textModificadors}");

            // Nivell 3 · Es compara amb la despesa DEL MES, no amb DespesaAcumulada:
            // el que es demana és qui ha costat més AQUEST mes, no en total
            total = total + despesa;
            if (mesCar == null || despesa > despesaMesCar)
            {
                mesCar = a;
                despesaMesCar = despesa;
            }
        }

        // UN cop, fora del bucle, com a la pista: desa tots els animals de cop.
        // Sense això, la consola enganya: el context recorda els canvis en memòria i el mes 2 surt bé,
        // però a la BD tot és 0. Es veu obrint la BD o amb un context nou
        _context.SaveChanges();

        // Nivell 3 · Res de _context.Animal.Sum(...) ni OrderByDescending(...) aquí: seria tornar a consultar la BD.
        // I la despesa del mes no és a la BD: a la BD només hi ha l'acumulada
        Console.WriteLine($"Total del mes: {total:F2} €");
        if (mesCar != null)     // si no hi ha cap animal al refugi, mesCar és null i mesCar.Nom petaria
        {
            Console.WriteLine($"L'animal que ha costat més aquest mes: {mesCar.Nom} ({despesaMesCar:F2} €)");
        }
    }
}
