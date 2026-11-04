import { describe, expect, it } from 'vitest'
import { collectTexts } from '@/test/localeTexts'
import en from './locales/en.json'
import mk from './locales/mk.json'

const namespacesAboutPayments = [
  'settlements',
  'balances',
  'closing',
  'groupTotals',
  'groupPlan',
  'home',
  'welcome',
  'group',
  'groups',
]

const scannedKeys = new RegExp(
  `^(${namespacesAboutPayments.join('|')})\\.|^activity\\.(settlement|closing)|^errors\\.(SETTLEMENT|CLOSING|GROUP)_`,
)

const bannedEnglishWords = [
  'send money',
  'transfer',
  'collect',
  'wallet',
  'kitty',
  'interest',
  'fee',
  'fees',
  'pay now',
  'payment link',
]

const bannedMacedonianStems = [
  'пренес',
  'префрл',
  'собер',
  'наплат',
  'паричник',
  'касичка',
  'камат',
  'провизија',
  'такса',
]

const bannedEnglish = new RegExp(`\\b(${bannedEnglishWords.join('|')})\\b`, 'i')
const bannedMacedonian = new RegExp(bannedMacedonianStems.join('|'), 'i')

function scannedTexts(texts: object): [string, string][] {
  return collectTexts(texts).flatMap(([path, text]): [string, string][] =>
    scannedKeys.test(path) && typeof text === 'string' ? [[path, text]] : [],
  )
}

function keysThatSayMovingMoney(texts: object, banned: RegExp): string[] {
  return scannedTexts(texts)
    .filter(([, text]) => banned.test(text))
    .map(([path]) => path)
}

describe('the texts about payments and balances only record, they never move money', () => {
  it('has no English text that talks about sending, transferring or collecting money, a wallet, a kitty, interest, fees or a payment link', () => {
    expect(keysThatSayMovingMoney(en, bannedEnglish)).toEqual([])
  })

  it('has no Macedonian text that talks about transferring or collecting money, a wallet, a kitty, interest, a commission or a fee', () => {
    expect(keysThatSayMovingMoney(mk, bannedMacedonian)).toEqual([])
  })

  it.each(namespacesAboutPayments)('scans at least one English and one Macedonian text under %s', (namespace) => {
    const startsWithNamespace = (path: string): boolean => path.startsWith(`${namespace}.`)

    expect(scannedTexts(en).filter(([path]) => startsWithNamespace(path)).length).toBeGreaterThan(0)
    expect(scannedTexts(mk).filter(([path]) => startsWithNamespace(path)).length).toBeGreaterThan(0)
  })

  it.each(['activity.settlementRecorded', 'activity.closingStarted', 'errors.SETTLEMENT_NOT_FOUND', 'errors.CLOSING_ALREADY_ANSWERED', 'errors.GROUP_NOT_KVIT'])('scans the text %s', (key) => {
    expect(scannedKeys.test(key)).toBe(true)
  })

  it.each(['terms.sections.3.body', 'privacy.sections.2.body', 'legal.ratesSource', 'common.switchToDarkMode'])('does not scan %s, where the legal texts say that Kvit does not hold, send or receive money', (key) => {
    expect(scannedKeys.test(key)).toBe(false)
  })

  it.each([
    'Send money to Ana',
    'Transfer 500 MKD',
    'Collect the money',
    'Your wallet',
    'The kitty is empty',
    'Interest is added',
    'A small fee applies',
    'No fees',
    'Pay now',
    'Open the payment link',
  ])('finds a banned English word in "%s"', (text) => {
    expect(bannedEnglish.test(text)).toBe(true)
  })

  it.each([
    'Ana recorded a payment',
    'Waits until Ana confirms',
    'Send',
    'Share invite link',
    'Convert to EUR',
    'Everyone is kvit',
    'Feel free to add a name',
  ])('finds no banned English word in "%s"', (text) => {
    expect(bannedEnglish.test(text)).toBe(false)
  })

  it.each([
    'Пренеси пари',
    'Префрли 500 ден.',
    'Собери ги парите',
    'Наплати',
    'Твојот паричник',
    'Касичка',
    'Камата се додава',
    'Провизија за секоја уплата',
    'Една такса',
  ])('finds a banned Macedonian stem in "%s"', (text) => {
    expect(bannedMacedonian.test(text)).toBe(true)
  })

  it.each([
    'Ана запиша плаќање',
    'Чека Ана да потврди',
    'Сподели линк за покана',
    'Сите се квит',
  ])('finds no banned Macedonian stem in "%s"', (text) => {
    expect(bannedMacedonian.test(text)).toBe(false)
  })
})
