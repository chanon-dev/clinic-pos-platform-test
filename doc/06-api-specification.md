# API Specification

## Clinic POS Platform — v1

---

## Convention

| Item               | Standard                                            |
| ------------------ | --------------------------------------------------- |
| Base URL           | `http://localhost:5000/api`                          |
| Format             | JSON (`application/json`)                            |
| Auth               | Bearer JWT in `Authorization` header                 |
| Tenant Isolation   | TenantId extracted from JWT claim — **ไม่ส่งผ่าน body** |
| Error Format       | RFC 7807 Problem Details                             |
| ID Format          | UUID v4                                              |
| DateTime           | ISO 8601 with timezone (`2026-02-17T09:00:00Z`)     |
| Naming             | camelCase (JSON) / snake_case (DB)                   |

### Standard Error Response (RFC 7807)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Conflict",
  "status": 409,
  "detail": "A patient with this phone number already exists in this tenant.",
  "traceId": "00-abc123..."
}
```

### Standard Validation Error (400)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "firstName": ["First name is required."],
    "phoneNumber": ["Phone number format is invalid."]
  }
}
```

---

## 1. Authentication

### 1.1 Login

```
POST /api/auth/login
```

**Access:** Public (No Auth)

**Request Body:**

```json
{
  "username": "admin01",
  "password": "P@ssw0rd"
}
```

| Field      | Type   | Required | Validation           |
| ---------- | ------ | -------- | -------------------- |
| username   | string | Yes      | 3-100 chars          |
| password   | string | Yes      | min 6 chars          |

**Response 200 OK:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2026-02-17T22:00:00Z",
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "username": "admin01",
    "role": "Admin",
    "tenantId": "550e8400-e29b-41d4-a716-446655440000",
    "branches": [
      {
        "id": "660e8400-e29b-41d4-a716-446655440001",
        "name": "สาขาสยาม"
      },
      {
        "id": "660e8400-e29b-41d4-a716-446655440002",
        "name": "สาขาทองหล่อ"
      }
    ]
  }
}
```

**JWT Claims:**

```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440001",
  "tenantId": "550e8400-e29b-41d4-a716-446655440000",
  "role": "Admin",
  "branchIds": ["660e8400-...-001", "660e8400-...-002"],
  "exp": 1740009600
}
```

**Error Responses:**

| Status | Condition              |
| ------ | ---------------------- |
| 400    | Missing fields         |
| 401    | Invalid credentials    |

---

## 2. Users

### 2.1 Create User

```
POST /api/users
```

**Access:** `Admin` only

**Request Body:**

```json
{
  "username": "staff01",
  "password": "P@ssw0rd",
  "role": "User",
  "branchIds": [
    "660e8400-e29b-41d4-a716-446655440001"
  ]
}
```

| Field      | Type     | Required | Validation                       |
| ---------- | -------- | -------- | -------------------------------- |
| username   | string   | Yes      | 3-100 chars, unique              |
| password   | string   | Yes      | min 6 chars                      |
| role       | string   | Yes      | enum: `Admin`, `User`, `Viewer`  |
| branchIds  | string[] | No       | Valid branch UUIDs within tenant  |

> **Note:** `tenantId` จะถูกดึงจาก JWT ของ Admin ที่สร้าง — ไม่ต้องส่งใน body

**Response 201 Created:**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440010",
  "username": "staff01",
  "role": "User",
  "tenantId": "550e8400-e29b-41d4-a716-446655440000",
  "branches": [
    {
      "id": "660e8400-e29b-41d4-a716-446655440001",
      "name": "สาขาสยาม"
    }
  ],
  "createdAt": "2026-02-17T10:30:00Z"
}
```

**Error Responses:**

| Status | Condition                         |
| ------ | --------------------------------- |
| 400    | Validation error                  |
| 401    | Not authenticated                 |
| 403    | Not Admin role                    |
| 409    | Username already exists           |

---

### 2.2 Assign Role

```
PUT /api/users/{userId}/role
```

**Access:** `Admin` only

**Request Body:**

```json
{
  "role": "Viewer"
}
```

| Field | Type   | Required | Validation                      |
| ----- | ------ | -------- | ------------------------------- |
| role  | string | Yes      | enum: `Admin`, `User`, `Viewer` |

**Response 200 OK:**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440010",
  "username": "staff01",
  "role": "Viewer",
  "tenantId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Error Responses:**

| Status | Condition                              |
| ------ | -------------------------------------- |
| 400    | Invalid role value                     |
| 403    | Not Admin                              |
| 404    | User not found (within same tenant)    |

---

### 2.3 Associate User ↔ Branches

```
PUT /api/users/{userId}/branches
```

**Access:** `Admin` only

**Request Body:**

```json
{
  "branchIds": [
    "660e8400-e29b-41d4-a716-446655440001",
    "660e8400-e29b-41d4-a716-446655440002"
  ]
}
```

