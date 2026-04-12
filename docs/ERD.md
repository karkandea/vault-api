# Entity Relationship Diagram

## Users table
| Field | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-generated |
| Username | string | Required, 3-50 chars, unique |
| Email | string | Required, max 150 chars, unique |
| PasswordHash | string | Required, bcrypt hash |
| CreatedAt | datetime | Auto-set UTC |

## Products table
| Field | Type | Constraints |
|---|---|---|
| Id | int | PK, auto-generated |
| Name | string | Required, max 200 chars |
| Description | string | Optional, max 500 chars |
| Price | decimal(18,2) | Required, 100000-10000000 |
| CreatedAt | datetime | Auto-set UTC |
| UpdatedAt | datetime | Nullable, auto-set on update |
| ImageUrl | string | Optional, max 2000 chars |

No foreign key relationships — Users and Products are independent entities.
Auth is handled via JWT, not DB-level foreign keys.
