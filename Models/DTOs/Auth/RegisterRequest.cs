using System.ComponentModel.DataAnnotations;

namespace Vault.Models.DTOs.Auth;

/// <summary>
/// Request DTO for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Gets or sets the username
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}
