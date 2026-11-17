namespace Solucio_RefugiAnimals.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public class Animal
{
    public int AnimalID { get; set; }

    // required: el compilador obliga a donar-li valor en crear l'animal (new Animal() { Nom = ... })
    // [Required]: la columna surt NOT NULL a la BD
    [Required]
    [MaxLength(50)]
    public required string Nom { get; set; }

    // enum, bool i DateTime ja surten NOT NULL sense cap anotació
    public Especie Especie { get; set; }
    public bool EsFemella { get; set; }
    public DateTime DataNaixement { get; set; }
    public DateTime DataEntrada { get; set; }

    [Range(0.1, 100)]               // queda escrit, però EF no el comprova en desar
    public double Pes { get; set; }

    public bool EstaAdoptat { get; set; }
    public DateTime? DataAdopcio { get; set; }      // el ? fa que admeti NULL

    [Precision(8, 2)]               // queda al model, SQLite no la força
    public decimal QuotaAdopcio { get; set; }

    // A la relació hi va l'objecte, no l'id: la columna XipID la crea EF (veure RefugiContext)
    // Tot animal porta xip: required obliga a posar-li en crear-lo
    [Required]
    public virtual required Xip Xip { get; set; }

    // P2 · Un animal pot no tenir adoptant: ? i SENSE [Required]
    // (un [Required] aquí faria la relació obligatòria, just el contrari del que volem).
    // Com amb el xip, hi va l'objecte: la columna AdoptantID la crea EF (veure RefugiContext)
    public virtual Adoptant? Adoptant { get; set; }

    // P3 · L'altre costat de la N-M amb Voluntari: les DUES classes tenen la col·lecció.
    // Sense aquesta, EF no sap que és una N-M i ho fa 1-N (posa una columna VoluntariID a Animal)
    public virtual ICollection<Voluntari> Voluntaris { get; set; }

    // P4 · Relació RECURSIVA: les dues navegacions són del mateix tipus, Animal.
    // Un animal pot no tenir mare (ha arribat de fora del refugi): ? i SENSE [Required], com l'Adoptant.
    // Hi va l'objecte, no l'id: la columna MareID la crea EF (veure RefugiContext)
    public virtual Animal? Mare { get; set; }

    // L'altre costat: les cries d'aquest animal. Sense aquesta col·lecció no es pot fer el WithMany
    public virtual ICollection<Animal> Cries { get; set; }

    public Animal()
    {
        DataEntrada = DateTime.Today;   // per defecte, la data d'avui
        EstaAdoptat = false;
        DataAdopcio = null;
        QuotaAdopcio = 0;
        // P3 · Inicialitzada, com Animals a l'Adoptant i al Voluntari
        Voluntaris = new HashSet<Voluntari>();
        // P4 · Inicialitzada: si no, un animal nou té Cries = null
        // i el primer Cries.Count o Cries.Add peta (NullReferenceException)
        Cries = new HashSet<Animal>();
    }

    // P4 · Al ToString NO hi posem {Mare.Nom}: si l'animal no té mare, Mare és null i peta.
    // La mare i les cries es mostren al Program, comprovant el null (veure MostrarFamilia)
    public override string ToString()
    {
        return $"{AnimalID}. {Nom} ({Especie}) · entrada {DataEntrada:dd/MM/yyyy} · xip {Xip.Codi}";
    }
}
