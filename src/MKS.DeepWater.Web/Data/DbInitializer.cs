using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MKS.DeepWater.Web.Data;

/// <summary>
/// Tworzy/aktualizuje plikową bazę SQLite i zakłada konto startowe administratora.
/// Świadomie <b>nie</b> wpisuje przykładowych członków ani stawek - baza zawiera wyłącznie
/// prawdziwe dane spółki, a przykłady przy pustej bazie wracałyby po każdym jej odtworzeniu.
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
    }

    /// <summary>Wartość z konfiguracji albo domyślna - puste ustawienie traktujemy jak brak.</summary>
    private static string Value(string? configured, string fallback) =>
        string.IsNullOrWhiteSpace(configured) ? fallback : configured;
}
