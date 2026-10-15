# Status: Kvit

_Last updated: 2026-10-15 (Phase 11 steps 1 to 6 done and pushed; the code-review fixes and the close-out are left)_

## Where we stopped
- Branch: `feat/11-dashboard`, pushed; last commit `04bc558` (the Step 6 full-pass fixes). Only `docs/BACKLOG.md` (review leftovers) is staged and uncommitted.
- Done in Phase 11: the dashboard endpoint and the `Active` usage event (Step 2), the Home screen in layout C with Needs you answered on Home and the "+" list (Step 3), toasts above the closing bar (Step 4), the Phase 10 leftovers (Step 5), the full 360 px pass of every screen with its fixes (Step 6).
- The whole-branch code review ran (8 findings). Verified as real and **not yet fixed** (the tester that was writing their tests was stopped, nothing was left behind):
  1. Home shows "No groups yet" for a person whose groups are all Finished (the dashboard's `groups` holds only unfinished ones) and hides their Recent activity. Planned rule: the full empty state only when groups AND recentActivity are empty; otherwise a compact "No open groups right now" card with a New group button (new key `home.noOpenGroupsTitle`, MK draft «Моментално нема отворени групи») above Recent activity.
  2. `HomePaymentCard`: a refused Confirm/Reject with any error (for example `SETTLEMENT_NOT_PENDING`) must also refetch the dashboard, so a stale card goes away.
  3. `KvitUnbrokenSlashWords` must wrap only short slash words (12 characters or fewer), so a long name with slashes can still wrap.
  4. Refactor: the dashboard's closing "Needs you" rule should reuse the roster's `ClosingConfirmers` like `GetGroupQueryHandler` (guarded by existing tests).
  The other review findings are in BACKLOG ("Phase 11 whole-branch code review leftovers").

## Next step
Tests first (tester), then the coder, for the four fixes above; run build, `dotnet test`, lint, `npm run build`, `npm test`; a short live look at Home with only a Finished group. Then Step 7: a report in `docs/reports/` (move the Phase 11 decision log to its end), a guide in `docs/guides/`, STATUS and ROADMAP, and the pull request text. Filip then merges and deploys, and uses it for real: Release 1 is done when Filip says it works for him.

## Then
- Phase 12 (`feat/12-polish`): the glass look and the animations Filip picked (BACKLOG, 2026-10-09), the dark colours he dislikes (`color-mix` surfaces), the desktop layout and the small look items in BACKLOG.
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real Google picture in a circle, and a read of the Macedonian drafts of Phases 9, 10 and 11 (the dashboard, closing, waking-up, toast region texts; and the word «Позадина» for the theme).
- BACKLOG: "Accommodation" breaking in the Category sheet (Filip decides the wording), joining without an account (top "must do").

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; the English category text "Accommodation" (approved, pinned by tests, breaks mid-word at 360 px).

## Verification state
- VERIFIED by automated test (2026-10-15, after Step 6): `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 7088/7088; backend (last full run after Step 5, no backend change since): build 0 warnings, `dotnet test` 3485/3485.
- VERIFIED by live run (headless Chromium, 360 px, local stack, throwaway accounts): the Home screen in all states, the "+" list, answering Needs you on Home, the toast above the closing bar, the Step 5 and Step 6 fixes, and a 764-snapshot pass of every screen in EN and MK, light and dark.
- NOT VERIFIED: the four review fixes above, real Google sign-in, a real phone (keyboard, bottom inset), Safari, real Render and Neon (a real cold start), the production build's single `Active` POST (the dev server sends two), the real 24-hour finish, a line-by-line read of the Macedonian texts by Filip.
