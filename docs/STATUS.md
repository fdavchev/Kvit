# Status: Kvit

_Last updated: 2026-09-30 (Phase 4, Step 3 done and verified, not committed yet; next: Step 4 frontend)_

## Where we stopped
- **Phase 4 (database + email accounts) is ONE branch, `feat/04-accounts`, and ONE pull request (#6, open).** Filip merges it himself after Step 4 and the phone-width browser test. Plan: top entry of `DECISIONS.md`.
- **Done, committed and pushed:** Step 1 (database), Step 2a (accounts: register, log in, log out, me, lock ladder, login cookie, fallback policy), Step 2b (login keys in Postgres, encrypted with a certificate made by `scripts/NewDataProtectionCertificate.cs`). Reports: `docs/reports/2026-09-29-phase-04-step-1-database.md`, `...step-2a-accounts.md`, `...step-2b-login-keys.md`. CI on PR #6: `backend` and `frontend` green (VERIFIED by live run, 2026-09-30).
- **Step 3 (rate limits, visitor address, proxy secret) is DONE and verified, waiting for Filip's commit:** log-in 10 a minute, sign-up 5 per 10 minutes per address (IPv6 by /64), 429 `RATE_LIMITED` with `Retry-After`, the `Proxy:SharedSecret` gate (403 without the secret except health), `X-Kvit-Visitor-Ip`, the Cloudflare Function change. Report: `docs/reports/2026-09-30-phase-04-step-3-rate-limits.md`. Backend: `dotnet build Kvit.slnx` 0 warnings, `dotnet test Kvit.slnx` 452/452 (was 417); frontend 97/97, lint and build clean; live run with real API + `wrangler pages dev` + echo server passed.
- **Still to do in Phase 4:** Step 4 (frontend: Sign up, Log in, `RequireAuth`, Settings, 401 handling, Macedonian wording approved by Filip).
- **Answered 2026-09-30:** Filip ran the API after making the certificate, so the one row in `data_protection_keys` (id 3) is his own and stays. He asked for the test "a signed-in visitor gets 404 for the developer pages in Production"; it is in and passes.
- Phase 5 still proves the visitor-address chain on the real Cloudflare and Render (including that Render passes both Kvit headers through), sets `Proxy__SharedSecret` on Render and `API_PROXY_SECRET` on Cloudflare to the same value, makes a separate online certificate, decides Neon retry and the migration step; Phase 8 has "every error code has a translation key".
- `START-HERE-PROMPT.md` in a public repo is still Filip's decision. CI running twice on PR branches stays skipped on purpose (`BACKLOG.md`).

## Next step
Step 4: the frontend (Sign up, Log in, `RequireAuth`, Settings with language and log out, a temporary Home screen, 401 handling, screen tests, check at 360 px in EN and MK). Claude proposes it and waits for Filip's go; every new error code (including `RATE_LIMITED`) needs English and Macedonian wording that Filip approves in one list. After that Filip merges PR #6.

## Blockers
- None. Docker Desktop must be on for the tests.

## Verification state
See the step reports in `docs/reports/`. Latest: Step 3, 452/452 backend tests, 0 warnings (Debug and Release), frontend 97/97. CI on PR #6 was green for Steps 1 to 2b; Step 3 is not pushed yet. NOT VERIFIED anywhere yet: the real Cloudflare edge, Render's load balancer, a real phone (Phase 5).
