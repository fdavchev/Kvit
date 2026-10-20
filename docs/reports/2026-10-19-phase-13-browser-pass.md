# Phase 13 Step 6: real-browser pass (2026-10-19)

Branch `feat/13-personal-spending`. Run by the tester. Nothing in the app was changed, nothing was committed or staged. Only this report was written.

Labels: **VERIFIED by live run** (I did it in a real browser against the local app and looked at the result), **VERIFIED by automated test**, **NOT VERIFIED**.
The automated suites (dotnet build and test, npm lint, build and test) were **not run** in this pass; it was only the browser pass.

## What was run and how
- Docker Desktop was open. `.\scripts\start-local.ps1 -NoBrowser` started the database, the API (5018) and the website (5173). Both answered. At the end `.\scripts\start-local.ps1 -Stop` stopped everything ("Ports 5018 and 5173 are free").
- The two Phase 13 migrations were already applied: `20261019111652_PersonalCategories` and `20261019114833_PersonalEntries` are in `__EFMigrationsHistory`, and `GET /api/personal/categories` answered 200 for a signed-in user. VERIFIED by live run.
- Tool: Playwright for Python 1.63.0, headless Chromium only (no visible window). Phone touch emulation, `colorScheme` light and dark, English (`en-US`) and Macedonian (`mk-MK`) browser language, `reducedMotion: reduce` once. The Settings theme "Dark" was also tried once while the computer was light.
- Throwaway data left in the local database: user A "Filip Alpha" `p13a1792417478@example.com`, user B "Bojan Beta" `p13b1792417478@example.com` (both password `Passw0rdA`), a guest "Gosho Guest" (joined by invite link, "I'm new"), group "Ohrid trip". A has about 25 spending entries (months Aug 2025, Sep 2026, Oct 2026, Dec 2026, one in Oct 2027) and about 10 custom categories; B has 24 custom categories ("Cat number 1" to "24") made through the API for the many-categories test.
- Nothing was blocked. For the loading look I only held back requests whose address starts with `/api/personal/entries?` (answered later by the script), as allowed.
- No sign-up or log-in limit was reached (2 sign-ups, one guest join).
- Screenshots (164 files, not in the repository): `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\32e619e6-68b1-4a88-afd6-ffb8e56303eb\scratchpad\shots13\`. The scripts are in the `scratchpad` folder one level up.
- Matrix `m1-*`: 360, 768, 1280 px x English, Macedonian x light, dark = 12 combinations, each with the Budget screen, the category grid, the new-category sheet, My categories, the Edit category sheet and the entry edit sheet (names like `m1-grid-360-mk-dark.png`). I looked at a sample of them by eye (360 en/mk, light/dark, 768, 1280) and measured all 12 in the page (horizontal overflow, bottom bar labels, title, card width).

## Results table
| # | Check | Result | Label |
|---|---|---|---|
| A | Set-up: 2 normal users by email, a guest through an invite link, a group, Macedonian by browser language | PASS | VERIFIED by live run |
| B1 | Bottom bar at 360 px, English and Macedonian: four tabs Home, Groups, Budget, Settings / Почетна, Групи, Буџет, Поставки. Each tab is 80 px wide, every label fits on one line with no clipping (page-measured and seen in screenshots) | PASS | VERIFIED by live run |
| B2 | Active pill is on Budget on the Budget tab (white in light, tan in dark), at 360, 768 and 1280 | PASS | VERIFIED by live run |
| B3 | Desktop 1280: left menu order Home, Groups (open groups under it), Budget, Settings | PASS | VERIFIED by live run |
| C1 | Budget empty state at 360: "Nothing spent this month", "Tap + to add the first one.", round "+" | PASS | VERIFIED by live run |
| C2 | Home "+" menu: "Add to Ohrid trip", "New One bill", "Another group...", "Personal spending" **last**. Desktop: same four as round buttons | PASS | VERIFIED by live run |
| C3 | Home path: "+", "Personal spending", category, type amount, Save. Amount focused with a numeric keypad (`inputmode=numeric`), currency chip MKD the first time | PASS (4 taps, 1 field) | VERIFIED by live run |
| C4 | Budget "+" path: "+", category, Save. Amount focused | PASS (3 taps counted from the tab, 2 inside the sheet, 1 field) | VERIFIED by live run |
| C5 | After saving a EUR entry the next sheet opens in EUR, also after a page reload; user B, who never saved one, gets MKD. Stored per user as `kvit.lastEntryCurrency.<userId>` | PASS | VERIFIED by live run |
| C6 | Without an open group Home has no "+" and no Personal spending (already the open question in DECISIONS Step 4); the Budget "+" works there | Matches the notes | VERIFIED by live run |
| D1 | "+ New" tile opens the new-category sheet at once, name focused, emoji pre-filled with the tag, orange selected with outline and check mark | PASS | VERIFIED by live run |
| D2 | Create with default look, and with a quick-pick emoji (🏋️) and another colour (Blue): the emoji and `aria-pressed` change, category appears in the grid, entry header shows it | PASS | VERIFIED by live run |
| D3 | After Save of a new category the flow goes on to the amount, **but the amount is not focused** | **FAIL, Bug 1** | VERIFIED by live run |
| D4 | Sheet height at 360 wide with a short window (keyboard-like) | **FAIL, Bug 3** (Save stays reachable, the name field does not) | VERIFIED by live run (real phone NOT VERIFIED) |
| D5 | Name rules, English: empty gives "Enter a name (up to 30 characters)."; typing stops at 30 (the 31st character cannot be typed; the server also answers `CATEGORY_NAME_INVALID` for 31 sent directly); "coffee", "COFFEE " (trailing space) after "Coffee" give "You already have a category with this name."; Cyrillic "Кафе" then "кафе" and "КАФЕ" give the same message. Name box gets the red edge (computed border `rgb(165, 29, 29)`), the message sits above Save | PASS | VERIFIED by live run |
| D6 | Name rules, Macedonian: empty «Внеси име (најмногу 30 знаци).», duplicate «Веќе имаш категорија со ова име.» | PASS | VERIFIED by live run |
| D7 | Edit my categories: rename (PUT 204, list updates), Archive (toast "Archived: Tea" with Undo, Undo calls unarchive and the row is back), archived section "ARCHIVED (1)" with "Bring back", Bring back works; Bring back while an active category has the same name: 400 and the toast "You already have a category with this name." | PASS | VERIFIED by live run |
| D8 | Double click on Save and double Enter in the new-category sheet send one request each | PASS | VERIFIED by live run |
| E1 | Budget screen look 1B: title "Budget", card "Spent this month" (fixed label), MKD total big, EUR total on its own line with no "+", tiles with totals, two labelled groups (MKD, EUR) when both exist, one grid with no label otherwise (Aug 2025), entries newest first as "date · note" ("15 Sep · Dinner out", "Today · note") | PASS | VERIFIED by live run |
| E2 | Month strip: 24 pills (Nov 2024 to Oct 2026), current month selected and the strip scrolled to the right end (scrollLeft 1624 of max 1624), year on other years' months and on January ("Jan 2026", then Feb...), sideways scroll works by mouse wheel (horizontal) and by touch swipe | PASS | VERIFIED by live run |
| E3 | Switch months (Oct, Sep, Aug 2025, Jul 2025 empty): each shows its own numbers; a month never loaded shows the pulsing skeleton while its request is held back and no old numbers; a month already loaded shows at once | PASS | VERIFIED by live run |
| E4 | Long values at 360: 9-digit MKD total "123,457,643 MKD" fits in the card; a 30-letter one-word category name and a 60-character one-word note are cut with "..." in the list (two lines) and wrap in the tile; no sideways page scroll anywhere (`scrollWidth` 360 of 360 on Budget, in the category sheets and Macedonian too) | PASS, except Bug 5 (tile amount splits) | VERIFIED by live run |
| E5 | Floating "+" does not cover the last entry when scrolled to the end (last row bottom 552, "+" top 574, tab bar top 666). While scrolled mid-list it sits over the right end of rows; scrolling frees them | PASS | VERIFIED by live run |
| E6 | Title height at 360: the title box is 90 px high, starting 60 px from the top. At 1280 px I measured the Groups title next to it (top 115.6, height 96.9 against Budget 116 and 97), so it is the shared screen title, not a Budget-only problem; at 360 px I did not measure Groups | PASS (same as other tabs) | VERIFIED by live run |
| E7 | Future-month entry (see Bug 4) | **FAIL, Bug 4** | VERIFIED by live run |
| F1 | Tap an entry: sheet opens filled in (category header, amount focused, date chip, Category chip, note field when a note exists, Save, red Delete) | PASS | VERIFIED by live run |
| F2 | Change amount, currency, date, note and category: the Category chip opens the grid ("What did you spend on?") and comes back with the typed amount kept (7.25 stayed), Save updates the list and the totals (entry moved from the EUR group to the MKD group) | PASS | VERIFIED by live run |
| F3 | Double click on Save (edit) sent exactly one `PUT`; same for add (one `POST`), double Enter (one `POST`) | PASS | VERIFIED by live run |
| F4 | Delete: DELETE 204, toast "Deleted" with Undo, no "Are you sure?", the row leaves the list; Undo calls restore (204), toast "Restored", row is back | PASS | VERIFIED by live run |
| F5 | An entry whose category was archived afterwards: still shown in the list and in the totals with its own category name and emoji; it opens, the amount can be changed and saved (PUT 200); the grid (add and change category) no longer offers the archived category | PASS | VERIFIED by live run |
| G | Guest: bottom bar and desktop menu without Budget (Home, Groups, Settings); Home "+" has no Personal spending; `/budget` ends on `/settings` and sends no personal request; API answers 403 ("A guest cannot do this. Make an account first.") for GET entries, GET categories, GET categories with archived, POST categories, at 360 and 1280 | PASS | VERIFIED by live run |
| H | Privacy, as user B against user A's data: B's lists are empty (entries Oct and Sep) and B sees only the 10 built-in categories; B's PUT, DELETE and restore of A's entry id give 404; B's PUT, archive, unarchive of A's category id give 404; B adding an entry with A's category id gives 400; A's data unchanged afterwards. Bad month values (`2026-13`, `2026-1`, `abc`, empty, `2026-10-01`) give 400 | PASS | VERIFIED by live run |
| I | Desktop 1280: Budget content capped and centred (card 611 px wide, 443 to 1054, like the other screens), round "Add spending" button under the strip (no floating "+"), sheets are glass popups, no sideways scroll. 768: bottom bar and the 400 px phone column stay, floating "+" | PASS, except Bug 2 (strip scrollbar, clipped first pill) | VERIFIED by live run |
| J | Regression: group expense (Home "+", "Add to Ohrid trip", amount, Category chip) shows only the 10 built-in categories (no custom or archived ones); expense saved (POST 200); Home shows the new totals and activity; Settings unchanged; the group page shows the expense | PASS | VERIFIED by live run |
| K1 | Light and dark: swatch outline and check mark visible in both, selected pill is tan in dark and white in light, tile text readable, glass sheets readable, dark skeleton visible (`rgb(66, 53, 49)` on `rgb(31, 24, 22)`), Settings "Dark" with a light computer also gives the dark Budget | PASS | VERIFIED by live run |
| K2 | Reduced motion: sheets and nested sheets do not slide (`translate: none` at every sample, transition property `none`), skeleton has no animation (`none`), the bottom pill jumps (160 to 240 in one step; with normal motion it moves 160, 181, 215, 231, 239, 240), toast and button press transitions are `0s`. With normal motion the sheet slides 100 % to 0 % in 0.2 s | PASS | VERIFIED by live run |
| K3 | List rows fading in and out when entries change: the samples taken right after Save caught no animation either way (the sheet closes only after the list reloaded) | NOT CHECKED | NOT VERIFIED |
| L1 | Macedonian: every visible text on the Budget screen, the grid, new and edit category sheets, My categories, the entry sheet, the date sheet, the toasts and the empty month is Macedonian. No raw keys (`budget.screen.x`, `expense.x`) were found on any screen in either language (page text searched). The only Latin text is user data (category names I typed, notes) and "EUR" | PASS | VERIFIED by live run |
| L2 | Macedonian wording remarks | see Observations | VERIFIED by live run |

## BUGS
Numbered by how much they hurt. I did not fix anything.

**Bug 1 (medium): after "+ New" saves a category, the amount field is not focused, so the number keypad does not open and typed digits go nowhere.**
- Where: Budget tab "+", then "+ New" in the grid, name, Save; the screen goes on to the amount stage of the same sheet. Files involved (read only): `src/web/src/features/budget/categories/components/NewCategorySheet.tsx`, `PersonalCategoryGrid.tsx`, `src/web/src/features/budget/entries/components/PersonalSpendingSheet.tsx`, `src/web/src/features/expenses/shared/components/AmountField.tsx`.
- What I saw: the amount stage appears with the new category in the header and "0" in the box, but `document.activeElement` is the Budget page's floating "+" button (the focus goes back to the button that opened the first sheet when the small sheet closes). I typed "7": nothing appeared; the amount box stayed empty. Picking a normal tile instead focuses the amount box correctly (inputmode numeric / decimal).
- Expected: the amount focused with the number keypad (DECISIONS Step 4: "amount focused"; "speed is the product"). Costs one extra tap on every "new category" path (and on a phone the keyboard would close).
- Repeat: sign in, open `/budget`, tap the round "+", tap "New", type "Tea", tap Save, then without touching anything type "7" and look at the box. Screenshot `shots13\d2-after-tea.png`. Only the Budget "+" start was tried; the Home start was not (likely the same).

**Bug 2 (medium for Filip on Windows, low elsewhere): with a normal Windows scrollbar the month strip shows a rectangular grey scrollbar under the pills, sticking out of the rounded strip, and the first pill is cut in half.**
- Where: Budget at 1280 px (the strip is wider than the 40rem column). Also the quick-emoji row in the new and edit category sheet shows the same bar. File: `src/web/src/features/budget/overview/components/MonthStrip.tsx` (the strip has `overflow-x-auto`, nothing hides the scrollbar).
- What I saw: the default headless Chromium hides scrollbars, so I started a second browser with scrollbars switched on (what a normal Windows Chrome shows). The strip grows from 56 to 71 px, a grey track with arrows hangs under the pills outside the rounded background, and the left-most pill reads "2026" (English) or "ан 2026" (Macedonian), a clipped "Jan 2026". In the default run (no visible bar) the clipped pill is still there and there is no hint that it scrolls; a vertical mouse wheel does not scroll it (only a horizontal wheel, Shift + wheel, touch, or the bar).
- Expected: no bar outside the rounded strip, no half-cut pill (or a clear fade or arrows), the SCREENS note in DECISIONS already lists this as "to look at in Step 6".
- Repeat: open `/budget` on a 1280 px wide real Chrome on Windows. Screenshots `shots13\g2-strip-scrollbar-light.png`, `g2-strip-scrollbar-dark.png`, `g3-newcat-1280-scrollbars.png`.

**Bug 3 (medium, real phone NOT VERIFIED): the add and edit sheets can be taller than a keyboard-sized window, and the top of the sheet (title and name field) goes off the screen with no way to scroll.**
- Where: `src/web/src/shared/components/KvitSheet.tsx` has no height limit and the sheets are bottom-anchored. Measured at 360 px wide, sheet top and the position of the field:
  - New or Edit category sheet is 568 px high. Viewport 740: name field at y 262. 500: name field at y 22 (title already cut off). 440: name field at about y -38 (mostly off the screen). 400: name field at y -78 (completely off the screen, but it is the focused field, so typing works unseen); Save is still visible (bottom 375).
  - Edit entry sheet with a note open is 553 px high. At 500 the amount is at y 40 (fine). At 440 the amount box is at y -20 (cut off) and at 400 at y -60; Save and Delete stay visible.
  - Add sheet with a note is 489 px high: at 440 the amount is at y 44 (fine).
- A 740 px phone with a keyboard (about 290 to 300 px) leaves about 440 px, so on a real phone the new-category name field would likely be hidden. A real phone could scroll the page; that is NOT VERIFIED, this is the emulated short window the brief asked for.
- Expected: Save reachable (it is) and the focused field visible (it is not at 440 or less). Screenshots `shots13\e5-newcat-400.png` (name field and title off the top), `e5-newcat-740.png` for comparison.

**Bug 4 (medium as a product gap): an entry dated in a later month is saved ("Saved") and then can never be seen.**
- Where: the date sheet lets you pick up to a year ahead (its own hint says "Any date, up to one year ahead."), the server accepts it (I saved one dated 2026-12-05 and one 2027-10-19, both 200), but the Budget month strip stops at the current month by design, so there is no pill for December. Files: `src/web/src/features/expenses/shared/components/DateSheet.tsx`, `src/web/src/features/budget/overview/components/BudgetScreen.tsx` (`recentMonths`).
- What I saw: I added "Gifts 500 MKD, Dec present" for 2026-12-05 through the sheet: toast "Saved", the sheet closed, the entry is nowhere on the Budget tab. `GET /api/personal/entries?month=2026-12` returns it. A later date inside the current month (28 Oct) shows normally and counts toward "Spent this month".
- Expected / question for Filip: either limit the date to today for personal spending, or show later months. Today the user thinks it was saved and cannot find it. Screenshot `shots13\fu-after-future-save.png`.

**Bug 5 (low): a long amount in a category tile breaks inside the number.**
- Where: the 4-column tile in `src/web/src/features/budget/overview/components/CategoryTotals.tsx`. At 360: "€1,234,567.8" then "9" on the next line (`e9-budget-bottom.png`). At 768: "123,456,789 MK" then "D" (`m1-budget-768-en-light.png`). Normal amounts are fine. Expected: break at a space or shrink, not inside the number or the currency code.
- Also (very long amounts): in the edit sheet the box "1234567.89" at 360 touches the left edge, the first digit is clipped (`shots13\m1-editentry-360-mk-light.png`).

**Bug 6 (medium, only with many custom categories): the category grid sheet becomes taller than the screen and cannot be scrolled; the title and the first rows are cut off.**
- Where: `PersonalCategoryGrid.tsx` inside the sheet, no height limit (the "My categories" list is fine, it scrolls inside 60 % of the height).
- What I saw: user B with 24 custom categories: at 360 x 740 the sheet is 1011 px high, its top is 271 px above the screen, so "What did you spend on?", Food, Groceries, Transport and the first tiles are unreachable (the scroll height equals the client height). Same at 360 x 667 (top at -344 px) and at 1280 x 800 (top at -153 px, a 953 px sheet). Nothing stops a user making that many categories (the API took all 24). I only measured 24; the point where it starts is NOT measured, by arithmetic it is somewhere around 15 to 20 custom categories on a 740 px phone and around 12 on a 667 px phone (rows are 85 to 100 px).
- Expected: the grid scrolls inside the sheet. Screenshots `shots13\s1-many-cats-360x740.png`, `s1-many-cats-1280x800.png`.

No backend bug was found: every status code and error code I sent behaved as the DECISIONS say (see the API probe below).

## API probe (done from the browser session, VERIFIED by live run)
- Categories: name 31 characters, name blank give 400 `CATEGORY_NAME_INVALID`; empty emoji or 17 characters give `CATEGORY_EMOJI_INVALID`; colour "magenta" gives `CATEGORY_COLOR_INVALID`; "tEa" next to "Tea" gives `CATEGORY_NAME_TAKEN`.
- Entries: amount 0 and -5 give `EXPENSE_AMOUNT_NOT_POSITIVE`; 10^15 gives `EXPENSE_AMOUNT_TOO_LARGE`; currency USD gives `EXPENSE_CURRENCY_INVALID`; date 2030-01-01 and 1999-12-31 give `PERSONAL_ENTRY_DATE_INVALID`; 2027-10-19 (one year ahead) is accepted; a 501-character note gives `EXPENSE_NOTE_INVALID`; MKD 101 (not whole denars) gives `MONEY_NOT_ON_CURRENCY_STEP`; EUR 1 cent is accepted; the same `clientRequestId` twice returns the same entry id (no duplicate).

## OBSERVATIONS (small things, not bugs)
1. Switching the currency chip from EUR to MKD while the box holds "7.25" turns Save off with no message (MKD has no decimals). Same for an amount longer than 10 digits ("99999999999"): Save is simply disabled and nothing says why. It is the shared amount box, so it behaves the same on group expenses; worth a hint some day.
2. An empty category name is sent to the server (Save is enabled) and the translated error comes back. It works, but costs a request.
3. Adding: after picking a category there is no way back to the grid in the add sheet (decided in Step 4); closing and starting again is the only way.
4. The empty-month text says "Nothing spent this month" / «Овој месец нема трошоци» even when a past month (Feb 2025) is selected. It follows the fixed-label rule, but reads wrong for past months.
5. Macedonian wording, for Filip to look at: "Edit" on a category row is «Измени» but the link is «Уреди ги моите категории» (two verbs for one thing); «Врати» is used both for "Undo" on the toast and for "Bring back" (the Archived section and the toast both say «Врати»); the currency heading above the tiles in Macedonian is the lowercase «ден.» next to the English-looking "EUR"; «Допри + за да додадеш.» drops "the first one" of the English; «Сѐ уште немаш свои категории...» uses «Допри „+ Нова“» fine. I found nothing gendered in the Budget wording («Вратено», «Избришано», «Архивирано» are neutral). «На што потроши?» is informal singular. All Macedonian drafts remain NOT VERIFIED as approved by Filip.
6. The "Saved" toast from a previous save can sit over the lower fields of a nested sheet (`shots13\d1-dup-en.png`); it goes away by itself.
7. In the category grid sheet the first tile ("Food & drinks") shows a dark focus ring right after opening when the sheet was opened with the keyboard Escape/Enter route; not seen with a tap.
8. A 30-letter one-word category name wraps to 8 lines in "My categories" at 360, which pushes Edit and Archive to the middle (`shots13\m1-mine-360-mk-dark.png`); it is readable and scrolls.
9. The placeholder "Coffee" in the name box is brown and looks like typed text when the box is empty (it is only a placeholder).
10. In headless Chromium without the scrollbar flag the strip has no scrollbar and no hint that it scrolls (see Bug 2). On a phone the swipe works (touch swipe moved 1024 to 839).
11. Console: in the runs that logged it, there were no app errors or warnings, only the browser's own "Failed to load resource ... 400/401/403" lines for refused requests I sent on purpose.
12. The "Add" sheet is bottom-anchored on desktop too (glass, rounded top, centred horizontally); the entry edit sheet at 1280 x 800 touches the bottom edge of the window.

## Tap counts measured
- Home path: "+", "Personal spending", category, Save = **4 taps + 1 typed field** (amount focused, keypad). Matches DECISIONS Step 4.
- Budget path: "+", category, Save = **3 taps counted from the Budget tab (2 once the sheet is open) + 1 typed field**. (The brief said "2 taps"; that counts only category and Save.)
- EUR entry: one more tap (the currency chip). Default currency afterwards stays EUR.
- New category on the way: "+", "+ New", type the name, Save, **then one extra tap on the amount box because of Bug 1**, type, Save = 4 taps + 1 more tap (Bug 1) + 2 typed fields, and 2 taps + 1 field if Bug 1 were fixed (counting only from "+ New"; name field is focused at once).
- Edit an entry: tap the row, change, Save = 2 taps + the change; Delete = 2 taps (row, Delete) with Undo 1 more.

## NOT VERIFIED
- A real phone, real keyboards (Bug 3 and Bug 1 are only measured in an emulated window), iOS Safari, Firefox, a screen reader, hover states.
- Bug 1 from the Home "+" start (only the Budget "+" start was run).
- List rows animating on add and delete (the samples caught nothing), the sheet animation and the toast animation by eye in motion beyond the sampled numbers.
- The point at which Bug 6 starts (only 24 custom categories were measured).
- Different time zone (the Budget month is taken from the user's zone; only the browser default was used).
- A user with several hundred entries in one month (speed and layout).
- Macedonian texts approved by Filip.
- The automated suites (not run in this pass).

## Questions for Filip (product only)
1. Should a personal entry be allowed to have a date in a future month (Bug 4)? Example: today is 19 Oct, you type 5 Dec for a present you already paid for; it is saved but you could never see it until December. Choose: (a) limit the date to today, (b) show the months ahead on the strip, (c) keep as is.
2. Should "Personal spending" also appear when you have no open group yet (the open question from Step 4 is still true: the empty Home has no "+")?

---

# Re-check after fixes (2026-10-19)

Run by the tester against the working tree of `feat/13-personal-spending` (the fixes were uncommitted). Nothing in the app was changed, nothing was committed or staged. Only this section was added.

Labels: **VERIFIED by live run** (real headless Chromium against the local app, measured in the page and looked at in a screenshot), **VERIFIED by automated test**, **NOT VERIFIED**. The automated suites were **not run** in this re-check.

## How it was run
- Docker Desktop was open. `.\scripts\start-local.ps1 -NoBrowser` started everything; at the end `.\scripts\start-local.ps1 -Stop` stopped it ("Ports 5018 and 5173 are free").
- Playwright for Python, headless Chromium only. For the scrollbar checks Chromium was started with `--hide-scrollbars` switched off, so classic Windows scrollbars are drawn (measured: an `overflow: scroll` box has `offsetWidth - clientWidth` = 15). The sessions of the first pass (user A, user B with 24 custom categories, the guest) were reused, no new sign-up.
- Scripts: `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\32e619e6-68b1-4a88-afd6-ffb8e56303eb\scratchpad\` (`t1.py` to `t6.py`, helper `h2.py`). Screenshots (not in the repository): `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\32e619e6-68b1-4a88-afd6-ffb8e56303eb\scratchpad\shots13b\`.
- Throwaway data left in the local database: user A got two more custom categories ("Fxb...", "Fxh...") with a 7 EUR entry each, plus the category and entry from the first run of the Bug 1 test. The entries "Groceries 14" and "Transport 11" made for the double-click checks were deleted again. User A still has the very large amounts (123,456,789 MKD, 1234567.89 EUR) in October.

## Bug 1: amount focus after "+ New" -- PASS

| Check | Result | Label |
|---|---|---|
| Budget "+" -> "+ New" -> name -> Save, then type "7" without touching anything | amount box shows `7`, `document.activeElement` is the amount `INPUT` (`inputmode=decimal`) | PASS, VERIFIED by live run |
| Home "+" -> "Personal spending" -> "+ New" -> name -> Save, type "7" | same: amount shows `7`, active element is the amount `INPUT` | PASS, VERIFIED by live run |
| A normal tile (Transport) from Budget and from Home still focuses the amount | typed "5" shows `5`, active element is the amount `INPUT` | PASS, VERIFIED by live run |
| Saving that entry closes the sheet; focus goes back to the Budget "+" button (Budget) or to the page (Home) | no crash, no stuck sheet | PASS, VERIFIED by live run |
| Escape from the add sheet after a normal tile | sheet closes, no crash | PASS, VERIFIED by live run |
| Cancel the New sheet without saving (Escape, backdrop tap) | the New sheet closes, the grid stays, picking a tile afterwards focuses the amount, no crash | PASS with a remark, see Bug 8 |

Screenshots: `shots13b\t1-budget-after-new.png`, `t1-home-after-new.png`, `t1-budget-cancel-new.png`.

## Bug 2: windows-style scrollbars, month strip and emoji row -- PASS for the look, but a NEW bug (Bug 7) in the wheel behaviour

Measured at 1280 x 800, light and dark, with classic scrollbars drawn.

| Check | Result | Label |
|---|---|---|
| No grey scrollbar under the month strip | strip `offsetHeight` 56 = `clientHeight` 56, bar height 0, computed `scrollbar-width: none` | PASS, VERIFIED by live run |
| Strip height stays about 56 px | 56 at 1280 (first pass with the bar: 71); 52 at 360 | PASS, VERIFIED by live run |
| No grey scrollbar under the emoji quick-pick row (New category sheet) | row 51 px = 51 px, bar 0, light and dark | PASS, VERIFIED by live run |
| Edge fade only on the side with more to scroll | at the right end only the left fade (`data-more-before`); at the left end only the right fade; in the middle both (`t2-strip-light-crop-end/start/mid.png`, `t2-strip-dark-crop-mid.png`); the emoji row the same | PASS, VERIFIED by live run |
| Vertical mouse wheel over the strip moves it sideways | wheel 200 up: scrollLeft 1470 -> 1270; wheel 3000 up: 0; wheel 150 down: 150; wheel 5000 down: right end; the same over the emoji row (0 -> 100) | PASS, VERIFIED by live run |
| Horizontal wheel | scrollLeft moved 1470 -> 1269 with `wheel(-200, 0)` | PASS, VERIFIED by live run |
| Wheel off the strip scrolls the page | scrollY 0 -> 300 | PASS, VERIFIED by live run |
| Wheel up at the left end scrolls the page up (page scrolled down first) | scrollY 60 -> 0 at 1280 and 1366 | PASS, VERIFIED by live run |
| **Wheel down at the right end scrolls the page** | **does NOT at 1280 x 800 and at 1920 x 1080: scrollY stays 0 after 4 wheel-downs; it works at 1024, 1366, 1440 and 768** | **FAIL, Bug 7** |
| Touch swipe sideways (360 x 740, touch events) | finger 150 px right: scrollLeft 1639 -> 1497; swipe back: 1639; a vertical swipe on the strip scrolls the page, not the strip | PASS, VERIFIED by live run |
| Tab through the pills | every pill gets the focus ring (`:focus-visible` true) and the strip follows; but see Bug 9 | PASS with a remark, VERIFIED by live run |
| Selected pill is not clipped | at the right end "Oct" ends 4 px before the strip edge; at the left end the first pill, selected, is fully visible (`t2i-select-first-pill.png`, `t2-strip-light-crop-end.png`) | PASS, VERIFIED by live run |

## Bugs 3 and 6: sheet height limit and inner scroll -- PASS

How the new sheets work (measured): the popup is limited to about `100dvh - 1.5rem`, the title stays on top, the rest is `[data-kvit-sheet-body]` with `overflow-y: auto`. In the form sheets (New category, Edit category, add amount with the note, edit entry) the fields area scrolls on its own and the Save (and Delete) button stays pinned at the bottom. Every sheet below was opened and measured; the test was: sheet top not above 0, title inside the screen, Save and Delete inside the screen. Result for all of them: **OK** (no problem found).

| Sheet | 360 x 740 | 360 x 500 | 360 x 440 | 360 x 400 | 1280 x 800 |
|---|---|---|---|---|---|
| Add: grid stage (user A, 10 categories) | OK | OK, body scrolls 572 in 422 | OK, 572 in 362 | OK, 572 in 322 | OK |
| Add: grid stage, user B with 24 custom categories | OK, title at 45-68, scrolls 957 in 662 | OK | OK | OK | OK, scrolls 895 in 716 (dark), title visible |
| New category | OK | OK (fields scroll 412 in 320) | OK (412 in 260) | OK (412 in 220) | OK |
| Edit category | OK | OK | OK | OK | OK |
| My categories | OK | OK | OK | OK | OK (list scrolls inside) |
| Add: amount stage, with note open | OK | OK | OK | OK | OK |
| Edit entry with note open (Save and Delete visible) | OK | OK | OK | OK | OK |

Dark was run for user A at 740 and 440 and for user B at 1280 x 800: OK as well. All VERIFIED by live run (read from `getBoundingClientRect`; the screenshots `t3-*-A-360x400-light.png`, `t3-grid-B-360x400-light.png`, `t3-grid-B-1280x800-dark.png`, `t3-mine-B-360x440-light.png` were looked at).

| Other check | Result | Label |
|---|---|---|
| Scrolling inside a sheet does not scroll the page behind (360 x 400, page at scrollY 200 beforehand) | wheel in the fields (up, down, past the bottom), wheel over the title, wheel over the backdrop, 3 touch swipes up and 3 down inside the sheet: scrollY stays 200 every time | PASS, VERIFIED by live run |
| Focus rings of the first and last field are not clipped (New category at 400 px high: name field, last colour swatch "Slate") | both rings fully visible (`t3e-focus-first-newcat-400.png`, `t3e-focus-last-newcat-400.png`) | PASS, VERIFIED by live run |
| Opening and closing animation still plays | normal motion: the sheet slides from `translate 0 100%` to `none` in about 0.3 s and closes `0% -> 100%` with opacity 1 -> 0 in about 0.2 s (sampled each frame); reduced motion: no slide, `translate: none` all the time, close jumps straight to the end | PASS, VERIFIED by live run |
| Classic scrollbars drawn: a sheet that needs scrolling shows one scrollbar inside the sheet (New category at 1280 x 500: the fields area, 15 px wide, `t3f-newcat-1280x500-scrollbars.png`), none on sheets that fit | as designed | PASS, VERIFIED by live run |

Regression glance at the other `KvitSheet` users at 360 x 740, light and dark (measured: no scrollbar in the body, no button outside the sheet, no sideways page scroll, glass look `backdrop-filter: blur(14px) saturate(1.6)` intact; screenshots looked at for date, split, Home menu, record payment (dark), join Log in (dark), guest Log out): date sheet ("Pick a date"), Paid by, Split, Pick a category (group expense), Record another payment, "Bojan Beta paid me" (Mark paid), member options (Make owner, Remove, Close), join page "Log in" sheet, guest "Log out as a guest?" sheet, Home "+" menu: **all PASS, VERIFIED by live run**. The only console line was the expected 401 on the logged-out join page. These other sheets were not re-run at the short window heights.

## Bug 5: long amounts -- PASS (one legibility remark)

User A has 123,456,789 MKD and 1234567.89 EUR in October (`t5-tiles-360-en-US.png`, `t5-tiles-360-mk-MK.png`).

| Check | Result | Label |
|---|---|---|
| Tile amounts on one line, 360 and 768, English and Macedonian | every `[data-tile-amount]` is exactly one line (height = line height), `scrollWidth` = `clientWidth`; the currency stays glued to the number (non-breaking space) | PASS, VERIFIED by live run |
| No horizontal page scroll | `scrollWidth` 360 of 360 and 768 of 768 in all four combinations | PASS, VERIFIED by live run |
| Font size | normal amounts 12 px; "123,456,789 MKD" 7.9 px at 360 and 10.3 px at 768; "123.456.789 ден." 7.4 px at 360 and 9.6 px at 768; "€1,234,567.90" 9.1 px at 360 | PASS, see Observation 2 |
| Edit sheet with the huge entry: padding on both sides, first digit visible | input padding 12 px left and right (plus 2 px border), no overflow (`scrollWidth` = `clientWidth`), "123456789" and "1234567.89" fully inside at 360 and 768, English and Macedonian (`t5b-edit-big-EUR-360-en-US.png`, `t5b-edit-big-MKD-360-mk-MK.png`) | PASS, VERIFIED by live run |
| Typing longer amounts steps the size down | 1 to 7 characters 48 px, 8 to 10 characters 36 px, 11 or more 30 px, for MKD "1234567890123" and EUR "1234567.89012" | PASS, VERIFIED by live run (see Observation 1) |
| Group expense amount field with normal amounts looks as before | `t5c-group-600.png` against the first pass `shots13\j1-add-expense.png`: same position, size, chip and focus ring; with 12 digits it steps down to 30 px and stays in the box | PASS, VERIFIED by live run |

## Item 5: happy path regression -- PASS

| Check | Result | Label |
|---|---|---|
| Add from Budget "+" (Transport 11), double click on Save | exactly one `POST /api/personal/entries` (200), sheet closed | PASS, VERIFIED by live run |
| Add from Home "+" -> "Personal spending" (Groceries 13), double click on Save | exactly one `POST` (200), sheet closed | PASS, VERIFIED by live run |
| Edit (13 -> 14), double click on Save | exactly one `PUT` (200), the row shows 14 and 13 is gone | PASS, VERIFIED by live run |
| Delete | `DELETE` 204, toast "Deleted" with Undo, row gone | PASS, VERIFIED by live run |
| Undo | restore call 204, toast "Restored", row back | PASS, VERIFIED by live run |

## NEW bugs found in the re-check

**Bug 7 (medium, desktop mouse only): the month strip swallows the mouse wheel at its right end, so the page cannot be scrolled down with the cursor over the strip at some window widths.**
- Where: `src/web/src/shared/hooks/useSidewaysScroll.ts`, lines 23 to 43 (`lastScrollLeft` and the "did it move" test `strip.scrollLeft === scrollLeftBefore`).
- What I saw: at 1280 x 800 (strip width 611.22 px) and at 1920 x 1080 (612 px) the strip starts at its right end (current month). Move the cursor over the strip and wheel down: the page never scrolls (scrollY stays 0 after 4 wheel-downs, 0.7 s apart) and the strip's `scrollLeft` toggles 1470, 1469, 1470, 1469. At 1024, 1366, 1440 and 768 the same test scrolls the page (120 px per click). With the hook switched off (an init script that skips the `wheel` listener) the page scrolls 120 per click at 1280 x 800, so the hook is the cause.
- Why (my diagnosis, not checked in the code by changing it): the layout width is fractional, so the real maximum scroll position is fractional (about 1469.x) while `scrollWidth - clientWidth` is the rounded integer 1470. The hook sets `scrollLeft = 1470`, the browser clamps it, the value read back differs from the value before, so the hook thinks the strip moved, calls `preventDefault()` and the page does not get the wheel. At the left end the limit is exactly 0, so it works there.
- Expected: at the right end a downward wheel scrolls the page (what the fix claims).
- Steps: start the stack, sign in, open `/budget` at 1280 x 800 in a browser with a mouse, put the cursor over the month pills and turn the wheel down with the page not scrolled. The strip jiggles by one pixel and the page stays.
- Evidence: scripts `t2c.py` (prints `sl 1470`, then `sl 1469` with `defaultPrevented` false, then `sl 1470` with `defaultPrevented` true), `t2d.py nohook` and `t2d.py hook`, and `t2h.py` for the width table, all in the scratchpad.
- Not verified: whether a real physical wheel with Chrome's smooth wheel scrolling behaves the same (the test sends discrete wheel events).

**Bug 8 (low, keyboard and screen reader): after you cancel the New category sheet (Escape or a tap on the backdrop), the focus is on the page behind, not inside the grid sheet that is still open.**
- Where: Budget "+" or Home "+" -> "Personal spending" -> "+ New" -> Escape. Related file: `src/web/src/features/budget/categories/components/NewCategorySheet.tsx` (`returnsFocusOnClose`).
- What I saw: the grid sheet stays open, `document.activeElement` is the Budget "+" button (Budget start) or `BODY` (Home start), outside the open dialog; Tab then goes to the sidebar links "Home", "Groups", "Budget" behind the sheet (focus is not trapped); Escape still closes the grid. Tapping a tile afterwards focuses the amount as it should.
- Expected: focus returns to the "+ New" tile (or stays inside the grid sheet).
- Not verified: whether this also happened before the fix (the first pass only looked at the focus after Save). Screenshot `shots13b\t1-budget-cancel-new.png`; script `t1b.py`.

**Bug 9 (low, keyboard): Tab onto a month pill that is only slightly visible does not scroll it into view.**
- Where: the month strip at 360 x 740. Focus the first pill, press Tab repeatedly.
- What I saw: every third pill ("Feb 2025", "May 2025", "Aug 2025" and so on) gets the focus with only 8 px of it inside the strip (right gap -82 px), so it is almost invisible (and the new fade lies over it); only the next Tab scrolls the strip. This is Chrome's "scroll only if completely out of view" rule for `focus()`, so it is probably not caused by the fix, but it is still true after it.
- Expected: the focused pill is fully visible (for example `scroll-padding`, or `scrollIntoView({ inline: 'nearest' })` on focus).
- Not verified: other browsers.

## Observations (not bugs)
1. Typing an amount longer than 7 characters switches the size 48 -> 36 px, and over 10 characters 30 px. The amount row shrinks from 72 to 48 to 44 px high, so the bottom-anchored sheet and the fields below it move by that much; on the group expense page the whole form jumps up 28 px. It is a visible step, not a smooth change.
2. The huge tile amounts end up very small: 7.4 px (Macedonian, 360 px) and 7.9 px (English, 360 px) are hard to read on a phone. It fits and never splits, which is what was asked; a readable size would need a different layout.
3. At short window heights the Colour section of New and Edit category is below the fold of the inner scroll area with no hint that it scrolls (no fade, and touch devices show no scrollbar): at 440 px high the name, emoji and Save are visible and "Colour" needs a scroll.
4. MKD and EUR amounts of 12 or more digits overflow the amount box (30 px size, `scrollWidth` larger than `clientWidth`), but Save is disabled for them anyway (the earlier Observation 1 about no message still holds).

## NOT VERIFIED (could not check)
- A real phone, real touch scrolling with momentum, iOS Safari, Android Chrome, Firefox, a screen reader.
- `dvh` with the on-screen keyboard open (only emulated with short windows 740, 500, 440, 400 px).
- A real mouse wheel with smooth scrolling (Bug 7 uses discrete wheel events).
- Whether Bug 8 and Bug 9 already existed before the fixes.
- List rows fading in and out (still not caught by sampling).
- Different time zones, several hundred entries in a month, Macedonian text approved by Filip.
- The automated suites (`dotnet test`, `npm test`, `npm run lint`, `npm run build`): not run in this re-check.
- Other `KvitSheet` screens at short window heights (only 360 x 740 was checked for them).

## Summary
| Item | Result |
|---|---|
| Bug 1 (amount focus after + New), Budget and Home start | PASS, VERIFIED by live run |
| Bug 2 (scrollbars, fade, height, wheel, swipe, pills) | PASS for the look; FAIL for "wheel down at the right end scrolls the page" at 1280 x 800 and 1920 x 1080, new Bug 7 |
| Bugs 3 and 6 (sheet height, inner scroll, 24 categories, scroll isolation, focus rings, animation) | PASS, VERIFIED by live run |
| Other sheets regression, 360 x 740, light and dark | PASS, VERIFIED by live run |
| Bug 5 (long amounts: tiles, edit sheet, typing, group field) | PASS, VERIFIED by live run (7.4 to 9 px text at 360 px, see Observation 2) |
| Happy path and double click | PASS, VERIFIED by live run |
| New bugs | 7 (medium), 8 (low), 9 (low) |

---

# Re-check 2 (Bugs 7 and 8), 2026-10-20

Run by the tester against the working tree of `feat/13-personal-spending` (the fixes in `useSidewaysScroll.ts` and `KvitSheet.tsx` were uncommitted). Nothing in the app was changed, nothing was committed or staged; only this section was added. Docker Desktop was open; `.\scripts\start-local.ps1 -NoBrowser` started everything and `-Stop` stopped it again ("Ports 5018 and 5173 are free").

Labels: **VERIFIED by live run** (real headless Chromium, measured in the page, screenshots looked at), **NOT VERIFIED**. The automated suites (`dotnet test`, `npm test`, `npm run lint`, `npm run build`) were **not run**.

How: Playwright for Python, headless Chromium, classic scrollbars drawn for the strip checks (`--hide-scrollbars` off). User A's saved session was reused (his Budget has about 40 entries, so the page is 2200 to 2450 px high and scrolls at every width tested). Scripts and screenshots (outside the repository): `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\32e619e6-68b1-4a88-afd6-ffb8e56303eb\scratchpad\` (`w7.py`, `w7b.py`, `w7c.py`, `w7e.py`, `f8.py`, `f9.py`, `f10.py`, `f11.py`, helpers `h3.py`, `lib8.py`); screenshots in `...\scratchpad\shots13c\`.

## Bug 7: month strip and the mouse wheel -- PASS

Wheel tested with the cursor over the strip, 120 px per click, 0.5 s between clicks. "Right end" = the strip as it opens (current month, `scrollLeft` = max).

| Check | 1280x800 | 1920x1080 | 1024x768 | 1366x768 | 1440x900 | 768x900 | Label |
|---|---|---|---|---|---|---|---|
| (a) Strip at right end, wheel down x4: page scrolls | scrollY 0 -> 360 PASS | 0 -> 360 PASS | 0 -> 480 PASS | 0 -> 480 PASS | 0 -> 480 PASS | 0 -> 480 PASS | VERIFIED by live run |
| (a2) Same with big wheel steps (500 x2) | 1000 PASS | 1000 PASS | 1000 PASS | 1000 PASS | 1000 PASS | 1000 PASS | VERIFIED by live run |
| (b) Middle of strip: wheel down then up moves the strip, page stays | scrollLeft 700 -> 800 -> 700, scrollY 0 PASS | same PASS | same PASS | same PASS | same PASS | same PASS | VERIFIED by live run |
| (c) Left end, page scrolled to 60, wheel up scrolls the page | 60 -> 0 PASS | 60 -> 0 PASS | 60 -> 0 PASS | 60 -> 0 PASS | 60 -> 0 PASS | 60 -> 0 PASS | VERIFIED by live run |
| (d) Cursor off the strip, wheel scrolls the page | PASS | PASS | PASS | PASS | PASS | PASS | VERIFIED by live run |
| (e) Fades: left end only the right fade (`data-more-after`), middle both, right end only the left fade (`data-more-before`) | PASS | PASS | PASS | PASS | PASS | PASS | VERIFIED by live run |
| (e) No grey scrollbar under the strip (`offsetHeight` = `clientHeight`, `scrollbar-width: none`) | 56 = 56 PASS | 59 = 59 PASS | 54 = 54 PASS | 57 = 57 PASS | 57 = 57 PASS | 52 = 52 PASS | VERIFIED by live run |
| (e) Touch drag at 360x740 (real touch events through the DevTools protocol, finger 150 px right, then 150 px left) | scrollLeft 1639 -> 1504 -> 1639 PASS; a finger drag up on the strip scrolls the page (scrollY 135), not the strip | | | | | | VERIFIED by live run |

Screenshots looked at: `shots13c\f13-strip-1280-end.png`, `f13-strip-1280-mid.png` (left fade only / both fades, no scrollbar, pill fully visible).

Remarks (not bugs of the fix):
- Strip height is not exactly 56 everywhere: 52 at 360 and 768, 54 at 1024, 56 at 1280, 57 at 1366 and 1440, 59 at 1920. This is the same as before (first pass: 56 at 1280, 52 at 360); the point of the check was the bar (0 px).
- At 1280x800 and 1920x1080 (fractional strip width) the FIRST wheel click with the cursor over the strip at the right end does not scroll the page (scrollY 0, `scrollLeft` 1470 -> 1469, `defaultPrevented` false, so the hook did not cancel it); clicks 2, 3, 4 scroll 120 px each. I switched the strip wheel listener off with an init script (`w7c.py nohook`) and got the identical sequence (0, 0, 120, 240), so this is Chrome's own behaviour with the headless wheel events, not the hook. Before the fix the page never scrolled at all at these widths (Bug 7). Not verified: whether a real mouse wheel also loses the first tick.
- The DevTools `Input.synthesizeScrollGesture` touch gesture did not move the strip at all in this run (it did in the first re-check); manual `Input.dispatchTouchEvent` start/move/end drags work, so I used those. I treat the synthesized gesture as a tool quirk, not an app problem (not investigated further).

## Bug 8: focus after cancelling "New category" -- PASS

`document.activeElement` read 0.7 s after the cancel, then 8 to 12 Tab presses, each checked to still be inside the same open dialog.

| Check | Result | Label |
|---|---|---|
| Budget "+" -> grid -> "+ New" -> Escape, 360x740 and 1280x800 | one dialog left (the grid), active element = the "+ New" tile (BUTTON "+ New", inside the grid dialog "What did you spend on?"); 12 Tabs all stay inside the grid dialog. PASS | VERIFIED by live run |
| Same, cancelled by tapping the backdrop of the New sheet | identical: focus on "+ New", Tabs stay inside the grid. PASS | VERIFIED by live run |
| Same after typing a name first, then Escape | identical. PASS | VERIFIED by live run |
| Home -> "Personal spending" (360: "+" menu; 1280: the Personal spending item on the page) -> "+ New" -> Escape / backdrop / Escape after typing | identical on all 6 combinations at both sizes. PASS | VERIFIED by live run |
| After SAVE of a new category the amount field has the focus, typing "7" shows 7 (Bug 1 not regressed) | Budget and Home, 360x740 and 1280x800: active element = amount INPUT (`inputmode=decimal`), value "7". PASS | VERIFIED by live run |
| Cancel "Edit category" opened from "Edit my categories" (Escape and backdrop; Budget and Home; 360 and 1280) | 2 dialogs stay open, focus on the "Edit" button inside the "My categories" sheet, 8 Tabs stay inside it. PASS (8 of 8 combinations) | VERIFIED by live run |
| Escape returns focus to the opener, with a mouse click and with focus + Enter: group add page chips "Date" (the date sheet), "Paid by", "Split", "Category" | focus is back on the very same chip each time (8 of 8). PASS | VERIFIED by live run |
| Personal add sheet, amount stage: the date sheet opened from the "Today" chip | after Escape the add sheet stays open and focus is on the same "Today" chip inside it (mouse and keyboard). PASS | VERIFIED by live run |
| Balances: "Record another payment" and "Mark paid" sheets | focus back on the same button (mouse and keyboard). PASS | VERIFIED by live run |
| Members: member options sheet ("..." button) | focus back on the same "..." button (mouse and keyboard). PASS | VERIFIED by live run |
| Guest settings: "Log out" sheet | focus back on the "Log out" button (mouse and keyboard). PASS | VERIFIED by live run |
| Escape from the grid opened with the Budget "+" | focus back on the "Add spending" button. PASS | VERIFIED by live run |

Screenshots: `shots13c\f8-budget-escape-360.png` (taken after the Tab presses, so the focus ring is on a later tile, not on "+ New"; the measured value above is from right after the cancel), `f9-after-save-budget-360.png`.

Remark: Escape from the grid opened through the Home "+" menu (360 px) leaves the focus on the page (`BODY`), because the menu item that opened it is gone once the menu closes. Not caused by this fix as far as I can tell (nothing to return to); not verified whether it was the same before the fix. Low.

## Quick regression -- PASS

| Check | Result | Label |
|---|---|---|
| Add entry from Budget "+": category tap (Transport), type 21, Save | one `POST /api/personal/entries` (200), sheet closed, new row "Transport, Today, EUR 21.00" in the list. PASS | VERIFIED by live run |
| Add entry from Home -> Personal spending: same | one `POST` (200), sheet closed, second row "Transport, Today, EUR 21.00" on Budget. PASS | VERIFIED by live run |
| 360x740: sheet shows its title and Save | grid: title at y 50 to 72, sheet top 29; amount stage: title 425 to 473, Save 659 to 715 inside the 740 px window. PASS (`shots13c\f11-amount-360.png`) | VERIFIED by live run |
| Console | no errors or warnings in any run (the log lists were empty) | VERIFIED by live run |

## New bugs

None found in this re-check (no Bug 10 or higher).

## Throwaway data left in the local database (user A, `p13a1792417478@example.com`)
- Four more custom categories made by the save tests ("Fzb...", "Fzh..." at 360 and at 1280), each with a 7 entry, plus two "Transport 21 EUR" entries from today. Nothing deleted.

## NOT VERIFIED (could not check)
- A real mouse wheel with Chrome smooth wheel scrolling (the tests send discrete wheel events); in particular whether the first wheel tick over the strip at the right end is also lost on a real mouse at 1280 and 1920 wide (see the remark above).
- A real phone (touch momentum, on-screen keyboard), iOS Safari, Android Chrome, Firefox, screen readers (focus was read from `document.activeElement` only).
- Settings sheets for a signed-in non-guest user were only checked through the guest "Log out" sheet (the same button and sheet).
- The automated suites (`dotnet test`, `npm test`, `npm run lint`, `npm run build`).
- Whether the Home-menu focus remark existed before the fixes.

## Summary
| Item | Result |
|---|---|
| Bug 7: wheel at the right end scrolls the page at 1280, 1920, 1024, 1366, 1440, 768; middle moves the strip; left end up scrolls page; off the strip scrolls; fades, no scrollbar, touch drag | PASS, VERIFIED by live run |
| Bug 8: focus on "+ New" after Escape / backdrop, Tab trapped in the grid, Budget and Home; focus after Save; Edit category cancel; 8 other sheets Escape returns to opener | PASS, VERIFIED by live run |
| Quick regression (add from Budget and Home, 360x740 title and Save) | PASS, VERIFIED by live run |
| New bugs | none |
