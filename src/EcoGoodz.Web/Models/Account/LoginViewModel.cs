using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.Account;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Email or username")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
