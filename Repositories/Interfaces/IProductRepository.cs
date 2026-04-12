using Vault.Models.Entities;

namespace Vault.Repositories.Interfaces;

/// <summary>
/// Repository interface for Product entity operations
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<Product> CreateAsync(Product product);
    Task<Product?> UpdateAsync(int id, Product product);
    Task<bool> DeleteAsync(int id);
    Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        string? name,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder);
}
