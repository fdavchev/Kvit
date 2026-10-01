# Phase 5, Step 2: Neon and the first migration (report, 2026-10-01)

## What was done
- Filip moved the local database and the tests to Postgres 18 (decision in `DECISIONS.md`), then created the Neon project (Frankfurt, Postgres 18; the version is REPORTED by Filip).
- The three migrations were applied to Neon from Filip's PC. The connection string was read from Filip's clipboard by Claude's command into a setting that lived only for that command; it was never printed or written to a file, and the setting and clipboard were cleared afterwards.

## Results
| Claim | Label |
|---|---|
| Local container runs PostgreSQL 18.6 with 10 tables and the 3 migrations | VERIFIED by live run |
| 508 of 508 backend tests pass on Postgres 18, 0 build warnings | VERIFIED by automated test |
| The string had the right shape (direct host, no `-pooler`), checked without printing it | VERIFIED by live run |
| Before: the 3 migrations were `(Pending)` on Neon; `dotnet ef database update` printed the 3 `Applying migration` lines and `Done.`; after: the list shows all 3 without `(Pending)` | VERIFIED by live run |
| Neon accepted `SSL Mode=VerifyFull;Channel Binding=Require` from Windows | VERIFIED by live run (the same setting is NOT yet verified on GitHub's Linux runner) |
| Neon's Tables view shows the tables | NOT VERIFIED (Filip to check) |
| GitHub secret `NEON_DIRECT_CONNECTION_STRING` exists; Push protection is on | NOT VERIFIED (Filip to do) |

## Notes
- The pooled string (`-pooler` host plus `No Reset On Close=true`) is in Filip's private notes and is used in Step 3 on Render. It has not been tried against Neon yet (NOT VERIFIED).
- Filip's first attempts failed because the clipboard held the command text instead of the connection string (copying the command from the guide replaced it). The guide now uses a prompt (`Read-Host`) that avoids the mix-up.
