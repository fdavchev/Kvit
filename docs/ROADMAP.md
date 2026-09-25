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
- [ ] **Filip:** commit, push, open the pull request, see CI go green on GitHub, merge into `main`

## Phase 2: Frontend skeleton · `feat/02-frontend-skeleton`
- [ ] `src/web` from the official Vite `react-ts` template (it installs TypeScript 6.0 and oxlint; VERIFIED from the template's `package.json`, create-vite 9.2.1)
- [ ] **Try TypeScript 7:** switch, run lint + build + test; keep it if all pass, otherwise stay on 6.0 and log why in `DECISIONS.md`
- [ ] Styling: Tailwind CSS v4 + CSS variables as design tokens (light and dark), shadcn/ui (Base UI) set up with its files in `shared/components/ui/`, Sonner for toasts
- [ ] React Router with the route table and a `<RequireAuth>` placeholder
- [ ] react-i18next with `en.json` / `mk.json`, `<html lang>` switching, a plural test (21 → "one" in Macedonian)
- [ ] TanStack Query provider; `core/api/apiClient.ts` + `endpoints.ts`; Vite dev proxy `/api` → the local API
- [ ] Shared `KvitButton`, `KvitLoading`, `KvitError`, `KvitEmpty`; money formatting in `shared/utils` (MKD without decimals, EUR with two; `mk-MK` / `en`)
- [ ] Welcome screen (screen 1, buttons not wired yet) that pings `/api/health`
- [ ] Font check: Ѓ Ќ Ѕ Ј Љ Њ Џ render correctly
- [ ] Cloudflare Pages Function `src/web/functions/api/[[path]].ts` that forwards `/api/*` to `API_ORIGIN` (check docs)
- [ ] CI: frontend job (`npm ci`, lint, build, test)
- **Done when:** lint, build and Vitest pass; the Welcome screen looks right at 360 px in light and dark, in EN and MK; the health ping works through the dev proxy.

## Phase 3: Money core · `feat/03-money-core`
Pure C# in `Kvit.Domain/Money/`, no database. The heart of the app, tested hardest.
- [ ] `Currency`, `Money(long MinorUnits, Currency Currency)`; adding two currencies is a failure
- [ ] Rounding step per currency (MKD to whole denars, EUR to the cent)
- [ ] The four split functions (Equal + extras, Exact, Percentage, Shares), leftover to the payer
- [ ] Balances per member per currency; "everyone's kvit"
- [ ] Debt simplification per currency, stable order
- [ ] Tests: every example from `DATA-MODEL.md`, both currencies, rounding edge cases, "shares always add up to the total", "balances always add up to 0"
- **Done when:** `dotnet test` passes and every rule in "Money rules" has a test.

## Phase 4: Database + email accounts · `feat/04-accounts`
- [ ] `Kvit.Contracts` (request/response shapes) and `Kvit.Infrastructure`: `AppDbContext`, snake_case naming, Identity with `Guid` ids, the Kvit user columns
- [ ] Data Protection keys in Postgres, encrypted with a certificate (and a small script that makes the certificate)
- [ ] Login cookie: HttpOnly, Secure, SameSite=Lax, long-lived
- [ ] Own auth endpoints: register (name + email + password), log in, log out, "me". Not `MapIdentityApi`: it has no name field, no Google, and exposes password-reset/2FA endpoints we can't support without email (decided 2026-09-25)
- [ ] Rate limiting on log-in and sign-up; time zone and language saved from the phone
- [ ] `usage_events` table + `SignedUp` event
- [ ] First migration; `compose.yaml` with a local Postgres 17 (no password, only reachable from this PC); connection string through `dotnet user-secrets`
- [ ] `Kvit.Api.Tests` with Testcontainers (a real Postgres in Docker): register → me → log out, wrong password, rate limit, cookie flags
- [ ] Frontend: Sign up, Log in, `RequireAuth`, Settings (language, log out)
- **Done when:** all tests pass; sign up → log out → log in works in the browser at phone width.
- **Filip:** Docker Desktop running; run the one-time local setup commands (a guide is written in this phase).

## Phase 5: First deploy · `chore/05-first-deploy`
Put the skeleton online early, so the hosting traps show up before there are features.
- [ ] Forwarded headers for Render (`KnownIPNetworks`), production settings
- [ ] How migrations reach Neon (proposal: a CI step runs an EF migration bundle with the **direct** connection string from a GitHub secret, before Render deploys; alternative: the app migrates at startup over the direct connection)
- [ ] Fill in every «from the session» value in `guides/free-hosting-setup.md`
- [ ] Once the site is live: a **"Try it: kvit-mk.pages.dev"** link at the very top of `README.md`, so visitors see it's a real, working app (Filip, 2026-09-25). Also put the address in GitHub's **About → Website** field
- **Filip:** hosting guide Part B1–B3 and B5; test on his iPhone/Android: sign up, close the browser, come back, still logged in.
- **Done when:** `https://kvit-mk.pages.dev` loads; sign-up works through the proxy; after a manual Render redeploy the user is **still logged in** (proves the database key storage); `/health` never wakes Neon.

## Phase 6: Google sign-in + privacy page · `feat/06-google-sign-in`
- [ ] Google Identity Services button; the API checks the ID token (`aud`, `iss`, `exp`), `sub` stored in `user_logins`
- [ ] No automatic merge with a password account (error code + message)
- [ ] Security headers that allow Google's script and popup (CSP, COOP)
- [ ] Privacy page (screen 19), EN + MK
- [ ] Tests with a fake token checker (Google itself can't be called from tests)
- **Filip:** hosting guide Part B4 (Google), then try it on the phone.

## Phase 7: Groups, members, invite links · `feat/07-groups`
- [ ] Create a group, list groups, group screen shell, rename / emoji / currency, soft delete + Undo
- [ ] Plain-name members; invite link (make, share, reset)
- [ ] Join screen incl. the "Are you one of these?" claim; undo claim; rate limit on joining
- [ ] Leave, remove, make owner (the zero-balance checks are wired in Phase 9, once balances exist)
- [ ] Activity events and usage events for all of the above
- [ ] Screens 6, 7 (Group part), 8 (shell), 14, 15, 16

## Phase 8: Expenses · `feat/08-expenses`
- [ ] Built-in categories (seed); exchange rate table + seed + lazy NBRM refresh
- [ ] Add, edit, delete, Undo; all four split types through the Phase 3 functions; `client_request_id` duplicate protection
- [ ] Change history in the activity feed (old → new)
- [ ] One bill (group + expense in one step)
- [ ] Screens 7 (One bill part), 8 (Expenses tab), 9 with its sheets, 10, 13

## Phase 9: Balances + settle up · `feat/09-settle-up`
- [ ] Balances per currency; "who pays whom"
- [ ] Settlements: record, confirm, reject, cancel, delete; owner acts for plain names
- [ ] Zero-balance rules for leave / remove / delete group
- [ ] Screens 11, 12; "Needs you" items for pending payments

## Phase 10: Finishing groups · `feat/10-finish-groups`
- [ ] Closing: start, confirm, object (with reason), cancel on any change, lazy 24-hour finish
- [ ] One bill finishes by itself; owner unlocks a Finished group
- [ ] Read-only and "frozen while closing" checks in every command
- [ ] Screen 17 banners

## Phase 11: Dashboard + Release 1 · `feat/11-dashboard`
- [ ] Dashboard (Filip decides the layout that day), recent activity, "+" to the last-used group
- [ ] Saved data shown instantly (TanStack Query cache in IndexedDB), "Updating…" note, the full waking screen only without saved data
- [ ] `Active` usage event (once per user per day)
- [ ] Full pass at 360 px in EN and MK, light and dark; every text translated
- [ ] Deploy; **Filip** and friends use it on something real
- **Done when:** all tests pass and Filip says Release 1 works for him.

---

# Release 2: "budget" (phases get detailed when Release 1 is done)
- Phase 12: personal spending + custom categories
- Phase 13: income, once and repeating (lazy)
- Phase 14: budget share per group, budget currency, limits, monthly summary
- Phase 15: possible duplicates, group totals per category, group spending plan

# Release 3: "polish"
- Phase 16: the outbox (queue while the server wakes, "All up to date ✓", failed items with Retry / Discard)
- Phase 17: admin statistics page
