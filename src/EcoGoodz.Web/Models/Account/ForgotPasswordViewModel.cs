using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.Account;

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
