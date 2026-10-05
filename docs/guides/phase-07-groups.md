# Phase 7: what you do after the merge (plain words)

Nothing here costs money. The database change for groups (three new tables) is applied by the pipeline itself when the pull request is merged. You do not run anything on Neon.

1. Merge the pull request on GitHub.
2. Open the **Actions** tab on GitHub and wait until the run on `main` is green. It applies the database change and then deploys the API. If it is red, tell the Claude session and do not try anything else.
3. Open `https://kvit-mk.pages.dev` on your phone, log in, tap **Groups**, and create a group.
4. Open the group, tap **Share invite link** and send the link to a second account (another phone, or a private browser window).
5. On the second account: open the link, sign up or log in, tap **Join**. You should land in the group.
6. Back on your phone: open **Members**. You should see the second person. Try **Remove**, then **Undo** in the red toast.
7. Try the Google button on the invite page once (the one thing the automatic tests could not check). If the page does not update after Google signs you in, tell the Claude session.

If anything looks wrong, write down the screen and what you did, and tell the Claude session.
