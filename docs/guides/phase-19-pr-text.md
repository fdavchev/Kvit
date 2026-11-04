# Phase 19: first-push texts (commit subject, PR title, PR description)

Branch: `feat/19-legal`. This file is the one place for these texts. The checklist below is copied into the PR. After the first push, later pushes only get a one-line commit subject (listed at the bottom).

## Commit subject (first push)
Add the Phase 19 prompt, mockups and decisions for the legal pages and data rights.

## PR title
Phase 19: legal pages and data rights

## PR description (paste this)

### Overview
Terms of Use, a complete privacy notice and the features a small free app is expected to have: agreeing to the terms at sign-up, downloading your data, deleting your account, reporting a group, and owner tools to suspend a person or close a group. Not legal advice and not checked by a lawyer; the research and the accepted risks are in `docs/reports/2026-11-04-phase-19-legal-research.md`.

### Scope
- `/terms` and a rewritten `/privacy`, English and Macedonian, with an "In short" box and the support email `kvit.support@gmail.com`.
- "By continuing you agree to the Terms and Privacy Policy" under the sign-up, Google and guest-join buttons; the accepted version is saved per person; a banner when the terms change.
- Download my data (JSON) and Delete my account (off at once, 30 days to change your mind, then removed; keep your name or show as "Former member").
- A "Report" item in the group menu; `SECURITY.md` and `security.txt`.
- Owner tools: suspend a person, close a group, a log of what was done and why.
- A daily cleanup job and a weekly encrypted database backup.
- Exchange-rate source line, a third-party licences page, and dated copies of every legal text in `docs/legal/`.

### Database changes
Terms acceptance columns, the account-deletion state, the owner action log and any monthly-totals table. Each is a migration that CI applies on Neon; the exact list is added here as the steps land.

### Checklist
- [ ] Step 0: mockups, picks and plan logged
- [ ] Step 1: the texts and `docs/legal/`
- [ ] Step 2: backend, terms acceptance
- [ ] Step 3: frontend, the agree line and banners
- [ ] Step 4: Download my data
- [ ] Step 5: backend, delete my account and the cleanup job
- [ ] Step 6: frontend, delete my account
- [ ] Step 7: Report, contact and safety files
- [ ] Step 7b: owner tools
- [ ] Step 8: backup, licences, vendors
- [ ] Step 9: desktop layout and the browser pass
- [ ] Step 10: code review, report, guide

## Later one-line commit subjects
(none yet)
