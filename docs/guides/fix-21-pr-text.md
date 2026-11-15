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
- Steps 2 to 9: long words and narrow screens; tap sizes, contrast and dark mode; toasts, sheets and focus; texts in English and Macedonian with one privacy-notice change; behaviour bugs on the website and on the server; same-moment problems; tidy-up and speed.
- Step 10: full check, browser pass, code review, report.

### Database changes
None so far. Step 8 will add migrations.

### Checklist
- [ ] Step 0: backlog items that are already built are ticked
- [x] Step 1: the five bugs
- [x] Step 2: long words and narrow screens
- [x] Step 3: the lightest peach light theme, contrast and dark mode leftovers
- [ ] Step 4: toasts, sheets, focus
- [ ] Step 5: texts and one privacy-notice change
- [ ] Step 6: behaviour bugs, website
- [ ] Step 7: behaviour bugs, server
- [ ] Step 8: same-moment problems (migrations)
- [ ] Step 9: tidy-up and speed
- [ ] Step 10: full check, browser pass, code review, report

## Later pushes (one-line commit subjects)
- Keep long names, titles and large amounts inside narrow screens.
- Switch the light theme to the lightest peach and fix the contrast leftovers, the Split sheet pill and the Home totals with large amounts.
