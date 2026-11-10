namespace Solucio_RefugiAnimals.Models;

// A la BD és una columna amb un enter: GOS = 0, GAT = 1...
// Perquè surti text seria amb fluent API: modelBuilder.Entity<Animal>().Property(a => a.Especie).HasConversion<string>();
public enum Especie
{
    GOS,
    GAT,
    CONILL,
    AU,
    ALTRES
}