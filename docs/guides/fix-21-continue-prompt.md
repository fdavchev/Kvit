# Prompt to continue Fix 21 from any step

Paste everything below the line into a new session. It works at any point of the branch: the session finds the current step itself.

---

We are continuing Kvit on the branch `fix/21-small-bugs` ("Fix 21: empty the backlog"). It is the last planned branch: everything open in `docs/BACKLOG.md` is either fixed here, listed as a Release 4 feature in `docs/ROADMAP.md`, or moved to "Skipped on purpose". Nothing is saved for a later branch.

## Read first
1. `docs/STATUS.md` and the "Fix 21" section of `docs/ROADMAP.md`: the first unticked step is where we are.
2. `docs/DECISIONS.md`, the section "2026-11-14: Fix 21 (empty the backlog)": what was decided in the finished steps and what was rejected.
3. `docs/BACKLOG.md`: the open items, and the "Fix 21 Step N leftovers" entries written after each finished step.
4. `docs/guides/fix-21-pr-text.md`: the PR checklist and the one-line commit subjects so far.

## Find out where we are (do not trust this prompt or STATUS for it; look)
- `git status`, `git log --oneline -8`, `git status -sb`: is the working folder clean, is the last step committed and pushed?
- If the working folder has uncommitted changes, read `git diff --stat` and ask me what they are before touching anything.
- `gh pr list --head fix/21-small-bugs --state all`: is the pull request open? It must NOT be merged before every step is done and my live check of Fix 20 is over.
- Ask me whether I have done the Fix 20 live check on the real site (the list is in `docs/STATUS.md`). If something failed there (a "verify you are human" box, no return to Kvit, the old "Something went wrong" screen), that comes before the next step, and its fix goes on this same branch.
- Until I say the live check is over: nothing is merged into `main`, and nobody calls `kvit-mk.pages.dev` or the Render address. Local work and pushing the branch are fine.

## The steps
State when this prompt was last updated (2026-11-20): Steps 0 to 5 and 8 are committed (Step 5 is `db810d8`); the wake fixes found on Filip's phone (web app manifest, calm wait after the trip, DECISIONS "Wake fixes") are done and may not be committed yet. Next: Steps 6, 7, 9, 10. The Fable allowance is used up: every coder run goes on Opus 5.5.

**Order (Filip, 2026-11-15): after Step 4 comes Step 8, before Steps 5, 6, 7 and 9.** Step 8 (same-moment problems, with migrations) is the one step where the strongest model is worth it for the coder, and Filip has a Fable allowance that resets on 2026-11-16, so its coder run goes on Fable (`model: fable`), not Opus. If allowance is left afterwards, the next best uses are Step 7, then Step 6. The steps do not depend on each other. Not known: whether that allowance also covers the tester, which runs on Sonnet and is what hit the 5-hour limit twice; Step 8's tester run is heavy (the full backend suite, Docker, new same-moment tests). A half-done database change must never be committed. If today is after 2026-11-16, ask me whether Fable is still available before choosing the model for Step 8.

- **Step 0, tick already-built BACKLOG items:** six are ticked. Still to mark inside longer lines: the limit amount message and "near" in words (Phase 15 leftovers c and d), the Add expense closing warning (`closing.addCancels` exists), and the duplicate "JavaScript file size" lines. Docs only; do it yourself.
- **Step 4, toasts, sheets, focus.** Checked in the code on 2026-11-15, before any change:
  - The toaster is `src/web/src/shared/components/KvitToaster.tsx` (Sonner, bottom centre); sheets are `shared/components/KvitSheet.tsx` (backdrop and viewport `z-50`). Open: a sheet opening while an Undo toast is up has its Close button covered; a new toast on Members stacks over "Removed: x · Undo" and covers "Let back in"; a toast covers the floating "+" for 4 seconds; at 1280 px the toast is centred on the window and the closing bar on the content column; Sonner slides in at 0.4 s (wanted about 0.2 s) and adds its own "alt+T" hint.
  - `KvitSheet` returns focus through `opener` / `returnsFocusOnClose` by hand for chained sheets (Phase 14 leftover b); `useFocusWhenOpenerIsGone` (Phase 16 leftover b).
  - `KvitTextField` links only `hint` through `aria-describedby`; its `invalid` prop is already used by Make account and the category sheet. Open: the 61-character name error not linked to its field, and server errors beside the field on Sign up (`AUTH_EMAIL_TAKEN`, `AUTH_PASSWORD_TOO_WEAK`).
  - "Updating…" is `shared/components/KvitUpdatingNote.tsx`, shown only from `core/router/BottomBarLayout.tsx`. Ask me what it should watch before putting it on group pages.
  - The wake bar is `core/router/ServerAsleepBar.tsx`; its button leaves the page at once. Open: a warning when a form has unsaved typing.
  - Already gone: the Welcome "Coming soon" toast (no such text in the code any more).
  - Ask me: is the closing bar as a popup (my idea of 2026-10-14) still wanted? It is a floating bar today and stays until answered.
  - Not yet checked: the "+ Add a name" slide glitch on Members.
