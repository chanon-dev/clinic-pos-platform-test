# Clinic POS Platform — v1

Multi-Tenant, Multi-Branch B2B Clinic Point-of-Service Platform.

## Tech Stack

| Layer      | Technology                |
| ---------- | ------------------------- |
| Backend    | .NET 10 / C# (Clean Architecture) |
| Frontend   | Next.js 15 / TypeScript / Tailwind CSS |
| Database   | PostgreSQL 16             |
| Auth       | JWT Bearer Token          |

## Quick Start

### Prerequisites
- Docker & Docker Compose

### Run (one command)

```bash
docker compose up --build
```

This will start:
- **Frontend:** http://localhost:3000
- **API:** http://localhost:5000
- **PostgreSQL:** localhost:5432

The database is automatically migrated and seeded on startup.

### Demo Accounts

| Username  | Password  | Role   | Branches               |
| --------- | --------- | ------ | ---------------------- |
| admin01   | P@ssw0rd  | Admin  | สาขาสยาม, สาขาทองหล่อ  |
| user01    | P@ssw0rd  | User   | สาขาสยาม               |
| viewer01  | P@ssw0rd  | Viewer | สาขาสยาม               |

## Architecture

### Backend — Clean Architecture

```
src/backend/
├── ClinicPOS.API/              # Controllers, Middleware, Program.cs
├── ClinicPOS.Application/      # Use Cases (Commands/Queries/Handlers)
├── ClinicPOS.Domain/           # Entities, Enums
├── ClinicPOS.Infrastructure/   # EF Core, Repositories, JWT, Seeder
└── ClinicPOS.UnitTests/        # xUnit tests
```

### Frontend — Feature-Based

```
src/frontend/src/
├── app/                  # Next.js App Router pages
├── features/             # Feature modules
└── shared/               # Shared components, hooks, API client
```

## API Endpoints (Phase 1)

| Method | Endpoint                   | Access            |
| ------ | -------------------------- | ----------------- |
| POST   | /api/auth/login            | Public            |
| POST   | /api/users                 | Admin             |
| PUT    | /api/users/{id}/role       | Admin             |
| PUT    | /api/users/{id}/branches   | Admin             |
| POST   | /api/patients              | Admin, User       |
| GET    | /api/patients              | All authenticated |
| GET    | /api/branches              | All authenticated |

## Key Design Decisions

### Patient ↔ Branch Relationship
Used `PrimaryBranchId` (nullable FK) instead of a separate mapping table:
- **MVP Simplicity** — Only need to track registration branch
- **Query Performance** — No extra JOIN needed
- **Extensible** — Can add `patient_visits` table later without schema change

### Tenant Isolation
- **Global Query Filter** (EF Core) — All queries auto-filtered by TenantId
- **TenantId from JWT** — Never passed in request body
- **UNIQUE(tenant_id, phone_number)** — Phone uniqueness is per-tenant, not global

### Authorization
- **RBAC via attribute** — `[RequirePermission(Permission.CreatePatient)]`
- **Admin** → Full access
- **User** → Create/View patients and appointments
- **Viewer** → Read-only access

## Running Tests

```bash
cd src/backend
dotnet test
```

## Development (without Docker)

### Backend
```bash
cd src/backend
dotnet run --project ClinicPOS.API
```

### Frontend
```bash
cd src/frontend
npm install
npm run dev
```
