# Phase 17 guide: the outbox (saving while the server sleeps)

This is for you (Filip). It says what is new, how to try it on your computer and on your phone, and how to get it online. No programming knowledge needed. If a step fails, copy the message and send it to Claude.

## What is new
1. **Saving never makes you wait for a sleeping server.** If the server does not answer in 3 seconds (or there is no connection), Kvit keeps your change in a small waiting list inside the browser and lets you carry on. It sends the changes by itself, in the order you made them, as soon as the server answers.
2. **A waiting change shows in its list at once**, greyed, with a dashed edge and the words "Not synced yet". It is in the group Expenses list, the Budget list, the group Payments list, or the Groups list (a One bill). Tap it to throw it away (Discard).
3. **A bar at the bottom of the screen** says "2 changes waiting · Sending…". When everything is sent it says "All up to date ✓" for 3 seconds.
4. **If the server refuses a change** (for example the group was finished meanwhile), the bar says "1 change couldn't be saved". Tap **Review**: you see the reason, and **Retry** or **Discard**. The other changes carry on.
5. **Numbers wait.** While changes are waiting, balances, totals, limits and plans do not change. A line says "2 changes waiting, not counted yet".
6. **Logging out with waiting changes asks first.** If you log out anyway, they are deleted.
7. **What is queued:** new expenses, One bills, new personal spending and income, and a payment that counts right away ("Marko paid me"). Edits, deletes, Undo, repeating income and "I paid" to a person with an account still need the server.

No database change, so there is nothing to apply on Neon.

## 1. On your computer (before pushing)

### Docker Desktop
Open Docker Desktop and wait until it says the engine is running.

### Start Kvit
`.\scripts\start-local.ps1` starts the database, the API and the website and opens the browser. When you are done: `.\scripts\start-local.ps1 -Stop`.

### Try it (about 10 minutes)
- **Normal saves are unchanged.** Add an expense: it saves, shows "Expense saved" and goes back to the group, as always. The bottom bar never appears.
- **Pretend the server is asleep.** Keep Kvit open. In the PowerShell window called "Kvit API", press Ctrl+C to stop the API. Now add an expense in a group and tap Save.
  - You should go back to the group at once, without "Expense saved".
  - The new expense is at the top of the list, greyed, with "Not synced yet".
  - The bar at the bottom says "1 change waiting · Sending…".
  - The group's balances do not change and a line says "1 change waiting, not counted yet".
- **Add two more** (a personal spending in the Budget tab, a "Marko paid me" payment on the Balances tab).
- **Wake the server.** Run `.\scripts\start-local.ps1` again (or `dotnet run --project src/api/Kvit.Api` in another window). Within a few seconds to a minute the changes are sent one by one, the rows turn into normal rows and the bar says "All up to date ✓". Nothing is saved twice.
- **Discard:** while the API is stopped, queue a change and tap its grey row: **Discard** removes it.
- **A refused change:** while the API is stopped, queue an expense in a group; in another browser (a second person) finish that group; start the API. Your change should show "1 change couldn't be saved" with the reason; **Review** shows Retry and Discard.
- **Log out warning:** with a change waiting, go to Settings and tap Log out. You should see "Changes not sent yet" with **Log out anyway** and **Stay**.

## 2. On your phone (after it is online)
The best test is the real thing: leave the app alone for 20 minutes so the server sleeps, then open it and add an expense at once. You should go on without waiting, and a minute later the bar says "All up to date ✓". You can also use airplane mode: turn it on, add an expense (it should close at once and show greyed), turn it off, and watch the bar.

## 3. Get it online
There is no migration. Commit and push as usual (the texts are in `docs/guides/phase-17-pr-text.md`), open the pull request, wait for CI to go green, merge.

## What I could not check for you
A real phone, iOS Safari, Firefox, a screen reader, the real Cloudflare and Render path (so the 3 second wait is tested only on your computer), and the Macedonian words, which are drafts for you to read.
