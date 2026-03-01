using System.ComponentModel.DataAnnotations;

namespace EmailServiceAPI.Models;

public class ContactFormRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SurName { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [AllowedValues("general", "project", "feedback", "other")]
    public string QueryType { get; set; } = string.Empty;

    [Required]
    [MaxLength(5000)]
    public string Message { get; set; } = string.Empty;

    public string? Website { get; set; }
}
