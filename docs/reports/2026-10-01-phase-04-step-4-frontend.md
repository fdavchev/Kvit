# Phase 4, Step 4: the frontend (sign up, log in, session guard, settings, forced password change)

Date: 2026-10-01. Branch `feat/04-accounts` (PR #6, open). Two runs, each tester (failing tests first), check by Claude, Filip's go, then coder. Decisions: `reports/2026-10-01-phase-04-accounts.md` (Decisions section), "Step 4" blocks and "Look of the first screens". Look: `docs/design/2026-10-01-round-3/` (images and the sources that made them).

## Labels
VERIFIED by automated test / VERIFIED by live run: run by me, output read. REPORTED by the tester: run by the browser-check agent, I looked at its screenshots (contact sheets) but did not repeat the measurements. REPORTED by the coder: the coder ran it. NOT VERIFIED: not run.

## What was built
- **Run A (plumbing):** `me` and auth services, `createQueryClient` with the 401 and 403 handlers, the `me` query, the real `RequireAuth` (spinner, an error with Try again that never counts as signed out, redirect to Welcome, redirect to `/change-password`), five hooks, all 13 error codes with English and approved Macedonian text.
- **Run B (screens):** Welcome wired to Sign up and Log in, Sign up, Log in (with "Forgot your password? Ask Filip to reset it."), Home (temporary placeholder), Settings (language, change password, log out), Change password (forced after a reset, voluntary from Settings), a back arrow on the form screens, shared `KvitLanguageSwitch`, `KvitTextField`, `KvitInlineError`, `KvitBackButton` and friends, the router with public and protected routes, 19 new texts in both languages, the approved colours as tokens, and the mobile-native baseline (viewport-fit=cover, theme-color per scheme, no tap flash, 16 px inputs, 100dvh with safe-area padding, hover only on devices with a mouse, press state with reduced-motion support).

## Results
| Check | Result | Label |
|---|---|---|
| Tests first, run A | 111 of 220 red for the right reasons, 109 green (97 old + 12 guards) | VERIFIED by automated test |
| Tests first, run B | 130 of 364 red (missing translation key, shells, missing navigation, index.html); 234 green | VERIFIED by automated test |
| `npm test` after both runs | **364 of 364 pass** (was 97 at the start of Step 4) | VERIFIED by automated test |
| `npm run lint` | 0 warnings | VERIFIED by automated test |
| `npm run build` | 0 type errors | VERIFIED by automated test |
| The 13 Macedonian error texts in `mk.json` equal the approved tables exactly | 0 differences | VERIFIED by automated test |
| Real browser (Chromium, 360x780, 2x, touch) against the real API on a throwaway database and a throwaway certificate, in en/mk x light/dark: Welcome, sign up (cookie `kvit_auth` HttpOnly, Secure, SameSite=Lax), taken email, home, settings (language PUT 204, log out, browser Back stays on Welcome), log in (wrong password, account language wins), forced change after `scripts/ResetPassword.cs` (redirect, no back link, cannot leave, three errors, success, old temporary password refused) | 112 recorded checks pass | REPORTED by the tester |
| Layout at 360 px: no horizontal scroll, nothing clipped (Macedonian included), inputs 17 px, `100dvh` with no gap, theme-color and viewport metas | pass | REPORTED by the tester |
| Language buttons EN / МК are 38 px tall (below 44 px) | fail, small | REPORTED by the tester |
| Colours equal the approved mockups (pixel samples), positions within a few px, three layout differences (see DECISIONS) | as listed | REPORTED by the tester; contact sheets viewed by me (light en, dark mk) |
| Console: no app errors; browser notes for the expected 401 on `/api/me` when signed out and 4xx on rejected forms; `ERR_ABORTED` on three 204 answers through the Vite proxy | not investigated further | REPORTED by the tester |
| Cleanup of the browser check: API stopped, throwaway database dropped, only `kvit` remains, repo unchanged | done | REPORTED by the tester |
| A real phone (touch feel, keyboard, notch, status bar), the real Cloudflare and Render | not run | NOT VERIFIED until Phase 5 |
| Filip's own click-through on his PC at phone width | not run | NOT VERIFIED |
| CI on the push that contains Step 4 | not run yet | NOT VERIFIED |

## Open findings (Filip decides what goes to the coder)
1. EN / МК buttons are 38 px tall: give them an invisible 44 px tap area (no visual change). Recommended.
2. The forced Change-password screen has no back link, so its title sits about 45 px higher than the other screens: keep the same space. Recommended.
3. Layout differences from the mockups caused by the back link (title gap about 23 px smaller on Sign up and Log in): Filip's call whether the back link stays; if it stays, accept.
4. Big titles wrap to two lines in places (English "Change password", Macedonian "Направи профил"): accept or shrink the title size a little.
5. Settings: both buttons look the same: accept or make Log out different.
6. Apostrophe in "I don't have an account": straight in the app, curly in the mockup: cosmetic.

## Cost (subagent tokens as reported by the harness)
| Agent | Tokens | Tool calls |
|---|---|---|
| tester, run A (failing tests) | 134,287 | 48 |
| coder, run A | 129,900 | 47 |
| tester, run B (failing tests, with the follow-up) | 158,884 | 111 |
| coder, run B | 194,733 | 76 |
| tester, browser check | 123,567 | 38 |
| **Step 4 total** | **741,371** | 320 |
| Mockup rounds (3 rounds and the two small changes, resumed coder) | 665,145 | |

(Earlier steps: 1 about 369k, 2a about 390k, 2b about 220k to 315k, 3 about 237k, 3b about 365k. Resumed agents carry their history, so their numbers include re-read context.)

## Step 4c (2026-10-01, after Filip's first click-through): theme choice, sign-up limit, tap areas, wide screens
Asked by Filip: a light/dark choice in Settings (default follows the device), typos on Create account must not lock him out for minutes, the app must also look right on the web. Two testers (backend and frontend, tests first), two coders in parallel, a second browser check.

| Check | Result | Label |
|---|---|---|
| Tests first: backend 6 of 502 red (30 bad inputs got 429; a mixed case), frontend 65 of 437 red (theme module shells, missing translation keys, no inline script) | right reasons | VERIFIED by automated test |
| `dotnet build Kvit.slnx` (Debug; Release reported by the coder) | 0 warnings, 0 errors | VERIFIED by automated test |
| `dotnet test Kvit.slnx` | **502 of 502 pass** (was 493) | VERIFIED by automated test |
| `npm test` / `npm run lint` / `npm run build` | **437 of 437 pass** (was 364), 0 warnings, 0 type errors | VERIFIED by automated test |
| Theme in a real browser: Same as device follows the browser colour scheme (peach `rgb(255,217,168)` / `rgb(53,31,27)`); Light and Dark override it; `data-theme`, both `theme-color` metas and the Sonner toast follow; survives reload; no request sent; Macedonian labels Позадина / Како на уредот / Светла / Темна each on one line at 360 px | pass | REPORTED by the tester |
| No flash of the wrong theme: production build (CDP frames after reload and fresh navigation all dark) | pass | REPORTED by the tester |
| No flash in the Vite DEV server | first frame white, then dark: Vite adds the CSS by JavaScript after the first paint; the inline script already sets `data-theme`. Dev-only, not seen in the production build; NOT VERIFIED on Cloudflare itself | REPORTED by the tester |
| Tap areas: every EN / МК and theme button is 38 px with a 44 px hit area (`::after`); touches 2.5 px outside the visible button still hit it; track still 44 px | pass | REPORTED by the tester |
| Forced Change-password title at the same y as the voluntary screen (60 px phone, 112 px desktop, difference 0) | pass | REPORTED by the tester |
| Phone layout unchanged (Welcome, Sign up, Log in, Home pixel-identical in en-light and mk-dark; Settings only gained the theme switch; forced Change-password moved down 48 px on purpose) | pass | REPORTED by the tester |
| Sign-up limit through the real UI (counted address `::1`, the Vite proxy uses IPv6): 30 weak passwords in a row, none refused; then 5 counted sign-ups succeeded and the 6th got 429 `RATE_LIMITED`, `Retry-After: 600`, with the translated English and Macedonian message; a weak password while limited still gets its own 400 | pass | REPORTED by the tester |
| Log-in lock: 401 four times, the fifth wrong password 403 with the locked message, the right password also refused while locked | pass | REPORTED by the tester |
| Smoke of sign up, log out, log in, wrong password, taken email, language PUT 204, forced change after reset | pass | REPORTED by the tester |
| Console: only expected 4xx notes on `/api/*`; `net::ERR_ABORTED` on three 204 answers through the Vite proxy (same as the first check, not investigated) | no app errors | REPORTED by the tester |
| Desktop 1280x800, en/mk, light/dark: column centred (x 440 to 840), about 64 px top space (112 px to the title on form screens), Settings 'Log out' 12 px under 'Change password', nothing broken. Honest judgment: it is the phone column (400 px wide, 17 px text) on a mostly empty field; clean but small | works, not designed for the web | REPORTED by the tester; contact sheets viewed by me |
| Welcome toast covers the 'I already have an account' link while it shows | noted, ordinary toast behaviour | REPORTED by the tester |
| A real phone, Cloudflare, Render | not run | NOT VERIFIED until Phase 5 |

Cost (subagent tokens): backend tester 60,993; frontend tester 78,494; backend coder 107,297; frontend coder 171,810; browser check 2: 147,795. Step 4c total 566,389.
