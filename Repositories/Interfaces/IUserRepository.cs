using Vault.Models.Entities;

namespace Vault.Repositories.Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by email address
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <returns>The user if found, otherwise null</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user by username
    /// </summary>
    /// <param name="username">The username</param>
    /// <returns>The user if found, otherwise null</returns>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// Creates a new user in the database
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <returns>The created user with generated ID</returns>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Checks if a user exists with the given email or username
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <param name="username">The username to check</param>
    /// <returns>A tuple indicating if the email and/or username already exist</returns>
    Task<(bool emailExists, bool usernameExists)> ExistsAsync(string email, string username);
}
