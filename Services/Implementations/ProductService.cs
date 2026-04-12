using Microsoft.Extensions.Caching.Memory;
using Vault.Models.DTOs.Products;
using Vault.Models.Entities;
using Vault.Repositories.Interfaces;
using Vault.Services.Interfaces;

namespace Vault.Services.Implementations;

/// <summary>
/// Service implementation for product operations
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;
    private readonly IMemoryCache _cache;
    private static int _cacheVersion = 0;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger, IMemoryCache cache)
    {
        _productRepository = productRepository;
        _logger = logger;
        _cache = cache;
    }

    public async Task<PagedProductResponse> GetAllAsync(
        int page,
        int pageSize,
        string? name,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortBy,
        string? sortOrder)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            throw new ArgumentException("Page must be at least 1", nameof(page));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("PageSize must be between 1 and 100", nameof(pageSize));
        }

        // Validate price range
        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
        {
            throw new ArgumentException("minPrice cannot be greater than maxPrice");
        }

        _logger.LogInformation("Retrieving products - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var cacheKey = $"products_v{_cacheVersion}_{page}_{pageSize}_{name}_{minPrice}_{maxPrice}_{sortBy}_{sortOrder}";

        if (_cache.TryGetValue(cacheKey, out PagedProductResponse? cachedResponse) && cachedResponse != null)
        {
            _logger.LogInformation("Returning cached products");
            return cachedResponse;
        }

        var (items, totalCount) = await _productRepository.SearchAsync(
            name,
            minPrice,
            maxPrice,
            page,
            pageSize,
            sortBy,
            sortOrder);

        var productResponses = items.Select(p => new ProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var response = new PagedProductResponse
        {
            Data = productResponses,
            Page = page,
            PageSize = pageSize,
            Total = totalCount,
            TotalPages = totalPages
        };

        _cache.Set(cacheKey, response, CacheDuration);
        _logger.LogInformation("Cached products");

        return response;
    }

    public async Task<ProductResponse> GetByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving product with ID: {ProductId}", id);

        var cacheKey = $"product_{id}";

        if (_cache.TryGetValue(cacheKey, out ProductResponse? cachedProduct) && cachedProduct != null)
        {
            _logger.LogInformation("Returning cached product: {ProductId}", id);
            return cachedProduct;
        }

        var product = await _productRepository.GetByIdAsync(id);

        if (product == null)
        {
            _logger.LogWarning("Product not found: {ProductId}", id);
            throw new KeyNotFoundException("Product not found");
        }

        var response = new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        _cache.Set(cacheKey, response, CacheDuration);
        _logger.LogInformation("Cached product: {ProductId}", id);

        return response;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        _logger.LogInformation("Creating new product: {ProductName}", request.Name);

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price
        };

        var createdProduct = await _productRepository.CreateAsync(product);

        _logger.LogInformation("Product created successfully: {ProductId}", createdProduct.Id);

        InvalidateProductsListCache();

        return new ProductResponse
        {
            Id = createdProduct.Id,
            Name = createdProduct.Name,
            Description = createdProduct.Description,
            Price = createdProduct.Price,
            CreatedAt = createdProduct.CreatedAt,
            UpdatedAt = createdProduct.UpdatedAt
        };
    }

    public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request)
    {
        _logger.LogInformation("Updating product: {ProductId}", id);

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price
        };

        var updatedProduct = await _productRepository.UpdateAsync(id, product);

        if (updatedProduct == null)
        {
            _logger.LogWarning("Product not found for update: {ProductId}", id);
            throw new KeyNotFoundException("Product not found");
        }

        _logger.LogInformation("Product updated successfully: {ProductId}", id);

        _cache.Remove($"product_{id}");
        InvalidateProductsListCache();

        return new ProductResponse
        {
            Id = updatedProduct.Id,
            Name = updatedProduct.Name,
            Description = updatedProduct.Description,
            Price = updatedProduct.Price,
            CreatedAt = updatedProduct.CreatedAt,
            UpdatedAt = updatedProduct.UpdatedAt
        };
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting product: {ProductId}", id);

        var result = await _productRepository.DeleteAsync(id);

        if (!result)
        {
            _logger.LogWarning("Product not found for deletion: {ProductId}", id);
            throw new KeyNotFoundException("Product not found");
        }

        _logger.LogInformation("Product deleted successfully: {ProductId}", id);

        _cache.Remove($"product_{id}");
        InvalidateProductsListCache();
    }

    private void InvalidateProductsListCache()
    {
        _cacheVersion++;
        _logger.LogInformation("Product list cache invalidated");
    }
}
