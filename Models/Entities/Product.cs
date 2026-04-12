using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Vault.Models.Entities;

/// <summary>
/// Represents a product in the inventory system
/// </summary>
public class Product
{
    /// <summary>
    /// Gets or sets the unique identifier for the product
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the detailed description of the product (optional)
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price in the system currency
    /// </summary>
    [Required]
    [Range(100000, 10000000, ErrorMessage = "Price must be between 100,000 and 10,000,000")]
    [Precision(18, 2)]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the product was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the product was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the URL of the product image stored in Supabase Storage
    /// </summary>
    [MaxLength(2000)]
    public string? ImageUrl { get; set; }
}
