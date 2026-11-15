# Status: Kvit

_Last updated: 2026-11-14 (Fix 20 merged as PR #24; `fix/21-small-bugs` started: Step 1, the five bugs, is built and tested, not committed; Filip's live check of Fix 20 is still open)_

## Where we stopped
- Branch: `fix/21-small-bugs` (made from `main` = `25c4a3d`, the Fix 20 merge). Nothing committed yet: Step 1 (code and tests) and the docs edits are in the working folder.
- The plan: `ROADMAP.md`, "Fix 21: empty the backlog" (Steps 0 to 10, then Release 4 for new features). Decisions: `DECISIONS.md`, "2026-11-14: Fix 21". Commit and PR texts: `docs/guides/fix-21-pr-text.md`. The prompt that started it: `docs/guides/fix-21-small-bugs-prompt.md`.
- Step 0 is partly done: six already-built BACKLOG items are ticked; four small notes inside longer lines are still to mark (listed in `ROADMAP.md`).

## Next step
Filip:
1. The live check of Fix 20 (only the real site can prove it). Until it is over, nothing is merged into `main` and nobody calls the live site or the Render address:
   - Leave Kvit alone for 20 minutes, then open `https://kvit-mk.pages.dev` on the phone and on the PC. Expected: Render's black "waking up" page for about 30 seconds, then back on the same Kvit screen.
   - Leave a Kvit tab open for 20 minutes, then tap something. Expected: a bar "The server is asleep. · Wake the server" at the top; the button does the same trip.
   - Add an expense while the bar shows. Expected: it shows as waiting and is sent after the trip.
   - In the Render log after the first signed-in visit: a line "The daily cleanup of … finished".
2. Commit Step 1 and push the branch with the texts in `docs/guides/fix-21-pr-text.md` (pushing is fine; merging waits).
3. Delete `Cleanup__Secret` on Render and `CLEANUP_SECRET` on GitHub (no longer used).
4. Still open from Phase 19 (`docs/guides/phase-19-legal.md`): run the **Backup** workflow once, the practice restore, **Private vulnerability reporting** on GitHub (checked 2026-11-14: off), and Part E.

Claude: finish Step 0, then Step 2 (long words and narrow screens): read the code, tell Filip the changes in plain words, ask about "Accommodation" and the Macedonian category words, wait for his go before the coder.

## Blockers and open questions
- Not known until the live check: whether every person passes Render's robot check without a tap, how an installed home-screen Kvit and iOS Safari behave on the trip, and whether Render changes this again.
- For Filip to confirm (built as described in `DECISIONS.md`): a person who joins while a form is open is added only on Add expense, not on Edit; a signed-in person is sent away from Welcome, Sign up and Log in using only the saved copy of "me"; a signed-in guest is sent away too.
- The privacy page and the delete sheet say "after 30 days"; the cleanup runs on the first signed-in visit after that day. Rewording is planned in Step 5, together with every other notice change, so the "Terms changed" pill shows once.
- Open from Phase 19 Step 11: old "active people" numbers drop after 12 months (not decided, not built; first cut about 2027-10).
- Waiting for Filip: the light theme peach (asked in Step 3), the Macedonian drafts, including «Серверот спие.» and «Разбуди го серверот».
- Docker Desktop must be open for the backend tests (ask Filip, never work around it).
- Local database has throwaway accounts from the Phase 19 and Fix 21 browser checks. Harmless.

## Verification state
On `fix/21-small-bugs` (working folder, 2026-11-14, after Step 1):
- VERIFIED by automated test (tester, after the last code change): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 7473/7473 (23 new); `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 15807/15807 (87 more than before Step 1).
- VERIFIED by live run (headless Chromium, 360 px, English, local stack): a 60-letter name on the Members page and in Home's greeting gives no sideways scroll; Back after sign-up, after the in-app Back and after joining through an invite never shows Sign up or Log in while signed in; a member removed in a second session while Add expense is open: no crash, the typed amount stays, Paid by falls back to the signed-in person, the save goes through without the removed person.
- NOT VERIFIED: that each new test fails without its fix (not tried); Macedonian, dark mode and widths other than 360 px for the Step 1 screens; a real Google sign-in through the new redirect; a real phone; anything on the live site.

On `main` = `25c4a3d` (2026-11-14):
- VERIFIED by live run: CI run 43614330770 green (frontend and backend jobs), which includes the Render deploy.
- VERIFIED by automated test (on the Fix 20 branch before the merge): backend 7450/7450, frontend 15720/15720, build, lint, licences, the API image build.
- NOT VERIFIED: Fix 20 on the real site (the list under "Next step" 1), the Backup workflow (never run), the real Neon dump and restore, the Phase 19 screens signed in on the live site, a real phone, iOS Safari, Firefox, a screen reader.
- Known flaky tests under load (pass alone; in BACKLOG): `GroupScreenMarkPaid.test.tsx` "queues the payment…", `JoinScreen.test.tsx` "goes straight to the group…".
