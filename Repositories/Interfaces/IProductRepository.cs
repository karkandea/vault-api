using Vault.Models.Entities;

namespace Vault.Repositories.Interfaces;

/// <summary>
/// Repository interface for Product entity operations
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Retrieves all products from the database
    /// </summary>
    /// <returns>A collection of all products</returns>
    Task<IEnumerable<Product>> GetAllAsync();

    /// <summary>
    /// Retrieves a product by its unique identifier
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>The product if found, otherwise null</returns>
    Task<Product?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new product in the database
    /// </summary>
    /// <param name="product">The product to create</param>
    /// <returns>The created product with generated ID</returns>
    Task<Product> CreateAsync(Product product);

    /// <summary>
    /// Updates an existing product in the database
    /// </summary>
    /// <param name="id">The ID of the product to update</param>
    /// <param name="product">The updated product data</param>
    /// <returns>The updated product if found, otherwise null</returns>
    Task<Product?> UpdateAsync(int id, Product product);

    /// <summary>
    /// Deletes a product from the database
    /// </summary>
    /// <param name="id">The ID of the product to delete</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Searches for products by name and/or price range with pagination and sorting
    /// </summary>
    /// <param name="name">Optional product name filter (case-insensitive, partial match)</param>
    /// <param name="minPrice">Optional minimum price filter</param>
    /// <param name="maxPrice">Optional maximum price filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="sortBy">Field to sort by (price or name)</param>
    /// <param name="sortOrder">Sort order (desc for descending, anything else for ascending)</param>
    /// <returns>A tuple containing the list of products and total count before pagination</returns>
    Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        string? name,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder);
}
