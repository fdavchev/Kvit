# Backlog

## To be done (after the first version)
- [ ] Scanning receipt QR codes (Macedonian fiscal receipts). The QR format still needs research; it's NOT VERIFIED.
- [ ] Recurring expenses (rent, subscriptions)
- [ ] CSV export
- [ ] Reminders (Gmail SMTP app password or a Telegram bot)
- [ ] PWA: installing it as an app on the phone, plus real phone notifications (push)
- [ ] Uploaded pictures for profiles and groups. Shrink them on the phone to about 256×256 (roughly 20–30 KB) before uploading. Where to store them: small pictures fit in the Neon database (0.5 GB is about 15,000+ pictures), or Supabase Storage (1 GB free; third-party sites say no card is needed, but that's NOT VERIFIED on a Supabase page). **Cloudflare R2 is rejected:** it needs a card before it can be switched on, even for the free tier. Checked 2026-09-25, several Cloudflare Community threads confirm it; not stated on Cloudflare's official pricing page.
- [ ] Offline editing and deleting through the outbox, with rules for when two people change the same thing
- [ ] Unspent budget at the end of the month: the user picks where it goes (move to savings, carry over to next month, etc.)
- [ ] Several payers for one expense
- [ ] Charts
- [ ] "Share balances" button: turns a group's balances into a text message and opens the phone's share menu (Viber, WhatsApp…). A free reminder, with no bot needed. Checked 2026-09-24: the phone's share menu (Web Share API), `wa.me` links and `viber://forward` links are all free. Desktop Chrome has no share menu, so show WhatsApp/Viber buttons there.
- [ ] Joining a group without an account: open the invite link, tap your name, and the device remembers you.
- [ ] Keeping the server awake: ask Render support whether pings are allowed, or move to a cheap paid plan if there are real daily users.
- [ ] Forgot password / password reset by email (needs Gmail SMTP)
- [ ] "Delete my account" and automatic cleanup (Filip's idea, 2026-09-25):
  - **Deleting an account:** for 30 days **nothing is wiped**. The account is only switched off and the user can still change their mind by logging in. After 30 days the email, login and personal budget are permanently removed. In groups, the person **turns into a plain-name member with their name** (like "Grandma"), not "Deleted user", so the group history still reads "Marko paid 2,400". Their shares stay because other people's balances depend on them. (Changed 2026-09-25, Filip's answer.)
  - **Also cleaned up after 30 days:** expenses marked deleted, and old invite links.
  - **Usage events older than 12 months** are merged into monthly totals.
  - **How it runs:** a cleanup that runs lazily at most once a day (no scheduler needed), or a GitHub Actions job calling a protected endpoint.
  - **Size, for context:** one expense is a few hundred bytes, so Neon's 0.5 GB holds millions. Cleanup is about privacy and tidiness more than space.
- [ ] Shared pot / kitty for a trip: everyone pays in up front, one person holds it, leftovers are handed back at the end
- [ ] "People you've been in a group with": one tap to add them to a new group. Full friend requests are possible too; they cost nothing to run (a few small database rows), only development time
- [ ] Leave a single expense out of your budget (on top of the per-group switch)
- [ ] Backups. Suggested plan:
  - a weekly GitHub Actions job runs `pg_dump`, **encrypts** the file with a secret passphrase (kept in GitHub Secrets) and saves it as an artifact (kept up to 90 days),
  - plus Neon's built-in restore window,
  - GitHub emails you if the job fails.
  - Check: whether artifacts on a public repository are downloadable by anyone, and that scheduled jobs stop after about 60 days without repository activity.

- [ ] Make a successful log-in save its changes without Identity's concurrency stamp (one `ExecuteUpdate` for `lockout_count` and `time_zone`, only when they change). Today two log-ins of the same account at the very same moment can give one 500; retrying works (Step 2a review)

## Skipped on purpose
- CI runs twice on a pull-request branch (one run for `push`, one for `pull_request`; review finding 01-5, 2026-09-29). Left as it is: the second run tests the merge result with `main`, the cost is only free GitHub minutes, and limiting `push` to `main` would drop the check on a branch that has no pull request yet. Revisit only if the free minutes ever run short.

## Open question: allow a card?
- On 2026-09-25 Filip asked whether a card is OK as long as it's never charged. The current answer: not needed for the first version, so the no-card plan stays.
- **Revisit when:**
  - the Render cold start gets annoying (Oracle Always Free gives an always-on server), or
  - uploaded pictures are added (Cloudflare R2 gives 10 GB free).
- **If a card is ever used:** a virtual or prepaid card with a small balance and a low online-payment limit, billing alerts at 0, and never pressing "upgrade".

## Moved into the first version
- EUR alongside MKD. Filip wants both from the start: MKD for everyday use, EUR for trips. Decided 2026-09-24.
