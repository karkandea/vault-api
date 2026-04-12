using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Vault.Helpers;
using Vault.Models.DTOs.Auth;
using Vault.Models.Entities;
using Vault.Repositories.Interfaces;
using Vault.Services.Implementations;

namespace Vault.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task Register_NewUser_ReturnsToken()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repo => repo.ExistsAsync("new@example.com", "newuser"))
            .ReturnsAsync((false, false));
        repository
            .Setup(repo => repo.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
            {
                user.Id = 1;
                return user;
            });

        var service = CreateService(repository.Object);
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "Password123"
        };

        // Act
        var response = await service.RegisterAsync(request);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("new@example.com", response.Email);
        Assert.Equal("newuser", response.Username);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsException()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repo => repo.ExistsAsync("existing@example.com", "newuser"))
            .ReturnsAsync((true, false));

        var service = CreateService(repository.Object);
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "Password123"
        };

        // Act
        var action = () => service.RegisterAsync(request);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(action);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repo => repo.GetByEmailAsync("user@example.com"))
            .ReturnsAsync(new User
            {
                Id = 2,
                Username = "user",
                Email = "user@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            });

        var service = CreateService(repository.Object);
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "Password123"
        };

        // Act
        var response = await service.LoginAsync(request);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("user@example.com", response.Email);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsException()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(repo => repo.GetByEmailAsync("user@example.com"))
            .ReturnsAsync(new User
            {
                Id = 2,
                Username = "user",
                Email = "user@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
            });

        var service = CreateService(repository.Object);
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword123"
        };

        // Act
        var action = () => service.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
    }

    private static AuthService CreateService(IUserRepository repository)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-jwt-secret-key-minimum-32-chars",
                ["Jwt:Issuer"] = "vault-tests",
                ["Jwt:Audience"] = "vault-tests-client",
                ["Jwt:ExpiryHours"] = "24"
            })
            .Build();

        var jwtHelper = new JwtHelper(configuration);
        var logger = new Mock<ILogger<AuthService>>();

        return new AuthService(repository, jwtHelper, logger.Object, configuration);
    }
}
