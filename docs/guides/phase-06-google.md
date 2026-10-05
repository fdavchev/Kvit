# Phase 6: your Google Cloud steps (plain words)

Do these in order. Nothing here costs money or asks for a card. If Google ever asks for billing or a card, stop and tell the Claude session.

## Part 1: now (before the frontend is built, about 10 minutes)
This continues Part A4 and Part B4 of `free-hosting-setup.md`. If you already did A4 (project `kvit`, Google Auth Platform, External), start at step 1. If the project does not exist yet, do A4 first.

1. Open **console.cloud.google.com**, make sure the project **kvit** is selected at the top.
2. Search **Google Auth Platform** → **Branding**. You saved the authorized domain `kvit-mk.pages.dev` here earlier. Check it is still there. Leave the privacy link empty for now.
3. **Data Access:** it is fine (and normal) if all three lists say "No rows to display". Sign-in with Google always includes **openid**, **email** and **profile** on its own, so nothing needs to be added. Just don't add any other scope.
4. **Clients** → **Create client**:
   - **Application type:** Web application. **Name:** `Kvit web`.
   - **Authorized JavaScript origins:** add all four, with no slash at the end and no path:
     - `http://localhost`
     - `http://localhost:5173`
     - `http://localhost:8788`
     - `https://kvit-mk.pages.dev`
   - **Authorized redirect URIs:** leave empty.
   - Click **Create**.
   - **You should see:** a **Client ID** ending in `.apps.googleusercontent.com`.
5. Google may take a few minutes (sometimes longer) before a new origin works. That is normal.
6. **Audience:** look at **Publishing status** at the top. If it says **Testing**, only the people on the **Test users** list (further down the same page) can sign in: click **Add users** and add your own Gmail. If it already says **In production**, there is no test-user list and anybody with a Google account can sign in, so there is nothing to add.
7. The Client ID is **not secret** (every visitor's browser sees it); Kvit puts it in the code. The downloaded `client_secret_...json` file also holds the **client secret**, which Kvit never uses: keep it only in the secrets folder (outside the repository) or delete it.

## Part 1b: try it on your own PC (before the merge)
You need: your Gmail on the **Test users** list (Part 1, step 6), Docker Desktop open, and the local database running.
1. **Docker Desktop:** open it, wait for "Engine running".
2. **One command** (terminal in the project folder, the one with `Kvit.slnx`): `.\scripts\start-local.ps1`. **You should see:** it starts the database, then two new windows ("Kvit API" and "Kvit website"), then `Kvit is running.` and your browser opens `http://localhost:5173`. Use exactly `localhost:5173`, because that is the address Google knows. (If it says a port is already in use, an old copy is running: run `.\scripts\start-local.ps1 -Stop` first.) If it prints a yellow `WARNING` that the local database is missing updates, it still starts; once it is running, run the command the warning shows (from the project folder). When you are done: `.\scripts\start-local.ps1 -Stop`.
3. **The "email already has an account" test:** tap **Sign up with email**, make an account with **your Gmail address** and a password like `Sunce2026`. Open **Settings → Log out**. Now tap the Google button and pick the same Gmail. **You should see:** the pop-up "This email already has an account." Tap **Log in with password**: the Log in screen opens with your email already typed; type the password and you are in.
4. **The first Google sign-in test:** delete that test account so Google can create a fresh one. In another terminal in the same folder: `docker exec kvit-postgres psql -U kvit -d kvit -c "delete from users where normalized_email = upper('YOUR@GMAIL.COM');"` (put your Gmail in, keep the quotes). **You should see:** `DELETE 1`. Back in the browser, tap the Google button, pick your Gmail. **You should see:** the screen "Almost there" with your Google name filled in. Tap **Continue**: you land on the home screen.
5. **Set a password:** open **Settings**. You should see **Set a password** (not "Change password"). Open it, try Save with nothing in the box (you should see the red password rule), then type `Sunce2026` and Save: you are back in Settings and it now says **Change password**.
6. **Log in both ways:** **Log out**, tap the Google button: you should be in at once with no name screen. **Log out**, then Log in with your email and `Sunce2026`: you are in.
7. **Language:** switch to МК on Welcome and repeat steps 3–6 quickly if you want to see the Macedonian texts. Also check the **Privacy** link on Welcome and in Settings.
8. **If the Google button does not appear or says "origin not allowed":** the address in the browser is not exactly `http://localhost:5173`, or Google has not yet picked up the new origin (wait 5–10 minutes). **If Google says "access blocked" or "app not verified":** your Gmail is not on the Test users list (Part 1, step 6).
9. This does **not** test the security headers: they only work on Cloudflare Pages. They are checked after the branch is pushed (the Cloudflare preview address), see Part 2.

## Part 2: after the pull request is merged and CI is green
1. **Branding:** App home page `https://kvit-mk.pages.dev`, Privacy policy link `https://kvit-mk.pages.dev/privacy`. Save.
2. **Audience → Publish app → Confirm.** **You should see:** **In production**. (Nothing needs Google's review because only the basic sign-in scopes openid, email and profile are used.)
3. Phone test on the real site: Continue with Google → check the name → Continue → you are in. Log out. Continue with Google again → you are in at once. Settings → Set a password → log out → Log in with email and that password.
4. Things that are expected: Google's window shows `kvit-mk.pages.dev` instead of the name Kvit (accepted on 2026-10-01); the button does not work on test addresses like `abc123.kvit-mk.pages.dev`.
