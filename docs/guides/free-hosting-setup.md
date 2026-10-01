# Putting Kvit online for free: step by step

This guide puts Kvit on the internet using only free services, none of which asks for a bank card.
It has two parts:
- **Part A: do it any time, even today.** Creating the accounts and the database. No code needed.
- **Part B: do it when the building session says "ready to go online".** Connecting the code to the accounts.

> Written 2026-09-25 from the providers' own help pages. Their screens change, so if a button has a slightly different name, pick the one that means the same thing. Items marked **(NOT VERIFIED)** couldn't be confirmed on the official page. Sources: `docs/reports/2026-09-25-hosting-setup-facts.md`.

---

## Before you start: two safety rules
1. **Make a private notes file OUTSIDE the Kvit folder**, e.g. `Documents\kvit-secrets.txt`. Passwords and connection strings go there, and only there.
   - **Never** put them in the Kvit folder, never in the code, and **never paste them into the Claude chat**.
   - When the building session needs one, it will ask you to paste it straight into Render or Cloudflare yourself.
2. **Whenever a site offers a paid plan, "upgrade" or "add payment method", click away.** Everything here works on the free plan.

**The whole picture in one sentence:** your phone opens `kvit-mk.pages.dev` (Cloudflare) → Cloudflare quietly passes every `/api` request on to `kvit-mk-api.onrender.com` (Render, which runs the .NET backend) → Render talks to the database on Neon.

---

# PART A: accounts and database (any time)

## A1. Neon: the database
1. Go to **console.neon.tech** and click **Sign up**. Picking **GitHub** is easiest.
   - **You should see:** a welcome screen and no request for a card. Neon says: "no credit card required".
2. Click **New Project** and fill in:
   - **Project name:** `kvit`
   - **Region:** **AWS Europe (Frankfurt)** (`aws-eu-central-1`). ⚠️ **This can't be changed later.** Frankfurt is the closest to Macedonia.
   - **Postgres version:** choose **18**. It is the same version as the local database (`compose.yaml`) and the tests, so a migration behaves the same everywhere (decided 2026-10-01).
   - Leave any extra services (Neon Auth, storage, functions) **off**. Kvit has its own login.
   - Click **Create project**.
   - **You should see:** a project dashboard called "kvit".
3. **Get the two connection strings** (the "address + password" of the database):
   1. Click **Connect** on the project dashboard.
   2. Make sure **Connection pooling is ON**. Look for a .NET / C# option in the list of examples and pick it if it's there. Copy the text into your secrets file under the heading `POOLED (for the app)`. It contains `-pooler` in the host part.
   3. Turn **Connection pooling OFF** and copy again, under the heading `DIRECT (for migrations)`. It **doesn't** contain `-pooler`.
   - ⚠️ **If what you copied starts with `postgresql://`**, that's the web-address format, and .NET can't read it. It needs to look like this:
     ```
     Host=ep-something-pooler.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=YOUR_PASSWORD;SSL Mode=VerifyFull;Channel Binding=Require
     ```
     The pieces map like this: `postgresql://USERNAME:PASSWORD@HOST/DATABASE?...`
     Rewrite it by hand in your secrets file and double-check every piece.
4. **Done for now.** The building session creates the tables automatically later (these are called "migrations").

**What's free:** 0.5 GB of storage, 100 compute hours a month, and a 6-hour "undo" window. The database sleeps after 5 minutes without use and wakes up by itself.

## A2. Render: where the backend will run
1. Go to **dashboard.render.com/register** and sign up with **GitHub**.
   - **You should see:** an empty dashboard, with no card requested.
2. That's all for now. The service itself is created in Part B, once there's code.

## A3. Cloudflare: where the website will run
1. Go to **dash.cloudflare.com** and click **Sign up** (email + password). Confirm your email.
   - ⚠️ If it ever asks for a card for **Pages**, stop and tell the Claude session. It shouldn't (NOT VERIFIED on an official page).
2. That's all for now. The project is created in Part B.

## A4. Google Cloud: only for the "Sign in with Google" button
1. Go to **console.cloud.google.com** and sign in with your Google account.
2. At the top, click the **project picker** → **New project** → name it `kvit` → **Create**. ⚠️ If it offers to set up **billing**, skip it. Sign-in doesn't need billing.
3. In the search bar, type **Google Auth Platform** and open it. Click **Get started**:
   - **App Information:** App name `Kvit`, User support email = your Gmail.
   - **Audience:** **External**.
   - **Contact Information:** your Gmail.
   - Tick the agreement and click **Create**.
   - **You should see:** a menu with **Overview, Branding, Audience, Clients, Data Access**.
4. **Stop here for now.** The rest (the web address, the privacy page, publishing) is in Part B, because it needs the live website address.

---

# PART B: going online (when the building session says so)

