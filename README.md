# Clinic POS Platform — v2

Multi-Tenant, Multi-Branch B2B Clinic Point-of-Service Platform.

## Tech Stack

| Layer      | Technology                |
| ---------- | ------------------------- |
| Backend    | .NET 10 / C# (Clean Architecture) |
| Frontend   | Next.js 15 / TypeScript / Tailwind CSS |
| Database   | PostgreSQL 16             |
| Cache      | Redis 7                   |
| Messaging  | RabbitMQ 3 (AMQP)        |
| Auth       | JWT Bearer Token          |

## Quick Start

### Prerequisites

- Docker & Docker Compose

### Run (one command)

```bash
docker compose up --build
```

This will start:

- **Frontend:** <http://localhost:3000>
- **API:** <http://localhost:5001>
- **API Docs (Scalar):** <http://localhost:5001/scalar/v1>
- **PostgreSQL:** localhost:5432
- **Redis:** localhost:6379
- **RabbitMQ:** localhost:5672 (Management UI: <http://localhost:15672> — guest/guest)

The database is automatically migrated and seeded on startup.

### Environment Variables

Copy `.env.example` to `.env` and adjust as needed:

```bash
cp .env.example .env
```

See [doc/09-environment-variables.md](doc/09-environment-variables.md) for full documentation.

### Demo Accounts

| Username  | Password  | Role   | Branches               |
| --------- | --------- | ------ | ---------------------- |
| admin01   | P@ssw0rd  | Admin  | สาขาสยาม, สาขาทองหล่อ  |
| user01    | P@ssw0rd  | User   | สาขาสยาม               |
| viewer01  | P@ssw0rd  | Viewer | สาขาสยาม               |

### Seeded Data

On first run, the database is seeded with:

- **1 Tenant** — คลินิกสุขภาพดี
- **2 Branches** — สาขาสยาม, สาขาทองหล่อ
- **3 Users** — admin01 (Admin), user01 (User), viewer01 (Viewer)
- **10 Patients** — with Thai names, phone numbers, and assigned branches
- **8 Appointments** — 4 past + 4 upcoming, across both branches
- **12 Patient Visits** — with clinical notes (e.g. ตรวจสุขภาพประจำปี, ติดตามผลเลือด)

## Architecture

### Backend — Clean Architecture

```
src/backend/
├── ClinicPOS.API/              # Controllers, Middleware, Program.cs
├── ClinicPOS.Application/      # Use Cases (Commands/Queries/Handlers)
├── ClinicPOS.Domain/           # Entities, Enums
├── ClinicPOS.Infrastructure/   # EF Core, Repositories, JWT, Redis, RabbitMQ
└── ClinicPOS.UnitTests/        # xUnit tests
```

### Frontend — Feature-Based

```
src/frontend/src/
├── app/                  # Next.js App Router pages
│   ├── (auth)/           # Login page
│   ├── patients/         # Patient list & detail
│   └── appointments/     # Appointment list & create
└── shared/               # Shared components, hooks, API client
```

### Tenant Isolation (Multi-Tenant Safety)

The platform ensures **complete data isolation** between tenants:

1. **Global Query Filters (EF Core)** — Every query on `Patient`, `Branch`, `Appointment`, and `PatientVisit` is automatically filtered by `TenantId`. No tenant can see another tenant's data, even if a developer forgets to add a `WHERE` clause.

2. **TenantId from JWT** — The `TenantId` is extracted from the authenticated user's JWT token via `TenantContextMiddleware`. It is **never** passed in the request body, preventing tenant spoofing.

3. **Scoped Tenant Context** — Each HTTP request gets its own `TenantContext` (scoped DI), ensuring tenant isolation in concurrent scenarios.

4. **Per-Tenant Unique Constraints** — Business uniqueness rules (e.g., patient phone number) are scoped per-tenant: `UNIQUE(tenant_id, phone_number)`.

5. **User ↔ Branch Authorization** — Users are assigned to specific branches. The system enforces branch-level access in addition to tenant-level isolation.

### Assumptions & Trade-offs

| Decision | Rationale | Trade-off |
|---|---|---|
| `PrimaryBranchId` (nullable FK) instead of mapping table | MVP simplicity, no extra JOIN needed | Cannot track multiple branch memberships (can extend with `patient_visits` table) |
| Global Query Filter for tenant isolation | Zero-risk of data leakage, auto-applied | Slightly less flexible for cross-tenant admin queries (need `IgnoreQueryFilters()`) |
| In-memory rate limiter (per-instance) | No Redis dependency for rate limiting | Doesn't share state across multiple API instances (use Redis-backed for horizontal scaling) |
| ILike search over full-text search | Simpler, sufficient for this scale | Not as performant at large scale (can add GIN indexes later) |
| Cursor-based over offset pagination | Stable under concurrent inserts | Slightly more complex implementation |
| Fire-and-forget event publishing | Appointment creation always succeeds | Event may be lost if RabbitMQ is down (acceptable for notifications) |
| Redis graceful degradation | App works even if Redis is down | May cause increased DB load when Redis is unavailable |
| RabbitMQ consumer as BackgroundService | Simpler than separate Docker service | Not independently scalable (adequate for v1) |

## API Endpoints

### Auth & Users

| Method | Endpoint                   | Access            |
| ------ | -------------------------- | ----------------- |
| POST   | /api/auth/login            | Public            |
| POST   | /api/users                 | Admin             |
| PUT    | /api/users/{id}/role       | Admin             |
| PUT    | /api/users/{id}/branches   | Admin             |

### Patients

| Method | Endpoint                                    | Access            |
| ------ | ------------------------------------------- | ----------------- |
| POST   | /api/patients                               | Admin, User       |
| GET    | /api/patients?branchId=&search=&cursor=&limit= | All authenticated |

### Appointments

| Method | Endpoint                             | Access            |
| ------ | ------------------------------------ | ----------------- |
| POST   | /api/appointments                    | Admin, User       |
| GET    | /api/appointments?branchId=&date=    | All authenticated |

### Patient Visits

| Method | Endpoint                                    | Access            |
| ------ | ------------------------------------------- | ----------------- |
| POST   | /api/patients/{patientId}/visits            | Admin, User       |
| GET    | /api/patients/{patientId}/visits            | All authenticated |

### Other

| Method | Endpoint                   | Access            |
| ------ | -------------------------- | ----------------- |
| GET    | /api/branches              | All authenticated |
| GET    | /api/audit-logs            | Admin             |
| GET    | /health                    | Public            |

> 📖 **Full curl examples:** [doc/08-api-curl-examples.md](doc/08-api-curl-examples.md)

### Authorization (RBAC)

- **`[RequirePermission]` attribute** for endpoint-level access control
- **Admin** → Full access (CRUD patients, appointments, manage users, view audit logs)
- **User** → Create/View patients, appointments, visits
- **Viewer** → Read-only access

### Caching (Redis)

`GET /api/patients` responses are cached in Redis (5min TTL, cache-aside pattern). Cache is automatically invalidated when a new patient is created.

### Events (RabbitMQ)

When an appointment is created, an `appointment.created` event is published to the `clinic` topic exchange (RabbitMQ). A background consumer service processes these events for notifications.

## API Example (curl)

```bash
# 1. Login — get JWT token
TOKEN=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin01","password":"P@ssw0rd"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

# 2. List patients
curl -s http://localhost:5001/api/patients \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool

# 3. Create a patient
curl -s -X POST http://localhost:5001/api/patients \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"firstName":"ทดสอบ","lastName":"ระบบ","phoneNumber":"099-999-9999","primaryBranchId":"b0000000-0000-0000-0000-000000000001"}'

# 4. List appointments
curl -s http://localhost:5001/api/appointments \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool
```

> 📖 See [doc/08-api-curl-examples.md](doc/08-api-curl-examples.md) for all endpoints.

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
