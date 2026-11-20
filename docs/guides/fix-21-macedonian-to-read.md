# Macedonian texts for Filip to read

For reading on the phone. Each row is one text: the English, then the Macedonian draft. Mark anything that reads wrong (write the row number and your wording in the chat); nothing here counts as final until you have read it. Made on 2026-11-19 from `en.json` and `mk.json` on `fix/21-small-bugs`.

Already confirmed by Filip, not repeated here: the "Someone else just changed this expense" message (2026-11-19).

The five currency errors (expense, group, personal entry, repeating income, payment) all got the same new text, so only the group one is listed.

## 1. New in Fix 21 Step 5 (2026-11-19)

| # | English | Macedonian |
|---|---|---|
| 1 | Plan reached | Планот е достигнат |
| 2 | Someone just took this name, or it was removed. | Некој штотуку го зеде ова име или е отстрането. |
| 3 | You're already in this bill. | Ти веќе си во сметката. |
| 4 | {{name}} restored “{{title}}” | {{name}} го врати „{{title}}“ |
| 5 | That's not a group number. Copy the whole number from the report email. | Ова не е број на група. Копирај го целиот број од е-поштата за пријава. |
| 6 | Type at least 2 letters. | Внеси барем 2 букви. |
| 7 | At most 500 characters. | Најмногу 500 знаци. |
| 8 | Pick MKD or EUR. | Избери денари или евра. |
| 9 | Type a plan amount above 0 (whole denars for MKD). | Внеси износ за планот поголем од 0 (цели денари). |
| 10 | Rate: 1 EUR = {{rate}} MKD · {{date}} | Цена на еврото: 1 EUR = {{rate}} ден. · {{date}} |

## 2. Privacy notice, the two changed sections (Step 5)

| # | English | Macedonian |
|---|---|---|
| 11 | Your data stays while your account exists. When you delete your account it is switched off at once and removed after 30 days. Removals that are due happen the first time someone uses Kvit after that day, so if nobody uses Kvit for a while, they can come a little later. A deleted group or expense can be restored for a while, and is removed 30 days after that. After 12 months, usage events are no longer tied to an account. The companies above keep technical logs for a short time, from days to a few weeks. Kvit's database can be restored up to 6 hours back, and Kvit keeps an encrypted weekly backup for up to 90 days. Accounts that are not used are kept and are not deleted automatically. | Твоите податоци остануваат додека постои профилот. Кога ќе го избришеш профилот, тој веднаш се исклучува и се отстранува по 30 дена. Отстранувањата што се на ред се случуваат првиот пат кога некој ќе го користи Kvit по тој ден, па ако никој не го користи Kvit некое време, може да дојдат малку подоцна. Избришана група или трошок може да се врати некое време, а потоа се отстранува по 30 дена. По 12 месеци, настаните за користење повеќе не се врзани за профил. Компаниите погоре чуваат технички дневници кратко време, од денови до неколку недели. Базата на Kvit може да се врати до 6 часа наназад, а Kvit чува шифрирана неделна резервна копија до 90 дена. Профилите што не се користат се чуваат и не се бришат автоматски. |
| 12 | In Settings, tap “Download my data”. You get a file with your profile, your personal budget and your groups with their expenses, payments and activity. It does not contain other people's emails or private budgets. Groups you deleted in the last 30 days are not in the file, though Kvit keeps them until they are removed. The file also does not list the days you opened Kvit, which Kvit counts for its usage numbers. | Во Поставки допри „Преземи ги моите податоци“. Добиваш датотека со твојот профил, личниот буџет и твоите групи со нивните трошоци, плаќања и активности. Во неа нема туѓи е-пошти ниту приватни буџети. Групите што ги избриша во последните 30 дена не се во датотеката, иако Kvit ги чува додека не се отстранат. Во датотеката ги нема ни деновите кога го отвори Kvit, што Kvit ги брои за бројките за користењето. |
| 13 | Last updated 19 November 2026 · version 2026-11-19 | Последно ажурирано на 19 ноември 2026 · верзија 2026-11-19 |

## 3. The server bar (Fix 20)

| # | English | Macedonian |
|---|---|---|
| 14 | The server is asleep. | Серверот спие. |
| 15 | Wake the server | Разбуди го серверот |

## 4. Income and repeating income (Phase 14, never read by Filip)

