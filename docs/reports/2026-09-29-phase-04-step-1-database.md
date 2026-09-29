# Phase 4, Step 1: database foundation

Date: 2026-09-29. Branch `feat/04-accounts`. Built by the `tester` (tests first) and the `coder` (code), checked by Claude in the main session.

## Labels used
- **VERIFIED by automated test:** a test command was run and its output read.
- **VERIFIED by live run:** a command was run by hand and its output read.
- **REPORTED by the coder, not repeated:** the coder ran it and reported the result; I did not run it again.
- **NOT VERIFIED:** not run.

## What was built
- New project `Kvit.Infrastructure`: `AppDbContext` (Identity + Data Protection keys + usage events), `AppUser` (Guid v7 id, the Kvit columns and `lockout_count`), one configuration file per table.
- `Kvit.Domain/Entities`: `UsageEvent` with the `SignedUp` factory, `UsageEventType`, `SignUpMethod`.
- First migration `InitialCreate`: `users`, `user_logins`, `user_claims`, `user_tokens`, `roles`, `user_roles`, `role_claims`, `usage_events`, `data_protection_keys`.
- Start-up check: the app refuses to start without `ConnectionStrings:KvitDatabase` and says how to set it. It never connects to the database at start-up, and `/health` never touches it.
- `compose.yaml` (Postgres 17, no password, `127.0.0.1` only), `dotnet-ef` as a project tool, the Dockerfile `COPY` line for `Kvit.Infrastructure`, the local-setup guide part 1.
- Test setup with a real Postgres 17 (Testcontainers), one container per test run.

## Results
| Check | Result | Label |
|---|---|---|
| Tests written first failed | `dotnet build Kvit.slnx`: 37 errors, 0 warnings, every error a missing production name (`AppDbContext`, `AppUser`, `Kvit.Infrastructure`, `Kvit.Domain.Entities`, `SignUpMethod`, Npgsql/EF types) | VERIFIED by live run |
| `dotnet build Kvit.slnx` (Debug) | 0 warnings, 0 errors | VERIFIED by automated test |
| `dotnet build Kvit.slnx --configuration Release` (what CI runs) | 0 warnings, 0 errors | REPORTED by the coder, not repeated |
| `dotnet test Kvit.slnx` | **292 of 292 pass** (was 173: Domain 150 → 161, Api 23 → 131) | VERIFIED by automated test |
| Migration builds all tables, names, constraints, indexes in a real Postgres 17 | passes (108 new Api tests) | VERIFIED by automated test |
| No model changes missing from the migration | test passes; `dotnet ef migrations has-pending-model-changes` said none | VERIFIED by automated test, and REPORTED by the coder for the command |
| Guide flow: compose up, set the secret, `dotnet ef database update`, count tables | container healthy, migration applied, 10 tables (9 + history), then the temporary secret and container were removed | VERIFIED by live run |
| `docker build -f src/api/Dockerfile .` | image builds | VERIFIED by live run |
| Image answers 200 on `/health` and `/api/health` with a wrong database address; without the setting it refuses to start with the clear message | as described | REPORTED by the coder, not repeated |
| Compose binds `127.0.0.1:5432` only | `docker port` showed `127.0.0.1:5432` | REPORTED by the coder, not repeated |
| Frontend | no frontend file was touched (`git status`); checks not re-run | NOT VERIFIED (not needed) |

## Decisions made in this step (also in `DECISIONS.md`)
See the "Step 1" notes in the Phase 4 entry. The main ones: the start-up check is an `IHostedLifecycleService` (not `ValidateOnStart`, which throws a different exception type); `usage_events.user_id` is a nullable foreign key with ON DELETE SET NULL; a plain index on `usage_events.user_id` is added on purpose; the role-claims foreign key is named by hand; `AddInfrastructure()` takes no parameter.

## Not verified
- **Why the container exited with code 139 (not 134) when it failed to start without the setting.** The message and the refusal to start were seen; the odd exit code was not looked into. It does not change what Render or you see (the app stops with the clear message), but it is not understood.
- **The tests inside GitHub Actions.** Testcontainers needs Docker on the runner; GitHub's `ubuntu-latest` has it, but this only shows on the PR's CI run.
- The empty `display_name` and `time_zone` are not rejected by the database (only by the handler's validation, Step 2). Step 2 also adds a database check that `display_name` has at least 1 character (DATA-MODEL says 1–60).
- A leftover `testcontainers/ryuk` helper container may show in Docker Desktop for a while; Testcontainers removes it by itself.

## PR checklist (grows each step; copy into the PR description at the end)
- [x] Step 1: database foundation (Infrastructure, first migration, compose.yaml, Testcontainers setup)
- [ ] Step 2: accounts (Identity, cookie, certificate, register/log in/log out/me)
- [ ] Step 3: rate limiting, visitor address, proxy gate
- [ ] Step 4: frontend (Sign up, Log in, RequireAuth, Settings)

## Commit message for Step 1 (first commit of the Phase 4 PR)
Subject: `Add Kvit.Infrastructure, the first migration and real-Postgres tests`

Body:
```
Add the database layer for Phase 4: the Kvit.Infrastructure project with
AppDbContext, AppUser (Identity with Guid ids) and the first migration
(users, Identity tables, usage_events, data_protection_keys), plus a
compose.yaml for a local Postgres 17.

The app now stops at start-up with a clear message when
ConnectionStrings:KvitDatabase is missing, and never connects to the
database at start-up. Tests run against a real Postgres 17 through
Testcontainers. The Dockerfile copies the new project file so the image
still builds.
```
Later commits on this branch get one plain line each, and their line is ticked in the PR checklist.
