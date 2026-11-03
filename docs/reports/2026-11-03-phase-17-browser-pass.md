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

## Re-check after the fixes (2026-11-03)

Run by the tester on branch `feat/17-outbox` at HEAD `bbede2f` ("Fix the Phase 17 browser-pass findings ..."). Real headless Chromium (Playwright for Python), 360 px wide unless said otherwise, time zone `Europe/Skopje`, local API, Vite dev server and Docker database. No production code, no tests and no docs other than this report were changed; nothing was committed or staged. The automated suites were **not run**, so nothing here is VERIFIED by automated test. Labels: **VERIFIED by live run** or **NOT VERIFIED**.

### What was run and how
- `.\scripts\start-local.ps1 -NoBrowser` started the database, API and website with no migration warning. VERIFIED by live run.
- At the end `.\scripts\start-local.ps1 -Stop` printed "Closed the 'Kvit API' window", "Closed the 'Kvit website' window", "Stopped the database container kvit-postgres (its data is kept)" and "Ports 5018 and 5173 are free". `netstat` shows no listener on 5018 or 5173 (only old `TIME_WAIT` lines, which are not listeners), `docker ps -a` shows `kvit-postgres Exited (0)`. `git status --short` showed nothing before this section was written. VERIFIED by live run.
- Fresh throwaway users made for this re-check (password `Passw0rdA`): `p17bana1793733033@example.com` (Ana Alfa, owner), `p17bbojan1793733033@example.com` (Bojan Beta), `p17bcvetan1793733033@example.com`, and the guest "Gosho Guest" joined by invite link. Groups "Outbox MKD" and "Outbox EUR" (Ana, Bojan, plain name Marko Plain, 2 seed expenses each). Every save under test was made through the real screens.
- "Offline" is `context.set_offline(True)`. "API stopped" is the real `Kvit.Api` process killed on port 5018 and started again (Vite and the database stayed up). Every time is measured in the page (click event to the Save button leaving the page, or the click to the first DOM change of the bar) or from the browser's request log; where a time includes a sheet closing animation I say so.
- Mistake of mine, put right: my first seed run wrote its throwaway-user session files and `seed.json` into the old `p17` scratch folder instead of `p17b` (the path in my copy was still the old one). I copied them to `p17b`, wrote the old `seed.json` back from the text I had printed earlier, and made new session files for the three old users by logging them in again (same users, new cookies). The old scripts, results and screenshots in `p17` were not touched. Nothing in the repository or outside the throwaway data was affected.

### Result per bug
| Bug | Verdict | Label |
|---|---|---|
| 1 ("I paid" offline: no error, paid later after Cancel) | **Fixed** | VERIFIED by live run |
| 2 (offline save took about 3.4 s) | **Fixed** (see the note on sheets) | VERIFIED by live run |
| 3 (double POSTs and wrong order after going online) | **Fixed** | VERIFIED by live run |
| 4 (3 s counted the refresh) | **Fixed**; the slow-POST case still queues at 3 s | VERIFIED by live run |
| 6 (back-off counted saves, long wait after the server is back) | **Half fixed**: the counting is fixed, the wait after the server is back is shorter but is still a timer | VERIFIED by live run |
| 7 (bar flickers when the queue empties) | **Fixed** | VERIFIED by live run |
| 10 (euro line on a waiting payment) | **Fixed** | VERIFIED by live run |
| 11 (dashed edge contrast) | **Fixed** (3.295:1 light, 3.013:1 dark against the page; the dark dash is 2.67:1 against the row's own card, see below) | VERIFIED by live run |

### Bugs 1 and 2: browser offline
Six saves with `set_offline(True)`, then a second identical run. "Tap to waiting row" is the time from the Save tap to the first change of the bar text (the "N changes waiting" count and the greyed row appear in the same render). "Tap to closed" is the time until the Save button left the page; for the three sheets (payment, spending, income) it includes the sheet's own closing animation (awake saves measured 285 to 356 ms in the first pass and 298 to 337 ms in this pass's 20 awake saves).

| Save | Tap to waiting row, run 1 / run 2 | Tap to closed, run 1 / run 2 | First pass (before the fix) |
|---|---|---|---|
| EUR expense | 23 / 21 ms | 60 / 45 ms | 3,424 ms |
| MKD expense | 19 / 18 ms | 42 / 41 ms | 3,421 ms |
| "Marko paid me" (sheet) | 25 / 29 ms | 231 / 269 ms | 3,640 ms |
| One bill | 15 / 17 ms | 40 / 36 ms | 3,546 ms |
| Spending (sheet) | 32 / 59 ms | 289 / 404 ms | 3,391 ms |
| Income (sheet) | 36 / 55 ms | 289 / 394 ms | 3,496 ms |

