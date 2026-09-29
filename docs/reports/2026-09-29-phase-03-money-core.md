# Report: Phase 3, money core (2026-09-29)

## What was asked
Build the money rules from `DATA-MODEL.md` → "Money rules" as plain C# with no database, no API and no screens (ROADMAP Phase 3): amounts in two currencies, rounding, the four ways to split an expense, balances, and "who pays whom". Later phases (8 Expenses, 9 Settle up, 10 Closing) call this code.

## What was built
Written by the `coder` subagent (Opus) on the branch `feat/03-money-core`. Nothing is committed: Filip commits.

| Part | File(s) in `src/api/Kvit.Domain/MoneyRules/` | What it does |
|---|---|---|
| Currencies | `Currency.cs`, `CurrencyRules.cs` | MKD and EUR. MKD is split to the whole denar (100 deni), EUR to the cent. Also writes amounts for error messages ("3,000", "10.00") |
| Money | `Money.cs` | An amount plus its currency. Can only be made through `Money.Create`, which refuses e.g. 120.50 MKD. Adding MKD to EUR is a failure, not a conversion |
| Splitting | `SplitType.cs`, `SplitInput.cs`, `SplitShare.cs`, `Splitter.cs` | Equal (+ extras), Exact, Percentage, Shares. Every result adds up exactly to the total; the rounding leftover goes to the payer |
| Balances | `SettlementStatus.cs`, `BalanceExpense.cs`, `BalanceSettlement.cs`, `MemberBalance.cs`, `BalanceSummary.cs`, `Balances.cs` | Each member's balance per currency, and "is everyone kvit?", from one call |
| Who pays whom | `Payment.cs`, `DebtSimplifier.cs` | Turns balances into a short list of payments |
| Error codes | `src/api/Kvit.Domain/Results/ResultCodes.cs` | 9 new codes (listed in `DECISIONS.md`, 2026-09-29) |
| Tests | `tests/Kvit.Domain.Tests/MoneyRules/` (12 files: 11 test classes + `MoneyTestData.cs` with shared test helpers) | 139 new tests (127, plus 12 from the code-review follow-up) |

## The rules, with the examples that are tested
All of these are **VERIFIED by automated test**, in both MKD and EUR unless the example names one currency.

| Rule | Example tested |
|---|---|
| Equal with extras | Hotel 3,000 MKD, 5 people, Marko +600 → Marko 1,080, everyone else 480 |
| Payer takes the leftover | 1,000 MKD among 3 → payer 334, others 333 |
| Payer takes the whole leftover, even more than one step | 1,000 MKD among 7 → payer 148, others 142 |
| Payer not sharing → first listed person gets the leftover | Taxi 1,000 MKD, Filip paid, Ana/Marko/Bojan share → Ana 334 |
| EUR to the cent | 10.00 EUR among 3 → payer 3.34 |
| Extras too big | Marko +1,200 on 1,000 → fails, message names 1,200 and 200 |
| Exact must add up | 2,700 of 3,000 MKD → fails with "Split adds up to 2,700 of 3,000 MKD; 300 left to assign." |
| Percentages must total 100 % | 33.33 × 3 → fails with "Percentages add up to 99.99 %, not 100 %; 0.01 % left to assign." |
| Listed at 0 still counts | Shares Filip 0, Ana 1, Marko 1, Bojan 1 on 1,000 MKD, Filip paid → Filip 1, others 333 (same for 0 %) |
| All shares 0 | Fails with `EXPENSE_SPLIT_NO_SHARES` |
| Balances | Filip pays 3,000 for 3, Ana pays 600 for 2, Marko pays Filip 500 → Filip +1,500, Ana −700, Marko −800 |
| Only confirmed, non-deleted payments count; deleted expenses don't | Pending, rejected, cancelled and deleted payments change nothing |
| MKD and EUR never mix | Separate balances per currency |
| "Everyone's kvit" | False while a payment is pending (even at all zeros); false when only EUR is still owed; true when everything is settled; true when nothing counts and nothing is pending |
| Impossible amounts are refused | An expense of 0 or less, a negative share, a payment of 0 or less, or a payment to yourself stops with a clear error naming the person and the amount; deleted rows are skipped |
| Who pays whom | The example above → Marko pays Filip 800, Ana pays Filip 700. Ties go to whoever joined first. Same input → same list every time |

**Random checks** (a fixed starting number, so every run tests exactly the same cases):
- 2,000 random splits for each of the 4 split types × 2 currencies: the shares always add up to the total, none is negative, all are whole denars in MKD, and only the payer (or the first listed person) gets the leftover, which is always smaller than one step per person. The rounding is checked with the test's own rule, not a copy of the splitter's formulas (see the follow-up below). **VERIFIED by automated test.**
- 500 random groups with expenses in both currencies and payments in every status: balances always add up to 0 in each currency. **VERIFIED by automated test.**
- 2,000 random sets of balances per currency: paying the "who pays whom" list brings everyone to 0, with at most (people − 1) payments. **VERIFIED by automated test.**

