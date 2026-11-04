# Phase 18 Step 4: real-browser pass of the Statistics page (2026-11-04)

Branch `feat/18-admin-stats` at HEAD `df42b67`. Run by the tester. No production code, no tests and no docs other than this report were changed. Nothing was committed or staged (`git status --short` was empty at the end). Screenshots and scripts are in the scratchpad, not in the repository (see "Where the files are").

Labels: **VERIFIED by live run** (done in a real headless browser against the local app, result read or screenshot looked at), **NOT VERIFIED**. The automated suites (`dotnet build`, `dotnet test`, `npm test`, `npm run lint`, `npm run build`) were **not run**, so nothing here is VERIFIED by automated test.

## What was run and how
- Docker Desktop open. `.\scripts\start-local.ps1 -NoBrowser` started the database, API (`http://localhost:5018`) and website (`http://localhost:5173`) with no migration warning. VERIFIED by live run.
- At the end I stopped the API process I had restarted myself (for the "API stopped" test), then `.\scripts\start-local.ps1 -Stop` printed "Closed the 'Kvit API' window", "Closed the 'Kvit website' window", "Stopped the database container kvit-postgres (its data is kept)" and "Ports 5018 and 5173 are free". `netstat` showed no listener on 5018 or 5173, `docker ps -a` showed `kvit-postgres Exited (0)`. VERIFIED by live run.
- Playwright for Python, headless Chromium only, time zone `Europe/Skopje`, machine date 2026-11-04 (about 10:00 UTC, so Skopje is UTC+1). Language through the browser locale (`en-US` / `mk-MK`); one run used the Settings language buttons. Theme through the browser colour scheme; reduced motion through the browser emulation.
- **Accounts** (password `Passw0rdA`, all made through the real sign-up page): admin `p18admin1793785415@example.com` ("Admin Adminov", role granted with `dotnet run scripts/GrantAdmin.cs -- <email>`; it ran without stopping the API and printed "is now an admin"), normal users Ana Alfa and Bojan Beta, guest "Gosho Guest" (joined by invite link as a guest). Ana made the group "P18 Trip", Bojan joined by invite, the guest joined by invite, two expenses, one payment recorded by Bojan and confirmed by Ana. The admin made groups and (later) three expenses of their own.
- **Extra usage events** (local database only, through `docker exec` + psql, file `extra.sql`): 3,286 rows with ids 1370 to 4655: sign-ups every 4 days from 2025-09-03 (Email, Google, Guest), GuestUpgraded every 23 days, mixed JoinedViaInvite / GroupCreated / ExpenseAdded / SettlementConfirmed from 2025-09-05 to 2026-10-24, 2,784 Active rows for 30 existing throwaway people (22 normal, 8 guests; one per person per UTC day), one very busy day (2026-11-02: 90 expenses, 12 groups), a second busy day (2026-10-12: 35 expenses, 7 payments), and the day-shift rows: 4 expenses at 22:30 UTC and 5 at 23:30 UTC on 2026-10-31 (winter, UTC+1), 6 at 22:30 and 3 at 23:30 UTC on 2026-08-14 (summer, UTC+2), one group at 23:30 UTC on 2026-11-03 (already 4 Nov in Skopje). `occurred_on` equals the UTC day of `occurred_at`. The database already held about 1,280 events from earlier phases, so the "since the start" numbers are not small.
- **Independent check of the numbers:** `expect.py` computes every count straight from `usage_events` with SQL (Skopje day, admin accounts left out, weeks starting Monday) and compares it with the API answer, and `verify.py` computes every text the page must show (numbers, plural forms, compare line, bar heights, axis labels, in EN and MK) from the API answer and compares it with what the browser shows.
- "Mocked response" below means Playwright `route.fulfill` gave the page a changed copy of a real API answer (needed because the local data can never show "1 account" or a number of 8 digits). Those lines are labelled "mocked".

## BUGS
I did not fix anything. Numbered by how much they hurt.