The exact values (folder names and the names of the settings) were filled in during Phase 5 (2026-10-01), when this guide was used for real. The step-by-step version for Kvit itself is `phase-05-go-online.md`.

## B1. GitHub: the public repository
1. On github.com, click **+** (top right) → **New repository**.
   - **Name:** `kvit`
   - **Public**
   - **Don't** tick README, .gitignore or license. The code already has them.
   - Click **Create repository**.
2. The building session gives you the commands to upload the code. You run them yourself, as usual.
3. **Turn on push protection** (it blocks uploading a password by accident):
   - Open the repository → **Settings** → in the left menu under **Security**, open **Advanced Security** (it may be called "Code security") → find **Push protection** → **Enable**.
   - **You should see:** Push protection "Enabled".

## B2. Render: put the backend online
1. In Render, click **+ New** (top right) → **Web Service** → **Git Provider** → **GitHub**. Allow Render to see the `kvit` repository (choose **Only select repositories** → `kvit`). Then select `kvit` and click **Connect**.
2. Fill in:
   | Field | Value |
   |---|---|
   | Name | `kvit-mk-api` (this becomes `kvit-mk-api.onrender.com`; `kvit-api` is already taken, checked 2026-09-25) |
   | Region | **Frankfurt** ⚠️ can't be changed later |
   | Branch | `main` |
   | Language | **Docker** |
   | Root Directory | leave empty, unless the session says otherwise |
   | Dockerfile Path | `src/api/Dockerfile` |
   | Instance Type | **Free** |
3. **Environment Variables:** click **Add Environment Variable** for each one (two underscores `__` where the setting has a dot or colon):
   | Name | Value |
   |---|---|
   | `ASPNETCORE_HTTP_PORTS` | `10000` (makes .NET listen where Render expects) |
   | `ConnectionStrings__KvitDatabase` | your **POOLED** connection string from A1 (ending in `;No Reset On Close=true`) |
   | `DataProtection__CertificateBase64` | the online certificate (made with `scripts/NewDataProtectionCertificate.cs`; keep a copy in your notes file, losing it logs everybody out) |
   | `DataProtection__CertificatePassword` | the certificate's password |
   | `Proxy__SharedSecret` | the **proxy secret** (see «Make the proxy secret» below; the **same** value goes into Cloudflare in B3). Without it the app refuses to start, on purpose |
   | the Google client id setting | added in Phase 6 (B4); its name is given then |
4. Click **Deploy Web Service**.
   - **You should see:** a log scrolling by, then **"Live"** in green (the first build can take 5–10 minutes).
   - Your backend address is at the top: `https://kvit-mk-api.onrender.com`. Copy it to your secrets file.
