using System.ComponentModel.DataAnnotations;
using MKS.DeepWater.Web.Models.Domain;

namespace MKS.DeepWater.Web.Models.ViewModels;

/// <summary>Dane członka wraz z adresem zameldowania - formularz dodawania i edycji.</summary>
public class MemberFormViewModel : IValidatableObject
{
    public int Id { get; set; }

    public bool IsNew => Id == 0;

    [Required(ErrorMessage = "Podaj numer członkowski.")]
    [StringLength(20)]
    [Display(Name = "Numer członkowski")]
    public string MembershipNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj imię.")]
    [StringLength(100)]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj nazwisko.")]
    [StringLength(100)]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(11, MinimumLength = 11, ErrorMessage = "PESEL ma 11 cyfr.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL ma 11 cyfr.")]
    [Display(Name = "PESEL")]
    public string? NationalId { get; set; }

    [StringLength(254)]
    [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(30)]
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

    [Display(Name = "Status")]
    public MemberStatus Status { get; set; } = MemberStatus.Active;

    [Range(0, 10_000, ErrorMessage = "Liczba udziałów musi być z zakresu 0-10000.")]
    [Display(Name = "Liczba udziałów")]
    public int SharesCount { get; set; } = 1;

    [StringLength(1000)]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }

    // --- Adres zameldowania (opcjonalny jako całość) ---

    [StringLength(150)]
    [Display(Name = "Ulica")]
    public string? Street { get; set; }

    [StringLength(20)]
    [Display(Name = "Nr domu")]
    public string? HouseNumber { get; set; }

    [StringLength(20)]
    [Display(Name = "Nr lokalu")]
    public string? ApartmentNumber { get; set; }

    [StringLength(10)]
    [Display(Name = "Kod pocztowy")]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    [Display(Name = "Miejscowość")]
    public string? City { get; set; }

    [StringLength(100)]
    [Display(Name = "Kraj")]
    public string? Country { get; set; } = "Polska";

    /// <summary>Czy w bloku adresowym cokolwiek wpisano.</summary>
    public bool HasAddress =>
        !string.IsNullOrWhiteSpace(Street) ||
        !string.IsNullOrWhiteSpace(HouseNumber) ||
        !string.IsNullOrWhiteSpace(ApartmentNumber) ||
        !string.IsNullOrWhiteSpace(PostalCode) ||
        !string.IsNullOrWhiteSpace(City);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
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
