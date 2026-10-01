# Phase 5, Step 3: Render, Cloudflare Pages, and the added screens (report, 2026-10-01)

Branch `chore/05-first-deploy`, PR #7 (open). Steps 1 and 2 are in their own reports (`2026-10-01-phase-05-step-1-migration-path.md`, `2026-10-01-phase-05-step-2-neon.md`).

## What was done
- **Online secrets:** the online certificate (script `scripts/NewDataProtectionCertificate.cs` into a throwaway project) and the proxy secret were made and appended to Filip's private notes file; the temporary secrets store was deleted. Values were put on Filip's clipboard by commands and pasted by him; none appeared in the chat.
- **Render:** service `kvit-mk-api` (Frankfurt, Docker, `main`, Free) created by Filip with five environment variables (port, pooled Neon string, certificate, certificate password, proxy secret).
- **Cloudflare Pages:** project `kvit-mk` created by Filip (root `src/web`, `API_ORIGIN`, `NODE_VERSION=24`, `API_PROXY_SECRET`) through the "legacy Pages workflow" link.
- **Added scope (approved by Filip):** show/hide eye button on every password box (`KvitPasswordToggle`), light/dark button on the Welcome screen next to EN/МК (`KvitThemeToggle`, design A of three), local database and tests moved to Postgres 18.
- **README** rewritten with the live link and 4 real screenshots (`docs/screenshots/`); 4 spare screenshots deleted at Filip's request.

## Results
| Claim | Label |
|---|---|
| `https://kvit-mk-api.onrender.com/health` and `/api/health` answer 200 (0.4 s) | VERIFIED by live run |
| `onrender.com/api/auth/me` without the proxy secret answers 403 | VERIFIED by live run |
| A fake log-in with the secret and a made-up visitor address answers 401 (the API reached Neon through the pooled string, certificate and secrets accepted) | VERIFIED by live run |
| `https://kvit-mk.pages.dev/` serves the app (200, HTML) | VERIFIED by live run |
| `kvit-mk.pages.dev/api/health` answers 200 and `/api/auth/me` answers 401 (passed the proxy gate) | VERIFIED by live run |
| GitHub secret `NEON_DIRECT_CONNECTION_STRING` exists, push protection is enabled, CI green on the branch | VERIFIED by live run |
| Backend: build 0 warnings, 508/508 tests on Postgres 18 | VERIFIED by automated test |
| Frontend: 467/467 tests, lint 0 warnings, build 0 type errors | VERIFIED by automated test |
| Eye button and light/dark button look and animation in a real browser; Filip opened both pages locally | REPORTED by the browser opening only; Filip's verdict NOT recorded |
| Screenshots are real pages of the running app (checked two by eye) | VERIFIED by live run |
| Render settings Health Check Path `/health` and Auto-Deploy "After CI Checks Pass" | NOT VERIFIED (Filip's step, not confirmed) |
| Cloudflare variables set for Production AND Preview | NOT VERIFIED |
| Visitor-address chain: 12 log-ins through the site, each with different made-up `X-Forwarded-For`, `X-Kvit-Visitor-Ip`, `True-Client-IP`, `X-Real-IP`: #1 to #10 answered 401, #11 and #12 answered 429 (the API counted one real address and ignored the fakes; Render passes the proxy's headers through) | VERIFIED by live run |
| A made-up `CF-Connecting-IP` sent through the site is refused by Cloudflare itself (403, "error code: 1000"); Cloudflare's error-1000 page lists "The request includes a CF-Connecting-IP header" as a cause | VERIFIED by live run and by Cloudflare's docs |
| Sign-up limit: sign-up #1 created an account (200), #2 to #5 answered 400 `AUTH_EMAIL_TAKEN`, #6 and #7 answered 429 `RATE_LIMITED` (5 counted in 10 minutes, as decided) | VERIFIED by live run (test account `phase5-check@example.com` now exists in Neon) |
| Still logged in on `kvit-mk.pages.dev` after a manual Render deploy (proves the login keys live in Postgres) | REPORTED by Filip (his browser session); the API log he pasted shows the cookie's user being loaded from Neon |
| Cookie round trip from curl (sign-up then `/api/auth/me`) | NOT VERIFIED (my curl cookie jar was overwritten by the later requests; a test mistake, not an app result) |
| Neon not woken by `/health`, wake-up times of Render and Neon, sign-up on a real phone | NOT VERIFIED (still to do) |
| The `libgssapi_krb5.so.2` line in Render's log is harmless | VERIFIED indirectly (the 401 above); cause NOT VERIFIED from docs |
| Migration step on `main` in CI | NOT VERIFIED (first run at the merge) |
| Google accepts `kvit-mk.pages.dev` as an authorized domain | NOT VERIFIED |
| Real phone checks (status bar, zoom, keyboard, notch, tap flash, 44 px, theme default) | NOT VERIFIED |

## Render deploy driven by CI (added scope, built after the live proofs)
| Claim | Label |
|---|---|
| The Render API works on the free plan: a read-only list of the service's deploys answered; service `kvit-mk-api`, plan `free`, `autoDeployTrigger` = `checksPass` (confirms the Auto-Deploy setting) | VERIFIED by live run |
| `scripts/RenderDeploy.cs` `--watch-latest` run against the real Render service printed `deploy dep-...: live` and exited 0 (real response shapes accepted) | VERIFIED by live run |
| 9 tests against a fake Render server (live after polling, build_failed, unknown status, 401, timeout, missing key, 202 queued, watch-latest, key never printed); full suite 517/517, build 0 warnings | VERIFIED by automated test |
| The trigger call (`POST .../deploys` with `commitId`) against the real Render | NOT VERIFIED (first run at the merge; Auto-Deploy must be Off) |
| The three new CI steps on GitHub's runner | NOT VERIFIED (first run at the merge) |
| The line shows on the commit on `main`, not in the pull-request box | VERIFIED in GitHub's docs |

## Notes
- A first attempt to start the local API failed only because it ran without Development mode (no user secrets); fixed by setting `ASPNETCORE_ENVIRONMENT=Development`. The local API and dev server were stopped at the end.
- The coder added 5 lines to `router.test.tsx` (stub of the device colour scheme); no assertion changed.
