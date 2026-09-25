# Phase 1: commit, push and merge (your steps)

Everything is typed in the VS Code terminal (**Terminal → New Terminal**), inside the Kvit folder, on branch `feat/01-backend-skeleton`.
Run the commands **one block at a time**, in order. Each `git commit` should answer with a line like `[feat/01-backend-skeleton 1a2b3c4] ...`.

> A line like `warning: ... LF will be replaced by CRLF` is normal on Windows. Ignore it.

## 1. The seven commits

**Commit 1: repository basics** (the first commit on the branch, so it has a longer message)
```
git add .gitignore .gitattributes .editorconfig global.json Directory.Build.props Directory.Packages.props
```
```
git commit -m "Add repository basics and shared build settings" -m "Sets up the .NET 10 build for the backend: the SDK is pinned in global.json, every package version lives in Directory.Packages.props, and Directory.Build.props makes warnings and code-style violations fail the build. First commit of Phase 1 (backend skeleton); no application code yet."
```

**Commit 2: the Result pattern**
```
git add src/api/Kvit.Domain tests/Kvit.Domain.Tests
```
```
git commit -m "Add the Result pattern to Kvit.Domain with tests"
```

**Commit 3: the dispatcher**
```
git add src/api/Kvit.Application
```
```
git commit -m "Add the command and query dispatcher to Kvit.Application"
```

**Commit 4: the API**
```
git add src/api/Kvit.Api tests/Kvit.Api.Tests Kvit.slnx
```
```
git commit -m "Add the API project with health checks, OpenAPI and tests"
```

**Commit 5: Docker**
```
git add src/api/Dockerfile .dockerignore
```
```
git commit -m "Add the API Dockerfile"
```

**Commit 6: CI**
```
git add .github/workflows/ci.yml
```
```
git commit -m "Add the backend CI workflow"
```

**Commit 7: docs**
```
git add CLAUDE.md docs
```
```
git commit -m "Update docs for Phase 1"
```

**Check:** `git status` now says `nothing to commit, working tree clean`. If it lists anything, stop and ask in the Claude session.

## 2. Push (once, at the end)
```
git push -u origin feat/01-backend-skeleton
```
- **You should see:** lines ending with `branch 'feat/01-backend-skeleton' set up to track 'origin/feat/01-backend-skeleton'`.
- Why only one push: every push makes GitHub run the tests, and the in-between commits aren't complete on their own (the solution file only arrives in commit 4).

## 3. Open the pull request
1. Open `https://github.com/fdavchev/Kvit`. A yellow bar offers **Compare & pull request**. Click it.
2. **Title:** `Phase 1: backend skeleton`
3. **Description:** paste the text from section 5 below.
4. Click **Create pull request**.

## 4. Wait for the green check, then merge
1. On the pull request page, near the bottom, **CI / backend** runs (about 1–3 minutes).
2. **You should see:** a green ✓ "All checks have passed".
   - A red ✗ means stop: click **Details**, copy the red error text, and paste it into the Claude session.
3. When it's green: edit the description and tick the last box ("CI green on GitHub").
4. Click **Merge pull request** → **Confirm merge**. Keep the default "Create a merge commit", so the seven small commits stay visible in the history.
5. Bring your PC up to date:
   ```
   git switch main
   ```
   ```
   git pull
   ```
   - **You should see:** a list of the Phase 1 files being added to `main`.

The branch for Phase 2 gets created when Phase 2 starts.

## 5. Pull request description (copy everything inside the box)
```
## Overview
The empty .NET 10 backend that every later phase builds on: the project structure, the Result pattern, a small hand-written command/query dispatcher (instead of MediatR), health checks, the API reference page, a Docker image for Render and a CI workflow. No features and no database yet.

## Scope
- Repository basics and shared build settings: SDK pinned, central package versions, warnings and code-style rules fail the build
- Kvit.Domain: Result / Result<T> / ResultCodes
- Kvit.Application: dispatcher and handler interfaces, registered with Scrutor
- Kvit.Api: BaseController (Result → HTTP; errors as ProblemDetails with an errorCode field), /health and /api/health (never touch a database), OpenAPI + Scalar in Development only
- src/api/Dockerfile: multi-stage, non-root user, port from ASPNETCORE_HTTP_PORTS
- .github/workflows/ci.yml: restore, build and test on every push and pull request
- 26 tests (Domain 11, Api 15)

Not included: Kvit.Contracts and Kvit.Infrastructure (Phase 4), the frontend (Phase 2).

## Checklist
- [x] Repository basics and build settings
- [x] Result pattern + tests
- [x] Dispatcher + tests
- [x] API skeleton: health, OpenAPI/Scalar, BaseController + tests
- [x] Dockerfile (checked locally on ports 8080 and 10000, runs as non-root)
- [x] CI workflow
- [x] Docs: roadmap, status, decisions, report
- [ ] CI green on GitHub
```
