import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import { translated } from '@/test/translated'
import { createI18n } from './i18n'
import en from './locales/en.json'
import mk from './locales/mk.json'

const approvedTexts: [string, string, string][] = [
  ['common.privacy', 'Privacy', 'Приватност'],
  ['common.close', 'Close', 'Затвори'],
  ['welcome.or', 'or', 'или'],
  ['welcome.googleUnavailable', "Couldn't load Google sign-in. Check your connection and try again.", 'Не можевме да ја вчитаме најавата со Google. Провери ја врската и обиди се повторно.'],
  ['auth.googleTaken.title', 'This email already has an account.', 'Оваа е-пошта веќе има профил.'],
  ['auth.googleTaken.logIn', 'Log in with password', 'Најави се со лозинка'],
  ['auth.googleTaken.another', 'Use another Google account', 'Користи друг Google профил'],
  ['auth.googleUses.title', 'This account signs in with Google.', 'Овој профил се најавува преку Google.'],
  ['auth.googleUses.body', 'You can add a password in Settings after you sign in.', 'Лозинка можеш да додадеш во Поставки откако ќе се најавиш.'],
  ['auth.googleSignUp.title', 'Almost there', 'Уште малку'],
  ['auth.googleSignUp.hint', 'This is how your friends will see you in groups.', 'Вака ќе те гледаат пријателите во групите.'],
  ['auth.googleSignUp.submit', 'Continue', 'Продолжи'],
  ['auth.setPassword.title', 'Set a password', 'Постави лозинка'],
  ['auth.setPassword.submit', 'Save password', 'Зачувај ја лозинката'],
  ['auth.setPassword.done', 'Password saved. You can now also log in with your email and password.', 'Лозинката е зачувана. Сега можеш да се најавуваш и со е-пошта и лозинка.'],
  ['privacy.title', 'Privacy', 'Приватност'],
  ['privacy.updated', 'Last updated 3 October 2026.', 'Последно ажурирано на 3 октомври 2026.'],
  ['privacy.sections.1.heading', 'What Kvit keeps', 'Што чува Kvit'],
  ['privacy.sections.1.body', 'Your name, email, language and time zone. The groups, expenses and payments that you or your friends enter. Simple counts of how the app is used (for example how many people signed up), never amounts or names.', 'Твоето име, е-пошта, јазик и временска зона. Групите, трошоците и плаќањата што ги внесуваш ти или твоите пријатели. Едноставни бројки за користењето на апликацијата (на пример, колку луѓе се регистрирале), никогаш износи или имиња.'],
  ['privacy.sections.2.heading', 'Passwords', 'Лозинки'],
  ['privacy.sections.2.body', 'A password is never stored the way you type it, only as a scrambled code that nobody can turn back into the password.', 'Лозинката никогаш не се чува онака како што ја внесуваш, туку само како измешан код што никој не може да го врати во лозинка.'],
  ['privacy.sections.3.heading', 'Sign in with Google', 'Најава со Google'],
  ['privacy.sections.3.body', 'If you use Google, Kvit receives only your name, email address and profile picture link. Kvit cannot read your Gmail, your contacts or anything else in your Google account.', 'Ако користиш Google, Kvit добива само твоето име, адреса на е-пошта и линк до профилната слика. Kvit не може да ја чита твојата Gmail-пошта, контактите или нешто друго во твојот Google профил.'],
  ['privacy.sections.4.heading', 'Who can see what', 'Кој што може да види'],
  ['privacy.sections.4.body', "People in a group see that group's expenses and balances. The person who runs Kvit sees only totals (for example how many accounts exist), not your expenses.", 'Луѓето во една група ги гледаат трошоците и салдата на таа група. Тој што го води Kvit гледа само вкупни бројки (на пример, колку профили има), не и твоите трошоци.'],
  ['privacy.sections.5.heading', 'Cookies and tracking', 'Колачиња и следење'],
  ['privacy.sections.5.body', 'One login cookie keeps you signed in for 90 days. No ads, no tracking, and your data is never sold or shared.', 'Еден колач за најава те одржува најавен 90 дена. Без реклами, без следење, а твоите податоци никогаш не се продаваат ниту се споделуваат.'],
  ['privacy.sections.6.heading', 'Where it is stored', 'Каде се чува'],
  ['privacy.sections.6.body', 'On Cloudflare (the website), Render and Neon (the data, in Frankfurt, Germany).', 'Кај Cloudflare (веб-страницата), Render и Neon (податоците, во Франкфурт, Германија).'],
  ['privacy.sections.7.heading', 'Deleting your account', 'Бришење на профилот'],
  ['privacy.sections.7.body', "Ask Filip to delete your account. Your name stays on old group expenses, so other people's balances stay correct.", 'Побарај од Filip да го избрише твојот профил. Твоето име останува на старите трошоци во групите, за салдата на другите да останат точни.'],
]

