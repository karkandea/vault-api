using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Vault.Data;
using Vault.Models.DTOs.Products;
using Vault.Models.Entities;
using Vault.Tests.Infrastructure;

namespace Vault.Tests.Controllers;

public class ProductsControllerTests
{
    [Fact]
    public async Task GetAll_WithToken_Returns200()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db =>
        {
            SeedUser(db);
            db.Products.Add(new Product { Id = 1, Name = "Laptop", Description = "Office", Price = 1500000 });
        }, useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_NoToken_Returns401()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db =>
        {
            SeedUser(db);
            db.Products.Add(new Product { Id = 10, Name = "Laptop", Description = "Office", Price = 1500000 });
        }, useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();

        // Act
        var response = await client.GetAsync("/api/products/10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db => SeedUser(db), useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();

        // Act
        var response = await client.GetAsync("/api/products/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns400()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db => SeedUser(db), useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();

        // Act
        var response = await client.GetAsync("/api/products/0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidData_Returns201()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db => SeedUser(db), useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();
        var request = new CreateProductRequest
        {
            Name = "Keyboard",
            Description = "Mechanical keyboard",
            Price = 250000
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_PriceTooLow_Returns400()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db => SeedUser(db), useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();
        var request = new CreateProductRequest
        {
            Name = "Cheap Cable",
            Description = "Too cheap",
            Price = 50000
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ValidData_Returns200()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db =>
        {
            SeedUser(db);
            db.Products.Add(new Product { Id = 21, Name = "Mouse", Description = "Old", Price = 150000 });
        }, useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();
        var request = new UpdateProductRequest
        {
            Name = "Mouse Pro",
            Description = "Updated",
            Price = 200000
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/products/21", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_NotFound_Returns404()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db => SeedUser(db), useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();
        var request = new UpdateProductRequest
        {
            Name = "Mouse Pro",
            Description = "Updated",
            Price = 200000
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/products/999", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_InvalidId_Returns400()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db => SeedUser(db), useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();
        var request = new UpdateProductRequest
        {
            Name = "Mouse Pro",
            Description = "Updated",
            Price = 200000
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/products/0", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db =>
        {
            SeedUser(db);
            db.Products.Add(new Product { Id = 31, Name = "Monitor", Description = "4K", Price = 2500000 });
        }, useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();

        // Act
        var response = await client.DeleteAsync("/api/products/31");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NotFound_Returns404()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db => SeedUser(db), useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();

        // Act
        var response = await client.DeleteAsync("/api/products/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_InvalidId_Returns400()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db => SeedUser(db), useTestAuthentication: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = CreateAuthHeader();

        // Act
        var response = await client.DeleteAsync("/api/products/0");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static void SeedUser(AppDbContext db)
    {
        db.Users.Add(new User
        {
            Id = 100,
            Username = "producttester",
            Email = "producttester@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
        });
    }

    private static AuthenticationHeaderValue CreateAuthHeader()
    {
        return new AuthenticationHeaderValue(TestAuthHandler.SchemeName, TestAuthHandler.ValidToken);
    }
}
