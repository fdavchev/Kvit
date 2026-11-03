# Phase 17: the outbox

_2026-11-03 · branch `feat/17-outbox` · first phase of Release 3 ("polish")_

## What was asked
Render's free server sleeps after 15 minutes and takes 20 to 60 seconds to wake. Before this phase a Save on a sleeping server just waited and then showed "The server may be waking up…". The outbox lets Filip keep working: what he saves while the server does not answer goes into a queue in the browser, shows in its list at once as "Not synced yet", is sent in order when the server answers, and ends with "All up to date ✓". A change the server refuses stays visible with the reason, Retry and Discard.

## What was built
- **A queue in the browser** (`src/web/src/core/outbox/`): its own IndexedDB database `kvit-outbox`, one list per signed-in person, kept apart from the saved copy of server data. Every item has the request id made on the phone (`ClientRequestId`), so a repeated send never saves twice. A sender sends one item at a time, oldest first, waits 2, 5, 15, 30 then 60 seconds after a failed try, pauses on a 401, marks any other refusal as failed (the rest keeps going), and sends again on app open, when the browser is back online and when the tab becomes visible.
- **What is queued:** new expenses, One bill, new personal spending and income, and a payment that counts right away ("Marko paid me", or the owner paying a plain name). Not queued: edits, deletes, Undo, repeating income and "I paid" to a person with an account.
- **When an item is queued:** the save works as before. If the server gives no answer within 3 seconds, or the browser says it cannot reach the server (no connection, 502, 503, 504), the item moves to the queue and the screen goes on without a "saved" toast. An expense goes back to its group, a One bill to the Groups list, a spending or income sheet closes, a payment sheet closes. The 3 seconds count only the server's answer to the save, not the screen refresh after it. A late answer from the original request removes the queued item.
- **On screen (glue in `src/web/src/features/outbox/`):**
  - Waiting rows (greyed, dashed edge, "Not synced yet", "Euro price set when sent" on EUR expenses and Budget entries) at the top of the group Expenses list, the Budget month list, the group Payments list and the Groups list (a waiting One bill). Tapping one opens a small sheet with Discard.
  - A bar at the bottom: "N changes waiting · Sending…", "All up to date ✓" for 3 seconds, or "N changes couldn't be saved" with Review.
  - A failed-changes sheet with the translated reason, Retry (same id) and Discard.
  - A line "N changes waiting, not counted yet" on the group Balances and Totals tabs, the Budget screen and Home. Balances, totals, limits and plans change only after the server saved the item.
  - A log-out warning when changes are waiting; logging out anyway deletes that person's queue after the log-out worked.
- **Fixes after the browser pass:** all saves run at once when the browser is offline, so an offline save is queued at once and "I paid" offline shows the network error (before, they waited and fired later, even after Cancel); back-off no longer jumps to 60 seconds after many saves; the bar no longer flashes away when the queue empties; a waiting payment shows no euro-price line; the dashed edge has full contrast.
- **Texts:** a new `outbox` namespace in `en.json` and `mk.json`. The Macedonian texts are drafts that Filip has not read.
- **Backend:** no production change and no migration. 33 new backend tests pin how the four save endpoints answer a repeated or late request.
- **Docs:** `docs/guides/phase-17-outbox.md` (plain-language guide), `docs/guides/phase-17-pr-text.md` (first-push texts), `docs/reports/2026-11-03-phase-17-browser-pass.md` (browser pass and re-check), mockup `docs/design/2026-10-31-phase-17/`.

## Verification (labels are exact)
- **VERIFIED by automated test** (run by Claude after the last coder run, 2026-11-03): `npm test` 358 files, 12613 of 12613 (the second full run; the first full run after the last fixes had one failure in an old One bill screen test that passed alone three times, see "Left"); `dotnet test Kvit.slnx` 5975 of 5975 (5942 before, 33 new); `dotnet build Kvit.slnx` 0 warnings; `npm run lint` 0 warnings; `npm run build` 0 type errors.
- **VERIFIED by live run** (headless Chromium, local stack, fresh throwaway users): the first browser pass (11 bugs found) and the re-check of the fixes in `docs/reports/2026-11-03-phase-17-browser-pass.md`. Offline saves now queue within 59 ms and close the screen within 60 ms (sheets 231 to 404 ms with their animation; was about 3.4 s); six queued items go out as exactly six POSTs, one at a time, in creation order; a slow refresh no longer queues a save that the server answered in 30 ms; the bar never vanishes between "waiting" and "All up to date ✓"; 20 awake saves are never queued (slowest tap to closed 374 ms); 96 layout views at 320 and 360 px in EN and MK, light and dark, have no sideways scroll; the dashed edge is 3.29:1 (light) and 3.01:1 (dark) against the page.
- **NOT VERIFIED:** a real phone, airplane mode on a real phone, iOS Safari, Android Chrome, Firefox, a screen reader; the Cloudflare Pages Function path and real Render and Neon wake-up times (so the 3 second setting is only checked locally); the 60 second back-off step; the queue pausing on a 401; two tabs of the same person open at once; a guest who makes an account while changes wait; the Macedonian texts (drafts); bug 4 on a truly throttled network.

