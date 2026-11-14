# Record of processing (one page)

Written 2026-11-04. Not required by law for a project this size (research, section 2); kept because it is the quickest way to answer "what do you hold and why". Update it with every change to the privacy notice. Not legal advice, not checked by a lawyer.

**Controller:** Filip Davchev, North Macedonia. Contact `kvit.support@gmail.com`. No data protection officer.

| Purpose | Basis | People | Data | Recipients | Kept for |
|---|---|---|---|---|---|
| Run accounts, groups, expenses, payments, personal budget | Contract: what the person asks Kvit to do | Account holders, guests | Name, language, time zone, email and password hash (email accounts), Google ID and picture link (Google accounts), groups, expenses, payments, notes, budget entries, limits, plans, categories | Cloudflare, Render, Neon; Google for sign-in | While the account exists; 30 days after a deletion request, then removed (name stays on shared expenses or shows "Former member") |
| Show the shared expenses to the group | Contract with the members; the member who typed a plain name acts as the source | Plain-name members (named by someone else) | Name, shares and balances inside that group | Group members | Until removal is asked for; then name removed or replaced |
| Security, abuse prevention, rate limits | Legitimate interest: keeping a free service working | Visitors, account holders | Visitor internet address (in memory for counters; host logs), failed-login counters | Cloudflare, Render | Logs: days to a few weeks (host-controlled, to confirm) |
| Anonymous-style usage counts (sign-ups, active people, groups, expenses, payments) | Legitimate interest: knowing whether the service is used | Account holders, guests | Event type, time, account id; never amounts or names | Neon | Single events tied to the account for 12 months, then the account tie is cut (the counts stay) |
| Owner actions on reports (suspend a person, close a group) | Legitimate interest: acting on reports and abuse | Reported people and groups | Who, what, when, reason, in the owner action log | Only Filip | Kept with the log; reviewed yearly |
| Backups | Legitimate interest: not losing everyone's data | All of the above | Encrypted copy of the database | GitHub (encrypted artifact, from Phase 19 Step 8), Neon restore window | Neon 6 hours; weekly encrypted backup up to 90 days |

**Transfers outside the EU/EEA:** Cloudflare, Render, Google and GitHub are US companies; the database is in Frankfurt. See `vendors.md`.

**Security measures:** HTTPS everywhere; passwords stored only as hashes; login cookie HttpOnly, Secure, SameSite Lax; per-address rate limits; proxy secret so the server only answers through Cloudflare; login-key certificate kept in the database; secrets only in Render, Cloudflare and GitHub secret settings; the admin role is granted by hand with `scripts/GrantAdmin.cs`; the admin statistics page shows counts only.

**Rights handling:** download (JSON) and delete are buttons in Settings; other requests by email to the contact address, answered within one month.

**No automated decisions, no profiling, no sale of data, no ads.**
