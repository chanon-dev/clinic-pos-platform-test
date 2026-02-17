# Technical Architecture Design

## Clinic POS Platform — v1

---

## 1. System Overview

```
┌─────────────────────────────────────────────────────────┐
│                      Client Layer                       │
│                  Next.js (App Router)                    │
│         SSR + Client Components + API Routes            │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTPS (REST JSON)
┌──────────────────────▼──────────────────────────────────┐
│                    API Gateway                          │
│               .NET 10 Web API                           │
│          JWT Auth │ RBAC Middleware │ Validation         │
├─────────────────────────────────────────────────────────┤
│                 Application Layer                       │
│            Use Cases / Command & Query                  │
├─────────────────────────────────────────────────────────┤
│                  Domain Layer                           │
│         Entities │ Value Objects │ Rules                 │
├─────────────────────────────────────────────────────────┤
│               Infrastructure Layer                      │
│    PostgreSQL │ Redis Cache │ RabbitMQ Publisher         │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Backend — Clean Architecture (.NET 10)

### 2.1 Folder Structure

```
src/
├── ClinicPOS.API/                      # Frameworks & Drivers
│   ├── Controllers/
│   │   ├── PatientsController.cs
│   │   ├── AppointmentsController.cs
│   │   └── UsersController.cs
│   ├── Middleware/
│   │   ├── TenantContextMiddleware.cs   # Extract TenantId from JWT/header
│   │   ├── AuthorizationMiddleware.cs
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Filters/
│   │   └── ValidationFilter.cs
│   ├── Program.cs
│   ├── appsettings.json
│   └── Dockerfile
│
├── ClinicPOS.Application/              # Use Cases (Application Layer)
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IPatientRepository.cs
│   │   │   ├── IAppointmentRepository.cs
│   │   │   ├── IUserRepository.cs
│   │   │   ├── ICacheService.cs
│   │   │   └── IEventPublisher.cs
│   │   └── Models/
│   │       ├── Result.cs               # Operation result wrapper
│   │       └── PagedList.cs
│   ├── Patients/
│   │   ├── Commands/
│   │   │   ├── CreatePatientCommand.cs
│   │   │   └── CreatePatientHandler.cs
│   │   └── Queries/
│   │       ├── ListPatientsQuery.cs
│   │       └── ListPatientsHandler.cs
│   ├── Appointments/
│   │   ├── Commands/
│   │   │   ├── CreateAppointmentCommand.cs
│   │   │   └── CreateAppointmentHandler.cs
│   │   └── Events/
│   │       └── AppointmentCreatedEvent.cs
│   └── Users/
│       ├── Commands/
│       │   ├── CreateUserCommand.cs
│       │   ├── AssignRoleCommand.cs
│       │   └── AssociateUserBranchCommand.cs
│       └── Queries/
│           └── AuthenticateUserQuery.cs
│
├── ClinicPOS.Domain/                   # Entities (Domain Layer)
│   ├── Entities/
│   │   ├── Patient.cs
│   │   ├── Appointment.cs
│   │   ├── User.cs
│   │   ├── Tenant.cs
│   │   ├── Branch.cs
│   │   └── UserBranch.cs              # Many-to-Many: User ↔ Branch
│   ├── Enums/
│   │   ├── Role.cs                    # Admin, User, Viewer
│   │   └── Permission.cs             # CreatePatient, ViewPatient, CreateAppointment
│   └── ValueObjects/
│       └── PhoneNumber.cs             # Validation logic encapsulated
│
├── ClinicPOS.Infrastructure/           # Interface Adapters & Drivers
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── PatientConfiguration.cs     # Unique index: TenantId + PhoneNumber
│   │   │   ├── AppointmentConfiguration.cs # Unique index: TenantId + BranchId + PatientId + StartAt
│   │   │   ├── UserConfiguration.cs
│   │   │   └── TenantConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── PatientRepository.cs
│   │   │   ├── AppointmentRepository.cs
│   │   │   └── UserRepository.cs
│   │   ├── Migrations/
│   │   └── Seeder/
│   │       └── DataSeeder.cs           # 1 Tenant, 2 Branches, 3 Users
│   ├── Caching/
│   │   └── RedisCacheService.cs
│   ├── Messaging/
│   │   └── RabbitMqEventPublisher.cs
│   └── Auth/
│       └── JwtTokenService.cs
│
tests/
├── ClinicPOS.UnitTests/
│   ├── Domain/
│   │   └── PatientTests.cs
│   └── Application/
│       ├── CreatePatientHandlerTests.cs
│       └── CreateAppointmentHandlerTests.cs
└── ClinicPOS.IntegrationTests/
    └── Patients/
        └── PatientsApiTests.cs
