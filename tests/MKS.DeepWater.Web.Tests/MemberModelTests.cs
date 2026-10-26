using MKS.DeepWater.Web.Controllers;
using MKS.DeepWater.Web.Models.Domain;
using MKS.DeepWater.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MKS.DeepWater.Web.Tests;

/// <summary>
/// Uproszczony model członka: bez PESEL-u, e-maila, daty urodzenia i dat członkostwa,
/// z jednym adresem zapisanym wprost na rekordzie.
/// </summary>
public class MemberModelTests
{
    [Theory]
    [InlineData("NationalId")]
    [InlineData("Email")]
    [InlineData("BirthDate")]
    [InlineData("JoinDate")]
    [InlineData("LeaveDate")]
    public void Tabela_czlonkow_nie_ma_usunietej_kolumny(string kolumna)
    {
        using var db = TestDb.Create();

        var createTable = db.QuerySingleText(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Members'");

        Assert.NotNull(createTable);
        Assert.DoesNotContain(kolumna, createTable, StringComparison.Ordinal);
    }

    [Fact]
    public void Tabela_adresow_nie_istnieje()
    {
        using var db = TestDb.Create();

        var nazwa = db.QuerySingleText(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'Addresses'");

        Assert.Null(nazwa);
    }

    [Fact]
    public void Adres_zapisuje_sie_na_rekordzie_czlonka()
    {
        using var db = TestDb.Create();
        db.Context.Members.Add(new Member
        {
            MembershipNumber = "001",
            FirstName = "Anna",
            LastName = "Kowalska",
            Street = "Morska",
            HouseNumber = "14",
            PostalCode = "81-001",
            City = "Gdynia"
        });
        db.Context.SaveChanges();

        using var verification = db.NewContext();
        var member = verification.Members.Single();

        Assert.Equal("Morska", member.Street);
        Assert.Equal("14", member.HouseNumber);
        Assert.Equal("81-001", member.PostalCode);
        Assert.Equal("Gdynia", member.City);
    }

    [Theory]
    [InlineData("Morska", "14", "81-001", "Gdynia", "Morska 14, 81-001 Gdynia")]
    [InlineData("Morska", "14", null, null, "Morska 14")]
    [InlineData(null, null, "81-001", "Gdynia", "81-001 Gdynia")]
    [InlineData(null, null, null, "Gdynia", "Gdynia")]
    [InlineData("Morska", "14", null, "Gdynia", "Morska 14, Gdynia")]
    [InlineData("Morska", null, "81-001", "Gdynia", "Morska, 81-001 Gdynia")]
    [InlineData(null, null, null, null, "")]
    public void AddressLine_pomija_brakujace_czesci(
        string? street, string? houseNumber, string? postalCode, string? city, string oczekiwany)
    {
        Assert.Equal(oczekiwany, Member.FormatAddress(street, houseNumber, postalCode, city));
    }

    [Fact]
    public void Niekompletny_adres_jest_odrzucany()
    {
        var form = Form();
        form.Street = "Morska";

        var bledy = form.GetRuleViolations().SelectMany(v => v.MemberNames).ToList();

        Assert.Contains(nameof(MemberFormViewModel.HouseNumber), bledy);
        Assert.Contains(nameof(MemberFormViewModel.PostalCode), bledy);
        Assert.Contains(nameof(MemberFormViewModel.City), bledy);
        Assert.DoesNotContain(nameof(MemberFormViewModel.Street), bledy);
    }

    [Fact]
    public void Sam_kod_pocztowy_wymaga_reszty_adresu()
    {
        var form = Form();
        form.PostalCode = "81-001";

        var bledy = form.GetRuleViolations().SelectMany(v => v.MemberNames).ToList();

        Assert.Contains(nameof(MemberFormViewModel.Street), bledy);
        Assert.Contains(nameof(MemberFormViewModel.HouseNumber), bledy);
        Assert.Contains(nameof(MemberFormViewModel.City), bledy);
        Assert.DoesNotContain(nameof(MemberFormViewModel.PostalCode), bledy);
    }

    [Fact]
    public void Pusty_adres_jest_dozwolony()
    {
        Assert.Empty(Form().GetRuleViolations());
    }

    [Fact]
    public void Kompletny_adres_przechodzi_walidacje()
    {
        var form = Form();
        form.Street = "Morska";
        form.HouseNumber = "14";
        form.PostalCode = "81-001";
        form.City = "Gdynia";

        Assert.Empty(form.GetRuleViolations());
    }

    [Fact]
    public async Task Lista_czlonkow_pokazuje_adres_z_pol_rekordu()
    {
        using var db = TestDb.Create();
        db.Context.Members.Add(new Member
        {
            MembershipNumber = "001",
            FirstName = "Anna",
            LastName = "Kowalska",
            Street = "Morska",
            HouseNumber = "14",
            PostalCode = "81-001",
            City = "Gdynia"
        });
        await db.Context.SaveChangesAsync(CancellationToken.None);

        var controller = new MembersController(db.Context);
        var result = await controller.Index(search: null, status: null, feeYear: null,
                                            showDeleted: false, ct: CancellationToken.None);

        var model = Assert.IsType<MemberListViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Morska 14, 81-001 Gdynia", model.Items.Single().AddressLine);
    }

    private static MemberFormViewModel Form() => new()
    {
        MembershipNumber = "001",
        FirstName = "Anna",
        LastName = "Kowalska"
    };
}
