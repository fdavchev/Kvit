# Status: Kvit

_Last updated: 2026-11-03 (Phase 17 built, browser-checked, code-reviewed and the review findings fixed; waiting for Filip: push, PR, merge)_

## Where we stopped
- Release 1 accepted 2026-10-16. Phases 12 to 16 are merged (Release 2 done). Branch `feat/17-outbox`: Phase 17 (the outbox, first phase of Release 3) is finished, Steps 0 to 6 and the follow-up fixes. No migration.
- Report: `docs/reports/2026-11-03-phase-17-outbox.md`; browser pass and re-check: `docs/reports/2026-11-03-phase-17-browser-pass.md`; guide: `docs/guides/phase-17-outbox.md`; first-push texts: `docs/guides/phase-17-pr-text.md`; mockup: `docs/design/2026-10-31-phase-17/`.
- Everything is committed. The last round of fixes and the 320 px layout fix are re-checked by live run.

## Next step
1. Phase 17 is built, browser-checked, code-reviewed and the review findings are fixed (VERIFIED by automated test: frontend 12765 of 12765, backend 5975 of 5975; the first round of fixes re-checked by live run; the last round of fixes is re-checked next). Everything is committed on `feat/17-outbox`, nothing is pushed by Claude.
2. Filip: read the Macedonian drafts, push, open the PR with the texts in `docs/guides/phase-17-pr-text.md`, CI green, merge (no migration). Then follow `docs/guides/phase-17-outbox.md` on the real site and a phone.
3. After that: Phase 18, the admin statistics page, then Phase 19 (legal pages and data rights; planned 2026-11-04, research in `docs/reports/2026-11-04-phase-19-legal-research.md`, Filip's open questions at the top of `docs/DECISIONS.md`).

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real phone check of the Phase 16 screens, the Macedonian drafts of Phases 9 to 16, the Google button and "Make an account", the invite link on his phone.
- BACKLOG: future and planned payments, the Phase 16 leftovers, the Phase 15, 14, 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step. Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; whether the light theme gets a lighter peach; whether the Totals tab gets bars later.

## Verification state
- VERIFIED by automated test (2026-10-31, run by Claude after the last coder run): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 5942/5942; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 11675/11675.
- VERIFIED by live run (headless Chromium, local stack): all Phase 16 screens at 320/360/768/1280/1920 px in EN/MK, light/dark, reduced motion (4 bugs, fixed, re-checked twice); the review fixes at 360 px.
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, other time zones, Neon and Render (2 migrations), the deploy gap, the Cloudflare Pages path for the new calls, the Macedonian drafts.
