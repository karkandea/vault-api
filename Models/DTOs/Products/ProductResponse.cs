namespace Vault.Models.DTOs.Products;

/// <summary>
/// Response DTO for product data
/// </summary>
public class ProductResponse
{
    /// <summary>
    /// Gets or sets the product ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the product price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
