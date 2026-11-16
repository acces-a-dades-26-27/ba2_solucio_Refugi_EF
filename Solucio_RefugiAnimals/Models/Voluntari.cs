namespace Solucio_RefugiAnimals.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

// Nom únic: com el DNI de l'Adoptant, l'índex va a sobre de la CLASSE.
// L'enunciat ho demana i és el que fa que buscar per nom (FirstOrDefault) trobi sempre el voluntari que toca
[Index(nameof(Nom), IsUnique = true)]
public class Voluntari
{
    // PK i Identity sense cap anotació: EF ho dedueix pel nom VoluntariID
    public int VoluntariID { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Nom { get; set; }

    // enum: ja surt NOT NULL sense cap anotació
    public Torn Torn { get; set; }

    // Per defecte 0: ho posem al constructor (un int ja val 0, però així queda escrit)
    public int MinutsPasseig { get; set; }

    // P3 · Un costat de la N-M. L'altre és Animal.Voluntaris (configurada a RefugiContext).
    // virtual: amb UseLazyLoadingProxies és obligatori.
    // Inicialitzada al constructor: si no, un voluntari nou té Animals = null
    // i el primer Animals.Count o Animals.Add peta (NullReferenceException)
    public virtual ICollection<Animal> Animals { get; set; }

    public Voluntari()
    {
        MinutsPasseig = 0;
        // Un voluntari no pot tenir el mateix animal dues vegades: un SET
        Animals = new HashSet<Animal>();
    }

    // Com a l'Adoptant, sense {Animals}: una col·lecció no sap escriure's. Es recorre amb un foreach (veure Program)
    public override string ToString()
    {
        return $"{VoluntariID}. {Nom} ({Torn}) · {MinutsPasseig} minuts de passeig";
    }
}
