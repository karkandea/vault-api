using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Vault.Models.DTOs.Products;
using Vault.Models.Entities;
using Vault.Repositories.Interfaces;
using Vault.Services.Implementations;

namespace Vault.Tests.Services;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAll_ReturnsPagedResponse()
    {
        // Arrange
        var repository = new Mock<IProductRepository>();
        repository
            .Setup(repo => repo.SearchAsync(null, null, null, 1, 10, null, null))
            .ReturnsAsync((
                new List<Product>
                {
                    new() { Id = 1, Name = "Laptop", Price = 1500000, CreatedAt = DateTime.UtcNow }
                }.AsEnumerable(),
                1));

        var service = CreateService(repository.Object);

        // Act
        var response = await service.GetAllAsync(1, 10, null, null, null, null, null);

        // Assert
        Assert.Single(response.Data);
        Assert.Equal(1, response.Total);
        Assert.Equal("Laptop", response.Data[0].Name);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsProduct()
    {
        // Arrange
        var repository = new Mock<IProductRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(5))
            .ReturnsAsync(new Product
            {
                Id = 5,
                Name = "Keyboard",
                Description = "Mechanical",
                Price = 300000,
                CreatedAt = DateTime.UtcNow
            });

        var service = CreateService(repository.Object);

        // Act
        var response = await service.GetByIdAsync(5);

        // Assert
        Assert.Equal(5, response.Id);
        Assert.Equal("Keyboard", response.Name);
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = new Mock<IProductRepository>();
        repository.Setup(repo => repo.GetByIdAsync(99)).ReturnsAsync((Product?)null);

        var service = CreateService(repository.Object);

        // Act
        var action = () => service.GetByIdAsync(99);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(action);
    }

    [Fact]
    public async Task Create_ValidProduct_ReturnsProductResponse()
    {
        // Arrange
        var repository = new Mock<IProductRepository>();
        repository
            .Setup(repo => repo.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product product) =>
            {
                product.Id = 7;
                product.CreatedAt = DateTime.UtcNow;
                return product;
            });

        var service = CreateService(repository.Object);
        var request = new CreateProductRequest
        {
            Name = "Mouse",
            Description = "Wireless",
            Price = 200000
        };

        // Act
        var response = await service.CreateAsync(request);

        // Assert
        Assert.Equal(7, response.Id);
        Assert.Equal("Mouse", response.Name);
    }

    [Fact]
    public async Task Update_ExistingProduct_ReturnsUpdated()
    {
        // Arrange
        var repository = new Mock<IProductRepository>();
        repository
            .Setup(repo => repo.UpdateAsync(7, It.IsAny<Product>()))
            .ReturnsAsync((int _, Product product) => new Product
            {
                Id = 7,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            });

        var service = CreateService(repository.Object);
        var request = new UpdateProductRequest
        {
            Name = "Mouse Pro",
            Description = "Updated",
            Price = 250000
        };

        // Act
        var response = await service.UpdateAsync(7, request);

        // Assert
        Assert.Equal("Mouse Pro", response.Name);
        Assert.Equal(250000, response.Price);
    }

    [Fact]
    public async Task Delete_ExistingProduct_Completes()
    {
        // Arrange
        var repository = new Mock<IProductRepository>();
        repository.Setup(repo => repo.DeleteAsync(7)).ReturnsAsync(true);

        var service = CreateService(repository.Object);

        // Act
        var action = () => service.DeleteAsync(7);

        // Assert
        await action();
    }

    [Fact]
    public async Task Delete_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repository = new Mock<IProductRepository>();
        repository.Setup(repo => repo.DeleteAsync(99)).ReturnsAsync(false);

        var service = CreateService(repository.Object);

        // Act
        var action = () => service.DeleteAsync(99);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(action);
    }

    private static ProductService CreateService(IProductRepository repository)
    {
        var logger = new Mock<ILogger<ProductService>>();
        var cache = new MemoryCache(new MemoryCacheOptions());

        return new ProductService(repository, logger.Object, cache);
    }
}
