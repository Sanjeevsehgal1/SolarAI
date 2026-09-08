# SolarShield AI

**Predictive Maintenance & Asset Life Maximization Platform**

Angular + .NET 9 Web API + PostgreSQL

## Project Structure

```
SolarShield AI/
├── backend/
│   ├── SolarShield.API/          # ASP.NET Core Web API
│   ├── SolarShield.Core/         # Entities, DTOs, Enums
│   └── SolarShield.Infrastructure/ # EF Core, DbContext, Migrations
├── frontend/                     # Angular 20 SPA (Technician + Dashboard)
└── SolarShield.sln
```

## Features (MVP)

- **Dashboard** — PR, soiling index, generation, fault counts
- **Fault Alerts** — AI-predicted + manual faults with severity
- **Tasks** — Technician task management (start/complete)
- **Cleaning Schedule** — Zone soiling map + cleaning calendar
- **Analytics** — 7-day generation vs target, PR trends
- **JWT Auth** — Role-based (Admin, Supervisor, Technician)

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL 15+](https://www.postgresql.org/download/)

## Quick Start

### 1. Backend (API)

```bash
cd backend/SolarShield.API
dotnet run
```

API runs at: `http://localhost:5034`  
Swagger UI: `http://localhost:5034/swagger`

Database `SolarShieldDb` is auto-created via EF migrations on first run.

### 2. Frontend (Angular)

```bash
cd frontend
npm install
ng serve
```

App runs at: `http://localhost:4200`

## Database Connection

Default connection string in `appsettings.json`:

```
Host=localhost;Port=5432;Database=SolarShieldDb;Username=postgres;Password=postgres
```

Update `ConnectionStrings:DefaultConnection` in `backend/SolarShield.API/appsettings.json` with your PostgreSQL credentials.

### Setup PostgreSQL

```sql
CREATE DATABASE "SolarShieldDb";
```

Or let EF Core create it automatically on first `dotnet run` (if the user has create-db permission).

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login |
| GET | `/api/dashboard/summary` | Dashboard KPIs |
| GET | `/api/faults` | List faults |
| POST | `/api/faults/predict/{siteId}` | Run AI fault prediction |
| GET | `/api/tasks` | List tasks |
| PUT | `/api/tasks/{id}` | Update task status |
| GET | `/api/cleaning/soiling/{siteId}` | Zone soiling index |
| GET | `/api/analytics/{siteId}` | Analytics summary |

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 20, SCSS, Standalone Components |
| Backend | ASP.NET Core 9 Web API |
| ORM | Entity Framework Core 9 |
| Database | PostgreSQL |
| Auth | JWT Bearer Tokens |
| Password | BCrypt |

---

Built for **Sanjeev(Sanju) Sehgal** — SolarShield AI MVP
