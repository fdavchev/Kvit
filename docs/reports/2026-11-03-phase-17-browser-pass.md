# Phase 17 Step 5: real-browser pass of the outbox (2026-11-03)

Branch `feat/17-outbox`. Run by the tester. No production code, no tests and no docs other than this report were changed. Nothing was committed or staged. Screenshots and scripts are in the scratchpad, not in the repository (see "Where the files are").

Labels: **VERIFIED by live run** (done in a real headless browser against the local app, result read or screenshot looked at), **NOT VERIFIED**. The automated suites (`dotnet build`, `dotnet test`, `npm test`, `npm run lint`, `npm run build`) were **not run**, so nothing here is VERIFIED by automated test.

## What was run and how
- Docker Desktop open. `.\scripts\start-local.ps1 -NoBrowser` started the database, API (`http://localhost:5018`) and website (`http://localhost:5173`) with no migration warning. `__EFMigrationsHistory` ends with `20261027124121_GroupPlan`, `20261027092926_DuplicateAnswers`, `20261024121657_BudgetLimits` (nothing applied by me). VERIFIED by live run.
- At the end I stopped the API process I had restarted myself, then `.\scripts\start-local.ps1 -Stop` printed "Closed the 'Kvit API' window", "Closed the 'Kvit website' window", "Stopped the database container kvit-postgres (its data is kept)" and "Ports 5018 and 5173 are free". `netstat` showed nothing listening on 5018 or 5173 and `docker ps -a` showed `kvit-postgres Exited (0)`. `git status --short` showed nothing before this report was written. VERIFIED by live run.
- Playwright for Python, headless Chromium only, time zone `Europe/Skopje`, machine date 2026-11-03. Language through the browser locale (`en-US` / `mk-MK`; the app takes it from the browser when nothing is saved), theme through the browser colour scheme, reduced motion through the browser emulation. One run used the Settings language buttons.
- Throwaway users (password `Passw0rdA`, `@example.com`): Ana Alfa (owner), Bojan Beta (member), Cvetan Gama (second person on the same browser); guest Gosho Guest joined through the invite link. Sign-up and guest join were real, driven through the app's own API. Groups "Outbox MKD" and "Outbox EUR" (Ana, Bojan, plain name Marko Plain, two seed expenses each) plus groups made for scenarios D and the closing bar. Every save under test was made through the real screens (Add expense, One bill, Budget spending and income, "Mark paid" and "Record another payment"). The refused cases in D were made by the owner through the API.
- Sign-up is limited to 5 per 10 minutes per address and the counter lives in the API process. My first recon runs hit that limit once (4 or 5 throwaway sign-ups), so I restarted the API (this resets the counter) and then made the 3 sign-ups of the seed. Nothing outside the throwaway data was touched.
- "Offline" means Playwright `context.set_offline(True)` (this sets `navigator.onLine` to false). "Network failure" means a Playwright `route` that aborts every non-GET request with the browser still "online". "API stopped" means the real `Kvit.Api.exe` process killed and started again (the database stayed up).
- Scenario A to J are all run. The matrix for I: 320x640, 360x740, 768x1024, 1280x800 and 1920x1080, EN and MK, light and dark, 7 screens (Home, Groups, Budget, Settings, group Expenses, Balances, Totals) with 6 waiting items, each measured at the top and at the bottom of the page (36 views at 320, 36 at 360, 36 at 768, 12 Balances views re-run at 320 / 360 / 768, 28 views at 1280 and 28 at 1920 in the final run), plus the failed bar on Home and the group Expenses tab (8 views per width).

## BUGS
I did not fix anything. Numbered by how much they hurt.

**Bug 1 (high, existing behaviour made visible): with the phone offline, "I paid" to a person who has an account shows no error, and after Cancel the payment is created anyway when the network returns.**
- Steps: be in a group where Ana owes or is owed. Go offline (airplane mode; Playwright `set_offline(True)`). Open "Record another payment", choose "I paid", choose "Bojan Beta" (has an account), type 10, tap Save. Wait. Tap Cancel. Go online.
- Expected (by the task and by the plan): the payment is not queued and "shows the network error as before".
- Actual: no error at all. The sheet stays open with a dimmed Save button for as long as the phone is offline (waited 1.5 s in the run, no text appeared; screenshot `shots\b-ipaid-offline.png`). After Cancel and going online the request is sent: the API then listed a new payment "Ana Alfa to Bojan Beta, 1000 minor (10 MKD), Pending, created 10:15:29" that nobody confirmed on screen. Before the run the group had 3 payments, after it 5 (the second one is the real "Marko paid me 123"). With a network failure while the browser is still "online" the same save shows the error as expected ("The server may be waking up. Check your connection and try again in a moment.", sheet stays open, nothing queued, nothing sent later; checked with the abort route).
- Cause (read, not changed): `src/web/src/core/api/queryClient.ts` sets only `mutations: { retry: false }`, so the data library's default `networkMode: 'online'` holds every mutation while `navigator.onLine` is false and starts it when the network returns; Cancel closes the sheet but not the held mutation. This was already so before Phase 17 (the Phase 16 report, Observation 2, saw the same with the plan sheet), but the outbox now makes it a visible hole: queueable saves are handled, every other save still waits silently.
- Proposed fix: `mutations: { retry: false, networkMode: 'always' }` in `createQueryClient`, so a save while offline fails at once with the existing "no connection" error (the code already turns that into `ApiError` with no status). That also fixes Bug 2 and most of Bug 3. Add a test that a payment saved while `onlineManager` is offline shows the error and sends nothing later.

