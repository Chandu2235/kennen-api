# Kennen API — Project notes for agents

## What this is

ASP.NET Core 8 backend for kennen-technologies.com, layered into Domain / Infrastructure / Api.

## Key commands

```bash
dotnet build Kennen.sln
dotnet test tests/Kennen.Api.Tests
dotnet ef migrations add <name> --project src/Kennen.Infrastructure --startup-project src/Kennen.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/Kennen.Infrastructure --startup-project src/Kennen.Api
dotnet run --project src/Kennen.Api --urls "http://localhost:5220"
dotnet user-secrets list --project src/Kennen.Api
```

## Local development requirements

- .NET 8 SDK (installed at `%LOCALAPPDATA%\Microsoft\dotnet` on this Windows machine)
- PostgreSQL 16 installed as a service
- Database `kennen` and role `kennen_app` created
- User secrets configured:
  - `ConnectionStrings:Default`
  - `Jwt:Key` (>=32 chars)
  - `Seed:AdminEmail`
  - `Seed:AdminPassword`

## Dev URLs

- API: `http://localhost:5220`
- Swagger: `http://localhost:5220/swagger/index.html`
- Admin SPA: `http://localhost:5220/admin/`
- Marketing site: `http://127.0.0.1:3000` (Python `http.server`)

## Architecture decisions

- **No public user registration.** Accounts are created by admins or the seeder.
- **Identity roles:** `Admin` (full access) and `Editor` (content, leads, applications, no user management).
- **Refresh token rotation** in `RefreshToken` table; hashes stored, replay detection by replacement reference.
- **Local file storage** for résumés with generated names and path-traversal guards; swap for blob storage by implementing `IFileStorage`.
- **Rate limiting:** `public-write` (5 per 10 min) for contact/applications, `authentication` (10 per 5 min) for login/refresh.
- **CORS:** strict allowlist in production; `AllowAnyOrigin` in Development to support browser-preview tunnels.

## Deployment artifacts

- `Dockerfile` — multi-stage build, runs on port 8080
- `docker-compose.yml` — Postgres + API, expects `.env`
- `.github/workflows/deploy.yml` — build, test, push Docker image

## Frontend link

The marketing site lives at `C:\Users\Chandrkant\kennen-website` and calls the API. It reads `window.KENNEN_CONFIG.apiBaseUrl` from `config.js`.