## Bugs found and fixed
Browser pass (11 found, 8 fixed, 3 left): fixed were bugs 1, 2 and 3 (offline saves held and fired later), 4 (the 3 seconds counted the refresh), 6 (back-off at 60 seconds after many saves), 7 (bar flash), 10 (euro line on payments) and 11 (dashed edge contrast); left are 5, 8 and 9 (see below). The details, with steps and measurements, are in the browser-pass report.

## Code review (`/code-review high` on the whole branch): 10 findings, NOT fixed, waiting for Filip
Claude checked each against the code. Order is by how much I think it matters.
1. **A waiting Budget entry can crash the Budget screen** (`WaitingEntryRow.tsx`): if the category of a queued spending is no longer in the list (archived or deleted on another device before it is sent), the row throws while rendering and the whole Budget screen fails, on every visit, because the item lives in IndexedDB. Confirmed in the code. Suggested fix: show the row with a neutral tile and no category name instead of throwing.
2. **One request that never answers blocks the whole queue** (`outboxSender.ts`): `fetch` has no timeout, and the sender shares one running promise. A POST that hangs (a stalled cold start) keeps the item "sending", it cannot be discarded, and no trigger starts a new run until the page is reloaded. Confirmed in the code; the browser eventually ends such a request after minutes. Suggested fix: a timeout of about 30 seconds on the queued send, treated as "no answer".
3. **A queued save can be discarded while the server still gets it** (`outboxSender.ts`, `discard`): Discard removes only the local entry. If the original request is still in flight (a save slower than 3 seconds), or an earlier try reached the server and only the answer was lost, the change appears later although the row was discarded, and the discard sheet says "It has not been saved yet". Real, but rare. Suggested fix: change the sheet words ("It may still arrive") or make the sender skip and cancel items whose original is in flight (this is browser-pass bug 5).
4. **If the first load of the queue from the browser database fails, the outbox stays dead until a reload** (`outboxSender.ts`): every later queueing rejects with the same error and the user sees the generic error for a save that may have succeeded. Real, for a slow device or private browsing. Suggested fix: try loading again on the next `enqueue` or `run`.
5. **A rare race can leave an item unsent** (`outboxSender.ts`, `run`): a Retry or a new save at the exact moment the loop finishes can get the old finished run back and start no new one. The window is a few microtasks; I think it is theoretical, but the fix is small (look again after a run ends).
6. **The same "signal when answered" wrapper is copied into four mutation hooks** (`useCreateExpense`, `useCreateOneBill`, `useRecordSettlement`, `useAddPersonalEntry`): maintainability only. A shared helper would remove the copies.
7. **`leaveErrorToQueuedSave` is an empty function** (`useQueuedSave.ts`), which looks like a swallowed error. The reviewer's story that a throw in the success handler goes into it is wrong (the second argument of `.then` handles only the save's own failure, which `useQueuedSave` already stored). The empty function does conflict with your "no silent catch" rule in spirit, and a throw inside the success handler would be an unhandled rejection. Low.
8. **Log-out warning count is read at the moment of the tap** (`SettingsScreen.tsx`): an item queued after the tap but before the log-out finishes is deleted without being counted; and if deleting the queue fails after the log-out, only a generic error shows. Narrow.
9. **After a 401 the sender stays paused** and the bar keeps saying "sending" (`outboxSender.ts`): only if the sign-out does not happen. In the app a 401 signs the person out and the outbox goes away, so this is hypothetical.
10. **Other gateway statuses (520 to 524) make a queued item fail for good** (`outboxSender.ts`): only 502, 503 and 504 are retried. The reviewer assumed Cloudflare adds 52x codes; the Pages Function returns 502 itself when the API cannot be reached and 500 only when it is misconfigured, so this is probably not a problem. NOT VERIFIED against a real sleeping Render.

