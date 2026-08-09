# Kennen Technologies Backend

ASP.NET Core 8 Web API for [kennen-technologies.com](https://kennen-technologies.com). Provides public contact intake, a CMS for the marketing site, careers/job applications, and an admin dashboard.

## Features

- **Contact form** — `POST /api/contact` captures leads from the marketing site.
- **CMS** — content sections, testimonials, and stats cards served to the site.
- **Careers** — public job listings and résumé uploads; admin applicant tracking.
- **Authentication** — ASP.NET Core Identity with JWT access tokens and refresh-token rotation.
- **Admin dashboard** — single-page app at `/admin` for managing leads, content, jobs, and applications.
- **Swagger/OpenAPI** — `http://localhost:5220/swagger/index.html` (dev only).

## Stack

- .NET 8 LTS
- Entity Framework Core 8 + Npgsql (PostgreSQL)
- ASP.NET Core Identity + JWT Bearer auth
- Swashbuckle.AspNetCore (Swagger)
- Docker & docker-compose

## Local development setup

### Prerequisites

- .NET 8 SDK
- PostgreSQL 14+ (local or Docker)

### 1. Database

Create a PostgreSQL database and user:

```sql
CREATE ROLE kennen_app LOGIN PASSWORD 'your-db-password';
CREATE DATABASE kennen OWNER kennen_app ENCODING 'UTF8';
```

### 2. User secrets

From the `src/Kennen.Api` folder, run:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=kennen;Username=kennen_app;Password=your-db-password"
dotnet user-secrets set "Jwt:Key" "a-32-byte-or-longer-random-key"
dotnet user-secrets set "Seed:AdminEmail" "admin@kennen-technologies.com"
dotnet user-secrets set "Seed:AdminPassword" "YourStrongAdminPassword123!"
```

Generate a strong `Jwt:Key` with at least 32 characters.

### 3. Run migrations and start

```bash
dotnet ef database update --project src/Kennen.Infrastructure --startup-project src/Kennen.Api
dotnet run --project src/Kennen.Api --urls "http://localhost:5220"
```

The API will:
- apply migrations automatically in Development
- seed the admin account and marketing site content
- open Swagger at `http://localhost:5220/swagger/index.html`
- serve the admin dashboard at `http://localhost:5220/admin/`

### 4. Marketing site

The static site in `kennen-website` expects the API at `http://localhost:5220` on localhost. Open it with any static server, e.g.:

```bash
cd ../kennen-website
python -m http.server 3000 --bind 127.0.0.1
```

## Testing

```bash
dotnet test
```

## Production deployment

### Option A: Docker on a VPS (cheapest)

1. Set up a server and install Docker + docker-compose.
2. Copy `docker-compose.yml` and a `.env` file.
3. Fill `.env` with production values (see `.env.example`).
4. Run:

```bash
docker compose up -d
```

5. Point your domain (e.g. `api.kennen-technologies.com`) at the server.
6. Add the real frontend URL to `Cors__AllowedOrigins__0` in `.env`.
7. Set `Jwt__Key` to a strong random string of at least 32 characters.

### Option B: GitHub Actions + DockerHub

1. Fork/push this repo to GitHub.
2. Add `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` to repository secrets.
3. Push to `main` — the workflow builds, tests, and pushes the image.
4. On the server, pull `docker compose pull` and `docker compose up -d`.

### Option C: Azure App Service

1. Create an Azure SQL / Azure Database for PostgreSQL.
2. Publish the `Kennen.Api` project to an App Service.
3. Set connection string, `Jwt:Key`, `Cors:AllowedOrigins`, and `Seed:Admin*` in Configuration → Application settings.

### Option D: Render (free tier) + Supabase (free database)

This is the recommended fully-free setup.

1. **Create the Supabase project**
   - Go to [supabase.com](https://supabase.com) and create a free project.
   - In **Project Settings → Database**, copy the **Connection string** for a direct connection.
   - It will look like:
     ```
     Server=db.xxxxx.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password=your-password;SSL Mode=Require;Trust Server Certificate=true;
     ```
   - Save this string for the next step.

2. **Deploy the API on Render**
   - Push this repo to GitHub.
   - Create a new Render account and use the **Blueprint** tab.
   - Connect the repo and choose `render.yaml`.
   - In the dashboard, set these three environment variables manually:
     - `ConnectionStrings__Default` — your Supabase connection string
     - `Jwt__Key` — at least 32 random characters
     - `Seed__AdminPassword` — a strong admin password

3. **The app is served at** `https://kennen-api.onrender.com` (or your custom domain).

**Free-tier limits:**
- Render free web services spin down after 15 minutes of inactivity (cold start on first request).
- Supabase free tier has a 500 MB database limit and 2 GB egress; fine for a small site.

### Option E: Railway / Fly.io

The `Dockerfile` works on any container platform. On Railway, connect the repo and it auto-detects the Dockerfile. On Fly.io, run:

```bash
fly launch --dockerfile Dockerfile
fly secrets set Jwt__Key=<key> Seed__AdminPassword=<pw>
```

## Vercel frontend

The Vercel frontend is in `kennen-website`. In `config.js`, set the production `apiBaseUrl` to your live backend domain, e.g.:

```js
apiBaseUrl: 'https://api.kennen-technologies.com'
```

Then commit and push to the `kennen-website` repo. Vercel will auto-deploy.

## Project structure

```
kennen-api/
  src/
    Kennen.Domain/          # Entities, enums, common base types
    Kennen.Infrastructure/  # EF Core, Identity, migrations, seeding
    Kennen.Api/             # Controllers, auth, storage, admin SPA
  tests/
    Kennen.Api.Tests/       # Unit tests
```

## Security notes

- Never commit `Jwt:Key`, connection strings, or admin passwords to source control. Use `dotnet user-secrets` locally and environment variables in production.
- The admin dashboard is served as a static SPA; all destructive actions require authenticated `Admin` or `Editor` roles.
- Résumés are stored under `storage/resumes` (or a mounted volume in Docker) with generated file names to prevent path-traversal.
