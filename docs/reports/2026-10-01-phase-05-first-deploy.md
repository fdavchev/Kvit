# Phase 5: first deploy (report, 2026-10-01)

Branch `chore/05-first-deploy`, PR #7. Step reports: `2026-10-01-phase-05-step-1-migration-path.md`, `2026-10-01-phase-05-step-2-neon.md`, `2026-10-01-phase-05-step-3-online-and-ui.md` (the live proofs and the CI-deploy details are in the third).

## What Phase 5 delivered
- Kvit is online for free: website `https://kvit-mk.pages.dev` (Cloudflare Pages), API on Render (`kvit-mk-api`, Frankfurt, Free), database on Neon (Frankfurt, Postgres 18, Free). No card anywhere.
- The way database changes reach Neon: an EF migration bundle built and run by CI on a push to `main`, using a design-time factory so only the connection string is needed.
- CI deploys to Render and waits until it is live (`scripts/RenderDeploy.cs`), shown as a line in the CI list.
- Added scope, approved by Filip: Postgres 18 everywhere, a show/hide eye button on every password box, a light/dark button on the Welcome screen, a rewritten README with the live link and 4 real screenshots.
- Docs: hosting guide values filled in, `guides/phase-05-go-online.md`, the online steps for `guides/reset-a-password.md`, new traps in `ARCHITECTURE.md` Part 4.

