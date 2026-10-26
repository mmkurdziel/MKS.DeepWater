using System.ComponentModel.DataAnnotations;

namespace MKS.DeepWater.Web.Models.Domain;

/// <summary>
/// Członek spółki. Spółka nie zbiera PESEL-u, e-maila, daty urodzenia ani dat
/// przyjęcia i ustania członkostwa - tych pól świadomie nie ma w modelu.
/// </summary>
public class Member
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Numer członkowski")]
    public string MembershipNumber { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(30), Phone]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    // --- Adres: jeden na członka, wprost na rekordzie (bez osobnej tabeli) ---

    [StringLength(150)]
    [Display(Name = "Ulica")]
    public string? Street { get; set; }

    [StringLength(20)]
    [Display(Name = "Nr domu")]
    public string? HouseNumber { get; set; }

    [StringLength(10)]
    [Display(Name = "Kod pocztowy")]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    [Display(Name = "Miejscowość")]
    public string? City { get; set; }

    [Display(Name = "Status")]
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    [Display(Name = "Liczba udziałów")]
    public int SharesCount { get; set; } = 1;

    [StringLength(1000)]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>Znacznik miękkiego usunięcia - rekord zostaje w bazie i można go przywrócić.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    [StringLength(256)]
    public string? DeletedBy { get; set; }

    public bool IsDeleted => DeletedAt is not null;

    public ICollection<FeePayment> Payments { get; set; } = new List<FeePayment>();

    [Display(Name = "Imię i nazwisko")]
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Adres w jednej linii - pusty string, gdy adresu nie podano.</summary>
    [Display(Name = "Adres")]
    public string AddressLine => FormatAddress(Street, HouseNumber, PostalCode, City);

    /// <summary>
    /// Składanie adresu w jednym miejscu, żeby lista członków i karta członka nie
    /// rozjechały się w formatowaniu. Pomija części, których nie podano.
    /// Układ polski: "Morska 14, 81-001 Gdynia".
    /// </summary>
    public static string FormatAddress(string? street, string? houseNumber,
        string? postalCode, string? city)
    {
        var ulica = Join(" ", street, houseNumber);
        var miejscowosc = Join(" ", postalCode, city);
        return Join(", ", ulica, miejscowosc);
    }

    private static string Join(string separator, params string?[] parts) =>
        string.Join(separator, parts.Where(part => !string.IsNullOrWhiteSpace(part)));
}
