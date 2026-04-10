using Microsoft.EntityFrameworkCore;
using Vault.Models.Entities;

namespace Vault.Data;

/// <summary>
/// Database context for the Vault application
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the AppDbContext class
    /// </summary>
    /// <param name="options">The options to be used by the DbContext</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Gets or sets the Products DbSet
    /// </summary>
    public DbSet<Product> Products { get; set; }

    /// <summary>
    /// Configures the entity models and relationships
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            // Email must be unique
            entity.HasIndex(e => e.Email)
                .IsUnique();

            // Configure PasswordHash column type as text
            entity.Property(e => e.PasswordHash)
                .HasColumnType("text");

            // CreatedAt has default value of now()
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()");
        });

        // Product entity configuration
        modelBuilder.Entity<Product>(entity =>
        {
            // Configure Price column type as decimal(18,2)
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)");

            // CreatedAt has default value of now()
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()");
        });
    }
}
