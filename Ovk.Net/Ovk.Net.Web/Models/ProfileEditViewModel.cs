using System.ComponentModel.DataAnnotations;

namespace Ovk.Net.Web.Models;

public class ProfileEditViewModel
{
    public string Mode { get; set; } = "main";
    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "Имя")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(255)]
    [Display(Name = "Статус")]
    public string? Status { get; set; }

    [Display(Name = "Пол")]
    public bool Sex { get; set; } = true;

    [StringLength(255)]
    [Display(Name = "Родной город")]
    public string? Hometown { get; set; }

    [StringLength(255)]
    [Display(Name = "Город")]
    public string? City { get; set; }

    [Display(Name = "Интересы")]
    public string? Info { get; set; }

    [Display(Name = "О себе")]
    public string? About { get; set; }

    [EmailAddress, StringLength(90)]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(36)]
    [Display(Name = "Телефон")]
    public string? Phone { get; set; }

    public ulong ProfileId { get; set; }
}