5. After it's live, go to **Settings**:
   - **Health Check Path:** `/health`
   - **Auto-Deploy:** **After CI Checks Pass**, so it only goes online when the tests passed on GitHub. (From the Phase 5 merge on, Kvit's CI deploys by itself and shows the result on GitHub, so Auto-Deploy is set to **Off**; see `phase-05-go-online.md`, Part 4.)
6. **Check:** open `https://kvit-mk-api.onrender.com/health` in your browser. It should show a short "healthy" message. If it takes about a minute, that's the free server waking up. That's normal.

### Make the proxy secret (once, before B2 step 3)
1. Open PowerShell (any folder) and paste these two lines:
   ```
   $b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
   ($b | ForEach-Object { $_.ToString('x2') }) -join ''
   ```
   **You should see:** one line of 64 letters and digits.
2. Copy that line into your secrets file under the heading `PROXY SECRET`, then paste it from there into both places (Render in B2, Cloudflare in B3). Never put it in the repository or in a chat. If you ever think it leaked, make a new one and change it in **both** places, then redeploy both.

## B3. Cloudflare Pages: put the website online
1. In Cloudflare, open **Workers & Pages** → **Create application** → **Pages** → **Connect to Git** (it may say "Import an existing Git repository"). Log in to GitHub and click **Install & Authorize**, allowing only the `kvit` repository. Select `kvit` and click **Begin setup**.
2. Fill in:
   | Field | Value |
   |---|---|
   | Project name | `kvit-mk` (becomes `kvit-mk.pages.dev`; `kvit` and `kvit-app` are already taken, checked 2026-09-25. Backups that looked free: `kvitsme`, `mojkvit`) |
   | Production branch | `main` |
   | Framework preset | **React (Vite)** |
   | Build command | `npm run build` |
   | Build output directory | `dist` |
   | Root directory (advanced) | `src/web` |
3. Under **Environment variables (advanced)**, add:
   - `API_ORIGIN` = your Render address from B2 (e.g. `https://kvit-mk-api.onrender.com`, **no slash at the end**).
   - `API_PROXY_SECRET` = the same **proxy secret** as `Proxy__SharedSecret` in Render (B2). Use the **Encrypt** option if Cloudflare offers it for this variable. It is the password that tells Kvit's server "this request came through my own website, not straight from the internet". Nothing works without it: the website answers 500.
   - `NODE_VERSION` = `24` (Cloudflare's build machine defaults to Node 22.16, which is too old for React Router 8.4; the frontend pins Node 24 itself via `src/web/.nvmrc`, but Cloudflare's own build step needs this variable too).
   - Nothing else is needed. Cloudflare's new dashboard hides Pages behind the link "Need to use the legacy Pages workflow? **Continue to Pages**" on the "Create an app" page; the big "Continue with GitHub" button there is the Workers flow, which is the wrong one. Set `API_PROXY_SECRET` to the type **Secret**, not Text.
4. Click **Save and Deploy**.
   - **You should see:** a build log, then **Success**, and your address `https://kvit-mk.pages.dev`.
5. Go to **Settings → Variables and Secrets**. Make sure `API_ORIGIN` is set for **both Production and Preview**. ⚠️ **After any change here, redeploy:** **Deployments** → the latest one → **⋯** → **Retry deployment**.
6. **Check:** open `https://kvit-mk.pages.dev`. The Kvit start screen appears.

## B4. Google: finish the sign-in setup
You need the live address from B3 **and** the privacy page (the building session builds it at `https://kvit-mk.pages.dev/privacy`).
1. **Google Auth Platform → Branding:**
   - **App home page:** `https://kvit-mk.pages.dev`
   - **Privacy policy link:** `https://kvit-mk.pages.dev/privacy` (required before publishing)
   - **Authorized domains:** add `kvit-mk.pages.dev`. ⚠️ If Google refuses this domain, stop and tell the Claude session. It's a known question mark (NOT VERIFIED).
   - Click **Save**.
2. **Data Access:** make sure only **openid**, **email** and **profile** are listed. Add nothing else. More would trigger a long Google review.
3. **Clients** → **Create client**:
   - **Application type:** **Web application**, Name: `Kvit web`.
   - **Authorized JavaScript origins:** add all three:
     - `http://localhost`
     - `http://localhost:5173`
     - `https://kvit-mk.pages.dev`

     (No slash at the end, no path.)
   - **Authorized redirect URIs:** leave empty.
   - Click **Create**.
   - It shows a **Client ID** (ends with `.apps.googleusercontent.com`). Copy it to your secrets file. It also shows a **client secret**: Kvit doesn't use it, so you can ignore it.
4. **Audience** → **Publish app** → confirm. **You should see:** status **In production**.
5. Put the Client ID where the session tells you: in Render (B2 step 3), and in Cloudflare if the session says so. Then redeploy both.
6. **Check:** on `https://kvit-mk.pages.dev`, the "Sign in with Google" button opens Google's window and logs you in. The button won't work on test addresses like `abc123.kvit-mk.pages.dev`. That's expected.

## B5. GitHub secrets (for the automatic tests and, later, backups)
Kvit uses these (Phase 5): the secrets `NEON_DIRECT_CONNECTION_STRING` (the DIRECT Neon string, for the CI migration step) and `RENDER_API_KEY` (a Render API key, for the CI deploy step), and the variable `RENDER_SERVICE_ID` (the `srv-...` id of the Render service; not secret). In the repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret** → **Name** «from the session» and **Secret** = the value → **Add secret**.
- ⚠️ On a public repository, anything the automatic jobs *save as a file* can be downloaded by anyone. That's why backups must be encrypted (see `BACKLOG.md`).

---

# Once a month: is it still free? (5 minutes)
- **Neon:** open the project → **Usage/Billing**. Plan = **Free**. Storage is under 0.5 GB and compute under 100 hours.
- **Render:** **Billing**. Plan = **Hobby / Free**, and no payment method added. The free hours (750 a month) aren't used up.
- **Cloudflare:** **Workers & Pages** → overview. Requests are well under 100,000 a day. Plan = **Free**.
- **GitHub:** the repository → **Actions**. The latest runs are green.
- **Google:** **Google Auth Platform → Overview**. No warnings.
- If any page shows a warning or asks you to upgrade, **don't click upgrade**. Screenshot it and ask in the Claude session.

# If something goes wrong
| What you see | What it usually means |
|---|---|
| Render deploy fails with "no open ports detected" | `ASPNETCORE_HTTP_PORTS=10000` is missing (B2 step 3) |
| The backend log says the connection string format is invalid | You pasted the `postgresql://` version. Use the `Host=...;` format (A1 step 3) |
| The website loads but every action fails | `API_ORIGIN` is wrong or missing, or you didn't redeploy after setting it (B3 steps 3–5) |
| First visit takes about a minute | The free server was asleep. Normal |
| The Google button shows "origin not allowed" | The address isn't in **Authorized JavaScript origins** exactly (B4 step 3). Check there's no slash at the end |
| GitHub refuses your push with "secret detected" | Good, push protection caught a password. Don't force it. Ask the session how to remove it |
