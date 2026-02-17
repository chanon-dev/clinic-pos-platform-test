# Product Requirement Document (PRD)

## Clinic POS Platform — v1

| Field          | Value                                      |
| -------------- | ------------------------------------------ |
| Product        | Clinic POS Platform                        |
| Version        | 1.0 (MVP)                                  |
| Stack          | .NET 10 / Next.js / PostgreSQL / Redis / RabbitMQ |
| Architecture   | Multi-Tenant, Multi-Branch B2B SaaS        |
| Last Updated   | 2026-02-17                                 |

---

## 1. Executive Summary

Clinic POS Platform เป็นระบบ **Point-of-Service** สำหรับคลินิกที่ออกแบบในรูปแบบ **Multi-Tenant, Multi-Branch B2B SaaS** โดยเน้น:

- **Tenant Isolation** — ข้อมูลผู้ป่วยแต่ละ Tenant ถูกแยกอย่างสมบูรณ์ ไม่มี Cross-Tenant Exposure
- **Branch-Aware** — 1 Tenant มีได้หลาย Branch; ผู้ป่วยสังกัด 1 Tenant แต่สามารถเข้ารับบริการได้หลาย Branch
- **Thin Vertical Slice** — MVP มุ่งเน้น Patient Management + Appointment + Role-Based Access เป็นหลัก
- **Thailand-First, Global-Ready** — รองรับบริบทการใช้งานในไทยเป็นหลัก พร้อมขยายสู่ตลาดสากล

ระบบนี้ให้ความสำคัญกับ **Correctness under Concurrency**, **Tenant Safety**, และ **Developer Experience** (รันได้ด้วยคำสั่งเดียว)

---

## 2. User Personas

### Persona 1: Clinic Admin (ผู้ดูแลระบบคลินิก)

| Attribute      | Detail                                              |
| -------------- | --------------------------------------------------- |
| Role           | Admin                                               |
| Goal           | จัดการผู้ป่วย, ผู้ใช้, นัดหมาย และดูแลข้อมูลทุก Branch |
| Pain Points    | ระบบเดิมไม่รองรับหลาย Branch ต้องใช้ Excel จัดการ    |
| Key Actions    | สร้าง/แก้ไขผู้ป่วย, กำหนดสิทธิ์ผู้ใช้, ดูข้อมูลทุก Branch |

### Persona 2: Staff / User (พนักงานคลินิก)

| Attribute      | Detail                                              |
| -------------- | --------------------------------------------------- |
| Role           | User                                                |
| Goal           | ลงทะเบียนผู้ป่วยใหม่ และจัดการนัดหมาย                 |
| Pain Points    | ต้องเช็คเบอร์ซ้ำ manual, นัดหมายซ้อนกันบ่อย           |
| Key Actions    | สร้างผู้ป่วย, สร้างนัดหมาย, ค้นหาผู้ป่วย              |

### Persona 3: Viewer (ผู้ดูข้อมูลอย่างเดียว)

| Attribute      | Detail                                              |
| -------------- | --------------------------------------------------- |
| Role           | Viewer                                              |
| Goal           | ดูรายชื่อผู้ป่วยและนัดหมายเพื่อเตรียมการ               |
| Pain Points    | ต้องร้องขอข้อมูลจากพนักงานทุกครั้ง                     |
| Key Actions    | ดูรายชื่อผู้ป่วย, ดูตารางนัดหมาย (Read-Only)          |

---

## 3. Functional Requirements

### FR-01: Patient Management

| ID       | Requirement                                                     | Priority |
| -------- | --------------------------------------------------------------- | -------- |
| FR-01.1  | สร้างผู้ป่วยใหม่ (FirstName, LastName, PhoneNumber, TenantId)     | Must     |
| FR-01.2  | PhoneNumber unique ภายใน Tenant เดียวกัน (ไม่ใช่ Global)          | Must     |
| FR-01.3  | แสดงรายชื่อผู้ป่วย filter ด้วย TenantId (บังคับ)                  | Must     |
| FR-01.4  | Filter เพิ่มเติมด้วย BranchId (optional)                         | Should   |
| FR-01.5  | เรียงลำดับด้วย CreatedAt DESC                                    | Must     |
| FR-01.6  | รองรับ PrimaryBranchId (optional field)                          | Should   |
| FR-01.7  | Return safe error message เมื่อ PhoneNumber ซ้ำภายใน Tenant       | Must     |

