using System.ComponentModel.DataAnnotations;

namespace Vault.Models.DTOs.Auth;

/// <summary>
/// Request DTO for user login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the email address
    /// </summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
