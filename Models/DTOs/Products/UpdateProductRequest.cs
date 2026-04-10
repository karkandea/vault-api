using System.ComponentModel.DataAnnotations;

namespace Vault.Models.DTOs.Products;

/// <summary>
/// Request DTO for updating an existing product
/// </summary>
public class UpdateProductRequest
{
    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price
    /// </summary>
    [Required]
    [Range(100000, 10000000, ErrorMessage = "Price must be between 100,000 and 10,000,000")]
    public decimal Price { get; set; }
}
