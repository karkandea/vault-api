namespace Vault.Services.Interfaces;

/// <summary>
/// Service interface for image upload operations
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Uploads a product image to Supabase Storage
    /// </summary>
    /// <param name="productId">The ID of the product (used in filename)</param>
    /// <param name="file">The image file to upload</param>
    /// <returns>The public URL of the uploaded image</returns>
    Task<string> UploadProductImageAsync(int productId, IFormFile file);

    /// <summary>
    /// Deletes a product image from Supabase Storage
    /// </summary>
    /// <param name="imageUrl">The public URL of the image to delete</param>
    Task DeleteProductImageAsync(string imageUrl);
}
