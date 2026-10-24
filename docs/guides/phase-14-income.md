# Phase 14 guide: try income, once and repeating

For Filip. Plain steps, no jargon. Needs about 15 minutes.

## Before you start (one time)
1. Open Docker Desktop and wait until it says it is running.
2. Open PowerShell in the Kvit folder (`C:\Users\Davchev\Projects\Kvit`).
3. Start the database if it is not running: `docker compose up -d`
4. Apply the four new database changes (they add the income tables and rules):
   `dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api --no-build`
   You should see lines starting with "Applying migration" for Income, RecurringIncomeRules, GeneratedIncomeEntries and PersonalEntryOriginRules (whichever were not applied yet), then "Done."
   If it complains, paste the error to me.
5. Start the app: `.\scripts\start-local.ps1` (it opens the browser). When you are done: `.\scripts\start-local.ps1 -Stop`.

## Try it
1. Log in and tap **Budget** in the bottom bar.
2. **Add income once:** tap the round **+**. A small sheet asks "What do you add?". Tap **Income**, type an amount (the box is already ready), tap **Save**. A green row with a "+" appears in the list and the top card now says **Left this month**, with Income and Spent under it.
3. **Add spending:** tap **+**, **Spending**, a category, an amount, **Save**. The Left number goes down.
4. **Title and date:** on the income sheet tap the **Note** chip and type "Salary". Tap the date chip to pick another day (the last 24 months).
5. **Repeating income:** tap **+**, **Income**, type an amount, tap **Repeats: Never** once (it becomes Monthly, twice gives Weekly). The date chip now says **Starts**. Pick a start date about three months ago and tap **Save**. The past months get their entries at once: check them with the month strip at the top.
6. **End date:** on a repeating income tap the **Ends: never** chip and choose "In 3 months" (exactly 3 payments), or "Pick a date…".
7. **The row "Repeating income · 1":** tap it. With one or two incomes a sheet opens, with three or more a full screen opens. Each has the next date, **Edit** and **Stop**.
8. **Edit:** tap **Edit**, change the amount, **Save**. Only future entries use the new amount. The start date is locked once an income has made entries (the sheet says why).
9. **Stop:** tap **Stop**. A "Stopped · Undo" message shows. Tap **Undo** to bring it back.
10. **Generated entries:** tap one in the list. You can change the amount, note or delete it, but the date is locked (the sheet says why). If you delete one, it is not made again.
11. **Reopen:** leave the Budget tab and come back. No duplicates should appear.
12. **EUR:** add an income in EUR (tap MKD next to the amount) and a spending in MKD. The card shows a Left for EUR and keeps MKD spending as its own line.
13. **Guest check:** open an invite link in a private window and join as a guest. There should be no Budget tab.
14. Switch the language (Settings) and the theme (light / dark) and look again.

## What to tell me
- Anything slow, anything that needs more taps than it should, anything cut off or hard to read.
- The Macedonian texts you want changed (the new ones are drafts; list in `docs/reports/2026-10-24-phase-14-income.md`).
- Whether the one-entry-per-month rule feels right: if you move the pay day to the 25th after the 1st was already made, the next salary appears next month, not twice in one month.
