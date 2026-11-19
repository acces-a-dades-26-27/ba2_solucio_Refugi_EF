using Solucio_RefugiAnimals.Data;
using Solucio_RefugiAnimals.Models;

// P6 · VISITES
public class VisitaService
{
    private RefugiContext _context;

    public VisitaService(RefugiContext context)
    {
        _context = context;
    }

    // Nivell 2 · o. El cost segons el motiu. private: és una peça interna, com DespesaBase.
    // rnd.Next(min, max): el max NO hi entra. Per incloure els dos extrems, max + 1: 30 a 60 → Next(30, 61).
    // Rep el Random: si en creéssim un aquí cada vegada, és més lent i no cal
    private int CostAleatori(Motiu motiu, Random rnd)
    {
        switch (motiu)
        {
            case Motiu.REVISIO:
                return rnd.Next(30, 61);
            case Motiu.VACUNA:
                return rnd.Next(25, 51);
            case Motiu.FERIDA:
                return rnd.Next(60, 151);
            default:
                return rnd.Next(250, 601);      // CIRURGIA. Sense default no compila, com a DespesaBase
        }
    }

    // Nivell 1 · m, amb la millora del nivell 2 (o)
    public void GenerarVisites(int quantes)
    {
        // Els animals es porten a memòria UN cop, abans del bucle.
        // Error típic: _context.Animal.ToList() dins el bucle, una consulta per visita
        List<Animal> animals = _context.Animal.ToList();
        if (animals.Count == 0)     // amb la llista buida, rnd.Next(0) dona 0 i animals[0] peta
        {
            Console.WriteLine("No hi ha cap animal al refugi: no es poden generar visites");
            return;
        }

        // UN sol Random, abans del bucle, i es va reutilitzant.
        // Crear-ne un de nou a cada volta no és la manera: un Random és un generador, no un número
        Random rnd = new Random();
        Motiu[] motius = Enum.GetValues<Motiu>();

        for (int i = 0; i < quantes; i++)
        {
            // Next(Count): de 0 a Count - 1, just els índexs vàlids.
            // Errors típics: Next(1, Count), que no treu mai el primer, i Next(Count - 1), que no treu mai l'últim
            Animal animal = animals[rnd.Next(animals.Count)];
            Motiu motiu = motius[rnd.Next(motius.Length)];

            // Nivell 1: DateTime data = DateTime.Today.AddDays(-rnd.Next(0, 365));
            //           int cost = rnd.Next(20, 201);

            // Nivell 2 · Entre l'entrada i avui, o entre l'entrada i l'adopció.
            // DataAdopcio és DateTime?: cal .Value (o ??) per fer-la servir com una data normal.
            // Sense .Value no compila: no es pot posar un DateTime? dins un DateTime
            DateTime inici = animal.DataEntrada;
            DateTime fi = DateTime.Today;
            if (animal.EstaAdoptat && animal.DataAdopcio != null)
            {
                fi = animal.DataAdopcio.Value;
            }
            int dies = (fi - inici).Days;
            if (dies < 0)       // per si alguna data està malament: amb un negatiu, rnd.Next peta
            {
                dies = 0;
            }
            // dies + 1: si no, l'últim dia (avui o el de l'adopció) no surt mai.
            // Error típic: AddDays(-rnd.Next(dies)) des d'avui, que en un animal adoptat surt després de l'adopció
            DateTime data = inici.AddDays(rnd.Next(0, dies + 1));

            Visita visita = new Visita()
            {
                Animal = animal,
                Data = data,
                Motiu = motiu,
                Cost = CostAleatori(motiu, rnd)
            };
            // També val: animal.Visites.Add(visita), com les cries a RegistrarCria
            _context.Visita.Add(visita);
            Console.WriteLine(visita);
        }

        // UN cop, fora del bucle, com a CalcularDespesaMes
        _context.SaveChanges();
        Console.WriteLine($"S'han generat {quantes} visites");
    }

    // Nivell 2 · p
    public void EstadistiquesPerMotiu()
    {
        // Tot ho fa la BD: GROUP BY Motiu amb COUNT, SUM i AVG.
        // Cost és int: Sum i Average funcionen. Average d'un int retorna double
        var perMotiu = _context.Visita
            .GroupBy(v => v.Motiu)
            .Select(g => new
            {
                Motiu = g.Key,
                Visites = g.Count(),
                Total = g.Sum(v => v.Cost),
                Mitjana = g.Average(v => v.Cost)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        // Així NO: _context.Visita.ToList().GroupBy(...). Porta totes les visites a memòria i agrupa en C#
        foreach (var x in perMotiu)
        {
            Console.WriteLine($"{x.Motiu}: {x.Visites} visites · total {x.Total} € · mitjana {x.Mitjana:F2} €");
        }
    }

    // Nivell 3 · s
    public void EstadistiquesPerMes()
    {
        // La clau és un objecte amb dues coses: g.Key.Year i g.Key.Month.
        // Error típic: agrupar només per v.Data.Month, que ajunta el novembre de 2025 amb el de 2026
        var perMes = _context.Visita
            .GroupBy(v => new { v.Data.Year, v.Data.Month })
            .Select(g => new
            {
                Any = g.Key.Year,
                Mes = g.Key.Month,
                Visites = g.Count(),
                Total = g.Sum(v => v.Cost)
            })
            .OrderBy(x => x.Any)
            .ThenBy(x => x.Mes)     // ThenBy, no un altre OrderBy: el segon OrderBy desfaria el primer
            .ToList();

        foreach (var x in perMes)
        {
            Console.WriteLine($"{x.Mes:D2}/{x.Any}: {x.Visites} visites · {x.Total} €");
        }
    }
}
