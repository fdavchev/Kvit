# Phase 6, Step 0: branch, baseline, stale docs (2026-10-03)

Branch `feat/06-google-sign-in`. No code changed in this step, only docs.

## Results
| Claim | Label |
|---|---|
| On `main`, clean, up to date with origin; PR #7 MERGED (2026-10-01) | VERIFIED by live run (`git`, `gh`) |
| CI run 36925617309 on `main` (commit 9a4018f): every backend and frontend step green, including "Apply the database migrations to Neon" and "Deploy to Render and wait until it is live" | VERIFIED by live run (`gh run view`) |
| Backend `dotnet build Kvit.slnx`: 0 warnings, 0 errors | VERIFIED by live run |
| Backend `dotnet test`: 161 passed, 356 failed, because Docker Desktop was closed (the tests start a Postgres container). Not a code problem | NOT VERIFIED (re-run once Docker is open; expected 517) |
| Frontend `npm run lint` (0 warnings, exit 0), `npm run build` (0 type errors), `npm test` 467/467 | VERIFIED by automated test |
| A log-in with the deleted test account `phase5-check@example.com` through `kvit-mk.pages.dev` answers 401 | VERIFIED by live run |
| Google saved `kvit-mk.pages.dev` under Authorized domains (Branding) | REPORTED by Filip (no error text; the real check is when the button is used) |

## Docs fixed in this step
- STATUS, ROADMAP (Phase 5 migration line) and the Phase 5 report: the CI migration and Render deploy steps are VERIFIED by live run (run 9a4018f).
- BACKLOG: the test-account line is removed; the Google domain line says REPORTED; three new lines (privacy contact email before Kvit grows, safe Google-to-password linking, changing the display name).
- DECISIONS: the Phase 6 decision log started (Filip's four product answers and the technical choices with rejected alternatives).
- `docs/guides/phase-06-google.md`: Filip's Google Cloud steps in plain words.

## Filip, your part
**1. Docker Desktop:** open it and wait until the whale icon says it is running. It must stay open for Step 1 (the backend tests).

**2. Terminal commands:** none this step.

**3. Only you can do:**
- Google Cloud, following `docs/guides/phase-06-google.md` (about 10 minutes): create the Web client, add your Gmail as a test user, and paste the **Client ID** in the chat. It is public, not a secret, and Step 2b needs it.
- GitHub About → Website = `https://kvit-mk.pages.dev` (if not done).
- Commit the docs. Message for the first commit of the branch:
  - **Commit subject:** `Start Phase 6: fix the stale Phase 5 docs and log the Google sign-in decisions`
  - **PR title:** `Phase 6: Google sign-in, the privacy page and security headers`
  - **PR description:**
    ```
    ## Overview
    Real "Continue with Google" sign-in on the live site. The API checks Google's ID token and keeps Google's permanent id in user_logins. A Google email that already has a password account is never merged automatically. Google accounts can add a password in Settings. Adds the public privacy page (EN + MK) that Google needs, and the security headers (CSP, COOP) that let Google's script and pop-up work without touching the /api proxy.

    ## Scope
    - Backend: token check behind an interface (fake in tests), three auth endpoints, new error codes, no migration
    - Frontend: Google button, name screen, "email already has an account" and "signs in with Google" pop-ups, Settings "Set a password", /privacy
    - Cloudflare _headers, theme script moved out of index.html
    - Docs, guide for the Google Cloud setup, reports

    ## Checklist
    - [x] Step 0: branch, baseline, stale Phase 5 docs fixed, decisions logged
    - [ ] Step 1: backend (tests first)
    - [ ] Step 2a: mockup and approved Macedonian wording
    - [ ] Step 2b: frontend, headers, privacy page
    - [ ] Step 3: Google "In production", phone test, wrap-up docs
    ```
