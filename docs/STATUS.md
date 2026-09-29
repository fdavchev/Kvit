# Status: Kvit

_Last updated: 2026-09-29 (Phase 4, Step 2a done and verified; waiting for Filip to commit and to say go for Step 2b)_

## Where we stopped
- **Phase 4 (database + email accounts) runs on branch `feat/04-accounts`, one step at a time; Filip commits after each step.** Plan: top entry of `DECISIONS.md`. Steps: 1 database (done, committed), 2a accounts (**done, not committed yet**), 2b Data Protection keys in Postgres with a certificate, 3 rate limiting + visitor address + proxy gate, 4 frontend.
- **Step 2a report:** `docs/reports/2026-09-29-phase-04-step-2a-accounts.md`. Backend: `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 403/403 (was 290).
- **Filip's part of 2a:** run `dotnet ef database update ...` once (`docs/guides/phase-04-local-setup.md`, Part 2), then commit.
- Still open in Phase 4: Data Protection keys and certificate (2b), the rate limit on the forwarded visitor address (3), the frontend (4). Phase 5 proves the visitor-address chain on the real Cloudflare and Render; Phase 8 has "every error code has a translation key".
- `START-HERE-PROMPT.md` in a public repo is still Filip's decision. CI running twice on PR branches stays skipped on purpose (`BACKLOG.md`).

## Next step
Step 2b: a small .NET script that makes the certificate and saves it in `dotnet user-secrets`, Data Protection keys stored in Postgres and encrypted with it, tests (keys encrypted, a cookie still works in a second app instance on the same database, start-up fails clearly without the certificate). Claude proposes it and waits for Filip's go.

## Blockers and open questions
- None for Filip, except the migration command and the commit.

## Verification state
See the step reports in `docs/reports/`. Latest: Step 2a, 403/403 backend tests, 0 warnings, Docker image builds. Frontend last checked in the Phase 1 + 2 review fixes (89/89, lint and build clean); not touched since. NOT VERIFIED anywhere yet: the real Cloudflare edge, Render's load balancer, a real phone (Phase 5).
