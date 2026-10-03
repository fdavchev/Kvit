# Status: Kvit

_Last updated: 2026-10-03 (Phase 7 in progress: backend done through Step 3; Step 4 frontend is next)_

## Where we stopped
- Branch: `feat/07-groups`, last commit `4ad5370` (Step 2: groups backend).
- Uncommitted (Step 3, all green): `RunInTransactionAsync` helper (7 handlers switched), members and invites backend (add name, remove, let back in, leave, make owner, claim "That's me", undo claim, reset and undo link, preview, join, `invite` rate limit 20 per 10 min), and its tests. Docs edited: DECISIONS (Step 3 contract), ARCHITECTURE (helper). Not committed: Filip commits.
- Done: Step 0 to 3. The mockup `docs/design/2026-10-03-groups/` is approved except some Macedonian lines.
- Verification (VERIFIED by automated test, 2026-10-03): `dotnet build` 0 warnings; `dotnet test` 1454/1454 (593 before Phase 7); migration guard exits 0 and Step 3 needs no migration. NOT VERIFIED: the `Groups` migration on Neon (CI runs it at the merge), any live run of the API.

## Next step
1. Filip commits Step 3, subject: `Add the members and invite backend, the transaction helper and their tests.`
2. Step 4 frontend groups (tester first, then coder): bottom bar (pill, tap only), Groups list with Finished row and Recently deleted screen, New group, group shell with the two-line Add people card, Group settings; Undo toast 4 s through Sonner; wine-red `--danger` token; `jsonRequest` gains DELETE.
3. Before Step 4 starts: Filip confirms the remaining Macedonian lines (mockup, МК button).

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
