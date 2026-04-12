# Architecture

## Layer diagram
HTTP Request
↓
Controllers (AuthController, ProductsController)
↓
Services (AuthService, ProductService, ImageService)
↓
Repositories (UserRepository, ProductRepository)
↓
AppDbContext (EF Core 10) → PostgreSQL (Supabase)
ImageService → Supabase Storage (product images)

## Design decisions
- Repository Pattern: data access fully abstracted from business logic
- Service Layer: all business logic lives here, controllers are thin
- DTOs: request/response objects separate from database entities
- Global Exception Middleware: consistent error responses, no stack traces exposed
- In-Memory Cache: GET endpoints cached, version-based invalidation on writes
- JWT Auth: stateless, 24-hour expiry, validated on every protected request
