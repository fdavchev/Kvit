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

## Part 2: after the pull request is merged and CI is green
1. **Branding:** App home page `https://kvit-mk.pages.dev`, Privacy policy link `https://kvit-mk.pages.dev/privacy`. Save.
2. **Audience → Publish app → Confirm.** **You should see:** **In production**. (Nothing needs Google's review because only the basic sign-in scopes openid, email and profile are used.)
3. Phone test on the real site: Continue with Google → check the name → Continue → you are in. Log out. Continue with Google again → you are in at once. Settings → Set a password → log out → Log in with email and that password.
4. Things that are expected: Google's window shows `kvit-mk.pages.dev` instead of the name Kvit (accepted on 2026-10-01); the button does not work on test addresses like `abc123.kvit-mk.pages.dev`.
