# Phase 5, Step 1: the migration path (report, 2026-10-01)

Branch `chore/05-first-deploy`. Nothing is online yet. This step prepares how database changes will reach Neon.

## What was built
- `UseKvitDatabase`: the Npgsql + snake_case database setup, now in one place (`src/api/Kvit.Infrastructure/Persistence/AppDbContextOptionsExtensions.cs`). The app and the new factory both use it.
- `AppDbContextFactory` (`src/api/Kvit.Api/Persistence/AppDbContextFactory.cs`): lets EF's tools build the database context from only the connection string (user secrets, then environment variables; the environment wins). The certificate is not needed.
- Five new steps at the end of the `backend` job in `.github/workflows/ci.yml`: restore the .NET tools; check that every model change has a migration (every run); on a push to `main` only: check that the secret `NEON_DIRECT_CONNECTION_STRING` exists, build the migration bundle, run the bundle against Neon.
- 6 new tests in `tests/Kvit.Api.Tests/DesignTime/`, written first and failing (the test project did not compile without the new types), then green.

## Results
| Claim | Label |
|---|---|
| `dotnet build Kvit.slnx`: 0 warnings, 0 errors | VERIFIED by automated test (run again by Claude after the coder) |
| `dotnet test Kvit.slnx`: 508 of 508 pass (502 old + 6 new) | VERIFIED by automated test (run again by Claude) |
| The factory builds the same table and column names as the running app, and a model with no changes missing from the migrations | VERIFIED by automated test |
| The factory fails with the clear `ConnectionStrings:KvitDatabase` message when the string is missing | VERIFIED by automated test |
| `has-pending-model-changes` exits 0 with no change and exits 1 after a model change (the change was reverted) | REPORTED by the coder |
| The bundle, built as CI builds it and run with only `ConnectionStrings__KvitDatabase` (no certificate, no user secrets), applied all 3 migrations to a throwaway local database, created 10 tables, and said "already up to date" on a second run; the throwaway database was dropped | REPORTED by the coder |
| The CI YAML parses and the jobs are still `backend` and `frontend` | REPORTED by the coder (Python YAML parse; `actionlint` was not run) |
| The new CI steps on GitHub's Linux runner, the main-only steps, and the run against Neon | NOT VERIFIED (first possible on the push to `main` after the merge, with the secret set) |
| The "Failed executing DbCommand ... __EFMigrationsHistory" line on a first run against an empty database is harmless (EF reads the history table before creating it) | REPORTED by the coder; expect the same line in Neon's first run |

## Facts re-checked against official docs (researcher, 2026-10-01)
- Render "After CI Checks Pass": all checks on the commit must pass (a skipped one counts as passing); a failing check blocks the deploy. VERIFIED (render.com/docs/deploys).
- Neon free: 0.5 GB, 100 compute hours, sleeps after 5 minutes (cannot be turned off), wakes in "a few hundred milliseconds"; Postgres 14 to 18; no card. Pooled string for the app, direct for migrations. VERIFIED.
- EF Core: with a design-time factory and one context, the tools do not run `Program.cs`; Microsoft recommends a bundle for automated deployment; Npgsql locks the history table during migration. VERIFIED from EF source and docs.
- NOT VERIFIED: whether Render's health checks keep a free server awake; whether Render passes custom headers untouched; whether Neon makes a connection wait during wake-up; whether a Cloudflare Pages project needs a card.

## Not covered by a test, on purpose
`CreateDbContext(string[])` (the method EF's tools call) reads user secrets and environment variables, so a test would depend on the PC, and setting environment variables inside xUnit would leak into the other tests. The bundle run above exercised it.
