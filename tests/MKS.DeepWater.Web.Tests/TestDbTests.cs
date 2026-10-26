using MKS.DeepWater.Web.Models.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MKS.DeepWater.Web.Tests;

/// <summary>
/// Testy samego narzędzia testowego. Zanim zacznie się nim badać kod produkcyjny, trzeba
/// wiedzieć, że potrafi dać wynik negatywny - sonda, która zawsze świeci zielono, nie jest sondą.
/// </summary>
public class TestDbTests
{
    [Fact]
    public void TestDb_tworzy_baze_z_migracjami()
    {
        using var db = TestDb.Create();

        Assert.Empty(db.Context.Members);
        Assert.Empty(db.Context.Users);
    }

    [Fact]
    public void TestDb_ma_unikalny_indeks_w_schemacie()
    {
        using var db = TestDb.Create();

        var indexSql = db.QuerySingleText(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IX_Members_MembershipNumber'");

        Assert.NotNull(indexSql);
        Assert.Contains("UNIQUE", indexSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestDb_wymusza_unikalny_indeks()
    {
        using var db = TestDb.Create();
        db.Context.Members.Add(Member("001"));
        db.Context.SaveChanges();

        using var second = db.NewContext();
        second.Members.Add(Member("001"));

        var ex = Assert.Throws<DbUpdateException>(() => second.SaveChanges());
        var sqlite = Assert.IsType<SqliteException>(ex.InnerException);
        Assert.Equal(19, sqlite.SqliteErrorCode);
        Assert.Contains("Members.MembershipNumber", sqlite.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kontrola negatywna dla testu wyżej: po zdjęciu indeksu ten sam zapis przechodzi.
    /// Bez tego nie wiadomo, czy <see cref="TestDb_wymusza_unikalny_indeks"/> rzeczywiście
    /// bada indeks, czy przypadkiem coś innego.
    /// </summary>
    [Fact]
    public void Bez_indeksu_ten_sam_numer_przechodzi()
    {
        using var db = TestDb.Create();
        db.Execute("DROP INDEX IX_Members_MembershipNumber");

        db.Context.Members.Add(Member("001"));
        db.Context.SaveChanges();

        using var second = db.NewContext();
        second.Members.Add(Member("001"));
        second.SaveChanges();

        using var verification = db.NewContext();
        Assert.Equal(2, verification.Members.Count(m => m.MembershipNumber == "001"));
    }

    private static Member Member(string membershipNumber) => new()
    {
        MembershipNumber = membershipNumber,
        FirstName = "Jan",
        LastName = "Testowy"
    };
}
