# Phase 9: Balances and settle up (report, 2026-10-13)

Branch `feat/09-settle-up`. Step 4b and the review fixes are not committed yet when this is written; Steps 1 to 4a are in commits `df22ef8`, `33ffeb5`, `70bcdc9` and `9e281c2`.

## What Phase 9 delivered
- **Balances:** `GET /api/groups/{id}/balances` gives, per currency (MKD before EUR), every member the group ever had (left and removed ones too, in joining order), their balance in minor units, the simplified "who pays whom" payments and whether everyone is kvit. Rows left behind by "That's me" are hidden.
- **Payments (settlements):** record, confirm, reject, cancel, delete and Undo of a delete. The owner acts for plain names. Rules for who may do what are in `DECISIONS.md` (Phase 9, Step 2).
- **Zero-balance rules:** leave, remove and delete group are refused while money is open (a balance that is not 0, or a pending payment). The "That's me" rule counts payments too.
- **Balances tab (screens 11 and 12):** your balance card, "Needs you" cards (Confirm, Reject), "Between others", "Everyone", a **Payments** list with Delete and an Undo toast, and "Record another payment". Colours: green and red tokens for light and dark. Buttons share one glass look, including the "+" button on the expenses list.
- **Mark paid sheet:** amount prefilled with the suggested amount and editable, a note that says whether the payment waits for confirmation or counts right away. The number in the amount box sits on the left in the two payment sheets (Filip's choice); Add expense and One bill are unchanged.
- **Plain-name rule (Step 4b):** when the owner pays a plain name, the payment is Confirmed at once. Other cases are unchanged.
- **Activity sentences** for the six payment events, in English and Macedonian.

## Review fixes (2026-10-13)
A code review of the whole phase found 9 points. Fixed:
1. **Delete and Restore of a payment** are refused (400 `SETTLEMENT_MEMBER_NOT_CURRENT`) when the payer or the receiver has left or been removed. Without it, such a delete could leave a former member with a balance nobody could settle, and the group could never be deleted.
2. **Members list** is refreshed after every payment change, so "That's me" is not offered for a name that already has a payment.
3. **Balances rows** are matched to members by id instead of by position; a mismatch now stops with a specific error.
4. The amount field's styling uses the `cn` helper.

Not fixed, written to `BACKLOG.md`: two people answering the same pending payment at the same moment, the partial-debt row rule, a retry of Record after the payment was deleted, the unlimited settlements list, the duplicated Pending/Confirmed rule on the website. The unneeded expenses refresh after a payment change was left (harmless).

## Results
- **VERIFIED by automated test (2026-10-13, run by me after the last change):** backend `dotnet build Kvit.slnx` 0 warnings and 0 errors; `dotnet test Kvit.slnx` 2790 of 2790; `npm run lint` 0 warnings; `npm run build` 0 type errors; `npm test` 5457 of 5457 in 135 files. One JoinScreen test failed once under load and passed on re-run (a timing flake, not touched by this phase).
- **VERIFIED by automated test, the new tests:** the Step 4b tests (345 frontend, 20 backend) and the review-fix tests (20 backend, 9 frontend) were red for the right reason before the code and green after; the testers proved they can pass with a throwaway implementation.
- **VERIFIED by live run (2026-10-13, headless Chrome, 360 px, local stack, throwaway accounts):**
  - The Balances tab in light, dark and Macedonian; Mark paid sheet; saving; the Payments list; Delete and Undo; Record another payment sheet; the left-aligned amount.
  - The pending flow with two real accounts (owner and a member who joined by invite): Mark paid gives a pending payment with "Waiting for … to confirm" and Cancel; Cancel brings Mark paid back; the owner's Needs you card with Reject and Confirm; Reject frees the pair; Confirm moves the payment to Payments and the member's balance goes to 0.
- **REPORTED by Filip:** he saw the sheets on his phone and liked the layout (2026-10-13); he asked for the amount to sit on the left, with a small gap, and approved it.
- **NOT VERIFIED:**
  - The review fixes in a browser (covered by automated tests only).
  - Mark paid on a "Between others" row by the owner for a plain name (tests only), server errors shown in the sheet, and EUR payments in a browser.
  - A real phone's keyboard, Safari and other browsers, screen readers, real Render and Neon.
  - The `Settlements` migration on Neon: CI applies it at the merge.
  - The Macedonian texts were looked at in the browser by me only; Filip approved the wording by his instruction, not line by line.