- **Step 5, texts in EN and MK, and ONE privacy-notice change.** All notice wording changes go together so people see the "Terms changed" pill once: the "after 30 days" wording (the cleanup runs on the first signed-in visit after that day) and one sentence about recently deleted groups in the export. Publishing steps: `docs/legal/CHANGELOG.md`. The notice already names GitHub; nothing to do there. I read every Macedonian text before it counts as final.
- **Step 6, behaviour bugs on the website.** The biggest list (BACKLOG: Phase 7, 9, 10, 11, 12b, 17, 18, 19 leftovers). Likely needs two coder runs.
- **Step 7, behaviour bugs on the server, no migration.** Starts with the global exception handler and `SERVER_ERROR`.
- **Step 8, same-moment problems, with migrations.** Remind me to run `dotnet ef database update` locally. It comes right after Step 4 and its coder run goes on Fable (see "Order" above). Before the coder: read the code, tell me what would change, and ask me what a person should see when their save is refused because someone else changed the same expense or payment a moment earlier.
- **Step 9, tidy-up and speed.** Change only what is clearly cheaper or clearly duplicated; measure locally.
- **Step 10, full pass.** `.\scripts\start-local.ps1 -Check`, a browser pass from 320 to 1920 px in EN and MK, light and dark (the light theme is new since Step 3 and was contrast-measured only at 360 px in English), `/code-review high`, `README.md` brought up to date as a whole (what Kvit does now, the features since it was last written, commands, and new screenshots, which still show the old peach; Filip, 2026-11-19), the final report in `docs/reports/`.

The lists in the plan came from the backlog, and the backlog is partly out of date: in Steps 2 and 3 about a third of the listed items were already fixed. So for every step: read the code for each item first, drop what is already fixed, and only then tell me the list.

## How we work on this branch
- One step at a time. For each step: read the code, tell me in plain words what would change (and what you would skip, and why), give your own suggestions, and wait for my "go" before any coder run. Ask me before every coder run, also for a follow-up run.
- Coder runs go on Opus 5.5 (`model: opus`), except Step 8 (Fable, see "Order"), and do NOT start the local stack or measure in a browser: the coder edits and runs lint, build and tests. The tester then writes the tests, does the full run and measures in a headless browser once. This is to save my usage; I am on the Pro plan and the 5-hour limit ran out twice on this branch. If a subagent stops on the limit, resume the same agent when I say "continue" instead of starting a new one.
- A fix of one or two styling classes after a tester report: you may do it yourself with my OK, then the tester re-measures only that.
- Give every subagent the full path of your memory folder and these rules: no git commit, push or branch commands; no calls to the live site or `onrender.com`; no docs edits; no code comments; shell commands from a path that starts with a capital `C:` (a lowercase drive letter makes Vitest fail with "failed to find the current suite").
- The tester must not change production code. Ask it to confirm that the new tests fail against the old code (it used a scratch copy of `src/web` for that).
- Docker Desktop must be open for backend tests and for the local stack. Ask me to open it; never work around it.
- Known flaky tests under load, pass alone: several tests in `GroupScreenMarkPaid.test.tsx`, and `JoinScreen.test.tsx` "goes straight to the group…". If only one of those fails, re-run that file alone and report both results.
- The local database holds throwaway accounts and groups from the browser checks (`f21b…@example.com`, groups named "F21 …"). Harmless; the tester may reuse or replace them.
- To show me a look, open the screenshots folder in File Explorer; opening several pictures at once showed me only one.
- After each step: tick the step in `docs/ROADMAP.md`, update `docs/STATUS.md` (with VERIFIED by automated test / VERIFIED by live run / NOT VERIFIED), log decisions in `docs/DECISIONS.md`, tick or annotate `docs/BACKLOG.md` and add a "Fix 21 Step N leftovers" entry for what stayed open, tick the step and add the one-line commit subject in `docs/guides/fix-21-pr-text.md`. No separate report file per step; the results go in the chat.
- Commits: one subject line, no body, no Claude trailer. I commit myself unless I say "you commit". After each commit I push and tick the step in the PR description.

## Decisions already made (do not ask again)
- Editing an old expense: a person who left stays allowed in the split when their typed value and the split type are unchanged, even if their share changes with the amount; the same for an unchanged payer who left.
- The light theme is the lightest peach (page `#fff4e6`, white cards, pills `#ffe9cc`), with every line kept at `#9c6f47` and no edge on cards. The dark theme must not change.
- Left as they are: a hyphenated name breaking at its hyphen; big titles on two lines; the amount on its own line in a waiting row at 320 px; the Home "+" floating over cards while scrolling.
- New features are not part of this branch (Release 4 in `docs/ROADMAP.md`).

## Still mine, not yours
The Fix 20 live check; deleting `Cleanup__Secret` on Render and `CLEANUP_SECRET` on GitHub; running the Backup workflow once with the practice restore; turning on GitHub private vulnerability reporting; Part E of `docs/guides/phase-19-legal.md`. Remind me of whichever are still open when we wrap up.
