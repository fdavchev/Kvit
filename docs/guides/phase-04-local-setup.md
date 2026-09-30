# Phase 4: local setup, step by step

> For Filip. Plain steps you can follow from your phone. Each step says what to do, what you should see, and what to do if it fails.
> **This file grows.** Part 1 is written now (Step 1, the database). Parts 2 to 4 are added when their steps are done.
> Nothing in here is a secret. The database on your PC has **no password on purpose**: it can only be reached from your own PC. That only holds while `compose.yaml` keeps `127.0.0.1:5432:5432` for the port. **Never change the `127.0.0.1` part**, and never copy that file to a server. The database **online** (Neon, Phase 5) will always have a password; that is written down as a Phase 5 line in `docs/ROADMAP.md`.

## Part 1: the local database (Step 1)

### What you need to know first
- **Docker Desktop** is the app on your PC that runs the database. You only need to **open it and leave it running**. You never type anything inside Docker Desktop itself.
- **The terminal** is where you type the commands below. Use the one in VS Code (Terminal → New Terminal). It must be in the **Kvit folder** (the one that contains `Kvit.slnx`). If you are not sure, type `dir Kvit.slnx` and it should list the file.
- Type (or paste) one command, press Enter, wait until it finishes, then do the next.

### Step 1: open Docker Desktop
1. Open **Docker Desktop** from the Start menu.
2. Wait until the bottom-left corner says **"Engine running"** (green). This can take a minute after Windows starts.

**If it does not start:** restart Windows and open Docker Desktop again. If it still fails, send me the message it shows.

### Step 2: restore the project tools (once)
```
dotnet tool restore
```
**You should see:** `Tool 'dotnet-ef' (version '10.0.12') was restored.`
(`dotnet-ef` is the helper that builds the database tables from our code.)

**If it fails:** send me the text it shows.

### Step 3: start the local database
```
docker compose up -d
```
**You should see:** the first time it downloads the database (1 to 2 minutes), then `Container kvit-postgres Started`.
In Docker Desktop, under **Containers**, you will now see **kvit-postgres** with a green dot.

Check that it is ready:
```
docker compose ps
```
**You should see:** the line for `kvit-postgres` says `(healthy)` at the end (it can say `starting` for a few seconds first; run the command again).

**If it fails:**
- `error during connect` or `pipe/docker_engine` in the message: Docker Desktop is not running yet. Do Step 1 again and wait for "Engine running".
- `port is already allocated` or `Bind for 127.0.0.1:5432 failed`: another database on your PC already uses that door (port 5432). Send me the message.

### Step 4: tell Kvit where the database is
```
dotnet user-secrets set "ConnectionStrings:KvitDatabase" "Host=localhost;Port=5432;Database=kvit;Username=kvit" --project src/api/Kvit.Api
```
**You should see:** `Successfully saved ConnectionStrings:KvitDatabase to the secret store.`
(This is saved in a private folder in your Windows profile, **not** in the project, so it can never be committed.)

**If it fails:** send me the text it shows.

### Step 5: create the tables
> **On a new PC, do Part 3, Step 2 (the certificate) first.** Since Step 2b this command needs it too. On your PC now it is already done.
```
dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api
```
**You should see:** lots of lines, then `Applying migration '20260929182825_InitialCreate'.` and the last line `Done.`

