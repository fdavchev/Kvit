# Report: the build plan (2026-09-25)

## What was asked
Filip asked for the project to be read, a thorough plan for starting to code, and clear steps for what he has to do himself (he creates the repository; hosting setup happens tonight). Mid-session he asked whether TypeScript 7 can be used.

## What was done
| File | What it is |
|---|---|
| `docs/SCREENS.md` | New. Every Release 1 screen (20), what's on it and how many taps the main action takes; Release 2 and 3 in summary; "who can do what" |
| `docs/DATA-MODEL.md` | New. All Release 1 tables with every column, Release 2 additions, the money rules (splits, rounding, balances, "who pays whom"), the group-closing states and 6 open questions |
| `docs/ROADMAP.md` | Rewritten. Phase 0 ticked; 11 small phases to a deployed Release 1, each with a branch name, a checklist and a "done when"; Releases 2 and 3 outlined |
| `CLAUDE.md` | New. Stack, hard rules, layout and the planned test commands |
| `docs/guides/start-the-repository.md` | New. Filip's steps: git init, first commit, first branch (now); hosting accounts and GitHub (tonight) |
| `docs/DECISIONS.md` | New top entry: facts checked, technical decisions, proposals waiting for OK, clashes and how they were resolved |
| `docs/ARCHITECTURE.md` | Trap note on TypeScript updated; lint row added (oxlint) |
| `docs/guides/install-tools.md` | ESLint extension replaced by Oxc, because the template uses oxlint |
| `docs/STATUS.md` | Updated |

## Answer to "can we use TypeScript 7?"
- **Probably yes.** The main blocker was ESLint's TypeScript plugin, which supports TS < 6.1 only. The current Vite template uses **oxlint** instead, and oxlint doesn't depend on TypeScript. (VERIFIED from the npm registry and the template's `package.json`.)
- **Known snag either way:** `openapi-typescript` asks for TypeScript 5, which clashes with both 6 and 7. (VERIFIED from its `peerDependencies`.)
- **Not checked yet:** VS Code editor support for TS 7 and the template's `tsc -b` build on 7.0. (NOT VERIFIED.)
- **Plan:** try TS 7 in Phase 2; keep it if lint, build and tests pass.

## Verification
- **VERIFIED by live run (local commands, 2026-09-25):** installed tools: .NET SDK 10.0.400, Node 24.19.0, npm 11.17.0, Docker 29.7.2 (engine was not running at the time), git 2.47, gh 2.98. `dotnet new sln` creates `.slnx`. Git's default branch isn't set, so the guide uses `git init -b main`.
- **VERIFIED by registry lookup (2026-09-25):** the NuGet and npm versions listed in `DECISIONS.md`; the Vite template's TypeScript and lint setup; the Oxc VS Code extension exists (`oxc.oxc-vscode`).
- **NOT VERIFIED:** everything in the plan itself. No code was written and nothing was built or tested.

## What's next
Filip answers the open questions, runs Part 1 of `guides/start-the-repository.md`, and says go; then Phase 1 (backend skeleton) starts.
