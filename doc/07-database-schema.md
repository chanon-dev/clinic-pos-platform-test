# Database Schema Design

## Clinic POS Platform — v1

---

## 1. Design Principles

| Principle                    | Implementation                                              |
| ---------------------------- | ----------------------------------------------------------- |
| **Tenant Isolation**         | ทุก table มี `tenant_id` FK + Global Query Filter (EF Core) |
| **Concurrency Safety**       | UNIQUE constraints ที่ DB level (ไม่พึ่ง app-level check)     |
| **UUID Primary Keys**        | ป้องกัน ID enumeration, รองรับ distributed systems           |
| **Timestamps with Timezone** | `TIMESTAMPTZ` ทุก datetime column                           |
| **Naming Convention**        | snake_case สำหรับ tables/columns                             |
| **Soft Delete**              | ไม่ใช้ใน v1 (MVP simplicity) — เพิ่มได้ภายหลัง               |

---

## 2. Entity Relationship Diagram

```
                              ┌─────────────────┐
                              │     tenants      │
                              ├─────────────────┤
                              │ id          (PK) │
                              │ name             │
                              │ created_at       │
                              └────────┬────────┘
                                       │
                   ┌───────────────────┼───────────────────┐
                   │                   │                   │
          ┌────────▼────────┐ ┌────────▼────────┐ ┌────────▼────────┐
          │    branches      │ │     users        │ │    patients      │
          ├─────────────────┤ ├─────────────────┤ ├─────────────────┤
          │ id          (PK) │ │ id          (PK) │ │ id          (PK) │
          │ tenant_id   (FK) │ │ tenant_id   (FK) │ │ tenant_id   (FK) │
          │ name             │ │ username         │ │ first_name       │
          │ created_at       │ │ password_hash    │ │ last_name        │
          └──┬───────────┬──┘ │ role             │ │ phone_number     │
             │           │    │ created_at       │ │ primary_branch_id│──┐
             │           │    └────────┬────────┘ │ created_at       │  │
             │           │             │           └────────┬────────┘  │
             │           │    ┌────────▼────────┐          │           │
             │           │    │  user_branches   │          │           │
             │           │    ├─────────────────┤          │           │
             │           └───►│ branch_id   (FK) │          │           │
             │                │ user_id     (FK) │◄─────────┘           │
             │                └─────────────────┘                      │
             │                                                         │
             │     FK (primary_branch_id → branches.id) ◄──────────────┘
             │
             │           ┌──────────────────┐
             └──────────►│   appointments    │
                         ├──────────────────┤
                         │ id           (PK) │
                         │ tenant_id    (FK) │
                         │ branch_id    (FK) │
                         │ patient_id   (FK) │
                         │ start_at          │
                         │ created_at        │
                         └──────────────────┘
```

---

## 3. Table Definitions

### 3.1 `tenants`

Root entity — ทุกข้อมูลในระบบสังกัด Tenant

```sql
CREATE TABLE tenants (
    id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(200)    NOT NULL,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT now()
);
```

| Column     | Type           | Nullable | Default             | Description       |
| ---------- | -------------- | -------- | ------------------- | ----------------- |
| id         | UUID           | No       | gen_random_uuid()   | Primary Key       |
| name       | VARCHAR(200)   | No       | —                   | ชื่อคลินิก/องค์กร  |
| created_at | TIMESTAMPTZ    | No       | now()               | วันที่สร้าง        |

**Indexes:** PK only (low cardinality table)

---

### 3.2 `branches`

สาขาของแต่ละ Tenant

```sql
CREATE TABLE branches (
    id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID            NOT NULL REFERENCES tenants(id),
    name        VARCHAR(200)    NOT NULL,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT now()
);

CREATE INDEX ix_branches_tenant_id ON branches(tenant_id);
```

| Column     | Type           | Nullable | Default             | Description         |
| ---------- | -------------- | -------- | ------------------- | ------------------- |
| id         | UUID           | No       | gen_random_uuid()   | Primary Key         |
| tenant_id  | UUID           | No       | —                   | FK → tenants        |
| name       | VARCHAR(200)   | No       | —                   | ชื่อสาขา             |
| created_at | TIMESTAMPTZ    | No       | now()               | วันที่สร้าง          |

**Indexes:**

| Name                    | Columns     | Type    | Purpose              |
| ----------------------- | ----------- | ------- | -------------------- |
| ix_branches_tenant_id   | tenant_id   | B-tree  | Tenant filter        |

---

### 3.3 `users`

ผู้ใช้งานระบบ — สังกัด 1 Tenant, มีได้หลาย Branch

