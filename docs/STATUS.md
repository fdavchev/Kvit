# Status: Kvit

_Last updated: 2026-10-06 (Phase 8 in progress, Steps 0 to 5 built)_

## Where we stopped
- Branch `feat/08-expenses`, last commit `a667286 Add the expenses frontend: ...`. The PR is not opened yet.
- Steps 0 to 5 are built and VERIFIED by automated test. Step 5 (Activity tab with the pill switch, New group choice, One bill form with circles) is built but **uncommitted** together with its tests and the docs edits (DECISIONS, ROADMAP, STATUS, BACKLOG).

## Next step
1. Filip commits and pushes Step 5 (commit subject in the chat).
2. Step 6: the real-browser check by the debugger. Ask Filip before launching it (and before every coder run, memory 2026-10-06).

## Then
- Step 6: real-browser check by the debugger (360 and 1280 px, EN light and dark, MK light; production build with the CSP; the real NBRM call; a stand-in Google picture; orange category colour contrast; the Date sheet on desktop).
- Step 7: `/code-review`, phase report (decision log moves into it), ROADMAP and STATUS as merged, a plain-words guide, PR title, description and one commit subject in the chat. Filip merges.
- Phase 9 balances and settle up. Remember: `Balances.Calculate` needs removed and left members too.

## Blockers and open questions
- Nothing blocks Step 5.
- BACKLOG: 12 Phase 7 browser items, animations, API error handler, joining without an account (top "must do"), editing an old expense with a removed person in its split, bundle size 650 kB.
- Still Filip's decision: whether `START-HERE-PROMPT.md` stays in the public repository.

## Verification state
- VERIFIED by automated test (2026-10-06, after the Step 5 code, `.\scripts\start-local.ps1 -Check`): backend build 0 warnings, `dotnet test` 2082/2082; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 3679/3679.
- VERIFIED by live run: the migrations `CategoriesAndExchangeRates` and `Expenses` applied to the local database (10 categories, rate row 61.5610).
- NOT VERIFIED: every Phase 8 screen in a real browser, the real NBRM call from the API, the Google picture host (`https://*.googleusercontent.com` is a guess), the new migrations on Neon (they run in CI at the merge).
- REPORTED by Filip: Phase 7 works on the live site.
