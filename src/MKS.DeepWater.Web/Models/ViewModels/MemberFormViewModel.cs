using System.ComponentModel.DataAnnotations;
using MKS.DeepWater.Web.Models.Domain;

namespace MKS.DeepWater.Web.Models.ViewModels;

/// <summary>Dane członka wraz z adresem zameldowania - formularz dodawania i edycji.</summary>
public class MemberFormViewModel
{
    public int Id { get; set; }

    public bool IsNew => Id == 0;

    [Required(ErrorMessage = "Podaj numer członkowski.")]
    [StringLength(20, ErrorMessage = "Numer członkowski może mieć maksymalnie 20 znaków.")]
    [Display(Name = "Numer członkowski")]
    public string MembershipNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj imię.")]
    [StringLength(100, ErrorMessage = "Imię może mieć maksymalnie 100 znaków.")]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj nazwisko.")]
    [StringLength(100, ErrorMessage = "Nazwisko może mieć maksymalnie 100 znaków.")]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(11, MinimumLength = 11, ErrorMessage = "PESEL ma 11 cyfr.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL ma 11 cyfr.")]
    [Display(Name = "PESEL")]
    public string? NationalId { get; set; }

    [StringLength(254, ErrorMessage = "E-mail może mieć maksymalnie 254 znaki.")]
    [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "Telefon może mieć maksymalnie 30 znaków.")]
    [Phone(ErrorMessage = "Nieprawidłowy numer telefonu.")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Data urodzenia")]
    [DataType(DataType.Date)]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "Podaj datę przyjęcia.")]
    [Display(Name = "Data przyjęcia")]
    [DataType(DataType.Date)]
    public DateOnly? JoinDate { get; set; }

    [Display(Name = "Data ustania członkostwa")]
    [DataType(DataType.Date)]
    public DateOnly? LeaveDate { get; set; }

    [Required(ErrorMessage = "Wybierz status członkostwa.")]
    [Display(Name = "Status")]
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    [Required(ErrorMessage = "Podaj liczbę udziałów.")]
    [Range(0, 10_000, ErrorMessage = "Liczba udziałów musi być z zakresu 0-10000.")]
    [Display(Name = "Liczba udziałów")]
    public int SharesCount { get; set; } = 1;

    [StringLength(1000, ErrorMessage = "Uwagi mogą mieć maksymalnie 1000 znaków.")]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }

    // --- Adres zameldowania (opcjonalny jako całość) ---

    [StringLength(150, ErrorMessage = "Ulica może mieć maksymalnie 150 znaków.")]
    [Display(Name = "Ulica")]
    public string? Street { get; set; }

    [StringLength(20, ErrorMessage = "Numer domu może mieć maksymalnie 20 znaków.")]
    [Display(Name = "Nr domu")]
    public string? HouseNumber { get; set; }

    [StringLength(20, ErrorMessage = "Numer lokalu może mieć maksymalnie 20 znaków.")]
    [Display(Name = "Nr lokalu")]
    public string? ApartmentNumber { get; set; }

    [StringLength(10, ErrorMessage = "Kod pocztowy może mieć maksymalnie 10 znaków.")]
    [Display(Name = "Kod pocztowy")]
    public string? PostalCode { get; set; }

    [StringLength(100, ErrorMessage = "Miejscowość może mieć maksymalnie 100 znaków.")]
    [Display(Name = "Miejscowość")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "Nazwa kraju może mieć maksymalnie 100 znaków.")]
    [Display(Name = "Kraj")]
    public string? Country { get; set; } = "Polska";

    /// <summary>Czy w bloku adresowym cokolwiek wpisano.</summary>
    public bool HasAddress =>
        !string.IsNullOrWhiteSpace(Street) ||
        !string.IsNullOrWhiteSpace(HouseNumber) ||
        !string.IsNullOrWhiteSpace(ApartmentNumber) ||
        !string.IsNullOrWhiteSpace(PostalCode) ||
        !string.IsNullOrWhiteSpace(City);

    /// <summary>
    /// Reguły obejmujące kilka pól. Wywoływane jawnie z kontrolera - IValidatableObject
    /// jest przez walidator pomijany, gdy któreś pojedyncze pole jest już niepoprawne.
    /// </summary>
    public IEnumerable<ValidationResult> GetRuleViolations()
    {
        // Adresu można nie podawać, ale podany musi być kompletny.
        if (HasAddress)
        {
            if (string.IsNullOrWhiteSpace(Street))
            {
                yield return new ValidationResult("Podaj ulicę.", new[] { nameof(Street) });
            }

            if (string.IsNullOrWhiteSpace(HouseNumber))
            {
                yield return new ValidationResult("Podaj numer domu.", new[] { nameof(HouseNumber) });
            }

            if (string.IsNullOrWhiteSpace(PostalCode))
            {
                yield return new ValidationResult("Podaj kod pocztowy.", new[] { nameof(PostalCode) });
            }

            if (string.IsNullOrWhiteSpace(City))
            {
                yield return new ValidationResult("Podaj miejscowość.", new[] { nameof(City) });
            }
        }

        if (LeaveDate.HasValue && JoinDate.HasValue && LeaveDate < JoinDate)
        {
            yield return new ValidationResult(
                "Data ustania członkostwa nie może być wcześniejsza niż data przyjęcia.",
                new[] { nameof(LeaveDate) });
        }

        if (BirthDate.HasValue && BirthDate > DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult("Data urodzenia nie może być z przyszłości.",
                new[] { nameof(BirthDate) });
        }
    }
}
