# Phase 11: what you do after the merge (plain words)

This phase has no database change and costs nothing. After the merge, the pipeline deploys by itself.

1. Merge the pull request on GitHub.
2. Open the **Actions** tab on GitHub and wait until the run on `main` is green. The backend test that failed on every run before (`Active_SecondCall…`) should now pass. If any run is red, tell the Claude session and do not try anything else.
3. Open `https://kvit-mk.pages.dev` on your phone and log in. The first open after a pause can take up to a minute (the server wakes up). You should see a "Waking up the server" text.
4. **Home:** you see two boxes, "You are owed" and "You owe", then your groups as cards, then Recent activity.
5. **New account with nothing:** use a private window and sign up with another email. Home must say "No groups yet" with a New group button.
6. **Only a finished group:** with a group that is Finished and no other group, Home must say "No open groups right now" with one New group button, and Recent activity below it.
7. **The "+" button:** tap it. A short list opens: "Add to <your group>", "New One bill", "Another group…". Add one real expense this way.
8. **Needs you:** ask a friend to mark a payment to you as paid. On your Home, a bar says something needs you. Tap it and press **Confirm** or **Reject** right on Home. Do the same with a closing request from the group's owner (**Confirm** or **Object**).
9. **One real EUR expense** in a real group: it is the first real call to the exchange-rate service. Tell the Claude session if the rate looks wrong or the screen hangs.
10. Switch to Macedonian in Settings and read Home, the Needs you card, the "+" list and the new "Моментално нема отворени групи". Tell the Claude session about any text that sounds wrong. Also tell it if you would rather call the theme setting «Тема» than «Позадина».
11. Look at everything in dark mode too.
12. Use it with your family and friends on something real for a few days. When it works for you, tell the Claude session: **Release 1 is done**.

If anything looks wrong, write down the screen and what you did, and tell the Claude session.
