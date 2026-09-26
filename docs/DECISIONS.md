# Decisions

## 2026-09-26: Phase 2 built and verified
The coder's session ended before reporting back (Filip's usage limit), so its work was picked up cold from its git worktree in a fresh session, reviewed file-by-file against the plan, then verified rather than trusted.
- **TypeScript 7 (7.0.2) kept**, confirmed by running `npm run lint`/`build`/`test` on the actual code: 0 lint warnings, 0 type errors, 52/52 Vitest tests passing.
- **VS Code needs an extra extension for TS 7 to work correctly:** "TypeScript (Native Preview)" (`TypeScriptTeam.native-preview`, Microsoft). Installing it isn't enough — it must be turned on via Command Palette → "TypeScript Native Preview: Enable (Experimental)", otherwise VS Code silently keeps using its own older bundled TypeScript. Added to `install-tools.md`. Not confirmed: whether it can be pinned to the project's exact `node_modules/typescript` 7.0.2 rather than whatever version ships with the extension (NOT VERIFIED, the extension/project only just merged into the main TypeScript repo).
- **Cloudflare proxy verified live** with `wrangler pages dev`: `/api/health` → 200 with `API_ORIGIN` set; a missing/invalid `API_ORIGIN` answers a clean 500 rather than guessing or crashing.
- **Button contrast calculated from the actual CSS tokens:** 5.32:1 light mode, 8.91:1 dark mode — both clear the 4.5:1 WCAG AA bar the plan required.
- **Macedonian font check is inconclusive on this PC.** The app's real UI text (Welcome screen, not-found screen) renders correctly. A raw on-page check of the full special-letter set (Ѓ Ќ Ѕ Ј Љ Њ Џ and lowercase) showed a few glyphs that looked like possible Latin-lookalike substitutions — not confirmed as a real font gap versus normal Cyrillic/Latin glyph-sharing. As the plan already noted, this check is PC-fonts-only regardless; the real confirmation is on an actual phone in Phase 5.
- No other deviations from the plan found during review.

