using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Vault.Data;
using Vault.Helpers;
using Vault.Middleware;
using Vault.Repositories.Implementations;
using Vault.Repositories.Interfaces;
using Vault.Services.Implementations;
using Vault.Services.Interfaces;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/vault-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        rollOnFileSizeLimit: true)
    .CreateLogger();

try
{
    Log.Information("Starting Vault API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();

    // Configure DbContext with PostgreSQL
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add Memory Cache
    builder.Services.AddMemoryCache();

    // Register Repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();

    // Register Services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IProductService, ProductService>();

    // Register Helpers
    builder.Services.AddScoped<JwtHelper>();

    // Configure JWT Authentication
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

    builder.Services.AddAuthorization();

    // Configure Swagger with JWT support
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "Vault API",
            Version = "v1",
            Description = "Product Management API with JWT Authentication"
        });

        // Add JWT Authentication to Swagger
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Description = "Enter your JWT token in the format: Bearer {your token}"
        });

        options.AddSecurityRequirement(document =>
        {
            var securityScheme = new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document);
            return new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                [securityScheme] = new List<string>()
            };
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vault API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.UseHttpsRedirection();

    // Exception handling middleware (must be early in pipeline)
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("Vault API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
