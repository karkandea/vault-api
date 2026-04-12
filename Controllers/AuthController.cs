using Microsoft.AspNetCore.Mvc;
using Vault.Models.DTOs.Auth;
using Vault.Services.Interfaces;

namespace Vault.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("Registration request received for email: {Email}", request.Email);

        var result = await _authService.RegisterAsync(request);

        var response = new
        {
            success = true,
            message = "User registered successfully",
            data = result
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login request received for email: {Email}", request.Email);

        var result = await _authService.LoginAsync(request);

        var response = new
        {
            success = true,
            message = "Login successful",
            data = result
        };

        return Ok(response);
    }
}
