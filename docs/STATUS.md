# Status: Kvit

_Last updated: 2026-10-13 (Phase 9 finished and checked, waiting for Filip's commit, push and merge; Phase 10 is next)_

## Where we stopped
- Branch: `feat/09-settle-up`, last commit `9e281c2` (Step 4a, the Balances tab).
- Uncommitted: all of Phase 9 Step 4b (payment actions, the Payments list, the glass look, the plain-name rule) plus the three code-review fixes, the report, the guide and the doc updates. Everything is staged and nothing is committed.
- Last thing done: the code review of the whole phase, three fixes (Delete/Restore need both people to be current members, members list refresh, balances rows matched by id), and the report and guide.

## Next step
Filip: commit (one subject line), push, open the pull request, wait for CI, merge. Then Phase 10 (`feat/10-finish-groups`): read `ROADMAP.md` Phase 10 and `DECISIONS.md`.

## Then
- After the merge: follow `guides/phase-09-settle-up.md` on the real site (two people, one pending payment).
- Filip, once on the real site: one real EUR expense on Render (the first call to the real NBRM) and a real Google picture in a circle.
- Phase 10 closes groups; Phase 11 is the dashboard and Release 1.

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository.
- BACKLOG: the Phase 9 review leftovers (same-moment answers, partial-debt rows, retry after delete, settlements list limit), the owner Cancel for a plain-name payer, animations, API error handler, joining without an account (top "must do").

## Verification state
- VERIFIED by automated test (2026-10-13): backend build 0 warnings, `dotnet test` 2790/2790; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 5457/5457 (one JoinScreen test is occasionally flaky under load).
- VERIFIED by live run (2026-10-13, headless Chrome, 360 px, local stack): the Balances tab in light, dark and Macedonian; the sheets; the Payments list with Delete and Undo; the pending flow with two accounts (Mark paid, Cancel, Reject, Confirm).
- NOT VERIFIED: the review fixes in a browser, a real phone keyboard, Safari, screen readers, real Render and Neon, the `Settlements` migration on Neon (CI applies it at the merge), a line-by-line read of the Macedonian texts by Filip.
