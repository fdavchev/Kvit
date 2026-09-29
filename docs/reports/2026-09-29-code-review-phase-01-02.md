# Code review: Phase 1 (backend skeleton) and Phase 2 (frontend skeleton)

Date: 2026-09-29
Reviewed: `f0f3ad6..0b85675` (feat/01-backend-skeleton) and `4a91f7d..8e7f53f` (feat/02-frontend-skeleton), both already merged into main.
Method: read every changed file, then built and tested each commit in a scratch copy made with `git archive` (no tracked file was touched).

**Result:** no critical bugs. Build, lint and tests are clean. The weak spots are the Cloudflare proxy function and three test gaps.

## Labels used in this report

- **VERIFIED by automated test:** a build, lint or test command was run and its output read.
- **VERIFIED by live run:** run by hand in Node or a browser and the output read.
- **NOT VERIFIED:** found by reading the code only. Not run.

## What was actually run

| Check | Result | Label |
|---|---|---|
| Branch 01: `dotnet build Kvit.slnx` (Debug) | 0 warnings, 0 errors | VERIFIED by automated test |
| Branch 01: `dotnet test Kvit.slnx` | 26 of 26 passed | VERIFIED by automated test |
| Branch 02: `npm run lint` | 0 warnings | VERIFIED by automated test |
| Branch 02: `npm run build` | 0 type errors, built | VERIFIED by automated test |
| Branch 02: `npm test` | 52 of 52 passed | VERIFIED by automated test |
| GitHub CI runs on both branches | all green (`gh run list`) | VERIFIED by live run |
| URL behaviour behind findings 02-2 (Node `new URL`) | confirmed, see 02-2 | VERIFIED by live run |
| Release build (what CI uses), Docker image build, Cloudflare, browser | not run | NOT VERIFIED |

---

## Branch 01: feat/01-backend-skeleton

### 01-1 (medium): CI never builds the Dockerfile, and it will break in Phase 4
- File: `src/api/Dockerfile` lines 5-8.
- The Dockerfile copies only three `.csproj` files before `dotnet restore`. When `Kvit.Api` starts referencing `Kvit.Infrastructure` or `Kvit.Contracts`, the restore fails because those project files are missing. CI still passes, so it would only show up when Render deploys.
- Fix: add a `docker build` step to the backend job in `ci.yml`, and add the new `COPY` lines when those projects arrive.
- NOT VERIFIED (Docker not run; reasoned from the file).

### 01-2 (medium): the handler scan has no test
- Files: `src/api/Kvit.Api/Registers/Register.Application.cs` lines 11-18, `tests/Kvit.Api.Tests/Dispatching/DispatcherTests.cs` line 14.
- The tests register handlers by hand. The Scrutor scan finds zero handlers today. A wrong lifetime or a missed open-generic pattern would pass every test, then fail with "No handler is registered" on the first real endpoint.
- Fix: add a test with a small fixture assembly, or assert the first real handler resolves once it exists.
- NOT VERIFIED (reasoned from the code).

### 01-3 (low-medium): controllers are public unless they remember `[Authorize]`
- File: `src/api/Kvit.Api/Controllers/BaseController.cs` lines 6-7.
- No `[Authorize]` on the base class and no fallback authorization policy. A Phase 4 controller written without the attribute serves data to anyone.
- Fix: set a fallback policy that requires a signed-in user, and mark `/health` as anonymous.
- NOT VERIFIED.

### 01-4 (low): only the 404 branch of Result-to-HTTP is tested
- File: `tests/Kvit.Api.Tests/Controllers/BaseControllerTests.cs`.
- 400, 401 and 403 are never checked. A successful `Result<T>` holding null returns 200 with an empty body, which the frontend `apiClient` would fail to parse as JSON.
- NOT VERIFIED.

### 01-5 (low): every push to a PR branch runs CI twice
- File: `.github/workflows/ci.yml` lines 3-5 (`push` and `pull_request`).
- Confirmed in the run list: each PR commit has two runs. VERIFIED by live run.

### Checked and fine
- Dockerfile runs as non-root (`USER $APP_UID`), has no build arguments, `.dockerignore` excludes key and secret files.
- CI permissions are `contents: read`.
- `/health` and `/api/health` register no health checks, so they never touch the database.
- Scalar and OpenAPI are Development-only, and tests cover the 404 in Production (VERIFIED by automated test).
- No secrets or connection strings in the commit range.

---

