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

## Gotchas hit in Theme 2 and Theme 1 — don't rediscover these

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
  this, try removing the package before assuming it's needed. The opposite trap also
  exists: `SQLitePCLRaw.bundle_e_sqlite3` looked equally redundant under
  `Microsoft.EntityFrameworkCore.Sqlite`, but removing it let the transitive
  resolution drop to a version with a known high-severity vulnerability (NU1903) —
  verify redundancy with `dotnet nuget why` before removing, don't assume from the
  warning alone.
- **EF Core's Sqlite provider translates `string.Contains`/`StartsWith`/`EndsWith` to
  `instr()`, which is case-sensitive** (a deliberate EF Core 5+ change to match
  .NET's ordinal semantics) — not to SQL `LIKE`, which SQLite treats as
  case-insensitive for ASCII regardless of column collation. A naive
  `post.Title.Contains(search)` silently fails a "case-insensitive search"
  requirement on Sqlite. Use `EF.Functions.Like(post.Title, $"%{search}%")` instead
  (see `PostsController.Index`).
- **Registering a remote authentication scheme (anything with a `CallbackPath`, e.g.
  `AddGoogle`) with an empty `ClientId`/`ClientSecret` breaks every page on the site,
  not just the feature that uses it.** ASP.NET Core's authentication middleware
  initializes every such handler on *every* request — not just requests that touch
  it — to check whether that request is hitting its OAuth callback URL, and
  `OAuthOptions.Validate()` throws unconditionally on an empty `ClientId`. Only call
  `.AddGoogle(...)` when both secrets are actually configured (see `Program.cs`); a
  missing/optional external provider should degrade to "that one button doesn't
  work," not a site-wide 500.
- **`dotnet user-secrets` *does* flow into `WebApplicationFactory` test hosts**,
  because the factory defaults to the `Development` environment and
  `WebApplication.CreateBuilder` adds the user-secrets configuration source
  whenever `IsDevelopment()` is true. Verified empirically: temporarily removed the
  Google secrets from this machine's secret store and reran the full Portal suite —
  all 12 tests still passed, because none of them exercise the real `AddGoogle`
  scheme (`ExternalLoginTests` fakes the OAuth round trip via a test-only
  `IStartupFilter` instead). Worth knowing before writing a test that *does* touch a
  real external-provider scheme — such a test would silently depend on whatever is
  in the developer's local secret store, the same class of problem
  `TodoApiFactory` avoids for the JWT signing key by supplying its own.

## Theme 1 — Portal (`src/Portal`) — in progress (see Status)

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

### Deliberate non-goal: auto-linking external logins to existing local accounts

`AccountController.ExternalLoginCallback` **rejects** a Google sign-in when the
Google account's email matches an existing local (password) account, rather than
linking the two. This was a considered decision, not an omission — see commit
`feat(portal): add Google external login`.

**Why reject:** local registration in this app never verifies email ownership (no
`RequireConfirmedEmail`, no confirmation email sent). Auto-linking on email match
would let an attacker pre-register a victim's real email locally, then have the
victim's later, genuinely Google-verified sign-in silently absorbed into the account
the attacker already controls via the local password. Gating on Google's own
`email_verified` claim doesn't fix this — it only verifies the Google side; the hole
is entirely on the unverified local side.

**The correct shape, if linking is ever wanted:** initiate it from an
*already-authenticated* session — a "connect your Google account" action available
only to a user already signed in with their password, so ownership of the local
account is proven before any external identity is attached to it. Never link
implicitly during an anonymous login attempt.

**Prerequisite before auto-linking-during-login could be safe:** local email
confirmation (`RequireConfirmedEmail` plus a real confirmation-email flow), so the
local account's `Email` field carries the same verification confidence as Google's.
Without that, "the emails match" isn't a trustworthy signal no matter how tightly the
external side is gated.

### Personal contacts: directional, DisplayName-only, dual enforcement

Contacts are **directional** (I add you, you don't automatically have me) — matches
the assignment's own one-directional phrasing ("προσωπικές επαφές" / "add other users
as contacts"), and the described behaviour has no request/accept step anywhere in it.

Browsing to add a contact never renders another user's email. Results use
`UserSearchResultViewModel` (`Id`/`DisplayName` only), not the `PortalUser` entity, so
email is structurally absent rather than just conventionally omitted. Search matches
`DisplayName` by partial, case-insensitive substring, and email by **exact match
only** (against `NormalizedEmail`) — findability for someone who already knows the
address, without turning the page into an enumerable directory.

The no-self-contact and no-duplicate-pair rules are each enforced twice, on purpose:
a SQLite `CHECK` constraint and a composite unique index on `(OwnerId,
ContactUserId)` are the guarantee — they're what actually closes the race between two
concurrent adds. Matching checks in `ContactsController.Add` are for the message — a
readable "already in your contacts" instead of a raw constraint-violation 500.
Verified as two separate claims: `ContactConstraintTests` inserts straight through
`PortalContext`, bypassing the controller, to prove the database itself rejects the
row; `ContactsControllerTests` proves the friendly-message paths.

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

Theme 1 (Portal) — in progress:
- [x] ASP.NET Core Identity: `PortalUser : IdentityUser` + `DisplayName`, `PortalContext
      : IdentityDbContext<PortalUser>` on its own SQLite database (`portal.db`,
      separate from `todos.db`); hand-written (not scaffolded) `AccountController`
      with Register/Login/Logout
- [x] Posts: `Post` entity with a real FK to `PortalUser` (not an email string, unlike
      `Todo.CreatedBy`); CRUD via `PostsController`; edit/delete scoped to the author
      via the same 404-not-403 convention as TodoApi; case-insensitive search
      (`EF.Functions.Like`, not `string.Contains` — see Gotchas) plus category filter
      (`PostCategory` enum via `HasConversion<string>()`) composed onto one
      `IQueryable` before a single `ToListAsync`
- [x] HTTP Basic authentication: custom `AuthenticationHandler<AuthenticationSchemeOptions>`
      registered as an *additional* scheme via the parameterless `AddAuthentication()`
      overload (never the default), validated against `UserManager.CheckPasswordAsync`,
      demoed on `GET /api/me`
- [x] Google external login: `AddGoogle`, registered only when both
      `Authentication:Google:ClientId`/`ClientSecret` are configured (registering it
      unconditionally 500s the whole site when they're absent — see Gotchas); rejects
      rather than auto-links on an email match with an existing local account (see
      "Deliberate non-goal" above), and rejects a missing or unverified email claim
- [x] Personal contacts: `Contact` join entity, directional, with a database CHECK
      constraint (no self-contact) and a composite unique index (no duplicate pairs)
      as the guarantee, plus matching `ContactsController` checks for a readable
      message (see "Personal contacts" above); browse/search results expose
      `DisplayName` only (own view model, not the `PortalUser` entity), with email
      matched exact-only
- [x] All 17 Portal tests passing (66 total across both themes)

Not yet built:
- [ ] Direct messages (popup window, 1-to-1 and group)
- [ ] Live notifications
