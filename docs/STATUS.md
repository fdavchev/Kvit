# Status: Kvit

_Last updated: 2026-10-05 (Phase 7 done and merged; Phase 8 is next)_

## Where we stopped
- Phase 7 (groups, members, invite links) is done and merged into `main`. Report: `reports/2026-10-05-phase-07-groups.md`; guide: `guides/phase-07-groups.md`.
- Next: **Phase 8, expenses** (`feat/08-expenses`): categories, exchange rates, add / edit / delete with Undo, the four split types, One bill, the Expenses tab. Start with a plan for Filip.

## Next step
1. Phase 7 is merged, CI is green and Filip tried it on the live site (REPORTED: works).
2. Phase 8: read `ROADMAP.md` Phase 8 and `DECISIONS.md`, propose the plan, wait for Filip.

## Then
- Phase 9 balances and settle up, Phase 10 finishing groups, then the dashboard (Phase 11, Filip decides its layout, phone and web).

## Blockers and open questions
- Nothing blocks Phase 8.
- Still Filip's decision: whether `START-HERE-PROMPT.md` stays in the public repository.
- BACKLOG top "must do": joining a group without an account. Not built in Phase 7.
- Private notes file `Desktop\secrets\kvit-secrets.txt` (outside the repo) holds the Neon strings, the online certificate pair, the proxy secret and the Render API key. Never lose the certificate pair (everybody would be logged out). The downloaded Google `client_secret_*.json` in the same folder holds a client secret Kvit never uses; it can be deleted.

## Verification state
- VERIFIED by automated test (2026-10-05, after the last code change): backend build 0 warnings, `dotnet test` 1454/1454; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 1940/1940.
- VERIFIED by live run (2026-10-05, headless Chrome, local stack, throwaway local accounts): every Phase 7 screen at 360 px and 1280 px in English, light and dark; Members and Join in Macedonian at 360 px, light; the sheet slide, the dark pills, not-found, the Join states and the signed-out round trip. Details in the Phase 7 report.
- NOT VERIFIED: Google sign-in from the join card (only a throw-away test), a real phone share menu and touch, a screen reader, Macedonian in dark mode and at 1280 px, the `Groups` migration on Neon (runs in CI at the merge).
- REPORTED by Filip (2026-10-05): the Phase 7 live-site test (create a group, invite a second account, join, Google button on the invite page) "everything works fine"; CI on `main` green.
- REPORTED by Filip (earlier phases): the real Google sign-in and the phone test (Phase 6); "In production" for the Google app.
