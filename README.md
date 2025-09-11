## Dengue Watch API

Minimal, production-ready ASP.NET Core 9.0 Web API for dengue monitoring and forecasting.

### Highlights
- **.NET 9 minimal API** with feature and endpoint discovery via reflection
- **PostgreSQL** with Entity Framework Core migrations
- **Supabase** integration (Auth/JWT); JWT Bearer authentication
- **Quartz** for background jobs (in-memory)
- **SignalR** hubs discovery
- **Serilog** structured logging (console + rolling files)
- **OpenAPI + Scalar** API reference in Development
- Weather data ingestion via **Open-Meteo** client

---

## Requirements
- .NET SDK 9.0+
- PostgreSQL (local or hosted; Supabase recommended)
- Optional: `dotnet-ef` tool for migrations

Install EF Core CLI (once):
```bash
dotnet tool install --global dotnet-ef
```

Verify versions:
```bash
dotnet --version
dotnet ef --version
```

---

## Project Structure
- `dengue.watch.api/` — main Web API project (root namespace `dengue.watch.api`)
- `dengue.watch.api.tests/` — tests project

Key folders in `dengue.watch.api/`:
- `features/` — vertical feature slices
  - Implement endpoints by adding classes that implement `IEndpoint` with static `MapEndpoints(...)`
- `infrastructure/` — database, host, hubs, ml, etc.
- `common/` — interfaces, extensions, options, services, exception handling
- `Migrations/` — EF Core migrations
- `logs/` — Serilog output (gitignored in typical setups)

Discovery extensions (reflection-based):
- `IFeature` → registered via `DiscoverFeatures(...)`
- `IEndpoint` → mapped via `DiscoverEndpoints(...)`
- `IHub` → mapped via `DiscoverHubs(...)`

Example endpoint (weather pooling): `GET /api/weatherpooling/dailyweather`

---

## Configuration
Copy the example app settings and adjust for your environment:
```bash
cp dengue.watch.api/appsettings.Example.json dengue.watch.api/appsettings.Development.json
```

Edit values under:
- **Serilog** (logging)
- **Supabase**
  - `Url` — your project URL
  - `AnonKey` — anonymous key
  - `JwtSecret` — service role JWT secret (enables JWT validation)
- **Postgres**
  - Either standard connection parameters (`Host`, `Port`, `Database`, `Username`, `Password`, `SslModeRequire`, `TrustServerCertificate`)
  - Or session pooling parameters (`UserId`, `Server`, `ServerPort`, `Database`, `Password`)

Configuration binding is standard ASP.NET Core, so environment variables can be used with `:` separators (e.g. `Supabase:Url`).

User Secrets (local dev) are supported via the project’s `UserSecretsId`:
```bash
cd dengue.watch.api
dotnet user-secrets set "Supabase:Url" "https://YOUR_PROJECT.supabase.co"
dotnet user-secrets set "Supabase:AnonKey" "..."
dotnet user-secrets set "Supabase:JwtSecret" "..."
dotnet user-secrets set "Postgres:Host" "localhost"
dotnet user-secrets set "Postgres:Database" "dengue"
dotnet user-secrets set "Postgres:Username" "postgres"
dotnet user-secrets set "Postgres:Password" "postgres"
```

Logging writes to `logs/dengue-watch-api-*.log` with 30-day retention by default.

---

## Database
Apply migrations to create/update the schema:
```bash
# From repo root
dotnet ef database update --project dengue.watch.api --startup-project dengue.watch.api

# Or from within the project directory
cd dengue.watch.api
dotnet ef database update
```

Connection string is built from the `Postgres` section. Session pooling is used automatically when `UserId/Server/ServerPort` are provided; otherwise standard connection fields are used.

---

## Run
Development run:
```bash
# From repo root
dotnet run --project dengue.watch.api

# Or inside the project folder
cd dengue.watch.api
dotnet run
```

Set the environment explicitly if needed:
```bash
# Windows (PowerShell)
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Windows (Git Bash / WSL / macOS / Linux)
export ASPNETCORE_ENVIRONMENT=Development
```

When running in Development:
- OpenAPI is exposed and Scalar reference UI is enabled
- Visit the Scalar UI (commonly `/scalar`) or the OpenAPI JSON (`/openapi/v1.json`)

Health check:
```text
GET /health
```

Example feature endpoint:
```text
GET /api/weatherpooling/dailyweather
```

---

## Authentication
JWT Bearer is enabled when `Supabase:JwtSecret` is configured.

- Issuer: `{Supabase:Url}/auth/v1`
- Audience: `dengue-watch-api`
- Send tokens via:
```http
Authorization: Bearer <jwt>
```

If no `JwtSecret` is set, authentication is not added.

---

## Logging
Serilog is configured via `appsettings` and programmatically to write to console and rolling files under `logs/`. Adjust levels and sinks as needed in `appsettings.*.json`.

---

## Jobs & Services
- **Quartz** is configured with in-memory store and a default thread pool
- **Open-Meteo** HTTP client is registered as `HttpClient("OpenMeteo")`
- **ML.NET** services are added via `AddMLServices()` (see `infrastructure/ml`)

---

## Testing
Run all tests:
```bash
dotnet test
```

---

## Conventions
- Root namespace and folders use all lowercase: `dengue.watch.api`
- Add new features under `features/<yourfeature>/` and expose endpoints via `IEndpoint.MapEndpoints(...)`
- Register feature services by implementing `IFeature.ConfigureServices(...)`
- Add SignalR hubs by implementing `IHub.MapHub(...)`

---

## Troubleshooting
- Verify database connectivity using the values in `appsettings.*.json`
- Ensure `ASPNETCORE_ENVIRONMENT` is set to `Development` to access docs locally
- On Windows behind proxies, ensure TLS/SSL trust settings match your Postgres host; `SslModeRequire` and `TrustServerCertificate` can be adjusted in `Postgres` config
- Check `logs/` for detailed Serilog output if startup fails

---

## License
Add your license here (e.g., MIT). If unsure, keep this section as TODO.