> **Behavior:** Replace ทั้งหมด (set-based) — ส่ง branchIds ใหม่ทั้ง array

| Field     | Type     | Required | Validation                        |
| --------- | -------- | -------- | --------------------------------- |
| branchIds | string[] | Yes      | Non-empty, valid UUIDs in tenant  |

**Response 200 OK:**

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440010",
  "username": "staff01",
  "branches": [
    { "id": "660e8400-...-001", "name": "สาขาสยาม" },
    { "id": "660e8400-...-002", "name": "สาขาทองหล่อ" }
  ]
}
```

---

## 3. Patients

### 3.1 Create Patient

```
POST /api/patients
```

**Access:** `Admin`, `User` (Viewer → 403)

**Request Body:**

```json
{
  "firstName": "สมชาย",
  "lastName": "ใจดี",
  "phoneNumber": "0812345678",
  "primaryBranchId": "660e8400-e29b-41d4-a716-446655440001"
}
```

| Field           | Type   | Required | Validation                               |
| --------------- | ------ | -------- | ---------------------------------------- |
| firstName       | string | Yes      | 1-100 chars                              |
| lastName        | string | Yes      | 1-100 chars                              |
| phoneNumber     | string | Yes      | Thai format: 0x-xxxx-xxxx (10 digits)    |
| primaryBranchId | string | No       | Valid branch UUID within the same tenant  |

> **TenantId** ถูกดึงจาก JWT claim อัตโนมัติ — Frontend ไม่ต้องส่ง

**Response 201 Created:**

```json
{
  "id": "770e8400-e29b-41d4-a716-446655440001",
  "firstName": "สมชาย",
  "lastName": "ใจดี",
  "phoneNumber": "0812345678",
  "tenantId": "550e8400-e29b-41d4-a716-446655440000",
  "primaryBranch": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "name": "สาขาสยาม"
  },
  "createdAt": "2026-02-17T10:30:00Z"
}
```

**Response 409 Conflict (Duplicate Phone):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Conflict",
  "status": 409,
  "detail": "A patient with phone number '0812345678' already exists in this clinic."
}
```

**Error Responses:**

| Status | Condition                                  |
| ------ | ------------------------------------------ |
| 400    | Validation error (missing/invalid fields)  |
| 401    | Not authenticated                          |
| 403    | Viewer role                                |
| 409    | PhoneNumber duplicate within same Tenant   |

**Concurrency Note:**

```
Duplicate detection ใช้ DB UNIQUE constraint (tenant_id, phone_number)
→ Safe under concurrent requests
→ Catch DbUpdateException → map to 409
```

---

### 3.2 List Patients

```
GET /api/patients?branchId={branchId}
```

**Access:** `Admin`, `User`, `Viewer`

**Query Parameters:**

| Parameter | Type   | Required | Description                             |
| --------- | ------ | -------- | --------------------------------------- |
| branchId  | string | No       | Filter by primary branch UUID           |

> **TenantId** filter ถูก apply อัตโนมัติจาก Global Query Filter — ไม่ต้องส่งเป็น query param

**Response 200 OK:**

```json
{
  "data": [
    {
      "id": "770e8400-e29b-41d4-a716-446655440001",
      "firstName": "สมชาย",
      "lastName": "ใจดี",
      "phoneNumber": "0812345678",
      "primaryBranch": {
        "id": "660e8400-e29b-41d4-a716-446655440001",
        "name": "สาขาสยาม"
      },
      "createdAt": "2026-02-17T10:30:00Z"
    },
    {
      "id": "770e8400-e29b-41d4-a716-446655440002",
      "firstName": "สมหญิง",
      "lastName": "รักดี",
      "phoneNumber": "0898765432",
      "primaryBranch": {
        "id": "660e8400-e29b-41d4-a716-446655440002",
        "name": "สาขาทองหล่อ"
      },
      "createdAt": "2026-02-16T14:00:00Z"
    }
  ],
  "total": 2
}
```

**Sorting:** `CreatedAt DESC` (fixed — newest first)

**Caching (Phase 2):**

```
Cache Key:  tenant:{tenantId}:patients:list:{branchId|all}
TTL:        5 minutes
Invalidate: On POST /api/patients
```

---

## 4. Appointments (Phase 2)

### 4.1 Create Appointment

```
POST /api/appointments
```

**Access:** `Admin`, `User` (Viewer → 403)

**Request Body:**

```json
{
  "patientId": "770e8400-e29b-41d4-a716-446655440001",
  "branchId": "660e8400-e29b-41d4-a716-446655440001",
  "startAt": "2026-02-18T09:00:00Z"
}
```

| Field     | Type   | Required | Validation                               |
| --------- | ------ | -------- | ---------------------------------------- |
| patientId | string | Yes      | Valid patient UUID within same tenant     |
| branchId  | string | Yes      | Valid branch UUID within same tenant      |
| startAt   | string | Yes      | ISO 8601, must be in the future           |

