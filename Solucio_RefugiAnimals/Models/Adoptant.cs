namespace Solucio_RefugiAnimals.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

// DNI únic: com el Codi del Xip, l'índex va a sobre de la CLASSE
[Index(nameof(DNI), IsUnique = true)]
public class Adoptant
{
    // Les propietats han de ser PUBLIQUES: si són private, EF no les mapeja (no troba la PK)
    // Els noms, els de l'enunciat: Cognoms (no Cognom), TeJardi, DataRegistre...
    public int AdoptantID { get; set; }

    [Required]
    [MaxLength(9)]
    public required string DNI { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Nom { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Cognoms { get; set; }

    // bool i DateTime ja surten NOT NULL sense cap anotació
    public bool TeJardi { get; set; }
    public DateTime DataRegistre { get; set; }

    // El costat N de la relació (configurada a RefugiContext).
    // virtual: amb UseLazyLoadingProxies és obligatori; si no, EF dona error en arrencar.
    // Inicialitzada al constructor: un adoptant nou té la llista buida, no null.
    public virtual ICollection<Animal> Animals { get; set; }

    public Adoptant()
    {
        DataRegistre = DateTime.Today;  // per defecte, la data d'avui
        // Com un adoptant no pot tenir un mateix animal dues vegades té sentit que sigui un SET
        Animals = new HashSet<Animal>();
    }

    // Al ToString NO hi posem {Animals}: una col·lecció no sap escriure's i surt el nom del tipus
    // (System.Collections.Generic.List`1[...]). Els animals es recorren amb un foreach (veure Program).
    public override string ToString()
    {
        string jardi;
        if (TeJardi)
        {
            jardi = "té jardí";
        }
        else
        {
            jardi = "sense jardí";
        }
        return $"{AdoptantID}. {Nom} {Cognoms} ({DNI}) · {jardi} · registre {DataRegistre:dd/MM/yyyy}";
    }
}
