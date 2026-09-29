# Status: Kvit

_Last updated: 2026-09-29 (review-fix branch, Step 2 done)_

## Where we stopped
- Working on branch `fix/review-phase-01-02`: fixing the Phase 1 + 2 code review (`docs/reports/2026-09-29-code-review-phase-01-02.md`), in four steps. Filip commits between steps.
- **Step 1 (docs only) is done:** Phase 4 and Phase 8 checklist lines in `ROADMAP.md`, the `formatMoney` wording in `ARCHITECTURE.md`, the skipped finding 01-5 in `BACKLOG.md`, a dated entry in `DECISIONS.md`, and the "Fix status" table at the end of the review report. Committed by Filip.
- **Step 2 (backend) is done:** Docker build step in `ci.yml`; `BaseController` tests for 400/401/403/404; a real-HTTP test showing a null success answers 204 (the review's "200 empty body" was wrong). Waiting for Filip's commit.
- Phase 3 (money core) is merged to `main` via PR #4; Phase 2 via PR #2.

## Next step
Step 3 of the review fixes (Cloudflare proxy: findings 02-1, 02-2, 02-3 and the proxy tests), then Step 4 (frontend). After all four steps and a green CI, Phase 4 (`feat/04-accounts`) is next; its checklist now carries the deferred review items.

## Blockers and open questions
- None outstanding for Filip.

## Verification state
- Backend (Phase 3, 2026-09-29): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` 165/165 passing (Domain 150, Api 15), after the code-review follow-up — VERIFIED by automated test (Filip re-ran it, 2026-09-29).
- A deliberate break of the leftover rule (always to the first person) made 9 tests fail, then was undone — VERIFIED by live run.
- Frontend (Phase 2): VERIFIED 2026-09-26, unchanged by Phase 3 (no frontend files touched; frontend checks not re-run).
- Backend (review-fix Step 2, 2026-09-29): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` 173/173 (was 165; Api 23, Domain 150) — VERIFIED by automated test. The Docker build was run locally with Docker Desktop: image built, container answered 200 on `/health` and `/api/health` as a non-root user — VERIFIED by live run. The step inside GitHub Actions is NOT VERIFIED until the PR's CI run.
- Review-fix Step 1 changed only `.md` files (checked with `git status`); no build or test was needed and none was run.
