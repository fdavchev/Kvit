# Roadmap: Kvit

Each phase is one branch, small enough to finish and test on its own. After every phase: run the tests, tick the boxes here, update `STATUS.md`, write a report in `docs/reports/`.
Branch names follow Filip's rule `<type>/<NN>-<short-name>`, with NN = the phase number.
**Filip:** lines are things only Filip can do (accounts, secrets, commits, phone tests).

## Phase 0: Planning (no code)
- [x] Choose the idea and stack (shared expenses + budget, .NET + React)
- [x] Choose free, no-card hosting (Neon + Render + Cloudflare Pages)
- [x] Decide the first-version features and rules (see `DECISIONS.md`)
- [x] Write `docs/ARCHITECTURE.md`
- [x] Write the full feature and screen list (`docs/SCREENS.md`)
- [x] Write the data model (`docs/DATA-MODEL.md`)
- [x] Write the build roadmap (this file)
- [x] Write `docs/guides/install-tools.md` (setting up Filip's PC)
- [x] Write `docs/guides/free-hosting-setup.md` (the «from the session» values get filled in while building)
- [x] Write the project `CLAUDE.md`
- [x] Write `START-HERE-PROMPT.md` for the building session
- [x] **Filip:** answer the open questions for Release 1 (end of `DATA-MODEL.md`; all answered 2026-09-25)
- [x] **Filip:** OK the data model and this roadmap ("go", 2026-09-25)
- [x] **Filip:** create the repository: `main` pushed to `https://github.com/fdavchev/Kvit`, branch `feat/01-backend-skeleton` created (checked 2026-09-25)

---

# Release 1: "splitting works"

## Phase 1: Backend skeleton · `feat/01-backend-skeleton`
Only the projects needed now; `Kvit.Contracts` and `Kvit.Infrastructure` arrive in Phase 4, when the first request/response shapes and the database need them (final plan review: "build it one project at a time").
- [x] Repository basics: `.gitignore`, `.gitattributes`, `.editorconfig`, `global.json` (pins the .NET 10 SDK)
- [x] `Directory.Build.props` (net10.0, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`) and `Directory.Packages.props` (every NuGet version in one file)
- [x] `Kvit.slnx` with `Kvit.Api`, `Kvit.Application`, `Kvit.Domain`, `tests/Kvit.Domain.Tests`, `tests/Kvit.Api.Tests`
- [x] `Result` / `Result<T>` / `ResultCodes` (Domain), the small dispatcher and handler interfaces (Application), `BaseController` that turns a `Result` into an HTTP answer (Api), Scrutor registration in `Registers/`
- [x] `GET /health` and `GET /api/health` that never touch the database
- [x] OpenAPI + Scalar API reference page (development only) and `Kvit.Api.http`
- [x] `src/api/Dockerfile` (multi-stage, runs as a non-root user)
- [x] `.github/workflows/ci.yml`: backend job (restore, build, test)
- [x] Tests: `Result` factories, dispatcher finds the right handler, `/health` answers 200
- **Done when:** `dotnet build` has 0 warnings, `dotnet test` passes, `docker build -f src/api/Dockerfile .` succeeds and the container answers `/health`.
- **Checked 2026-09-25:** 0 warnings, 26/26 tests pass, the image builds and answers `/health` on port 10000 as a non-root user (VERIFIED by live run, see `reports/2026-09-25-phase-01-backend-skeleton.md`).
- [x] **Filip:** commit, push, open the pull request, see CI go green on GitHub, merge into `main` (PR #1 merged 2026-09-25; CI green on the branch and on `main`)

## Phase 2: Frontend skeleton · `feat/02-frontend-skeleton`
- [x] `src/web` from the official Vite `react-ts` template (it installs TypeScript 6.0 and oxlint; VERIFIED from the template's `package.json`, create-vite 9.2.1)
- [x] **Try TypeScript 7:** switch, run lint + build + test; keep it if all pass, otherwise stay on 6.0 and log why in `reports/2026-09-26-phase-02-frontend-skeleton.md` — **kept** (VERIFIED: `npm run lint`/`build`/`test` all pass on TS 7.0.2)
- [x] Styling: Tailwind CSS v4 + CSS variables as design tokens (light and dark), shadcn/ui (Base UI) set up with its files in `shared/components/ui/`, Sonner for toasts
- [x] React Router with the route table and a `<RequireAuth>` placeholder
- [x] react-i18next with `en.json` / `mk.json`, `<html lang>` switching, a plural test (21 → "one" in Macedonian)
- [x] TanStack Query provider; `core/api/apiClient.ts` + `endpoints.ts`; Vite dev proxy `/api` → the local API
- [x] Shared `KvitButton`, `KvitLoading`, `KvitError`, `KvitEmpty`; money formatting in `shared/utils` (MKD without decimals, EUR with two; the app language `mk` / `en` is passed to `Intl`)
- [x] Welcome screen (screen 1, buttons not wired yet) that pings `/api/health`
- [ ] Font check: Ѓ Ќ Ѕ Ј Љ Њ Џ render correctly — the app's own Macedonian text (Welcome, not-found) renders correctly (VERIFIED by screenshot); a raw check of the full special-letter set showed a few glyphs that looked like possible Latin-lookalike substitutions on this PC. NOT fully verified — real confirmation is Phase 5, on an actual phone.
- [x] Cloudflare Pages Function `src/web/functions/api/[[path]].ts` that forwards `/api/*` to `API_ORIGIN` (check docs) — VERIFIED live with `wrangler pages dev`: 200 with `API_ORIGIN` set, a clear 500 when it's missing
- [x] CI: frontend job (`npm ci`, lint, build, test)
- **Done when:** lint, build and Vitest pass; the Welcome screen looks right at 360 px in light and dark, in EN and MK; the health ping works through the dev proxy. **Checked 2026-09-26:** all VERIFIED (automated: lint 0 warnings, build 0 type errors, 52/52 Vitest; live: health ping 200 through the dev proxy and through `wrangler pages dev`, language switch + `<html lang>` persist across reload, button contrast 5.32:1 light / 8.91:1 dark).
- [x] **Filip:** commit, push, open the pull request, see CI go green on GitHub, merge into `main` (PR #2 merged 2026-09-26; CI green on the branch and on `main`)

## Phase 3: Money core · `feat/03-money-core`
Pure C# in `Kvit.Domain/MoneyRules/` (not `Money/`, see `reports/2026-09-29-phase-03-money-core.md`), no database. The heart of the app, tested hardest.
- [x] `Currency`, `Money(long MinorUnits, Currency Currency)`; adding two currencies is a failure
- [x] Rounding step per currency (MKD to whole denars, EUR to the cent)
- [x] The four split functions (Equal + extras, Exact, Percentage, Shares), leftover to the payer
- [x] Balances per member per currency; "everyone's kvit"
- [x] Debt simplification per currency, stable order
- [x] Tests: every example from `DATA-MODEL.md`, both currencies, rounding edge cases, "shares always add up to the total", "balances always add up to 0"
- **Done when:** `dotnet test` passes and every rule in "Money rules" has a test.
- **Checked 2026-09-29 on the branch:** `dotnet build` 0 warnings; `dotnet test` 165/165 pass (Domain 150, of which 139 are new; Api 15), after the code-review follow-up. Seeded random checks: 2,000 cases per split type and currency, 500 random groups for balances, 2,000 cases per currency for "who pays whom" (VERIFIED by automated test, see `reports/2026-09-29-phase-03-money-core.md`).
- [x] **Filip:** commit, push, open the pull request, see CI go green on GitHub, merge into `main` (PR #4 merged 2026-09-29)

## Phase 4: Database + email accounts · `feat/04-accounts`
- [x] `Kvit.Contracts` (request/response shapes) and `Kvit.Infrastructure`: `AppDbContext`, snake_case naming, Identity with `Guid` ids, the Kvit user columns. **Done in Steps 1 and 2a (VERIFIED by automated test, 403/403)**
- [x] Add each new project's `.csproj` to the `COPY` lines of `src/api/Dockerfile` (`Kvit.Infrastructure`, `Kvit.Contracts`). Without them `dotnet restore` inside the image fails, and only Render's deploy would show it (review 01-1; CI builds the image from Step 2 of the review-fix branch, so the CI run on this phase's PR catches a missing line). **Both lines done in Steps 1 and 2a (image builds, VERIFIED by live run)**
- [x] **Done in Step 2a (VERIFIED by automated test).** A test that the Scrutor scan in `Register.Application.cs` registers the first real handler: resolve it through the real `AddApplication()` and send a request through the dispatcher. Today the scan finds zero handlers and no test proves it works (review 01-2)
- [x] **Done in Step 2a (VERIFIED by automated test); side effect: an unknown path now answers 401 to an anonymous visitor (`DECISIONS.md`).** A fallback authorization policy that requires a signed-in user, with `/health` and `/api/health` explicitly marked anonymous, so a controller written without `[Authorize]` is closed, not open. It can't be built before an auth scheme exists, so it belongs here. Test: an endpoint without `[Authorize]` answers 401, and both health routes still answer 200 without a login (review 01-3)
- [x] **Done in Step 3 (VERIFIED by automated test and by live run with `wrangler pages dev`; Render and the real Cloudflare edge NOT VERIFIED until Phase 5).** Per-IP rate limiting reads the visitor address through ASP.NET's forwarded-headers middleware from the header `X-Kvit-Visitor-Ip`, which only the Cloudflare proxy can set (it carries the secret `x-kvit-proxy-secret`; the API answers 403 without it except on the health routes); a client-sent `X-Forwarded-For` does not change the counted address (tested). Render's own `X-Forwarded-For` is no longer used
- [x] **Done in Step 2b (VERIFIED by automated test; the Linux image restart check was run by the coder, not repeated by me).** Data Protection keys in Postgres, encrypted with a certificate (and a small script that makes the certificate)
- [x] **Done in Step 2a (SameSite=Lax, 90 days; VERIFIED by automated test).** Login cookie: HttpOnly, Secure, SameSite=Lax (or Strict), long-lived. This is the only CSRF protection: there is no anti-forgery token, and the API is also reachable directly on `onrender.com`. Checked by the cookie-flags test below (review 02-9)
- [x] **Done in Step 2a (VERIFIED by automated test; live curl run reported by the coder).** Own auth endpoints: register (name + email + password), log in, log out, "me". Not `MapIdentityApi`: it has no name field, no Google, and exposes password-reset/2FA endpoints we can't support without email (decided 2026-09-25)
- [x] **Done in Step 3 (VERIFIED by automated test and by live run with `wrangler pages dev`).** Rate limiting on log-in (10 a minute) and sign-up (5 per 10 minutes) per visitor address, the proxy secret gate and `X-Kvit-Visitor-Ip`; time zone and language saved from the phone (done in Step 2a, VERIFIED by automated test)
- [x] `usage_events` table + `SignedUp` event. **Table in Step 1, the event written at sign-up in Step 2a (VERIFIED by automated test)**
- [x] First migration; `compose.yaml` with a local Postgres 17 (no password, only reachable from this PC); connection string through `dotnet user-secrets` (Step 1: VERIFIED by live run; Filip sets his own secret with `guides/phase-04-local-setup.md`)
- [x] `Kvit.Api.Tests` with Testcontainers (a real Postgres in Docker): register → me → log out, wrong password, rate limit, cookie flags. **Done: Testcontainers setup and database tests (Step 1); register → me → log out, wrong password, lock ladder, cookie flags (Step 2a); rate limits, proxy gate, address grouping (Step 3). 452 tests, VERIFIED by automated test**
- [x] **Step 3b (done 2026-10-01; VERIFIED by automated test, 493/493; the script run on a throwaway database was REPORTED by the coder):** password reset by hand for Filip (`scripts/ResetPassword.cs`: temporary password shown once, only its hash saved), `must_change_password` enforced by the API, `POST /api/auth/change-password`, three new error codes. Filip never sees anyone's real password
- [x] **Done 2026-10-01 (VERIFIED by automated test, 437/437; browser runs REPORTED by the tester; Filip's own click-through REPORTED by Filip; a real phone is Phase 5):** Frontend: Sign up, Log in (with "Forgot your password? Ask Filip to reset it."), `RequireAuth`, forced change-password screen, Settings (language, theme, change password, log out). Report: `reports/2026-10-01-phase-04-step-4-frontend.md`
- [x] **Filip:** commit, push, open the pull request, see CI go green on GitHub, merge into `main` (PR #6 merged 2026-10-01; CI green on the branch and on `main`)
- **Done when:** all tests pass; sign up → log out → log in works in the browser at phone width. **Reached 2026-10-01** (502 backend and 437 frontend tests pass; Filip tested it himself and said everything looks fine). Report: `reports/2026-10-01-phase-04-accounts.md`
- **Filip:** Docker Desktop running; run the one-time local setup commands (a guide is written in this phase).

## Phase 5: First deploy · `chore/05-first-deploy`
Put the skeleton online early, so the hosting traps show up before there are features.
- [x] **Done 2026-10-01 (VERIFIED by live run: onrender.com answers 403 without the secret and 200 on /health; a fake log-in with the secret reaches Neon and answers 401; the site answers /api/health 200).** Production settings: make the proxy secret once and set it as `Proxy__SharedSecret` on Render and `API_PROXY_SECRET` on Cloudflare (same value, `guides/free-hosting-setup.md` B2/B3). Render refuses to start without it, on purpose. Phase 4 Step 3 already built the forwarded-headers reading (no `KnownIPNetworks` work is left)
- [x] **Done 2026-10-01 (VERIFIED by live run: 12 log-ins with different made-up headers, #11 got 429; a made-up `CF-Connecting-IP` is refused by Cloudflare with error 1000; the sign-up limit gave 429 on the 6th; Render passes the proxy's headers through).** On the deployed site, prove the visitor-address chain on the real Cloudflare: send a request with a made-up `X-Forwarded-For` and `CF-Connecting-IP` and check that the API sees your own address, not the made-up one (locally this was only checked with `wrangler pages dev`, review 02-1). Also prove that Render passes `x-kvit-proxy-secret` and `x-kvit-visitor-ip` through untouched (a request without the secret must get 403 from `onrender.com`, one through Cloudflare must work)
- [x] **Built in Step 1 (VERIFIED by automated test; the bundle on a throwaway local database REPORTED by the coder); first migration applied to Neon from Filip's PC 2026-10-01 (VERIFIED by live run); the CI migration step on `main` and the Render deploy step are VERIFIED by live run (run 9a4018f, checked 2026-10-03: every step green; Render deploy `trigger=api`, commit 9a4018f, live in 48 s, `autoDeploy=no`).** How migrations reach Neon (proposal: a CI step runs an EF migration bundle with the **direct** connection string from a GitHub secret, before Render deploys; alternative: the app migrates at startup over the direct connection)
- [x] **Done 2026-10-01 (the Neon string lives only in Render and the GitHub secret `NEON_DIRECT_CONNECTION_STRING`; VERIFIED the secret exists).** **The online database always has a password.** Neon's connection string contains one; it goes only into Render's secret settings and the GitHub secret used for migrations, never into the repository. The passwordless local database (`compose.yaml`, `trust`) is for this PC only. If `compose.yaml` is ever used anywhere except this PC, or the port is ever opened beyond `127.0.0.1`, first switch it to a password kept in an ignored `.env` file or in `dotnet user-secrets` (Filip, Step 1 review)
- [x] **Done 2026-10-01: project created, tables built (3 migrations); `/health` does not wake Neon and the first request after a sleep succeeded in 4.05 s, so no `EnableRetryOnFailure` (VERIFIED by live run, one sample; see the Step 3 report).** Neon: create the project on Postgres 18 (same major version as `compose.yaml` and the tests, which moved from 17 to 18 on 2026-10-01; 508/508 tests pass on 18). Decide on `EnableRetryOnFailure` for the first request after Neon's idle suspend, once its real wake-up time is seen. If adopted, `IUnitOfWork` must run a whole unit of work inside the execution strategy, because EF's retry refuses user-started transactions (Step 1 code review)
- [x] **Done 2026-10-01: made and put into Render; copies in Filip's private notes. No GitHub secret for the certificate is needed any more (the design-time factory).** Make a separate certificate for the online app with `dotnet run scripts/NewDataProtectionCertificate.cs -- --project <a throwaway project>` (never reuse the local one) and put its two values into Render (`DataProtection__CertificateBase64`, `DataProtection__CertificatePassword`) and into the GitHub secrets for the CI migration step. `dotnet ef` and the migration bundle run `Program.cs`, so the CI step needs both DataProtection settings and the connection string (Step 2b). Option to decide then: an `IDesignTimeDbContextFactory` in `Kvit.Infrastructure`, so `dotnet ef` and the bundle need only the connection string and the certificate never reaches the migration job (Step 2b code review)
- [x] **Done 2026-10-01.** Fill in every «from the session» value in `guides/free-hosting-setup.md`
- [x] **README link and real screenshots done 2026-10-01 (4 screenshots); Filip sets GitHub About → Website after the merge.** Once the site is live: a **"Try it: kvit-mk.pages.dev"** link at the very top of `README.md`, so visitors see it's a real, working app (Filip, 2026-09-25). Also put the address in GitHub's **About → Website** field
- [x] **Done 2026-10-01 (steps written in `guides/reset-a-password.md`; the run against Neon itself is NOT VERIFIED).** How to run `scripts/ResetPassword.cs` against the online database: its connection string through the environment variable `ConnectionStrings__KvitDatabase` on Filip's PC (the variable wins over local secrets); write the exact steps into `guides/reset-a-password.md` once Neon exists
- [x] **Done 2026-10-01 (REPORTED by Filip: "everything checks out", no per-item notes).** On a real phone, check what a desktop screenshot cannot show (`reports/2026-10-01-phase-04-step-4-frontend.md`): status-bar colour in light, dark and the Settings theme choice, no page zoom on tapping an input, the right keyboard for email, padding at the notch and home bar, no grey tap flash, 44 px tap areas, the 'Same as device' default on the phone
- [x] **Added scope, approved by Filip 2026-10-01 (VERIFIED by automated test, 467/467 frontend; the look in a real browser is REPORTED by Filip once he has seen it):** a show/hide eye button on every password box, and a light/dark button on the Welcome screen next to EN/МК (design A)
- [x] **Added scope, done 2026-10-01:** local database, tests and Neon on Postgres 18 (508/508 backend tests pass on 18)
- **Filip:** hosting guide Part B1–B3 and B5; test on his iPhone/Android: sign up, close the browser, come back, still logged in.
- [x] **Filip:** commit, push, open the pull request, see CI go green on GitHub, add the Render secret and variable, set Render Auto-Deploy to Off, merge into `main` (PR #7 merged 2026-10-01; first CI run on `main` applies migrations and deploys, check its Actions run)
- **Done when:** `https://kvit-mk.pages.dev` loads; sign-up works through the proxy; after a manual Render redeploy the user is **still logged in** (proves the database key storage); `/health` never wakes Neon. **Reached 2026-10-01** (site loads, sign-up through the proxy and still logged in after a Render deploy are REPORTED by Filip and seen in the Render log; `/health` never wakes Neon is VERIFIED by timing). Report: `reports/2026-10-01-phase-05-first-deploy.md`.

## Phase 6: Google sign-in + privacy page · `feat/06-google-sign-in`
- [x] **Done 2026-10-03 (VERIFIED by automated test: backend 593/593, frontend 778/778; the real Google sign-in is REPORTED by Filip, 2026-10-03).** Google Identity Services button; the API checks the ID token (`aud`, `iss`, `exp`, verified email), `sub` stored in `user_logins`
- [x] **Done (VERIFIED by automated test).** No automatic merge with a password account (error code + pop-up); first Google sign-in asks the name once; Google accounts can add a password in Settings
- [x] **Done (headers VERIFIED by live run on the Cloudflare preview; CSP in a real browser see the report).** Security headers that allow Google's script and popup (CSP, COOP)
- [x] **Done (VERIFIED by automated test).** Privacy page (screen 19), EN + MK, plus Privacy links on Welcome and Settings
- [x] **Done (VERIFIED by automated test).** Tests with a fake token checker (Google itself can't be called from tests)
- [x] **Added scope, approved by Filip:** `scripts/start-local.ps1` (start, `-Check`, `-Stop`), the "or" divider, no Welcome pitch line, Log out always at the bottom, Google button drawn once per language and always white. Report: `reports/2026-10-03-phase-06-google-sign-in.md`
- [x] **Filip:** commit, push, open the pull request, see CI go green, merge into `main` (PR #8 merged 2026-10-03)
- **Filip, after the merge:** hosting guide Part B4 / `guides/phase-06-google.md` Part 2 (Branding links, Publish app → In production), then the phone test. The phone test and "In production" are REPORTED done by Filip (2026-10-03).

## Phase 7: Groups, members, invite links · `feat/07-groups`
- [x] **Done 2026-10-05 (VERIFIED by automated test, backend 1454, frontend 1940; every new screen VERIFIED by live run in headless Chrome, see the report).** Create a group, list groups, group screen shell, rename / emoji / currency, soft delete + Undo
- [x] **Done (VERIFIED by automated test).** Plain-name members; invite link (make, share, reset)
- [x] **Done (VERIFIED by automated test).** Join screen incl. the "Are you one of these?" claim; undo claim; rate limit on joining
- [x] **Done (VERIFIED by automated test).** Leave, remove, make owner (the zero-balance checks are wired in Phase 9, once balances exist)
- [x] **Done (VERIFIED by automated test).** Activity events and usage events for all of the above
- [x] **Done (VERIFIED by automated test).** Screens 6, 7 (Group part), 8 (shell), 14, 15, 16
- [x] **Filip:** commit, push, open the pull request, see CI go green, merge into `main` (CI applies the `Groups` migration on Neon). Report: `reports/2026-10-05-phase-07-groups.md`; guide: `guides/phase-07-groups.md`

## Phase 8: Expenses · `feat/08-expenses`
- [x] **Done 2026-10-05 (VERIFIED by automated test).** A way to check that every API error code the frontend can receive has a translation key (review 02-5): a Vitest test reads `ResultCodes.cs` (`DECISIONS.md`, Phase 8)
- [x] **Done 2026-10-05 (VERIFIED by automated test; the real NBRM call NOT VERIFIED).** Built-in categories (seed); exchange rate table + seed + lazy NBRM refresh
- [x] **Done 2026-10-06 (VERIFIED by automated test).** Add, edit, delete, Undo and Recently deleted (5 days); all four split types through the Phase 3 functions; `client_request_id` duplicate protection (also for two requests at the same moment, 2026-10-09)
- [x] **Done 2026-10-06 (VERIFIED by automated test).** Change history in the activity feed (old → new): backend, the detail History and the Activity tab
- [x] **Done 2026-10-06 (VERIFIED by automated test).** One bill (group + expense in one step): backend and the form with circles; the group name follows the app language (2026-10-09)
- [x] **Done 2026-10-06 (VERIFIED by automated test).** Screens 7 (One bill part), 8 (Expenses and Activity tabs), 9 with its sheets, 10, 13
- [x] **Real-browser check done 2026-10-07 (VERIFIED by live run; findings fixed in Step 6b).**
- [x] **Code review done 2026-10-09 (VERIFIED by automated test: backend 2143, frontend 4402):** 4 findings fixed, the rest in BACKLOG. Filip's hand check of the finished screens is REPORTED, not independently verified. Report: `reports/2026-10-09-phase-08-expenses.md`; guide: `guides/phase-08-expenses.md`
- [x] **Filip:** commit, push, open the pull request, see CI go green, merge into `main` (CI applies the two new migrations on Neon)

## Phase 8b: Saved data shows instantly · `feat/08b-instant-saved-data`
Moved forward from Phase 11 because the Render wake-up (about 22 s) made the app look stuck.
- [x] **Done 2026-10-09 (VERIFIED by automated test, frontend 4481).** TanStack Query cache saved to IndexedDB (24 hours, version string), `me` included, one-person rule (log-out, 401 or another person wipes the saved copy), screens show at once while `/api/me` is re-checked once per app open
- [x] **Done 2026-10-09 (VERIFIED by automated test).** "Updating…" note (EN + MK) above the bottom tab bar, a 3-second limit on reading the saved copy, one "server may be waking up" message for 502/503/504 and no answer
- [x] **Merged (PR #11).** Filip: real-browser and phone check, commit, push, merge

## Phase 9: Balances + settle up · `feat/09-settle-up`
- [x] **Done 2026-10-13 (VERIFIED by automated test).** Balances per currency; "who pays whom"
- [x] **Done 2026-10-13 (VERIFIED by automated test).** `Balances.Calculate` gets every member the group has ever had, including removed and left members
- [x] **Done 2026-10-13 (VERIFIED by automated test and live run).** Settlements: record, confirm, reject, cancel, delete, Undo; owner acts for plain names
- [x] **Done 2026-10-13 (VERIFIED by automated test).** Zero-balance rules for leave / remove / delete group
- [x] **Done 2026-10-13 (VERIFIED by live run, local, 360 px).** Screens 11, 12; "Needs you" items for pending payments
- [x] **Code review done 2026-10-13:** 3 fixes made, the rest in BACKLOG. Report: `reports/2026-10-13-phase-09-settle-up.md`; guide: `guides/phase-09-settle-up.md`
- [ ] **Filip:** commit, push, open the pull request, see CI go green, merge into `main` (CI applies the `Settlements` migration on Neon)

## Phase 10: Finishing groups · `feat/10-finish-groups`
- [x] **Done 2026-10-15 (VERIFIED by automated test and live run).** Closing: start, confirm, object (with reason), cancel on a money or member change by the owner, lazy 24-hour finish (a group needs at least one expense to close)
- [x] **Done 2026-10-15 (VERIFIED by automated test and live run).** One bill finishes by itself; owner unlocks a Finished group
- [x] **Done 2026-10-15 (VERIFIED by automated test and live run).** Read-only and "frozen while closing" checks in every command
- [x] **Done 2026-10-15 (VERIFIED by automated test and live run; the last toast fix by automated test only).** Screen 17: the bottom bar in five states, badge, Object sheet, read-only screens. Report: `reports/2026-10-15-phase-10-finish-groups.md`; guide: `guides/phase-10-finish-groups.md`
- [x] **Code review done 2026-10-15:** no correctness bugs; cleanup items in BACKLOG
- [ ] **Filip:** commit, push, see CI go green, merge into `main` (CI applies the `ClosingResponses` migration on Neon)

## Phase 11: Dashboard + Release 1 · `feat/11-dashboard`
- [x] **Done 2026-10-15 (VERIFIED by automated test and live run).** Dashboard layout C, "Needs you" across groups answered on Home, recent activity, "+" list with "Add to" the group last added to
- [x] Saved data shown instantly: moved to Phase 8b
- [x] **Done 2026-10-15 (VERIFIED by automated test).** `Active` usage event (once per user per day)
- [x] **Done 2026-10-15 (VERIFIED by automated test and live run).** Closing bar: already floating and staying until answered; toasts now sit above it
- [x] **Done 2026-10-15 (VERIFIED by automated test and live run).** Phase 10 leftovers from BACKLOG (except the awaited refetches and a few small items, which are in BACKLOG)
- [x] **Done 2026-10-15 (VERIFIED by live run, 764 snapshots).** Full pass at 360 px in EN and MK, light and dark; every text translated (the Macedonian drafts are NOT read by Filip)
- [x] **Code review done 2026-10-16:** four findings fixed (Home with only Finished groups, stale payment card, slash words, one closing rule) and the CI test `Active_SecondCall…` made independent of run order (VERIFIED by automated test: backend 3485, frontend 7119). Report: `reports/2026-10-16-phase-11-dashboard.md`; guide: `guides/phase-11-dashboard.md`
- [ ] **Filip:** commit, push, see CI go green, merge into `main` (no migration)
- [ ] Deploy; **Filip** and friends use it on something real
- **Done when:** all tests pass and Filip says Release 1 works for him.

## Phase 12: Animations + touch-ups · `feat/12-polish`
- [x] The glass-look redesign on every popup, and the animation pass with Filip's 2026-10-09 picks (BACKLOG)
- [x] Dark mode surface colours: solid colours B instead of `color-mix`, tan selected pill (2026-10-16)
- [x] The desktop layout (round-4 hybrid, phone unchanged) and the other small look items from BACKLOG (the "Updating…" label on group pages and the Split sheet pill are left in BACKLOG)
- Moved here from Phase 11 on 2026-10-15 (Filip: "it's big").

## Phase 12b: Join without an account · `feat/12b-guest-join`
Release 1 accepted 2026-10-16. Filip put this BACKLOG "must do" before Phase 13. Decisions: `DECISIONS.md`.
- [x] Step 0: mockup (join screen B picked) and docs
- [x] **Done 2026-10-16 (VERIFIED by automated test).** Step 1: backend, guest accounts and `POST /api/invites/join-as-guest` (migration `GuestAccounts`)
- [x] **Done (VERIFIED by automated test).** Step 2: backend, guest limits and owner's "Let in again" (migration `GuestLetInAgain`)
- [x] **Done (VERIFIED by automated test).** Step 3: backend, upgrade a guest to an account, email or Google (migration `GuestUpgradedUsageEvent`)
- [x] **Done (VERIFIED by automated test).** Step 4: frontend, the join screen
- [x] **Done (VERIFIED by automated test).** Step 5: frontend, Guest badge, limits, Settings card, Log out warning
- [x] **Done (VERIFIED by automated test).** Step 6: frontend, upgrade screens
- [x] **Done 2026-10-19 (VERIFIED by live run, headless, local stack; 3 findings fixed).** Step 7: real-browser pass. Backend 3710, frontend 7870 tests. Report: `reports/2026-10-19-phase-12b-guest-join.md`; guide: `guides/phase-12b-guest-join.md`
- [x] **Code review done 2026-10-19 (VERIFIED by automated test: backend 3717, frontend 7874):** 2 findings fixed (every member-row save runs once more after a same-moment change; a guest row shows only Let in again), the rest in BACKLOG (l) and (g)
- [ ] **Filip:** commit, push, PR, CI green, merge (CI applies the 3 migrations on Neon)

---

# Release 2: "budget" (phases get detailed when Release 1 is done)

## Phase 13: Personal spending + custom categories · `feat/13-personal-spending`
- [x] Step 0: mockup and Filip's picks (2026-10-19, `DECISIONS.md`)
- [x] **Done 2026-10-19 (VERIFIED by automated test: backend 3897, frontend 7909).** Step 1: backend, custom categories (migration `PersonalCategories`)
- [x] **Done 2026-10-19 (VERIFIED by automated test: backend 4072, frontend 7958).** Step 2: backend, personal entries (migration `PersonalEntries`)
- [x] **Done 2026-10-19 (VERIFIED by automated test: frontend 8194; no screen yet, not seen in a browser).** Step 3: frontend, categories (grid with "+ New", new-category sheet, Edit my categories)
- [x] **Done 2026-10-19 (VERIFIED by automated test: frontend 8391; not seen in a browser).** Step 4: frontend, add, edit and delete spending (entry point: Home "+")
- [x] **Done 2026-10-19 (VERIFIED by automated test: frontend 8527; not seen in a browser).** Step 5: frontend, Budget tab (`/budget`, month strip, totals, entries; not for guests)
- [x] **Done 2026-10-20 (VERIFIED by live run, headless Chromium, local stack; 9 bugs found in the passes, 8 fixed, Bug 4 in BACKLOG; frontend 8581).** Step 6: desktop layout and the 360 px browser pass
- [x] **Done 2026-10-20 (VERIFIED by automated test: backend 4169, frontend 8631; the review fixes are not yet seen in a browser).** Step 7: code review (8 findings fixed), report `reports/2026-10-20-phase-13-personal-spending.md`, guide `guides/phase-13-personal-spending.md`, first-push texts `guides/phase-13-pr-text.md`
- [ ] **Filip:** commit, push, PR, CI green, merge (CI applies the 2 migrations on Neon)

## Phase 14: Income, once and repeating · `feat/14-income`
- [x] Step 0: mockups (three rounds) and Filip's picks (2026-10-20, `DECISIONS.md`)
- [x] **Done 2026-10-20 (VERIFIED by automated test: backend 4354, frontend 8651; no UI yet).** Step 1: backend, income once and the `Income` migration (also created the empty `recurring_incomes` table)
- [x] **Done 2026-10-20 (VERIFIED by automated test: backend 4608, frontend 8749; no UI yet).** Step 2: backend, repeating income (create, list, edit, stop, restart; migration `RecurringIncomeRules`)
- [x] **Done 2026-10-20 (VERIFIED by automated test: backend 4854, frontend 8749; no UI yet).** Step 3: backend, lazy generation (catch-up endpoint, schedule, local today, `nextDate`; migration `GeneratedIncomeEntries`)
- [x] **Done 2026-10-20 (VERIFIED by automated test: frontend 8955; not seen in a browser).** Step 4: frontend, "Spending or Income?" sheet and add income once
- [x] **Done 2026-10-20 (VERIFIED by automated test: frontend 9503; not seen in a browser).** Step 5: frontend, repeating income (Repeats and Ends chips, the row and the list)
- [x] **Done 2026-10-21 (VERIFIED by automated test: frontend 9553; not seen in a browser).** Step 6: frontend, Budget tab with the Left card
- [x] **Done 2026-10-21 (VERIFIED by live run, headless Chromium, local stack; 6 bugs found, all fixed; Bug 6 fix VERIFIED by automated test only; backend 4860, frontend 9600).** Step 7: desktop layout and the 360 px browser pass (`reports/2026-10-21-phase-14-browser-pass.md`)
- [x] **Done 2026-10-24 (VERIFIED by automated test: backend 4892, frontend 9717; the fixes VERIFIED by live run in the tester's re-checks).** Step 8: two code reviews (20 findings fixed), report `reports/2026-10-24-phase-14-income.md`, guide `guides/phase-14-income.md`, first-push texts `guides/phase-14-pr-text.md` (migrations `RecurringIncomeRules`, `GeneratedIncomeEntries`, `PersonalEntryOriginRules` came with it)
- [x] **Filip:** commit, push, PR, CI green, merge (PR #18 merged)

## Phase 15: Budget share, budget currency, limits, monthly summary · `feat/15-budget`
One big phase (Filip, 2026-10-24). Prompt: `guides/phase-15-budget-prompt.md`; first-push texts: `guides/phase-15-pr-text.md`.
- [ ] Step 0: mockup (budget currency, shares on the tab, switch place, limits, summary) and Filip's picks
- [x] **Done 2026-10-24 (VERIFIED by automated test: backend 4958 of 4959 pass, frontend 9717; the one failure is the old `ExpenseSharesTableTests.Table_HasExactlyTheFourColumns`, which still lists four columns and needs `counts_in_budget` added).** Step 1: backend, budget share (per-group switch, remembered flag, claimed names; migration `BudgetShare`)
- [x] **Done 2026-10-24 (VERIFIED by automated test: backend 5087 of 5087 pass, frontend 9721).** Step 2: backend, budget currency (optional converted total setting, the conversion rule, `convertedTotals` in the Budget month answer; migration `BudgetTotalCurrency`)
- [x] **Done 2026-10-24 (VERIFIED by automated test: backend 5284 of 5284 pass, frontend 9728).** Step 3: backend, limits with history (set and remove per category and for the whole month, same-moment saves and removals answer 204, `limits` in the Budget month answer; migration `BudgetLimits`) and the any-letter-case budget currency fix
- [ ] Step 4: backend, monthly summary
- [ ] Step 5: frontend, Settings (budget currency, per-group switch)
- [ ] Step 6: frontend, group shares on the Budget tab
- [ ] Step 7: frontend, limits
- [ ] Step 8: frontend, monthly summary
- [ ] Step 9: desktop layout and the 360 px browser pass
- [ ] Step 10: code review, report, guide
- [ ] **Filip:** commit, push, PR, CI green, merge (CI applies the migrations on Neon)

## Later phases
- Phase 16: possible duplicates, group totals per category, group spending plan

# Release 3: "polish"
- Phase 17: the outbox (queue while the server wakes, "All up to date ✓", failed items with Retry / Discard)
- Phase 18: admin statistics page
