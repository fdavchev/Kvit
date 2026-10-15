# Phase 10: what you do after the merge (plain words)

Nothing here costs money. One database change (a new table that remembers who confirmed closing a group) is applied by the pipeline itself when the pull request is merged. You do not run anything on Neon.

1. Merge the pull request on GitHub.
2. Open the **Actions** tab on GitHub and wait until the run on `main` is green. It applies the database change and then deploys the API. If it is red, tell the Claude session and do not try anything else.
3. You need two accounts for this check: yours, and a friend's (or a second account of your own in a private window). Open `https://kvit-mk.pages.dev` on your phone and log in.
4. Make a group called "Test". Add one expense, for example 1,000 denars, paid by you and split equally with the friend. Ask the friend to join with the invite link.
5. The friend opens **Balances** and taps **Mark paid**, then **Save**. You open **Balances** and tap **Confirm**. Now everyone is kvit.
6. At the bottom of your group screen a bar appears: **Everyone's kvit – close the group?** Tap **Close the group**. The group now shows a **Closing** label.
7. On the friend's phone, a bar says you want to close the group, with **Confirm** and **Object**. The friend has no "+" button, and no **Leave group** in the group settings, while it is closing.
8. Test an objection first: the friend taps **Object**, types a reason and taps **Send**. The group goes back to normal. Look at the **Activity** tab: it shows the objection with the reason.
9. Close the group again. This time the friend taps **Confirm**. The group becomes **Finished**. Nobody sees the "+" button or any button that changes something. You (the owner) see **Unlock for changes**; the friend does not.
10. Tap **Unlock for changes**. The group is open again.
11. Close it once more, then change the amount of the expense yourself. The closing is cancelled by itself (look at the Activity tab).
12. Try the invite link of a group that is closing or finished with a third account. It should say the group cannot be joined right now, with a **Home** button.
13. Make a **One bill** (for example 600 denars with a name like "Marko"). Record that Marko paid you 300. The bill finishes by itself, and it moves to the **Finished** list on the Groups screen.
14. A brand new group with nothing in it must not offer "close the group?". You can simply delete such a group.
15. Switch the app to Macedonian in Settings and look at the closing bar, the Object box and the Activity sentences. Tell the Claude session about any text that sounds wrong. All Macedonian texts of this phase are drafts.
16. Look at the app in dark mode too: the buttons in the bar and the **Closing** label should be easy to read.

If anything looks wrong, write down the screen and what you did, and tell the Claude session.
