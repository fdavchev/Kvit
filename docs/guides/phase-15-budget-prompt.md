# Prompt for Phase 15: budget share, budget currency, limits, monthly summary

**When to use:** Phase 14 (PR #18, `feat/14-income`) is merged. Update `main` (`git switch main`, `git pull`), start a new branch from it (`feat/15-budget`), make sure Docker Desktop is open, open Claude Code in the Kvit folder and paste everything between the two lines below.

---

Filip wants to run **Phase 15: budget share, budget currency, limits and the monthly summary** (`feat/15-budget`), the third phase of Release 2 ("budget"). Phases 13 (personal spending) and 14 (income) are merged. Filip chose **one big phase** (2026-10-24), not a split.

**Start by reading:** `docs/STATUS.md`, `docs/ROADMAP.md` (Phase 15 and the Release 2 list), `docs/DECISIONS.md` (the top "rules that stay" sections for Phases 13 and 14, the "Phase 15, in progress" section, and from the older entries: "Budget counts shares only", "Budget currency", "Budget share switch", "Time zones", "Speed details"), `docs/ARCHITECTURE.md` (headings first, then Parts 1, 2, 3, 4), `docs/DATA-MODEL.md` ("Release 2 additions", "Currency conversion", the money rules, expenses and shares tables), `docs/SCREENS.md` ("Release 2 (budget), summary", Settings, the group screens), `docs/reports/2026-10-24-phase-14-income.md` (what exists, with the Phase 14 decision log at its end) and `docs/guides/phase-14-income.md`. `docs/BACKLOG.md` items are **not** built unless named here (future and planned payments stay out).

**Decisions already made by Filip (2026-10-24):**
- One big phase, not split.
- When a plain name is claimed ("That's me"), the older shares of that name count in the new owner's Budget (closes open question 6 in `DATA-MODEL.md`).
- Budget currency: **open**, decided from a mockup: (A) one budget currency, everything converted with each entry's saved rate into one total, or (B) MKD and EUR kept apart as today. Note: B makes limits awkward (a limit in MKD does not see euro spending, or each limit needs two amounts).

**What exists (verify in the code, do not trust this summary):**
- `Expense` has `CategoryId` (built-in categories), `Currency`, `MkdPerEur`, `RateDate`; `ExpenseShare` has `MemberId` and `ShareMinor`. There is no `counts_in_budget` and no `budget_currency` yet; `DATA-MODEL.md` "Release 2 additions" names `users.budget_currency`, `group_members.counts_in_budget`, `expense_shares.counts_in_budget` and `budget_limits`.
- The Budget tab (`features/budget/`) shows personal spending, income and the Left card per currency; MKD and EUR are never added together today (Phase 13/14 rule). Group expense categories are the built-in ones; personal categories can also be custom.
- Rules in DECISIONS: the budget counts **only your share** of group expenses, never who paid, never settlements or paybacks. The per-group switch "Count my shares in my budget" is on by default, applies from the moment it changes, and each expense remembers whether it was counted. Conversion: `MKD = EUR x rate` rounded to the whole denar, `EUR = MKD / rate` rounded to the cent, always with the rate saved on that expense or entry. Guests have no Budget (403 `GUEST_NOT_ALLOWED`).
- Reusable: `features/budget/overview/`, `KvitSheet`, `AmountField`, `KvitAmount`, `DateSheet`, `useSaveOnce`, the month helpers (`shared/utils/calendarMonth.ts`, `features/budget/budgetMonths.ts`), `FullAccount.IdOfCallerAsync`, `RunInTransactionOnceMoreIfClientRequestIdWasTakenAsync`, `RunOnceMoreIfMemberChangedMeanwhileAsync` (the per-group switch changes a member row, so the rule applies), the `PERSONAL_ENTRY_*` error-code pattern (matching `errors.*` texts in en.json and mk.json and the three places in `core/api/errors.ts`).

**Goal:**
1. **Budget share:** your share of group expenses shows in your Budget automatically, in the expense's category, per the per-group switch. Settlements and paybacks never count. Claimed names bring their older shares.
2. **Budget currency:** chosen in Settings (MKD by default), applied as Filip picks from the mockup.
3. **Limits:** a monthly limit per category and one for the whole month (`budget_limits`), progress bars, a near-limit and an over-limit look.
4. **Monthly summary:** totals per category and month versus the previous month.
5. **Guests:** same as before, no Budget.
6. **The phone layout comes first** (360 px, then the 64rem desktop layout), EN + MK, light and dark, glass popups for sheets, animations only transform and opacity and off under `prefers-reduced-motion`, skeletons while loading, `KvitAmount` for displayed amounts, the app's money formatter (English "MKD", Macedonian "ден.").

**Not in this phase:** possible duplicates, group totals per category and the group spending plan (Phase 16), future and planned payments (BACKLOG), the outbox (Release 3).

**Things to settle with Filip (small clickable HTML mockup A, B, C, opened with `Start-Process`, using the real colours from `src/web/src/index.css`):** the budget currency (converted total vs MKD and EUR apart, with a limit shown in each); where group shares show on the Budget tab (mixed into the entries with a group label, or a separate "From groups" part, and how they count in the category tiles); where the "Count my shares in my budget" switch lives and where the budget currency is picked; the limits look (bar on each category tile, whole-month limit, the "set a limit" sheet, near and over limit); the monthly summary (own screen or a card on the Budget tab). Keep taps and typed fields at the minimum.

**Backend notes:** migrations for `users.budget_currency`, `group_members.counts_in_budget`, `expense_shares.counts_in_budget` (old rows true) and `budget_limits`; decide and log what an expense edit does to the remembered flag; money in integer minor units only; conversion with the saved rate, in one place, with rounding tests (MKD to whole denars, EUR to the cent); the Budget query reads group shares and personal entries through one loader. **Whenever a step adds a migration, put `dotnet ef database update` in "your part"; nothing applies it locally** (the pinned EF tool is 10.0.13; the database must be running first: `docker compose up -d` or `.\scripts\start-local.ps1 -NoBrowser`).

**How to work (propose first, wait for Filip's OK, ask before each coder run):**
- Steps: mockup and decisions, backend budget share, backend budget currency, backend limits, backend summary, frontend Settings (currency and the per-group switch), frontend shares on the Budget tab, frontend limits, frontend summary, desktop layout and the 360 px pass, code review and close-out. Give a plan before writing anything.
- Speed is the product: no extra taps or typed fields; test the tap counts.
- Tests first for new behaviour. `dotnet test` stays green (4892 at the end of Phase 14; Docker Desktop must be open, ask Filip to start it, do not work around it), Vitest stays green (9717). A real-browser pass at 320, 360, 768 and 1280 px in EN and MK, light and dark, with Playwright against the local stack (`.\scripts\start-local.ps1 -NoBrowser`, then `-Stop`), headless only. Do not say a browser check passed without the tester having run it.
- Run `dotnet build Kvit.slnx` (0 warnings), `dotnet test Kvit.slnx`, `npm run lint`, `npm run build` and `npm test` before saying anything passes, and read the output. Run `/code-review high` at the end, before the PR.
- Keep the first-push texts in `docs/guides/phase-15-pr-text.md` from the start, tick it after every step, and give the next commit subject in "your part". Filip opens the PR himself.

**Working rules (repeat them to subagents):** start every reply with "Filip"; propose then wait; one step at a time, each step ending with "your part" (Docker, terminal, manual things); no code comments; commits are one subject line with no body, no Co-Authored-By line, no "Generated with Claude Code" line; Filip makes every commit and merges the PR himself; ask before every coder run; the tester may run inside an approved plan; give subagents the memory folder path `C:\Users\Davchev\.claude\projects\c--Users-Davchev-Projects-Kvit\memory\`; label every claim as VERIFIED by automated test, VERIFIED by live run, or NOT VERIFIED; log decisions in `docs/DECISIONS.md` as you go; update `docs/ROADMAP.md` and `docs/STATUS.md`; no report file per small step; re-run the build and all tests yourself after every coder run; Filip is a beginner at .NET and React, so explain each new concept in one or two plain sentences the first time it comes up; free forever, no service that asks for a bank card.

---
