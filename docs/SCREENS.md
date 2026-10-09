# Kvit: Features and screens

> Written 2026-09-25 in the building session, from `DECISIONS.md`. Nothing is built yet (NOT VERIFIED by any run).
> **How taps are counted:** a *tap* is a press on a button, chip or list row. A *field* is something typed. Typing itself isn't counted as taps. Opening the app isn't counted.
> Items marked **(proposal)** wait for Filip's OK. Anything marked "layout decided when built" is Filip's call on the day (DECISIONS: dashboard layout).

## Everywhere
- **Phone first:** designed for 360 px width, then grows.
- **Dark mode** follows the phone's or PC's setting by default; Settings can force Light or Dark (2026-10-01).
- **Every text** comes from `en.json` / `mk.json`. `<html lang>` switches to `mk` for Macedonian.
- **Bottom bar (decided 2026-10-03, Phase 7):** Home · Groups · Settings, in the pill look (the active tab is a white pill with icon and name), switched by tapping only. Release 2 adds **Budget** between Groups and Settings.
- **Undo toast** instead of "Are you sure?": every delete shows "Deleted · Undo" for a few seconds.
- **"Updating…"** small note while the server wakes (saved data is already on screen). Built in Phase 8b as a small label just above the bottom tab bar on the main tabs.
- **Errors** show the translated message for the API's error code (`ResultCodes`), never a raw error.

---

# Release 1

## Getting in

