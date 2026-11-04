# Phase 18: Admin statistics page (`feat/18-admin-stats`)

## Context
Last phase in the roadmap (Release 3, "polish"). `usage_events` has been recorded since Release 1 (SignedUp with Email/Google/Guest, Active once per user per UTC day, JoinedViaInvite, GroupCreated, ExpenseAdded, SettlementConfirmed, GuestUpgraded) but nothing shows them. Phase 18 gives Filip a `/admin` page with counts only (never names, amounts or budgets), per DECISIONS "2026-09-24: Admin statistics page", ARCHITECTURE (`ADMIN_STATS_VIEW`, `<RequireAdmin>`), DATA-MODEL (`roles`/`user_roles` for the admin role, Release 3).

After Phase 18: no planned phases left, only Filip's merges/real-site checks and BACKLOG.

## Filip's picks (2026-11-04)
- **Layout A:** cards with a big total and small CSS bars per period (no chart library).
- **Active people:** show both lines, "people" (different people) and "person-days"; in Day view they are equal, so only one line.
- **Entry:** a "Statistics" row in Settings only Filip sees; anyone else typing `/admin` gets the normal not-found page.
- **Default:** This month, by day. Range chips: 7 days, 30 days, This month, 12 months, All, Custom (two dates). Grouping: Day / Week (Monday start) / Month / Year.
- **Extras:** guests apart (sign-ups Email/Google/Guest, plus "guests who made an account"), all-time totals line on top, compare with the period just before ("+12% vs previous …"; hidden for All; "new" when the previous is 0). No tap-a-bar.
- **Day clock:** Europe/Skopje.
- **Filip's own use is left out** (every account in the admin role is excluded, everywhere incl. totals).
- **All-time totals = everything that ever happened** (from usage_events, deleted things included).
- **Guests in Active:** counted, with "of them guests N" (guest = `users.is_guest` today).
- **Working mode for Phase 18:** Claude plans, runs tester/coder without stopping, commits each step itself (one-line subject, no body, no Co-Authored-By, no "Generated with" line), never pushes; collects open questions and asks them all at the end. Filip pushes, opens PR, merges.
- **Branch:** wait until Filip merges Phase 17; then start from updated `main`.

## Decisions Claude makes (logged in DECISIONS under "Decided while building, Filip may change")
- Admin = Identity role `Admin` (tables already exist via Identity; add `.AddRoles<IdentityRole<Guid>>()` in `src/api/Kvit.Api/Registers/Register.Auth.cs`), permission constant `ADMIN_STATS_VIEW`. Granted only by a new script `scripts/GrantAdmin.cs` (by email, modelled on `scripts/ResetPassword.cs`, same `ConnectionStrings__KvitDatabase` trick for Neon). `GET /api/me` gets `isAdmin`.
- `GET /api/admin/stats?from&to&by` → all-time totals, per-metric range total + previous-period total + buckets. Non-admin: 403. Day grouping allowed up to 92 days. Dates not in the future. Counts in SQL with `AT TIME ZONE 'Europe/Skopje'`; person-days = distinct (user, Skopje day). Known edge: Active rows are deduped per UTC day, so a late-night Skopje visit can miss one person-day (logged, not fixed).
- Page texts in EN + MK (MK drafts listed for Filip at the end). Desktop: cards in a grid at 64rem. Skeletons while loading, glass/animation rules as in earlier phases, reduced motion respected.
- An index on `usage_events (type, occurred_at)` only if the query needs it; if so, a migration and `dotnet ef database update` goes in "your part".

## Steps (each: tester writes tests first, red for the right reason → coder → Claude runs build + all tests and reads output → commit)
0. **Start:** check `main` contains the Phase 17 merge (if not, stop and tell Filip); branch `feat/18-admin-stats`; write `docs/guides/phase-18-admin-stats-prompt.md` (this plan), DECISIONS entry, `docs/guides/phase-18-pr-text.md`, clickable HTML mockup of layout A in `docs/design/2026-11-04-phase-18/` (real colours from `src/web/src/index.css`, opened with `Start-Process` for the record); update the memory file on coder/commit rules for Phase 18. Commit.
1. **Backend, admin role:** roles registration, `ADMIN_STATS_VIEW` + `[AuthorizationFilter]` (reuse existing `Kvit.Api/Authorization`), `isAdmin` on me, `scripts/GrantAdmin.cs`, `docs/guides/` steps to make Filip admin locally and on Neon.
2. **Backend, statistics query:** handler + repository (extend `UsageEventRepository`), contracts, controller. Tests with Testcontainers: Skopje midnight edges, week Monday start, admin excluded, guests split, person-days vs people, previous period, all-time, bad ranges (400 with specific codes), non-admin 403, no polling.
3. **Frontend:** `isAdmin` in me types, `<RequireAdmin>` (not-found for others), `/admin` route, Settings row, page (range chips, grouping, totals line, cards with bars, compare line, guest lines), endpoints in `core/api/endpoints.ts`, EN/MK keys (error-code translation test covers new codes).
4. **Browser pass:** Playwright headless against `.\scripts\start-local.ps1 -NoBrowser`, 320/360/768/1280/1920, EN/MK, light/dark, reduced motion, as admin and as normal user (no row, `/admin` not found). Report `docs/reports/2026-11-xx-phase-18-browser-pass.md`. Fix findings, re-check.
5. **Close-out:** `/code-review high`, fix findings, report `docs/reports/…-phase-18-admin-stats.md` (VERIFIED/NOT VERIFIED labels, decision log), guide `docs/guides/phase-18-admin-stats.md` (plain steps for Filip incl. GrantAdmin on Neon), ROADMAP/STATUS/BACKLOG, PR text. Commit.
6. **Ask Filip** every open question collected (MK drafts, any "decided while building" items).

## Verification
`dotnet build Kvit.slnx` 0 warnings, `dotnet test Kvit.slnx` (≥5975 + new), `npm run lint` 0 warnings, `npm run build` 0 errors, `npm test` (≥12765 + new); browser pass above. Docker Desktop must be open (ask Filip, don't work around it). Nothing called done without running it.