**Bug 1 (medium): with many bars the bar chart runs out of its card, and at 320 and 360 px the whole page can scroll sideways.**
- Where: `src/web/src/features/admin/stats/components/AdminStatsCards.tsx` line 122 (`flex h-14 items-end gap-0.5`) and line 126 (`block min-w-0.5 flex-1`): every bar is at least 2 px wide with a 2 px gap, so n bars need 4n - 2 px, and nothing caps or wraps them.
- Steps: sign in as admin, tap Custom, set From 2026-08-04 and To 2026-11-03 (92 days, Day grouping is still allowed).
- What I saw: the bars of every card continue past the right edge of the card (screenshot `shots\r14-360-custom92-bars.png`: the tall right-hand bars sit outside the rounded card at x 330 to 360 and further), the last bar is far to the right of the last axis label "3 Nov", and the page gets a sideways scroll bar (scroll width 406 px on a 360 px window, 406 on a 320 px window; `scrollX` could be moved by 46 px).
- Measured where it starts (Custom, To = 2026-11-03, bars box scroll width against its client width):

  | Window | Bars box | Bars no longer fit from | Page scrolls sideways from |
  |---|---|---|---|
  | 320 px | 240 px | 61 days (242 > 240) | 71 days (322 > 320) |
  | 360 px | 280 px | 71 days (282 > 280) | 92 days (406 > 360); 80 days still 360 |
  | 768 px and up | 368 px or more | never (92 bars need 366) | never |

- Also real today without Custom: **All + Week** gives 62 bars (since 2025-09-01), 246 px in a 240 px box at 320 px, so the bars poke 6 px into the card padding. It gets worse every week (all-time by week will reach 71 at 360 px after about 9 more weeks of data).
- Expected: bars stay inside the card and the page never scrolls sideways (the task asks for `scrollWidth <= innerWidth`).
- Severity: medium (a plausible "last 3 months by day" on a phone shows a broken chart and a sideways-scrolling page). Sizes 320 and 360; EN and MK; light and dark (all 8 views of the matrix failed the same way). The matrix has 160 views; 12 of them had this problem (8 x "Custom 92 days", 4 at 320 and 4 at 360, and 4 x "All + Week" at 320).

**Bug 2 (low, keyboard): at 360 x 740 the Statistics row in Settings is hidden behind the bottom bar when it gets keyboard focus.**
- Steps: Settings, press Tab until "Statistics" is focused (9th Tab).
- What I saw: the browser scrolled only as far as the window edge, so the row sits at y 688 to 740 while the bottom bar covers y 666 to 726; only about 14 px of the row and the bottom of its focus ring peek out (screenshot `shots\r10-en-light-settings-focus.png`). The row above ("Privacy", y 631 to 683) is covered by 17 px in the same way; "Log out" is fine because the page is scrolled to the end by then.
- Expected: a focused row is visible. This is the page-wide pattern (no scroll padding for the floating bar), not special to the new row, but the new row is the first one to be fully hidden.
- Size 360 x 740 (same layout at any phone height where the page can scroll); the row top was 688 in the EN light, EN dark and MK light runs.

**Bug 3 (low, accessibility): in light mode the small text of a card is 3.21:1 while the cards are dimmed.**
- Steps: with the stats loading (range change), the grid has opacity 0.6 and `aria-busy="true"`.
- Measured: light mode title 3.98:1, sub line and axis label 3.21:1; dark mode 5.92:1 and 5.10:1 (computed from the real colours blended at 0.6 over the page; undimmed they are 12.72:1, 8.20:1 and 8.20:1).
- The dimming lasts the length of the request (a few hundred ms normally) but about 7 s when the server is failing (see Observation 2), because the old numbers stay on screen dimmed during the three automatic retries.
- Expected: at least 4.5:1 for text, or dim less. Severity low (it is a loading state). Size 360, light, EN.

