using Microsoft.AspNetCore.Identity;

namespace MKS.DeepWater.Web.Data;

/// <summary>Konto użytkownika aplikacji (obsługa spółki).</summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
