# Implementation Tasks (Backlog)

## Clinic POS Platform — v1

---

## Definition of Done (Global)

ทุก Task ถือว่า Done เมื่อ:

- Code compiles / builds สำเร็จ
- ผ่าน Lint / Format check
- มี Unit Test (ถ้า Task ระบุ)
- ทดสอบ manual ผ่าน
- Commit message สื่อความหมาย

---

## Phase 1: Foundation + Core Slice

### P1-INFRA: Infrastructure Setup

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P1-INF-01  | สร้าง `docker-compose.yml` (API + Frontend + PostgreSQL) | `docker compose up` รัน 3 services สำเร็จ, healthy           |
| P1-INF-02  | สร้าง .NET 10 Web API project (Clean Architecture)     | Solution build สำเร็จ, โครงสร้าง 4 projects ตาม spec         |
| P1-INF-03  | สร้าง Next.js project (Feature-Based)                   | `npm run dev` สำเร็จ, โครงสร้าง folders ตาม spec             |
| P1-INF-04  | Setup EF Core + PostgreSQL connection                   | DbContext สร้าง, connection string config ถูกต้อง             |
| P1-INF-05  | สร้าง Initial Migration                                 | Migration files สร้าง, `dotnet ef database update` สำเร็จ    |
| P1-INF-06  | Setup Exception Handling Middleware                     | ทุก unhandled exception return RFC 7807 Problem Details       |
| P1-INF-07  | Setup Validation Filter (FluentValidation / DataAnnotations) | Invalid request return 400 + field-level errors          |

### P1-AUTH: Authentication & Authorization

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P1-AUT-01  | สร้าง User Entity + Migration                          | Table `users` สร้างใน DB, มี Role column                     |
| P1-AUT-02  | สร้าง Tenant, Branch, UserBranch Entities + Migration   | Tables สร้างสำเร็จ, FK relationships ถูกต้อง                 |
| P1-AUT-03  | Implement JWT Token Generation                         | `/api/auth/login` return JWT token พร้อม TenantId, Role claims |
| P1-AUT-04  | Implement TenantContext Middleware                      | ทุก request ดึง TenantId จาก JWT → inject เข้า scoped service  |
| P1-AUT-05  | Implement Authorization Middleware (RBAC)               | `[RequirePermission(Permission.CreatePatient)]` ทำงานถูกต้อง |
| P1-AUT-06  | สร้าง Create User API (`POST /api/users`)               | สร้าง User + hash password สำเร็จ                            |
| P1-AUT-07  | สร้าง Assign Role API (`PUT /api/users/{id}/role`)      | เปลี่ยน Role สำเร็จ, return updated user                     |
| P1-AUT-08  | สร้าง Associate User-Branch API                        | เพิ่ม/ลบ branch association สำเร็จ                            |
| P1-AUT-09  | Implement Global Query Filter (TenantId)               | ทุก query ถูก filter ด้วย TenantId อัตโนมัติ                   |
| P1-AUT-10  | **Test:** Viewer ไม่สามารถ Create Patient ได้            | Unit test ผ่าน: Viewer → 403 Forbidden                       |

### P1-PAT: Patient Management

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P1-PAT-01  | สร้าง Patient Entity + Migration                       | Table `patients` + UNIQUE(tenant_id, phone_number) สร้างสำเร็จ |
| P1-PAT-02  | Implement CreatePatientCommand + Handler               | สร้าง Patient สำเร็จ, PhoneNumber validate, TenantId จาก context |
| P1-PAT-03  | สร้าง `POST /api/patients` endpoint                    | 201 Created + Patient data, 409 เมื่อ phone ซ้ำ               |
| P1-PAT-04  | Implement ListPatientsQuery + Handler                  | Return patients filtered by TenantId, optional BranchId       |
| P1-PAT-05  | สร้าง `GET /api/patients` endpoint                     | 200 OK + Patient list, sorted CreatedAt DESC                  |
| P1-PAT-06  | Handle Duplicate PhoneNumber gracefully                | Catch DB unique violation → return 409 + user-friendly message |
| P1-PAT-07  | **Test:** CreatePatient ด้วย duplicate phone            | Unit test ผ่าน: ได้ Result.Failure + error message             |
| P1-PAT-08  | **Test:** ListPatients filter by TenantId only         | Unit test ผ่าน: ไม่มี cross-tenant data leak                   |

### P1-SEED: Data Seeder

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P1-SED-01  | Implement DataSeeder class                             | สร้าง 1 Tenant, 2 Branches, 3 Users (Admin/User/Viewer)     |
| P1-SED-02  | Hook Seeder to application startup                     | `dotnet run --seed` หรือ auto-seed on first run              |
| P1-SED-03  | Document seed credentials                              | README ระบุ username/password สำหรับแต่ละ role                |

### P1-FE: Frontend — Auth + Patient

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P1-FE-01   | Setup Tailwind CSS + Base Layout (Sidebar + Header)    | Layout render ถูกต้อง, responsive                             |
| P1-FE-02   | Implement Login Page + AuthProvider                    | Login สำเร็จ → store token → redirect to dashboard            |
| P1-FE-03   | Implement API Client (axios/fetch + JWT interceptor)   | ทุก request แนบ Authorization header                          |
| P1-FE-04   | Implement Patient List Page                            | แสดงรายชื่อผู้ป่วย, filter by branch, sorted by date          |
| P1-FE-05   | Implement Create Patient Page                          | Form validation, submit, handle duplicate error               |
| P1-FE-06   | Implement BranchSelector component                     | Dropdown แสดง branches ของ tenant, เลือกเพื่อ filter           |
| P1-FE-07   | Implement Role-Based UI (ซ่อน button สำหรับ Viewer)     | Viewer ไม่เห็น "New Patient" button                           |

