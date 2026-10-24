# Status: Kvit

_Last updated: 2026-10-24 (Phase 14 merged; Phase 15 branch made, prompt written, mockup next)_

## Where we stopped
- Release 1 was accepted on 2026-10-16. Phases 12, 12b, 13 and 14 are merged (Phase 14 = PR #18).
- Branch: `feat/15-budget`, made from the updated `main`. Phase 15 (budget share, budget currency, limits, monthly summary) is one big phase (Filip's choice). Written so far: the prompt `docs/guides/phase-15-budget-prompt.md`, the PR texts `docs/guides/phase-15-pr-text.md`, the first decisions in `DECISIONS.md`, the steps in `ROADMAP.md`. No code yet.
- Filip's answers: one big phase; a claimed name's older shares count in his Budget; the budget currency (one converted total or MKD and EUR apart) is decided from a mockup.

## Next step
1. Step 0: the mockup `docs/design/2026-10-24-phase-15/index.html` (budget currency, shares on the Budget tab, where the switch lives, limits, monthly summary). Filip picks.
2. Then the backend steps, one at a time; Claude asks before each coder run.

## Then
- Filip, once on the real site: one real EUR expense (first real NBRM call), a real Google picture in a circle, the Google button inside the Log in sheet and "Make an account", the invite link on his phone, and a read of the Macedonian drafts of Phases 9 to 14.
- BACKLOG: future and planned payments (paid / not yet paid), the Phase 14 leftovers, the Phase 12 and 12b leftovers.

## Blockers and open questions
- Nothing blocks the next step.
- Filip to decide: whether `START-HERE-PROMPT.md` stays in the public repository; whether the light theme gets a lighter peach (BACKLOG, 2026-10-16); the budget currency (mockup).
- Open, Filip has not decided: a second "Edit my categories" link on the Budget tab (not built; my view: not needed).

## Verification state
- Last full run (2026-10-24, Phase 14, on the merged code): `dotnet build` 0 warnings; `dotnet test` 4892/4892; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 9717/9717 (VERIFIED by automated test then; not re-run on the new branch, which has no code changes).
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader, Neon and Render for the Phase 14 migrations, the Macedonian drafts.