```

### 2.2 Key Design Decisions

| Decision                         | Rationale                                                    |
| -------------------------------- | ------------------------------------------------------------ |
| CQRS-lite (Command/Query split)  | แยก Read/Write logic ชัดเจน รองรับ Cache ฝั่ง Query ง่าย       |
| Repository Pattern               | Abstraction สำหรับ Data Access ทดสอบง่ายด้วย Mock              |
| TenantContext Middleware          | ดึง TenantId จาก JWT claims แล้ว inject เข้า scope ทุก request  |
| Global Query Filter (EF Core)    | ทุก query จะถูก filter ด้วย TenantId อัตโนมัติ ป้องกัน data leak |
| Result Pattern (ไม่ throw exception) | ใช้ Result<T> wrap ผลลัพธ์ ลด exception overhead              |

### 2.3 Tenant Safety Strategy

```csharp
// AppDbContext.cs — Global Query Filter
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.Entity<Patient>()
        .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);

    builder.Entity<Appointment>()
        .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
}
```

ทุก Entity ที่เป็น Tenant-scoped จะถูก filter โดยอัตโนมัติ — Developer ไม่ต้อง remember to filter

---

## 3. Frontend — Feature-Based Architecture (Next.js)

### 3.1 Folder Structure

```
frontend/
├── src/
│   ├── app/                             # Next.js App Router
│   │   ├── layout.tsx                   # Root layout + providers
│   │   ├── page.tsx                     # Dashboard / redirect
│   │   ├── (auth)/
│   │   │   └── login/
│   │   │       └── page.tsx
│   │   ├── patients/
│   │   │   ├── page.tsx                 # List Patients
│   │   │   └── new/
│   │   │       └── page.tsx             # Create Patient
│   │   └── appointments/
│   │       ├── page.tsx                 # List Appointments
│   │       └── new/
│   │           └── page.tsx             # Create Appointment
│   │
│   ├── features/                        # Feature modules
│   │   ├── auth/
│   │   │   ├── components/
│   │   │   │   └── LoginForm.tsx
│   │   │   ├── hooks/
│   │   │   │   └── useAuth.ts
│   │   │   ├── services/
│   │   │   │   └── authApi.ts
│   │   │   └── types/
│   │   │       └── auth.types.ts
│   │   │
│   │   ├── patients/
│   │   │   ├── components/
│   │   │   │   ├── PatientForm.tsx
│   │   │   │   ├── PatientList.tsx
│   │   │   │   └── PatientCard.tsx
│   │   │   ├── hooks/
│   │   │   │   ├── usePatients.ts
│   │   │   │   └── useCreatePatient.ts
│   │   │   ├── services/
│   │   │   │   └── patientApi.ts
│   │   │   └── types/
│   │   │       └── patient.types.ts
│   │   │
│   │   └── appointments/
│   │       ├── components/
│   │       │   ├── AppointmentForm.tsx
│   │       │   └── AppointmentList.tsx
│   │       ├── hooks/
│   │       │   ├── useAppointments.ts
│   │       │   └── useCreateAppointment.ts
│   │       ├── services/
│   │       │   └── appointmentApi.ts
│   │       └── types/
│   │           └── appointment.types.ts
│   │
│   ├── shared/                          # Shared across features
│   │   ├── components/
│   │   │   ├── ui/                      # Base UI components
│   │   │   │   ├── Button.tsx
│   │   │   │   ├── Input.tsx
│   │   │   │   ├── Select.tsx
│   │   │   │   ├── Table.tsx
│   │   │   │   └── Alert.tsx
│   │   │   ├── layout/
│   │   │   │   ├── Sidebar.tsx
│   │   │   │   ├── Header.tsx
│   │   │   │   └── MainLayout.tsx
│   │   │   └── BranchSelector.tsx       # Reusable branch filter
│   │   ├── hooks/
│   │   │   ├── useApi.ts               # Fetch wrapper with auth
│   │   │   └── useTenant.ts            # Tenant context hook
│   │   ├── lib/
│   │   │   ├── apiClient.ts            # Axios/fetch instance
│   │   │   └── constants.ts
│   │   └── types/
│   │       └── api.types.ts            # Common API response types
│   │
│   └── providers/
│       ├── AuthProvider.tsx
│       └── TenantProvider.tsx
│
├── public/
├── next.config.ts
├── tailwind.config.ts
├── tsconfig.json
├── package.json
└── Dockerfile
```

### 3.2 Key Design Decisions

| Decision                    | Rationale                                                |
| --------------------------- | -------------------------------------------------------- |
| Feature-Based Structure     | แต่ละ feature เป็น self-contained, ง่ายต่อการเพิ่ม feature ใหม่ |
| Colocation (components + hooks + services) | ลด cross-folder dependency, ง่ายต่อการ delete/refactor |
| App Router (Next.js 15)     | Server Components + Streaming + Parallel Routes           |
| TanStack Query (React Query) | Server state management, caching, optimistic updates     |
| Tailwind CSS                | Rapid prototyping, consistent design tokens               |

---

## 4. Data Schema (Entity Relationship)

### 4.1 ER Diagram

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│   Tenant     │       │   Branch     │       │    User      │
├──────────────┤       ├──────────────┤       ├──────────────┤
│ Id (PK)      │──┐    │ Id (PK)      │    ┌──│ Id (PK)      │
│ Name         │  │    │ Name         │    │  │ Username     │
│ CreatedAt    │  │    │ TenantId(FK) │◄───┤  │ PasswordHash │
└──────────────┘  │    │ CreatedAt    │    │  │ Role (enum)  │
                  │    └──────────────┘    │  │ TenantId(FK) │
                  │                        │  │ CreatedAt    │
                  │    ┌──────────────┐    │  └──────────────┘
                  │    │ UserBranch   │    │         │
                  │    ├──────────────┤    │         │
                  │    │ UserId (FK)  │────┘         │
                  │    │ BranchId(FK) │              │
                  │    └──────────────┘              │
                  │                                  │
                  │    ┌──────────────────┐          │
                  ├───►│   Patient        │          │
                  │    ├──────────────────┤          │
                  │    │ Id (PK)          │          │
                  │    │ FirstName        │          │
                  │    │ LastName         │          │
                  │    │ PhoneNumber      │          │
                  │    │ TenantId (FK)    │          │
                  │    │ PrimaryBranchId  │ (FK, nullable)
                  │    │ CreatedAt        │          │
                  │    └──────────────────┘          │
                  │    UNIQUE(TenantId, PhoneNumber)  │
                  │                                  │
                  │    ┌──────────────────┐          │
                  └───►│  Appointment     │          │
                       ├──────────────────┤          │
                       │ Id (PK)          │          │
                       │ TenantId (FK)    │          │
                       │ BranchId (FK)    │          │
                       │ PatientId (FK)   │          │
                       │ StartAt          │          │
                       │ CreatedAt        │          │
                       └──────────────────┘
                       UNIQUE(TenantId, BranchId, PatientId, StartAt)
```

