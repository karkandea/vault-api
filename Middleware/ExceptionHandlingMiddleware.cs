namespace Vault.Middleware;

/// <summary>
/// Middleware for handling unhandled exceptions globally
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        // KeyNotFoundException -> 404: Services throw this when product/user doesn't exist
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            await HandleExceptionAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        // ArgumentException -> 400: Services throw this for invalid parameters (bad page, price range, etc.)
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request: {Message}", ex.Message);
            await HandleExceptionAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        // UnauthorizedAccessException -> 401: AuthService throws this for invalid credentials
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access: {Message}", ex.Message);
            await HandleExceptionAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        // InvalidOperationException -> 409: AuthService throws this for duplicate email/username
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conflict: {Message}", ex.Message);
            await HandleExceptionAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            success = false,
            message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
