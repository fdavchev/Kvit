# Prompt for Phase 9: Balances + settle up

**When to use:** after Phase 8b is merged to `main`. Start a new branch from `main`
(`feat/09-settle-up`), open Claude Code in the Kvit folder and paste everything between
the two lines below.

---

Filip wants to run **Phase 9: Balances + settle up** (`feat/09-settle-up`).

**Start by reading:** `docs/STATUS.md`, `docs/ROADMAP.md` (Phase 9), `docs/DECISIONS.md`
(the "Settle-up rule" entry, the settlement and zero-balance rules, the Phase 7 roles
line, the Phase 8 rules that stay), `docs/ARCHITECTURE.md` (the headings, then Parts 1, 3, 4
for the backend and 2, 3, 4 for the frontend), `docs/DATA-MODEL.md` (the settlements table
and the money rules), `docs/SCREENS.md` (screens 11 and 12, the "Needs you" item, the
Balances tab), and `reports/2026-09-29-phase-03-money-core.md` (the `Balances.Calculate`
follow-up).

**Goal (from the ROADMAP):**
1. Balances per currency (MKD and EUR are never mixed) and "who pays whom".
2. Pass `Balances.Calculate` every member the group has ever had, **including removed and
   left members** (`removed_at` set), in joining order; otherwise it throws by design.
3. Settlements: record, confirm, reject, cancel, delete; the owner acts for plain names.
   A settlement starts **pending** when the payer records it and only changes balances once
   the receiver confirms; when the receiver records it ("Marko paid me") it counts at once.
4. Zero-balance rules for leave, remove and delete group (the owner cannot remove a member
   or delete a group while balances are not zero).
5. Screens 11 and 12, and the "Needs you" items for pending payments.
6. The activity feed needs a sentence for every new settlement event type (an event type
   with no sentence must still fail with a clear error naming the type).

**How to work (propose first, wait for Filip's OK, then ask before each coder run):**
- Split it into small steps (suggested order: backend balances including left members, then
  settlement commands and rules, then the zero-balance checks, then the frontend screens).
  Give a plan with the tap counts for each screen and wait for Filip's go.
- The frontend must work with the saved-data cache from Phase 8b: new query keys follow the
  existing `['groups', id, ...]` pattern, and every settlement change invalidates the
  balances and the expense list for that group. Saving still waits for the server.
- Money is integer minor units. Never `decimal` or `double`.
- `client_request_id` duplicate protection for settlements (the column already exists), also
  for two requests at the same moment, as done for expenses in Phase 8.
- Tests: backend xUnit (Docker Desktop must be open for Testcontainers), frontend Vitest.
  Ask Filip to start Docker if it is not running; do not work around it.
- Remind Filip in "your part" to run `dotnet ef database update` whenever a step adds a
  migration; nothing applies it locally.

**Working rules (repeat them to subagents):** start every reply with "Filip"; propose then
wait; one step at a time, each step ending with "your part" (Docker, terminal, manual
things); no code comments; commits are one subject line with no body; Filip makes every
commit and merges the PR himself; ask before every coder run; the tester may run inside an
approved plan; give subagents the memory folder path
`C:\Users\Davchev\.claude\projects\c--Users-Davchev-Projects-Kvit\memory\`; label every
claim as VERIFIED by automated test, VERIFIED by live run, or NOT VERIFIED; log decisions
in `docs/DECISIONS.md` as you go; update `docs/ROADMAP.md` and `docs/STATUS.md`; when Filip
cannot check something himself, run a real-browser check with Playwright against the
local stack (`.\scripts\start-local.ps1 -NoBrowser`, then `-Stop`).

---
