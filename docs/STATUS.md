# Status: Kvit

_Last updated: 2026-10-19 (Phase 12b built and checked; waiting for the code review decision, then Filip's push, PR and merge)_

## Where we stopped
- Release 1 was accepted by Filip on 2026-10-16. Phase 12 is merged (PR #15).
- Branch: `feat/12b-guest-join`. Phase 12b (join a group without an account) is built, committed in steps and checked: guest accounts, join by tapping a name or typing a new one, Guest badge, guest limits, the owner's "Let in again", Log out warning, and "Make an account" (Google or email). Three migrations: `GuestAccounts`, `GuestLetInAgain`, `GuestUpgradedUsageEvent`.
- Report: `docs/reports/2026-10-19-phase-12b-guest-join.md` (browser pass: `docs/reports/2026-10-19-phase-12b-browser-pass.md`); guide: `docs/guides/phase-12b-guest-join.md`.

## Next step
1. Filip says whether he wants a code review of the branch before the PR.
2. Filip pushes, opens the PR (texts are in the chat), checks that CI is green, and merges (CI applies the 3 migrations on Neon).
3. Then Phase 13 (personal spending + custom categories); its prompt is `docs/guides/phase-13-personal-spending-prompt.md`.

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real Google picture in a circle, the Google button inside the Log in sheet and "Make an account", the invite link on his phone, and a read of the Macedonian drafts of Phases 9 to 12b.
- BACKLOG: the Phase 12 and Phase 12b leftovers, then Release 2.

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository.
- Filip to decide: whether the light theme gets a lighter peach (options in the BACKLOG, 2026-10-16).

## Verification state
- VERIFIED by automated test (2026-10-19, run by me after the last change): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 3710/3710; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 7870/7870.
- VERIFIED by live run (the tester, headless Chromium, local stack): join by tap and by new name, no-free-names, guest limits, Let in again and rejoin, email upgrade, double click on Save, the Log in sheet, 360/768/1280 px in EN/MK, light/dark, reduced motion. Three findings were fixed and re-checked by tests and a headless check of the join page.
- NOT VERIFIED: the Activity tab and Members after a real guest tap with the fixes, the Google upgrade and Google log in, a real phone, Safari, Firefox, a screen reader, Closing and Finished groups with guests in a browser, two tabs upgrading one guest, the Macedonian drafts, Neon.
