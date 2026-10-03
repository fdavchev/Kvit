import { describe, expect, it } from 'vitest'
import { translated } from '@/test/translated'

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
