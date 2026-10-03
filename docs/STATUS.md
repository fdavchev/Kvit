# Status: Kvit

_Last updated: 2026-10-03 (Phase 6 started: branch made, stale Phase 5 docs fixed, plan approved)_

## Where we stopped
- Branch: `feat/06-google-sign-in` (Phase 6, Step 0 done: the stale Phase 5 docs fixed). `chore/05-first-deploy` is merged into `main` as PR #7.
- Uncommitted changes: the Step 0 docs (Filip commits).
- Plan: Step 1 backend (tests first), Step 2a mockup and Macedonian wording, Step 2b frontend, Step 3 online. The decisions so far are in the Phase 6 section of `DECISIONS.md`.
- Before that, Phase 5: the first free deploy. The site `https://kvit-mk.pages.dev` is live on Cloudflare Pages, the API on Render, the database on Neon (Postgres 18). Verified from outside: the proxy gate, the visitor-address chain, the limits, `/health` not waking Neon. Filip checked sign-up, staying logged in after a Render deploy, and his real phone. Added on the way: Postgres 18, the eye button on password boxes, the light/dark button on Welcome, README with screenshots, CI that applies migrations to Neon and deploys to Render (Auto-Deploy is Off). Report: `docs/reports/2026-10-01-phase-05-first-deploy.md` (its last section holds the Phase 5 decision log).

## Next step
1. Filip: Docker Desktop open (the 517 backend tests need it); GitHub About → Website = `https://kvit-mk.pages.dev` if not done.
2. Filip, Google Cloud (guide `docs/guides/free-hosting-setup.md` B4, steps 2–3): make the Web client "Kvit web" with the four origins, add his Gmail as a test user, paste the Client ID (public) in the chat.
3. Phase 6 Step 1: backend (tests first, then the code). Then Step 2a (mockup and the Macedonian lines for Filip's approval), Step 2b (frontend), Step 3 (online).

## Then
- Phase 7 onward: groups, expenses, balances, then the dashboard (Phase 11, Filip decides its layout, phone and web).

## Blockers and open questions
- Nothing blocks Phase 6 Step 1. The Client ID is needed for Step 2b.
- Still Filip's decision: whether `START-HERE-PROMPT.md` stays in the public repository.
- Private notes file `Desktop\secrets\kvit-secrets.txt` (outside the repo) holds the Neon strings, the online certificate pair, the proxy secret and the Render API key. Never lose the certificate pair (everybody would be logged out).

## Verification state
- Backend: build 0 warnings (VERIFIED 2026-10-03). `dotnet test` was 517/517 on Postgres 18 in Phase 5; on 2026-10-03 only the 161 tests that need no Docker ran (Docker Desktop was off), so the full run is NOT VERIFIED again until Docker is open.
- Frontend (VERIFIED by automated test, 2026-10-03): `npm test` 467/467, lint 0 warnings, build 0 type errors.
- Live (VERIFIED by live run): site, Render and Neon answer; `onrender.com` 403 without the secret; made-up address headers ignored (429 on the 11th log-in); sign-up limit 429 on the 6th; `/health` does not wake Neon; first request after a Neon sleep 4.05 s and succeeded.
- VERIFIED by live run (2026-10-03, `gh`): the CI migration step and the Render deploy step ran green on `main` (run 9a4018f; Render deploy `trigger=api`, live in 48 s, `autoDeploy=no`); the test account `phase5-check@example.com` is gone (a log-in answers 401).
- REPORTED by Filip: still logged in after a Render deploy; the real-phone checks; Google saved `kvit-mk.pages.dev` under Authorized domains (no error text). NOT VERIFIED: that Google accepts the address when the button is used (checked in Phase 6).