const approvedPhase7Texts: [string, string, string][] = [
  ['nav.home', 'Home', 'Почетна'],
  ['nav.groups', 'Groups', 'Групи'],
  ['nav.settings', 'Settings', 'Поставки'],
  ['common.undo', 'Undo', 'Врати'],
  ['groups.title', 'Groups', 'Групи'],
  ['groups.newButton', 'New group', 'Нова група'],
  ['groups.empty', 'You have no groups yet. Create one, or open a link a friend sent you.', 'Сè уште немаш групи. Направи една или отвори линк што ти го испратил пријател.'],
  ['groups.people_one', '{{count}} person', '{{count}} лице'],
  ['groups.people_other', '{{count}} people', '{{count}} лица'],
  ['groups.finished', 'Finished', 'Завршени'],
  ['groups.finishedReadOnly', 'Finished · read-only', 'Завршена · само за читање'],
  ['groups.recentlyDeletedLink', 'Recently deleted', 'Неодамна избришани'],
  ['recentlyDeleted.title', 'Recently deleted', 'Неодамна избришани'],
  ['recentlyDeleted.intro', "Groups you delete stay here for 30 days. Only the group's owner can restore one.", 'Групите што ќе ги избришеш остануваат тука 30 дена. Само сопственикот на групата може да ја врати.'],
  ['recentlyDeleted.empty', 'Nothing was deleted in the last 30 days.', 'Во последните 30 дена ништо не е избришано.'],
  ['recentlyDeleted.restore', 'Restore', 'Врати'],
  ['recentlyDeleted.restorableUntil', 'Can be restored until {{date}}', 'Може да се врати до {{date}}'],
  ['recentlyDeleted.restored', 'Group restored', 'Групата е вратена'],
  ['groupFields.name', 'Group name', 'Име на групата'],
  ['groupFields.namePlaceholder', 'Greece trip', 'Патување во Грција'],
  ['groupFields.emoji', 'Emoji', 'Емоџи'],
  ['groupFields.currency', 'Currency', 'Валута'],
  ['groupFields.currencyHint', 'New expenses use this currency.', 'Новите трошоци ја користат оваа валута.'],
  ['groupFields.nameInvalid', 'Enter a name (up to 60 characters).', 'Внеси име (најмногу 60 знаци).'],
  ['newGroup.title', 'New group', 'Нова група'],
  ['newGroup.create', 'Create group', 'Направи група'],
  ['group.addPeople', 'Add people', 'Додај луѓе'],
  ['group.addPeopleNoKvit', 'No Kvit? Add them as a name.', 'Немаат Kvit? Додај ги како име.'],
  ['group.addPeopleHasKvit', 'Have Kvit? Share the link and they join with their own account.', 'Имаат Kvit? Сподели го линкот и ќе се придружат со свој профил.'],
  ['group.addName', 'Add a name', 'Додај име'],
  ['group.shareLink', 'Share invite link', 'Сподели линк за покана'],
  ['group.linkCopied', 'Link copied', 'Линкот е копиран'],
  ['group.settings', 'Group settings', 'Поставки на групата'],
  ['addName.title', 'Add a name', 'Додај име'],
  ['addName.label', 'Name', 'Име'],
  ['addName.hint', 'For someone without Kvit, like Grandma.', 'За некој без Kvit, на пример баба.'],
  ['addName.submit', 'Add', 'Додај'],
  ['groupSettings.title', 'Group settings', 'Поставки на групата'],
  ['groupSettings.save', 'Save', 'Зачувај'],
  ['groupSettings.saved', 'Saved', 'Зачувано'],
  ['groupSettings.delete', 'Delete group', 'Избриши ја групата'],
  ['groupSettings.deleted', 'Group deleted', 'Групата е избришана'],
  ['groupSettings.leave', 'Leave group', 'Напушти ја групата'],
  ['groupSettings.left', 'You left {{name}}', 'Ја напушти групата „{{name}}“'],
  ['groupSettings.ownerCannotLeave', 'You are the owner. Make someone else the owner before you leave.', 'Ти си сопственик. Направи некој друг сопственик пред да ја напуштиш.'],
  ['errors.GROUP_NOT_FOUND', "This group doesn't exist or you're no longer in it.", 'Оваа група не постои или повеќе не си во неа.'],
  ['errors.GROUP_NOT_OWNER', "Only the group's owner can do this.", 'Ова може да го направи само сопственикот на групата.'],
  ['errors.GROUP_NAME_INVALID', 'Enter a name (up to 60 characters).', 'Внеси име (најмногу 60 знаци).'],
  ['errors.GROUP_EMOJI_INVALID', 'Pick an emoji.', 'Избери емоџи.'],
  ['errors.GROUP_CURRENCY_INVALID', 'Pick MKD or EUR.', 'Избери MKD или EUR.'],
  ['errors.GROUP_NOT_DELETED', 'This group is not deleted.', 'Оваа група не е избришана.'],
  ['errors.GROUP_RESTORE_EXPIRED', "This group was deleted more than 30 days ago and can't be restored.", 'Оваа група е избришана пред повеќе од 30 дена и не може да се врати.'],
  ['errors.MEMBER_NAME_INVALID', 'Enter a name (up to 60 characters).', 'Внеси име (најмногу 60 знаци).'],
  ['errors.MEMBER_NAME_TAKEN', 'Someone called “{{name}}” is already in this group.', 'Некој по име „{{name}}“ е веќе во групата.'],
  ['errors.MEMBER_OWNER_CANNOT_LEAVE', 'You are the owner. Make someone else the owner before you leave.', 'Ти си сопственик. Направи некој друг сопственик пред да ја напуштиш.'],
]