## Verification
| Check | Result | Label |
|---|---|---|
| `dotnet build Kvit.slnx` | Build succeeded, 0 warnings, 0 errors | **VERIFIED by live run** (coder) |
| `dotnet test Kvit.slnx` | First build: 153 total, 153 passed, 0 failed (Domain 138 = 11 earlier + 127 new; Api 15). After the code-review follow-up: 165 total, 165 passed, 0 failed (Domain 150 = 11 earlier + 139 new; Api 15) | **VERIFIED by automated test** (coder) |
| The tests really catch mistakes | Breaking the leftover rule on purpose (always the first person) made 9 tests fail; the break was then undone | **VERIFIED by live run** (coder) |
| `with { ... }` can't bypass `Money.Create` | A throwaway file trying it failed to build (CS0200), then was deleted | **VERIFIED by live run** (coder) |
| No comments, no `var`, no `decimal`/`double` | 0 matches for `//`, `var`, `decimal`, `double`, `float` in the new `.cs` files | **VERIFIED by search** (coder) |
| CI on GitHub | Not pushed yet | **NOT VERIFIED** |

## Where the code differs from the plan
- **"Total off the step" test:** the splitter can't receive such a total, because `Money.Create` already refuses it. The test in `SplitValidationTests` checks exactly that (120.50 MKD can't be made) instead of calling the splitter.
- **Two extra safety checks in `Balances`:** an expense whose shares are in a different currency, or don't add up to its amount, throws with a clear message. Without them, a damaged row would silently show wrong balances. Logged in `DECISIONS.md`.
- **A currency only appears in the balances when a counted row uses it** (the plan said "each currency that appears" without saying whether deleted rows count). Logged in `DECISIONS.md`.
- **`CurrencyRules.FormatAmount`** was added so error messages can name amounts ("2,700 of 3,000 MKD"), which the plan asked for. It's for messages only; the website formats amounts itself.

## Follow-up after code review
Done by the `coder` subagent on the same branch, same day. Details and reasons in `DECISIONS.md` (2026-09-29, follow-up).

| Change | Label |
|---|---|
| **One call for balances and "everyone's kvit".** `Balances.Calculate` now returns a `BalanceSummary` with the balances per currency and `IsEveryoneKvit`, both from the same input. The separate `IsEveryoneKvit` function is gone, so the two can't get out of step. All earlier balance tests kept, rewritten for the new shape | **VERIFIED by automated test** |
| **"Everyone's kvit" with nothing counted:** no counted rows and no pending payment → kvit; a pending payment with nothing else → not kvit (2 new tests) | **VERIFIED by automated test** |
| **Impossible amounts are refused** with an error naming the person and the amount: expense of 0, negative expense, negative share, payment of 0, negative payment, payment to yourself (confirmed and pending payments both checked); deleted bad rows are skipped (10 new tests) | **VERIFIED by automated test** |
| **The random split test no longer copies the splitter's rounding.** It checks each person against the exact fair part using whole-number cross-multiplication instead | **VERIFIED by automated test** |
| **The new check really catches rounding mistakes.** Switching the splitter to round to the nearest step (Percentage and Shares) made the 4 matching random tests fail with "person 0 is not rounded down to the step" (no example test noticed that one); making the equal part one step too low (Equal) made the 2 Equal random tests fail the same way (22 example tests failed too). Both breaks were undone, and `Splitter.cs` was confirmed byte-for-byte identical to before (same SHA-256 hash; `git diff` can't show it because the file isn't committed yet) | **VERIFIED by live run** |
| **Rule for Phase 9:** the member list given to `Balances.Calculate` must include removed and left members too, or it throws. Added to `ROADMAP.md` Phase 9 | **NOT VERIFIED** (a rule for later code; nothing to run yet) |
| `dotnet build Kvit.slnx`: 0 warnings, 0 errors; `dotnet test Kvit.slnx`: 165/165 pass | **VERIFIED by live run** / **VERIFIED by automated test** |
| No `//`, `var`, `decimal`, `double`, `float` in the changed `.cs` files | **VERIFIED by search** |

## New concepts in this phase (plain words)
- **Record:** a small C# class that just holds values. Two records with the same values count as equal, which makes tests simple.
- **`checked` arithmetic:** if a sum gets too big for the number type, .NET throws an error instead of silently wrapping round to a wrong (even negative) number.
- **`Int128`:** a whole-number type twice as big as `long`, used for in-between sums and products, so they can't overflow.
- **Seeded random tests:** the test makes thousands of random examples from a fixed starting number, so they are the same on every run and a failure can be repeated.

## Next
Filip commits, pushes `feat/03-money-core`, opens the pull request, checks CI turns green and merges. Then Phase 4 (database + email accounts).
