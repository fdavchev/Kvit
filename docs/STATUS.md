# Status: Kvit

_Last updated: 2026-09-26 (Phase 2 merged to `main`)_

## Where we stopped
- Phase 2 (frontend skeleton) is merged to `main` via PR #2. `feat/02-frontend-skeleton` is done.
- A GitHub ruleset now protects `main`: PR required before merging, `backend`/`frontend` CI checks required, force-push and deletion blocked (0 required approvals, since this is solo — see project memory for what to revisit if a collaborator ever joins).

## Next step
Start Phase 3 (Money core): branch `feat/03-money-core`, pure C# in `Kvit.Domain/Money/`, no database, per `docs/ROADMAP.md`.

## Then
- Follow Phase 3's checklist in `docs/ROADMAP.md`.

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
