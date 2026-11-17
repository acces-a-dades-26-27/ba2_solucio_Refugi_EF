using Solucio_RefugiAnimals.Models;

namespace Solucio_RefugiAnimals.Data;
using Microsoft.EntityFrameworkCore;

public class RefugiContext : DbContext
{
    // 1 · Els DbSet: una propietat per taula
    public DbSet<Animal> Animal { get; set; }
    public DbSet<Xip> Xip { get; set; }
    public DbSet<Adoptant> Adoptant { get; set; }   // P2: sense DbSet no podem fer _context.Adoptant
    public DbSet<Voluntari> Voluntari { get; set; } // P3. La taula Cura NO té DbSet: és la intermèdia i la gestiona EF

    // 2 · On és la BD i els proxies del lazy loading
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=RefugiAnimals.db")
            .UseLazyLoadingProxies();

        // Per veure el SQL que fa EF (nivell 3). Cal: using Microsoft.Extensions.Logging;
        // optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
    }

    // 3 · Fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tot animal porta un xip i un xip només és d'un animal.
        // La FK va a Animal: no hi ha animal sense xip.
        modelBuilder.Entity<Animal>()
            .HasOne(a => a.Xip)
            .WithOne(x => x.Animal)
            .HasForeignKey<Animal>("XipID")     // EF hi posa un índex únic: un xip, un sol animal
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // P2 · Adoptant 1 → N Animal. La FK va a Animal (el costat N) i pot ser NULL.
        modelBuilder.Entity<Animal>()
            .HasOne(a => a.Adoptant)
            .WithMany(ad => ad.Animals)
            .HasForeignKey("AdoptantID")        // columna AdoptantID a la taula Animal (int, nullable)
            .IsRequired(false)                  // un animal pot no tenir adoptant
            .OnDelete(DeleteBehavior.SetNull);  // si s'esborra l'adoptant, els seus animals queden sense adoptant.
                                                // Amb Cascade s'esborrarien els animals!
        // Si a Animal hi poses la propietat public int? AdoptantID, aleshores .HasForeignKey(a => a.AdoptantID)

        modelBuilder.Entity<Animal>()
            .Property(a => a.Especie)
            .HasConversion<string>();

        // P3 · Voluntari N → N Animal. Sense Fluent API també funciona, però la taula es diria AnimalVoluntari.
        // UsingEntity només li posa nom a la taula intermèdia: les columnes (AnimalsAnimalID, VoluntarisVoluntariID) les crea EF.
        // Compte si poses els noms de les FK a mà amb UsingEntity<Dictionary<string, object>>: un espai de més
        // dins del text ("AnimalID ") també és el nom de la columna
        modelBuilder.Entity<Voluntari>()
            .HasMany(v => v.Animals)
            .WithMany(a => a.Voluntaris)
            .UsingEntity(j => j.ToTable("Cura"));

        // Com l'Especie: el torn surt com a text a la BD
        modelBuilder.Entity<Voluntari>()
            .Property(v => v.Torn)
            .HasConversion<string>();

        // P4 · Animal 1 → N Animal (una mare, moltes cries). Relació RECURSIVA:
        // és una 1-N normal amb Animal als dos costats. La FK va a la cria (el costat N) i pot ser NULL.
        modelBuilder.Entity<Animal>()
            .HasOne(a => a.Mare)
            .WithMany(m => m.Cries)
            .HasForeignKey("MareID")            // columna MareID a la taula Animal (int, nullable)
            .IsRequired(false)                  // un animal pot no tenir mare
            .OnDelete(DeleteBehavior.SetNull);  // si s'esborra la mare, les cries queden sense mare.
                                                // Restrict (la de la pista) també val: no deixa esborrar una mare amb cries.
                                                // Amb Cascade s'esborrarien les cries!

        // [Range] i el mínim de [StringLength] EF no els comprova en desar.
        // Amb un CHECK ho comprova la BD: SQLite sí que el respecta i, si no es compleix, SaveChanges peta.
        modelBuilder.Entity<Animal>()
            .ToTable(t => t.HasCheckConstraint("CK_Animal_Pes", "\"Pes\" >= 0.1 AND \"Pes\" <= 100"));

        // Exactament 15 caràcters i tots dígits (GLOB '*[^0-9]*' = "conté algun caràcter que no és un dígit")
        modelBuilder.Entity<Xip>()
            .ToTable(t => t.HasCheckConstraint("CK_Xip_Codi", "length(\"Codi\") = 15 AND \"Codi\" NOT GLOB '*[^0-9]*'"));
    }
}
