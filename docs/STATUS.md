# Status: Kvit

_Last updated: 2026-11-09 (Phase 19 built and reviewed; waiting for Filip's commit, answers to the questions and the merge steps)_

## Where we stopped
- Branch: `feat/19-legal` (made from `main` = `ddf41f2`, the Phase 18 merge). Nothing is pushed; Filip pushes, opens the PR and merges.
- Committed: Steps 0 to 8 (one commit each), `97db15b` (guide, interim report, Step 9 brief), `0023b3c` (browser-pass fixes).
- **Uncommitted:** the code-review fixes (backend: `AccountPurger`, `AccountModeration`, `AdminAction`, `IAccountPurger`, `MeController`, tests, `backup.yml`) and docs (final report, ROADMAP, STATUS, BACKLOG, DECISIONS, PR text, the guide's passphrase line). Suggested commit subjects: "Fix the Phase 19 code-review findings." and then "Add the final Phase 19 report and update the roadmap, status and PR text."
- Phase 19 map: report `docs/reports/2026-11-09-phase-19-legal.md` (final; 8 questions for Filip at its end); browser pass `docs/reports/2026-11-09-phase-19-browser-pass.md`; Filip's step-by-step guide `docs/guides/phase-19-legal.md`; PR text `docs/guides/phase-19-pr-text.md`; decisions in `DECISIONS.md` ("Phase 19"); research `docs/reports/2026-11-04-phase-19-legal-research.md`.

## Next step
Filip: commit the review fixes and docs, answer the 8 questions at the end of the Phase 19 report (Step 11), then push, open the PR with `docs/guides/phase-19-pr-text.md`, add three secrets before merging (`Cleanup__Secret` on Render, `CLEANUP_SECRET` and `BACKUP_PASSPHRASE` on GitHub; the passphrase at least 30 random characters, the repository is public), CI green, merge, run the Cleanup and Backup workflows once by hand, and do the outside-the-code list in `docs/guides/phase-19-legal.md`. After the merge: update ROADMAP and STATUS for Phase 20 (or whatever Filip picks next).

## Blockers and open questions
- Waiting for Filip: the 8 questions in the Phase 19 report (including `START-HERE-PROMPT.md` in the public repository); Phase 18 leftovers (light theme peach, Totals bars); the Macedonian legal drafts to read (list in the report).
- Docker Desktop must be open for the backend tests (ask Filip, never work around it).
- Local database has throwaway `p19*@example.com` accounts from the browser pass; `p19admin1794216234@example.com` has the Admin role (revoke command in the browser-pass report). Harmless.

## Verification state
- VERIFIED by automated test (tester, 2026-11-09, after the last change): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 7417/7417; `dotnet ef migrations has-pending-model-changes` clean; frontend `npm run lint` 0 warnings, `npm run build` 0 type errors, `npm test` 15645/15645 (last full run after the browser-pass fixes, no frontend change after it); `npm run check:licenses` ok.
- VERIFIED by live run: the browser pass in headless Chromium (432 views plus 40 flow checks); backup, lock, unlock and restore on local throwaway databases (24 tables identical).
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, the real Google sign-in, the terms pill fix in a browser, the Macedonian drafts, both new GitHub workflows (and the passphrase-length step), the cleanup call through Cloudflare, the real Neon dump, the 3 migrations on Neon, the Cloudflare build serving the licence file.
- Known flaky tests under load (pass alone; in BACKLOG): `GroupScreenMarkPaid.test.tsx` "queues the payment…", `JoinScreen.test.tsx` "goes straight to the group…".
