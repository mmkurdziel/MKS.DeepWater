using System.ComponentModel.DataAnnotations;

namespace MKS.DeepWater.Web.Models.Domain;

/// <summary>Stawka składki członkowskiej obowiązująca w danym roku (na udział).</summary>
public class FeeRate
{
    public int Id { get; set; }

    [Display(Name = "Rok")]
    public int Year { get; set; }

    [Display(Name = "Stawka za udział")]
    [Range(0.01, 1_000_000)]
    public decimal AmountPerShare { get; set; }

    [StringLength(300)]
    [Display(Name = "Podstawa (uchwała)")]
    public string? ResolutionReference { get; set; }
}
