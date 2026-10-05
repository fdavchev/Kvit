# Decisions

## 2026-10-03: Phase 7 (groups, members, invite links): decision log, in progress
Facts checked on 2026-10-03 against the code: the only entity today is `UsageEvent`; `GroupCreated` and `JoinedViaInvite` are already allowed by the `usage_events` check constraint (no migration for usage events); `IUnitOfWork` has no `SaveChanges`; the frontend has no "return here after sign-in" path, no Undo toast helper, no share/copy code and no DELETE in `jsonRequest`.

**Filip's answers (product, 2026-10-03)**
- **Undo claim (owner):** the person who claimed stays in the group as themselves; the claimed name becomes an unclaimed plain name again. *Rejected:* taking the person out of the group (they would have to rejoin for the owner's undo).
- **A removed person is blocked.** Opening the old link shows "You were removed from <group>. Ask the owner to let you back in." The owner sees a small **Removed** list on Members with **Let back in**, which brings the same member back with the old history. A person who **left** on their own can rejoin with the link. Right after a removal a "removed · Undo" toast shows. Filip's reasoning: the owner may remove someone for a reason (an old link must not undo that), or by mistake (a block must not force a new group). *Rejected:* always rejoin with the link (undoes a deliberate removal unless the link is reset); the owner picks "remove" or "remove and block" each time (an extra choice every time); the removed person asks to rejoin and the owner approves (a waiting list and a new kind of notice).
- **Deleted group:** a "Group deleted · Undo" toast, plus a **Recently deleted** section at the bottom of the Groups list for **30 days** (Filip: 15 days, or 30 if it is cheap on the database; it is a few KB per group). Only the owner sees it and restores it. After 30 days it drops off the list; the rows stay until the BACKLOG cleanup exists. *Rejected:* a 10-second Undo only.
- **Navigation: a bottom bar Home · Groups · Settings, in the "pill" look (Filip, 2026-10-03; the pill is the look of the EN/МК switch: a floating rounded bar, the active tab a white pill with an icon and its name).** Tabs are switched by **tapping only** (Claude's recommendation, Filip did not object): a swipe between tabs would fight the phone's swipe-back from the screen edge, needs both screens kept alive side by side, and a mouse on the web can't swipe anyway. *Rejected:* navigation B (no bar, Home lists the groups); the flat-line, icons-grow, peach-button and white-pill looks (all shown in `docs/design/2026-10-03-groups/`); swiping.
- **The selected tab must be easy to see in dark mode (Filip, 2026-10-03: in the first pill version it vanished, because the selected pill and the bar were nearly the same dark brown).** In light mode it stays a white pill. For dark mode Filip did not like a filled peach pill ("I don't like this on the dark side") and **chose a deep orange pill with white text (`#b54a00`, the light-mode main-button orange, 5.3:1)** from four looks. *Rejected:* a white pill, a peach outline only, a short orange line under the tab, the peach pill.
- **Anything that removes or ends something is wine red (Filip, 2026-10-03: "a slightly bright red wine colour", neither dark nor light).** Text colour `#b71f1f` in light mode (4.9:1 on the page, 5.8:1 on cards) and `#ff6b64` in dark mode (5.5:1 on the page), as a new design token `--danger`. The first dark value `#ff7a8f` looked pink to Filip, and the second round was still not red enough (2026-10-03), so both modes were moved to a plainer red. It applies to the buttons Reset link, Remove from group, Delete group and Leave group, **and to the toasts they leave behind** ("Link reset… Undo", "Removed: …", "Group deleted", "You left …"): the toast is filled red-wine with white text and a white Undo link: `#b71f1f` in light mode (6.5:1) and `#d12828` in dark mode (5.2:1). *Rejected:* a lighter pink fill with dark text, and `#c9303d` (still read as pink). Other toasts keep the normal toast colours. It does **not** apply to Undo, Undo claim, Let back in, Restore or Make owner (they fix or give, they don't take away). The error text colour stays as it is. *Rejected:* a filled red button (too loud next to the main button), the old error red for these (too plain brown-red, and it would mix "something went wrong" with "this takes something away").
- **Toasts (the "… · Undo" messages) stay 4 seconds and slide up from the bottom (Filip, 2026-10-03: 6 seconds was too long).** They slide in over about 0.2 s and slide away over about 0.2 s; a person whose phone is set to "reduce motion" gets no sliding. The app already uses Sonner for toasts, which slides toasts in by itself, so Step 4 only sets its duration to 4 seconds and gives the Undo action to it (checked against Sonner's docs before it is built). *Rejected:* keeping 6 seconds; no animation (animations are cheap here and do not wait for a later phase).
- **Groups list order (Filip, 2026-10-03): open groups, then a collapsed "Finished" row, then a quiet "Recently deleted ›" link that opens its own screen** (groups you own, 30 days, Restore there). Groups can only be Finished from Phase 10, so the real Phase 7 app hides the Finished row when it is empty.
- **"Add a name" is a quiet "+ Add a name" link under the list of names, left-aligned like the names, and opens a small sheet with the field (Filip, 2026-10-03: "like Recently deleted").** The sheet keeps the error inside it (empty name, a name already there) and closes after a name is added. *Rejected:* a field plus button on the page (Filip did not like it), a field that opens in the list.
- **Macedonian wording for a group's name always says "групата" before the name, with the name in quotes (Filip asked, 2026-10-03, whether "во" or "на" is right).** "Во Грција" (a place) and "на вечера" (an event) need different small words, and the app cannot know which one a typed name is. So: "Те поканија во групата „Патување во Грција“", "Се придружи на групата „…“", "Веќе си во групата „…“", "Те отстранија од групата „…“", "Ја напушти групата „…“". English needs no change ("You're invited to Greece trip" / "to dinner"). *Rejected:* a fixed "во" or "на"; one word chosen per group kind.
- **The names-versus-account idea is explained on the new group's Add people card, not on Welcome (Filip, 2026-10-03), as two short lines each next to its own button:** "No Kvit? Add them as a name." (Add a name) and "Have Kvit? Share the link and they join with their own account." (Share invite link). Filip pointed out that Claude's first wording ("Friends don't need Kvit. Add them as a name, or share the link.") made the link sound as if it needed no account. *Rejected:* that one sentence; a permanent line on Members.
- **Joining through the link without an account stays out of Phase 7, and is a firm later "must do" (Filip, 2026-10-03: "we will add no account join later")**: open the link, tap your name, the phone remembers you (BACKLOG, top item). It needs a guest identity on the phone, rules for who may claim which name, fixing wrong claims, and what happens if the phone is lost. Until then a friend who uses the app joins with Google (1 tap) or email, and a friend who doesn't is a plain name the group manages. *Rejected:* building it in Phase 7 (a much bigger phase, and it would change the claim rules settled above).
- **Same name in a group is refused** (letter case ignored, among people currently in the group): "There's already a Marko in this group." *Rejected:* allowing two rows with one name.
- **The invite card has no inviter name:** "You're invited to Greece trip", the emoji and the member names. The 2026-09-25 line "Filip invited you to Greece trip" is replaced. *Rejected:* the owner's name, the sharer's name (one link per group, so a sharer's name would need a link per member).
- **"No, I'm new" by mistake (Claude's proposal, Filip said yes on 2026-10-03):** a **That's me** button shows on Members next to an unclaimed name, only to a member who has nothing recorded under their own name. Tapping it moves that person onto the name and sets their own empty entry aside; the owner's Undo claim brings it back. Once the person has expenses or payments under their own name, the button is gone; merging two people's expenses goes to the BACKLOG. *Rejected:* a merge of two members' expenses now (risky, and the money screens do not exist yet).

**Defaults (from the 2026-09-25 rules, not asked again)**
- Any member adds plain names and shares the link; only the owner renames, changes the emoji or currency, resets the link (with Undo), removes, makes someone owner, undoes a claim, deletes and restores.
- The owner can't leave until someone else is owner; only a person with an account can become owner. Zero-balance checks for leave, remove and delete come in Phase 9.
- A person opening a link for a group they are already in goes straight to the group.
- Activity events are recorded now; the Activity tab is Phase 8.

**Technical choices (Claude decided)**
- **Entities** `Group`, `GroupMember`, `ActivityEvent` in `Kvit.Domain/Entities` (private constructor, `Create` returning `Result<T>`, behaviour methods). Enums stored as text with check constraints built from the enum (the `usage_events` pattern). *Rejected:* a database enum type (migrations get awkward).
- **Domain services** (`Domain/Services/Groups/`) and **repository interfaces** (`Domain/Interfaces/`), repositories in `Infrastructure/Repositories`, found by Scrutor. This is the first use of ARCHITECTURE's pattern. `UnitOfWork.CommitAsync` saves the changes before it commits. *Rejected:* domain services using the DbContext (ARCHITECTURE forbids it).
- **One migration `Groups`: three new tables only** (`groups`, `group_members`, `activity_events`), so Neon can take it before the new code is live. It carries the columns Phase 10 needs (`status`, `closing_started_at`, `finished_at`), so no later migration touches `groups` for them, plus two new columns: `groups.previous_invite_token` (Undo of a reset) and `group_members.end_kind` (`Left`, `Removed`, `SetAside`). `xmin` as a concurrency token waits for Phase 10, the first status change that can race.
- **Soft delete** by explicit `deleted_at IS NULL` filters. *Rejected:* an EF global query filter (hidden behaviour; Recently deleted would need `IgnoreQueryFilters`).
- **Invite token:** 32 bytes from `RandomNumberGenerator`, written base64url (43 characters), stored as plain text so any member can Share it again. *Rejected:* a hashed token (Share would need a new link every time). A reset keeps the old token in `previous_invite_token` until the next reset, so Undo can restore it.
- **Endpoints:** the group id is in the route; the invite token is in a **POST body**, never in the URL (URLs land in Render's request logs). Preview is public, join needs a login. *Rejected:* `GET /api/invites/{token}` (token in the URL).
- **A non-member gets 404 `GROUP_NOT_FOUND`** (the group's existence is not revealed); a non-owner gets 403 `GROUP_NOT_OWNER`.
- **Rate limit on joining:** a named policy `invite`, 20 requests per 10 minutes per visitor address (preview and join together, every request counts), constants in `Register.RateLimiting.cs`. *Rejected:* the `log-in` policy (10 a minute is too tight for family opening links) and no limit (DECISIONS 2026-09-25 asks for one).
- **Joining order** is `joined_at`, then id (the money rules need a stable order).
- **Claim data rules:** a claim at join sets `user_id` and `claimed_at` on the plain-name row and creates no row of its own for the person. **Undo claim** empties them and creates (or restores) the person's own row, so the person stays in the group. "That's me" sets the person's own row aside (`end_kind = SetAside`) and claims the name in one transaction; the one-active-membership index forces that order.
- **Going back to the join screen after sign-in:** router state `{ joinToken }` carried through Welcome, Sign up, Log in and Google's name screen; only a token is accepted, never a free address (no open redirect). A reload in the middle of sign-up lands on the dashboard. *Rejected:* `sessionStorage` (a stale invite could hijack a later, unrelated sign-up).
- **Share:** `navigator.share` where it exists, otherwise copy the link and show "Link copied".
- **Step 2 contract (backend groups, written 2026-10-03 before the tests):**
  - **Create** `POST /api/groups` `{name, emoji, currency}` → 200 with the group. It makes a `Group` kind `Group` (One bill arrives in Phase 8) and the owner's `GroupMember` row (the owner's account name at that moment), writes the `GroupCreated` activity event and the `GroupCreated` usage event, all in one transaction.
  - **List** `GET /api/groups` → `{ groups: [...], finishedGroups: [...], recentlyDeleted: [...] }`. A row has id, name, emoji, defaultCurrency, memberCount. `recentlyDeleted` holds only groups the caller owns, deleted less than 30 days ago, with `deletedAt` and `restorableUntil`. Only people currently in a group (not removed or left) see it in `groups`; a deleted group is only in `recentlyDeleted`.
  - **One group** `GET /api/groups/{groupId}` → id, kind, name, emoji, defaultCurrency, status, ownerUserId, isOwner, memberCount, inviteToken (any current member may share it). A group the caller is not in, a missing id and a deleted group all answer 404 `GROUP_NOT_FOUND`.
  - **Change** `PUT /api/groups/{groupId}` `{name, emoji, currency}` → 204, owner only. **Delete** `DELETE /api/groups/{groupId}` → 204, owner only. **Restore** `POST /api/groups/{groupId}/restore` → 204, owner only, only for a group deleted less than 30 days ago.
  - **Rules and codes:** name trimmed, 1 to 60 characters (`GROUP_NAME_INVALID`); emoji not empty and at most 32 characters, no other checking (`GROUP_EMOJI_INVALID`); currency `MKD` or `EUR` (`GROUP_CURRENCY_INVALID`); not a member 404 `GROUP_NOT_FOUND`; a member who is not the owner 403 `GROUP_NOT_OWNER`; restoring a group that is not deleted 400 `GROUP_NOT_DELETED`; restoring after 30 days 400 `GROUP_RESTORE_EXPIRED`. The check order is: group exists and caller is a current member (404), then owner (403), then input (400).
  - **Events:** `GroupRenamed` when the name changes (with the old and new name in `changes`), `GroupSettingsChanged` when the emoji or the currency changes (old and new in `changes`), `GroupDeleted`, `GroupRestored`; none when nothing changed. A change that changes nothing still answers 204.
  - **The 30 days** are `DeletedGroupRestoreDays = 30`, counted from `deleted_at`, read through `TimeProvider`.
  - **Tables (migration `Groups`, new tables only):** `groups`, `group_members`, `activity_events` as in `DATA-MODEL.md` with the two added columns; `activity_events.expense_id` and `settlement_id` exist as plain nullable `uuid` columns with no foreign key until those tables exist (Phases 8 and 9).
  - **Gaps the tester found, settled by Claude (2026-10-03):** a group id that is not a Guid answers **404** (route constraint `{groupId:guid}`, the same answer as any unknown path; *rejected:* the framework's 400). The `changes` JSON is `[{"field":"name","old":"…","new":"…"}]` with the field names `name`, `emoji` and `defaultCurrency`; an emoji and a currency change in one request write **one** `GroupSettingsChanged` event listing both. A `Closing` group stays in `groups`; only `Finished` goes to `finishedGroups`. The currency is case-sensitive (`MKD`, `EUR`; "mkd" is invalid) and a whitespace-only emoji is invalid. A deleted group is restorable while `now < deleted_at + 30 days` and is listed in `recentlyDeleted` by the same test. When several fields are invalid the first failing code wins in the order name, emoji, currency. Foreign keys exist only on `groups.owner_user_id`, `group_members.group_id`, `group_members.user_id` and `activity_events.group_id`; the "who did it" columns (`created_by_user_id`, `deleted_by_user_id`, `added_by_user_id`, `removed_by_user_id`, `actor_user_id`) and `activity_events.member_id` are plain `uuid` columns, so account deletion later (BACKLOG) cannot be blocked by history. *Rejected:* a foreign key on every user column.
  - **Step 3 contract (backend members and invites, written 2026-10-03 before the tests).** All routes under `/api`, cookie auth, the group id in the route, the invite token only in a POST body. Check order everywhere: the group exists, is not deleted and the caller is a current member (404 `GROUP_NOT_FOUND`), then the owner rule (403 `GROUP_NOT_OWNER`), then the member or input rule (400/404). "Current member" means `removed_at` is empty. A helper `RunInTransactionAsync` (Step 3 part A) wraps every command.
    - **Members list** `GET /groups/{groupId}/members` (any current member) → `{ members, removed, canClaimNames }`. `members` (joined order, then id) rows: `id`, `name` (the stored name: the plain name, or the account name at joining), `displayName` (the account's current name when `userId` is set, else `name`), `userId` (null for a plain name), `isOwner`, `isYou`, `isNameOnly` (no account), `claimedName` (the plain name this person took, null if none: set when `claimed_at` is set). `removed` lists members with `end_kind` Removed, **owner only** (an empty array for everyone else): `id`, `displayName`. `canClaimNames`: true when the caller has an ordinary own current row (not a claimed one) and nothing recorded under it (always true in Phase 7; Phase 8 adds the expense check).
    - **Add a name** `POST /groups/{groupId}/members` `{name}` → 200 the new row (same shape as a `members` row). Any current member. Name trimmed, 1 to 60 characters (`MEMBER_NAME_INVALID`); refused when a current member already shows that name, letter case ignored (`MEMBER_NAME_TAKEN`). Event `MemberAdded`; `added_by_user_id` is the caller.
    - **Remove** `DELETE /groups/{groupId}/members/{memberId}` → 204, owner only. The target must be a current member (404 `MEMBER_NOT_FOUND`); the owner cannot be removed (400 `MEMBER_IS_OWNER`). Sets `removed_at`, `removed_by_user_id`, `end_kind = Removed`. Event `MemberRemoved`.
    - **Let back in** `POST /groups/{groupId}/members/{memberId}/let-back-in` → 204, owner only, only for a row with `end_kind = Removed` (else 404 `MEMBER_NOT_FOUND`); a plain name whose name is now taken answers 400 `MEMBER_NAME_TAKEN`. Clears `removed_at`, `removed_by_user_id`, `end_kind`. Event `MemberLetBackIn`.
    - **Leave** `POST /groups/{groupId}/leave` → 204, any current member with an account except the owner (400 `MEMBER_OWNER_CANNOT_LEAVE`). Sets `removed_at`, `removed_by_user_id` = the caller, `end_kind = Left`. Event `MemberLeft`. Zero-balance checks come in Phase 9.
    - **Make owner** `POST /groups/{groupId}/owner` `{memberId}` → 204, owner only. The target must be a current member (404 `MEMBER_NOT_FOUND`) with an account (400 `MEMBER_NOT_ACCOUNT`) who is not already the owner (400 `MEMBER_ALREADY_OWNER`). Sets `groups.owner_user_id`; the old owner stays a normal member. Event `OwnershipTransferred` (`member_id` = the new owner).
    - **That's me (claim)** `POST /groups/{groupId}/members/{memberId}/claim` → 204, any current member with an account. The target must be a current plain name (no `user_id`, not removed; else 404 `MEMBER_NOT_FOUND`); the caller must have an ordinary own current row (not already holding a claimed name) and `canClaimNames` true (else 400 `MEMBER_CANNOT_CLAIM`). The caller's own row is set aside (`removed_at`, `removed_by_user_id` = the caller, `end_kind = SetAside`), then the target row gets `user_id` = the caller and `claimed_at`. **The two writes must be separate saves in that order** (the unique one-active-row-per-person index would otherwise fail). Event `MemberClaimed` (`member_id` = the target).
    - **Undo claim** `POST /groups/{groupId}/members/{memberId}/undo-claim` → 204, owner only. The target must be a current row (404 `MEMBER_NOT_FOUND`) with `claimed_at` set (400 `MEMBER_NOT_CLAIMED`). The target row becomes a plain name again (`user_id` and `claimed_at` cleared); the person stays in the group: their set-aside row is restored if one exists (the latest `SetAside` row of that user in the group), otherwise a new own row is created (account name now, `joined_at` now). Clear the target first, then restore (same index reason). Event `ClaimUndone` (`member_id` = the target).
    - **Reset link** `POST /groups/{groupId}/invite/reset` → 200 `{ inviteToken }`, owner only: a new token, the old one saved in `previous_invite_token`, `invite_token_created_at` now. Event `InviteLinkReset`. **Undo** `POST /groups/{groupId}/invite/undo-reset` → 200 `{ inviteToken }`, owner only: `invite_token` becomes `previous_invite_token`, `previous_invite_token` is cleared, `invite_token_created_at` now; with no previous token 400 `INVITE_NOTHING_TO_UNDO`. Event `InviteLinkRestored`. After a reset the old link answers `INVITE_NOT_FOUND`.
    - **Preview** `POST /invites/preview` `{token}` → 200 (public, `[AllowAnonymous]` on the action, rate limited): `{ status, groupId, name, emoji, memberNames, unclaimedNames }`. `status` is `Open` (can join; also for anonymous callers), `AlreadyMember` (the caller is a current member; `groupId` is set only then), or `Removed` (the caller has an `end_kind = Removed` row; they cannot join). `memberNames` are the display names of current members, `unclaimedNames` are `{ id, name }` of current plain names. An unknown or old token or a deleted group answers 404 `INVITE_NOT_FOUND`.
    - **Join** `POST /invites/join` `{token, claimMemberId?}` → 200 `{ groupId }` (signed in, rate limited). Unknown or old token or deleted group: 404 `INVITE_NOT_FOUND`; an `end_kind = Removed` row: 403 `INVITE_REMOVED`; already a current member: 200 with the group id and nothing written (and `claimMemberId` is ignored); a person who `Left` comes back on their old row (no new row); a new person gets a row (their account name, `joined_at` now, `added_by_user_id` = themselves). With `claimMemberId` (allowed only for a person with no row yet, else 400 `MEMBER_CANNOT_CLAIM`) the target must be a current plain name (else 404 `MEMBER_NOT_FOUND`) and the person gets **no row of their own**: the target row gets `user_id` and `claimed_at`. Events: `MemberJoined` (and `MemberClaimed` when claiming; `member_id` is the row), usage event `JoinedViaInvite` for every real join or rejoin, none for "already a member". An account whose name equals a plain name may still join without claiming (two rows may show the same name).
    - **Rate limit:** a named policy `invite`, 20 requests per 10 minutes per visitor address, one counter shared by preview and join, every request counts, constants in `Register.RateLimiting.cs`; the 429 answer is the existing `RATE_LIMITED` with `Retry-After`.
    - **Points the tester found, settled by Claude (2026-10-03):** `memberNames` in the preview includes plain names (a plain name is also in `unclaimedNames`). `groupId` is absent (null) unless the status is `AlreadyMember`; a `Removed` preview carries `status` and the group's name and emoji. A missing body, `{}` or `{"token":null}` answers the normal 400 of `[ApiController]`; a blank or whitespace token answers 404 `INVITE_NOT_FOUND`. Claim error order: unknown or invalid target first (404 `MEMBER_NOT_FOUND`), then the caller's own rule (400 `MEMBER_CANNOT_CLAIM`). Join: `INVITE_REMOVED` (403) comes before any claim rule. Let back in checks name conflicts for plain names only (an account's name may coincide with another's). The owner may claim a name; ownership belongs to the account (`groups.owner_user_id`), so it is not affected. A person who holds a claimed name and leaves goes out on that claimed row (`Left`) and comes back on it. Event `data` for the member and invite events is `{"name":"<the member's display name at that moment>"}` (also `"claimedName"` for `MemberClaimed` and `ClaimUndone`, and the new owner's name for `OwnershipTransferred`); `InviteLinkReset` and `InviteLinkRestored` carry no data (tokens never go into the feed).
    - **New codes:** `MEMBER_NAME_INVALID`, `MEMBER_NAME_TAKEN`, `MEMBER_NOT_FOUND` (404), `MEMBER_IS_OWNER`, `MEMBER_OWNER_CANNOT_LEAVE`, `MEMBER_NOT_ACCOUNT`, `MEMBER_ALREADY_OWNER`, `MEMBER_CANNOT_CLAIM`, `MEMBER_NOT_CLAIMED`, `INVITE_NOT_FOUND` (404), `INVITE_REMOVED` (403), `INVITE_NOTHING_TO_UNDO` (400).
  - **Invite token** is made by an `IInviteTokenGenerator` (Domain interface, Infrastructure implementation, 32 random bytes as 43 base64url characters); tests use a fake in the domain tests.
  - **Step 4 contract (frontend groups, written 2026-10-05 before the tests; Filip said "go" to the plan and its Macedonian lines).**
    - **Routes:** `/groups`, `/groups/new`, `/groups/recently-deleted`, `/groups/:groupId`, `/groups/:groupId/settings`, with path builders in `routes.ts`. A layout route under `RequireAuth` draws the bottom bar around Home, Groups, Recently deleted and Settings only (not New group, the group screen or Group settings). Settings loses its Back button.
    - **Service** `core/services/groups/groupsService.ts`: list, get, create, update, delete, restore, leave, addMember, each answer checked by a hand-written parser that throws a clear message (the `parseMe` style). Paths in `endpoints.ts`. No `DELETE` in `jsonRequest`: neither DELETE call has a body, so they use `{ method: 'DELETE' }` (corrects the earlier note that `jsonRequest` gains DELETE). *Rejected:* generated OpenAPI types (clash with TS 7, ARCHITECTURE Part 4).
    - **Query keys** `['groups']` and `['groups', id]`; changes invalidate the list and the group; a deleted group's query is removed.
    - **Error codes** added to `errors.ts` with en + mk texts: `GROUP_NOT_FOUND`, `GROUP_NOT_OWNER`, `GROUP_NAME_INVALID`, `GROUP_EMOJI_INVALID`, `GROUP_CURRENCY_INVALID`, `GROUP_NOT_DELETED`, `GROUP_RESTORE_EXPIRED`, `MEMBER_NAME_INVALID`, `MEMBER_NAME_TAKEN` (takes the typed name), `MEMBER_OWNER_CANNOT_LEAVE`. The other Step 3 codes come in Step 5.
    - **Shared pieces:** `KvitTabBar`, `KvitSheet` (Base UI Dialog, slides up about 0.2 s, none under reduce motion), `KvitChoiceChips`, `KvitEmojiTile`, `showUndoToast` / `showDangerToast` in `shared/toasts/` (Sonner `duration: 4000`, `action`), `GroupFields` in `features/groups/shared/`.
    - **Tokens:** `--danger` `#b71f1f` light / `#ff6b64` dark; `--danger-fill` `#b71f1f` light / `#d12828` dark with white text; selected dark tab `#b54a00` with white text. `--destructive` unchanged.
    - **Share:** `navigator.share` if present, else clipboard copy and "Link copied"; link `<site>/join/<token>`. Closing the share menu does nothing; other failures show the error toast.
    - **Dates:** "Can be restored until" is day, month, year; English uses `en-GB` for this one date (the single exception to "language only, no region tag").
    - **Leave** is built in Step 4 on Group settings (one call; owner sees it disabled with the hand-over line); a leave toast is wine red with no Undo.
    - **Macedonian "Врати"** is used for both Undo and Restore (Filip approved with the plan). *Rejected:* "Поништи" for Undo.

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
- **After login** the app opens on the **dashboard**, with a big "+" button that adds to the last-used group.
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
