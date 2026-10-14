# Status: Kvit

_Last updated: 2026-10-14 (Phase 10, Steps 1 and 2 done; Step 3 tests written, code not yet)_

## Where we stopped
- Branch: `feat/10-finish-groups`, last commit `8abf824 Add closing a group: start, confirm, object, unlock and the 24-hour finish.` (Step 1).
- Staged, not committed: Step 2 (guards in every write command, `GROUP_FINISHED` / `GROUP_CLOSING`, One bill auto-finish, `GroupWriteTransaction`), the Step 2 tests, the closing mockup, DECISIONS and BACKLOG notes, this file.
- Not staged on purpose: the Step 3 frontend tests (red until the coder runs): `src/web/src/**/closing*`, `*Closing*.test.*`, `*ReadOnly.test.*`, `writesAfterClosing.test.tsx`, `src/test/closingTest*.ts`, plus the edits to `groupTestData.ts` and `approvedTexts.test.ts`. They would fail CI if pushed now.
- Last thing done: Filip picked banner style C (a bottom bar that stays until answered); the tester wrote the Step 3 tests (148 files, 393 red, as intended).

## Next step
Send Step 3 to the **coder** (ask Filip first; he said "send" for Steps 1 and 2): service + hooks + `ClosingBar` + badge + read-only hiding + activity sentences + join messages + EN/MK texts, and draw the "+" button as a centred shape (it looked off-centre). Make `npm run build` and all frontend tests green (target: the 393 red tests green, the old 5497 still green). Claude's recommendation, awaiting Filip's OK: hide "Leave group" for a member while the group is Closing (the backend refuses it), keep it when Finished.

## Then
- Step 4: live check in headless Chrome at 360 px (light/dark, EN/MK) with two accounts: start, confirm, object, owner edit cancels, unlock, One bill finishing; code review; report in `docs/reports/`; guide in `docs/guides/`; ROADMAP ticks; DECISIONS log moves into the report.
- Phase 10 PR text is drafted in the chat history (title "Phase 10: Finishing groups"); tick the checklist as steps land.
- Phase 11 (dashboard): closing bar as a popup (BACKLOG).

## Blockers and open questions
- Filip: the Macedonian drafts for the closing texts (`src/test/closingTestData.ts`, plus the Step 1 and 2 error texts in `mk.json`) are not read by him.
- Filip: apply the Step 1 migration locally (`dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api --no-build`) if not done yet; no migration in Step 2.
- Until Step 3 is built, the Activity tab stops on closing events: do not close a group in the app yet.

## Verification state
- VERIFIED by automated test (2026-10-14, after Step 2): backend build 0 warnings; `dotnet test Kvit.slnx` 3326/3326; `npm run lint` 0 warnings; `npm run build` OK; `npm test` 5497/5497 (without the Step 3 tests).
- NOT VERIFIED: any live run of closing; the Step 3 code (does not exist yet); Macedonian wording; real Render and Neon.
