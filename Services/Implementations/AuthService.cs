using Microsoft.Extensions.Configuration;
using Vault.Helpers;
using Vault.Models.DTOs.Auth;
using Vault.Models.Entities;
using Vault.Repositories.Interfaces;
using Vault.Services.Interfaces;

namespace Vault.Services.Implementations;

/// <summary>
/// Service implementation for authentication operations
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, JwtHelper jwtHelper, ILogger<AuthService> logger, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _jwtHelper = jwtHelper;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Attempting to register user with email: {Email}", request.Email);

        var (emailExists, usernameExists) = await _userRepository.ExistsAsync(request.Email, request.Username);

        if (emailExists)
        {
            _logger.LogWarning("Registration failed: Email already registered - {Email}", request.Email);
            throw new InvalidOperationException("Email already registered");
        }

        if (usernameExists)
        {
            _logger.LogWarning("Registration failed: Username already taken - {Username}", request.Username);
            throw new InvalidOperationException("Username already taken");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash
        };

        var createdUser = await _userRepository.CreateAsync(user);

        _logger.LogInformation("User registered successfully: {UserId}", createdUser.Id);

        var token = _jwtHelper.GenerateToken(createdUser);
        var expiryHours = double.Parse(_configuration["Jwt:ExpiryHours"] ?? "24");

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            UserId = createdUser.Id,
            Username = createdUser.Username,
            Email = createdUser.Email
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Attempting to login user with email: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email);

        // Same error message for both non-existent user and wrong password prevents
        // attackers from enumerating valid email addresses.
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid credentials for email {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        var token = _jwtHelper.GenerateToken(user);
        var expiryHours = double.Parse(_configuration["Jwt:ExpiryHours"] ?? "24");

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email
        };
    }
}