const approvedPhase7Step5Texts: [string, string, string][] = [
  ['members.title', 'Members', 'Членови'],
  ['members.owner', 'Owner', 'Сопственик'],
  ['members.you', 'You', 'Ти'],
  ['members.nameOnly', 'Just a name', 'Само име'],
  ['members.tookName', 'Took the name “{{name}}”', 'Го зеде името „{{name}}“'],
  ['members.addNameLink', '+ Add a name', '+ Додај име'],
  ['members.invite.title', 'Invite link', 'Линк за покана'],
  ['members.invite.share', 'Share', 'Сподели'],
  ['members.invite.copy', 'Copy link', 'Копирај линк'],
  ['members.invite.reset', 'Reset link', 'Ресетирај линк'],
  ['members.invite.resetDone', 'Link reset. The old link no longer works.', 'Линкот е ресетиран. Стариот повеќе не работи.'],
  ['members.makeOwner', 'Make owner', 'Направи сопственик'],
  ['members.ownerNow', '{{name}} is now the owner', '{{name}} сега е сопственик'],
  ['members.remove', 'Remove from group', 'Отстрани од групата'],
  ['members.removed', 'Removed: {{name}}', 'Отстрането: {{name}}'],
  ['members.undoClaim', 'Undo claim', 'Врати го името'],
  ['members.claimUndone', '{{name}} stays in the group. “{{claimed}}” is just a name again.', '{{name}} останува во групата. „{{claimed}}“ повторно е само име.'],
  ['members.thatsMe', "That's me", 'Тоа сум јас'],
  ['members.claimed', 'You are now “{{name}}” in this group.', 'Сега си „{{name}}“ во оваа група.'],
  ['members.removedTitle', 'Removed', 'Отстранети'],
  ['members.letBackIn', 'Let back in', 'Врати во групата'],
  ['members.backInGroup', '{{name}} is back in the group', '{{name}} е назад во групата'],
  ['members.optionsFor', 'Options for {{name}}', 'Опции за {{name}}'],
  ['join.invitedTo', "You're invited to {{name}}", 'Те поканија во групата „{{name}}“'],
  ['join.inGroup', 'In the group', 'Во групата'],
  ['join.join', 'Join', 'Придружи се'],
  ['join.areYou', 'Are you one of these?', 'Дали си некој од овие?'],
  ['join.imNew', "No, I'm new", 'Не, прв пат сум тука'],
  ['join.joined', 'You joined {{name}}', 'Се придружи на групата „{{name}}“'],
  ['join.signInHint', 'Create an account or log in to join.', 'Направи профил или најави се за да се придружиш.'],
  ['join.removed', 'You were removed from {{name}}. Ask the owner to let you back in.', 'Те отстранија од групата „{{name}}“. Побарај од сопственикот да те врати.'],
  ['errors.MEMBER_NOT_FOUND', 'This person is no longer in the group.', 'Оваа личност веќе не е во групата.'],
  ['errors.MEMBER_IS_OWNER', "The owner can't be removed.", 'Сопственикот не може да се отстрани.'],
  ['errors.MEMBER_NOT_ACCOUNT', 'Only a person with a Kvit account can become the owner.', 'Само личност со Kvit профил може да стане сопственик.'],
  ['errors.MEMBER_ALREADY_OWNER', 'This person is already the owner.', 'Оваа личност веќе е сопственик.'],
  ['errors.MEMBER_CANNOT_CLAIM', "You can't take this name.", 'Не можеш да го земеш ова име.'],
  ['errors.MEMBER_NOT_CLAIMED', 'This name was not taken by anyone.', 'Ова име не го зел никој.'],
  ['errors.INVITE_NOT_FOUND', 'This invite link no longer works. Ask for a new one.', 'Овој линк за покана повеќе не работи. Побарај нов.'],
  ['errors.INVITE_REMOVED', 'You were removed from this group. Ask the owner to let you back in.', 'Те отстранија од оваа група. Побарај од сопственикот да те врати.'],
  ['errors.INVITE_NOTHING_TO_UNDO', 'There is no earlier link to go back to.', 'Нема претходен линк за враќање.'],
]

