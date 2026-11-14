# Status: Kvit

_Last updated: 2026-11-14 (Phase 19 built, reviewed and committed; Step 11 answered; waiting for Filip to push, check CI, add the secrets and merge)_

## Where we stopped
- Branch: `feat/19-legal` (made from `main` = `ddf41f2`, the Phase 18 merge). Pushed up to `f7e2478`; the PR exists. Filip pushes the rest and merges.
- Committed: Steps 0 to 10, the browser-pass fixes (`0023b3c`), the code-review fixes and final report (`f7e2478`), the activity feed in Download my data (`2cb5b94`), and the fix for the one failing GitHub test plus the Render deploy script build (`c3eabe7`). The last two are NOT pushed yet.
- CI on `f7e2478` failed on exactly one test, `CleanupGroupTests.Post_RemovedGroup_TakesTheDuplicateAnswers...` (Release build runs the tests of a class in a different order than Debug; an earlier test left an answer behind). Fixed in `c3eabe7` and reproduced and proven in a Release run. NOT VERIFIED on GitHub until the next push.
- Phase 19 map: report `docs/reports/2026-11-09-phase-19-legal.md` (final; 8 questions for Filip at its end); browser pass `docs/reports/2026-11-09-phase-19-browser-pass.md`; Filip's step-by-step guide `docs/guides/phase-19-legal.md`; PR text `docs/guides/phase-19-pr-text.md`; decisions in `DECISIONS.md` ("Phase 19"); research `docs/reports/2026-11-04-phase-19-legal-research.md`.

## Next step
Filip: push, check CI is green, add three secrets before merging (`Cleanup__Secret` on Render, `CLEANUP_SECRET` and `BACKUP_PASSPHRASE` on GitHub; the passphrase at least 30 random characters, the repository is public), CI green, merge, run the Cleanup and Backup workflows once by hand, and do the outside-the-code list in `docs/guides/phase-19-legal.md`. After the merge: update ROADMAP and STATUS for Phase 20 (or whatever Filip picks next).

## Blockers and open questions
- Open from Step 11: old "active people" numbers drop after 12 months (Filip said that is not fine; options: a monthly summary table or keeping the tie longer; not decided, not built). Waiting for Filip: Phase 18 leftovers (light theme peach, Totals bars); the Macedonian legal drafts to read (list in the report).
- Docker Desktop must be open for the backend tests (ask Filip, never work around it).
- Local database has throwaway `p19*@example.com` accounts from the browser pass; `p19admin1794216234@example.com` has the Admin role (revoke command in the browser-pass report). Harmless.

## Verification state
- VERIFIED by automated test (tester, 2026-11-09, after the last change): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 7432/7432; `dotnet ef migrations has-pending-model-changes` clean; frontend `npm run lint` 0 warnings, `npm run build` 0 type errors, `npm test` 15645/15645 (last full run after the browser-pass fixes, no frontend change after it); `npm run check:licenses` ok.
- VERIFIED by live run: the browser pass in headless Chromium (432 views plus 40 flow checks); backup, lock, unlock and restore on local throwaway databases (24 tables identical).
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, the real Google sign-in, the terms pill fix in a browser, the Macedonian drafts, both new GitHub workflows (and the passphrase-length step), the cleanup call through Cloudflare, the real Neon dump, the 3 migrations on Neon, the Cloudflare build serving the licence file.
- Known flaky tests under load (pass alone; in BACKLOG): `GroupScreenMarkPaid.test.tsx` "queues the payment…", `JoinScreen.test.tsx` "goes straight to the group…".
