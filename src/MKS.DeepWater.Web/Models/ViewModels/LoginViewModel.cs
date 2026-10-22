using System.ComponentModel.DataAnnotations;

namespace MKS.DeepWater.Web.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Podaj login.")]
    [Display(Name = "Login")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj hasło.")]
    [DataType(DataType.Password)]
    [Display(Name = "Hasło")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Zapamiętaj mnie")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