**Bug 2 (medium): with the phone offline, a queueable save takes about 3.4 s to close instead of closing at once.**
- Steps: go offline, add an expense (or One bill, spending, income, "Marko paid me") and tap Save.
- Expected: the item is queued as soon as the network failure is known, like with the API stopped (the sheet or screen moved on 57 to 92 ms after the tap).
- Actual (tap to moved on, measured): EUR expense 3,424 ms, MKD expense 3,421 ms, "Marko paid me" 3,640 ms, One bill 3,546 ms, spending 3,391 ms, income 3,496 ms. The Save button spins for 3 s and then the item is queued by the 3-second timer. The original save is never sent (it is held); only the outbox's own attempt, made right after the item is queued, reaches the browser's "disconnected" error.
- Cause: same as Bug 1 (the original save is held, so only the 3-second timer in `saveOrQueue.ts` can queue it).
- Proposed fix: the same `networkMode: 'always'`; then the first attempt fails at once with a network error and the existing "queue at once" branch runs. Re-measure: expect under 150 ms.

**Bug 3 (medium): when the network comes back, the held original saves fire at the same moment as the outbox, so the same item is POSTed twice and the one-at-a-time order is not kept.**
- Steps: queue six items offline (scenario B), then `set_offline(False)`.
- Expected: the outbox sends one item at a time, oldest first, one request per item.
- Actual: 10 POSTs for 7 saves. Eight POSTs left within 2 ms of each other (t = 0.005 to 0.007 s after going online) and two more followed (0.14 s and 0.22 s): the EUR expense, the MKD expense and the spending entry were each sent twice with the same `clientRequestId`. The settlement, the One bill and the income entry were saved by the held original first and the outbox then dropped them (`onLateSuccess`). The first POST of two of the pairs was answered only after 2.09 s and 2.11 s (the others in 30 ms; I did not find out why). No duplicate data: every item exists once (expenses +1 +1, entries +2, One bill once, settlements +2 of which one is Bug 1). The bar went from 6 waiting to 0 in 0.2 s and showed "All up to date ✓" for 3.0 s as designed.
- Cause: same as Bug 1 (the held mutations resume when `onlineManager` goes online).
- Proposed fix: the same `networkMode: 'always'`. After it, re-run scenario B and check the sends are one at a time (scenario B2 below already shows the correct order when nothing is held).

**Bug 4 (medium): the 3-second limit counts the refresh requests after the save, so a save that took 30 ms can be shown as "not synced yet" and is sent a second time.**
- Steps: add an expense while the refresh request `GET /api/groups/<id>` is slow (I delayed it by 4 s inside the page; on a phone a slow network or a waking database does the same).
- Expected: the POST answered in 30 ms, so the normal path: toast "Expense saved", back to the group.
- Actual: after exactly 3.4 s the screen moved on with no toast, a greyed "Not synced yet" row and the bar "1 change waiting · Sending…"; the sender POSTed the same id again at 3.04 s (answered 200 at once); the bar said "All up to date ✓" about 4 s after the screen had moved on (it waited for the same slow refresh). One expense in the database.
- Cause: `useQueuedSave.saveOrQueueItem` races the whole `mutateAsync(...)`, and the mutation's `onSuccess` (for example `refreshAfterExpenseChange` in `features/expenses/add/hooks/useCreateExpense.ts`) is awaited before `mutateAsync` resolves, so the 3 seconds cover POST plus refetch.
- Proposed fix: start the 3-second timer for the POST only (resolve `saveOrQueue` when the POST answers) and let the refresh run after the screen has moved on. This also removes a source of wrongly queued saves on a slow phone connection.

