using System.ComponentModel.DataAnnotations;

namespace Vault.Models.Entities;

/// <summary>
/// Represents a user in the system with authentication credentials
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the username for the user account
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address used for authentication
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the BCrypt hashed password for secure authentication
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
