# Phase 15 report: budget share, budget currency, limits, monthly summary

Branch `feat/15-budget`. Written 2026-10-26 for Filip, readable on a phone. Every claim is labelled.

## What was built
| Step | What | Commit |
|---|---|---|
| 0 | Mockup (5 parts) and Filip's picks | with step 1 |
| 1 | Backend: per-group "Count my shares in my budget" switch, remembered flag on each share, group shares in the Budget month answer (migration `BudgetShare`) | `ad76011` (first part `8be9d63`, Filip) |
| 2 | Backend: optional converted total (`users.budget_total_currency`), the conversion rule, `convertedTotals` in the month answer (migration `BudgetTotalCurrency`) | `ad76011` |
| 3 | Backend: monthly limits with history (per category and whole month), spent / over / EUR-included amounts (migration `BudgetLimits`) | `7023261` |
| 4 | Backend: `GET /api/personal/summary` | `6f64e6c` |
| 5 | Frontend: Budget card in Settings, group switch in group settings | `e589f81` |
| 6 | Frontend: "From groups" block, shares in totals and tiles, single converted total | `6f3db0e` |
| 7 | Frontend: Limits card, set-a-limit sheet, choice sheet | `fefec5c` |
| 8 | Frontend: Summary screen `/budget/summary` and the Summary row | `9b162f2` |
| 9 | Real-browser pass and its fixes | `a25e5ab`, `3c7e252` |
| 10 | Code review (9 findings: 4 fixed, 5 left or wrong) | `2e31eb3` |

## Filip's picks
Currency apart by default with an optional converted total; group shares in their own "From groups" block; limits as a card with a set-a-limit sheet and an "N over" line; summary on its own screen; switch in each group's settings; limits keep history (so a changed limit never rewrites past months); limits count euro spending converted.

## Verification
- VERIFIED by automated test (run by me after the last coder run, 2026-10-26): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 5388 of 5388; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 10663 of 10663.
- VERIFIED by live run (the tester, headless Chromium, local stack): all Phase 15 screens at 320 / 360 / 768 / 1280 / 1920 px, EN and MK, light and dark, reduced motion. 3 bugs found, none a blocker: report `docs/reports/2026-10-26-phase-15-browser-pass.md`.
- The fixes to those 3 bugs and the 4 code review fixes: VERIFIED by automated test only, NOT VERIFIED again in a browser.
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, other time zones, Neon and Render (3 new migrations), the Macedonian drafts, the deploy gap (new website against the old API for a few minutes).

## What Filip must do
1. `dotnet ef database update ...` on his PC (the guide, "Before you start").
2. Push the branch, open the PR with `docs/guides/phase-15-pr-text.md` (tick Step 0), check CI is green, merge (CI applies the 3 migrations on Neon).
3. Try the app with `docs/guides/phase-15-budget.md`.
4. Answer the open items in `docs/DECISIONS.md` ("Needs Filip's decision", items 8 to 11).
5. Read the Macedonian drafts of Phases 9 to 15.

## Code review (Step 10)
Fixed: the limit sheet from a past month (Save blocked / Remove logic used the viewed month), summaries not refreshed after entry or setting changes, an error toast when the group was not cached after saving the switch, and timeouts (408, 425) no longer retried. Wrong claim: list keys in the From groups block (one expense can bring two shares to a claimed name). Left in BACKLOG: summary loads full rows, malformed `?month=` generic error, invalid limit amount message, bar "near" state only by colour, arrow keys on radios, twin hooks and handlers, one unused entity method, 38 px theme buttons, Back from Summary returns to the current month.

## Decisions
The full Phase 15 log (what changed, why, alternatives rejected) is in `docs/DECISIONS.md`, section "2026-10-24: Phase 15". It stays there until Filip has answered the open items; then it moves to the end of this report, as in earlier phases.
