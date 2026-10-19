# Phase 12b: join a group without an account

Branch `feat/12b-guest-join`. Release 1 was accepted on 2026-10-16; Filip put this BACKLOG "must do" before Phase 13. The decision log is in `docs/DECISIONS.md` (section "Phase 12b"). The real-browser pass is in `2026-10-19-phase-12b-browser-pass.md`.

## What now works
- Open an invite link signed out: **"Who are you? Tap your name:"**, a list of the free names (1 tap joins as a guest), an "I'm new" row (1 field + 2 taps), and "Have an account? Log in" (a sheet with Google and Log in with email that returns to the join screen).
- A guest is a normal user with no email and no password (`users.is_guest`), signed in with the same 90-day cookie. Every group, expense, payment and closing rule works unchanged.
- Guests: a "Guest" badge; no New group, no One bill, no Add a name; cannot become owner; edit and delete only their own expenses; Log out shows a warning first.
- Lost phone: the owner taps **Let in again** on the guest's row (2 taps); the name becomes free and keeps its expenses, payments and balance.
- A guest can **make an account** (Settings, Google or email + password) and keeps the same user, groups and money.
- Hardening: two guests tapping the same name at the same instant (the second gets a clean "not found"); a signed-in person calling join-as-guest is refused (409); a double tap on Save sends one request.

## Migrations (CI applies them on Neon)
`GuestAccounts`, `GuestLetInAgain`, `GuestUpgradedUsageEvent`.

## Checks (run by me after the last change)
| Check | Result | Label |
|---|---|---|
| `dotnet build Kvit.slnx` | 0 warnings, 0 errors | VERIFIED by live run |
| `dotnet test Kvit.slnx` | 3710 of 3710 (Phase 12: 3485) | VERIFIED by automated test |
| `npm run lint` | no warnings | VERIFIED by live run |
| `npm run build` | no type errors | VERIFIED by live run |
| `npm test` | 7870 of 7870 (Phase 12: 7508) | VERIFIED by automated test |
| Join by tap, "I'm new", no-free-names, guest limits, Let in again and rejoin, email upgrade, double click on Save, Log in sheet, 360/768/1280 px in EN and MK, light and dark, reduced motion | passed, 3 small bugs found and fixed | VERIFIED by live run (headless Chromium, local stack; details in the browser-pass report) |
| The three fixes (one "joined" line, no extra name line, long names on the invite card) | tests and a headless check of the join page at 320 and 360 px | VERIFIED by automated test and by live run (the invite answer was faked by the script) |
| The Activity tab and Members screen after a real guest tap, with the fixes, in the real app | not re-run after the fixes | NOT VERIFIED |
| Google upgrade and Google log in from the Log in sheet | the button renders; never used | NOT VERIFIED |
| A real phone, Safari, Firefox, a screen reader | not tried | NOT VERIFIED |
| Closing and Finished groups with guests in a browser | backend tests only | NOT VERIFIED |
| Two tabs upgrading the same guest at once | not tried | NOT VERIFIED |
| The Macedonian texts | drafts, not read by Filip | NOT VERIFIED |
| Neon and the deployed site | not run | NOT VERIFIED |

## Decisions Filip made
Tap a free name = in; type a new name = in; the owner's Let in again for a lost phone; guests cannot add names or create groups (the buttons are hidden); Guest badge; warning before a guest logs out; the upgrade is built now; join screen option B; a second "Marko" after Let in again is left alone (A). Full reasoning and rejected alternatives: `docs/DECISIONS.md`.

## Left over
`docs/BACKLOG.md`, the item "Phase 12b leftovers (2026-10-19)". The lighter peach for the light theme is still an open decision for Filip (top of BACKLOG).
