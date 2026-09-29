# Status: Kvit

_Last updated: 2026-09-29 (review-fix branch, Step 4 done)_

## Where we stopped
- Working on branch `fix/review-phase-01-02`: fixing the Phase 1 + 2 code review (`docs/reports/2026-09-29-code-review-phase-01-02.md`), in four steps. Filip commits between steps.
- **Step 1 (docs only) is done:** Phase 4 and Phase 8 checklist lines in `ROADMAP.md`, the `formatMoney` wording in `ARCHITECTURE.md`, the skipped finding 01-5 in `BACKLOG.md`, a dated entry in `DECISIONS.md`, and the "Fix status" table at the end of the review report. Committed by Filip.
- **Step 2 (backend) is done:** Docker build step in `ci.yml`; `BaseController` tests for 400/401/403/404; a real-HTTP test showing a null success answers 204 (the review's "200 empty body" was wrong). Committed by Filip.
- **Step 3 (Cloudflare proxy) is done:** the proxy drops visitor-sent forwarding headers and sets `X-Forwarded-For` from `cf-connecting-ip`, only forwards `/api/` paths, answers 502 when the API can't be reached (and the frontend shows "Can't reach the server" for it); 19 new proxy tests. Committed by Filip.
- **Step 4 (frontend) is done:** an error page for crashed screens and a plain two-language fallback if the app can't start; unmapped API error codes and failed health pings are logged; language switching saves only after it worked and shows a toast when it fails; `src/shared/utils/cn.ts` now exists for the shadcn alias. New dev tools: `jsdom` and Testing Library. Waiting for Filip's commit, then CI on the PR.
- Phase 3 (money core) is merged to `main` via PR #4; Phase 2 via PR #2.

## Next step
Commit Step 4, push, open the pull request, check that CI is green (that also runs the new Docker build step for the first time), and merge it yourself. Then Phase 4. After all four steps and a green CI, Phase 4 (`feat/04-accounts`) is next; its checklist now carries the deferred review items.

## Blockers and open questions
- None outstanding for Filip.

## Verification state
- Backend (Phase 3, 2026-09-29): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` 165/165 passing (Domain 150, Api 15), after the code-review follow-up — VERIFIED by automated test (Filip re-ran it, 2026-09-29).
- A deliberate break of the leftover rule (always to the first person) made 9 tests fail, then was undone — VERIFIED by live run.
- Frontend (Phase 2): VERIFIED 2026-09-26, unchanged by Phase 3 (no frontend files touched; frontend checks not re-run).
- Backend (review-fix Step 2, 2026-09-29): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` 173/173 (was 165; Api 23, Domain 150) — VERIFIED by automated test. The Docker build was run locally with Docker Desktop: image built, container answered 200 on `/health` and `/api/health` as a non-root user — VERIFIED by live run. The step inside GitHub Actions is NOT VERIFIED until the PR's CI run.
- Frontend (review-fix Step 3, 2026-09-29): `npm run lint` 0 warnings, 0 errors; `npm run build` 0 type errors; `npm test` 72/72 (was 52; proxy tests 8 -> 27, errors 3 -> 4) — VERIFIED by automated test. Live run with `wrangler pages dev`: spoofed headers dropped, stopped API gives 502 — VERIFIED by live run. NOT VERIFIED: the real Cloudflare edge and Render's load balancer (Phase 4/5 lines).
- Frontend (review-fix Step 4, 2026-09-29): `npm run lint` 0 warnings, 0 errors; `npm run build` 0 type errors; `npm test` 89/89 (was 72) — VERIFIED by automated test. Chrome at 360 px: Welcome in EN and MK, language switch saved `mk`, no horizontal scroll, start-up fallback drawn — VERIFIED by live run. NOT VERIFIED: the error page and the failed-language toast in a real browser.
- Review-fix Step 1 changed only `.md` files (checked with `git status`); no build or test was needed and none was run.
