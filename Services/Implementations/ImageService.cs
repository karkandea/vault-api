using System.Net.Http.Headers;
using Vault.Services.Interfaces;

namespace Vault.Services.Implementations;

/// <summary>
/// Service implementation for image upload operations
/// </summary>
public class ImageService : IImageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImageService> _logger;
    private readonly HttpClient _httpClient;

    private readonly string[] _allowedExtensions;
    private readonly string[] _allowedMimeTypes;
    private readonly long _maxFileSizeBytes;

    public ImageService(
        IConfiguration configuration,
        ILogger<ImageService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;

        _allowedExtensions = _configuration.GetSection("ImageUpload:AllowedExtensions").Get<string[]>()
            ?? new[] { ".jpg", ".jpeg", ".png", ".webp" };
        _allowedMimeTypes = _configuration.GetSection("ImageUpload:AllowedMimeTypes").Get<string[]>()
            ?? new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        _maxFileSizeBytes = _configuration.GetValue<long>("ImageUpload:MaxFileSizeBytes", 2097152);
    }

    public async Task<string> UploadProductImageAsync(int productId, IFormFile file)
    {
        _logger.LogInformation("Uploading image for product: {ProductId}", productId);

        // Validate file
        ValidateFile(file);

        // Read Supabase configuration
        var supabaseUrl = _configuration["Supabase:Url"];
        var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"];
        var bucketName = _configuration["Supabase:BucketName"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(serviceRoleKey) || string.IsNullOrEmpty(bucketName))
        {
            _logger.LogError("Supabase configuration is missing");
            throw new InvalidOperationException("Supabase configuration is not properly set");
        }

        // Generate filename with milliseconds timestamp and random suffix to prevent collisions
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var randomSuffix = Guid.NewGuid().ToString("N")[..8];
        var filename = $"{productId}_{timestamp}_{randomSuffix}{extension}";

        // Upload to Supabase Storage
        var uploadUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{filename}";

        using var content = new MultipartFormDataContent();
        using var fileStream = file.OpenReadStream();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", filename);

        // Create request with headers set per-request instead of mutating shared DefaultRequestHeaders
        using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);
        request.Content = content;

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Transport error uploading to Supabase for product {ProductId}", productId);
            throw new InvalidOperationException("Failed to connect to storage service", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to upload image to Supabase: {StatusCode} - {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"Failed to upload image: {response.StatusCode}. Details: {error}");
        }

        // Generate public URL
        var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{bucketName}/{filename}";

        _logger.LogInformation("Image uploaded successfully for product {ProductId}: {ImageUrl}", productId, publicUrl);

        return publicUrl;
    }

    public async Task DeleteProductImageAsync(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            return;
        }

        _logger.LogInformation("Deleting image: {ImageUrl}", imageUrl);

        // Read Supabase configuration
        var supabaseUrl = _configuration["Supabase:Url"];
        var serviceRoleKey = _configuration["Supabase:ServiceRoleKey"];
        var bucketName = _configuration["Supabase:BucketName"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(serviceRoleKey) || string.IsNullOrEmpty(bucketName))
        {
            _logger.LogError("Supabase configuration is missing");
            return;
        }

        // Extract filename from URL
        var publicPrefix = $"{supabaseUrl}/storage/v1/object/public/{bucketName}/";
        if (!imageUrl.StartsWith(publicPrefix))
        {
            _logger.LogWarning("Invalid image URL format: {ImageUrl}", imageUrl);
            return;
        }

        var filename = imageUrl.Substring(publicPrefix.Length);
        var deleteUrl = $"{supabaseUrl}/storage/v1/object/{bucketName}/{filename}";

        // Create request with headers set per-request instead of mutating shared DefaultRequestHeaders
        using var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceRoleKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Transport error deleting image from Supabase: {ImageUrl}", imageUrl);
            // Don't throw - this is a cleanup operation
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to delete image from Supabase: {StatusCode} - {Error}", response.StatusCode, error);
        }
        else
        {
            _logger.LogInformation("Image deleted successfully: {ImageUrl}", imageUrl);
        }
    }

    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is required");
        }

        if (file.Length > _maxFileSizeBytes)
        {
            throw new ArgumentException($"File size cannot exceed {_maxFileSizeBytes / (1024 * 1024)}MB");
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"Only {string.Join(", ", _allowedExtensions)} files are allowed");
        }

        // Validate MIME type
        var mimeType = file.ContentType.ToLowerInvariant();
        if (!_allowedMimeTypes.Contains(mimeType))
        {
            throw new ArgumentException($"Invalid file type. Only {string.Join(", ", _allowedMimeTypes)} are allowed");
        }
    }
}
