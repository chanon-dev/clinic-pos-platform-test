# Environment Variables

> See `.env.example` in the project root for a ready-to-use template.

---

## Backend API (ASP.NET)

| Variable | Default | Description |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Runtime environment (`Development`, `Production`) |
| `ConnectionStrings__DefaultConnection` | `Host=db;Port=5432;Database=clinicpos;Username=postgres;Password=postgres` | PostgreSQL connection string |
| `Redis__ConnectionString` | `redis:6379` | Redis host and port |
| `RabbitMQ__Host` | `rabbitmq` | RabbitMQ hostname |
| `Jwt__Key` | `ClinicPOS-SuperSecret-Key-2026-Min32Chars!!` | JWT signing key (min 32 chars) |
| `Jwt__Issuer` | `ClinicPOS` | JWT issuer claim |
| `Jwt__Audience` | `ClinicPOS` | JWT audience claim |

## Frontend (Next.js)

| Variable | Default | Description |
|---|---|---|
| `NEXT_PUBLIC_API_URL` | `http://localhost:5001` | Backend API base URL (⚠️ build-time variable, must be set during `npm run build`) |

## PostgreSQL

| Variable | Default | Description |
|---|---|---|
| `POSTGRES_DB` | `clinicpos` | Database name |
| `POSTGRES_USER` | `postgres` | Database username |
| `POSTGRES_PASSWORD` | `postgres` | Database password |

## RabbitMQ

| Variable | Default | Description |
|---|---|---|
| `RABBITMQ_DEFAULT_USER` | `guest` | Management UI username |
| `RABBITMQ_DEFAULT_PASS` | `guest` | Management UI password |

---

## Docker Compose Port Mapping

| Service | Container Port | Host Port | URL |
|---|---|---|---|
| Frontend | 3000 | **3000** | <http://localhost:3000> |
| API | 5000 | **5001** | <http://localhost:5001> |
| PostgreSQL | 5432 | **5432** | localhost:5432 |
| Redis | 6379 | **6379** | localhost:6379 |
| RabbitMQ AMQP | 5672 | **5672** | localhost:5672 |
| RabbitMQ Management | 15672 | **15672** | <http://localhost:15672> |

---

## ⚠️ Important Notes

1. **`NEXT_PUBLIC_API_URL`** is a **build-time** variable in Next.js. It is inlined during `npm run build`. Changing it at runtime has no effect. The `Dockerfile` sets a default `ARG` for this.

2. **JWT Key** must be at least 32 characters long. In production, use a strong random key and store it securely (e.g., via secrets manager).

3. **PostgreSQL password** should be changed in production. Update both `POSTGRES_PASSWORD` and `ConnectionStrings__DefaultConnection` accordingly.
