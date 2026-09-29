# Status: Kvit

_Last updated: 2026-09-29 (review fixes merged to `main`)_

## Where we stopped
- The Phase 1 + 2 code review fixes are merged to `main` via PR #5 (`fix/review-phase-01-02`). CI is green on `main`, including the new Docker build step (checked with `gh run view`, run 36601325663). Findings and results: `docs/reports/2026-09-29-code-review-phase-01-02.md` ("Fix status" table).
- Deferred on purpose, now lines in the Phase 4 checklist in `ROADMAP.md`: the handler-scan test (01-2), the fallback authorization policy (01-3), the auth cookie flags (02-9), the Dockerfile `COPY` lines for the new projects, and the per-IP rate limit reading the forwarded visitor address. Phase 8 has the "every error code has a translation key" check; Phase 5 has the proof that the visitor-address chain works on the real Cloudflare.
- Skipped on purpose: CI running twice on PR branches (01-5, `BACKLOG.md`). `START-HERE-PROMPT.md` in a public repo is still Filip's decision.
- Phase 3 (money core) is merged via PR #4; Phase 2 via PR #2.

## Next step
Start Phase 4 (database + email accounts): branch `feat/04-accounts`, per `docs/ROADMAP.md`. Docker Desktop must be running first. Read the Phase 4 checklist and the Cloudflare/forwarded-address trap in `docs/ARCHITECTURE.md` before designing rate limiting.

## Blockers and open questions
- None outstanding for Filip.

## Verification state
- Backend (Phase 3, 2026-09-29): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` 165/165 passing (Domain 150, Api 15), after the code-review follow-up — VERIFIED by automated test (Filip re-ran it, 2026-09-29).
- A deliberate break of the leftover rule (always to the first person) made 9 tests fail, then was undone — VERIFIED by live run.
- Frontend (Phase 2): VERIFIED 2026-09-26, unchanged by Phase 3 (no frontend files touched; frontend checks not re-run).
- Backend (review-fix Step 2, 2026-09-29): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` 173/173 (was 165; Api 23, Domain 150) — VERIFIED by automated test. The Docker build was run locally with Docker Desktop: image built, container answered 200 on `/health` and `/api/health` as a non-root user — VERIFIED by live run. The step also ran green inside GitHub Actions on `main` after the merge — VERIFIED by live run.
- Frontend (review-fix Step 3, 2026-09-29): `npm run lint` 0 warnings, 0 errors; `npm run build` 0 type errors; `npm test` 72/72 (was 52; proxy tests 8 -> 27, errors 3 -> 4) — VERIFIED by automated test. Live run with `wrangler pages dev`: spoofed headers dropped, stopped API gives 502 — VERIFIED by live run. NOT VERIFIED: the real Cloudflare edge and Render's load balancer (Phase 4/5 lines).
- Frontend (review-fix Step 4, 2026-09-29): `npm run lint` 0 warnings, 0 errors; `npm run build` 0 type errors; `npm test` 89/89 (was 72) — VERIFIED by automated test. Chrome at 360 px: Welcome in EN and MK, language switch saved `mk`, no horizontal scroll, start-up fallback drawn — VERIFIED by live run. NOT VERIFIED: the error page and the failed-language toast in a real browser.
- Review-fix Step 1 changed only `.md` files (checked with `git status`); no build or test was needed and none was run.
