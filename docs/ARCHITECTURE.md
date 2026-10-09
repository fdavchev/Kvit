# Kvit: Architecture & Conventions

> Read this in full **before** implementing, refactoring or reviewing Kvit code.
> **What** to build lives in `DECISIONS.md`. **How** the code is shaped lives here.
> This file is a snapshot. Where the code and this file disagree, read the code, then fix whichever one is wrong.
> Items marked **(check docs)** are library details to confirm against current docs before first use. Don't guess them.

---

# PART 1: .NET BACKEND (`src/api`)

## Stack
| Concern | Choice |
|---|---|
| Runtime | .NET 10 (LTS), ASP.NET Core Web API with controllers |
| Database | PostgreSQL (Neon) through EF Core 10 + Npgsql |
| Schema | **EF Core migrations** |
| Auth | ASP.NET Core Identity + cookie (through the Cloudflare `/api` proxy) + Google ID-token endpoint |
| DI scanning | Scrutor |
| Tests | xUnit v3 on Microsoft Testing Platform + Testcontainers (real Postgres, from Phase 4) |
| API docs | Built-in OpenAPI + Scalar page (check docs) |

## Architecture: Clean Architecture + CQRS
```
Controller → dispatcher.Send(Query | Command)
          → Handler
          → Domain Service (.Execute → Result)      [commands only]
          → Repository (interface) → AppDbContext → PostgreSQL
```
- **Deliberate choices:**
  - **No MediatR.** MediatR changed its licence in 2025 (NOT VERIFIED which versions are free), so Kvit uses a small hand-written dispatcher: `ICommandHandler<TCommand>`, `IQueryHandler<TQuery, TResult>` and a `Dispatcher` that looks up the handler through DI. It's about 40 lines, free, and there's no magic.
  - **Queries and commands live in one project** (`Kvit.Application`) as two folders, instead of two projects. Fewer projects are easier for a beginner, and the separation is still clear.

## Project layout
```
src/api/
  Kvit.Api/              Web API entry: Controllers/, Registers/ (DI), Program.cs
  Kvit.Application/      Queries/<Feature>/  and  Commands/<Feature>/  (one folder per feature; the command, its handler and their DTOs sit in it)
  Kvit.Domain/           Entities/, Interfaces/ (repository interfaces), Services/<Entity>/, MoneyRules/ (not `Money/`, for the same reason; see `reports/2026-09-29-phase-03-money-core.md`), Results/ (not `Result/`: a namespace named like its class confuses C#; see `reports/2026-09-25-phase-01-backend-skeleton.md`)
  Kvit.Infrastructure/   Persistence/ (AppDbContext, Configurations/, Migrations/), Repositories/, Auth/, ExchangeRates/
  Kvit.Contracts/        Request/response DTOs shared by controllers and handlers, IUnitOfWork
tests/
  Kvit.Domain.Tests/     Money, splits, rounding, debt simplification, entity factories
  Kvit.Api.Tests/        API tests against real Postgres (Testcontainers)
```

## Handlers
- **Query handler:** read-only. Uses `AppDbContext` directly with `.AsNoTracking()` and a `.Select(...)` projection straight to the DTO, and returns `Result<TDto>` or `Result<PagedResponse<TDto>>`. The projection method is **named for the DTO it returns**, and paging (`Skip`/`Take`) happens inside it.
- **Command handler:** gets the current user, opens a transaction, calls the domain service, then commits or rolls back:
```csharp
public class CreateExpenseCommandHandler(
    ICurrentUserProvider _currentUserProvider,
    Domain.Services.Expenses.CreateExpense _createExpense,
    IUnitOfWork _unitOfWork) : ICommandHandler<CreateExpenseCommand>
{
    public async Task<Result> Handle(CreateExpenseCommand request, CancellationToken ct)
    {
        Result<ICurrentUser> user = await _currentUserProvider.GetCurrentUserAsync();
        if (!user.IsSuccess) return Result.Unauthorized(user.Error, ResultCodes.USER_NOT_FOUND);
        return await _unitOfWork.RunInTransactionAsync(() => _createExpense.Execute(user.Value, request, ct), ct);
    }
}
```
- **The transaction block lives once**, in `UnitOfWorkExtensions.RunInTransactionAsync` (`Kvit.Application/Persistence`, Phase 7 Step 3): it opens the transaction, runs the work, rolls back when the returned `Result` failed, otherwise commits (the unit of work saves the changes first). It has no try/catch, so an exception propagates and the scoped `DbContext` is disposed with its open transaction at the end of the request, which makes Postgres roll back. Every new command handler uses it. The one exception that is caught: a command that carries a `clientRequestId` (Add expense, One bill) uses `RunInTransactionOnceMoreIfClientRequestIdWasTakenAsync` beside it, which runs the transaction once more when `UnitOfWork.CommitAsync` reports `ClientRequestIdAlreadySavedException` (two identical requests at the same moment), so the normal duplicate check answers. The exceptions are handlers that must save even when the `Result` fails, such as log-in, which stores the failed-attempt count (they keep their own block on purpose).
- **Naming:** `<GetX>QueryHandler` and `<CreateX>CommandHandler`. Class names carry the entity: `CreateExpense`, `ConfirmSettlement`.
- Shapes a handler needs go in their own `<X>Dto.cs` in the same folder. Handlers don't declare nested or private classes.

