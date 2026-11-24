# CLAUDE.md

Kioskitos: a web platform where freelancers in Uruguay's creative/digital sectors manage clients, projects, stages, tags, and a Google Calendar connection. Monorepo: `backend/` (.NET 10 REST API, Clean Architecture), `frontend/` (Next.js 16 App Router + TypeScript), PostgreSQL 16. See [README.md](README.md) for full local setup and the Google sign-in walkthrough.

## Hard rules

- **Code is always in English**: identifiers, comments, test names, log/exception messages. Only user-facing UI copy is Spanish (Uruguayan voseo: "Ingresá", "Revisá", "Probá").
- Never commit or print `.env` contents. Use `.env.example` as the reference.
- Branches: `feat/KSK-<n>-<description>` / `fix/KSK-<n>-<description>`. `main` is protected (PR + 1 approval).

## Commands

### Backend (run from `backend/`)

`global.json` pins SDK **10.0.403** with `rollForward: disable`. dotnet does not read `.env`, so load it first: `set -a; source ../.env; set +a`.

```bash
docker compose -f ../docker-compose.yml up -d postgres
dotnet build Kioskitos.sln
dotnet test Kioskitos.sln                                  # DB tests skipped unless ConnectionStrings__Default is set
dotnet test --filter "FullyQualifiedName~ProjectsDatabaseTests"   # single class
dotnet test --filter "FullyQualifiedName~CreateProjectServiceTests.Create_SavesProjectOnceAndReturnsDto"
dotnet run --project src/Kioskitos.API                     # http://localhost:5076, Swagger at /swagger in Development

dotnet tool restore
dotnet ef migrations add <Name> --project src/Kioskitos.Infrastructure --startup-project src/Kioskitos.API --output-dir Persistence/Migrations
dotnet ef database update --project src/Kioskitos.Infrastructure --startup-project src/Kioskitos.API
```

The API fails at startup without `Jwt__Key` (≥32 chars), `Jwt__Issuer`, `Jwt__Audience`, `Google__ClientId`, `Google__ClientSecret`, and `ConnectionStrings__Default`.

### Frontend (run from `frontend/`, Node 22)

```bash
npm ci
npm run dev        # http://localhost:3000; needs NEXT_PUBLIC_API_URL (and NEXT_PUBLIC_GOOGLE_CLIENT_ID) in .env.local
npm run lint
npm test           # vitest run
npx vitest run app/clients/new/page.test.tsx   # single file
npm run build
```

### Full stack

`cp .env.example .env && docker compose up --build`: frontend :3000, backend :8080, postgres :5432.

CI ([.github/workflows/cicd.yaml](.github/workflows/cicd.yaml)) runs on PRs to `main`: backend restore/build/test against a Postgres service, frontend lint/test/build, then Docker builds with Trivy scans (fails on CRITICAL/HIGH).

## Backend architecture

`Kioskitos.sln`. The dependency direction is API → Application + Infrastructure, Infrastructure → Application → Domain.

- **Domain** (`src/Kioskitos.Domain/Entities`): rich entities with private setters, invariants checked in constructors/methods (throw `ArgumentException`/`InvalidOperationException`), a private parameterless ctor for EF, and collections exposed as `IReadOnlyCollection` over private `List<>` fields. Entities: User, Client, ClientContact, Project, ProjectStatus, Stage, StageName, Tag, ProjectTag, GoogleCalendarConnection. Ids are `Guid.NewGuid()` set in the constructor.
- **Application** (`src/Kioskitos.Application/<Feature>/`): one folder per feature with `Abstractions.cs` (repository/gateway interfaces), DTOs, and concrete services (e.g. `CreateProjectService`, `ProjectQueryService`). Services take a `userId` explicitly and return result records (`XResult.Success(...)` / `XResult.Failure(XError.…)` with an error enum) instead of throwing for expected failures. Services are concrete classes registered in `Program.cs`, not interfaces.
- **Infrastructure** (`src/Kioskitos.Infrastructure`): `AppDbContext` + `Persistence/Configurations/*Configuration.cs` (`IEntityTypeConfiguration`), repositories, migrations, Google token validation, JWT generation, Google Calendar gateway, and DataProtection (keys persisted in the DB) to encrypt refresh tokens. Registrations live in `DependencyInjection.AddInfrastructure`.
- **API** (`src/Kioskitos.API`): `[ApiController][Authorize]` controllers with primary-constructor injection. Request contracts live in `Contracts/`. Get the user with `User.TryGetUserId(out var userId)` (the JWT `sub` claim, `MapInboundClaims = false`). Map service error enums to `ProblemDetails` with an `errorCode` extension (see `ProjectsController.ProjectProblem`). `ExceptionHandlingMiddleware` turns unhandled exceptions into a 500 ProblemDetails. Enums are serialized as strings (`JsonStringEnumConverter`). CORS policy "Frontend" uses `FrontendUrl` (default `http://localhost:3000`).

### Conventions

