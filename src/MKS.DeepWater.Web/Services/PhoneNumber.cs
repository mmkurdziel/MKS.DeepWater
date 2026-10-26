using System.Text.RegularExpressions;

namespace MKS.DeepWater.Web.Services;

/// <summary>
/// Jedyne miejsce w aplikacji, które wie, czym jest numer telefonu i jak wygląda
/// kierunkowy. W bazie numer trzymany jest jako dokładnie 9 cyfr, bez kierunkowego;
/// <see cref="CountryCode"/> doklejany jest wyłącznie przy wysyłce SMS.
/// </summary>
public static partial class PhoneNumber
{
    /// <summary>Kierunkowy Polski. Obsługiwane są tylko numery polskie.</summary>
    public const string CountryCode = "+48";

    /// <summary>Dokładnie 9 cyfr, bez wiodącego zera, bez spacji i znaków specjalnych.</summary>
    public const string Pattern = "^[1-9][0-9]{8}$";

    /// <summary>
    /// Prefiksy komórkowe z planu numeracji UKE. Lista jest ruchoma - operatorzy dostają nowe
    /// zakresy - więc służy <b>tylko</b> do ostrzeżenia użytkownika, nigdy do blokowania
    /// zapisu. Numer spoza listy przechodzi walidację, jeśli pasuje do <see cref="Pattern"/>.
    /// </summary>
    private static readonly string[] MobilePrefixes =
        ["45", "50", "51", "53", "57", "60", "66", "69", "72", "73", "78", "79", "88"];

    [GeneratedRegex(Pattern)]
    private static partial Regex PatternRegex();

    /// <summary>Czy numer da się zapisać w bazie: dokładnie 9 cyfr bez wiodącego zera.</summary>
    public static bool IsValid(string? number) =>
        number is not null && PatternRegex().IsMatch(number);

    /// <summary>Numer w formacie wymaganym przez bramkę SMS: <c>+48600100200</c>.</summary>
    public static string ToGatewayFormat(string number) => CountryCode + number;

    /// <summary>
    /// Numer do pokazania na ekranie: <c>600 100 200</c>. Pusty string dla braku numeru.
    /// Numer w nieoczekiwanym formacie zwracany jest bez zmian - lepiej pokazać go
    /// takim, jaki jest w bazie, niż ukryć przed użytkownikiem.
    /// </summary>
    public static string ToDisplay(string? number) =>
        IsValid(number)
            ? $"{number![..3]} {number[3..6]} {number[6..]}"
            : number ?? string.Empty;

    /// <summary>
    /// Czy numer wygląda na komórkowy. Tylko wskazówka do ostrzeżenia - SMS na numer
    /// stacjonarny nie dojdzie, ale decyzję o zapisie podejmuje użytkownik.
    /// </summary>
    public static bool LooksLikeMobile(string? number) =>
        IsValid(number) && MobilePrefixes.Contains(number![..2]);
}