**Bug 4 (low): a non-admin who opens `/admin` gets "Page not found" with the bottom bar, but any other unknown address gets it without the bar.**
- Steps: sign in as Ana (or the guest), open `/admin`, then open `/zzz-nothing`.
- What I saw: `/admin` shows the not-found text plus the Home / Groups / Budget / Settings bar (guest: Home / Groups / Settings) and the text sits about 40 px higher; `/zzz-nothing` shows only the not-found text (`shots\r7b-admin.png`, `shots\r7b-unknown.png`). The page text differs by the four bar labels.
- Expected (DECISIONS 2026-11-04, Step 3): "the normal not-found page". The difference tells a curious user that `/admin` is a real address. Severity low. Sizes 360 EN (also checked in MK for the text; the guest too).

**Bug 5 (low, cosmetic): percentages of 1000 and more have no thousands separator, the counts next to them do.**
- Seen with real data: "+1043% vs the period before" beside "160", "+9229%", "+1940%"; with big counts "1,000 payments" or "12,345,678" next to "+9999999800%" (mocked). Macedonian: «+1940% во однос на претходниот период» with «1.234.567» counts.
- Where: `AdminStatsCards.tsx` line 194 (`String(change.percent)`; the counts use `formatCount`).
- Expected: the same number format for both. Size 360; EN and MK.

