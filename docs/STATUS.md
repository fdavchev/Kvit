# Status: Kvit

_Last updated: 2026-10-01 (Phase 5, Step 1 done on the branch, not yet committed)_

## Where we stopped
- Branch: `chore/05-first-deploy` (Phase 4 is merged as PR #6). Uncommitted: Step 1 (design-time factory, CI migration steps, 6 tests, docs).
- Last thing done: Phase 5 plan approved. Step 1 built: `AppDbContextFactory` and `UseKvitDatabase`, plus CI steps that check for forgotten migrations and, on `main`, apply migrations to Neon with a bundle. 508/508 backend tests, 0 warnings. Report: `docs/reports/2026-10-01-phase-05-step-1-migration-path.md`. The Phase 5 decision log is at the top of `docs/DECISIONS.md`.

## Next step
Phase 5, Step 2: Filip commits Step 1, moves the local database to Postgres 18, creates the Neon project (Postgres 18, Frankfurt), applies the first migration from his PC and adds the GitHub secret `NEON_DIRECT_CONNECTION_STRING` (before the merge, or CI on `main` goes red on purpose). Claude writes `docs/guides/phase-05-go-online.md` for it. No card anywhere.

## Then
- Test on Filip's real phone: sign up, close the browser, come back still logged in; the phone-only items listed in `docs/ROADMAP.md` Phase 5.
- Prove the visitor-address chain and the proxy secret on the real Cloudflare and Render.
- Phase 6: Google sign-in and the privacy page.
- Phase 7 onward: groups, expenses, balances, then the dashboard (Phase 11, Filip decides its layout, phone and web).

## Blockers and open questions
- Nothing blocks Phase 5 except Filip creating the accounts. No cost may appear (`CLAUDE.md` hard rule).
- Still Filip's decision: whether `START-HERE-PROMPT.md` stays in the public repository.

## Verification state
- Backend: `dotnet test Kvit.slnx` 502/502, `dotnet build` 0 warnings (VERIFIED by automated test, 2026-10-01).
- Frontend: `npm test` 437/437, lint 0 warnings, build 0 type errors (VERIFIED by automated test, 2026-10-01).
- CI on PR #6: `backend` and `frontend` green (VERIFIED by live run).
- Browser: two Chromium runs REPORTED by the tester; Filip's click-through REPORTED by Filip. NOT VERIFIED: a real phone, the real Cloudflare edge, Render's load balancer, Neon, Google (all Phase 5).