**Bug 5 (low): a save that is only slow (no answer after 3 s) is sent twice at once; the plan says the sender waits for the original.**
- Steps: delay the reply of an expense save by 10 s (the server saves it at once; the reply is held).
- Expected (plan, "Save or queue"): "The sender waits for it first: if it succeeds, nothing is sent again."
- Actual: at 3.03 s the sender POSTed the same id (200 in 30 ms); the held original answered at 10.1 s. The same when the original is held before it reaches the server (second POST at 3.04 s; the original reaches the server at 10.07 s and returns the first result). One row each time, so the result is right; the cost is one extra request for every save slower than 3 s, sent to a server that may just be waking up.
- Cause: `useQueuedSave.saveOrQueueItem` calls `runWithoutWaiting(sender)` right after the item is queued; nothing makes the sender wait for the in-memory original.
- Proposed fix: either make the sender skip an item whose original request is still running (the plan's wording), or change the plan's text to "the id makes the second request harmless" and accept the extra request.

**Bug 6 (low to medium): the back-off counts attempts, not time, so six queued saves already put it at 60 s, and nothing tells the app that the server is back.**
- Steps: stop the API, queue one expense and one spending, start the API.
- Expected: sent within a few seconds of the server answering.
- Actual: the retries ran at +5.05, +15.03, +30.01, +60.02, +60.02, +60.01 s (the 2 s step was cut short because the second save started a new attempt; a clean run with one item gave attempts at +1.96, +7.00 and +22.03 s, so 2, 5, 15 are right). The API answered `/health` again 5.1 s after I started it (at 214.6 s); the next retry only came at 258.7 s, so the items waited 44 s although the server was up. Then both items were sent in order (expense 0.62 s because the API was cold, spending 0.22 s) and "All up to date ✓" showed for 3.0 s.
- A second way to reach the cap: every new save while the server is down runs one more failed attempt, so after six saves the wait is already 60 s. In scenario B2 (six saves, then the block lifted) the queue emptied 59.3 s later.
- Cause: `failedAttempts` in `core/outbox/outboxSender.ts` counts every failed attempt, including attempts that new saves cause.
- Proposed fix (needs Filip's choice, see questions): count back-off steps by elapsed time (do not raise the counter when the new attempt was triggered by a new save), and keep the 60 s cap. The browser `online` event and returning to the tab already send at once (checked: an `online` event sent a waiting item within 0.13 s).

**Bug 7 (low, cosmetic): the bar disappears for one frame and comes back with its slide-in when the queue empties.**
- Steps: let a queue empty (every scenario that goes online).
- Expected: "1 change waiting · Sending…" turns into "All up to date ✓" without leaving the screen.
- Actual: the bar element is removed for 9 to 37 ms in the DOM log and then added again with the 200 ms fade and 8 px move; the frame sampler saw one painted frame without the bar (14 ms and 20 ms) in two runs. In scenario C1 it was `[259.725 gone] [259.762 "All up to date ✓"]`.
- Cause: `forgetItem` empties the list one commit before `justFinished` is published in the `finally` block of `sendWaitingItems`, so `OutboxBar.content()` returns null in between.
- Proposed fix: publish `justFinished` together with the last removal, or keep the bar mounted while `isSending` is true.

**Bug 8 (low, cosmetic): for one frame (18 to 22 ms) the real row and the waiting row are both on screen.**
- Steps: stay on the group Expenses tab while the queue sends.
- Expected: the waiting row turns into the real row without a flash.
- Actual (frame sampler, one sample per painted frame): in two of five runs there was a frame with the waiting row still present and the real row already added (expense list 52 to 53 rows, waiting row count 1, then 0 after 18 ms; and 12 to 13 rows with the waiting row for 22 ms). In the other three runs both changes landed in one frame. In scenario B2 the DOM had both for 7 and 10 ms.
- Cause: the refresh (real row appears) and `forgetItem` (waiting row removed) are two separate renders.
- Proposed fix: remove the item and let the refreshed list render in one update (for example remove it inside the same `act`/batch, or hide a waiting row whose `clientRequestId` already exists in the list).

**Bug 9 (low): in Macedonian at 320 px the word «синхронизирано» breaks in the middle in the waiting rows of the group Expenses list, the Groups list and the Budget list.**
- Steps: Macedonian, 320 px wide, queue an expense, a One bill and a spending, open the three lists.
- Expected: the text wraps by word.
- Actual: the text column is 93 px (Expenses and Groups) or 71 to 95 px (Budget) wide and the word is 100 px wide at 13 px, so it breaks as «синхронизиран / о» (screenshot `shots\i-320-mk-dark-g-expenses.png`). The MKD expense row becomes 5 text lines; the tallest waiting row at 320 px (the long-title test row) was 169 px. At 360 px and above it fits. Nothing is cut off and there is no sideways scroll.
- Cause: `src/web/src/features/outbox/components/WaitingRow.tsx`: `break-words` on a column squeezed by the amount (`whitespace-nowrap`).
- Proposed fix: a shorter Macedonian word (for example «Чека праќање»; the wording is a draft, so this is Filip's choice) or let the amount drop under the text at 320 px.

**Bug 10 (low, wording): a waiting payment in EUR says "Euro price set when sent".**
- Steps: queue "Someone paid me", Marko Plain, EUR 5.00 in a group (checked offline with the abort route).
- Actual: the waiting row reads "Marko Plain paid Ana Alfa · Not synced yet · Euro price set when sent · EUR 5.00" (`shots\eur-payment-waiting.png`). A payment has no euro price (`mkd_per_eur` is on expenses and personal entries only).
- Cause: `WaitingRow.tsx` adds that line for every EUR row and `PaymentsSection.tsx` uses the same row.
- Proposed fix: a flag so only expense and Budget rows show it.

**Bug 11 (low, accessibility): the dashed edge of a waiting row is below 3:1 against the page (2.51:1 light, 2.38:1 dark).**
- Measured from real pixels (see "Contrast"). The meaning is also carried by the words "Not synced yet", so nothing is lost for reading, but the edge itself is under the 3:1 asked for in the task. The bar text is at the limit in dark mode: the worst sampled background pixel gives exactly 4.50:1 (the centre of the bar gives 12 to 14:1).
- Proposed fix: a slightly darker dashed colour in light mode and a lighter one in dark mode (about 3:1 against the page).

## OBSERVATIONS (not bugs, for Filip to decide or know)
1. **The dev proxy answers 502, not 500** (answer to the task question). With the API process killed, `curl` and the browser got `HTTP/1.1 502 Bad Gateway`, `Content-Type: text/plain` for GET and POST on `/api/health`, `/api/me` and `/api/personal/entries`. `docs/ARCHITECTURE.md` (Part 2, `errors.ts`) lists 502, 503 and 504 as the proxy's answers when the API is down, so the Vite proxy matches that, and the save is queued at once (92 ms from tap to the screen moving on). I did not test the Cloudflare function itself.
2. **The bar says "Sending…" even when nothing is being sent.** While the API was down for 3 minutes (and while the phone was offline) it read "2 changes waiting · Sending…" the whole time. Not wrong in the sense of the plan, but a person may read it as "something is happening".
3. **When a failed item and waiting items exist together, the bar shows only the failed count** ("1 change couldn't be saved · Review" while 2 items still wait; bar log in D). The waiting count comes back when the failed item is discarded.
4. **The discard sheet text "It has not been saved yet" is shown also for an item that is being sent** (the Discard button is disabled and says "Sending…", correct). During the 3 to 10 s window of scenario F the item may already be saved on the server.
5. **Desktop placement:** the bar is centred on the whole window, not on the content column. At 1280 px it spans x 416 to 864 (window centre 640; the content column starts at the side rail, about 217 px, so its centre is about 748); at 1920 px x 726 to 1194 (centre 960, content about 582 to 1627). It never covers the rail, but it sits over the left list column of a group page (see `shots\i-1280-en-dark-g-expenses-failed.png`, `shots\i-1920-mk-dark-g-expenses.png`). Nothing is hidden when the page is scrolled to the bottom (content bottom 685 against bar top 740 at 1280; 959 against 1018 at 1920).
6. **After the last failed item is discarded the bar leaves at once** (no "All up to date ✓", because nothing was sent in that round).
7. **Tapping a waiting One bill row opens the Discard sheet,** not a "waiting to send" screen as the second-round answer in DECISIONS says; this follows the later planning-round answer ("tapping opens a small sheet with Discard").
8. **Possible toast over the Save button (suspected, not verified):** when two expenses were saved less than 4 s apart without a page load, Playwright waited about 3.6 s for the Save button of the next form to become clickable (the first Save click came at 3.6 s, POST right after). The pattern fits the "Expense saved" toast of the first save sitting over the Save button (4 s default), but I did not look at the pixels. If it is true, a quick second expense could tap the toast instead of Save. Not related to the outbox.
9. **Rail at 1920x1080 with 14 groups:** the last rail item "Settings" has its box 5 px below the window bottom (bottom 1084.9 on a 1080 px window; the Macedonian screenshot shows it fully readable). Caused by my many throwaway groups; not investigated and not related to the outbox.
10. **Retry on a still-refused item** ends as asked (the request is sent, 404 again, the item stays failed and the sheet stays open); I did not test a Retry that succeeds.
11. **Macedonian plural forms 1 and 2 and 6** were seen ("1 промена чека", "2 промени чекаат", "1 промена не можеше да се зачува", "2 промени не можеа да се зачуваат"); 21, 31 and 101 were not.

## Results table
| # | Check | Result | Label |
|---|---|---|---|
| A | **20 awake saves** (5 expenses: 3 MKD, 2 EUR; 3 One bills; 5 spendings; 4 incomes; 3 payments: "Mark paid" x2 and "Someone paid me") with the API running | PASS: 20 of 20 sent one POST, answered 200; the bar never appeared (`barEver` false), 0 waiting rows, nothing queued; toasts as before ("Expense saved" for expenses and One bills, "Saved" for spending and income, no toast for payments); expense and One bill moved on (group page; the new group's page), sheets closed. Database: expenses +3 (MKD) and +2 (EUR), personal entries +9 (15 to 24). Tap to closed, measured inside the page (last Save click to the Save button leaving the page): expense 59 to 87 ms, One bill 25 to 33 ms, spending 327 to 356 ms, income 312 to 342 ms, payments 285 to 295 ms (the sheet kinds include the sheet's close animation); server answer 16 to 32 ms. **Slowest save: 356 ms (spending); slowest server answer 32 ms.** An earlier identical run timed from outside the page gave 1,061 ms for the first payment (includes Playwright waiting for the sliding sheet) and 84 ms server time | VERIFIED by live run |
| B1 | **Offline, six saves** (EUR expense, MKD expense, "Marko paid me", One bill, spending, income) | PASS with Bug 2: no success toast; expense went to its group, One bill to `/groups`, the others closed their sheet; greyed dashed rows "Not synced yet" appeared at once: group Expenses list ("B eur expense ... Not synced yet / Euro price set when sent / EUR 5.50"), Payments list ("Marko Plain paid Ana Alfa / Not synced yet / 123 MKD"), Groups list ("B offline bill / Not synced yet"), Budget list (income and spending rows at the top); bar counts 1, 2, 3, 4, 5, 6 changes in order. IndexedDB held 6 items, all `waiting`, under the person id | VERIFIED by live run |
| B2 | **Numbers while waiting** | PASS: Balances "+2,061 MKD" and the three lines below it unchanged, group Totals "4,371 MKD" unchanged, Budget "Left 6,435 MKD / Income 9,006 / Spent 2,571" unchanged; after sending they changed (Totals 28,803 to 30,037 = +1,234; Budget EUR spent 21.20 to 28.70 = +7.50; Balances +17,694 to +18,368) | VERIFIED by live run |
| B3 | **"N changes waiting, not counted yet" line** | PASS: group Balances and Totals "2 changes" (expense + payment of that group; "1 change" before the payment), Budget "1 change" then "2 changes" (the two personal items), Home "4 changes" (EUR expense, MKD expense, payment, One bill; personal items not counted); after sending the line is gone on all four. Guest sees "1 change" on Balances and Totals | VERIFIED by live run |
| B4 | **"I paid" to a person with an account offline** | FAIL: not queued, but no network error either (**Bug 1**). With a network failure (abort route) it shows the error as before. "I paid" by the owner to a plain name ("Marko Plain") counts right away and is queued (290 ms, one waiting row) | VERIFIED by live run |
| B5 | **Going online** | PASS for the data, FAIL for the order (**Bug 3**): all 6 items saved once; rows turned into real rows; "All up to date ✓" for 2.999 s and gone; no item saved twice (counts above). Not VERIFIED that the requests went one at a time: they did not, see Bug 3 | VERIFIED by live run |
| B6 | **Network failure instead of offline** (abort route, 6 saves: 2 expenses, payment, One bill, spending, income) then unblock | PASS: every save moved on in 132 to 639 ms (no 3 s wait); 6 items stored; on unblock **sent one at a time in creation order**: expense 1 (58.921 to 58.955 s), expense 2 (58.985 to 59.017), payment (59.052 to 59.125), One bill (59.150 to 59.201), spending (59.212 to 59.233), income (59.243 to 59.262), each started after the previous one ended; bar 6 to 0 waiting, "All up to date ✓" 2.999 s; database +2 expenses, +2 entries, +1 payment, +1 One bill, no duplicates. The wait of 59.3 s after the unblock is Bug 6 | VERIFIED by live run |
| C1 | **API process stopped** | PASS: browser sees 502 (Observation 1); expense queued 75 to 92 ms after the tap, spending 373 to 424 ms (includes sheet animation); retries as in Bug 6; after the API came back both items were sent in order and the bar showed "All up to date ✓" 3.0 s; one expense and one entry saved | VERIFIED by live run |
| C2 | **No answer for 10 s** | PASS with Bug 5: the screen moved on after 3,446 ms (reply held) and 3,445 ms (request held), the bar appeared at 3.03 s, the real row at 3.09 s; when the original answered at 10.1 s nothing changed; one row in the list and in the API | VERIFIED by live run |
| D | **Refused items** (Bojan queued 4: "D ok first" in Outbox MKD, "D refused removed" in a group he was then removed from, "D refused deleted" in a group the owner then deleted, one spending; owner removed the member (204) and deleted the group (204) before the network came back) | PASS: sent in creation order, the two good items saved (200), the two refused got 404 `GROUP_NOT_FOUND`, the failed items did not stop the rest (the spending after them was sent 0.18 s in); bar "2 changes couldn't be saved Review" 0.94 s after the trigger; sheet lists each item with title, amount and "This group doesn't exist or you're no longer in it." / MK «Оваа група не постои или повеќе не си во неа.»; Retry sent the request, 404 again, item stayed failed; Discard removed one item (bar "1 change couldn't be saved") and the last (bar gone, IndexedDB empty). Buttons 52 to 53 px high (EN and MK) | VERIFIED by live run |
| E | **Survives reload and close** | PASS: 2 items queued, reload on Budget and on the group (rows and bar back, "1 change waiting, not counted yet"), tab closed and a new page in the same context (bar "2 changes waiting", expense row, IndexedDB 2 items); swapping the session cookie to another person (Cvetan): no bar, no rows, no note, his own queue key separate; his own item stayed his; swapping back to Ana: her 2 items back and sent (`online` event) in 0.13 s; Cvetan's own item was sent when he opened the app | VERIFIED by live run |
| F | **Tap a waiting row** | PASS: Discard sheet "Discard this change? / It has not been saved yet. If you discard it, it is gone." (glass, `shots\f1-discard-sheet-360.png`); Discard removed the row, the bar changed 2 to 1, IndexedDB 1 item, and unblocking sent only the other item (the discarded one never reached the server). For an item that is being sent the Discard button is disabled and says "Sending…" (`shots\f2-discard-sheet-sending.png`). Tapping a waiting One bill row in Groups opens the same sheet | VERIFIED by live run |
| G | **Log-out warning** | PASS: 1 item "1 change hasn't been sent yet. If you log out now it is lost."; 2 items "2 changes haven't been sent yet. If you log out now they are lost." with Log out anyway and Stay; Stay closed the sheet and changed nothing (still on `/settings`, IndexedDB 1 item); Log out anyway with the log-out request failing: toast "The server may be waking up...", still signed in, queue kept, sheet stayed open; Log out anyway with the request allowed: `/welcome`, the person's queue key gone from IndexedDB; signed in again: no bar, nothing sent, the "G expense" never reached the group | VERIFIED by live run |
| H | **Guest** | PASS: guest queued an expense in the group (57 ms), waiting row "Paid by Gosho Guest · Not synced yet", "1 change waiting, not counted yet" on Balances and Totals; navigation has Home, Groups, Settings only (no Budget; `/budget` ends on Settings); the guest log-out sheet shows its own text and the extra line "1 change hasn't been sent yet. If you log out now it is lost." | VERIFIED by live run |
| I1 | **Bar position** | PASS: with a tab bar (Home, Groups, Budget, Settings) the bar sits 3.8 px above the tab bar (320: bar y 506 to 554, tab bar top 557.8; 768: bar x 176 to 592 over a tab bar of x 160 to 608); on group screens without a tab bar at the bottom edge (320: bar y 584 to 632 of 640, 8 px gap); on desktop centred on the window (Observation 5) | VERIFIED by live run |
| I2 | **Nothing hidden, no sideways scroll** | PASS: in all 176 views of the final matrix scroll width equals window width; with the page scrolled to the bottom the last text and the last row end above the bar (for example 320: content bottom 469.7 against bar top 506; 1280: 685 against 740; 1920: 959 against 1018); the "+" button of Home, Budget and the group never overlaps the bar (0 findings); the Budget toast ("Couldn't update repeating income") sits above the "+" and above the bar (360: toast y 426 to 502, "+" 518 to 578, bar 606 to 654, tab bar from 658); the group's status bar ("Finished · read-only, Unlock for changes") ends 20 px above the outbox bar at 320 and 360 and 16 px above at 1280 (`shots\cl-*.png`). Checker note: at 1920 my checker still flagged the side rail's last item (Observation 9), not the content | VERIFIED by live run |
| I3 | **Long Macedonian texts** | PASS except Bug 9: the bar text «6 промени чекаат · Се праќа…» and «1 промена не можеше да се зачува  Прегледај» fit on one or two lines at 320 and 360 (no cut: bar text scroll width equals its width in all views), the failed sheet wraps «Обиди се повторно» to two lines, the log-out text and the failed sheet text wrap without cutting | VERIFIED by live run |
| I4 | **Tap targets** | PASS: bar button "Review" 65.8 x 44 px (EN), «Прегледај» 88.3 x 44 px; Retry and Discard 135 to 214 x 52 to 56 px; Discard sheet and log-out sheet buttons 52 px and Cancel / Stay 48 px; waiting rows at least 65 px high | VERIFIED by live run |
| I5 | **Contrast** (see "Contrast") | PASS for text (waiting row text 7.10:1 light, 9.45:1 dark; bar text at least 4.50:1; failed reason 5.17:1 light, 6.97:1 dark); FAIL for the dashed edge (**Bug 11**) | VERIFIED by live run |
| I6 | **Animations** | PASS: normal motion: the bar uses a CSS animation `outbox-bar-in`, 0.2 s, opacity and `translate` only; waiting rows have no animation; the sheets use opacity and translate transitions. Reduced motion: bar `animation-name: none`, 0 running animations, the row has `transition-property: none`, the sheets have 0 animations. The bar has no exit animation (it vanishes in one frame, see Bug 7) | VERIFIED by live run |
| I7 | **Sheets in small windows** | PASS: failed sheet (2 items), Discard sheet and log-out sheet at 320x440, 360x500, 1280x600, EN light and MK dark: sheet top inside the window (24 px at 320x440 for the failed sheet), bottom at the window bottom, no sideways scroll; at 320x440 the failed sheet's body scrolls by 15 px (EN) or 40 px (MK) and after scrolling the last button ends at 415 to 416 of 440 px (431 and 455 before scrolling) | VERIFIED by live run |
| J | **The 3 seconds** | see below | VERIFIED by live run |

## Contrast (computed from real pixels, 360 px, EN and MK give the same numbers)
Method: screenshot pixels for backgrounds, the browser's computed colour for text, a waiting row blended at 0.8 opacity over the page for its text.

| Item | Light | Dark |
|---|---|---|
| Waiting row text (title, second line, amount) against its card | 7.10:1 | 9.45:1 |
| A normal row for comparison | 12.72:1 | 13.91:1 |
| Waiting row dashed edge against the page | **2.51:1** | **2.38:1** |
| Waiting row card against the page | 1.15:1 | 1.10:1 |
| Bar text against the darkest-to-lightest sampled background (page and list behind it) | 5.09:1 to 12.71:1 | 4.50:1 to 14.08:1 |
| Bar edge against what is behind it | 3.02:1 to 3.61:1 | 3.51:1 to 3.86:1 |
| Failed-sheet reason text against the sheet | 5.17:1 | 6.97:1 |
| Retry / Discard button text against the button | 9.13:1 | 16.82:1 |

## J. The 3-second setting
Numbers:
- **Awake saves (20):** slowest tap to closed 356 ms (a Budget sheet, which includes its close animation), expense 59 to 87 ms; slowest server answer 32 ms (84 ms in the first run). The setting is about 9 times the slowest closed time and about 90 times the slowest server answer. None of the 20 was queued.
- **API stopped (502):** the screen moved on 75 to 92 ms after the tap (expense), 373 to 424 ms for a Budget sheet.
- **Network failure (abort route):** 132 to 639 ms for all six saves.
- **Phone offline:** 3,391 to 3,640 ms (Bug 2, not the 3 s rule's fault).
- **No answer / slow reply:** 3,445 to 3,446 ms (3.0 s plus about 0.4 s of screen change). A save whose POST is fast but whose refresh takes 4 s: 3.4 s (Bug 4).
- **Real wake-up of Render and Neon: NOT VERIFIED** (only the local API and database were available).

Recommendation: keep 3 seconds. A queued save is harmless (the same id returns the first result; every double send in this pass left exactly one row), a warm save is far below 3 s, and 3 s is the time a person waits before the app lets them go on. Two caveats: (1) fix Bug 4 first, otherwise the limit really means "save plus all refresh requests inside 3 s", which a slow phone connection will exceed more often than a slow save; (2) if the first real-server days show many "Not synced yet" rows for saves that were only slow (a database waking after 5 idle minutes can add one to three seconds, which I could not measure), raise it to 5 s. It is one constant (`outboxQueueAfterMilliseconds` in `src/web/src/core/outbox/saveOrQueue.ts`).

## Macedonian drafts (not read by Filip)
All new texts below were only checked for presence, plural form and fit, not for wording:
- «Сѐ уште не е синхронизирано» (not synced yet), «Цената на еврото се одредува при праќање» (euro price set when sent)
- «Се праќа…», «Сѐ е ажурирано ✓», «{{count}} промена чека» / «{{count}} промени чекаат», «{{count}} промена не можеше да се зачува» / «{{count}} промени не можеа да се зачуваат»
- «Прегледај», «Обиди се повторно», «Отфрли», «{{count}} промена чека, сѐ уште не се брои» / «{{count}} промени чекаат, сѐ уште не се бројат»
- «Да ја отфрлиш оваа промена?», «Сѐ уште не е зачувана. Ако ја отфрлиш, се губи.», «Промени што не можеа да се зачуваат»
- «Промени што не се испратени», «{{count}} промена не е испратена. Ако се одјавиш сега, се губи.» / «{{count}} промени не се испратени. Ако се одјавиш сега, се губат.», «Сепак одјави се», «Остани»
- Seen on screen: «Оваа група не постои или повеќе не си во неа.» (refused reason, an older text), the Macedonian label «Откажи» (Cancel) on the Discard sheet, an older text.

## Tap counts measured
- Awake save: unchanged (the same taps as before; no extra tap for the outbox).
- Discard a waiting item: row, Discard = **2 taps**. Retry a failed item: bar "Review", Retry = **2 taps**. Discard a failed item: "Review", Discard = **2 taps**. Log out with waiting items: Settings, Log out, Log out anyway = **3 taps** (one more than before; Stay costs 1 tap).

## NOT VERIFIED
- A real phone (airplane mode, a real mobile network, touch, background and foreground of the app), iOS Safari, Android Chrome, Firefox, a screen reader.
- The Cloudflare Pages Function path (only the Vite dev proxy was used) and a real Render wake-up or Neon resume, so the 3 s setting is not checked against real wake-up times.
- A Retry that succeeds after the cause was fixed; a 500 answer marking an item failed; 401 pausing the whole queue (session ended while items wait); 408, 425, 429 retried by the back-off; items stored while a request is "sending" when the tab is killed.
- Two tabs of the same person open at once (both would send the same ids; the server returns the first result, but I did not run it).
- A guest who makes an account while items wait (whether the queue follows the person); a queued One bill opened through its Discard sheet only (no "waiting to send" screen exists).
- Macedonian wording (all drafts), Macedonian plural forms 21, 31, 101, `{{count}}` above 6.
- The toast over the Save button (Observation 8), the rail at 1920x1080 (Observation 9).
- The group's status bar in the **Closing** state (waiting for other people): the group I made finished at once because the only real member confirmed, so only the **Finished** state of the same bar component was checked against the outbox bar.
- Contrast of the bar over every possible background behind it (the glass panel is translucent; I sampled the page and the list behind it at two scroll positions).
- The automated suites: `dotnet build`, `dotnet test`, `npm test`, `npm run lint`, `npm run build`.

## Questions for Filip (product only)
1. Phone in airplane mode: when you tap Save on an expense, should the screen move on at once with "Not synced yet" (recommended; today it waits 3.4 s with a spinner), and should "I paid" in airplane mode show "No connection, try again" straight away instead of waiting silently (today it waits and can save later even after Cancel)?
2. Example: the server wakes up at 20 s, but the app tries again only at 22 s, 52 s or 112 s. Today the longest wait after the server is back is 60 s (and after six saves in a row it is always 60 s). Is that OK, or should the app also try once when you come back to the tab or tap the bar?
3. The Macedonian line under a waiting row, «Сѐ уште не е синхронизирано», is too long for a 320 px phone (the word breaks in the middle). Do you prefer a shorter line such as «Чека праќање»?
4. On a desktop window should the bar stay centred on the window (today) or sit centred over the content next to the side menu?
5. A waiting payment in EUR now says "Euro price set when sent". Payments have no euro price, so I suggest leaving that line out for payments. OK?
6. Should the bar say "Sending…" only while a request is really running, and otherwise just "N changes waiting"?

## Where the files are
- Scripts, data and screenshots: `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\11c66925-bd89-4c3f-9def-e4fb33a230e3\scratchpad\p17\` (screenshots in `shots\`). Helpers `lib.py`, `lib2.py` (in-page bar log, frame sampler, tap timer), `warm.py`, `i_helpers.py`, `apictl.ps1` (stop and start only the API process); scenario scripts `a.py` (A), `b.py` and `b2.py` (B), `c1.py`, `c2.py`, `c3.py` (C), `d.py` (D), `e.py` (E), `f.py` (F), `g.py` (G and H), `i.py`, `i_seed.py`, `k.py`, `s.py`, `rm.py`, `cl.py`, `wrap.py` (I), `j.py`, `j2.py`, `slowref.py` (J), `n.py`, `ip.py`, `ipm.py`, `eurpay.py` (extra checks). Results as JSON: `a_res.json`, `b_res.json`, `b2_res.json`, `c1b_res.json`, `c2_res.json`, `d_res.json`, `e_res.json`, `f_res.json`, `g_res.json`, `i_res_*.json`, `k_res.json`, `s_res.json`, `rm_res.json`, `cl_res.json`, `n_res.json`; seed ids in `seed.json`. Not in the repository.

## Throwaway data left in the local database
- Users `p17ana1793700433@example.com`, `p17bojan1793700433@example.com`, `p17cvetan1793700433@example.com`, the guest "Gosho Guest" and the sign-up users of my first recon runs (`p17ana<time>@example.com`, 4 or 5 of them), all with password `Passw0rdA`.
- Groups: "Outbox MKD" and "Outbox EUR" with many "A exp", "B ...", "C ...", "C2 ...", "D ok first" (3 times, one per run), "E expense", "F1 keep me", "F2 sending", "J ..." expenses, 6 payments (one of them the unwanted Pending "Ana pays Bojan 10 MKD" from Bug 1); about 12 One bill groups ("A bill 1 to 3" twice, "Bill · 3 Nov", "B offline bill", "B2 bill", "N bill"); three "Outbox D1 ..." groups (Bojan removed) and their deleted "D2" groups; "Closing test" (Finished); personal entries of Ana (about 30 spendings and incomes) and Bojan (3 x 77 MKD) and Cvetan (one 19 MKD Fun entry). Nothing of any other user was touched.

## Summary
| Item | Result |
|---|---|
| A Awake saves | PASS: 20 of 20 sent once, never queued, slowest 356 ms |
| B Offline | Data PASS, **Bug 1 (high)**, **Bug 2**, **Bug 3** (medium) |
| C API stopped, no answer | PASS (502 from the dev proxy), **Bug 4**, **Bug 5**, **Bug 6** |
| D Refused item | PASS |
| E Reload, close, other person | PASS |
| F Tap a waiting row | PASS |
| G Log-out warning | PASS |
| H Guest | PASS |
| I Layout and look | PASS except **Bug 7**, **Bug 8**, **Bug 9**, **Bug 10**, **Bug 11** (low) |
| J 3 seconds | keep 3 s; fix Bug 4 first |
| Bugs | 1 (high), 2, 3, 4 (medium), 5, 6, 7, 8, 9, 10, 11 (low) |
