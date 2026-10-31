# Status: Kvit

_Last updated: 2026-10-31 (Phase 16 built, browser-checked and code-reviewed; waiting for Filip: commit, push, PR, merge)_

## Where we stopped
- Release 1 accepted 2026-10-16. Phases 12 to 15 are merged. Branch `feat/16-group-insights`: Phase 16 (possible duplicates, group totals per category, group spending plan; last phase of Release 2) is finished, Steps 0 to 8. Two migrations, both applied locally and not yet on Neon: `DuplicateAnswers`, `GroupPlan`.
- Report: `docs/reports/2026-10-31-phase-16-group-insights.md`; browser pass: `docs/reports/2026-10-30-phase-16-browser-pass.md`; guide: `docs/guides/phase-16-group-insights.md`; first-push texts: `docs/guides/phase-16-pr-text.md`; mockups: `docs/design/2026-10-27-phase-16/` and `2026-10-30-phase-16-tabs/`.
- Uncommitted: the code review fixes, the live-check section of the browser-pass report, the report, the guide and the docs updates.

## Next step
1. Filip: commit and push (subject in `docs/guides/phase-16-pr-text.md`), tick the checklist in the PR, CI green, merge (CI applies the 2 migrations on Neon). Then follow the "after the merge" part of `docs/guides/phase-16-group-insights.md`.
2. Then Release 2 is done. Next is Release 3: Phase 17, the outbox (queue while the server wakes, "All up to date", failed items with Retry / Discard).

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real phone check of the Phase 16 screens, the Macedonian drafts of Phases 9 to 16, the Google button and "Make an account", the invite link on his phone.
- BACKLOG: future and planned payments, the Phase 16 leftovers, the Phase 15, 14, 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step. Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; whether the light theme gets a lighter peach; whether the Totals tab gets bars later.

## Verification state
- VERIFIED by automated test (2026-10-31, run by Claude after the last coder run): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 5942/5942; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 11675/11675.
- VERIFIED by live run (headless Chromium, local stack): all Phase 16 screens at 320/360/768/1280/1920 px in EN/MK, light/dark, reduced motion (4 bugs, fixed, re-checked twice); the review fixes at 360 px.
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, other time zones, Neon and Render (2 migrations), the deploy gap, the Cloudflare Pages path for the new calls, the Macedonian drafts.
