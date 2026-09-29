# Status: Kvit

_Last updated: 2026-09-29 (Phase 3 merged to `main`)_

## Where we stopped
- Phase 3 (money core) is merged to `main` via PR #4. `feat/03-money-core` is done.
- New code: `src/api/Kvit.Domain/MoneyRules/` (money, rounding steps, the four split types, balances, "who pays whom") and 9 new error codes in `Results/ResultCodes.cs`. Tests: `tests/Kvit.Domain.Tests/MoneyRules/`.
- A small `docs/guides/install-tools.md` edit that predates Phase 3 went into the same commit; it isn't part of the money core.
- Phase 2 (frontend skeleton) is merged to `main` via PR #2.

## Next step
Start Phase 4 (database + email accounts): branch `feat/04-accounts`, per `docs/ROADMAP.md`. Docker Desktop must be running first.

## Then
- Phase 4 (database + email accounts), per `docs/ROADMAP.md`. Docker Desktop is needed from here on (Testcontainers).

## Blockers and open questions
- None outstanding for Filip.

## Verification state
- Backend (Phase 3, 2026-09-29): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` 165/165 passing (Domain 150, Api 15), after the code-review follow-up — VERIFIED by automated test (Filip re-ran it, 2026-09-29).
- A deliberate break of the leftover rule (always to the first person) made 9 tests fail, then was undone — VERIFIED by live run.
- Frontend (Phase 2): VERIFIED 2026-09-26, unchanged by Phase 3 (no frontend files touched; frontend checks not re-run).
