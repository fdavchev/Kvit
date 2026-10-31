# Status: Kvit

_Last updated: 2026-10-26 (Phase 15 built, browser-checked and code-reviewed; waiting for Filip: migrations, push, PR, decisions)_

## Where we stopped
- Release 1 accepted 2026-10-16. Phases 12 to 15 are merged (Phase 15 as PR #19).
- Branch: `feat/16-group-insights` (from `main`). Phase 16 (possible duplicates, group totals per category, group spending plan; last phase of Release 2) finished Step 0 (mockup `docs/design/2026-10-27-phase-16/index.html`, Filip's picks logged). Prompt: `docs/guides/phase-16-group-insights-prompt.md`; first-push texts: `docs/guides/phase-16-pr-text.md`.
- Filip chose to include two Phase 15 leftovers: the limit amount message and "near the limit" in words.
- Nothing is pushed.

## Next step
1. Steps 0 to 7 are done (backend 5884, frontend 11649). Open: Filip's yes/no on "Not the same" (save "No, it's new" instead of deleting the answer; browser-pass bug 2). Next is Step 8: `/code-review high`, then the Phase 16 report and guide, DECISIONS "rules that stay", BACKLOG, final PR text.

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real Google picture in a circle, the Google button inside the Log in sheet and "Make an account", the invite link on his phone, and a read of the Macedonian drafts of Phases 9 to 15.
- BACKLOG: future and planned payments, the Phase 15 leftovers, the Phase 14, 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step.
- DECISIONS items 8 to 11 are answered (2026-10-26) and built. Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; whether the light theme gets a lighter peach.

## Verification state
- VERIFIED by automated test (2026-10-26, run by me after the last coder run): `dotnet build Kvit.slnx` 0 warnings; `dotnet test Kvit.slnx` 5388/5388; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 10663/10663.
- VERIFIED by live run (the tester, headless Chromium, local stack): all Phase 15 screens at 320/360/768/1280/1920 px in EN/MK, light/dark, reduced motion (3 bugs, all fixed; the fixes VERIFIED by automated test only).
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, other time zones, Neon and Render (3 migrations), the deploy gap, the Macedonian drafts.