---

## Phase 2: Appointment + Caching + Messaging

### P2-APT: Appointment Management

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P2-APT-01  | สร้าง Appointment Entity + Migration                   | Table + UNIQUE constraint สร้างสำเร็จ                         |
| P2-APT-02  | Implement CreateAppointmentCommand + Handler           | สร้าง Appointment, validate ไม่ซ้ำ, TenantId จาก context      |
| P2-APT-03  | สร้าง `POST /api/appointments` endpoint                | 201 Created, 409 เมื่อ duplicate booking                      |
| P2-APT-04  | Handle Duplicate Booking (DB constraint + friendly error) | Catch unique violation → 409 + clear message                |
| P2-APT-05  | **Test:** Concurrent duplicate booking                 | Integration test: 2 parallel requests → 1 success, 1 fail    |

### P2-MSG: RabbitMQ Messaging

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P2-MSG-01  | เพิ่ม RabbitMQ ใน docker-compose                       | RabbitMQ container healthy, management UI accessible          |
| P2-MSG-02  | Implement IEventPublisher + RabbitMqEventPublisher     | Publish message สำเร็จ, message ปรากฏใน RabbitMQ management   |
| P2-MSG-03  | Publish `appointment.created` event                    | Event payload มี TenantId, verify ใน RabbitMQ UI              |

### P2-CACHE: Redis Caching

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P2-CAC-01  | เพิ่ม Redis ใน docker-compose                          | Redis container healthy, redis-cli connect สำเร็จ             |
| P2-CAC-02  | Implement ICacheService + RedisCacheService            | Get/Set/Delete ทำงานถูกต้อง                                   |
| P2-CAC-03  | Cache List Patients endpoint                           | First call → DB, second call → cache (verify via Redis CLI)   |
| P2-CAC-04  | Implement Tenant-Scoped cache keys                     | Key format: `tenant:{id}:patients:list:{branch|all}`          |
| P2-CAC-05  | Implement Cache Invalidation on Create Patient         | After create → relevant cache keys deleted                    |
| P2-CAC-06  | Implement Cache Invalidation on Create Appointment     | After create → relevant cache keys deleted                    |

### P2-FE: Frontend — Appointment

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P2-FE-01   | สร้าง `features/appointments/` folder structure        | Components, hooks, services, types files สร้างครบ             |
| P2-FE-02   | Implement Appointment List Page                        | แสดงรายการนัดหมาย, filter by branch                          |
| P2-FE-03   | Implement Create Appointment Page                      | Patient search + Branch select + DateTime picker              |
| P2-FE-04   | Handle Duplicate Booking Error ใน UI                   | แสดง alert เมื่อ 409, ไม่ clear form                          |

---

## Phase 3: Production Readiness (Future)

### P3-ADV: Advanced Features

| ID         | Task                                                  | DoD                                                          |
| ---------- | ----------------------------------------------------- | ------------------------------------------------------------ |
| P3-ADV-01  | Implement Cursor-Based Pagination                      | `GET /api/patients?cursor=xxx&limit=20` ทำงานถูกต้อง         |
| P3-ADV-02  | สร้าง PatientVisit Entity + Migration                  | Track multi-branch visits                                    |
| P3-ADV-03  | Implement Full-Text Search (Patient)                   | ค้นหาด้วยชื่อ/เบอร์โทร, performant                            |
| P3-ADV-04  | Implement Audit Logging Middleware                     | Log ทุก mutation action พร้อม UserId, TenantId                |
| P3-ADV-05  | Implement Health Check endpoints                       | `/health` return OK + dependency status                       |
| P3-ADV-06  | Implement RabbitMQ Consumer (notification)             | Consume `appointment.created` → log/process                  |
| P3-ADV-07  | Setup CI/CD Pipeline (GitHub Actions)                  | Build → Test → Docker Push automated                         |
| P3-ADV-08  | Implement Per-Tenant Rate Limiting                     | 429 Too Many Requests เมื่อเกิน threshold                     |

---

## Summary Matrix

| Phase   | Total Tasks | Critical Path                              |
| ------- | ----------- | ------------------------------------------ |
| Phase 1 | 28 tasks    | INF → AUTH → PAT → SEED → FE              |
| Phase 2 | 15 tasks    | APT + MSG + CACHE (parallel) → FE          |
| Phase 3 | 8 tasks     | Independent features (parallel)            |
| **Total** | **51 tasks** |                                          |

### Phase 1 Critical Path Order

```
P1-INF-01 → P1-INF-02 → P1-INF-04 → P1-INF-05
                ↓
         P1-AUT-01 → P1-AUT-02 → P1-AUT-03 → P1-AUT-04 → P1-AUT-05
                                                    ↓
                                            P1-PAT-01 → P1-PAT-02 → P1-PAT-03
                                                    ↓
                                            P1-SED-01 → P1-SED-02
                                                    ↓
                                  P1-FE-01 → P1-FE-02 → P1-FE-04 → P1-FE-05
```
