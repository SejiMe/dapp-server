# Dengue Watch API — Project Goal (Initial Understanding)

## Short summary
This repository implements the Dengue Watch API: a .NET 9 web API that provides dengue surveillance functionality, including data storage, real-time updates, scheduled processing, ML-based predictions, and authenticated access for clients.

## Primary goal
Provide a reliable, production-ready backend that:
- Ingests and persists dengue-related data in PostgreSQL.
- Runs scheduled jobs (Quartz) to process and analyze incoming data.
- Exposes REST endpoints and SignalR hubs for real-time updates.
- Supplies ML-powered predictions or analytics (ML.NET components).
- Secures endpoints using Supabase-backed JWT authentication.
- Provides OpenAPI documentation and developer references.

## Key components (what I see in the code)
- Platform: .NET 9 minimal API.
- Logging: Serilog (console + file rolling logs).
- Persistence: EF Core with Npgsql (Postgres).
- Background jobs: Quartz scheduler.
- Realtime: SignalR hubs discovered by reflection.
- Auth: Supabase configuration + JWT Bearer tokens.
- Rate limiting: ASP.NET Core Rate Limiting (global + named limiters).
- CORS: Strict origins from configuration (`Cors:AllowedOrigins`).
- Docs: Scalar/OpenAPI integration for API docs and client generation.
- External integrations: OpenMeteo service and Supabase client.

## Environment & run notes (initial)
- Required configuration:
  - `Cors:AllowedOrigins` (comma-separated production origins).
  - `Supabase:JwtSecret` and `Supabase:Url` for auth.
  - Postgres settings under the `PostgresOptions` section used by EF Core.
- Dev debug URL set in DEBUG to `http://0.0.0.0:5000`.
- Health endpoint: `GET /health` (current code requires authorization).
- To run locally:
  - `dotnet build`
  - set environment variables or user secrets for the required config
  - `dotnet run --project dengue.watch.api`

## Suggested next steps
- Add a README with a complete list of environment variables and example `appsettings.Development.json`.
- Provide a Dockerfile and docker-compose for local Postgres + Supabase emulation (if needed).
- Document how to run migrations (`dotnet ef database update`) and which startup project to use.
- Confirm whether `/health` should require authorization and adjust if needed.

If you want, I can create a full `README.md` with run steps, or generate a checklist of required environment variables and example values.