## Results
| Claim | Label |
|---|---|
| Backend 517/517 tests on Postgres 18, build 0 warnings | VERIFIED by automated test |
| Frontend 467/467 tests, lint 0 warnings, build 0 type errors | VERIFIED by automated test |
| CI green on the branch (backend and frontend) | VERIFIED by live run |
| Neon has the 3 migrations; the site, Render and Cloudflare answer; `onrender.com` is 403 without the proxy secret; a log-in reaches Neon | VERIFIED by live run |
| Visitor-address chain (made-up headers ignored, 429 on the 11th log-in), `CF-Connecting-IP` refused by Cloudflare, sign-up limit 429 on the 6th | VERIFIED by live run |
| `/health` does not wake Neon; first database request after a sleep took 4.05 s and succeeded; Render wake-up 22.4 s | VERIFIED by live run (one sample each) |
| Still logged in on the site after a Render redeploy (login keys live in Postgres) | REPORTED by Filip |
| Real phone: sign-up, closed browser, still logged in, and the visual checks (status bar, zoom, keyboard, notch padding, tap flash, tap sizes, theme default) | REPORTED by Filip ("everything checks out"; no per-item notes) |
| Read-only Render API call and `--watch-latest` against the real service | VERIFIED by live run |
| The migration step on `main`, the Render deploy trigger and the CI line on GitHub | NOT VERIFIED until the first push to `main` (the merge) |
| Google accepts `kvit-mk.pages.dev` as an authorized domain | NOT VERIFIED (Filip's two-minute test, Phase 6) |
| Render's handling of a CI line created late | not applicable: Auto-Deploy is Off and CI deploys |

## Decisions and rejected alternatives (moved word for word from DECISIONS.md on 2026-10-01)
## 2026-10-01: Phase 5 (first deploy): decision log, in progress
Facts behind these choices were re-checked against official docs on 2026-10-01 (`reports/2026-10-01-phase-05-step-1-migration-path.md`).

**Step 1 (migration path)**
- **Migrations reach Neon through an EF migration bundle run by CI,** as steps at the end of the existing `backend` job, on a push to `main` only, with the **direct** Neon string from the GitHub secret `NEON_DIRECT_CONNECTION_STRING`.
  - *Why:* Microsoft's guide (updated 2026-08) says "for automated deployment, use a migration bundle". Render's "After CI Checks Pass" waits for all checks on the commit and skips the deploy if one fails, so a failed migration stops the deploy.
  - *Why steps and not a new job:* whether a job queued behind `needs:` already counts as a check when Render looks is not documented. Steps inside `backend` have no such gap and need no ruleset change.
  - *Rejected:* the app migrating itself at start-up (every Render wake-up would touch Neon); `dotnet ef database update` in CI (Microsoft: development only); a SQL script (needs a manual review step and skips EF's migration lock).
- **A design-time factory (`AppDbContextFactory`) in `Kvit.Api`,** so `dotnet ef` and the bundle need only the connection string. EF's tools return before running `Program.cs` when a factory exists and there is one context (EF source, release/10.0).
  - *Why:* the online certificate never has to go to GitHub, and local `dotnet ef` no longer needs the certificate settings.
  - *Rejected:* putting the certificate in GitHub secrets (two more secrets in one more place).
  - The `UseNpgsql` + snake_case setup became one method, `UseKvitDatabase`, used by the app and the factory.
- **The bundle is built with a harmless placeholder connection string and run with the secret.** Creating the bundle starts the context through the factory, so it needs a string even though it does not connect. Only the step that runs the bundle sees the secret, so the build step (project code) never does.
- **A CI guard, `dotnet ef migrations has-pending-model-changes`,** runs on every push and fails when the model changed without a migration. Proven locally: exit 0 with no change, exit 1 after a change.
- **Migrations must be backward-compatible** with the code still running, because they run before the new code is live (rule added to `ARCHITECTURE.md` Part 1).
- **`EnableRetryOnFailure` is NOT adopted (decided 2026-10-01 after the live run):** after Neon had slept, the first database request on the real site took 4.05 s and succeeded (401 for a wrong password, no 500); the next took 0.54 s. Render's wake-up took 22.4 s. One sample, so it is watched: if a first request ever fails after a sleep, adopt the retry and move the whole unit of work into the execution strategy. (Original reasoning follows.) Not adopted unless the live run shows Neon's wake-up failing the first request. Neon says it wakes in "a few hundred milliseconds" but not whether a connection waits (NOT VERIFIED). Step 4 measures it. If it fails, `IUnitOfWork` must run the whole unit of work inside `CreateExecutionStrategy().ExecuteAsync`, because EF throws on user-started transactions.

**Postgres 18, not 17 (Filip's call, 2026-10-01)**
- Local database (`compose.yaml`), the Testcontainers tests (`PostgresFixture`) and Neon all move to Postgres 18 together, so they stay the same major version.
  - *Why:* Filip: moving now, while there is no real data, is cheaper than moving later. 18 brings speed for big databases that Kvit does not need yet (async I/O, skip scan), plus features Kvit does not use (`uuidv7()`, virtual generated columns). The reason for 17 was only "same version everywhere", and that holds for 18 too.
  - *Condition set before the move, met:* all 508 backend tests pass on 18 (0 warnings).
  - *Docker catch (VERIFIED, docker-library README):* the 18 image keeps its data in `/var/lib/postgresql/18/docker`, so the volume is mounted at `/var/lib/postgresql`, not `/var/lib/postgresql/data`. Mounted at the old path the data does not persist. The volume was renamed `kvit-postgres-18-data`, so the old 17 data is not mixed in.
  - *Rejected:* staying on 17 (cheap now, harder later; Neon's later-upgrade path was never checked).

**Render deploy driven by CI, shown on GitHub (Filip, 2026-10-01)**
- Filip wants to see on GitHub when the API is live, not by email. A separate watcher workflow could deadlock with Render's "After CI Checks Pass" (Render waits for all checks on the commit; the docs are silent about checks that appear late: NOT VERIFIED). So CI itself does the deploy: Render's **Auto-Deploy is switched to Off right before the merge**, and a last step of the `backend` job on a push to `main` runs `scripts/RenderDeploy.cs`, which asks Render to deploy that exact commit (`POST /v1/services/{id}/deploys` with `commitId`) and waits until the deploy is `live`, failing on `build_failed`, `update_failed`, `pre_deploy_failed`, `canceled`, `deactivated`, an unknown status, a 202 "queued" answer or a 20-minute timeout. It shows as a normal CI line.
- Needs a Render API key as the GitHub secret `RENDER_API_KEY` (account-wide as far as the docs say, NOT VERIFIED; only readable by workflows on `main`) and the service id as the GitHub variable `RENDER_SERVICE_ID` (`srv-...`, not secret). Both are checked first, with a clear error naming the missing one.
- *Rejected:* a separate `workflow_run` watcher (possible deadlock, can only be tested after the merge); email only (Filip: "if it really gets buggy I'll have it by mail"; that stays the fallback: set Auto-Deploy back to "After CI Checks Pass" and delete the CI step).
- *Verified:* the Render API works on the free plan (read-only calls, 2026-10-01); the script's `--watch-latest` mode ran against the real service and answered live; 9 tests against a fake Render server pass. The trigger path against the real Render and the CI steps on GitHub are NOT VERIFIED until the merge.

**How secrets are handled in Phase 5 (2026-10-01)**
- Filip keeps every secret in a private notes file outside the repository. Claude's commands read a value from that file (or from his clipboard) into a process and put it on his clipboard for him to paste into Neon, Render, Cloudflare or GitHub. Values are never printed, so they never appear in the chat; only booleans, lengths and exit codes are shown. The certificate and proxy secret for the online app were made by the repository script into a throwaway project, appended to the notes file, and the temporary store was deleted.
- *Why:* Filip found the manual copy-and-paste commands error-prone (the clipboard held the command text instead of the string). He allowed Claude to see things, but the rule "no secrets in the chat" stays.
- *Rejected:* Neon's AI-agent setup prompt and `neon mcp` (they would write a `.env` file into the repository and connect an AI tool to his Neon account).

**Welcome screen: light/dark button (Filip, 2026-10-01; design not final)**
- Filip wants visitors to see the dark mode without logging in. Rejected: making the word "Kvit" a hidden toggle (nobody finds it, and it needs a hidden button name for screen readers). Chosen direction: a small visible button on the Welcome screen next to the EN/МК switch; one tap flips light and dark and is remembered (`kvit.theme`); "Same as device" stays in Settings. Filip liked variant A (round 44 px moon/sun button left of the language pill, icon turns and fades about 0.18 s, none with reduced motion) and asked to see other designs. Mockups: `docs/design/2026-10-02-welcome-theme-button/index.html` (A, B two-icon pill, C left corner). **Filip picked A ("the one right next to EN or MK") and it is built:** `KvitThemeToggle`, one tap flips light/dark (when the saved choice is "Same as device" it reads the device setting and then saves light or dark), hidden names `common.switchToDarkMode` / `common.switchToLightMode`. Known small gap: if the phone itself switches light/dark while the screen is open and the choice is "Same as device", the icon updates on the next render, not instantly.
- Show/hide password eye button (Filip, 2026-10-01): added to this PR. Its hidden accessible names `common.showPassword` / `common.hidePassword` stay (Filip: "keep the names, I just wanted an explanation").

**The web address**
- **Stays `kvit-mk.pages.dev`** (Filip asked for a free name like `kvit.something`; researched 2026-10-01).
  - *Rejected:* is-a.dev and js.org (their terms forbid an app like Kvit); eu.org (commercial use strongly discouraged, and Google would very likely refuse it as an authorized domain); DigitalPlat us.kg (spam blocklist, suspended once); pp.ua (allowed, but publishes the owner's name and phone number and needs a manual yearly renewal).
  - A paid name (about 10 € a year, needs a card) can be attached later with no code change, only if Filip decides to pay.
- **Google sign-in on `pages.dev`: works with conditions.** Only openid, email and profile means no app verification, no warning screen and no 100-user cap. Brand verification (the "Kvit" name and logo on Google's window) needs a DNS record, impossible on `pages.dev`, so people see "kvit-mk.pages.dev". Preview addresses cannot sign in. Whether Google accepts `kvit-mk.pages.dev` as an authorized domain is NOT VERIFIED, and Filip tests it in Step 3.
- **Filip, 2026-10-01:** seeing "kvit-mk.pages.dev" in Google's window instead of "Kvit" is fine for now. Revisit a paid name only if users find it confusing or Google refuses the address.

