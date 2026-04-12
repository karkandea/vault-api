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

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<(bool emailExists, bool usernameExists)> ExistsAsync(string email, string username)
    {
        var emailExists = await _context.Users.AnyAsync(u => u.Email == email);
        var usernameExists = await _context.Users.AnyAsync(u => u.Username == username);

        return (emailExists, usernameExists);
    }
}
