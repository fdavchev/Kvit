# Start-here prompt for the building session

1. Open the `Kvit` folder in VS Code and start a new Claude Code session.
2. Copy everything inside the box below and paste it as your first message.

---

```
This is Kvit, a shared expense splitter + personal budget web app. Planning is finished: every product decision is already made. Don't re-ask things that are decided; build them.

READ FIRST, IN THIS ORDER
1. docs/STATUS.md and docs/ROADMAP.md
2. docs/DECISIONS.md: every rule for the app. It's the source of truth for WHAT to build.
3. docs/BACKLOG.md: things that are NOT in the first version. Don't build them.
4. docs/ARCHITECTURE.md: the source of truth for HOW the code is structured, plus my working rules (propose then wait, never guess, no comments). Where it clashes with DECISIONS.md, follow ARCHITECTURE.md on code structure and DECISIONS.md on features, and log each clash and how you resolved it in DECISIONS.md.
5. docs/reports/: the research (versions, free-tier terms, login traps, NBRM exchange rate, Google sign-in, sharing) and the final plan review (2026-09-25-final-plan-review.md).

HARD RULES
- Everything free forever. No service that asks for a bank card at sign-up. Allowed: Neon (database), Render free web service (API in Docker), Cloudflare Pages + Pages Function proxy (frontend and /api forwarding), GitHub (public repository and Actions), Google Identity Services. Rejected: Oracle, Cloudflare R2, Sign in with Apple, Render's free Postgres, Vercel Hobby, Azure F1. If something would cost money, stop and tell me.
- No passwords, keys or connection strings in the code or the repository. Use the secret settings on Render, Cloudflare and GitHub.
- I'm a beginner at .NET and React. Explain each new concept in one or two plain sentences the first time it comes up.
- Speed is the product: every screen must need as few taps and typed fields as possible.

STEP 1: FINISH THE PLAN DOCUMENTS (short, before any code)
- The feature and screen list: every screen, what's on it, and how many taps the main action takes.
- The data model: tables, fields and relationships, following DECISIONS.md (groups, plain-name members, expenses with 4 split types, settlements pending/confirmed, currencies MKD/EUR kept separate, personal budget, recurring income, categories, activity events, change history, Data Protection keys, exchange rate). Money is stored as integer minor units.
- The build roadmap in docs/ROADMAP.md: phases from an empty repository to a deployed first version, each phase small and testable.
- docs/guides/install-tools.md and docs/guides/free-hosting-setup.md already exist (written 2026-09-25, facts in docs/reports/2026-09-25-hosting-setup-facts.md). Don't rewrite them. Wherever the hosting guide says «from the session», fill in the exact values (Dockerfile path, setting names) once the code decides them, and fix anything that turns out wrong.
- A short CLAUDE.md for this project with the stack, the hard rules above and the test commands.
Show me the data model and roadmap, and wait for my OK before coding.

STEP 2: BUILD, PHASE BY PHASE
- Create the git repository and follow my git rules (branch names, commit messages; I run the commits myself).
- Stack: .NET 10 (LTS), ASP.NET Core Web API, EF Core + Npgsql, ASP.NET Core Identity (email + password, plus Google sign-in through Google Identity Services with the ID token checked on the API), React + TypeScript + Vite, react-i18next (English + Macedonian), Docker, GitHub Actions.
- Traps already found in research, to handle from the start:
  - Store the Data Protection keys in Postgres (encrypted), because Render wipes its files on restart.
  - Log in with a cookie through the same-origin Cloudflare proxy, because Safari blocks cookies between different sites.
  - Check which TypeScript version the Vite template installs (TS 7 has tooling caveats).
- For API testing, use the built-in API reference page (e.g. Scalar) and .http files. No Postman.
- Tests are required for all the money logic (splits, rounding, debt simplification per currency, settlements) and for the API against a real Postgres database (Testcontainers).
- The offline outbox and the cold-start handling follow the exact rules in DECISIONS.md (what gets queued, unique IDs, no silent failures, "All up to date ✓").
- After each phase: run the tests, tick off ROADMAP.md, update STATUS.md, and write a short report in docs/reports/.
```
