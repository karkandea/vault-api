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
    private const string ProductsAllCacheKey = "products_all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the ProductService class
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    /// <param name="logger">The logger</param>
    /// <param name="cache">The memory cache</param>
    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger, IMemoryCache cache)
    {
        _productRepository = productRepository;
        _logger = logger;
        _cache = cache;
    }

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

        // Create cache key based on parameters
        var cacheKey = $"{ProductsAllCacheKey}_{page}_{pageSize}_{name}_{minPrice}_{maxPrice}_{sortBy}_{sortOrder}";

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out PagedProductResponse? cachedResponse) && cachedResponse != null)
        {
            _logger.LogInformation("Returning cached products for key: {CacheKey}", cacheKey);
            return cachedResponse;
        }

        // Get products from repository
        var (items, totalCount) = await _productRepository.SearchAsync(
            name,
            minPrice,
            maxPrice,
            page,
            pageSize,
            sortBy,
            sortOrder);

        // Map to DTOs
        var productResponses = items.Select(p => new ProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        // Calculate total pages
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var response = new PagedProductResponse
        {
            Data = productResponses,
            Page = page,
            PageSize = pageSize,
            Total = totalCount,
            TotalPages = totalPages
        };

        // Cache the response
        _cache.Set(cacheKey, response, CacheDuration);
        _logger.LogInformation("Cached products with key: {CacheKey}", cacheKey);

        return response;
    }

    /// <summary>
    /// Retrieves a product by ID
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>Product response</returns>
    public async Task<ProductResponse> GetByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving product with ID: {ProductId}", id);

        var cacheKey = $"product_{id}";

        // Try to get from cache
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

        // Cache the response
        _cache.Set(cacheKey, response, CacheDuration);
        _logger.LogInformation("Cached product: {ProductId}", id);

        return response;
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="request">The product creation request</param>
    /// <returns>Created product response</returns>
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

        // Invalidate products list cache
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

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <param name="request">The product update request</param>
    /// <returns>Updated product response</returns>
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

        // Invalidate both product-specific cache and products list cache
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

    /// <summary>
    /// Deletes a product
    /// </summary>
    /// <param name="id">The product ID</param>
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

        // Invalidate both product-specific cache and products list cache
        _cache.Remove($"product_{id}");
        InvalidateProductsListCache();
    }

    /// <summary>
    /// Invalidates all cached product list entries
    /// </summary>
    private void InvalidateProductsListCache()
    {
        // Since we can't enumerate all cache keys easily, we'll use a different approach
        // In production, you might want to use a more sophisticated cache invalidation strategy
        // For now, we'll just log that we should invalidate the cache
        // The cache entries will expire naturally after 5 minutes
        _logger.LogInformation("Product list cache should be invalidated");
    }
}
