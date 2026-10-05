# Backlog

## To be done (after the first version)
- [ ] **Animations (Filip, 2026-10-03: "we are going to be doing these animations, remember that we need them on certain things").** Seen so far in the Phase 7 mockup: the Undo toast slides up and away (built in Phase 7 Step 4 through Sonner, 4 s; the bottom sheet slide is built there too), bottom sheets slide up (Add a name, the row menu), the selected tab moves between tabs, a row appearing or leaving a list (a name added, a person removed), the Finished row opening. Left for this pass (Filip, 2026-10-05): Sonner's toasts slide in at its own 0.4 s, DECISIONS wants about 0.2 s. Each one respects "reduce motion". Claude proposes the full list and the exact timings in the Phase 11 polish pass at the latest, and does the cheap ones (toast, sheets) while building the screens that have them. Filip decides what stays.
- [ ] **API error handler (Filip, 2026-10-05, found when the Groups list showed the generic message because a local migration was missing):** the API has no global exception handler, so any unexpected crash is a bare 500 with no error code and the website shows "Something went wrong" for it. Add one that logs the exception with its details and answers a problem-details body with a clear code (for example `SERVER_ERROR`), plus the matching `errors.SERVER_ERROR` text in en.json and mk.json, so a crash can be told apart from other errors. Never put exception text or secrets in the answer.
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
- [ ] **MUST DO (Filip, 2026-10-03, "we will add no account join later"):** Joining a group without an account: open the invite link, tap your name, and the device remembers you. Needs a guest identity on the phone, rules for who may claim which name, fixing wrong claims, and a plan for a lost phone. Phase 7 only offers Google (1 tap) or email at the link. Schedule it right after Release 1 unless Filip decides otherwise.
- [ ] Keeping the server awake: ask Render support whether pings are allowed, or move to a cheap paid plan if there are real daily users.
- [ ] Time zone: a picker on Sign up and Log in only when the phone reports no usable zone, plus Settings → "Choose manually" (needs an endpoint that saves the zone and sets `is_time_zone_manual`). When it exists, the `TIME_ZONE_INVALID` text gets "Choose manually" back (Filip, 2026-09-30)
- [ ] Forgot password / password reset by email (needs Gmail SMTP) When it exists, the Log in note "Forgot your password? Ask Filip to reset it." becomes a "Reset password" link (Filip's joke idea: keep "Ask Filip to reset it" beside it). Until then Filip resets by hand with `scripts/ResetPassword.cs` (Phase 4, Step 3b)
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

- [ ] Warn loudly (log, or fail with a clear message) when the login-key certificate is expired or about to expire (it is valid 10 years; whether an expired one still decrypts is only from the source), and check its `NotBefore` (Step 2b code review)

- [ ] A real desktop layout for the web (a wider column, larger text, sidebar and several columns); it comes with the dashboard in Phase 11. Today the five Phase 4 screens are a centred 400 px column on a wide window (Filip, 2026-10-01)
- [ ] Theme choice kept on the account instead of only on the device, so it follows the person to another phone
- [ ] Show the field-level server errors next to the field (the email field for `AUTH_EMAIL_TAKEN`, the password field for `AUTH_PASSWORD_TOO_WEAK`); `KvitTextField` already has an unused `invalid` prop
- [ ] `Retry-After` is the whole window (60 or 600 seconds), not the time left; show or send the time left if a countdown is ever wanted
- [ ] Check on the deployed site: `net::ERR_ABORTED` that the browser tool logged on 204 answers through the Vite proxy (language, log out, change password; the answers were 204 and the app worked), and the one white frame of the Vite dev server before a saved dark theme appears (the production build has none)
- [ ] Small look items from the browser checks: the 'Coming soon' toast on Welcome covers the 'I already have an account' link while it shows; big titles wrap to two lines in places ("Change password", Macedonian "Направи профил"); Settings' 'Change password' and 'Log out' look the same; the apostrophe in "I don't have an account" is straight in the app and curly in the mockup
- [ ] The sign-up and log-in counters are in memory and restart with the free Render instance; a window can let a burst through at its end (accepted for the first version)
- [ ] Privacy page contact: today it says "ask Filip" with no email (Filip, 2026-10-03: friends and family only). Before Kvit grows beyond family and friends, Claude asks Filip for a contact email (a Gmail made just for Kvit) and puts it on `/privacy` and in Google's Branding "support email" if needed
- [ ] Link a Google sign-in to an existing password account (a safe way, e.g. the person logs in with the password first, then adds Google in Settings). Phase 6 never merges automatically (DECISIONS), so today the pop-up only offers "Log in"
- [ ] Google sign-in loose ends (Step 2b review): (a) a 401 `AUTH_GOOGLE_TOKEN_INVALID` is treated like "session ended" by `queryClient.ts` (only matters if a signed-in person taps Google on Welcome; fix: also exempt that code, with a test); (b) Google keeps one callback for the whole page, so two Google buttons that ever need different handlers would clash (today every button runs the same flow)- [ ] The main JavaScript file is 559 kB (limit warning at 500 kB; 497 kB before Phase 6). Split the code by screen (`React.lazy` per route) when the build warning matters or the app feels slow on a phone
- [ ] Settings: change the display name (Phase 6 asks the name once, pre-filled from Google; changing it later has no screen yet)

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

## Found during Phase 5 (2026-10-01)
- **Cloudflare calls Pages "legacy".** The new dashboard puts the Workers flow first and hides Pages behind "Need to use the legacy Pages workflow? Continue to Pages". Pages still works and its docs show no end date. Revisit if Cloudflare announces one: the same site can run as a Worker, but the forwarding function and a config file would change.
- **Harmless log line on Render:** `libgssapi_krb5.so.2: cannot open shared object file`. The database driver looks for a Kerberos library the slim image lacks; the live log-in test (401 from Neon) shows nothing is broken. Fix only if it clutters logs: install the library in the Dockerfile.
- **Theme icon does not follow the phone live.** With the choice "Same as device", if the phone switches light/dark while Kvit is open, the Welcome moon/sun icon updates on the next redraw, not instantly (`KvitThemeToggle` reads `matchMedia` during render).
- **Google sign-in on `pages.dev`:** Google saved `kvit-mk.pages.dev` under Authorized domains in Branding (REPORTED by Filip 2026-10-01, "branding changes saved", no error text; the real check is Phase 6, when the button is used). Brand verification (the "Kvit" name and logo on Google's window) cannot work on `pages.dev`; people see the address. Preview addresses cannot sign in.
- **A paid name** (about 10 € a year, needs a card) can be attached to Cloudflare Pages later with no code change, only if Filip decides to pay.
- **Older Kvit screenshots in `fdavchev.github.io`** (`v2/src/assets/screenshots/kvit-welcome.png`, `kvit-api-scalar.png`) are out of date (desktop Welcome before the theme button; the Phase 1 API page). Filip's portfolio, not part of this repository.
- **Render health checks and the free sleep:** whether Render's `/health` checks keep a free server awake is NOT VERIFIED; the live run (Step 4) shows it.
- **Render logs show every SQL command** (EF Core Information level, parameters hidden as `?`). Harmless but noisy; set the Render variable `Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command` to `Warning` if the log gets hard to read.
- **Render rebuilds on every commit to `main`**, even docs-only or website-only ones. Render "Build Filters" (folder list) could limit it to `src/api`; how Render's free build allowance works is NOT VERIFIED.
