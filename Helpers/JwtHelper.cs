using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Vault.Models.Entities;

namespace Vault.Helpers;

/// <summary>
/// Helper class for JWT token generation
/// </summary>
public class JwtHelper
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the JwtHelper class
    /// </summary>
    /// <param name="configuration">The application configuration</param>
    public JwtHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generates a JWT token for the specified user
    /// </summary>
    /// <param name="user">The user to generate a token for</param>
    /// <returns>A JWT token string</returns>
    public string GenerateToken(User user)
    {
        var secret = _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured");
        var expiryHours = double.Parse(_configuration["Jwt:ExpiryHours"] ?? throw new InvalidOperationException("Jwt:ExpiryHours not configured"));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiryHours),
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}