## Branch 02: feat/02-frontend-skeleton

### 02-1 (medium): the proxy forwards client-controlled forwarding headers
- File: `src/web/functions/api/[[path]].ts` line 33.
- `request.headers` is passed straight through, including `x-forwarded-for`, `x-forwarded-host` and `forwarded`. Example: a client sends `X-Forwarded-For: 1.2.3.4`, and the Phase 4 per-IP login rate limit counts the wrong address, so it can be dodged.
- Fix: drop those headers and set the visitor address from `cf-connecting-ip`.
- NOT VERIFIED (a live probe was blocked by a tool error; read from the code).

### 02-2 (medium-low): the proxy does not enforce the `/api` prefix
- File: `src/web/functions/api/[[path]].ts` lines 24-25.
- VERIFIED by live run (Node): `new URL('https://k.pages.dev/api/%2e%2e/health').pathname` is `/health`, so upstream becomes `https://kvit-mk-api.onrender.com/health`. Also `new URL('//evil.com/x', origin)` becomes `https://evil.com/x`.
- The `//` case cannot reach the function today, only because `public/_routes.json` limits it to `/api/*`. The function itself has no guard.
- Whether Cloudflare routes the `%2e%2e` request to the function is NOT VERIFIED. Today the only reachable non-`/api` path is `/health`.
- Fix: after parsing, reject any path that does not start with `/api/` before calling `fetch`.
- `API_ORIGIN` is validated as an http(s) origin and only Filip can set it, so this is not counted as SSRF. It does allow plain `http:` and any host, so consider `https` only in production.

### 02-3 (medium): no handling when the API is asleep or down
- File: `src/web/functions/api/[[path]].ts` line 30.
- No `try/catch`, no timeout. If Render is waking (about a minute) or unreachable, the function throws and Cloudflare shows its own HTML error page. `apiClient` then reports the generic error instead of "Can't reach the server".
- Fix: catch the failure and answer 502 or 504 with a clear text body.
- NOT VERIFIED.

### 02-4 (medium): nothing catches a render-time crash
- Files: `src/web/src/shared/utils/formatMoney.ts` lines 14-28, `src/web/src/core/router/router.tsx`, `src/web/src/main.tsx` line 16.
- `formatMoney` deliberately throws on non-whole MKD or unsafe numbers. There is no `errorElement` and no error boundary. One bad amount from the API unmounts the whole app, and React Router shows its own English-only "Unexpected Application Error!" page, which breaks the `t('key')` rule. The same happens if `useLanguage` throws, and a failure in `await startI18n()` gives a blank page.
- Fix: add a route `errorElement` built from `KvitError` with translated text, and catch the startup failure in `main.tsx`.
- NOT VERIFIED (not run in a browser).

### 02-5 (low-medium): unknown API error codes vanish silently
- File: `src/web/src/core/api/errors.ts` lines 12-16.
- Any unmapped `errorCode` becomes "Something went wrong" with no log. Example: the backend adds `GROUP_NOT_OWNER` and nobody notices it has no translation. A 401 is not handled anywhere yet (expected until Phase 4).
- Fix: `console.error` on unmapped codes, and once `ResultCodes` has entries, a test that every code has a translation key.
- NOT VERIFIED.

### 02-6 (low): a failed health ping is invisible
- File: `src/web/src/features/auth/welcome/hooks/useApiHealth.ts`.
- Errors are ignored, so a broken proxy setting shows nothing. TanStack also re-pings on every window refocus (harmless: the endpoint does not touch Neon).
- NOT VERIFIED.

### 02-7 (low): language switching has loose ends
- Files: `LanguageSwitch.tsx` line 28 (promise from `changeLanguage` is never awaited), `useLanguage.ts` lines 13-14 (saves to `localStorage` before the language actually changes), `savedLanguage.ts` (a failed save only does `console.warn`).
- NOT VERIFIED.

### 02-8 (low): tooling and docs drift
- `src/web/components.json` points the `utils` alias at `@/shared/utils/cn`, which does not exist. The code imports the `cn` npm package instead (its manifest points at shadcn-ui/cn, so it is legitimate). Running `shadcn add` would generate imports to a missing file.
- `docs/ARCHITECTURE.md` says `formatMoney` uses `mk-MK`, the code uses `mk`.

### 02-9 (low, assumption to check in Phase 4): CSRF
- Cookie auth relies entirely on `SameSite`, with no anti-forgery token. The API is also reachable directly on onrender.com. Make sure the Phase 4 auth cookie is `SameSite=Lax` or `Strict`, and `Secure`.

