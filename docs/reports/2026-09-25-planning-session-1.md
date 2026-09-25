# Report: planning session 1 (2026-09-24 → 2026-09-25)

## What was asked
Filip wanted to choose a new project and plan it completely (about 90% of the features decided) before any coding. Everything must be free, with no bank card for any service.

## Outcome
- **The app is Kvit.** It's a shared expense splitter plus a personal budget, for Filip, his family and close friends.
- **The stack:**
  - Backend: ASP.NET Core (.NET 10) + EF Core + PostgreSQL.
  - Frontend: React + TypeScript (Vite).
  - Hosting: Neon (database), Render (API) and Cloudflare Pages (frontend, with a proxy function).
  - A public GitHub repository.
- **Where the decisions are:** all of them, with the reasons and the rejected options, are in `docs/DECISIONS.md`. The later features are in `docs/BACKLOG.md`.
- **Other ideas** are parked in `C:\Users\Davchev\Projects\Ideas For Later\IDEAS.md`.

## Files created
| File | What it is |
|---|---|
| `docs/DECISIONS.md` | Every decision from this session |
| `docs/BACKLOG.md` | The "to be done later" list |
| `docs/ROADMAP.md` | The planning checklist. The build roadmap comes after ARCHITECTURE.md arrives |
| `docs/STATUS.md` | Where we stopped |
| `docs/reports/2026-09-24-idea-and-free-hosting-research.md` | Idea candidates and free-hosting terms |
| `docs/reports/2026-09-24-competitors-and-stack-check.md` | Competitor lessons, login traps and versions |
| `docs/reports/2026-09-24-google-apple-login-and-sharing.md` | Google/Apple sign-in and Viber/WhatsApp sharing |
| `START-HERE-PROMPT.md` | **Out of date.** It gets rewritten once the plan is finished |
| `..\Ideas For Later\IDEAS.md` | Other project ideas, grouped by size and stack |

## Verification
- **No code was written and nothing was run.** There are no tests or app runs to report.
- **Research facts** were checked on the web by the `researcher` subagent on 2026-09-24/25 (**VERIFIED by live web check**), except where a report marks something NOT VERIFIED.
- **Cloudflare R2 needs a card**, according to several Cloudflare Community threads (the researcher got a 403 and relied on search summaries). It isn't stated on the official pricing page, so it's NOT VERIFIED from a primary source. R2 is rejected either way.
- **Supabase Storage needing no card** comes from third-party sites only (NOT VERIFIED).
