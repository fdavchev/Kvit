# Phase 16 Step 7: real-browser pass (2026-10-30)

Branch `feat/16-group-insights`. Run by the tester. No production code, no tests and no docs other than this report were changed. Nothing was committed or staged. Screenshots and scripts are in the scratchpad, not in the repository (see "Where the files are").

Labels: **VERIFIED by live run** (done in a real headless browser against the local app, result read or screenshot looked at), **NOT VERIFIED**. The automated suites (`dotnet build`, `dotnet test`, `npm test`, `npm run lint`, `npm run build`) were **not run**, so nothing here is VERIFIED by automated test.

## What was run and how
- Docker Desktop open. `.\scripts\start-local.ps1 -NoBrowser` started the database, API (`http://localhost:5018`) and website (`http://localhost:5173`) with no migration warning. `__EFMigrationsHistory` ends with `20261027092926_DuplicateAnswers` and `20261027124121_GroupPlan`, so both Phase 16 migrations were already applied (nothing was applied by me). VERIFIED by live run.
- At the end `.\scripts\start-local.ps1 -Stop` printed "Closed the 'Kvit API' window", "Closed the 'Kvit website' window", "Stopped the database container kvit-postgres (its data is kept)" and "Ports 5018 and 5173 are free". VERIFIED by live run. `git status` shows only this report as a new file.
- Playwright for Python, headless Chromium only, fresh in-memory sessions, time zone `Europe/Skopje`, machine date 2026-10-30. Language switched with `PUT /api/me/language`, browser locale `en-US` / `mk-MK`; theme through the browser colour scheme; reduced motion through the browser emulation.
- Throwaway users (password `Passw0rdA`, `@example.com` addresses): A "Filip Alfa" (owner), B "Bojan Beta" (member), C "Cvetan Gamma" (owner of the big-amount group), guest "Gosho Guest" (joined through the invite link), plain name "Vesna Plain". Sign-up of A, B, C and the guest join were real. Groups, expenses, personal entries, plans, limits, the settlement and the closing were made through the API of the running app. The plan sheet, the duplicate card, the edit sheet and the limit sheet were driven through the real UI. Existing user data was not touched.
- Groups: "Ohrid trip" EUR (A, B, guest, plain name; plan 600 EUR, spent 543.78 EUR = 90 %, includes 3,000 MKD converted), "Skopje weekend" MKD (plan 20,000 ден., an EUR expense converted, over), a 58-letter group name (MKD, plan 90,000 ден., 88 %), "No plan group" EUR (expenses, no plan), "Empty group" EUR (nothing, no plan), "Closing group" (plan 100 EUR, Closing, A owner), "Finished group" (plan 300 EUR, Finished), "On track group" MKD (plan 50,000 ден., 20 %), and C's big-amount group (58-letter name, plan 999 999 999 900 minor, expenses 999 999 999 900 and 999 999 999 999 minor, one expense with a 60-letter one-word title). B and A have one personal entry pair each that matches a group share (two pairs for B in "Ohrid trip", two for A in "Skopje weekend"); A has a Food limit at 94 % and a Transport limit over.
- Big-amount note: the server refuses an expense above 999 999 999 999 minor units (`EXPENSE_AMOUNT_TOO_LARGE`), so the largest MKD expense is 999 999 999 900 minor, which the app shows as 9,999,999,999 ден. The number `999 999 999 900 ден.` from the task cannot be created (it would be 99 999 999 990 000 minor).
- Scripts, seed data and screenshots: see "Where the files are". An earlier attempt left old files in that folder; they were moved to `old\` and not used.
- Matrix sizes actually run: (a) 80 measurements (EN/MK x light/dark x 320/360/768/1280/1920 x 4 tabs); (b) 120 Totals views (6 groups x the same 20 combinations) with a sideways-scroll and off-screen-element measurement plus screenshots of 320, 360, 1280; (c) 64 plan-card state readings at 360 (A, B, C x EN/MK x light/dark) plus the sheet flows; (d) 20 Budget readings as B (EN/MK x light/dark x 5 widths) plus the merge flows; (e) 20 Limits readings as A plus the limit sheet in EN and MK; reduced motion once (360, EN); sheets at 320x440, 360x500, 1280x600.

## BUGS
I did not fix anything. Numbered by how much they hurt.

**Bug 1 (high): the four-tab bar of a group does not fit at 320 px in any language, and at 360 px in Macedonian it only fits with no gap and the sliding pill does not sit under the labels. In dark mode letters become invisible.**
- Steps: open any group (`/groups/<id>`, `/balances`, `/activity`, `/totals`) at 320 px (English or Macedonian), or at 360 px in Macedonian. Switch between the tabs.
- Expected: four readable labels, each fully inside the highlighted pill, with some space between neighbours.
- Actual (measured with `getBoundingClientRect`; the bar's inner width is 264 px at 320 and 304 px at 360):
  - 320 px, Macedonian: label widths 76.5 + 49.7 + 97.1 + 60.8 = 284 px, 20 px more than the 264 px available. The words touch ("ТрошоциСалдаАктивностиВкупно"), the last label "Вкупно" ends at x=312, 12 px outside the bar (which ends at x=300), and in the dark theme its last letters are cut ("Вкуп"). The pill is a fixed quarter of the bar (66 px) at x=28, 94, 160, 226, while the labels start at x=28, 104.5, 154.2, 251.3: under "Активности" (97 px wide) the pill covers 66 px, under "Вкупно" it misses its left 25 px.
  - 320 px, English: labels 72.6 / 69.2 / 61.4 / 60.8 px (sum 264), no gap between "Expenses" and "Balances", and the pill (66 px) is narrower than "Expenses" (72.6 px). In the dark theme the last letter "s" of "Expenses", "Balances" and "Activity" is dark text on the dark bar, so it vanishes when that tab is active.
  - 360 px, Macedonian: labels 76.5 + 65.2 + 97.1 + 65.2 = 304 px, exactly the inner width: zero space between "Активности" and "Вкупно". The pill is 76 px and "Активности" is 97 px, so part of the active word hangs outside the pill; in dark mode "А" and "и" are dark on dark ("ктивност" is what shows).
  - 360 px English: fine (76 px per tab, longest label 72.6 px, 3 px spare). 768, 1280, 1920 px: fine in both languages.
- Cause (read, not changed): `src/web/src/features/groups/group/components/GroupTabs.tsx`: the links are `flex-1` with no padding and no `min-w-0`, so each link grows to its own text width, while `KvitTabPill` (`src/web/src/shared/components/KvitTabPill.tsx`) is always `(100% - 0.5rem) / tabCount` wide and moved by `activeIndex * 100%`. That only lines up when all four links are the same width.
- Proposed fix: for narrow windows give the four cells equal width (`grid grid-cols-4`, each cell `min-w-0 px-0.5`, centred text), use a smaller font only in this bar below about 400 px (about 0.8125rem), and choose a shorter Macedonian label for Activity if it still does not fit at 320 px (it is the longest word, 97 px at 17 px). Alternatively make the pill take the real rectangle of the active link instead of a quarter. Whichever is chosen, re-measure at 320 and 360 px in both languages and both themes. This is the risk Filip's pick 4 noted, so it should go back to him: four tabs do not fit as built.
- Screenshots: `shots\a-bar-320-mk-light.png`, `shots\a-bar-320-mk-dark.png`, `shots\a-bar-320-en-dark.png`, `shots\a-bar-360-mk-light.png`, `shots\a-bar-360-mk-dark.png` (each shows the four tabs active in turn, magnified three times); whole page `shots\a-tabs-totals-320-mk-dark.png`.

**Bug 2 (medium, needs a decision from Filip): "Not the same" on a merged entry makes the card ask the same question again at once.**
- Steps: as B, tap "Same, merge" on the pair "Dinner (29.00 EUR) / Dinner Ohrid". Tap the greyed entry, tap "Not the same".
- Expected (by the words): the person said these are not the same, so the question should not come back.
- Actual: one `DELETE /api/personal/duplicate-answers` removes the stored answer, the toast says "Counted again in your budget", and the pair immediately shows again at the top of the Budget tab as "Is this the same? ... Same, merge / No, it's new". To stop being asked the person must tap "No, it's new" as well, so "not the same" takes two decisions. It is what DECISIONS Step 4 describes ("one DELETE"), so it is by design, but it reads as a contradiction on screen.
- Proposed fix (product choice): make "Not the same" store the answer `New` (`PUT` with `answer: "New"`, which the server already supports) instead of deleting it, so the entry counts again and the card does not return; or keep the delete and change the toast to "Counted again. Asking again if it is the same.".
- Screenshots: `shots\d-edit-merged-360-en-light.png`, `shots\d-after-notsame-360-en-light.png`.

**Bug 3 (low): in the Totals tab a very large amount squeezes the category name to one to four letters per line at 320 and 360 px.**
- Steps: group with an expense of 9,999,999,999 ден. in "Shopping" (C's big-amount group, open Totals at 320 or 360 px, English or Macedonian).
- Expected: the name stays readable (wraps by word, or the amount drops under the name).
- Actual: the amount is `whitespace-nowrap` and takes almost the whole row, so the name column shrinks: "Shoppi / ng" at 360 px, and in Macedonian «Ку / пу / ва / ње» at 320 px; "Смештај" becomes «См / еш / тај». No sideways scroll, nothing outside the card (0 of 120 views overflowed), only the squeezed name. 9,999,999,999 is the largest amount the app accepts, so this needs an unusual group.
- Cause (read, not changed): `src/web/src/features/groups/totals/components/GroupTotals.tsx`, the row `li` (`flex items-center gap-3`): name `min-w-0 flex-1 break-words`, amount `whitespace-nowrap`.
- Proposed fix: let the row wrap (`flex-wrap`, name `basis-32`) so the amount moves under the name when it does not fit, or give the name a minimum width.
- Screenshots: `shots\b-G9-360-en-light.png`, `shots\b-G9-320-mk-dark.png`.

**Bug 4 (low, wording): the plan amount message says "MKD" in Macedonian and in English when EUR is chosen.**
- Steps: open the plan sheet, type `abc` (EN or MK, any chip).
- Actual: EN "Type an amount above 0 (whole denars for MKD)." (also shown with the EUR chip, where the denar rule does not apply); MK «Внеси износ поголем од 0 (цели денари за MKD).» has Latin letters "MKD" while every other Macedonian text says «ден.»; the limit sheet text («Внеси износ поголем од 0, во цели денари.») has no such issue. Drafts.
- Proposed fix: say the same thing as the limit sheet for MKD and say only "Type an amount above 0." for EUR, or use the currency name of the chosen chip (`currencyLabel`).
- Screenshot: `shots\c-sheet-invalid-360-mk-dark.png`.

## OBSERVATIONS (not bugs, for Filip to decide or know)
1. **The currency is shown twice in the plan sheet** (answer to the question in the task): the amount field has the static text "EUR" (or «ден.») to its right, and under it the two chips "MKD | EUR" (MK «ден. | EUR»). It looks redundant but not broken; both change together (checked). `AmountField` already has a built-in currency toggle button (`onCurrencyChange`) that the Add-entry sheet uses; using it instead of the chips would show the currency once. Screenshot `shots\c-sheet-none-360-en-light.png`.
2. **Offline, "Save" in the plan sheet does nothing visible and saves later.** With the network off, tapping Save gives no spinner, no error and no toast (waited 14 s, the sheet stays); the PUT is sent when the network returns (the plan changed to 60,000 MKD after I switched the network back on, with the sheet already gone). This is the data library's default of pausing writes while offline; it is not specific to Phase 16, but a plan or limit can change later than the person expects.
3. **After a refused Save (sheet stays open) the keyboard focus is on the page body**, and the next Tab goes to the first control ("Remove plan"). The limit sheet from Phase 15 behaves the same (checked with a faked `BUDGET_LIMIT_AMOUNT_INVALID`), so it is an old pattern of the shared sheet, not new.
4. **At exactly 100 % the plan says "Near the plan · 0 MKD left"** (known follow-up in DECISIONS). Checked: 12,501 plan = "On track", 12,500 = near (80 % exactly), 10,001 = near "1 MKD left", 10,000 = near "0 MKD left", 9,000 = "1,000 MKD over".
5. **The possible-duplicate card only shows in the month that holds the entry.** As A, October shows the card, September shows none. A pair that straddles a month end (entry 30 Sep, share 1 Oct) is therefore asked only when the person happens to open the entry's month. This follows DECISIONS ("a pair shows in the entry's month"); noting it because the card is described as "the next time Budget opens".
6. **Desktop (1280 and 1920 px):** the Totals tab uses the same full-width cards as the Balances tab (986 px at 1280, 1044 px at 1920), so each category name sits far from its amount on a wide window. Not broken and consistent with Balances. The plan card there is one row ("€543.78 of €600.00" left, "€150.00 each · 4 people" right), and the Budget top card is the 611 px content column with the two buttons side by side (47 to 50 px high). Nothing cramped or overflowing was found.
7. **Merged row contrast (measured from real pixels):** body text 7.05:1 light / 9.31:1 dark (a normal row is 12.72:1 / 13.91:1); the grey line "Merged with Dinner Ohrid · Ohrid trip" 5.11:1 light / 7.83:1 dark at 13 px, so it passes 4.5:1. The row stays visibly grey (opacity 0.8) with the label.
8. **Plan bar colours (measured):** fill against its card/track: green 5.91:1 / 4.93:1 light and 8.78:1 / 9.93:1 dark; amber 4.87:1 / 4.07:1 light and 8.16:1 / 9.22:1 dark; red 5.84:1 / 4.88:1 light and 7.62:1 / 8.61:1 dark. The coloured words (near, over) have the same ratios as their fills; all at least 4.5:1 for text and 3:1 for the bar. "On track" is grey text (8.2:1 / 11.6:1).
9. **Closing group, as designed:** the owner can open the sheet and save a plan (it saved, the group stayed Closing); the member sees the card as plain content. **Finished group:** plain card for owner and member, no control. A guest sees the Totals tab and the plan card as plain content (no button, no "Set a plan"); a guest `PUT .../plan` answers 403 `GROUP_NOT_OWNER`.
10. **Test mistake I made and fixed:** while comparing the limit sheet I changed A's entry "Skopje dinner" from 3,100 to 22,000 ден. by mistake, saw the Food limit jump, and set it back to 3,100 through the same edit sheet (verified: 310000 minor, both duplicate pairs back). Nothing else was affected.
11. **Macedonian wording** was only checked for presence and fit, not approved: «Дали е ова исто?», «Исто, спој», «Не, ново е», «Споено со ...», «Вкупно», «План за трошоците», «Постави план», «Близу до планот · уште €56,22», «Близу до лимитот», «Во рамки» (On track). All drafts.
12. The only console message in all runs was one `net::ERR_CONNECTION_TIMED_OUT` for a resource on one dark 360 px Totals load of the long-name group; 6 reloads of the same view gave 0 errors afterwards (dev server hiccup). The other console messages were the browser's own 400/403/404/500 lines for refusals I faked on purpose and two `No translation key is mapped for API error code "X_UNKNOWN"` lines for my own faked unknown code.

## Results table
| # | Check | Result | Label |
|---|---|---|---|
| a | **Four tabs** at 320 / 360 / 768 / 1280 / 1920 px, EN and MK, light and dark, each of the four tabs active (80 measurements, screenshots looked at) | FAIL at 320 (EN, MK) and 360 (MK); PASS at 360 EN and 768 and above: **Bug 1**. Tab height 44 px at 320 to 768, 47 to 50 px at 1280 and 1920 | VERIFIED by live run |
| b1 | **Totals tab layout**: no sideways scroll and nothing outside the window in 120 views (A: Ohrid trip, Skopje weekend, long-name group, No plan group, Empty group; C: big-amount group; 5 widths, EN/MK, light/dark) | PASS (one-off network timeout, not repeatable, Observation 12) | VERIFIED by live run |
| b2 | One currency (No plan group: EUR 105.00) and two currencies (Ohrid trip: MKD 3,000 then EUR 495.00, never added); MKD first, then EUR; categories in built-in order; "No category" last with the dashed tag tile; totals checked by hand: EUR 120 + 300 + 60 + 15 = 495 | PASS | VERIFIED by live run |
| b3 | Loading skeleton (request held open: grey blocks, status "Loading..." / «Се вчитува...», EN/MK, light/dark), error state (faked 500, after the retries: "Something went wrong. Please try again." + "Try again"), empty state (📊 "No spending yet" + hint, with the "Set a plan" row above for the owner) | PASS | VERIFIED by live run |
| b4 | Big amounts and long names: 58-letter group name wraps over three lines and the plan card wraps "624,972,999,998 MKD of 9,999,999,999 MKD" to two lines; no overflow; a one-word 60-letter title does not appear in Totals (only categories do) | PASS except the squeezed category name: **Bug 3** | VERIFIED by live run |
| b5 | A new expense by another member shows after a reload (G5: 12.34 EUR Food); `/spending` is asked only on the Totals tab, once per visit | PASS | VERIFIED by live run |
| c1 | **Plan card states** (EN/MK, light/dark): owner without plan = "🎯 Set a plan ›" row, 48 px; member and guest without plan = nothing; On track (grey words, green bar 50 %/33 %/20 %); Near the plan with words "Near the plan · €56.22 left" (amber bar, 90 %); Over with red words "4,075 MKD over" and a full red bar; converted note "includes 3,000 MKD converted to EUR" / "includes €50.00 converted to MKD" / MK «вклучува ... претворени во ден.»; "each" line "€150.00 each · 4 people" / «€150,00 по човек · 4 лица» | PASS (contrast: Observations 7 and 8) | VERIFIED by live run |
| c2 | Owner vs member: owner's card is a button (166 px high at 360 in Ohrid trip), member's card is plain; Closing group: owner has the button, member plain; Finished group: plain for both | PASS | VERIFIED by live run |
| c3 | **Plan sheet**: opens with the amount focused; chips default to the group currency (EUR for an EUR group, MKD for an MKD group); prefilled with the plan's amount and currency when a plan exists; Save off when empty, unchanged or invalid; "Remove plan" only when a plan exists | PASS | VERIFIED by live run |
| c4 | Invalid amounts (message shown only when text is typed and not valid; Save off): `abc`, `0`, `1e3`, `-5`, `0000`, 14 digits (message "Type an amount above 0 (whole denars for MKD)."); `12.5` is accepted with EUR and refused with MKD; the red field shows `aria-invalid` and is linked with `aria-describedby` | PASS (wording: **Bug 4**) | VERIFIED by live run |
| c5 | Set, change, remove: set = `PUT {"amountMinor":60000,"currency":"EUR"}`, toast "Saved"; change to 1,250 MKD = one PUT, card shows "6,457 MKD of 1,250 MKD, 5,207 MKD over, includes €105.00 converted to MKD"; remove = one `DELETE`, red toast "Plan removed", card replaced by the "Set a plan" row | PASS | VERIFIED by live run |
| c6 | **Focus after the sheet** (recorded): after Save the card button; after Save on a first plan the new card button; after Remove the "Set a plan" row; after Escape the opener (card or row); after a refused Save by `GROUP_NOT_OWNER` / `GROUP_FINISHED` / `GROUP_CLOSING` (sheet closes) the card button; after a refusal that keeps the sheet open the page body (Observation 3) | PASS (Observation 3) | VERIFIED by live run |
| c7 | Refusals (faked): `GROUP_PLAN_AMOUNT_INVALID`, `GROUP_CURRENCY_INVALID` ("Pick MKD or EUR."), `GROUP_NOT_FOUND`, a 500: message above Save, amount kept, sheet stays; `GROUP_NOT_OWNER`, `GROUP_FINISHED`, `GROUP_CLOSING`: toast and the sheet closes on Save; on Remove they show a toast and keep the sheet (as decided); offline: Observation 2 | PASS | VERIFIED by live run |
| c8 | Currency shown twice in the sheet | noted: Observation 1 | VERIFIED by live run |
| c9 | Sheets at small window heights (320x440, 360x500, 1280x600; EN/MK, dark): sheet top and title inside the window, Save inside the window, no sideways scroll | PASS | VERIFIED by live run |
| d1 | **Possible duplicates card** on Budget at the top, EN and MK: "Is this the same?" / «Дали е ова исто?», one line of text naming both sides with amounts and dates, "Same, merge" / «Исто, спој», "No, it's new" / «Не, ново е»; buttons 44 px at 320 to 768 (47 to 50 px at 1280 and 1920) and as wide as the card; no sideways scroll in 20 of 20 views; looked at 320 MK light, 360 EN light and dark, 1280 | PASS | VERIFIED by live run |
| d2 | Same, merge: one `PUT {"answer":"Same"}`, the next pair shows (focus on its "Same, merge"); "No, it's new": one `PUT {"answer":"New"}`, the card disappears and the focus goes to the "Budget" heading | PASS | VERIFIED by live run |
| d3 | **Totals effect**: B's EUR spending 394.25 to 365.25 (the 29.00 merged entry leaves the totals); the group share (30.00) keeps counting (from-groups sum 276.25 before and after); the "No" answer changes nothing; "Not the same" puts 29.00 back (EUR 118.00 in the API) | PASS | VERIFIED by live run |
| d4 | Merged row greyed (opacity 0.8) with "Merged with Dinner Ohrid · Ohrid trip" / «Споено со ...», readable (Observation 7), light/dark, EN/MK, 320, 360, 1280 | PASS | VERIFIED by live run |
| d5 | Edit sheet of the merged entry: Save, "Not the same" (52 px), red Delete; "Not the same" = one `DELETE`, toast "Counted again in your budget", focus back on the row | PASS (the question returns at once: **Bug 2**) | VERIFIED by live run |
| d6 | A merged entry whose group expense is deleted counts again (EUR spending 89.00 to 118.00, the entry no longer merged, the group share drops by 30.00) and is merged again when the expense is restored | PASS | VERIFIED by live run |
| e1 | **Limits "Near the limit"**: Food 19,826 / 21,000 MKD (94 %) is a near row: amber bar, words "Near the limit" / «Близу до лимитот», `aria-describedby` set; Transport 4,710 / 4,000 is over with "710 MKD over" and no near words; 20 of 20 views (EN/MK, light/dark, 5 widths) without sideways scroll | PASS | VERIFIED by live run |
| e2 | **Limit sheet invalid message**: `abc`, `0`, `12.5`, `12,5`, `1e3`, `-5`, `0000`, 14 digits show "Type an amount above 0, in whole denars." / «Внеси износ поголем од 0, во цели денари.» with Save off; empty shows no message; the unchanged value (21000) shows none and Save off; a new value (15000) turns Save on | PASS | VERIFIED by live run |
| f | **Desktop 1280 and 1920 px**: Totals tab and plan card placement, Budget top card | PASS, nothing cramped or overflowing (Observation 6) | VERIFIED by live run |
| g1 | **Requests**: `/api/groups/<id>/spending` is requested only on the Totals tab (not on Expenses, Balances, Activity, group settings, Members, Budget, Groups list); coming back to Totals asks again once; the Budget tab sends no spending request | PASS | VERIFIED by live run |
| g2 | **Keyboard**: Tab reaches the tab links, then the plan card or "Set a plan" row (3 px solid outline in all cases); Space and Enter open the sheet; in the sheet Tab goes input, MKD, EUR, (Save when enabled), Remove plan and wraps inside the sheet (checked with a pause between presses); Enter in the amount saves; Shift+Tab walks back; Escape returns to the opener; Space on "Remove plan" removes it. Duplicate card: Tab reaches "Same, merge" then "No, it's new" (3 px ring made of a shadow), Space merges (focus on the next card's "Same, merge"), Enter on "No, it's new" answers (focus on the "Budget" heading) | PASS | VERIFIED by live run |
| g3 | **Reduced motion** (360 px): the duplicate card has `animation: none` (normal: `tab-panel-in` 0.15 s), the tab pill and the plan bar have `transition-property: none`, the plan sheet does not slide (translate `none` in 25 samples; normal motion slides from 100 % to none) | PASS | VERIFIED by live run |
| g4 | **Console and failed requests** in the matrices and flows | PASS except Observation 12 | VERIFIED by live run |

## Tap counts measured
- Plan: first plan = "Set a plan" row, type, Save = **2 taps + 1 typed amount** (the amount is focused with no tap). Change = card, type, Save = **2 taps + 1 field**. Remove = card, "Remove plan" = **2 taps**.
- Duplicates: **1 tap** per pair ("Same, merge" or "No, it's new"); "Not the same" = tap the row, tap "Not the same" = **2 taps**.

## NOT VERIFIED
- A real phone (touch, on-screen keyboard, whether the number keypad opens), iOS Safari, Android Chrome, Firefox, a screen reader (focus read from `document.activeElement`).
- Macedonian wording approved by Filip (all new texts are drafts).
- Contrast of the emoji tiles (unchanged), and the plan card against the page background (only against its own card).
- A different time zone (month edge for the duplicate card at midnight), Neon / Render behaviour, a slow network (only offline was tried), the deploy gap (new website against an old API).
- A group of many members or many categories with real-looking data; a plan with a plain-name or guest count changing (the "each" line was only read for fixed member counts).
- A custom (non-built-in) category never appears in group totals by design, so it was not exercised.
- The widths 768 for the plan sheet and the duplicate card were measured (no overflow, sizes) but their screenshots were not looked at one by one; 1920 px Macedonian and dark screenshots were measured, not all viewed.
- The automated suites: `dotnet build`, `dotnet test`, `npm test`, `npm run lint`, `npm run build`.

## Questions for Filip (product only)
1. Bug 1: the four tabs do not fit at 320 px, and at 360 px in Macedonian they only fit with no gap. Do you want the same four tabs with a smaller font on small phones, a shorter Macedonian word for "Активности", or a different place for "Totals" (for example back to your option A, a card on the Expenses tab)?
2. Bug 2: when you tap "Not the same" on a merged entry, should the app stop asking about that pair (recommended), or ask again at the top as it does now?
3. Observation 1: should the plan sheet show the currency once (a tap on the currency name flips it, like the Add sheet) instead of the name beside the amount plus two chips?
4. Observation 4: at exactly 100 % should the plan say "Plan reached" instead of "Near the plan · 0 left"?

## Where the files are
- Scripts and screenshots: `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\cf6aea75-2ae0-43e6-9fa4-93aa319fb090\scratchpad\p16\` (screenshots in `shots\`; measurement dumps `ma_res.json`, `b2_res.json`, `c1_res.json`, `d1_res.json`; seed ids in `seed.json`). Not in the repository.

## Throwaway data left in the local database
Users A, B, C and the guest, the groups listed at the top (one extra "Skopje weekend" created by a failed first seed run was deleted through the app), their expenses, entries, limits and plans. At the end: "Closing group" plan is 150 EUR (changed by me through the sheet), "On track group" plan back at 50,000 ден.; "No plan group" has no plan; A and B each have two pending duplicate pairs again (their answers were removed through the API to repeat the card tests); A's budget currency is "MKD" (total shown in MKD) and A's and B's "Count my shares" switch is off for the long-name group. Nothing was deleted from other users' data.

## Summary
| Item | Result |
|---|---|
| (a) Four tabs at 320 / 360, EN and MK | FAIL, **Bug 1 (high)**; fine from 768 up and at 360 EN |
| (b) Totals tab | PASS, except squeezed names for huge amounts (**Bug 3, low**) |
| (c) Plan card and sheet | PASS (**Bug 4, low**, Observations 1 to 4) |
| (d) Possible duplicates | PASS (**Bug 2, medium**, product decision) |
| (e) Limits near words and invalid messages | PASS |
| (f) Desktop | PASS |
| (g) Console, requests, keyboard, reduced motion | PASS |
| Bugs | 1 (high), 2 (medium), 3 (low), 4 (low) |

## Re-check after the fixes (2026-10-30)

Same method as above: headless Chromium (Playwright), throwaway users and groups from the first pass, nothing in the repository changed except this section. `__EFMigrationsHistory` still ends with `DuplicateAnswers` and `GroupPlan` (confirmed before the run). The stack was started with `start-local.ps1 -NoBrowser` and closed with `-Stop` ("Ports 5018 and 5173 are free"). The automated suites (`dotnet test`, `npm test`, lint, build) were **not run**. Scripts and screenshots: `...\scratchpad\p16\r2_*.py`, `r2_*.json`, screenshots in `...\scratchpad\p16\shots2\` (not in the repository).

### Result per check
| # | Check | Result | Label |
|---|---|---|---|
| 1a | Bug 1: pill sits over the active link (left and width within 1 px), 4 equal cells | PASS: 192 of 192 measurements (EN/MK x light/dark x 320/360/375/390/400/768 x 4 tabs, each reached both by opening the URL and by tapping through the four tabs): no difference above 1 px, none at all in the rounded numbers (cell = 66 px at 320, 76 at 360, 79.8 at 375, 83.5 at 390, 86 at 400, 98 at 768) | VERIFIED by live run |
| 1b | Bug 1: every label fits its cell | PASS everywhere except Macedonian «Активности» at 320 px (2.5 px too wide) and at 400 px (5.3 px too wide); see table below | VERIFIED by live run |
| 1c | Bug 1: dark mode, active label readable | PASS: label over pill is 9.03:1 in dark and 14.16:1 in light (computed from the real colours, the same in all 192 states) | VERIFIED by live run |
| 1d | Bug 1: nothing cut off by the bar, no sideways scroll, no console message | PASS: all label boxes lie inside the bar (bar 24..296 at 320), scroll width equals window width in all 192 views, console and failed-request list empty | VERIFIED by live run |
| 2 | Bug 3: Totals with 9,999,999,999 ден. (and EUR 9,999,999,999.99) at 320 and 360, EN and MK, light and dark | PASS: name «Shopping» / «Купување» / «Stay» / «Смештај» is on one line, 184 px wide at 320 and 224 px at 360, the amount sits under the name (right aligned), no sideways scroll (320/320 and 360/360), console empty | VERIFIED by live run |
| 3 | Bug 4: plan sheet message follows the chip | PASS (see below): 4 combinations (EN/MK x light/dark) x 11 steps | VERIFIED by live run |
| 4 | Sanity at 360 px EN/MK light/dark | PASS (see below), with one new low-priority finding (Bug 5) | VERIFIED by live run |

### 1. Label fit per cell (text width from a Range, cell width from the link box, px)
Text widths are the same in every theme. A label "fits" if the text width is not more than the cell width.

| Width | Font | Cell | EN labels (Expenses / Balances / Activity / Totals) | MK labels (Трошоци / Салда / Активности / Вкупно) | Fit |
|---|---|---|---|---|---|
| 320 | 12 px | 66 | 51.3 / 48.8 / 43.3 / 33.1 | 54.0 / 35.1 / **68.5** / 42.9 | EN fits; MK «Активности» **+2.5 px** over (1.2 px each side) |
| 360 | 12 px | 76 | same | same | all fit (MK «Активности» has 3.7 px spare each side) |
| 375 | 12 px | 79.8 | same | same | all fit |
| 390 | 12 px | 83.5 | same | same | all fit |
| 400 | 16 px | 86 | 68.3 / 65.1 / 57.8 / 44.1 | 72.0 / 46.8 / **91.3** / 57.2 | EN fits; MK «Активности» **+5.3 px** over (2.65 px each side) |
| 768 | 16 px | 98 | same as 400 | same as 400 | all fit |

Answer for «Активности» at 320 px (screenshots `shots2\bar4-320-mk-dark.png`, `shots2\bar4-320-mk-light.png`): the word is 68.5 px wide in a 66 px cell, so it is 1.2 px wider than the cell on each side. It is not cut by the bar (the bar is 272 px wide and the word ends well inside it) and it does not cover a neighbour: the gap to «Салда» stays about 14 px and to «Вкупно» about 10 px. It touches the pill edges: with «Активности» active, the first «А» and the last «и» reach exactly the left and right edge of the pill; in dark mode the outermost pixel or so is dark text on the dark bar and cannot be seen, but the letters stay recognisable. With «Салда» active, the pill's right edge touches the «А» of «Активности». So at 320 px it only touches the cell edge, nothing is lost. At 400 px (the first width with the 16 px font) it is clearly visible: the word sticks out of the pill by about 2.7 px on both sides (`shots2\bar4-400-mk-dark.png`, `shots2\bar4-400-mk-light.png`); readable but not tidy, and in dark mode the ends of «А» and «и» sit on the dark bar.

Other screenshots: `shots2\bar4-360-mk-dark.png`, `shots2\bar4-320-en-dark.png` (all four states each, magnified three times), whole pages `shots2\bar-<width>-<lang>-<theme>-<tab>.png` for 320, 360, 400.

### 3. Plan sheet messages (as the owner, EUR group "No plan group", 360 px, EN and MK, light and dark)
| Step | EN | MK |
|---|---|---|
| EUR chip (default), type `0` | "Type an amount above 0, with up to two decimals." | «Внеси износ поголем од 0, најмногу две децимали.» |
| tap MKD chip (text `0`) | at once "Type an amount above 0, in whole denars." | at once «Внеси износ поголем од 0, во цели денари.» |
| MKD, type `12.5` | same denar text, Save off | same denar text, Save off |
| tap EUR chip with `12.5` | message gone, Save on (12.5 is valid in EUR) | same |
| EUR, type `1.234` | decimals text, Save off | decimals text, Save off |
| EUR, type `0` | decimals text | decimals text |
| tap MKD with `1.234`, then EUR again | denar text, then decimals text, switching each time | same |
| EUR `12.34` valid, tap MKD | message appears (denar text), Save off | same |

The message is linked to the field (`aria-describedby`, `aria-invalid=true`). No Latin "MKD" appears in any Macedonian message (the chip says «ден.»). No plan was written (no PUT or DELETE was sent). Console empty. Screenshots `shots2\sheet-mkd-360-<lang>-<theme>.png`, `shots2\sheet-eur-360-<lang>-<theme>.png`.

### 4. Sanity at 360 px (as A for the group "Ohrid trip", as B for Budget)
- All four group tabs open and show their content in EN and MK, light and dark; the active tab label matches the page; no sideways scroll (360/360) on the group page or on Budget; console and failed-request lists empty in all 16 runs. Not run for Members, group settings or the Groups list.
- Budget (as B): the part above y = 541 px (header, month strip, duplicates card) is pixel-identical to the first-pass screenshots in all four EN/MK x light/dark combinations; the lower part differs only because the data changed (the "spent this month" card now shows 400.42 EUR after the flows of the first pass). The Budget files are not in the changed-files list. This is a comparison with old screenshots, not an automated test.

### Remaining findings
**Bug 1a (low): Macedonian «Активности» is 2.5 px too wide at 320 px and 5.3 px too wide at 400 px; it touches the pill edge at 320 and sticks out of the pill by about 2.7 px per side at 400.**
- Steps: open a group in Macedonian at 320 px or 400 px, tap "Активности".
- Cause: 12 px below 400 px and 16 px from 400 px, with cells that are the bar width divided by four. At 400 px the font grows by a third while the cell grows only by 10 px.
- Proposed fix: keep 12 px up to a higher breakpoint (for example `min-[480px]:text-base`, or use 14 px from 400 px: about 80 px of text in an 86 px cell), and for 320 px use `text-[0.6875rem]` (11 px: about 62.8 px of text in a 66 px cell) or the shorter label «Активност» for this bar only; re-measure at 320, 400 and 480 px in both languages.

**Bug 5 (low, new): the Bug 3 fix moves the amount under the category name in normal groups too, at 320 px and at 360 px in English.**
- Steps: open "Ohrid trip" Totals at 320 px: all five rows ("Transport 3,000 MKD", "Food & drinks 120.00 EUR", "Stay 300.00 EUR", "Fun 60.00 EUR", "No category 15.00 EUR") show the amount under the name (name 184 px wide, amount 54 to 88 px). At 360 px English "Transport / 3,000 MKD" also drops under the name while "Food & drinks / 120.00 EUR" stays beside it, so the rows look uneven. At 360 px Macedonian and at 390 px all rows have the amount beside the name.
- Cause: in `src/web/src/features/groups/totals/components/GroupTotals.tsx` the name has `basis-32` (128 px) and the row only has 240 px at 320 px (tile 40 + gaps 24 + name 128 leaves 48 px for the amount).
- Proposed fix: a smaller basis (for example `basis-20`, 80 px: ordinary amounts up to about 100 px then stay beside the name at 320 px, and only huge amounts such as 9,999,999,999 ден. at 150 px wrap), then re-measure the huge-amount group (name must stay on one or two lines) and "Ohrid trip" at 320, 360 and 390 px. This is also a taste question: say whether the amount under the name at 320 px is acceptable as the cost of the Bug 3 fix.

Bug 2 (product decision) and the Observations of the first pass were not re-checked and still stand. Bug 3 and Bug 4 are fixed as far as this run shows; Bug 1 is fixed for 360, 375 and 390 px and for English everywhere, with the Macedonian exception above.

### NOT VERIFIED in the re-check
- A real phone, other browsers, a screen reader.
- Widths between the ones listed (for example 399, 420 to 700 px) and 1280 px and up for the new tab bar.
- Pixel-exact ink position of the letters at the pill edge (judged from magnified screenshots and Range widths, which include the letters' side gaps).
- Members, group settings and the Groups list after the fixes; Bug 2.
- The automated suites.

## Re-check 2 (tab sizes and row basis, 2026-10-31)

Same method: headless Chromium (Playwright), throwaway users and groups of the first pass, nothing in the repository changed except this section. `__EFMigrationsHistory` ends with `GroupPlan` and `DuplicateAnswers` (confirmed before the run). Stack started with `start-local.ps1 -NoBrowser`, closed with `-Stop` ("Ports 5018 and 5173 are free"). Automated suites (`dotnet test`, `npm test`, lint, build) were **not run**. Scripts and screenshots: `...\scratchpad\p16\r3_*.py`, `r3_*.json`, `shots3\` (not in the repository). Class changes measured: tab labels `text-[11px]` / `min-[360px]:text-xs` / `min-[480px]:text-base`, Totals name `basis-20`.

### Result
| # | Check | Result | Label |
|---|---|---|---|
| 1a | Tab bar: every label fits its cell (Range text width <= link width), EN and MK, light and dark, 320, 359, 360, 375, 390, 399, 400, 479, 480, 768 px, 4 tabs, opened by URL and by tapping | PASS: 320 of 320 views, no label wider than its cell | VERIFIED by live run |
| 1b | Pill over the active link (left and width) within 1 px | PASS: largest difference 0.00 px in all 320 views | VERIFIED by live run |
| 1c | No clipping, one line per label, no sideways scroll | PASS: every label box lies inside the bar, one line each, scroll width equals window width in all 320 views | VERIFIED by live run |
| 1d | Active label contrast | PASS: 14.16:1 light, 9.03:1 dark (same as the earlier run) | VERIFIED by live run |
| 1e | Console and failed requests during the bar run | PASS after one retry: the first full run showed one `ERR_CONNECTION_TIMED_OUT` on a `/balances` load (EN light 375, tapping through the tabs) while an earlier script of mine was still running against the same dev server; the same view alone, repeated (8 views), had an empty console and 0.00 px pill difference | VERIFIED by live run |
| 2a | "Ohrid trip" Totals at 320, 360, 390, EN and MK: ordinary amounts stay beside the name | PASS: all 5 rows beside the name in all 6 combinations (Transport, Food & drinks, Stay, Fun, No category / Превоз, Храна и пијалоци, Смештај, Забава, Без категорија) | VERIFIED by live run |
| 2b | Huge-amount group (9,999,999,999 MKD and EUR 9,999,999,999.99) at 320, 360, 390, EN and MK, light and dark | PASS: name on one line everywhere; 320 and 360 amount under the name (name column 184 and 224 px); 390 amount beside the name (name column 86 to 97 px, still one line) | VERIFIED by live run |
| 2c | No sideways scroll, console empty in the Totals runs | PASS: 320/320, 360/360, 390/390 in all runs; console and failed-request lists empty | VERIFIED by live run |

### 1. Label fit (text width / cell width, px; same in light and dark)
| Width | Font | Cell | EN Expenses / Balances / Activity / Totals | MK Трошоци / Салда / Активности / Вкупно |
|---|---|---|---|---|
| 320 | 11 | 66 | 47.0 / 44.8 / 39.7 / 30.4 | 49.5 / 32.2 / **62.8** / 39.3 |
| 359 | 11 | 75.8 | same as 320 | same as 320 (62.8 of 75.8) |
| 360 | 12 | 76 | 51.3 / 48.8 / 43.3 / 33.1 | 54.0 / 35.1 / **68.5** / 42.9 |
| 375 | 12 | 79.8 | same as 360 | same as 360 (68.5 of 79.8) |
| 390 | 12 | 83.5 | same as 360 | same as 360 (68.5 of 83.5) |
| 399 | 12 | 85.8 | same as 360 | same as 360 (68.5 of 85.8) |
| 400 | 12 | 86 | same as 360 | same as 360 (68.5 of 86; the 16 px step no longer happens at 400) |
| 479 | 12 | 98 | same as 360 | same as 360 (68.5 of 98) |
| 480 | 16 | 98 | 68.3 / 65.1 / 57.8 / 44.1 | 72.0 / 46.8 / **91.3** / 57.2 |
| 768 | 16 | 98 | same as 480 | same as 480 |

«Активности» spare room per side: 320 px 1.6 px, 359 px 6.5, 360 px 3.7, 400 px 8.8, 479 px 14.8, 480 px 3.35, 768 px 3.35. The step at 360 px shrinks the spare room from 6.5 to 3.7 px per side and the step at 480 px from 14.8 to 3.35 px, but the word stays inside its cell at every width tested, so the steps do not break the fit. Screenshot of «Активности» at 480 px (`shots3\crop-bar-480-mk-light-activity.png`): word sits inside the pill. Not looked at in a magnified screenshot at 320 px; judged from numbers only.

### 2. Totals rows (name column width / amount width / position), light theme
"Ohrid trip" (all rows beside the name):
| Width | Transport (3,000 MKD; MK 3.000 ден.) | Food & drinks / Stay (EUR 120.00 / 300.00) | Fun / No category (EUR 60.00 / 15.00) |
|---|---|---|---|
| 320 EN | name 84, amount 88 | 109, 63 | 118, 54 |
| 360 EN | 124, 88 | 149, 63 | 158, 54 |
| 390 EN | 154, 88 | 179, 63 | 188, 54 |
| 320 MK | 89, 83 | 109, 63 (Храна и пијалоци) | 118, 54 |
| 360 MK | 129, 83 | 149, 63 | 158, 54 |
| 390 MK | 159, 83 | 179, 63 | 188, 54 |

Huge amounts (row = 240 px at 320, 280 at 360, 310 at 390), same in light and dark:
| Width | EN Shopping / Stay | MK Купување / Смештај |
|---|---|---|
| 320 | name 184 px, 1 line, amount (156.2 / 145.6 px) under the name | name 184 px, 1 line, amount (151.1 / 145.6) under |
| 360 | name 224 px, 1 line, amount under | name 224 px, 1 line, amount under |
| 390 | name 85.8 / 96.4 px, 1 line, amount beside | name 90.9 / 96.4 px, 1 line, amount beside |

### Remaining findings
Bug 1a and Bug 5 are fixed as far as this run shows. No new bug. Small observation (not a bug): at 390 px with huge amounts the name column is only 86 to 97 px wide; "Shopping", "Stay", "Купување", "Смештај" still fit on one line, but a longer custom category name would wrap to two or three lines there (not tested).

### NOT VERIFIED in re-check 2
- A real phone, other browsers, a screen reader; widths other than the ten listed.
- Letter-level look at 320 px (numbers only); long names at 390 px with huge amounts.
- Members, group settings, Groups list, Budget; Bug 2; the automated suites.

## Re-check 3 (review fixes, 2026-10-31)

Same method: headless Chromium (Playwright), throwaway users of the first pass, time zone `Europe/Skopje`. Only this section of the repository was changed. `__EFMigrationsHistory` ends with `20261027124121_GroupPlan` and `20261027092926_DuplicateAnswers` (confirmed before the run; no new migration). Stack started with `start-local.ps1 -NoBrowser`, closed with `-Stop` ("Ports 5018 and 5173 are free"). The automated suites (`dotnet build`, `dotnet test`, `npm test`, lint) were **not run**. Scripts and screenshots: `...\scratchpad\p16\r4_*.py`, `r4_*.json`, `shots4\` (not in the repository). Runs: 360 px EN light, EN dark, MK light, MK dark, and 320 px EN light (5 combinations) for checks 1 to 4.

### Result per check
| # | Check | Result | Label |
|---|---|---|---|
| 1 | "Not the same" on a merged entry (B, "Hotel booking" 77.00 EUR merged with "Hotel lake view"): row tap, then "Not the same" | PASS in 5 of 5 combinations: the only write is one `PUT /api/personal/duplicate-answers {"answer":"New"}` (no DELETE); the entry's own EUR total goes 41.00 (merged) to 118.00 (counted again); toast "Counted again in your budget" / «Повторно се брои во вашиот буџет»; the pair is not offered again (possible duplicates list loses it, also after a reload; the card shows only the next pair "Dinner / Dinner Ohrid"); database row `answer = New`; no sideways scroll, console empty | VERIFIED by live run |
| 2a | One entry, two expenses (B: "Dinner" 29.00 EUR, "Dinner Ohrid" and a new "Brunch Ohrid", 29.00 share each): tap "Same, merge" on the first pair | PASS in 5 of 5: the second pair is gone from the list and the card; a direct `PUT ... Same` for the second expense answers **400 `DUPLICATE_ALREADY_MERGED`** | VERIFIED by live run |
| 2b | Two entries, one expense (A: "Skopje dinner" 3,100 MKD, new "Dinner 2" 3,050 MKD, one share "Dinner Skopje"): same steps | PASS in 5 of 5: same result, **400 `DUPLICATE_ALREADY_MERGED`** | VERIFIED by live run |
| 2c | Extra: stale card (second tab merged the other pair, then tap on the old card), EN light and MK dark | PASS: translated toast "This entry or expense is already merged with another one." / «Овој внес или трошок веќе е споен со друг.», the card goes away; the only console line is the browser's own 400 for that refusal | VERIFIED by live run |
| 3a | Edit merged entry, note only (A, "Skopje dinner" to "Skopje dinner late") | PASS in 5 of 5: one PUT, still merged, row greyed (opacity 0.8, "Merged with" label), own MKD total unchanged (18,350 before and after), not offered | VERIFIED by live run |
| 3b | Edit merged entry, amount 3,100 to 3,050 | PASS in 5 of 5: no longer merged, row not greyed (opacity 1), MKD total 18,350 to 21,400 (counts again), the card offers the pair again ("Skopje dinner late" 3,050 MKD with "Dinner Skopje" 3,000 MKD, still matches) | VERIFIED by live run |
| 3c | Edit merged entry, category Food to Groceries (360 EN light, 360 MK dark) | PASS in 2 of 2: no longer merged, not greyed, counts again (21,450 MKD), not offered (category differs) | VERIFIED by live run |
| 4 | Plan card "each · N people" ("Ohrid trip", plan 600 EUR; A in tab 1, plain name "Temp Tester" added and later removed through the API from tab 2, then reload of Totals) | PASS in 5 of 5: 4 people 150.00 each, after add **5 people 120.00 each** (EN "€120.00 each · 5 people", MK «€120,00 по човек · 5 лица»), after remove back to 4 and 150.00; the API's `plan.memberCount` equals the group's member count and `perPersonMinor` = 60000 / N each time | VERIFIED by live run |
| 5 | Sanity: Budget (A and B) and group Totals, 360 px EN/MK light/dark and 320 px EN | PASS: 30 views, document width equals window width in all of them, console and failed-request lists empty; the four group tabs (Expenses, Balances, Activity, Totals) in all 4 views per setting: 4 links, every label fits its cell, the pill lies over the active link within 1 px, all inside the bar. (On Budget 18 to 19 elements lie outside the window only inside the month strip, a scroller made for that.) | VERIFIED by live run |

### BUGS
None found in these five checks.

### Observations (not bugs)
1. "Not the same" is two taps in total (tap the row, then "Not the same"), as before; the "Not the same" button itself is one tap and sends one request.
2. After "Not the same" the answer `New` stays, so the pair cannot be offered again unless the entry is edited. Editing a "New"-answered entry (amount, category, date) does not clear `New` (DECISIONS: "`New` answers stay"); I did not test it.
3. Plan card: tested only add and remove of a plain name; a member who leaves, a guest joining and a claimed name were not exercised.

### Not verified
- A real phone, other browsers, a screen reader; widths other than 320 and 360; 768 and above.
- Check 3 for a changed date or currency; edit of a merged entry from the group's side (expense edited or deleted).
- Check 4 without reloading (the other tab's change appearing on its own); members who leave or are claimed.
- The "Not the same" flow for the 320 px MK and 320 px dark combinations; the automated suites.
- The Cloudflare Pages Function path (only the Vite dev proxy was used).

### Throwaway data left behind
- A: new expense "Brunch Ohrid" (116.00 EUR, Ohrid trip, Oct 28) which raised the "Ohrid trip" spent to 659.78 EUR (59.78 over a 600 EUR plan); new entry "Dinner 2" 3,050 MKD (Oct 29); "Skopje dinner" is now in Groceries with no merge (the last category run); answers set to `New` for "Apartment deposit" / "Apartment" and "Dinner 2" / "Dinner Skopje"; "Temp Tester" was added and removed (a removed member row stays in the database).
- B: answers `New` for "Hotel booking" / "Hotel lake view", none for the two "Dinner" pairs.
