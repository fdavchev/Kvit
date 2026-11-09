# Status: Kvit

_Last updated: 2026-11-09 (handoff: Phase 19 Steps 0 to 8 built and committed; Step 9 browser pass, Step 10 review and Step 11 questions still open)_

## Where we stopped
- Branch: `feat/19-legal` (made from `main` = `ddf41f2`, the Phase 18 merge), last commit `b3301e6 Add the weekly encrypted database backup, its connection script and the restore guide.` Nothing is pushed; Filip pushes, opens the PR and merges.
- Uncommitted changes: only docs: `docs/guides/phase-19-legal.md` (new), `docs/guides/phase-19-step-9-browser-pass-prompt.md` (new), `docs/guides/restore-a-backup.md` (changed), `docs/reports/2026-11-09-phase-19-legal.md` (new), ROADMAP and STATUS edits. Suggested commit subject: "Add the Phase 19 guide, the interim report and the Step 9 brief."
- Last thing done: Step 8, the weekly encrypted backup (restore proven on throwaway local databases). The browser pass (Step 9) was started and stopped before any result; the local stack is stopped (`-Stop` was run).
- Phase 19 map: report `docs/reports/2026-11-09-phase-19-legal.md` (what was built, results, accepted risks, 8 questions for Filip); Filip's step-by-step guide `docs/guides/phase-19-legal.md`; PR text `docs/guides/phase-19-pr-text.md`; decisions in `DECISIONS.md` ("Phase 19", entries marked "decided by Claude, please confirm"); research `docs/reports/2026-11-04-phase-19-legal-research.md`.

## Next step
Run Step 9 in a new session: give the `tester` agent the brief in `docs/guides/phase-19-step-9-browser-pass-prompt.md` (add the memory folder path `C:\Users\Davchev\.claude\projects\c--Users-Davchev-Projects-Kvit\memory\`; Docker Desktop must be open; apply the 3 new migrations locally first: `dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api`). Then fix the findings with the coder. Filip's standing go for all Phase 19 steps (2026-11-04): tester first, then coder, one commit per step, never push.

## Then
- Step 10: `/code-review high` on the branch and fix findings; extend the interim report into the final one; update ROADMAP, STATUS, BACKLOG and the PR text; commit.
- Step 11: ask Filip the 8 questions at the end of the report (including `START-HERE-PROMPT.md`).
- Filip: the secrets, first workflow runs and the outside-the-code list in `docs/guides/phase-19-legal.md` (three secrets before the merge: `Cleanup__Secret` on Render, `CLEANUP_SECRET` and `BACKUP_PASSPHRASE` on GitHub).

## Blockers and open questions
- Docker Desktop must be open for the backend tests (it was off once; ask Filip, never work around it).
- Waiting for Filip: the 8 questions in the report; Phase 18 leftovers (light theme peach, Totals bars).
- The usage limit interrupted agents twice; if an agent shows "completed" without a report, check `git status` and resume it with SendMessage.

## Verification state
- VERIFIED by automated test (2026-11-09): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 7397/7397; `dotnet ef migrations has-pending-model-changes` clean; frontend `npm run lint` 0 warnings, `npm run build` 0 type errors, `npm test` 15626/15626 (last run before the backup step, which changed no frontend file); `npm run check:licenses` ok.
- VERIFIED by live run: backup, lock, unlock and restore on local throwaway databases (24 tables identical).
- NOT VERIFIED: every new screen in a real browser, a real phone, the Macedonian drafts, both new GitHub workflows, the cleanup call through Cloudflare, the real Neon dump, the 3 migrations on Neon.
- Known flaky test under load: `GroupScreenMarkPaid.test.tsx` "queues the payment…" (passes alone; in BACKLOG).
