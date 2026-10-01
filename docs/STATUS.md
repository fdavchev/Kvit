# Status: Kvit

_Last updated: 2026-10-01 (Phase 4: Steps 1 to 3b committed (3b is `abc1068`); Step 4 run A (frontend plumbing) done and verified, NOT committed yet; run B (screens) next)_

## Where we stopped
- **Phase 4 (database + email accounts) is ONE branch, `feat/04-accounts`, and ONE pull request (#6, open).** Filip merges it himself after Step 4 and the phone-width browser test. Plan: top entry of `DECISIONS.md`.
- **Done, committed and pushed:** Step 1 (database), Step 2a (accounts: register, log in, log out, me, lock ladder, login cookie, fallback policy), Step 2b (login keys in Postgres, encrypted with a certificate made by `scripts/NewDataProtectionCertificate.cs`). Reports: `docs/reports/2026-09-29-phase-04-step-1-database.md`, `...step-2a-accounts.md`, `...step-2b-login-keys.md`. CI on PR #6: `backend` and `frontend` green (VERIFIED by live run, 2026-09-30).
- **Step 3 (rate limits, visitor address, proxy secret) is DONE and committed (`73a642c`).** Report: `docs/reports/2026-09-30-phase-04-step-3-rate-limits.md`. Backend 452/452, 0 warnings; frontend 97/97; live run with the real API + `wrangler pages dev` + echo server passed.
- **Step 3b (password reset by hand, must-change-password, change password) is DONE, verified and committed (`abc1068`).** Report: `docs/reports/2026-10-01-phase-04-step-3b-password-reset.md`. Backend: `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 493/493 (was 452). New: column `must_change_password` (migration `MustChangePassword`), `mustChangePassword` in the account answers, 403 `AUTH_MUST_CHANGE_PASSWORD` while the flag is set, `POST /api/auth/change-password`, security stamp checked on every request, `scripts/ResetPassword.cs` (guide: `docs/guides/reset-a-password.md`). The script was proved on a throwaway database (REPORTED by the coder). **Filip's local database still lacks the new migration: `docs/guides/phase-04-local-setup.md` Part 4, Step 2.**
- **Step 4 run A (frontend plumbing) is DONE and verified, waiting for Filip's commit:** `me` and auth services, `createQueryClient` with the 401/403 handlers, the `me` query, the real `RequireAuth` (spinner, error with retry, redirects), five hooks, all 13 error codes with English and approved Macedonian text. `npm test` 220/220 (was 97), lint 0 warnings, build 0 type errors (VERIFIED by automated test). Decisions: the Step 4 block in `DECISIONS.md`.
- **Still to do in Phase 4:** Step 4 run B (screens: Welcome wiring, Sign up, Log in, Home, Settings with change password, forced change-password screen, back arrow, `KvitTextField`, `KvitLanguageSwitch`, router, translations for the screens in the approved look `docs/design/2026-10-01-round-3/`), then the browser test at 360 px in EN and MK (sign up, log out, log in, wrong password, taken email, language switch, reset by hand then forced change), the report and the merge wrap-up. Approved Macedonian wording: two tables in `DECISIONS.md`; Sign up/Log in/Welcome/Home/Settings texts from the first table are approved too.
- **Answered 2026-09-30:** Filip ran the API after making the certificate, so the one row in `data_protection_keys` (id 3) is his own and stays. He asked for the test "a signed-in visitor gets 404 for the developer pages in Production"; it is in and passes.
- Phase 5 still proves the visitor-address chain on the real Cloudflare and Render (including that Render passes both Kvit headers through), sets `Proxy__SharedSecret` on Render and `API_PROXY_SECRET` on Cloudflare to the same value, makes a separate online certificate, decides Neon retry and the migration step; Phase 8 has "every error code has a translation key".
- `START-HERE-PROMPT.md` in a public repo is still Filip's decision. CI running twice on PR branches stays skipped on purpose (`BACKLOG.md`).

## Next step
Step 4 run B: the screens, in the approved look. Claude proposes the file list, then the tester writes failing screen tests (EN and MK), Filip says go, the coder builds. The commit of run A is Filip's: the commit line is in the chat.

## Blockers
- None. Docker Desktop must be on for the tests.

## Verification state
See the step reports in `docs/reports/`. Latest: backend 493/493 tests, 0 warnings (Debug and Release); frontend 220/220 tests, lint and build clean. NOT VERIFIED anywhere yet: any screen in a real browser, the real Cloudflare edge, Render's load balancer, a real phone (Phase 5).