### FR-02: Appointment Management

| ID       | Requirement                                                     | Priority |
| -------- | --------------------------------------------------------------- | -------- |
| FR-02.1  | สร้างนัดหมาย (TenantId, BranchId, PatientId, StartAt)            | Must     |
| FR-02.2  | ป้องกัน Duplicate Booking (PatientId + StartAt + BranchId + Tenant) | Must   |
| FR-02.3  | ใช้ DB Unique Constraint เพื่อ Concurrency Safety                 | Must     |
| FR-02.4  | Publish Event ไปยัง RabbitMQ เมื่อสร้างนัดหมายสำเร็จ               | Must     |

### FR-03: User & Authorization

| ID       | Requirement                                                     | Priority |
| -------- | --------------------------------------------------------------- | -------- |
| FR-03.1  | สร้าง User, กำหนด Role (Admin / User / Viewer)                   | Must     |
| FR-03.2  | Associate User กับ Tenant และ Branch(es)                         | Must     |
| FR-03.3  | Enforce permissions server-side (policy/middleware)               | Must     |
| FR-03.4  | Viewer ห้ามสร้างผู้ป่วย                                           | Must     |
| FR-03.5  | ระบบ Authentication (JWT / Cookie / Simple Token)                 | Must     |

### FR-04: Caching

| ID       | Requirement                                                     | Priority |
| -------- | --------------------------------------------------------------- | -------- |
| FR-04.1  | Cache read path อย่างน้อย 1 endpoint (e.g., List Patients)       | Must     |
| FR-04.2  | Cache key ต้อง scoped ด้วย TenantId                              | Must     |
| FR-04.3  | Invalidate cache เมื่อมีการเปลี่ยนแปลงข้อมูล                      | Must     |

### FR-05: Seeder

| ID       | Requirement                                                     | Priority |
| -------- | --------------------------------------------------------------- | -------- |
| FR-05.1  | Seed 1 Tenant, 2 Branches                                       | Must     |
| FR-05.2  | Seed Users สำหรับแต่ละ Role (Admin, User, Viewer)                 | Must     |
| FR-05.3  | Seed Tenant/Branch associations ให้ถูกต้อง                       | Must     |
| FR-05.4  | รันได้ด้วยคำสั่งเดียว                                             | Must     |

---

## 4. Non-Functional Requirements

### Performance

| ID       | Requirement                                   | Target            |
| -------- | --------------------------------------------- | ----------------- |
| NFR-01   | API Response Time (p95)                        | < 200ms           |
| NFR-02   | Cached Read Response Time (p95)                | < 50ms            |
| NFR-03   | Concurrent Create Patient (ไม่ Duplicate)       | Safe under 100 RPS |

### Security

| ID       | Requirement                                   | Standard          |
| -------- | --------------------------------------------- | ----------------- |
| NFR-04   | Tenant Data Isolation                          | Zero Cross-Tenant Leak |
| NFR-05   | Server-Side Authorization Enforcement          | OWASP RBAC        |
| NFR-06   | Input Validation (Request)                     | OWASP Input Validation |
| NFR-07   | Consistent Error Response (ไม่ Leak Internals)  | RFC 7807 Problem Details |

### Reliability & DevOps

| ID       | Requirement                                   | Target            |
| -------- | --------------------------------------------- | ----------------- |
| NFR-08   | รันระบบทั้งหมดด้วย 1 command (docker compose)    | Mandatory         |
| NFR-09   | Database Migration อัตโนมัติ                     | On Startup        |
| NFR-10   | Automated Test Suite (Minimal but Meaningful)   | > 0 passing tests |

---

## 5. Out of Scope (v1)

- Payment / Billing
- Advanced scheduling (time slots, doctor assignment)
- Patient medical records
- Notification system (SMS, Email)
- Multi-language UI
- Enterprise-grade Tenant enforcement (Row-Level Security)
- Audit logging
