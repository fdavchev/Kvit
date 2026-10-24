# Status: Kvit

_Last updated: 2026-10-24 (Phase 14 built, browser-checked and code-reviewed; waiting for Filip's last commits, push, CI and merge)_

## Where we stopped
- Release 1 was accepted on 2026-10-16. Phases 12, 12b and 13 are merged.
- Branch: `feat/14-income`. Phase 14 (income, once and repeating, second phase of Release 2) is built: the Budget "+" asks Spending or Income, income once, repeating income (lazy catch-up when the Budget tab opens), the Repeating income row, list sheet and `/budget/repeating` screen, the "Left this month" card, guests excluded. Four migrations: `Income`, `RecurringIncomeRules`, `GeneratedIncomeEntries`, `PersonalEntryOriginRules`.
- Report: `docs/reports/2026-10-24-phase-14-income.md` (decision log at its end); browser pass: `docs/reports/2026-10-21-phase-14-browser-pass.md`; guide: `docs/guides/phase-14-income.md`; first-push texts: `docs/guides/phase-14-pr-text.md`; mockups: `docs/design/2026-10-20-phase-14/`.

## Next step
1. Filip: commit the last round (Bugs 7/8/9 fixes + the close-out docs; subjects in `docs/guides/phase-14-pr-text.md`), push, open the PR with the texts in that file, check that CI is green, and merge (CI applies the 4 migrations on Neon).
2. Optional first: Filip tries the app with `docs/guides/phase-14-income.md` (needs `dotnet ef database update` for the migrations).
3. Then Phase 15 (budget share per group, budget currency, limits, monthly summary); its prompt is not written yet.

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real Google picture in a circle, the Google button inside the Log in sheet and "Make an account", the invite link on his phone, and a read of the Macedonian drafts of Phases 9 to 14.
- BACKLOG: future and planned payments (paid / not yet paid), the Phase 14 leftovers, the Phase 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; whether the light theme gets a lighter peach (BACKLOG, 2026-10-16).
- Open, Filip has not decided: a second "Edit my categories" link on the Budget tab (not built; my view: not needed).

## Verification state
- VERIFIED by automated test (2026-10-24, run by me after the last coder run): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 4892/4892; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 9717/9717.
- VERIFIED by live run (the tester, headless Chromium, local stack): all of Phase 14 at 320/360/768/1280/1920 px in EN/MK, light/dark, reduced motion (three passes, 9 bugs found and fixed, including a Postgres deadlock of parallel catch-ups; no duplicate entries in SQL checks).
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, the native calendar pop-up, other time zones beyond the emulated ones, the roll animation of a 10-digit amount, Neon and Render (migrations, Bug 9), the Macedonian drafts.
