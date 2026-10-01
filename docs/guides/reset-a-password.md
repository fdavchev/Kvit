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

## For the online database (later, Phase 5)
The script uses the connection string from your local secrets, or from the environment variable `ConnectionStrings__KvitDatabase` when it is set (the variable wins). Pointing it at the online database needs the online connection string; Phase 5 writes the exact steps. Never paste that string into the repository or into a chat.

**Note:** the "choose a new password" screen arrives with the frontend (Phase 4, Step 4). Until then the script works, but a person cannot yet finish the change in the browser.