| # | English | Macedonian |
|---|---|---|
| 16 | Income has no category. | Приходот нема категорија. |
| 17 | This repeating income doesn't exist or was stopped. | Овој редовен приход не постои или е запрен. |
| 18 | Pick MKD or EUR. | Избери денари или евра. |
| 19 | The amount must be more than 0. | Износот мора да е поголем од 0. |
| 20 | This amount is too large. | Овој износ е премногу голем. |
| 21 | The name can have up to 60 characters. | Името може да има најмногу 60 знаци. |
| 22 | Pick monthly or weekly. | Избери месечно или неделно. |
| 23 | Pick the day it comes. | Избери ден кога доаѓа. |
| 24 | Pick a start date from the last 24 months, up to one year ahead. | Избери почеток од последните 24 месеци, најдоцна една година однапред. |
| 25 | The end date must be after the start, at most 10 years later. | Крајот мора да е по почетокот, најмногу 10 години подоцна. |
| 26 | The start date can't change once income was added from it. | Почетокот не може да се смени откако е додаден приход од него. |
| 27 | This request was already used for something else. | Ова барање веќе е искористено за друго. |
| 28 | You can have up to 20 repeating incomes. Stop one first. | Можеш да имаш најмногу 20 редовни приходи. Прво запри еден. |
| 29 | This repeating income is not stopped. | Овој редовен приход не е запрен. |
| 30 | This repeating income was stopped more than 5 days ago and can't be restarted. | Овој редовен приход е запрен пред повеќе од 5 дена и не може повторно да се вклучи. |
| 31 | What do you add? | Што додаваш? |
| 32 | Spending | Трошење |
| 33 | Income | Приход |
| 34 | New income | Нов приход |
| 35 | Income | Приход |
| 36 | Income | Приход |
| 37 | Repeats: Never | Се повторува: никогаш |
| 38 | Repeats: Monthly | Се повторува: месечно |
| 39 | Repeats: Weekly | Се повторува: неделно |
| 40 | Starts: {{day}} | Почнува: {{day}} |
| 41 | Ends: never | Завршува: никогаш |
| 42 | Ends: {{day}} | Завршува: {{day}} |
| 43 | When does it end? | Кога завршува? |
| 44 | Never | Никогаш |
| 45 | In 3 months | За 3 месеци |
| 46 | In 6 months | За 6 месеци |
| 47 | In 1 year | За 1 година |
| 48 | Pick a date… | Избери датум… |
| 49 | Pick a start date from the last 24 months, up to one year ahead. | Избери почеток од последните 24 месеци, најдоцна една година однапред. |
| 50 | Pick an end date from the start, up to 10 years later. | Избери крај од почетокот, најмногу 10 години подоцна. |
| 51 | The start date can't change once entries exist. | Почетокот не може да се смени откако веќе има ставки. |
| 52 | This date comes from a repeating income. | Овој датум доаѓа од редовен приход. |
| 53 | Repeating income | Редовен приход |
| 54 | Repeating income · {{count}} | Редовен приход · {{count}} |
| 55 | Repeating income · none yet | Редовен приход · сѐ уште нема |
| 56 | +{{count}} more | уште {{count}} |
| 57 | +{{count}} more | уште {{count}} |
| 58 | Every month on the {{ordinal}} | Месечно, на {{ordinal}} |
| 59 | Every month on the {{ordinal}} (or the last day of shorter months) | Месечно, на {{ordinal}} (или последниот ден во пократките месеци) |
| 60 | Every week on {{weekday}} | Неделно: {{weekday}} |
| 61 | next {{day}} | следно {{day}} |
| 62 | Stop | Запри |
| 63 | Stopped | Запрено |
| 64 | No repeating income yet | Сѐ уште немаш редовен приход |
| 65 | + New repeating income | + Нов редовен приход |
| 66 | Couldn't update repeating income. Try again later. | Редовниот приход не можеше да се ажурира. Обиди се подоцна. |
| 67 | Sunday | Недела |
| 68 | Monday | Понеделник |
| 69 | Tuesday | Вторник |
| 70 | Wednesday | Среда |
| 71 | Thursday | Четврток |
| 72 | Friday | Петок |
| 73 | Saturday | Сабота |
| 74 | Left this month | Остануваат овој месец |
| 75 | Income | Приход |
| 76 | Spent | Потрошено |

## 5. New in Fix 21 Step 6 (2026-11-20)
| # | English | Macedonian |
|---|---|---|
| 77 | Someone called “Marko” is already in this bill. | Некој по име „Marko“ е веќе во сметката. |
| 78 | Join “Trip” on Kvit. (the text sent with the invite link) | Придружи се на групата „Trip“ во Kvit. |
