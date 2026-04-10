using Microsoft.EntityFrameworkCore;
using Vault.Data;
using Vault.Models.Entities;
using Vault.Repositories.Interfaces;

namespace Vault.Repositories.Implementations;

/// <summary>
/// Repository implementation for Product entity operations
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the ProductRepository class
    /// </summary>
    /// <param name="context">The application database context</param>
    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all products from the database
    /// </summary>
    /// <returns>A collection of all products</returns>
    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products.ToListAsync();
    }

    /// <summary>
    /// Retrieves a product by its unique identifier
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>The product if found, otherwise null</returns>
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products.FindAsync(id);
    }

    /// <summary>
    /// Creates a new product in the database
    /// </summary>
    /// <param name="product">The product to create</param>
    /// <returns>The created product with generated ID</returns>
    public async Task<Product> CreateAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    /// <summary>
    /// Updates an existing product in the database
    /// </summary>
    /// <param name="id">The ID of the product to update</param>
    /// <param name="product">The updated product data</param>
    /// <returns>The updated product if found, otherwise null</returns>
    public async Task<Product?> UpdateAsync(int id, Product product)
    {
        var existingProduct = await _context.Products.FindAsync(id);

        if (existingProduct == null)
        {
            return null;
        }

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingProduct;
    }

    /// <summary>
    /// Deletes a product from the database
    /// </summary>
    /// <param name="id">The ID of the product to delete</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return false;
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

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
    public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        string? name,
        decimal? minPrice,
        decimal? maxPrice,
        int page,
        int pageSize,
        string? sortBy,
        string? sortOrder)
    {
        var query = _context.Products.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(p => p.Name.ToLower().Contains(name.ToLower()));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        if (sortBy?.ToLower() == "price")
        {
            query = sortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(p => p.Price)
                : query.OrderBy(p => p.Price);
        }
        else
        {
            query = sortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name);
        }

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
