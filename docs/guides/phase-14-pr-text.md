# Phase 14: first-push texts (commit subject, PR title, PR description)

Branch: `feat/14-income`. This file is the one place for these texts. It is updated as steps land: the checklist below is copied into the PR, and Filip ticks the boxes there. After the first push, later pushes only get a one-line commit subject (listed at the bottom).

## Commit subject (first push)
Add income once to personal entries with the Income migration and the Phase 14 mockups.

## PR title
Phase 14: income, once and repeating

## PR description (paste this)

### Overview
Second phase of Release 2 ("budget"). Adds personal income: added once, or repeating monthly or weekly. Repeating income creates its entries lazily when the person opens the Budget tab (no scheduler). The Budget tab shows what is left this month: income minus spending. Everything here is private to the person who owns it.

### Scope
- Income once: amount, currency (the last used one is the default), date, optional title. No category.
- Repeating income: monthly on a day (31 = the last day of shorter months) or weekly on a weekday, with a start date and an optional end date. Edit and stop. Entries already created stay as they are.
- Entries are created lazily in the person's own time zone, never more than 24 months back, and never twice (unique on the income and the date).
- Budget tab: one card with Left, Income and Spent, plus a "Repeating income" row that opens the list.
- Adding starts from the "+" on the Budget tab, which asks "Spending or Income?" in a small bottom sheet.
- Guests have no income and no Budget (403).
- Phone first (360 px), then the desktop layout. English and Macedonian, light and dark.
- Not in this phase: budget share of group expenses, budget currency, limits, monthly summary, duplicates, group totals, future and planned payments. Paybacks and settlements are never income.

### Database changes
- Migration `Income` (Step 1): `Income` allowed as an entry kind, `personal_entries.recurring_income_id` with a unique index on (`recurring_income_id`, `entry_date`), and the new `recurring_incomes` table.
- Migration `RecurringIncomeRules` (Step 2): a repeating income has a day matching its rhythm and a title of 1 to 60 characters.
- Migration `GeneratedIncomeEntries` (Step 3): `personal_entries.client_request_id` may be empty (entries made from a repeating income have none).
- CI applies all three on Neon after the merge.

### Checklist
- [x] Step 0: mockups, picks logged in DECISIONS
- [x] Step 1: backend, income once and the migration
- [x] Step 2: backend, repeating income (create, list, edit, stop, restart)
- [x] Step 3: backend, lazy generation with same-moment, month-end and time zone tests
- [x] Step 4: frontend, "Spending or Income?" sheet and add income once
- [x] Step 5: frontend, repeating income (Repeats and Ends chips, the row and the list)
- [ ] Step 6: frontend, Budget tab with the Left card
- [ ] Step 7: desktop layout and the 360 px browser pass (EN/MK, light/dark)
- [ ] Step 8: code review fixes, report and guide
- [ ] CI green

## Later pushes: one-line commit subjects
- Step 2: Add repeating income: create, list, edit, stop and restart, with the RecurringIncomeRules migration.
- Step 3: Add lazy income generation: the catch-up endpoint, next dates and the GeneratedIncomeEntries migration.
- Step 4: Add the Spending or Income sheet and the income sheet to the Budget tab.
- Step 5: Add repeating income to the Budget tab: Repeats and Ends chips, the row, the list and the catch-up.
