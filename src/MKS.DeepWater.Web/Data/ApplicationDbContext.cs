using MKS.DeepWater.Web.Models.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MKS.DeepWater.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<FeeRate> FeeRates => Set<FeeRate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Member>(e =>
        {
            e.HasIndex(m => m.MembershipNumber).IsUnique();
            e.HasIndex(m => new { m.LastName, m.FirstName });
            e.HasIndex(m => m.DeletedAt);
            // Miękkie usuwanie: domyślnie żadne zapytanie nie widzi usuniętych rekordów.
            // Archiwum i przywracanie sięgają po nie przez IgnoreQueryFilters().
            e.HasQueryFilter(m => m.DeletedAt == null);
            e.HasMany(m => m.Addresses)
                .WithOne(a => a.Member!)
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(m => m.Payments)
                .WithOne(p => p.Member!)
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Address>(e =>
        {
            // Filtr dopasowany do Member - adres usuniętego członka nie wychodzi
            // też z bezpośredniego zapytania o adresy.
            e.HasQueryFilter(a => a.Member!.DeletedAt == null);
        });

        builder.Entity<FeePayment>(e =>
        {
            e.HasIndex(p => new { p.MemberId, p.FeeYear });
            e.HasQueryFilter(p => p.Member!.DeletedAt == null);
            // SQLite nie ma typu decimal - mapowanie na double pozwala sumowac i sortowac w SQL.
            e.Property(p => p.Amount).HasConversion<double>();
        });

        builder.Entity<FeeRate>(e =>
        {
            e.HasIndex(r => r.Year).IsUnique();
            e.Property(r => r.AmountPerShare).HasConversion<double>();
        });
    }
}
