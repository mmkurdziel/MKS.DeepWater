using System.ComponentModel.DataAnnotations;
using MKS.DeepWater.Web.Models.Validation;
using MKS.DeepWater.Web.Services;

namespace MKS.DeepWater.Web.Tests;

/// <summary>
/// Numer telefonu jest w tej aplikacji loginem, więc format musi być jednoznaczny:
/// dokładnie 9 cyfr w bazie, kierunkowy doklejany tylko przy wysyłce SMS.
/// </summary>
public class PhoneNumberTests
{
    [Fact]
    public void IsValid_przyjmuje_dziewiec_cyfr()
    {
        Assert.True(PhoneNumber.IsValid("600100200"));
    }

    [Theory]
    [InlineData("600 100 200")]
    [InlineData("600-100-200")]
    [InlineData("+48600100200")]
    [InlineData("0048600100200")]
    [InlineData("(600)100200")]
    public void IsValid_odrzuca_spacje_i_znaki_specjalne(string numer)
    {
        Assert.False(PhoneNumber.IsValid(numer));
    }

    [Theory]
    [InlineData("60010020")]
    [InlineData("6001002000")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_odrzuca_zla_dlugosc(string? numer)
    {
        Assert.False(PhoneNumber.IsValid(numer));
    }

    [Theory]
    [InlineData("0600100200")]
    [InlineData("012345678")]
    public void IsValid_odrzuca_wiodace_zero(string numer)
    {
        Assert.False(PhoneNumber.IsValid(numer));
    }

    [Fact]
    public void IsValid_odrzuca_litery()
    {
        Assert.False(PhoneNumber.IsValid("60010020a"));
    }

    [Fact]
    public void ToGatewayFormat_doklada_kierunkowy()
    {
        Assert.Equal("+48600100200", PhoneNumber.ToGatewayFormat("600100200"));
    }

    [Fact]
    public void ToDisplay_formatuje_w_trzy_grupy()
    {
        Assert.Equal("600 100 200", PhoneNumber.ToDisplay("600100200"));
        Assert.Equal(string.Empty, PhoneNumber.ToDisplay(null));
    }

    [Theory]
    [InlineData("600100200", true)]
    [InlineData("581234567", false)] // stacjonarny Gdańsk - SMS nie dojdzie
    public void LooksLikeMobile_rozpoznaje_prefiksy(string numer, bool oczekiwany)
    {
        Assert.Equal(oczekiwany, PhoneNumber.LooksLikeMobile(numer));
    }

    [Fact]
    public void Atrybut_zwraca_komunikat_o_dziewieciu_cyfrach()
    {
        var atrybut = new PolishMobileNumberAttribute();

        var wynik = atrybut.GetValidationResult("600 100 200", new ValidationContext(new object()));

        Assert.NotNull(wynik);
        Assert.Equal("Numer telefonu to 9 cyfr, bez spacji i znaków specjalnych.",
            wynik.ErrorMessage);
    }
}
