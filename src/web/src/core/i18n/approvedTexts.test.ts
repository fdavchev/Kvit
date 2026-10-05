import { describe, expect, it } from 'vitest'
import { translated } from '@/test/translated'
import { createI18n } from './i18n'

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
  ['privacy.sections.3.body', 'If you use Google, Kvit receives only your name, email address and profile picture link. Kvit cannot read your Gmail, your contacts or anything else in your Google account.', 'Ако користиш Google, Kvit добива само твоето име, адреса на е-пошта и врска до профилната слика. Kvit не може да ја чита твојата Gmail-пошта, контактите или нешто друго во твојот Google профил.'],
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
  ['groups.empty', 'You have no groups yet. Create one, or open a link a friend sent you.', 'Сè уште немаш групи. Направи една или отвори врска што ти ја испратил пријател.'],
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
  ['group.addPeopleHasKvit', 'Have Kvit? Share the link and they join with their own account.', 'Имаат Kvit? Сподели ја врската и ќе се придружат со свој профил.'],
  ['group.addName', 'Add a name', 'Додај име'],
  ['group.shareLink', 'Share invite link', 'Сподели ја врската за покана'],
  ['group.linkCopied', 'Link copied', 'Врската е копирана'],
  ['group.settings', 'Group settings', 'Поставки на групата'],
  ['group.expensesSoon', 'Expenses will show up here soon.', 'Овде наскоро ќе се појавуваат трошоците.'],
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
  ['errors.MEMBER_NAME_TAKEN', "There's already a {{name}} in this group.", 'Во оваа група веќе има {{name}}.'],
  ['errors.MEMBER_OWNER_CANNOT_LEAVE', 'You are the owner. Make someone else the owner before you leave.', 'Ти си сопственик. Направи некој друг сопственик пред да ја напуштиш.'],
]

const pluralCounts: [number, string, string][] = [
  [1, '1 person', '1 лице'],
  [2, '2 people', '2 лица'],
  [11, '11 people', '11 лица'],
  [21, '21 people', '21 лице'],
  [101, '101 people', '101 лице'],
]

const removedKeys = ['welcome.pitch', 'common.comingSoon']

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
