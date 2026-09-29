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
See the "Step 1" notes in the Phase 4 entry. The main ones: the start-up check first was an `IHostedLifecycleService` and is now one line in `Program.cs` (not `ValidateOnStart`, which throws a different exception type); `usage_events.user_id` is a nullable foreign key with ON DELETE SET NULL; a plain index on `usage_events.user_id` is added on purpose; the role-claims foreign key is named by hand; `AddInfrastructure()` takes no parameter.

## Not verified
- **Why the container exited with code 139 (not 134) when it failed to start without the setting.** The message and the refusal to start were seen; the odd exit code was not looked into. It does not change what Render or you see (the app stops with the clear message), but it is not understood.
- **The tests inside GitHub Actions.** Testcontainers needs Docker on the runner; GitHub's `ubuntu-latest` has it, but this only shows on the PR's CI run.
- The empty `display_name` and `time_zone` are not rejected by the database (only by the handler's validation, Step 2). Step 2 also adds a database check that `display_name` has at least 1 character (DATA-MODEL says 1–60).
- A leftover `testcontainers/ryuk` helper container may show in Docker Desktop for a while; Testcontainers removes it by itself.

## Code review (run after the Step 1 commit, `/code-review` at high effort)
7 findings; none is a crash or a security hole today. Decisions are in `DECISIONS.md` (top entry, "Code review of Step 1"). Labels: findings NOT VERIFIED by a run (read from the code), each checked against the code and plan by Claude.
| Finding | Decision |
|---|---|
| `AppUser` defaults hide missing values (`CreatedAt`, `time_zone`, `display_name`) | Accepted, fixed in Step 2a (validation + database checks) |
| `ix_users_normalized_email` is not unique | Accepted, made unique in Step 2a's migration; one Step 1 test changes |
| No transient-failure retry for Neon | Accepted, decided in Phase 5 (new roadmap line); Step 2a keeps `IUnitOfWork` small |
| Every test class creates a database in the container | Declined (Docker is required from Phase 4 anyway; costs milliseconds) |
| Two overlapping blank-connection-string theories | Fixed after Filip asked: merged into one; tests 292 → 290, all pass, 0 warnings (VERIFIED by automated test) |
| Five empty `IHostedLifecycleService` members | Fixed after Filip asked: the class is replaced by one line in `Program.cs` after `Build()`; 290 of 290 tests pass, 0 warnings, live `dotnet run` with an empty setting prints the clear error and never listens (VERIFIED by automated test and by live run) |
| Unpinned `postgres:17` and passwordless `trust` | Declined; guide now warns never to change `127.0.0.1`; Phase 5 line for Neon on Postgres 17 |

## PR checklist (grows each step; copy into the PR description at the end)
- [x] Step 1: database foundation (Infrastructure, first migration, compose.yaml, Testcontainers setup)
- [ ] Step 2: accounts (Identity, cookie, certificate, register/log in/log out/me)
- [ ] Step 3: rate limiting, visitor address, proxy gate
- [ ] Step 4: frontend (Sign up, Log in, RequireAuth, Settings)

## Commit message for Step 1 (first commit of the Phase 4 PR)
`Add Kvit.Infrastructure, the first migration and real-Postgres tests`

PR title: `Phase 4: database and email accounts`. The PR description is written when the PR is opened (overview, scope, the checklist above). Later commits get one plain line each, and their line is ticked in the PR checklist.
