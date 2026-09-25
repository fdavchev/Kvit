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

The design choices and their reasons are in `DECISIONS.md` ("Phase 1 technical decisions").

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

## Where the code differs from the plan (all logged in DECISIONS)
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
