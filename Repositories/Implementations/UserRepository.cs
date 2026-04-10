using Microsoft.EntityFrameworkCore;
using Vault.Data;
using Vault.Models.Entities;
using Vault.Repositories.Interfaces;

namespace Vault.Repositories.Implementations;

/// <summary>
/// Repository implementation for User entity operations
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the UserRepository class
    /// </summary>
    /// <param name="context">The application database context</param>
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a user by email address
    /// </summary>
    /// <param name="email">The user's email address</param>
    /// <returns>The user if found, otherwise null</returns>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Retrieves a user by username
    /// </summary>
    /// <param name="username">The username</param>
    /// <returns>The user if found, otherwise null</returns>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    /// <summary>
    /// Creates a new user in the database
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <returns>The created user with generated ID</returns>
    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Checks if a user exists with the given email or username
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <param name="username">The username to check</param>
    /// <returns>A tuple indicating if the email and/or username already exist</returns>
    public async Task<(bool emailExists, bool usernameExists)> ExistsAsync(string email, string username)
    {
        var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
        var usernameExists = await _context.Users.AnyAsync(u => u.Username == username);

        return (emailExists, usernameExists);
    }
}
