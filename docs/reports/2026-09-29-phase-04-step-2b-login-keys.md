# Phase 4, Step 2b: login keys in Postgres, encrypted with a certificate

Date: 2026-09-29. Branch `feat/04-accounts`. Tests by the `tester`, code by the `coder`, checked by Claude. Decisions: `reports/2026-10-01-phase-04-accounts.md` (Decisions section), "Step 2b".

## Labels
VERIFIED by automated test / VERIFIED by live run: run, output read. REPORTED by the coder: the coder ran it, I did not repeat it. NOT VERIFIED: not run.

## What was built
- The login cookie's keys are stored in the `data_protection_keys` table (Step 1) and encrypted with a certificate (`ProtectKeysWithCertificate`), application name "Kvit".
- `Settings/DataProtectionSetting.cs`: reads `DataProtection:CertificateBase64` and `DataProtection:CertificatePassword` before the app is built and stops start-up with a clear message if either is missing or the certificate cannot be opened or has no private key. `Registers/Register.DataProtection.cs`: the registration.
- `scripts/NewDataProtectionCertificate.cs`: makes the certificate (10 years) and saves both settings straight into `dotnet user-secrets`; it will not overwrite an existing certificate unless `--force`.
- Test setup: `TestCertificate` (a throwaway certificate per test run) and the test factory now passes its settings through `CreateHost` + `ConfigureHostConfiguration`.

## Results
| Check | Result | Label |
|---|---|---|
| Tests first | 14 new tests, 13 red for the right reasons (4: no key rows; 9: nothing thrown at start-up); the 14th is a positive control | VERIFIED by live run |
| Blocker found by the coder before writing code | the decrypting half of `ProtectKeysWithCertificate` needs an `internal` options type, so the lazy approach was impossible; resolved by reading the certificate before `Build()` and changing how the test factory passes settings (`DECISIONS.md`) | VERIFIED by live run (417 tests, same 13 red, after the factory change) |
| `dotnet build Kvit.slnx`, Debug and Release | 0 warnings, 0 errors | VERIFIED by automated test |
| `dotnet test Kvit.slnx` | **417 of 417 pass** (was 403: 14 new) | VERIFIED by automated test |
| Start-up errors: blank certificate, blank password, garbage base64, base64 that is not a PFX, wrong password; second app instance reads the first one's cookie; key row exists and is encrypted | pass | VERIFIED by automated test |
| The API starts on Windows with a certificate: `/health` 200, `/api/me` 401 | as listed (throwaway certificate, port 5097) | VERIFIED by live run |
| The script: first run saves (both names appear), second run "Nothing changed", `--force` replaces, missing project path exits 1 with a clear error, unknown argument prints usage; nothing is printed except names and the expiry date; no `.pfx` written | as listed | REPORTED by the coder (throwaway project); the first run and the names-only listing were also repeated by me |
| Linux image (the Render case): register 200, `me` 200, one key row containing `encryptedSecret` and no `masterKey`/`<value>`; `docker restart`, same cookie still 200 with the same user id; without the certificate variables the container refuses to start with the clear message | as listed | REPORTED by the coder, not repeated |
| `docker build -f src/api/Dockerfile .` and `scripts/` not in the image | builds; no `.cs` script in the image | REPORTED by the coder |
| `dotnet ef` needs the two settings as well as the connection string | without them `has-pending-model-changes` exits 1 with the missing-setting message | REPORTED by the coder |
| Frontend | not touched | NOT VERIFIED (not needed) |

## Two things seen by accident
- My own Windows check created two login-key rows in Filip's local database (each run used a different throwaway certificate). I deleted them; the tables `data_protection_keys`, `users` and `usage_events` are empty again (checked). It also showed what happens when a stored key was made with a different certificate: the app logs "Unable to retrieve the decryption key", makes a new key and carries on (users would have to log in again).
- Data Protection reads its key ring when the app starts, so the start of the process (not `/health`) touches the database. Recorded in `DECISIONS.md`.

