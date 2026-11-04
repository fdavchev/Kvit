# Make your account an admin (by hand)

The Statistics page is only for admins. An admin is an account with the **admin role** (a label stored in the database next to the account). Nobody can give it from the app: you give it with a small script on your PC, by email. You do it once on your PC's database and once on the online database.

You stay logged in. The change counts from your next click or page load.

## What you need
- Docker Desktop open, with the `kvit-postgres` container running (green dot).
- A terminal in the Kvit folder (`C:\Users\Davchev\Projects\Kvit`).
- The email of your Kvit account (the one you log in with, or your Google email if you use Google). Capital letters do not matter.
- The account must already exist: sign up in the app first if you have not.

## Steps (your PC)
1. In the terminal, in the Kvit folder, run (change the email):
   ```
   dotnet run scripts/GrantAdmin.cs -- filip@example.com
   ```
   The first time it takes about a minute, because it builds first.
2. **You should see**, after the build:
   ```
   filip@example.com is now an admin. This counts from their next request; they do not need to log in again.
   ```
3. Running it again is safe. It says the same thing and changes nothing.

## Take the admin role away
Same command with `--revoke` at the end:
```
dotnet run scripts/GrantAdmin.cs -- filip@example.com --revoke
```
**You should see** `filip@example.com is no longer an admin. ...`. The account stays logged in, it only loses the admin pages.

## If something goes wrong
- `No account has the email '...'. Nothing changed.`: the email is spelled differently from the account, or the account does not exist yet. A guest (someone who joined by a link without an account) has no email, so it cannot be an admin.
- `Usage, from the repository root: ...`: the email is missing, or there is something extra after it. Only `--revoke` may follow the email.
- `The setting ConnectionStrings:KvitDatabase is missing or empty`: do `docs/guides/phase-04-local-setup.md`, Part 1, Step 4.
- `Failed to connect` or `Connection refused`: Docker Desktop or the database container is not running.
- Anything red you do not understand: copy the text and send it to me.

## For the online database (kvit-mk.pages.dev)
The script uses the connection string from your local secrets, or from the environment variable `ConnectionStrings__KvitDatabase` when it is set (the variable wins). For the online database you set that variable, in one PowerShell window only, to the **DIRECT** Neon line from your private notes file. Never paste that line into the repository or into a chat.

**What you need:** the notes file (`Desktop\secrets\kvit-secrets.txt`), your email, and a PowerShell in `C:\Users\Davchev\Projects\Kvit`. Docker Desktop is **not** needed. The Kvit API must not be running on your PC.

1. Run (it waits for you; the cursor blinks after the text):
   ```
   $env:ConnectionStrings__KvitDatabase = (Read-Host 'Paste the DIRECT line here, then press Enter').Trim()
   ```
2. In Notepad click inside the `Host=...` line under `DIRECT:`, press Home, then Shift+End, then Ctrl+C. Back in PowerShell **right-click** inside the window and press Enter.
3. Run the script with your email (change it):
   ```
   dotnet run scripts/GrantAdmin.cs -- filip@example.com
   ```
   **You should see** the same line as on your PC (`... is now an admin ...`). If Neon was asleep, the first run can take a few seconds longer.
4. **Switch the online connection off, then close the window:**
   ```
   Remove-Item Env:ConnectionStrings__KvitDatabase
   ```
   If you leave it set, anything you run in that window would talk to the online database.

If it says `No account has the email '...'. Nothing changed.`, the email is spelled differently or you have no account on the online site yet. If it cannot connect, check the DIRECT line is on one line with the password and no `-pooler` in the host, and run it again after a minute (Neon may be waking up).
