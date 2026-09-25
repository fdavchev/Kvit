# Kvit

A shared expense splitter + personal budget web app ("Квит сме." = "we're even"), for Filip, his family and friends.
Filip is a beginner at .NET and React: explain each new concept in one or two plain sentences the first time it comes up.

## Read first
1. `docs/STATUS.md`, `docs/ROADMAP.md`: where we stopped, which phase is next.
2. `docs/DECISIONS.md`: **what** to build (source of truth for features and rules).
3. `docs/ARCHITECTURE.md`: **how** the code is shaped, plus the working rules (propose then wait, never guess, no comments).
4. `docs/DATA-MODEL.md`, `docs/SCREENS.md`: tables, money rules, screens and tap counts.
5. `docs/BACKLOG.md`: **not** in the first version. Don't build it.

## Stack
- **API:** .NET 10, ASP.NET Core Web API (controllers), EF Core 10 + Npgsql, ASP.NET Core Identity (cookie) + Google ID-token check, Scrutor, OpenAPI + Scalar. Tests: xUnit + Testcontainers.
- **Web:** React + TypeScript + Vite, TanStack Query, React Router, react-i18next (EN + MK), oxlint, Vitest.
- **Hosting:** Neon (Postgres), Render free web service (Docker), Cloudflare Pages + a Pages Function that forwards `/api/*` to Render, GitHub Actions.
- **Layout:** `src/api/` (backend projects + `Dockerfile`), `src/web/` (frontend + `functions/`), `tests/` (backend tests), `Kvit.slnx` at the root.

## Hard rules
- **Free forever. No service that asks for a bank card.** Rejected: Oracle, Cloudflare R2, Sign in with Apple, Render Postgres, Vercel Hobby, Azure F1. If something would cost money, stop and tell Filip.
- **No passwords, keys or connection strings in the code or the repository.** Local: `dotnet user-secrets`. Online: Render, Cloudflare and GitHub secret settings.
- **Speed is the product:** as few taps and typed fields as possible on every screen.
- **Money is integer minor units** (MKD in deni ×100, EUR in cents). Never `decimal` or `double` for money. MKD and EUR are never mixed.
- `/health` never touches the database, and nothing polls Neon.
- Filip runs every git commit himself. No `Co-Authored-By` trailer, no "Generated with Claude Code" line.

## Commands (planned; confirm and fix once Phases 1–2 exist)
Backend, from the repository root:
- `dotnet build Kvit.slnx`
- `dotnet test Kvit.slnx` (Docker Desktop must be running for `Kvit.Api.Tests`)
- `dotnet run --project src/api/Kvit.Api`
- Windows: a running API locks its DLLs. Stop it → build → start → test.

Frontend, from `src/web`:
- `npm run dev` · `npm run lint` · `npm run build` · `npm test`
