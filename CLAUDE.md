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
└── docs/                 # openapi.json export + httpie-verification.md transcript
```

Target framework for all projects: `net10.0`.

## Theme 2 — TodoApi (`src/TodoApi`) — COMPLETE

All twelve endpoints, tests, auth, and docs are done (see Status). **Do not modify
`src/TodoApi`, `tests/TodoApi.Tests`, or `docs/` without asking first** — treat it as
frozen reference code while working on Theme 1.

Controller-based Web API, chosen over Minimal API for a 1:1 mapping between the
endpoint contract and testable controller actions. Persistence: EF Core with SQLite.
Auth: hand-rolled JWT bearer (see Gotchas) plus a jti-denylist for real logout.

Endpoint contract:

```
POST   /signup                    GET    /todos/:id
POST   /auth/login                PUT    /todos/:id
GET    /auth/logout               DELETE /todos/:id
GET    /todos                     GET    /todos/:id/items/:iid
POST   /todos                     POST   /todos/:id/items
PUT    /todos/:id/items/:iid      DELETE /todos/:id/items/:iid
```

Tests live in `tests/TodoApi.Tests`, project-referencing `src/TodoApi` and running real
HTTP requests via a `WebApplicationFactory<Program>` fixture (`TodoApiFactory`), backed
by an isolated SQLite in-memory connection per factory instance (not the EF Core
InMemory provider — that doesn't enforce FK/relational constraints). `TodoApiFactory`
sets its own `Jwt__SigningKey` environment variable before the host is created, so
`dotnet test` is fully self-contained and needs no local setup.

### Required local setup for running the app (not the tests): JWT signing key

The JWT signing key lives in **user-secrets**, never in `appsettings.json` or the repo.
`src/TodoApi` (via `dotnet run`) won't start without it. One-time setup from
`src/TodoApi`:

```
dotnet user-secrets set "Jwt:SigningKey" "<base64-encoded 256-bit key>"
```

Generate a key (PowerShell):

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[System.Convert]::ToBase64String($bytes)
```

Stored outside the repo, under
`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` on Windows.

## Patterns established in Theme 2 — keep using where applicable in Theme 1

- **Separate request/response DTOs, always.** Never return EF entities directly from
  an action: over-posting risk on input, and navigation properties (e.g.
  `Todo.Items`/`TodoItem.Todo`) cause serialization cycles on output.
- **Failure-path tests written and confirmed red before implementation, every time.**
  Not just for the happy path — 404s, 400s, cross-user/cross-parent cases too. Run the
  test against the unimplemented/unlocked code first and see it fail for the expected
  reason before writing the fix.
- **Verify a test actually guards something by deliberately breaking the
  implementation it protects, confirming the test fails, then restoring the real
  code.** Used repeatedly (DB isolation, cascade delete, cross-tenant item access,
  cross-user todo access, JWT denylist) — catches tests that would pass trivially
  regardless of whether the guarded behavior is correct.
- **Scope every nested or owned resource query at every level, not just the
  immediate parent.** Items are scoped to their todo (`TodoId` match) *and* the todo
  is scoped to its owner (`CreatedBy` match) — both checks, on every item endpoint.
  A resource that exists but belongs to someone else must be indistinguishable from
  one that doesn't exist at all (404, not 403).

## Gotchas hit in Theme 2 — don't rediscover these

- **`[property: Required]` on a record's primary constructor parameter compiles but
  breaks at request time.** MVC's model validation for record types requires the
  attribute directly on the parameter (`[Required] string Title`, no `property:`
  target) — the `property:`-targeted form throws `InvalidOperationException` the
  first time the model is validated.
- **`WebApplicationFactory.ConfigureWebHost`'s `ConfigureAppConfiguration`/service
  overrides run too late to affect configuration read eagerly in `Program.cs` before
  `builder.Build()`.** For the minimal-hosting model, all of `Program.cs`'s
  builder-construction code runs during `WebApplicationFactory`'s host-factory
  resolution, before any test customization is applied. Use an environment variable
  set before the host is created instead (see `TodoApiFactory`'s constructor) — env
  vars are read by `WebApplication.CreateBuilder(args)` itself, early enough to
  matter.
- **`JwtBearerOptions.MapInboundClaims = false`** is set explicitly on the JWT
  handler. ASP.NET Core's default inbound claim-type remapping (short names like
  `sub`/`email` silently rewritten to long XML claim URIs) has been a source of
  confusion across .NET versions; setting this makes claims in code match exactly
  what's in the token.
