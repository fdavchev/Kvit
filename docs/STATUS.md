# Status: Kvit

_Last updated: 2026-11-04 (Phase 18 merged; Phase 19 Step 0 started: branch, doc fixes, mockup page)_

## Where we stopped
- Release 1 accepted 2026-10-16. Phases 12 to 18 are merged (Release 3 "polish" done; Phase 18, the admin statistics page, merged as PR #22 on 2026-11-04, `main` = `ddf41f2`).
- Branch `feat/19-legal` (made from that `main`): Phase 19, legal pages and data rights. Step 0 done: ROADMAP fixed (support email, no LICENSE file), mockup page `docs/design/2026-11-04-phase-19/index.html`, Filip's ten picks logged in `DECISIONS.md`.
- Brief: `docs/guides/phase-19-legal-prompt.md`; research: `docs/reports/2026-11-04-phase-19-legal-research.md` (not legal advice); Filip's decisions: `docs/DECISIONS.md`, "Phase 19"; first-push texts: `docs/guides/phase-19-pr-text.md`.
- Nothing is pushed by Claude.

## Next step
1. Filip: commit the Step 0 files (suggested subject: "Add the Phase 19 mockups, picks and doc fixes.").
2. Claude: Step 1, the texts (EN + MK) and `docs/legal/`. Tester first, then coder after Filip's go.
3. Filip, whenever: make the online account admin (`docs/guides/grant-admin.md`) and look at the Statistics page on the real site and a phone (`docs/guides/phase-18-admin-stats.md`). Phase 19 Step 7b (owner tools) needs it.

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real phone check of the Phase 16 to 18 screens, the Macedonian drafts of Phases 9 to 18, the Google button and "Make an account", the invite link on his phone.
- BACKLOG: future and planned payments, the Phase 18, 17, 16, 15, 14, 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step. Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository (asked again at the end of Phase 19); whether the light theme gets a lighter peach; whether the Totals tab gets bars later; the Phase 18 questions in its report.

## Verification state
- Last full run (Phase 18, 2026-11-04, VERIFIED by automated test): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 6153/6153; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 13309/13309. Phase 19 has changed no code yet.
- VERIFIED by live run (headless Chromium, local stack): the Statistics page at 320/360/768/1280/1920 px in EN/MK, light/dark, reduced motion, as admin, normal user and guest.
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, the real Neon data and the Cloudflare Pages path for `/api/admin/stats`, a real visit at 23:30 UTC, the Macedonian drafts.
