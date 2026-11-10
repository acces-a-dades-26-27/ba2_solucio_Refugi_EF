namespace Solucio_RefugiAnimals.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

// Camp únic: l'anotació va a sobre de la CLASSE (a la propietat no compila).
// L'altra opció és la Fluent API: modelBuilder.Entity<Xip>().HasIndex(x => x.Codi).IsUnique();
[Index(nameof(Codi), IsUnique = true)]
public class Xip
{
    public int XipID { get; set; }

    [Required]
    [StringLength(15, MinimumLength = 15)]  // exactament 15. A la BD només surt el màxim: el mínim EF no el comprova, ho fem al servei
    public required string Codi { get; set; }

    public DateTime DataImplantacio { get; set; }

    // No pot ser required: el xip es crea dins l'animal (Xip = new Xip() { ... }) i allà encara no el té.
    // null! = "ara és null, però EF l'omplirà en llegir-lo de la BD"
    public virtual Animal Animal { get; set; } = null!;

    public Xip()
    {
        DataImplantacio = DateTime.Today;   // per defecte, la data d'avui
    }
}