```sql
CREATE TABLE users (
    id              UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID            NOT NULL REFERENCES tenants(id),
    username        VARCHAR(100)    NOT NULL,
    password_hash   VARCHAR(500)    NOT NULL,
    role            VARCHAR(20)     NOT NULL CHECK (role IN ('Admin', 'User', 'Viewer')),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),

    CONSTRAINT uq_users_username UNIQUE (username)
);

CREATE INDEX ix_users_tenant_id ON users(tenant_id);
```

| Column        | Type           | Nullable | Default             | Description             |
| ------------- | -------------- | -------- | ------------------- | ----------------------- |
| id            | UUID           | No       | gen_random_uuid()   | Primary Key             |
| tenant_id     | UUID           | No       | —                   | FK → tenants            |
| username      | VARCHAR(100)   | No       | —                   | Login username (unique) |
| password_hash | VARCHAR(500)   | No       | —                   | BCrypt hashed password  |
| role          | VARCHAR(20)    | No       | —                   | Admin / User / Viewer   |
| created_at    | TIMESTAMPTZ    | No       | now()               | วันที่สร้าง              |

**Indexes:**

| Name                  | Columns    | Type    | Purpose                |
| --------------------- | ---------- | ------- | ---------------------- |
| uq_users_username     | username   | Unique  | Prevent duplicate user |
| ix_users_tenant_id    | tenant_id  | B-tree  | Tenant filter          |

**Design Note:** `username` เป็น globally unique (ไม่ใช่ per-tenant) เพื่อ simplicity ในการ login — ไม่ต้องเลือก tenant ก่อน login

---

### 3.4 `user_branches`

Many-to-Many: User ↔ Branch — กำหนดว่า User เข้าถึง Branch ไหนได้บ้าง

```sql
CREATE TABLE user_branches (
    user_id     UUID    NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    branch_id   UUID    NOT NULL REFERENCES branches(id) ON DELETE CASCADE,

    PRIMARY KEY (user_id, branch_id)
);

CREATE INDEX ix_user_branches_branch_id ON user_branches(branch_id);
```

| Column    | Type | Nullable | Description          |
| --------- | ---- | -------- | -------------------- |
| user_id   | UUID | No       | FK → users           |
| branch_id | UUID | No       | FK → branches        |

**PK:** Composite (user_id, branch_id)

**Indexes:**

| Name                         | Columns    | Type    | Purpose              |
| ---------------------------- | ---------- | ------- | -------------------- |
| PK (user_id, branch_id)     | composite  | Unique  | Prevent duplicates   |
| ix_user_branches_branch_id   | branch_id  | B-tree  | Reverse lookup       |

---

### 3.5 `patients`

ผู้ป่วย — สังกัด 1 Tenant, Phone unique ภายใน Tenant เดียวกัน

```sql
CREATE TABLE patients (
    id                  UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           UUID            NOT NULL REFERENCES tenants(id),
    first_name          VARCHAR(100)    NOT NULL,
    last_name           VARCHAR(100)    NOT NULL,
    phone_number        VARCHAR(20)     NOT NULL,
    primary_branch_id   UUID            REFERENCES branches(id),
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT now(),

    CONSTRAINT uq_patients_tenant_phone UNIQUE (tenant_id, phone_number)
);

CREATE INDEX ix_patients_tenant_id ON patients(tenant_id);
CREATE INDEX ix_patients_tenant_branch ON patients(tenant_id, primary_branch_id);
CREATE INDEX ix_patients_created_at ON patients(created_at DESC);
```

| Column            | Type           | Nullable | Default             | Description                 |
| ----------------- | -------------- | -------- | ------------------- | --------------------------- |
| id                | UUID           | No       | gen_random_uuid()   | Primary Key                 |
| tenant_id         | UUID           | No       | —                   | FK → tenants                |
| first_name        | VARCHAR(100)   | No       | —                   | ชื่อ                         |
| last_name         | VARCHAR(100)   | No       | —                   | นามสกุล                      |
| phone_number      | VARCHAR(20)    | No       | —                   | เบอร์โทร (unique per tenant) |
| primary_branch_id | UUID           | **Yes**  | NULL                | FK → branches (optional)    |
| created_at        | TIMESTAMPTZ    | No       | now()               | วันที่สร้าง (server-gen)      |

**Constraints:**

| Name                         | Type    | Columns                    | Purpose                           |
| ---------------------------- | ------- | -------------------------- | --------------------------------- |
| uq_patients_tenant_phone     | UNIQUE  | (tenant_id, phone_number)  | Phone unique ภายใน tenant เดียวกัน  |

**Indexes:**

