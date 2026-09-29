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

## Decisions and rejected alternatives (moved word for word from DECISIONS.md on 2026-09-29)

### 2026-09-25: Phase 2 plan (frontend skeleton), Filip's answers and technical decisions
Planned in a fresh plan-mode session (Filip's rule: Phase 2 is planned in plan mode, Opus, then built by the coder). Full plan kept at the time in `C:\Users\Davchev\.claude\plans\read-status-and-roadmap-reactive-fox.md`; the coder run itself hadn't finished as of this entry, so results and any deviations are logged separately once it reports back.

**Filip's answers:**
- **Language:** Kvit follows the phone (a Macedonian phone → MK, anything else → EN). A small **EN · МК** switch sits at the top of the Welcome screen, remembered on that phone.
- **Amounts:**
  | | Denars | Euros |
  |---|---|---|
  | English | `1,200 MKD` | `€15.50` · `€1,234.50` |
  | Macedonian | `1.200 ден.` | `€15,50` · `€1.234,50` |

  The € always goes in front. Macedonian uses the dot for thousands and the comma before cents (matching the denar style already used); English uses the comma for thousands and the dot before cents. `Intl.NumberFormat`'s own currency formatting doesn't produce these exact shapes (Macedonian default puts € after the number; English default writes `MKD 1,200` not `1,200 MKD`), so `formatMoney` uses `Intl.NumberFormat` for the number part only and places the currency mark/code by hand.
- **Main colour: sunny orange.** The "you owe" red (from Phase 9) must stay clearly different from it.
- **Tagline in English:** "Квит сме." with "We're even." in smaller text under it. Macedonian keeps only "Квит сме.".

**Technical decisions (Claude's, per "ask product questions only"):**
- TypeScript 7 is tried last, after everything is green on the template's TS 6.0 — kept only if lint/build/tests all pass on the real code, otherwise reverted with the reason logged.
- The `@/` import alias uses `paths` without `baseUrl` (TS 7 removed `baseUrl`; shadcn's Vite guide still shows it, but it's skipped).
- Dark mode is CSS-only (`prefers-color-scheme`), no manual toggle and no `.dark` class, since Kvit always follows the phone.
- Filled buttons need ≥4.5:1 text contrast in both light and dark orange, calculated not eyeballed.
- Font is the phone's own `system-ui` stack (no web font download; Macedonian letters covered on Android/iPhone/Windows).
- Toasts: Sonner directly with `theme="system"`, wrapped in `KvitToaster` — dropping shadcn's default `next-themes` dependency, which Kvit doesn't need.
- Language detection is a small hand-written function (saved choice, else `mk*` → mk, else en), not `i18next-browser-languagedetector`.
- Money formatting works only in integer minor units; a non-whole-denar MKD value or a non-safe-integer amount throws, naming the value, since it can only be a bug.
- Cloudflare proxy (`functions/api/[[path]].ts`): its own `functions/tsconfig.json` with `@cloudflare/workers-types` (kept out of the app's own tsconfig, which needs DOM types instead); `public/_routes.json` restricts functions to `/api/*` so static pages don't spend the free function-call quota; a missing/invalid `API_ORIGIN` answers 500 with a clear message; upstream redirects are passed back to the browser, not followed.
- Node pinned to 24 via `src/web/.nvmrc` and `package.json` `engines` (React Router 8.4 needs Node ≥ 22.22; Cloudflare's build image defaults to 22.16, so the hosting guide's Cloudflare env vars need `NODE_VERSION=24` added once Phase 5 sets up hosting).
- The Welcome screen's Google / email / "I already have an account" buttons show a "Coming soon" toast until Phases 4 and 6 wire them up.
- `RequireAuth` is a placeholder that always redirects to `/welcome` until Phase 4 adds the real check.
- The not-found screen (SCREENS.md screen 20) is built now, ahead of its listed phase, because the route table needs a catch-all route regardless.
- oxlint is configured to fail on warnings, matching the backend's `TreatWarningsAsErrors`. Vite's dev server uses `strictPort: true` on 5173, since Phase 6's Google sign-in only allow-lists that exact port.
- Tests sit next to the code they test (e.g. `formatMoney.test.ts`); the Cloudflare proxy's tests go in `src/web/test/functions/`.

### 2026-09-26: Phase 2 built and verified
The coder's session ended before reporting back (Filip's usage limit), so its work was picked up cold from its git worktree in a fresh session, reviewed file-by-file against the plan, then verified rather than trusted.
- **TypeScript 7 (7.0.2) kept**, confirmed by running `npm run lint`/`build`/`test` on the actual code: 0 lint warnings, 0 type errors, 52/52 Vitest tests passing.
- **VS Code needs an extra extension for TS 7 to work correctly:** "TypeScript (Native Preview)" (`TypeScriptTeam.native-preview`, Microsoft). Installing it isn't enough — it must be turned on via Command Palette → "TypeScript Native Preview: Enable (Experimental)", otherwise VS Code silently keeps using its own older bundled TypeScript. Added to `install-tools.md`. Not confirmed: whether it can be pinned to the project's exact `node_modules/typescript` 7.0.2 rather than whatever version ships with the extension (NOT VERIFIED, the extension/project only just merged into the main TypeScript repo).
- **Cloudflare proxy verified live** with `wrangler pages dev`: `/api/health` → 200 with `API_ORIGIN` set; a missing/invalid `API_ORIGIN` answers a clean 500 rather than guessing or crashing.
- **Button contrast calculated from the actual CSS tokens:** 5.32:1 light mode, 8.91:1 dark mode — both clear the 4.5:1 WCAG AA bar the plan required.
- **Macedonian font check is inconclusive on this PC.** The app's real UI text (Welcome screen, not-found screen) renders correctly. A raw on-page check of the full special-letter set (Ѓ Ќ Ѕ Ј Љ Њ Џ and lowercase) showed a few glyphs that looked like possible Latin-lookalike substitutions — not confirmed as a real font gap versus normal Cyrillic/Latin glyph-sharing. As the plan already noted, this check is PC-fonts-only regardless; the real confirmation is on an actual phone in Phase 5.
- No other deviations from the plan found during review.

