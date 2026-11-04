# Phase 18: first-push texts (commit subject, PR title, PR description)

Branch: `feat/18-admin-stats`. This file is the one place for these texts. The checklist below is copied into the PR. After the first push, later pushes only get a one-line commit subject (listed at the bottom).

## Commit subject (first push)
Add the Phase 18 prompt, mockup and decisions for the admin statistics page.

## PR title
Phase 18: admin statistics page

## PR description (paste this)

### Overview
Last phase of Release 3 ("polish"). A Statistics page at `/admin`, for Filip only, that shows counts from the usage events recorded since Release 1: sign-ups (Email, Google, Guest), active people, joins through invite links, groups created, expenses added and payments confirmed, for a date range and grouped by day, week, month or year. Totals only, never names or amounts.

### Scope
- An `Admin` role, given only by a script run on Filip's PC (`scripts/GrantAdmin.cs`).
- `GET /api/admin/stats`, 403 for everyone who is not an admin.
- The page: range chips, grouping, all-time totals, one card per count with bars and a compare line against the period before.
- A "Statistics" row in Settings that only the admin sees; `/admin` is the not-found page for everyone else.
- Days are counted in Skopje time. The admin's own account is left out of every number.
- Phone first (360 px), then desktop. English and Macedonian, light and dark.

### Database changes
None planned. If the query needs an index on the usage events, it is added with a migration and noted here.

### Checklist
- [x] Step 0: mockup, picks and plan logged
- [x] Step 1: backend, the admin role
- [x] Step 2: backend, the statistics query
- [x] Step 3: frontend, the page and the Settings row
- [x] Step 4: desktop layout and the browser pass
- [x] Step 5: code review, report, guide

## Later one-line commit subjects
- Step 0: Add the Phase 18 prompt, mockup and decisions for the admin statistics page.
- Step 1: Add the Admin role, the admin policy, isAdmin on me and the GrantAdmin script.
- Step 2: Add GET /api/admin/stats with sign-ups, active people, joins, groups, expenses and payments counted in Skopje time.
- Step 3: Add the Statistics page, the admin-only route and the Settings row.
- Step 4: Fix the Phase 18 browser-pass findings: bars that overflowed the card, the Settings row behind the bottom bar, dimmed text contrast and big percentages.
- Step 5: Add the Phase 18 report, guide and backlog notes, and refuse statistics start dates before 2026.
