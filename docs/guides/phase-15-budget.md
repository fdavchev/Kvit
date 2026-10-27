# Phase 15 guide: try the budget share, limits and summary

For Filip. Plain steps, no jargon. Needs about 20 minutes.

## Before you start (one time)
1. Open Docker Desktop and wait until it says it is running.
2. Open PowerShell in the Kvit folder (`C:\Users\Davchev\Projects\Kvit`).
3. Start the database if it is not running: `docker compose up -d`
4. Apply the three new database changes (they add the share switch, the converted-total setting and the limits):
   `dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api --no-build`
   You should see "Applying migration" lines for BudgetShare, BudgetTotalCurrency and BudgetLimits (whichever were not applied yet), then "Done." The tester already applied them on this PC for the browser pass, so you may see only "Done."
   If it complains, paste the error to me.
5. Start the app: `.\scripts\start-local.ps1` (it opens the browser). When you are done: `.\scripts\start-local.ps1 -Stop`.

## Try it
1. **Group switch.** Open a group, tap its settings. Find **Count my shares in my budget** (on by default). Turn it off, add an expense, turn it on again, add another. Only the expense saved while it was on counts in your Budget. Older expenses do not change.
2. **From groups.** Tap **Budget**. Under your own entries there is a block **From groups** with your share of each group expense (small label with the group name). Tap one: it opens that expense in the group. The card total and the category tiles include these shares. MKD and EUR stay on separate lines.
3. **One total (optional).** Settings, card **Budget**, **Show one total**: choose **In MKD** or **In EUR**. Go to Budget: the card shows one amount "in MKD" with a note that euro amounts are converted. **No, keep MKD and EUR apart** goes back.
4. **Limits.** On the Budget tab tap **Set a limit**, choose **Whole month** or a category, type an amount, **Save** (3 taps and one amount for the first one). A **Limits** card appears with bars: green under 80 %, amber from 80 %, red when over, with a line like "300 MKD over". If euro spending is counted, a note says "includes … converted to MKD".
5. **Change a limit.** Tap a limit row, type a new amount, **Save**. **Remove limit** is in the same sheet.
6. **History (important).** Lower a limit now, then switch to the previous month in the month strip. The previous month must still show the limit it had before. A limit you set now starts this month. Open a limit from a past month: the sheet says "You are looking at … The change starts this month."
7. **Summary.** Under the card tap **Summary** (bottom bar, Budget, Summary: 2 taps). It compares the month you were looking at with the one before: total spent, income, every category with up / down / new and a percentage. Try a month that has nothing before it, and an empty month ("Nothing to compare yet"). Pick August on the month strip, open **Summary**, then tap **Back**: the Budget tab shows August again, not the current month.
8. **Language and theme.** Switch EN / МК and light / dark and look again at all of the above.
9. **Guest check.** Open an invite link in a private window and join as a guest: no Budget tab, no Budget card in Settings, no Count my shares switch.

## What to tell me
- Anything slow, anything that needs more taps than it should, anything cut off or hard to read.
- The Macedonian texts you want changed (the new ones are drafts; the list is in `docs/reports/2026-10-26-phase-15-budget.md`).
- The open decisions at the top of the Phase 15 section in `docs/DECISIONS.md` (see "Needs Filip's decision"): items 8 to 11 in particular.