## What Filip has to do
- Read the code review list above and the "Questions for Filip" in the browser-pass report; tell me which findings to fix (my suggestion: 1, 2 and 4 now, 3 as a wording change, the rest later).
- Commit is done by Claude on this branch (never pushed). When happy: push, open the pull request with the texts in `docs/guides/phase-17-pr-text.md`, wait for CI green, merge. There is no migration in this phase.
- Then follow the plain guide `docs/guides/phase-17-outbox.md` on the real site and on a phone (airplane mode check).
- Read the Macedonian drafts (list in the browser-pass report, section "Macedonian drafts").

## Left in BACKLOG
See `docs/BACKLOG.md` "Phase 17 leftovers": browser-pass bugs 5, 8 and 9, the new Groups list problem when the API is stopped, the wait after the server is back (no "is it back?" check), the bar position on desktop, the "Sending…" wording, the code review findings Filip does not want now, and a One bill test that fails rarely under full-suite load (three different tests in `OneBillScreen.test.tsx` and `OneBillScreenQueued.test.tsx` failed once each in separate full runs and passed alone).

## Decisions and rejected alternatives
Mockup: `design/2026-10-31-phase-17/index.html`. The first version of the outbox is in "Hiding the server wake-up (first version)" below; this entry settles what it looks like.

