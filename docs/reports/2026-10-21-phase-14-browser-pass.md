# Phase 14 Step 7: real-browser pass (2026-10-21)

Branch `feat/14-income`. Run by the tester. Nothing in the app was changed, nothing was committed or staged. Only this report was written.

Labels: **VERIFIED by live run** (I did it in a real headless browser against the local app and read the result or looked at the screenshot), **VERIFIED by automated test**, **NOT VERIFIED**.
The automated suites (`dotnet build`, `dotnet test`, `npm run lint`, `npm run build`, `npm test`) were **not run** in this pass; it was only the browser pass.

## What was run and how
- Docker Desktop was open. `dotnet ef migrations list --project src/api/Kvit.Infrastructure --startup-project src/api/Kvit.Api` showed all migrations applied, none "(Pending)", the last three being `20261020101558_Income`, `20261020121456_RecurringIncomeRules`, `20261020131043_GeneratedIncomeEntries`. VERIFIED by live run. I applied nothing.
- `.\scripts\start-local.ps1 -NoBrowser` started the database, API (5018) and website (5173). At the end `.\scripts\start-local.ps1 -Stop` printed "Ports 5018 and 5173 are free" and "Stopped the database container kvit-postgres (its data is kept)".
- Tool: Playwright for Python 1.63.0, headless Chromium only, no visible window, no use of Filip's browser profile (every context is a fresh in-memory one made by Playwright; saved log-ins are scratch JSON files). Time zone `Europe/Skopje`. The machine date is 2026-10-21, so the Budget strip runs Nov 2024 to Oct 2026.
- Matrix: 320, 360, 768, 1280 and 1920 px wide; English (`en-US`) and Macedonian (`mk-MK`) browser language; light and dark; `prefers-reduced-motion: reduce` once; phone touch emulation below 768 px. Window heights 740, 600, 500, 440 and 400 at 360 px for the sheets. Not every check ran in every combination; each row below says which ones.
- Throwaway users made through the app's sign-up (password `Passw0rdA`): A `p14a1792566755@example.com` "Filip Alpha" (main user: income once, three-to-four repeating incomes, Home path), B `p14b1792566755@example.com` "Bojan Beta" (privacy, API validation probe; ended with 20 repeating incomes), C `p14c1792566755@example.com` "Cvetan Gamma" (Left card months), D `p14d1792566755@example.com` "Dragan Delta" (day-31 and ended incomes, long amounts), E `p14e1792567505@example.com` "Elena Epsilon" (catch-up race), F `p14f1792568018@example.com` "Filipa Phi" (exactly two repeating incomes with 58 and 60 character names, for the glass list sheet and long text). One guest "Gosho Guest" joined group "Ohrid trip 14" (owned by A) through an invite link.
- SQL: `docker exec kvit-postgres psql -U kvit -d kvit`, read-only `SELECT`s only.
- Scripts and screenshots (not in the repository): `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\aa3bba29-e12a-4673-93c1-a69afe6b2802\scratchpad\t14\` (scripts `t1.py` to `t16.py`, helper `h14.py`) and screenshots (127 files) in `...\scratchpad\t14\shots\`.
- Console: no app errors or warnings in any run, except (a) the browser's own "Failed to load resource ... 400" lines for refusals I caused on purpose with faked or invalid requests and (b) the app's own `console.error("The catch-up of repeating income failed when the Budget tab opened ...")` in the run where I made the catch-up answer 500 on purpose. VERIFIED by live run.

## Results table
| # | Check | Result | Label |
|---|---|---|---|
| 1a | Budget "+" opens "What do you add?" with Spending and Income. Phone: round "+" (name "Add" / «Додај») at 360 and 768; desktop: round "+ Add" button under the strip at 1280 and 1920. English and Macedonian («Што додаваш?», «Трошење», «Приход») | PASS | VERIFIED by live run |
| 1b | Escape closes it, a tap on the backdrop closes it, focus returns to the "+" / "Add" button. 6 window and language combinations x 6 checks = 36 of 36 | PASS | VERIFIED by live run |
| 1c | Spending goes to the category grid ("What did you spend on?"), the amount is focused after the category tap, one POST with `kind: Spending` and the category id | PASS | VERIFIED by live run |
| 1d | Income opens the income sheet (title "New income", amount, MKD chip, date chip, Note chip, "Repeats: Never", Save) | PASS | VERIFIED by live run |
| 1e | Tap count, Spending: tap Spending, tap category, Save = **3 taps + 1 field** from the open choice sheet | PASS | VERIFIED by live run |
| 1f | Tap count, Income: the amount is **not** focused (Bug 1), typing "456" shows nothing, so Income needs **3 taps + 1 field**, not the 2 + 1 of DECISIONS Step 4 | **FAIL, Bug 1** | VERIFIED by live run |
| 2a | Income once: amount, Note typed as the title ("Salary"), Save: POST body `{"kind":"Income","amountMinor":3000000,"currency":"MKD","entryDate":"2026-10-21","note":"Salary"}`, no category; sheet closes, "Saved" toast, the card and list update | PASS | VERIFIED by live run |
| 2b | Currency MKD to EUR: chip toggles, keypad becomes `decimal`, `250.50` sends `amountMinor 25050`, `EUR`. The next sheet opens in EUR (also from `localStorage` key `kvit.lastEntryCurrency.<userId>`); the first time (no value) it is MKD | PASS | VERIFIED by live run |
| 2c | Date chip, income once: the date input has `min 2024-11-01`, `max 2026-10-21` (24 months to today). Typed 2024-10-31, 2026-10-22, 1999-01-01 and 2027-01-01 leave "Done" disabled; 2024-11-01 and 2026-10-20 enable it. Hint "Pick a date from the last 24 months, up to today." | PASS | VERIFIED by live run |
| 2d | The green "+" row in the list: round green tile with 💰, the note (or "Income") as the title, "Today" / "15 Oct" under it, green "+30,000 MKD" / "+€250.50". Contrast green on card 5.91:1 light, 8.78:1 dark | PASS | VERIFIED by live run |
| 2e | Edit an income: tap the row, sheet opens filled in with the amount focused, no Repeats chip, Save and Delete. Change 1001 to 1002: one PUT, row shows +1,002. Delete: DELETE, toast "Deleted" with Undo, row gone; Undo: `restore` call, row back | PASS, except the title (Bug 3) | VERIFIED by live run |
| 2f | Bad amount: `0`, `abc`, `1.234` (EUR) and an 11-digit amount keep Save disabled with the red edge on the amount, but show **no message** (same shared amount box as Phase 13, see Observation 8) | PASS (as before) | VERIFIED by live run |
| 2g | Error texts for `PERSONAL_ENTRY_AMOUNT_NOT_POSITIVE`, `_AMOUNT_TOO_LARGE`, `_CURRENCY_INVALID`, `MONEY_NOT_ON_CURRENCY_STEP`, `_NOTE_INVALID`, `_DATE_INVALID`, `_INCOME_HAS_CATEGORY` made by a faked 400: translated text sits above Save, no raw code or server English; red edge on the amount (first four), on the date chip (date code). 7 codes x 4 combinations (360 en light, 360 mk dark, 1280 mk light, 1280 en dark) = 28 of 28 | PASS | VERIFIED by live run |
| 2h | Double tap on Save sent exactly one POST; double Enter exactly one POST | PASS | VERIFIED by live run |
| 3a | Repeats chip cycles Never, Monthly, Weekly, Never (when adding). "Starts: Today" replaces the date label from Monthly on; the rhythm line shows "Every month on the 21st" / "Every week on Wednesday" (Macedonian «Месечно, на 21-ви», «Неделно: Среда») and follows the start date | PASS | VERIFIED by live run |
| 3b | Ends chip appears only when repeating ("Ends: never"); its box opens as a small sheet "When does it end?" with Never, In 3 months, In 6 months, In 1 year, Pick a date... (the chosen one has `aria-pressed`). The options count from the start date (start 15 Jul: 15 Oct, 15 Jan 2027, 15 Jul 2027); "Pick a date" opens the date sheet with `min` = start and `max` = start + 10 years; a picked end shows on the chip ("Ends: 1 Mar 2027") | PASS | VERIFIED by live run |
| 3c | Start date limits: `min 2024-11-01`, `max 2027-10-21` (today + 1 year); 2024-10-31 and 2027-10-22 leave Done disabled; hint "Pick a start date from the last 24 months, up to one year ahead." With a future start the end sheet's date input starts at the start date and its Today / Yesterday chips are disabled | PASS | VERIFIED by live run |
| 3d | Monthly income started 15 Jul, saved: POST `/recurring-incomes` then `POST .../catch-up`; at once the months Jul, Aug, Sep and Oct each show one "Pay job 15 <month> +30,000 MKD" row (month strip clicked one by one) and SQL has exactly 4 rows dated 2026-07-15, 08-15, 09-15, 10-15. The Left card of each month shows Income +30,000 and Spent 0 | PASS | VERIFIED by live run |
| 3e | Weekly income (Tuesday) started 6 Oct: entries 6, 13 and 20 Oct (Yesterday) at once (SQL checked); the list says "Every week on Tuesday · next 27 Oct" | PASS | VERIFIED by live run |
| 3f | Day-31 monthly income started 31 Jan: SQL has 31 Jan, 28 Feb, 31 Mar, 30 Apr, 31 May, 30 Jun, 31 Jul, 31 Aug, 30 Sep (month ends respected); list "next 31 Oct". An income with an end date in the past (picked 1 Sep): only Jul 15 and Aug 15 made, the list shows no "next" | PASS | VERIFIED by live run |
| 3g | A start in the future (1 Dec): no entry made, list shows "next 1 Dec" | PASS | VERIFIED by live run |
| 3h | Row "Repeating income · N" with the hint: 1 income "Pay job"; 2 "Tutoring, Pay job"; 3 "Flat rent, Tutoring +1 more"; 4 "Future pay, Flat rent +2 more"; Macedonian «Редовен приход · 4», «...уште 2». None: "Repeating income · none yet" / «сѐ уште нема» (Cyrillic ѐ U+0450) | PASS | VERIFIED by live run |
| 3i | 1 or 2 incomes: the row opens the glass list sheet (name, green amount, rhythm, "next <date>", Edit, Stop). 3 or more, and 0: the row goes to `/budget/repeating` with "Back" (returns to `/budget`), the list and "+ New repeating income" | PASS | VERIFIED by live run |
| 3j | `/budget/repeating` "+ New repeating income" opens the income sheet with Repeats already on Monthly, amount focused (typed 5 shows 5) | PASS | VERIFIED by live run |
| 3k | Edit (on the screen and in the sheet): opens filled in; the Repeats chip toggles **Monthly and Weekly only** (never "Never"); Monthly to Weekly saved: PUT then catch-up, list shows "Every week on Monday · next 26 Oct" (no extra entry, because that week already had the 21 Oct entry); back to Monthly saved | PASS | VERIFIED by live run |
| 3l | Stop (list button): POST `/stop`, toast "Stopped" (red) with "Undo", income leaves the list; Undo: POST `/restart` then catch-up, toast "Restored", income is back. Stop inside the edit sheet: sheet closes, same toast and Undo. A double tap on Stop sends one request; a double tap on Undo sends one restart. Stopping the last income inside the glass sheet leaves the sheet open with "No repeating income yet" and the row says "none yet" | PASS | VERIFIED by live run |
| 3m | Double tap on Save of a repeating income created one income (one POST) | PASS | VERIFIED by live run |
| 3n | Real server refusal when the start date of an income with entries is changed: text "The start date can't change once income was added from it." (Macedonian text in 3o), red date chip. The chip is still enabled in the edit sheet (Bug 5) | PASS for the refusal; **FAIL, Bug 5** (UX) | VERIFIED by live run |
| 3o | Error texts of all 12 `RECURRING_INCOME_*` codes plus `MONEY_NOT_ON_CURRENCY_STEP` (faked 400) in 360 en light and 360 mk dark = 24 of 24: translated, above Save, red edge on the amount (first four), the date chip (day, start date, start cannot change), the Ends chip (end date); no red edge for title (Note not open), frequency, request id, limit | PASS | VERIFIED by live run |
| 3p | Limit of 20: the API refuses the 21st with `RECURRING_INCOME_LIMIT_REACHED` (user B). The text in the sheet was checked with a faked 400 (3o) | PASS | VERIFIED by live run |
| 3q | Known gap (DECISIONS Step 5): a future start kept after cycling the Repeats chip back to Never shows "5 Dec" and Save is refused with "Pick a date from the last 24 months, up to today." and a red date chip | Confirmed as documented | VERIFIED by live run |
| 4a | Catch-up on opening: every opening of the Budget tab sends one `POST .../catch-up`. 9 openings (direct, via Home, reload) for user A: generated rows 9 before and 9 after, no duplicates; user E's Budget shows 14 October rows and SQL agrees | PASS | VERIFIED by live run |
| 4b | Two browser contexts of the same user (E) opening `/budget` at the same moment (async `gather`), three rounds, each with a new income that had not been generated yet (monthly 4 rows, weekly 12 rows, monthly 6 rows): both tabs sent a catch-up, SQL shows exactly 4, 12 and 6 rows, each with as many distinct dates | PASS | VERIFIED by live run |
| 4c | Eight parallel catch-up calls from the two contexts, three rounds with a weekly income from 1 Jun: one call answered `created: 21`, the other seven `0`; SQL 21 rows, 21 distinct dates each. The database also has the unique index `ix_personal_entries_recurring_income_id_entry_date` on `(recurring_income_id, entry_date)` | PASS | VERIFIED by live run |
| 4d | Deleting a generated entry (Tutoring 13 Oct): DELETE, row gone; after a reload and a new catch-up it does not come back (SQL live count 3 to 2 to 2) | PASS | VERIFIED by live run |
| 4e | A failed list load (faked 500) shows the quiet row "Repeating income / Try again" (Macedonian «Обиди се повторно») and the month with the card and entries stays usable; Try again loads the row; a failed catch-up (faked 500) shows nothing to the user and logs one `console.error` (see Observation 7) | PASS | VERIFIED by live run |
| 5a | Left card, no income (user C, May): the old "Spent this month" card ("1,200 MKD"), unchanged | PASS | VERIFIED by live run |
| 5b | Income and spending in MKD (Jun): "Left this month 18,000 MKD", Income +30,000 MKD (green), Spent 12,000 MKD | PASS | VERIFIED by live run |
| 5c | Income MKD, spending EUR (Jul): Left 30,000 MKD, Income +30,000, Spent 0 MKD, and below only a "Spent €50.00" line (no EUR Left, no Income line) | PASS | VERIFIED by live run |
| 5d | Overspent (Aug): "-4,000 MKD" in red with the minus, Income +1,000, Spent 5,000. Measured red 5.84:1 light, 7.62:1 dark | PASS | VERIFIED by live run |
| 5e | Income only (Sep): Left 25,000 MKD, Spent 0 MKD. Income in both currencies (Oct): two blocks, MKD 25,500 left and EUR €259.00 left, each with its own Income and Spent; EUR income only plus MKD spending (Nov 2025): EUR block and a separate "Spent 1,000 MKD" line. MKD and EUR are never added | PASS | VERIFIED by live run |
| 5f | Long amounts, page width: 10-digit MKD (9,999,999,999) and EUR €999,999,999.99 in Income/Spent at 320, 360, 768, 1280, 1920, English and Macedonian: no sideways page scroll, the card never overflows (`scrollWidth` = `clientWidth`) and no child sticks out of the card | PASS for overflow; **FAIL, Bug 4** (the text breaks inside the currency code / number) | VERIFIED by live run |
| 5g | Text contrast, measured in the page (computed colour on the resolved background), 360 px, light and dark: label «Left this month» 8.2:1 / 11.6:1; Left amount 12.72:1 / 13.91:1; green Income 5.91:1 / 8.78:1; red negative Left 5.84:1 / 7.62:1; Spent 12.72:1 / 13.91:1; muted date line 8.2:1 / 11.6:1; Save and "+ New repeating income" white on orange 5.32:1; "Stop" red on card in dark 6.78:1; the "Stopped" toast white on its red, sampled from the screenshot, 5.74:1 light and 5.92:1 dark. All at least 4.5:1. The tool cannot resolve three kinds of element (emoji tiles, the glass "+" button, the bottom-bar active pill), so those are not measured | PASS | VERIFIED by live run (the three unmeasured items NOT VERIFIED) |
| 6a | Guest through an invite link (360 en light and 1280 mk dark): nav has Home, Groups, Settings only (no Budget), `/budget` and `/budget/repeating` end on `/settings`, the UI sent no `/api/personal` request | PASS | VERIFIED by live run |
| 6b | Guest API: GET `/api/personal/recurring-incomes`, POST `/api/personal/recurring-incomes/catch-up`, POST and PUT of a repeating income, stop, restart and GET entries all answer **403 `GUEST_NOT_ALLOWED`** (22 of 22 across both windows). Not signed in: 401 | PASS | VERIFIED by live run |
| 6c | The Home "+" list is unchanged (Add to <group>, New One bill, Another group..., Personal spending; no Income); "Personal spending" still leads to the category grid and saves (360 and 1280) | PASS | VERIFIED by live run |
| 7 | Privacy, user B against user A's data: B's Budget shows no card, no rows and "none yet"; B's list of repeating incomes is `[]`; entries for Jul, Aug, Sep and Oct are empty; B's PUT, stop and restart of A's repeating income id, and PUT, DELETE and restore of A's income entry and DELETE of a generated entry all answer 404 (`RECURRING_INCOME_NOT_FOUND` / `PERSONAL_ENTRY_NOT_FOUND`); B's catch-up creates 0; A's row counts and the "Pay job" income unchanged afterwards (19 of 19) | PASS | VERIFIED by live run |
| 8a | No sideways page scroll at 320, 360, 768, 1280, 1920 (page `scrollWidth` = window width) on the Budget tab (user F, long names), the list sheet, the choice sheet, the income sheet with repeat + a 60-letter one-word note, the Ends sheet and the end-date sheet, in English and Macedonian. (My element-level detector flagged only the month strip's own scrolling pills and screen-reader-only text, which I excluded.) | PASS | VERIFIED by live run |
| 8b | Sheet heights at 360 px wide and 740, 600, 500, 440, 400 px tall, English and Macedonian: choice, income once, once with a 60-character note, monthly with note + Ends + rhythm line, Ends sheet, end-date sheet, start-date sheet, edit repeating (with Stop), list sheet. Sheet top and title never above the window, Save and Stop inside the window; 84 of 90 measurements pass strictly. The other 6 are the list sheet at 500, 440 and 400 px tall (its body scrolls; at 400 px I scrolled to the end and the last "Stop" button ends at y 361 of 400) | PASS | VERIFIED by live run |
| 8c | `/budget/repeating` on a phone: the "+ New repeating income" button ends 32 px above the bottom bar when scrolled to the end at 320 and 360 px (167 px at 768 px, where the page is shorter than the window) | PASS | VERIFIED by live run |
| 8d | Desktop 1280 and 1920: left menu with Budget active, title, month strip, round "+ Add" button, card, "Repeating income" row all in the 611 to 612 px column like Phase 13; sheets are the glass bottom popups. `/budget/repeating` is the "wide" screen (986 px at 1280, 1044 px at 1920) with a 2-column grid of cards, so its left edge (257 px) differs from the Budget tab's (443 px): see Observation 5 | PASS, one question for Filip | VERIFIED by live run |
| 8e | Macedonian: all Phase 14 texts on the Budget tab, sheets, list, screen, errors and toasts are Macedonian; no raw keys seen; the only Latin letters are the currency codes MKD/EUR inside error texts and user data. A scan of every `budget.*` and `RECURRING_*`/`PERSONAL_*` text in `mk.json` found no word that mixes Latin and Cyrillic letters, and the `ѐ` in «сѐ уште» is Cyrillic U+0450 | PASS | VERIFIED by live run (wording not approved by Filip, NOT VERIFIED) |
| 8f | Reduced motion: sheets and nested sheets do not slide (`translate: none` at every sample, transition `none`); with normal motion the choice sheet slides `100%` to `0` in about 0.2 s and closes with opacity falling | PASS | VERIFIED by live run |
| 8g | Dark: Budget, Left card, choice sheet, income sheet and repeating list look right in dark at 320, 360, 1280, 1920 (screenshots looked at: `t10-F-budget-320-mk-dark.png`, `t10-F-incomesheet-360-en-dark.png`, `t10-F-incomesheet-320-mk-dark.png`, `t1-choice-1280-mk-dark.png`) | PASS | VERIFIED by live run |
| 9 | Focus on Escape: Income sheet, Spending grid, the date sheet and the Ends sheet close with Escape and a backdrop tap; focus returns to "Add" (or to the chip that opened the nested sheet) | PASS | VERIFIED by live run |

## BUGS
Numbered by how much they hurt. I did not fix anything.

**Bug 1 (medium to high, "speed is the product"): Income from the choice sheet opens with the amount NOT focused, so no keypad opens and typed digits go nowhere. Income once costs 3 taps + 1 field instead of 2 + 1.**
- Steps: sign in, open `/budget`, tap the round "+" (desktop: "+ Add"), tap "Income", wait one second, type `456` (do not touch the amount box).
- Expected: the amount box is focused with the number keypad (DECISIONS Step 4: "amount focused"; Step 7 brief: income once = 2 taps + 1 field from the open choice sheet).
- Actual: `document.activeElement` is the Budget "+" / "Add" button behind the sheet; the amount box stays empty (`value ''`), Save stays disabled. The second tap on the box is needed: the measured count is Income + tap box + Save = 3 taps. Same at 360 and at 1280. Tab then walks the page behind the sheet (focus is not inside the dialog). The Spending path is fine (its amount stage mounts later and focuses; 3 taps + 1 field).
- My guess at the cause (not checked by changing the code): in `src/web/src/features/budget/overview/components/BudgetScreen.tsx` lines 166 to 168, the choice sheet is unmounted in the same render that mounts `PersonalIncomeSheet`; the choice sheet's `finalFocus` (`focusAfterClose`, `src/web/src/shared/components/KvitSheet.tsx` lines 21 to 56) returns focus to its opener (the "Add" button) after the income sheet's `autoFocus` (`src/web/src/features/expenses/shared/components/AmountField.tsx` line 48) has run. This is the same family as Phase 13 Bug 1 and Bug 8.
- Screenshot: `t14\shots\e2-income-360.png`. Scripts `e2b.py`, `t1b.py` print the focus samples and the empty value.

**Bug 2 (medium for keyboard and screen-reader users): "Edit" in the Repeating income list sheet on the Budget tab opens the edit sheet with the focus on the page behind it, and Tab does not stay in the sheet.**
- Steps: have 1 or 2 repeating incomes (user F), open `/budget`, tap the "Repeating income · 2" row (glass sheet opens), tap "Edit" on one.
- Expected: focus inside the edit sheet (the amount box, like when you edit a one-time income or when you press Edit on `/budget/repeating`).
- Actual: the edit sheet is open, but `document.activeElement` is the "Repeating income · 2 ..." row button of the Budget page; four Tab presses move over the entry rows behind the sheet, none stays in the dialog. A pointer user is not affected. From `/budget/repeating` the same Edit gives the focus on the amount `INPUT`.
- Guess: same pattern as Bug 1: `RecurringIncomeListSheet.tsx` lines 24 to 30 close the list sheet, then `BudgetScreen.tsx` lines 169 to 176 mount the edit sheet; the first sheet's focus return wins.
- Script `t12.py` (prints each Tab target and whether it is inside the dialog).

**Bug 3 (low, wording): the edit sheet of an income is titled "New income" / «Нов приход».**
- Steps: tap any income row in the list (or "Edit" on a repeating income): title "New income", although it is an existing income.
- Expected: a title that fits editing (for example "Income" / "Edit income"); the spending edit sheet shows the category name instead.
- Cause: `src/web/src/features/budget/entries/components/PersonalIncomeSheet.tsx` line 82 always uses `t('budget.income.title')`.
- Screenshots: `t14\shots\t2c-edit-360.png`, `t3e-edit-360.png`.

**Bug 4 (low): long amounts in the Left card's Income and Spent columns break inside the currency code or after the "+".**
- Steps: have an income of 1,240,567 MKD in a month (or one of 9,999,999,999 MKD), open the Budget tab at 320 px (also 360 with the 10-digit one).
- Expected: the amount stays whole (shrink or break only at the space), as Phase 13 Bug 5 was fixed for the category tiles.
- Actual: "+1,240,567 M" / "KD" on two lines at 320 px (English), "+1.240.567 де" / "н." (Macedonian); with the 10-digit amounts "9,999,999,998 M / KD", "+" alone on one line and the number on the next, and Macedonian numbers split between digits ("+9.999.999.99 / 9 ден."). The page itself does not scroll sideways and the card stays inside its box; the big "Left" number is fine (one line, `KvitAmount`).
- Guess: `src/web/src/features/budget/overview/components/BudgetTotalCard.tsx` lines 85 and 91 (`data-income-amount`, `data-spent-amount` spans use `break-words` on plain `formatSignedMoney` / `formatMoney` text), lines 41, 75 and 105 for the others.
- Screenshots: `t14\shots\t5d-320-en-light.png`, `t5c-Dec2025-320-mk-dark.png`, `t5a-Dec2025-360-en-light.png`, `t10-F-budget-320-mk-dark.png`.

**Bug 5 (low, UX): when you edit a repeating income that already has entries, the "Starts" date chip is still enabled, but the server refuses any change.**
- Steps: `/budget/repeating`, Edit "Pay job" (started 15 Jul, entries exist), tap "Starts: 15 Jul", pick 14 Jul, Done, Save.
- Expected (DECISIONS Step 1 defaults: "the start date can change only until the first entry exists"): the chip is disabled, or the date picker is not offered, so the person does not do work that is refused.
- Actual: the request is sent, the server answers `RECURRING_INCOME_START_CANNOT_CHANGE` and the sheet shows "The start date can't change once income was added from it." with the date chip red (that text is correct). Nothing is saved.
- Files: `PersonalIncomeSheet.tsx` (the date chip is always enabled while not busy), `useRepeatingIncomeForm.ts` `refusedCodesOf.date`. Screenshot `t14\shots\t3l-startchange-360.png`. This is a product call for Filip (see Questions).

No backend bug was found: the API answers match DECISIONS (see the API probe below).

## API probe (direct calls from the signed-in browser session, VERIFIED by live run)
- Repeating income create: amount 0 and -1 `RECURRING_INCOME_AMOUNT_NOT_POSITIVE`; 10^15 `_AMOUNT_TOO_LARGE`; USD `_CURRENCY_INVALID`; MKD 101 `MONEY_NOT_ON_CURRENCY_STEP`; title of 61 characters `_TITLE_INVALID`; frequency "Daily" `_FREQUENCY_INVALID`; day of month 0, 32 and a Monthly with no day, weekday 7 `_DAY_INVALID`; start 2027-10-22 (today + 1 year + 1 day) `_START_DATE_INVALID`; end before the start and end more than 10 years after the start `_END_DATE_INVALID`; the same `clientRequestId` twice returns the same id (no duplicate); the 21st active income `_LIMIT_REACHED`; restart of an active one `_NOT_STOPPED`; stop twice and edit of a stopped one 404.
- Accepted (not refused): start 2024-10-31 (one day before the oldest strip month, the same documented edge as Phase 13), start 2024-11-01, start today + 1 year, end equal to the start (the error text says "after the start", DECISIONS says `ends_on >= starts_on`), end exactly 10 years after the start, a day of month that differs from the start date's day, a blank title (stored as no title).
- Catch-up and privacy: see rows 4a to 4d, 6b and 7.

## OBSERVATIONS (small things, not bugs)
1. The Budget row at zero ("Repeating income · none yet") is my own recommendation in DECISIONS, still not confirmed by Filip; it works as described and the empty screen (`/budget/repeating`) shows "No repeating income yet" with "+ New repeating income".
2. When the last repeating income is stopped inside the glass list sheet, the sheet stays open showing only "No repeating income yet" with no "+ New" button; the person must close it and tap the row again.
3. The "Saved" and "Stopped" toasts sit over the first card's Edit / Stop buttons in the glass list sheet for a few seconds (it goes by itself).
4. The glass list sheet has no "Close" button (the mockup `round3.html` showed one); it closes with Escape, a tap on the backdrop, or the drag. Not checked whether a swipe-down closes it.
5. Desktop `/budget/repeating` uses the wide screen width with two columns, while the Budget tab uses the 40 rem column; going between them moves the content's left edge from 443 px to 257 px at 1280. Product question below.
6. The list never shows that an income has ended or its end date (the "Ended" test income shows the rhythm and no "next"), and the edit sheet of an entry made by a repeating income looks the same as a one-time income (no hint that it came from a repeating one).
7. If the catch-up fails when the Budget tab opens, the user sees nothing (only a `console.error` with a clear message). That follows the DECISIONS Step 5 choice "never makes the month wait", but a person would not know their salary entries are missing until the next opening.
8. Amounts that cannot be saved (`0`, `abc`, `1.234` EUR, 11 digits) disable Save with the red edge and no message, as in Phase 13 Observation 1 (shared amount box).
9. The rhythm line says "Every month on the 31st" for a day-31 income; the mockup's own text said "day 31 means the last day of shorter months". The behaviour is right (28 Feb, 30 Apr...), only the explanation is missing. The same applies to the 29th and 30th.
10. The Undo toast of "Stop" says "Restored" («Вратено»), reusing the existing key.
11. After the first Monthly or Weekly tap the date chip says "Starts: Today"; "Ends: ..." has no year for the current year, and "Starts: 15 Jul" has no year either (the same date format as the list).
12. Macedonian drafts for Filip to look at (not approved): «Остануваат овој месец» uses the plural verb for one amount (singular would be «Останува»); «Неделно: Среда» capitalises the weekday after the colon; «Редовен приход» (regular income) versus the English "Repeating income"; «Додај» / «Што додаваш?» (informal singular).
13. The "Ends" chip's ⏹ icon is a plain monochrome square glyph (text colour), unlike the coloured emoji of the other chips.
14. Like Phase 13 Observation 12, the choice sheet and income sheet touch the bottom edge of the window on desktop (glass bottom popup, centred in the window, not in the 40 rem column).
15. Two checks in my own scripts first printed FAIL ("row shows 1,002", "row back after Undo") because the money text has a non-breaking space; I read the list again and both rows were there. Not app problems.
16. A first full run of my contrast script crashed twice between "Stop" and "Undo", which left three of user A's repeating incomes stopped; I restarted them through the API (204 each). Not app problems.

## Tap counts measured
- Spending from the open choice sheet: Spending, category, Save = **3 taps + 1 typed field** (amount focused after the category tap). Matches DECISIONS Step 4.
- Income once from the open choice sheet: Income, **tap the amount box (Bug 1)**, Save = **3 taps + 1 field**; with Bug 1 fixed it would be Income, Save = 2 taps + 1 field. Adding a Note or a EUR switch costs one tap each.
- Repeating income: Income, Repeats chip (once for Monthly, twice for Weekly), Save, plus the amount; pick a start date or an end adds 2 or 3 taps. From `/budget/repeating`: "+ New repeating income", Save = 2 taps + 1 field (Repeats already Monthly, amount focused).
- Stop: row, Stop (2 taps, sheet or screen), Undo 1 more; with 3 or more incomes: row, Stop. Edit: row, Edit, change, Save.

## Throwaway data left in the local database
Users A to F and the guest listed at the top, with their incomes and entries. The database container was stopped with `-Stop` and its data is kept. B has 20 active repeating incomes (the limit test) and E, D, C, F have many entries between Nov 2025 and Oct 2026. Nothing was deleted from the database.

## NOT VERIFIED (could not check)
- A real phone (touch, the on-screen keyboard, the keypad actually opening), iOS Safari, Android Chrome, Firefox, a screen reader (focus was read from `document.activeElement` only).
- The native calendar pop-up of the date input (only the input's `min` / `max` and the Done button were checked).
- Contrast of three element kinds my script cannot resolve: the emoji tiles, the glass "+" button and the active pill of the bottom bar (unchanged since Phase 13).
- Macedonian wording approved by Filip.
- List rows fading in or out when entries change (the sampling cannot catch it), and the toast animation by eye.
- A different time zone (only `Europe/Skopje`), a person west of UTC right after midnight, the 5-day restart limit (`RECURRING_INCOME_RESTART_EXPIRED`, only its text and code exist; it needs a clock change or an old stop), and real network slowness.
- Several hundred entries in one month; 20 repeating incomes on the screen (user B has them, I did not look at its layout).
- Whether the real catch-up and the Budget month request interleave well on a slow Render wake-up (only local).
- The automated suites (`dotnet test`, `npm test`, `npm run lint`, `npm run build`).

## Questions for Filip (product only)
1. Bug 5: when an income already has entries, should the "Starts" date be locked in the edit sheet (a greyed chip), or stay open and give the refusal after Save as today? Example: Pay job started 15 Jul; you open Edit to change the amount and tap the date by accident.
2. Observation 5: should the "Repeating income" screen on a computer use the same narrow column as the Budget tab (one card under the other), or stay wide with two cards side by side as in the Groups list?
3. Observation 2: when you stop your last repeating income inside the small list window, should the window close by itself, or stay open with "+ New repeating income" in it?
4. Observation 9: should the rhythm line say "last day of the month" when the start day is the 29th, 30th or 31st?

## Summary
| Item | Result |
|---|---|
| 1 Choice sheet, phone and desktop, Escape, backdrop, focus, spending taps | PASS, VERIFIED by live run |
| 1 Income opens focused (2 taps + 1 field) | **FAIL, Bug 1** |
| 2 Income once (amount, MKD/EUR + default, date limits, note, list row, edit, delete + Undo, errors, double tap) | PASS, VERIFIED by live run (edit sheet title: Bug 3) |
| 3 Repeating income (chips, Ends box, start in the past, weekly, day 31, future start, row + hint, sheet / screen split, edit, stop + Undo, errors) | PASS, VERIFIED by live run (Bug 2, Bug 5) |
| 4 Catch-up (reopen, two tabs, eight parallel calls, deleted entry stays deleted) | PASS, VERIFIED by live run |
| 5 Left card (6 scenarios, long amount width, contrast) | PASS, VERIFIED by live run (Bug 4: text breaks at 320 px) |
| 6 Guest (no tab, redirects, 403) | PASS, VERIFIED by live run |
| 7 Privacy | PASS, VERIFIED by live run |
| 8 Layout (320 to 1920, sheet heights 740 to 400, Macedonian, dark, reduced motion, console) | PASS, VERIFIED by live run |
| Bugs | 1 (medium to high), 2 (medium, keyboard), 3 (low), 4 (low), 5 (low, UX) |

## Re-check (2026-10-21)

Run by the tester after the coder's fixes for Bugs 1 to 5, Observations 2, 5 and 9. Nothing in the app was changed, nothing was committed. The only file written in the repository is this report.

Labels: **VERIFIED by live run** (real headless Chromium against the local app), **NOT VERIFIED**. The automated suites (`dotnet build`, `dotnet test`, `npm test`, `npm run lint`, `npm run build`) were **not run** in this re-check.

### How it was run
- Docker Desktop was open. `dotnet ef migrations list` printed the same last three migrations as before (`Income`, `RecurringIncomeRules`, `GeneratedIncomeEntries`); this round adds none. With the database down it could not print the applied state, so I started the app first: `.\scripts\start-local.ps1 -NoBrowser` runs its own "every database update is applied" check and passed, and the list after start-up showed nothing pending. VERIFIED by live run.
- `.\scripts\start-local.ps1 -Stop` at the end: "Ports 5018 and 5173 are free", database container stopped, data kept.
- Playwright for Python, headless Chromium only, fresh in-memory contexts (log-ins saved as scratch JSON files), time zone `Europe/Skopje`, machine date 2026-10-21. No visible window, never Filip's profile.
- New throwaway users (password `Passw0rdA`): R `p14r1792570742@example.com` (checks, one and two repeating incomes), S `p14s1792570742@example.com` (several repeating incomes, month-end days, locked date), T (long amounts, Feb to Jun 2026), V (two accounts `p14v1792571479` and `p14v1792571506`, day-31 income and the no-duplicate check), X (stale `hasEntries`). From the first pass I reused F (two incomes), C (Left card months) and the guest G.
- SQL: read-only `SELECT`s. Scripts `r0.py` to `r16.py` and screenshots `r2-*`, `r3-*`, `r5-*`, `r7-*`, `r8-*`, `r9-*`, `r10-*` are in `C:\Users\Davchev\AppData\Local\Temp\claude\c--Users-Davchev-Projects-Kvit\aa3bba29-e12a-4673-93c1-a69afe6b2802\scratchpad\t14\` (`shots\` for the pictures). Not in the repository.
- Console: no app errors or warnings in any run, except the browser's own "Failed to load resource ... 400" for the one refusal I caused on purpose (Bug 6 below).

### Result per item
| # | Item | Result | Label |
|---|---|---|---|
| 1 | Bug 1, focus after Income and Spending. 360 and 1280 px, English and Macedonian, Escape and backdrop tap, Income and Spending = 64 of 64 checks: "+" or "Add", Income, type `456` without touching the box: the amount shows `456` (focus is the `INPUT`); Spending: focus is inside the dialog on the grid; Tab x8 never leaves the sheet; Escape or a tap on the backdrop closes it and focus is back on "Add" / «Додај». Also Escape from the Spending amount stage (after a category tap): focus back on "Add" (360 en, 1280 mk). **Taps recounted: Income once = Income, type, Save = 2 taps + 1 field from the open choice sheet (measured with real clicks, one POST `amountMinor 123400`, `kind Income`). Spending = Spending, category, Save = 3 taps + 1 field, unchanged.** | **PASS** | VERIFIED by live run |
| 2 | Bug 2, Edit from the list sheet. User F (two incomes) and user R (one income), 360 en, 1280 en, 360 mk, Escape and backdrop = 24 of 24, plus the one-income case: Edit leaves exactly one dialog open, focus is on the amount `INPUT`, Tab x8 stays in the sheet, Escape or backdrop returns focus to the "Repeating income · N" row (a `BUTTON`). | **PASS** | VERIFIED by live run |
| 3 | Bug 3, edit title. One-time income edit: "Income" at 360 en, «Приход» at 1280 mk. Repeating income edit (from the sheet): "Income" / «Приход». Adding: "New income" / «Нов приход». | **PASS** | VERIFIED by live run |
| 4 | Bug 4, long amounts in the Left card. 320 and 360 px x EN and MK x light and dark x 5 months (user T: `+1,240,567 MKD` income; 10-digit `9,999,999,999 MKD` income and spent; negative Left `-9,998,759,432 MKD`; second currency `€999,999,999.99` income, `€123,456,789.01` spent, second Left `€876,543,210.98` and `-€998,765,432.10`) = **168 of 168 elements OK**: every Income, Spent, big Left and second-currency Left sits on one line (`white-space: nowrap`), stays inside the card, is not clipped, and the page has no sideways scroll (`scrollWidth` = `clientWidth`). No break in "MKD" / «ден.» and none after "+". Smallest contrast measured: **5.84:1** (the red negative Left); Income green 5.91:1 light / 8.78:1 dark, Spent and Left 12.72:1 light / 13.91:1 dark, second-currency Left 8.2:1: all at least 4.5:1. Screenshots looked at: `r7-320-en-light-Mar.png`, `r7-320-mk-dark-May.png`. See Observation 17 (the shrunk text gets as small as 9.8 px). | **PASS** | VERIFIED by live run |
| 5a | Bug 5, locked start date. Editing a repeating income that has entries: the "Starts" chip is disabled (`disabled`, opacity 0.5, `pointer-events: none`), a forced tap opens nothing. Seen for an income made in the UI (same session, no reload), after a reload, on `/budget/repeating` and in the list sheet. | **PASS** | VERIFIED by live run |
| 5b | An income with a future start and no entries (1 Dec): chip enabled; changed to 5 Dec, PUT then catch-up, the list says "next 5 Dec". | **PASS** | VERIFIED by live run |
| 5c | **Is `hasEntries` stale right after the opening catch-up?** Yes, in one case. Setup (users S and X): an income with a start in the past is made without the UI catch-up (API call, or on another device); then `/budget` is opened. The opening requests are `GET /recurring-incomes` (says `hasEntries: false`) and `POST .../catch-up` (answers `created: 3`), no second GET. With **exactly 1 or 2 repeating incomes** the row opens the glass list sheet, which re-uses that list: Edit shows the "Starts" chip **enabled** (2 of 2 runs). A change to an earlier day is sent and the server answers **400 `RECURRING_INCOME_START_CANNOT_CHANGE`** (direct call: "The start date cannot change from 2026-08-20 to '2026-08-19' because entries were already created from this income."); the sheet shows the correct translated text "The start date can't change once income was added from it." and the chip stays red and tappable. After a reload the chip is disabled. With **3 or more** incomes the row goes to `/budget/repeating`, which loads the list again, and the chip was already disabled on the first open (5 of 5 runs, user S). An income made in the UI is not affected (the create flow refreshes the list). So only the edge "income made elsewhere, then the first Budget opening, 1 or 2 incomes, change the start date" shows the old behaviour, and nothing wrong is saved. See Bug 6. | PASS for the lock, **Bug 6** for the stale case | VERIFIED by live run |
| 6 | `/budget/repeating` at 1280 and 1920 px (user S, 7 incomes). Budget tab and repeating screen share the same column: left edge 443 px at 1280 (611 wide) and 798 px at 1920 (612 wide); the title and every list card have that same left edge and width, so it is **one column**. No sideways scroll. | **PASS** | VERIFIED by live run |
| 7 | Stop in the glass list sheet. One income (user R): Stop closes the sheet (0 dialogs), the toast "Stopped" with "Undo" shows, the row says "Repeating income · none yet"; Undo sends `restart` then `catch-up`, the row is back with "Solo R". Two incomes: Stop one keeps the sheet open with one card left, the toast shows, Undo puts the second card back in the still-open sheet (2 cards). | **PASS** | VERIFIED by live run |
| 8 | Month-end line. Monthly days 28, 29, 30, 31 (user S), list and edit sheet, 360 and 1280, EN and MK = 32 of 32; plus the new-income sheet hint (start dates 28 Aug, 29, 30, 31 Aug, 15 Sep) and the glass list sheet (user V). Days 29, 30, 31 end with "(or the last day of shorter months)" / «(или последниот ден во пократките месеци)»; days 28 and 15 are unchanged ("Every month on the 28th", «Месечно, на 28-ми»). The list line keeps "· next 29 Oct". No sideways scroll at 360. | **PASS** | VERIFIED by live run |

### Regression sweep
| Check | Result | Label |
|---|---|---|
| Choice sheet: "What do you add?" with Spending and Income (360 en); «Што додаваш?», «Трошење», «Приход» (1280 mk) | PASS | VERIFIED by live run |
| Income once: save (one POST), edit (PUT, amount focused), delete (DELETE, "Deleted" / «Избришано» toast with Undo / «Врати»), Undo (`restore`, row back). 360 en and 1280 mk, 16 of 16 | PASS | VERIFIED by live run |
| Spending path: category tap focuses the amount, one POST `Spending` with the category id, 3 taps + 1 field (360 en, 1280 mk) | PASS | VERIFIED by live run |
| Past-start monthly income creates its entries: day-31 income started 31 Aug 2026 (user V): SQL has exactly 2026-08-31 and 2026-09-30 (month ends respected) | PASS | VERIFIED by live run |
| No duplicates on reopening: three reloads of `/budget`, SQL still exactly 2 rows per user V | PASS | VERIFIED by live run |
| Left card cases, user C, all scenarios again in EN and MK (spent-only card, Left with Income and Spent, MKD income + EUR spending, overspent `-4,000 MKD`, income only, both currencies, EUR income + MKD spending): same texts as in the first pass | PASS | VERIFIED by live run |
| Guest (user G): `/budget` and `/budget/repeating` end on `/settings`, nav is Home, Groups, Settings, the UI sent no `/api/personal` request, a direct GET answers 403 | PASS | VERIFIED by live run |
| No sideways scroll at 320 and 360 on the Budget tab, the choice sheet, the income sheet and the list sheet (users S and T, 4 of 4) | PASS | VERIFIED by live run |
| Console errors and warnings | none from the app (only the browser's own 400 line for the refusal I made on purpose) | VERIFIED by live run |

### New bugs
**Bug 6 (low, UX): the "Starts" chip stays enabled in the list sheet's Edit right after the opening catch-up, when the income was made outside this session and has just gotten its first entries.**
- Steps: have exactly one repeating income with a start in the past that has no entries yet (make it by a direct API call, or on another device without opening Budget), open `/budget` (the opening catch-up creates the entries), tap "Repeating income · 1", tap Edit, tap "Starts: ...", pick an earlier day, Done, Save.
- Expected: the chip is greyed out (entries exist now).
- Actual: the chip is tappable; Save sends the PUT and the server refuses with `RECURRING_INCOME_START_CANNOT_CHANGE` (the text is correct, nothing is saved). A reload fixes it. With 3 or more incomes it does not happen.
- Cause (read in the code, not changed): `catchUpOnOpen` in `src/web/src/features/budget/recurring/recurringIncomeCache.ts` (lines 17 to 22) refreshes only the personal entries (`refreshEntriesMadeByCatchUp`), not the recurring list `recurringIncomesQueryKey`; the list sheet for 1 or 2 incomes uses the list fetched before the catch-up (`hasEntries: false`), while the screen for 3 or more mounts a new list query and refetches. A fix would be to also call `refreshRecurringIncomes` when `created > 0`.
- This is the exact edge the coder warned about. It loses no data, only a refused Save with a correct message, so I rate it low.

### Observations (new, not bugs)
17. In the Left card the Income and Spent columns shrink with the amount, so a 10-digit amount in the Income column reaches **9.8 px** (Macedonian) / 10.4 px (English) at 320 px, next to a Spent amount that can be 17 px; the two columns then look uneven. Contrast and layout pass, it is only small to read. Details per element are in the scratchpad file `t14\r7.out`.
18. After a refused date change (Bug 6) the chip stays red and tappable; harmless.
19. The Stop and Undo checks in the list sheet ran at 360 px, English only; the same component is used at 1280 px and in Macedonian (see NOT VERIFIED).
20. First-pass Observation 2 (empty list sheet after stopping the last income) and Observation 9 (month-end text) are now resolved by the new behaviour (items 7 and 8).

### Still NOT VERIFIED
- Everything on the first pass's NOT VERIFIED list still stands: a real phone and its on-screen keyboard (focus was read from `document.activeElement`, and the keypad cannot be seen headless), iOS Safari, Android Chrome, Firefox, a screen reader, the native calendar pop-up, contrast of the emoji tiles / glass "+" / active pill, Macedonian wording approved by Filip (the new «(или последниот ден во пократките месеци)» and the title «Приход» are drafts), other time zones, the 5-day restart limit, real network slowness.
- Whether an entry **deleted** by the person (soft delete) still counts for `hasEntries` (so the date stays locked): the server decides it, I did not test it.
- Stop and Undo in the list sheet at 1280 px and in Macedonian.
- Contrast of the shrunk Left card amounts at widths other than 320 and 360.
- The automated suites, including the new `RecurringIncomeHasEntriesTests.cs` and the changed web tests.
