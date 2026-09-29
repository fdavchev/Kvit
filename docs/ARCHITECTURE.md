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
  Kvit.Application/      Queries/<Feature>/<QueryName>/  and  Commands/<Feature>/<CommandName>/
  Kvit.Domain/           Entities/, Interfaces/ (repository interfaces), Services/<Entity>/, MoneyRules/ (not `Money/`, for the same reason; see DECISIONS, Phase 3), Results/ (not `Result/`: a namespace named like its class confuses C#; see DECISIONS, Phase 1)
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
        await _unitOfWork.OpenTransactionAsync(ct);
        Result result = await _createExpense.Execute(user.Value, request, ct);
        if (!result.IsSuccess) await _unitOfWork.RollbackAsync(ct);
        else await _unitOfWork.CommitAsync(ct);
        return result;
    }
}
```
- **Naming:** `<GetX>QueryHandler` and `<CreateX>CommandHandler`. Class names carry the entity: `CreateExpense`, `ConfirmSettlement`.
- Shapes a handler needs go in their own `<X>Dto.cs` in the same folder. Handlers don't declare nested or private classes.

## Controllers
- Inherit `BaseController`, with `[Route("api")]` and `[Authorize]`.
- **Thin:** build the query or command, `await _dispatcher.Send(...)`, `return Result(result);`.
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
- **Money is a value type, `Money(long MinorUnits, Currency Currency)`.** It's never a `decimal` or `double` in entities. Adding two different currencies is a failure, not a conversion.
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
- **Connection string:** the key is `KvitDatabase`. The value only ever comes from environment variables or secrets, **never** from `appsettings.json` in the repository.
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
- **Error codes** are string constants in `ResultCodes` (e.g. `EXPENSE_SPLIT_DOES_NOT_ADD_UP`). The frontend translates these codes, so they're a contract. Never rename one silently.
- **Exceptions are only for real bugs and outages** (the database is down, the NBRM service is unreachable). They're logged with the cause and never swallowed.

## DI registration (Scrutor, in `Registers/Register.*.cs`)
- **Domain services:** scan `Kvit.Domain.Services`, registered with `AsSelf()` and a scoped lifetime.
- **Repositories:** scan `Kvit.Infrastructure.Repositories`, registered with `AsMatchingInterface()` and a scoped lifetime.
- **Handlers:** registered as `AssignableTo(ICommandHandler<>)`, `ICommandHandler<,>` and `IQueryHandler<,>`, with `AsImplementedInterfaces()` and a transient lifetime.

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
| Tests | Vitest for logic (money formatting, outbox), and Playwright later for key flows |

## Folder structure (`src/web/src/`): vertical slices
- **`core/`**: infrastructure shared by all features.
  - `api/`: `apiClient.ts`, `endpoints.ts` (all paths in one place; functions for paths with parameters), `errors.ts` (maps `ResultCodes` to translation keys; only the generic "something went wrong"/"can't reach the server" keys exist while `ResultCodes` is still empty). `generated/` (OpenAPI types, never edited by hand) arrives once the backend has real contracts to generate from.
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
- **"commit msg"** means a subject line plus a body for the branch and its PR. Flag any files that were already modified before the task started.
- **English only** in commits and PR text. Macedonian stays in `mk.json` and in the docs.
- Branch names and the rest of the git workflow follow Filip's global rules (`feat/NN-short-name`). **No Claude co-author trailer.**

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
  - *2026-09-25:* checked. The Vite `react-ts` template (create-vite 9.2.1) installs TS ~6.0.2 and uses **oxlint**, not ESLint, so typescript-eslint isn't involved. TS 7 gets tried in Phase 2 (see `DECISIONS.md`). `openapi-typescript` 7.13.0 asks for TS 5, which clashes with both 6 and 7.
- **Macedonian plurals:** 21, 31, 101 use the "one" form, so always pass `count` to `t()`. Set `<html lang="mk">` when Macedonian is active.
- **NBRM exchange rate:** if the service fails, keep the last saved rate and show its date, and log the error. Never fall back silently to a made-up number.
- **TS 7 removed `baseUrl`; TS 6 had already deprecated it.** shadcn's own Vite setup guide still shows `baseUrl` for the `@/` import alias. Kvit's `tsconfig.json` uses `paths` alone; Vite's `resolve.alias` (in `vite.config.ts`, built with `import.meta.dirname`, not `__dirname`) makes the same alias work at build/dev time. *2026-09-26, verified: build and dev server both resolve `@/` correctly with this setup.*
- **Dark mode is CSS-only.** No `.dark` class, no toggle, no `next-themes` — everything follows `prefers-color-scheme` because Kvit never has a manual theme switch (`DECISIONS.md`: always follows the phone). Sonner is used directly with `theme="system"` for the same reason; shadcn's own generated Sonner wrapper pulls in `next-themes`, which was dropped.
- **Behind the Cloudflare Pages Function proxy, the API only sees Cloudflare's own address, not the visitor's.** The proxy (`src/web/functions/api/[[path]].ts`) throws away every forwarding header the visitor's browser sent (`x-forwarded-*`, `forwarded`, `x-real-ip`, `true-client-ip`, `cf-connecting-ip`, `cf-connecting-ipv6`) and sets `X-Forwarded-For` to the one address in `cf-connecting-ip`, which Cloudflare adds (its docs: "provides the client IP address connecting to Cloudflare to the origin"). If `cf-connecting-ip` is missing the proxy answers 500 instead of guessing. Phase 4's per-IP rate limiting reads `X-Forwarded-For` through ASP.NET's forwarded-headers middleware, never `HttpContext.Connection.RemoteIpAddress`. Decision and reasons: `DECISIONS.md` 2026-09-29. Three open points, none checked yet:
  - Render's own load balancer sits between the proxy and the API. What it does to `X-Forwarded-For` (append its peer's address? how many entries reach the API?) is NOT VERIFIED; it decides `ForwardLimit` / `KnownIPNetworks` in Phase 4/5 (a separate but related trap: the backend's own `KnownIPNetworks` note for Render forwarding, Phase 5).
  - The API is also reachable directly on `onrender.com`, where anyone can send their own `X-Forwarded-For`. Unless the API only trusts requests that came through the proxy (for example a secret header the proxy adds), a per-IP limit can be dodged by skipping Cloudflare. Decide this in Phase 4.
  - Local `wrangler pages dev` honours a client-sent `cf-connecting-ip`; the real Cloudflare edge is documented to set it itself. Prove it on the deployed site in Phase 5.

New traps found while building get added here, with the date.