## OBSERVATIONS (not bugs, for Filip to decide or know)
1. **"This month" is compared with an equally long window just before, not with last month.** On 4 Nov the request asks for 1 to 4 Nov against 28 to 31 Oct (`previousFrom`/`previousTo` in the answer), and the page says "+200% vs the period before". The mockup said "vs last month" for This month. The wording is true, but a reader may think it is last month. Same for 12 months (previous window 2024-12-27 to 2025-11-30, 340 days) and Custom.
2. **The error state takes 7.5 s to show** when the server is stopped or answers 500, 502 or 504 (4 tries at 0, 1, 3 and 7 s; the 403 answer shows at once). During those seconds the old numbers stay on screen dimmed (Bug 3) under the newly chosen chip, and after Retry the **old** range's numbers come back dimmed for another 7 s before the error returns (sampled every 0.2 s: cards at 0.0 s, alert at 7.1 to 7.2 s, in both the network-abort and the 500 case). This is the app-wide query retry setting (`maxQueryRetries = 3` in `core/api/queryClient.ts`), not special to this page.
3. **Week labels show the Monday before the range.** This month by Week labels the first bar "26 Oct" (range starts 1 Nov); by Month or Year a partial range has one bar with one label ("Nov 2026" or "2026"), as decided ("a one-bar range shows its label once").
4. **Day and week labels carry no year,** so Custom 1 Sep 2025 to 4 Nov 2026 by Week reads "1 Sep ... 2 Nov".
5. **Nothing on the page says the admin accounts are left out** (the mockup note "Your own account is left out" is not on the page). The decision is built correctly (see check 19).
6. **Invalid custom dates keep the old numbers on screen** while the fields show the new, invalid dates (as decided, with the red message between them).
7. **"1 person-days"** (known, in DECISIONS) was seen with a mocked response: EN "1 person-days", MK «1 човеко-дена».
8. **At 768 px the page is a single 400 px column** with wide empty sides; two columns start at 1024 px (as in the mockup, `64rem`). At 1280 and 1920 px the sticky side rail is on the left and two cards sit side by side.
9. **Macedonian nouns after numbers:** the page uses the "one" form for 1, 21, 31 and 101 and "other" for the rest («21 профил», «11 гости», «216 профили»). Whether «5 профили» is the right count form is for Filip to read (all Macedonian text is a draft).
10. **Each date change sends a request at once.** Setting a range by filling From first sends one request for the half-set range (for example 2024-01-01 to today) before the second field is set. A normal user sets two fields one after the other, so this is two requests per range, no debounce.
11. **Browser date fields** showed day/month/year order in this headless browser in both languages (the machine's own setting), so I could not judge the date field order a real phone would show.

## Results table
| # | Check | Result | Label |
|---|---|---|---|
| 1 | Settings shows the "Statistics" row for the admin (EN "Statistics", MK «Статистика»), 52 px high; tap opens `/admin` | PASS (EN and MK, light and dark; row text and link `/admin` read from the DOM) | VERIFIED by live run |
| 2 | Opens on This month + Day: `GET /api/admin/stats?from=2026-11-01&to=2026-11-04&by=day`, "This month" and "Day" pressed | PASS | VERIFIED by live run |
| 3 | Every range chip: 7 days (29 Oct to 4 Nov, Day), 30 days (6 Oct to 4 Nov, Day), This month, 12 months (1 Dec 2025 to 4 Nov, Month), All (no from/to, Month), Custom (fields filled with the period on screen: after All 2025-09-01 to 2026-11-04 by Week; after 30 days 2026-10-06 to 2026-11-04; after 12 months 2025-12-01 to 2026-11-04 by Week) | PASS | VERIFIED by live run |
| 4 | Every enabled grouping chip under every range, EN and MK, 360 px light: 36 combinations, UI against the API answer (title, total, compare line, sub lines, bar count and heights, first and last label, totals line, plural forms) | PASS 36 of 36 | VERIFIED by live run |
| 5 | The API answer against SQL computed independently from `usage_events` (admin accounts left out, Skopje days, Monday weeks): the 36 combinations, 11 more ranges (including the 1-day ranges 2026-10-31, 2026-08-15 and 2026-11-04, and 14 months by Month, Week and Year) and the Custom ranges of checks 6 and 8 | PASS, 0 differences | VERIFIED by live run |
| 6 | The Day chip is disabled over 92 days: disabled under 12 months, All, and Custom of 93 days; enabled at exactly 92 days (2026-08-04 to 2026-11-03) and under; the grouping moves to Week by itself at 93 days | PASS (EN and MK) | VERIFIED by live run |
| 7 | Custom: two date fields (48 px high, `max` = today), stacked at 320 px, side by side at 360 px and up; the title of the fields "From"/"To" and «Од»/«До» | PASS | VERIFIED by live run |
| 8 | Custom invalid pairs: From after To; To tomorrow; To far in the future (2030); an emptied field. Each shows the red message ("Pick a start on or before the end, and an end no later than today." / «Почетокот мора да е пред крајот или ист ден, а крајот најдоцна денес.»), sends no request for the invalid pair itself (while I filled the two fields one after the other, the half-set pair was sometimes valid and sent one request; far-future sent none), keeps the six cards with the last valid numbers; setting a valid pair again removes the message | PASS (EN and MK) | VERIFIED by live run |
| 9 | Totals line "Since the start: 216 accounts · 36 guests · 272 groups · 749 expenses · 173 payments" (MK «Од почетокот: ...»); equals SQL | PASS | VERIFIED by live run |
| 10 | Six cards with big number, sub lines ("Email 18 · Google 0 · Guest 3", "Guests who made an account: 1", "of them guests 2", "N person-days" only when not Day), bars, first and last label, one label for a one-bar range | PASS (checks 4 and 28) | VERIFIED by live run |
| 11 | Compare line: up (+200%, green), down (-24%, red), new ("new, none before"; seen with real data under Custom after All), same ("same as the period before"; also a change that rounds to 0%: 1000 against 1001; both mocked), hidden for All (real data) and hidden when both periods are 0 (mocked), -100% and 1 against 3 (-67%) correct | PASS | VERIFIED by live run (same, 0 of 0 and the 0% case mocked) |
| 12 | Plural forms and big numbers (mocked): "1 account · 1 guest · 1 group · 1 expense · 1 payment"; MK «1 профил · 1 гостин · 1 група · 1 трошок · 1 плаќање», 21 and 101 use the one form, 11 and 0 and 2 the other; counts of 12,345,678 and 98,765,432 (MK `12.345.678`); 12 mocked situations x EN and MK x 360 and 320 px = 48 views, 0 text differences, 0 clipped elements, 0 sideways scroll | PASS | VERIFIED by live run (mocked responses) |
| 13 | Skeleton on first load: title and all 10 chips stay on screen, 7 pulsing blocks (1 line + 6 cards), a `role=status` region with the hidden text "Loading…" / «Се вчитува…», no cards yet (`shots\r5-en-light-no-preference-skeleton.png` looked at: a pale slab for the totals line and big pale slabs for the cards) | PASS (EN and MK; the 8 matrix views per width too) | VERIFIED by live run |
| 14 | Dimmed cards while changing range: opacity 0.6, `aria-busy="true"`, transition 0.2 s on opacity; back to 1 after the answer; the chips stay usable (`shots\r5-en-light-no-preference-dimmed.png` looked at: old numbers faded under the newly pressed "7 days") | PASS (the contrast of the dimmed text is check 38 and Bug 3) | VERIFIED by live run |
| 15 | Error state with Retry: network abort, 500, 502 text, 403, 200 with a broken body (mocked), and the real API process stopped (browser got 502 from the Vite proxy); each shows the red text with a 52 px high "Try again" / «Обиди се повторно» button, title and chips stay, the totals line and cards are gone; Retry after the cause is gone shows the six cards. EN texts: "The server may be waking up. Check your connection and try again in a moment." (network, 502) and "Something went wrong. Please try again." (500, 403, broken body); MK «Серверот можеби се буди. ...» / «Нешто тргна наопаку. ...»; after restarting the real API (back in 2 s) Retry showed 30 days with 147 sign-ups | PASS (delay is Observation 2) | VERIFIED by live run |
| 16 | Normal user (Ana), EN and MK: no Statistics row in Settings (links: Change password, Privacy), `/admin` shows "Page not found" / «Страницата не е пронајдена», the page made no call to `/api/admin/*`, the address stays `/admin`; a direct `GET /api/admin/stats` answers 403 (also with bad parameters, as decided) | PASS except Bug 4 | VERIFIED by live run |
| 17 | Guest (Gosho), EN and MK: the same (links: Make an account, Privacy; bar Home / Groups / Settings), no call, 403 | PASS except Bug 4 | VERIFIED by live run |
| 18 | Not signed in: `/admin` goes to `/welcome`, no stats call, `GET /api/admin/stats` answers 401 | PASS | VERIFIED by live run |
| 19 | The admin's own events are not counted: the admin made 3 expenses, 1 more group, opened the app and reloaded `/admin`. `usage_events` for the admin went from `Active 1, GroupCreated 1, SignedUp 1` to `Active 1, ExpenseAdded 3, GroupCreated 2, SignedUp 1` (so they were recorded), while the API answer (all counts, all buckets, all-time) and the six cards and the totals line were identical before and after | PASS | VERIFIED by live run |
| 20 | No sideways scroll and nothing outside its card: 320, 360, 768, 1280 and 1920 px, EN and MK, light and dark, 8 views each (default, 30 days, 12 months, All + Week, Custom 92 days, Custom invalid, error, skeleton) = 160 views | **FAIL** in 12 of 160 views (Bug 1: 4 x Custom 92 days at 320 px, 4 x Custom 92 days at 360 px, 4 x All + Week at 320 px); 148 views clean; 8 more views at reduced motion clean | VERIFIED by live run |
| 21 | Tap targets: all 10 chips at least 44 px high and 50 px wide in every view (44 px high at every width), date fields 48 to 54 px, Settings row 52 px, Retry 264 x 52 px | PASS | VERIFIED by live run |
| 22 | Visible focus ring: chips and date fields get a solid 3 px outline (light `rgb(61,38,16)`, dark `rgb(255,165,58)`), offset 2 px for chips and 0 for date fields; `:focus-visible` true; the ring looked at in `shots\r10-en-dark-tab2.png` (orange ring round "This month") | PASS for the page; the Settings row ring is hidden behind the bar (Bug 2) | VERIFIED by live run |
| 23 | Keyboard path: Settings Tab order EN, МК, theme buttons, switch, Change password, Privacy, Statistics; Enter on the row opens `/admin`; on the page Tab goes through the 6 range chips, the grouping chips (the disabled "Day" is skipped: after 12 months the order is All, Custom, Week, Month, Year, then the bottom bar), Enter on a chip and Space on a chip both work, the Custom date field takes typed digits (MK order day month year gave 2026-10-01) | PASS (EN light, EN dark, MK light) | VERIFIED by live run |
| 24 | Colour contrast of text from the real computed colours (see "Contrast"), undimmed | PASS, 5.32:1 and above everywhere | VERIFIED by live run |
| 25 | Bars against the card at least 3:1: 4.78:1 light, 11.60:1 dark; chip and date-field borders 3.29:1 light and 3.01:1 dark (dark date field 4.44:1) | PASS | VERIFIED by live run |
| 26 | Reduced motion: skeleton animation `none` (normal motion: `pulse`), dimming `transition-property: none` (normal: opacity 0.2 s), the layout views at 360 and 1280 px (EN, light and dark) clean | PASS | VERIFIED by live run |
| 27 | Long Macedonian text: totals line wraps to 3 lines at 320 px («Од почетокот: 216 профили · 36 / гости · 272 групи · 749 трошоци · / 173 плаќања»), compare line «+1000% во однос на претходниот период» wraps below the title when it does not fit, the first card is 241 px high at 320 px (EN 222); no element is cut, minimum gap between first and last axis label 140 px at 320 px | PASS (`shots\m-320-mk-light-default.png`, `shots\m-1920-mk-dark-12months.png` looked at) | VERIFIED by live run |
| 28 | No amounts, names or emails on the page or in the answer: 200 display names and group names from the database searched in the page text (5 ranges, EN and MK) and in the raw answers: 0 found; no "@", no MKD / EUR / € / ден; the answer has only the keys `activePeople, allTime, buckets, by, expensesAdded, from, groupsCreated, joinedViaInvite, paymentsConfirmed, previousFrom, previousTo, signUps, to` | PASS | VERIFIED by live run |
| 29 | Skopje day clock: 31 Oct (UTC+1): the 4 events at 22:30 UTC stay on 31 Oct (SQL `utc_day 2026-10-31, skopje_day 2026-10-31, 4`), the 5 at 23:30 UTC count on 1 Nov (`2026-10-31 -> 2026-11-01, 5`); 14 Aug (UTC+2): both groups count on 15 Aug (14 Aug: 0 expenses, 15 Aug: 10); the group at 23:30 UTC on 3 Nov counts on 4 Nov. API and page agree with SQL | PASS | VERIFIED by live run |
| 30 | 14 months of data: 12 months by Month (Dec 2025 to Nov 2026), All by Month (Sep 2025 to Nov 2026, 15 bars), by Year (2025, 2026), by Week (62 bars); first and last labels as expected | PASS | VERIFIED by live run |
| 31 | The page asks for the Skopje day, not the device day: with the browser clock fixed at 2026-11-03 23:30 UTC and the device zone America/Los_Angeles (device date 3 Nov), the page asked `to=2026-11-04` and the Custom date fields had `max=2026-11-04` (same as with the Skopje device) | PASS | VERIFIED by live run (browser clock faked) |
| 32 | Language switch with the Settings buttons: tap МК, `<html lang="mk">`, tap Статистика: title «Статистика», first chip «7 дена», first card «Нови регистрации»; back to EN restored (`/api/me` language `en`) | PASS | VERIFIED by live run |
| 33 | Browser Back from `/admin` goes to Settings, Forward returns with 6 cards; a reload goes back to This month + Day (the choice is not kept) | PASS | VERIFIED by live run |
| 34 | The statistics are not stored in the browser: after loading, IndexedDB (`keyval-store`, `kvit-outbox`) and localStorage hold no `adminStats`, `allTime`, `previousTotal` or `personDays` text | PASS | VERIFIED by live run |
| 35 | Not-found for a non-admin looks the same as the normal not-found | **FAIL** (Bug 4) | VERIFIED by live run |
| 36 | Keyboard focus on the new Settings row is visible at 360 x 740 | **FAIL** (Bug 2) | VERIFIED by live run |
| 37 | Percent text uses the same number format as the counts | **FAIL** (Bug 5) | VERIFIED by live run |
| 38 | Text contrast while the cards are dimmed (opacity 0.6) is at least 4.5:1 | **FAIL** in light mode, 3.21:1 (Bug 3); dark mode passes (5.10:1) | VERIFIED by live run (computed from the colours) |

Count: 38 checks, **33 passed, 5 failed** (checks 20, 35, 36, 37 and 38 = Bugs 1, 4, 2, 5 and 3). The automated suites were not run.

## Contrast (computed from the real computed colours, canvas-normalised, worst value over all 160 views)
| Item | Light | Dark |
|---|---|---|
| Card title, big number, totals line | 12.72:1 | 13.91:1 |
| Sub lines and axis labels (13 px and 12 px) | 8.20:1 | 11.60:1 |
| Compare up (green) / down (red) / new | 5.91 / 5.84 / 5.91:1 | 8.78 / 7.62 / 8.78:1 |
| Compare "same" (muted; mocked) | 8.20:1 | 11.60:1 |
| Pressed chip text against its fill | 5.32:1 (white on `#b54a00`) | 13.11:1 |
| Unpressed chip text, date field text | 14.16:1 | 16.82:1 |
| "Group by" label, date label, title | 10.61:1 | 15.72:1 |
| Red invalid-dates and error text | 5.64:1 | 7.66:1 |
| Bar against the card (non-text, needs 3:1) | 4.78:1 | 11.60:1 |
| Chip border against the page (needs 3:1) | 3.29:1 | 3.01:1 |
| Dimmed (opacity 0.6): title / sub line and axis | 3.98 / **3.21**:1 | 5.92 / 5.10:1 |
The disabled "Day" chip (opacity 0.5) is exempt and was not measured.

## Look against the mockup (what I saw)
Looked at `shots\m-360-*`, `m-768-en-light-30days`, `m-1280-en-light-default`, `m-1920-mk-dark-12months`, `m-320-mk-*` and the r-series. The page follows layout A: a big bold "Statistics" title, two rows of pill chips (white pills with a thin brown edge, the active one filled orange `#b54a00` in light mode and filled peach in dark mode), the "Group by" label, a pale rounded totals line, then one rounded card per count with the title on the left and a small green or red compare line on the right (below the title when it does not fit), a 32 px heavy number, small muted sub lines, a 56 px bar strip with square-topped orange bars (peach in dark mode) and the first and last label under it. Differences from the mockup: the compare text is the same wording for every range ("vs the period before"; the mockup said "vs last month" for This month, Observation 1); the sign-up sub line is split in two lines instead of one long line; there is no "Your own account is left out" note and no in-page theme button (Observation 5); the "Range" label is hidden (only "Group by" shows, as in the mockup). Small days look right: on 4 Nov the 1, 2, 3 and 4 Nov bars have one tall bar (the busy day, 2 Nov) and three short stubs (4% minimum height). At 1280 and 1920 px two cards sit per row beside the sticky rail, at 768 px one 400 px column with the floating bottom bar (32 px between the last card and the bar when scrolled to the end at 320 and 360 px, 56 px at 768 px; the bar never covers the last card).

## NOT VERIFIED
- A real phone (touch, real date pickers, iOS Safari, Android Chrome), Firefox, a screen reader (the cards are `section` + `h2`, the bars are `aria-hidden`; I did not hear them read).
- The Cloudflare Pages Function path (only the Vite dev proxy), a real Render wake-up or Neon resume (the 7.5 s error delay and the 4 requests were measured against the local API only).
- Real "1 account" or an 8-digit number with real data (only mocked responses), and a real database with only one guest or one payment.
- The exact date-field look on a phone (headless Chromium showed day/month/year order from this machine's settings).
- A device whose local date differs from the Skopje date at the moment of use, other than with the faked browser clock (check 31), and a real late-night visit at 23:30 UTC (the known edge in DECISIONS about `Active` rows once per UTC day was not exercised: my Active rows were inserted at 09:00 UTC).
- Dark-mode contrast of the disabled chip, the contrast of the dimmed text over every background (computed by arithmetic from the colours, not from pixels), the contrast of the bars at the 4% minimum height.
- The automated suites: `dotnet build`, `dotnet test`, `npm test`, `npm run lint`, `npm run build`.
- Macedonian wording (all drafts), and Macedonian plural forms beyond 0, 1, 2, 11, 21, 101.

## Questions for Filip (product only)
1. Example: today is 4 Nov and you tap "This month". Should the line say "+200% vs 28 to 31 Oct" (the 4 days before, as built), or compare with the whole of last month, or say nothing for This month while the month is young?
2. When the server does not answer, the page shows faded old numbers for 7 seconds before the red message. Do you want the message sooner (for example after 2 seconds), at the price of fewer automatic tries?
3. Should the page say somewhere, in small text, that your own account is not counted (the mockup had that note)?
4. When the range is long on a phone (3 months by day) should the page show fewer bars (for example group by week automatically over 60 days on a narrow phone) or let the chart scroll sideways inside the card?

## Where the files are
- Scripts, data and screenshots: `C:\Users\Davchev\AppData\Local\Temp\claude\9b97ba450b6300bd\ece0a1cd-5680-4d8d-b184-23f5a4150fc6\scratchpad\p18\` (screenshots in `shots\`; `m-*` is the matrix, `r5-*` states, `r7-*` normal user and guest, `r10-*` focus, `r14-*` the bar overflow). Helpers `lib.py`, `h.py` (reads the page), `verify.py` (what the page must show), `expect.py` (SQL), `apictl.ps1` (stop and start only the API); scenario scripts `seed.py`, `extra.sql`, `r1.py` to `r16.py`, `q1.py` to `q4.py`. Results as JSON: `r2_res.json`, `r5_res.json`, `r6_full.json`, `r6_reduced.json`, `r7_res.json`, `r9_res.json`, `r10_res.json`. Not in the repository.

## Throwaway data left in the local database
- Users `p18admin1793785415@example.com` (has the Admin role; remove it with `dotnet run scripts/GrantAdmin.cs -- p18admin1793785415@example.com --revoke`), `p18ana1793785415@example.com`, `p18bojan1793785415@example.com`, the guest "Gosho Guest", all with password `Passw0rdA`.
- Groups "P18 Trip" (Ana, Bojan, the guest; expenses "P18 dinner" and "P18 taxi", one confirmed payment), "P18 Admin group" and "P18 admin g2" (the admin; 3 expenses "admin exp 0 to 2").
- `usage_events` ids 1370 to 4655 are my synthetic rows (3,286). To remove them: `DELETE FROM usage_events WHERE id BETWEEN 1370 AND 4655;` (the events just below id 1370 and ids 4657 to 4661 were made by my accounts through the real app).
- The admin's saved language was switched to MK and back to EN once (it is `en` now). No other user's data was touched.

## Summary
| Item | Result |
|---|---|
| Entry, defaults, every range and grouping chip, Custom and its invalid pairs | PASS (checks 1 to 8) |
| Numbers (UI against API against SQL), Skopje days, 14 months | PASS (checks 4, 5, 29, 30) |
| Cards, compare line, plurals, big numbers | PASS (checks 9 to 12) |
| Skeleton, dimmed, error and Retry, real API stop | PASS (checks 13 to 15); Observation 2 and Bug 3 |
| Normal user, guest, logged out, admin's own events | PASS (checks 16 to 19) except Bug 4 |
| Sizes, scroll, tap targets, focus, keyboard, contrast, motion, Macedonian text | PASS except Bug 1 (medium), Bug 2, Bug 3 (checks 20, 36, 38) |
| Privacy of the page and of the browser | PASS (checks 28, 34) |
| Bugs | 1 (medium), 2, 3, 4, 5 (low) |
