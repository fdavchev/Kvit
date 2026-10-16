# Prompt for Phase 12: Animations + touch-ups

**When to use:** Phase 11 is merged (PR #14). Update `main` (`git switch main`, `git pull`), start a new branch from it (`feat/12-polish`), open Claude Code in the Kvit folder and paste everything between the two lines below.

---

Filip wants to run **Phase 12: Animations + touch-ups** (`feat/12-polish`). Phase 11 (dashboard) is merged. Release 1 is done when Filip says it works for him on something real, so expect his feedback from real use to arrive during this phase; collect it in `docs/BACKLOG.md` and ask him what comes first.

**Start by reading:** `docs/STATUS.md`, `docs/ROADMAP.md` (Phase 12), `docs/DECISIONS.md` (the Phase 11 "rules that stay", and the "Speed" rules), `docs/ARCHITECTURE.md` (the headings first, then Parts 2, 3, 4 for the frontend), `docs/BACKLOG.md` (the items called "Animations", "Phase 11 Step 6 full-pass leftovers", "Phase 11 Step 6 re-check leftovers", and the desktop layout item), and `docs/reports/2026-10-16-phase-11-dashboard.md` (the Step 5 and Step 6 decision log at its end).

**Goal (from the ROADMAP; check the docs, do not trust this summary):**
1. **The glass-look redesign on every popup** (sheets, dialogs, the closing bar, toasts). The glass look is in `kvitGlassLooks.ts` and `kvitButtonLooks.ts`. Contrast stays at 4.5:1 or better everywhere, also on mouse hover (a Phase 11 leftover).
2. **The animation pass with Filip's 2026-10-09 picks** (BACKLOG): bottom sheet A (quick, CSS); tab switch B (CSS slide of the active pill, a short fade of the panel); button press B (subtle `active:scale`); list add and remove B (AutoAnimate, `@formkit/auto-animate`); Undo toast B (a pop, through Sonner's own settings, to be checked; Sonner's default 0.4 s should be about 0.2 s); loading A (pulse skeleton); the animated amount: Filip does not like the first draft, so propose a redesign (NumberFlow `@number-flow/react` is the candidate) and wait for his OK. Every animation respects `prefers-reduced-motion`. Check each library's licence and version against its real docs before using it. Free forever, no paid service.
3. **Dark mode surface colours:** Filip dislikes the `color-mix` ones (`--color-night-field`, `--color-night-raised`, `--color-night-line` in `src/web/src/index.css`). Show him options in a small HTML mockup (A, B, C with clickable buttons) before changing them.
4. **The desktop layout:** a wider column, larger text, perhaps a sidebar and several columns on a wide window. Today the screens are a centred 400 px column. Show options in a mockup first. The phone layout at 360 px must not get worse.
5. **The small look items in BACKLOG**, in this order of value: contrast on hover, the Google button iframe title language, the "Accommodation" wording in the Category sheet (Filip decides the word), a toast covering the "+", the "Updating…" label covering a Home card title, the Macedonian «Позадина» versus «Тема» (Filip decides), the Add expense warning for a Closing group owner.

**How to work (propose first, wait for Filip's OK, ask before each coder run):**
- Split it into small steps and give a plan before writing anything. Suggested order: the cheap animations (sheet, tab, button press, toast), then the list animations and skeletons, then the dark colours, then the glass redesign, then the desktop layout, then the small items. For each look decision, open a mockup in his browser (`Start-Process` on the HTML file) with the options on one page, and let him pick.
- Animate only transform and opacity; keep each one short (about 0.15 to 0.25 s); never block a tap; test with `prefers-reduced-motion`. The skills `animate`, `emil-design-eng`, `apple-design` and `mobile-native` are available for the details.
- Speed is the product: no animation may slow a save or add a tap.
- Tests: the frontend Vitest suite must stay green (7119 tests at the end of Phase 11). Add tests for new behaviour (for example that an animation is switched off under reduced motion). A real-browser check at 360 px in EN and MK, light and dark, with Playwright against the local stack (`.\scripts\start-local.ps1 -NoBrowser`, then `-Stop`; in a Vite dev run only block URLs that start with `/api/`). Docker Desktop must be open for the backend tests; ask Filip to start it, do not work around it.
- Run `dotnet build Kvit.slnx` (0 warnings), `dotnet test Kvit.slnx`, `npm run lint`, `npm run build` and `npm test` before saying anything passes, and read the output.
- The first-push texts (commit subject, PR title, PR description) come at the end; later pushes get a one-line subject.

**Working rules (repeat them to subagents):** start every reply with "Filip"; propose then wait; one step at a time, each step ending with "your part" (Docker, terminal, manual things); no code comments; commits are one subject line with no body, no Co-Authored-By line; Filip makes every commit and merges the PR himself; ask before every coder run; the tester may run inside an approved plan; give subagents the memory folder path `C:\Users\Davchev\.claude\projects\c--Users-Davchev-Projects-Kvit\memory\`; label every claim as VERIFIED by automated test, VERIFIED by live run, or NOT VERIFIED; log decisions in `docs/DECISIONS.md` as you go; update `docs/ROADMAP.md` and `docs/STATUS.md`; no report file per small step.

---