### Proxy test gaps (`src/web/test/functions/proxyToApi.test.ts`)
Missing tests for: spoofed forwarding headers, dot-segment and `//` paths, upstream network failure, DELETE or PUT with no body, and the `onRequest` wiring.

### Checked and fine
- No user-facing string bypasses `t()`. The only literals are the decorative `·`, the brand `<title>`, and developer-only error messages.
- `formatMoney` handles safe-integer limits, MKD fractions, negatives, zero and EUR rounding (VERIFIED by automated test).
- `detectLanguage` handles `mk-MK` and `MK` (VERIFIED by automated test).
- `RequireAuth` fails closed: it always redirects to `/welcome`.
- No secrets in the commit range.

---

## Outside the two ranges
`START-HERE-PROMPT.md` is tracked in the repo and the hosting plan says the repo will be public. It contains no secrets, but check that publishing it is intended.

## Note on the automated `/code-review` skill
Its output was discarded: it reviewed the Phase 3 MoneyRules work instead of these two ranges. Nothing from it is in this report.

---

## Fix status

The fixes are done on branch `fix/review-phase-01-02`, in four steps. Filip commits between steps. This report keeps the findings above as the record of what was found; this table is updated after every step.

**Steps:** 1 = docs only (done). 2 = backend. 3 = Cloudflare proxy. 4 = frontend.

| Finding | Status | What changed | Label |
|---|---|---|---|
| 01-1 Docker build not in CI | Step 2 (not done yet). The Phase 4 `COPY` line is recorded now | Step 1: Phase 4 line in `docs/ROADMAP.md` for the new `.csproj` files | NOT VERIFIED (docs only) |
| 01-2 handler scan has no test | DEFERRED to Phase 4 | Line in the Phase 4 checklist: test that the first real handler resolves | NOT VERIFIED (docs only) |
| 01-3 no fallback authorization policy | DEFERRED to Phase 4 | Line in the Phase 4 checklist: fallback policy, `/health` and `/api/health` anonymous | NOT VERIFIED (docs only) |
| 01-4 Result-to-HTTP tests missing | Step 2 (not done yet) | Nothing yet | NOT VERIFIED |
| 01-5 CI runs twice | SKIPPED on purpose | Reason logged in `docs/BACKLOG.md`: the second run tests the merge with `main`, costs only free minutes | NOT VERIFIED (a decision, nothing to run) |
| 02-1 proxy forwards spoofable headers | Step 3 (not done yet). The Phase 4 rate-limit line is recorded now | Step 1: Phase 4 line for reading the forwarded visitor address | NOT VERIFIED (docs only) |
| 02-2 proxy doesn't enforce `/api` | Step 3 (not done yet) | Nothing yet | NOT VERIFIED |
| 02-3 no handling when the API is down | Step 3 (not done yet) | Nothing yet | NOT VERIFIED |
| 02-4 no error page or startup fallback | Step 4 (not done yet) | Nothing yet | NOT VERIFIED |
| 02-5 unknown error codes vanish | Step 4 (log it) and DEFERRED to Phase 8 (every-code-has-a-key check) | Step 1: Phase 8 line in `docs/ROADMAP.md` | NOT VERIFIED (docs only) |
| 02-6 failed health ping invisible | Step 4 (not done yet) | Nothing yet | NOT VERIFIED |
| 02-7 language switching loose ends | Step 4 (not done yet) | Nothing yet | NOT VERIFIED |
| 02-8 shadcn alias and `mk-MK` wording | Docs part FIXED in Step 1; the shadcn alias is Step 4 (not done yet) | `docs/ARCHITECTURE.md` and the Phase 2 line in `docs/ROADMAP.md` now say the app language `mk` / `en` is passed to `Intl`. Checked against `formatMoney.ts` first | NOT VERIFIED by a run; checked by reading the code (`new Intl.NumberFormat(language, ...)`) |
| 02-9 CSRF, cookie flags | DEFERRED to Phase 4 | The Phase 4 login-cookie line now says `SameSite=Lax` (or `Strict`), `Secure`, and why | NOT VERIFIED (docs only) |
| Proxy test gaps | Step 3 (not done yet) | Nothing yet | NOT VERIFIED |
| `START-HERE-PROMPT.md` in a public repo | NOT IN THIS WORK: Filip's decision | Left alone | NOT VERIFIED |
