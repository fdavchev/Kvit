# Report: Phase 2 — Frontend skeleton

_2026-09-26_

## What this phase built
The empty React website that every later screen sits in. No real features yet — nothing gets logged in (that's Phase 4). What it does have:
- A React app (Vite, TypeScript 7) styled with Tailwind CSS v4 and shadcn/ui components, in Kvit's orange, with dark mode that follows the phone automatically.
- Two languages (English and Macedonian) with a switch on the Welcome screen that's remembered on the phone.
- A "Welcome" screen (the very first thing anyone sees) and a "page not found" screen.
- The wiring for talking to the backend API (through a local Vite proxy in development, and through a Cloudflare "function" once it's live online) — but no screen actually uses it yet beyond a background health check.
- Shared building blocks (a button, a loading spinner, an error message, an empty-state message) that every future screen will reuse instead of each screen inventing its own.
- An automatic check on GitHub that runs the linter, builds the app and runs its tests on every change.

## New concepts, in plain words
- **Vite / TypeScript:** the toolchain that turns the React source code into the files a browser can run. TypeScript is JavaScript with type-checking bolted on, which catches a class of bugs before the code even runs.
- **Tailwind CSS / shadcn/ui:** instead of writing custom CSS by hand for every element, Tailwind gives small reusable style classes (`text-center`, `bg-primary`), and shadcn/ui provides ready-made, accessible pieces (buttons, etc.) built with those classes, copied into the project so there's no hidden library magic.
- **oxlint:** a fast linter — it reads the code without running it and flags mistakes and style problems. Kvit is set to fail the build on any warning, same as the backend.
- **i18n (internationalization):** the system that shows English or Macedonian text from the same code, based on a `key` (e.g. `welcome.tagline`) that maps to the right sentence in each language.
- **The Cloudflare "Pages Function" proxy:** a tiny piece of code that runs on Cloudflare (where the website lives) and silently forwards any `/api/...` request to the real backend server (which lives elsewhere, on Render). This makes the browser think it's talking to one website, which is needed because Safari blocks login cookies between two different sites.
- **Vitest:** the test runner for the frontend, equivalent to what xUnit is for the backend.

## What was verified, and how
| Check | Result | How verified |
|---|---|---|
| Lint (oxlint, 0 warnings allowed) | PASS | Automated: `npm run lint`, exit 0 |
| Build (TypeScript + Vite) | PASS, 0 type errors | Automated: `npm run build` |
| Tests (Vitest) | PASS, 52/52 | Automated: `npm test` |
| Backend still green | PASS, 0 warnings, 26/26 | Automated: `dotnet build`/`dotnet test` |
| `package-lock.json` has Linux entries for native packages | PASS | Automated: counted entries for rolldown, lightningcss, `@tailwindcss/oxide`, TS7's native binary |
| Health check through the local dev proxy | PASS, 200 | Live run: Playwright loaded the app, watched the network log |
| Health check through the real Cloudflare proxy | PASS, 200 (missing config → clean 500) | Live run: `wrangler pages dev` against the local backend |
| Language switch changes the page and is remembered | PASS | Live run: Playwright switched language, reloaded the page, confirmed it stuck |
| Welcome screen at phone width (360px), light/dark, EN/MK | Looks correct | Live run: Playwright screenshots (not committed to the repo) |
| Button colour is readable enough (contrast) | PASS: 5.32:1 light, 8.91:1 dark (needs ≥ 4.5:1) | Calculated from the actual CSS colour values |
| All the special Macedonian letters render correctly everywhere | NOT VERIFIED | The app's own real text renders fine, but a dedicated check of the full letter set was inconclusive on this PC; the real test is on an actual phone, planned for Phase 5 |
| CI goes green on GitHub | NOT VERIFIED yet | Only checkable after the branch is pushed |

## How this phase actually happened (worth knowing)
A previous session started building this with an AI "coder" working in an isolated copy of the project (a git "worktree"), so the main conversation could keep working while it built. That session ran out of its usage allowance before the coder finished and before anyone reviewed its work. In this session, the coder's already-finished code was found sitting untouched in that isolated copy, reviewed file by file against the original plan, verified with the checks above, and then merged in — nothing was rebuilt from scratch, and nothing was taken on trust without running it first.

## Left for you
- Push the branch and open the pull request (description already drafted in chat).
- Install the "TypeScript (Native Preview)" VS Code extension (see `docs/guides/install-tools.md`) so your editor understands TypeScript 7 correctly.
- Once merged to `main`, next up is Phase 3 (the money-splitting logic).
