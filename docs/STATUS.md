# Status: Kvit

_Last updated: 2026-10-20 (Phase 14 Step 0 done: mockups and Filip's picks logged; no code written yet)_

## Where we stopped
- Release 1 was accepted on 2026-10-16. Phases 12, 12b and 13 are merged (Phase 13 = PR #17).
- Branch: `feat/14-income` (from `main`). Phase 14 (income, once and repeating) is in Step 0: three mockup rounds in `docs/design/2026-10-20-phase-14/`, Filip's picks and my defaults in `docs/DECISIONS.md` (top section), steps in `docs/ROADMAP.md`, first-push texts in `docs/guides/phase-14-pr-text.md`. Prompt: `docs/guides/phase-14-income-prompt.md`; plan: `C:\Users\Davchev\.claude\plans\filip-wants-to-run-foamy-crystal.md`.
- Phase 13 report: `docs/reports/2026-10-20-phase-13-personal-spending.md`; guide: `docs/guides/phase-13-personal-spending.md`.

## Next step
1. Filip: answer the open Phase 14 questions (row visible at 0, the date and edit rules, no Home "+" Income item, future and planned payments out), and commit the Step 0 docs.
2. Step 1: backend, income once and the `Income` migration (ask Filip before the coder run; Docker Desktop open).

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real Google picture in a circle, the Google button inside the Log in sheet and "Make an account", the invite link on his phone, and a read of the Macedonian drafts of Phases 9 to 13.
- BACKLOG: future and planned payments (paid / not yet paid), the Phase 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; whether the light theme gets a lighter peach (BACKLOG, 2026-10-16).
- Open, Filip has not decided: a second "Edit my categories" link on the Budget tab (not built; my view: not needed, the grid is one tap away on "+"). Done on Filip's go (2026-10-20): `AddExpense` / One bill now look up the exchange rate last.

## Verification state
- VERIFIED by automated test (2026-10-20, run by me after the last change): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 4171/4171; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 8636/8636.
- VERIFIED by live run (the tester, headless Chromium, local stack): both ways to add, tap counts, currency memory, category rules, archive and Bring back, the Budget screen, edit/delete/Undo, guest redirect and 403, privacy between users, 360/768/1280+ px in EN/MK, light/dark, reduced motion, sheet height, month strip wheel and scrollbar, focus after cancel (three passes, 9 bugs found, 8 fixed, 1 left as Chrome behaviour).
- VERIFIED by live run (re-check 3, 2026-10-20): the code review fixes (date limits, Save/Delete in flight, emoji, duplicate names, month answers, error texts). The date hint text and the expense rate order were fixed after it (automated test only).
- NOT VERIFIED: the native calendar pop-up, other time zones, a real phone, iOS Safari, Firefox, a screen reader, the sheet with a real keyboard open, Neon (migrations, `lc_ctype` for the unique name index), the Macedonian drafts.
