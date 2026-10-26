# Status: Kvit

_Last updated: 2026-10-26 (Phase 15 built, browser-checked and code-reviewed; waiting for Filip: migrations, push, PR, decisions)_

## Where we stopped
- Release 1 was accepted on 2026-10-16. Phases 12, 12b, 13 and 14 are merged.
- Branch: `feat/15-budget`. Phase 15 (budget share, budget currency, limits with history, monthly summary, third phase of Release 2) is built, step after step, and committed (11 commits; the last is `2e31eb3` plus the close-out docs). Three migrations: `BudgetShare`, `BudgetTotalCurrency`, `BudgetLimits`.
- Report: `docs/reports/2026-10-26-phase-15-budget.md`; browser pass: `docs/reports/2026-10-26-phase-15-browser-pass.md`; guide: `docs/guides/phase-15-budget.md`; first-push texts: `docs/guides/phase-15-pr-text.md`; mockup: `docs/design/2026-10-24-phase-15/`.
- Nothing is pushed.

## Next step
1. Filip: apply the migrations locally (`dotnet ef database update ...`, see the guide), try the app with `docs/guides/phase-15-budget.md`.
2. Filip: answer the open items in `docs/DECISIONS.md` ("Needs Filip's decision", items 8 to 11).
3. Filip: push, open the PR with the texts in `docs/guides/phase-15-pr-text.md` (tick Step 0), CI green, merge (CI applies the 3 migrations on Neon).
4. Then Phase 16 (possible duplicates, group totals per category, group spending plan); its prompt is not written yet.

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real Google picture in a circle, the Google button inside the Log in sheet and "Make an account", the invite link on his phone, and a read of the Macedonian drafts of Phases 9 to 15.
- BACKLOG: future and planned payments, the Phase 15 leftovers, the Phase 14, 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: DECISIONS items 8 to 11 (deploy gap, Macedonian wording, Back from Summary, bars without animation); whether `START-HERE-PROMPT.md` stays in the public repository; whether the light theme gets a lighter peach.

## Verification state
- VERIFIED by automated test (2026-10-26, run by me after the last coder run): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 5388/5388; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 10663/10663.
- VERIFIED by live run (the tester, headless Chromium, local stack): all Phase 15 screens at 320/360/768/1280/1920 px in EN/MK, light/dark, reduced motion (3 bugs, all fixed; the fixes VERIFIED by automated test only).
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, other time zones, Neon and Render (3 migrations), the deploy gap, the Macedonian drafts.
