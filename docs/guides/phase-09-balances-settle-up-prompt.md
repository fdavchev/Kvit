# Prompt for Phase 9: Balances + settle up

**When to use:** Phase 8b is merged. Start a new branch from `main` (`feat/09-settle-up`),
open Claude Code in the Kvit folder and paste everything between the two lines below.

---

Filip wants to run **Phase 9: Balances + settle up** (`feat/09-settle-up`). Phase 8b
(saved data shows instantly) is merged.

**Start by reading:** `docs/STATUS.md`, `docs/ROADMAP.md` (Phase 9), `docs/DECISIONS.md`
(the "2026-09-24: Settle-up rule" entry, the line about zero-balance checks for leave,
remove and delete, the Phase 7 roles line, the rules that stay from Phase 8 and Phase 8b),
`docs/ARCHITECTURE.md` (the headings first, then Parts 1, 3, 4 for the backend and
2, 3, 4 for the frontend), `docs/DATA-MODEL.md` (the `settlements` table, "Balances", "Who
pays whom", the money rules), `docs/SCREENS.md` (screens 11 and 12, the Balances tab, the
"Needs you" item), and `docs/reports/2026-09-29-phase-03-money-core.md` (the
`Balances.Calculate` follow-up).

**What the docs already say (check them, do not trust this summary):**
- `balance = paid − owed + settlements sent − settlements received`, confirmed settlements
  only, deleted rows ignored; balances are never stored, always added up. Positive means
  others owe them. MKD and EUR are never mixed. Invariant: balances in one currency in a
  group add up to exactly 0.
- "Who pays whom": repeatedly match the biggest debtor with the biggest creditor, at most
  (people − 1) payments, ties broken by joining order.
- `settlements`: `from_member_id` (handed over the money), `to_member_id` (received it, must
  differ), `amount_minor`, `currency`, `status` Pending / Confirmed / Rejected / Cancelled
  (only Confirmed changes balances), resolved_by/at, soft delete with Undo for a confirmed
  one entered by mistake (the person who recorded it, or the owner), `client_request_id`.
- Recorded by the **payer** ("I paid Ana"): Pending until the receiver confirms or rejects;
  the payer can cancel while pending. Recorded by the **receiver** ("Marko paid me"):
  Confirmed at once. A **plain-name** payer or receiver: the owner acts for them.
- "Everyone's kvit" = every balance in every currency is 0 and nothing is pending.

**Goal (from the ROADMAP):**
1. Balances per currency and "who pays whom".
2. Pass `Balances.Calculate` every member the group has ever had, **including removed and
   left members** (`removed_at` set), in joining order; otherwise it throws by design.
3. Settlements: record, confirm, reject, cancel, delete (with Undo), the owner acting for
   plain names.
4. Zero-balance rules for leave, remove and delete group.
5. Screens 11 and 12, and the "Needs you" items for pending payments.
6. The activity feed gets a sentence for every new settlement event; an event type with no
   sentence must still fail with a clear error naming the type.

**How to work (propose first, wait for Filip's OK, ask before each coder run):**
- Split it into small steps, suggested order: backend balances including left members, then
  settlement commands and rules, then the zero-balance checks, then the frontend screens.
  Give a plan with the tap counts for each screen and wait for Filip's go.
- The frontend sits on the saved-data cache from Phase 8b: new query keys follow the
  `['groups', id, ...]` pattern, every settlement change invalidates the balances and the
  expense list of that group, and the wipe rules (log-out, 401, another person) already
  cover new queries. Saving still waits for the server; do not queue anything offline.
- Money is integer minor units, never `decimal` or `double`.
- `client_request_id` duplicate protection for settlements, also for two requests at the
  same moment, as done for expenses in Phase 8.
- Tests: backend xUnit (Docker Desktop must be open for Testcontainers; ask Filip to start
  it, do not work around it), frontend Vitest. Every money test checks the sum-to-zero
  invariant.
- Put `dotnet ef database update` in "your part" whenever a step adds a migration; nothing
  applies it locally.
- The first-push texts (commit subject, PR title, PR description) come at the end; later
  pushes get a one-line subject.

**Working rules (repeat them to subagents):** start every reply with "Filip"; propose then
wait; one step at a time, each step ending with "your part" (Docker, terminal, manual
things); no code comments; commits are one subject line with no body, no Co-Authored-By
line; Filip makes every commit and merges the PR himself; ask before every coder run; the
tester may run inside an approved plan; give subagents the memory folder path
`C:\Users\Davchev\.claude\projects\c--Users-Davchev-Projects-Kvit\memory\`; label every
claim as VERIFIED by automated test, VERIFIED by live run, or NOT VERIFIED; log decisions in
`docs/DECISIONS.md` as you go; update `docs/ROADMAP.md` and `docs/STATUS.md`; when Filip
cannot check something himself, run a real-browser check with Playwright against the local
stack (`.\scripts\start-local.ps1 -NoBrowser`, then `-Stop`; in a Vite dev run only block
URLs that start with `/api/`, not every URL containing "api").

---
