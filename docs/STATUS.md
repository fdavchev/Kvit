# Status: Kvit

_Last updated: 2026-10-03 (Phase 7 in progress: Steps 0 to 2 done; Step 3 is next)_

## Where we stopped
- Branch: `feat/07-groups` (PR not opened yet; the first commit `af2a556` is in).
- Uncommitted: the mockup `docs/design/2026-10-03-groups/`, doc edits (DECISIONS Phase 7 log, SCREENS, BACKLOG), and all Step 2 code and tests (Filip commits between steps).
- Done: Step 0 (branch, baseline, decision log). Step 1 (clickable mockup, approved by Filip: pill tab bar, "+ Add a name" link opening a sheet, wine-red for removing things, "That's me" yes; his Macedonian approval is partly given: the "групата" wording yes, the rest of the lines still to be confirmed before Step 4). Step 2 (backend groups): entities, migration `Groups`, repositories, domain services, `GroupsController` (create, list, get, change, delete, restore), events, tests.
- Verification (VERIFIED by automated test, 2026-10-03): `dotnet build` 0 warnings; `dotnet test` 950/950 (593 before Phase 7, 357 new); `dotnet ef migrations has-pending-model-changes` exits 0. NOT VERIFIED: the migration on Neon (CI runs it at the merge), a live API run.

## Next step
1. Filip: commit Step 1 (docs and mockup) and Step 2 (code and tests), each with its own one-line subject.
2. Step 3: backend members and invites (plain names, invite link make/reset/undo, preview, join + claim, "That's me", leave, remove, let back in, make owner, undo claim, the `invite` rate limit). Tester first, then coder.
3. Before Step 4 (frontend): Filip confirms the remaining Macedonian lines.

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
