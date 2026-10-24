# Phase 15: first-push texts (commit subject, PR title, PR description)

Branch: `feat/15-budget`. This file is the one place for these texts. It is updated as steps land: the checklist below is copied into the PR, and Filip ticks the boxes there. After the first push, later pushes only get a one-line commit subject (listed at the bottom).

## Commit subject (first push)
Add the Phase 15 prompt and mockup and the budget share: per-group switch, remembered flag on shares and group shares in the Budget month answer, with the BudgetShare migration.

## PR title
Phase 15: budget share, budget currency, limits and monthly summary

## PR description (paste this)

### Overview
Third phase of Release 2 ("budget"). Your share of group expenses now counts in your personal Budget, in its own "From groups" block. You can set monthly limits per category and see a monthly summary. MKD and EUR stay apart; an optional setting shows one converted total. Everything here is private to the person who owns it.

### Scope
- Budget share: your share of group expenses in the expense's category, per-group switch "Count my shares in my budget" in each group's settings (on by default). Settlements and paybacks never count. Claimed names bring their older shares.
- Budget currency: MKD and EUR apart by default, with an optional converted total in Settings.
- Monthly limits per category and for the whole month on a Limits card; euro spending counts converted; an over-limit row says how much it is over.
- Monthly summary on its own screen (Budget, then Summary): this month against the previous one.
- Guests have no Budget (403).
- Phone first (360 px), then the desktop layout. English and Macedonian, light and dark.
- Not in this phase: possible duplicates, group totals and the group spending plan (Phase 16), future and planned payments.

### Database changes
- Migration `BudgetShare`: adds `group_members.counts_in_budget` and `expense_shares.counts_in_budget` (true / false, never empty, default true, so every existing member and share counts).
- Migration `BudgetTotalCurrency`: adds `users.budget_total_currency` (empty, `MKD` or `EUR`; empty for every existing account, so nobody's Budget changes).
- Migration `BudgetLimits`: adds the `budget_limits` table (one row per person, category or whole month, and month the limit starts applying; an empty amount means "no limit from that month"). It starts empty, so nobody has a limit until they set one.

### Checklist
- [ ] Step 0: mockup, picks logged in DECISIONS
- [x] Step 1: backend, budget share
- [x] Step 2: backend, budget currency
- [x] Step 3: backend, limits
- [ ] Step 4: backend, monthly summary
- [ ] Step 5: frontend, Settings (budget currency, per-group switch)
- [ ] Step 6: frontend, group shares on the Budget tab
- [ ] Step 7: frontend, limits
- [ ] Step 8: frontend, monthly summary
- [ ] Step 9: desktop layout and the 360 px browser pass (EN/MK, light/dark)
- [ ] Step 10: code review fixes, report and guide
- [ ] CI green

## Later pushes: one-line commit subjects
- Step 1: Add the budget share: per-group switch, remembered flag on shares and group shares in the Budget month answer, with the BudgetShare migration.
- Step 2: Add the budget total currency: optional converted total setting, the conversion rule and convertedTotals in the Budget month answer, with the BudgetTotalCurrency migration.
- Step 3: Add monthly limits with history: set and remove per category and for the whole month, and limits with spent and over amounts in the Budget month answer, with the BudgetLimits migration.
