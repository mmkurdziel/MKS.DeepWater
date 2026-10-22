using System.ComponentModel.DataAnnotations;

namespace MKS.DeepWater.Web.Models.Domain;

/// <summary>Członek spółki - dane podstawowe.</summary>
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

    [StringLength(11)]
    [Display(Name = "PESEL")]
    public string? NationalId { get; set; }

    [StringLength(254), EmailAddress]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(30), Phone]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Data urodzenia")]
    public DateOnly? BirthDate { get; set; }

    [Display(Name = "Data przyjęcia")]
    public DateOnly JoinDate { get; set; }

    [Display(Name = "Data ustania członkostwa")]
    public DateOnly? LeaveDate { get; set; }

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

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<FeePayment> Payments { get; set; } = new List<FeePayment>();

    [Display(Name = "Imię i nazwisko")]
    public string FullName => $"{FirstName} {LastName}";
}
