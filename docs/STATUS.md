# Status: Kvit

_Last updated: 2026-10-16 (Phase 12 built and checked; waiting for Filip's commit, push, PR and merge)_

## Where we stopped
- Branch: `feat/12-polish` (from `main` after PR #14). **Nothing is committed yet:** all Phase 12 code and docs are uncommitted.
- Done in Phase 12: dark colours B and the tan selected pill, the animation pass (sliding tab pill, tab fade, toast pop, list animations, pulse skeletons, rolling amount), glass on every popup with the hover and rest contrast fixes, the desktop layout (64rem and wider; phone unchanged), and the small items ("Stay" / «Смештај» / «Тема», Google button title, toast vs "+", "Updating…" at the top, Closing-owner notice on Add expense, «ден.» labels).
- Report: `docs/reports/2026-10-16-phase-12-polish.md`; guide: `docs/guides/phase-12-polish.md`.

## Next step
Filip commits (texts are in the chat), pushes, checks that CI is green, and merges (no migration). Then he runs the app himself (`.\scripts\start-local.ps1`) and follows `docs/guides/phase-12-polish.md`. Release 1 is done when Filip says it works for him on something real.

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real Google picture in a circle, and a read of the Macedonian drafts of Phases 9 to 12.
- BACKLOG: joining without an account (top "must do"), the Phase 12 leftovers (desktop requests, the Split sheet pill, "Updating…" on group pages), then Release 2 (Phase 13: personal spending + custom categories).

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository.
- Filip to decide: whether the light theme gets a lighter peach (options in the top item of `BACKLOG.md`, 2026-10-16).

## Verification state
- VERIFIED by automated test (2026-10-16, run by me after the last change): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 3485/3485 (no backend code changed in Phase 12); `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 7508/7508.
- VERIFIED by live run (the tester, headless Chromium, local stack, two passes): all screens at 360, 768 and 1280 px in EN/MK, light/dark, no overflow; sliding pills, toast pop, list animations, skeletons, rolling amount, reduced motion switching them off; glass sheets, dialog and toast; field edges; "Updating…" at the top; the new words; «ден.» labels; the Closing notice; the closing bar at 40rem; no console errors.
- NOT VERIFIED: a real phone, Safari, Firefox, real Google sign-in, the sign-up screen, hover states, a Finished group, saving an expense in a Closing group, keyboard order in the new menu, a screen reader, the Undo toast over the closing bar in a natural run, a line-by-line read of the Macedonian texts by Filip, and Filip running the app himself.