## Controllers
- Inherit `BaseController` and give each controller its own `[Route("api/...")]`. **Everything is closed by default** (a fallback authorization policy requires a signed-in user); `[AllowAnonymous]` opens an endpoint on purpose (only register, log in, the two Google endpoints, log out and the health routes). An anonymous request to an unknown path answers 401, not 404 (Phase 4, Step 2a).
- **No GET endpoint ever changes anything.** The login cookie is SameSite=Lax and there is no anti-forgery token, so every change is a POST, PUT or DELETE (Phase 4).
- **Thin:** build the query or command, `await _dispatcher.Send(...)`, `return Result(result);`.
- **Temporary password:** a signed-in account with `must_change_password` is refused (403 `AUTH_MUST_CHANGE_PASSWORD`) on every endpoint except the ones marked `[AllowedWithTemporaryPassword]` (`GET /api/me`, change password; log-out is `[AllowAnonymous]`). The rule lives on the default and fallback policies (`Authorization/PasswordChangedRequirement.cs`). Put `[AllowAnonymous]` on the single actions, never on the controller class: with it on the class, ASP.NET ignores an `[Authorize]` on its actions (Step 3b).
- **Rate limits** are counted per visitor address. Log-in and change-password use the named policy `[EnableRateLimiting("log-in")]` (every request counts). Sign-up uses `[ServiceFilter<SignUpLimitFilter>]` (`RateLimiting/SignUpLimitFilter.cs`), which counts only requests that pass the cheap checks (name, email, password rule, time zone, language) so typos never lock anybody out. Both are defined from the same constants in `Registers/Register.RateLimiting.cs`, and both 429 answers come from `RateLimitedAnswer`. A new endpoint that takes a password, a code or an invite link needs one too.
- **Ids in the route:** the id that scopes the request goes in the route (`groups/{groupId}/expenses`). Only genuine filters go in `[FromQuery]`.

## Authorization
Kvit mostly asks **"is this user a member or the owner of *this* group?"**, so there are two levels:
1. **Platform role**, as permission constants in `Permissions.cs` (`{SCOPE}_{ENTITY}_{OPERATION}`). The only one for now is `ADMIN_STATS_VIEW`, for Filip only. Checked with `[AuthorizationFilter(...)]` on the controller.
2. **Group rules** are checked inside the **domain service** through repositories, and return `Result.Forbid(..., ResultCodes.GROUP_NOT_OWNER)`. Group rules are business rules, so they live in the domain and are unit-tested there. They are:
   - member / owner,
   - "who added this expense",
   - "the receiver confirms a settlement".
- **The personal budget is always scoped to the current user.** Queries filter by the current user id at the lowest layer. There's no endpoint that takes someone else's user id.

