# AI Prompts Log

## Tool Used
- **Claude Code** (Claude Opus) — AI-assisted development via CLI

## Prompting Strategy

### Phase 1: Planning & Architecture Review
- Read all 7 documentation files (PRD, Technical Architecture, Implementation Roadmap, UI/UX, Backlog, API Spec, DB Schema) to understand the full scope
- Identified Phase 1 critical path: INFRA → AUTH → PAT → SEED → FE
- Decided to follow the Clean Architecture pattern as specified in the technical architecture doc

### Phase 2: Implementation (Backend)
- Created .NET solution with 4 projects following Clean Architecture (Domain → Application → Infrastructure → API)
- Domain entities first (Tenant, Branch, User, Patient, UserBranch), then interfaces, then implementations
- Used Result pattern for error handling without exceptions
- Implemented Global Query Filter for automatic tenant isolation
- Created JWT-based authentication with role claims
- Added RBAC via custom RequirePermission attribute filter

### Phase 3: Implementation (Frontend)
- Created Next.js with App Router + Tailwind CSS
- Feature-based folder structure (auth, patients)
- AuthProvider context for JWT token management
- Client-side form validation + server error handling (409 duplicate phone)
- Role-based UI (hide New Patient button for Viewer)

### Phase 4: Infrastructure & Testing
- Docker Compose with PostgreSQL, API, and Frontend
- Auto-migration and seeding on startup
- Unit tests for CreatePatientHandler (5 tests covering success, duplicate, validation, tenant context)

## Key AI-Assisted Decisions

1. **BCrypt in Application layer** — Initially placed BCrypt dependency only in Infrastructure, but needed it in Application layer for user creation/authentication handlers. AI caught the build error and added the package reference.

2. **JWT package in Infrastructure** — JwtTokenService needs System.IdentityModel.Tokens.Jwt, which requires Microsoft.AspNetCore.Authentication.JwtBearer package in the Infrastructure project.

3. **Global Query Filter null-safety** — Used tenantContext null guard in query filters to allow migrations and seeding to run without tenant context.

4. **UserRepository.IgnoreQueryFilters()** — Users need to be queried by username globally (login does not know tenant yet), so UserRepository uses IgnoreQueryFilters() for authentication.

### Phase 5: Phase 2 — Appointments + Caching + Messaging

**Prompt:** "implement Phase 2: Appointment + Caching + Messaging"

**What was implemented:**
- **Appointment entity** with TenantId, BranchId, PatientId, StartAt — full CRUD via REST API
- **Redis caching** (StackExchange.Redis) — cache-aside pattern on patient list with 5min TTL, auto-invalidation on patient creation
- **RabbitMQ messaging** (RabbitMQ.Client v7) — publishes `appointment.created` events to `clinic` topic exchange on appointment creation (fire-and-forget, non-blocking)
- **Docker Compose** updated with Redis 7 + RabbitMQ 3 (management) services with health checks
- **Frontend** — appointments list page with branch/date filters, create appointment form with patient/branch dropdowns
- **Global Query Filter** extended to Appointment entity for tenant isolation
- **Unique constraint** on (tenant_id, branch_id, patient_id, start_at) to prevent double-booking

**Key decisions:**
1. **RabbitMQ.Client v7 async API** — v7 uses `IChannel` instead of `IModel`, `CreateConnectionAsync`, `BasicPublishAsync`. All operations are async-first.
2. **Redis graceful degradation** — All cache operations are wrapped in try/catch. If Redis is down, the app falls back to direct DB queries without errors.
3. **Fire-and-forget event publishing** — Appointment creation succeeds even if RabbitMQ publish fails. Event publishing is logged but doesn't block the response.
4. **Cache key strategy** — `patients:{tenantId}:{branchId|all}` enables targeted invalidation by tenant prefix.

## What I Would Improve With More Time

1. Integration tests — Testing API endpoints with real database (TestContainers)
2. More comprehensive error handling — FluentValidation for complex validation rules
3. Frontend state management — TanStack Query for server state, optimistic updates
4. Pagination — Cursor-based pagination for patient list
5. Search — Full-text search on patient name/phone
6. Appointment updates/cancellation — Currently only create and list
7. RabbitMQ consumers — Add workers to process appointment events (notifications, analytics)
