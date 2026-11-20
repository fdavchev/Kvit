# Prompt for the next session (after Fix 21)

Paste everything below the line into a new session.

---

We are continuing Kvit. Fix 21 ("empty the backlog", branch `fix/21-small-bugs`) is finished: Steps 0 to 10 are committed and pushed, PR "Fix 21: empty the backlog" is open and NOT merged.

## Read first
1. `docs/STATUS.md` and `docs/ROADMAP.md` (the Fix 21 section and Release 4).
2. `docs/reports/2026-11-21-fix-21-small-bugs.md`: what Fix 21 changed, how it was checked, the "For Filip" list, and the full decision log at the end.
3. `docs/BACKLOG.md`: the "Fix 21 Step N leftovers" entries at the top.

## Check where we are (look, don't trust this prompt)
- `git status -sb`, `git log --oneline -5`, `gh pr list --head fix/21-small-bugs --state all`: clean? pushed? PR open or merged? Did CI pass on the last push (`gh run list --branch fix/21-small-bugs --limit 3`)? The CI frontend job now runs `npm run smoke` (Chromium); this is its first run, so if it fails, find out why before anything else.
- Nothing is merged into `main` and nobody calls `kvit-mk.pages.dev` or the Render address until I say my Fix 20 live check is over. I merge PRs myself.

## Ask me, one at a time, in plain words
1. Has CI passed, and have I done the Fix 20 live check (list in `STATUS.md`)? If something failed there, its fix comes first, on this branch.
2. Do I confirm Claude's choices from Fix 21 (each one explained in the report's "For Filip" list): an expired login certificate stops the server from starting; guest logins left with nothing are deleted 30 days after they were made; the desktop menu lists Closing groups; the Google button is blank for about 0.3 s when the language changes; the Edit date limit counts from today in UTC. If I say no to one, it becomes a small fix on a new branch.
3. Have I read the Macedonian texts in `docs/guides/fix-21-macedonian-to-read.md` (rows 1 to 83)? Fix the rows I send.
4. After the merge: did I re-add the Kvit icon to my phone's home screen and open it signed in after 20 minutes away?

## Remind me of what is still mine
Deleting `Cleanup__Secret` on Render and `CLEANUP_SECRET` on GitHub; running the Backup workflow once with the practice restore; turning on GitHub private vulnerability reporting; Part E of `docs/guides/phase-19-legal.md`.

## Then
Release 4 (new features, `ROADMAP.md`): ask me which phase to start. Each becomes its own branch and plan.

## How we work (my rules are in CLAUDE.md and the memory folder; the key ones)
- Coder runs on Opus, the tester on Sonnet. Ask me before every coder run, with a summary of the changes and your suggestions.
- Before sending a tester or review finding to the coder as a bug, check `docs/DECISIONS.md` for a choice I already confirmed; if there is one, ask me first.
- Subagents get the memory folder path and the rules: no git commit/push/branch commands, no live site, no docs edits, no code comments, shell paths starting with a capital `C:`.
- Docker Desktop must be open for backend tests and the local stack; ask me to open it.
- One step at a time; after each step update ROADMAP, STATUS, DECISIONS, BACKLOG and the PR text file. I commit myself unless I say "you commit".