- **NU1510 ("will not be pruned... likely unnecessary")**: some packages are
  redundant as explicit `PackageReference`s in `Microsoft.NET.Sdk.Web` projects
  because their types already ship in the `Microsoft.AspNetCore.App` shared
  framework. Hit this with `Microsoft.Extensions.Identity.Core` — removed it,
  `PasswordHasher<TUser>` still resolved and all tests still passed. If NuGet warns
  this, try removing the package before assuming it's needed.

## Theme 1 — Portal (`src/Portal`) — not started

Lab-group collaboration portal. ASP.NET Core MVC (Controllers + Views), chosen over
Razor Pages because it maps more naturally onto distinct "services" (posts, contacts,
messaging, auth) and makes it easier to apply different auth schemes per
controller/action.

**Auth is deliberately full ASP.NET Core Identity with cookies** — the opposite of
TodoApi's hand-rolled stateless JWT — so the student can compare and defend both
approaches at the oral exam. The Identity UI is to be **hand-written, not scaffolded**
(no `dotnet aspnet-codegenerator identity`): the student needs to be able to explain
every Razor page and controller action, not just accept generated boilerplate.

Planned features (not yet built — add one at a time):
- Posts with search and categories, across multiple users
- Adding other users as personal contacts
- Local (username/password) authentication — full Identity + cookies
- Google third-party login
- HTTP Basic authentication
- Direct messages in a popup window (1-to-1 and group)
- Live notifications

## Working conventions (how to collaborate on this repo)

- **Small increments only.** Finish one thing, stop, explain what was built and why,
  and wait for explicit go-ahead before continuing. Never scaffold multiple features
  in a single pass, even if it would be more efficient to batch them.
- **Ask before adding any NuGet package.** State what the package does and why it's
  needed. This includes transitive dependencies that need pinning (e.g. security
  patches) — confirm the exact version before editing a `.csproj`.
- **TDD, no exceptions.** Write the failing test first, run it, watch it fail for the
  expected reason, then write the minimum implementation to pass it. Never write
  implementation code before its test exists.
- **Flag uncertainty about .NET 10 APIs explicitly.** Patterns changed between .NET 8
  and .NET 10 (e.g. built-in OpenAPI document generation, identity APIs). If unsure
  whether something is current, say so rather than guessing, and check docs/verify
  before using it.
- **The student must be able to explain every line.** Prefer explicit, conventional
  code over clever abstractions. Avoid generating large blocks of boilerplate the
  student hasn't seen built up piece by piece. This is why Theme 1's Identity UI is
  hand-written rather than scaffolded.

## Environment

- OS: Windows 11
- .NET SDKs installed: 8.0.302, 9.0.312, 10.0.201 — this repo is pinned to 10.0.201
  via `global.json` (`rollForward: disable`)
- `dotnet-ef` 10.0.5
- Git 2.51
- Node 24 (available if the Portal frontend ends up needing it; not yet in use)
- Verification tool for TodoApi: `httpie`

## Status

Theme 2 (TodoApi) — complete:
- [x] All twelve endpoints (`/signup`, `/auth/login`, `/auth/logout`, five `/todos`,
      four `/todos/:id/items/:iid`)
- [x] `Todo`/`TodoItem`/`User`/`RevokedToken` entities; cascade delete on
      `Todo → TodoItem`
- [x] JWT auth: `PasswordHasher<User>` (no extra package), signing key in
      user-secrets, jti-denylist logout with lazy purge-on-logout
- [x] `[Authorize]` + per-user/per-owner query scoping on `TodosController` and
      `ItemsController`
- [x] Swagger UI (`/swagger/index.html`, dev only) with a JWT bearer security scheme,
      scoped to `[Authorize]`-protected operations only; XML doc comments +
      `[ProducesResponseType]` on every action, including 409 on signup
- [x] `docs/openapi.json` export + `docs/httpie-verification.md` (every endpoint
      exercised for real, including the revoked-token 401)
- [x] All 49 tests passing, no known package vulnerabilities

Theme 1 (Portal) — not started:
- [ ] Everything (see "Theme 1 — Portal" above)
