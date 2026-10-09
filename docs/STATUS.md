# Status: Kvit

_Last updated: 2026-10-09 (Phase 8 done and merged; Phase 9 is next)_

## Where we stopped
- Phase 8 (expenses) is done and merged into `main` (`feat/08-expenses`). Report: `reports/2026-10-09-phase-08-expenses.md`; guide: `guides/phase-08-expenses.md`. CI applies the two new migrations (`CategoriesAndExchangeRates`, `Expenses`) on Neon at the merge.
- Delivered: categories, the NBRM exchange rate, expenses with four splits, Undo and Recently deleted (5 days), the activity feed and change history, One bill (with the group name in the app language), the Expenses · Activity tabs, profile-picture circles, layout B, the two-person auto-fill, the real-browser fixes and the four code-review fixes.

## Next step
1. Phase 9, balances and settle up (`feat/09-settle-up`): read `ROADMAP.md` Phase 9 and `DECISIONS.md`. `Balances.Calculate` needs removed and left members too (they stay in old splits).
2. Filip, once on the real site: one real EUR expense on Render (the first call to the real NBRM) and a real Google picture in a circle.

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository.
- BACKLOG: the Phase 7 and Phase 8 browser items, the deferred review findings (rate fetched before the checks, NBRM holiday look-back, archived-category edit check, edit concurrency, Add-screen split draft after a member is removed), animations, API error handler, joining without an account (top "must do").

## Verification state
- VERIFIED by automated test (2026-10-09, `.\scripts\start-local.ps1 -Check`): backend build 0 warnings, `dotnet test` 2143/2143; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 4402/4402.
- VERIFIED by live run (2026-10-07, headless Chrome, local stack): the Phase 8 screens and the real NBRM call. Details in the report.
- REPORTED by Filip: Phase 7 works on the live site; a hand check of the finished Phase 8 screens on 2026-10-09 ("everything is good").
- NOT VERIFIED: a real phone, other browsers, a screen reader, a real user's Google picture, the code-review fixes and the One bill group name in a browser by me, the two new migrations on Neon (run in CI at the merge).
