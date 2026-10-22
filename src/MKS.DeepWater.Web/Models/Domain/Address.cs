using System.ComponentModel.DataAnnotations;

namespace MKS.DeepWater.Web.Models.Domain;

/// <summary>Adres członka (zameldowania, korespondencyjny, inny).</summary>
public class Address
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member? Member { get; set; }

    [Display(Name = "Rodzaj adresu")]
    public AddressType Type { get; set; } = AddressType.Registered;

    [Required, StringLength(150)]
    [Display(Name = "Ulica")]
    public string Street { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [Display(Name = "Nr domu")]
    public string HouseNumber { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Nr lokalu")]
    public string? ApartmentNumber { get; set; }

    [Required, StringLength(10)]
    [Display(Name = "Kod pocztowy")]
    public string PostalCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Miejscowość")]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Kraj")]
    public string Country { get; set; } = "Polska";

    [Display(Name = "Adres")]
    public string SingleLine
    {
        get
        {
            var house = string.IsNullOrWhiteSpace(ApartmentNumber)
                ? HouseNumber
                : $"{HouseNumber}/{ApartmentNumber}";
            return $"{Street} {house}, {PostalCode} {City}";
        }
    }
}
