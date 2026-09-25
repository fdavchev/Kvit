# Research: hosting setup facts, click by click (2026-09-25)

Done by the `researcher` subagent (Opus) with web search on 2026-09-25. It fed `guides/free-hosting-setup.md`.
Claims with a source are **VERIFIED by live web check** on that date. Anything marked **NOT VERIFIED** couldn't be confirmed on an official page. Nothing was run: no accounts were created and no deploys were tested.

## Key facts
- **Card:**
  - Neon says "no credit card required" ([pricing](https://neon.com/pricing)).
  - Render treats "no payment method" as normal. If the free bandwidth runs out without a card, the free services are suspended for the rest of the month, not charged ([Render free](https://render.com/docs/free)).
  - Cloudflare: no official page found either way (NOT VERIFIED).
- **Neon:**
  - Region **AWS Europe (Frankfurt), `aws-eu-central-1`**. It can't be changed later ([regions](https://neon.com/docs/introduction/regions)).
  - Postgres 14–18 are supported.
  - The **pooled** connection (host contains `-pooler`) is for the app. The **direct** one is for EF migrations ([EF migrations](https://neon.com/docs/guides/entity-migrations)).
  - Npgsql needs the **key=value** format, not `postgresql://` ([npgsql#2090](https://github.com/npgsql/npgsql/issues/2090)). Neon's .NET example is `Host=...;Database=...;Username=...;Password=...;SSL Mode=VerifyFull;Channel Binding=Require` ([Neon Npgsql](https://neon.com/docs/guides/dotnet-npgsql)).
  - Through the pooler, add `No Reset On Close=true` ([Npgsql](https://www.npgsql.org/doc/compatibility.html)).
  - The free plan has a **6-hour** restore window and 10 branches ([plans](https://neon.com/docs/introduction/plans)).
- **Render:**
  - **+ New → Web Service → Git Provider**, Language **Docker**, Region **Frankfurt** (can't be changed later), Instance **Free** ([web services](https://render.com/docs/web-services), [regions](https://render.com/docs/regions)).
  - Render tells the app to use `PORT=10000`, but .NET containers listen on **8080**. Set `ASPNETCORE_HTTP_PORTS=10000` or `PORT=8080` ([MS](https://learn.microsoft.com/en-us/dotnet/core/compatibility/containers/8.0/aspnet-port)).
  - Environment variables are also passed to the Docker build, so a secret must never be referenced in the Dockerfile ([Render Docker](https://render.com/docs/docker)).
  - Auto-Deploy has an **"After CI Checks Pass"** option ([deploys](https://render.com/docs/deploys)).
  - The health check path is set under **Settings** ([health](https://render.com/docs/health-checks)).
- **Cloudflare Pages:**
  - **Workers & Pages → Create application → Pages → Connect to Git.**
  - Preset **React (Vite)**, build `npm run build`, output `dist`, root `src/web` ([build config](https://developers.cloudflare.com/pages/configuration/build-configuration/)).
  - Default Node is 22.16.0 ([build image](https://developers.cloudflare.com/pages/configuration/build-image/)).
  - The `functions/` folder goes inside the root directory, `src/web/functions/`. It must not be inside `dist` ([functions](https://developers.cloudflare.com/pages/functions/get-started/)). That this holds for a monorepo root directory comes from community sources only (NOT VERIFIED).
  - Variables are set under **Settings → Variables and Secrets** and need a redeploy to take effect ([bindings](https://developers.cloudflare.com/pages/functions/bindings/)).
  - Unknown paths go to the app automatically if there's no `404.html` ([serving](https://developers.cloudflare.com/pages/configuration/serving-pages/)).
  - Preview deployments are public.
  - Cloudflare now says "start new projects with Workers", but Pages still works ([Pages](https://developers.cloudflare.com/pages/get-started/)).
- **Google:**
  - The menu is now called **Google Auth Platform**, with pages **Overview / Branding / Audience / Clients / Data Access / Verification Center**.
  - Audience **External**. Scopes: only `openid`, `email`, `profile`.
  - A **privacy policy link is required** for production apps. **Authorized domains** must be added first ([branding](https://support.google.com/cloud/answer/15549049?hl=en)).
  - The client type is **Web application**. **Authorized JavaScript origins:** `http://localhost`, `http://localhost:5173` and `https://<project>.pages.dev`. No redirect URI and no client secret are needed ([GIS](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid)).
  - The API checks the token's `aud`, `iss` and `exp`, and uses `sub` as the user id ([verify](https://developers.google.com/identity/gsi/web/guides/verify-google-id-token)).
  - Whether `pages.dev` can be used as an authorized domain is NOT VERIFIED.
- **GitHub:**
  - Secrets are under **Settings → Secrets and variables → Actions** ([secrets](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets)).
  - Secret scanning is automatic on public repos. **Repo push protection is off by default**; turn it on under **Settings → Advanced Security → Secret Protection** ([push protection](https://docs.github.com/en/code-security/secret-scanning/introduction/about-push-protection)).
  - **Artifacts on a public repo can be downloaded by anyone who can read it** ([artifacts](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts)).
  - Scheduled workflows are **switched off after 60 days** without repo activity ([docs](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/disable-and-enable-workflows)).

## Traps for the building session
- **Keep two connection strings:** pooled for the app, direct for migrations. Don't run migrations at startup over the pooled connection.
- **Render ends HTTPS before the app,** so use forwarded headers or always-Secure cookies. Whether Render sends `X-Forwarded-Proto` is NOT VERIFIED. In .NET 10 use `KnownIPNetworks`; `KnownNetworks` is obsolete ([MS](https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/10/ipnetwork-knownnetworks-obsolete)).
- **Google sign-in in the browser:**
  - The site's security rules (Content Security Policy) must allow `accounts.google.com/gsi/*`.
  - If FedCM is off, the sign-in popup also needs the header `Cross-Origin-Opener-Policy: same-origin-allow-popups`.
  - The Google button won't work on preview addresses.
- **Cloudflare:** set `API_ORIGIN` for **Preview** as well as Production.
