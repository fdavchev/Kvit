# Status: Kvit

_Last updated: 2026-09-29 (Phase 4, Step 2b done and verified; waiting for Filip to run the certificate script, commit, and say go for Step 3)_

## Where we stopped
- **Phase 4 (database + email accounts) runs on branch `feat/04-accounts`, one step at a time; Filip commits after each step.** Plan: top entry of `DECISIONS.md`. Steps: 1 database (committed), 2a accounts (committed), 2b login keys with a certificate (**done, not committed yet**), 3 rate limiting + visitor address + proxy gate, 4 frontend.
- **Step 2b report:** `docs/reports/2026-09-29-phase-04-step-2b-login-keys.md`. Backend: `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 417/417 (was 403).
- **Filip's part of 2b:** run the certificate script once, check the API starts, commit (`docs/guides/phase-04-local-setup.md`, Part 3). Until he runs the script, his local API and `dotnet ef` refuse to start with a clear message.
- Still open in Phase 4: rate limit on the forwarded visitor address + proxy gate (3), the frontend (4). Phase 5 proves the visitor-address chain on the real Cloudflare and Render, makes a separate online certificate, and decides Neon retry; Phase 8 has "every error code has a translation key".
- `START-HERE-PROMPT.md` in a public repo is still Filip's decision. CI running twice on PR branches stays skipped on purpose (`BACKLOG.md`).

## Next step
Step 3: per-IP rate limits on log-in and sign-up, the visitor address read from a header only the Cloudflare proxy can set (secret header, gate for everything except health), the proxy function change and its tests. Claude proposes it and waits for Filip's go.

## Blockers and open questions
- None for Filip, except the script, the check and the commit.

## Verification state
See the step reports in `docs/reports/`. Latest: Step 2b, 417/417 backend tests, 0 warnings (Debug and Release), Docker image builds, the Linux restart check (reported by the coder). Frontend last checked in the Phase 1 + 2 review fixes (89/89, lint and build clean); not touched since. NOT VERIFIED anywhere yet: the real Cloudflare edge, Render's load balancer, a real phone (Phase 5).
