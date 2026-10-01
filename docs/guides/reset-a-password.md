# Reset a friend's password (by hand)

For when someone writes "I forgot my password". Kvit has no email yet, so you do it with a small script on your PC. You never see their real password: you only make a **temporary** one, read it out to them, and they must choose their own right after they log in with it.

Nobody can ever read a password from the database. It only keeps a scrambled version (a hash).

## What you need
- Docker Desktop open, with the `kvit-postgres` container running (green dot).
- A terminal in the Kvit folder (`C:\Users\Davchev\Projects\Kvit`).
- The email the person signed up with. Capital letters do not matter: `Ana@Example.com` finds `ana@example.com`.

## Steps
1. In the terminal, in the Kvit folder, run (change the email):
   ```
   dotnet run scripts/ResetPassword.cs -- ana@example.com
   ```
   The first time it takes about a minute, because it builds first.
2. **You should see**, after the build:
   ```
   The password of ana@example.com was reset, the account was unlocked and its other sessions were signed out.
   Temporary password (shown only this once, it is not saved anywhere):
   <12 letters and digits>
   Give it to the person. After logging in with it they must choose their own password.
   ```
3. Tell the person the temporary password (a call or a message is fine; it works only until they set their own). They log in with their email and the temporary password, and Kvit sends them straight to a "choose a new password" screen.
4. If you lose the temporary password, run the same command again. It makes a new one and the old one stops working.

## What the script does to the account
- Makes the temporary password and saves only its scrambled version.
- Unlocks the account if it was locked for wrong passwords.
- Signs the person out everywhere they were logged in.
- Forces them to choose a new password at the next log-in.

## If something goes wrong
- `No account has the email '...'. Nothing changed.`: the email is spelled differently from the one they signed up with. Ask them. Nothing was changed.
- `The setting ConnectionStrings:KvitDatabase is missing or empty`: do `docs/guides/phase-04-local-setup.md`, Part 1, Step 4.
- `Failed to connect` or `Connection refused`: Docker Desktop or the database container is not running.
- Anything red you do not understand: copy the text and send it to me.

## For the online database (the real users on kvit-mk.pages.dev)
The script uses the connection string from your local secrets, or from the environment variable `ConnectionStrings__KvitDatabase` when it is set (the variable wins). For the online database you set that variable, in one PowerShell window only, to the **DIRECT** Neon line from your private notes file. Never paste that line into the repository or into a chat.

**What you need:** the notes file (`Desktop\secrets\kvit-secrets.txt`), the person's email, and a PowerShell in `C:\Users\Davchev\Projects\Kvit`. Docker Desktop is **not** needed. The Kvit API must not be running on your PC.

1. Run (it waits for you; the cursor blinks after the text):
   ```
   $env:ConnectionStrings__KvitDatabase = (Read-Host 'Paste the DIRECT line here, then press Enter').Trim()
   ```
2. In Notepad click inside the `Host=...` line under `DIRECT:`, press Home, then Shift+End, then Ctrl+C. Back in PowerShell **right-click** inside the window and press Enter.
3. Run the script with the person's email (change it):
   ```
   dotnet run scripts/ResetPassword.cs -- ana@example.com
   ```
   **You should see** the same lines as locally (`The password of ... was reset ...`, then the temporary password once). If Neon was asleep, the first run can take a few seconds longer.
4. **Switch the online connection off, then close the window:**
   ```
   Remove-Item Env:ConnectionStrings__KvitDatabase
   ```
   If you leave it set, anything you run in that window would talk to the online database.
5. Tell the person the temporary password. They log in on the site and must choose their own.

If it says `No account has the email '...'. Nothing changed.`, the email is spelled differently. If it cannot connect, check the DIRECT line is on one line with the password and no `-pooler` in the host, and run it again after a minute (Neon may be waking up).