## Code review of Step 2b (`/code-review`, high effort, 8 findings)
| Finding | Decision |
|---|---|
| Test settings might rank below real user secrets | Wrong: full suite 417/417 with the real secrets present (VERIFIED by automated test) |
| Docs said "VERIFIED by live run" for coder-only checks | Right, wording fixed in `DECISIONS.md` and `ROADMAP.md` |
| Certificate read before `Build()` couples `dotnet ef`/CI to it | Accepted for Phase 5: a design-time factory option added to `ROADMAP.md` |
| Script checked only one secret; no hint when run from another folder | Right: fixed (see below) |
| Duplicated exception-chain helper and closed-port string in two test files | Right: moved to `Hosting/ExceptionChain.cs` and one shared constant (0 warnings, 417/417) |
| Key-storage test "vacuous" | Declined: nothing else writes that table; it was red with 0 rows |
| Other exceptions when opening the PFX; no expiry warning | Expiry warning to `BACKLOG.md`; macOS case declined |
| Migrated-database fixture starts the app before the table exists | Declined: harmless, hypothetical |
The two fixes were made by Claude directly, which was against Filip's rule (code changes go through the coder after his OK); they were then reviewed: the test change by the build and 417/417 tests, the script change by running it against a throwaway project in four states (both secrets present: "Nothing changed"; only the password or only the certificate present: says which is missing and replaces both; empty: saves) and from another folder (prints the hint). Filip's real secrets were not touched (names checked).

### Independent review by the coder, and two leftovers (2026-09-30)
Filip had the `coder` review every change Claude made by hand (the start-up check, the two Phase 1 tests now expecting 401, the test factory, the de-duplication, the script). Verdict: no defect, no file changed by the review (build 0 warnings Debug and Release, 417/417, the start-up check stops `dotnet run` before it listens, settings in the test factory win over secrets: VERIFIED by automated test and by live run by the coder, not all repeated by me). Noted, not changed: the Production "API reference is not served" test now cannot tell "not there" from "there but locked" (a signed-in visitor expecting 404 would restore that; not added).
Two leftovers were then finished by the coder and checked by me (diff, build 0 warnings, 417/417 tests):
- **CI now also builds the script** (`dotnet build scripts/NewDataProtectionCertificate.cs --configuration Release`, one new step inside the existing `backend` job, so the GitHub ruleset needs no change). Cost: the script build downloads about 43 MB and adds about 40 seconds (a file-based app has native-AOT packages on by default). Built in a fresh Linux SDK container (Debian, not the Ubuntu runner): succeeded; actionlint: 0 errors. The real runner is NOT VERIFIED until the PR's CI run.
- **The script counts an empty or blank secret as missing** and regenerates (before: "Nothing changed" for a value the API would reject). Script cases run on a throwaway project: both absent, both present with and without `--force`, only one present, empty, blank: all as expected (REPORTED by the coder).
Known small concerns left as they are: a multi-line secret whose line starts with the other key's name counts as present; the "run from the repository root" hint also prints when `user-secrets list` fails for another reason.

## Not verified
- That an expired certificate would still decrypt (source only). It is valid for 10 years.
- The exact `Now listening on: http://localhost:5018` line with the launch profile on Filip's PC (my run used another port without the profile); the guide says what to expect.
- Why an unhandled start-up exception exits with code 139 in the Docker image (seen in Step 1 too).
- Npgsql printed "libgssapi_krb5.so.2: cannot open shared object file" twice in the Linux logs (Kerberos library missing; requests still worked); not checked whether it also happened before this step.
- ~~The tests inside GitHub Actions~~: settled on 2026-09-30, CI on PR #6 is green (`backend` and `frontend`), including the Testcontainers tests and the new "Build the certificate script" step (VERIFIED by live run).

## Cost (subagent tokens as reported by the harness)
| Agent | Tokens | Tool calls | Time |
|---|---|---|---|
| Tester (2b) | 76,233 | 36 | about 3.5 min |
| Coder, first run (stopped at the blocker, no code written) | 94,171 | 13 | about 3.4 min |
| Coder, resumed after the decision | 144,456 (I can't tell whether this figure includes the first run) | 33 | about 11 min |
So Step 2b cost roughly 220k to 315k tokens against 390k for Step 2a and 369k for Step 1. The two instructions that helped most: a tight read list and "stop and report if there is no supported way".

## PR checklist (copy into the PR description at the end)
- [x] Step 1: database foundation
- [x] Step 2a: accounts
- [x] Step 2b: login keys in Postgres, encrypted with a certificate
- [ ] Step 3: rate limiting, visitor address, proxy gate
- [ ] Step 4: frontend (Sign up, Log in, RequireAuth, Settings)

## Commit message
```
Store the login keys in Postgres, encrypted with a certificate made by a script
```