const approvedPhase7Step6Texts: [string, string, string][] = [
  ['groups.goToGroups', 'Go to Groups', 'Кон групите'],
]

const approvedPhase8Step1Texts: [string, string, string][] = [
  ['errors.MONEY_CURRENCY_MISMATCH', 'These amounts are in different currencies.', 'Износите се во различни валути.'],
  ['errors.MONEY_NOT_ON_CURRENCY_STEP', 'Denars have no decimals. Use a whole number.', 'Денарите немаат децимали. Внеси цел број.'],
  ['errors.EXPENSE_AMOUNT_NOT_POSITIVE', 'The amount must be more than 0.', 'Износот мора да е поголем од 0.'],
  ['errors.EXPENSE_SPLIT_NO_PARTICIPANTS', 'Pick at least one person to split with.', 'Избери барем една личност за делење.'],
  ['errors.EXPENSE_SPLIT_DUPLICATE_MEMBER', 'The same person is in the split twice.', 'Истата личност е два пати во поделбата.'],
  ['errors.EXPENSE_SPLIT_NEGATIVE_INPUT', "Amounts, percentages and shares can't be negative.", 'Износите, процентите и деловите не можат да бидат негативни.'],
  ['errors.EXPENSE_SPLIT_EXTRAS_EXCEED_TOTAL', 'The extras are more than the total.', 'Додатоците се повеќе од вкупниот износ.'],
  ['errors.EXPENSE_SPLIT_DOES_NOT_ADD_UP', "The split doesn't add up to the total.", 'Поделбата не е иста како вкупниот износ.'],
  ['errors.EXPENSE_SPLIT_NO_SHARES', 'Give at least one person a share.', 'Дади барем на една личност дел.'],
]