## 2026-09-25: Phase 2 plan (frontend skeleton), Filip's answers and technical decisions
Planned in a fresh plan-mode session (Filip's rule: Phase 2 is planned in plan mode, Opus, then built by the coder). Full plan kept at the time in `C:\Users\Davchev\.claude\plans\read-status-and-roadmap-reactive-fox.md`; the coder run itself hadn't finished as of this entry, so results and any deviations are logged separately once it reports back.

**Filip's answers:**
- **Language:** Kvit follows the phone (a Macedonian phone → MK, anything else → EN). A small **EN · МК** switch sits at the top of the Welcome screen, remembered on that phone.
- **Amounts:**
  | | Denars | Euros |
  |---|---|---|
  | English | `1,200 MKD` | `€15.50` · `€1,234.50` |
  | Macedonian | `1.200 ден.` | `€15,50` · `€1.234,50` |

  The € always goes in front. Macedonian uses the dot for thousands and the comma before cents (matching the denar style already used); English uses the comma for thousands and the dot before cents. `Intl.NumberFormat`'s own currency formatting doesn't produce these exact shapes (Macedonian default puts € after the number; English default writes `MKD 1,200` not `1,200 MKD`), so `formatMoney` uses `Intl.NumberFormat` for the number part only and places the currency mark/code by hand.
- **Main colour: sunny orange.** The "you owe" red (from Phase 9) must stay clearly different from it.
- **Tagline in English:** "Квит сме." with "We're even." in smaller text under it. Macedonian keeps only "Квит сме.".

**Technical decisions (Claude's, per "ask product questions only"):**
- TypeScript 7 is tried last, after everything is green on the template's TS 6.0 — kept only if lint/build/tests all pass on the real code, otherwise reverted with the reason logged.
- The `@/` import alias uses `paths` without `baseUrl` (TS 7 removed `baseUrl`; shadcn's Vite guide still shows it, but it's skipped).
- Dark mode is CSS-only (`prefers-color-scheme`), no manual toggle and no `.dark` class, since Kvit always follows the phone.
- Filled buttons need ≥4.5:1 text contrast in both light and dark orange, calculated not eyeballed.
- Font is the phone's own `system-ui` stack (no web font download; Macedonian letters covered on Android/iPhone/Windows).
- Toasts: Sonner directly with `theme="system"`, wrapped in `KvitToaster` — dropping shadcn's default `next-themes` dependency, which Kvit doesn't need.
- Language detection is a small hand-written function (saved choice, else `mk*` → mk, else en), not `i18next-browser-languagedetector`.
- Money formatting works only in integer minor units; a non-whole-denar MKD value or a non-safe-integer amount throws, naming the value, since it can only be a bug.
- Cloudflare proxy (`functions/api/[[path]].ts`): its own `functions/tsconfig.json` with `@cloudflare/workers-types` (kept out of the app's own tsconfig, which needs DOM types instead); `public/_routes.json` restricts functions to `/api/*` so static pages don't spend the free function-call quota; a missing/invalid `API_ORIGIN` answers 500 with a clear message; upstream redirects are passed back to the browser, not followed.
- Node pinned to 24 via `src/web/.nvmrc` and `package.json` `engines` (React Router 8.4 needs Node ≥ 22.22; Cloudflare's build image defaults to 22.16, so the hosting guide's Cloudflare env vars need `NODE_VERSION=24` added once Phase 5 sets up hosting).
- The Welcome screen's Google / email / "I already have an account" buttons show a "Coming soon" toast until Phases 4 and 6 wire them up.
- `RequireAuth` is a placeholder that always redirects to `/welcome` until Phase 4 adds the real check.
- The not-found screen (SCREENS.md screen 20) is built now, ahead of its listed phase, because the route table needs a catch-all route regardless.
- oxlint is configured to fail on warnings, matching the backend's `TreatWarningsAsErrors`. Vite's dev server uses `strictPort: true` on 5173, since Phase 6's Google sign-in only allow-lists that exact port.
- Tests sit next to the code they test (e.g. `formatMoney.test.ts`); the Cloudflare proxy's tests go in `src/web/test/functions/`.

## 2026-09-25: Phase 1 technical decisions
**Projects**
- **Phase 1 has five projects:** `Kvit.Api`, `Kvit.Application`, `Kvit.Domain`, `tests/Kvit.Domain.Tests` and `tests/Kvit.Api.Tests`. `Kvit.Contracts` and `Kvit.Infrastructure` arrive in Phase 4, when the first request shape and the database need them. This replaces "Phase 1 creates Api, Application, Domain and Contracts" in the plan entry below.
- **Central package management:** every NuGet version lives in `Directory.Packages.props`, and the project files only name the package. `Directory.Build.props` sets net10.0, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` once for all projects.

**Result**
- **The folder is `Results/` (namespace `Kvit.Domain.Results`), not `Result/`** as ARCHITECTURE sketches it. A namespace called `Result` holding a class called `Result` confuses C# name lookup.
- **`StatusCode` is `System.Net.HttpStatusCode`.** It's part of .NET itself, so the Domain project doesn't depend on ASP.NET, and the code reads `HttpStatusCode.NotFound` instead of a bare 404.
- **A success has an empty `Error` and `ErrorCode`.** Callers check `IsSuccess` first.
- **Reading `Value` of a failed `Result<T>` throws**, with the error code in the message. Rejected: returning `default` (a silent 0 or null).
- **Added `Failure<T>`, `Unauthorized<T>`, `Forbid<T>` and `NotFound<T>`** next to the factories ARCHITECTURE lists, so a query handler can return "not found" as a `Result<T>`. Rejected: an automatic conversion from `Result` to `Result<T>` (hidden magic).
- **`ResultCodes` is empty.** No error code is used yet, and codes are a contract with the frontend, so none are invented ahead of time.
- **File names for the generic twins:** `ResultOfT.cs` and `ICommandHandlerOfTResult.cs`, so each type still has its own file.

**Dispatcher (replaces MediatR)**
- **Three methods:** `Send<TCommand>` (returns `Result`), `Send<TCommand, TResult>` and `Query<TQuery, TResult>` (both return `Result<TResult>`). The controller names the types; nothing is guessed at run time.
- **Exactly one handler per request.** No handler, or two or more, throws an `InvalidOperationException` naming the request type. Rejected: "the last registered wins", which silently picks one.
- **Rejected: marker interfaces (`IQuery<TResult>`) with reflection** to work out the result type, as MediatR does. It saves typing the type names but hides how the call is routed.
- **Registration:** the dispatcher is scoped (one per request). Handlers are found by Scrutor (`AssignableToAny` of the three handler interfaces, `AsImplementedInterfaces`, transient) in `Registers/Register.Application.cs`. The domain-service and repository scans are added when those folders exist.

**API**
- **Error body: ProblemDetails** (the web standard for error answers) with an extra `errorCode` field, and `detail` holding the English message. Example: `{"status":404,"title":"Not Found","detail":"Group was not found.","errorCode":"GROUP_NOT_FOUND",...}`. ASP.NET's automatic 400 answer for invalid input (from `[ApiController]`) already uses this shape, so the frontend reads one format. Unexpected server errors (500) and unknown routes don't yet: they answer with an empty body until a later phase adds `AddProblemDetails()`. Rejected: a custom `{ error, errorCode }` body (a second format next to the built-in one).
- **Success:** `Result` → 204 No Content, `Result<T>` → 200 with the value as JSON.
- **Health:** the built-in health checks with no checks registered, so nothing ever touches the database. Mapped at `/health` (Render) and `/api/health` (the website, through the Cloudflare proxy). Both answer `Healthy` with 200.
- **OpenAPI + Scalar only in Development:** the document at `/openapi/v1.json`, the Scalar page at `/scalar`. Tests check both are 200 in Development and 404 in Production.
- **Local run over plain HTTP** at `http://localhost:5018` (one launch profile, no HTTPS redirect). Render handles HTTPS in front of the container, so the app never needs its own certificate. Rejected: the template's HTTPS profile (needs a trusted dev certificate for no gain). Forwarded headers come in Phase 5.

**Code style enforced by the build** (VERIFIED with a throwaway file that broke each rule, then deleted)
- Errors: `var` (IDE0008), file-scoped namespaces (IDE0160), a namespace that doesn't match its folder (IDE0130).
- Warnings, which `TreatWarningsAsErrors` turns into errors: primary constructors (IDE0290), collection expressions (IDE0028, IDE0300–IDE0305), switch expressions (IDE0066).
- **Not checkable by the compiler:** "no comments" and "file name = class name". These stay review rules.

**SDK and tests**
- **`global.json`: SDK 10.0.400 with `rollForward: latestFeature`.** Any installed .NET 10 SDK from 10.0.400 up is accepted, never .NET 11. Rejected: `latestPatch`, which would break the build after an update to a 10.0.5xx SDK until someone edits the file. The Docker `sdk:10.0` image currently ships 10.0.401 (VERIFIED).
- **Tests use xUnit v3 (`xunit.v3` 4.0.1) on Microsoft Testing Platform**, the newer test runner. `global.json` has `"test": { "runner": "Microsoft.Testing.Platform" }`, which .NET 10 needs to run it. The older VSTest packages (`xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`) aren't used. `dotnet test Kvit.slnx` works as written (VERIFIED on SDK 10.0.400).
- **Dispatcher and BaseController tests live in `Kvit.Api.Tests`.** They go through the API's own DI registration, so no separate Application test project is needed yet.

**Docker and CI**
- **Dockerfile:** build on `sdk:10.0`, run on `aspnet:10.0` (Ubuntu 24.04). The build context is the repository root. It copies the build settings and the three project files first, so the package download is cached between builds, then only `src/api`. It runs as the image's built-in non-root user (`USER $APP_UID`, `app`, uid 1654). There's no port in the file: the image listens on 8080 by default and Render's `ASPNETCORE_HTTP_PORTS=10000` overrides it (VERIFIED live on both ports). No build arguments, so no secret can end up in the image.
- **Image tags aren't pinned to a digest,** so security patches arrive with the next build. Rejected for now: the smaller "chiseled" images (no shell, harder for a beginner to debug); worth a look later.
- **The Dockerfile has no comments** (the "no comments" rule); its reasons live here.
- **`.dockerignore`** keeps tests, docs, the website, `.git` and secret-like files out of the image build.
- **`.gitattributes`:** `* text=auto`, so Git keeps using Filip's Windows line-ending setting, with LF forced for `Dockerfile` and `*.sh` (Linux needs it).
- **CI (`.github/workflows/ci.yml`):** a `backend` job on every push and pull request: checkout (`actions/checkout@v7`), .NET from `global.json` (`actions/setup-dotnet@v6`), restore, Release build, test. The job's token is read-only. Both action versions were the latest releases on 2026-09-25 (VERIFIED on GitHub); `actionlint` found 0 errors.

## 2026-09-25: Web addresses, README and licence
- **Addresses:** the website will be **`kvit-mk.pages.dev`** and the API **`kvit-mk-api.onrender.com`**.
  - `kvit.pages.dev` and `kvit-app.pages.dev` are taken (both answered with a live site). `kvit-api.onrender.com` gave no answer within 70 seconds, while unused Render names answer at once with "no server", so it's treated as taken. (VERIFIED by live requests on 2026-09-25.)
  - `kvit-mk`, `kvitsme` and `mojkvit` on pages.dev, and `kvit-mk-api` on Render, answered like unused names. Whether they're truly free only shows when the project is created (NOT VERIFIED).
  - The hosting guide and roadmap now use the new names. Older reports keep the old ones as history.
- **README:** added at the root for GitHub visitors, so GitHub's auto-generated README isn't needed.
- **Licence: none for now ("All rights reserved").** Kvit may get paid features later (see "Speed details"), and a licence like MIT can't be taken back for code already published under it. A public repository with no licence can still be read by anyone (employers included), but nobody may reuse the code. MIT or AGPL can be added any time later.

## 2026-09-25: Filip's answers to the plan questions
- **MKD is split to whole denars** (1,000 / 3 → 333, 333, 334). EUR is split to the cent.
- **Exchange rate:** NBRM only, with no override in settings. A per-expense override comes in Release 2. This settles the clash between the two 2026-09-24 entries.
- **A confirmed payment entered by mistake** can be deleted by whoever recorded it, or by the owner, with Undo.
- **Own login endpoints, not `MapIdentityApi`:** a technical choice, so Claude decided it (Filip didn't need to weigh in). Reason: `MapIdentityApi` has no name field at sign-up, no Google sign-in, and exposes password-reset/2FA endpoints that can't work without email.
- **Styling: Tailwind CSS v4 + shadcn/ui (Base UI variant) + Sonner for toasts.** Filip asked for a search for something better than plain Tailwind (research by the `researcher` subagent, 2026-09-25).
  - **Why:** Tailwind alone has no bottom sheets, toasts or tabs, and hand-building accessible ones (focus handling, closing with the back gesture) is hard for a beginner. shadcn/ui copies small, readable component files into the project, so there's no big library API to learn and only the used components are shipped. Tailwind's CSS variables cover the design tokens and dark mode.
  - **Facts:** shadcn 4.21.0, `@base-ui/react` 1.8.0 (React 17–19), Sonner 2.0.8 (React 18–19), Tailwind 4.3.3, all MIT (VERIFIED by registry lookup). shadcn made Base UI its default for new projects in July 2026 (VERIFIED by the researcher's live web check of the shadcn changelog).
  - **Rejected:** Mantine (heavier on cheap phones), CSS Modules + a headless library (the whole visual system by hand), Panda CSS / UnoCSS / Park UI (maturity not checked).
  - **Traps:** pick the Base UI option in `shadcn init`, and follow the Tailwind **v4** setup (`@theme` in CSS), not old v3 guides with `tailwind.config.js`. The copied components go in `shared/components/ui/` and are wrapped by the `Kvit*` components, so features never import them directly.
- **Leftover denar when the payer isn't sharing** (e.g. you pay a 1,000 MKD taxi for Ana, Marko and Bojan → 333 + 333 + 334): the first of them in the group's member list pays the extra one.
- **Anyone in the group can add name-only members** ("Grandma"). Removing people stays owner-only.
- **How questions get asked from now on:** only questions about how the app behaves, each with a concrete example in plain words. Technical choices are decided by Claude and logged here.

## 2026-09-25: Building session, plan (see `DATA-MODEL.md`, `SCREENS.md`, `ROADMAP.md`)
**Facts checked today (VERIFIED by registry lookup / local run on 2026-09-25):**
- Filip's PC has .NET SDK 10.0.400, Node 24.19, npm 11.17, Docker 29.7 and git 2.47. `dotnet new sln` makes a `.slnx` file.
- The Vite `react-ts` template (create-vite 9.2.1) installs **TypeScript ~6.0.2** and lints with **oxlint**, not ESLint. So the "TS 7 breaks ESLint" trap doesn't apply as written.
- `openapi-typescript` 7.13.0 declares it needs TypeScript 5 (`peerDependencies: typescript ^5.x`). That clashes with TS 6 and TS 7 alike, so type generation needs a workaround either way (decided in the phase that adds it).
- Every NuGet package ARCHITECTURE names has a .NET 10 version: Npgsql EF 10.0.3, EFCore.NamingConventions 10.0.1, Scrutor 7.0.0, Scalar.AspNetCore 2.17.10, Testcontainers.PostgreSql 4.15.0, Google.Apis.Auth 1.76.0, DataProtection.EntityFrameworkCore 10.0.12.

**Decided (technical, following ARCHITECTURE):**
- **TypeScript 7 (Filip asked, 2026-09-25):** try it in Phase 2. Keep it if lint, build and tests pass; otherwise stay on the template's 6.0 and log why here. The main reason TS 7 was risky (typescript-eslint supports TS < 6.1 only) is gone, because the template uses oxlint. Still unchecked: VS Code editor support for TS 7 and `tsc -b` on 7.0 (NOT VERIFIED).
- **Lint is oxlint** (the template's default) instead of ESLint. `install-tools.md` now lists the Oxc extension.
- **Repository layout:** `Kvit.slnx` at the root, backend in `src/api/`, backend tests in `tests/`, frontend in `src/web/`, Dockerfile at `src/api/Dockerfile`, Pages Function at `src/web/functions/api/[[path]].ts`.
- **Backend projects arrive one at a time:** Phase 1 creates Api, Application, Domain and Contracts; Infrastructure comes with the database in Phase 4.
- **Ids** are `uuid` v7. **Currency and statuses** are stored as text. **Balances are never stored**, always added up from rows.
- **"Equal + extras" is the Equal split type with extras**, not a fifth type. The four split types stay as decided.
- **Usage events live in their own table**, apart from the group activity feed, because they're platform counts that never hold amounts or names.
- **`client_request_id` columns exist from Release 1** on expenses, settlements and personal entries, so the Release 3 outbox needs no table changes.
- **Closing: who must confirm** = every current member with an account except the owner. With nobody left to ask (owner + plain names), the group finishes at once.
- **Deploy early:** a skeleton goes online in Phase 5, before the features, so the hosting traps (proxy cookie, keys in the database) are tested first.

**Proposals at the time (see "Filip's answers to the plan questions" above for what was settled):**
- Tailwind CSS v4 + CSS variables for styling (ARCHITECTURE left it open).
- Own auth endpoints instead of `MapIdentityApi`.
- Migrations reach Neon through a CI step with the direct connection string (decided in Phase 5).
- The Scalar API reference page only in development.
- Business rules: the five open questions at the end of `DATA-MODEL.md`.

**Clashes found and how they were resolved:**
- `reports/2026-09-25-final-plan-review.md` (B1) puts Google sign-in and change history in Release 3; the later answers in this file put both in **Release 1**. This file wins (it's the source of truth for features).
- "Change history in the first version if it stays small, otherwise postpone" (2026-09-24) vs. "Change history is part of the activity feed" (2026-09-25): the later one wins, so it's in Release 1.
- "EUR converted with a saved rate of about 61.5, editable in settings and per expense" (2026-09-24) vs. "refreshed from NBRM every 8 hours" (2026-09-24): not resolved yet, it's open question 3 in `DATA-MODEL.md`.
- No clash found between `ARCHITECTURE.md` and this file.

## 2026-09-25: Business-review answers (see `reports/2026-09-25-business-logic-review.md`)
- **"Equal + extras" split (Release 1).** Filip's hotel example: 5 people share a hotel, and Marko takes the better room for 600 more. You enter the total, it splits equally, and you tap a person to add an extra (+600 for Marko). The rest is shared equally among everyone. It's the fast version of the "exact amounts" split, made for the most common uneven case. It's also the default: a new expense is split equally among all members.
- **Totals per category in a group** (Hotel, Food, Transport, Other…): Release 2, together with the budget.
- **Group spending plan (Release 2).** For example "600 EUR planned, 4 people = 150 each", with a progress bar and a warning near the limit.
- **Shared pot / kitty** (everyone pays in up front): backlog.
- **Budget counts shares only.** Your budget counts only **your share** of expenses. It never counts who paid, and it never counts paybacks ("I paid Ana 1,200" / "Marko paid me"). Paybacks are not income either.
- **Possible duplicates (Release 2).** When your share of a group expense lands in your budget and you already logged a personal entry in the same category with a similar amount within a few days, Kvit asks: *"Is this the same as 'Dinner – 1,200' you added on Friday? · Same, merge · No, it's new"*.
- **Plain-name members:** the group owner confirms "I paid Grandma" on her behalf.
- **Budget currency:** each user picks one in settings, MKD by default. Other currencies are converted with the rate saved on each expense.
- **Owner's account deleted:** ownership passes to the member who joined earliest (only matters for groups that are still open).
- **"Who pays whom"** shows one line under it: *"Simplified: fewer payments, same totals."*
- **Finishing a group (Release 1). Updated with Filip's answer, 2026-09-25:**
  1. When every balance in every currency is zero and nothing is pending, the owner sees **"Everyone's kvit – close the group?"**, with the checklist of settlements. The owner taps **Confirm**.
  2. The group is now **"Closing"**. Every other member sees **Confirm** or **Object**.
     - **Object** can include an optional short reason. The text field costs almost nothing, so it's included.
     - An objection cancels the closing, and the group goes back to normal so people can sort it out in person.
  3. The group becomes **Finished** when **everyone has confirmed**, or **automatically 24 hours after the owner confirmed** if nobody objected.
  4. **While it's "Closing", the group is frozen for members.** They can only Confirm or Object, and they can't add or change anything. **Only the owner** can still fix an expense (e.g. after an objection), and any change **cancels the closing**, because the balances are no longer zero. The owner starts the closing again once it's sorted. While the group is open, the normal rule applies: whoever added an expense, plus the owner. (Filip, 2026-09-25.)
  5. **Finished = read-only for everyone.** All members can still open it and see every expense and settlement, but nobody can change anything.
     - Unlocking it again for changes is owner-only, and it's recorded in the activity feed.
     - **Rejected:** only the platform admin can unlock. Filip isn't in other people's groups.
  - **How the 24 hours work without a scheduler:** the group stores when the owner confirmed. Whenever anyone opens the group or the dashboard after 24 hours with no objection, it counts as Finished. The same lazy trick as the exchange rate, so it's free.
- **Group type when creating (Release 1).** Filip's idea:
  - **"One bill"**: a single dinner or taxi. Enter the amount, pick the people, done. There's no group to manage. It uses the same group underneath, marked as one-bill, and it **finishes automatically** once everyone is kvit, with no closing step.
  - **"Group"** (a trip, a household, a friend circle): many expenses over time. Closing works as described above.
- **No deadline.** It's a hobby project with no deadline (Filip, 2026-09-25). The releases are there to keep each step finishable, not to hit dates.

## 2026-09-25: Final review answers (see `reports/2026-09-25-final-plan-review.md`)
**Releases**
- **Release 1, "splitting works":**
  - email + password and **Google sign-in** (moved up from Release 3 so there's no email-address clash and no locked-out users),
  - groups, invite links, plain-name members,
  - expenses with 4 split types,
  - MKD/EUR balances and "who pays whom",
  - settle up with confirmation,
  - activity feed with change history, dashboard,
  - EN/MK, dark mode,
  - deployed, with saved data shown instantly.
  - Usage events are recorded from day one.
- **Release 2, "budget":** categories and limits, income (once and repeating), the budget share, the monthly summary.
- **Release 3, "polish":** the outbox and the admin statistics page.

**Rules**
- **Exchange rate:** each EUR expense keeps the exchange rate from the day it was saved.
- **Removing and deleting:** the owner can't remove a member or delete a group while balances aren't zero. The owner has to hand over ownership before leaving.
- **"I paid":** the payer can cancel it while it's still pending.
- **Wrong claims:** the owner can undo a wrong name claim.
- **Invite links:** long and random, and the owner can reset them. Anyone with the link joins straight away (no approval).
- **Joining through an invite link:**
  - **Already logged in:** the join screen shows *"Filip invited you to Greece trip"*, the group's emoji, and the members already in it, with one **Join** button below. The name comes from the account, so no name field is shown (Filip, 2026-09-25).
    - **Only if the group has unclaimed plain names:** after tapping Join, one optional question appears: *"Are you one of these? Marko · Grandma · No, I'm new"*. This is for when the owner already added "Marko" as a plain name and recorded expenses for him. Claiming joins those expenses to Marko's account instead of creating a second "Marko".
  - **No account yet:** the link opens a short sign-up first: Google (one tap) or name + email + password. It then comes **back to the same join screen**. The invite is remembered through the sign-up.
  - **Already a member:** opening the link again just opens the group. There's no second join, because the login remembers him.
  - **Joining with no account at all** (the device remembers you) stays in the backlog.
- **Google vs password accounts:** Google sign-in never merges automatically into an existing password account with the same email. The user is told to log in with their password.
- **Forgot password (until email exists):** Filip resets it by hand. Google is shown as the main sign-in button.
- **Time zones:**
  - Everything is stored in UTC.
  - Each user has a time zone that is **detected automatically from the phone**, with an optional manual choice in settings. "Today", months and repeating income use the user's own time zone, so it works on a trip to America too.
  - An expense's date is a plain calendar date (no time), so the date doesn't change between time zones.
  - **Rejected:** fixed Skopje time (wrong abroad).
- **Categories:** group expenses use the built-in categories. Custom categories are for personal spending only.
- **Budget share switch:** per group, "count my shares in my budget", on by default. It applies from the moment you change it; past months stay as they were. Each expense remembers whether it was counted.
- **Deleting:** it marks an item as deleted, and Undo restores it.
- **Change history** is part of the activity feed, showing old → new values.
- **MKD amounts** are stored in deni (×100), the same as euro cents. The denar is shown without decimals.
- **Security basics:** limits on login and join attempts, HTTPS only.
- **Privacy page:** a simple page describing the data Kvit stores. Release 1, because Google may require it (check).
- **Account deletion:** backlog (see the cleanup item there).

## 2026-09-25: Architecture
- **Written in:** `docs/ARCHITECTURE.md`.
- **Backend:**
  - Clean Architecture + CQRS.
  - Rich entities with `Create`/`Update` factories that return `Result`.
  - Domain services, repositories and `IUnitOfWork`.
  - Query handlers with `AsNoTracking` + projection.
  - Thin controllers, `ResultCodes` and Scrutor registration.
  - `TreatWarningsAsErrors`, explicit types and block namespaces.
  - **No MediatR.** A small hand-written dispatcher instead, because of the MediatR licence change.
  - **One Application project** holding both Queries and Commands.
  - **Postgres with EF migrations and snake_case.**
  - **Group rules are checked in domain services,** on top of platform permissions.
- **Frontend:**
  - Vertical feature slices.
  - Shared components that know no feature.
  - All text goes through translations.
  - Mobile first.
  - TanStack Query + IndexedDB persistence, and types generated from OpenAPI.
- **Working rules:** propose then wait, never guess, search before writing, no comments, format only changed lines, and the commit shortcuts.
- **Tests are required.**
- **Open for the building session:** Tailwind vs another styling approach, and the exact library APIs marked "check docs".

## 2026-09-24: Name is Kvit
- **Chosen:** "Kvit", from "квит сме" ("we're even"). The start screen tagline is "Квит сме." The folder, repository and web addresses use `kvit`.
- **Rejected:** "Kvit-sme". Hyphens are awkward in web addresses and repository names.

## 2026-09-24: Login technical fixes (from the stack research)
- **Login keys go in the database.** The .NET Data Protection keys are stored in Postgres (`PersistKeysToDbContext`), because Render wipes its files on every restart, which would log everyone out. They must also be encrypted.
- **One site for the browser.** A free Cloudflare Pages Function forwards `/api/*` to Render, so the browser only talks to one site. This lets a secure login cookie work on iPhones (Safari blocks cookies between different sites) and removes the need for CORS.
- **Rejected:** bearer tokens kept in `localStorage`. They're weaker if the site ever has a script-injection bug. Kept as a fallback only.

## 2026-09-24: Exchange rate refreshes every 8 hours
- The saved MKD/EUR rate is refreshed from the National Bank (NBRM) free service, which needs no key.
- **How:** "lazy" refreshing. When the API is used and the saved rate is more than 8 hours old, it fetches a new one. No scheduler is needed, which suits a server that sleeps.
- **Cost:** none.
- **Note:** NBRM publishes about once per working day, so most refreshes will return the same number.
- **If NBRM can't be reached,** the app keeps the last saved rate and shows its date. It doesn't hide the error.

## 2026-09-24: Speed details
- **Adding an expense:** one screen with the amount field focused and the number keypad open. The other fields are **pre-filled with the most common choice** and shown as tappable chips: paid by me, split equally among everyone, today, the group's currency, and a category guessed or left empty. Tap a chip only to change it. The title is optional.
- **After login** the app opens on the **dashboard**, with a big "+" button that adds to the last-used group.
- **Personal spending:** tap a category (or "+ new category" right there), type the amount, save.
- **Undo instead of "Are you sure?"** pop-ups.
- **No limits and no ads** in the app for now. Paid features may come later if it becomes popular. If that happens, the free-hosting terms must be checked again for commercial use.

## 2026-09-24: Accounts and login
- **First version: everyone who uses the app has an account.** People like grandma are plain-name members that others add expenses for.
- **Guest joining without an account is postponed** (see the backlog). It needs rules for who can claim a name, fixing wrong claims and so on, which is too much for the first version.
- **Ways to log in:**
  - Email + password.
  - **Sign in with Google**, using Google Identity Services: the ID token is posted to the API and checked there. It's free with no card; only the openid/email/profile scopes; publishing status "In production". Source: `reports/2026-09-24-google-apple-login-and-sharing.md`.
- **Rejected:** Sign in with Apple. It needs a $99-a-year Apple developer account.

## 2026-09-24: No keep-awake pings for now
- Render's rules neither allow nor forbid them, so we don't ping. The app wakes the server as soon as the page opens. Saved data is shown while it wakes (see "Hiding the server wake-up"). This will be looked at again later, e.g. by asking Render support or moving to a cheap paid plan if real users arrive.

## 2026-09-25: Hiding the server wake-up (first version)
**The problem:** Render's free server sleeps after 15 minutes without visits and takes about 1 minute to wake. The first person after a quiet period would have to wait.

**The solution:**
1. **Show saved data instantly.** The app loads right away from Cloudflare, which never sleeps. It shows the last balances and lists it saved in the browser, with a small "updating…" note until the server answers.
2. **A background "outbox".** Everything the user does while the server is waking goes into a queue saved in the browser (IndexedDB). Those items appear on screen immediately, marked "not synced yet". When the server is up, the queue is sent in order and the user sees an in-app message: **"All up to date ✓"**. That was Filip's idea.
   - **Every queued action gets a unique ID made on the phone,** so if it's sent twice, the server saves it only once.
   - **The queue survives closing the tab.** Anything still waiting is sent the next time Kvit is opened.
   - **What can be queued in the first version:** new expenses, new personal spending or income, and **"Marko paid me"** records. The receiver's word counts immediately and the payer has no reason to dispute it, so it's safe to queue.
   - **What waits for the server:** **"I paid"** records (they create a pending request the receiver has to confirm, so they go straight to the server), and all editing and deleting. This keeps conflicts out of the first version.
   - **If the server rejects an item** (for example the user was removed from the group in the meantime), it is **not dropped silently**. The user sees "1 change couldn't be saved" with the reason and can retry or discard it.
   - **The message is an in-app notification,** not a phone push notification. Push notifications come later with the PWA.
- **Why:** this keeps the "seconds, not minutes" goal on a free server that sleeps.
- **Rejected:** keep-awake pings (Render's rules are unclear) and a paid plan (not free).

## 2026-09-24: Admin statistics page
- There's a **platform admin** role, for Filip only. It's separate from group owners.
- **The admin page shows counts for a date range you choose,** grouped by day, week, month or year:
  - new sign-ups (split into email vs Google),
  - active users (logged in or used the app),
  - people who joined groups through invite links,
  - groups created,
  - expenses added,
  - settlements confirmed.
- **Privacy:** the admin page only shows totals. It never shows anyone's personal budget, expense details or amounts.
- **How:** a small activity-events table (user, event type, time). "Active" is counted at most once per user per day.

## 2026-09-24: Public GitHub repository (Filip agreed 2026-09-25)
- **Public:** employers can see the code, GitHub Actions minutes are unlimited, and GitHub's secret protection is free.
- **Rule:** no passwords, keys or connection strings ever go into the code. They live in the Render, Cloudflare and GitHub secret settings.
- **Can switch to private later** if Kvit ever becomes a paid product.
- **Rejected:** private. It gets only 2,000 free Actions minutes a month and employers can't see it.

## 2026-09-24: Dashboard layout decided while building
- The app opens on the dashboard. Filip decides the exact layout when that screen gets built.

## 2026-09-25: Who can edit and delete (confirmed)
- **Group settings** (rename, remove members, delete the group): the owner only.
- **An expense:** whoever added it, plus the owner.
- **Personal spending and income:** only that user.
- **Rejected:** owner-only editing of expenses. The owner would have to fix everyone else's typos, which is slow.
- Clashes (two people changing the same thing) are avoided in the first version because editing and deleting need the server to be awake. The activity feed shows who changed what.

## 2026-09-25: Pictures (first version = no uploads)
- **Users:** a coloured circle with their initials. Google users get their Google profile picture automatically. It comes free with the "profile" scope, so no storage is needed.
- **Groups:** an emoji of the user's choice (🏖️ ✈️ 🏠 🍕…) on a coloured background.
- **Uploading your own pictures comes later** (see the backlog).
- **Why:** it costs nothing, needs no file storage and takes no time to set up.

## 2026-09-24: Settle-up rule
- If the **payer** records "I paid Ana 1,200 MKD", it stays **pending** until **Ana confirms** it. Ana can also reject it.
- If the **receiver** records "Marko paid me 1,200 MKD", it counts **immediately**. The receiver is the one who could lose money, so their word is enough.
- The money itself moves outside the app (cash or a bank transfer). The app only keeps the record.

## 2026-09-24: Stack is .NET + React (full-stack)
- **Chosen:** C# ASP.NET Core Web API + EF Core + PostgreSQL, React frontend, Docker, GitHub Actions.
- **Why:** Filip's DocuMind (Python/AI) and Book Scanner (PWA frontend) projects don't cover a typical business backend. Full-stack shows both sides, and if he likes .NET and React he can grow the project later.
- **Rejected:**
  - Flutter + DocuMind mobile, because app stores cost money and AI hosting is slow on free servers.
  - Java Spring + Kafka, because it's heavy and hosted Kafka isn't free.

## 2026-09-24: Everything free, and no bank card
- **Rule:** no paid tiers, no trials that expire, and no service that asks for a card at sign-up.
- **Rejected:** Oracle Cloud Always Free. It asks for a card at sign-up and can take back idle servers after 7 days.
- **Rejected for the database:** Render free Postgres, because it's deleted after about 30 days.

## 2026-09-24: App is shared expenses + personal budget
- **Chosen:** a Splitwise-style shared expense splitter with a personal budget. First users are Filip, his family and close friends. Local businesses might come later.
- **Why:**
  - It's the best fit for a free server that sleeps, because it needs no live updates or jobs every minute.
  - Filip would use it daily.
  - The money logic ("who pays whom" in the fewest payments, exact rounding, currencies) gives real backend problems to talk about in interviews.
- **Rejected:** pitch/court booking, barber booking, tutoring booking, weekly game organizer, roommate app, gym tracker, event tickets and shift scheduler. They're saved in `C:\Users\Davchev\Projects\Ideas For Later\IDEAS.md`. The booking apps in particular need an always-on server.
- **Not needed:** automatic bank sync. It's no longer free anyway.

## 2026-09-24: Scope is a small MVP first
- **Chosen:** a small version that can be finished in a few weeks. Filip decides afterwards whether to continue.

## 2026-09-24: First version features, languages, currencies
- **Features:**
  - Email + password login (ASP.NET Identity), plus Sign in with Google (added later the same day, see "Accounts and login").
  - Groups with invite links.
  - Expenses split equally, by exact amounts, by percentages or by shares (percentages and shares were added later the same day, see "How the app works").
  - Balances and "who pays whom" in the fewest payments.
  - Settle up.
  - Personal budget with categories, monthly limits and a monthly summary.
  - Phone-friendly layout.
- **Languages:** English and Macedonian, with a switch in the app.
- **Currencies:** MKD and EUR from the start. MKD is for everyday use and EUR for trips abroad.
- **Postponed:** forgot password (needs email). The other later features are listed in `BACKLOG.md`.

## 2026-09-24: Frontend and money-handling rules
- **Frontend:** React + TypeScript, built with Vite. TypeScript is what job ads ask for and it catches mistakes early.
- **Money:** always stored as whole minor units (denars / euro cents) in integers, never as floating-point numbers.

## 2026-09-24: How the app works (answers to the planning questions)
**Groups**
- Members without an account can be added as a plain name, e.g. "Grandma". When that person signs up and joins through the invite link, **they** pick "that's me" from the list of unclaimed names. Someone else can't claim it for them.
- Roles: a group owner (removes people, deletes the group) and normal members.
- You can't leave a group while you owe money or are owed money.

**Expenses**
- One payer per expense. Several payers are on the "to be done" list.
- Split types: equal, exact amounts, percentages, shares.
- An expense can be edited or deleted by whoever added it and by the group owner.
- An expense has a title, amount, currency, date, category, payer, split and an optional note. No photos.
- Every group has an activity feed.
- Change history ("Filip changed 1,200 → 1,500"): in the first version if it stays small, otherwise it's the first thing to postpone.

**Settling up needs two sides**
- A settlement starts as **pending**. It only changes balances once the **person receiving the money confirms** it. This covers the case where Marko says he paid but didn't.
- The receiver can also reject it.

**Currencies**
- Balances are kept separately per currency (MKD and EUR are never mixed).
- Each group has a default currency, and any expense can use the other one.
- For the personal budget, EUR is converted using a saved rate of about 61.5, editable in settings and per expense.

**Budget**
- Your share of group expenses counts toward your budget automatically, in the expense's category.
- Income is optional. It can be added once or set to repeat (e.g. 30,000 MKD every month on the 1st, or weekly).
- Categories come as a ready-made list with icons and colours, and you can add your own.
- The personal budget is always private.
- Leftover money at the end of the month is handled later (see the backlog).

**Look and feel**
- Dark mode follows the phone's setting.
- The main screen is a dashboard: what you owe and are owed, this month's budget and recent activity.
- The cold start is covered by saved data plus the outbox (see "Hiding the server wake-up"). A full "Waking up the server…" screen only appears when there's no saved data yet, e.g. on the very first visit on a new device.

## 2026-09-24: Speed and simplicity above everything
- Filip's rule: setting up and using the app must take seconds. Nobody should spend 5 minutes paying someone back or 15 minutes setting up.
- Every screen is judged by how few taps and how few typed fields it needs.

## 2026-09-24: Cost: free now, cheap later if it's really used
- It runs free for now. If real people start using it heavily, moving to a cheap paid server must be easy. This is why everything runs in Docker and nothing is tied to one provider.

## 2026-09-24: Architecture document
- Done on 2026-09-25: `docs/ARCHITECTURE.md` (see "Architecture" at the top).

## 2026-09-24: Database is Neon free
- **Chosen:** Neon free PostgreSQL. No card, no expiry, and it wakes up on its own after sleeping.
- **Rejected:** Supabase free. Its database pauses after about a week without use and has to be restored by hand. Its built-in login would also replace the .NET login work this project is meant to show. Its free file storage (1 GB) stays an option if receipt photos are ever wanted.
