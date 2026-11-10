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
- A "Report this group" row in Group settings (opens an email with the group number); `SECURITY.md` and `security.txt`.
- Owner tools: suspend a person, close a group, a log of what was done and why.
- A daily cleanup job and a weekly encrypted database backup.
- Exchange-rate source line, a third-party licences page, and dated copies of every legal text in `docs/legal/`.

### Database changes
Each is a migration that CI applies on Neon when this PR is merged.
- `TermsAcceptance`: two empty columns on `users` (`terms_version`, `terms_accepted_at`) and one rule. Existing people stay empty and see the "terms changed" notice once.
- `AccountDeletion`: the delete-request state on `users` (when it was asked, when it was purged).
- `OwnerTools`: the owner action log table, the suspended mark on `users` and the closed-by-owner mark on groups.
- All three only add empty columns and one table, so the old server keeps working while the migrations run.

### Before you merge
Three secrets: `Cleanup__Secret` on Render, `CLEANUP_SECRET` and `BACKUP_PASSPHRASE` (at least 30 random characters) on GitHub. Without them the cleanup answers 404 and the backup job fails with a clear message. Steps: `docs/guides/phase-19-legal.md`.

### Checklist
- [x] Step 0: mockups, picks and plan logged
- [x] Step 1: the texts and `docs/legal/`
- [x] Step 2: backend, terms acceptance
- [x] Step 3: frontend, the agree line and the terms-changed notice
- [x] Step 4: Download my data
- [x] Step 5: backend, delete my account and the cleanup job
- [x] Step 6: frontend, delete my account
- [x] Step 7: Report, contact and safety files
- [x] Step 7b: owner tools
- [x] Step 8: backup, licences, vendors
- [x] Step 9: desktop layout and the browser pass
- [x] Step 10: code review, report, guide
- [ ] Step 11: Filip's answers to the questions in the report
- [ ] After merge: first run of the Cleanup and Backup workflows (Actions tab), `docs/guides/phase-19-legal.md`

## Later one-line commit subjects
- Add the Terms of Use, the full privacy notice, the rates source line and the legal documents.
- Save the accepted terms version per person and add the accept endpoint.
- Add the agree line, the terms-changed notice and the merged plain-name hint.
- Add Download my data: the export endpoint, the Settings row and the leave-time cut-off.
- Add Report this group, SECURITY.md and security.txt.
- Add Delete my account: the request, cancel by log-in, the purge service and the Settings flow.
- Generate the third-party licence notices at build, check them in CI and update the privacy wording.
- Add the protected cleanup endpoint and the daily cleanup workflow.
- Add the owner tools, the suspended screen and groups closed by the owner of Kvit.
- Add the weekly encrypted database backup, its connection script and the restore guide.
- Add the Phase 19 guide, the interim report and the Step 9 brief.
- Fix the Phase 19 browser-pass findings.
- Fix the Phase 19 code-review findings.
- Add the final Phase 19 report and update the roadmap, status and PR text.
