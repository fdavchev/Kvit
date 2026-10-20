# Phase 13: first-push texts (commit subject, PR title, PR description)

Branch: `feat/13-personal-spending`. This file is the one place for these texts. It is updated as steps land: the checklist below is copied into the PR, and Filip ticks the boxes there. After the first push, later pushes only get a one-line commit subject (listed at the bottom).

## Commit subject (first push)
Add custom personal categories: create, rename, archive and bring back.

## PR title
Phase 13: personal spending and custom categories

## PR description (paste this)

### Overview
First phase of Release 2 ("budget"). Adds personal spending (tap a category, type the amount, Save) and your own categories, plus a first Budget tab that shows this month's personal spending. Everything here is private to the person who owns it.

### Scope
- Custom categories for personal spending only (name, emoji, one of the 10 app colours). Create, rename, archive and bring back. Archived categories keep their old entries. Group expenses keep the built-in list.
- Personal entries: amount in whole minor units (MKD in deni, EUR in cents), currency, date, optional note, category. Edit, delete with Undo, and safe against double taps.
- Budget tab (`/budget`) between Groups and Settings: month strip, a big "Spent this month" total (MKD and EUR apart), a tile per category with its total, and the list of entries.
- Adding starts from the Budget tab "+" and from the Home "+" list ("Personal spending").
- Guests (joined without an account) do not get the Budget tab or personal spending; the API answers 403 for them.
- Phone first (360 px), then the desktop layout. English and Macedonian, light and dark.
- Not in this phase: income, budget share of group expenses, budget currency, limits, monthly summary, duplicates, group totals.

### Database changes
- Migration `PersonalCategories` (Step 1): owner link, index and a colour check on `categories`.
- Migration `PersonalEntries` (Step 2): the new `personal_entries` table.
- Migration `PersonalCategoryNameUnique` (code review): no two active categories with the same name (letter case ignored) for one person.
- CI applies all three on Neon after the merge.

### Checklist
- [x] Step 0: mockup and picks logged in DECISIONS
- [x] Step 1: backend, custom categories (migration `PersonalCategories`)
- [x] Step 2: backend, personal entries (migration `PersonalEntries`)
- [x] Step 3: frontend, categories (grid with "+ New", new-category sheet, Edit my categories)
- [x] Step 4: frontend, add, edit and delete spending
- [x] Step 5: frontend, Budget tab (not for guests)
- [x] Step 6: desktop layout and the 360 px browser pass (EN/MK, light/dark)
- [x] Step 7: code review fixes, report and guide
- [ ] CI green

## Later pushes: one-line commit subjects
- Step 2: Add personal spending entries: add, edit, delete, restore and the monthly list.
- Step 3: Add the personal category grid with a new-category sheet and Edit my categories.
- Step 4: Add the add, edit and delete spending sheet.
- Step 5: Add the Budget tab with month strip, totals and entries.
- Step 6: Fix the Phase 13 browser pass findings: sheet height, focus, scrollbar and long amounts.
- Step 6b: Fix the month strip mouse wheel and the focus after cancelling New category.
- Step 7: Fix the Phase 13 code review findings: date range, races, emoji check and own error codes.
- Step 7b: Record where Phase 13 stopped: report, guide, roadmap and status.
