using Vault.Models.DTOs.Auth;

namespace Vault.Services.Interfaces;

/// <summary>
/// Service interface for authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="request">The registration request</param>
    /// <returns>Authentication response with JWT token</returns>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticates a user
    /// </summary>
    /// <param name="request">The login request</param>
    /// <returns>Authentication response with JWT token</returns>
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
