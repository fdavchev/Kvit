# Status: Kvit

_Last updated: 2026-09-26 (Phase 2 built, verified and being committed)_

## Where we stopped
- Branch: `feat/02-frontend-skeleton`. The coder's Phase 2 work (built in a worktree while a prior session was out of usage) was reviewed, verified, and merged into the main working copy.
- Filip is committing in stages himself. So far landed: "Add the frontend skeleton: routing, i18n, money formatting and the Welcome screen", "Add the Cloudflare Pages Function proxy", "Add the frontend CI job".
- Still uncommitted: `docs/DECISIONS.md`, `docs/ROADMAP.md`, `docs/STATUS.md` (this doc round) — ready to be the next ("Update docs for Phase 2") commit.
- The worktree at `.claude/worktrees/agent-a9e7589f42efc6be1` is no longer needed; its content is now merged into the main working copy.
- Branch not yet pushed to GitHub (or was pushed once at an earlier point mid-session — check `git status` / `git log --oneline` against `origin/feat/02-frontend-skeleton` before pushing again).

## Next step
Filip commits this docs update, pushes, and opens the Phase 2 PR (description drafted in this session's chat, following PR #1's format). Then watch CI go green on GitHub.

## Then
- Once Phase 2 is merged to `main`: start Phase 3 (Money core) — pure C# in `Kvit.Domain/Money/`, no database, per `docs/ROADMAP.md`.

## Blockers and open questions
- None outstanding for Filip.

## Verification state
- Backend (Phase 1): still 0 warnings, 26/26 tests — re-confirmed 2026-09-26 against the merged worktree.
- Frontend (Phase 2): VERIFIED 2026-09-26.
  - Automated: `npm run lint` 0 warnings, `npm run build` 0 type errors, `npm test` 52/52 passing, `package-lock.json` has the Linux native-package entries CI needs.
  - Live: `/api/health` → 200 through both the Vite dev proxy and `wrangler pages dev`; a missing `API_ORIGIN` fails closed with a clear 500; language switch changes `<html lang>` and persists across reload; `/` → `/welcome`, `/nope` → not-found screen; a "Coming soon" toast shows on the placeholder buttons.
  - Calculated: button contrast 5.32:1 (light) / 8.91:1 (dark) — both clear WCAG AA (4.5:1).
  - NOT fully verified: the full Macedonian special-letter set (Ѓ Ќ Ѕ Ј Љ Њ Џ and lowercase) rendered ambiguously in a raw on-PC check — the app's own actual UI text renders correctly, but this is PC-fonts-only regardless; the real check is on a phone in Phase 5.
  - NOT VERIFIED yet: CI green on GitHub (branch not pushed as of this doc update).
