# API Curl Examples

> Base URL: `http://localhost:5001`
>
> All endpoints (except `/api/auth/login` and `/health`) require a `Bearer` token.

---

## 1. Health Check

```bash
curl -s http://localhost:5001/health | python3 -m json.tool
```

**Response:**

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "postgresql", "status": "Healthy" },
    { "name": "redis",      "status": "Healthy" },
    { "name": "rabbitmq",   "status": "Healthy" }
  ]
}
```

---

## 2. Authentication

### Login (get JWT token)

```bash
curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin01","password":"P@ssw0rd"}'
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsIn...",
  "expiresAt": "2026-02-18T02:37:18Z",
  "user": {
    "id": "c0000000-0000-0000-0000-000000000001",
    "username": "admin01",
    "role": "Admin",
    "tenantId": "a0000000-0000-0000-0000-000000000001",
    "branches": [
      { "id": "b0000000-0000-0000-0000-000000000001", "name": "สาขาสยาม" },
      { "id": "b0000000-0000-0000-0000-000000000002", "name": "สาขาทองหล่อ" }
    ]
  }
}
```

### Save token to variable (for subsequent requests)

```bash
TOKEN=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin01","password":"P@ssw0rd"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")
```

---

## 3. Branches

### List branches

```bash
curl -s http://localhost:5001/api/branches \
  -H "Authorization: Bearer $TOKEN"
```

---

## 4. Patients

### List patients (with pagination)

```bash
curl -s "http://localhost:5001/api/patients?limit=5" \
  -H "Authorization: Bearer $TOKEN"
```

### List patients — filter by branch

```bash
curl -s "http://localhost:5001/api/patients?branchId=b0000000-0000-0000-0000-000000000001" \
  -H "Authorization: Bearer $TOKEN"
```

### List patients — search

```bash
curl -s "http://localhost:5001/api/patients?search=สมชาย" \
  -H "Authorization: Bearer $TOKEN"
```

### List patients — cursor pagination (use nextCursor from previous response)

```bash
curl -s "http://localhost:5001/api/patients?cursor=NEXT_CURSOR_VALUE&limit=5" \
  -H "Authorization: Bearer $TOKEN"
```

### Create patient

```bash
curl -s -X POST http://localhost:5001/api/patients \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "firstName": "ทดสอบ",
    "lastName": "ระบบ",
    "phoneNumber": "099-999-9999",
    "primaryBranchId": "b0000000-0000-0000-0000-000000000001"
  }'
```

---

## 5. Appointments

### List appointments

```bash
curl -s http://localhost:5001/api/appointments \
  -H "Authorization: Bearer $TOKEN"
```

### List appointments — filter by branch and date

```bash
curl -s "http://localhost:5001/api/appointments?branchId=b0000000-0000-0000-0000-000000000001&date=2026-02-20" \
  -H "Authorization: Bearer $TOKEN"
```

### Create appointment

```bash
curl -s -X POST http://localhost:5001/api/appointments \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "patientId": "d0000000-0000-0000-0000-000000000001",
    "branchId": "b0000000-0000-0000-0000-000000000001",
    "startAt": "2026-03-01T10:00:00Z"
  }'
```

---

## 6. Patient Visits

### Get visit history for a patient

```bash
curl -s http://localhost:5001/api/patients/d0000000-0000-0000-0000-000000000001/visits \
  -H "Authorization: Bearer $TOKEN"
```

### Record a new visit

```bash
curl -s -X POST http://localhost:5001/api/patients/d0000000-0000-0000-0000-000000000001/visits \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "branchId": "b0000000-0000-0000-0000-000000000001",
    "visitedAt": "2026-02-17T09:30:00Z",
    "notes": "ตรวจร่างกายประจำปี ผลปกติ"
  }'
```

---

## 7. User Management (Admin only)

### Create user

```bash
curl -s -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "username": "doctor01",
    "password": "P@ssw0rd",
    "role": "User"
  }'
```

### Assign role

```bash
curl -s -X PUT http://localhost:5001/api/users/USER_ID/role \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"role": "Admin"}'
```

### Assign branches

```bash
curl -s -X PUT http://localhost:5001/api/users/USER_ID/branches \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "branchIds": [
      "b0000000-0000-0000-0000-000000000001",
      "b0000000-0000-0000-0000-000000000002"
    ]
  }'
```

---

## 8. Audit Logs (Admin only)

### List audit logs

```bash
curl -s http://localhost:5001/api/audit-logs \
  -H "Authorization: Bearer $TOKEN"
```

### List audit logs — filter by entity type

```bash
curl -s "http://localhost:5001/api/audit-logs?entityType=patients&limit=10" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Error Response Format (RFC 7807)

All error responses follow the Problem Details format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Validation Failed",
  "status": 400,
  "detail": "First name is required."
}
```

| Status Code | Meaning                              |
|-------------|--------------------------------------|
| 400         | Validation error                     |
| 401         | Invalid credentials / missing token  |
| 403         | Insufficient permissions             |
| 409         | Duplicate resource (phone, appointment) |
| 429         | Rate limit exceeded                  |
