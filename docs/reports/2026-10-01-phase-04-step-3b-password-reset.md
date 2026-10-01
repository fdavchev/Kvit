# Phase 4, Step 3b: password reset by hand, must-change-password, change password

Date: 2026-10-01. Branch `feat/04-accounts` (PR #6, open). Tests by the `tester`, code by the `coder`, checked by Claude. Decisions: `reports/2026-10-01-phase-04-accounts.md` (Decisions section), "Step 3b". Guides: `guides/reset-a-password.md`, `guides/phase-04-local-setup.md` Part 4.

## Labels
VERIFIED by automated test / VERIFIED by live run: run by me, output read. REPORTED by the coder: the coder ran it, I did not repeat it. NOT VERIFIED: not run.

## What was built
- **Database:** `users.must_change_password` (boolean, not null, default false), migration `MustChangePassword`.
- **Answers:** every account answer (sign-up, log-in, `GET /api/me`) carries `mustChangePassword`.
- **Forced change:** while the flag is true, every signed-in endpoint except `GET /api/me`, log-out and change-password answers 403 `AUTH_MUST_CHANGE_PASSWORD`. Anonymous requests still get 401.
- **`POST /api/auth/change-password`** (`currentPassword`, `newPassword`): 204; clears the flag, ends other sessions, keeps this one signed in. A wrong current password answers 400 `AUTH_CURRENT_PASSWORD_WRONG` and counts in the lock ladder; a weak new password answers 400 `AUTH_PASSWORD_TOO_WEAK`; the same password answers 400 `AUTH_PASSWORD_UNCHANGED`. It uses the `log-in` rate limit.
- **Security stamp checked on every request**, so a reset or a password change ends other sessions at once.
- **Reset by hand:** `PasswordResetService` (temporary password: 12 characters, no 0/O/1/l/I, follows the password rule; only the hash is saved; flag set; lock cleared; sessions ended; email found case-insensitively) and the script `scripts/ResetPassword.cs`. CI compiles the script in the `backend` job (new step, no new job, so nothing to add in the GitHub ruleset).
- **Not in this step:** the screens (Step 4) and the Macedonian texts for the three new codes.

## Results
| Check | Result | Label |
|---|---|---|
| Tests first | 44 red for the right reasons (20 empty reset shell, 6 missing route, 4 rate limit and `Retry-After` on the new route, 5 missing `mustChangePassword` field, 4 missing column, 5 behaviour of change-password); 2 new tests already green as guards | VERIFIED by automated test (I re-ran them before the coder started) |
| `dotnet build Kvit.slnx`, Debug and Release | 0 warnings, 0 errors | VERIFIED by automated test |
| `dotnet test Kvit.slnx` | **493 of 493 pass** (was 452: 41 new) | VERIFIED by automated test |
| Both scripts compile in Release (`dotnet build scripts/ResetPassword.cs -c Release`, and the certificate script) | build succeeds, exit 0 | VERIFIED by live run |
| Pending-model-changes test, schema tests for the new column, rate-limit tests for change-password, existing closed-by-default, cookie-flag and rate-limit tests | pass inside the 493 | VERIFIED by automated test |
| Script on a throwaway database (`kvit_throwaway_reset` on `kvit-postgres`, API on port 5099 in the Staging environment so your user secrets were not loaded): unknown email exits 1 with "Nothing changed"; no argument prints usage, exit 1; `Ana@ExaMple.com` finds `ana@example.com`, prints a 12-character password once, flag true, lock cleared, hash does not contain the password; old password 401; temporary password logs in with `mustChangePassword: true`; the lowercase email finds the same single account; a blank connection string gives the clear message, exit 1 | as listed | REPORTED by the coder |
| Your real database after the run | only the database `kvit` exists, `users` is empty, `data_protection_keys` still has only your row (id 3) | VERIFIED by live run |
| Your local database has the new column | **not yet**: it has 2 of the 3 migrations. You run Part 4, Step 2 of the local guide | NOT VERIFIED (needs Filip's command) |
| Reading the connection string from user secrets inside the script | not run (it would connect to the real database). The environment variable winning over secrets was seen with a blank variable | NOT VERIFIED |
| `docker build` | not run; no new project was added | NOT VERIFIED |
| Frontend, real browser, a real phone | not touched | NOT VERIFIED |

## Things to know
- A signed-in person with the flag set who asks for a path that does not exist gets 403, not 404.
- The new password is checked before the current one, so a too-weak or unchanged password does not count as a wrong try. A successful change sets the lock counter to 0.
- Every signed-in request now costs one extra indexed lookup (the security stamp check). It was your choice to let me pick; I kept it.
- Until the frontend exists, a person cannot finish the forced change in a browser. The API side is complete and tested.

## Cost (subagent tokens as reported by the harness)
| Agent | Tokens | Tool calls | Time |
|---|---|---|---|
| tester, resumed run (the first run was stopped by Filip; its cost was not reported) | 143,865 | 7 | about 1.5 min |
| coder | 221,535 | 58 | about 11 min |
| **Total (known)** | **365,400** | 65 | |

(Earlier steps: 1 about 369k, 2a about 390k, 2b about 220k to 315k, 3 about 237k.)
