# Kvit

**Квит сме.** ("We're even.")

Kvit splits shared expenses with friends and family, shows who pays whom in as few payments as possible, and keeps a personal budget. It works in **Macedonian denars and euros** and in **English and Macedonian**.

> **Status: in development.** Planning is finished and the code is being built phase by phase. See the [roadmap](docs/ROADMAP.md).

## What it does
**Release 1: splitting**
- Groups for a trip, a household or a single bill. Friends without an account can be added by name, so only one person needs to sign up.
- Four ways to split: **equally** (with extras, e.g. "Marko took the better room, +600"), **exact amounts**, **percentages** and **shares**.
- Balances per currency. MKD and EUR are never mixed, and each euro expense keeps the National Bank exchange rate from the day it was saved.
- "Who pays whom", simplified to fewer payments with the same totals.
- Settling up with confirmation: "I paid Ana" counts once Ana confirms it.
- An activity feed with change history ("1,200 → 1,500"), and Undo instead of "Are you sure?" pop-ups.
- Sign in with Google or with email. Dark mode. Designed for phones first.

**Release 2: budget.** Categories and monthly limits, income (once or repeating), and your share of group expenses counted in your budget automatically.

**Release 3: polish.** An offline queue for when the server is waking up, and a usage-statistics page.

## Built for speed
Every screen is judged by how few taps it needs. Adding an expense takes **2 taps plus the amount**: the other fields start with the most common choice (paid by me, split equally, today).

## Tech stack
| Part | Technology |
|---|---|
| API | .NET 10, ASP.NET Core Web API, EF Core 10 + PostgreSQL, ASP.NET Core Identity + Google sign-in |
| Web | React, TypeScript, Vite, TanStack Query, React Router, react-i18next, Tailwind CSS + shadcn/ui |
| Tests | xUnit + Testcontainers (a real PostgreSQL in Docker), Vitest |
| Hosting | Neon (database), Render (API in Docker), Cloudflare Pages (website), GitHub Actions (CI). All free, with no card |

## Interesting parts
- **Money is whole numbers.** Amounts are stored as integer minor units (deni and cents), never floating point. Splits always add up exactly to the total, and a group's balances always add up to zero. Both rules are covered by tests.
- **Debt simplification** runs separately for each currency.
- **Clean Architecture + CQRS** with a small hand-written dispatcher instead of MediatR, rich domain entities, and a `Result` pattern instead of exceptions for business rules.
- **Free hosting that sleeps.** The API sleeps after 15 minutes on the free plan, so the website shows saved data instantly while it wakes. Scheduled work (exchange-rate refresh, closing a group after 24 hours) runs lazily on the next request instead of on a timer.
- **Login across two hosts.** The website and the API live on different domains, and Safari blocks cookies between sites. A Cloudflare Pages Function forwards `/api/*`, so the browser sees one site and a secure login cookie works everywhere.
- **Logins survive restarts.** The keys that sign login cookies are stored encrypted in PostgreSQL, because Render wipes its disk on every restart.

## Repository layout
```
src/api/     ASP.NET Core API (Api, Application, Domain, Infrastructure, Contracts) and its Dockerfile
src/web/     React app, plus the Cloudflare Pages Function in functions/
tests/       Backend tests
docs/        Decisions, architecture, data model, screens, roadmap and reports
```

## Running it locally
Instructions arrive with the first code (roadmap phases 1 and 2).

## Documentation
- [Decisions](docs/DECISIONS.md): every product rule and the reasons behind it
- [Architecture](docs/ARCHITECTURE.md): how the code is structured
- [Data model](docs/DATA-MODEL.md): tables and money rules
- [Screens](docs/SCREENS.md): every screen and its tap count
- [Roadmap](docs/ROADMAP.md) and [status](docs/STATUS.md)

## License
© 2026 Filip Davchev. All rights reserved. You're welcome to read the code, but no licence to reuse it is granted yet.
