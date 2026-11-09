# Phase 19 for Filip: what to do, in order

Written 2026-11-09. Everything Claude built is on the branch `feat/19-legal` (nothing is pushed). This page is only about **your** part. Tick the boxes as you go. Nothing here is legal advice and nothing was checked by a lawyer.

## A. Before you push (10 minutes, at your laptop)
- [ ] **Open Docker Desktop**, then in the repository folder run the database update for your local database (it adds the new columns and the new table; three updates came with this phase):
  ```
  dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api
  ```
  You should see "Applying migration ... TermsAcceptance", "... AccountDeletion", "... OwnerTools" and then "Done."
- [ ] Start the app (`.\scripts\start-local.ps1`) and look at it yourself once: Welcome, `/terms`, `/privacy`, Settings. Try "Download my data" and the owner tools (you must be admin: `docs/guides/grant-admin.md`).
- [ ] Read `docs/reports/2026-11-09-phase-19-legal.md`: it says what is proven, what is not, and lists the questions for you.

## B. Three secrets (do this BEFORE the merge, otherwise the new jobs fail with a clear message)
All three are long random texts. Make them in PowerShell (it prints one text; copy it into your private notes file, never into the repository):
```
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(36))
```
- [ ] **`Cleanup__Secret` on Render:** Render dashboard, your service, **Environment**, add `Cleanup__Secret` = a new random text. (Two underscores.)
- [ ] **`CLEANUP_SECRET` on GitHub:** repository, **Settings**, **Secrets and variables**, **Actions**, **New repository secret**, name `CLEANUP_SECRET`, **the same text** as on Render.
- [ ] **`BACKUP_PASSPHRASE` on GitHub:** a second new repository secret with a different random text of your own. **Write this one in your notes twice (two places). If you lose it, the backups are useless.**
- [ ] On GitHub: repository **Settings**, **Code security**, turn on **Private vulnerability reporting** (so `SECURITY.md` is true).

## C. Push and merge
- [ ] Push the branch, open the pull request with the texts in `docs/guides/phase-19-pr-text.md`, wait for the green CI, merge. CI applies the three database updates on Neon by itself (all of them only add empty columns and a table, so the old server keeps working while it updates).
- [ ] After the merge, wait for Render to finish the deploy.

## D. First runs on the real site (15 minutes)
- [ ] GitHub, **Actions**, **Cleanup**, **Run workflow**. Green tick = the secret matches. (It wakes the server and the database once; it will do the same every day at 03:17 UTC, which is 04:17 in Macedonia in winter and 05:17 in summer.) If it is red, open it: the message says which secret is missing or wrong.
- [ ] **Actions**, **Backup**, **Run workflow**. Green tick, then the run page shows a file under **Artifacts**.
- [ ] Do the practice restore once: `docs/guides/restore-a-backup.md` ("Practice run"). Write the date in `docs/STATUS.md`.
- [ ] Make your online account admin if it is not yet (`docs/guides/grant-admin.md`), then open **Settings, Owner tools** on your phone.
- [ ] On your phone, look at: `/terms`, `/privacy`, the agree line when making an account, the "Terms changed" bar after you log in (everyone who signed up before this phase sees it once), Settings (Download my data, Delete my account only to look at the sheet and press Cancel), a group's Settings (Report this group). Tell Claude anything that looks wrong or overlaps.

## E. Outside the code (only you can do these)
- [ ] **Google console** (Google Auth Platform, Branding): privacy link `https://kvit-mk.pages.dev/privacy`, terms link `https://kvit-mk.pages.dev/terms`, support email `kvit.support@gmail.com`; look at the "publish check" and tell Claude if it complains.
- [ ] **Kvit mailbox:** keep its password out of the repository. If you want the mail in your own inbox: Gmail, Settings, Forwarding.
- [ ] **Save the data-processing pages** of Cloudflare, Render, Neon and Google as screenshots or PDFs (with the date) in a new folder `docs/legal/vendors/`. The list of links is in `docs/legal/vendors.md`. Claude cannot sign in to them.
- [ ] **Render:** in the dashboard check whether the free plan offers the Frankfurt region and write the answer in `docs/legal/vendors.md`.
- [ ] **Name check:** search "Kvit" and «Квит» in TMview (tmview.org) and at the Macedonian intellectual property office (ipo.gov.mk).
- [ ] **Email the National Bank (NBRM)** and ask whether fetching the exchange rates automatically once or twice a day is fine. A draft (find their contact address on nbrm.mk):
  > Subject: Question about using the exchange rate list
  >
  > Hello, I run a small free app for sharing expenses between friends (Kvit, kvit-mk.pages.dev). It shows amounts in denars and euros and takes the daily exchange rate list from nbrm.mk automatically, about three times a day, and names NBRM as the source. Is this use fine, and is there a page or service you prefer I use? Thank you. Filip Davchev, kvit.support@gmail.com
  >
  > Македонски: Здраво, водам мала бесплатна апликација за делење трошоци меѓу пријатели (Kvit, kvit-mk.pages.dev). Прикажува износи во денари и евра и автоматски ја презема дневната курсна листа од nbrm.mk, околу три пати дневно, а НБРСМ е наведена како извор. Дали ваквото користење е во ред и дали има страница или услуга што ја препорачувате? Благодарам. Filip Davchev, kvit.support@gmail.com
- [ ] **Read the Macedonian texts** (they are drafts, Claude is not a native speaker). They are in `src/web/src/core/i18n/locales/mk.json` under `terms`, `privacy`, `legal`, `ownerTools`, `suspended`, and the new lines in `settings` (download, delete), `auth.deletionCancelled`, `group.closedByAdmin`, `groupSettings.report*`, `addName.hint` and `errors` (the new codes). The two long documents are also in `docs/legal/terms-2026-11-04.md` and `privacy-2026-11-04.md` (English first, then Macedonian). Changing a text means changing its pinned test too: ask Claude.
- [ ] **Decide** the questions at the end of the report (including whether `START-HERE-PROMPT.md` stays in the public repository).

## F. Yearly and later
- [ ] **Before 2027-09-30:** renew the date in `src/web/public/.well-known/security.txt` (a test turns red when it is due).
- [ ] If GitHub emails you that the scheduled jobs are switched off (no activity for 60 days): Actions tab, **Enable**.
- [ ] If Kvit starts to grow beyond friends and family (strangers joining, any money feature, ads, fees, a complaint or a letter from an authority): stop and read "Accepted risks" in the report; that is the moment for the lawyer.
