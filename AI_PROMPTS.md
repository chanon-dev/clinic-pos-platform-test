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

## What I Would Improve With More Time

1. Phase 2 features — Appointment management, Redis caching, RabbitMQ messaging
2. Integration tests — Testing API endpoints with real database (TestContainers)
3. More comprehensive error handling — FluentValidation for complex validation rules
4. Frontend state management — TanStack Query for server state, optimistic updates
5. Pagination — Cursor-based pagination for patient list
6. Search — Full-text search on patient name/phone
