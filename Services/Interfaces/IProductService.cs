using Vault.Models.DTOs.Products;

namespace Vault.Services.Interfaces;

/// <summary>
/// Service interface for product operations
/// </summary>
public interface IProductService
{
    Task<PagedProductResponse> GetAllAsync(
        int page,
        int pageSize,
        string? name,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        string? sortOrder);

    Task<ProductResponse> GetByIdAsync(int id);
    Task<ProductResponse> CreateAsync(CreateProductRequest request);
    Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request);
    Task DeleteAsync(int id);
    Task<ProductResponse> UpdateProductImageAsync(int id, IFormFile file);
}
