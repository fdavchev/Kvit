# Status: Kvit

_Last updated: 2026-10-03 (Phase 6 complete and merged; Phase 7 is next, after Filip's Google steps)_

## Where we stopped
- Branch: `feat/06-google-sign-in` merged into `main` as PR #8; next branch `feat/07-groups`.
- Uncommitted changes: none (the wrap-up docs are the last commit before the merge).
- Last thing done: Phase 6, Google sign-in and the privacy page. Welcome has Google's own button (always white), a small "or", then the email options and a Privacy link. The API checks Google's ID token; a Google email that already has a password account is never merged (pop-up). First Google sign-in asks the name; Google accounts can add a password in Settings. Public `/privacy` page (EN + MK), security headers from Cloudflare `_headers`, and `scripts/start-local.ps1` (start, `-Check`, `-Stop`). Report: `docs/reports/2026-10-03-phase-06-google-sign-in.md` (its last section holds the Phase 6 decision log).

## Next step
1. **Filip, after the merge** (`docs/guides/phase-06-google.md` Part 2): wait for the green CI on `main` (Render deploy), then Google Auth Platform → Branding (home page and `https://kvit-mk.pages.dev/privacy`), Audience → **Publish app** → In production, then the phone test.
2. **Filip, before or right after:** the real Google sign-in try on localhost (`guides/phase-06-google.md` Part 1b) has not been reported yet.
3. Phase 7, groups, members and invite links (`docs/ROADMAP.md`). Where "friends can be just names" is explained to newcomers (the Welcome line was removed) is decided there.

## Then
- Phase 8 onward: expenses, balances, then the dashboard (Phase 11, Filip decides its layout, phone and web).

## Blockers and open questions
- Nothing blocks Phase 7. Google "In production" is needed before people other than Filip's test users can sign in.
- Still Filip's decision: whether `START-HERE-PROMPT.md` stays in the public repository.
- Private notes file `Desktop\secrets\kvit-secrets.txt` (outside the repo) holds the Neon strings, the online certificate pair, the proxy secret and the Render API key. Never lose the certificate pair (everybody would be logged out). The downloaded Google `client_secret_*.json` in the same folder holds a client secret Kvit never uses; it can be deleted.

## Verification state
- Backend (VERIFIED by automated test, 2026-10-03): build 0 warnings; `dotnet test` 593/593 on Postgres 18.
- Frontend (VERIFIED by automated test, 2026-10-03): `npm test` 778/778, lint 0 warnings, build 0 type errors.
- VERIFIED by live run (2026-10-03): CI green on the PR branch; the CSP, COOP, `nosniff` and referrer headers are on the Cloudflare preview's pages and not on `/api`, and a real browser (headless Chrome) shows no CSP violation or console error on /welcome, /privacy and /login in light and dark; `start-local.ps1` start, stop and `-Check`; the Google button's white box, jump and pill speed (headless Chrome 154, earlier build).
- REPORTED by Filip: the final Welcome look and behaviour in his browser ("everything is alright"); Google saved `kvit-mk.pages.dev` under Authorized domains (Phase 5).
- NOT VERIFIED: the real Google sign-in (localhost and live), that Google's real token passes our audience check, the published app and the phone test.
