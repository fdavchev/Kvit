# Status: Kvit

_Last updated: 2026-10-03 (Phase 6 started: branch made, stale Phase 5 docs fixed, plan approved)_

## Where we stopped
- Branch: `feat/06-google-sign-in` (Phase 6, Step 0 done: the stale Phase 5 docs fixed). `chore/05-first-deploy` is merged into `main` as PR #7.
- Step 2a (mockup, texts approved) done. Step 2b (frontend) is built: 752/752 frontend tests, lint 0 warnings, build 0 type errors (VERIFIED by automated test, 2026-10-03). **Not yet seen with the real Google button** (Filip's local try, `guides/phase-06-google.md` Part 1b) **and the security headers are not yet seen on Cloudflare** (the preview deploy of the pushed branch).
- Step 1 (backend) is built and green: 593/593 backend tests, 0 warnings, no migration needed (VERIFIED by automated test, 2026-10-03). Uncommitted: Step 1 code and tests, deleting the Step 0 report. A garbage token (empty, `a.b.c`, non-JSON parts) answers 401 `AUTH_GOOGLE_TOKEN_INVALID`; a failure to reach Google's keys stays a 500.
- Plan: Step 1 backend (tests first), Step 2a mockup and Macedonian wording, Step 2b frontend, Step 3 online. The decisions so far are in the Phase 6 section of `DECISIONS.md`.
- Before that, Phase 5: the first free deploy. The site `https://kvit-mk.pages.dev` is live on Cloudflare Pages, the API on Render, the database on Neon (Postgres 18). Verified from outside: the proxy gate, the visitor-address chain, the limits, `/health` not waking Neon. Filip checked sign-up, staying logged in after a Render deploy, and his real phone. Added on the way: Postgres 18, the eye button on password boxes, the light/dark button on Welcome, README with screenshots, CI that applies migrations to Neon and deploys to Render (Auto-Deploy is Off). Report: `docs/reports/2026-10-01-phase-05-first-deploy.md` (its last section holds the Phase 5 decision log).

## Next step
1. Filip: Docker Desktop open (the 517 backend tests need it); GitHub About → Website = `https://kvit-mk.pages.dev` if not done.
2. Google client "Kvit web" exists (project `kvit-510321`, four origins match, checked in the downloaded JSON 2026-10-03; the Client ID is in `appsettings.json`). Still open: Audience status and test user (`docs/guides/phase-06-google.md`).
3. Phase 6 Step 1 is built; next Step 2a (mockup and the Macedonian lines for Filip's approval), Step 2b (frontend), Step 3 (online).

## Then
- Phase 7 onward: groups, expenses, balances, then the dashboard (Phase 11, Filip decides its layout, phone and web).

## Blockers and open questions
- Nothing blocks Phase 6 Step 1. The Client ID is needed for Step 2b.
- Still Filip's decision: whether `START-HERE-PROMPT.md` stays in the public repository.
- Private notes file `Desktop\secrets\kvit-secrets.txt` (outside the repo) holds the Neon strings, the online certificate pair, the proxy secret and the Render API key. Never lose the certificate pair (everybody would be logged out).

## Verification state
- Backend (VERIFIED by automated test, 2026-10-03): build 0 warnings; `dotnet test` 593/593 on Postgres 18 (517 before Phase 6 plus 76 new).
- Frontend (VERIFIED by automated test, 2026-10-03): `npm test` 467/467, lint 0 warnings, build 0 type errors.
- Live (VERIFIED by live run): site, Render and Neon answer; `onrender.com` 403 without the secret; made-up address headers ignored (429 on the 11th log-in); sign-up limit 429 on the 6th; `/health` does not wake Neon; first request after a Neon sleep 4.05 s and succeeded.
- VERIFIED by live run (2026-10-03, `gh`): the CI migration step and the Render deploy step ran green on `main` (run 9a4018f; Render deploy `trigger=api`, live in 48 s, `autoDeploy=no`); the test account `phase5-check@example.com` is gone (a log-in answers 401).
- REPORTED by Filip: still logged in after a Render deploy; the real-phone checks; Google saved `kvit-mk.pages.dev` under Authorized domains (no error text). NOT VERIFIED: that Google accepts the address when the button is used (checked in Phase 6).