### Answered by Filip (2026-10-31)
- **1 · Marking a waiting item: C.** A greyed, dashed row with "Not synced yet" in its second line, the same on the group Expenses list, the Budget lists and the Payments list. Rejected: a grey tag (A), a clock icon (B).
- **2 · Waiting count and "All up to date ✓": A.** A bar at the bottom of the screen: "N changes waiting · Sending…", then "All up to date ✓" for a few seconds, or "1 change couldn't be saved" with Review. Rejected: pop-up only (B), a Settings line with a dot (C). Correction found in the code search: today's "Updating…" note sits at the top of the screen, not above the tabs (the mockup text was wrong). The group screens, where expenses and payments are added, have no bottom tab bar, so the bar is built into the signed-in layout (`DesktopRailLayout`): above the tab bar where there is one, at the bottom edge where there is not.
- **3 · Waiting money: A.** Lists show a waiting item at once. Balances, totals, limits, the group plan and the summary change only after the server saved it, with the line "N changes waiting, not counted yet". Rejected: counting it right away as "estimated" (B).
- **4 · Euro price of a waiting EUR item: A.** The server sets it when it saves (today's behaviour); the waiting row says "Euro price set when sent". Rejected: the phone looking it up (B), which would be a guess.
- **5 · A failed item: A.** The rest of the queue keeps going; the failed item stays with its translated reason and Retry (same id) / Discard (asks nothing).

### Answered by Filip (2026-10-31, second round)
- **When an item goes into the queue:** Save works as today; if the server gives no answer within 3 seconds, or the network is down, the item moves to the queue and the sheet closes. A mistake such as a bad amount still shows in the form at once. The 3 seconds is one named setting and a guess: Filip asked if it is enough, answer: a warm save takes under a second, a slow awake server queued by mistake is harmless (the id prevents a second save), and Step 5 checks it live; raise it to 5 if awake saves get queued too often. Alternative: queue every save (mistakes would only show later as failed items).
- **What can be queued:** new expenses, One bill, new personal spending and income, and a payment that counts right away ("Marko paid me", or the owner recording for a plain name). "I paid" waits for the server. Edits, deletes and Undo are not queued.
- **A queued One bill** shows in the Groups list as a waiting row; opening it shows a "waiting to send" screen, not a group.
- **BACKLOG:** none of "Phase 16 leftovers", "Offline editing and deleting through the outbox" and "API error handler" is built here. (Filip said OK to 2 and 3 only; item 1 was his question, so the 3 seconds counts as agreed unless he says otherwise.)

### Answered by Filip (2026-10-31, planning round)
- **Log out with changes waiting: warn first.** A sheet says "N changes haven't been sent yet. If you log out now they are lost." with Log out anyway / Stay. Logging out deletes that person's queue (same one-person rule as the saved copy). Guests get the same line in their existing log-out sheet. Rejected: deleting without asking; keeping the queue for the next sign-in on the same browser.
- **A waiting (not failed) row can be discarded:** tapping it opens a small sheet with Discard. While that item is being sent, Discard is disabled and says "Sending…". Rejected: waiting rows that cannot be tapped.

### Technical choices made in the plan
- **Storage:** its own IndexedDB database (`kvit-outbox`, via `idb-keyval` `createStore`), one key per signed-in person, so the saved-copy wipe in `keepCacheToOnePerson.ts` never touches it.
- **Waiting rows are drawn by each list from the outbox, never written into the query cache,** so a refetch or the saved copy cannot lose or duplicate them.
- **The original request keeps running when the 3 seconds pass;** the sender waits for it, so a save that was only slow is never sent twice (and the id protects it anyway).
- **Back-off:** after a network error, 502/503/504, 408, 425 or 429 the sender waits 2, 5, 15, 30, then 60 seconds. 401 pauses the whole queue. Any other 4xx marks that item failed and the rest goes on.
- **Built in Step 1:**
  - **A 500 answer marks the item failed** (Retry is offered), instead of retrying a possible server bug forever. Only no answer, 502, 503, 504, 408, 425 and 429 are retried by itself.
  - **"Sending" is kept in memory only;** the browser database keeps "waiting" and "failed", so an item left over from a closed tab loads as "waiting".
  - **While the tab is hidden or the browser is offline,** a finished back-off wait does not send; the next `online` or visible signal does.
  - **If the refresh after a successful send fails,** the item is marked failed and logged; Retry re-sends the same id and the server returns the first result.
  - **If the first load from the browser database fails,** the sender fails with a clear error and never shows an empty queue.
  - **`enqueue` does not start sending;** the caller calls `run()`.
- **Built in Step 2:** the queue is used only when the original save fails with no answer or after 3 seconds; the original request keeps running and a late success removes the queued item. If the answer comes after the screen is already gone, Add expense and One bill still go to the group or the groups list (a small change from before; to be looked at in the code review).
- **Built in Step 3:**
  - **Waiting rows sit at the top of their list, newest first,** not placed by date (simpler, and a waiting item is always the newest action). Failed items show only in the bar and the failed sheet, never as rows.
  - **Waiting rows use 80 % opacity, a dashed border and the normal text colour** for the second line, not the mockup's 60 % and grey: that would drop below the 4.5:1 contrast rule on the peach page.
  - **The bar is 64 px of space** (`--outbox-bar-space`) added to the toast offsets, the floating buttons and the screens' bottom padding, only while the bar is on the page.
  - **The log-out warning counts items in every state;** the queue is deleted only after the log-out request succeeded, and a failed log-out keeps it. The count comes from the sender, which loads the saved items at sign-in (a tap in the first milliseconds after sign-in would get no warning; accepted).
  - **Macedonian outbox texts are drafts** that Filip has not read.
- **Browser-pass fixes (Step 5):**
  - **All saves run at once when the browser is offline** (`networkMode: 'always'` for mutations), so offline saves are queued at once, "I paid" offline shows the network error, and nothing is held and fired later.
  - **The 3-second clock covers only the server's answer to the save,** not the refresh after it; a save answered in time is never queued because the refresh is slow.
  - **A failed attempt caused by a new save does not push the back-off up** (it waits 2 s at least, never more because of it); attempts by the timer, the triggers or Retry still go 2, 5, 15, 30, 60 s.
  - **`justFinished` is published with the last removal,** so the bar never vanishes for a frame; a waiting payment shows no euro-price line; the dashed edge of a waiting row has full strength (3.29:1 light, 3.01:1 dark).
  - **Left on purpose for Filip:** a save that is only slow is sent twice at the same moment (bug 5; the server returns the first result, one row results; the plan's sentence "the sender waits for the original" is not built); the real and waiting row share one frame (bug 8); the Macedonian "not synced" word breaks at 320 px (bug 9); the desktop bar position, "Sending…" wording, the wait after the server is back.
- **Backend: no change, no migration.** The four endpoints already return the first result for a repeated `ClientRequestId`, also when the group is Closing or Finished, and none is rate-limited. Known edge, left as it is: a repeat of an item that did save, sent after the group was deleted, the person left, or the item was deleted, is refused (404 or 400). In the outbox that shows as a failed item with its reason, never as a duplicate. New API tests pin this behaviour in Step 4.