**Response 201 Created:**

```json
{
  "id": "880e8400-e29b-41d4-a716-446655440001",
  "tenantId": "550e8400-e29b-41d4-a716-446655440000",
  "patient": {
    "id": "770e8400-e29b-41d4-a716-446655440001",
    "firstName": "สมชาย",
    "lastName": "ใจดี"
  },
  "branch": {
    "id": "660e8400-e29b-41d4-a716-446655440001",
    "name": "สาขาสยาม"
  },
  "startAt": "2026-02-18T09:00:00Z",
  "createdAt": "2026-02-17T10:45:00Z"
}
```

**Response 409 Conflict (Duplicate Booking):**

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Conflict",
  "status": 409,
  "detail": "This patient already has an appointment at this branch and time."
}
```

**Error Responses:**

| Status | Condition                                            |
| ------ | ---------------------------------------------------- |
| 400    | Validation error / startAt in the past               |
| 401    | Not authenticated                                    |
| 403    | Viewer role                                          |
| 404    | Patient or Branch not found (within tenant)          |
| 409    | Duplicate booking (same patient + branch + startAt)  |

**Side Effects:**

```
✅ Publish to RabbitMQ:
   Exchange:    clinic-pos.events
   Routing Key: appointment.created
   Payload:     { appointmentId, tenantId, branchId, patientId, startAt }

✅ Cache Invalidation:
   DELETE tenant:{tenantId}:appointments:*
```

---

### 4.2 List Appointments

```
GET /api/appointments?branchId={branchId}&date={date}
```

**Access:** `Admin`, `User`, `Viewer`

**Query Parameters:**

| Parameter | Type   | Required | Description                     |
| --------- | ------ | -------- | ------------------------------- |
| branchId  | string | No       | Filter by branch                |
| date      | string | No       | Filter by date (YYYY-MM-DD)     |

**Response 200 OK:**

```json
{
  "data": [
    {
      "id": "880e8400-e29b-41d4-a716-446655440001",
      "patient": {
        "id": "770e8400-...-001",
        "firstName": "สมชาย",
        "lastName": "ใจดี",
        "phoneNumber": "0812345678"
      },
      "branch": {
        "id": "660e8400-...-001",
        "name": "สาขาสยาม"
      },
      "startAt": "2026-02-18T09:00:00Z",
      "createdAt": "2026-02-17T10:45:00Z"
    }
  ],
  "total": 1
}
```

---

## 5. Branches (Read-Only)

### 5.1 List Branches

```
GET /api/branches
```

**Access:** `Admin`, `User`, `Viewer`

> Returns branches for the authenticated user's tenant only (auto-filtered)

**Response 200 OK:**

```json
{
  "data": [
    {
      "id": "660e8400-e29b-41d4-a716-446655440001",
      "name": "สาขาสยาม",
      "createdAt": "2026-02-01T00:00:00Z"
    },
    {
      "id": "660e8400-e29b-41d4-a716-446655440002",
      "name": "สาขาทองหล่อ",
      "createdAt": "2026-02-01T00:00:00Z"
    }
  ]
}
```

---

## 6. API Endpoint Summary

### Phase 1

| Method | Endpoint                        | Access            | Description          |
| ------ | ------------------------------- | ----------------- | -------------------- |
| POST   | `/api/auth/login`               | Public            | Login + get JWT      |
| POST   | `/api/users`                    | Admin             | Create user          |
| PUT    | `/api/users/{id}/role`          | Admin             | Assign role          |
| PUT    | `/api/users/{id}/branches`      | Admin             | Set branch access    |
| POST   | `/api/patients`                 | Admin, User       | Create patient       |
| GET    | `/api/patients`                 | Admin, User, Viewer | List patients      |
| GET    | `/api/branches`                 | Admin, User, Viewer | List branches      |

### Phase 2

| Method | Endpoint                        | Access            | Description          |
| ------ | ------------------------------- | ----------------- | -------------------- |
| POST   | `/api/appointments`             | Admin, User       | Create appointment   |
| GET    | `/api/appointments`             | Admin, User, Viewer | List appointments  |

**Total: 9 endpoints**

---

## 7. Permission Matrix

```
                    Admin    User     Viewer
                    ─────    ─────    ──────
POST /auth/login      ✅       ✅       ✅      (public)
POST /users           ✅       ❌       ❌
PUT  /users/role      ✅       ❌       ❌
PUT  /users/branches  ✅       ❌       ❌
POST /patients        ✅       ✅       ❌
GET  /patients        ✅       ✅       ✅
GET  /branches        ✅       ✅       ✅
POST /appointments    ✅       ✅       ❌
GET  /appointments    ✅       ✅       ✅
```
