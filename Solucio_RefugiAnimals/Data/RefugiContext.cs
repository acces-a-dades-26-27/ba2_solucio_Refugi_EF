using Solucio_RefugiAnimals.Models;

namespace Solucio_RefugiAnimals.Data;
using Microsoft.EntityFrameworkCore;

public class RefugiContext : DbContext
{
    // 1 · Els DbSet: una propietat per taula
    public DbSet<Animal> Animal { get; set; }
    public DbSet<Xip> Xip { get; set; }

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

        modelBuilder.Entity<Animal>()
            .Property(a => a.Especie)
            .HasConversion<string>();

        // [Range] i el mínim de [StringLength] EF no els comprova en desar.
        // Amb un CHECK ho comprova la BD: SQLite sí que el respecta i, si no es compleix, SaveChanges peta.
        modelBuilder.Entity<Animal>()
            .ToTable(t => t.HasCheckConstraint("CK_Animal_Pes", "\"Pes\" >= 0.1 AND \"Pes\" <= 100"));

        // Exactament 15 caràcters i tots dígits (GLOB '*[^0-9]*' = "conté algun caràcter que no és un dígit")
        modelBuilder.Entity<Xip>()
            .ToTable(t => t.HasCheckConstraint("CK_Xip_Codi", "length(\"Codi\") = 15 AND \"Codi\" NOT GLOB '*[^0-9]*'"));
    }
}
