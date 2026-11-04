# Status: Kvit

_Last updated: 2026-11-04 (Phase 18 built, browser-checked, code-reviewed; waiting for Filip: read the questions in the report, push, PR, merge)_

## Where we stopped
- Release 1 accepted 2026-10-16. Phases 12 to 17 are merged (Release 2 done; Phase 17, the outbox, merged as PR #21 on 2026-11-04). Branch `feat/18-admin-stats`: Phase 18 (the admin statistics page, last phase of Release 3) is finished, Steps 0 to 5. No migration.
- Report: `docs/reports/2026-11-04-phase-18-admin-stats.md` (with the questions for Filip at the end); browser pass: `docs/reports/2026-11-04-phase-18-browser-pass.md`; guide: `docs/guides/phase-18-admin-stats.md`; make-admin steps: `docs/guides/grant-admin.md`; first-push texts: `docs/guides/phase-18-pr-text.md`; mockup: `docs/design/2026-11-04-phase-18/`.
- Everything is committed on the branch; nothing is pushed by Claude.

## Next step
1. Filip: read the open questions at the end of the Phase 18 report, push, open the PR with the texts in `docs/guides/phase-18-pr-text.md`, CI green, merge (no migration). Then make the online account admin (`docs/guides/grant-admin.md`) and look at the page on the real site and a phone (`docs/guides/phase-18-admin-stats.md`).
2. After that: Phase 19 (legal pages and data rights; planned 2026-11-04, research in `docs/reports/2026-11-04-phase-19-legal-research.md`, Filip's open questions at the top of `docs/DECISIONS.md`). Its Step 7b needs the `Admin` role from Phase 18.

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real phone check of the Phase 16 to 18 screens, the Macedonian drafts of Phases 9 to 18, the Google button and "Make an account", the invite link on his phone.
- BACKLOG: future and planned payments, the Phase 18, 17, 16, 15, 14, 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step. Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; whether the light theme gets a lighter peach; whether the Totals tab gets bars later; the Phase 18 questions in its report.

## Verification state
- VERIFIED by automated test (2026-11-04, run by Claude after the last coder run): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 6153/6153; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 13309/13309.
- VERIFIED by live run (headless Chromium, local stack): the Statistics page at 320/360/768/1280/1920 px in EN/MK, light/dark, reduced motion, as admin, normal user and guest (5 findings; 4 fixed and re-measured in a browser, 1 in BACKLOG; the earliest-date fix from the code review is VERIFIED by automated test only).
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, the real Neon data and the Cloudflare Pages path for `/api/admin/stats`, a real visit at 23:30 UTC, the Macedonian drafts.
