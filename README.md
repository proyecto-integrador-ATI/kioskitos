# Kioskitos

Kioskitos is a web platform for independent professionals and freelancers in
Uruguay's creative and digital sectors. It centralizes the management of
clients, projects, tasks, estimates, tracked hours, contracts, and collections
in a single interface.

## Functional scope

- Modular management of clients, projects, stages, and tasks.
- Estimate generation and tracking.
- Worked-hours tracking.
- Formalization of contractual agreements with clients.
- Project and collection status tracking.
- Google Calendar integration.

## Architecture

The project is organized as a monorepo:

- `/frontend`: web application built with Next.js and TypeScript.
- `/backend`: REST API built with .NET and C#.
- PostgreSQL: relational database.

## Structure

```text
kioskitos/
├── frontend/
└── backend/
```

## Workflow

- `main` is protected: direct commits are not allowed.
- Every change must be submitted through a Pull Request with at least one approval.

## Branch naming

- `feat/<ticket>-<description>` - for example, `feat/KIOS-12-user-login`
- `fix/<ticket>-<description>` - for example, `fix/KIOS-30-cart-error`

Use hyphen-separated words and avoid spaces.

## Environment variables

Do not commit `.env` files. Use `.env.example` files as references.

## Local development with Docker

Create the local environment file from the example and start all services:

```bash
cp .env.example .env
docker compose up --build
```

The services are available at:

- Frontend: [http://localhost:3000](http://localhost:3000)
- Backend: [http://localhost:8080](http://localhost:8080)
- PostgreSQL: `localhost:5432`

PostgreSQL data is persisted in the `postgres_data` Docker volume. The backend
waits for PostgreSQL to pass its health check before starting.

## Database migrations

EF Core Tools are installed as a local tool (`backend/.config/dotnet-tools.json`).
From `backend/`:

```bash
dotnet tool restore
docker compose -f ../docker-compose.yml up -d postgres
set -a; source ../.env; set +a   # loads ConnectionStrings__Default

dotnet ef database update \
  --project src/Kioskitos.Infrastructure \
  --startup-project src/Kioskitos.API
```

To create a new migration:

```bash
dotnet ef migrations add <Name> \
  --project src/Kioskitos.Infrastructure \
  --startup-project src/Kioskitos.API \
  --output-dir Persistence/Migrations
```
## Running the API locally
Prerequisites: Docker Desktop running, .NET SDK from global.json, and a .env with Google__ClientId, Google__ClientSecret and a Jwt__Key of at least 32 characters (shorter keys make every request fail with 500).

dotnet does not read .env, so load it into the shell first. Run everything from the repo root, in the same terminal (Windows cmd; on bash use set -a; source .env; set +a).

docker compose up -d postgres
for /f "usebackq eol=# tokens=1* delims==" %a in (".env") do set "%a=%~b"
cd backend
dotnet ef database update --project src/Kioskitos.Infrastructure --startup-project src/Kioskitos.API
dotnet run --project src/Kioskitos.API
The API listens on http://localhost:5076. Check GET /api/health returns {"status":"Healthy"}. Stop with Ctrl + C and docker compose stop.

## Testing Google sign-in
The backend only validates a Google ID token; get one from the OAuth 2.0 Playground:

Gear icon → Use your own OAuth credentials → paste the Client ID and secret from .env. The OAuth client must list https://developers.google.com/oauthplayground as a redirect URI, and your account must be a test user.
Step 1: scopes openid email profile → Authorize APIs.
Step 2: Exchange authorization code for tokens → copy id_token (valid 1 hour).
From a second cmd window:

curl -X POST http://localhost:5076/api/auth/google -H "Content-Type: application/json" -d "{\"idToken\":\"<id_token>\"}"
curl http://localhost:5076/api/auth/me -H "Authorization: Bearer <accessToken>"
The first call returns an accessToken and isNewUser: true on first login (false afterwards). The second returns the user's userId, email and name; without the header it returns 401.

Symptom	Likely cause
failed to connect to the docker API	Docker Desktop is not running
Configurá ConnectionStrings__Default	.env not loaded in this terminal
500 on every request	Jwt__Key missing or shorter than 32 characters
401 on /api/auth/google	Expired id_token, or issued for another Client ID
redirect_uri_mismatch	Playground URI missing from the OAuth client


## Integration tests

Tests that need the database read `ConnectionStrings__Default` and are skipped
when it is not set. With Postgres running and `.env` loaded: `dotnet test`.

## Frontend

For frontend configuration and available commands, see
[frontend/README.md](./frontend/README.md).
