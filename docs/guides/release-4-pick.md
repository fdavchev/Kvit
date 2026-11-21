# Release 4: which phase first?

Written 2026-11-21 by Claude while Filip was away, so he can pick from his phone. Nothing here is built or planned in detail yet. Each pick becomes its own branch, with a plan Filip approves before any coder run.

Size: **S** = one coder run, **M** = two or three, **L** = a phase like Phase 14 or bigger. Sizes are Claude's guess from the docs, NOT VERIFIED against the code yet (that is the first thing a phase plan does).

## Claude's recommendation
1. **Settings: change your name, theme kept on the account** (S). Today a name typed wrong at sign-up can never be fixed; it is small and touches no money rules. A good warm-up after the long Fix 21.
2. **"Share balances"** (S). One button turns a group's balances into a message for Viber or WhatsApp. Free, no bot, and it is a reminder without building reminders.
3. **Repeating expenses** (M). Rent and subscriptions. Phase 14's repeating income already has the "repeat" machinery to copy.
4. **Forgot password by e-mail** (M). Ends "Ask Filip to reset it". Needs a Gmail account for Kvit with an app password (free, no card), set up by Filip.

## Everything on the list
| Feature | Size | Note |
|---|---|---|
| Settings: change name | S | |
| Theme kept on the account | S | Follows you to another phone |
| Time zone picker | S | Only when the phone gives no usable zone |
| Share balances | S | Phone share menu; WhatsApp / Viber buttons on desktop |
| A tile colour per group | S | Needs a rule for which colour a group gets |
| Rolling amounts (last animation) | S | Filip did not like the first draft; needs a new look first |
| CSV export | S | |
| Leave one expense out of the budget | S | On top of the per-group switch |
| Forgot password by e-mail | M | Gmail app password from Filip |
| Link Google to a password account | M | Log in with the password first, then add Google in Settings |
| Repeating expenses | M | |
| Planned and future payments | M | Also answers the open question: until then, should the personal date picker stop at today? |
| Unspent budget at month end | M | Where the rest goes: savings, next month… |
| Charts and Totals bars | M | |
| "People you've been in a group with" | M | One tap to add them to a new group |
| Partial pending-payment rule | M | Owe 20, 5 waiting: let the other 15 be marked paid. Changes an approved rule |
| "Second Marko" option B | S | Refuse or rename a joining account whose name is already in the group |
| Old statistics after 12 months | S | First old rows about 2027-10, so no hurry |
| Desktop look changes | ? | Filip said "satisfactory for now" |
| Several payers for one expense | L | Touches the money core and every balance |
| Shared pot for a trip | L | A new kind of money flow |
| Reminders | L | E-mail or a Telegram bot |
| Install as an app with notifications | L | Push notifications; the install part already exists since the wake fixes |
| Uploaded pictures | L | Small pictures in Neon; R2 is rejected (card) |
| Receipt QR codes | L | The QR format needs research first |
| Offline editing and deleting | L | Rules for two people changing the same thing |
| Keeping the Render server awake / try Railway | ? | Only if the wake trip still annoys after the Fix 20 live check |

## What to send Claude
Just the name of the feature, for example "Share balances". Claude then reads the code, proposes the plan, and waits for your go.
