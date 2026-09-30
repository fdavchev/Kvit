# Phase 4, Step 3: rate limits, the visitor's address, the proxy secret

Date: 2026-09-30. Branch `feat/04-accounts` (PR #6, open). Tests by the `tester`, code by the `coder`, checked by Claude. Decisions: `DECISIONS.md`, top entry, "Step 3".

## Labels
VERIFIED by automated test / VERIFIED by live run: run by me, output read. REPORTED by the coder: the coder ran it, I did not repeat it. NOT VERIFIED: not run.

## What was built
- **Rate limits** (per visitor address, in memory): log-in 10 a minute, sign-up 5 per 10 minutes. The next try answers 429 with `errorCode: RATE_LIMITED` (new entry in `ResultCodes`) and a `Retry-After` header in seconds. Log-out and `me` are not limited.
- **Visitor address:** IPv6 addresses are grouped by their /64 block, an IPv4 address written in IPv6 form counts as the IPv4 address, a missing address throws (no shared "unknown" bucket). Code: `RateLimiting/VisitorAddressKey.cs`.
- **Proxy gate:** when `Proxy:SharedSecret` is set, every request without the right `X-Kvit-Proxy-Secret` gets 403 (compared in constant time), except `/health` and `/api/health`. Then ASP.NET's forwarded-headers step reads `X-Kvit-Visitor-Ip` (one entry, no known proxies) and the visitor address becomes the connection address. Production without the setting stops at start-up with a clear message; other environments run with the gate off. Code: `Settings/ProxySetting.cs`, `Proxy/ProxyGateMiddleware.cs`, `Program.cs` (order: gate, forwarded headers, routing, rate limiter, authentication, authorization, endpoints).
- **Cloudflare Function** (`src/web/functions/api/[[path]].ts`): drops any visitor-sent `x-kvit-proxy-secret` and `x-kvit-visitor-ip`, sets both from `API_PROXY_SECRET` and `cf-connecting-ip`, no longer sets `X-Forwarded-For`; a missing `API_PROXY_SECRET` answers 500 like a missing `API_ORIGIN`.
- **Docs:** `ARCHITECTURE.md` (proxy trap rewritten, rate-limit line under Controllers), `guides/free-hosting-setup.md` (proxy secret: how to make it, B2 and B3 lines), `ROADMAP.md`, `DECISIONS.md`. `.dev.vars` was already in `.gitignore` (checked with `git check-ignore`).

## Results
| Check | Result | Label |
|---|---|---|
| Tests first | 39 backend tests in the five touched classes: 24 red for the right reasons (no gate, no limiter, empty `VisitorAddressKey` shell), 15 already green (they pin today's behaviour); frontend: 9 of 35 red | VERIFIED by live run (I re-ran them before the coder started) |
| `dotnet build Kvit.slnx` | 0 warnings, 0 errors | VERIFIED by automated test |
| `dotnet build Kvit.slnx -c Release` | 0 warnings, 0 errors | REPORTED by the coder |
| `dotnet test Kvit.slnx` | **452 of 452 pass** (was 417: 35 new) | VERIFIED by automated test |
| Frontend `npm run lint`, `npm run build`, `npm test` | lint 0 warnings, build 0 type errors, **97 of 97 tests** (89 at the last check in the Phase 1 + 2 review fixes) | VERIFIED by automated test |
| Signed-in visitor gets 404 for `/scalar` and `/openapi/v1.json` in Production (your question) | passes; it already passed before the code, because those pages are not mapped in Production at all, so it guards against a future change | VERIFIED by automated test |
| Real API on Windows (Development, your local database and certificate, throwaway random secret, port 5018), called directly: `/health` 200 without the secret; log-in without or with a wrong secret 403; right secret but no visitor address 500; 10 log-ins 401 then the 11th and 12th 429 with `Retry-After: 60`, `application/problem+json`, `errorCode: RATE_LIMITED`; a fake `X-Forwarded-For` on every try did not change the counted address | as listed | VERIFIED by live run |
| Same API behind `wrangler pages dev` (bindings `API_ORIGIN`, `API_PROXY_SECRET`): `/api/health` 200; 12 log-ins, each with a forged `x-kvit-proxy-secret`, `x-kvit-visitor-ip` and `x-forwarded-for`, gave 10 × 401 then 429 (the forged values changed nothing) | as listed | VERIFIED by live run |
| Echo server in place of the API: the forwarded request carried exactly `x-kvit-proxy-secret` (the real 28-character value, not the forged one), `x-kvit-visitor-ip` (wrangler's own `127.0.0.1`, not the forged `7.7.7.7`), the unrelated `x-request-id`; no `x-forwarded-for`, `forwarded`, `x-real-ip` or `true-client-ip` | as listed | VERIFIED by live run |
| A second visitor address is still allowed while the first is refused; IPv6 addresses in one /64 share a bucket | by automated tests (`RateLimitTests`, `VisitorAddressKeyTests`). My live "other address" and "same /64" calls only returned 400 (empty body), which shows they were not refused but did not prove the counting | VERIFIED by automated test |
| The sign-up limit (5 per 10 minutes) | automated test only; no live sign-ups, so no accounts were created in your local database | VERIFIED by automated test |
| Production without `Proxy:SharedSecret` refuses to start (null, empty, blank); other environments start with the gate off | tests | VERIFIED by automated test |
| Fixed-window leases carry `RetryAfter` | read in the .NET source (`FixedWindowRateLimiter.cs`, release/10.0) and seen in the live answers | REPORTED by the coder, confirmed by live run |
| Render passes both Kvit headers through untouched; the real Cloudflare sets `cf-connecting-ip` itself (local wrangler honours a client-sent one) | not run | NOT VERIFIED until Phase 5 |
| A real phone, the real Cloudflare and Render, Neon | not run | NOT VERIFIED until Phase 5 |

## Side effects of the live run
- Everything I started (API, wrangler, workerd, the echo server) was stopped, the throwaway secret file was deleted, and no file was written inside the repository.
- Your local database was not changed: `data_protection_keys` still has only your row (id 3), `users` is empty (checked).

## Things to know
- `Retry-After` is the whole window (60 or 600 seconds), not the time left. That is what .NET reports; it may ask a visitor to wait a little longer than needed.
- The counters live in memory, so they reset when the free Render instance sleeps or restarts.
- The limiter counts every request to log-in and sign-up, including requests with a bad body.
- Every refused request with the right secret but no usable visitor address is a 500 by design: it means the proxy is broken, not that somebody attacked.

## Cost (subagent tokens as reported by the harness)
| Agent | Tokens | Tool calls | Time |
|---|---|---|---|
| tester (failing tests) | 118,493 | 56 | about 7.5 min |
| coder (implementation) | 119,036 | 61 | about 5 min |
| **Total** | **237,529** | 117 | |

(Steps 1, 2a and 2b cost about 369k, 390k and 220k to 315k.)
