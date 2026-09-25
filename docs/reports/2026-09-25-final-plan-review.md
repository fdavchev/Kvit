# Final plan review (2026-09-25)

Every planning file was read line by line: DECISIONS, BACKLOG, ARCHITECTURE, ROADMAP, STATUS, START-HERE-PROMPT and the three research reports.
Nothing here was run. It's a document review, so every point is **NOT VERIFIED** by tests or runs. Facts marked "check" still need checking on the web.

## A. Contradictions found and fixed
1. **Split types.** The first feature list said "equal or exact" only, but a later decision added percentages and shares. The list now says all four.
2. **Google sign-in** was missing from the first feature list. Added.
3. **"Architecture document coming"** was out of date. Now marked done.
4. **"Waking up…" screen.** Two entries described a full-screen wake-up message, which clashed with the later "show saved data instantly" decision. Now the full screen only shows when there's no saved data yet (first visit on a device).
5. **STATUS and ROADMAP** still said "waiting for ARCHITECTURE.md". Updated.
6. **START-HERE-PROMPT** still said "if ARCHITECTURE.md exists" and didn't ask for the feature/screen list. Updated.

## B. Suggestions waiting for Filip's OK

### B1. Big one: split the first version into three releases
The first version is a lot for a beginner learning .NET and React at the same time:
- Clean Architecture
- 4 split types and 2 currencies
- settlements with confirmation
- a budget with recurring income
- the outbox
- Google sign-in
- admin statistics
- two languages

That's more than "a few weeks". Nothing gets deleted; it only changes the **order**:
- **Release 1, "splitting works":**
  - email + password accounts,
  - groups + invite link + plain-name members,
  - expenses (all 4 split types),
  - MKD/EUR balances + "who pays whom",
  - settle up with confirmation,
  - activity feed and dashboard,
  - EN/MK, dark mode,
  - deployed, with saved data shown instantly.
- **Release 2, "budget":**
  - categories and limits, income (once and repeating),
  - your share of group expenses counted in your budget,
  - the monthly summary.
- **Release 3, "polish":**
  - the outbox (queue while the server wakes),
  - Google sign-in,
  - the admin statistics page (the events are recorded from Release 1, so no data is lost),
  - change history.
- **Why:** after Release 1 you and your friends can already use it on a trip. Every release is small enough to finish, and each one is testable on its own.

### B2. Rules that are missing (suggested answers)
1. **Store the exchange rate on each EUR expense** when it's saved. Otherwise last month's budget changes whenever the rate changes.
2. **Removing members and deleting groups.**
   - The owner can't remove a member or delete a group while balances aren't zero, which is the same rule as leaving.
   - The owner can't leave without first handing ownership to someone else.
3. **Cancelling "I paid".** The payer can cancel it while it's still pending.
4. **Wrong name claims.** If someone claims the wrong plain name, the owner can undo the claim.
5. **Invite link.**
   - It's a long random link.
   - The owner can reset it to make the old one stop working.
   - Anyone with the link joins straight away, with no approval step, because of speed. The owner can remove people.
6. **Google + password with the same email (security).**
   - Kvit can't verify emails (sending email is postponed), so someone could sign up with password using *your* Gmail address before you do.
   - Rule: Google sign-in never merges automatically into an existing password account. It shows "This email already has a password account, log in with your password".
7. **Forgot password.** There's no email in the first version, so a password user who forgets it is locked out. Make **Google the main sign-in button** once Release 3 adds it. Until then Filip resets it by hand; a small admin "reset" tool can come later.
8. **Time zone.** Everything is stored in UTC. Months, "today" and repeating income use Europe/Skopje.
9. **Repeating income without a scheduler.** Created "lazily" when the user opens the app, the same trick as the exchange rate.
10. **Categories for group expenses.** Group expenses use the built-in category list, so your share always lands in a matching budget category. Your own custom categories are for personal spending only.
11. **Budget share switch.** Per group: "count my shares in my budget", on by default. Useful for groups like "work lunches" that you don't want in your budget.
12. **Deleting with Undo.** Deleting an expense marks it deleted, and "Undo" restores it. This makes the "Undo instead of Are you sure?" decision actually possible.
13. **Change history.** Not a separate feature: an edit in the activity feed stores the old and new values ("Filip changed 1,200 → 1,500"). That settles the "if it stays small" question.
14. **MKD amounts** are stored in deni (×100), the same as euro cents, so the code treats both currencies the same way. The denar is shown without decimals.
15. **Security basics:**
    - limit login attempts (ASP.NET Core has a built-in rate limiter),
    - HTTPS only,
    - invite and join endpoints are also rate-limited.
16. **Privacy page.** A simple "what data Kvit stores" page. Google may ask for a privacy policy link when the sign-in app goes to "In production" (check). It's also fair to the friends whose data is stored.
17. **Deleting your account:** later (backlog). The expenses stay, and the name becomes "Deleted user".

### B3. Traps not yet written anywhere
1. **Backups on a public repository.** Anything a GitHub Action uploads (artifacts) can be downloaded by **anyone** on a public repository (check). A nightly database dump must be **encrypted with a secret key** before upload, or not stored on GitHub at all. Neon also has a short built-in restore window on the free plan (length: check).
2. **Scheduled GitHub Actions stop by themselves** after about 60 days without activity in the repository (check). A backup job could silently stop, so the guide must say how to notice.
3. **Taken names.** `kvit.pages.dev` or `kvit.onrender.com` may already be taken. Have fallback names ready, e.g. `kvit-app`.
4. **Tools to install on Windows.** All of these need to go in a plain-language guide:
   - .NET 10 SDK,
   - Node LTS,
   - Docker Desktop (needed for the Testcontainers tests; free for personal use, check),
   - VS Code with the C# extension (the C# Dev Kit licence is free for individuals, check).

## C. What's fine as it is
- Hosting (Neon + Render + Cloudflare Pages with the proxy), no card, and the public repository.
- The outbox rules, the settle-up rules and the edit/delete rules.
- The architecture file. One note: 5 backend projects is a lot for a beginner. Keep it, because it teaches the structure used at work, but build it one project at a time.
