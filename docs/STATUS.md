# Status: Kvit

_Last updated: 2026-09-29 (Phase 3 built on `feat/03-money-core`, not merged yet)_

## Where we stopped
- Phase 3 (money core) is built on the branch `feat/03-money-core` and passes build and tests. The changes are not committed yet: Filip commits, pushes and opens the pull request.
- New code: `src/api/Kvit.Domain/MoneyRules/` (money, rounding steps, the four split types, balances, "who pays whom") and 9 new error codes in `Results/ResultCodes.cs`. Tests: `tests/Kvit.Domain.Tests/MoneyRules/`.
- `docs/guides/install-tools.md` has an uncommitted change from before Phase 3. It isn't part of this phase; Filip decides what happens to it.
- Phase 2 (frontend skeleton) is merged to `main` via PR #2.

## Next step
Filip: commit Phase 3, push `feat/03-money-core`, open the pull request, see CI go green, merge into `main`.

## Then
- Phase 4 (database + email accounts), per `docs/ROADMAP.md`. Docker Desktop is needed from here on (Testcontainers).

## Blockers and open questions
- None outstanding for Filip.

## Verification state
- Backend (Phase 3, 2026-09-29, on the branch): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` 165/165 passing (Domain 150, Api 15), after the code-review follow-up — VERIFIED by automated test (Filip re-ran it, 2026-09-29).
- A deliberate break of the leftover rule (always to the first person) made 9 tests fail, then was undone — VERIFIED by live run.
- NOT VERIFIED yet: CI on GitHub for this branch (not pushed).
- Frontend (Phase 2): VERIFIED 2026-09-26, unchanged by Phase 3 (no frontend files touched; frontend checks not re-run).
