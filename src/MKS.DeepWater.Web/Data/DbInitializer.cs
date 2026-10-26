using MKS.DeepWater.Web.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MKS.DeepWater.Web.Data;

/// <summary>
/// Tworzy/aktualizuje plikową bazę SQLite i zakłada konto startowe administratora.
/// </summary>
public static class DbInitializer
{
    public const string AdminRole = "Administrator";
    public const string ClerkRole = "Pracownik";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AdminRole, ClerkRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var login = Value(configuration["SeedAdmin:UserName"], "admin");

        var admin = await userManager.FindByNameAsync(login);
        if (admin is null)
        {
            // Hasło jest wymagane tylko wtedy, gdy konto trzeba utworzyć - kolejne starty
            // działają bez niego, więc usunięcie sekretu nie wywraca uruchomionej instancji.
            var password = configuration["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    $"Brak hasła dla zakładanego konta startowego \"{login}\". " +
                    "Ustaw SeedAdmin:Password poza repozytorium - lokalnie: " +
                    "dotnet user-secrets set \"SeedAdmin:Password\" \"<haslo>\" " +
                    "(projekt src/MKS.DeepWater.Web), na serwerze: zmienna środowiskowa " +
                    "SeedAdmin__Password.");
            }

            admin = new ApplicationUser
            {
                UserName = login,
                Email = Value(configuration["SeedAdmin:Email"], "admin@localhost"),
                EmailConfirmed = true,
                FullName = "Administrator systemu"
            };
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Nie udało się utworzyć konta administratora: " +
                    string.Join("; ", result.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
        }

        await SeedDemoMembersAsync(db);
    }

    /// <summary>Wartość z konfiguracji albo domyślna - puste ustawienie traktujemy jak brak.</summary>
    private static string Value(string? configured, string fallback) =>
        string.IsNullOrWhiteSpace(configured) ? fallback : configured;

    private static async Task SeedDemoMembersAsync(ApplicationDbContext db)
    {
        if (await db.Members.AnyAsync())
        {
            return;
        }

        var year = DateTime.Today.Year;

        db.FeeRates.Add(new FeeRate
        {
            Year = year,
            AmountPerShare = 240m,
            ResolutionReference = $"Uchwała nr 1/{year} Zgromadzenia Członków"
        });

        db.Members.AddRange(
            new Member
            {
                MembershipNumber = "001",
                FirstName = "Anna",
                LastName = "Kowalska",
                PhoneNumber = "600100200",
                Status = MemberStatus.Active,
                SharesCount = 2,
                Street = "Morska",
                HouseNumber = "14",
                PostalCode = "81-001",
                City = "Gdynia",
                Payments =
                {
                    new FeePayment
                    {
                        FeeYear = year, PaymentDate = new DateOnly(year, 1, 20),
                        Amount = 480m, Method = PaymentMethod.Transfer, DocumentNumber = "WB/1/2026"
                    }
                }
            },
            new Member
            {
                MembershipNumber = "002",
                FirstName = "Marek",
                LastName = "Nowak",
                PhoneNumber = "601300400",
                Status = MemberStatus.Active,
                SharesCount = 1,
                Street = "Leśna",
                HouseNumber = "7",
                PostalCode = "80-180",
                City = "Gdańsk"
            },
            new Member
            {
                MembershipNumber = "003",
                FirstName = "Ewa",
                LastName = "Wiśniewska",
                PhoneNumber = "602500600",
                Status = MemberStatus.Former,
                SharesCount = 1,
                Street = "Polna",
                HouseNumber = "22",
                PostalCode = "84-200",
                City = "Wejherowo"
            });

        await db.SaveChangesAsync();
    }
}
