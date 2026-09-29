# Phase 4, Step 2a: accounts (without the certificate)

Date: 2026-09-29. Branch `feat/04-accounts`. Tests by the `tester`, code by the `coder`, checked by Claude. Decisions: `DECISIONS.md`, top entry, "Step 2a".

## Labels
- **VERIFIED by automated test** / **VERIFIED by live run:** run, output read.
- **REPORTED by the coder, not repeated:** the coder ran it; I did not run it again.
- **NOT VERIFIED:** not run.

## What was built
- New project `Kvit.Contracts` (request/response shapes, `IAccountService`, `ICurrentUserProvider`, `IUnitOfWork`) and its Dockerfile `COPY` line.
- Domain rules (`AccountRules`, `NewAccount`, `LockoutLadder`): name 1–60, email, password (8+, a capital, a digit, Cyrillic counts), language, time zone.
- Handlers: register, log in, log out, change language, get me. Endpoints: `POST api/auth/register|login|logout`, `GET api/me`, `PUT api/me/language`.
- Identity (own endpoints, not `MapIdentityApi`), the `kvit_auth` cookie (HttpOnly, Secure, SameSite=Lax, 90 days), 401/403 instead of redirects, a fallback authorization policy (everything closed, health and the developer pages explicitly open).
- The lock ladder (5 wrong tries lock the account for 5, then 10, then 15 minutes), the `SignedUp` usage event written in the sign-up transaction, time zone updated at log-in unless set by hand.
- Migration `AccountRules`: unique email index, and checks that `display_name`, `time_zone` and `created_at` are set (from the Step 1 code review).
- 9 new error codes (8 planned + `AUTH_NOT_SIGNED_IN`); they need EN and MK text in Step 4.

## Results
| Check | Result | Label |
|---|---|---|
| Tests written first failed | the test project did not compile; the only error shown was the missing namespace `Kvit.Application.Commands` (it hides the others), 0 warnings; the tester listed the rest: the 8 missing `ResultCodes` constants and `ChangeLanguageCommand` | VERIFIED by live run |
| `dotnet build Kvit.slnx` (Debug and `--configuration Release`) | 0 warnings, 0 errors | VERIFIED by automated test |
| `dotnet test Kvit.slnx` | **403 of 403 pass** (was 290: Domain 161 → 161, Api 129 → 242, so 113 new). One reported test conflict (below) was resolved by adapting two old tests | VERIFIED by automated test |
| Register → me → log out → me is 401 → log in; wrong password; unknown email; lock ladder 5/10/15/15 and reset; email/name/password/language/time-zone rules; two same-time sign-ups; `SignedUp` event; cookie flags and 90-day expiry; closed by default (a controller without `[Authorize]` answers 401, health stays 200); handler scan and dispatcher round trip; database constraints and unique email | all pass | VERIFIED by automated test |
| `docker build -f src/api/Dockerfile .` | image builds | VERIFIED by live run |
| `dotnet ef migrations has-pending-model-changes` | none | VERIFIED by automated test (the pending-changes test) and REPORTED by the coder for the command |
| Live curl run against a temporary Postgres and the running API: register 200, me 200, log out 204, me 401, log in 200, me 200, wrong password 401 with `AUTH_INVALID_CREDENTIALS`, register `Set-Cookie` = `kvit_auth; expires` 90 days out; `path=/; secure; samesite=lax; httponly` | as listed | REPORTED by the coder, not repeated |
| `Europe/Skopje` time-zone file inside `mcr.microsoft.com/dotnet/aspnet:10.0` | file exists, tzdata 2026c installed | REPORTED by the coder, not repeated |
| Frontend | not touched, checks not re-run | NOT VERIFIED (not needed) |

## The one conflict found, and how it was resolved
The fallback policy also applies when no endpoint matches, so an anonymous request to an unknown path answers **401, not 404**. Two Phase 1 tests ("the API reference is not served in Production") expected 404. Decision (mine, logged): keep the fallback policy and change those two tests to expect 401; a signed-in visitor still gets 404.

## Not verified
- `TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Skopje")` inside our own image (only the file and package were checked).
- Two log-ins of the same account at the very same moment can give one 500 (`BACKLOG.md`); no test covers it.
- The tests inside GitHub Actions (the PR's CI run shows it).
- What the browser does with the cookie (Step 4 live run); Secure cookies over `http://localhost` worked in curl.
- Data Protection keys are still the .NET default (kept in memory or in the local profile); Step 2b moves them to Postgres with the certificate, before anything is deployed.

## Cost of this step (subagent token counts as reported by the harness; Step 1 for comparison)
| Agent | Tokens | Tool calls | Time |
|---|---|---|---|
| Tester (2a) | 169,509 | 72 | about 10.5 min |
| Coder (2a) | 220,517 | 118 | about 15 min |
| Step 1: tester + coder | 193,276 + 175,477 | 95 + 65 | about 17.5 + 10.7 min |
The total is about the same as Step 1 (390k vs 369k), for a bigger step (113 new tests here, 119 in Step 1, but far more production code now). The "no throwaway proofs" instruction cut the tester by about 12% in tokens and about 40% in time; the coder's cost grew with the size of the work. So the reading and instruction changes did not lower the total by themselves; most of the cost is the agents' own working calls.

## PR checklist (copy into the PR description at the end)
- [x] Step 1: database foundation
- [x] Step 2a: accounts (Identity, cookie, register / log in / log out / me, lock ladder)
- [ ] Step 2b: Data Protection keys in Postgres with a certificate
- [ ] Step 3: rate limiting, visitor address, proxy gate
- [ ] Step 4: frontend (Sign up, Log in, RequireAuth, Settings)

## Commit message
```
Add email accounts: register, log in, log out and me with the lock ladder and login cookie
```