## Domain entities (rich model)
- **Construction:** a private constructor for EF. A static `Create(...) : Result<Entity>` and an instance `Update(...) : Result<Entity>` do all the validation.
- **Behaviour methods:** e.g. `settlement.Confirm(byUserId)`, `settlement.Reject(byUserId)`, `member.Claim(userId)`.
- No logic in property getters.
- **Money is a sealed record class, `Money(long MinorUnits, Currency Currency)`, made only through `Money.Create`** (a struct's empty default would skip validation). It's never a `decimal` or `double` in entities. An amount off the currency's step fails with `MONEY_NOT_ON_CURRENCY_STEP`; adding two different currencies fails with `MONEY_CURRENCY_MISMATCH`, never a conversion; negatives are allowed because balances use `Money`. Sums and products of typed values use `Int128`. Anything that can only be a bug or damaged data (a member missing from the list, shares in another currency, an impossible amount) throws `InvalidOperationException` naming the id; it is never skipped silently (Phase 3).
- **The split logic is pure functions** in `Domain/MoneyRules/` (equal, exact, percentage, shares), so it's easy to unit-test. Rounding leftovers go to the payer.

## Domain services
- They live in `Domain/Services/<Entity>/<ServiceName>.cs`, with repositories injected through the primary constructor.
- Each has a public `async Task<Result> Execute(...)` that:
  1. validates through the repositories (group membership, ownership),
  2. builds the entity through its factory,
  3. calls `repo.Add(...)` and records the activity event,
  4. returns a `Result`.
- **They never touch `DbContext` directly.**

## Persistence
- **Configuration:** `IEntityTypeConfiguration<T>` per entity, applied with `ApplyConfigurationsFromAssembly`.
- **Naming:** tables and columns in **snake_case** (Postgres convention, via `EFCore.NamingConventions`, check docs). Foreign keys use the EF default `<Entity>Id`, so EF needs no extra config. Booleans are `Is...`.
- **Repositories:** the interface is in `Domain/Interfaces` and the implementation in `Infrastructure/Repositories`. They return `Result` or `Result<T>` and hold data access only.
- **Connection string:** the key is `KvitDatabase`. The value only ever comes from environment variables or secrets, **never** from `appsettings.json` in the repository. The context reads it lazily, and a start-up check (`KvitDatabaseSetting.Read(app.Configuration)` in `Program.cs`, right after `Build()`) stops the app with a clear `InvalidOperationException` when it is missing, before the web server listens. The app never connects to the database or migrates at start-up (built in Phase 4, Step 1; Identity's own table and index names are renamed by hand because EFCore.NamingConventions leaves explicit names alone).
- **Data Protection keys** are stored in the database (`PersistKeysToDbContext`) and encrypted. Render wipes its files on restart (see Traps).
- **Duplicate protection:** every queued write from the outbox carries a client-generated `ClientRequestId` (a GUID) with a unique index on it. A repeated request returns the original result and doesn't create a second row.

## Result pattern (no exceptions for business failures)
- `Result` / `Result<T>` have `IsSuccess`, `Error`, `ErrorCode`, `StatusCode` and `Value`.
- **Factories:**
  - `Result.Ok()`
  - `Result.Ok<T>(v)`
  - `Result.Failure(err, code)` (400)
  - `Result.Unauthorized(...)` (401)
  - `Result.Forbid(...)` (403)
  - `Result.NotFound(...)` (404, new in Kvit)
  - Each failure factory also has a generic twin (`Result.NotFound<T>(...)` etc.), so a query handler returning `Result<T>` can fail (Phase 1).
- **Dispatcher calls:** `Send<TCommand>(...)` → `Result`, `Send<TCommand, TResult>(...)` and `Query<TQuery, TResult>(...)` → `Result<TResult>`. No handler, or more than one, throws with the request's name.
- **HTTP answers:** success → 204 (`Result`) or 200 with the value (`Result<T>`); failure → ProblemDetails with an extra `errorCode` field.
- **Two traps in `Result<T>` answers (VERIFIED by test, review-fix Step 2):** a `Result<T>` holding null answers **204 with no body**, so never return `Result.Ok<T>(null)` where the frontend expects an object; a handler that finds nothing returns `Result.NotFound<T>`. A `Result<string>` answers `text/plain` without quotes, so wrap a string in a small record when the frontend expects JSON.
- **Error codes** are string constants in `ResultCodes` (e.g. `EXPENSE_SPLIT_DOES_NOT_ADD_UP`). The frontend translates these codes, so they're a contract. Never rename one silently.
- **Exceptions are only for real bugs and outages** (the database is down, the NBRM service is unreachable). They're logged with the cause and never swallowed.

## DI registration (Scrutor, in `Registers/Register.*.cs`)
- **Domain services:** scan `Kvit.Domain.Services`, registered with `AsSelf()` and a scoped lifetime.
- **Repositories:** scan `Kvit.Infrastructure.Repositories`, registered with `AsMatchingInterface()` and a scoped lifetime.
- **Handlers:** registered as `AssignableTo(ICommandHandler<>)`, `ICommandHandler<,>` and `IQueryHandler<,>`, with `AsImplementedInterfaces()` and a transient lifetime.

## Build, Docker and CI
- `global.json` pins SDK 10.0.400 with `rollForward: latestFeature` (any .NET 10 SDK from 10.0.400, never .NET 11). Tests use xUnit v3 on Microsoft Testing Platform (`global.json` sets the runner).
- The Dockerfile builds on `sdk:10.0` and runs on `aspnet:10.0` as the non-root `app` user. It has **no build arguments** (no secret can end up in the image) and **no port** (Render sets `ASPNETCORE_HTTP_PORTS=10000`). **Every new project's `.csproj` needs a `COPY` line before `dotnet restore`**; CI's backend job ends with `docker build`, so a missing line fails CI.
- The build enforces style: `var` (IDE0008), file-scoped namespaces (IDE0160) and a namespace that doesn't match its folder (IDE0130) are errors; primary constructors, collection expressions and switch expressions are warnings, which `TreatWarningsAsErrors` turns into errors. "No comments" and "file name = class name" can't be checked by the compiler and stay review rules. Generated migrations are marked `generated_code` in `.editorconfig`.

- **`dotnet ef` and the migration bundle need only `ConnectionStrings:KvitDatabase`** (Phase 5, Step 1). `AppDbContextFactory` (`src/api/Kvit.Api/Persistence/`, an `IDesignTimeDbContextFactory`) builds the context from user secrets and environment variables, and EF's tools never run `Program.cs` when it exists, so the DataProtection certificate is not needed. The `UseNpgsql` + snake_case setup lives once in `UseKvitDatabase` (`Kvit.Infrastructure/Persistence`), used by the app and the factory.
- **Migrations reach Neon from CI, not from the app.** The `backend` job in `ci.yml` has steps after the Docker build: `has-pending-model-changes` on every run (fails when the model changed without a migration), and on a push to `main` only: a check that the secret `NEON_DIRECT_CONNECTION_STRING` exists, a migration bundle build, and the bundle run against Neon with the **direct** connection string. A failed step fails the job, so Render's "After CI Checks Pass" does not deploy.
- **A migration must be backward-compatible with the code that is still running**, because the bundle runs before the new code is live. Add a column or table in one phase and use it in the next; never drop or rename something the running code still reads in the same push.

## Code style
- **Every `.csproj`:** `Nullable=enable`, `ImplicitUsings=enable`, **`TreatWarningsAsErrors=true`**.
- **Syntax:** primary constructors, collection expressions `[]`, switch expressions.
- **Explicit types**, not `var`, however long the generic.
- **Namespaces:** block style.
- Namespaces are `Kvit.<Layer>.<Feature>`, and file names match class names.
- **Literals:** a literal used once or twice stays inline. A `const` earns its place at three or more uses, or when it's a setting worth tuning (e.g. `ExchangeRateMaxAgeHours = 8`).
- **Sentinels get a name.** A value with business meaning is a named constant, never a bare number repeated.
- **Nulls are handled once,** at the layer that owns the absence. Everything below that takes a definite type, and nothing falls back silently.

---

# PART 2: REACT FRONTEND (`src/web`)

## Stack
| Concern | Choice |
|---|---|
| Language / build | TypeScript 7 + Vite (Phase 2 tried TS 7 last, on the real code; kept because lint, build and tests all passed on it) |
| Server state + caching | **TanStack Query**, with its cache persisted to IndexedDB so saved data shows instantly (check docs) |
| Local UI state | `useState` / `useReducer`. A global store only if a real need appears |
| Offline outbox | Own small module over IndexedDB (e.g. `idb-keyval` or Dexie, check docs) |
| HTTP | `fetch` wrapper `apiClient.ts` (cookie auth, so no token interceptor) |
| Models | TypeScript types **generated from the API's OpenAPI spec** (`openapi-typescript`, check docs), so frontend and backend can't drift apart |
| Routing + guards | React Router + `<RequireAuth>` / `<RequireAdmin>` wrappers |
| Localisation | react-i18next, `en.json` + `mk.json` |
| Styling | Tailwind CSS v4 with CSS variables as design tokens (colours, spacing) + shadcn/ui components (Base UI variant) in `shared/components/ui/`, wrapped by `Kvit*` components + Sonner for toasts (decided 2026-09-25, see `DECISIONS.md`) |
| Lint | oxlint (the Vite template's default since create-vite 9) |
| Tests | Vitest for logic (money formatting, outbox) and for screens and hooks (jsdom + Testing Library, `environment: 'jsdom'` for every test file, cleanup in `src/test/setup.ts`), and Playwright later for key flows |

## Folder structure (`src/web/src/`): vertical slices
- **`core/`**: infrastructure shared by all features.
  - `api/`: `apiClient.ts`, `endpoints.ts` (all paths in one place; functions for paths with parameters), `errors.ts` (maps `ResultCodes` to translation keys; `errorMessageKey` answers `errors.network` for a fetch that never got an answer and for HTTP 502, the proxy's answer when the API is asleep or down; an unmapped error code is logged with `console.error` and answers `errors.generic`). `generated/` (OpenAPI types, never edited by hand) arrives once the backend has real contracts to generate from.
  - `services/<domain>/`: one module per domain. Phase 2 only has `services/health/healthService.ts` (the sole caller of `apiClient` so far); more arrive with the features that need them.
  - `router/`: the route table (`routes.ts`, `router.tsx`) and the `RequireAuth` guard (a placeholder until Phase 4: it always redirects to `/welcome`).
  - `i18n/`: setup (`i18n.ts`), `locales/en.json` + `locales/mk.json`, and the hand-written `detectLanguage.ts` (saved choice in `localStorage`, else the phone's language list) instead of `i18next-browser-languagedetector`.
  - `outbox/`: the offline queue (IndexedDB) — Release 3, not built yet.
- **`features/<feature>/<sub_feature>/`**, each slice containing:
  - `components/`: the screen (`*Screen.tsx`) and its pieces,
  - `hooks/`: `use*.ts` (TanStack Query hooks wrapping the services),
  - `types.ts`: slice-only types, if any.
  - Phase 2 has `features/auth/welcome/` (the Welcome screen) and `features/notFound/`.
- **`shared/components/`**: reusable `Kvit*` components. Phase 2 has `KvitButton`, `KvitLoading`, `KvitError`, `KvitEmpty`, `KvitToaster` (Sonner, `theme="system"`, no `next-themes`). Others (`KvitAmountInput`, `KvitChip`, `KvitAvatar`…) arrive with the features that need them. shadcn/ui's own copied files live in `shared/components/ui/` and are only ever imported by the `Kvit*` wrappers, never by a feature directly.
- **`shared/utils/`**: money and date formatting (`Intl` gets the app language, `mk` or `en`, not a region tag like `mk-MK`). `formatMoney` works only in integer minor units; a non-whole-denar MKD amount or a non-safe-integer value throws, naming the value, because it can only be a bug.

## Rules
- **Shared components know no feature.** Everything comes in through props and goes out through callbacks, and they never call feature hooks or services.
- **One level watches the data.** Either the screen reads the query hook and passes data down, or each child reads its own. Never both in one feature.
- **Components that only draw what they're given** are plain function components with no hooks beyond local UI state.
- **All user-facing text goes through `t('key')`.** Never hardcode strings. Reuse an existing key with the same meaning before adding a new one.
- **Every server call goes through a service.** Components never call `fetch` directly.
- **Loading, error and empty states** use the shared `KvitLoading` / `KvitError` / `KvitEmpty` components.
- **Mobile first.** Kvit is used on phones: design for 360 px width first and grow from there.
- **Money in the frontend** is also integer minor units. It's only formatted for display at the last moment.
- **Error codes and the session (Phase 4):** every `ResultCodes` entry the frontend can receive has a translation `errors.<CODE>` in `en.json` and `mk.json` (a Vitest test checks the 13 codes of Phase 4; Phase 8 widens it). A failed "who is signed in" query is never treated as signed out; only a 401 on `GET /api/me` means signed out. Forms use `noValidate` and no client-side password rule: the server answers and the screen shows the translated message with the typed values kept.
- **Look (Phase 4):** colours and sizes exist once as tokens in `src/index.css` (peach and `#351F1B` pages, deep-orange main buttons in light, peach in dark); one main-button look app-wide; text contrast at least 4.5:1 (3:1 for large text and for button or field edges); the approved reference images are in `docs/design/2026-10-01-round-3/`.
- **Tests sit next to the code they test** (`formatMoney.test.ts`); the Cloudflare proxy's tests are in `src/web/test/functions/`. Every route sits under one `errorElement` (`RouteError`), and `main.tsx` shows a two-language fallback if `startI18n()` fails. Node 24 is pinned (`.nvmrc`, `engines`).

## Naming
- **Components:** PascalCase file and name (`AddExpenseScreen.tsx`, `ExpenseListItem.tsx`).
- **Hooks:** `useX.ts`. **Services:** `xService.ts`.
- **Suffixes:** `*Screen`, `*Dialog`, `*Form`, `*ListItem`.
- **Callback props** are named `on<Event>` (`onSave`, `onCancel`).

---

# PART 3: WORKING RULES (Filip's own rules)

## How to respond
- Start every message with `Filip`. **Every** message.
- Explain what you're changing and why, to someone who knows nothing about the project. Filip is a beginner at .NET and React, so explain each new concept in one or two plain sentences the first time it comes up.

## Propose, then wait
Before changing code, send Filip:
1. **What** will change: which files, and the shape of the change.
2. **Why:** the problem it solves.
3. **Why not the alternatives.** If the choice is a business rule, ask instead of picking.

Then **stop and wait for "continue"**. The proposal and the edit never share a message. On a big task, propose the next piece, not the whole plan.

## Never guess
If you're even slightly unsure of a name, path, signature, default or library API, read the code or the official docs again. The results of other tools or agents are leads to check, not facts. When reading can't settle it, **stop and ask**.

## Search before writing
Before adding a shape (a DTO, service method, shared component, translation key, `ResultCodes` entry), grep for whether one already exists. **A count of zero is the finding.** If the new code would be the only place doing something a certain way, it's probably the wrong way.

## Comments and formatting
- **Don't write comments.** No `//`, no `///`. Explanations go in the PR description or the docs.
- **Format only the lines you changed.** After formatting, check `git diff` and make sure every hunk is yours.

## Commit messages
- **"msg"** means one sentence and nothing else.
- **"commit msg"** means, for the first push of a branch, a commit subject, a PR name and a PR description; for every later push, just the one-line commit subject. Commits never have a body. Flag any files that were already modified before the task started.
- **English only** in commits and PR text. Macedonian stays in `mk.json` and in the docs.
- Branch names and the rest of the git workflow follow Filip's global rules (`feat/NN-short-name`). **No Claude co-author trailer.**

## Where to write what (so the docs stay small; agreed with Filip 2026-09-29)
- **`DECISIONS.md`** holds the product rules (what the app does) and the **log of the phase in progress**. Only real decisions go in it, with the rejected alternatives, and never verification results.
- **A finished phase's log moves, word for word, to the end of that phase's report** in `docs/reports/` (section "Decisions and rejected alternatives"). Any rule from it that still binds future code gets one line in this file first.
- **Verification results** (what was run, the numbers, VERIFIED / NOT VERIFIED) live only in the step's report. `STATUS.md` gets a few lines and a link to the report; `ROADMAP.md` gets its box ticked.
- **Reading:** read the headings first (`Grep '^#'`) and then the parts that apply: backend work reads Part 1, 3 and 4 here; frontend work reads Part 2, 3 and 4; `DATA-MODEL.md` only for the tables the step touches. Read a part completely once it applies; nothing is summarized.

## When a hypothesis can be counted, count it
When debugging, prefer the command that produces a number (a grep count, a SQL `SELECT`, a test run) over reasoning that produces a story.

## Verification
- **Kvit has tests.** Before calling something done, run `dotnet test` and the frontend checks (`npm run lint`, `npm run build`, `npm test`).
- Then run the real flow in the browser at **phone width**, with a **normal user**, not only the admin.
- Money changes need a unit test that covers rounding and both currencies.
- **Backend on Windows:** a running API locks its own DLLs, so `dotnet build` can "succeed" while nothing reaches `bin\`. The loop is: **stop the API → build → start → test.**

---

# PART 4: RECORDED TRAPS (from the research, before any code)
- **Render wipes files on every restart or redeploy.** Data Protection keys kept on disk would log everyone out, so they're stored in Postgres (encrypted).
- **Safari blocks cookies between different sites** (`pages.dev` vs `onrender.com`). All API calls go through the Cloudflare Pages Function at `/api/*`, and the frontend never calls the Render address directly.
- **Neon's free compute hours** run out if something keeps the database awake. The health check (`/health`) must not touch the database, and nothing may poll it.
- **Render's free service sleeps after 15 minutes idle** and takes about 1 minute to wake. The frontend wakes it on page load and shows saved data plus the outbox while it wakes.
- **TypeScript 7** has no stable tooling API yet, so typescript-eslint may need TS 6. Check the generated `package.json`.
  - *2026-09-25:* checked. The Vite `react-ts` template (create-vite 9.2.1) installs TS ~6.0.2 and uses **oxlint**, not ESLint, so typescript-eslint isn't involved. TS 7 gets tried in Phase 2 (see `reports/2026-09-26-phase-02-frontend-skeleton.md`). `openapi-typescript` 7.13.0 asks for TS 5, which clashes with both 6 and 7.
- **Macedonian plurals:** 21, 31, 101 use the "one" form, so always pass `count` to `t()`. Set `<html lang="mk">` when Macedonian is active.
- **NBRM exchange rate:** if the service fails, keep the last saved rate and show its date, and log the error. Never fall back silently to a made-up number.
- **TS 7 removed `baseUrl`; TS 6 had already deprecated it.** shadcn's own Vite setup guide still shows `baseUrl` for the `@/` import alias. Kvit's `tsconfig.json` uses `paths` alone; Vite's `resolve.alias` (in `vite.config.ts`, built with `import.meta.dirname`, not `__dirname`) makes the same alias work at build/dev time. *2026-09-26, verified: build and dev server both resolve `@/` correctly with this setup.*
- **Dark mode follows the device by default, with a Settings choice (Same as device / Light / Dark, decided 2026-10-01; `DECISIONS.md`).** The choice is a `data-theme` attribute on `<html>` set by a small theme module and saved in `localStorage` (`kvit.theme`); the colour tokens stay in CSS; still no `next-themes` and no `.dark` class. Sonner is used directly and must follow the same attribute. (Before 2026-10-01 this trap said Kvit never has a theme switch.)
- **Behind the Cloudflare Pages Function proxy, the API only sees Render's and Cloudflare's own addresses, not the visitor's, and the API is also reachable directly on `onrender.com`.** So the per-visitor rate limit works like this (built in Phase 4, Step 3):
  - The proxy (`src/web/functions/api/[[path]].ts`) throws away every forwarding header the visitor's browser sent (`x-forwarded-*`, `forwarded`, `x-real-ip`, `true-client-ip`, `cf-connecting-ip[v6]`, and any `x-kvit-proxy-secret` / `x-kvit-visitor-ip`), then sets `x-kvit-proxy-secret` (the Cloudflare secret `API_PROXY_SECRET`) and `x-kvit-visitor-ip` (from `cf-connecting-ip`, which Cloudflare adds; its docs: "provides the client IP address connecting to Cloudflare to the origin"). It no longer sets `X-Forwarded-For`. A missing `API_PROXY_SECRET` or `cf-connecting-ip` makes it answer 500 instead of guessing.
  - The API (`Proxy:SharedSecret` set) answers 403 to every request without the right secret except `/health` and `/api/health`, then reads the visitor from `X-Kvit-Visitor-Ip` through ASP.NET's forwarded-headers middleware (`ForwardLimit = 1`, both known lists empty), so the visitor becomes `RemoteIpAddress`. Production without the setting stops at start-up; other environments run with the gate off and the header ignored. Rate limits read `RemoteIpAddress` only, through `VisitorAddressKey` (IPv6 grouped by /64; a missing address throws).
  - *Why a Kvit-only header:* Render's load balancer *appends* to `X-Forwarded-For` with an unknown number of entries (a Render staff reply; no official doc), so a hop count would be a guess. Decision and rejected alternatives: the Phase 4 entry in `DECISIONS.md` (Step 3).
  - **VERIFIED on the deployed site (Phase 5, 2026-10-01):** 12 log-ins through `kvit-mk.pages.dev`, each with different made-up `X-Forwarded-For`, `X-Kvit-Visitor-Ip`, `True-Client-IP` and `X-Real-IP`, answered 401 ten times and 429 from the 11th: the API counted one real address, so Render passes both proxy headers through untouched. A made-up `CF-Connecting-IP` never reaches our code: Cloudflare's edge refuses it with 403 "error code: 1000" (its error-1000 page lists "The request includes a CF-Connecting-IP header"). The sign-up limit answered 429 on the 6th counted sign-up.
- **Cloudflare calls Pages "legacy".** A new project is created through the link "Continue to Pages" on the "Create an app" page; the big GitHub button there is the Workers flow. Pages still works and its docs show no end date.
- **Render deploys through CI, not by itself.** From the Phase 5 merge on, Render's Auto-Deploy is Off and the last CI step on a push to `main` (`scripts/RenderDeploy.cs`, needs the secret `RENDER_API_KEY` and the variable `RENDER_SERVICE_ID`) triggers the deploy of that commit and waits until it is `live`. A separate watcher check beside Render's "After CI Checks Pass" could deadlock, because Render waits for all checks on the commit.
- **Wake-up times (one sample each, 2026-10-01):** Render free from a full sleep about 22 s; the first database request after Neon slept 4 s and succeeded, so no `EnableRetryOnFailure`. `/health` does not wake Neon (VERIFIED by timing). The Neon console's chart is 10 minutes off and the console itself (Tables, SQL editor) can wake the database, so never use it as proof.
- **Harmless log noise on Render:** `libgssapi_krb5.so.2: cannot open shared object file` (the driver looks for Kerberos; password log-in works).

- **Google sign-in (Phase 6, 2026-10-03):**
  - `GoogleJsonWebSignature.ValidateAsync` with `ValidationSettings.Audience = null` **switches the audience check off**; `GoogleSetting.Read` stops the app at start-up when `Google:ClientId` is missing. People are identified by the token's `sub`, never by email. The library throws `ArgumentException`, `FormatException` or `JsonReaderException` (not only `InvalidJwtException`) for malformed tokens; all four become 401 `AUTH_GOOGLE_TOKEN_INVALID`, network errors stay 500.
  - The client id is public and committed (`appsettings.json`, `src/web/.env`; `.gitignore` has `!src/web/.env`). The client secret is never used.
  - Cloudflare `public/_headers` is **not applied to Pages Function responses**, so `/api/*` never gets the CSP; it is delivered on the site pages (VERIFIED on the preview). The CSP forbids inline scripts: the theme script lives in `public/theme-boot.js`. A new third-party script, frame or style needs the CSP edited too. Preview addresses (`<hash>.kvit-mk.pages.dev`, `feat-...kvit-mk.pages.dev`) get the headers but cannot sign in with Google (origin not authorized).
  - Google's button is its own iframe (about 40 px high) and cannot be styled. Every `renderButton` call empties the box and shows Google's Macedonian placeholder before the iframe loads (1-2 frames stacked, a 36 px jump). So `GoogleSignInButton` draws one slot per language once, overlaps the slots in one grid cell with a fixed height, and hides the inactive slot with `opacity-0 inert` (a `visibility:hidden` iframe never finishes drawing in Chrome). It is always the white `outline` theme and carries `scheme-light`, because in dark mode Chrome paints an opaque white backdrop behind a light-scheme iframe.
  - At a merge Cloudflare shows the new site minutes before CI finishes the Render deploy; a frontend that needs a new API field (`hasPassword`) errors in between. Merge, then wait for the green CI.
  - Never open a browser on Filip's profile for checks (a bare `msedge.exe --version` opened his Edge and restored his tabs). Use headless Chrome with a scratch `--user-data-dir`.


- **Expenses (Phase 8, 2026-10-09):**
  - **Chrome has no Macedonian `Intl` data** (`Intl.DateTimeFormat.supportedLocalesOf(['mk'])` is empty), so Macedonian dates, numbers and plurals are our own tables in the code (`shared/utils/formatDate.ts`, `formatMoney`, translation keys with plural forms). Never pass `'mk'` to `Intl`. Node has the data, so a test that reads `Intl` output passes in Node and fails in Chrome: `src/test/setup.ts` stubs `Intl` to behave like Chrome without Macedonian, and tests pin the Macedonian text without relying on the runner's data.
  - **Latin `è` is not Cyrillic `ѐ`** (U+00E8 vs U+0450) and the word «Сè» looks the same either way; write the Cyrillic one in `mk.json`. "Link" is «линк», never «врска».
  - **NBRM** (checked live 2026-10-05): `GET https://www.nbrm.mk/KLServiceNOV/GetExchangeRate?StartDate=dd.MM.yyyy&EndDate=dd.MM.yyyy&format=json` (HTTPS, no key; `yyyy-MM-dd` gives 400). The answer is a list with one entry per currency; take `oznaka == "EUR"`, `sreden` divided by `nomin` as a decimal (never `double`), `datum` is the rate's date. A Sunday returns the carried-over Friday rate, a date far ahead answers 404, a public holiday is NOT VERIFIED (BACKLOG). The real call worked locally on 2026-10-07 (61.6423).
  - **The CSP lives in `src/web/public/_headers`.** A new image host needs `img-src` edited (Google profile pictures: `https://*.googleusercontent.com`); the picture `<img>` carries `referrerPolicy="no-referrer"`.
  - **Two identical requests at the same moment** both pass the "is this `clientRequestId` known?" check and the second insert hits the unique index. Handle the Postgres 23505 on `ix_expenses_client_request_id` and run the transaction once more; do not catch it anywhere else. A race test needs a gate (a BEFORE INSERT trigger waiting on an advisory lock, `ExpenseInsertGate`), a loop of parallel calls is only timing.
  - **`[NotEmptyGuid]` on a record request goes on the constructor parameter** (`AttributeTargets.Parameter`). With `[property: …]` .NET 10 ignores the metadata and every request answers 500 ("validation metadata … will be ignored"). A missing JSON property arrives as `Guid.Empty`, so one check covers both.
  - **jsdom cannot compute colours:** a test for a red border checks the Tailwind class (`aria-invalid:border-destructive`), not the pixel.
  - **PowerShell and bash quote differently for `psql -c`;** write the SQL in a file or use the quoting of the shell you are in.

New traps found while building get added here, with the date.
