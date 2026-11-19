namespace Solucio_RefugiAnimals.Models;
using System.ComponentModel.DataAnnotations;

// P6 · Una visita al veterinari. Animal 1 → N Visita (configurada a RefugiContext)
public class Visita
{
    public int VisitaID { get; set; }

    public DateTime Data { get; set; }

    // enum: NOT NULL sense cap anotació. A la BD, text (HasConversion al context)
    public Motiu Motiu { get; set; }

    // int a posta: euros sencers. Amb decimal, SQLite no sap fer Sum ni OrderBy a la BD
    // i EstadistiquesPerMotiu peta (excepció: SQLite no pot aplicar Sum a decimal).
    // Si algú el posa decimal: g.Sum(v => (double)v.Cost) o .HasConversion<double>() al context
    public int Cost { get; set; }

    // Tota visita és d'un animal: required (el compilador obliga) i [Required] (NOT NULL a la BD).
    // Hi va l'objecte, no l'id: la columna AnimalID la crea EF (veure RefugiContext)
    [Required]
    public virtual required Animal Animal { get; set; }

    public override string ToString()
    {
        return $"{Animal.Nom} · {Data:dd/MM/yyyy} · {Motiu} · {Cost} €";
    }
}
