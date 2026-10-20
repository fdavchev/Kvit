# Phase 13 guide: try personal spending, your own categories and the Budget tab

For Filip. Plain steps, no jargon. Needs about 10 minutes.

## Before you start (one time)
1. Open Docker Desktop and wait until it says it is running.
2. Open PowerShell in the Kvit folder.
3. Start the database if it is not running: `docker compose up -d`
4. Apply the three new database changes (they add the tables and rules for personal spending):
   `dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api --no-build`
   You should see three lines starting with "Applying migration" (PersonalCategories, PersonalEntries, PersonalCategoryNameUnique, whichever were not applied yet) and then "Done."
   If it complains about a duplicate key, paste the error to me; it means one person has two categories whose names differ only by capital letters.
5. Start the app: `.\scripts\start-local.ps1` (it opens the browser). When you are done: `.\scripts\start-local.ps1 -Stop`.

## Try it
1. Log in. Open a group if you have one (you need an open group for the Home "+" to show).
2. **Add from the Budget tab:** tap **Budget** in the bottom bar. Tap the round **+**. Tap a category, type an amount, tap **Save**. The entry appears in the list and the "Spent this month" card changes.
3. **Add from Home:** on Home tap **+**, then **Personal spending** (the last item), then a category, type, **Save**.
4. **Currency:** tap MKD next to the amount to switch to EUR. Save one EUR entry, then add another: it now starts in EUR.
5. **Your own category:** in the category grid tap the dashed **+ New**. Type a name, tap **Save**. The new category is chosen for you; type the amount and Save. Use the emoji row or type your own emoji, and pick a colour.
6. **Edit my categories:** in the grid tap **Edit my categories**. Try **Edit**, **Archive** (an Undo toast appears), and **Bring back** in the Archived list. An archived category disappears from the grid, but old entries keep it.
7. **Change an entry:** tap an entry in the Budget list. Change the amount, date, note or category (the **Category** chip) and **Save**. Tap **Delete** to remove it; tap **Undo** in the toast to bring it back.
8. **Months:** tap another month in the strip at the top. You can pick dates in the last 24 months, up to today.
9. **Guest check:** open an invite link in a private window and join as a guest. There should be no Budget tab and no "Personal spending" on Home.
10. Switch the language (Settings) and the theme (light / dark) and look again.

## What to tell me
- Anything that feels slow, needs more taps than it should, or looks cut off.
- The Macedonian texts you want changed (list in `docs/reports/2026-10-20-phase-13-personal-spending.md`, "Open questions").
- Whether you want a second "Edit my categories" link on the Budget tab, and whether the empty Home should get a Personal spending button.