### 4.2 Table Definitions

#### `tenants`

| Column     | Type         | Constraints       |
| ---------- | ------------ | ----------------- |
| id         | UUID         | PK, DEFAULT gen   |
| name       | VARCHAR(200) | NOT NULL          |
| created_at | TIMESTAMPTZ  | NOT NULL, DEFAULT |

#### `branches`

| Column     | Type         | Constraints              |
| ---------- | ------------ | ------------------------ |
| id         | UUID         | PK, DEFAULT gen          |
| name       | VARCHAR(200) | NOT NULL                 |
| tenant_id  | UUID         | FK → tenants(id), NOT NULL |
| created_at | TIMESTAMPTZ  | NOT NULL, DEFAULT        |

#### `users`

| Column        | Type         | Constraints              |
| ------------- | ------------ | ------------------------ |
| id            | UUID         | PK, DEFAULT gen          |
| username      | VARCHAR(100) | NOT NULL, UNIQUE         |
| password_hash | VARCHAR(500) | NOT NULL                 |
| role          | VARCHAR(20)  | NOT NULL (Admin/User/Viewer) |
| tenant_id     | UUID         | FK → tenants(id), NOT NULL |
| created_at    | TIMESTAMPTZ  | NOT NULL, DEFAULT        |

#### `user_branches`

