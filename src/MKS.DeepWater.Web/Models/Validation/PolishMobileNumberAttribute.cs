using System.ComponentModel.DataAnnotations;
using MKS.DeepWater.Web.Services;

namespace MKS.DeepWater.Web.Models.Validation;

/// <summary>
/// Sprawdza, że numer telefonu jest zapisany tak, jak trzyma go baza: dokładnie 9 cyfr,
/// bez kierunkowego i bez separatorów. Puste pole przechodzi - o tym, czy numer jest
/// wymagany, decyduje <see cref="RequiredAttribute"/> na konkretnej właściwości.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class PolishMobileNumberAttribute : ValidationAttribute
{
    public const string Message = "Numer telefonu to 9 cyfr, bez spacji i znaków specjalnych.";

    public PolishMobileNumberAttribute() : base(Message)
    {
    }

    public override bool IsValid(object? value) => value switch
    {
        null => true,
        string number when string.IsNullOrWhiteSpace(number) => true,
        string number => PhoneNumber.IsValid(number),
        _ => false
    };
}
