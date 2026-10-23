# Decisions

## 2026-10-20: Phase 14 (income, once and repeating), in progress
Branch `feat/14-income`. Plan: `C:\Users\Davchev\.claude\plans\filip-wants-to-run-foamy-crystal.md`. Mockups: `docs/design/2026-10-20-phase-14/` (`index.html`, `round2.html`, `round3.html`).
**Filip's picks (2026-10-20):**
- **Where income starts = C:** the "+" on the Budget tab opens a small bottom sheet that asks "Spending or Income?". Spending then goes to the category grid (+, Spending, category, Save = 3 taps + 1 field, one more than Phase 13), income opens the income sheet with no category (+, Income, Save = 2 taps + 1 field). *Rejected:* A (a switch inside the sheet), B (a second "+ Income" button), C2 (an "Income" tile in the category grid, which would have kept spending at 2 taps; I recommended it for speed and Filip kept C).
- **Income sheet = the round-1 look:** amount focused, MKD ⇄ EUR (the last used currency is the default), a date chip (Today), an optional title chip ("Salary"), a "🔁 Repeats" chip that cycles Never, Monthly, Weekly. For Monthly or Weekly an "⏹ Ends" chip appears; tapping it lets the person **choose when it ends**: Never, in 3 months, in 6 months, in 1 year, or a picked date. Ending is optional. *Rejected:* the round-2 sheet looks (a chip that opens a list, a three-way switch, an On/Off switch).
- **Budget tab = one card (3B):** "Left this month" (income minus spending) with Income and Spent below it. MKD and EUR are never added together.
- **Repeating income list = slim row (3A):** one row under the card, "Repeating income · N ›" with a hint such as "Salary, Flat rent +1 more". With 1 or 2 repeating incomes it opens a sheet; with **3 or more it opens its own screen**. Each income shows its name, amount, rhythm and next date, with Edit and Stop. *Rejected:* green-edge cards on the Budget tab (B), a line inside the big card (C), a button only (D).
- **Row at zero (my recommendation, taken because Filip said "think about that idea", not yet confirmed by him):** the row stays visible when there are none, as "Repeating income · none yet ›", and opens the list with an empty state and a "+ New repeating income" button that opens the income sheet with Repeats already on Monthly. This gives a second way to create one and the only place to find them again.
**Defaults I take until Filip says otherwise (he has not answered these):**
- Repeating dates: the start may be in the past, but entries are never written for a month older than the oldest month on the Budget strip (24 months). A start in the future is allowed up to one year ahead and its entries appear when their day comes. Income once uses the Phase 13 rule (the last 24 months up to today).
- Edit changes only the entries that come from now on (generation never goes back to a date on or before the last entry made for that income, deleted ones included; the start date can change only until the first entry exists). Stop records `stopped_at` only (the end date is not touched, which would break `ends_on >= starts_on` for a start in the future); a stopped income is hidden and generates nothing more. Restart (the Undo of Stop) works within 5 days. At most 20 active repeating incomes per person. Entries already created stay as they are. Deleting one created entry does not bring it back (the deleted row still holds that date).
- **One entry per month (monthly) or per week (weekly, weeks Sunday to Saturday) (Step 3, accepted by Filip by moving on):** a new generated entry can only fall in a period after the one that holds the last entry of that income (deleted entries count). So moving a pay day inside a month never gives a second salary in that month; the next one is the next month. `nextDate` follows the same rule. Known and not handled: switching Monthly to Weekly back-fills the weeks after the last entry. *Rejected:* "the day after the last entry" (it created a second salary after a pay day edit and failed the tester's test).
- **Restart after a gap back-fills the missed dates (Step 3, not pinned by a test):** Restart works only within 5 days, so at most one or two dates.
- **Step 3 migration `GeneratedIncomeEntries`:** `personal_entries.client_request_id` is nullable (the unique index stays), because entries made from a repeating income have no request id.
- **Step 4 (frontend):** the Budget "+" (phone and desktop) opens "What do you add?" (Spending / Income); the "+" button's name is now "Add" / «Додај». The income title is stored as the entry note (the existing Note chip). Editing a one-time income has no Repeats chip. The Home "+" list is unchanged.
- **Step 5 (frontend, choices not pinned by tests):** the Repeating income row sits under the month card and does not follow the selected month; a failed list load shows a quiet row with "Try again"; the catch-up runs once each time the Budget tab opens and never makes the month wait (a catch-up that creates entries cancels the running month request and asks again); the opening catch-up refreshes the entries, not the list (the "next" dates can lag until the list refreshes); Stop sends no catch-up, Undo of Stop (restart) does; editing a repeating income toggles Monthly ⇄ Weekly only; the Ends options count from the start date with month-end clamping; Today/Yesterday in the date sheet are greyed out outside the allowed range. Known gap: a future start kept after cycling back to Never is refused by the server (red date chip).
- **Step 6 (frontend):** a month with income shows "Left this month" per income currency (income minus spending in that currency, red with a minus when negative, normal colour when positive; the mockup's green Left was not used), with Income (green, "+") and Spent columns; a currency with spending but no income shows only a "Spent" line; a month with no income keeps the Phase 13 "Spent this month" card unchanged; MKD and EUR are never added.
- **Step 7 (browser pass, Filip took my recommendations, 2026-10-21; `reports/2026-10-21-phase-14-browser-pass.md`):**
  - **Focus between chained sheets:** after Spending or Income, the choice sheet gives no focus back; `KvitSheet` got an optional `opener` (the button focus returns to on cancel), and the Budget tab hands the "+" (or the Repeating income row, for Edit from the list sheet) to the next sheet. Income once is 2 taps + 1 field again.
  - **Edit title:** editing an income (one-time or repeating) says "Income" / «Приход»; adding still says "New income".
  - **Long amounts** in the Left card shrink to one line (`fit-amount`, as the category tiles).
  - **Start date locked once entries exist:** each repeating income row carries `hasEntries` (computed from the last-entry date the list already loads, deleted entries included); the "Starts" chip is disabled then; the server's `RECURRING_INCOME_START_CANNOT_CHANGE` stays as the safety net.
  - **`/budget/repeating` on a computer** uses the Budget tab's 40rem single column (the wide two-column layout was dropped: the left edge jumped).
  - **Stopping the last income in the list sheet closes it** (the "Stopped · Undo" toast stays).
  - **Days 29 to 31** add "(or the last day of shorter months)" to the rhythm line.
  - **Bug 6:** the opening catch-up now also reloads the repeating-income list when it created entries (cancelling a request still on its way first, like the entries), so `hasEntries` is current. This replaces the Step 5 choice "the opening catch-up refreshes the entries, not the list".
- **Step 8 (code review, two `/code-review high` passes; Filip took my recommendations, 2026-10-21):**
  - **The date of an entry made by a repeating income cannot change** (new `PERSONAL_ENTRY_DATE_LOCKED`; the entry row carries `recurringIncomeId`; the date chip is locked with a short reason). Amount, note, delete and restore still work. *Why:* the catch-up continues from the period of the latest entry, so a moved date made it duplicate or skip a month. *Rejected:* remembering each entry's original due day in a new column (more work for a rare edit).
  - **"In 3 months / 6 months / 1 year" means exactly 3 / 6 / 12 payments:** the end date is the day before start + N months (the end date itself is a payment day on the server). The end is recalculated when the start changes after such an option was picked. *Rejected:* keeping start + N months and renaming the options "Until …".
  - **The latest repeating start gets the same 1-day time-zone tolerance as entries** (users ahead of UTC just after midnight).
  - **Migration `PersonalEntryOriginRules`:** the database also refuses income with a category, a generated entry that is not income, and an entry with neither a request id nor a repeating income.
  - **A failed catch-up is shown, not hidden** (Filip's "no silent fallbacks" rule): a short error toast, with the real error logged.
  - **The website tolerates an API without `hasEntries`** (Cloudflare publishes before Render finishes deploying): missing means unknown, the start is not locked, and the server still refuses a wrong change.
  - Cleanups: one generic "run once more" helper, one shared add path for spending and income, a neutral type for the shared amount/date/note checks, one shared shrink-to-fit piece, `shortestMonthDays`.
  - **Choices from the frontend fix run (Filip: "your recommendations", 2026-10-24):**
    - The plain "Spent" line of a currency with no income shows the word above the amount, centred (the shrink-to-fit amount needs the full width; a visible change from the approved side-by-side look).
    - The "+" and the number of an amount are one unbroken piece (no non-breaking space, which would show a gap); a wrap can only happen before the currency label; the amount never shrinks below 0.75 of its normal size.
    - The locked chip keeps the faded disabled look; its contrast is measured by the tester and made less faded if it is under 4.5:1.
    - The saved-copy version (`savedQueryCache`) went from '1' to '2' so an older saved copy cannot show a locked date by mistake; everyone's saved copy is thrown away once after the deploy.
    - Euro amounts have no space before the label; a very long euro amount at the smallest size could overflow a narrow box (rare; checked by the tester).
    - `hasEntries` has a third value, "not known" (the older API): only a clear yes locks the start. `recurringIncomeId` missing means an entry the person added.
    - The "Stopped · Undo" toast and the list reload still happen when the sheet was closed before the server answered.
- Future and planned payments stay in the BACKLOG, not in this phase.
- The Home "+" list gets no "Income" item for now.
- Lazy generation is triggered by `POST /api/personal/recurring-incomes/catch-up` when the Budget tab opens and after a repeating income is saved (no GET ever changes anything). A created entry saves the newest stored rate on or before its date (the oldest stored one if none), and generation never calls NBRM.

## 2026-10-20: Phase 13 (personal spending + custom categories): done, the rules that stay
The whole Phase 13 decision log (Filip's mockup picks, the Step 1 to 6 notes, the browser-pass choices, the code review fixes, rejected alternatives) moved word for word to the end of `reports/2026-10-20-phase-13-personal-spending.md`, section "Decisions and rejected alternatives". What stays true:
- **Guests have no Budget:** no tab, no Home item, `/budget` redirects a guest to Settings, the API answers 403 `GUEST_NOT_ALLOWED` on every personal endpoint (personal data lives on the user, and a guest on a lost phone could not reach it).
- **Personal spending:** tap a category, type the amount, Save (Budget "+": 2 taps + 1 field; Home "+" > "Personal spending": 4 taps + 1 field). Integer minor units, MKD and EUR never added, the exchange rate saved with each entry, `clientRequestId` makes a resend safe, soft delete with Undo (5 days), tap an entry to edit it. Dates: the last 24 months up to today. No activity or usage events (private data).
- **Custom categories are personal only:** name 1 to 30 characters, an emoji, one of the 10 colour tokens, unique per person among active ones (also in the database); archive and bring back, never delete; group expenses and `GET /api/categories` keep the built-ins only. Edit them from "Edit my categories" in the category grid.
- **Budget tab look (Filip's picks):** month strip of the last 24 months, a centred "Spent this month" card (EUR line without a "+"), category tiles with totals (one group per currency), the entries list, a round "+".
- **Default currency of a new entry = the currency of the last saved one** (MKD the first time), kept per person in the browser.
- **Sheets:** every `KvitSheet` has a maximum height, a fixed title and a scrolling body, and keeps Save and Delete visible; focus returns to the opener on close.
- **Not in Phase 13 (later phases):** income (14), budget share of group expenses, budget currency, limits, monthly summary (15), duplicates and group totals (16), future and planned payments (BACKLOG).
- **Rate lookup last, also for expenses (2026-10-20, Filip: "fix it"):** `AddExpense` and `CreateOneBill` now look up the exchange rate last, inside the transaction, as personal entries do, so a resend of a saved expense answers without asking the rate source. Correction: before this a resend did not fail when the source was down (the last saved rate is kept); it only asked for nothing and could wait up to the 10-second timeout. Side effects: a stale-rate refresh happens inside the open transaction (up to 10 s), and a refused request no longer refreshes the stored rate.
- **Date hint (browser re-check 3, Bug 10):** the personal date sheet shows its own hint "Pick a date from the last 24 months, up to today." (`budget.entries.dateHint`, Macedonian draft); the expense hint is unchanged. The 24 is written into the text, as in the date error text.

## 2026-10-16: Phase 12b (join a group without an account), in progress
Release 1 is accepted (Filip, 2026-10-16). Filip put the BACKLOG "must do" (join without an account) **before** Phase 13. Branch `feat/12b-guest-join`. Plan: `C:\Users\Davchev\.claude\plans\radiant-toasting-kay.md`.
- **A guest is a light account:** a normal `users` row with no email and no password (`is_guest = true`), signed in with the same 90-day login cookie. "The phone remembers you" = the cookie. Every group, expense, payment and closing rule works unchanged because a guest has a real user id. *Rejected:* a separate guest-token system (a second auth path through every endpoint); plain-name rows only (a guest could not add expenses or confirm).
- **Joining:** open the link → tap a free name (**1 tap**) or type a new name (**1 field + 1 tap**). Nothing else. Anyone with the link can tap any free name; the owner can undo a wrong one.
- **Join screen = option B (Filip):** the heading *"Who are you? Tap your name:"*, then one list of the free names with an *"I'm new"* row that opens the name box. *"Have an account? Log in"* is one small link that opens a sheet with **Google** and **Log in with email**; after logging in the person comes back to the join screen (as in Phase 7) and joins as a normal member. *Rejected:* A (chips and box), C (account first, guest small).
- **Lost phone or another browser:** the owner taps **Let in again** on the guest's row; the name becomes a free name and the guest taps it on the new phone. *Rejected:* waiting for the owner's Undo claim only; a guest-to-account upgrade as the only way back.
- **Guest limits:** a guest **cannot create groups and has no One bill**; the New group and One bill buttons are **not shown** to guests (Filip, 2026-10-16: they can see and use the groups they were invited to, and add expenses and payments there). A guest **cannot add plain names**, cannot be made owner (already so for names without an account), deletes and edits only their own expenses (already the rule for everyone). A **Guest** badge shows next to their name.
- **Upgrade is built in this phase:** Settings shows *"You're a guest. Make an account…"*; a guest adds Google or email + password and keeps all groups and money (same user id).
- **Log out as a guest:** a warning sheet first (*"After logging out you can't get back in by yourself. Ask the owner to let you in again."*) with Make an account / Log out anyway / Cancel.
- **Colours:** the guest screens are drawn in the app's own colours. Filip likes a lighter peach for the light theme; that is a separate open decision (top of BACKLOG).
- **Step 1, guest accounts (backend):**
  - `users.is_guest` (default false) with the check `ck_users_guest_has_no_email_or_password`: a guest row never has an email, a normalized email or a password hash. `SignUpMethod.Guest` is stored as the text `Guest` in `usage_events.detail`, which has no check constraint, so no database change for it.
  - **Identity and the missing email:** `RequireUniqueEmail` stays on. `KvitUserValidator` (`Infrastructure/Auth`) replaces Identity's own user validator; it skips the checks only for a user with `IsGuest = true` and runs Identity's normal checks (valid email, unique email, unique user name) for everyone else, so sign-up still answers `AUTH_EMAIL_TAKEN` / `AUTH_EMAIL_INVALID` as before and the unique index `ix_users_normalized_email` still catches two sign-ups at the same moment. A guest's user name is `guest-<user id>`, which can never be an email (no `@`). *Rejected:* `RequireUniqueEmail = false` plus our own email checks (a copy of Identity's rules that can drift); writing the guest row straight into the database without `UserManager` (would copy Identity's security-stamp and normalizing work); a second validator added next to Identity's (Identity runs all of them, so the guest would still be refused).
  - **`POST /api/invites/join-as-guest`** (anonymous): `{ token, claimMemberId?, name?, language, timeZone }`, exactly one of `claimMemberId` / `name`, else 400 `INVITE_GUEST_CHOICE_INVALID` (new code). The answer is the same `{ groupId }` as the normal join; the screen reads `GET /api/me` after it. Order of work in one transaction: `PickGuestName` (domain: the free name's own name, or the typed name if no current member of the group has it, else `MEMBER_NAME_TAKEN`) → `AccountService.CreateGuestAsync` → the unchanged `JoinGroup.Execute` (claim path or new-row path; it refuses Finished / Closing groups) → commit → sign in with the 90-day cookie, as sign-up does. A refusal after the guest row was written rolls the whole transaction back, so no guest is left behind. A guest who taps a name gets that name as their account name. *Rejected:* letting `JoinGroup` call back into the account service (a domain service would then create accounts); signing in inside the transaction (a cookie could be sent for a guest that was then rolled back).
  - A typed name that is invalid answers `AUTH_DISPLAY_NAME_INVALID` (the guest's account name), not `MEMBER_NAME_INVALID`. In a Finished or Closing group a wrong name answers the name error (`MEMBER_NOT_FOUND` / `MEMBER_NAME_TAKEN`) before the group's state, because the name is checked before the guest is created; a correct name answers `GROUP_FINISHED` / `GROUP_CLOSING`.
  - **Rate limits:** the existing "invite" limit (20 per 10 minutes per visitor) plus a guest limit of 10 per 10 minutes per visitor (`GuestJoinLimitFilter`) that, like the sign-up limit, counts only requests that pass the cheap checks (choice, name, language, time zone). The shared filter code moved into `CheckedRequestLimitFilter<TRequest>`; `SignUpLimitFilter` keeps its behaviour.
  - **`GET /api/me`** (and the sign-up and log-in answers): `email` may be null, new field `isGuest`. The frontend `meService` accepts both; nothing else on the frontend changed in this step.
- **Step 2, guest limits and Let in again (backend):**
  - **`GUEST_NOT_ALLOWED` (403)** for Create group, One bill, Add a name and Set password. The refusal lives once, in `AccountRules.CheckIsNotGuest`. Create group, One bill and Set password read "is guest" from the existing `GetMeQuery`; Add a name reads it from the group's roster (`NamedMember.IsGuest`, loaded with the member rows), so the order stays: not a member 404, guest 403, then the group's state, then the name. Set password refuses a guest before the password rule. *Rejected:* a guest claim in the login cookie (goes stale when the guest upgrades in Step 3; the database decides); checking Add a name in its handler through `me` (a guest outside the group would get 403 instead of 404).
  - **Make owner:** `Group.TransferOwnership` now takes the roster row (`NamedMember`) instead of the bare member, so it sees `IsGuest`; a guest answers the existing `MEMBER_NOT_ACCOUNT`. The domain still never reads the database. *Rejected:* an extra `isGuest` flag parameter.
  - **Let in again:** `POST /api/groups/{groupId}/members/{memberId}/let-in-again`, owner only, through `GroupWriteTransaction`. Order: not a member 404 `GROUP_NOT_FOUND`, not the owner 403 `GROUP_NOT_OWNER`, Finished 400 `GROUP_FINISHED` (a running closing is cancelled, like Undo claim), unknown or no longer current row 404 `MEMBER_NOT_FOUND`, a current row that is not a guest's (plain name, full account, the owner) 400 `MEMBER_NOT_GUEST` (new). The same row keeps its id, name, expenses, payments, pending payments and balance; only its user id and claimed-at are cleared, so it shows as a free name on the invite. The old guest user is left as it is (still signed in on the old phone, no longer in the group). Activity event `GuestLetInAgain` with the guest's shown name; migration `GuestLetInAgain` only widens `ck_activity_events_type`. *Rejected:* removing the guest's row and adding a new plain name (history and balance would split over two rows); giving the old guest a row back as Undo claim does (the old guest is the lost phone).
  - **The Activity tab sentence came with the event,** because an event type without a sentence stops the Activity tab: EN "{{name}} let “{{member}}” in again", MK draft «{{name}} го/ја пушти „{{member}}“ повторно во групата».
  - **Members list:** every member row has `isGuest` (frontend `membersService` type only, no screen yet).
  - **Two guests tap the same free name at the same moment:** `group_members.user_id` is an EF concurrency token, so the claim's save only changes the row while its account is still the one that was read (`WHERE … user_id IS NULL`). The losing save changes nothing; `SaveChangesOrReportDuplicateAsync` turns that into `MemberChangedMeanwhileException`, rolls back (the new guest user too) and join-as-guest runs once more, where the normal check answers 404 `MEMBER_NOT_FOUND`, the same answer as tapping a name someone took a second earlier. This is the Phase 8 pattern (database guard, specific exception, run once more). The token needs no migration (no column changes). *Rejected:* a row lock (`SELECT … FOR UPDATE`, a second pattern beside Phase 8's); a hand-written conditional UPDATE (bypasses the entity's own `Claim`). Side effect: any other save of a member row whose account changed in between now fails loudly (500) instead of overwriting it silently; only join-as-guest runs once more.
  - **Join as a guest while signed in:** 409 `GUEST_JOIN_SIGNED_IN` (new `Result.Conflict`), checked first, before the choice and before any guest is made. A signed-in guest joins another group through the normal join. The guest rate-limit tests now use one browser per guest on one address (a family on one Wi-Fi), because one browser can no longer make a second guest.
  - EN and MK texts for `GUEST_NOT_ALLOWED`, `GUEST_JOIN_SIGNED_IN` and `MEMBER_NOT_GUEST`; the Macedonian is a draft until Filip approves it.
- **Step 3, a guest makes a real account (backend):**
  - **Same row, nothing moves:** the guest's own `users` row gets the email (and user name = the email, as every account), a password hash or a Google login, and `is_guest = false`. The user id, the name the guest picked, the language, the time zone, and every member row, expense, payment and balance stay exactly as they were. No activity event in the group (the group sees no change).
  - **`POST /api/auth/upgrade`** `{ email, password }` and **`POST /api/auth/google/upgrade`** `{ idToken }`, both for a signed-in person only, both rate-limited by `"log-in"`, both answer the same `MeResponse` as sign-up and log-in (`isGuest` false, `email` set) and sign in again with a fresh 90-day cookie (the security stamp changes, and Kvit checks it on every request, so the old cookie stops working at once).
  - **Order of checks:** not signed in 401 → not a guest **409 `AUTH_NOT_GUEST`** (new code, `AccountRules.CheckIsGuest`, also checked again inside the transaction) → email (`AUTH_EMAIL_INVALID`) → password (`AUTH_PASSWORD_TOO_WEAK`), the same `AccountRules` checks as sign-up → email taken. Google: not a guest 409 → token (`AUTH_GOOGLE_TOKEN_INVALID`) → verified email (`AUTH_GOOGLE_EMAIL_NOT_VERIFIED`) → email shape → this Google account already linked to someone else → email taken. A person with a full account (password or Google-only) gets 409 and nothing changes, so the upgrade can never add a password to a Google account or replace an email.
  - **No half-guest row:** `is_guest = false` and the email are set in the same save as the password hash (`AddPasswordAsync`) or the new security stamp (`UpdateSecurityStampAsync`, Google path), so `ck_users_guest_has_no_email_or_password` is never broken mid-save. The Google login is linked in a second save inside the same transaction.
  - **Email taken, also at the same moment:** the email check of `KvitUserValidator` (Identity's own) answers `AUTH_EMAIL_TAKEN`; when two guests send the same email at the same moment, the unique index `ix_users_normalized_email` refuses the second save and `AccountService.IsEmailUniqueViolation` turns it into the same answer; the loser stays a guest and stays signed in. Proven with a gate on the `users` UPDATE (`InsertGate.CloseUpdatesAsync`, a test helper), not by timing.
  - **Google codes, chosen to match Phase 6 (deviation from the step text, which said `AUTH_EMAIL_TAKEN`):** a Google email that already has an account answers **`AUTH_GOOGLE_EMAIL_TAKEN`** (400), the same code Google sign-up already uses and the one the Google button hook (`useGoogleSignIn` `onEmailTaken`) already handles with the Phase 6 pop-up "This email already has an account." A Google account already linked to another Kvit user also answers `AUTH_GOOGLE_EMAIL_TAKEN`: Google sign-in has no error for that case (it simply logs that user in), and logging the guest into another account would silently leave the guest's groups behind. No automatic merge (BACKLOG). *Rejected:* `AUTH_EMAIL_TAKEN` on the Google path (a second code for the same Google situation); a new `AUTH_GOOGLE_ALREADY_LINKED` (a new frontend text for a case that in practice always also has the email taken); signing the guest into the linked account.
  - **A bad Google token on the upgrade answers 400, not 401** (same code `AUTH_GOOGLE_TOKEN_INVALID`). The frontend treats every 401 as "signed out" (`updateMeAfterFailure`), and a guest shown the signed-out screen cannot get back in alone. Google log-in and sign-up keep 401. *Rejected:* 401 plus a frontend exception for this one code (a rule hidden in the frontend that the next endpoint would miss).
  - **Picture:** the Google path stores the Google picture, as Google sign-up does; the name stays the guest's own (not the Google name).
  - **Usage event `GuestUpgraded`** with detail `Email` or `Google`, so the admin page can count guests who made an account without counting them twice as sign-ups (`SignedUp` already happened with detail `Guest`). Migration `GuestUpgradedUsageEvent` only widens `ck_usage_events_type`. *Rejected:* a second `SignedUp` (double-counts people); no event (the Release 3 statistics would lose how many guests convert).
  - **`AUTH_NOT_GUEST`** EN "You already have an account." MK draft «Веќе имаш профил.» (draft until Filip approves it; the `errors` texts are not locked in `approvedTexts.test.ts`, as for the Step 2 codes).
  - **Known gap (NOT VERIFIED, not built):** the same guest sending two upgrades at the very same moment (a double tap). The second save finds the row already changed (Identity's concurrency stamp) and Identity answers `ConcurrencyFailure`, which Kvit does not map, so that second request answers 500 while the first one succeeded. The Step 6 screen should disable its button while the request runs.
- **Step 4, the join screen for a visitor without an account (frontend):**
  - **Screen B as built:** under the invite card, the heading *"Who are you? Tap your name:"*, one card with a row per free name (initial circle, the name, an arrow; at least 56 px high; long names wrap anywhere) and an *"I'm new"* row last. Tapping a name joins at once (**1 tap**); the tapped row shows the spinner while the server answers, because a sleeping Render can take about 20 s. *"I'm new"* opens one name field with the focus already in it, so it is tap, type, tap (Join or the keyboard's Go). With no free names the list has only the *"I'm new"* row and the heading becomes *"What's your name?"* (kept as the step said; opening the field straight away would save one tap, a question for Filip). Under it *"Have an account? Log in"*.
  - **Log in sheet:** the glass `KvitSheet` with the existing Google button (the existing `useGoogleSignIn(token)` round trip, so the person comes back to this screen signed in and sees the normal Join), *"Log in with email"* (opens `/login` with `joinTokenState`), Cancel and the note. The *"Sign up with email"* link and the old hint *"Create an account or log in to join."* (`join.signInHint`, removed from both languages) are gone from this screen; sign-up is still reachable from the log-in screen's *"I don't have an account"*, and every route stays.
  - **Texts:** new keys under `join.guest` (Macedonian drafts). Reused: `auth.logIn.title` (the link and the sheet title), `auth.signUp.name` (the field label), `join.join`, `join.joined` (the toast), `settlements.cancel` (as the closing Object sheet already does). The guest row has its own *"I'm new"* key because the approved `join.imNew` *"No, I'm new"* answers the signed-in question, which stays unchanged.
  - **The request:** `invitesService.joinAsGuest` sends the token, either `claimMemberId` or `name`, the screen's language and the device time zone. The time-zone reading moved from `authService` into `core/services/deviceTimeZone.ts` so both use the same one. The typed name is sent as typed; the server trims and checks it (no length rule in the browser, like every other form).
  - **After joining:** the join answers only `{ groupId }`, so the hook asks `GET /api/me`, puts the guest into the `me` cache directly (as sign-up does: the guest's language is the screen's language, so no language switch), marks the invite preview as old without asking it again, refreshes the group lists, shows the Joined toast and opens the group with `flushSync`, so the group page is drawn in the same step as the switch to the guest. All of it runs in the hook's own `onSuccess`, which runs even if the screen is gone by then. Switching from "nobody" to the guest forgets nothing, so the one-person cache rule holds (it only clears when one person changes to another). *Rejected:* `useFinishLogIn` (its wait for the language switch lets the screen redraw as signed in before it leaves); asking the preview again at once (it would answer "already a member" and race the toast); a normal navigation (React draws it as a transition, so the signed-in Join card could flash for a frame).
  - **Errors:** a refused tap shows the translated message above the list; a refused typed name shows it under the field, and the field gets the red edge for `AUTH_DISPLAY_NAME_INVALID` and `MEMBER_NAME_TAKEN` (with the typed name in the text) until the person types again. `MEMBER_NOT_FOUND` (someone took the name a moment earlier) also asks the invite again, so that name disappears from the list. `GROUP_CLOSING` / `GROUP_FINISHED` show the existing message with Home, as the signed-in join does. `GUEST_JOIN_SIGNED_IN` shows an error toast and asks again who is signed in and the invite, so the screen switches to that account's normal Join (*rejected:* an inline message, which would vanish with the screen switch). `RATE_LIMITED` does not ask the invite again (that would be refused too). A join followed by "nobody is signed in" shows the generic message instead of pretending it worked.
  - **Double taps:** every name, *"I'm new"*, Join and Log in are disabled while a join waits for its answer.
- **Step 5, a guest inside the app (frontend; Filip was away, so the safest option was picked and logged here):**
  - **One way to ask "am I a guest":** `useIsGuest()` (`core/auth/useIsGuest.ts`) reads the `me` cache; nobody signed in (after Log out) counts as not a guest. Every screen and the route guard use it. *Rejected:* `useSignedInMe().isGuest` (Settings sets `me` to null on Log out and would throw while it is still drawn); a copy of `me?.isGuest === true` in every screen.
  - **Guest badge:** *Guest* / «Гостин» after the Owner and You badges, a dashed outline in the muted text colour (the mockup's look), in the same wrapping line as the name.
  - **Let in again** / «Пушти повторно»: the first button in the owner's options sheet of a guest row, with the help line under it (*"Use this when Marko lost their phone. The name becomes free to tap on the new one."*). **2 taps** (options, Let in again), no "Are you sure?", every button of the sheet disabled while it waits. Success: toast *"Marko can tap their name again"*, the sheet closes, and the same `refreshEveryGroup` as the other member actions asks the members, the activity and the dashboard again. Errors go through the existing translation (`MEMBER_NOT_GUEST`, `GROUP_FINISHED`, `GROUP_CLOSING`, `MEMBER_NOT_FOUND`, …) as an error toast. Closing and Finished follow the existing rule (the options button exists only where the owner can change the group). **Make owner** is hidden for a guest row, as for a plain name. The help line says "their", not the mockup's "his", because the app does not know a person's gender (the same reason as «го/ја»).
  - **Guest limits on screen:** no New group button on Groups (and its empty text becomes *"You have no groups yet. Open a link a friend sent you."*), no New group button in either Home empty state (the hint becomes *"Open an invite link a friend sent you, then add your first expense."*), no *New One bill* in the Home "+" menu or in the desktop round buttons (`homePlusItems` takes `isGuest`), no *+ Add a name* on Members. The desktop left menu has no New group item, so nothing changed there. The *Add a name* button of the group page's empty card is not touched: it shows only when the group has one member, and a group with a guest always has the owner too.
  - **Routes a guest cannot use** (`/groups/new`, `/groups/new/group`, `/groups/new/bill`, `/settings/set-password`, `/change-password`) sit under one new guard, `RequireAccount` (next to `RequireAuth`), which **redirects a guest to Settings** (`replace`), where the guest card says what to do. *Rejected:* a separate "guests can't do this" screen (a new screen and texts for a page no button leads to; Settings already explains it); the mockup's "Make an account first" sheet on a New group button (Filip decided the buttons are not shown at all, so nothing opens it).
  - **Settings for a guest:** the card *"You're a guest"* / *"Make an account to use Kvit on any phone and to create your own groups."* with a main **Make an account** button sits right under the title (first thing seen, as in the mockup); Change password and Set a password are hidden; language, theme and Privacy stay as they were (Settings has no name editing today, and none was added). **Log out** opens the glass `KvitSheet` *"Log out as a guest?"* with the warning text, **Make an account** (main button, opens the make-account screen without logging out), **Log out anyway** (danger look, the existing log-out) and **Cancel**. A person with an account still logs out with one tap and no sheet.
  - **Make-account route:** `routes.makeAccount` = `/settings/make-account` with a placeholder screen (back button and the title *Make an account* only) so the button works now; **Step 6 fills it in** (and decides what a non-guest sees there).
  - **Join screen, no free names (Filip's speed rule, refines Step 4):** the list and the *"I'm new"* row are not drawn at all; under *"What's your name?"* the name field is open at once with the cursor in it, so joining is **1 field + 1 tap**. This also happens when the last free name is taken while the screen is open (the invite is asked again and comes back empty). *Rejected:* keeping the one-row list (an extra tap for nothing).
  - **Activity line when a guest taps a free name:** the server records it as `MemberClaimed` with the same name twice (the guest's account name is the tapped name), which read *"Marko took the name “Marko”"*. When the two names are exactly the same, the line now uses the existing *"Marko joined"* (`activity.memberJoined`) text; when they differ (also only in letter case) it stays *"Petar took the name “Darko”"*. The dashboard's activity uses the same function, so it reads the same there. *Rejected:* a new event type on the server (old events would still read badly); comparing without letter case ("marko" and "Marko" are written differently, so the "took the name" line still tells something).
  - **Texts:** new keys `members.guest`, `members.letInAgain`, `members.letInAgainHint`, `members.letInAgainDone`, `settings.guest.*`, `groups.emptyGuest`, `home.noGroupsHintGuest`, pinned in `guestAppTexts.test.ts`. The English follows the mockup; the **Macedonian is a draft** until Filip approves it, written in the app's «ти» form (the mockup used «Вие») and «профил» for account, with «сам/а» and «загубил/а» because the gender is unknown. Reused: `settlements.cancel` (Cancel, as the Step 4 sheet does).
- **Step 6, the make-account screen (frontend; Filip was away, so the safest option was picked and logged here):**
  - **The screen** `/settings/make-account` is a normal full screen (back arrow to Settings, capped at 40rem on a desktop like every other screen): the title *Make an account*, the existing Google button, *or*, Email, Password (with the eye and the sign-up password rule under it), **Save**, and under Save *"You keep all your groups and expenses."* The name is not asked; the guest keeps their own. **Email path: 2 fields + 1 tap. Google: 1 tap.** The Google button sits on the screen itself, not in a sheet, so it measures its width once the page is drawn (as on Welcome).
  - **Google calls the upgrade path, never Google sign-in:** the button is shared, but the screen gives it its own hook `useUpgradeGuest` (`features/auth/makeAccount/hooks`), which sends `POST /api/auth/google/upgrade` with only `{ idToken }`; the email path sends `POST /api/auth/upgrade` with only `{ email, password }` (service functions `upgradeGuest` / `googleUpgradeGuest` in `authService`). *Rejected:* a parameter on `useGoogleSignIn` that switches it to the upgrade (a flag that changes what the hook does).
  - **After success:** the returned person goes straight into the `me` cache (same user id, so the one-person cache rule forgets nothing), the group lists are marked as old with the existing `refreshEveryGroup` (only what is on screen is asked again, so the members list shows the account on its next load), the toast *"Your account is ready"* / «Профилот е готов», and Settings opens with `replace` (Back does not lead to the make-account screen). Settings then shows the normal account view. No language switch: the upgrade keeps the guest's language.
  - **Double tap:** a double tap on Save, a double send from the keyboard, or Save while a Google upgrade waits (and the other way round) sends **one** request. A flag in the hook is set the moment a request starts and cleared when it ends, because the button's disabled state only takes effect a moment later; Save is also disabled while either request waits. This closes the Step 3 server gap on the screen side. *Rejected:* relying on the disabled button only (two taps in the same moment both get through); a server change (out of scope for a frontend step).
  - **Errors:** `AUTH_EMAIL_INVALID`, `AUTH_EMAIL_TAKEN`, `AUTH_PASSWORD_TOO_WEAK`, `RATE_LIMITED` and a network failure show the translated text above Save, as Sign up does, with the typed values kept. The field the code is about also gets the red edge (email for the two email codes, password for the password code) until the person types in that field again (the Step 4 pattern). `KvitEmailField` and `KvitPasswordField` gained the optional `invalid` that `KvitTextField` already had. Google errors (`AUTH_GOOGLE_TOKEN_INVALID`, which answers 400 here so the guest is never signed out, `AUTH_GOOGLE_EMAIL_NOT_VERIFIED`, `RATE_LIMITED`) show as an error toast, as the Google button does everywhere else.
  - **`AUTH_GOOGLE_EMAIL_TAKEN`** opens the Phase 6 pop-up *"This email already has an account."* with *Use another Google account* (the second button also upgrades) and **Close**, but **without "Log in with password"**: logging in would switch the phone to the other account and leave the guest's groups behind (no merge, BACKLOG). `GoogleEmailTakenDialog` got an optional `offersPasswordLogIn` (on by default, so Welcome is unchanged). *Rejected:* keeping the log-in button (the guest would quietly lose their groups on this phone); a new pop-up (a second copy of the same text and button).
  - **`AUTH_NOT_GUEST`** (the account was already made, for example in another tab): an error toast *"You already have an account."*, `me` is asked again, and Settings opens with `replace`, showing the account view.
  - **A person who is not a guest** opening `/settings/make-account` is sent to Settings at once (`replace`), with nothing sent. The check sits in the screen, as Set a password does for a person who already has a password. *Rejected:* a new route guard `RequireGuest` next to `RequireAccount` (a new component for one route).
  - **Reuse:** the *or* line became `KvitOrDivider` (shared), used by Welcome and this screen, so the look exists once. Save reuses `groupSettings.save` (*Save* / «Зачувај», the mockup's word); title `settings.guest.makeAccount`; fields `auth.email`, `auth.password`, `auth.passwordHint`. New keys `auth.makeAccount.keeps` (MK draft «Ги задржуваш сите групи и трошоци.») and `auth.makeAccount.done`, pinned in `guestAppTexts.test.ts`; the Macedonian is a draft until Filip approves it.
- **Fixes from the browser pass (2026-10-19, `reports/2026-10-19-phase-12b-browser-pass.md` Bugs 1 to 3):**
  - **A guest who taps a free name writes only `MemberJoined`** (Bug 1, "Marko joined" twice). The claim itself is unchanged (the row gets the guest's user id and claimed-at), only the second event is gone. `JoinGroup` has two entry points: `Execute` (a signed-in person: "Are you one of these?" still writes `MemberJoined` + `MemberClaimed`, with the existing lines) and `ExecuteForGuest` (called only by `JoinAsGuestCommandHandler`; a guest's name is always the tapped name, see `PickGuestName`, so the claim event would only repeat the joined line). Both share the private join; the difference is one private parameter. Nothing reads `MemberClaimed` to find the claimer: Undo claim and Let in again work from the row (`user_id`, `claimed_at`), checked with a new API test (Undo claim on a guest who tapped a name). **Old events stay as they are:** a guest tap stored before this fix keeps both rows and still shows two "joined" lines; the Step 5 same-name rule in `activityLines.ts` stays for those and for signed-in same-name claims. *Rejected:* hiding a same-name `MemberClaimed` next to a `MemberJoined` on the frontend (it would hide stored events and needs pairing logic across pages of the feed); a public `isGuest` flag on `Execute` (a flag that switches what the method does; the Make owner decision rejected the same); dropping the claim event for every same-name claim, also signed-in (out of the step's scope; see the open question below).
  - **Open question for Filip (not built):** a signed-in person whose account name is "Marko" who joins and picks the plain name "Marko" in "Are you one of these?" still gets "Marko joined" twice, as before. Same fix possible on that path if Filip wants it.
  - **Members: no "Took the name" line when the taken name is the person's own name** (Bug 2). The rule is "the taken name equals the shown name", not "is a guest", so the line also stays away after that guest makes an account (same name, no longer a guest). Petar who took "Darko" still shows *Took the name "Darko"*. *Rejected:* checking `isGuest` (the line would come back after the upgrade).
  - **Invite card: long names wrap** (Bug 3, a 60-letter name made the join page 774 px wide on a 360 px phone). The heading has `wrap-anywhere`, as the group screen's title. Each member name sits in an `inline-block max-w-full wrap-anywhere` box with its comma inside: a name that fits on a line still never splits (the Phase 7 fix for "Filip / Davchev"), and only a name longer than a whole line breaks; normal names lay out as before. The comma has to sit inside the box because Chrome will start a line with a comma that follows an inline-block. *Rejected:* `wrap-anywhere` without the box (multi-word names would split across lines again); non-breaking spaces in the names (changes the text people copy, and hyphens would still break). The Members screen and the Home greeting keep their long-name problem (older, going to BACKLOG).
- **Fixes from the code review (2026-10-19):**
  - **Every change of an existing member row runs once more when its account changed meanwhile,** not only join-as-guest (the Step 2 side effect, which left a raw 500 in a same-moment race). The signed-in join (claiming a free name or coming back on a left row), "That's me", Undo claim, Let in again, Leave, Remove and Let back in now go through `GroupWriteTransaction.RunOnceMoreIfMemberChangedMeanwhileAsync`, the group twin of `RunForInviteOnceMoreIfMemberChangedMeanwhileAsync`; both call the existing `RunInTransactionOnceMoreIfMemberChangedMeanwhileAsync`, so there is still one pattern. The second run's normal checks give the answer: two people claim the same free name at the same moment (join or "That's me") → the later one gets 404 `MEMBER_NOT_FOUND` and keeps their own row; Let in again while Undo claim runs on the same guest → 400 `MEMBER_NOT_GUEST`; a guest leaves while the owner lets them in again → 404 `GROUP_NOT_FOUND`; the owner removes a free name while someone joins on it → 204 and the row is removed together with the person who just took it, the same answer as tapping Remove a second later. `RunForInviteAsync` lost its last caller and is deleted. *Rejected:* building the retry into `RunAsync` for every group write (expense and settlement writes never change a member row, and a hidden catch there would run them twice for nothing); a row lock (already rejected in Step 2).
  - **Not changed on purpose:** Make owner (it changes only the group row), Add a name, Create group and One bill (they only add new member rows, and the account check never runs on an added row). Running once more could never be needed there.
  - **Race tests:** `MemberChangedMeanwhileTests` holds real requests with `InsertGate` at the `activity_events` insert, which EF sends before the member row's update in the same save, so both requests have read the row before either changes it. A gate on the member row's own update cannot hold two requests: Postgres locks the row before a BEFORE UPDATE trigger runs, so the second request waits on the row, not on the gate.
  - **Known gap (not built, question for Filip):** the check covers only the account (`user_id`), not the end of a membership (`removed_at`). So in the other order of the same races nothing fails, and the later write lands on a row that changed: a join or "That's me" can claim a free name the owner removed a moment earlier (the person gets "Joined" but is then shown as removed), and Let in again can turn a row the guest just left into a free name that is still removed. Not a 500 and very unlikely (two people in the same fraction of a second). Possible fix: make `removed_at` a concurrency token too (no migration), then the same once-more run answers the clean error.
  - **Members: a guest row shows only Let in again, never Undo claim** (also when the guest tapped a free name, so `claimedName` is set). Both buttons freed the name, but Undo claim also gives the guest a new own row, so the lost phone would stay in the group; one button leaves no wrong choice. Undo claim stays for every claimed row of a person with an account, also a former guest who made an account. Taps unchanged: options (1) + Let in again (1). *Rejected:* keeping both buttons (two near-identical actions, and the owner could tap both at once, the race above).

## 2026-10-16: Phase 12 (animations + touch-ups): done, the rules that stay
The whole Phase 12 decision log (Filip's answers, the colour, glass and desktop picks, the Step 7 and browser-pass choices, rejected alternatives) moved word for word to the end of `reports/2026-10-16-phase-12-polish.md`, section "Decisions and rejected alternatives". What stays true:
- **Dark mode uses solid colours,** not `color-mix`: background `#1f1816`, field `#161110`, card/sheet `#2c2220`, line `#72625c`, raised line `#8d7d77`. The selected pill is tan `#d9b48a` with dark text everywhere in dark (bottom bar, left menu, group tabs, Settings switches).
- **Motion:** only transform and opacity, about 0.15 to 0.25 s, everything off under `prefers-reduced-motion`, never blocks a tap. Lists animate with AutoAnimate (our `useListAnimation`), loading shows pulse skeletons, amounts roll with NumberFlow (display only, never typing).
- **Popups use the full glass look** (blur 14 px, 90% card tint, bright edge, solid fallback); text stays at 4.5:1 and edges at 3:1 over any backdrop, also on hover. Fields inside glass use the lighter edge.
- **Currency labels:** English "MKD", Macedonian "ден." (chips, size line, amounts); the value sent to the server stays the code. MKD is whole denars only.
- **Desktop (64rem = about 1024 px and wider) is a separate layout; the phone layout never changes.** A soft left menu on every signed-in screen, wide Home and group page, other screens capped at 40rem, the closing bar at most 40rem, a round "Add expense" button instead of the floating "+" on the group page. Filip may change the desktop look later.
- **"Updating…" sits at the top of the screen** (not above the tab bar).
- **Words:** English "Stay", Macedonian «Смештај», settings theme «Тема».

## 2026-10-16: Phase 11 (dashboard + Release 1): done, the rules that stay
The whole Phase 11 decision log (Filip's answers, the Step 2 to 6 choices, rejected alternatives) moved word for word to the end of `reports/2026-10-16-phase-11-dashboard.md`, section "Decisions and rejected alternatives". What stays true:
- **Home is layout C:** the "You are owed" and "You owe" boxes (MKD and EUR apart, two different colours), a pinned "Needs you" bar (the count only in the circle), groups as cards, Recent activity = everyone's last 5 actions.
- **"Needs you" is answered on Home** (payments: Confirm / Reject; closing: Confirm / Object). Closing items come first, then payments, oldest first.
- **The "+" opens a short list:** "Add to <group>" (the Open normal group of the caller's newest own expense, else the one joined last; never a One bill), "New One bill", "Another group…". No "New group" item: a group is made in the Groups section (the empty Home keeps its New group button). Adding from Home is 3 taps, which Filip accepted.
- **A person whose groups are all Finished** sees a compact "No open groups right now" card with a New group button above Recent activity; the full "No groups yet" screen only when there are no groups and no activity.
- **`Active` usage event:** one row per user per UTC day (`POST /api/me/active`).
- **Toasts sit above the closing bar** while it is on screen. The dark `color-mix` surfaces are reconsidered in Phase 12.

## 2026-10-15: Phase 10 (finishing groups): done, the rules that stay
The whole Phase 10 decision log (Filip's answers, the Step 1 to 3 choices, the live-check fixes, rejected alternatives) moved word for word to the end of `reports/2026-10-15-phase-10-finish-groups.md`, section "Decisions and rejected alternatives". What stays true:
- **Closing:** the owner starts it when every balance is 0, nothing is pending and the group has at least one counted expense (a One bill never closes by hand). Every member with an account except the owner confirms or objects (optional reason, 200 characters). Everyone confirmed, or 24 hours with no objection (applied lazily on the next write, GET never writes), means Finished; an objection returns the group to Open. Only the owner can Unlock a Finished group.
- **Frozen and read-only:** a Finished group refuses every write except Unlock, Leave, Delete group and Restore group; while Closing, members can only Confirm or Object (no leave, claim or join). The owner's money changes and member changes (remove, let back in, make owner, undo claim) cancel a closing; rename, emoji, currency, add plain name and invite reset do not. Check order: not a member 404, who-may-act 403, then the state (`GROUP_FINISHED` / `GROUP_CLOSING`).
- **Joining:** refused for non-members while Closing or Finished; a current member just opens the group.
- **One bill** finishes by itself after a money write when it has a counted expense, a Confirmed payment, all balances 0 and nothing Pending; normal groups never auto-finish.
- **Screen 17:** a floating bottom bar that stays until answered, in five states; the "+" sits above it. In Phase 11 Filip wants it as a popup like the Undo toast (BACKLOG).
- **Looks:** text contrast at least 4.5:1 everywhere, also on the glass buttons in dark mode (measured live); the danger glass look uses cream text in dark mode.

## 2026-10-10: Phase 9 (balances + settle up), in progress
Filip's answers (2026-10-09/10): a **pending payment also blocks** leave, remove and delete group (not only a non-zero balance); **overpaying is allowed** (no cap, a partial amount too); **reject has no reason field**.
- **Step 1, balances:** `GET /api/groups/{groupId}/balances` returns, per currency (MKD before EUR, only currencies a counted row uses), every member the group ever had in joining order (`isCurrent` false once `removed_at` is set), their balance in minor units, the simplified payments, and `isEveryoneKvit`. Any current member may read it. Rows left behind by "That's me" (`SetAside`) currently show as extra 0 rows; hiding them is not decided yet.
- **Step 2, settlements (backend):**
  - **Who acts:** the caller's own row is `to` -> Confirmed at once; own row is `from` -> Pending; otherwise only the owner, for a plain name (`to` plain -> Confirmed, else `from` plain -> Pending); anything else 403 `SETTLEMENT_NOT_ALLOWED`. The own-row rule wins over the owner rule, with one exception added in Step 4b: the owner paying a plain name is Confirmed at once (see "Step 4b decisions").
  - Confirm and reject: the receiver, or the owner when the receiver is a plain name. Cancel: the payer, or the owner when the payer is a plain name. Delete (Confirmed only) and restore: whoever recorded it, or the owner. **Undo of a delete has no time limit.**
  - A payment confirmed at once also gets `resolved_by_user_id` (the recorder) and `resolved_at` (creation time). Rejected alternative: leave them empty.
  - Both people must be current members. Duplicate protection like expenses: same `clientRequestId` returns the first payment; two at the same moment both answer 200; another person's or another group's id answers 400 `SETTLEMENT_CLIENT_REQUEST_ID_USED`.
  - **Events:** recording writes `SettlementRecorded` only; a later confirm writes `SettlementConfirmed`. New type `SettlementRestored` (Undo of a delete). The activity sentences are written on the website in Step 4. One `SettlementConfirmed` usage event when a payment becomes Confirmed.
  - Check order for confirm, reject, cancel, delete, restore: who may act is checked before the payment's state (403 before 400), like expenses.
  - Migration `Settlements`: the table, four check constraints, a unique index on `client_request_id`, and `SettlementRestored` allowed in the activity type check.
  - 11 `SETTLEMENT_*` error texts added in EN and MK; the Macedonian is a draft until Filip approves it.
- **Step 3, zero-balance rules, claim rule, balances list:**
  - **Leave, remove, delete group** are refused (400) while the affected person's balance in any currency is not 0 (either sign), or a non-deleted Pending payment involves them. Delete group looks at every member row ever (left, removed, plain names) and any Pending payment in the group. Rejected, Cancelled and deleted payments never block; Confirmed ones count only through the balance. The balance reason wins over the pending one. The check runs after all existing checks and codes. Restore group never checks balances. Codes: `MEMBER_BALANCE_NOT_ZERO`, `MEMBER_HAS_PENDING_PAYMENT`, `GROUP_BALANCES_NOT_ZERO`, `GROUP_HAS_PENDING_PAYMENT`.
  - **One definition of a balance:** a pure domain rule, `GroupMoney`, calls `Balances.Calculate` with every member row ever; one loader (`BalanceQueries`) decides which expenses and payments count, shared by the balances endpoint and these checks.
  - **A claimed name keeps its balance:** whoever claims an indebted name cannot leave until it is settled.
  - **"That's me" counts payments:** the caller's own row being payer or receiver of any payment (every status, deleted ones too, like expenses today) blocks the claim and turns `canClaimNames` false. A plain name with only payments stays claimable and its payments stay on the name. Rejected: letting deleted payments through (Undo has no time limit, so a restored payment could land on a hidden row).
  - **Balances list hides `SetAside` rows** (left behind by "That's me"). They are filtered out BEFORE `Balances.Calculate`, so if such a row ever held money the call fails loudly. Left and Removed rows still show with `isCurrent` false.
  - EN and MK texts for the 4 new codes added. «салдо» ("balance") is already used in the app (privacy page, Home placeholder); Filip chose to keep it everywhere (2026-10-11), so those 4 texts are approved.
- **Step 4a, Balances tab (frontend, read-only):**
  - **Design:** Filip approved the B3 mockup (`docs/design/2026-10-11-balances/index.html`): Needs you first, then the balance card with the payment rows inside it, then Between others, the "Simplified" line, Everyone, Record another payment. Glass look (translucent in the element's own colour, blur, soft edge highlight) on the + button, Mark paid, Confirm, Reject, Save and the payment sheet; Filip wants the same glass on every pop-up in the animation pass.
  - **One card per currency** (MKD first), the currency code in the title only with two or more cards. English says "says **they** paid you" (the app does not know anyone's gender; Macedonian uses «платил/а»).
  - **Row rules:** a suggested payment is hidden when any Pending settlement exists with the same from, to and currency; every Pending payment I made as payer gets its own "You paid … / Waiting for … to confirm" row, even when no suggestion matches, in a card made for its currency if needed; a Pending payment waiting for my answer (receiver, or owner for a name-only receiver) lives only in Needs you with its own amount. The kvit message shows only when the server says everyone is kvit and at least one currency exists.
  - **Needs you** holds only things the person must answer; their own waiting payment is not a to-do. "Needs you" for the group stays on the Balances tab; the Home list across groups waits for the Phase 11 dashboard.
  - **Cache:** new keys under `['groups', id, ...]`; every settlement change refreshes balances, payments, activity and expenses; every expense change refreshes balances.
  - **Colours:** `text-positive` / `text-negative` tokens (contrast 4.9:1 to 8.7:1 on the page and card colours).
- **Step 4b decisions (Filip, 2026-10-11, taken from Claude's recommendations):**
  - **The owner paying a plain name is Confirmed at once** ("I paid Grandma" by the owner). Reason: the owner would otherwise have to confirm their own payment, and the same payment showed twice (Needs you and the owner's card). A non-owner paying a plain name, and the owner paying an account member, stay Pending. One condition in `Settlement.StatusWhenRecordedBy`; backend tests updated first.
  - **Deleting a mistaken confirmed payment:** a **Payments** list at the bottom of the Balances tab (Confirmed payments, newest first, **Delete** for whoever recorded it and the owner, Undo toast). Rejected: deleting from the Activity tab.
  - **The Macedonian drafts of Step 4a** (tab «Салда», «Твое салдо», «Должиш на», «ти должи», «Чека на тебе», the payment sentences, …) were approved by Filip on 2026-10-11 by his instruction to follow Claude's recommendations; he did not read them line by line.
- **Phase 9 code review and Step 4b results (2026-10-13):**
  - **Delete and Restore of a payment need both people to be current members** (`SETTLEMENT_MEMBER_NOT_CURRENT`, the same code Record uses). Reason: leave and remove need a zero balance, so deleting a confirmed payment of someone who already left would leave a balance nobody can settle and block deleting the group. Order of errors: not allowed, member not current, not confirmed / not deleted. Rejected: a new error code; letting the owner delete anyway.
  - **Every payment change also refreshes the members list** (the "That's me" rule depends on payments).
  - **Balances rows are built from the balance list and matched to members by id**; a mismatch throws instead of mislabeling.
  - **The amount number sits on the left in the two payment sheets** (Filip, 2026-10-13), with a small gap from the border. Add expense and One bill keep it on the right; the switch is that the field has no currency toggle.
  - **Left in BACKLOG:** same-moment answers to one pending payment, partial-debt rows, retry after delete, unlimited settlements list.

## 2026-10-09: Phase 8b (saved data shows instantly), in progress
Filip chose to build this before Phase 9 (it was a Phase 11 line) because the Render wake-up made the app look stuck.
- **What:** the TanStack Query cache is saved in the browser (IndexedDB, via `idb-keyval`, packages pinned to 5.103.2 like `react-query`). Opening the app shows the last screens at once and re-checks them with the server in the background.
- **`me` is saved too.** `RequireAuth` blocks every screen until `/api/me` answers, so without it the saved screens would stay hidden behind the spinner. Trade-off (Filip was told it and said to start the work; he did not answer the question separately): for the few seconds before the server answers, a person whose login has expired still sees their last screens (nothing can be saved or changed meanwhile). The first `/api/me` answer then either confirms them or sends them to Welcome and wipes the saved copy. The earlier prompt said not to save `me`; that was wrong.
- **One-person rule** (`core/auth/keepCacheToOnePerson.ts`, the only place that wipes): `me` going from a person to nobody removes all saved data; a different person resets it. Nothing is saved while nobody is signed in.
- **Saved copy:** 24 hours, plus a version string (`savedQueryCacheVersion`); bump it when saved data shapes change.
- **3-second limit** on reading the saved copy, so broken browser storage cannot leave a blank screen. After a timeout the library's cleanup delete is started but not waited for.
- **"Updating…" note** sits above the bottom tab bar on the main tabs only (Home, Groups, Recently deleted, Settings), shown only while the first re-check of a person loaded from the saved copy is running. Screens without the bar (a group opened from a link) do not show it. A toast can cover it.
- **Error text:** 502, 503, 504 and "no answer" all map to `errors.network` ("The server may be waking up. Check your connection and try again in a moment."), so every save path shows it without per-screen code.
- **Still not built:** saving while the server sleeps (the outbox). Saving waits for the server.
- **Rejected:** saving every query without a signed-in check (another person on the same phone could see it); a timer only in the router (queries stay idle until the restore settles).

## 2026-10-09: Phase 8 (expenses): done, the rules that stay
The whole Phase 8 decision log (Filip's product answers, the technical choices, the Step 1 to Step 6b contracts, the code-review fixes, rejected alternatives) moved word for word to the end of `reports/2026-10-09-phase-08-expenses.md`, section "Decisions and rejected alternatives". What stays true:
- **Editing keeps the saved rate** unless the currency changes.
- **Splits** always go through `Splitter.Calculate`, inputs in joining order, `share_minor` stored. Removed or left members stay in old splits; "everyone" in a new expense means current members, plain names included.
- **Duplicate protection:** unique `client_request_id`; the same id again returns the first expense.
- **Expenses list:** the whole list, newest date first, no paging.
- **Change history:** `ExpenseEdited` with `changes` for amount, currency, title, note, date, category, payer and split (the split as one line with old and new shares).
- **Claim rule:** `canClaimNames` and `MEMBER_CANNOT_CLAIM` turn false once the caller's own row is a payer or has a share.
- **Deleted expenses:** a 4-second Undo toast, plus **Recently deleted** for 5 days with Restore (whoever added it, and the owner). Future dates are allowed, up to one year ahead. An expense counts in balances as soon as it is saved, whatever its date.
- **Group tabs:** Expenses · Activity; Phase 9 adds Balances. One bill has the Paid by and Split chips too (me, equally by default).
- **Exchange rate:** NBRM middle rate, refreshed lazily after 8 hours; on failure keep the last rate, show its date, log the error; never a made-up number.
- **People circles:** the Google picture when there is one, otherwise the initial on a colour by place in the group's joining order (ten colours, then repeating).
- **Group screen:** layout B (Filip's final pick) for an empty group: the 🧾, "No expenses yet", the buttons Add a name and Share invite link, two small explanation lines; no card.
- **One bill name:** with no title the phone sends the group's default name in the app language ("Bill · 7 Oct" / «Сметка · 7 окт»); the expense stays untitled; a typed title wins.
- **Two-person auto-fill:** with exactly two people in an Exact or % split, typing one box fills the other with the remainder (only a box the user has not typed in).
- **`clientRequestId`:** required (missing or all-zero is a plain 400); two identical requests at the same moment both answer 200 with the same expense.
- **Macedonian:** the Activity tab is «Активности»; the exchange rate is «Цена на еврото», never «Курс»; «го/ја» is written literally where the person's gender is unknown; dates, numbers and relative times come from our own tables, never from the browser.
- **An activity event type with no sentence stops with a clear error** naming the type; settlements and closing add their sentences with their events (Phases 9 and 10).

## 2026-10-05: Phase 7 (groups, members, invite links): done and merged, the rules that stay
The whole Phase 7 decision log (Filip's product answers, the technical choices, the Step 2 to Step 5 contracts, the Step 6 fixes, rejected alternatives) moved word for word to the end of `reports/2026-10-05-phase-07-groups.md`, section "Decisions and rejected alternatives". What stays true:
- **Roles:** any member adds plain names and shares the link; only the owner renames, changes the emoji or currency, resets the link (with Undo), removes, makes someone owner, undoes a claim, deletes and restores. The owner can't leave until someone else is owner; only a person with an account can become owner. Zero-balance checks for leave, remove and delete come in Phase 9.
- **Removed vs left:** a removed person is blocked from the old link ("You were removed from <group>. Ask the owner to let you back in."); the owner's **Removed** list has **Let back in** (same member, old history). A person who left can rejoin with the link. Undo claim keeps the person in the group and turns the name back into a plain name.
- **Deleted group:** "Group deleted · Undo" toast and **Recently deleted** for 30 days (owner only, Restore). The rows stay until the BACKLOG cleanup exists.
- **Same name in a group is refused** (letter case ignored): "Someone called “{name}” is already in this group." The invite card has no inviter name.
- **"That's me"** lets a member with nothing recorded under their own name take a plain name; the owner's Undo claim brings the old entry back. Merging two people's expenses is in BACKLOG.
- **Joining without an account** is not in Phase 7 and is the top BACKLOG "must do".
- **Invite token:** 32 random bytes, base64url, plain text in the database, only ever in a POST body (never in a URL); one `invite` rate limit of 20 requests per 10 minutes shared by preview and join. A 429 is never retried by the website.
- **A non-member gets 404 `GROUP_NOT_FOUND`**, a non-owner 403 `GROUP_NOT_OWNER`; a group id that is not a Guid is a 404 too and the website shows "group not found" (with **Go to Groups**, never Try again).
- **Round trip after sign-in:** router state `{ joinToken }` only (a token, never a free address); a reload in the middle of sign-up lands on the dashboard.
- **Macedonian:** "link" is "линк", never "врска" (except the internet-connection lines); a group's name always follows "групата" and is in quotes.
- **Looks:** bottom bar Home · Groups · Settings in the pill look, tabs by tap only; the selected tab in dark mode is the deep orange `#b54a00` with white text; anything that removes or ends something is wine red (`--danger`, `--danger-fill`) including its toasts; Owner badge and "That's me" use `--pill` (white in light mode, the page background `#351f1b` in dark mode). Toasts stay 4 seconds; sheets slide up and away (about 0.2 s, none under reduce motion) through Tailwind's `starting:` classes and `useKvitSheet().close()`.
- **Group name:** 1 to 60 characters after trimming, checked on the phone and on the server.
- **Code:** soft delete by explicit `deleted_at IS NULL` filters (no global query filter); enums stored as text with check constraints; domain services and repository interfaces in the Domain, repositories in Infrastructure; the money rules still wait for Phase 8.

## 2026-10-03: Phase 6 (Google sign-in + privacy page): done and merged, the rules that stay
The whole Phase 6 decision log (token check, endpoints, headers, the Google button measurements, product answers, rejected alternatives) moved word for word to the end of `reports/2026-10-03-phase-06-google-sign-in.md`, section "Decisions and rejected alternatives". What stays true:
- **Google sign-in:** the API checks Google's ID token (audience = Kvit's client id) and identifies people by Google's permanent id (`sub`), never by email. A Google email that already has a password account is never merged automatically (pop-up "This email already has an account."). The first Google sign-in asks the name once, pre-filled. A Google account can add a password in Settings.
- **The Google client id is public** and lives in the repository (`appsettings.json` `Google:ClientId`, `src/web/.env` `VITE_GOOGLE_CLIENT_ID`). The client secret is never used and never stored.
- **Screens:** Welcome = Google's own button (always white, drawn once per language), a small "or", Sign up with email, I already have an account, a small Privacy link; no pitch line. Settings: Privacy link, Set a password or Change password, Log out always last at the bottom.
- **Privacy page** `/privacy` is public, EN + MK, names no email ("ask Filip"). Before Kvit grows beyond family and friends Claude asks Filip for a contact email (BACKLOG).
- **Headers** come from Cloudflare `public/_headers` (not applied to the `/api/*` function). No inline scripts, because of the CSP.

## 2026-10-01: Phase 5 (first deploy): done and merged, the rules that stay
The whole Phase 5 decision log (migration path, Postgres 18, the web address and Google, how secrets are handled, the CI-driven Render deploy, the Welcome light/dark button, the show/hide password button, rejected alternatives) moved word for word to the end of `reports/2026-10-01-phase-05-first-deploy.md`, section "Decisions and rejected alternatives". What stays true for the product and the setup:
- **Address:** `https://kvit-mk.pages.dev` (API `kvit-mk-api.onrender.com`). A paid name can be attached later with no code change, only if Filip decides to pay.
- **Hosting:** Neon (Postgres 18), Render free, Cloudflare Pages, all free with no card. Database changes are applied by CI before the deploy; CI deploys to Render and waits until live; Render's Auto-Deploy is Off.
- **Postgres 18** locally, in the tests and on Neon.
- **Screens:** every password box has a show/hide eye button; the Welcome screen has a round moon/sun button left of the EN/МК switch that flips light and dark and is remembered on that phone ("Same as device" stays in Settings).
- **Google sign-in on `pages.dev`:** works without brand verification; people see the address instead of "Kvit" on Google's window. Filip accepted that for now.

## 2026-10-01: Phase 4 (database + email accounts): done and merged, the product rules that stay
The whole Phase 4 decision log (technical decisions, rejected alternatives, code-review findings, the three mockup rounds, the approved Macedonian wording tables, every step block) moved word for word to the end of `reports/2026-10-01-phase-04-accounts.md`, section "Decisions and rejected alternatives". What the app does, as Filip decided it:

**Filip's answers (2026-09-29):**
- **Password:** at least 8 characters, with at least one capital letter and one number. No symbol rule, no lowercase rule. *"Sunce2026" is accepted, "sunce2026" is not.* The rule is shown under the field.
- **Staying logged in:** 90 days, reset on every visit.
- **Email already used:** say so plainly ("This email already has an account. Log in, or use a different email."). Filip: large sites do the same, and Kvit may grow beyond family and friends.
- **Wrong password:** 5 wrong tries in a row lock the account: 5 minutes the first time, 10 the second, 15 every time after. A successful log-in resets the ladder. Identity has only one fixed lock time, so this needs a `lockout_count` column on `users` and our own lock end after each new lock.
- **Language after log-in (Claude's default, told to Filip, not objected to):** the account's saved language wins over the phone's.
- **Forgot password (until email exists):** Log in shows "Forgot your password? Ask Filip to reset it." Filip resets by hand with `scripts/ResetPassword.cs` (`guides/reset-a-password.md`): a temporary password shown once; the person must choose their own at the next log-in; nobody, Filip included, can read a real password. When email reset exists the note becomes a "Reset password" link (BACKLOG).
- **Limits:** log-in 10 tries a minute; sign-up 5 per 10 minutes per address, counting only requests that pass the cheap checks (a weak password, a bad email or a bad name never counts; a taken email and a created account do).
- **Theme:** Settings has Same as device (default) / Light / Dark, remembered on that device.
- **Time zone:** read from the phone at sign-up and log-in; a phone that reports none cannot sign up or log in yet ("We couldn't read your phone's time zone, so this isn't possible on this device for now."); a picker and Settings "Choose manually" are in the BACKLOG.
- **Look:** the approved look is in `docs/design/2026-10-01-round-3/` and its tokens in `src/web/src/index.css`; one main-button look across the app.
- **Web and phone:** one responsive app, phone first at 360 px; the real desktop layouts (sidebar, several columns) are designed with the dashboard in Phase 11, Filip's call that day.
- **The wording** of every Phase 4 error text and screen text lives in `en.json` and `mk.json`; Filip approved each Macedonian line (the tables are in the report).

## 2026-09-25: Web addresses, README and licence
- **Addresses:** the website will be **`kvit-mk.pages.dev`** and the API **`kvit-mk-api.onrender.com`**.
  - `kvit.pages.dev` and `kvit-app.pages.dev` are taken (both answered with a live site). `kvit-api.onrender.com` gave no answer within 70 seconds, while unused Render names answer at once with "no server", so it's treated as taken. (VERIFIED by live requests on 2026-09-25.)
  - `kvit-mk`, `kvitsme` and `mojkvit` on pages.dev, and `kvit-mk-api` on Render, answered like unused names. Whether they're truly free only shows when the project is created (NOT VERIFIED).
  - The hosting guide and roadmap now use the new names. Older reports keep the old ones as history.
- **README:** added at the root for GitHub visitors, so GitHub's auto-generated README isn't needed.
- **Licence: none for now ("All rights reserved").** Kvit may get paid features later (see "Speed details"), and a licence like MIT can't be taken back for code already published under it. A public repository with no licence can still be read by anyone (employers included), but nobody may reuse the code. MIT or AGPL can be added any time later.

## 2026-09-25: Filip's answers to the plan questions
- **MKD is split to whole denars** (1,000 / 3 → 333, 333, 334). EUR is split to the cent.
- **Exchange rate:** NBRM only, with no override in settings. A per-expense override comes in Release 2. This settles the clash between the two 2026-09-24 entries.
- **A confirmed payment entered by mistake** can be deleted by whoever recorded it, or by the owner, with Undo.
- **Own login endpoints, not `MapIdentityApi`:** a technical choice, so Claude decided it (Filip didn't need to weigh in). Reason: `MapIdentityApi` has no name field at sign-up, no Google sign-in, and exposes password-reset/2FA endpoints that can't work without email.
- **Styling: Tailwind CSS v4 + shadcn/ui (Base UI variant) + Sonner for toasts.** Filip asked for a search for something better than plain Tailwind (research by the `researcher` subagent, 2026-09-25).
  - **Why:** Tailwind alone has no bottom sheets, toasts or tabs, and hand-building accessible ones (focus handling, closing with the back gesture) is hard for a beginner. shadcn/ui copies small, readable component files into the project, so there's no big library API to learn and only the used components are shipped. Tailwind's CSS variables cover the design tokens and dark mode.
  - **Facts:** shadcn 4.21.0, `@base-ui/react` 1.8.0 (React 17–19), Sonner 2.0.8 (React 18–19), Tailwind 4.3.3, all MIT (VERIFIED by registry lookup). shadcn made Base UI its default for new projects in July 2026 (VERIFIED by the researcher's live web check of the shadcn changelog).
  - **Rejected:** Mantine (heavier on cheap phones), CSS Modules + a headless library (the whole visual system by hand), Panda CSS / UnoCSS / Park UI (maturity not checked).
  - **Traps:** pick the Base UI option in `shadcn init`, and follow the Tailwind **v4** setup (`@theme` in CSS), not old v3 guides with `tailwind.config.js`. The copied components go in `shared/components/ui/` and are wrapped by the `Kvit*` components, so features never import them directly.
- **Leftover denar when the payer isn't sharing** (e.g. you pay a 1,000 MKD taxi for Ana, Marko and Bojan → 333 + 333 + 334): the first of them in the group's member list pays the extra one.
- **Anyone in the group can add name-only members** ("Grandma"). Removing people stays owner-only.
- **How questions get asked from now on:** only questions about how the app behaves, each with a concrete example in plain words. Technical choices are decided by Claude and logged here.

## 2026-09-25: Building session, plan (see `DATA-MODEL.md`, `SCREENS.md`, `ROADMAP.md`)
**Facts checked today (VERIFIED by registry lookup / local run on 2026-09-25):**
- Filip's PC has .NET SDK 10.0.400, Node 24.19, npm 11.17, Docker 29.7 and git 2.47. `dotnet new sln` makes a `.slnx` file.
- The Vite `react-ts` template (create-vite 9.2.1) installs **TypeScript ~6.0.2** and lints with **oxlint**, not ESLint. So the "TS 7 breaks ESLint" trap doesn't apply as written.
- `openapi-typescript` 7.13.0 declares it needs TypeScript 5 (`peerDependencies: typescript ^5.x`). That clashes with TS 6 and TS 7 alike, so type generation needs a workaround either way (decided in the phase that adds it).
- Every NuGet package ARCHITECTURE names has a .NET 10 version: Npgsql EF 10.0.3, EFCore.NamingConventions 10.0.1, Scrutor 7.0.0, Scalar.AspNetCore 2.17.10, Testcontainers.PostgreSql 4.15.0, Google.Apis.Auth 1.76.0, DataProtection.EntityFrameworkCore 10.0.12.

**Decided (technical, following ARCHITECTURE):**
- **TypeScript 7 (Filip asked, 2026-09-25):** try it in Phase 2. Keep it if lint, build and tests pass; otherwise stay on the template's 6.0 and log why here. The main reason TS 7 was risky (typescript-eslint supports TS < 6.1 only) is gone, because the template uses oxlint. Still unchecked: VS Code editor support for TS 7 and `tsc -b` on 7.0 (NOT VERIFIED).
- **Lint is oxlint** (the template's default) instead of ESLint. `install-tools.md` now lists the Oxc extension.
- **Repository layout:** `Kvit.slnx` at the root, backend in `src/api/`, backend tests in `tests/`, frontend in `src/web/`, Dockerfile at `src/api/Dockerfile`, Pages Function at `src/web/functions/api/[[path]].ts`.
- **Backend projects arrive one at a time:** Phase 1 creates Api, Application, Domain and Contracts; Infrastructure comes with the database in Phase 4.
- **Ids** are `uuid` v7. **Currency and statuses** are stored as text. **Balances are never stored**, always added up from rows.
- **"Equal + extras" is the Equal split type with extras**, not a fifth type. The four split types stay as decided.
- **Usage events live in their own table**, apart from the group activity feed, because they're platform counts that never hold amounts or names.
- **`client_request_id` columns exist from Release 1** on expenses, settlements and personal entries, so the Release 3 outbox needs no table changes.
- **Closing: who must confirm** = every current member with an account except the owner. With nobody left to ask (owner + plain names), the group finishes at once.
- **Deploy early:** a skeleton goes online in Phase 5, before the features, so the hosting traps (proxy cookie, keys in the database) are tested first.

**Proposals at the time (see "Filip's answers to the plan questions" above for what was settled):**
- Tailwind CSS v4 + CSS variables for styling (ARCHITECTURE left it open).
- Own auth endpoints instead of `MapIdentityApi`.
- Migrations reach Neon through a CI step with the direct connection string (decided in Phase 5).
- The Scalar API reference page only in development.
- Business rules: the five open questions at the end of `DATA-MODEL.md`.

**Clashes found and how they were resolved:**
- `reports/2026-09-25-final-plan-review.md` (B1) puts Google sign-in and change history in Release 3; the later answers in this file put both in **Release 1**. This file wins (it's the source of truth for features).
- "Change history in the first version if it stays small, otherwise postpone" (2026-09-24) vs. "Change history is part of the activity feed" (2026-09-25): the later one wins, so it's in Release 1.
- "EUR converted with a saved rate of about 61.5, editable in settings and per expense" (2026-09-24) vs. "refreshed from NBRM every 8 hours" (2026-09-24): not resolved yet, it's open question 3 in `DATA-MODEL.md`.
- No clash found between `ARCHITECTURE.md` and this file.

## 2026-09-25: Business-review answers (see `reports/2026-09-25-business-logic-review.md`)
- **"Equal + extras" split (Release 1).** Filip's hotel example: 5 people share a hotel, and Marko takes the better room for 600 more. You enter the total, it splits equally, and you tap a person to add an extra (+600 for Marko). The rest is shared equally among everyone. It's the fast version of the "exact amounts" split, made for the most common uneven case. It's also the default: a new expense is split equally among all members.
- **Totals per category in a group** (Hotel, Food, Transport, Other…): Release 2, together with the budget.
- **Group spending plan (Release 2).** For example "600 EUR planned, 4 people = 150 each", with a progress bar and a warning near the limit.
- **Shared pot / kitty** (everyone pays in up front): backlog.
- **Budget counts shares only.** Your budget counts only **your share** of expenses. It never counts who paid, and it never counts paybacks ("I paid Ana 1,200" / "Marko paid me"). Paybacks are not income either.
- **Possible duplicates (Release 2).** When your share of a group expense lands in your budget and you already logged a personal entry in the same category with a similar amount within a few days, Kvit asks: *"Is this the same as 'Dinner – 1,200' you added on Friday? · Same, merge · No, it's new"*.
- **Plain-name members:** the group owner confirms "I paid Grandma" on her behalf.
- **Budget currency:** each user picks one in settings, MKD by default. Other currencies are converted with the rate saved on each expense.
- **Owner's account deleted:** ownership passes to the member who joined earliest (only matters for groups that are still open).
- **"Who pays whom"** shows one line under it: *"Simplified: fewer payments, same totals."*
- **Finishing a group (Release 1). Updated with Filip's answer, 2026-09-25:**
  1. When every balance in every currency is zero and nothing is pending, the owner sees **"Everyone's kvit – close the group?"**, with the checklist of settlements. The owner taps **Confirm**.
  2. The group is now **"Closing"**. Every other member sees **Confirm** or **Object**.
     - **Object** can include an optional short reason. The text field costs almost nothing, so it's included.
     - An objection cancels the closing, and the group goes back to normal so people can sort it out in person.
  3. The group becomes **Finished** when **everyone has confirmed**, or **automatically 24 hours after the owner confirmed** if nobody objected.
  4. **While it's "Closing", the group is frozen for members.** They can only Confirm or Object, and they can't add or change anything. **Only the owner** can still fix an expense (e.g. after an objection), and any change **cancels the closing**, because the balances are no longer zero. The owner starts the closing again once it's sorted. While the group is open, the normal rule applies: whoever added an expense, plus the owner. (Filip, 2026-09-25.)
  5. **Finished = read-only for everyone.** All members can still open it and see every expense and settlement, but nobody can change anything.
     - Unlocking it again for changes is owner-only, and it's recorded in the activity feed.
     - **Rejected:** only the platform admin can unlock. Filip isn't in other people's groups.
  - **How the 24 hours work without a scheduler:** the group stores when the owner confirmed. Whenever anyone opens the group or the dashboard after 24 hours with no objection, it counts as Finished. The same lazy trick as the exchange rate, so it's free.
- **Group type when creating (Release 1).** Filip's idea:
  - **"One bill"**: a single dinner or taxi. Enter the amount, pick the people, done. There's no group to manage. It uses the same group underneath, marked as one-bill, and it **finishes automatically** once everyone is kvit, with no closing step.
  - **"Group"** (a trip, a household, a friend circle): many expenses over time. Closing works as described above.
- **No deadline.** It's a hobby project with no deadline (Filip, 2026-09-25). The releases are there to keep each step finishable, not to hit dates.

## 2026-09-25: Final review answers (see `reports/2026-09-25-final-plan-review.md`)
**Releases**
- **Release 1, "splitting works":**
  - email + password and **Google sign-in** (moved up from Release 3 so there's no email-address clash and no locked-out users),
  - groups, invite links, plain-name members,
  - expenses with 4 split types,
  - MKD/EUR balances and "who pays whom",
  - settle up with confirmation,
  - activity feed with change history, dashboard,
  - EN/MK, dark mode,
  - deployed, with saved data shown instantly.
  - Usage events are recorded from day one.
- **Release 2, "budget":** categories and limits, income (once and repeating), the budget share, the monthly summary.
- **Release 3, "polish":** the outbox and the admin statistics page.

**Rules**
- **Exchange rate:** each EUR expense keeps the exchange rate from the day it was saved.
- **Removing and deleting:** the owner can't remove a member or delete a group while balances aren't zero. The owner has to hand over ownership before leaving.
- **"I paid":** the payer can cancel it while it's still pending.
- **Wrong claims:** the owner can undo a wrong name claim.
- **Invite links:** long and random, and the owner can reset them. Anyone with the link joins straight away (no approval).
- **Joining through an invite link:**
  - **Already logged in:** the join screen shows *"Filip invited you to Greece trip"*, the group's emoji, and the members already in it, with one **Join** button below. The name comes from the account, so no name field is shown (Filip, 2026-09-25).
    - **Only if the group has unclaimed plain names:** after tapping Join, one optional question appears: *"Are you one of these? Marko · Grandma · No, I'm new"*. This is for when the owner already added "Marko" as a plain name and recorded expenses for him. Claiming joins those expenses to Marko's account instead of creating a second "Marko".
  - **No account yet:** the link opens a short sign-up first: Google (one tap) or name + email + password. It then comes **back to the same join screen**. The invite is remembered through the sign-up.
  - **Already a member:** opening the link again just opens the group. There's no second join, because the login remembers him.
  - **Joining with no account at all** (the device remembers you) stays in the backlog.
- **Google vs password accounts:** Google sign-in never merges automatically into an existing password account with the same email. The user is told to log in with their password.
- **Forgot password (until email exists):** Filip resets it by hand. Google is shown as the main sign-in button.
- **Time zones:**
  - Everything is stored in UTC.
  - Each user has a time zone that is **detected automatically from the phone**, with an optional manual choice in settings. "Today", months and repeating income use the user's own time zone, so it works on a trip to America too.
  - An expense's date is a plain calendar date (no time), so the date doesn't change between time zones.
  - **Rejected:** fixed Skopje time (wrong abroad).
- **Categories:** group expenses use the built-in categories. Custom categories are for personal spending only.
- **Budget share switch:** per group, "count my shares in my budget", on by default. It applies from the moment you change it; past months stay as they were. Each expense remembers whether it was counted.
- **Deleting:** it marks an item as deleted, and Undo restores it.
- **Change history** is part of the activity feed, showing old → new values.
- **MKD amounts** are stored in deni (×100), the same as euro cents. The denar is shown without decimals.
- **Security basics:** limits on login and join attempts, HTTPS only.
- **Privacy page:** a simple page describing the data Kvit stores. Release 1, because Google may require it (check).
- **Account deletion:** backlog (see the cleanup item there).

## 2026-09-24: Name is Kvit
- **Chosen:** "Kvit", from "квит сме" ("we're even"). The start screen tagline is "Квит сме." The folder, repository and web addresses use `kvit`.
- **Rejected:** "Kvit-sme". Hyphens are awkward in web addresses and repository names.

## 2026-09-24: Login technical fixes (from the stack research)
- **Login keys go in the database.** The .NET Data Protection keys are stored in Postgres (`PersistKeysToDbContext`), because Render wipes its files on every restart, which would log everyone out. They must also be encrypted.
- **One site for the browser.** A free Cloudflare Pages Function forwards `/api/*` to Render, so the browser only talks to one site. This lets a secure login cookie work on iPhones (Safari blocks cookies between different sites) and removes the need for CORS.
- **Rejected:** bearer tokens kept in `localStorage`. They're weaker if the site ever has a script-injection bug. Kept as a fallback only.

## 2026-09-24: Exchange rate refreshes every 8 hours
- The saved MKD/EUR rate is refreshed from the National Bank (NBRM) free service, which needs no key.
- **How:** "lazy" refreshing. When the API is used and the saved rate is more than 8 hours old, it fetches a new one. No scheduler is needed, which suits a server that sleeps.
- **Cost:** none.
- **Note:** NBRM publishes about once per working day, so most refreshes will return the same number.
- **If NBRM can't be reached,** the app keeps the last saved rate and shows its date. It doesn't hide the error.

## 2026-09-24: Speed details
- **Adding an expense:** one screen with the amount field focused and the number keypad open. The other fields are **pre-filled with the most common choice** and shown as tappable chips: paid by me, split equally among everyone, today, the group's currency, and a category guessed or left empty. Tap a chip only to change it. The title is optional.
- **After login** the app opens on the **dashboard**, with a big "+" button that opens a short list (Add to the last-used group, New One bill, Another group…); see the Phase 11 entry (2026-10-15).
- **Personal spending:** tap a category (or "+ new category" right there), type the amount, save.
- **Undo instead of "Are you sure?"** pop-ups.
- **No limits and no ads** in the app for now. Paid features may come later if it becomes popular. If that happens, the free-hosting terms must be checked again for commercial use.

## 2026-09-24: Accounts and login
- **First version: everyone who uses the app has an account.** People like grandma are plain-name members that others add expenses for.
- **Guest joining without an account is postponed** (see the backlog). It needs rules for who can claim a name, fixing wrong claims and so on, which is too much for the first version.
- **Ways to log in:**
  - Email + password.
  - **Sign in with Google**, using Google Identity Services: the ID token is posted to the API and checked there. It's free with no card; only the openid/email/profile scopes; publishing status "In production". Source: `reports/2026-09-24-google-apple-login-and-sharing.md`.
- **Rejected:** Sign in with Apple. It needs a $99-a-year Apple developer account.

## 2026-09-24: No keep-awake pings for now
- Render's rules neither allow nor forbid them, so we don't ping. The app wakes the server as soon as the page opens. Saved data is shown while it wakes (see "Hiding the server wake-up"). This will be looked at again later, e.g. by asking Render support or moving to a cheap paid plan if real users arrive.

## 2026-09-25: Hiding the server wake-up (first version)
**The problem:** Render's free server sleeps after 15 minutes without visits and takes about 1 minute to wake. The first person after a quiet period would have to wait.

**The solution:**
1. **Show saved data instantly.** The app loads right away from Cloudflare, which never sleeps. It shows the last balances and lists it saved in the browser, with a small "updating…" note until the server answers.
2. **A background "outbox".** Everything the user does while the server is waking goes into a queue saved in the browser (IndexedDB). Those items appear on screen immediately, marked "not synced yet". When the server is up, the queue is sent in order and the user sees an in-app message: **"All up to date ✓"**. That was Filip's idea.
   - **Every queued action gets a unique ID made on the phone,** so if it's sent twice, the server saves it only once.
   - **The queue survives closing the tab.** Anything still waiting is sent the next time Kvit is opened.
   - **What can be queued in the first version:** new expenses, new personal spending or income, and **"Marko paid me"** records. The receiver's word counts immediately and the payer has no reason to dispute it, so it's safe to queue.
   - **What waits for the server:** **"I paid"** records (they create a pending request the receiver has to confirm, so they go straight to the server), and all editing and deleting. This keeps conflicts out of the first version.
   - **If the server rejects an item** (for example the user was removed from the group in the meantime), it is **not dropped silently**. The user sees "1 change couldn't be saved" with the reason and can retry or discard it.
   - **The message is an in-app notification,** not a phone push notification. Push notifications come later with the PWA.
- **Why:** this keeps the "seconds, not minutes" goal on a free server that sleeps.
- **Rejected:** keep-awake pings (Render's rules are unclear) and a paid plan (not free).

## 2026-09-24: Admin statistics page
- There's a **platform admin** role, for Filip only. It's separate from group owners.
- **The admin page shows counts for a date range you choose,** grouped by day, week, month or year:
  - new sign-ups (split into email vs Google),
  - active users (logged in or used the app),
  - people who joined groups through invite links,
  - groups created,
  - expenses added,
  - settlements confirmed.
- **Privacy:** the admin page only shows totals. It never shows anyone's personal budget, expense details or amounts.
- **How:** a small activity-events table (user, event type, time). "Active" is counted at most once per user per day.

## 2026-09-24: Public GitHub repository (Filip agreed 2026-09-25)
- **Public:** employers can see the code, GitHub Actions minutes are unlimited, and GitHub's secret protection is free.
- **Rule:** no passwords, keys or connection strings ever go into the code. They live in the Render, Cloudflare and GitHub secret settings.
- **Can switch to private later** if Kvit ever becomes a paid product.
- **Rejected:** private. It gets only 2,000 free Actions minutes a month and employers can't see it.

## 2026-09-24: Dashboard layout decided while building
- The app opens on the dashboard. Filip decides the exact layout when that screen gets built.

## 2026-09-25: Who can edit and delete (confirmed)
- **Group settings** (rename, remove members, delete the group): the owner only.
- **An expense:** whoever added it, plus the owner.
- **Personal spending and income:** only that user.
- **Rejected:** owner-only editing of expenses. The owner would have to fix everyone else's typos, which is slow.
- Clashes (two people changing the same thing) are avoided in the first version because editing and deleting need the server to be awake. The activity feed shows who changed what.

## 2026-09-25: Pictures (first version = no uploads)
- **Users:** a coloured circle with their initials. Google users get their Google profile picture automatically. It comes free with the "profile" scope, so no storage is needed.
- **Groups:** an emoji of the user's choice (🏖️ ✈️ 🏠 🍕…) on a coloured background.
- **Uploading your own pictures comes later** (see the backlog).
- **Why:** it costs nothing, needs no file storage and takes no time to set up.

## 2026-09-24: Settle-up rule
- If the **payer** records "I paid Ana 1,200 MKD", it stays **pending** until **Ana confirms** it. Ana can also reject it.
- If the **receiver** records "Marko paid me 1,200 MKD", it counts **immediately**. The receiver is the one who could lose money, so their word is enough.
- The money itself moves outside the app (cash or a bank transfer). The app only keeps the record.

## 2026-09-24: Stack is .NET + React (full-stack)
- **Chosen:** C# ASP.NET Core Web API + EF Core + PostgreSQL, React frontend, Docker, GitHub Actions.
- **Why:** Filip's DocuMind (Python/AI) and Book Scanner (PWA frontend) projects don't cover a typical business backend. Full-stack shows both sides, and if he likes .NET and React he can grow the project later.
- **Rejected:**
  - Flutter + DocuMind mobile, because app stores cost money and AI hosting is slow on free servers.
  - Java Spring + Kafka, because it's heavy and hosted Kafka isn't free.

## 2026-09-24: Everything free, and no bank card
- **Rule:** no paid tiers, no trials that expire, and no service that asks for a card at sign-up.
- **Rejected:** Oracle Cloud Always Free. It asks for a card at sign-up and can take back idle servers after 7 days.
- **Rejected for the database:** Render free Postgres, because it's deleted after about 30 days.

## 2026-09-24: App is shared expenses + personal budget
- **Chosen:** a Splitwise-style shared expense splitter with a personal budget. First users are Filip, his family and close friends. Local businesses might come later.
- **Why:**
  - It's the best fit for a free server that sleeps, because it needs no live updates or jobs every minute.
  - Filip would use it daily.
  - The money logic ("who pays whom" in the fewest payments, exact rounding, currencies) gives real backend problems to talk about in interviews.
- **Rejected:** pitch/court booking, barber booking, tutoring booking, weekly game organizer, roommate app, gym tracker, event tickets and shift scheduler. They're saved in `C:\Users\Davchev\Projects\Ideas For Later\IDEAS.md`. The booking apps in particular need an always-on server.
- **Not needed:** automatic bank sync. It's no longer free anyway.

## 2026-09-24: Scope is a small MVP first
- **Chosen:** a small version that can be finished in a few weeks. Filip decides afterwards whether to continue.

## 2026-09-24: First version features, languages, currencies
- **Features:**
  - Email + password login (ASP.NET Identity), plus Sign in with Google (added later the same day, see "Accounts and login").
  - Groups with invite links.
  - Expenses split equally, by exact amounts, by percentages or by shares (percentages and shares were added later the same day, see "How the app works").
  - Balances and "who pays whom" in the fewest payments.
  - Settle up.
  - Personal budget with categories, monthly limits and a monthly summary.
  - Phone-friendly layout.
- **Languages:** English and Macedonian, with a switch in the app.
- **Currencies:** MKD and EUR from the start. MKD is for everyday use and EUR for trips abroad.
- **Postponed:** forgot password (needs email). The other later features are listed in `BACKLOG.md`.

## 2026-09-24: Frontend and money-handling rules
- **Frontend:** React + TypeScript, built with Vite. TypeScript is what job ads ask for and it catches mistakes early.
- **Money:** always stored as whole minor units (denars / euro cents) in integers, never as floating-point numbers.

## 2026-09-24: How the app works (answers to the planning questions)
**Groups**
- Members without an account can be added as a plain name, e.g. "Grandma". When that person signs up and joins through the invite link, **they** pick "that's me" from the list of unclaimed names. Someone else can't claim it for them.
- Roles: a group owner (removes people, deletes the group) and normal members.
- You can't leave a group while you owe money or are owed money.

**Expenses**
- One payer per expense. Several payers are on the "to be done" list.
- Split types: equal, exact amounts, percentages, shares.
- An expense can be edited or deleted by whoever added it and by the group owner.
- An expense has a title, amount, currency, date, category, payer, split and an optional note. No photos.
- Every group has an activity feed.
- Change history ("Filip changed 1,200 → 1,500"): in the first version if it stays small, otherwise it's the first thing to postpone.

**Settling up needs two sides**
- A settlement starts as **pending**. It only changes balances once the **person receiving the money confirms** it. This covers the case where Marko says he paid but didn't.
- The receiver can also reject it.

**Currencies**
- Balances are kept separately per currency (MKD and EUR are never mixed).
- Each group has a default currency, and any expense can use the other one.
- For the personal budget, EUR is converted using a saved rate of about 61.5, editable in settings and per expense.

**Budget**
- Your share of group expenses counts toward your budget automatically, in the expense's category.
- Income is optional. It can be added once or set to repeat (e.g. 30,000 MKD every month on the 1st, or weekly).
- Categories come as a ready-made list with icons and colours, and you can add your own.
- The personal budget is always private.
- Leftover money at the end of the month is handled later (see the backlog).

**Look and feel**
- Dark mode follows the phone's setting.
- The main screen is a dashboard: what you owe and are owed, this month's budget and recent activity.
- The cold start is covered by saved data plus the outbox (see "Hiding the server wake-up"). A full "Waking up the server…" screen only appears when there's no saved data yet, e.g. on the very first visit on a new device.

## 2026-09-24: Speed and simplicity above everything
- Filip's rule: setting up and using the app must take seconds. Nobody should spend 5 minutes paying someone back or 15 minutes setting up.
- Every screen is judged by how few taps and how few typed fields it needs.

## 2026-09-24: Cost: free now, cheap later if it's really used
- It runs free for now. If real people start using it heavily, moving to a cheap paid server must be easy. This is why everything runs in Docker and nothing is tied to one provider.

## 2026-09-24: Database is Neon free
- **Chosen:** Neon free PostgreSQL. No card, no expiry, and it wakes up on its own after sleeping.
- **Rejected:** Supabase free. Its database pauses after about a week without use and has to be restored by hand. Its built-in login would also replace the .NET login work this project is meant to show. Its free file storage (1 GB) stays an option if receipt photos are ever wanted.