**A red-looking `fail:` line near the top is normal the first time.** It says `Failed executing DbCommand` for `SELECT migration_id, product_version FROM "__EFMigrationsHistory"`. That is the tool asking an empty database "what have you already got?", and the answer is "nothing yet, the list doesn't exist". It then creates the list and applies the migration. What counts is the last line: **`Done.`** If you run the command a second time, the `fail:` line is gone. (Seen on a fresh empty database: exit code 0, `Done.`, 10 tables. The reason is my reading of the order of the lines; the tool does not print the database's own error text.)

Check the tables:
```
docker exec kvit-postgres psql -U kvit -d kvit -c "\dt"
```
**You should see:** a table list of **10 rows**: `data_protection_keys`, `role_claims`, `roles`, `usage_events`, `user_claims`, `user_logins`, `user_roles`, `user_tokens`, `users` and `__EFMigrationsHistory`.

**If it fails:**
- `The setting ConnectionStrings:KvitDatabase is missing or empty`: Step 4 was not done or was typed differently. Do Step 4 again.
- `Failed to connect` or `Connection refused`: the database is not running. Do Step 3 again.

### Step 6 (optional): run all the tests yourself
Docker Desktop must be running. In the same terminal:
```
dotnet test Kvit.slnx
```
**You should see:** `total: 292`, `failed: 0`, `succeeded: 292`. The first time takes 1 to 2 minutes (it downloads a test database); after that about 20 seconds. The tests start their **own** temporary database, so they never touch the one from Step 3.

### Everyday use
| I want to... | Command |
|---|---|
| Stop the database (keeps the data) | `docker compose stop` |
| Start it again | `docker compose start` |
| Stop it and remove the container (keeps the data) | `docker compose down` |
| **Wipe all data and start empty** | `docker compose down -v`, then Steps 3 and 5 again |

The database also starts by itself when Docker Desktop starts.

## Part 2: the accounts step (Step 2a)

### What changed for you
The database got one more small update (a new migration): a rule that two accounts can't share an email, and checks that name, time zone and creation time are never empty. You apply it with one command. Nothing else needs your hands in this step: the sign-up screens come in the last step, and that is where you try it in the browser.

### Step 1: keep Docker Desktop open
Open Docker Desktop and wait for "Engine running". The `kvit-postgres` container from Part 1 should still show a green dot.

**If the container is not there or is stopped:** in the terminal, in the Kvit folder, run `docker compose up -d`.

### Step 2: apply the new database update
In the terminal, in the Kvit folder:
```
dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api
```
**You should see:** `Applying migration '20260929200613_AccountRules'.` and the last line `Done.`
(A red `fail:` line is not expected this time. It only appears the first time, on an empty database.)

**If it fails:**
- `The setting ConnectionStrings:KvitDatabase is missing or empty`: do Part 1, Step 4 again.
- `Failed to connect` or `Connection refused`: the database is not running; do Step 1 above.
- `violates check constraint` or `could not create unique index`: the database already holds test users that break the new rules. Send me the text.

### Step 3 (optional): check the tests yourself
```
dotnet test Kvit.slnx
```
**You should see:** `total: 403`, `failed: 0`, `succeeded: 403` (the first run is slower while it starts a test database).

## Part 3: the login-key certificate (Step 2b)

### What changed for you
The login cookie is locked with secret keys. From now on those keys are kept in your database (not in a file), and they are locked with a **certificate**: a small secret file that only your PC and, later, the online server know. One command makes it and saves it in your private secrets, so you never copy anything by hand.

**From this step on, the local API refuses to start until you have done Step 2 below.** Its error message tells you what to do. The same goes for the `dotnet ef` commands from Parts 1 and 2: on a new PC, do Step 2 below before them.

### Step 1: keep Docker Desktop open
The script itself does not need Docker, but Step 4 starts the API, which reads the database. Open Docker Desktop and check that `kvit-postgres` has a green dot.

### Step 2: make the certificate (once)
In the terminal, in the Kvit folder:
```
dotnet run scripts/NewDataProtectionCertificate.cs
```
**You should see** (the first run takes a few seconds while it compiles):
- `Saved DataProtection:CertificateBase64 and DataProtection:CertificatePassword to the user secrets of src/api/Kvit.Api.`
- `The certificate expires on 20xx-xx-xx.` (10 years from today.)

**If you run it a second time** it says `Nothing changed: ...` on purpose. Replacing the certificate would make every saved login unreadable and log everybody out. Only add `-- --force` at the end if you really want that.

**If it fails:** send me the text it shows.

### Step 3: check that it was saved (this shows names only, never the values)
```
(dotnet user-secrets list --project src/api/Kvit.Api) -replace ' =.*',''
```
**You should see three names:** `ConnectionStrings:KvitDatabase`, `DataProtection:CertificateBase64` and `DataProtection:CertificatePassword`.
(Do not run `dotnet user-secrets list` without the part after it: it would print the password on screen.)

### Step 4: start the API and stop it again
```
dotnet run --project src/api/Kvit.Api
```
**You should see:** a few grey lines about `data_protection_keys` (the app reading and, the first time, creating its login key in the database; this is normal), then `Now listening on: http://localhost:5018`.
Open `http://localhost:5018/api/health` in the browser: it shows `Healthy`. Then go back to the terminal and press **Ctrl+C** to stop the API.

**If it fails:**
- `The setting DataProtection:CertificateBase64 is missing or empty`: Step 2 was not done.
- `The setting ConnectionStrings:KvitDatabase is missing or empty`: do Part 1, Step 4.
- `Failed to connect` or `Connection refused`: the database is not running; do Step 1.
- `Unable to retrieve the decryption key` in the log: the database holds a login key made with a different certificate (for example after `--force`). The app carries on and makes a new key; everybody has to log in again. To tidy up, run `docker exec kvit-postgres psql -U kvit -d kvit -c "delete from data_protection_keys"`.
