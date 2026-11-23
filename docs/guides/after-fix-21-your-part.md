# What is left for Filip after Fix 21

Written 2026-11-23, after Claude ran the Fix 20 live check on the PC with you ticking the robot box. Do the parts in order. Each part says where to click.

## What Claude already checked on the real site (2026-11-23)
All VERIFIED by live run (a Chrome window on your PC that Claude drove; you ticked Render's "Verify you are human" box each time):
- **Open Kvit while the server sleeps, signed out:** Kvit sent the browser to Render's wake page about 1 second after opening, Render showed "SERVICE WAKING UP" for about 27 seconds, then Kvit came back by itself on Welcome.
- **Open Kvit while the server sleeps, signed in, on a group page:** same trip; Kvit came back on the same group page, still signed in, and the server answered as you (no new log-in needed).
- **A tab left open 22 minutes, then a tap:** the open tab sent no requests by itself in those 22 minutes (nothing keeps the server awake), and the tap showed the bar "The server is asleep. · Wake the server".
- **An expense added while the bar shows:** it showed as "Not synced yet". After "Wake the server" and the robot box, Kvit came back on the same page and sent the expense by itself about 16 seconds later.
- **Clean-up:** the test group was deleted, then the test account (e-mail `kvit-livecheck-…@example.com`, name "Live Check") was deleted with "Former member". The account is switched off now and wiped after 30 days by the normal cleanup.

What Claude could not check: the robot box ticking itself (it never does for a remote-controlled browser, so you ticked it; on your phone on 2026-11-19 it ticked itself), your phone, and the Render log.

One question for you: in the first test the robot box came back about five times before Render woke. Did you have to tick it five times, or only once?

## Part 1. The Render log line (2 minutes, PC or phone)
1. Open `https://dashboard.render.com` and log in.
2. Tap the service **kvit-mk-api**.
3. Tap **Logs** on the left.
4. In the search box type `daily cleanup`.
5. Look for a line from **23 November** around **12:40** Skopje time (Render may show **11:40**, UTC). It should read "The daily cleanup of … finished".
6. Tell Claude what you see, or "nothing found".

## Part 2. Say the Fix 20 live check is over, then merge (10 minutes, PC)
Only when Part 1 is fine.
1. In VS Code's terminal, in the Kvit folder: `git push` (sends the docs commits Claude made; nothing else changes).
2. Open PR #25 on GitHub ("Fix 21: empty the backlog"), press **Edit** on the description and tick **Step 4** to **Step 10**. Save.
3. Wait for the green tick on the checks (about 8 minutes).
4. Press **Merge pull request**. Render deploys by itself after the merge.

## Part 3. Your phone, after the merge (25 minutes, mostly waiting)
1. On the phone, press and hold the Kvit icon on the home screen and remove it.
2. Open Chrome, go to `https://kvit-mk.pages.dev`, menu (three dots) → **Add to Home screen** (or **Install app**).
3. Open Kvit from the new icon and log in.
4. Leave it alone for 20 minutes (lock the phone, don't open Kvit).
5. Tap the Kvit icon. Expected: Render's black "waking up" page for about 30 seconds, then Kvit on the same screen, still signed in, **without** the "Wake the server" bar. Tell Claude what you saw (and whether the robot box ticked itself).

## Part 4. Clean up two secrets that are no longer used (3 minutes)
1. **Render:** dashboard → **kvit-mk-api** → **Environment** → find `Cleanup__Secret` → the trash icon → **Save changes**. Render restarts the server; that is fine.
2. **GitHub:** the Kvit repository → **Settings** → **Secrets and variables** → **Actions** → `CLEANUP_SECRET` → **Remove**.

## Part 5. Backup, once (15 minutes, PC)
1. GitHub → **Actions** → **Backup** (left list) → **Run workflow** → green **Run workflow** button.
2. Wait for the green tick; open the run; under **Artifacts** there is one file.
3. Do the practice restore: `docs/guides/restore-a-backup.md`, section "Practice run". Tell Claude the date, so it goes into STATUS.

## Part 6. GitHub private vulnerability reporting (1 minute)
GitHub → the Kvit repository → **Settings** → **Advanced Security** (on some accounts it is called **Code security**) → **Private vulnerability reporting** → **Enable**. It lets someone who finds a security hole tell you privately instead of in a public issue.

## Part 7. Things outside the code (Part E of `docs/guides/phase-19-legal.md`)
Still open there: the Google console links (privacy, terms, support e-mail), saving the data-processing pages of Cloudflare, Render, Neon and Google into `docs/legal/vendors/`, whether Render offers Frankfurt, the "Kvit" name check in TMview and ipo.gov.mk, the e-mail to NBRM (the draft is in that file), and the older Macedonian texts.

## Part 8. Answers Claude needs from you (any time, can be from the phone)
1. **Five choices Claude made in Fix 21.** Say yes or no to each (explained in the "For Filip" list of `docs/reports/2026-11-21-fix-21-small-bugs.md`):
   - an expired login certificate stops the server from starting (instead of running with a warning);
   - guest logins left with nothing are deleted 30 days after they were made;
   - the desktop menu lists groups that are Closing;
   - the Google button is blank for about 0.3 seconds when you switch the language;
   - the "how far back can I date an Edit" limit counts from today in UTC (so between midnight and 1 or 2 in the morning Skopje time it is one day off).
2. **Macedonian texts:** read `docs/guides/fix-21-macedonian-to-read.md`, rows 1 to 83, and send the row numbers that read wrong with your wording.
3. **Release 4:** pick the first phase from `docs/guides/release-4-pick.md`.