const approvedPhase8Step2Texts: [string, string, string][] = [
  ['errors.EXPENSE_NOT_FOUND', "This expense doesn't exist or was deleted.", 'Овој трошок не постои или е избришан.'],
  ['errors.EXPENSE_NOT_ALLOWED', "Only the person who added this expense or the group's owner can do this.", 'Ова може само личноста што го додала трошокот или сопственикот на групата.'],
  ['errors.EXPENSE_NOT_DELETED', 'This expense is not deleted.', 'Овој трошок не е избришан.'],
  ['errors.EXPENSE_RESTORE_EXPIRED', "This expense was deleted more than 5 days ago and can't be restored.", 'Овој трошок е избришан пред повеќе од 5 дена и не може да се врати.'],
  ['errors.EXPENSE_TITLE_INVALID', 'The title can have up to 80 characters.', 'Насловот може да има најмногу 80 знаци.'],
  ['errors.EXPENSE_NOTE_INVALID', 'The note can have up to 500 characters.', 'Белешката може да има најмногу 500 знаци.'],
  ['errors.EXPENSE_DATE_INVALID', 'Pick a date between 2000 and one year from now.', 'Избери датум од 2000 до една година однапред.'],
  ['errors.EXPENSE_CURRENCY_INVALID', 'Pick MKD or EUR.', 'Избери MKD или EUR.'],
  ['errors.EXPENSE_SPLIT_TYPE_INVALID', 'Pick how to split: equally, exact amounts, percentages or shares.', 'Избери како да се подели: поеднакво, точни износи, проценти или делови.'],
  ['errors.EXPENSE_CATEGORY_INVALID', 'Pick a category from the list.', 'Избери категорија од листата.'],
  ['errors.EXPENSE_AMOUNT_TOO_LARGE', 'This amount is too large.', 'Овој износ е премногу голем.'],
  ['errors.EXPENSE_CLIENT_REQUEST_ID_USED', 'This request was already used for something else.', 'Ова барање веќе е искористено за друго.'],
]

const approvedPhase8Step4Texts: [string, string, string][] = [
  ['expenses.title', 'Expenses', 'Трошоци'],
  ['expenses.empty', 'No expenses yet. Tap + to add the first one.', 'Сè уште нема трошоци. Допри + за да додадеш.'],
  ['expenses.paidByShare', 'Paid by {{name}} · your share {{amount}}', 'Платено од {{name}} · твој дел {{amount}}'],
  ['expenses.paidByNotInSplit', "Paid by {{name}} · you're not in the split", 'Платено од {{name}} · не си во поделбата'],
  ['expenses.today', 'Today', 'Денес'],
  ['expenses.yesterday', 'Yesterday', 'Вчера'],
  ['expenses.saved', 'Expense saved', 'Трошокот е зачуван'],
  ['expenses.deleted', 'Expense deleted', 'Трошокот е избришан'],
  ['expenses.restored', 'Expense restored', 'Трошокот е вратен'],
  ['expenses.deletedEmpty', 'Nothing deleted in the last 5 days.', 'Ништо не е избришано во последните 5 дена.'],
  ['expense.addTitle', 'Add expense', 'Додади трошок'],
  ['expense.editTitle', 'Edit expense', 'Измени трошок'],
  ['expense.paidBy', 'Paid by', 'Платено од'],
  ['expense.split', 'Split', 'Поделба'],
  ['expense.date', 'Date', 'Датум'],
  ['expense.category', 'Category', 'Категорија'],
  ['expense.titleField', 'Title (optional)', 'Наслов (по желба)'],
  ['expense.note', 'Note', 'Белешка'],
  ['expense.me', 'me', 'јас'],
  ['expense.equally', 'equally', 'поеднакво'],
  ['expense.everyone', 'everyone', 'сите'],
  ['expense.done', 'Done', 'Готово'],
  ['expense.splitEqual', 'Equal', 'Поеднакво'],
  ['expense.splitExact', 'Exact', 'Точно'],
  ['expense.splitPercentage', '%', '%'],
  ['expense.splitShares', 'Shares', 'Делови'],
  ['expense.extra', '+ extra', '+ додаток'],
  ['expense.amountLeft', '{{amount}} left to assign', 'Уште {{amount}} да се распредели'],
  ['expense.percentLeft', '{{percent}} % left', 'Уште {{percent}} %'],
  ['expense.pickCategory', 'Pick a category', 'Избери категорија'],
  ['expense.pickDate', 'Pick a date', 'Избери датум'],
  ['expense.dateHint', 'Any date, up to one year ahead.', 'Било кој датум, до една година однапред.'],
  ['expense.history', 'History', 'Историја'],
  ['expense.rate', 'Rate: 1 EUR = {{rate}} MKD · {{date}}', 'Цена на еврото: 1 EUR = {{rate}} MKD · {{date}}'],
  ['expense.edit', 'Edit', 'Измени'],
  ['expense.delete', 'Delete', 'Избриши'],
  ['expense.currencyToggle', 'Currency: {{currency}}. Tap to change.', 'Валута: {{currency}}. Допри за промена.'],
  ['expense.amount', 'Amount', 'Износ'],
  ['expense.paidByOn', 'Paid by {{name}} · {{date}}', 'Платено од {{name}} · {{date}}'],
  ['expense.historyAdded', '{{name}} added it', '{{name}} го додаде'],
  ['expense.historyChangedAmount', '{{name}} changed the amount: {{old}} → {{new}}', '{{name}} го измени износот: {{old}} → {{new}}'],
  ['expense.historyChangedCurrency', '{{name}} changed the currency: {{old}} → {{new}}', '{{name}} ја измени валутата: {{old}} → {{new}}'],
  ['expense.historyChangedTitle', '{{name}} changed the title: {{old}} → {{new}}', '{{name}} го измени насловот: {{old}} → {{new}}'],
  ['expense.historyChangedNote', '{{name}} changed the note: {{old}} → {{new}}', '{{name}} ја измени белешката: {{old}} → {{new}}'],
  ['expense.historyChangedDate', '{{name}} changed the date: {{old}} → {{new}}', '{{name}} го измени датумот: {{old}} → {{new}}'],
  ['expense.historyChangedCategory', '{{name}} changed the category: {{old}} → {{new}}', '{{name}} ја измени категоријата: {{old}} → {{new}}'],
  ['expense.historyChangedPaidBy', '{{name}} changed who paid: {{old}} → {{new}}', '{{name}} измени кој платил: {{old}} → {{new}}'],
  ['expense.historyChangedSplit', '{{name}} changed the split', '{{name}} ја измени поделбата'],
  ['expense.historyDeleted', '{{name}} deleted it', '{{name}} го избриша'],
  ['expense.historyRestored', '{{name}} restored it', '{{name}} го врати'],
  ['categories.food', 'Food & drinks', 'Храна и пијалоци'],
  ['categories.groceries', 'Groceries', 'Намирници'],
  ['categories.transport', 'Transport', 'Превоз'],
  ['categories.accommodation', 'Accommodation', 'Сместување'],
  ['categories.fun', 'Fun', 'Забава'],
  ['categories.shopping', 'Shopping', 'Купување'],
  ['categories.bills', 'Bills', 'Сметки'],
  ['categories.health', 'Health', 'Здравје'],
  ['categories.gifts', 'Gifts', 'Подароци'],
  ['categories.other', 'Other', 'Друго'],
]

