# Status: Kvit

_Last updated: 2026-10-16 (Phase 11 built and checked; waiting for Filip's commit, push and merge)_

## Where we stopped
- Branch: `feat/11-dashboard`. The last pushed commit is `03f80db`. **Uncommitted:** the four code-review fixes, the CI test fix and the close-out docs (report, guide, DECISIONS, ROADMAP, this file).
- Done in Phase 11: the dashboard endpoint and the `Active` usage event, the Home screen in layout C with Needs you answered on Home and the "+" list, toasts above the closing bar, the Phase 10 leftovers, the full 360 px pass, and now the review fixes:
  - Home with only Finished groups shows a compact "No open groups right now" card.
  - A refused Confirm/Reject on a Home payment card refetches the dashboard.
  - Slash words stay unbroken only up to 12 characters.
  - The dashboard's closing "Needs you" rule reuses `ClosingConfirmers`.
  - The CI test `Active_SecondCallTheSameUtcDay…` no longer depends on run order (it failed on every GitHub run).
- Report: `docs/reports/2026-10-16-phase-11-dashboard.md`; guide: `docs/guides/phase-11-dashboard.md`.

## Next step
Filip commits (suggested subject: `Fix the dashboard review findings and make the Active test independent of run order.`), pushes, and checks that the CI run on GitHub is green (especially the backend job). Then he merges (no migration), follows `docs/guides/phase-11-dashboard.md`, and uses the app for real. Release 1 is done when Filip says it works for him.

## Then
- Phase 12 (`feat/12-polish`): the glass look and the animations Filip picked (BACKLOG, 2026-10-09), the dark colours he dislikes (`color-mix` surfaces), the desktop layout and the small look items in BACKLOG.
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real Google picture in a circle, and a read of the Macedonian drafts of Phases 9, 10 and 11 (and the word «Позадина» for the theme).
- BACKLOG: "Accommodation" breaking in the Category sheet (Filip decides the wording), joining without an account (top "must do").

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; the English category text "Accommodation" (approved, pinned by tests, breaks mid-word at 360 px).

## Verification state
- VERIFIED by automated test (2026-10-16, run by me after the last change): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 3485/3485; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 7119/7119.
- VERIFIED by live run (headless Chromium, 360 px, local stack, throwaway accounts): Home in all states including an account with only a Finished group (EN/MK, light/dark) and a brand-new account; the "+" list; answering Needs you on Home; the toast above the closing bar; a 764-snapshot pass of every screen.
- NOT VERIFIED: the CI fix on GitHub (shown by the next CI run), the refused-Confirm refetch and the long slash word in a browser, real Google sign-in, a real phone (keyboard, bottom inset), Safari, real Render and Neon (a real cold start), the production build's single `Active` POST (the dev server sends two), the real 24-hour finish, the sign-up screen, a line-by-line read of the Macedonian texts by Filip.
