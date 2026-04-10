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

    /// <summary>
    /// Initializes a new instance of the AuthService class
    /// </summary>
    /// <param name="userRepository">The user repository</param>
    /// <param name="jwtHelper">The JWT helper</param>
    /// <param name="logger">The logger</param>
    /// <param name="configuration">The configuration</param>
    public AuthService(IUserRepository userRepository, JwtHelper jwtHelper, ILogger<AuthService> logger, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _jwtHelper = jwtHelper;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="request">The registration request</param>
    /// <returns>Authentication response with JWT token</returns>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        _logger.LogInformation("Attempting to register user with email: {Email}", request.Email);

        // Check for duplicate email or username
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

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Create user entity
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash
        };

        // Save user to database
        var createdUser = await _userRepository.CreateAsync(user);

        _logger.LogInformation("User registered successfully: {UserId}", createdUser.Id);

        // Generate JWT token
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

    /// <summary>
    /// Authenticates a user
    /// </summary>
    /// <param name="request">The login request</param>
    /// <returns>Authentication response with JWT token</returns>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        _logger.LogInformation("Attempting to login user with email: {Email}", request.Email);

        // Get user by email
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // Verify user exists and password is correct
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid credentials for email {Email}", request.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        // Generate JWT token
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
