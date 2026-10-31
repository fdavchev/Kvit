# Phase 17: first-push texts (commit subject, PR title, PR description)

Branch: `feat/17-outbox`. This file is the one place for these texts. The checklist below is copied into the PR. After the first push, later pushes only get a one-line commit subject (listed at the bottom).

## Commit subject (first push)
Add the Phase 17 prompt, mockup and decisions, and the outbox module with its tests.

## PR title
Phase 17: the outbox (save while the server wakes up)

## PR description (paste this)

### Overview
First phase of Release 3 ("polish"). Render's free server sleeps and takes about a minute to wake. With the outbox, what you save during that time goes into a queue in the browser, shows in its list at once as "Not synced yet", and is sent in order when the server answers, ending with "All up to date ✓". An item the server refuses stays visible with the reason and Retry / Discard.

### Scope
- A queue in the browser (IndexedDB), per signed-in person, every item with its own id so a repeated send never saves twice.
- Queued: new expenses, One bill, new personal spending and income, payments that count right away.
- Not queued: edits, deletes, Undo, "I paid".
- Waiting items are greyed in the lists; balances and totals change only after the server saved them. A waiting item can be discarded.
- A bar at the bottom shows the count, "All up to date ✓" and failed items (Retry / Discard). Logging out with changes waiting asks first.
- Phone first (360 px), then desktop. English and Macedonian, light and dark.
- Not in this phase: admin statistics, notifications, a service worker or installable app.

### Database changes
None. The four save endpoints already return the first result for a repeated request id; Step 4 adds tests that pin this.

### Checklist
- [x] Step 0: mockup, picks and plan logged in DECISIONS, ARCHITECTURE fixed
- [x] Step 1: the outbox module (storage, queue, sender, back-off) with tests
- [ ] Step 2: wire the queueable saves
- [ ] Step 3: the screens (waiting rows, bar, failed items, log-out warning)
- [ ] Step 4: backend tests for late and repeated sends
- [ ] Step 5: desktop layout and the browser pass, including a real "server asleep" test
- [ ] Step 6: code review, report, guide

## Later one-line commit subjects
- Step 1 (tests, then the module): Add the outbox module with its storage, sender, back-off and tests.
