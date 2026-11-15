using MKS.DeepWater.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MKS.DeepWater.Web.Tests;

/// <summary>
/// Start na pustej bazie zakłada wyłącznie role i konto administratora - bez przykładowych
/// członków, wpłat i stawek, które mieszałyby się z prawdziwymi danymi spółki.
/// </summary>
public class DbInitializerTests
{
    [Fact]
    public async Task Pusta_baza_nie_dostaje_przykladowych_danych()
    {
        using var db = TestDb.Create();
        await using var services = Services(db);

        await DbInitializer.InitializeAsync(services, Configuration());

        using var verification = db.NewContext();
        Assert.Empty(await verification.Members.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await verification.FeePayments.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await verification.FeeRates.ToListAsync());
    }

    [Fact]
    public async Task Pusta_baza_dostaje_role_i_konto_administratora()
    {
        using var db = TestDb.Create();
        await using var services = Services(db);

        await DbInitializer.InitializeAsync(services, Configuration());

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByNameAsync("admin");
        Assert.NotNull(admin);
        Assert.True(await userManager.IsInRoleAsync(admin, DbInitializer.AdminRole));
        Assert.True(await services.GetRequiredService<RoleManager<IdentityRole>>()
            .RoleExistsAsync(DbInitializer.ClerkRole));
    }

    private static ServiceProvider Services(TestDb db)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db.Context);
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        return services.BuildServiceProvider();
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeedAdmin:UserName"] = "admin",
                ["SeedAdmin:Password"] = "Test-Haslo1!"
            })
            .Build();
}
