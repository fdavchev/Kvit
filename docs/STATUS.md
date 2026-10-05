# Status: Kvit

_Last updated: 2026-10-05 (Phase 7 in progress: Step 4 frontend done; Step 5 is next)_

## Where we stopped
- Branch: `feat/07-groups`, last commit `96c10fc` (Step 3: members and invite backend).
- Uncommitted: Step 4 frontend groups (bottom bar, Groups list, Recently deleted, New group, group screen with the Add people card and Add a name sheet, Group settings with Delete + Undo and Leave, groups service and hooks, error codes, Undo toast helper, colour tokens) with its tests and the Step 4 contract in DECISIONS. Filip commits.
- Done: Step 0 to 4. Left in Phase 7: Step 5 (Members screen, Join screen incl. the logged-out round trip; its Macedonian lines need Filip's OK first), Step 6 (real-browser check, /code-review), Step 7 (report, wrap-up).
- Verification (VERIFIED by automated test, 2026-10-05): `dotnet build` 0 warnings; `dotnet test` 1454/1454; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 1332/1332. NOT VERIFIED: any real-browser look at the new screens (Step 6), the `Groups` migration on Neon.

## Next step
1. Filip commits Step 4.
2. Step 5: propose, confirm the Members and Join Macedonian lines, then tester, then coder.

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
