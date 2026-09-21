using System.ComponentModel.DataAnnotations;

namespace EcoGoodz.Web.Models.Account;

/// <summary>
/// Used for the mandatory one-time password reset that every migrated user must
/// complete on first login, since legacy plaintext passwords cannot be migrated.
/// </summary>
public class ForcePasswordChangeViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
