namespace Vault.Models.DTOs.Auth;

/// <summary>
/// Response DTO for authentication operations
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Gets or sets the JWT token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the user ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
