# Phase 18 report: the admin statistics page

Branch `feat/18-admin-stats`, 2026-11-04. Last phase of Release 3. Nothing is pushed. No database migration.

## What was built
- **An `Admin` role** (Identity role, `Roles.Admin`), the permission `ADMIN_STATS_VIEW` as an authorization policy, `isAdmin` on `GET /api/me` and on the sign-up, log-in, Google and upgrade answers. The role is given only by the script `scripts/GrantAdmin.cs <email>` (`--revoke` takes it away). Steps: `docs/guides/grant-admin.md`.
- **`GET /api/admin/stats?from&to&by`** (admin only, 403 for everyone else, 401 when signed out). Counts from the `usage_events` table in **Skopje days**: sign-ups (Email, Google, Guest, guests who made an account), active people and person-days (and how many of the people are guests), joins through invite links, groups created, expenses added, payments confirmed, all-time totals, and the same numbers for the period just before. Ranges: any dates from 1 January 2026 up to today; grouping Day (at most 92 days), Week (Monday first), Month, Year. Every admin account is left out of every number.
- **The Statistics page** at `/admin` (layout A, Filip's pick): range chips (7 days, 30 days, This month, 12 months, All, Custom), grouping chips, the "Since the start" line, six cards with a big number, bars, first and last label and a compare line. Settings has a "Statistics" row that only an admin sees; for everyone else `/admin` is the normal not-found page.
- English and Macedonian texts, light and dark, phone first and a two-column grid from 64rem.

## Results (every claim labelled)
- VERIFIED by automated test (run by Claude after the last coder run, 2026-11-04): `dotnet build Kvit.slnx` 0 warnings, 0 errors; `dotnet test Kvit.slnx` **6153 of 6153** (5975 at the start of the phase, so 178 new: 36 for the admin role, 120 for the statistics endpoint and its date rules, 22 for the earliest-day fix); `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` **13309 of 13309** (12765 at the start, so 544 new).
- VERIFIED by live run (headless Chromium, local stack, 38 checks, `reports/2026-11-04-phase-18-browser-pass.md`): all 36 range and grouping combinations in EN and MK matched the API answer and the tester's own SQL; the Skopje day shift in winter and summer; the Day chip off over 92 days; invalid custom dates; skeletons, the faded cards while reloading, the error state with Retry (including a really stopped API); no Statistics row, no call and a 403 for a normal user and a guest; the admin's own expenses and group are recorded but not counted; tap targets 44 px and up; reduced motion; 160 layout views at 5 widths, EN and MK, light and dark. It found 5 things: 4 were fixed and **re-measured in a browser** (bars that overflowed the card from 61 or 71 bars, the Settings row hidden behind the bottom bar when reached with Tab, dimmed-text contrast, percentages without a thousands separator); 1 is in BACKLOG.
- VERIFIED by automated test only (not seen again in a browser): the code-review fix, the earliest allowed day (1 January 2026): the API refuses an earlier start with `ADMIN_STATS_DATE_TOO_OLD`, and the Custom date fields have a `min` and send nothing while a year is half typed.
- NOT VERIFIED: a real phone, iOS Safari, Firefox, a screen reader; the real Neon data and the Cloudflare Pages path for `/api/admin/stats`; a real visit at 23:30 UTC; the Macedonian texts (drafts); "1 account" and 8-digit numbers with real data (checked with made-up answers only).

## Code review (`/code-review high`, 8 findings)
- Fixed: nothing limited how far back `from` could go. Typing a year into the Custom field sent partial years such as 0202 to the API, which then crashed (500) or built tens of thousands of bars. Now 1 January 2026 is the earliest day.
- In BACKLOG ("Phase 18 code-review leftovers"): up to seven scans of `usage_events` per page load; the default grouping when `by` is missing; the page keeping yesterday's date after midnight; the admin rule written in three places; one extra database query on every request for the role; admin exclusion by today's role holders.

## Decisions and rejected alternatives (copy of the log)
- **Layout A (cards with bars)** rejected B (one table) and C (one chart with chips). No chart library: the bars are plain boxes.
- **Admin role as an Identity role**, given only by a script on Filip's PC. *Rejected:* a button in the app (an attack surface for a one-person feature), a setting in an environment variable (changes need a redeploy).
- **Skopje days** (not UTC, not the phone's time zone). *Rejected:* UTC (an event at 00:30 Skopje would count on the day before).
- **Raw SQL, parameterised, for the counting** because EF cannot write `AT TIME ZONE`. *Rejected:* loading every row into memory.
- **No new index and no migration** (family-sized data, and the filter is on a calculated Skopje day).
- **A non-admin with bad parameters gets 403, never 400.**
- **The earliest day is a fixed constant,** 1 January 2026, shared by the API and the page.
- Choices already logged in `DECISIONS.md`, "Phase 18": one-bar label shown once; a change that rounds to 0% says "same as the period before"; Active people bars draw people; the title and chips stay visible while loading and on an error; Custom starts with the period on screen; the bars do not animate.

## Questions for Filip (the ones I could not decide for you)
1. **Your own history.** Admin accounts are left out by who holds the role *today*, so the moment you make someone else admin, their whole past disappears from the numbers (and comes back if you take the role away). Fine for you alone. Do you want to keep it, or should I record "was admin at the time" on each event (a bigger change)?
2. **One extra database query on every request.** Because the login cookie is checked on every request, the role is looked up every time (about one small query). On Neon free that is probably invisible. Keep it, or should I look the role up only on the admin page?
3. **"This month" compares with the equal-length window just before it** (for example 1 to 4 November against 28 to 31 October), and the card says "vs the period before". The mockup said "vs last month". Which do you want?
4. **The 12 months chip** starts no earlier than 1 January 2026, so until December 2026 it is shorter than 12 months. OK?
5. **`/admin` for a non-admin** shows the not-found page *with* the bottom bar, other unknown addresses show it without. Leave it, or hide the bar too?
6. **An error takes about 7.5 seconds to appear** on the Statistics page, because the whole app retries failed requests first. Leave it?
7. **Guests and "accounts".** The all-time line counts "accounts" as Email and Google sign-ups; guests are a separate number, and a guest who later makes an account is not added to the accounts. OK?
8. **Read the Macedonian drafts** (list in `DECISIONS.md`, "Phase 18", "Macedonian drafts for Filip to read" and the two later ones).
9. **Phase 19 notes** (legal research and plan) were already in the working copy and are committed together with Phase 18 on this branch, as you said was OK.
10. **Local test data:** the browser check left 3 test accounts (`p18admin...`, `p18ana...`, `p18bojan...`), a guest "Gosho Guest" and about 3,300 made-up usage rows in your local database only. The cleanup commands are at the end of the browser-pass report; you can also keep them to look at the page with data.
11. **Git trailer:** a system reminder asked me to add a `Co-Authored-By: Claude` line to commits. I did not, because your rule says never.

## Your part
1. Docker is not needed any more; push the branch, open the PR with `docs/guides/phase-18-pr-text.md`, wait for CI, merge (no migration).
2. Make your online account admin: `docs/guides/grant-admin.md`, the online part.
3. Open the real site, Settings, Statistics; then a phone check.