### 1. Welcome, `/welcome` (public)
- Logo and the tagline **"Квит сме."**. (The line "Only you need an account. Friends can be just names." was removed 2026-10-03: too long in Macedonian.)
- **Continue with Google** (Google's own button: always white, in light and dark mode, 40 px high, DECISIONS Phase 6).
- A small **"or"** (no lines), then **Sign up with email** · **I already have an account** · a small **Privacy** link.
- **Main action:** Google = **1 tap** + Google's own account picker → dashboard. **First time only:** a name screen (pre-filled from Google) + **Continue** → dashboard (2 taps).
- **Email already has a password account:** a pop-up "This email already has an account." with **Log in with password** (opens Log in with the email typed), Google's button under the label "Use another Google account", **Close**.

### 2b. Name screen after Google, `/signup/google` (public; needs the token from Welcome)
- Title "Almost there", the name field pre-filled with the Google name, hint, **Continue**, back button.

### 2. Sign up, `/signup` (public)
- Name, email, password. **Create account**.
- **3 fields, 1 tap.**
- If the user came from an invite link, it returns to the join screen afterwards (DECISIONS: "the invite is remembered through the sign-up").

### 3. Log in, `/login` (public)
- Email, password. **Log in**. **2 fields, 1 tap.**
- "Forgot your password?" shows a short note: ask Filip to reset it (DECISIONS: Filip resets by hand until email exists). It does not mention Google, because Google can't help a password account.
- A Google-only account that types a password gets a pop-up "This account signs in with Google." with Google's button and **Close**; those tries never count toward the lock.

### 4. Waking up (full screen, only on a device with no saved data)
- *"Waking up the server… This takes about a minute the first time."*
- Everywhere else the saved data shows instantly instead (DECISIONS: "Hiding the server wake-up").

## Home

### 5. Dashboard, `/` (layout decided when built)
Contents from DECISIONS:
- **Needs you:** settlements waiting for your confirmation, groups waiting for your Confirm / Object.
- **You owe / You're owed**, in MKD and EUR separately.
- **Your groups** with your balance in each.
- **Recent activity** across your groups.
- A big **"+"** that adds an expense to the **last-used group** (the group you last added an expense to; if none, the group you joined last; if no groups, it opens "New group").
- **Main action:** add an expense = "+" (**1**) → type the amount → **Save** (**1**) = **2 taps, 1 field**.

## Groups

### 6. Groups list, `/groups`
- Open groups first, then a collapsed **Finished** section.
- **New group** button.
- **Recently deleted:** a quiet link under the Finished row opens its own screen (only groups you own, deleted in the last 30 days): **Restore** on each (Filip, 2026-10-03).

### 7. New group, `/groups/new`
First choice: **One bill** or **Group** (1 tap).
- **Group** (trip, household): name, emoji (suggested, tap to change), currency chip (MKD) → **Create**.
  **2 taps, 1 field.** It opens the new group with an **"Add people"** card: *Share invite link* (1 tap → the phone's share menu) and *Add a name*, as two short lines, each next to its own button: *"No Kvit? Add them as a name."* → **Add a name**, and *"Have Kvit? Share the link and they join with their own account."* → **Share invite link** (Filip, 2026-10-03: this is where newcomers learn it; the link needs an account, a name does not).
- **One bill** (a dinner, a taxi): amount (number keypad), then people's names (type a name, press Enter, it becomes a chip; "me" is already in), optional title, currency chip → **Save**.
  **2 taps; 1 field + one per name.** Afterwards it shows the split and a *Share invite link* button.

### 8. Group, `/groups/:groupId`
- **Header:** emoji, name, status badge (Closing / Finished).
- **Your balance** in each currency, big.
- **Banners** (see screen 17): "Everyone's kvit – close the group?", Confirm / Object, "Finished · read-only".
- **Three tabs:** **Expenses** · **Balances** (screen 11) · **Activity** (screen 13).
- **Expenses tab:** grouped by date. Each row: category emoji, title (or category name), amount, who paid, your share.
- A **"+"** button for a new expense.
- Links to **Members** and **Settings** in the header menu.

### 9. Add / edit expense, `/groups/:groupId/expenses/new` and `…/:expenseId/edit`
The speed screen (DECISIONS "Speed details"):
- The **amount** field is focused with the number keypad open. MKD has no decimal key (MKD is split to whole denars; Filip agreed 2026-09-25).
- **Chips, pre-filled with the most common choice:** Paid by **me** · Split **equally among everyone** · **Today** · the group's **currency** · **Category** (empty in Release 1) · **Title** (optional) · **Note** (under "More").
- **Save.**
- **Main action: 1 field, 1 tap** from inside the group.
- Tapping a chip opens a small sheet:
  - **9a. Split:** four tabs, **Equal · Exact · % · Shares**.
    - Equal: every member with a tick (tap to leave someone out) and **"+ extra"** per person (the hotel case).
    - Exact: an amount per person, with "left to assign: 300".
    - %: a percentage per person, with "left: 12.5 %".
    - Shares: − 1 + steppers.
    - **Done** stays disabled until it adds up.
  - **9b. Paid by:** member list, 1 tap.
  - **9c. Category:** emoji grid, 1 tap.
  - **9d. Date:** Today · Yesterday · calendar.

### 10. Expense detail, `/groups/:groupId/expenses/:expenseId`
- Amount, who paid, date, category, title, note.
- The split per person, with the typed values (extra, %, shares).
- For EUR: the saved rate and its date.
- **History:** "Filip changed 1,200 → 1,500 · 2 h ago" (from the activity feed).
- **Edit** and **Delete** for whoever added it and the owner. Delete shows the Undo toast.

### 11. Balances (tab inside the group)
- Per currency: every member's balance (+ is owed, − owes).
- **Who pays whom:** the simplified list, with the line *"Simplified: fewer payments, same totals."*
- Rows that involve you have **Mark paid**. The owner also sees it on rows involving plain-name members.
- **Pending settlements:** *"Marko says he paid you 1,200 MKD"* → **Confirm** · **Reject** (for the receiver, or the owner if the receiver is a plain name). The payer sees **Cancel** instead.

### 12. Record a payment (sheet)
- Opened from **Mark paid**: the amount and currency are already filled in (editable for a partial payment) → **Save**.
  **2 taps, 0 fields.**
- If you're the **payer**, it saves as *pending* until the receiver confirms. If you're the **receiver**, it counts immediately (DECISIONS: settle-up rule).
- **Record another payment** (at the bottom of Balances): pick the person and the direction, type the amount.

### 13. Activity (tab inside the group)
- Newest first: who did what, when.
- Edits show old → new values. Objections show their reason.

### 14. Members, `/groups/:groupId/members`
- The member list: owner badge, "not claimed yet" on plain names.
- **Add a name** (any member; Filip agreed 2026-09-25): a quiet **+ Add a name** link under the names (like "Recently deleted ›") opens a small sheet: 1 field, 1 tap (2 taps with the link). A name that is already in the group (any letter case) is refused: *"There's already a Marko in this group."*
- **Invite link:** **Share** (1 tap, phone share menu; on desktop, Copy), and **Reset link** for the owner (with Undo).
- **Owner, per member:** Remove (only at zero balance; then a "removed · Undo" toast) · Make owner (only a person with an account) · Undo claim (the person stays in the group; the name becomes unclaimed again).
- **Removed** (small list, owner only): each removed person with **Let back in**.
- **That's me** next to an unclaimed name, for a member who has nothing recorded under their own name yet (fixes a wrong "No, I'm new"; Filip said yes, 2026-10-03).
- **Leave group** (not the owner; only at zero balance). The owner has to hand over ownership first.

### 15. Group settings, `/groups/:groupId/settings`
- **Owner:** name, emoji, default currency, **Delete group** (only when every balance is zero; Undo), **Unlock for changes** (Finished groups).
- **Everyone:** Leave group.

### 16. Join through an invite link, `/join/:token`
- **Logged in:** *"You're invited to Greece trip"* (no inviter name, Filip 2026-10-03), the group's emoji, the member names, and one **Join** button (1 tap). No name field; the name comes from the account.
  - **Only if the group has unclaimed plain names:** *"Are you one of these? Marko · Grandma · No, I'm new"* (1 tap).
  - **Total: 1 or 2 taps.**
- **Not logged in:** the same invite card, then Google (1 tap) or the email sign-up, then **back to this screen**.
- **Already a member:** the link opens the group.
- **Old or reset link:** *"This invite link no longer works. Ask for a new one."*
- **Removed by the owner:** *"You were removed from Greece trip. Ask the owner to let you back in."* (a person who **left** can rejoin with the link).

### 17. Finishing a group (banners on screen 8)
- **Owner, when everyone's kvit:** *"Everyone's kvit – close the group?"* with the checklist of settlements → **Confirm** (1 tap).
- **Other members while Closing:** *"Filip wants to close Greece trip"* → **Confirm** (1 tap) or **Object** (optional reason → **Send**, 2 taps). Nothing else can be changed.
- **Finished:** *"Finished · read-only"*. The owner sees **Unlock for changes**.
- **One bill:** finishes by itself once everyone is kvit; no banner step.

## Account

### 18. Settings, `/settings`
- Name.
- Language: **English / Македонски**.
- Time zone: detected automatically, shown as text, with **Choose manually**.
- **Set a password** (only for Google accounts with no password yet; replaces **Change password** until one is set) → `/settings/set-password`.
- **Theme:** Same as device (default) · Light · Dark (decided 2026-10-01).
- Link to the privacy page.
- **Log out** (last, at the bottom).

### 19. Privacy, `/privacy` (public)
- A plain page in EN and MK: what Kvit stores (account, groups, expenses, usage counts), what it never shows (the admin sees totals only), and who to ask.
- Google needs this link before sign-in goes "In production" (`guides/free-hosting-setup.md`, B4).

### 20. Not found
- A friendly "page not found" with a button to the dashboard.

---

# Release 2 (budget), summary
- **Budget tab, `/budget`:** this month's spending vs. limits, with progress bars per category; your share of group expenses included automatically.
- **Add personal spending:** tap a category → type the amount → **Save**. **2 taps, 1 field.** "+ new category" sits right in the grid.
- **Add income:** once, or repeating (monthly on day N, or weekly).
- **Categories and limits:** custom categories (personal only) and monthly limits.
- **Monthly summary:** totals per category and month vs. month.
- **Possible duplicate prompt:** *"Is this the same as 'Dinner – 1,200' you added on Friday? · Same, merge · No, it's new"*.
- **In a group:** totals per category, and the group spending plan with its progress bar.
- **Settings:** budget currency; per group, "Count my shares in my budget".

# Release 3 (polish), summary
- **Outbox:** queued items show "not synced yet"; when the queue is sent, *"All up to date ✓"*; a failed item shows *"1 change couldn't be saved"* with the reason and **Retry** / **Discard**.
- **Admin statistics, `/admin`** (Filip only): counts per day/week/month/year for a chosen date range. Totals only.

---

# Who can do what (Release 1)
| Action | Who |
|---|---|
| Create a group or one bill | Anyone with an account |
| Add an expense | Any member, while the group is Open |
| Edit or delete an expense | Whoever added it, plus the owner. While Closing: the owner only, and it cancels the closing |
| "I paid X" | The payer (the owner for a plain-name payer) → Pending |
| "X paid me" | The receiver (the owner for a plain-name receiver) → Confirmed |
| Confirm / reject a pending payment | The receiver (the owner for a plain-name receiver) |
| Cancel a pending payment | The payer, while it's pending |
| Add a plain name | Any member |
| Rename, emoji, currency, reset link, remove a member, make someone owner, undo a claim, delete the group, start closing, unlock a finished group | The owner |
| Leave | Any member except the owner, only at zero balance |
| Confirm / object to closing | Every member with an account except the owner |
| See a Finished group | Every member (read-only) |
