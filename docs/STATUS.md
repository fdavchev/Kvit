# Status: Kvit

_Last updated: 2026-10-01 (Phase 5 in progress; the site is live)_

## Where we stopped
- Branch: `chore/05-first-deploy`, PR #7 open, last commit `a36f132 Add the Step 2 report and the Neon guide fixes.`
- Uncommitted changes: the eye button, the light/dark button on Welcome, README + 4 screenshots, the mockup page `docs/design/2026-10-02-welcome-theme-button/`, and the docs (STATUS, ROADMAP, BACKLOG, DECISIONS, the Step 3 report, the Neon guide). Filip commits them next.
- Last thing done: Neon has the 3 migrations; Render (`kvit-mk-api`) and Cloudflare Pages (`kvit-mk`) are live and answer; the proxy gate and the Neon connection are proven from outside. Report: `docs/reports/2026-10-01-phase-05-step-3-online-and-ui.md`.

## Next step (updated: Render deploy via CI built, uncommitted: `scripts/RenderDeploy.cs`, `tests/Kvit.Api.Tests/Scripts/`, `ci.yml`)
Filip: remind him of the **phone test on mobile data** (he shares a hotspot with the laptop; remind him at the start of the next session). Before the merge, do `docs/guides/phase-05-go-online.md` Part 4 in order: GitHub variable `RENDER_SERVICE_ID`, GitHub secret `RENDER_API_KEY` (Claude puts it on his clipboard from the notes file), then Render Auto-Deploy → Off, then merge. Not yet done from the older list below:
Finish Render: open the service → Settings → Health Check Path `/health` and Auto-Deploy "After CI Checks Pass" (not confirmed yet), and check Cloudflare Settings → Variables for both Production and Preview. Then Step 4, the live proofs in `docs/ROADMAP.md` Phase 5: (c) the made-up-header test (11 log-ins with different fake `X-Forwarded-For`, 11th must answer 429), (d) sign up through the site and still logged in after a Render redeploy, (f) the sign-up limit, Neon not woken by `/health`, Render and Neon wake-up times, then the phone checks.

## Then
- Filip's 2-minute Google test: add `kvit-mk.pages.dev` under Authorized domains (guide: `docs/DECISIONS.md` Phase 5 log).
- Fill the «from the session» values in `docs/guides/free-hosting-setup.md`; write the Neon steps in `docs/guides/reset-a-password.md`; GitHub About → Website.
- Last commit before the merge: docs written as merged, the Phase 5 decision log moved to the end of the phase report. Filip adds the PR checklist lines (eye button, light/dark button, Postgres 18) and merges; then check the CI migration step and Render's deploy on `main`.
- Phase 6: Google sign-in and the privacy page.

## Blockers and open questions
- Filip's verdict on how the eye button and the moon/sun button look and feel (he opened both locally).
- Before the merge, the GitHub secret `NEON_DIRECT_CONNECTION_STRING` must stay in place (it exists now).
- Still Filip's decision: whether `START-HERE-PROMPT.md` stays in the public repository.

## Verification state
- Backend: `dotnet test Kvit.slnx` 508/508 on Postgres 18, build 0 warnings (VERIFIED by automated test).
- Frontend: `npm test` 467/467, lint 0 warnings, build 0 type errors (VERIFIED by automated test).
- Live: `kvit-mk.pages.dev` loads; `/api/health` through it 200; onrender.com 403 without the secret; a fake log-in reaches Neon (401) (VERIFIED by live run). NOT VERIFIED: visitor-address chain, sign-up, login after redeploy, limits, phone, CI migration step on `main`, Google.
- Private notes file: `Desktop\secrets\kvit-secrets.txt` (outside the repo) holds the Neon strings, the online certificate and password, and the proxy secret. Never lose the certificate pair.
