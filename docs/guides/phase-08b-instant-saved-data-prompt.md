# Prompt for the next phase: "Saved data shows instantly" (Phase 8b)

**When to use:** after Phase 8 is merged to `main`. Start a new branch from `main`
(suggested name: `feat/08b-instant-saved-data`), open Claude Code in the Kvit folder
and paste everything between the two lines below.

---

Filip wants to run a new phase, **Phase 8b: saved data shows instantly**. It is the
ROADMAP line currently in Phase 11 ("Saved data shown instantly (TanStack Query cache
in IndexedDB), 'Updating…' note, the full waking screen only without saved data"),
moved forward because the Render wake-up (about 22 s) makes the app feel stuck.

**Start by reading:** `docs/STATUS.md`, `docs/ROADMAP.md`, the decision
"2026-09-25: Hiding the server wake-up (first version)" in `docs/DECISIONS.md`,
`docs/ARCHITECTURE.md` (Part 2, the "Server state + caching" row, and the Traps line
about Render sleeping), and `src/web/src/core/api/queryClient.ts`, `src/web/src/main.tsx`,
`src/web/src/core/auth/useMe.ts`.

**Goal:** when Filip opens Kvit after a quiet period, the screens he saw last time
appear at once from data saved in the browser, with a small "Updating…" note while the
server wakes. He can look around right away. The full "waking up" screen appears only
when there is no saved data (first visit, or after log-out).

**What to build (propose first, wait for Filip's OK, then ask before running coder):**
1. Save the TanStack Query cache to IndexedDB (check the current TanStack Query docs
   for the persister package and the `idb-keyval` option before choosing; do not guess
   names).
2. Show saved data first, then refetch in the background. Add a small "Updating…"
   note (EN + MK) while a refetch is running and the data came from the saved copy.
3. Show the full waking screen only when a screen has no saved data.
4. **Safety, must not be skipped:**
   - On log-out, and when the server answers 401, delete the saved copy. Another person
     on the same phone must never see the first person's data.
   - Tie the saved copy to the user id; ignore it if a different user logs in.
   - Add a version/expiry to the saved copy so an old shape of data after an app
     update is thrown away instead of crashing a screen.
   - Do not save the login (`me`) query in a way that makes Filip look logged in after
     the server says he is not; the login state still comes from the server.
5. Mutations (add, edit, delete) still wait for the server in this phase. The offline
   outbox is a separate, later phase. Say so plainly in the screens that cannot save
   while the server is waking, with a clear message, not a silent failure.

**Out of scope:** the outbox, push notifications, keep-awake pings (rejected in
DECISIONS), paid hosting.

**Tests:** Vitest for the persisting rules (cleared on log-out, ignored for another
user, ignored when the version changes, "Updating…" shown only for saved data). Also
tell Filip what to check by hand: open the app, close it, wait 15+ minutes (or stop
the API), reopen, and see the old data at once with the note.

**Working rules (from CLAUDE.md and memory, repeat them to subagents):** start every
reply with "Filip"; propose then wait; one step at a time and end each step with
"your part" (Docker, terminal, manual things); no code comments; no commit body; Filip
makes every commit; ask before every coder run; tester may run inside the approved
plan; give subagents the memory folder path
`C:\Users\Davchev\.claude\projects\c--Users-Davchev-Projects-Kvit\memory\`; label every
claim in the final result as VERIFIED by automated test, VERIFIED by live run, or NOT
VERIFIED; log decisions in `docs/DECISIONS.md` as you go; update `docs/ROADMAP.md`
(move the Phase 11 line into Phase 8b and tick it when done) and `docs/STATUS.md`.

---

## Notes for Filip (not part of the prompt)
- This phase does **not** let you add expenses while the server sleeps. That is the
  outbox, still later. It only removes the empty waiting screen when you open the app.
- Phase 11 will lose its "Saved data shown instantly" line once this phase is done.
