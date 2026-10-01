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

**Steps:** 1 = docs only (done). 2 = backend (done). 3 = Cloudflare proxy (done). 4 = frontend (done).

| Finding | Status | What changed | Label |
|---|---|---|---|
| 01-1 Docker build not in CI | FIXED in Step 2 (the Phase 4 `COPY` lines stay a Phase 4 item, recorded in Step 1) | `.github/workflows/ci.yml`: the backend job ends with `docker build -f src/api/Dockerfile .` | VERIFIED by live run: the same command was run locally with Docker Desktop (image built; container answered 200 on `/health` and `/api/health`, running as user `app`, not root). VERIFIED by live run: the step also ran green inside GitHub Actions on `main` after the merge (`gh run view 36601325663`, backend job, step "Docker build": success) |
| 01-2 handler scan has no test | DEFERRED to Phase 4 | Line in the Phase 4 checklist: test that the first real handler resolves | NOT VERIFIED (docs only) |
| 01-3 no fallback authorization policy | DEFERRED to Phase 4 | Line in the Phase 4 checklist: fallback policy, `/health` and `/api/health` anonymous | NOT VERIFIED (docs only) |
| 01-4 Result-to-HTTP tests missing | FIXED in Step 2. The report's claim "200 with an empty body" was WRONG: the real answer is 204 with no body | `BaseControllerTests` now covers 400, 401, 403 and 404 for `Result` and `Result<T>`; new `ResultOverHttpTests` sends real requests: a value gives 200 + JSON, a null value gives 204 + no body. Decision in the "Decisions" section at the end of this report | VERIFIED by automated test (backend 165 -> 173 tests; breaking the status code on purpose made 6 fail) |
| 01-5 CI runs twice | SKIPPED on purpose | Reason logged in `docs/BACKLOG.md`: the second run tests the merge with `main`, costs only free minutes | NOT VERIFIED (a decision, nothing to run) |
| 02-1 proxy forwards spoofable headers | FIXED in Step 3 (per-IP rate limiting in Phase 4 stays a Phase 4 item) | `src/web/functions/api/[[path]].ts` drops `x-forwarded-*`, `forwarded`, `x-real-ip`, `true-client-ip`, `cf-connecting-ip[v6]` from the visitor and sets `X-Forwarded-For` from `cf-connecting-ip` (500 if it is missing). Reasons and Cloudflare-doc sources in the "Decisions" section at the end of this report; the `ARCHITECTURE.md` trap is rewritten | VERIFIED by automated test and VERIFIED by live run (`wrangler pages dev` with an echo server as the API: a spoofed `X-Forwarded-For`, `-Host`, `-Proto`, `-Port`, `Forwarded`, `X-Real-IP`, `True-Client-IP` all vanished, `X-Forwarded-For` was the address wrangler set, `127.0.0.1`). NOT VERIFIED: that the real Cloudflare edge overwrites a visitor-sent `cf-connecting-ip` (local wrangler honoured it), and what Render's load balancer does to `X-Forwarded-For`; both are Phase 4/5 lines |
| 02-2 proxy does not enforce `/api` | FIXED in Step 3 | After building the upstream URL the proxy answers 400 unless it is on the API's own origin and the path starts with `/api/`; `API_ORIGIN` must be https except `localhost`, `127.0.0.1`, `[::1]` (new clear 500 message) | VERIFIED by automated test (dot-segment, backslash, `//`, `/health`, `/api`, `/apis/...`; a `//` inside `/api/` stays on the API host). Live run: `/api/%2e%2e/health` and `//evil.example/x` never reach the function under wrangler (the app page is served, 200), so the guard is a second line of defence, tested only by unit tests |
| 02-3 no handling when the API is down | FIXED in Step 3 | A failed upstream fetch answers 502 with a plain-text body and logs the cause with `console.error`. No timeout on purpose (Render's cold start is about a minute). `errorMessageKey` in `src/web/src/core/api/errors.ts` maps status 502 to the "Can't reach the server" message | VERIFIED by automated test (proxy 502, and the 502 mapping in `errors.test.ts`) and VERIFIED by live run (`API_ORIGIN` pointing at a stopped server answered 502 with the text body). NOT VERIFIED: what Cloudflare's own limits do to a very long Render wake-up, and the message on screen in a browser |
| 02-4 no error page or startup fallback | FIXED in Step 4 | All routes sit under one `errorElement` (`src/core/router/RouteError.tsx`, built from `KvitError`, reusing `errors.generic` and `common.retry`, so no new Macedonian text). `main.tsx` catches a failed `startI18n()` and shows a plain two-language fallback (`src/core/startup/renderStartupFailure.ts`), then rethrows | VERIFIED by automated test (a real route crashing while rendering, in EN and MK; the fallback function) and VERIFIED by live run (the fallback drawn in Chrome at 360 px). NOT VERIFIED: the error page and its reload button in a real browser, `main.tsx`'s catch with a real i18n failure, the reload buttons |
| 02-5 unknown error codes vanish | FIXED in Step 4 (log); the "every code has a key" check stays DEFERRED to Phase 8 | `errorMessageKey` writes `No translation key is mapped for API error code "X"` to `console.error` | VERIFIED by automated test |
| 02-6 failed health ping invisible | FIXED in Step 4 | `useApiHealth` logs `The health ping failed` with the error once retries are used up. No screen change | VERIFIED by automated test |
| 02-7 language switching loose ends | FIXED in Step 4 | `useLanguage` saves only after `i18n.changeLanguage` succeeded; `LanguageSwitch` catches a rejection, logs it and shows `toast.error(t('errors.generic'))`. `saveLanguage` keeps its `console.warn` on purpose (language changes, isn't remembered) | VERIFIED by automated test (order, rejection, blocked storage, the toast). VERIFIED by live run: switching EN to MK in Chrome changed `<html lang>` and saved `mk`. NOT VERIFIED: the error toast in a real browser |
| 02-8 shadcn alias and `mk-MK` wording | FIXED (wording in Step 1, alias in Step 4) | New `src/shared/utils/cn.ts` (`export { cn } from 'cn'`), the file the `utils` alias in `components.json` already named; `components.json` unchanged. Reasons and rejected options in the "Decisions" section at the end of this report | VERIFIED by automated test (the alias target exists; the file exports a working `cn`; the test fails when the file is removed). VERIFIED by live run: read-only `shadcn info` resolves `utils` to that file. NOT VERIFIED: `shadcn add` (not run on purpose), so the exact import it writes is reasoned from its code |
| 02-9 CSRF, cookie flags | DEFERRED to Phase 4 | The Phase 4 login-cookie line now says `SameSite=Lax` (or `Strict`), `Secure`, and why | NOT VERIFIED (docs only) |
| Proxy test gaps | FIXED in Step 3 | `test/functions/proxyToApi.test.ts` (8 -> 27 tests): spoofed forwarding headers, dot-segment and `//` paths, upstream network failure, DELETE and PUT with no body (found a small fix: a request with no body is now forwarded with no body), the `onRequest` wiring, https rule, missing `cf-connecting-ip` | VERIFIED by automated test (the new tests failed 16 of 27 before the fix, then passed) |
| `START-HERE-PROMPT.md` in a public repo | NOT IN THIS WORK: Filip's decision | Left alone | NOT VERIFIED |

## Decisions and rejected alternatives for the fixes (moved word for word from DECISIONS.md on 2026-09-29)

### 2026-09-29: Fixes for the Phase 1 + 2 code review
Branch `fix/review-phase-01-02`, done in four steps, each committed by Filip. Findings: `reports/2026-09-29-code-review-phase-01-02.md`; the per-finding result is in its "Fix status" table.

**Step 1 (docs only):**
- **Findings 01-2, 01-3 and 02-9 are Phase 4 items, not fixed now.** They can't be done or tested before Phase 4 has something to test: the Scrutor scan finds no handler until the first one exists (01-2); a fallback authorization policy needs an auth scheme (01-3); the auth cookie doesn't exist yet (02-9). Each is now a line in the Phase 4 checklist in `ROADMAP.md`, worded so that it names its test. *Rejected:* building a fake auth scheme or a fake handler now just to have something to test, which would be thrown away in Phase 4.
- **Two more Phase 4 lines come from the review's fixes:** every new project's `.csproj` goes into the Dockerfile's `COPY` lines (01-1), and per-IP rate limiting reads the visitor address the proxy forwards (02-1; the header is chosen in Step 3).
- **Finding 02-5's "every error code has a translation key" check goes to Phase 8,** as a roadmap line. `ResultCodes` is C# and the frontend is TypeScript, so a test needs a bridge between them, and the list of codes the frontend can see is still tiny until Phase 8's expenses. The "how" is left to that phase and gets recorded then. Step 4 only logs an unmapped code.
- **Finding 01-5 (CI runs twice on a PR branch) is skipped on purpose,** see `BACKLOG.md`. *Why:* the second run tests the merge with `main` and only costs free minutes; limiting `push` to `main` would leave a branch without a pull request unchecked.
- **`formatMoney` passes the app language (`mk` / `en`) to `Intl`, not `mk-MK`** (checked in `src/web/src/shared/utils/formatMoney.ts`: `new Intl.NumberFormat(language, ...)`). `ARCHITECTURE.md` and the Phase 2 line in `ROADMAP.md` said `mk-MK`; both now match the code. The code stays as it is: the two languages are the only ones the app supports, and the number shapes were checked by test.

**Step 2 (backend, findings 01-1 and 01-4):**
- **01-1: the backend CI job now runs `docker build -f src/api/Dockerfile .` after the tests** (last step of the job, so a failing test stops it earlier and cheaper). GitHub's `ubuntu-latest` runners have Docker installed, so no setup step is needed. The image is only built in CI, not pushed or run (run once by hand on 2026-09-29: `/health` and `/api/health` answered 200 as a non-root user). *Rejected:* a separate CI job (it would restore and build twice for no gain), and running the container and calling `/health` in CI (Phase 1 already proved that by hand; the build is what breaks when a `.csproj` is missing).
- **01-4: the review's claim was wrong.** A successful `Result<T>` holding null does **not** answer "200 with an empty body". Through a real HTTP request (a test controller added to the app in `ResultOverHttpTests`) it answers **204 No Content, no body, no Content-Type.** ASP.NET's default null-to-204 output formatter does this (VERIFIED by automated test).
- **Decision: keep the 204 and make the test say so.** The frontend `apiClient` already turns a 204 into `undefined` (`apiClient.test.ts`, "returns undefined for 204 No Content"), so it can parse the answer. *Rule that follows:* a frontend service that expects an object must not be given a null success; a handler that finds nothing returns `Result.NotFound<T>(...)` (404 with an error code), never `Result.Ok<T>(null)`. *Rejected:* making `Result.Ok<T>(null)` throw (it changes the Phase 1 `Result` for a case that can't hurt the client), and switching the null formatter off so null answers 200 `null` (a non-default setting every future reader would have to know about).
- **Side finding while running it: a `Result<string>` answers `text/plain` with no quotes,** not JSON, because ASP.NET's string formatter runs first (seen in a probe run). `apiClient` returns it as a string, so nothing breaks, but a controller should wrap a string in a small record when the frontend expects JSON. Not changed, only noted.
- The 400, 401 and 403 branches of `BaseController` are now tested for both `Result` and `Result<T>` (the 404 case that already existed became a `[Theory]` with them). A deliberate break (always answering 404) made 6 of those tests fail, then was undone.

**Step 3 (Cloudflare proxy, findings 02-1, 02-2, 02-3 and the proxy tests):**
- **02-1: the visitor address.** The proxy drops `x-forwarded-for`, `x-forwarded-host`, `x-forwarded-port`, `x-forwarded-proto`, `forwarded`, `x-real-ip`, `true-client-ip`, `cf-connecting-ip` and `cf-connecting-ipv6` from what the browser sent, then sets `X-Forwarded-For` to the value of `cf-connecting-ip`. *Why this header:* Cloudflare's docs say `CF-Connecting-IP` "provides the client IP address connecting to Cloudflare to the origin" and holds one address, while `X-Forwarded-For` is appended to (so a visitor's value stays in it) and the docs recommend `CF-Connecting-IP` instead. `X-Forwarded-For` is the name ASP.NET's forwarded-headers middleware understands. *Why `x-real-ip` is dropped too:* the docs say that on a Worker's outgoing request `CF-Connecting-IP` reflects `x-real-ip`, and that `x-real-ip` can be altered, so a visitor-sent one must not get through. *Why `x-forwarded-proto`, `-host` and `-port` are dropped:* the API must see the values Render's own load balancer sets, not ones the visitor typed. *Rejected:* forwarding the visitor's headers and appending (a visitor could still put a fake address first), and having the API read `CF-Connecting-IP` directly (Cloudflare does add it on the outgoing request, but the middleware speaks `X-Forwarded-For`).
- **If `cf-connecting-ip` is missing, the proxy answers 500** ("the request has no cf-connecting-ip header, so the visitor address is unknown"), the same style as a bad `API_ORIGIN`. *Why:* Cloudflare always adds it in production; a missing one means a broken setup, and forwarding without it would make the Phase 4 rate limiter count the wrong address without anyone noticing. *Rejected:* silently forwarding without `X-Forwarded-For`. Local `wrangler pages dev` sets it to `127.0.0.1`, so development still works (VERIFIED by live run).
- **What is NOT verified for 02-1:** `wrangler pages dev` honours a client-sent `cf-connecting-ip` (it did in the live run), so this run can't prove that the real Cloudflare overwrites one. The check on the deployed site is a Phase 5 checklist line, and what Render's load balancer does to `X-Forwarded-For` is an open point in the `ARCHITECTURE.md` trap, together with direct requests to `onrender.com`.
- **02-2: only `/api/` paths on the API host are forwarded.** After the upstream URL is built, anything whose origin differs from `API_ORIGIN`, or whose path doesn't start with `/api/`, is answered **400** ("Kvit proxy only forwards paths that start with /api/") before `fetch` is called. This covers `/api/%2e%2e/health`, `/api/../health`, backslash forms, `//evil.example/x`, `/health`, `/api` and `/apis/...`. A double slash *inside* an `/api/` path stays on the API host and is forwarded. *Rejected:* 404 (it isn't a missing thing, it is a path the proxy never forwards). *Live result:* under `wrangler pages dev`, `/api/%2e%2e/health` and `//evil.example/x` never reach the function at all; the static-file fallback answers 200 with the app's `index.html` (harmless). So the guard is a second line of defence, VERIFIED by automated test only.
- **`API_ORIGIN` must be https, except for `localhost`, `127.0.0.1` and `[::1]`.** Same clear 500 as before, with a new message: "API_ORIGIN must be an https origin like https://kvit-mk-api.onrender.com (http is only allowed for localhost), got ...". Otherwise a typed `http://` would send cookies and passwords in plain text between Cloudflare and Render.
- **02-3: a failed upstream fetch answers 502** (text body: "Kvit proxy could not reach the API: the request to the API server failed"), and the cause goes to `console.error` (Cloudflare's logs). The body doesn't name the API address. **No timeout on purpose:** Render's free service takes about a minute to wake, and a proxy timeout would cut that wake-up off, so the first visitor of the day would get an error on every try. *Rejected:* 504 (that means "took too long", and nothing here times out), and a timeout of any length. Cloudflare's own limits on how long a Function may wait still apply; what they do to a very long wake-up is NOT VERIFIED.
- **The frontend shows "Can't reach the server" for that 502:** `errorMessageKey` in `src/web/src/core/api/errors.ts` now returns `errors.network` for HTTP status 502 as well as for a `fetch` that never got an answer (`httpStatus === null`). *Why by status:* the proxy's 502 is plain text, so there is no error code to look up. The same message is right for a 502 from Render's own gateway. `apiClient.ts` itself is unchanged.
- **A request with no body is forwarded with no body** (`request.body === null` gives `null`) instead of an empty buffer. DELETE and PUT without a body are tested. The upstream server still saw `content-length: 0` on a DELETE in the live run; the runtime adds that, and it is harmless.

**Step 4 (frontend, findings 02-4 to 02-8):**
- **New test tools (dev only): `jsdom`, `@testing-library/react` and its required partner `@testing-library/dom`** (all MIT, free). *Why:* an error page, a toast and a hook can only be tested by really drawing them, and Vitest's default (plain Node) has no page to draw on. `vite.config.ts` now sets `test.environment: 'jsdom'` for every test file, and `src/test/setup.ts` cleans up after each test (Testing Library only does it by itself when Vitest's global functions are on, and they are off here). All 72 older tests, the proxy tests included, still passed unchanged in jsdom. *Rejected:* `react-dom/server` (an error page can't be caught while drawing on the server), a `// @vitest-environment` line at the top of each test file (the no-comments rule), and testing only helper functions and not the screens.
- **02-4: the error page.** All routes now sit under one route with an `errorElement` (`RouteError`), so a crash on any screen, or in `useLanguage` or `formatMoney`, shows `KvitError` instead of React Router's English-only default page. It reuses the existing keys `errors.generic` and `common.retry`, so **no new Macedonian text was needed**. The button reloads the page; the crash is written to `console.error` ("A screen crashed while rendering"). *Rejected:* a new, more specific key ("this screen crashed"): it needs new Macedonian wording, which is Filip's call. *Tested with:* the real route table, with `useApiHealth` made to throw on `/welcome`, in English and Macedonian.
- **02-4: the start-up fallback.** `main.tsx` catches a failed `startI18n()`, calls `renderStartupFailure` (`src/core/startup/`) and rethrows, so the console still shows the real error. The fallback is built by hand with plain DOM calls and shows **both** languages next to each other, taking the words from `en.json` and `mk.json` directly (importing the two files needs no i18next), plus a "Try again / Обиди се повторно" button that reloads. *Why both languages:* the thing that failed is the language setup, so it can't pick one. *Rejected:* English only (the app's users are mostly Macedonian). `main.tsx` itself is not covered by a test (top-level `await`); the fallback function is, and a screenshot at 360 px was taken by calling it in the dev server.
- **02-5: an unmapped API error code is logged.** `errorMessageKey` writes `No translation key is mapped for API error code "X"` plus the error to `console.error`, and still answers `errors.generic`. It logs on every call, so a screen that re-renders repeats the line; that is fine for a developer warning. An answer with no error code at all logs nothing. The "every code has a key" test stays in Phase 8.
- **02-6: a failed health ping is logged** in `useApiHealth` with `console.error("The health ping failed", error)`, once, after TanStack Query has used up its retries (its default is 3, a few seconds). No screen change. *Rejected:* logging inside the query function (one line per retry).
- **02-7: language switching.** `useLanguage.changeLanguage` now saves the choice only **after** `i18n.changeLanguage` has succeeded (so a failed change is never remembered) and passes the failure on. `LanguageSwitch` catches it, writes `console.error` and shows `toast.error(t('errors.generic'))`, reusing the existing key. **Deliberate: `saveLanguage` still only does `console.warn` when the browser refuses `localStorage`** (a private window, blocked site data). The language still changes on screen; it just isn't remembered for next time. There is nothing the user can fix, and nothing on screen is wrong, so no toast. Covered by a test.
- **02-8: the shadcn `utils` alias.** `cn` is really imported from the npm package `cn` (`KvitButton.tsx`, `ui/button.tsx`), and `components.json` pointed the `utils` alias at `@/shared/utils/cn`, a file that didn't exist. From shadcn's own CLI code (`node_modules/shadcn/dist`): the alias must resolve through `tsconfig` paths (else "Could not resolve the following aliases"), and when `shadcn add` copies a component it rewrites that component's `@/lib/utils` import to the alias string. So the alias has to be a real file that exports `cn`. **Created `src/shared/utils/cn.ts` (`export { cn } from 'cn'`)**, the same shape as shadcn's own migration of `lib/utils` to the `cn` package, and left `components.json` alone. `shadcn info` (read-only) now resolves `utils` to that file. *Rejected:* setting the alias to `cn` (a package name isn't a `tsconfig` path, so the CLI would refuse it; reasoned from its code, not run), and changing `components.json` to a path that does not exist either. The existing `from 'cn'` imports are left as they are. A test checks that the alias in `components.json` points at a file that exists. `shadcn add` itself was not run.

