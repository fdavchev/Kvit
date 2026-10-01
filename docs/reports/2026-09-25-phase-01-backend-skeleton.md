# Report: Phase 1, backend skeleton (2026-09-25)

## What was asked
Build the empty backend that every later phase builds on (ROADMAP Phase 1): the project structure, the Result pattern, the command/query dispatcher, health checks, the API reference page, the Docker image and the automatic tests on GitHub. No features, no database.

## What was built
Written by the `coder` subagent (Opus); checked again independently in the main session.

| Part | Files | What it does |
|---|---|---|
| Repository basics | `.gitignore`, `.gitattributes`, `.editorconfig`, `global.json`, `Directory.Build.props`, `Directory.Packages.props` | Pins the .NET SDK, keeps every library version in one file, and makes warnings and style mistakes (like `var`) fail the build |
| Domain | `src/api/Kvit.Domain/Results/` | `Result` / `Result<T>`: a method answers "worked, here's the value" or "failed, with this message and code" instead of throwing |
| Application | `src/api/Kvit.Application/Dispatching/` | The dispatcher: the controller hands over a request, the dispatcher finds the one class that handles it (about 40 lines; replaces MediatR) |
| API | `src/api/Kvit.Api/` | Program start-up, `BaseController` (turns a Result into an HTTP answer), `/health` and `/api/health`, the Scalar API page (only on your PC), `Kvit.Api.http` |
| Docker | `src/api/Dockerfile`, `.dockerignore` | Builds the image Render will run |
| CI | `.github/workflows/ci.yml` | GitHub builds and tests every push and pull request |
| Tests | `tests/Kvit.Domain.Tests/`, `tests/Kvit.Api.Tests/` | 26 tests |
| Solution | `Kvit.slnx` | Lists the 5 projects |

The design choices and their reasons are at the end of this report.

## Verification
| Check | Result | Label |
|---|---|---|
| `dotnet build Kvit.slnx --no-incremental` | Build succeeded, 0 warnings, 0 errors | **VERIFIED by live run** (main session) |
| `dotnet test Kvit.slnx` | 26 total, 26 passed, 0 failed (Domain 11, Api 15) | **VERIFIED by automated test** (main session) |
| Docker image build | Succeeded | **VERIFIED by live run** (main session) |
| Container on Render's port (`ASPNETCORE_HTTP_PORTS=10000`) | `/health` and `/api/health` answer `Healthy` (200); `/scalar` is 404 (hidden in production); runs as user `app` (uid 1654), not root | **VERIFIED by live run** (main session) |
| Style rules really fail the build | A throwaway file with `var`, file-scoped namespace etc. failed the build, then was deleted | **VERIFIED by live run** (coder) |
| No comments in code, no secrets | 0 comment lines in the new `.cs` files; the only "secret" matches are ignore rules that keep secret files out of git | **VERIFIED by search** (main session) |
| GitHub Actions versions exist (`checkout@v7`, `setup-dotnet@v6`) | Both tags exist and are the latest releases | **VERIFIED by live check** (main session) |
| CI on GitHub itself | Runs on the first push | **NOT VERIFIED** yet |

## Where the code differs from the plan (all logged at the end of this report)
- The folder is `Results/`, not `Result/` (C# gets confused when a namespace and a class share a name). ARCHITECTURE updated.
- Failure factories also exist in a generic form (`Result.NotFound<T>`), so reading data can fail with "not found". ARCHITECTURE updated.
- `Kvit.Contracts` moves to Phase 4 with `Kvit.Infrastructure` (no empty projects).
- Tests use xUnit v3 on the newer Microsoft Testing Platform runner, without the older VSTest packages.

## New concepts in this phase (plain words)
- **Result pattern:** instead of throwing an error when a rule is broken, a method returns a small box: "worked, here's the value" or "failed, here's why, with an error code". Exceptions are kept for real bugs and outages.
- **Dependency injection:** a class lists what it needs in its constructor, and ASP.NET creates and passes those in. Nobody writes `new` for services, so tests can swap parts.
- **Scrutor:** finds every class that matches a rule (for example "is a handler") and registers it, so nobody has to list them by hand.
- **Commands, queries, handlers, dispatcher:** a *command* changes something, a *query* reads something. Each has exactly one *handler* class that does the work. The controller gives the request to the *dispatcher*, which finds that handler.
- **ProblemDetails:** the standard shape for API error answers. Kvit adds an `errorCode` that the website translates into English or Macedonian.
- **Health check:** an address that answers "Healthy". Render calls it to know the app is alive. It never touches the database, so it doesn't use up Neon's free hours.
- **OpenAPI + Scalar:** OpenAPI is a machine-readable list of every endpoint; Scalar is a web page that shows it and lets you try them (your replacement for Postman). Only on your PC.
- **Central package management / Directory.Build.props / global.json:** settings written once for all projects: which .NET version builds the code, which library versions are used, and which rules make the build fail.
- **Multi-stage Docker build:** stage one has the full .NET SDK and compiles the app; stage two copies only the finished app into a small image that runs as a normal user, not the all-powerful one.
- **WebApplicationFactory:** starts the whole API inside a test, without a real network, so a test can call `/health` like a browser would.

## Notes for later phases
- The website's wake-up ping should call `/api/health`. The local API runs at `http://localhost:5018`, which becomes the Vite dev-proxy target in Phase 2.
- Unknown addresses and unexpected server errors currently answer with an empty body, not ProblemDetails (NOT VERIFIED; the coder checked status codes only). Planned fix: `AddProblemDetails()` + an exception handler, probably in Phase 4.
- CI runs on every push **and** every pull request, so a PR branch runs twice. Fine for now; limit `push` to `main` if it gets noisy.
- xUnit's analyzers turn some suggestions into build errors here (because warnings are errors), for example "pass `TestContext.Current.CancellationToken`".

## Next
Filip commits in 7 small commits and pushes (`guides/phase-01-commit-and-push.md`), opens the pull request, checks CI turns green, merges. Then Phase 2 (frontend skeleton).

## Decisions and rejected alternatives (moved word for word from DECISIONS.md on 2026-09-29)

### 2026-09-25: Phase 1 technical decisions
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

