# Phased Implementation Roadmap

## Clinic POS Platform — v1

---

## Design Principles

- **Working Software per Phase** — ทุก Phase ต้อง deploy ได้ ใช้งานจริงได้
- **Open-Closed Principle** — โครงสร้างรองรับ Add-on Feature โดยไม่ต้องรื้อระบบเดิม
- **Incremental Value** — แต่ละ Phase เพิ่มคุณค่าให้ระบบอย่างเป็นรูปธรรม

---

## Phase 1: Foundation + Core Slice (MVP)

> **Goal:** ระบบสร้างและค้นหาผู้ป่วยได้ พร้อม Tenant Isolation + Auth
>
> **Deployable:** Yes — ใช้งานจริงได้สำหรับ Patient Registration workflow

### Scope

| Section | Items                                                                  |
| ------- | ---------------------------------------------------------------------- |
| Infra   | Docker Compose (API + Frontend + PostgreSQL), DB Migrations, Seeder    |
| A1–A3   | Create Patient, List Patients, Filter by Tenant/Branch                 |
| B1–B4   | Roles, Permissions, User Management APIs, Auth Middleware, Seeder      |

### Deliverables

```
✅ docker compose up — ระบบพร้อมใช้งาน
✅ Seeder สร้าง Tenant, Branch, Users อัตโนมัติ
✅ Frontend: Login → Patient List → Create Patient
✅ API: Validated, Auth-protected, Tenant-safe
✅ Unit Tests: CreatePatient, Auth middleware
```

### Architecture Foundation ที่ Phase นี้วาง

| Component               | Ready For                                      |
| ------------------------ | ---------------------------------------------- |
| Clean Architecture       | เพิ่ม Use Case ใหม่ได้โดยไม่แก้โครงสร้าง          |
| TenantContext Middleware  | ทุก feature ใหม่ได้ Tenant safety ฟรี             |
| Global Query Filter      | ทุก Entity ใหม่แค่เพิ่ม TenantId ก็ safe           |
| RBAC Middleware           | เพิ่ม Permission ใหม่ได้ใน enum เดิม              |
| Feature-Based Frontend    | เพิ่ม feature folder ใหม่ ไม่กระทบ feature เดิม    |
| Docker Compose            | เพิ่ม service ใหม่ (Redis, RabbitMQ) ได้เลย       |

---

## Phase 2: Appointment + Messaging + Caching

> **Goal:** ระบบจัดการนัดหมายได้ พร้อม Caching และ Event-Driven Architecture
>
> **Deployable:** Yes — เพิ่มความสามารถ Appointment Booking ให้คลินิก

### Scope

| Section | Items                                                             |
| ------- | ----------------------------------------------------------------- |
| C1–C3   | Create Appointment, Duplicate Prevention, RabbitMQ Event Publish  |
| D1–D3   | Redis Cache (List Patients), Tenant-Scoped Keys, Invalidation    |

### Deliverables

```
✅ Appointment CRUD + Duplicate Booking Prevention
✅ RabbitMQ publish appointment.created event
✅ Redis cache สำหรับ List Patients (tenant-scoped)
✅ Cache invalidation เมื่อ Create Patient / Appointment
✅ Frontend: Appointment form + list
✅ Integration Tests: Concurrency duplicate test
```

### What Changes from Phase 1

| Change Type              | Detail                                            |
| ------------------------ | ------------------------------------------------- |
| **Add** service          | Redis, RabbitMQ เพิ่มใน docker-compose             |
| **Add** feature module   | `features/appointments/` (Frontend + Backend)      |
| **Add** infra adapter    | RedisCacheService, RabbitMqEventPublisher          |
| **No breaking change**   | Phase 1 code ไม่ถูกแก้ไข (เพิ่มอย่างเดียว)          |

---

## Phase 3: Advanced Features & Production Readiness

> **Goal:** ระบบพร้อมใช้งานจริงในระดับ Production
>
> **Deployable:** Yes — Production-grade

### Scope (Future — Out of 90-min test)

| Feature                    | Description                                          |
| -------------------------- | ---------------------------------------------------- |
| Patient Visit History      | `patient_visits` table สำหรับ track multi-branch visits |
| Pagination                 | Cursor-based pagination สำหรับ List endpoints         |
| Audit Logging              | Track who did what, when (tenant-scoped)             |
| Search                     | Full-text search สำหรับ Patient name/phone           |
| Notification Consumer      | RabbitMQ consumer สำหรับ send confirmation            |
| Rate Limiting              | Per-tenant rate limiting ป้องกัน abuse                |
| Health Checks              | /health endpoint สำหรับ monitoring                    |
| CI/CD Pipeline             | GitHub Actions: build → test → deploy                |

### What Changes from Phase 2

| Change Type             | Detail                                              |
| ----------------------- | --------------------------------------------------- |
| **Add** domain entity   | `PatientVisit` entity (ไม่กระทบ `Patient`)            |
| **Add** middleware       | Rate Limiting, Health Check middleware               |
| **Add** consumer        | RabbitMQ consumer service (new docker service)       |
| **No breaking change**  | ไม่มีการ refactor Phase 1/2 code                      |

---

## Phase Dependency Map

```
Phase 1 (MVP)
  │  Foundation: Docker + Auth + Patient CRUD
  │  ✅ Deployable & Usable
  │
  ▼
Phase 2 (Scaling)
  │  +Appointment +Cache +Messaging
  │  ✅ Deployable & Usable (superset of Phase 1)
  │
  ▼
Phase 3 (Advanced)
     +Visit History +Audit +Search +CI/CD
     ✅ Production-Ready
```

---

## Open-Closed Compliance Checklist

| Principle                                    | How It's Achieved                          |
| -------------------------------------------- | ------------------------------------------ |
| New feature = new folder (Backend)            | Add `Application/NewFeature/` + `Domain/Entities/` |
| New feature = new folder (Frontend)           | Add `features/newFeature/`                  |
| New permission = extend enum                  | Add value to `Permission.cs`                |
| New cache = implement ICacheService           | No change to existing cache logic           |
| New event = implement IEventPublisher         | No change to existing publish logic         |
| New entity with tenant = add HasQueryFilter   | Global filter pattern already established   |
