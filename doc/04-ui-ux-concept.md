# UI/UX Concept

## Clinic POS Platform — v1

---

## 1. Design Philosophy

| Principle             | Application                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Clean & Minimal**   | White space เยอะ, ไม่ clutter, focus บน task ที่ต้องทำ          |
| **Task-Oriented**     | แต่ละหน้ามีจุดประสงค์ชัดเจน 1 อย่าง                             |
| **Fast Interaction**  | Form สั้น, submit ง่าย, feedback ทันที                         |
| **Accessible**        | Keyboard navigation, ARIA labels, color contrast AA          |
| **Responsive**        | ใช้งานได้ทั้ง Desktop (หลัก) และ Tablet                         |

### Visual Direction

- **Color Palette:** Neutral base (white/gray) + Primary accent (blue/teal สำหรับ medical context)
- **Typography:** Inter / Noto Sans Thai — clean, readable, รองรับภาษาไทย
- **Spacing:** 8px grid system, consistent padding
- **Component Library:** Tailwind CSS + Headless UI / Shadcn UI

---

## 2. Page Layout Structure

```
┌─────────────────────────────────────────────────────┐
│  Header                                [User] [▼]  │
│  ┌──────┐  ┌────────────────────────────────────┐   │
│  │      │  │                                    │   │
│  │ Side │  │         Main Content Area          │   │
│  │ bar  │  │                                    │   │
│  │      │  │                                    │   │
│  │ Nav  │  │                                    │   │
│  │      │  │                                    │   │
│  └──────┘  └────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

### Sidebar Navigation

| Menu Item       | Icon  | Route              | Permission     |
| --------------- | ----- | ------------------ | -------------- |
| Dashboard       | Home  | `/`                | All            |
| Patients        | Users | `/patients`        | All            |
| Appointments    | Calendar | `/appointments` | All            |
| User Management | Shield | `/users`          | Admin only     |

---

## 3. Key Pages & Components

### 3.1 Login Page (`/login`)

```
┌──────────────────────────────────┐
│                                  │
│        🏥 Clinic POS             │
│                                  │
│   ┌──────────────────────────┐   │
│   │  Username                │   │
│   └──────────────────────────┘   │
│   ┌──────────────────────────┐   │
│   │  Password                │   │
│   └──────────────────────────┘   │
│                                  │
│   [ ========= Login ========= ]  │
│                                  │
│   Tenant: Shown after login      │
│                                  │
└──────────────────────────────────┘
```

**Key Components:**
| Component      | Description                                      |
| -------------- | ------------------------------------------------ |
| LoginForm      | Username + Password fields, submit button         |
| ErrorAlert     | แสดง error message เมื่อ login ไม่สำเร็จ            |
| BrandingHeader | Logo + ชื่อระบบ                                    |

---

### 3.2 Patient List Page (`/patients`)

```
┌─────────────────────────────────────────────────────┐
│  Patients                          [ + New Patient ] │
│                                                      │
│  Branch: [ All Branches  ▼ ]                         │
│                                                      │
│  ┌──────────────────────────────────────────────┐    │
│  │  Name          │ Phone      │ Branch  │ Date │    │
│  ├──────────────────────────────────────────────┤    │
│  │  สมชาย ใจดี     │ 081-xxx    │ สาขา 1  │ 17/2 │    │
│  │  สมหญิง รักดี   │ 082-xxx    │ สาขา 2  │ 16/2 │    │
│  │  วิชัย มั่นคง    │ 083-xxx    │ สาขา 1  │ 15/2 │    │
│  └──────────────────────────────────────────────┘    │
│                                                      │
│  Showing 3 of 3 patients                             │
└─────────────────────────────────────────────────────┘
```

**Key Components:**

| Component        | Description                                           |
| ---------------- | ----------------------------------------------------- |
| PageHeader       | Title + "New Patient" button (hidden for Viewer)       |
| BranchFilter     | Dropdown เลือก Branch (optional filter)                |
| PatientTable     | Sortable table, แสดง Name, Phone, Branch, CreatedAt    |
| EmptyState       | แสดงเมื่อไม่มี patient ("No patients found")            |
| LoadingSpinner   | Skeleton loader ระหว่าง fetch data                      |

**UX Details:**
- TenantId ถูกส่งอัตโนมัติจาก Auth Context (User ไม่ต้องเลือก)
- Branch filter จำค่าที่เลือกไว้ใน URL query param
- Table เรียงตาม CreatedAt DESC (ล่าสุดอยู่บน)
- "New Patient" button ซ่อนสำหรับ Viewer role

---

### 3.3 Create Patient Page (`/patients/new`)

```
┌─────────────────────────────────────────────────────┐
│  ← Back to Patients                                  │
│                                                      │
│  New Patient                                         │
│                                                      │
│  ┌─────────────────────────────────────────────┐     │
│  │  First Name *    [                        ] │     │
│  │  Last Name *     [                        ] │     │
│  │  Phone Number *  [                        ] │     │
│  │  Primary Branch  [ Select Branch  ▼       ] │     │
│  │                                             │     │
│  │        [ Cancel ]  [ Save Patient ]         │     │
│  └─────────────────────────────────────────────┘     │
│                                                      │
│  ⚠ Phone number already exists in this clinic        │
│     (duplicate error — inline alert)                 │
└─────────────────────────────────────────────────────┘
```

**Key Components:**

| Component        | Description                                            |
| ---------------- | ------------------------------------------------------ |
| BackLink         | กลับไปหน้า Patient List                                 |
| PatientForm      | Controlled form: FirstName, LastName, PhoneNumber       |
| BranchSelect     | Dropdown เลือก Primary Branch (optional)                |
| SubmitButton     | Disabled ระหว่าง submitting, แสดง loading state          |
| DuplicateAlert   | Alert สีแดง เมื่อ API return 409 (phone duplicate)       |
| ValidationErrors | Inline error ใต้แต่ละ field เมื่อ validation ไม่ผ่าน      |

**UX Details:**
- Client-side validation ก่อน submit (required fields)
- เมื่อ submit สำเร็จ → redirect ไปหน้า Patient List + toast "Patient created"
- เมื่อ phone ซ้ำ → แสดง inline alert ไม่ clear form (ให้ user แก้เบอร์)

---

### 3.4 Create Appointment Page (`/appointments/new`) — Phase 2

```
┌─────────────────────────────────────────────────────┐
│  ← Back to Appointments                             │
│                                                      │
│  New Appointment                                     │
│                                                      │
│  ┌─────────────────────────────────────────────┐     │
│  │  Patient *       [ Search patient...  ▼   ] │     │
│  │  Branch *        [ Select Branch  ▼       ] │     │
│  │  Date & Time *   [ 2026-02-18  09:00      ] │     │
│  │                                             │     │
│  │       [ Cancel ]  [ Book Appointment ]      │     │
│  └─────────────────────────────────────────────┘     │
│                                                      │
│  ⚠ This patient already has an appointment           │
│     at this time and branch                          │
└─────────────────────────────────────────────────────┘
```

**Key Components:**

| Component           | Description                                       |
| ------------------- | ------------------------------------------------- |
| PatientSearchSelect | Searchable dropdown เลือก patient จาก list         |
| BranchSelect        | Dropdown เลือก Branch                              |
| DateTimePicker      | Date + Time picker component                       |
| DuplicateAlert      | Alert เมื่อ duplicate booking                       |

---

## 4. Responsive Behavior

| Breakpoint     | Behavior                                         |
| -------------- | ------------------------------------------------ |
| Desktop (>1024px) | Sidebar แสดงตลอด, Table full-width             |
| Tablet (768-1024px) | Sidebar collapsible, Table ปรับ column        |
| Mobile (<768px)   | Sidebar เป็น hamburger menu, Table → Card view  |

---

## 5. Accessibility Checklist

| Item                     | Implementation                          |
| ------------------------ | --------------------------------------- |
| Keyboard Navigation      | Tab order ถูกต้อง, focus visible          |
| Screen Reader            | ARIA labels บน form fields + buttons     |
| Color Contrast           | WCAG AA (4.5:1 text, 3:1 large text)    |
| Error Identification     | Error messages linked to fields via aria-describedby |
| Loading States           | aria-busy + skeleton placeholders        |

---

## 6. Feedback & States

| State          | Visual Treatment                                  |
| -------------- | ------------------------------------------------- |
| Loading        | Skeleton placeholder (ไม่ใช้ spinner ทั้งหน้า)      |
| Empty          | Illustration + "No data" message + CTA            |
| Success        | Toast notification (สีเขียว, auto-dismiss 3s)      |
| Error          | Inline alert (สีแดง, ไม่ auto-dismiss)             |
| Unauthorized   | Redirect to login + toast "Session expired"        |
| Forbidden      | แสดง 403 message + ซ่อน action button               |
