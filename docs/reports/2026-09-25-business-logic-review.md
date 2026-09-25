# Business and logic review (2026-09-25)

Filip asked for a check of whether the idea and its rules make sense from a business and logic point of view.
This is my judgement based on the plan and the earlier research. Nothing was run, so every point is **NOT VERIFIED** by tests.

## 1. Does the business idea hold up?
**Competition is strong.** Splitwise, Tricount, Splid and Settle Up all exist. Tricount is 100% free, Splid needs no account, and Splitwise limits free use.

**Where Kvit is genuinely different:**
- Macedonian language, and MKD + EUR with the National Bank's rate.
- Splitting **and** a personal budget in one app. Your share of a dinner lands in your budget by itself. The big apps keep these separate.
- No limits and no ads.
- Later: scanning Macedonian fiscal receipt QR codes. None of the big apps can do this, so it's the strongest local selling point.

**The biggest risk: every friend needs an account.** Tricount and Splid need none. The fixes are already in the plan:
- **One person can run the whole group alone** with plain-name members. Your friends don't have to install anything. Say this clearly on the start screen: *"Only you need an account."*
- "Share balances to Viber/WhatsApp" (backlog) lets non-users see where things stand.
- Google sign-in makes the account a one-tap thing.

**Money:** realistically this is a portfolio project that friends use, not a business yet. That's fine. If it grows, possible options are a small paid extra (receipt scanning, export) or donations. The free-hosting terms would have to be checked again then.

## 2. Logic gaps found (suggested fixes, waiting for Filip's OK)
1. **Settlements are not spending.** When you pay Ana back 1,200 MKD, that must **not** count in your budget again, because the dinner was already counted as your share. Rule: **the budget counts only your *share* of expenses, never who paid and never settlements.** If you paid the whole 2,400 dinner, only your 600 share counts. "Marko paid me" is not income either.
2. **Plain-name members and "I paid".** "I paid Grandma" needs Grandma to confirm, but Grandma has no account. Rule: **for a plain-name member, the group owner confirms on their behalf.** "Grandma paid me" counts straight away, as usual.
3. **Which currency is your budget in?** Suggestion: each user picks a **budget currency** in settings (MKD by default). Expenses in the other currency are converted using the rate saved on that expense.
4. **Double counting.** If you log a coffee as personal spending and also in a group, it counts twice. Kvit can't know. Suggestion: none for now. Maybe a gentle hint later.
5. **The owner leaves or deletes their account.** Rule: ownership has to be handed over first. If the owner's account is deleted, ownership goes to the member who joined earliest.
6. **"Who pays whom" (fewest payments)** can suggest that Ana pays Bojan even though they never shared an expense directly. That's normal and correct, but it can confuse people. Suggestion: a one-line explanation under the list: *"Simplified: fewer payments, same totals."*

## 3. Filip's new idea: a group budget with N people
"The owner creates the trip, sets a budget, says how many people, invites them, and the budget splits equally."
There are two different things this could mean:
- **(a) A spending plan.** "Greece trip: 600 EUR planned, 4 people = 150 EUR each." Kvit shows a progress bar ("spent 420 of 600 EUR") and warns when it's close. It's simple and fits Release 2 (budget).
- **(b) A shared pot (kitty).** Everyone puts 150 EUR in up front, one person holds the money and pays from it. This is more complicated (who holds the pot, what's left over, refunds at the end). It goes in the backlog.

**Inviting "in the app like friends":** instead of a full friends system (requests, accept/decline), Kvit can offer **"people you've been in a group with"** when you create a new group. One tap adds them, and there's nothing to manage. Suggestion: backlog (Release 3 or later). Until then, invite links shared through Viber/WhatsApp do the job.
