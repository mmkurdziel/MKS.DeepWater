using System.ComponentModel.DataAnnotations;

namespace MKS.DeepWater.Web.Models.Domain;

/// <summary>Wpłata składki członkowskiej.</summary>
public class FeePayment
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member? Member { get; set; }

    [Display(Name = "Rok składkowy")]
    public int FeeYear { get; set; }

    [Display(Name = "Data wpłaty")]
    public DateOnly PaymentDate { get; set; }

    [Display(Name = "Kwota")]
    [Range(0.01, 1_000_000)]
    public decimal Amount { get; set; }

    [Display(Name = "Forma wpłaty")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Transfer;

    [StringLength(50)]
    [Display(Name = "Nr dokumentu")]
    public string? DocumentNumber { get; set; }

    [StringLength(500)]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
