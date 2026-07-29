# Setup: clean clone to running system

Every command below was actually run against a fresh `git clone` of this repository
in a separate directory, not written from memory. Where something failed, the
failure is shown; where a secret/credential was involved, the exact value isn't
reproduced here.

## Prerequisites

- **.NET SDK 10.0.201**, exactly. `global.json` pins it with `rollForward: disable`,
  which means an SDK mismatch is not a warning — it's a hard failure of every `dotnet`
  command in the repo. Reproduced by editing a scratch copy of `global.json` to request
  a nonexistent version:
  ```
  A compatible .NET SDK was not found.
  Requested SDK version: 10.0.999
  global.json file: ...\global.json
  Installed SDKs:
  Install the [10.0.999] .NET SDK or update [...\global.json] to match an installed SDK.
  ```
  Check what's installed with `dotnet --list-sdks`; install 10.0.201 if it's missing.
- **The `dotnet-ef` global tool**, matching the version noted in `CLAUDE.md`'s
  Environment section (10.0.5 as of this writing). **This is not in the repo and not
  scaffolded by `dotnet restore`** — there's no local tool manifest
  (`.config/dotnet-tools.json`) committed, so a clean machine needs:
  ```
  dotnet tool install --global dotnet-ef --version 10.0.5
  ```
  Without it, every `dotnet ef ...` command in this document fails with a "command not
  found"-style error. Nothing in `CLAUDE.md` currently gives this install command — it
  only lists the tool as part of the documented dev environment.

## 1. Clone and build

```
git clone <repo-url>
cd saas-2026
dotnet build
```

`dotnet build` at the repo root finds the solution automatically. One thing worth
knowing before you go looking for it: the file is **`saas-2026.slnx`**, not
`saas-2026.sln` — `CLAUDE.md`'s repo-layout diagram is out of date on this point (the
`.slnx` format is the newer XML-based solution file introduced alongside .NET 9/10
tooling). Doesn't block anything since `dotnet build`/`dotnet test` resolve it
automatically either way, but worth knowing if you go looking for a `.sln` file and
don't find one.

## 2. Run the tests

```
dotnet test
```

Both suites (49 TodoApi tests, 22 Portal tests) pass from a completely clean clone
with **zero** local setup — no `.db` files, no user-secrets, nothing. This matches
what `CLAUDE.md` claims: both test fixtures (`TodoApiFactory`, `PortalFactory`) build
their own isolated in-memory SQLite database via `Database.EnsureCreated()`, and
`TodoApiFactory` supplies its own throwaway JWT signing key via an environment
variable set before the host is built.

## 3. Run TodoApi

Neither app runs any migration at startup — confirmed by grepping `Program.cs` in
both projects for `Migrate()`/`EnsureCreated()` (neither call exists) and by observing
what actually happens against a fresh clone with no `.db` file: `GET /todos` still
answers (401, no DB touched for an unauthenticated request), but the first request
that actually queries the database (e.g. `POST /signup`) throws
`SqliteException: SQLite Error 1: 'no such table: Users'` and returns 500.

**Required, every time, on a machine that's never run this repo before:**

```
cd src/TodoApi
dotnet ef database update
```

**Required once per machine** (the JWT signing key lives in user-secrets, never in
`appsettings.json` or the repo — already documented in `CLAUDE.md`, and independently
confirmed here): running `dotnet run` with no key configured throws immediately —

```
Unhandled exception. System.InvalidOperationException: Jwt:SigningKey is not
configured. Run: dotnet user-secrets set "Jwt:SigningKey" "<base64-key>" from
src/TodoApi.
```

— which is itself a good error message; it tells you exactly what to run. Generate a
key and set it (from `src/TodoApi`):

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$key = [System.Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:SigningKey" "$key"
```

**One non-obvious thing worth knowing:** user-secrets are keyed by the `UserSecretsId`
GUID baked into `TodoApi.csproj` (tracked in git), not by the clone's directory. Once
you've set this key for this `UserSecretsId` on a machine, *every* clone of this repo
on that same machine already has it — a second clone won't hit the error above, not
because anything was configured for it specifically, but because the secret store is
shared machine-wide by that GUID. Worth knowing if you're trying to reproduce the
"first run" experience on a machine that already has any copy of this repo on it.

Then:

```
dotnet run
```

Verified end-to-end against a clean clone: `POST /signup` → 201, `POST /auth/login` →
JWT, `POST /todos` with `Authorization: Bearer <token>` → 201, `GET /todos` → the
created todo. `/swagger/index.html` also loads (dev-only, per `Program.cs`).

## 4. Run Portal

Same DB story as TodoApi — no migration at startup, confirmed the same way (a
fresh-clone `POST /Account/Register` 500s with `no such table` until migrated).
Required, every time, on a machine that's never run this repo before:

```
cd src/Portal
dotnet ef database update
```

Google external login needs `Authentication:Google:ClientId`/`ClientSecret` in
user-secrets to actually sign in with Google — but this is optional, not required, to
run the app. Confirmed both states live:

- **Without** those secrets configured: the "Sign in with Google" button does not
  render on `/Account/Login` at all, and a direct `POST /Account/ExternalLogin` with
  `provider=Google` returns a normal page with a "That sign-in provider isn't
  available" message — not a crash. (This required a small fix during this
  verification pass — see `fix(portal): hide Google login when the provider is
  unconfigured` — the button used to render unconditionally and clicking it threw an
  unhandled exception; the Login view and the controller action now both check
  `SignInManager.GetExternalAuthenticationSchemesAsync()` before offering or acting on
  the provider.)
- **With** those secrets configured: the button appears, and submitting it redirects
  to the real `accounts.google.com` OAuth endpoint.

If you want Google login working, from `src/Portal`:

```
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"
```

(Same machine-wide-by-`UserSecretsId` caveat as TodoApi's signing key applies here
too.)

Then:

```
dotnet run --launch-profile https
```

Verified end-to-end against a clean clone: register two users, log in, create a post
(`Posts/Create`) and see it on `Posts/Index`, add a contact (`Contacts/Browse` →
`Contacts/Add`) and see the recipient's notification badge go to 1, start a
conversation (`Conversations/New` → `Conversations/Create`), send a message over
plain HTTP (`Conversations/SendMessage`) and see it persisted on reload, and
authenticate via HTTP Basic (`GET /api/me` with `-u email:password`) → 200.

**Running both apps at the same time:** their `launchSettings.json` `https` profiles
collide — both TodoApi's and Portal's `https` profile bind `https://localhost:7292`
*and* `http://localhost:5245`. Run TodoApi with its default `http` profile
(`dotnet run`, no flag — port 5245 only) alongside Portal's `https` profile
(`dotnet run --launch-profile https` — ports 7292/5036) if you need both running
simultaneously; running both with `--launch-profile https` fails with an
address-already-in-use error.

## Summary of gaps found in existing docs

- `dotnet-ef` global tool install command was assumed, never written down.
- `saas-2026.sln` in `CLAUDE.md`'s repo-layout diagram should read `saas-2026.slnx`.
- User-secrets being keyed machine-wide by `UserSecretsId`, not per-clone, wasn't
  called out anywhere and is easy to misread as "per-repo-copy" the first time you hit
  it on a machine that already has any clone of this repo.
- Running both apps' `https` launch profiles simultaneously collides on ports; wasn't
  documented anywhere.
- The Google-login-absent path 500'd on a direct button click before this pass (fixed
  during this verification, not merely documented as a known gap).
