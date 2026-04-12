using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vault.Data;
using Vault.Models.DTOs.Auth;
using Vault.Models.Entities;
using Vault.Tests.Infrastructure;

namespace Vault.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ValidData_Returns201()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(
            document.RootElement.GetProperty("data").GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db =>
        {
            db.Users.Add(new User
            {
                Username = "existing",
                Email = "existing@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            });
        });
        using var client = factory.CreateClient();
        var request = new RegisterRequest
        {
            Username = "someoneelse",
            Email = "existing@example.com",
            Password = "Password123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingFields_Returns400()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var payload = new
        {
            username = "",
            email = "",
            password = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        // Arrange
        const string password = "Password123";
        using var factory = new TestWebApplicationFactory(db =>
        {
            db.Users.Add(new User
            {
                Username = "validuser",
                Email = "valid@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            });
        });
        using var client = factory.CreateClient();
        var request = new LoginRequest
        {
            Email = "valid@example.com",
            Password = password
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(
            document.RootElement.GetProperty("data").GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        using var factory = new TestWebApplicationFactory(db =>
        {
            db.Users.Add(new User
            {
                Username = "validuser",
                Email = "valid@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            });
        });
        using var client = factory.CreateClient();
        var request = new LoginRequest
        {
            Email = "valid@example.com",
            Password = "WrongPassword123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