- **Multi-tenancy by user**: every query filters by `UserId`. A resource owned by another user returns **404**, never 403, so its existence is not revealed (e.g. `IClientOwnershipChecker`).
- **Persistence**: table names are snake_case (`projects`, `stage_names`) but columns stay PascalCase. Enums are stored as strings (`HasConversion<string>()`). Ids use `ValueGeneratedNever()`. Navigations use `PropertyAccessMode.Field`. Length limits are constants on the entity (`Project.NameMaxLength`) and reused by the EF config and validation.
- **Unit of work**: a single `SaveChangesAsync` per command. The repos that look up entities which may be reused (`FindByNamesAsync` for tags and stage names) must return **tracked** entities, or EF re-inserts them. Read-only queries use `AsNoTracking`.
- Tags and stage names are per user, unique on `(user_id, name)`, and reused case-insensitively. A project with `HasStages=false` gets one generic stage; with `HasStages=true` it needs ≥1 named stage, ordered by `Position`.
- Comments explain *why* (see existing ones), and `/// <summary>` goes on interface methods.

### Tests (`backend/tests/Kioskitos.API.Tests`, xUnit, a single project)

- **Service tests** use hand-written fakes/in-memory repos (`ProjectFakes.cs`), with no mocking library.
- **Endpoint tests** use `KioskitosWebApplicationFactory` (`IClassFixture`), which injects test JWT/Google config and runs migrations once when a DB is configured. Tests create real users and mint a bearer token with `JwtTokenGenerator`.
- **DB-backed tests** use `[RequiresDatabaseFact]` and are skipped when `ConnectionStrings__Default` is unset. They seed data with unique values (`$"sub-{Guid.NewGuid()}"`) and clean up after themselves.
- Naming: `Method_Scenario[_Expected]`, e.g. `CreateAndRead_PersistsProjectWithGenericStageAndReusedTags`.

## Frontend architecture

**Read `frontend/AGENTS.md` first.** Next 16.4 has breaking changes vs. training data, so check `frontend/node_modules/next/dist/docs/` before using Next APIs.

- `app/`: routes (`login`, `home`, `profile`, `calendar`, `clients`, `clients/new`, `clients/[clientId]` (detail + its projects), `clients/[clientId]/projects`, `clients/[clientId]/projects/new`, `projects/[id]`). `/` redirects to `/login`. Pages are `"use client"` and gate on `getToken()`, calling `router.replace("/login")` when the token is missing.
- `components/`: `AppHeader`, `Button`, `TagSelector`, `StageSelector`, `ClientProjectsList`. Tests sit next to the files they cover (`*.test.tsx`).
- `services/`: `api.ts` `apiFetch<T>()` prefixes `NEXT_PUBLIC_API_URL` and throws `ApiError(status, problem)` on non-2xx. `auth.ts` stores the JWT in `localStorage` under `kioskitos_token`. Feature services (`clients.ts`, `projects.ts`, `users.ts`) add the `Authorization: Bearer` header and define the TS types that mirror the backend DTOs. Spanish labels for enums live here (`STATUS_LABELS`).
- Forms: `react-hook-form` + `zod` (`zodResolver`) with Spanish validation messages. The 400/401/other `ApiError` statuses map to friendly Spanish messages.
- Google auth via `@react-oauth/google`: login sends the ID token to `POST /api/auth/google`. The calendar uses the `auth-code` popup flow, and the code is posted to `POST /api/google-calendar/connect`.

### Gotchas

- **`cacheComponents: true`**: navigating away hides a page with React `<Activity>` instead of unmounting it, so state persists. Forms must reset after a successful submit (`reset(...)`) and clear transient messages (see `useLayoutEffect(() => () => setResult(null), [])` in `clients/new/page.tsx`).
- `useParams` in dynamic routes must sit inside `<Suspense>`, or `next build` fails.
- Components that produce random ids (e.g. `useFieldArray`) must render client-only (`useSyncExternalStore` `isClient` guard) to avoid prerender failures.
- `api.ts` throws at import time if `NEXT_PUBLIC_API_URL` is missing.

### Styling (Figma design system, Tailwind v4 in `app/globals.css`)

- Colors: `green` (#276651, primary text/borders), `cream` (card background), `ash` (page background), `red` (errors), `blue`.
- Text utilities: `text-header1`, `text-header2` (Boldonse), `text-special-header` (Bitcount), `text-normal`, `text-small` (Inter).
- Patterns: page `bg-ash` with `<AppHeader />`, cards `rounded-[20px] border border-green bg-cream p-6`, inputs `rounded-[10px] border border-green bg-white`, pill buttons. Reuse `/home` and `/clients/new` as references.

### Frontend tests (Vitest + Testing Library + jsdom)

Mock `next/navigation` and `services/api` (`apiFetch`) with `vi.mock`, and seed `localStorage.setItem("kioskitos_token", "jwt")` in `beforeEach`. Use `userEvent` for interactions and query by role/label using the Spanish UI text. There is no `globals: true`, so import `describe/it/expect/vi` from `vitest`.
