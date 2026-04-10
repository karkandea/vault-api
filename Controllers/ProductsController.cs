using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vault.Models.DTOs.Products;
using Vault.Services.Interfaces;

namespace Vault.Controllers;

/// <summary>
/// Controller for product operations
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    /// <summary>
    /// Initializes a new instance of the ProductsController class
    /// </summary>
    /// <param name="productService">The product service</param>
    /// <param name="logger">The logger instance</param>
    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of products
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="name">Optional product name filter</param>
    /// <param name="minPrice">Optional minimum price filter</param>
    /// <param name="maxPrice">Optional maximum price filter</param>
    /// <param name="sortBy">Sort field (default: name)</param>
    /// <param name="sortOrder">Sort order (default: asc)</param>
    /// <returns>Paginated product list</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        _logger.LogInformation("Retrieving products - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var result = await _productService.GetAllAsync(page, pageSize, name, minPrice, maxPrice, sortBy, sortOrder);

        var response = new
        {
            success = true,
            message = "Products retrieved successfully",
            data = result
        };

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a product by ID
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid product ID"
            });
        }

        _logger.LogInformation("Retrieving product with ID: {ProductId}", id);

        var result = await _productService.GetByIdAsync(id);

        var response = new
        {
            success = true,
            message = "Product retrieved successfully",
            data = result
        };

        return Ok(response);
    }

    /// <summary>
    /// Creates a new product
    /// </summary>
    /// <param name="request">The product creation request</param>
    /// <returns>Created product</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("Creating new product: {ProductName}", request.Name);

        var result = await _productService.CreateAsync(request);

        var response = new
        {
            success = true,
            message = "Product created successfully",
            data = result
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <param name="request">The product update request</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid product ID"
            });
        }

        _logger.LogInformation("Updating product with ID: {ProductId}", id);

        var result = await _productService.UpdateAsync(id, request);

        var response = new
        {
            success = true,
            message = "Product updated successfully",
            data = result
        };

        return Ok(response);
    }

    /// <summary>
    /// Deletes a product
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>No content</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid product ID"
            });
        }

        _logger.LogInformation("Deleting product with ID: {ProductId}", id);

        await _productService.DeleteAsync(id);

        return NoContent();
    }
}