| Column    | Type | Constraints              |
| --------- | ---- | ------------------------ |
| user_id   | UUID | FK → users(id), NOT NULL |
| branch_id | UUID | FK → branches(id), NOT NULL |
| **PK**    |      | (user_id, branch_id)     |

#### `patients`

| Column            | Type         | Constraints                              |
| ----------------- | ------------ | ---------------------------------------- |
| id                | UUID         | PK, DEFAULT gen                          |
| first_name        | VARCHAR(100) | NOT NULL                                 |
| last_name         | VARCHAR(100) | NOT NULL                                 |
| phone_number      | VARCHAR(20)  | NOT NULL                                 |
| tenant_id         | UUID         | FK → tenants(id), NOT NULL               |
| primary_branch_id | UUID         | FK → branches(id), NULLABLE              |
| created_at        | TIMESTAMPTZ  | NOT NULL, DEFAULT                        |
| **UNIQUE**        |              | (tenant_id, phone_number)                |

#### `appointments`

| Column     | Type        | Constraints                                        |
| ---------- | ----------- | -------------------------------------------------- |
| id         | UUID        | PK, DEFAULT gen                                    |
| tenant_id  | UUID        | FK → tenants(id), NOT NULL                         |
| branch_id  | UUID        | FK → branches(id), NOT NULL                        |
| patient_id | UUID        | FK → patients(id), NOT NULL                        |
| start_at   | TIMESTAMPTZ | NOT NULL                                           |
| created_at | TIMESTAMPTZ | NOT NULL, DEFAULT                                  |
| **UNIQUE** |             | (tenant_id, branch_id, patient_id, start_at)       |

### 4.3 Design Rationale: Patient ↔ Branch Relationship

เลือกใช้ **`PrimaryBranchId` (nullable FK)** แทน separate mapping table เนื่องจาก:

1. **MVP Simplicity** — v1 ต้องการแค่ track ว่าผู้ป่วยสมัครที่ Branch ไหนเป็นหลัก
2. **Query Performance** — ไม่ต้อง JOIN table เพิ่ม
3. **Extensible** — Phase 2 สามารถเพิ่ม `patient_visits` table ได้โดยไม่ต้องเปลี่ยน schema เดิม

---

## 5. Cache Strategy (Redis)

### 5.1 Key Naming Convention

```
tenant:{tenantId}:patients:list:{branchId|all}
tenant:{tenantId}:patients:count
```

### 5.2 Invalidation Strategy

| Event                  | Invalidation Action                                    |
| ---------------------- | ------------------------------------------------------ |
| Create Patient         | DELETE keys matching `tenant:{tenantId}:patients:*`    |
| Create Appointment     | DELETE keys matching `tenant:{tenantId}:appointments:*` |

ใช้ **Key Pattern Deletion** (SCAN + DELETE) เพื่อ invalidate ทุก variant ของ cache key ภายใน Tenant เดียวกัน

### 5.3 TTL Policy

| Cache Type      | TTL      |
| --------------- | -------- |
| Patient List    | 5 min    |
| Count           | 5 min    |

---

## 6. Messaging (RabbitMQ)

### 6.1 Event: `appointment.created`

```json
{
  "eventType": "appointment.created",
  "timestamp": "2026-02-17T10:30:00Z",
  "payload": {
    "appointmentId": "uuid",
    "tenantId": "uuid",
    "branchId": "uuid",
    "patientId": "uuid",
    "startAt": "2026-02-18T09:00:00Z"
  }
}
```

Exchange: `clinic-pos.events` (topic exchange)
Routing Key: `appointment.created`

---

## 7. Infrastructure (Docker Compose)

```yaml
services:
  api:        .NET 10 Web API        (port 5000)
  frontend:   Next.js                (port 3000)
  db:         PostgreSQL 16          (port 5432)
  redis:      Redis 7                (port 6379)
  rabbitmq:   RabbitMQ 3-management  (port 5672 / 15672)
```

Single command: `docker compose up --build`