const expenseNamespaces = /^(expenses|expense|categories)\./

const currencyCodes = /MKD|EUR/g
const placeholders = /\{\{\w+\}\}/g

const textsWhereMacedonianMeansConnection =['errors.network', 'welcome.googleUnavailable']

const macedonianWordForConnection = 'врск'

const pluralCounts: [number, string, string][] = [
  [1, '1 person', '1 лице'],
  [2, '2 people', '2 лица'],
  [11, '11 people', '11 лица'],
  [21, '21 people', '21 лице'],
  [101, '101 people', '101 лице'],
]

const removedKeys = ['welcome.pitch', 'common.comingSoon', 'group.expensesSoon']

describe('the approved Phase 6 texts', () => {
  it.each(approvedTexts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(removedKeys.flatMap((key) => (['en', 'mk'] as const).map((language) => [language, key] as const)))('no longer has a %s text for %s', (language, key) => {
    expect(() => translated(language, key)).toThrow(key)
  })
})

describe('the approved Phase 7 texts', () => {
  it.each(approvedPhase7Texts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(pluralCounts)('writes the group size %i as the English "%s" and the Macedonian "%s"', async (count, english, macedonian) => {
    const englishI18n = await createI18n('en')
    const macedonianI18n = await createI18n('mk')

    expect(englishI18n.t('groups.people', { count })).toBe(english)
    expect(macedonianI18n.t('groups.people', { count })).toBe(macedonian)
  })
})

describe('the approved Phase 7 Step 5 texts', () => {
  it.each(approvedPhase7Step5Texts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })
})

describe('the approved Phase 7 Step 6 texts', () => {
  it.each(approvedPhase7Step6Texts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })
})

