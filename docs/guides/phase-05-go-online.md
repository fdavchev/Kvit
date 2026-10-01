# Phase 5: putting Kvit online (Filip's steps)

Plain steps for you. Do one part, tell the Claude session what you saw, and wait for the next part.

**Two safety rules for every part:**
1. A password or connection string goes **only** into your private notes file (outside the Kvit folder, e.g. `Documents\kvit-secrets.txt`) and into the website you are told to paste it into. **Never** into the Kvit folder and **never** into the Claude chat.
2. If any site asks for a **bank card**, stop and tell the Claude session. Everything here is on the free plan.

---

# Part 0: move your local database to Postgres 18 (once)

**What this does:** Kvit's database program (Postgres) has a new version, 18. Your PC's test database, the automatic tests and the online database should all use the same version, so you switch the one on your PC now. The tests already passed on 18 (508 of 508). Your local test accounts will be gone afterwards (they live in the old version's storage); you make a new one.

**Docker Desktop:** open, with the `kvit-postgres` container showing (green dot).

In PowerShell, in `C:\Users\Davchev\Projects\Kvit`, one command at a time. Make sure the Kvit API is **not running**.
1. Stop and remove the old container (your old data stays in its storage for now):
   ```
   docker compose down
   ```
   - You should see: `Container kvit-postgres  Removed`.
2. Start the new one (this one is version 18 and uses new, empty storage):
   ```
   docker compose up -d
   ```
   - You should see: `Container kvit-postgres  Started` (or `Healthy`). If it says the image must be pulled, wait a minute.
3. Check:
   ```
   docker ps --filter name=kvit-postgres --format "{{.Names}} {{.Image}} {{.Status}}"
   ```
   - You should see: `kvit-postgres postgres:18 Up ... (healthy)`. If it says `(health: starting)`, run it again after 10 seconds.
4. Build the tables in the new local database:
   ```
   dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api
   ```
   - You should see: three `Applying migration` lines, then `Done.`
5. Optional, when you are happy: delete the old storage (this permanently deletes your old **local** test accounts, nothing else):
   ```
   docker volume rm kvit_kvit-postgres-data
   ```
6. Optional: start the API (`dotnet run --project src/api/Kvit.Api`), open the app and sign up once, to see it works on 18.

**If something goes wrong:** a port error means an old container still holds port 5432: run `docker ps -a` and tell the Claude session the output. `dotnet ef` failing to connect means the container is not `(healthy)` yet: wait and repeat step 4.

---

# Part 1 (Step 2): the database on Neon, the first migration, the GitHub secret

**What this does:** Neon is the free online database. A "migration" is a small instruction list that builds Kvit's tables inside it. You will create the database, build the tables once from your PC, and give GitHub the address of the database so that later table changes are built automatically.

## 1. Create the Neon project
Follow `docs/guides/free-hosting-setup.md`, **Part A1** (Neon). The values that matter:
- Project name `kvit`, region **AWS Europe (Frankfurt)** (can't be changed later), Postgres version **18** (same as your local database after Part 0), everything extra (Neon Auth and similar) **off**.
- **You should see:** a project dashboard called "kvit", and no request for a card.

## 2. Get the two connection strings
Still in A1 step 3 of the free-hosting guide. A connection string is the "address + password" of the database. You need two:
- **DIRECT** (no `-pooler` in it): for building tables, and for GitHub.
- **POOLED** (has `-pooler` in it): for the online server. It is used in the next part, but copy it now while you are here.

Write both into your notes file. They must look like this, one line each, with your own values (not `postgresql://...`):
```
DIRECT:
Host=ep-something.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=YOUR_PASSWORD;SSL Mode=VerifyFull;Channel Binding=Require

POOLED (add the last piece, it is needed for Neon's pooled connection):
Host=ep-something-pooler.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=YOUR_PASSWORD;SSL Mode=VerifyFull;Channel Binding=Require;No Reset On Close=true
```
- If Neon shows `postgresql://USERNAME:PASSWORD@HOST/DATABASE?...`, rewrite it by hand into the format above. The pieces map one to one.
- Double-check: the DIRECT line has no `-pooler`, the POOLED line has `-pooler` and ends with `No Reset On Close=true`.

## 3. Build the tables from your PC (once)
**Docker Desktop:** not needed for this part.

Open PowerShell in `C:\Users\Davchev\Projects\Kvit`. Make sure the Kvit API is **not running** (a running API locks its files on Windows and the commands below fail). Run the commands **one at a time**.

1. Run this command. It waits for you (the cursor blinks after the text, that is normal):
   ```
   $env:ConnectionStrings__KvitDatabase = (Read-Host 'Paste the DIRECT line here, then press Enter').Trim()
   ```
   Now go to Notepad, click inside the `Host=...` line under `DIRECT:`, press Home, then Shift+End, then Ctrl+C. Go back to PowerShell, **right-click** inside the window (that pastes), then press Enter.
   - Copying the line only **after** the command is running matters: copying the command from this guide replaces what is on the clipboard.
   - You should see: the line on your screen (it is your own screen) and then the prompt again. The string now lives only in this PowerShell window, which overrides your local database for as long as the window is open.
   - Check its shape without showing it (both lines must print `True`):
     ```
     $env:ConnectionStrings__KvitDatabase -match '^Host=ep-[^;]+;Database=[^;]+;Username=[^;]+;Password=[^;]+;SSL Mode=VerifyFull;Channel Binding=Require$'
     $env:ConnectionStrings__KvitDatabase -notmatch 'pooler'
     ```
   - If a line prints `False`, don't go on: redo the copy (select only the `Host=...` line, not the `DIRECT:` heading) or tell the Claude session which line printed `False`.
2. Check that the first migration list shows everything as waiting:
   ```
   dotnet ef migrations list --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api
   ```
   - You should see three lines: `20260929182825_InitialCreate (Pending)`, `20260929200613_AccountRules (Pending)`, `20261001075232_MustChangePassword (Pending)`.
   - If it says `Failed to connect` or a certificate/SSL error, copy **only the error text** (never the string) to the Claude session. Don't change anything.
3. Build the tables:
   ```
   dotnet ef database update --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api
   ```
   - You should see: `Applying migration '...'` three times, then `Done.`
   - A red-looking line `Failed executing DbCommand ... FROM "__EFMigrationsHistory"` before them is **normal** on an empty database: EF asks for its history table before it exists, then creates it. Only a missing `Done.` is a problem.
4. Check again:
   ```
   dotnet ef migrations list --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api
   ```
   - You should see the same three lines, now **without** `(Pending)`.
5. **Switch the string off. This matters:**
   ```
   Remove-Item Env:ConnectionStrings__KvitDatabase
   Set-Clipboard -Value ' '
   ```
   Then close this PowerShell window. (If you left the string set, your local app would talk to the online database.)
6. Look in Neon: open the project → **Tables** (in the left menu). You should see `users`, `usage_events`, `data_protection_keys` and the other Identity tables, all empty.

## 4. Give GitHub the DIRECT string (the secret)
Follow `docs/guides/free-hosting-setup.md`, **B5**:
- Repository `kvit` → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**.
- **Name:** `NEON_DIRECT_CONNECTION_STRING` (exactly, capital letters).
- **Secret:** paste the **DIRECT** line from your notes file (one line, nothing before or after it) → **Add secret**.
- **You should see:** the secret in the list. GitHub never shows its value again. That is normal.

**Why now:** when you later merge the PR, GitHub runs the migration step on `main`. Without this secret that step fails on purpose with a clear message, and Render would not deploy.

## 5. One more safety check (1 minute)
Repository → **Settings** → **Advanced Security** (may be called "Code security") → **Push protection** should say **Enabled**. If not, enable it (free-hosting guide B1 step 3).

## What to tell the Claude session when done
- Whether step 3 ended with `Done.`, and what step 4 showed.
- Whether Neon's **Tables** list shows the tables.
- Whether the GitHub secret is in the list and push protection is on.
- If anything printed an error: the error text only, never the string.

## If something goes wrong
| What you see | What it usually means |
|---|---|
| `The setting ConnectionStrings:KvitDatabase is missing or empty` | Step 1 did not work: the copy was empty. Copy the DIRECT line again and redo step 1 |
| `Failed to connect` / `timeout` right after a quiet period | Neon was asleep (the free database sleeps after 5 minutes). Run the same command again |
| `password authentication failed` | The password in the line is wrong. Copy it again from Neon (Connect → show password) into your notes file |
| An SSL / certificate error | Tell the Claude session the exact text. It may need a different `SSL Mode` value. Don't change it yourself |
| `dotnet ef` is not found | Run `dotnet tool restore` once in the Kvit folder, then repeat |

---

# Part 2 (Step 3): coming next
The online certificate, the proxy secret, the Render service and Cloudflare Pages. The Claude session writes it after Part 1 is done.
