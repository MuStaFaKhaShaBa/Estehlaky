# Estehlaky: testing and CI/CD

## The flow

```
Pull request ──► CI (build, unit tests, API tests, migration check) ──► merge allowed only if green
      │
      ▼ merge to main
Staging workflow: CI again ─► build package ─► apply EF migrations ─► upload to MonsterASP ─► smoke test
      │
      ▼ git tag v1.0.0 && git push origin v1.0.0   (the tagged commit must be on main)
Production workflow: CI again ─► (manual approval) ─► migrations ─► upload ─► smoke test ─► GitHub Release
```

| File | Purpose |
|---|---|
| `.github/workflows/ci.yml` | Runs on every PR and is reused by both deploy flows |
| `.github/workflows/deploy.yml` | Reusable: build, migrate, Web Deploy, smoke test |
| `.github/workflows/staging.yml` | Trigger: push to `main` |
| `.github/workflows/production.yml` | Trigger: tag `v*.*.*` |
| `scripts/setup-github-environments.sh` | Creates the environments and secrets from the mock values |

## Tests

| Project | What it covers | Needs |
|---|---|---|
| `tests/Estehlaky.UnitTests` | `Result`/`Error` types, all FluentValidation validators, MediatR handlers, validation pipeline, `TokenService`, error-code contract, seed files | nothing |
| `tests/Estehlaky.API.Tests` | The real API on a real SQL Server (Testcontainers): register, login, lockout, refresh rotation and reuse detection, logout, forgot/reset/change password, JWT rejection cases, rate limiting, "every anonymous endpoint is on an allow-list", OpenAPI, migrations and seeding | **Docker running** |

```bash
dotnet test tests/Estehlaky.UnitTests
dotnet test tests/Estehlaky.API.Tests      # first run downloads the SQL Server image (~1.5 GB)
```

When you add property/utility endpoints, add the cross-user tests in `tests/Estehlaky.API.Tests/Security/AccessControlTests.cs`
(user A requests user B's data and must get 403/404). The helpers `Client.NewAccountAsync()` and `Api.Clock.Advance(...)` make that short.

## One-time GitHub setup

1. Put the code in a GitHub repository (the zip has no `.git` folder).
2. Fill the mock values in `scripts/setup-github-environments.sh` and run it (needs the `gh` CLI), or create them by hand:

| Name | Type | Example (mock) | Where it comes from |
|---|---|---|---|
| `BASE_URL` | variable | `https://estehlaky-staging.example.com` | your site URL |
| `WEBDEPLOY_SERVER` | secret | `https://site12345.siteasp.net:8172` | MonsterASP panel, Web Deploy details |
| `WEBDEPLOY_SITE` | secret | `site12345` | MonsterASP panel |
| `WEBDEPLOY_USERNAME` / `WEBDEPLOY_PASSWORD` | secret | `site12345` / `********` | MonsterASP panel |
| `DB_CONNECTION_STRING` | secret | `Server=db1234.databaseasp.net;Database=db1234;User Id=db1234;Password=********;TrustServerCertificate=True;` | MonsterASP database page |
| `JWT_KEY` | secret | 32+ random characters (`openssl rand -base64 48`) | you generate it; different per environment |
| `MEDIATR_LICENSE_KEY` | secret, optional | | your MediatR community key |

   Set them **per environment** (`staging`, `production`), not at repository level.
3. In the `production` environment: add **Required reviewers** (yourself) and restrict deployments to tags `v*.*.*`.
4. Branch protection on `main` (Settings > Branches): require a pull request, require the status check **CI result**, block force pushes.

## Things to confirm on MonsterASP (I could not verify these from here)

- The panel shows the Web Deploy server, site name and credentials the workflow expects, and the plan supports .NET 10.
- The SQL Server **accepts remote connections** from outside MonsterASP. The migration step connects from a GitHub runner. If it does not, tell me and I will switch migrations to an idempotent SQL script you run from the panel.
- Web Deploy replaces files of a running site. If you ever see "file in use" errors, add an `app_offline.htm` step before the upload.

## What the pipeline changed in your source (3 small edits)

- `Estehlaky.API/Extensions/RateLimitingExtensions.cs`: the limit of 10 requests/minute is now read from `RateLimiting:AuthPermitLimit` (default stays 10), so tests can raise it. Production behaviour is unchanged.
- `Estehlaky.Infrastructure/Extensions/AppDBContextFactory.cs`: also reads `ConnectionStrings:Default` (the name the app uses) before falling back to `DefaultConnection`.
- `Estehlaky.slnx`: adds the two test projects.

## Issues found in the existing project (not changed, you decide)

1. **Seeded passwords are in `users.json` and `developmentOnly` is `false` for 14 of the 16 users, including `admin@estehlaky.local`.** On an empty staging/production database the app creates these accounts with the passwords stored in the repository. The comment in `SeedUser.cs` says passwords come from configuration, but the code uses the file value. Fix before the first deploy: mark them `developmentOnly: true`, or read the password from `Seed:Passwords:*` as the comment describes.
2. **`appsettings.Production.json` contains a `Jwt:Key` value.** If it is a real key, treat it as leaked: rotate it and keep it only in the `JWT_KEY` secret. The pipeline overwrites it at deploy time and removes the other environments' appsettings files from the package.
3. **Swagger/Scalar are public in every environment.** In `Program.cs` the `if (IsDevelopment())` block is empty, so `/openapi` and `/scalar` are mapped (anonymous) in Production too.
4. **Password rules disagree.** Request validators and the API docs say 8 to 128 characters; Identity requires 10 plus upper, lower, digit and symbol. An 8-character password passes validation and then fails with a `password` field error. The tests document the current behaviour.
5. **`dotnet ef` and the app use different migration-history tables.** The app configures `core.__EFMigrationsHistory`; the design-time factory uses the default `dbo.__EFMigrationsHistory`. It works while the app never calls `Migrate()`, but decide before the first real database exists. (Changing it later means moving the history table.)
6. **Config key names drift.** `appsettings.Production.json` uses `DefaultConnection` and `AccessTokenExpirationMinutes`; the code reads `Default` and `AccessTokenMinutes`. The extra keys are ignored today.
7. No `/health` endpoint exists. The smoke test uses a bogus login instead; a health endpoint with a database check would be a better target.
8. `LoggingEmailService` only logs reset tokens. Password reset will not work in staging/production until a real sender is registered.
