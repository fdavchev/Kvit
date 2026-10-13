# Phase 9: what you do after the merge (plain words)

Nothing here costs money. One database change (a new table for payments) is applied by the pipeline itself when the pull request is merged. You do not run anything on Neon.

1. Merge the pull request on GitHub.
2. Open the **Actions** tab on GitHub and wait until the run on `main` is green. It applies the database change and then deploys the API. If it is red, tell the Claude session and do not try anything else.
3. Open `https://kvit-mk.pages.dev` on your phone and log in. Open a group that has a few expenses and at least two people.
4. Tap **Balances**. You should see your balance, who pays whom, and an **Everyone** list. Green means someone owes you, red means you owe.
5. Tap **Mark paid** next to a person and **Save**. If you paid a real person, it says it waits for them to confirm. If you were paid, or you paid a name without an account (like Grandma), it counts right away.
6. Ask a friend with an account to open the same group. They should see a **Needs you** card with **Confirm** and **Reject**. Try both on two payments.
7. Look at the **Payments** list at the bottom. Tap **Delete** on one payment: the red toast with **Undo** should appear, and Undo should bring it back.
8. Tap **Record another payment** and record a payment you forgot. Pick who paid, the person and the amount.
9. Switch the app to Macedonian in Settings and look at the Balances tab. Tell the Claude session about any text that sounds wrong.
10. Try to leave a group while someone still owes you: the app should refuse and say why.

If anything looks wrong, write down the screen and what you did, and tell the Claude session.
