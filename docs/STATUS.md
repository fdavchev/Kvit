# Status: Kvit

_Last updated: 2026-10-20 (Phase 13 built, browser-checked and code-reviewed; waiting for Filip's last commit, push, CI and merge)_

## Where we stopped
- Release 1 was accepted on 2026-10-16. Phases 12 and 12b are merged.
- Branch: `feat/13-personal-spending`. Phase 13 (personal spending + custom categories, first phase of Release 2) is built: custom categories, personal entries, the Budget tab (`/budget`), the add/edit/delete sheet, the Home "+" item "Personal spending", guests excluded. Three migrations: `PersonalCategories`, `PersonalEntries`, `PersonalCategoryNameUnique`.
- Report: `docs/reports/2026-10-20-phase-13-personal-spending.md` (decision log at its end); browser pass: `docs/reports/2026-10-19-phase-13-browser-pass.md`; guide: `docs/guides/phase-13-personal-spending.md`; first-push texts: `docs/guides/phase-13-pr-text.md`.

## Next step
1. Filip: run `dotnet ef database update` (the third migration is new), commit the last round (code review fixes + docs), push, check that CI is green, and merge (CI applies the 3 migrations on Neon).
2. Optional first: a browser re-check of the code review fixes (date picker limits, Save/Delete dimming) by the tester; and Filip tries the app with `docs/guides/phase-13-personal-spending.md`.
3. Then Phase 14 (income, once and repeating). Its prompt is not written yet.

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