- Every item is queued within 59 ms of the tap. The tap to closed is 36 to 60 ms for the screens that move on (expense, One bill) and 231 to 404 ms for the three sheets. Run 2 had two sheet times above 300 ms (404 and 394 ms) while the row and bar were already there after 59 and 55 ms, so that extra time is the sheet animation and the machine, not the outbox. Bar counts 1 to 6 in order, no toast, the expense went to its group, the One bill to `/groups`. Screenshot `shots\r1-eur-expense-offline.png` looked at. VERIFIED by live run.
- While offline each save shows two POST attempts in the browser log (the save itself and the outbox's first attempt), both failed at once with `net::ERR_INTERNET_DISCONNECTED`; nothing reached the server. VERIFIED by live run.
- **"I paid" Bojan (has an account), 10, offline:** the network error is in the sheet **25 ms (EN) and 18 ms (MK)** after the tap. EN: "The server may be waking up. Check your connection and try again in a moment." MK: «Серверот можеби се буди. Провери ја врската и обиди се повторно за малку.» The sheet stays open, the bar is absent, no waiting row, IndexedDB empty, one failed POST attempt, nothing queued. After Cancel and going online, 6 s later: no POST at all, the group's payment list 1 before and 1 after (EN and MK), bar still absent, IndexedDB empty. Screenshots `shots\r1b-ipaid-offline-en.png` and `shots\r1b-ipaid-offline-mk.png` looked at. VERIFIED by live run.

### Bug 3: six items, then back online
Run 1 and run 2 (EUR expense, MKD expense, "Marko paid me", One bill, spending, income queued offline; then `set_offline(False)`).

| Check | Run 1 | Run 2 |
|---|---|---|
| POSTs sent after going online | 6 (one per item) | 6 (one per item) |
| Duplicate `clientRequestId` | none | none |
| Order | EUR expense, MKD expense, settlement, One bill, entry, entry | same; amounts 550, 77700, 12300, One bill 45000, 9900, 432100 = creation order |
| One at a time | yes, each started 7 to 42 ms after the previous one ended | yes, 3 to 40 ms |
| Bar | 5, 4, 3, 2, 1 changes waiting, then "All up to date ✓" 0.56 s after going online, gone 3.0 s later | same, 2.39 s after going online (the first POST was answered after 2.1 s, the other five in 20 to 25 ms each; I did not find out why the first was slow, the first pass saw the same once) |
| Database | expenses +1 +1, entries +2, payments +1, One bill group +1 | same |

First pass: 10 POSTs for 7 saves, eight within 2 ms. VERIFIED by live run.

### Bug 4: the 3 seconds and the refresh
- **`GET /api/groups/<id>` delayed 4 s inside the page** (two runs): the POST left after 36 and 45 ms and was answered in 31 and 36 ms. The bar never appeared (`barEver` false), 0 waiting rows, exactly one POST, one expense in the database each time. The "Expense saved" toast appeared 4.10 s and 4.11 s after the tap and the screen was already the group page (the form left after 4.44 s). First pass: moved on at 3.4 s with a greyed row, the bar and a second POST.
- **POST held for 10 s (still queued at about 3 s):** reply held after the server saved (3.05 s: bar "1 change waiting", row at 3.07 s, second POST answered in 40 ms, "All up to date ✓" at 3.13 s) and request held before the server (same times). The screen left the form after 3,458 and 3,429 ms (3 s plus the screen change). One row each in the database. This is unchanged and is what the first pass called Bug 5 (the sender POSTs again at 3.05 s while the first request is still open); that bug was not part of this fix and is still there.
- Note for Filip: after the fix, on a slow refresh the form stays on screen with the Save spinner until the refresh is done (4 s in this test) although the save itself took 31 ms. That is what the task asked for ("toast and navigation after the refresh"), but the first pass proposed letting the refresh run after the screen moves on. See question 1.

VERIFIED by live run.

### Bug 6: API stopped, six saves, API started
Six different saves made while the API process was stopped (One bill, spending, income, EUR expense, MKD expense, "Marko paid me"), each queued at once (tap to closed 28 to 52 ms for the screens, 253 to 327 ms for the sheets). The browser got 502 for every POST. All six items were saved exactly once in both runs (expenses +1 +1, entries +2, payments +1, One bill +1, no duplicate id).

Retry timeline, attempts of the first waiting item (the One bill), seconds on the script clock:

| Run | Attempts | Gaps |
|---|---|---|
| A (API started 10 s after the last save) | 29.24, 31.28, (34.10, 35.22, 35.99, 36.46, 37.28 are the attempts started by the five new saves), 42.30, 57.31 (200) | +2.04 after the first attempt, then +5.02 after the last save's attempt, then +15.01 |
| B (API started 32 s after the last save) | 28.04, 30.08, (new-save attempts 32.91 to 36.35), 41.37, 56.38, 86.38 (200) | +2.04, then +5.02, +15.01, +30.00 |
| C, one item only (clean) | 5.83, 7.88, 12.91, 27.92 (200) | +2.05, +5.03, +15.01 (2, 5, 15 as designed) |

- Counting is fixed: six saves no longer push the wait to 60 s (first pass: all retries at 60 s). New saves no longer raise the step. The first wait after the last save is 5 s and not 2 s in runs A and B because my script needed 8 s for the saves, so one real 2 s retry had already happened and the step was already at 5 s; with one item (run C) the steps are 2, 5, 15.
- Wait after the server is back: run A: `/health` answered at 52.52 s, the queue was sent from 57.31 s = **4.8 s** later, the last item was saved at 60.62 s and "All up to date ✓" showed at 60.68 s (**8.2 s** after `/health`; the last POST, the payment, took 2.15 s on the freshly started API). Run B: `/health` at 73.74 s, first send at 86.38 s = **12.6 s** later, last item at 87.67 s, "All up to date ✓" at 89.76 s (**16.0 s** after `/health`; the screen refresh after the last item took 2.1 s). Run C: `/health` at 23.38 s, sent at 27.92 s = **4.5 s** later, "All up to date ✓" 5.2 s after `/health`. First pass: 44 s and 59 s.
- What still blocks it: nothing asks the server whether it is back. The queue is sent when the current back-off timer ends (steps 2, 5, 15, 30, 60 s after real retries), or at once when the browser fires `online` or the tab comes back. The wait after the server is back is therefore the rest of the current step: 4.5 to 12.6 s in these runs, up to 30 s at step 30 and up to 60 s at the cap (the 60 s step needs about five real retries in a row; I did not wait for it). So this part is **not fixed by design**; the wrong counting is. See question 2.

VERIFIED by live run.

### Bug 7: bar flicker (5 runs)
Runs 1 to 5 (three on Budget with a spending and an income, two on the group Expenses tab with two expenses); every painted frame sampled with `requestAnimationFrame`, plus a mutation log of the bar text.

| Run | Bar text log | Frames without the bar between "N changes waiting" and the end of "All up to date ✓" | "All up to date ✓" visible |
|---|---|---|---|
| 1 Budget | "1 change waiting" 5.053, "All up to date ✓" 5.123, gone 8.124 | none | 3.001 s |
| 2 Expenses | 11.254, 11.343, gone 14.341 | none | 2.998 s |
| 3 Budget | 19.596, 19.686, gone 22.691 | none | 3.005 s |
| 4 Expenses | 25.908, 25.970, gone 28.965 | none | 2.995 s |
| 5 Budget | 34.344, 34.412, gone 37.413 | none | 3.001 s |

The bar element was never absent (no `null` entry in the log) between the waiting text and "All up to date ✓" in any of the 5 runs, and it left only after its 3 s. The same held in the two six-item runs of Bug 3 (the frame log shows the bar present until 3.0 s after "All up to date ✓"). First pass: one frame without the bar in two of five runs. VERIFIED by live run.

### Bug 10: euro line on waiting rows
| Waiting row (EN) | Text | "Euro price set when sent"? |
|---|---|---|
| "Someone paid me", Marko Plain, EUR 5.00 | "Marko Plain paid Ana Alfa / Not synced yet / €5.00" | **no** (fixed) |
| "Someone paid me", Marko Plain, 9 MKD (comparison) | "Marko Plain paid Ana Alfa / Not synced yet / 9 MKD" | no |
| EUR expense in the EUR group | "R10 eur expense / Paid by Ana Alfa · Not synced yet / Euro price set when sent / €7.25" | yes (still) |
| EUR Budget spending (currency button set to EUR) | "Fun / Not synced yet / Euro price set when sent / €3.30" | yes (still) |

In Macedonian the EUR payment row reads «Marko Plain плати на Ana Alfa / Сѐ уште не е синхронизирано / €5,00» with no euro-price line (the MK expense and Budget rows were not checked, see "Not verified"). Screenshot `shots\r10-en-balances.png` looked at. VERIFIED by live run.

### Bug 11: contrast of the waiting row (real pixels, 360 px, EN)
Method: screenshot pixels for the page and the card, the pixel with the most contrast along the top edge of the dashed border, the browser's computed colours for the text, the span's 0.8 opacity blended over the card.

| Item | Light | Dark | Target |
|---|---|---|---|
| Dashed edge pixel against the page (the outside of the row) | **3.295:1** (rgb 156,111,71 on 255,217,168) | **3.013:1** (rgb 114,98,92 on 31,24,22) | 3:1 |
| Dashed edge against the row's own card (the gaps between the dashes) | 3.949:1 | 2.666:1 | for information |
| Title, second line and amount text against the card | 6.94:1 | 9.41:1 | 4.5:1 |
| Card against the page | 1.20:1 | 1.13:1 | for information |

The edge pixel equals the computed `border-color`, so the numbers are exact (the coder's 3.29 and 3.01 are confirmed). The dark dash is only just 3.01:1 against the page and 2.67:1 against the card behind the gaps; I used the page, as in the first pass, because the page is what the border separates the row from. Text stays well above 4.5:1 (the first pass gave 7.10 and 9.45 with a different blending; I now blend over the card, which is where the text is drawn). Screenshot `shots\r11-en-dark-row.png` looked at. Macedonian was not repeated (the colours do not depend on the language). VERIFIED by live run.

### Regression glance
| Check | Result | Label |
|---|---|---|
| 20 awake saves (as scenario A: 5 expenses, 3 One bills, 5 spendings, 4 incomes, 3 payments) | 20 of 20 sent one POST, answered 200; the bar never appeared (`barEver` false), 0 waiting rows, nothing queued; toasts as before. **Slowest tap to closed: 374 ms** (the first expense, 326 ms server time because the API had just been restarted); then 337 ms (a "Mark paid" sheet) and 336 ms (a spending sheet). Slowest server answer apart from the first: 78 ms. Database: expenses +3 (MKD) and +2 (EUR), personal entries +9 | VERIFIED by live run |
| Discard sheet | two waiting expenses, tap one: "Discard this change? / It has not been saved yet. If you discard it, it is gone." with Discard and Cancel; Discard removed the row (bar 2 to 1), the discarded item was never sent, the other was saved; for an item being sent the button reads "Sending…" and is disabled | VERIFIED by live run |
| Log-out warning | 1 item: "1 change hasn't been sent yet. If you log out now it is lost." (Log out anyway 52 px, Stay 48 px), Stay changed nothing; 2 items: "2 changes haven't been sent yet. If you log out now they are lost."; Log out anyway with the log-out request failing: toast, still signed in, queue kept (2 items); with the request allowed: `/welcome`, the queue gone; signing in again: no bar, the "G expense" never reached the group. The guest's sheet shows its own text plus the extra line | VERIFIED by live run |
| Failed sheet | Bojan queued 4 items, the owner removed him from one group and deleted another; "2 changes couldn't be saved Review" 0.89 s after going online; the sheet lists both with "This group doesn't exist or you're no longer in it."; Retry sent the request, 404 again, the item stayed; Macedonian text «Промени што не можеа да се зачуваат … Отфрли / Обиди се повторно» (seen in the second run); Discard one: bar «1 промена не можеше да се зачува Прегледај»; Discard the last: bar gone, IndexedDB empty | VERIFIED by live run |
| No sideways scroll | 96 views: 320 and 360 px, EN and MK, light and dark; Home, group Expenses, Groups, Budget, Balances and Settings; page top and page bottom, with 6 waiting items: document scroll width equals window width in all, the bar is inside the window, the bar text is not cut (scroll width equals width), every waiting row is inside the window. Screenshot `shots\r8-320-mk-g-expenses.png` looked at | VERIFIED by live run |

### New problems and observations (none blocks the fixes)
**New bug A (low; not caused by the outbox as far as I can tell, but I did not check whether it is older than Phase 17): with the API stopped (not offline), the Groups list can show only the error "The server may be waking up ... Try again" and no "New group" link or group rows.**
- Steps: warm up every screen online, stop the API process, open a group, then tap Back to the Groups list.
- Expected: the list that was already loaded stays on screen (as it did with the browser offline in the first pass), so more saves can be made.
- Actual: the whole page is the error text and a "Try again" button (screenshot `shots\r6-debug-no-newgroup.png`; the bar "3 changes waiting · Sending…" is still correct below it). The Budget tab and group pages opened by client navigation still worked. It only cost me a changed route in my script. Not investigated. This is where a person will end up on a real phone when Render is waking up.

**Observation 1 (unchanged, not part of this fix list): the first pass's Bug 5, Bug 8 and Bug 9 are still there.** Bug 5: second POST at 3.05 s for a save slower than 3 s (see Bug 4 above). Bug 8: in the Expenses-tab run 2 there was one frame (16 ms) with the real row added and the waiting row still present (frames `[11.331, 1 waiting, 5 rows]` then `[11.347, 0, 5]`). Bug 9: in Macedonian at 320 px the word «синхронизирано» (100 px) is still wider than its text column (93 px on the Expenses and Groups lists, 71 to 95 px on Budget); the tallest waiting row is still 169 px. No sideways scroll and nothing cut off.
**Observation 2:** the Settings language buttons use the server (`PUT /api/me/language`). With the network failing (abort route) pressing them changed nothing for at least 1.5 s (`lang` stayed `en`); I did not look for an error toast. With the server up the change takes about 65 ms. In one of the two runs of the failed-sheet script the Macedonian text appeared only after the sheet was opened (more than 0.8 s); the second run showed it at once. Not reproduced, so not claimed as a bug.
**Observation 3:** the first POST after a quiet period is sometimes slow on the local API (2.1 s in Bug 3 run 2, 0.1 s in run 1; 326 ms for the first save of the 20-save run). Not investigated; it looks like the API warming up, which is also why a cold Render may need the 3 s limit.

### Not verified
- A real phone (airplane mode, mobile network, touch, background and foreground), iOS Safari, Android Chrome, Firefox, a screen reader. The Cloudflare Pages Function and a real Render or Neon wake-up (only the Vite proxy, which answers 502 when the API is down, was used).
- Bug 4 on a really slow connection (the delay was made inside the page with a patched `fetch`, not with a throttled network).
- Bug 6 at the 60 s cap (the longest step I reached was 30 s) and with the browser `online` event instead of the API process being stopped.
- The Macedonian text of the "Euro price set when sent" line on expense and Budget rows (the screen labels I used for those two flows are English), the contrast numbers in Macedonian, and the bar contrast over every background (not part of this fix).
- The group's status bar in the Closing state, a Retry that succeeds, a 500 answer, 401 pausing the queue, two tabs of the same person, a guest who makes an account while items wait (all as in the first pass).
- The automated suites (`dotnet build`, `dotnet test`, `npm test`, `npm run lint`, `npm run build`).

### Questions for Filip (product only)
1. A slow connection: you tap Save on an expense, the server answers in a blink, but loading the updated group list takes 4 seconds. Today the form stays open with the spinner for those 4 seconds and then "Expense saved" and the group appear together. Should it instead go back to the group at once (the new expense shows when the list arrives)?
2. The server wakes up at 20 s. The app tries again at 22 s, 27 s, 42 s, 72 s and so on, so someone waits a few seconds after the server is back and, in a long outage, up to 30 or 60 s. Is that fine, or should a tap on the bar ("N changes waiting") also try again at once?
3. With the server unreachable the Groups list shows only "The server may be waking up. Try again" (New bug A) and nothing else. Do you want the groups you already saw on that phone to stay on screen, so you can keep adding?

### Where the files are
Scripts, results and screenshots of this re-check: `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\11c66925-bd89-4c3f-9def-e4fb33a230e3\scratchpad\p17b\` (screenshots in `shots\`). Copies and adaptations of the first pass's helpers (`lib.py`, `lib2.py` with a new in-page click, bar and error-time log, `warm.py`, `i_helpers.py`, `apictl.ps1`, `a.py`, `c2.py`, `d.py`, `f.py`, `g.py`) plus `seed.py` (3 users, 2 groups), `r1.py` (offline saves and going online, Bugs 2 and 3), `r1b.py` ("I paid" offline, EN and MK, Bug 1), `r4.py` and `c2.py` (Bug 4), `r5.py` (Bug 7), `r6.py` and `r6c.py` (Bug 6), `r10.py` (Bug 10), `r11.py` (Bug 11), `wrap2.py` (no sideways scroll), `restore_old_states.py`. Results as JSON next to them (`r1_run1_res.json`, `r1_run2_res.json`, `r1b_en_res.json`, `r1b_mk_res.json`, `r4_res.json`, `c2_res.json`, `r5_res.json`, `r6_A_res.json`, `r6_B_res.json`, `r10_res.json`, `r11_res.json`, `r8_res.json`, `a_res.json`, `d_res.json`, `f_res.json`, `g_res.json`). Not in the repository.
Throwaway data left in the local database: the three `p17b...@example.com` users and the guest, their groups "Outbox MKD" and "Outbox EUR" with many "A ...", "B ...", "R ...", "R4 ...", "Z5 ...", "C2 ...", "D ...", "F ...", "H ..." expenses, about 14 One bill groups, a few payments, and about 30 personal entries.

## Final re-check after the review fixes (2026-11-03)

Run by the tester on branch `feat/17-outbox` at HEAD `ef29a1a` ("Fix the Phase 17 code review findings and leftovers ..."). Real headless Chromium (Playwright for Python), 360 px wide unless said otherwise, time zone `Europe/Skopje`, local API, Vite dev server and Docker database. No production code, no tests and no docs other than this report were changed; nothing was committed or staged (`git status --short` was empty at the end, before this section was written). The automated suites were **not run**, so nothing here is VERIFIED by automated test. Labels: **VERIFIED by live run** or **NOT VERIFIED**.

### What was run and how
- `.\scripts\start-local.ps1 -NoBrowser` started the database, API and website with no migration warning. VERIFIED by live run.
- At the end I stopped the API process I had restarted myself (the script's `-Stop` found and stopped it), then `.\scripts\start-local.ps1 -Stop` printed "Closed the 'Kvit API' window", "Closed the 'Kvit website' window", "Stopped the database container kvit-postgres (its data is kept)" and "Ports 5018 and 5173 are free". `netstat` shows no listener on 5018, 5173 or 5180 and `docker ps -a` shows `kvit-postgres Exited (0)`. VERIFIED by live run.
- Fresh throwaway users (password `Passw0rdA`): `p17cana1793738352@example.com` (Ana Alfa, owner), `p17cbojan1793738352@example.com` (Bojan Beta), `p17ccvetan1793738352@example.com` (Cvetan Gama, the "other person" of check 8). Groups "Outbox MKD" and "Outbox EUR" (Ana, Bojan, plain name Marko Plain, 2 seed expenses each). Every real save was made through the real screens; the layout checks (4, 5, 6) used items written by hand into IndexedDB `kvit-outbox` (store `items`, key = Ana's person id, same shape as the app's own items, copied from what the app writes).
- One extra tool: for the "one error per bad item" count I built the production bundle into the scratchpad with `npx vite build --outDir <scratchpad>\p17c\prod` (nothing written into the repository, `git status` stayed empty) and served it with `npx vite preview --port 5180` (the same `/api` proxy), because the dev server runs every effect twice (React StrictMode) and would double the console errors. The preview process was stopped afterwards.
- "Offline" is `context.set_offline(True)`. "API stopped" is the real `Kvit.Api` process killed and started again. "Blocked" is a Playwright route that aborts non-GET requests (`connectionrefused`). Times are measured inside the page (mutation log and a `requestAnimationFrame` frame sampler) or from the browser's request log.

### Result per check
| # | Check | Verdict | Label |
|---|---|---|---|
| 1 | Held original (browser-pass bug 5, review 3) | **Fixed** | VERIFIED by live run |
| 2 | Discard sheet text EN and MK | **Fixed** | VERIFIED by live run |
| 3 | Tap the bar; "Sending…" only while a request runs | **Fixed** (a note on the server speed) | VERIFIED by live run |
| 4 | Desktop bar position, phone widths unchanged | **Fixed** | VERIFIED by live run |
| 5 | Macedonian 320 px waiting rows | **Word no longer breaks, but a NEW overflow bug appeared** (New bug A) | VERIFIED by live run |
| 6 | Bad stored items | **Fixed** (one error per item in the production build) | VERIFIED by live run |
| 7 | Hung request | **Fixed** | VERIFIED by live run |
| 8 | Log-out per ids | **Fixed** | VERIFIED by live run |
| 9 | Bug 8 flash | **Fixed on the group Expenses list and the Groups list; still there on Budget and Payments** | VERIFIED by live run |
| 10 | Regression | **PASS except the sideways scroll at 320 px (New bug A)** | VERIFIED by live run |

### 1. Held original
The reply or the request of the first POST of an expense is held 10 s by a route. "Tap to moved on" is the time until the Save button left the page. Every run: EN, 360 px, group "Outbox MKD", one expense.

| Case | Tap to moved on | Bar and row | POSTs from the browser | Original answered | Result |
|---|---|---|---|---|---|
| Reply held (server saved at once, reply held 10 s) | 3,425 ms | bar "1 change waiting" at 3.04 s, row "C1 reply_held / Paid by Ana Alfa · Not synced yet / 333 MKD" | **exactly 1** (route saw 1, request log 1) | 10.10 s (status 200) | bar gone at 10.13 s, row gone, **one** expense with that title via the API |
| Request held (never reaches the server for 10 s) | 3,428 ms | bar at 3.04 s | **exactly 1** (reached the server at 10.05 s) | 10.08 s (200) | bar gone at 10.10 s, **one** expense |
| Original fails (aborted at 5 s) | 3,416 ms | bar at 3.03 s | **2**: the failed original (5.05 s) and **one** sender POST at 5.07 s (200 in 0.06 s) | failed | bar "1 change waiting · Sending…" 5.06 s, "All up to date ✓" 5.17 s, gone 8.16 s, **one** expense |

- First pass for comparison: the second POST left at 3.03 s in the first two cases.
- While the original is held, tapping the waiting row opens the sheet "Discard this change? / It has not been confirmed yet. If you discard it, it may still arrive." with the Discard button **disabled and reading "Sending…"** and Cancel enabled (all three runs; Escape closes the sheet). Screenshot `shots\c1-reply_held-sheet.png` looked at.
- Console in the failing case: one `console.error` "The save of the queued expense ... failed after it was queued; the outbox sends it again" plus the browser's own "Failed to load resource" line (expected, that is the abort). No other messages in the other two cases.
- Note: while the original is held, the bar reads "1 change waiting" without "· Sending…" (the sender is not running, the original is). That fits check 3's wording but a person sees no sign of the open request apart from the Discard button.

### 2. Discard sheet text
- EN: "Discard this change? / It has not been confirmed yet. If you discard it, it may still arrive. / Discard / Cancel" (seen on waiting and on held rows).
- MK (browser locale `mk-MK`, production build): «Да ја отфрлиш оваа промена? Сѐ уште не е потврдена. Ако ја отфрлиш, можеби сепак ќе пристигне. Отфрли Откажи» on the Budget, Expenses, Payments and Groups rows. Both texts are exactly as asked. VERIFIED by live run.

### 3. Tap the bar, and the "Sending…" text
Two runs: stop the API process, queue 2 expenses (EN, 360 px; queued 58 / 39 ms and 59 / 53 ms after the tap), wait 25 s, start the API, poll `/health`, tap the bar button the moment `/health` answers.

| | Run 1 | Run 2 |
|---|---|---|
| Bar while the API is down | "2 changes waiting" (at queue and at 25 s) | same |
| `/health` answered to tap | 0 ms | 2 ms |
| Tap to first POST | **31 ms** | **31 ms** |
| First POST answered | 1.02 s after the tap (200) | 0.94 s (200) |
| Second POST left / answered | 1.19 s / 3.31 s (200) | 1.08 s / 3.19 s (200) |
| "All up to date ✓" after the tap | 3.35 s | 3.24 s |
| Last failed attempt before the tap | 7.4 s earlier (the back-off was at 30 s: the next timer would have fired about 22 s later) | 7.4 s earlier |

- So the tap works: the send starts within 31 ms and does not wait for the back-off. The whole queue took 3.2 to 3.4 s only because a just-started API answered the first POST in 0.9 to 1.0 s and the second in 2.1 s (the same slow first requests seen in the earlier passes, Observation 3 there). "Within about 1 s of the tap" is true for the start of sending and for the first item, not for the second item on a cold API.
- **While the API is down** the bar read "2 changes waiting" for the whole 25 s; "· Sending…" appeared only in six short windows of 3 to 33 ms, each around one failed attempt (each POST answered 502 in 0 to 9 ms). VERIFIED by live run.
- **During a delayed request** (API up, each queued POST held 3 s by a route): "2 changes waiting · Sending…" from 0.04 s to 3.12 s, then "1 change waiting · Sending…" until 6.21 s, then "All up to date ✓". The text had "Sending…" exactly while a request was open (my sampler saw two samples of 1 to 2 ms where the route's sleep had ended and the reply was still on its way). VERIFIED by live run.

### 4. Desktop bar position, phone widths
112 desktop views: 1280x800 and 1920x1080 x EN / MK x light / dark x 7 screens (Home, Groups, Budget, Settings, group Expenses, Balances, Totals) x page top and bottom, with 6 waiting items (EUR expense, MKD expense, payment, One bill, spending, income; sending blocked by a route that aborts only those ids).

- **Centred over the content column:** bar centre minus the centre of the area right of the rail (rail width read from `--rail-width`) is **0 px in all 112 views**. 1280: rail 217.6 px, bar x 525 to 972, centre 749 = content centre 749. 1920: rail 288 px, bar x 870 to 1338, centre 1104 = content centre 1104. The bar's left edge is right of the rail in all 112 views (first pass: centred on the window, 640 at 1280).
- **Covers nothing:** with the page scrolled to the bottom the last text ends above the bar top in every view and **no** link, button, list row or toast overlaps the bar (0 hits at the bottom; 0 fixed elements overlapping anywhere). At the top of the page 40 views have list links under the bar; they are normal scrolling content, not fixed, and move away when scrolling.
- **Toast:** a real Budget save while 6 items waited (1280 EN dark and 1920 MK light): the "Saved" toast sits 14 px and 12 px above the bar, no overlap. Observation: the toast is centred on the window (x 462 to 818 at 1280, centre 640) while the bar is centred on the content column (centre 749), so the two are not aligned. Cosmetic.
- **Phone:** 320x640 and 360x740, tab-bar screens: bar bottom 554 of 640 and 654 of 740 (the same y as in the earlier pass, where it sat 3.8 px above the tab bar; I did not re-measure the tab bar's own top, but `shots\l4-320-en-light-budget.png` shows the bar right above it; `shots\l4-320-mk-dark-g-expenses.png` looked at too); group screens: bar bottom 632 of 640 and 732 of 740, 8 px from the edge. At 360 EN light and 320 MK dark the "+" button and a Budget toast do not overlap the bar (toast 98 px above). Note: with the tap button inside, the waiting bar is now 54 px high (was 48 px in the earlier pass).
- Screenshots looked at: `shots\l4-1280-en-light-g-expenses.png`, `shots\l4-1920-mk-dark-budget.png`.

### 5. Macedonian at 320 px
- The line is now «Чека праќање». In all 112 phone views (EN and MK) **no word is wider than its box** (canvas measurement), so no word breaks in the middle; the text column is **120 px** in the Expenses, Groups, Budget and Balances rows at 320 px (`min-w-30`). The two-line detail «Платено од Ана Алфа · Чека праќање» wraps by word. Tallest waiting row at 320: 110 px (group Expenses, long title), Groups and Budget rows 65 px, Payments 91 px (first pass: 169 px).
- **But the 120 px column pushes the amount out of the row: New bug A below.**

### 6. Bad stored items
Six items written by hand under Ana's id: a Budget spending with a category id that does not exist and a valid amount; an income with only `entryKind`; an expense, a One bill and a settlement with `display: {}`; a spending with an unknown category and no amount or currency. API blocked so nothing is sent. Pages opened one after the other with a full load (EN and MK; MK on the production build too).

| Page | Rows seen (EN) | Crash page? | Console errors per bad item, production build | Dev server |
|---|---|---|---|---|
| Budget | "— / Not synced yet" (neutral tile), "Income / Not synced yet" (income tile, no amount), "— / Not synced yet / 123 MKD" | no | **1 each** (3 items) | 2 each |
| Group Expenses | "— / Not synced yet" | no | **1** | 2 |
| Balances (Payments) | "— / Not synced yet" | no | **1** | 2 |
| Groups | "🧾 — / Not synced yet" | no | **1** | 2 |
| Home | no rows | no | 0 | 0 |

- The error text names the item and what is missing, for example "The waiting expense 9377f859-... is shown with a dash in place of what it is missing: it has no amountMinor, currency, paidByName" and "its category 00000000-dead-beef-0000-000000000001 is not one of the categories". The bar still counted all 6.
- Tapping a bad row opens the Discard sheet (EN and MK text as in check 2); Discard removed the row and the IndexedDB item on Budget, Expenses, Payments and Groups (rows 3 to 2, 1 to 0, 1 to 0, 1 to 0; IndexedDB 6 to 5 to 4 to 3 to 2).
- The dev server shows each error **twice** because React StrictMode runs effects twice in development; the production build shows **once**, which is the real behaviour.
- Observation (not a bug for real data, found by accident): two other hand-made shapes are **not** covered by the dash fallback. (a) A MKD amount that is not a whole number of denars (704 minor): the whole screen crashes ("A screen crashed while rendering ... MKD amount must be whole denars (a multiple of 100 deni), got 704" from `formatMoney`) on the group page. (b) `display.title: null` on a Budget row shows the word "null" ("null · Not synced yet"), because only `undefined` is treated as "no title". The app itself never writes either (an amount comes from the form and the title is `undefined` when empty), so this is only about hand-written or damaged stored data.
- Screenshot `shots\b6-en-budget.png` looked at.

### 7. Hung request
Two items queued (written as real items, page loaded fresh so the back-off starts at step 1); a route holds the POST of the first item and never answers.

| Case | Result |
|---|---|
| First attempt hangs, later attempts pass | POST at 0.60 s, bar "2 changes waiting · Sending…"; at **30.70 s** the bar loses "Sending…" (**30.1 s after the POST**, limit 30 s); next attempt at **32.61 s** (1.9 s after the item went back to waiting = the 2 s back-off); both items answered 200 in creation order (first at 32.61, second at 32.69 s), "All up to date ✓" at 32.79 s, both exist once |
| Every attempt of the first item hangs | in the 2 s after the item is back to waiting (30.7 s) I opened its row: **Discard is enabled** (buttons "Discard" and "Cancel", not disabled) and Discard worked at 31.47 s; the bar went "1 change waiting"; the **second item was sent 2 s later (32.58 s, 200)**, "All up to date ✓" at 32.76 s; only the second item exists |
| Hanging item not discarded (100 s run) | attempts at 0.5, 32.5 and 67.5 s, each ended after 30 s (waiting at 30.7, 62.7, 97.6 s: back-off 2 s then 5 s as designed); the second item stayed waiting behind it the whole time (the queue keeps creation order, so one item that never answers holds the rest; it is only freed by the timeout cycle or by Discard) |

Before the fix (first pass reading of the code): no timeout, the item stayed "sending" for good and could not be discarded. VERIFIED by live run.

### 8. Log-out per ids
- **Part A (2 queued changes):** sheet "Changes not sent yet / 2 changes haven't been sent yet. If you log out now they are lost. / Log out anyway / Stay". Log out anyway: `/welcome` after about 2.5 s, no bar; the person's items are gone from IndexedDB (the person's key stays with an empty list `[]` instead of being deleted, harmless). Signed in as Cvetan on the same browser: no bar, no rows. Signed in as Ana again: no bar, 0 rows, IndexedDB list still empty; the group's expense list has no "H8A ..." expense (nothing was sent). VERIFIED by live run.
- **Part B (item queued after pressing Log out anyway):** two items queued in tab 1, tab 2 (same browser context) loaded and saw "2 changes waiting"; the log-out request was held 6 s by a route; 1.0 s after pressing "Log out anyway" tab 2 queued a third expense ("H8B later", IndexedDB list: 3 items). When the log-out finished (7.6 s after the press, `/welcome`) the IndexedDB list for Ana held **exactly one item, "H8B later"**: only the two counted ids were removed, the later item was not deleted. VERIFIED by live run.
- By design "H8B later" stays in that browser's storage for Ana and would be sent when she signs in again (the test browser was closed, so it never was).

### 9. Bug 8: waiting row and real row in the same frame
Method: a `requestAnimationFrame` sampler records the number of waiting rows (W) and of real rows of that list (R) whenever either changes; with the list at (W0, R0) before the send, every frame must show W + R equal to W0 + R0 ("both" = bigger, "neither" = smaller). 3 queued items per run, sent with the browser `online` event. 5 runs per list.

| List | Runs | Frames with both rows | Frames with neither |
|---|---|---|---|
| group Expenses | 5 | **0** (every run: (2 waiting, +1), (1, +2), (0, +3) in successive frames) | **0** |
| Groups (One bill) | 5 | **0** | **0** |
| Budget (spending, spending, income) | 5 | **3 of 3 sends in 5 of 5 runs**: the real row is added while the waiting row of the same item is still there; the extra row stays until the next item arrives (for example frames (3 waiting, 8 real) at 93 ms, (2, 9) at 191 ms, (1, 10) at 301 ms, (0, 10) at 306 ms): about **211 to 214 ms** with one row too many in every run | 0 |
| Payments (3 x "Someone paid me") | 5 | **same: 3 of 3 sends in 5 of 5 runs** (191 to 257 ms; in run 4 the last two waiting rows disappeared in one frame) | 0 |

- So the fix works for the group Expenses list (the one asked for) and the Groups list also shows no flash in these runs. **The Budget and Payments lists still flash, and for longer than one frame:** the duplicate row is visible for the whole send (about 0.2 s for 3 items, about 65 to 100 ms per item), not 18 to 22 ms as in the first pass. This is the known leftover in BACKLOG. VERIFIED by live run.

### 10. Regression
| Check | Result | Label |
|---|---|---|
| 20 awake saves (5 expenses, 3 One bills, 5 spendings, 4 incomes, 3 payments), API running | 20 of 20 sent exactly one POST answered 200; the bar never appeared (`barEver` false), 0 waiting rows; toasts as before; expenses +3 (MKD) +2 (EUR), personal entries +9. **Slowest tap to closed 360 ms** (a Budget spending sheet, includes the sheet's closing animation); by kind: expense 60 to 80 ms, One bill 29 to 34 ms, spending 331 to 360, income 332 to 346, payment 292 to 309 ms; slowest server answer 31 ms | VERIFIED by live run |
| Offline, the 5 queueable saves (+ "Marko paid me", 6 saves) | each queued **18 to 77 ms** after the tap (tap to bar change 22, 18, 34, 20, 77, 64 ms; tap to closed 44 to 50 ms for expenses and One bill, 326 / 394 / 447 ms for the three sheets with their animation); no toast; IndexedDB 6 items, all waiting; 13 failed POST attempts while offline, all `net::ERR_INTERNET_DISCONNECTED`, nothing reached the server | VERIFIED by live run |
| Going online | exactly **6 POSTs**, one per item, no duplicate id, **one at a time in creation order** (EUR expense, MKD expense, settlement, One bill, entry, entry; gaps between the end of one and the start of the next 2 to 52 ms); "All up to date ✓" 2.4 s after going online (the first POST took 2.1 s on the cold API); database +1 expense in each group, +2 entries, +1 payment, +1 One bill group | VERIFIED by live run |
| "I paid" to a person with an account, offline | the network error is in the sheet **19 ms (EN) and 18 ms (MK)** after the tap ("The server may be waking up. Check your connection and try again in a moment." / «Серверот можеби се буди. Провери ја врската и обиди се повторно за малку.»), nothing queued, no bar; after Cancel and going online no POST at all, payments 19 before and 19 after | VERIFIED by live run |
| Failed sheet (refused items) | Bojan queued 4 items, the owner removed him from one group and deleted another: "2 changes couldn't be saved Review" 0.94 s after going online; sheet lists both with "This group doesn't exist or you're no longer in it."; Retry sent the request, 404 again, item stayed; MK «Промени што не можеа да се зачуваат ... Отфрли / Обиди се повторно» and the bar «2 промени не можеа да се зачуваат Прегледај»; Discard one: «1 промена не можеше да се зачува»; Discard the last: bar gone, queue empty | VERIFIED by live run |
| No sideways scroll, 96 views (320 and 360 px, EN and MK, light and dark, 6 screens, top and bottom; 112 views with Totals) with the bar and 6 waiting rows | **FAIL at 320 px on the Budget screen: 8 views** (EN: document 330 px wide in a 320 px window; MK: 325 px), see New bug A. All other views (360 px everywhere, 320 px on Home, Groups, Settings, Expenses, Balances, Totals) have scroll width equal to window width; bar inside the window and its text not cut in all 112 | VERIFIED by live run |

### NEW bugs
**New bug A (medium, regression from the "MK 320 px" fix): at 320 px the waiting rows are wider than their box, the amount sticks out of the dashed edge and the Budget screen scrolls sideways.**
- Steps: 320 px wide (EN or MK), queue a Budget spending of 640 MKD or an income of 45,000 MKD (or a One bill / an expense of 1,800 to 2,450 MKD), open Budget, Groups or the group Expenses tab. Hand-written items of the exact shape the app stores were used; the same happens with items made through the screens, because it depends only on the amount and the tile.
- Expected: the row stays inside its dashed edge and the page does not scroll sideways (as at HEAD `bbede2f`: 0 sideways scroll in 96 views).
- Actual: the row's content is wider than the row by 13 to 42 px on Budget (EN 18 and 42, MK 13 and 37), 20 px (EN) or 15 px (MK) on Groups and group Expenses; the amount is drawn over and outside the right dashed edge (screenshot `shots\l4-320-mk-dark-g-expenses.png`, "2.450 ден." starts inside and ends past the edge; `shots\l5-320-mk-budget.png`, "45.000 ден." leaves the row). On Budget the document is 325 px (MK) or 330 px (EN) wide in a 320 px window, so the page scrolls 5 to 10 px sideways (8 of 112 phone views: Budget, EN and MK, light and dark, top and bottom). At 360 px only one Budget row is 2 px too wide, with no page scroll. No overflow at 768 and above (desktop views 0).
- Numbers (320 px MK, Budget income row): row 260 px wide (inner box 232 px), contents 44 px tile + 120 px text column + 93 px amount + 2 gaps of 12 px = 281 px.
- Cause (read, not changed): `src/web/src/features/outbox/components/WaitingRow.tsx`, the text column has `min-w-30` (120 px) together with `flex-1`; with the tile (44 px) and the amount (`font-bold`, not shrinking) the three do not fit in the 232 to 244 px inside the row at 320 px.
- Proposed fix: let the amount wrap under the text when the line is too narrow (`flex-wrap`), or use a smaller minimum column (about 96 px) so that «Чека праќање» still wraps by word, and re-measure with the 6-item set at 320 px (doc scroll width must be 320).

**New bug B (low, known leftover, now measured): on the Budget and Payments lists the real row and the waiting row are both visible for about 0.2 s while a queue sends.** See check 9: 5 of 5 runs on each list, a duplicate row for the whole send (about 0.2 s for 3 items, 65 to 110 ms per item), not just one frame. The group Expenses list and the Groups list are clean in 5 of 5 runs.

### Observations (not bugs, for Filip to decide or know)
1. A hung item at the front of the queue still holds every item behind it (the queue keeps creation order); it is freed by the 30 s timeout cycle (each attempt 30 s, then 2 s, 5 s, 15 s waits) or by Discard. That is consistent with "one at a time, oldest first", but a person with one stuck item sees nothing else leave.
2. A server that has just started answers the first POSTs in 0.9 to 2.1 s (as in the earlier passes), so a tap that sends 2 items on a cold API finishes in about 3.3 s although sending starts within 31 ms.
3. Hand-written bad data: an MKD amount that is not a whole number of denars crashes the screen, and `display.title: null` shows "null" (details in check 6). Neither can be produced by the app.
4. After log out the person's IndexedDB key stays with an empty list instead of being removed.
5. The toast is centred on the window while the bar is centred on the content column on desktop.
6. The waiting bar is 54 px high (the tap button is 44 px inside 4 px padding), 6 px taller than before; its bottom edge is at the same y as before.

### Not verified
- A real phone (airplane mode, mobile network, touch, background and foreground), iOS Safari, Android Chrome, Firefox, a screen reader; the Cloudflare Pages Function and a real Render / Neon wake-up (only the Vite proxy, which answers 502 when the API is down, and a locally restarted API were used).
- The group's status bar in the Closing state, a Retry that succeeds, a 500 answer, 401 pausing the queue, two tabs of the same person sending the same items (tab 2 only queued an item in check 8), a guest who makes an account while items wait, the 60 s back-off step, the toast over the Save button (Observation 8 of the first pass), the rail at 1920x1080 with many groups (Observation 9).
- Whether the production build behaves like the dev server in checks other than 6 (checks 1 to 5 and 7 to 10 ran on the dev server).
- The Macedonian wording (drafts) was only checked for presence and fit, not read for meaning; contrast was not re-measured (not part of this fix).
- Any bad stored item shape besides the six listed in check 6 and the two hand-made shapes of its observation; a bad item in the failed-changes sheet.
- The automated suites (`dotnet build`, `dotnet test`, `npm test`, `npm run lint`, `npm run build`).

### Questions for Filip (product only)
1. On a very narrow phone (320 px) a waiting row with a big amount cannot show the title column, the tile and the amount side by side. Do you prefer the amount to drop under the title (taller row) or the title to get shorter (it may be cut after two lines)?
2. On Budget and Payments you still see the new row twice for about 0.2 s when the queue sends. Is that fine for the first version, or should it be the next small fix?

### Where the files are
Scripts, results and screenshots of this re-check: `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\11c66925-bd89-4c3f-9def-e4fb33a230e3\scratchpad\p17c\` (screenshots in `shots\`). Copies and adaptations of the earlier helpers (`lib.py` with `KVIT_BASE` to point at the preview, `lib2.py`, `warm.py`, `i_helpers.py`, `apictl.ps1`, `seed.py`, `a.py`, `r1.py`, `r1b.py`, `d.py`) and new scripts: `c1.py` (check 1), `c3.py` and `c3b.py` (check 3), `l4.py`, `l5.py`, `l6.py` and `mkitems.py` (checks 4, 5, 10 layout), `b6.py` (check 6), `c7.py` (check 7), `c8.py` (check 8), `f9.py` (check 9). Results as JSON next to them (`c1_res.json`, `c3_run1_res.json`, `c3_res.json`, `c3b_res.json`, `l4_desk_res.json`, `l4_phone_res.json`, `l6_res.json`, `b6_en_dev_res.json` (the first dev run is `b6_en_res.json`), `b6_en_prod_res.json`, `b6_mk_prod_res.json`, `c7_*_res.json`, `c8_res.json`, `f9_*_res.json`, `a_res.json`, `r1_run1_res.json`, `r1b_en_res.json`, `r1b_mk_res.json`). The production bundle is in `prod\`. Not in the repository.
Throwaway data left in the local database: the three `p17c...1793738352@example.com` users, groups "Outbox MKD" and "Outbox EUR" with many "C1 ...", "T4/T6/T7 ...", "H7 ...", "H8 ...", "F9 ...", "A exp ...", "R ..." expenses, about 25 One bill groups ("F9 bill ...", "A bill ...", "R offline bill"), a few payments, about 30 personal entries, and Bojan's "D ..." groups.
## Layout fix re-check (2026-11-03)

Re-measure of New bug A (waiting rows too wide at 320 px) against the uncommitted change in `src/web/src/features/outbox/components/WaitingRow.tsx` (inner span got `flex-wrap items-center gap-x-3 gap-y-1`, the amount span got `ml-auto`). Real headless Chromium (Playwright for Python), time zone `Europe/Skopje`, local API, Vite dev server and Docker database started with `.\scripts\start-local.ps1 -NoBrowser` and stopped with `-Stop` (ports 5018 and 5173 free, `kvit-postgres Exited (0)`). Only this report was changed in the repository; nothing was committed. The automated suites were not run. Labels: VERIFIED by live run or NOT VERIFIED.

### What was measured
- 72 views: 320, 360 and 390 px wide x EN and MK x light and dark x 6 screens (Budget, Groups, Expenses of the MKD group, Expenses of the EUR group, Payments (Balances) of the MKD group, Payments of the EUR group). 168 waiting rows in total. API writes of the queued items were blocked so the rows stayed waiting.
- Items: the same six as before (EUR dinner with a long title, MKD taxi 2.450, settlement 350, One bill 1.800, spending 640, income 45.000) plus eight stress items: EUR expense 1.234.567,89 with a long title, MKD expense 9.999.999 den., MKD settlement 12.345.678 den., EUR settlement 987.654,32, One bill 4.567.890 den., Budget spending 7.777.777 den., Budget income 88.888.888 den., and a MKD expense whose title is one 66-letter word ("Supercalifragilistic...").
- Per row: the row box and the amount box (bounding boxes), `scrollWidth` against `clientWidth`, row height, width of the text column (`min-w-30`), and a canvas check that no word is wider than its box (a word wider than its box would be broken in the middle). Per view: `document.documentElement.scrollWidth` at the top and at the bottom of the page.

### Result (VERIFIED by live run)
**Fixed.** New bug A is gone in every view measured.

| Check | Result |
|---|---|
| Page scrolls sideways | **0 of 72 views.** Document scroll width equals the window width (320, 360, 390) at the top and at the bottom, including Budget at 320 px (before: 325 px MK, 330 px EN). |
| Amount sticks out past the dashed edge | **0 of 168 rows.** The right edge of the amount is always 14 px inside the row's right edge (12 px padding + 2 px border), at every width. Row `scrollWidth` never exceeds `clientWidth` (largest difference 0). No row is outside the window. |
| Amount overlaps the text column | 0 of 168 rows. |
| Text column | Never below 120 px: smallest column in any view 122 px (the column is at least 120 px because of `min-w-30`; at 320 px it is 123 to 196 px). |
| Word broken in the middle | Only the stress title made of one 66-letter word (503 px of text in a 188 px column) is cut inside the word; it has no place to break by word, that is expected and the title is clamped to two lines with an ellipsis. No ordinary word (EN or MK, including «Чека праќање», «Платено од», «Цената на евро...») is wider than its box in any of the 168 rows. |

### What changed visually (row height and where the amount goes)
The amount now drops under the text, right-aligned inside the dashed edge, when tile + 120 px column + amount do not fit in the 232 px inside a row at 320 px. That is the whole fix, and it costs height. Heights of the six original items, before (HEAD `ef29a1a`, 320 px) against now:

| 320 px | Before | Now | Amount on its own line |
|---|---|---|---|
| Budget row | 65 px | **95 px** | all 12 rows (even "640 ден.", 44 + 12 + 120 + 12 + 69 = 257 px is more than the 232 px available) |
| Groups row | 65 px | **95 px** | all |
| Payments row | 91 px | **120 px** | all |
| Expenses row, MKD taxi | 110 px | **114 px** | yes |
| Expenses row, EUR dinner with long title (EUR group, not measured before) | n/a | **169 px** | no (beside the text, it fits because the column is 123 px) |

- Big stress rows at 320: Expenses EUR 179 px (long title, two-line detail, rate note, amount line), Expenses MKD 140 px, Groups 120 px, Payments 120 px, Budget 95 px.
- At 360 px only the longest amounts drop to their own line (Budget rows 65 or 95 px, Groups 65 px, Expenses 110 to 159 px, Payments 91 to 120 px). At 390 px Budget 65 or 95 px, Groups 65 to 91 px, Expenses 85 to 169 px, Payments 91 px.
- Screenshots looked at: `shots\s2-320-mk-light-budget.png` and `shots\s2-320-mk-dark-budget.png` (the four Budget rows, amount under «Чека праќање», right-aligned inside the edge, "88.888.888 ден." fits), `shots\s-320-mk-light-expenses.png` (taxi, big wedding gift and the 66-letter title, amounts right-aligned inside the edge) and `shots\s-320-mk-dark-expenses-eur.png`. The title and the detail line wrap by word, nothing is cut at the right.
- Not part of this fix, seen in the screenshots: the round Add button floats over the last visible waiting row ("2.450 ден." is half hidden under it in `s-320-mk-light-expenses.png`); it moves with the page and does not hide anything once scrolled.

### Verdict and a note for Filip
Fixed. If the taller rows at 320 px (Budget and Groups 65 to 95 px, Payments 91 to 120 px) matter, the other option from the earlier proposal is a smaller minimum column (about 96 px) so that the amount stays beside the text, but then «Чека праќање» may no longer wrap cleanly by word; that was not tried.

Files: scripts and results in `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\11c66925-bd89-4c3f-9def-e4fb33a230e3\scratchpad\p17d\` (`m.py`, `m_res.json`, `s.py`, `s2.py`, `shots\`). Not in the repository.
