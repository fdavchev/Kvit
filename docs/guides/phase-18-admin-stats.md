# Phase 18 guide: the Statistics page (only for you)

This is for you (Filip). It says what is new, how to try it on your computer, how to get it online, and how to make your own account the admin. No programming knowledge needed. If a step fails, copy the message and send it to Claude.

## What is new
1. **A Statistics page** at `/admin`, only for an account with the admin role. It shows counts for a date range you pick, never names, amounts or anyone's budget.
2. **How to open it:** Settings has a new row **Statistics**. Only an admin sees it. Anyone else who types `/admin` in the address bar sees the normal "not found" page.
3. **The numbers** (each is a card with a big total, small bars and a line comparing with the period just before):
   - New sign-ups, split into Email, Google and Guest, plus guests who later made an account
   - Active people (different people) and person-days (a person counted once per day), and how many of the people are guests
   - People who joined through an invite link
   - Groups created
   - Expenses added
   - Payments confirmed
   - On top: "Since the start", the totals of everything that ever happened, deleted things included.
4. **Range and grouping:** chips for 7 days, 30 days, This month (the default), 12 months, All and Custom (two dates). Group by Day, Week (starting Monday), Month or Year. Day grouping works for at most 92 days, then the Day chip is grey.
5. **Days are Skopje days.** An event at 00:30 Skopje time on 4 November counts on 4 November.
6. **Your own account is left out of every number** (every account with the admin role is). Your tests do not inflate the statistics.
7. **The earliest date is 1 January 2026.** An earlier start date is refused with a clear message.

There is **no database change**, so there is nothing to apply on Neon. The role tables already existed.

## 1. Make your account an admin (once locally, once online)
Follow `docs/guides/grant-admin.md`. It is two short commands. You stay logged in; the change counts from your next click.

## 2. Try it on your computer (about 10 minutes)
1. Open Docker Desktop and wait until it says the engine is running.
2. In the Kvit folder run `.\scripts\start-local.ps1`.
3. Log in with the account you made admin. Open **Settings**: the **Statistics** row is there. Tap it.
4. **You should see:** the title "Statistics", chips "This month" and "Day" selected, the line "Since the start: ...", and six cards.
5. Tap the other chips. The numbers change; the old numbers fade a little while the new ones load.
6. Tap **Custom**: two date fields appear. Pick a start after the end and you get a short message and the old numbers stay.
7. Log out and log in as a normal user (or open a private window and join a group as a guest): there is **no** Statistics row, and `http://localhost:5173/admin` shows the "not found" page.
8. Your own expenses do not change the numbers: add an expense as the admin, reload Statistics, the "Expenses added" number stays the same.
9. When you are done: `.\scripts\start-local.ps1 -Stop`.

The local database may still hold test accounts and about 3,300 made-up usage rows from the browser check (accounts called `p18admin...`, `p18ana...`, `p18bojan...` and a guest "Gosho Guest"). They are only on your PC. The list of cleanup commands is at the end of `docs/reports/2026-11-04-phase-18-browser-pass.md`.

## 3. Getting it online
1. Push the branch and open the pull request with the texts in `docs/guides/phase-18-pr-text.md`. Wait for CI to go green and merge. No migration runs.
2. After Render has deployed, make your online account an admin: the **online** part of `docs/guides/grant-admin.md`.
3. Open kvit-mk.pages.dev, log in, Settings, Statistics. On the real site the numbers start small, because only real use is counted.

## 4. Things to look at yourself
- A real phone: the cards, the bars and the chips at your thumb's size.
- The Macedonian texts. All drafts are listed in `docs/DECISIONS.md` under "Phase 18", "Macedonian drafts for Filip to read".
- The questions at the end of the Phase 18 report (`docs/reports/2026-11-04-phase-18-admin-stats.md`).
