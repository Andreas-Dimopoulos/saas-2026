# saas-2026

Solo assignment for a Service-Oriented Software course. Two deliverables in one repo,
one solution, three projects.

## Purpose of this file

This project is being built by the student *with* Claude, not *for* the student. The
student must be able to verbally defend every line at an oral exam. That constraint
outranks speed or completeness — see "Working conventions" below.

## Repo layout

```
saas-2026/
├── global.json          # pins .NET SDK to 10.0.201, rollForward: disable
├── saas-2026.sln
├── src/
│   ├── Portal/           # Theme 1 — ASP.NET Core MVC (Controllers + Views)
│   └── TodoApi/          # Theme 2 — ASP.NET Core Web API (controller-based)
├── tests/
│   └── TodoApi.Tests/    # xUnit, project-references src/TodoApi
└── docs/                 # not yet created
```

Target framework for all projects: `net10.0`.

## Theme 1 — Portal (`src/Portal`)

Lab-group collaboration portal. ASP.NET Core MVC (Controllers + Views), chosen over
Razor Pages because it maps more naturally onto distinct "services" (posts, contacts,
messaging, auth) and makes it easier to apply different auth schemes per
controller/action.

Planned features (not yet built — add one at a time):
- Posts with search and categories, across multiple users
- Adding other users as personal contacts
- Local (username/password) authentication
- Google third-party login
- HTTP Basic authentication
- Direct messages in a popup window (1-to-1 and group)
- Live notifications

## Theme 2 — TodoApi (`src/TodoApi`)

Todo REST API, built test-driven. Controller-based Web API, chosen over Minimal API
for a 1:1 mapping between the endpoint table below and testable controller actions.

Exact endpoint contract (do not deviate without discussion):

```
POST   /signup                    GET    /todos/:id
POST   /auth/login                PUT    /todos/:id
GET    /auth/logout               DELETE /todos/:id
GET    /todos                     GET    /todos/:id/items/:iid
POST   /todos                     POST   /todos/:id/items
PUT    /todos/:id/items/:iid      DELETE /todos/:id/items/:iid
```

Must ship with OpenAPI/Swagger documentation and be verifiable from the command line
with `httpie`.

Persistence: EF Core with SQLite.

Tests live in `tests/TodoApi.Tests`, which project-references `src/TodoApi` and runs
real HTTP requests against it via a `WebApplicationFactory<Program>`-based fixture
(`TodoApiFactory`), backed by an isolated SQLite in-memory connection per factory
instance (not the EF Core InMemory provider — that doesn't enforce FK/relational
constraints).

### Required local setup: JWT signing key

The JWT signing key lives in **user-secrets**, never in `appsettings.json` or the repo.
`src/TodoApi` won't start (and `dotnet test` will fail at host startup) without it.
One-time setup from `src/TodoApi`:

```
dotnet user-secrets set "Jwt:SigningKey" "<base64-encoded 256-bit key>"
```

Generate a key (PowerShell):

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[System.Convert]::ToBase64String($bytes)
```

This is per-developer-machine by design (stored outside the repo, under
`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` on Windows) — a fresh
clone or CI environment needs this set (or the equivalent `Jwt__SigningKey` env var)
before the app or its tests will run.

## Working conventions (how to collaborate on this repo)

- **Small increments only.** Finish one thing, stop, explain what was built and why,
  and wait for explicit go-ahead before continuing. Never scaffold multiple features
  in a single pass, even if it would be more efficient to batch them.
- **Ask before adding any NuGet package.** State what the package does and why it's
  needed. This includes transitive dependencies that need pinning (e.g. security
  patches) — confirm the exact version before editing a `.csproj`.
- **TDD for Theme 2, no exceptions.** Write the failing test first, run it, watch it
  fail for the expected reason, then write the minimum implementation to pass it.
  Never write TodoApi implementation code before its test exists.
- **Flag uncertainty about .NET 10 APIs explicitly.** Patterns changed between .NET 8
  and .NET 10 (e.g. built-in OpenAPI document generation, identity APIs). If unsure
  whether something is current, say so rather than guessing, and check docs/verify
  before using it.
- **The student must be able to explain every line.** Prefer explicit, conventional
  code over clever abstractions. Avoid generating large blocks of boilerplate the
  student hasn't seen built up piece by piece.

## Environment

- OS: Windows 11
- .NET SDKs installed: 8.0.302, 9.0.312, 10.0.201 — this repo is pinned to 10.0.201
  via `global.json` (`rollForward: disable`)
- `dotnet-ef` 10.0.5
- Git 2.51
- Node 24 (available if the Portal frontend ends up needing it; not yet in use)
- Verification tool for TodoApi: `httpie`

## Status

- [x] Git repo initialized
- [x] `global.json` pinned to 10.0.201
- [x] Solution + three projects created and wired (`dotnet build` passes clean)
- [x] `Microsoft.OpenApi` pinned to 2.7.5 in TodoApi (template default 2.0.0 had a
      known high-severity advisory, GHSA-v5pm-xwqc-g5wc / CVE-2026-49451)
- [x] `Todo`/`TodoItem` entities, cascade delete, all five `/todos` endpoints
- [x] All four `/todos/:id/items/:iid` endpoints, scoped to their parent todo
- [x] `POST /signup` — `User` entity, `PasswordHasher<User>` (no extra NuGet package
      needed — ships in the `Microsoft.AspNetCore.App` shared framework already)
- [x] `POST /auth/login` — issues a JWT; `GET /auth/logout` — real jti-denylist
      revocation, lazy purge-on-logout keeps the table bounded
- [x] `[Authorize]` on TodosController and ItemsController; every query scoped to
      the authenticated user's email claim; `CreatedBy` no longer client-supplied
- [ ] Swagger/OpenAPI polish, httpie verification pass, Portal (Theme 1) — not started
