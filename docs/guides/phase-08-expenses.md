# Phase 8: what you do after the merge (plain words)

Nothing here costs money. The two database changes (new tables for categories, exchange rates and expenses) are applied by the pipeline itself when the pull request is merged. You do not run anything on Neon.

1. Merge the pull request on GitHub.
2. Open the **Actions** tab on GitHub and wait until the run on `main` is green. It applies the database changes and then deploys the API. If it is red, tell the Claude session and do not try anything else.
3. Open `https://kvit-mk.pages.dev` on your phone and log in.
4. Open a group and tap the round **+** button. Add an expense in **MKD** with the default split, and save. You should see it in the list.
5. Add a second expense, tap **MKD** next to the amount to switch to **EUR**, and save. Open it: you should see a line like "Rate: 1 EUR = 61,xxxx MKD" with a date. This is the first time the real server asks the National Bank (NBRM) for the rate, so it may take a few seconds. If the rate line is missing or shows an old date for days, tell the Claude session.
6. Look at the circles (Home, Members, the split of an expense): if your Google account has a profile picture you should see it in your circle. If you only see a letter on a colour, tell the Claude session.
7. Delete an expense and tap **Undo** in the red toast. Delete another one and open **Recently deleted** on the group screen to find it and tap **Restore**.
8. Tap **Activity**: you should see a sentence for each thing you just did.
9. Tap **Groups**, then **+**, then **One bill**. Type an amount, add one name, leave the title empty and save. With the app in Macedonian the new group should be called «Сметка · <date>», in English "Bill · <date>".

If anything looks wrong, write down the screen and what you did, and tell the Claude session.
