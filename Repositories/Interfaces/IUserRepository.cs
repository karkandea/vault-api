using Vault.Models.Entities;

namespace Vault.Repositories.Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> CreateAsync(User user);
    Task<(bool emailExists, bool usernameExists)> ExistsAsync(string email, string username);
}