describe('the approved Phase 8 Step 1 texts', () => {
  it.each(approvedPhase8Step1Texts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(approvedPhase8Step1Texts)('writes the Macedonian text of %s with Cyrillic letters only, no Latin look-alikes', (key, _english, macedonian) => {
    expect(macedonian).not.toMatch(/[A-Za-z]/)
    expect(translated('mk', key)).not.toMatch(/[A-Za-z]/)
  })
})

describe('the approved Phase 8 Step 2 texts', () => {
  it.each(approvedPhase8Step2Texts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(approvedPhase8Step2Texts)('writes the Macedonian text of %s with Cyrillic letters only apart from the currency codes, no Latin look-alikes', (key, _english, macedonian) => {
    expect(macedonian.replace(currencyCodes, '')).not.toMatch(/[A-Za-z]/)
    expect(translated('mk', key).replace(currencyCodes, '')).not.toMatch(/[A-Za-z]/)
  })
})

describe('the approved Phase 8 Step 4 texts', () => {
  it.each(approvedPhase8Step4Texts)('has the approved English and Macedonian wording for %s', (key, english, macedonian) => {
    expect(translated('en', key)).toBe(english)
    expect(translated('mk', key)).toBe(macedonian)
  })

  it.each(approvedPhase8Step4Texts)('writes the Macedonian text of %s with Cyrillic letters only apart from the placeholders and the currency codes, no Latin look-alikes', (key, _english, macedonian) => {
    expect(macedonian.replace(placeholders, '').replace(currencyCodes, '')).not.toMatch(/[A-Za-z]/)
    expect(translated('mk', key).replace(placeholders, '').replace(currencyCodes, '')).not.toMatch(/[A-Za-z]/)
  })

  it.each(['en', 'mk'] as const)('has no text under expenses, expense or categories in %s that is not listed above, so every new line is locked', (language) => {
    const texts = language === 'en' ? en : mk
    const keysInTheFile = collectTexts(texts)
      .map(([path]) => path)
      .filter((path) => expenseNamespaces.test(path))
      .sort()
    const keysListedAbove = approvedPhase8Step4Texts
      .map(([key]) => key)
      .sort()

    expect(keysInTheFile).toEqual(keysListedAbove)
  })

  it('writes the rate with a dot in English and a comma in Macedonian, with the exchange rate and the date filled in', () => {
    expect(translated('en', 'expense.rate', { rate: '61.5610', date: '24 Sep 2026' })).toBe(
      'Rate: 1 EUR = 61.5610 MKD · 24 Sep 2026',
    )
    expect(translated('mk', 'expense.rate', { rate: '61,5610', date: '24 сеп. 2026 г.' })).toBe(
      'Цена на еврото: 1 EUR = 61,5610 MKD · 24 сеп. 2026 г.',
    )
  })

  it('writes the history arrow between the old and the new value in every change sentence', () => {
    const changeKeys = ['Amount', 'Currency', 'Title', 'Note', 'Date', 'Category', 'PaidBy']

    for (const key of changeKeys) {
      for (const language of ['en', 'mk'] as const) {
        const text = translated(language, `expense.historyChanged${key}`, { name: 'N', old: 'A', new: 'B' })
        expect(text).toContain('A → B')
      }
    }
  })

  it('writes the split change without old and new values', () => {
    expect(translated('en', 'expense.historyChangedSplit', { name: 'N', old: 'A', new: 'B' })).not.toContain('A')
    expect(translated('mk', 'expense.historyChangedSplit', { name: 'N', old: 'A', new: 'B' })).not.toContain('→')
  })

  it('does not call the exchange rate «Курс» in any Macedonian text, which Filip does not like', () => {
    const textsWithTheWord = collectTexts(mk)
      .filter(([, text]) => typeof text === 'string' && text.toLowerCase().includes('курс'))
      .map(([path]) => path)

    expect(textsWithTheWord).toEqual([])
  })

  it('never writes the singular «Активност» for the Activity tab in any Macedonian text', () => {
    const activityTexts = collectTexts(mk)
      .filter(([, text]) => typeof text === 'string' && /Активност(?!и)/.test(text))
      .map(([path]) => path)

    expect(activityTexts).toEqual([])
  })
})

describe('the Macedonian word for a link', () => {
  it.each(textsWhereMacedonianMeansConnection)('still says "врската" in %s, where it means an internet connection', (key) => {
    expect(translated('mk', key)).toContain('врската')
  })

  it('is never "врска" in any Macedonian text except the two that mean an internet connection', () => {
    const textsWithTheWord = collectTexts(mk)
      .filter(([, text]) => typeof text === 'string' && text.toLowerCase().includes(macedonianWordForConnection))
      .map(([path]) => path)

    expect(textsWithTheWord.sort()).toEqual([...textsWhereMacedonianMeansConnection].sort())
  })
})
