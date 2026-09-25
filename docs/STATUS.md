# Status: Kvit

_Last updated: 2026-09-25 (building session, planning step)_

## Where we stopped
- Branch: none yet. Filip creates the local repository himself (`guides/start-the-repository.md`, Part 1).
- GitHub: `https://github.com/fdavchev/Kvit` exists (public, empty, description set, no licence, push protection on; VERIFIED with `gh` on 2026-09-25).
- Uncommitted changes: everything (not a repository yet).
- Last thing done: Step 1 of `START-HERE-PROMPT.md` is written:
  - `docs/SCREENS.md`: 20 Release 1 screens with tap counts, plus "who can do what".
  - `docs/DATA-MODEL.md`: every table, the money rules and the group-closing states.
  - `docs/ROADMAP.md`: 11 phases to a deployed Release 1, then Releases 2 and 3.
  - `CLAUDE.md` for the project, and `guides/start-the-repository.md` for Filip.
  - Planning decisions and clashes logged at the top of `DECISIONS.md`.

## Next step
1. **Filip:** read `DATA-MODEL.md` and `ROADMAP.md`; answer the open questions (end of `DATA-MODEL.md`) and the proposals (top of `DECISIONS.md`), or say "go with your suggestions".
2. **Filip:** Part 1 of `guides/start-the-repository.md` (git init, first commit, branch `feat/01-backend-skeleton`, open Docker Desktop).
3. **Claude:** Phase 1 (backend skeleton), after Filip says go.
4. **Filip, tonight:** Part 2 of that guide (hosting accounts, GitHub repository).

## Blockers and open questions
- Waiting for Filip's OK on the data model and roadmap (his rule: no code before that).
- Open business questions: all Release 1 questions answered on 2026-09-25. One Release 2 question is left (`DATA-MODEL.md`, question 6); it can wait.
- Styling decided: Tailwind v4 + shadcn/ui (Base UI) + Sonner (`DECISIONS.md`).

## Verification state
- Tests: none, since there's no code yet (NOT VERIFIED).
- App run: none (NOT VERIFIED).
- Tool versions and package versions: VERIFIED by local commands and registry lookups on 2026-09-25 (see `DECISIONS.md`).
