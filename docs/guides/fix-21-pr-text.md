# Fix 21: first-push texts (commit subject, PR title, PR description)

Branch: `fix/21-small-bugs`. This file is the one place for these texts. After the first push, later pushes only get a one-line commit subject (listed at the bottom). Do not merge before the Fix 20 live check is over.

## Commit subject (first push)
Fix editing an old expense with a person who left, the Add expense crash, Back after joining, long names and the cleanup retry.

## PR title
Fix 21: empty the backlog

## PR description (paste this)

### Overview
The last branch before new features: every open bug, glitch, wrong text, rare same-moment problem and tidy-up from `docs/BACKLOG.md`, done in steps. New features are listed as Release 4 in `docs/ROADMAP.md` and are not part of this PR.

### Scope
- Step 1: five bugs. An old expense whose split or payer is a person who left can be edited again; the Add expense screen no longer crashes when a member is removed elsewhere; a signed-in person is sent away from Welcome, Sign up and Log in, and Back no longer returns to them; long names without spaces no longer stretch the Members page and the Home greeting; a failed daily cleanup is tried again by the same person's next visit.
- Step 2: long names, titles and very large amounts stay inside narrow screens (320 to 390 px): the amount drops under the text instead of breaking names letter by letter, and the category grids use three columns on the narrowest phones.
- Step 3: the light theme is the lightest peach (page `#fff4e6`, white cards); the dark theme is unchanged. Contrast leftovers on hover, the Split sheet's selected tab in dark mode, a quieter "Log out", and the Welcome theme icon follows the device.
- Step 4: toasts sit above an open sheet instead of covering its buttons, two or three toasts show in full, and on desktop they are centred on the content column; a refused field is marked and linked to its error for a screen reader (Sign up too); "Wake the server" asks first when a form has unsaved typing; the "Updating…" note shows on every signed-in screen; "+ Add a name" no longer slides through a fading row; focus after the plan and limit sheets.
- Step 8: same-moment changes: the second of two edits of one expense is refused with a clear message instead of silently overwriting the first; two answers to one payment, two group plan changes, two "Same" answers, a free name removed while someone taps it, two log-ins, two password changes and two guest upgrades all end in one clean result instead of a mix or a server error. The Balances tab refreshes after a refused payment answer.
- Steps 5, 6, 7 and 9: texts in English and Macedonian with one privacy-notice change; behaviour bugs on the website and on the server; same-moment problems; tidy-up and speed.
- Step 10: full check, browser pass, code review, report.

### Database changes
One migration, `SameMomentSafety` (Step 8): two partial unique indexes on `duplicate_answers`, after removing "Same" answers that already broke the one-merge rule (a live merge is kept first, then the oldest). Row versions use Postgres's own `xmin`, so no table gets a new column.

### Checklist
- [ ] Step 0: backlog items that are already built are ticked
- [x] Step 1: the five bugs
- [x] Step 2: long words and narrow screens
- [x] Step 3: the lightest peach light theme, contrast and dark mode leftovers
- [x] Step 4: toasts, sheets, focus
- [ ] Step 5: texts and one privacy-notice change
- [ ] Step 6: behaviour bugs, website
- [ ] Step 7: behaviour bugs, server
- [x] Step 8: same-moment problems (migrations)
- [ ] Step 9: tidy-up and speed
- [ ] Step 10: full check, browser pass, code review, report

## Later pushes (one-line commit subjects)
- Keep long names, titles and large amounts inside narrow screens.
- Switch the light theme to the lightest peach and fix the contrast leftovers, the Split sheet pill and the Home totals with large amounts.
- Keep toasts clear of sheets and of each other, link refused fields to their error, ask before waking the server with unsaved typing and show Updating on every signed-in screen.
- Refuse or re-answer the second of two same-moment changes to expenses, payments, plans, merges, members and accounts.
