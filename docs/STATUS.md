# Status: Kvit

_Last updated: 2026-10-31 (Phase 16 built, browser-checked and code-reviewed; waiting for Filip: commit, push, PR, merge)_

## Where we stopped
- Release 1 accepted 2026-10-16. Phases 12 to 16 are merged (Release 2 done). Branch `feat/17-outbox`: Phase 17 (the outbox) is at Step 0. The mockup `docs/design/2026-10-31-phase-17/` is done and Filip picked 1C 2A 3A 4A 5A (logged in `docs/DECISIONS.md`). No code yet.
- Prompt: `docs/guides/phase-17-outbox-prompt.md`; first-push texts: `docs/guides/phase-17-pr-text.md`.
- Uncommitted: the prompt, the mockup, the Phase 17 sections in DECISIONS, ROADMAP and STATUS, and the PR text file.

## Next step
1. Phase 17 is built, browser-checked and code-reviewed (VERIFIED by automated test: frontend 12613 of 12613, backend 5975 of 5975; the fixes re-checked by live run). Everything is committed on `feat/17-outbox`, nothing is pushed.
2. Filip: read the code review list (10 findings, none fixed yet) in `docs/reports/2026-11-03-phase-17-outbox.md` and the questions at the end of `docs/reports/2026-11-03-phase-17-browser-pass.md`, and say which to fix. Then push, open the PR with the texts in `docs/guides/phase-17-pr-text.md`, CI green, merge (no migration). Then follow `docs/guides/phase-17-outbox.md` on the real site and a phone.
3. After that: Phase 18, the admin statistics page (last phase of Release 3).

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real phone check of the Phase 16 screens, the Macedonian drafts of Phases 9 to 16, the Google button and "Make an account", the invite link on his phone.
- BACKLOG: future and planned payments, the Phase 16 leftovers, the Phase 15, 14, 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step. Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; whether the light theme gets a lighter peach; whether the Totals tab gets bars later.

## Verification state
- VERIFIED by automated test (2026-10-31, run by Claude after the last coder run): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 5942/5942; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 11675/11675.
- VERIFIED by live run (headless Chromium, local stack): all Phase 16 screens at 320/360/768/1280/1920 px in EN/MK, light/dark, reduced motion (4 bugs, fixed, re-checked twice); the review fixes at 360 px.
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, other time zones, Neon and Render (2 migrations), the deploy gap, the Cloudflare Pages path for the new calls, the Macedonian drafts.
