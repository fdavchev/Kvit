# Research: competitor lessons + stack check (2026-09-24)

Done by the `researcher` subagent (Opus) with web search. All pages checked on 2026-09-24.
Claims with a source link are **VERIFIED by live web check** on that date. Anything marked **NOT VERIFIED** couldn't be confirmed from a primary source. Anything marked "suggestion" is the researcher's judgement.

## Short answer
- **No bank card needed:** Render, Neon and Cloudflare all say "no credit card required". The Neon statement is in its official FAQ. For Render it's Render's own article, and for Cloudflare third-party sites.
- **Render keep-alive pings:** its docs neither allow nor forbid them (NOT VERIFIED). The 750 free hours a month are enough for **one** service running all month. Waking from sleep takes about 1 minute.
- **Trap found: logins would break.** Render's free filesystem is wiped on every restart or redeploy. ASP.NET Core stores the keys that sign logins (Data Protection keys) on that filesystem, so **every user would be logged out each time the server sleeps**. Fix: store the keys in Postgres (`PersistKeysToDbContext`).
- **Logging in across two domains:** Safari blocks cookies between different sites (`kvit.pages.dev` vs `kvit.onrender.com`). Best fix: a free **Cloudflare Pages Function** that forwards `/api/*` to Render, so the browser only ever talks to one site and a safe login cookie works. Fallback: bearer tokens.
- **Versions:** .NET 10 is LTS (supported until 2028-11-14), React 19.3, Vite 8.3, TypeScript 7.0. TS 7 has tooling caveats, so TS 6.x may be safer with ESLint.
- **Exchange rate:** the National Bank (NBRM) has a free JSON service with no key. EUR was **61.561 MKD** on 2026-09-24. The denar is de facto pegged to the euro.
- **Translations:** react-i18next. Macedonian plurals treat 21, 31, 101 like 1.

---

## Part A: What competitors teach us

