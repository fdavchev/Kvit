# Status: Kvit

_Last updated: 2026-11-04 (Phase 18 merged; Phase 19 Step 0 started: branch, doc fixes, mockup page)_

## Where we stopped
- Release 1 accepted 2026-10-16. Phases 12 to 18 are merged (`main` = `ddf41f2`, PR #22). Branch `feat/19-legal`: Phase 19, legal pages and data rights, being built without Filip at the laptop (his go for all steps, 2026-11-04; one commit per step, never pushed).
- Done: Step 0 (mockups, ten picks in `DECISIONS.md`); Step 1 (Terms of Use, new privacy notice, rates source line, `docs/legal/`; frontend 13655 tests pass). Step 2 (backend terms acceptance, 6226 backend tests pass); Step 3 (frontend agree line, notice) is in progress.
- Brief: `docs/guides/phase-19-legal-prompt.md`; research: `docs/reports/2026-11-04-phase-19-legal-research.md` (not legal advice); decisions: `docs/DECISIONS.md`, "Phase 19"; first-push texts: `docs/guides/phase-19-pr-text.md`.
- Nothing is pushed by Claude.

## Next step
1. Claude: Steps 2 to 11 in order (see ROADMAP). The final report and guide are written at Step 10.
2. Filip, whenever: make the online account admin (`docs/guides/grant-admin.md`); Phase 19 Step 7b needs it.

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real phone check of the Phase 16 to 18 screens, the Macedonian drafts of Phases 9 to 18, the Google button and "Make an account", the invite link on his phone.
- BACKLOG: future and planned payments, the Phase 18, 17, 16, 15, 14, 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step. Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository (asked again at the end of Phase 19); whether the light theme gets a lighter peach; whether the Totals tab gets bars later; the Phase 18 questions in its report.

## Verification state
- Last full run (Phase 18, 2026-11-04, VERIFIED by automated test): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 6153/6153; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 13309/13309. Phase 19 has changed no code yet.
- VERIFIED by live run (headless Chromium, local stack): the Statistics page at 320/360/768/1280/1920 px in EN/MK, light/dark, reduced motion, as admin, normal user and guest.
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, the real Neon data and the Cloudflare Pages path for `/api/admin/stats`, a real visit at 23:30 UTC, the Macedonian drafts.
