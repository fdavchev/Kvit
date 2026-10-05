# Status: Kvit

_Last updated: 2026-10-05 (Phase 7 in progress: Step 5 frontend members and join done; Step 6 is next)_

## Where we stopped
- Branch: `feat/07-groups`. Step 4 and the follow-ups are committed or ready; uncommitted now: Step 5 (Members screen, Join screen, invite round trip through Sign up, Log in and the Google name screen, 9 more error codes, the "линк" wording) with its tests and the Step 5 contract in DECISIONS. Filip commits.
- Done: Step 0 to 5. Left in Phase 7: Step 6 (real-browser check at 360 px and wide, light/dark, EN/MK; /code-review), Step 7 (phase report, decision log moved, ROADMAP/STATUS).
- Verification (VERIFIED by automated test, 2026-10-05): `npm run lint` exit 0, `npm run build` exit 0, `npm test` 1862/1862. Backend unchanged since the baseline (1454/1454). NOT VERIFIED: any real-browser look at the group, Members and Join screens (Step 6); Google sign-in on the join card refreshing the preview (checked once by a throw-away test, no permanent test).

## Next step
1. Filip commits Step 5.
2. Step 6: real-browser check (debugger, headless Chrome, scratch profile), then /code-review.

## Then
- Phase 8 onward: expenses, balances, then the dashboard (Phase 11, Filip decides its layout, phone and web).

## Blockers and open questions
- Nothing blocks Phase 7.
- Still Filip's decision: whether `START-HERE-PROMPT.md` stays in the public repository.
- Private notes file `Desktop\secrets\kvit-secrets.txt` (outside the repo) holds the Neon strings, the online certificate pair, the proxy secret and the Render API key. Never lose the certificate pair (everybody would be logged out). The downloaded Google `client_secret_*.json` in the same folder holds a client secret Kvit never uses; it can be deleted.

## Verification state
- Backend (VERIFIED by automated test, 2026-10-03): build 0 warnings; `dotnet test` 593/593 on Postgres 18.
- Frontend (VERIFIED by automated test, 2026-10-03): `npm test` 778/778, lint 0 warnings, build 0 type errors.
- VERIFIED by live run (2026-10-03): CI green on the PR branch; the CSP, COOP, `nosniff` and referrer headers are on the Cloudflare preview's pages and not on `/api`, and a real browser (headless Chrome) shows no CSP violation or console error on /welcome, /privacy and /login in light and dark; `start-local.ps1` start, stop and `-Check`; the Google button's white box, jump and pill speed (headless Chrome 154, earlier build).
- REPORTED by Filip: the final Welcome look and behaviour in his browser ("everything is alright"); the real Google sign-in and the phone test ("I did Google and the phone test", no per-item notes); Google saved `kvit-mk.pages.dev` under Authorized domains (Phase 5).
- REPORTED by Filip: the Google app is "In production" (Branding home page and privacy link saved, terms link left empty).
- NOT VERIFIED: a sign-in by a Google account that is not on the old test-user list (nobody else has tried yet).