**What users complain about:**
- **Splitwise:** a daily limit on adding expenses in the free plan ([Splitwise](https://feedback.splitwise.com/knowledgebase/articles/2010350-why-am-i-seeing-an-expense-limit)). Third parties say 3–5 a day plus a 10-second wait between entries (NOT VERIFIED by Splitwise).
- **Tricount:** reports of lost data and missing trips after version 8.0 ([Tricount FAQ](https://together.bunq.com/d/59049-tricount-faqs)). Lesson: reliability and a change history matter.
- **Settle Up:** an ad after every third expense. Recurring expenses and export are paid features ([areweeven](https://www.areweeven.com/blog/free-expense-splitting-apps)).
- **YNAB:** about $109 a year, and users say it takes 2–3 months before it "clicks" ([envelopebudgeting](https://envelopebudgeting.com/articles/ynab-review)). Lesson: keep budgeting simple.
- **Wallet by BudgetBakers:** some reports of balances that don't match the transactions ([getfinny](https://getfinny.app/blog/wallet-budgetbakers-review-2026)).

**What users praise for speed:**
- **Splid:** no sign-up, and a group is ready "within seconds" ([App Store](https://apps.apple.com/us/app/splid-split-group-bills/id991473495)).
- **Spliit:** open source, no account needed, you just share a link ([GitHub](https://github.com/spliit-app/spliit)). The closest existing example of Kvit.
- **Tricount:** "Share your tricount link… Everyone can add their own expenses" ([tricount.com](https://tricount.com/)).
- **Money Manager:** tap a category, type the amount, save. About 12 seconds (third-party review).

### Suggestions for Kvit
| # | Suggestion | First version or later |
|---|---|---|
| 1 | **Join by link without an account.** Tap your name in the member list and the device remembers you. An account is optional. | First version (needs a design decision) |
| 2 | **One-screen quick add.** The amount field opens with the number keypad. Defaults: paid by me, split equally, today, the last currency used. The description is optional. Target: 3 taps, under 10 seconds. | First version |
| 3 | **Open straight into the last group** with a "+" button. | First version |
| 4 | **No limits, no ads, no waiting.** | First version |
| 5 | **One-tap settle up.** Each suggested payment has a "Mark paid" button with the amount filled in. | First version |
| 6 | **Simplify debts separately for each currency.** | First version |
| 7 | **Instant screen updates plus "Undo"** instead of "Are you sure?" dialogs. | First version |
| 8 | **Hide the server wake-up.** Wake the API as soon as the page opens, and have UptimeRobot ping a light route that doesn't touch the database. | First version |
| 9 | **Personal spending: category grid first, then the amount, then save.** | First version |
| 10 | **Your share of group expenses goes into your budget automatically** (you can turn it off). | First version (already decided) |
| 11 | **Simple monthly limits with a progress bar.** No complicated budgeting methods. | First version |
| 12 | **Basic change history, plus a nightly database backup.** | First version: basic. Later: a full history screen. |
| 13 | **Share balances to Viber or WhatsApp** through the phone's share menu. | Later |
| 14 | **Recurring expenses** | Later |
| 15 | **Receipt QR scanning, and offline entry that syncs later** | Later |
| 16 | **CSV export** | Later |

---

## Part B: Stack facts

### Render free web service
- **Hours:** 750 free instance hours a month. The service sleeps after 15 minutes without traffic and takes about 1 minute to wake ([Render](https://render.com/docs/free)).
- **Card:** "No credit card is required" ([Render article](https://render.com/articles/platforms-with-a-real-free-tier-for-developers-in-2026)).
- **Keep-alive pings:** no rule either way was found (NOT VERIFIED). Render answers `robots.txt` itself, so pinging it does NOT wake the app. Pinging all month uses about 744 of the 750 hours, so it only works for one service.
- **Files are wiped** on restart or redeploy, and disks are paid only ([Render disks](https://render.com/docs/disks)). So the login keys must be stored in the database ([Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0)). Microsoft notes you then need to add encryption for those keys yourself. Applying this to Render is the researcher's conclusion from the two docs and hasn't been tested.

### Neon and Cloudflare sign-up
- **Neon:** "no credit card required" ([Neon FAQ](https://neon.com/faqs/managed-postgres-databases-free-tier)). 0.5 GB, 100 compute-hours a month, and it sleeps after 5 minutes idle ([plans](https://neon.com/docs/introduction/plans)).
- **Cloudflare:** "no card" comes only from third-party sites (NOT VERIFIED on a Cloudflare page).

### Versions
- **.NET 10:** LTS, released 2025-11-11, supported until **2028-11-14**, latest patch 10.0.12 ([Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)).
- **React:** 19.3 ([react.dev](https://react.dev/versions)).
- **Vite:** 8.3.1, needs Node 20.19+ or 22.12+. Start with `npm create vite@latest kvit-web -- --template react-ts` ([vite.dev](https://vite.dev/guide/)).
- **TypeScript:** 7.0 is out (2026-07-08) but has no stable API for tools yet. ESLint's TypeScript plugin needs a TS 6 package. The TS team suggests waiting for 7.1 for tooling that uses the API ([TS blog](https://devblogs.microsoft.com/typescript/announcing-typescript-7-0/)). Which TS version the Vite template installs is NOT VERIFIED.

### Logging in when the site and API are on different domains
- **Safari** blocks cross-site cookies ([WebKit](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/)). Chrome leaves them to the user's settings ([Google](https://privacysandbox.google.com/blog/privacy-sandbox-next-steps)). So they're unreliable, especially on iPhones.
- **Microsoft's advice** for browser apps using `MapIdentityApi` is cookies (`/login?useCookies=true`). Tokens exist for clients that can't use cookies ([Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)).
- **Best option: a Cloudflare Pages Function proxy.** It forwards `/api/*` to Render, so the login cookie is first-party and no CORS setup is needed ([Cloudflare](https://developers.cloudflare.com/pages/functions/advanced-mode/)). The free plan allows 100,000 requests a day and 10 ms of CPU per request, and waiting for Render doesn't count toward the CPU time ([limits](https://developers.cloudflare.com/workers/platform/limits/)).
- **Fallback: bearer tokens.** The access token stays in memory and the refresh token goes in `localStorage`. That's weaker if the site ever has a script-injection bug.

### MKD/EUR exchange rate
- **NBRM web service:** free JSON at `https://www.nbrm.mk/KLServiceNOV/GetExchangeRate?StartDate=dd.MM.yyyy&EndDate=dd.MM.yyyy&format=json`, with no key ([NBRM](https://www.nbrm.mk/KLServiceNOV/EN)).
- **Live values:** 61.5337 on 2026-09-23 and 61.561 on 2026-09-24.
- **Peg:** the denar is de facto pegged to the euro ([IMF 2024](https://www.elibrary.imf.org/view/journals/002/2024/027/article-A002-en.xml)).

### Translations
- **Library:** react-i18next. Macedonian has only the "one" and "other" plural forms, and "one" covers 1, 21, 31, 101… so always pass the count ([CLDR](https://www.unicode.org/cldr/charts/latest/supplemental/language_plural_rules.html)).
- **Fonts:** pick one that has Ѓ, Ќ, Ѕ, Ј, Љ, Њ, Џ, and set `<html lang="mk">` for Macedonian letter shapes. Whether Inter or Roboto include those Macedonian shapes is NOT VERIFIED.
- **Formatting and sorting:** format money and dates with `Intl` and `mk-MK`, and sort with `localeCompare(..., 'mk')`.
