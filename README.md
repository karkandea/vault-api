# Vault — Product Management API

A secure, production-ready Product Management platform built with ASP.NET Core 10. Vault provides JWT-authenticated REST API with full product CRUD, image upload, search/filtering, and an integrated React frontend.

**Live Demo:** http://47.129.65.144  
**API Docs (Swagger):** http://47.129.65.144/swagger/index.html

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 Web API |
| Language | C# 13 |
| Database | PostgreSQL (Supabase) |
| ORM | Entity Framework Core 10 |
| Auth | JWT Bearer Token |
| Password | BCrypt |
| Logging | Serilog |
| Caching | In-Memory Cache |
| Storage | Supabase Storage (product images) |
| Frontend | React 18 + Vite + Bootstrap 5 |
| Tests | xUnit + Moq (29 tests, 100% controller coverage) |
| Container | Docker (multi-stage build) |
| Deployment | AWS EC2 + nginx |

---

## Local Setup (5 steps)

### Prerequisites
- .NET 10 SDK
- Node.js 18+
- PostgreSQL or Supabase account

### Steps

**1. Clone the repository**
```bash
git clone https://github.com/karkandea/vault-api.git
cd vault-api
```

**2. Configure environment**

Create `appsettings.Development.json` in the root folder:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SslMode=Require;TrustServerCertificate=true"
  },
  "Jwt": {
    "Secret": "your-jwt-secret-key-minimum-32-characters-long",
    "Issuer": "vault-api",
    "Audience": "vault-client",
    "ExpiryHours": 24
  },
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "ServiceRoleKey": "your-service-role-key",
    "BucketName": "product-images"
  },
  "AllowedOrigins": ["http://localhost:5173"]
}
```

**3. Run database migrations**
```bash
dotnet ef database update
```

**4. Start the backend**
```bash
dotnet run
```
API runs at `http://localhost:5278` — Swagger available at `http://localhost:5278/swagger`

**5. Start the frontend**
```bash
cd frontend
npm install
npm run dev
```
Frontend runs at `http://localhost:5173`

---

## API Endpoints

### Authentication (Public)
| Method | Endpoint | Description |
|---|---|---|
| POST | /api/auth/register | Register new user |
| POST | /api/auth/login | Login and receive JWT token |

### Products (Requires Bearer Token)
| Method | Endpoint | Description |
|---|---|---|
| GET | /api/products | List products with pagination, search, filter, sort |
| GET | /api/products/{id} | Get product by ID |
| POST | /api/products | Create product |
| PUT | /api/products/{id} | Update product |
| DELETE | /api/products/{id} | Delete product |
| POST | /api/products/{id}/image | Upload product image |

### Query Parameters (GET /api/products)
| Parameter | Type | Default | Description |
|---|---|---|---|
| page | int | 1 | Page number |
| pageSize | int | 10 | Items per page (max 100) |
| name | string | - | Search by name (partial match) |
| minPrice | decimal | - | Minimum price filter |
| maxPrice | decimal | - | Maximum price filter |
| sortBy | string | name | Sort field: name or price |
| sortOrder | string | asc | Sort direction: asc or desc |

---

## Architecture

```
Controllers → Services → Repositories → DbContext → PostgreSQL
```

- **Repository Pattern** — data access fully abstracted from business logic
- **Service Layer** — all business logic lives here, controllers are thin
- **DTOs** — request/response objects separate from database entities
- **Global Exception Middleware** — consistent error responses, no stack traces exposed
- **In-Memory Cache** — GET endpoints cached with version-based invalidation

---

## Running Tests

```bash
dotnet test
```

29 tests, 100% coverage on controller layer.

---

## Docker

```bash
# Build
docker build -t vault-api .

# Run (create .env file first from .env.example)
docker-compose up -d
```

---

## Assumptions

- All product endpoints require authentication — there is no public product browsing
- Price range: minimum 100,000, maximum 10,000,000 (Indonesian Rupiah context)
- Image upload supports JPG, JPEG, PNG, WEBP — max 2MB per file
- JWT token expires after 24 hours
- Product images stored in Supabase Storage (public bucket for read, authenticated for write)
- Concurrent delete scenario handled — returns 404 with clear message if resource deleted mid-request
- In-Memory Cache used for performance; cache invalidated on all write operations
- Swagger enabled in all environments for ease of reviewer testing