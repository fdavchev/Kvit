# Status: Kvit

_Last updated: 2026-10-01 (Phase 4 complete and merged; Phase 5 is next)_

## Where we stopped
- Branch: `feat/04-accounts` merged into `main` as PR #6 (2026-10-01); last feature commit `65a6064`. Next branch: `chore/05-first-deploy`.
- Uncommitted changes: none (the wrap-up docs are the last commit before the merge).
- Last thing done: Phase 4 is finished. Database, email accounts, rate limits and the proxy gate, reset by hand with a forced password change, and the frontend (Welcome, Sign up, Log in, Home placeholder, Settings with language and theme, Change password) in the approved look. Filip tested the screens himself and said everything looks fine. Report: `docs/reports/2026-10-01-phase-04-accounts.md` (its last section holds the Phase 4 decision log).

## Next step
Phase 5, first deploy. Claude proposes the plan (Neon, Render, Cloudflare Pages, the shared proxy secret, a separate online certificate, how migrations reach Neon) and waits for Filip's go. Filip then creates the free accounts with `docs/guides/free-hosting-setup.md` (Part A and B1 to B3, B5). No card anywhere.

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
