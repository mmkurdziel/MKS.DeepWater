using MKS.DeepWater.Web.Controllers;
using MKS.DeepWater.Web.Data;
using MKS.DeepWater.Web.Models.Domain;
using MKS.DeepWater.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MKS.DeepWater.Web.Tests;

/// <summary>
/// Kwoty zapisywane w bazie jako całkowita liczba groszy, a nie jako double -
/// dokładne sumy w SQL i migracja istniejących danych bez utraty wartości.
/// </summary>
public class MoneyTests
{
    private const string MigracjaPrzedGroszami = "20261026090058_SimplifyMemberModel";

    [Theory]
    [InlineData("480", 48000)]
    [InlineData("180.10", 18010)]
    [InlineData("0.01", 1)]
    [InlineData("0", 0)]
    [InlineData("1000000", 100000000)]
    public void Kwota_przeliczana_na_grosze(string zlote, long grosze)
    {
        var kwota = decimal.Parse(zlote, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(grosze, GroszeConverter.ToGrosze(kwota));
        Assert.Equal(kwota, GroszeConverter.FromGrosze(grosze));
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("180.105")]
    public void Kwota_z_ulamkiem_grosza_jest_odrzucana(string zlote)
    {
        var kwota = decimal.Parse(zlote, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentException>(() => GroszeConverter.ToGrosze(kwota));
    }

    [Fact]
    public void Kolumny_kwot_sa_calkowite_i_w_groszach()
    {
        using var db = TestDb.Create();

        var wplaty = db.QuerySingleText(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'FeePayments'");
        var stawki = db.QuerySingleText(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'FeeRates'");

        Assert.Contains("\"AmountGrosze\" INTEGER NOT NULL", wplaty, StringComparison.Ordinal);
        Assert.Contains("\"AmountPerShareGrosze\" INTEGER NOT NULL", stawki, StringComparison.Ordinal);
    }

    [Fact]
    public void Kwota_zapisuje_sie_w_bazie_jako_liczba_groszy()
    {
        using var db = TestDb.Create();
        db.Context.Members.Add(Czlonek(wplaty: 180.10m));
        db.Context.FeeRates.Add(new FeeRate { Year = 2026, AmountPerShare = 180m });
        db.Context.SaveChanges();

        Assert.Equal("integer", db.QueryScalar("SELECT typeof(AmountGrosze) FROM FeePayments"));
        Assert.Equal(18010L, db.QueryScalar("SELECT AmountGrosze FROM FeePayments"));
        Assert.Equal(18000L, db.QueryScalar("SELECT AmountPerShareGrosze FROM FeeRates"));

        using var verification = db.NewContext();
        Assert.Equal(180.10m, verification.FeePayments.Single().Amount);
        Assert.Equal(180m, verification.FeeRates.Single().AmountPerShare);
    }

    [Fact]
    public void Suma_wplat_liczona_w_SQL_jest_dokladna()
    {
        using var db = TestDb.Create();
        db.Context.Members.Add(Czlonek(Enumerable.Repeat(0.10m, 1000).ToArray()));
        db.Context.SaveChanges();

        using var verification = db.NewContext();
        var suma = verification.FeePayments.Where(p => p.FeeYear == 2026).Sum(p => p.Amount);

        Assert.Equal(100m, suma);
    }

    [Fact]
    public void Porownanie_wplaty_z_naleznoscia_w_SQL_jest_dokladne()
    {
        // Kontrola "kwota = stawka x udziały" liczona w bazie. W REAL 0.10 * 3 to
        // 0.30000000000000004, więc wpłata 0,30 zł nie zgadzałaby się z należnością.
        using var db = TestDb.Create();
        var czlonek = Czlonek(0.30m);
        czlonek.SharesCount = 3;
        db.Context.Members.Add(czlonek);
        db.Context.FeeRates.Add(new FeeRate { Year = 2026, AmountPerShare = 0.10m });
        db.Context.SaveChanges();

        using var verification = db.NewContext();
        var zgodne = verification.FeePayments
            .Where(p => verification.FeeRates.Any(r =>
                r.Year == p.FeeYear && r.AmountPerShare * p.Member!.SharesCount == p.Amount))
            .Count();

        Assert.Equal(1, zgodne);
    }

    [Fact]
    public void Ta_sama_kontrola_na_REAL_daje_niezgodnosc()
    {
        // Kontrola negatywna: dowód, że test powyżej odróżnia grosze od dawnego zapisu REAL.
        // Samo sumowanie tego nie pokazuje - SQLite od 3.43 sumuje REAL z kompensacją błędu.
        using var db = TestDb.Create();

        Assert.Equal(0L, db.QueryScalar("SELECT 0.10 * 3 = 0.30"));
        Assert.Equal(1L, db.QueryScalar("SELECT 10 * 3 = 30"));
    }

    [Fact]
    public async Task Lista_czlonkow_pokazuje_dokladna_sume_wplat_za_rok()
    {
        using var db = TestDb.Create();
        db.Context.Members.Add(Czlonek(wplaty: [0.10m, 0.20m, 0.30m, 179.40m]));
        await db.Context.SaveChangesAsync(CancellationToken.None);

        var controller = new MembersController(db.Context);
        var result = await controller.Index(search: null, status: null, feeYear: 2026,
                                            showDeleted: false, ct: CancellationToken.None);

        var model = Assert.IsType<MemberListViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(180.00m, model.Items.Single().PaidInSelectedYear);
    }

    [Fact]
    public void Migracja_przelicza_istniejace_kwoty_na_grosze()
    {
        using var db = TestDb.Create(MigracjaPrzedGroszami);
        WpiszDaneWStarymSchemacie(db);

        db.MigrateTo(null);

        Assert.Equal(48000L, db.QueryScalar("SELECT AmountGrosze FROM FeePayments WHERE Id = 1"));
        // 0.29 * 100 w double to 28.999999999999996 - bez ROUND zostałoby 28 groszy.
        Assert.Equal(29L, db.QueryScalar("SELECT AmountGrosze FROM FeePayments WHERE Id = 2"));
        Assert.Equal("integer", db.QueryScalar("SELECT typeof(AmountGrosze) FROM FeePayments WHERE Id = 2"));
        Assert.Equal(24000L, db.QueryScalar("SELECT AmountPerShareGrosze FROM FeeRates"));

        using var verification = db.NewContext();
        Assert.Equal([0.29m, 480m],
            verification.FeePayments.Select(p => p.Amount).AsEnumerable().Order().ToArray());
    }

    [Fact]
    public void Wycofanie_migracji_przywraca_kwoty_w_zlotych()
    {
        using var db = TestDb.Create(MigracjaPrzedGroszami);
        WpiszDaneWStarymSchemacie(db);
        db.MigrateTo(null);

        db.MigrateTo(MigracjaPrzedGroszami);

        Assert.Equal(480.0, db.QueryScalar("SELECT Amount FROM FeePayments WHERE Id = 1"));
        Assert.Equal(0.29, db.QueryScalar("SELECT Amount FROM FeePayments WHERE Id = 2"));
        Assert.Equal(240.0, db.QueryScalar("SELECT AmountPerShare FROM FeeRates"));
    }

    private static void WpiszDaneWStarymSchemacie(TestDb db)
    {
        db.Execute("""
            INSERT INTO Members (Id, MembershipNumber, FirstName, LastName, Status, SharesCount, CreatedAt)
            VALUES (1, '001', 'Anna', 'Kowalska', 0, 1, '2026-01-01 00:00:00+00:00');
            INSERT INTO FeePayments (Id, MemberId, FeeYear, PaymentDate, Amount, Method, CreatedAt)
            VALUES (1, 1, 2026, '2026-03-01', 480.0, 0, '2026-03-01 00:00:00+00:00'),
                   (2, 1, 2026, '2026-03-02', 0.29, 0, '2026-03-02 00:00:00+00:00');
            INSERT INTO FeeRates (Id, Year, AmountPerShare) VALUES (1, 2026, 240.0);
            """);
    }

    private static Member Czlonek(params decimal[] wplaty) => new()
    {
        MembershipNumber = "001",
        FirstName = "Anna",
        LastName = "Kowalska",
        Payments = wplaty
            .Select((kwota, i) => new FeePayment
            {
                FeeYear = 2026,
                PaymentDate = new DateOnly(2026, 3, 1).AddDays(i),
                Amount = kwota
            })
            .ToList()
    };
}
