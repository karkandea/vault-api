namespace Vault.Models.DTOs.Products;

/// <summary>
/// Response DTO for paginated product data
/// </summary>
public class PagedProductResponse
{
    /// <summary>
    /// Gets or sets the list of products
    /// </summary>
    public List<ProductResponse> Data { get; set; } = new();

    /// <summary>
    /// Gets or sets the current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages
    /// </summary>
    public int TotalPages { get; set; }
}
