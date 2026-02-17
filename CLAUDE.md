# CLAUDE.md - AI Development Guide

## 📍 Core Context & Requirements

- **Primary Source:** Read requirements from `/Users/chanon/Desktop/test-interview/requiment.txt`
- **Role:** You are a Team of Experts (Solution Architect, Lead Dev, Senior Software Engineer).
- **Goal:** Transform the .txt requirement into a production-ready "Thin Vertical Slice" within a 90-minute timebox.

## 🏗️ Architecture Standards

- **Backend (.NET 10):** - Pattern: **Clean Architecture** (Separation of Concerns).
  - Data Safety: Implement **Global Query Filters** for `TenantId` isolation in EF Core.
  - Features: REST API, FluentValidation, Auto-migrations.
- **Frontend (Next.js):** - Pattern: **Feature-based Architecture** (e.g., `src/features/patients/`).
  - UI: Modern & User-friendly (Tailwind CSS / Shadcn UI).
- **Infrastructure:** Docker-compose (PostgreSQL, Redis, RabbitMQ).

## 🛡️ The "Tenant-Safe" Laws

1. **Isolation:** Every database operation must be scoped to a `TenantId`.
2. **Uniqueness:** Phone numbers must be unique *within* the same Tenant, not globally.
3. **Roles:** Enforce RBAC (Admin, User, Viewer) strictly on the server-side.

## 📝 Document Handling

- **AI_PROMPTS.md:** **[MANDATORY]** Store every prompt I ask and the evolution of the code here.
- **README.md:** Store architecture summary, one-command run instructions, API examples, and seeded user credentials.
- **doc/:** Save PRD, Enterprise technical docs, and phased implementation tasks here.

## 🛠️ Mandatory Execution (One Command)

- The entire stack must be runnable via: `docker-compose up --build`
- Database migrations and **Seeder** (1 Tenant, 2 Branches, 3 Roles) must run automatically on startup.

## 🚨 Critical Instructions for AI

- Read `/Users/chanon/Desktop/test-interview/requiment.txt` before generating any code.
- Prioritize **Section A & B** (Core Slice & Auth).
- Maintain high code quality (SOLID, DRY) despite the time constraint.
- Log every iteration and "Why" behind technical choices in `AI_PROMPTS.md`.
