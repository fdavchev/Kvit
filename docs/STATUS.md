# Status: Kvit

_Last updated: 2026-10-01 (Phase 5 complete and merged; Phase 6 is next)_

## Where we stopped
- Branch: `chore/05-first-deploy` merged into `main` as PR #7; next branch `feat/06-google-sign-in`.
- Uncommitted changes: none (the wrap-up docs are the last commit before the merge).
- Last thing done: Phase 5, the first free deploy. The site `https://kvit-mk.pages.dev` is live on Cloudflare Pages, the API on Render, the database on Neon (Postgres 18). Verified from outside: the proxy gate, the visitor-address chain, the limits, `/health` not waking Neon. Filip checked sign-up, staying logged in after a Render deploy, and his real phone. Added on the way: Postgres 18, the eye button on password boxes, the light/dark button on Welcome, README with screenshots, CI that applies migrations to Neon and deploys to Render (Auto-Deploy is Off). Report: `docs/reports/2026-10-01-phase-05-first-deploy.md` (its last section holds the Phase 5 decision log).

## Next step
1. Check the first CI run on `main` (GitHub → Actions): the migration step and **Deploy to Render and wait until it is live** must be green. These ran for the first time at the merge (NOT VERIFIED until seen). If the Render step fails, set Render's Auto-Deploy back to "After CI Checks Pass", remove the step, and use Render's email notifications.
2. Filip: GitHub About → Website = `https://kvit-mk.pages.dev`; delete the test account `phase5-check@example.com` in Neon before real users arrive.
3. Phase 6, Google sign-in and the privacy page. First Filip's 2-minute test: Google Auth Platform → Branding → Authorized domains → `kvit-mk.pages.dev` (if Google refuses it, the domain question comes back to Filip). Then the plan in `docs/ROADMAP.md` Phase 6 and the guide `docs/guides/free-hosting-setup.md` B4.

## Then
- Phase 7 onward: groups, expenses, balances, then the dashboard (Phase 11, Filip decides its layout, phone and web).

## Blockers and open questions
- Nothing blocks Phase 6 except the Google domain test above.
- Still Filip's decision: whether `START-HERE-PROMPT.md` stays in the public repository.
- Private notes file `Desktop\secrets\kvit-secrets.txt` (outside the repo) holds the Neon strings, the online certificate pair, the proxy secret and the Render API key. Never lose the certificate pair (everybody would be logged out).

## Verification state
- Backend: `dotnet test Kvit.slnx` 517/517 on Postgres 18, build 0 warnings (VERIFIED by automated test).
- Frontend: `npm test` 467/467, lint 0 warnings, build 0 type errors (VERIFIED by automated test).
- Live (VERIFIED by live run): site, Render and Neon answer; `onrender.com` 403 without the secret; made-up address headers ignored (429 on the 11th log-in); sign-up limit 429 on the 6th; `/health` does not wake Neon; first request after a Neon sleep 4.05 s and succeeded.
- REPORTED by Filip: still logged in after a Render deploy; the real-phone checks. NOT VERIFIED: the CI migration and deploy steps on `main`, Google's domain check.
