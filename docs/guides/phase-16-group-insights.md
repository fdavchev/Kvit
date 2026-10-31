# Phase 16 guide: possible duplicates, group totals, group plan

This is for you (Filip). It says what is new, how to try it on your computer, and how to get it online. No programming knowledge needed. If a step fails, copy the message and send it to Claude.

## What is new
1. **"Is this the same?" card** on the Budget tab. It appears when a group expense of yours looks like something you already typed in yourself (same category, same currency, within 3 days, about the same amount). Tap **Same, merge** and your own entry turns grey ("Merged with ...") and stops being counted, so the money is counted once. Tap **No, it's new** and Kvit never asks about that pair again.
2. **"Not the same"** is a button in the sheet of a grey (merged) entry. It puts the entry back and Kvit never asks about that pair again.
3. **A 4th tab "Totals"** on every group page. It shows what the group spent per category, one card for denars and one for euros (never added together).
4. **A spending plan** at the top of the Totals tab. Only the owner sets it. Everyone sees "412 of 600 EUR", "150 each · 4 people", a bar, and the words "On track", "Near the plan" or "N over".
5. **Two small extras:** "Near the limit" now shows in words on the Budget limits, and a short message appears under the amount field when you type something that is not allowed.

## 1. On your computer (before pushing)

### Docker Desktop
Open Docker Desktop and wait until it says the engine is running.

### The two database changes (migrations)
Both were already applied on your computer by the browser test. To be sure, from the Kvit folder in PowerShell:

1. `docker compose up -d`
   You should see `kvit-postgres` started.
2. `dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api --no-build`
   If everything is already applied you will see "Done." and nothing else. If it applies something, it prints the migration name, then "Done.". Close the API window first if you get a "file is locked" message.

### Start Kvit
`.\scripts\start-local.ps1` starts the database, the API and the website and opens the browser. When you are done: `.\scripts\start-local.ps1 -Stop`.

### Try it (about 10 minutes)
- **The card:** log in, open a group, add an expense (for example Food, 1,200 MKD, split between two people). Go to the Budget tab, tap the round "+", add personal spending in Food for 1,200 MKD dated within 3 days. Reload the Budget tab. You should see the card "Is this the same?" at the top. Tap **Same, merge**: the card goes away, your own entry turns grey with "Merged with ...", and the Spent total drops by that entry (the group share stays).
- **Undo it:** tap the grey entry, tap **Not the same**. The entry counts again and the card does not come back for that pair.
- **Totals tab:** open the group, tap **Totals**. You should see a card with the total and the categories.
- **The plan:** on the Totals tab (as the owner) tap **Set a plan**, type an amount, tap **Save**. You should see the card with the bar. Add expenses until the bar passes 80 % (amber, "Near the plan") and then 100 % (red, "N over"). Tap the card, then **Remove plan** to remove it.
- **On your phone:** open the same address on your phone (same Wi-Fi) and check that the four tabs fit, also in Macedonian. This is the one thing the computer tests could not prove.

## 2. Getting it online
1. **Commit and push** the branch `feat/16-group-insights` (the commit subject is in `docs/guides/phase-16-pr-text.md`). If VS Code says it cannot push, run `git push -u origin feat/16-group-insights` in the terminal.
2. **Open the pull request** on GitHub with the title and description from `docs/guides/phase-16-pr-text.md`. All the steps in its checklist are done, so tick them.
3. **Wait for CI** (the green checks). If something is red, send Claude the name of the failing check.
4. **Merge** it yourself. When the merge finishes, GitHub applies the two new database changes on Neon by itself.
5. **After the merge:** wait a few minutes. The website is online in seconds, but the server (Render) takes a few minutes to update. During that gap the Budget tab and the group pages can show an error. This is normal. Refresh after about 5 minutes.
6. **Check it online:** open the real site, open a group and look at the Totals tab and the plan. Then go to the Budget tab. If something looks wrong, tell Claude.

## 3. Things only you can do later
- Read the Macedonian texts (Claude wrote drafts; marked "not approved" in the report).
- Check the screens on a real phone, including iPhone if you or someone you know has one.
- Decide whether the Totals tab should get small bars under each category (not built).