| Name                         | Columns                         | Type     | Purpose                         |
| ---------------------------- | ------------------------------- | -------- | ------------------------------- |
| uq_patients_tenant_phone     | (tenant_id, phone_number)       | Unique   | Duplicate prevention + lookup   |
| ix_patients_tenant_id        | tenant_id                       | B-tree   | Global Query Filter             |
| ix_patients_tenant_branch    | (tenant_id, primary_branch_id)  | B-tree   | Branch filter query             |
| ix_patients_created_at       | created_at DESC                 | B-tree   | Sort by newest                  |

**Why `primary_branch_id` (nullable FK) instead of mapping table:**

```
MVP Decision:
├── ✅ Simpler query (no JOIN)
├── ✅ Sufficient for v1 (track registration branch)
├── ✅ Extensible — add patient_visits table later without schema change
└── ❌ Cannot track multiple branch visits (acceptable for v1)
```

---

### 3.6 `appointments`

นัดหมาย — ป้องกัน duplicate ด้วย UNIQUE constraint

```sql
CREATE TABLE appointments (
    id          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID            NOT NULL REFERENCES tenants(id),
    branch_id   UUID            NOT NULL REFERENCES branches(id),
    patient_id  UUID            NOT NULL REFERENCES patients(id),
    start_at    TIMESTAMPTZ     NOT NULL,
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT now(),

    CONSTRAINT uq_appointments_no_duplicate
        UNIQUE (tenant_id, branch_id, patient_id, start_at)
);

CREATE INDEX ix_appointments_tenant_id ON appointments(tenant_id);
CREATE INDEX ix_appointments_tenant_branch ON appointments(tenant_id, branch_id);
CREATE INDEX ix_appointments_tenant_branch_date ON appointments(tenant_id, branch_id, start_at);
CREATE INDEX ix_appointments_patient_id ON appointments(patient_id);
```

| Column     | Type          | Nullable | Default             | Description                   |
| ---------- | ------------- | -------- | ------------------- | ----------------------------- |
| id         | UUID          | No       | gen_random_uuid()   | Primary Key                   |
| tenant_id  | UUID          | No       | —                   | FK → tenants                  |
| branch_id  | UUID          | No       | —                   | FK → branches                 |
| patient_id | UUID          | No       | —                   | FK → patients                 |
| start_at   | TIMESTAMPTZ   | No       | —                   | วันเวลาที่นัดหมาย               |
| created_at | TIMESTAMPTZ   | No       | now()               | วันที่สร้าง record              |

**Constraints:**

| Name                           | Type    | Columns                                      | Purpose                       |
| ------------------------------ | ------- | -------------------------------------------- | ----------------------------- |
| uq_appointments_no_duplicate   | UNIQUE  | (tenant_id, branch_id, patient_id, start_at) | ป้องกัน duplicate booking       |

**Indexes:**

| Name                                  | Columns                              | Type    | Purpose                  |
| ------------------------------------- | ------------------------------------ | ------- | ------------------------ |
| uq_appointments_no_duplicate          | (tenant_id, branch_id, patient_id, start_at) | Unique | Duplicate prevention     |
| ix_appointments_tenant_id             | tenant_id                            | B-tree  | Global Query Filter      |
| ix_appointments_tenant_branch         | (tenant_id, branch_id)               | B-tree  | Branch filter            |
| ix_appointments_tenant_branch_date    | (tenant_id, branch_id, start_at)     | B-tree  | Date range query         |
| ix_appointments_patient_id            | patient_id                           | B-tree  | Patient's appointments   |

**Concurrency Safety:**

```
Scenario: 2 concurrent requests สร้าง appointment เดียวกัน

Request A ──► INSERT ──► SUCCESS (201)
Request B ──► INSERT ──► UNIQUE VIOLATION ──► Catch ──► 409 Conflict

DB UNIQUE constraint เป็น serializable — ไม่มี race condition
ไม่ต้องใช้ application-level locking
```

---

## 4. EF Core Configuration

### 4.1 Global Query Filter (Tenant Isolation)

```csharp
// AppDbContext.cs
public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Auto-filter ทุก tenant-scoped entity
        builder.Entity<Patient>()
            .HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);

        builder.Entity<Appointment>()
            .HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);

        builder.Entity<Branch>()
            .HasQueryFilter(b => b.TenantId == _tenantContext.TenantId);

        builder.Entity<User>()
            .HasQueryFilter(u => u.TenantId == _tenantContext.TenantId);
    }
}
```

### 4.2 Entity Configuration Examples

