using Vault.Models.DTOs.Products;

namespace Vault.Services.Interfaces;

/// <summary>
/// Service interface for product operations
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Retrieves a paginated list of products with optional filtering and sorting
    /// </summary>
    /// <param name="page">Page number (minimum 1)</param>
    /// <param name="pageSize">Number of items per page (1-100)</param>
    /// <param name="name">Optional product name filter</param>
    /// <param name="minPrice">Optional minimum price filter</param>
    /// <param name="maxPrice">Optional maximum price filter</param>
    /// <param name="sortBy">Sort field (name or price)</param>
    /// <param name="sortOrder">Sort order (asc or desc)</param>
    /// <returns>Paginated product response</returns>
    Task<PagedProductResponse> GetAllAsync(
        int page,
        int pageSize,
        string? name,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        string? sortOrder);

    /// <summary>
    /// Retrieves a product by ID
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>Product response</returns>
    Task<ProductResponse> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="request">The product creation request</param>
    /// <returns>Created product response</returns>
    Task<ProductResponse> CreateAsync(CreateProductRequest request);

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <param name="request">The product update request</param>
    /// <returns>Updated product response</returns>
    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request);

    /// <summary>
    /// Deletes a product
    /// </summary>
    /// <param name="id">The product ID</param>
    Task DeleteAsync(int id);
}
