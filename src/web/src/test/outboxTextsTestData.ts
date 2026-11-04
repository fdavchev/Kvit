import { pluralForm } from '@/core/i18n/pluralForm'
import type { Language } from '@/core/i18n/language'

export interface OutboxText {
  key: string
  en: string
  mk: string
}

export const outboxTexts: readonly OutboxText[] = [
  { key: 'outbox.notSynced', en: 'Not synced yet', mk: 'Чека праќање' },
  { key: 'outbox.rateWhenSent', en: 'Euro price set when sent', mk: 'Цената на еврото се одредува при праќање' },
  { key: 'outbox.sending', en: 'Sending…', mk: 'Се праќа…' },
  { key: 'outbox.upToDate', en: 'All up to date ✓', mk: 'Сѐ е ажурирано ✓' },
  { key: 'outbox.waiting_one', en: '{{count}} change waiting', mk: '{{count}} промена чека' },
  { key: 'outbox.waiting_other', en: '{{count}} changes waiting', mk: '{{count}} промени чекаат' },
  {
    key: 'outbox.failed_one',
    en: "{{count}} change couldn't be saved",
    mk: '{{count}} промена не можеше да се зачува',
  },
  {
    key: 'outbox.failed_other',
    en: "{{count}} changes couldn't be saved",
    mk: '{{count}} промени не можеа да се зачуваат',
  },
  { key: 'outbox.review', en: 'Review', mk: 'Прегледај' },
  { key: 'outbox.retry', en: 'Retry', mk: 'Обиди се повторно' },
  { key: 'outbox.discard', en: 'Discard', mk: 'Отфрли' },
  {
    key: 'outbox.notCounted_one',
    en: '{{count}} change waiting, not counted yet',
    mk: '{{count}} промена чека, сѐ уште не се брои',
  },
  {
    key: 'outbox.notCounted_other',
    en: '{{count}} changes waiting, not counted yet',
    mk: '{{count}} промени чекаат, сѐ уште не се бројат',
  },
  { key: 'outbox.discardTitle', en: 'Discard this change?', mk: 'Да ја отфрлиш оваа промена?' },
  {
    key: 'outbox.discardBody',
    en: 'It has not been confirmed yet. If you discard it, it may still arrive.',
    mk: 'Сѐ уште не е потврдена. Ако ја отфрлиш, можеби сепак ќе пристигне.',
  },
  {
    key: 'outbox.failedSheetTitle',
    en: "Changes that couldn't be saved",
    mk: 'Промени што не можеа да се зачуваат',
  },
  { key: 'outbox.logOutTitle', en: 'Changes not sent yet', mk: 'Промени што не се испратени' },
  {
    key: 'outbox.logOutBody_one',
    en: "{{count}} change hasn't been sent yet. If you log out now it is lost.",
    mk: '{{count}} промена не е испратена. Ако се одјавиш сега, се губи.',
  },
  {
    key: 'outbox.logOutBody_other',
    en: "{{count}} changes haven't been sent yet. If you log out now they are lost.",
    mk: '{{count}} промени не се испратени. Ако се одјавиш сега, се губат.',
  },
  { key: 'outbox.logOutAnyway', en: 'Log out anyway', mk: 'Сепак одјави се' },
  { key: 'outbox.stay', en: 'Stay', mk: 'Остани' },
]

function textOf(key: string): OutboxText {
  const found = outboxTexts.find((text) => text.key === key)
  if (found === undefined) {
    throw new Error(`The outbox test texts have no entry for the key "${key}"`)
  }
  return found
}

export function outboxText(language: Language, key: string, count?: number): string {
  if (count === undefined) {
    return textOf(key)[language]
  }
  return textOf(`${key}_${pluralForm(count, language)}`)[language].replaceAll('{{count}}', String(count))
}
