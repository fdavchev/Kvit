# Vendors that handle Kvit data

Written 2026-11-04 from the research report (`docs/reports/2026-11-04-phase-19-legal-research.md`, sections 7 and 13). The links below come from the research and were **not opened by Claude**; "to confirm" marks what is not verified. Claude cannot sign in to any of these services.

| Vendor | What it does for Kvit | Personal data it handles | Where | Agreement and safeguard (to confirm) | Plan |
|---|---|---|---|---|---|
| Cloudflare | Hosts the website (Pages) and passes `/api/*` on to the server (Pages Function) | Everything that travels between the phone and the server, visitor internet address | Global network; US company | Data processing agreement is part of its self-serve agreement (https://www.cloudflare.com/cloudflare-customer-dpa/); says it relies on the EU-US Data Privacy Framework and standard clauses. To confirm. | Free |
| Render | Runs the API (Docker web service) | All data in transit, server logs | Region of the free plan not checked (see Filip's list); US company | DPA at https://render.com/dpa; research says certified under the EU-US Data Privacy Framework since January 2025. To confirm. Its docs say free services are for hobby projects. | Free |
| Neon | Stores the Postgres database | All stored data | AWS Europe (Frankfurt), `aws-eu-central-1`, set in `docs/guides/free-hosting-setup.md` | DPA at https://neon.com/dpa; whether it reaches free plans is unconfirmed. Free plan restores up to 6 hours back, no scheduled backups. | Free |
| Google | "Sign in with Google" button and ID-token check | Name, email, picture link, Google's ID for the person; the visitor's internet address when the Google script loads on Welcome | US company | Google's own privacy policy; Kvit's privacy page is linked in Google's console. To confirm the console settings. | Free |
| GitHub | Hosts the code, runs CI and the cleanup and backup jobs | The weekly encrypted database backup is stored as an artifact (from Phase 19 Step 8); no readable personal data | US company | GitHub terms. The repository is public, so the artifact can be downloaded by anyone signed in to GitHub: the dump is encrypted with a passphrase kept in a GitHub secret. | Free |
| National Bank of the Republic of North Macedonia (NBRM) | Source of exchange rates | None. Kvit only sends rate requests | North Macedonia | No agreement. Source must be named; Filip emails NBRM to ask whether automated daily fetching is fine. | Free |

## Filip's part (Claude cannot do it)
- Save a screenshot or PDF of each vendor's DPA page, dated, into `docs/legal/vendors/` (the folder does not exist yet; create it).
- Check in Render's dashboard whether the free plan offers Frankfurt, and write the answer here.
- Keep this list in step with `docs/legal/record-of-processing.md` and the privacy notice. A new vendor means a new line here, in the privacy notice and a new notice version.
- Check the EU-US Data Privacy Framework appeal (research: C-703/25 P) once a year; if the framework falls, the vendors' standard clauses become the safeguard.
