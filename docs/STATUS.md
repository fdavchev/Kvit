# Status: Kvit

_Last updated: 2026-09-29 (Phase 4, Step 1 done and verified; waiting for Filip to commit and to say go for Step 2)_

## Where we stopped
- **Phase 4 (database + email accounts) is running on branch `feat/04-accounts`, one step at a time; Filip commits after each step.** The plan: `DECISIONS.md` (top entry) and the plan file behind it. Steps: 1 database foundation (**done**), 2 accounts, 3 rate limiting + visitor address + proxy gate, 4 frontend.
- **Step 1 is built and verified** (report: `docs/reports/2026-09-29-phase-04-step-1-database.md`): `Kvit.Infrastructure` (AppDbContext, AppUser, first migration), `usage_events` and the `SignedUp` event maker, `compose.yaml` (local Postgres 17), a start-up check for the connection string, the Dockerfile `COPY` line, the real-Postgres test setup. `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 292/292 (was 173).
- **Filip has not yet done his part of Step 1** (start the local database, set the connection-string secret, run the migration): `docs/guides/phase-04-local-setup.md`, part 1.
- Deferred Phase 4 lines still open: `Kvit.Contracts` and its Dockerfile line, the handler-scan test, the fallback authorization policy, the cookie flags test, the per-IP rate limit on the forwarded visitor address (Steps 2 and 3). Phase 5 has the proof that the visitor-address chain works on the real Cloudflare and Render; Phase 8 has "every error code has a translation key".
- `START-HERE-PROMPT.md` in a public repo is still Filip's decision. CI running twice on PR branches stays skipped on purpose (`BACKLOG.md`).

## Next step
Step 2 (accounts): `Kvit.Contracts`, Identity, the login cookie, Data Protection keys in Postgres with a certificate script, the fallback policy, register / log in / log out / me, the `SignedUp` event, the Scrutor handler test. Claude proposes it in detail and waits for Filip's go before the tester and coder run.

## Blockers and open questions
- None outstanding for Filip, except doing his part of Step 1 (the guide) and committing.

## Verification state
- Backend (Phase 4, Step 1, 2026-09-29): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` 292/292 (Domain 161, Api 131) at the Step 1 commit; 290/290 after the code-review tidy that merged two overlapping start-up tests (Api 129) — VERIFIED by automated test. The guide flow (compose up, user secret, `dotnet ef database update`, 10 tables) and `docker build -f src/api/Dockerfile .` — VERIFIED by live run. The container answering `/health` with a wrong database address and refusing to start without the setting, the Release build, and the `127.0.0.1`-only port — reported by the coder, not repeated. NOT VERIFIED: the tests inside GitHub Actions (Testcontainers needs Docker on the runner); why a failed container start exits with code 139.
- Frontend (Phase 2 + review fixes, 2026-09-29): `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 89/89 — VERIFIED by automated test; no frontend file was touched in Step 1 and the checks were not re-run. Proxy checks with `wrangler pages dev` — VERIFIED by live run (review-fix Step 3). NOT VERIFIED: the real Cloudflare edge and Render's load balancer (Phase 5).
- Earlier phases: Phase 3 money core merged via PR #4; the Phase 1 + 2 review fixes merged via PR #5 (CI green on `main`, including the Docker build step).
