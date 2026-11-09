# How to get your data back from a backup

Written 2026-11-09 (Phase 19). Read this once now, so you know it works before you ever need it.

## What the backup is
Every Sunday at about 04:41 (Macedonian time) GitHub makes a copy of the whole Kvit database, locks it with a password (the secret `BACKUP_PASSPHRASE`) and keeps the locked file for 90 days. Anyone logged in to GitHub could download that file, but without the password it is useless. **If you lose the password, the backups are useless too.** Keep it in your private notes (the same place as your other secrets), never in the repository.

Neon also keeps the last 6 hours on its own. The weekly backup is for bigger accidents.

## One-time setup (before the first backup can work)
1. On GitHub, open the repository, **Settings**, **Secrets and variables**, **Actions**.
2. Make sure the secret `NEON_DIRECT_CONNECTION_STRING` exists (it already does, it is used for the database updates).
3. Add a new secret named `BACKUP_PASSPHRASE`. The value is a long random text of at least 30 characters. Write the same text in your private notes.
4. Open the **Actions** tab, choose **Backup** on the left, press **Run workflow**. After about two minutes the run should show a green tick, and the run page lists a file under **Artifacts** named `kvit-backup-<date>`. If it is red, open it: the message says what is missing.
5. Do the same for **Cleanup** once, after adding the secret `CLEANUP_SECRET` (see the Phase 19 guide).

Two things to know:
- GitHub switches off scheduled jobs in a public repository after about 60 days without any activity (a push, a merge). It sends you an email first. If it happens, open the Actions tab and press **Enable**.
- If your connection string says `SSL Mode=VerifyCA`, the backup fails (it works with `Require` and `VerifyFull`). Tell Claude and it is changed.

## Getting a backup back (when something went wrong)
You need: Docker Desktop open, and a Git Bash or PowerShell window. **Never restore into the live database.** Always restore into a new, empty one, look at it, and only then decide what to do.

1. **Download the file.** GitHub, repository, **Actions**, **Backup**, click the run you want (the date is in its name), scroll to **Artifacts**, click `kvit-backup-<date>`. A zip file downloads; unzip it. Inside is `kvit.dump.enc`.
2. **Unlock it.** In a terminal, in the folder with the file (replace the text after `=` with your password; the first line is for Git Bash, the PowerShell line is below it):
   ```
   BACKUP_PASSPHRASE='your long password' openssl enc -d -aes-256-cbc -pbkdf2 -iter 600000 -pass env:BACKUP_PASSPHRASE -in kvit.dump.enc -out kvit.dump
   ```
   PowerShell:
   ```
   $env:BACKUP_PASSPHRASE='your long password'; openssl enc -d -aes-256-cbc -pbkdf2 -iter 600000 -pass env:BACKUP_PASSPHRASE -in kvit.dump.enc -out kvit.dump
   ```
   If it says `bad decrypt`, the password is wrong. If `openssl` is not found in PowerShell, use Git Bash (it comes with Git for Windows).
3. **Start an empty database to look at it.**
   ```
   docker run -d --name kvit-restore -e POSTGRES_PASSWORD=restore -e POSTGRES_DB=kvit -p 55432:5432 postgres:18
   ```
4. **Put the data in.** The file is in the "custom" format, which the `pg_restore` program reads:
   ```
   docker cp kvit.dump kvit-restore:/tmp/kvit.dump
   docker exec kvit-restore pg_restore --username=postgres --dbname=kvit --no-owner --no-acl --exit-on-error --single-transaction /tmp/kvit.dump
   ```
   No output means success.
5. **Look at it.** For example how many people the backup holds:
   ```
   docker exec kvit-restore psql --username=postgres --dbname=kvit -c "select count(*) from users;"
   ```
6. **Clean up when you are done:** `docker rm -f kvit-restore`, and delete `kvit.dump` (it is not locked any more).

To bring the data back to the real site, ask Claude: the usual way is a new Neon project (or branch) filled from this file, then the connection string on Render is changed to it. Do not do it alone in a hurry.

## Practice run (recommended once, 10 minutes)
Do steps 1 to 6 with the first real backup. If step 5 shows a believable number of people, you know the backup works. Write the date you tried it in `docs/STATUS.md`.

## What was tested already (and what was not)
- **VERIFIED by live run on 2026-11-09 (local test databases only):** a database with 24 tables was dumped, locked, unlocked and restored into a second empty database; all 24 tables had the same rows. A wrong password was refused.
- **NOT VERIFIED:** the workflow on GitHub itself, the real Neon database (its certificate, its size, how long it takes), and the 90-day keeping. Your practice run is that check.
