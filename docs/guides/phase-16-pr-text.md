# Phase 16: first-push texts (commit subject, PR title, PR description)

Branch: `feat/16-group-insights`. This file is the one place for these texts. The checklist below is copied into the PR. After the first push, later pushes only get a one-line commit subject (listed at the bottom).

## Commit subject (first push)
Add the Phase 16 prompt, mockup and decisions, and the possible-duplicates backend with the DuplicateAnswers migration.

## PR title
Phase 16: possible duplicates, group totals per category and the group spending plan

## PR description (paste this)

### Overview
Last phase of Release 2 ("budget"). Kvit asks once when a group share looks like something you already logged yourself, so the money is not counted twice. Every group member sees totals per category, and the owner can set a spending plan for the group with a progress bar.

### Scope
- Possible duplicates: one question per pair, answers remembered, "Same, merge" can be undone.
- Group totals per category in a new "Totals" tab, MKD and EUR apart, visible to every member.
- Group spending plan (the owner sets it), progress bar, warning near the limit, "over by" line.
- Two Phase 15 leftovers: a message for an invalid limit amount, and words for the "near the limit" state.
- Guests have no Budget, so no duplicate prompt for them.
- Phone first (360 px), then desktop. English and Macedonian, light and dark.
- Not in this phase: future and planned payments, recurring expenses, the outbox, admin statistics.

### Database changes
- Migration `DuplicateAnswers` (done): new table `duplicate_answers` (starts empty, so nothing changes for anyone until a question is answered).
- Migration `GroupPlan` (done): adds `groups.plan_amount_minor` and `groups.plan_currency` (empty for every existing group).

### Checklist
- [x] Step 0: mockup, picks logged in DECISIONS
- [x] Step 1: backend, possible duplicates
- [x] Step 2: backend, group totals per category
- [x] Step 3: backend, group spending plan
- [x] Step 4: frontend, possible duplicates
- [ ] Step 5: frontend, group totals
- [ ] Step 6: frontend, group plan and the two leftovers
- [ ] Step 7: desktop layout and the 360 px browser pass
- [ ] Step 8: code review, report, guide

## Later one-line commit subjects
- Step 2 tests (red on purpose, before the coder): Add the failing tests for the group totals per category endpoint.
- Step 2 (after the coder): Add the group totals per category endpoint with its tests.
- Steps 2 and 3 tests together (red on purpose): Add the group totals per category endpoint, and the failing tests for the group spending plan.
- Steps 2 and 3 after the coder: Add the group totals per category endpoint and the group spending plan, with the GroupPlan migration.
(more are added after each step)