```csharp
// PatientConfiguration.cs
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");

        // Tenant-scoped unique phone number
        builder.HasIndex(p => new { p.TenantId, p.PhoneNumber })
            .IsUnique()
            .HasDatabaseName("uq_patients_tenant_phone");

        // Filter indexes
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("ix_patients_tenant_id");
        builder.HasIndex(p => new { p.TenantId, p.PrimaryBranchId })
            .HasDatabaseName("ix_patients_tenant_branch");
        builder.HasIndex(p => p.CreatedAt)
            .IsDescending()
            .HasDatabaseName("ix_patients_created_at");

        // Relationships
        builder.HasOne<Tenant>().WithMany()
            .HasForeignKey(p => p.TenantId);
        builder.HasOne<Branch>().WithMany()
            .HasForeignKey(p => p.PrimaryBranchId)
            .IsRequired(false);
    }
}
```

```csharp
// AppointmentConfiguration.cs
public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.StartAt).IsRequired();
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

        // Prevent duplicate booking
        builder.HasIndex(a => new { a.TenantId, a.BranchId, a.PatientId, a.StartAt })
            .IsUnique()
            .HasDatabaseName("uq_appointments_no_duplicate");

        // Query indexes
        builder.HasIndex(a => new { a.TenantId, a.BranchId, a.StartAt })
            .HasDatabaseName("ix_appointments_tenant_branch_date");

        // Relationships
        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId);
        builder.HasOne<Branch>().WithMany().HasForeignKey(a => a.BranchId);
        builder.HasOne<Patient>().WithMany().HasForeignKey(a => a.PatientId);
    }
}
```

---

## 5. Migration Strategy

### 5.1 Migration Commands

```bash
# สร้าง migration ใหม่
dotnet ef migrations add InitialCreate -p src/ClinicPOS.Infrastructure -s src/ClinicPOS.API

# Apply migration
dotnet ef database update -p src/ClinicPOS.Infrastructure -s src/ClinicPOS.API
```

### 5.2 Auto-Migrate on Startup

```csharp
// Program.cs
app.Services.CreateScope().ServiceProvider
    .GetRequiredService<AppDbContext>()
    .Database.Migrate();
```

### 5.3 Migration History

| Migration         | Tables Created                        | Phase |
| ----------------- | ------------------------------------- | ----- |
| InitialCreate     | tenants, branches, users, user_branches, patients | 1 |
| AddAppointments   | appointments                          | 2     |

---

## 6. Seed Data

```sql
-- Tenant
INSERT INTO tenants (id, name) VALUES
    ('a0000000-0000-0000-0000-000000000001', 'คลินิกสุขภาพดี');

-- Branches
INSERT INTO branches (id, tenant_id, name) VALUES
    ('b0000000-0000-0000-0000-000000000001', 'a0000000-...001', 'สาขาสยาม'),
    ('b0000000-0000-0000-0000-000000000002', 'a0000000-...001', 'สาขาทองหล่อ');

-- Users (password: P@ssw0rd → bcrypt hash)
INSERT INTO users (id, tenant_id, username, password_hash, role) VALUES
    ('c0000000-...-001', 'a0000000-...001', 'admin01',  '$2a$11$...', 'Admin'),
    ('c0000000-...-002', 'a0000000-...001', 'user01',   '$2a$11$...', 'User'),
    ('c0000000-...-003', 'a0000000-...001', 'viewer01', '$2a$11$...', 'Viewer');

-- User-Branch associations
INSERT INTO user_branches (user_id, branch_id) VALUES
    ('c0000000-...-001', 'b0000000-...-001'),  -- admin → สาขาสยาม
    ('c0000000-...-001', 'b0000000-...-002'),  -- admin → สาขาทองหล่อ
    ('c0000000-...-002', 'b0000000-...-001'),  -- user  → สาขาสยาม
    ('c0000000-...-003', 'b0000000-...-001');   -- viewer → สาขาสยาม
```

| User      | Role    | Branches             | Password  |
| --------- | ------- | -------------------- | --------- |
| admin01   | Admin   | สาขาสยาม, สาขาทองหล่อ | P@ssw0rd  |
| user01    | User    | สาขาสยาม              | P@ssw0rd  |
| viewer01  | Viewer  | สาขาสยาม              | P@ssw0rd  |

---

## 7. Future Schema Extensions (Phase 3)

สิ่งที่สามารถเพิ่มได้โดย **ไม่ต้องแก้ schema เดิม:**

```sql
-- Patient Visit History (multi-branch tracking)
CREATE TABLE patient_visits (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    patient_id  UUID NOT NULL REFERENCES patients(id),
    branch_id   UUID NOT NULL REFERENCES branches(id),
    visited_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Audit Log
CREATE TABLE audit_logs (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   UUID NOT NULL REFERENCES tenants(id),
    user_id     UUID NOT NULL REFERENCES users(id),
    action      VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id   UUID NOT NULL,
    payload     JSONB,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

ทั้งสอง table เป็น **additive** — ไม่ modify table เดิม, ไม่ต้อง downtime
